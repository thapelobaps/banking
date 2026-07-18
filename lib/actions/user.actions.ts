'use server';

import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import { signInSchema, signUpSchema } from '@/lib/utils';
import { SignUpParams, User } from '@/types';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

type ApiUser = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  mobileNumber: string;
  country: string;
};

type AuthResponse = {
  accessToken: string;
  expiresAt: string;
  user: ApiUser;
};

type ApiAccount = {
  id: string;
  bankId: string;
  bankName: string;
  accountNumber: string;
  branchCode: string;
  accountType: string;
  currentBalance: number;
  availableBalance: number;
  currency: 'ZAR';
  isDemo: boolean;
};

const toUser = (user: ApiUser): User => ({
  $id: user.id,
  userId: user.id,
  email: user.email,
  firstName: user.firstName,
  lastName: user.lastName,
  name: `${user.firstName} ${user.lastName}`,
  mobileNumber: user.mobileNumber,
  country: user.country === 'South Africa' ? 'South Africa' : undefined,
});

const storeAccessToken = (response: AuthResponse) => {
  const expiresAt = new Date(response.expiresAt);

  cookies().set(ACCESS_TOKEN_COOKIE, response.accessToken, {
    path: '/',
    httpOnly: true,
    sameSite: 'strict',
    secure: process.env.NODE_ENV === 'production',
    expires: expiresAt,
  });
};

const getAccessToken = () => cookies().get(ACCESS_TOKEN_COOKIE)?.value;

export const signIn = async ({ email, password }: { email: string; password: string }) => {
  const credentials = signInSchema.parse({ email, password });

  try {
    const response = await apiRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });

    storeAccessToken(response);
    return toUser(response.user);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      throw new Error('Invalid email or password.');
    }

    throw new Error(error instanceof Error ? error.message : 'Sign-in failed. Please try again.');
  }
};

export const signUp = async (userData: SignUpParams) => {
  const registration = signUpSchema.parse(userData);

  try {
    const response = await apiRequest<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({
        firstName: registration.firstName,
        lastName: registration.lastName,
        email: registration.email,
        mobileNumber: registration.mobileNumber,
        password: registration.password,
        confirmPassword: registration.confirmPassword,
        addressLine1: registration.address1,
        suburb: registration.suburb,
        city: registration.city,
        province: registration.province,
        postalCode: registration.postalCode,
        dateOfBirth: registration.dateOfBirth,
        country: registration.country,
        termsAccepted: registration.termsAccepted,
        privacyAccepted: registration.privacyAccepted,
      }),
    });

    storeAccessToken(response);
    return toUser(response.user);
  } catch (error) {
    throw new Error(error instanceof Error ? error.message : 'Account creation failed. Please try again.');
  }
};

export const getLoggedInUser = async () => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    return null;
  }

  try {
    const user = await apiRequest<ApiUser>('/api/auth/me', {}, accessToken);
    return toUser(user);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      return null;
    }

    console.error('Unable to load the current user', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
};

export const getUserInfo = async ({ userId }: { userId: string }) => {
  const currentUser = await getLoggedInUser();
  return currentUser?.userId === userId ? currentUser : null;
};

export const getUserBankAccount = async (_user: User) => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    return null;
  }

  try {
    const accounts = await apiRequest<ApiAccount[]>('/api/accounts', {}, accessToken);
    return accounts[0] ?? null;
  } catch (error) {
    console.error('Unable to load the bank account', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
};

export const logoutAccount = async () => {
  cookies().delete(ACCESS_TOKEN_COOKIE);
  return true;
};

export const createMockBank = async ({ userId: _userId, email: _email }: { userId: string; email: string }) => {
  const currentUser = await getLoggedInUser();
  return currentUser ? getUserBankAccount(currentUser) : null;
};
