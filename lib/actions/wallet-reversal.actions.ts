'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import type { ActionResult, WalletTransaction } from '@/types/wallet';

type WalletPurchaseReversal = {
  originalTransactionId: string;
  reversal: WalletTransaction;
  status: string;
  reversedAt: string;
};

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

const getErrorMessage = (error: unknown) => {
  if (error instanceof ApiError || error instanceof Error) return error.message;
  return 'The wallet purchase could not be reversed.';
};

export async function reverseWalletPurchase(
  walletTransactionId: string,
  reason: string
): Promise<ActionResult<WalletPurchaseReversal>> {
  try {
    const accessToken = cookies().get(ACCESS_TOKEN_COOKIE)?.value;
    if (!accessToken) throw new Error('Your session has expired. Sign in again to continue.');

    const result = await apiRequest<WalletPurchaseReversal>(
      `/api/wallet/transactions/${encodeURIComponent(walletTransactionId)}/reversals`,
      {
        method: 'POST',
        body: JSON.stringify({
          reason: reason.trim() || 'Kape Pay wallet purchase reversal',
          idempotencyKey: `wallet-reversal-${randomUUID()}`,
        }),
      },
      accessToken
    );

    revalidatePath('/wallet');
    revalidatePath('/payment-activity');
    revalidatePath('/transaction-history');
    revalidatePath('/vouchers');
    revalidatePath('/prepaid');
    return { ok: true, data: result };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}
