'use server';

import { Account, Client, ID, Models, Query } from 'node-appwrite';
import { cookies } from 'next/headers';

import { createAdminClient, createSessionClient } from '../appwrite';
import {
  getMockBankAccount,
  parseStringify,
  signInSchema,
  signUpSchema,
} from '../utils';
import { SignUpParams, User } from '@/types';

const {
  APPWRITE_DATABASE_ID: DATABASE_ID,
  APPWRITE_USER_COLLECTION_ID: USER_COLLECTION_ID,
  APPWRITE_BANK_COLLECTION_ID: BANK_COLLECTION_ID,
} = process.env;

const createPublicAccountClient = () => {
  const client = new Client()
    .setEndpoint(process.env.NEXT_PUBLIC_APPWRITE_ENDPOINT!)
    .setProject(process.env.NEXT_PUBLIC_APPWRITE_PROJECT!);

  return new Account(client);
};

const setSessionCookie = (secret: string) => {
  cookies().set('appwrite-session', secret, {
    path: '/',
    httpOnly: true,
    sameSite: 'strict',
    secure: process.env.NODE_ENV === 'production',
    maxAge: 60 * 60 * 24 * 7,
  });
};

const mapAuthenticationError = (error: any, fallback: string) => {
  if (error?.code === 401) {
    return 'Invalid email or password.';
  }

  if (error?.code === 409) {
    return 'An account with this email address already exists.';
  }

  if (error?.code === 429) {
    return 'Too many attempts. Please try again later.';
  }

  return fallback;
};

export const signIn = async ({ email, password }: { email: string; password: string }) => {
  const validation = signInSchema.safeParse({ email, password });
  if (!validation.success) {
    throw new Error(validation.error.issues[0]?.message ?? 'Invalid sign-in details.');
  }

  try {
    const account = createPublicAccountClient();
    const session = await account.createEmailPasswordSession(
      validation.data.email,
      validation.data.password
    );

    setSessionCookie(session.secret);

    const user = await getUserInfo({ userId: session.userId });
    if (!user) {
      cookies().delete('appwrite-session');
      throw new Error('Your profile could not be loaded. Please contact support.');
    }

    return parseStringify(user);
  } catch (error: any) {
    console.error('Sign-in failed', {
      code: error?.code,
      type: error?.type,
    });

    if (error instanceof Error && error.message.includes('profile could not be loaded')) {
      throw error;
    }

    throw new Error(mapAuthenticationError(error, 'Sign-in failed. Please try again.'));
  }
};

export const signUp = async (userData: SignUpParams) => {
  const validation = signUpSchema.safeParse(userData);
  if (!validation.success) {
    throw new Error(validation.error.issues[0]?.message ?? 'Registration details are invalid.');
  }

  const {
    email,
    password,
    firstName,
    lastName,
    mobileNumber,
    address1,
    suburb,
    city,
    province,
    postalCode,
    dateOfBirth,
    country,
  } = validation.data;

  const adminClient = await createAdminClient();
  const { account, database, users } = adminClient;
  let authUserId: string | null = null;
  let userDocumentId: string | null = null;
  let bankDocumentId: string | null = null;

  try {
    const newUserAccount = await account.create(
      ID.unique(),
      email,
      password,
      `${firstName} ${lastName}`
    );

    authUserId = newUserAccount.$id;
    const consentTimestamp = new Date().toISOString();

    const newUser = await database.createDocument(
      DATABASE_ID!,
      USER_COLLECTION_ID!,
      ID.unique(),
      {
        email,
        firstName,
        lastName,
        userId: authUserId,
        mobileNumber,
        address1,
        suburb,
        city,
        province,
        postalCode,
        dateOfBirth,
        country,
        termsAcceptedAt: consentTimestamp,
        privacyAcceptedAt: consentTimestamp,
      }
    );
    userDocumentId = newUser.$id;

    const mockBank = getMockBankAccount(email);
    const newBank = await database.createDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      ID.unique(),
      {
        userId: authUserId,
        bankId: mockBank.accountId,
        accountId: mockBank.accountId,
        accountNumber: mockBank.accountNumber,
        branchCode: mockBank.branchCode,
        bankName: mockBank.bankName,
        balance: mockBank.balance,
        currency: mockBank.currency,
        linkedAt: mockBank.linkedAt,
      }
    );
    bankDocumentId = newBank.$id;

    const publicAccount = createPublicAccountClient();
    const session = await publicAccount.createEmailPasswordSession(email, password);
    setSessionCookie(session.secret);

    return parseStringify(newUser);
  } catch (error: any) {
    // Registration should behave as one unit. Remove partial demo data when a
    // later step fails so the user can retry without orphaned records.
    if (bankDocumentId) {
      await database
        .deleteDocument(DATABASE_ID!, BANK_COLLECTION_ID!, bankDocumentId)
        .catch(() => undefined);
    }

    if (userDocumentId) {
      await database
        .deleteDocument(DATABASE_ID!, USER_COLLECTION_ID!, userDocumentId)
        .catch(() => undefined);
    }

    if (authUserId) {
      await users.delete(authUserId).catch(() => undefined);
    }

    cookies().delete('appwrite-session');

    console.error('Sign-up failed', {
      code: error?.code,
      type: error?.type,
    });

    throw new Error(
      mapAuthenticationError(error, 'Account creation failed. Please try again.')
    );
  }
};

export const getUserInfo = async ({ userId }: { userId: string }) => {
  try {
    const { database } = await createAdminClient();
    const user = await database.listDocuments(
      DATABASE_ID!,
      USER_COLLECTION_ID!,
      [Query.equal('userId', userId)]
    );

    if (!user.documents[0]) {
      return null;
    }

    return parseStringify(user.documents[0]);
  } catch (error: any) {
    console.error('Unable to load user profile', {
      code: error?.code,
      type: error?.type,
    });
    return null;
  }
};

export const getLoggedInUser = async () => {
  try {
    const sessionClient = await createSessionClient();
    if (!sessionClient) {
      return null;
    }

    const { account } = sessionClient;
    const session: Models.Session = await account.getSession('current');
    const user = await getUserInfo({ userId: session.userId });

    return user ? parseStringify(user) : null;
  } catch (error: any) {
    console.error('Unable to load the current session', {
      code: error?.code,
      type: error?.type,
    });
    return null;
  }
};

export const getUserBankAccount = async (user: User) => {
  try {
    const { database } = await createAdminClient();
    const bank = await database.listDocuments(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      [Query.equal('userId', user.userId)]
    );

    return bank.documents[0]
      ? parseStringify(bank.documents[0])
      : getMockBankAccount(user.email);
  } catch (error: any) {
    console.error('Unable to load the demo bank account', {
      code: error?.code,
      type: error?.type,
    });
    return getMockBankAccount(user.email);
  }
};

export const logoutAccount = async () => {
  try {
    const sessionClient = await createSessionClient();
    if (sessionClient) {
      await sessionClient.account.deleteSession('current');
    }

    cookies().delete('appwrite-session');
    return true;
  } catch (error: any) {
    console.error('Sign-out failed', {
      code: error?.code,
      type: error?.type,
    });
    return false;
  }
};

export const createMockBank = async ({ userId, email }: { userId: string; email: string }) => {
  try {
    const { database } = await createAdminClient();
    const mockBank = getMockBankAccount(email);
    const newBank = await database.createDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      ID.unique(),
      {
        userId,
        bankId: mockBank.accountId,
        accountId: mockBank.accountId,
        accountNumber: mockBank.accountNumber,
        branchCode: mockBank.branchCode,
        bankName: mockBank.bankName,
        balance: mockBank.balance,
        currency: mockBank.currency,
        linkedAt: mockBank.linkedAt,
      }
    );

    return parseStringify(newBank);
  } catch (error: any) {
    console.error('Unable to create a demo bank account', {
      code: error?.code,
      type: error?.type,
    });
    return null;
  }
};
