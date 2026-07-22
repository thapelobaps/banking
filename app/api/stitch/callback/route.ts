import { NextRequest, NextResponse } from 'next/server';

import { ApiError, apiRequest } from '@/lib/api/client';
import type { BankConnection } from '@/types/wallet';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';
const STITCH_CONNECTION_COOKIE = 'kape-stitch-connection-id';

const clearPendingConnection = (response: NextResponse) => {
  response.cookies.set(STITCH_CONNECTION_COOKIE, '', {
    httpOnly: true,
    sameSite: 'lax',
    secure: true,
    path: '/',
    maxAge: 0,
  });
  return response;
};

const redirectWithStatus = (request: NextRequest, status: string) => {
  const destination = new URL('/linked-banks', request.url);
  destination.searchParams.set('stitch', status);
  return clearPendingConnection(NextResponse.redirect(destination));
};

export async function GET(request: NextRequest) {
  const providerError = request.nextUrl.searchParams.get('error');
  const authorizationCode = request.nextUrl.searchParams.get('code');
  const state = request.nextUrl.searchParams.get('state');

  if (providerError || !authorizationCode || !state) {
    return redirectWithStatus(request, providerError ? 'cancelled' : 'invalid_callback');
  }

  const accessToken = request.cookies.get(ACCESS_TOKEN_COOKIE)?.value;
  const connectionId = request.cookies.get(STITCH_CONNECTION_COOKIE)?.value;
  if (!accessToken) {
    const signIn = new URL('/sign-in', request.url);
    signIn.searchParams.set('next', '/linked-banks');
    return clearPendingConnection(NextResponse.redirect(signIn));
  }

  if (!connectionId) {
    return redirectWithStatus(request, 'missing_connection');
  }

  try {
    await apiRequest<BankConnection>(
      '/api/bank-connections/callback',
      {
        method: 'POST',
        body: JSON.stringify({
          connectionId,
          authorizationCode,
          state,
        }),
      },
      accessToken
    );

    return redirectWithStatus(request, 'connected');
  } catch (error) {
    console.error('Unable to complete Stitch bank linking', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return redirectWithStatus(request, 'failed');
  }
}
