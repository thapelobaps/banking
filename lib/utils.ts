/* eslint-disable no-prototype-builtins */
import { type ClassValue, clsx } from 'clsx';
import qs from 'query-string';
import { twMerge } from 'tailwind-merge';
import { z } from 'zod';
import { CategoryCount, Transaction, User } from '@/types';

const usStates = [
  'AL', 'AK', 'AZ', 'AR', 'CA', 'CO', 'CT', 'DE', 'FL', 'GA',
  'HI', 'ID', 'IL', 'IN', 'IA', 'KS', 'KY', 'LA', 'ME', 'MD',
  'MA', 'MI', 'MN', 'MS', 'MO', 'MT', 'NE', 'NV', 'NH', 'NJ',
  'NM', 'NY', 'NC', 'ND', 'OH', 'OK', 'OR', 'PA', 'RI', 'SC',
  'SD', 'TN', 'TX', 'UT', 'VT', 'VA', 'WA', 'WV', 'WI', 'WY'
];

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export const formatDateTime = (dateString: Date) => {
  const dateTimeOptions: Intl.DateTimeFormatOptions = {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: 'numeric',
    hour12: true,
  };

  const dateDayOptions: Intl.DateTimeFormatOptions = {
    weekday: 'short',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  };

  const dateOptions: Intl.DateTimeFormatOptions = {
    month: 'short',
    year: 'numeric',
    day: 'numeric',
  };

  const timeOptions: Intl.DateTimeFormatOptions = {
    hour: 'numeric',
    minute: 'numeric',
    hour12: true,
  };

  const formattedDateTime: string = new Date(dateString).toLocaleString('en-US', dateTimeOptions);
  const formattedDateDay: string = new Date(dateString).toLocaleString('en-US', dateDayOptions);
  const formattedDate: string = new Date(dateString).toLocaleString('en-US', dateOptions);
  const formattedTime: string = new Date(dateString).toLocaleString('en-US', timeOptions);

  return {
    dateTime: formattedDateTime,
    dateDay: formattedDateDay,
    dateOnly: formattedDate,
    timeOnly: formattedTime,
  };
};

export function formatAmount(amount: number): string {
  const formatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  });

  return formatter.format(amount);
}

export const parseStringify = (value: any) => JSON.parse(JSON.stringify(value));

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
  const categoryCounts: { [category: string]: number } = {};
  let totalCount = 0;

  transactions &&
    transactions.forEach((transaction) => {
      const category = transaction.category;
      if (categoryCounts.hasOwnProperty(category)) {
        categoryCounts[category]++;
      } else {
        categoryCounts[category] = 1;
      }
      totalCount++;
    });

  const aggregatedCategories: CategoryCount[] = Object.keys(categoryCounts).map((category) => ({
    name: category,
    count: categoryCounts[category],
    totalCount,
  }));

  aggregatedCategories.sort((a, b) => b.count - a.count);
  return aggregatedCategories;
}

export const extractCustomerIdFromUrl = (url: string) => {
  const parts = url.split('/');
  return parts[parts.length - 1];
};

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

export const authFormSchema = (type: 'sign-in' | 'sign-up') =>
  z.object({
    email: z.string().email('Invalid email address').transform((val) => val.toLowerCase()),
    password: z.string().min(8, 'Password must be at least 8 characters'),
    ...(type === 'sign-up' && {
      firstName: z.string().min(3, 'First name must be at least 3 characters'),
      lastName: z.string().min(3, 'Last name must be at least 3 characters'),
      address1: z.string().min(1, 'Address is required').max(50, 'Address must be at most 50 characters'),
      city: z.string().min(1, 'City is required').max(50, 'City must be at most 50 characters'),
      state: z
        .string()
        .length(2, 'State must be a 2-letter code (e.g., NY)')
        .refine((val) => usStates.includes(val.toUpperCase()), {
          message: 'State must be a valid US state code (e.g., NY, CA)',
        }),
      postalCode: z.string().regex(/^\d{5}$/, 'Postal code must be exactly 5 digits'),
      dateOfBirth: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, 'Date of birth must be in YYYY-MM-DD format'),
      ssn: z.string().regex(/^\d{4}$/, 'SSN must be the last 4 digits'),
    }),
  });

export const getMockBankAccount = (email: string) => {
  const mockAccount = {
    accountId: `mock_${email}_${Date.now()}`,
    accountNumber: '1234567890',
    routingNumber: '111000614',
    bankName: 'Demo Bank',
    balance: 1000.0,
    currency: 'USD',
    linkedAt: new Date().toISOString(),
  };
  if (typeof window !== 'undefined') {
    localStorage.setItem(`mock_bank_${email}`, JSON.stringify(mockAccount));
  }
  return mockAccount;
};

export const retrieveMockBankAccount = (email: string) => {
  if (typeof window !== 'undefined') {
    const stored = localStorage.getItem(`mock_bank_${email}`);
    return stored ? JSON.parse(stored) : null;
  }
  return null;
};

export const getMockTransactions = (user: User) => {
  return [
    {
      id: `tx_${Date.now()}_1`,
      $id: `tx_${Date.now()}_1`,
      name: 'Demo Purchase',
      paymentChannel: 'card',
      type: 'debit',
      accountId: `mock_${user.email}_${Date.now()}`,
      amount: 50.0,
      createdAt: new Date().toISOString(),
      category: 'Shopping',
      date: new Date().toISOString().split('T')[0],
      image: '/icons/shopping-bag.svg',
      typeIcon: '/icons/debit.svg',
    },
    {
      id: `tx_${Date.now()}_2`,
      $id: `tx_${Date.now()}_2`,
      name: 'Demo Transfer',
      paymentChannel: 'bank',
      type: 'credit',
      accountId: `mock_${user.email}_${Date.now()}`,
      amount: 200.0,
      createdAt: new Date().toISOString(),
      category: 'Transfer',
      date: new Date().toISOString().split('T')[0],
      image: '/icons/transfer.svg',
      typeIcon: '/icons/credit.svg',
    },
  ];
};