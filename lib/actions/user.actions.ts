'use server';
import { Client, Account, Databases, ID, Query, Models } from 'node-appwrite';
import { cookies } from 'next/headers';
import { revalidatePath } from 'next/cache';
import { createAdminClient, createSessionClient } from '../appwrite';
import { getMockBankAccount, parseStringify } from '../utils';
import { SignUpParams, User } from '@/types';

const {
  APPWRITE_DATABASE_ID: DATABASE_ID,
  APPWRITE_USER_COLLECTION_ID: USER_COLLECTION_ID,
  APPWRITE_BANK_COLLECTION_ID: BANK_COLLECTION_ID,
} = process.env;

export const signIn = async ({ email, password }: { email: string; password: string }) => {
  try {
    console.log('Sign-in attempt with:', { email, password });
    console.log('Environment:', {
      endpoint: process.env.NEXT_PUBLIC_APPWRITE_ENDPOINT,
      project: process.env.NEXT_PUBLIC_APPWRITE_PROJECT,
    });
    const client = new Client()
      .setEndpoint(process.env.NEXT_PUBLIC_APPWRITE_ENDPOINT!)
      .setProject(process.env.NEXT_PUBLIC_APPWRITE_PROJECT!);
    const account = new Account(client);
    const session = await account.createEmailPasswordSession(email, password);
    console.log('Session created:', session);

    // Set the session cookie
    cookies().set('appwrite-session', session.secret, {
      path: '/',
      httpOnly: true,
      sameSite: 'strict',
      secure: process.env.NODE_ENV === 'production', // Only secure in production
      maxAge: 31536000, // 1 year
    });
    console.log('Cookie set:', cookies().get('appwrite-session'));

    const user = await getUserInfo({ userId: session.userId });
    if (!user) {
      throw new Error('User not found in database. Please sign up first.');
    }
    return parseStringify(user);
  } catch (error: any) {
    console.error('Sign-in error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    if (error.code === 401) {
      throw new Error('Invalid email or password. Please check your credentials or reset your password.');
    }
    throw new Error(error.message || 'Sign-in failed. Please try again.');
  }
};

export const signUp = async (userData: SignUpParams) => {
  const { email, password, firstName, lastName, address1, city, state, postalCode, dateOfBirth, ssn } = userData;
  try {
    console.log('Sign-up attempt with:', { email, firstName, lastName });
    const { account, database } = await createAdminClient();

    // Check if user already exists
    try {
      await account.createEmailPasswordSession(email, password);
      throw new Error('An account with this email already exists. Please sign in.');
    } catch (error: any) {
      if (error.code !== 401) {
        throw new Error('Failed to check existing user: ' + error.message);
      }
    }

    // Create new user account
    const newUserAccount = await account.create(
      ID.unique(),
      email,
      password,
      `${firstName} ${lastName}`
    );
    if (!newUserAccount) {
      throw new Error('Failed to create user account');
    }

    // Create mock bank
    const mockBank = getMockBankAccount(email);
    const newUser = await database.createDocument(
      DATABASE_ID!,
      USER_COLLECTION_ID!,
      ID.unique(),
      {
        email,
        firstName,
        lastName,
        userId: newUserAccount.$id,
        address1,
        city,
        state,
        postalCode,
        dateOfBirth,
        ssn,
      }
    );

    await database.createDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      ID.unique(),
      {
        userId: newUserAccount.$id,
        bankId: mockBank.accountId,
        accountId: mockBank.accountId,
        accountNumber: mockBank.accountNumber,
        routingNumber: mockBank.routingNumber,
        bankName: mockBank.bankName,
        balance: mockBank.balance,
        currency: mockBank.currency,
        linkedAt: mockBank.linkedAt,
      }
    );

    // Create session
    const session = await account.createEmailPasswordSession(email, password);
    console.log('Sign-up session created:', session);
    cookies().set('appwrite-session', session.secret, {
      path: '/',
      httpOnly: true,
      sameSite: 'strict',
      secure: process.env.NODE_ENV === 'production', // Only secure in production
      maxAge: 31536000,
    });
    console.log('Cookie set:', cookies().get('appwrite-session'));

    return parseStringify(newUser);
  } catch (error: any) {
    console.error('Sign-up error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    throw new Error(error.message || 'Failed to sign up. Please try again.');
  }
};

export const getUserInfo = async ({ userId }: { userId: string }) => {
  try {
    const { database } = await createAdminClient();
    const user = await database.listDocuments(
      DATABASE_ID!,
      USER_COLLECTION_ID!,
      [Query.equal('userId', [userId])]
    );
    if (!user.documents[0]) {
      throw new Error('User not found');
    }
    return parseStringify(user.documents[0]);
  } catch (error: any) {
    console.error('getUserInfo error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return null;
  }
};

export const getLoggedInUser = async () => {
  try {
    const sessionClient = await createSessionClient();
    if (!sessionClient) {
      throw new Error('No session client found');
    }
    const { account } = sessionClient;
    const session: Models.Session = await account.getSession('current');
    console.log('Retrieved session:', session);
    const user = await getUserInfo({ userId: session.userId });
    if (!user) {
      throw new Error('User not found in database');
    }
    return parseStringify(user);
  } catch (error: any) {
    console.error('getLoggedInUser error:', {
      message: error.message,
      code: error.code,
      type: error.type,
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
      [Query.equal('userId', [user.userId])]
    );
    if (bank.documents[0]) {
      return parseStringify(bank.documents[0]);
    }
    return getMockBankAccount(user.email);
  } catch (error: any) {
    console.error('getUserBankAccount error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return getMockBankAccount(user.email);
  }
};

export const logoutAccount = async () => {
  try {
    const sessionClient = await createSessionClient();
    if (!sessionClient) {
      throw new Error('No session client found');
    }
    const { account } = sessionClient;
    await account.deleteSession('current');
    cookies().delete('appwrite-session');
    return true;
  } catch (error: any) {
    console.error('Logout error:', {
      message: error.message,
      code: error.code,
      type: error.type,
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
        routingNumber: mockBank.routingNumber,
        bankName: mockBank.bankName,
        balance: mockBank.balance,
        currency: mockBank.currency,
        linkedAt: mockBank.linkedAt,
      }
    );
    return parseStringify(newBank);
  } catch (error: any) {
    console.error('Create mock bank error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return null;
  }
};