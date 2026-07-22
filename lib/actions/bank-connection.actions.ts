'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import type { ActionResult, BankConnection } from '@/types/wallet';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';
const STITCH_CONNECTION_COOKIE = 'kape-stitch-connection-id';

export type BankAggregationProviderMode = 'demo' | 'stitch';

type BankConnectionResult =
  | { mode: 'connected'; connection: BankConnection }
  | { mode: 'redirect'; linkUrl: string };

const configuredProvider = (): BankAggregationProviderMode =>
  process.env.BANK_AGGREGATION_PROVIDER?.trim().toLowerCase() === 'stitch'
    ? 'stitch'
    : 'demo';

const requireAccessToken = () => {
  const accessToken = cookies().get(ACCESS_TOKEN_COOKIE)?.value;
  if (!accessToken) {
    throw new Error('Your session has expired. Sign in again to continue.');
  }
  return accessToken;
};

const errorMessage = (error: unknown) => {
  if (error instanceof ApiError || error instanceof Error) return error.message;
  return 'The bank connection could not be started.';
};

const refreshBankUi = () => {
  revalidatePath('/');
  revalidatePath('/linked-banks');
  revalidatePath('/my-banks');
  revalidatePath('/transaction-history');
};

export async function getConfiguredBankProvider(): Promise<BankAggregationProviderMode> {
  return configuredProvider();
}

export async function connectBank(
  institutionId: string
): Promise<ActionResult<BankConnectionResult>> {
  try {
    const provider = configuredProvider();
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
          providerId: provider,
          institutionId,
          returnUrl: provider === 'stitch' ? process.env.STITCH_REDIRECT_URI ?? null : 'http://localhost:3000/linked-banks',
        }),
      },
      accessToken
    );

    if (provider === 'stitch') {
      const expiresAt = new Date(linkSession.expiresAt);
      const maxAge = Math.max(60, Math.floor((expiresAt.getTime() - Date.now()) / 1000));
      cookies().set(STITCH_CONNECTION_COOKIE, linkSession.connectionId, {
        httpOnly: true,
        sameSite: 'lax',
        secure: true,
        path: '/',
        maxAge,
      });

      return { ok: true, data: { mode: 'redirect', linkUrl: linkSession.linkUrl } };
    }

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

    refreshBankUi();
    return { ok: true, data: { mode: 'connected', connection } };
  } catch (error) {
    return { ok: false, error: errorMessage(error) };
  }
}
