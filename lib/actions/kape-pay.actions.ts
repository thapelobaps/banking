'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import type {
  KapePayPrepaidInput,
  KapePayQuote,
  KapePayVoucherInput,
  PaymentAttempt,
  PaymentReconciliation,
  PaymentRefund,
  PrepaidCheckout,
  VoucherCheckout,
} from '@/types/kape-pay';
import type { ActionResult, PageResponse } from '@/types/wallet';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

const requireAccessToken = () => {
  const accessToken = cookies().get(ACCESS_TOKEN_COOKIE)?.value;
  if (!accessToken) throw new Error('Your session has expired. Sign in again to continue.');
  return accessToken;
};

const getAccessToken = () => cookies().get(ACCESS_TOKEN_COOKIE)?.value;

const getErrorMessage = (error: unknown) => {
  if (error instanceof ApiError || error instanceof Error) return error.message;
  return 'The payment request could not be completed.';
};

const refreshKapePayUi = () => {
  revalidatePath('/');
  revalidatePath('/wallet');
  revalidatePath('/marketplace');
  revalidatePath('/vouchers');
  revalidatePath('/prepaid');
  revalidatePath('/transaction-history');
};

export async function previewKapePayVoucher(
  input: KapePayVoucherInput
): Promise<ActionResult<KapePayQuote>> {
  try {
    const accessToken = requireAccessToken();
    const quote = await apiRequest<KapePayQuote>(
      '/api/kape-pay/vouchers/quote',
      {
        method: 'POST',
        body: JSON.stringify({
          voucherProductId: input.voucherProductId,
          voucherDenominationId: input.voucherDenominationId,
          paymentSource: input.paymentSource,
          linkedBankAccountId: input.linkedBankAccountId ?? null,
        }),
      },
      accessToken
    );
    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitKapePayVoucher(
  input: KapePayVoucherInput
): Promise<ActionResult<VoucherCheckout>> {
  try {
    const accessToken = requireAccessToken();
    const checkout = await apiRequest<VoucherCheckout>(
      '/api/kape-pay/vouchers',
      {
        method: 'POST',
        body: JSON.stringify({
          voucherProductId: input.voucherProductId,
          voucherDenominationId: input.voucherDenominationId,
          paymentSource: input.paymentSource,
          linkedBankAccountId: input.linkedBankAccountId ?? null,
          scenario: input.scenario ?? 'success',
          returnUrl: input.returnUrl ?? null,
          idempotencyKey: input.idempotencyKey ?? `kape-pay-voucher-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshKapePayUi();
    return { ok: true, data: checkout };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function previewKapePayPrepaid(
  input: KapePayPrepaidInput
): Promise<ActionResult<KapePayQuote>> {
  try {
    const accessToken = requireAccessToken();
    const quote = await apiRequest<KapePayQuote>(
      '/api/kape-pay/prepaid/quote',
      {
        method: 'POST',
        body: JSON.stringify({
          productId: input.productId,
          recipient: input.recipient,
          amount: input.amount,
          paymentSource: input.paymentSource,
          linkedBankAccountId: input.linkedBankAccountId ?? null,
        }),
      },
      accessToken
    );
    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitKapePayPrepaid(
  input: KapePayPrepaidInput
): Promise<ActionResult<PrepaidCheckout>> {
  try {
    const accessToken = requireAccessToken();
    const checkout = await apiRequest<PrepaidCheckout>(
      '/api/kape-pay/prepaid',
      {
        method: 'POST',
        body: JSON.stringify({
          productId: input.productId,
          recipient: input.recipient,
          amount: input.amount,
          paymentSource: input.paymentSource,
          linkedBankAccountId: input.linkedBankAccountId ?? null,
          scenario: input.scenario ?? 'success',
          returnUrl: input.returnUrl ?? null,
          idempotencyKey: input.idempotencyKey ?? `kape-pay-prepaid-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshKapePayUi();
    return { ok: true, data: checkout };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getKapePayPayments(
  page = 1,
  pageSize = 50
): Promise<PageResponse<PaymentAttempt>> {
  const accessToken = getAccessToken();
  if (!accessToken) return { items: [], page, pageSize, total: 0, totalPages: 1 };

  try {
    return await apiRequest<PageResponse<PaymentAttempt>>(
      `/api/kape-pay/payments?page=${page}&pageSize=${pageSize}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load Kape Pay payments', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return { items: [], page, pageSize, total: 0, totalPages: 1 };
  }
}

export async function refreshKapePayPayment(
  paymentAttemptId: string
): Promise<ActionResult<PaymentAttempt>> {
  try {
    const accessToken = requireAccessToken();
    const payment = await apiRequest<PaymentAttempt>(
      `/api/kape-pay/payments/${encodeURIComponent(paymentAttemptId)}/refresh`,
      { method: 'POST' },
      accessToken
    );
    refreshKapePayUi();
    return { ok: true, data: payment };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function refundKapePayPayment(
  paymentAttemptId: string,
  amount: number,
  reason: string
): Promise<ActionResult<PaymentRefund>> {
  try {
    const accessToken = requireAccessToken();
    const refund = await apiRequest<PaymentRefund>(
      `/api/kape-pay/payments/${encodeURIComponent(paymentAttemptId)}/refunds`,
      {
        method: 'POST',
        body: JSON.stringify({
          amount,
          reason,
          idempotencyKey: `kape-pay-refund-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshKapePayUi();
    return { ok: true, data: refund };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function reconcileKapePay(): Promise<ActionResult<PaymentReconciliation>> {
  try {
    const accessToken = requireAccessToken();
    const reconciliation = await apiRequest<PaymentReconciliation>(
      '/api/kape-pay/reconciliation',
      { method: 'POST' },
      accessToken
    );
    return { ok: true, data: reconciliation };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}
