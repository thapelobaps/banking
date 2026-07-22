'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import type { ResolvedKapeUser, WalletTransferInput } from '@/types/money';
import type {
  ActionResult,
  WalletFundingQuote,
  WalletTransaction,
} from '@/types/wallet';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

const requireAccessToken = () => {
  const accessToken = cookies().get(ACCESS_TOKEN_COOKIE)?.value;
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

const refreshMoneyUi = () => {
  revalidatePath('/');
  revalidatePath('/wallet');
  revalidatePath('/my-banks');
  revalidatePath('/linked-banks');
  revalidatePath('/transaction-history');
  revalidatePath('/payment-transfer');
};

export async function resolveKapeRecipient(
  identifier: string
): Promise<ActionResult<ResolvedKapeUser>> {
  try {
    const value = identifier.trim();
    if (!value) {
      return { ok: false, error: 'Enter the recipient email address or mobile number.' };
    }

    const accessToken = requireAccessToken();
    const recipient = await apiRequest<ResolvedKapeUser>(
      '/api/kape-users/resolve',
      {
        method: 'POST',
        body: JSON.stringify({ identifier: value }),
      },
      accessToken
    );

    return { ok: true, data: recipient };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function previewKapeWalletTransfer(
  input: WalletTransferInput
): Promise<ActionResult<WalletFundingQuote>> {
  try {
    if (!Number.isFinite(input.amount) || input.amount <= 0) {
      return { ok: false, error: 'Enter an amount greater than zero.' };
    }

    const accessToken = requireAccessToken();
    const quote = await apiRequest<WalletFundingQuote>(
      '/api/wallet/transfers/preview',
      {
        method: 'POST',
        body: JSON.stringify({
          recipientUserId: input.recipientUserId,
          amount: input.amount,
          reference: input.reference?.trim() || null,
        }),
      },
      accessToken
    );

    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitKapeWalletTransfer(
  input: WalletTransferInput
): Promise<ActionResult<WalletTransaction>> {
  try {
    if (!Number.isFinite(input.amount) || input.amount <= 0) {
      return { ok: false, error: 'Enter an amount greater than zero.' };
    }

    const accessToken = requireAccessToken();
    const transaction = await apiRequest<WalletTransaction>(
      '/api/wallet/transfers',
      {
        method: 'POST',
        body: JSON.stringify({
          recipientUserId: input.recipientUserId,
          amount: input.amount,
          reference: input.reference?.trim() || null,
          idempotencyKey:
            input.idempotencyKey?.trim() || `wallet-transfer-ui-${randomUUID()}`,
        }),
      },
      accessToken
    );

    refreshMoneyUi();
    return { ok: true, data: transaction };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}
