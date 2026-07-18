/* eslint-disable no-prototype-builtins */
import { type ClassValue, clsx } from 'clsx';
import qs from 'query-string';
import { twMerge } from 'tailwind-merge';
import { z } from 'zod';
import { CategoryCount, SouthAfricanProvince, Transaction } from '@/types';

export const SOUTH_AFRICAN_PROVINCES = [
  'Eastern Cape',
  'Free State',
  'Gauteng',
  'KwaZulu-Natal',
  'Limpopo',
  'Mpumalanga',
  'North West',
  'Northern Cape',
  'Western Cape',
] as const satisfies readonly SouthAfricanProvince[];

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export const formatDateTime = (dateString: Date | string) => {
  const dateTimeOptions: Intl.DateTimeFormatOptions = {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  };

  const dateDayOptions: Intl.DateTimeFormatOptions = {
    weekday: 'short',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  };

  const dateOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
  };

  const timeOptions: Intl.DateTimeFormatOptions = {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  };

  const date = new Date(dateString);

  return {
    dateTime: date.toLocaleString('en-ZA', dateTimeOptions),
    dateDay: date.toLocaleString('en-ZA', dateDayOptions),
    dateOnly: date.toLocaleString('en-ZA', dateOptions),
    timeOnly: date.toLocaleString('en-ZA', timeOptions),
  };
};

export function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-ZA', {
    style: 'currency',
    currency: 'ZAR',
    minimumFractionDigits: 2,
  }).format(amount);
}

export const parseStringify = (value: unknown) => JSON.parse(JSON.stringify(value));

export const removeSpecialCharacters = (value: string) => {
  return value.replace(/[^\w\s]/gi, '');
};

export function formUrlQuery({ params, key, value }: { params: string; key: string; value: string }) {
  const currentUrl = qs.parse(params);
  currentUrl[key] = value;
  return qs.stringifyUrl(
    {
      url: window.location.pathname,
      query: currentUrl,
    },
    { skipNull: true }
  );
}

export function getAccountTypeColors(type: string) {
  switch (type) {
    case 'depository':
      return {
        bg: 'bg-blue-25',
        lightBg: 'bg-blue-100',
        title: 'text-blue-900',
        subText: 'text-blue-700',
      };
    case 'credit':
      return {
        bg: 'bg-success-25',
        lightBg: 'bg-success-100',
        title: 'text-success-900',
        subText: 'text-success-700',
      };
    default:
      return {
        bg: 'bg-green-25',
        lightBg: 'bg-green-100',
        title: 'text-green-900',
        subText: 'text-green-700',
      };
  }
}

export function countTransactionCategories(transactions: Transaction[]): CategoryCount[] {
  const categoryCounts: Record<string, number> = {};
  let totalCount = 0;

  transactions.forEach((transaction) => {
    categoryCounts[transaction.category] = (categoryCounts[transaction.category] ?? 0) + 1;
    totalCount += 1;
  });

  return Object.keys(categoryCounts)
    .map((category) => ({
      name: category,
      count: categoryCounts[category],
      totalCount,
    }))
    .sort((a, b) => b.count - a.count);
}

export function encryptId(id: string) {
  return btoa(id);
}

export function decryptId(id: string) {
  return atob(id);
}

export const getTransactionStatus = (date: Date) => {
  const today = new Date();
  const twoDaysAgo = new Date(today);
  twoDaysAgo.setDate(today.getDate() - 2);
  return date > twoDaysAgo ? 'Processing' : 'Success';
};

const stripSouthAfricanPhoneFormatting = (value: string) =>
  value.trim().replace(/[\s()-]/g, '');

export const normaliseSouthAfricanMobile = (value: string) => {
  const compact = stripSouthAfricanPhoneFormatting(value);

  if (/^0[6-8]\d{8}$/.test(compact)) {
    return `+27${compact.slice(1)}`;
  }

  if (/^\+27[6-8]\d{8}$/.test(compact)) {
    return compact;
  }

  return compact;
};

export const isValidSouthAfricanMobile = (value: string) =>
  /^\+27[6-8]\d{8}$/.test(normaliseSouthAfricanMobile(value));

export const parseSouthAfricanDateToIso = (value: string) => {
  const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value.trim());
  if (!match) return null;

  const [, dayText, monthText, yearText] = match;
  const day = Number(dayText);
  const month = Number(monthText);
  const year = Number(yearText);
  const date = new Date(Date.UTC(year, month - 1, day));

  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day ||
    date > new Date()
  ) {
    return null;
  }

  return `${yearText}-${monthText}-${dayText}`;
};

export const signInSchema = z.object({
  email: z.string().trim().email('Enter a valid email address').transform((value) => value.toLowerCase()),
  password: z.string().min(8, 'Password must be at least 8 characters'),
});

export const signUpSchema = z
  .object({
    firstName: z.string().trim().min(2, 'First name must be at least 2 characters').max(50),
    lastName: z.string().trim().min(2, 'Surname must be at least 2 characters').max(50),
    email: z.string().trim().email('Enter a valid email address').transform((value) => value.toLowerCase()),
    mobileNumber: z
      .string()
      .trim()
      .refine(isValidSouthAfricanMobile, 'Enter a valid South African mobile number')
      .transform(normaliseSouthAfricanMobile),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Za-z]/, 'Password must contain a letter')
      .regex(/\d/, 'Password must contain a number'),
    confirmPassword: z.string().min(1, 'Confirm your password'),
    address1: z.string().trim().min(3, 'Address line 1 is required').max(100),
    suburb: z.string().trim().min(2, 'Suburb is required').max(60),
    city: z.string().trim().min(2, 'City or town is required').max(60),
    province: z.enum(SOUTH_AFRICAN_PROVINCES, {
      required_error: 'Select a province',
    }),
    postalCode: z.string().trim().regex(/^\d{4}$/, 'Postal code must be exactly 4 digits'),
    dateOfBirth: z
      .string()
      .trim()
      .refine((value) => parseSouthAfricanDateToIso(value) !== null, 'Use a valid DD/MM/YYYY date')
      .transform((value) => parseSouthAfricanDateToIso(value)!),
    country: z.literal('South Africa').default('South Africa'),
    termsAccepted: z.boolean().refine((value) => value, 'You must accept the terms'),
    privacyAccepted: z.boolean().refine((value) => value, 'You must acknowledge the privacy notice'),
  })
  .superRefine(({ password, confirmPassword }, context) => {
    if (password !== confirmPassword) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['confirmPassword'],
        message: 'Passwords do not match',
      });
    }
  });

export const authFormSchema = (type: 'sign-in' | 'sign-up') =>
  type === 'sign-up' ? signUpSchema : signInSchema;
