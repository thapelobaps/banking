'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import type {
  ActionResult,
  BankConnection,
  BankConnectionSync,
  DebitOrder,
  DemoCardInput,
  LedgerReconciliation,
  LinkedBankAccount,
  LinkedBankTransaction,
  PageResponse,
  PaymentMethod,
  ResolvedKapeUser,
  WalletFundingInput,
  WalletFundingQuote,
  WalletSummary,
  WalletTransaction,
  WalletTransferInput,
} from '@/types/wallet';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

const getAccessToken = () => cookies().get(ACCESS_TOKEN_COOKIE)?.value;

const requireAccessToken = () => {
  const accessToken = getAccessToken();
  if (!accessToken) {
    throw new Error('Your session has expired. Sign in again to continue.');
  }
  return accessToken;
};

const getErrorMessage = (error: unknown) => {
  if (error instanceof ApiError || error instanceof Error) {
    return error.message;
  }
  return 'The request could not be completed.';
};

const emptyPage = <T>(): PageResponse<T> => ({
  items: [],
  page: 1,
  pageSize: 20,
  total: 0,
  totalPages: 1,
});

const refreshWalletUi = () => {
  revalidatePath('/');
  revalidatePath('/wallet');
  revalidatePath('/linked-banks');
  revalidatePath('/my-banks');
  revalidatePath('/transaction-history');
  revalidatePath('/payment-transfer');
};

export async function getWallet(): Promise<WalletSummary | null> {
  const accessToken = getAccessToken();
  if (!accessToken) return null;

  try {
    return await apiRequest<WalletSummary>('/api/wallet', {}, accessToken);
  } catch (error) {
    console.error('Unable to load wallet', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
}

export async function getWalletTransactions(
  page = 1,
  pageSize = 20
): Promise<PageResponse<WalletTransaction>> {
  const accessToken = getAccessToken();
  if (!accessToken) return emptyPage<WalletTransaction>();

  try {
    return await apiRequest<PageResponse<WalletTransaction>>(
      `/api/wallet/transactions?page=${page}&pageSize=${pageSize}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load wallet transactions', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return emptyPage<WalletTransaction>();
  }
}

export async function getLedgerReconciliation(): Promise<LedgerReconciliation | null> {
  const accessToken = getAccessToken();
  if (!accessToken) return null;

  try {
    return await apiRequest<LedgerReconciliation>('/api/ledger/reconciliation', {}, accessToken);
  } catch (error) {
    console.error('Unable to reconcile wallet ledger', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
}

export async function getPaymentMethods(): Promise<PaymentMethod[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    return await apiRequest<PaymentMethod[]>('/api/payment-methods', {}, accessToken);
  } catch (error) {
    console.error('Unable to load payment methods', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function createDemoPaymentMethod(
  input: DemoCardInput
): Promise<ActionResult<PaymentMethod>> {
  try {
    const accessToken = requireAccessToken();
    const setup = await apiRequest<{
      setupSessionId: string;
      clientSecret: string;
      expiresAt: string;
    }>(
      '/api/payment-methods/setup',
      {
        method: 'POST',
        body: JSON.stringify({
          providerId: 'demo',
          returnUrl: 'http://localhost:3000/wallet',
        }),
      },
      accessToken
    );

    const paymentMethod = await apiRequest<PaymentMethod>(
      '/api/payment-methods/confirm',
      {
        method: 'POST',
        body: JSON.stringify({
          setupSessionId: setup.setupSessionId,
          paymentToken: `demo-${input.bankName.toLowerCase().replaceAll(' ', '-')}-${input.last4}-${randomUUID()}`,
          brand: input.brand,
          bankName: input.bankName,
          last4: input.last4,
          expiryMonth: input.expiryMonth,
          expiryYear: input.expiryYear,
        }),
      },
      accessToken
    );

    refreshWalletUi();
    return { ok: true, data: paymentMethod };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function setDefaultPaymentMethod(
  paymentMethodId: string
): Promise<ActionResult<PaymentMethod>> {
  try {
    const accessToken = requireAccessToken();
    const paymentMethod = await apiRequest<PaymentMethod>(
      `/api/payment-methods/${encodeURIComponent(paymentMethodId)}/default`,
      { method: 'PATCH' },
      accessToken
    );
    refreshWalletUi();
    return { ok: true, data: paymentMethod };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function removePaymentMethod(
  paymentMethodId: string
): Promise<ActionResult<null>> {
  try {
    const accessToken = requireAccessToken();
    await apiRequest<void>(
      `/api/payment-methods/${encodeURIComponent(paymentMethodId)}`,
      { method: 'DELETE' },
      accessToken
    );
    refreshWalletUi();
    return { ok: true, data: null };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function previewWalletFunding(
  input: WalletFundingInput
): Promise<ActionResult<WalletFundingQuote>> {
  try {
    const accessToken = requireAccessToken();
    const route = input.operation === 'top_up' ? 'top-ups' : 'withdrawals';
    const quote = await apiRequest<WalletFundingQuote>(
      `/api/wallet/${route}/preview`,
      {
        method: 'POST',
        body: JSON.stringify({
          amount: input.amount,
          paymentMethodId: input.paymentMethodId ?? null,
          linkedBankAccountId: input.linkedBankAccountId ?? null,
          reference: input.reference ?? null,
        }),
      },
      accessToken
    );
    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitWalletFunding(
  input: WalletFundingInput
): Promise<ActionResult<WalletTransaction>> {
  try {
    const accessToken = requireAccessToken();
    const route = input.operation === 'top_up' ? 'top-ups' : 'withdrawals';
    const transaction = await apiRequest<WalletTransaction>(
      `/api/wallet/${route}`,
      {
        method: 'POST',
        body: JSON.stringify({
          amount: input.amount,
          paymentMethodId: input.paymentMethodId ?? null,
          linkedBankAccountId: input.linkedBankAccountId ?? null,
          reference: input.reference ?? null,
          idempotencyKey: input.idempotencyKey ?? `wallet-ui-${input.operation}-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshWalletUi();
    return { ok: true, data: transaction };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function resolveKapeUser(identifier: string): Promise<ActionResult<ResolvedKapeUser>> {
  try {
    const normalizedIdentifier = identifier.trim();
    if (!normalizedIdentifier) {
      return { ok: false, error: 'Enter the recipient email address, mobile number, or Kape user ID.' };
    }

    const accessToken = requireAccessToken();
    const user = await apiRequest<ResolvedKapeUser>(
      '/api/kape-users/resolve',
      {
        method: 'POST',
        body: JSON.stringify({ identifier: normalizedIdentifier }),
      },
      accessToken
    );
    return { ok: true, data: user };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function previewWalletTransfer(
  input: WalletTransferInput
): Promise<ActionResult<WalletFundingQuote>> {
  try {
    const accessToken = requireAccessToken();
    const quote = await apiRequest<WalletFundingQuote>(
      '/api/wallet/transfers/preview',
      {
        method: 'POST',
        body: JSON.stringify({
          recipientUserId: input.recipientUserId,
          amount: input.amount,
          reference: input.reference ?? null,
        }),
      },
      accessToken
    );
    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitWalletTransfer(
  input: WalletTransferInput
): Promise<ActionResult<WalletTransaction>> {
  try {
    const accessToken = requireAccessToken();
    const transaction = await apiRequest<WalletTransaction>(
      '/api/wallet/transfers',
      {
        method: 'POST',
        body: JSON.stringify({
          recipientUserId: input.recipientUserId,
          amount: input.amount,
          reference: input.reference ?? null,
          idempotencyKey: input.idempotencyKey ?? `wallet-ui-transfer-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshWalletUi();
    return { ok: true, data: transaction };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getBankConnections(): Promise<BankConnection[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    return await apiRequest<BankConnection[]>('/api/bank-connections', {}, accessToken);
  } catch (error) {
    console.error('Unable to load bank connections', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function getLinkedAccounts(): Promise<LinkedBankAccount[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    return await apiRequest<LinkedBankAccount[]>('/api/linked-accounts', {}, accessToken);
  } catch (error) {
    console.error('Unable to load linked bank accounts', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function connectDemoBank(
  institutionId: string
): Promise<ActionResult<BankConnection>> {
  try {
    const accessToken = requireAccessToken();
    const linkSession = await apiRequest<{
      connectionId: string;
      sessionId: string;
      linkUrl: string;
      expiresAt: string;
    }>(
      '/api/bank-connections/link-session',
      {
        method: 'POST',
        body: JSON.stringify({
          providerId: 'demo',
          institutionId,
          returnUrl: 'http://localhost:3000/linked-banks',
        }),
      },
      accessToken
    );

    const connection = await apiRequest<BankConnection>(
      '/api/bank-connections/callback',
      {
        method: 'POST',
        body: JSON.stringify({
          connectionId: linkSession.connectionId,
          authorizationCode: `demo-${institutionId}-${randomUUID()}`,
          state: institutionId,
        }),
      },
      accessToken
    );

    refreshWalletUi();
    return { ok: true, data: connection };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function syncBankConnection(
  connectionId: string
): Promise<ActionResult<BankConnectionSync>> {
  try {
    const accessToken = requireAccessToken();
    const sync = await apiRequest<BankConnectionSync>(
      `/api/bank-connections/${encodeURIComponent(connectionId)}/sync`,
      { method: 'POST' },
      accessToken
    );
    refreshWalletUi();
    return { ok: true, data: sync };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function disconnectBankConnection(
  connectionId: string
): Promise<ActionResult<null>> {
  try {
    const accessToken = requireAccessToken();
    await apiRequest<void>(
      `/api/bank-connections/${encodeURIComponent(connectionId)}`,
      { method: 'DELETE' },
      accessToken
    );
    refreshWalletUi();
    return { ok: true, data: null };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getLinkedAccountTransactions(
  linkedAccountId: string,
  page = 1,
  pageSize = 10
): Promise<PageResponse<LinkedBankTransaction>> {
  const accessToken = getAccessToken();
  if (!accessToken) return emptyPage<LinkedBankTransaction>();

  try {
    return await apiRequest<PageResponse<LinkedBankTransaction>>(
      `/api/linked-accounts/${encodeURIComponent(linkedAccountId)}/transactions?page=${page}&pageSize=${pageSize}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load linked bank transactions', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return emptyPage<LinkedBankTransaction>();
  }
}

export async function getLinkedAccountDebitOrders(
  linkedAccountId: string
): Promise<DebitOrder[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    return await apiRequest<DebitOrder[]>(
      `/api/linked-accounts/${encodeURIComponent(linkedAccountId)}/debit-orders`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load linked bank debit orders', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}
