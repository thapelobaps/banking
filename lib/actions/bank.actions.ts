'use server';

import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import {
  Account,
  CreateTransactionProps,
  getAccountsProps,
  getTransactionsByBankIdProps,
  Transaction,
} from '@/types';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

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

type ApiRecipientPreview = {
  id: string;
  bankName: string;
  accountMask: string;
  accountType: string;
  currency: 'ZAR';
  isDemo: boolean;
};

type ApiTransaction = {
  id: string;
  bankAccountId: string;
  relatedBankAccountId?: string | null;
  name: string;
  statementDescription: string;
  beneficiary?: string | null;
  amount: number;
  direction: 'credit' | 'debit';
  category: string;
  channel: string;
  status: string;
  transactionDate: string;
  isDemo: boolean;
};

export type RecipientPreview = {
  id: string;
  bankName: string;
  accountMask: string;
  accountType: string;
  currency: 'ZAR';
  isDemo: boolean;
};

const getAccessToken = () => cookies().get(ACCESS_TOKEN_COOKIE)?.value;

const toAccount = (account: ApiAccount): Account => ({
  id: account.id,
  availableBalance: account.availableBalance,
  currentBalance: account.currentBalance,
  officialName: account.bankName,
  mask: account.accountNumber.slice(-4),
  institutionId: account.bankId,
  name: account.bankName,
  type: 'depository',
  subtype: account.accountType,
  branchCode: account.branchCode,
  accountNumber: account.accountNumber,
  currency: account.currency,
  demoReference: account.id,
  isDemo: account.isDemo,
});

const toTransaction = (transaction: ApiTransaction): Transaction => ({
  id: transaction.id,
  name: transaction.name,
  amount: transaction.amount,
  category: transaction.category,
  date: transaction.transactionDate,
  paymentChannel: transaction.channel,
  channel: transaction.channel,
  type: transaction.direction,
  accountId: transaction.bankAccountId,
  relatedAccountId: transaction.relatedBankAccountId ?? undefined,
  status: transaction.status,
  statementDescription: transaction.statementDescription,
  beneficiary: transaction.beneficiary ?? undefined,
  isDemo: transaction.isDemo,
});

export const getAccounts = async ({ userId: _userId }: getAccountsProps) => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    return null;
  }

  try {
    const response = await apiRequest<ApiAccount[]>('/api/accounts', {}, accessToken);
    const data = response.map(toAccount);

    return {
      data,
      totalBanks: data.length,
      totalCurrentBalance: data.reduce((total, account) => total + account.currentBalance, 0),
    };
  } catch (error) {
    console.error('Unable to load accounts', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
};

export const getDemoRecipientPreview = async (accountId: string): Promise<RecipientPreview> => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    throw new Error('Sign in before reviewing a recipient.');
  }

  return apiRequest<ApiRecipientPreview>(
    `/api/accounts/demo-recipient/${encodeURIComponent(accountId)}`,
    {},
    accessToken
  );
};

export const getTransactionsByBankId = async ({ bankId }: getTransactionsByBankIdProps) => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    return { total: 0, documents: [] as Transaction[] };
  }

  try {
    const response = await apiRequest<ApiTransaction[]>(
      `/api/accounts/${encodeURIComponent(bankId)}/transactions`,
      {},
      accessToken
    );
    const documents = response.map(toTransaction);
    return { total: documents.length, documents };
  } catch (error) {
    console.error('Unable to load transactions', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return { total: 0, documents: [] as Transaction[] };
  }
};

export const createMockTransfer = async ({
  senderBankId,
  receiverBankId,
  amount,
  name,
}: {
  senderBankId: string;
  receiverBankId: string;
  amount: number;
  name: string;
}) => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    throw new Error('Sign in before simulating a transfer.');
  }

  const response = await apiRequest<ApiTransaction>(
    '/api/transfers/demo',
    {
      method: 'POST',
      body: JSON.stringify({
        senderBankAccountId: senderBankId,
        receiverBankAccountId: receiverBankId,
        amount,
        reference: name,
      }),
    },
    accessToken
  );

  return toTransaction(response);
};

export const createTransaction = async (transaction: CreateTransactionProps) =>
  createMockTransfer({
    senderBankId: transaction.senderBankId,
    receiverBankId: transaction.receiverBankId,
    amount: Number(transaction.amount),
    name: transaction.name,
  });
