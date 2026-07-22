'use server';

import { randomUUID } from 'node:crypto';
import { revalidatePath } from 'next/cache';
import { cookies } from 'next/headers';

import { ApiError, apiRequest } from '@/lib/api/client';
import type {
  MarketplaceQuote,
  PaymentRequest,
  PaymentRequestInput,
  PrepaidOperator,
  PrepaidOrder,
  PrepaidProduct,
  PrepaidPurchaseInput,
  PrepaidRecipientValidation,
  VoucherCategory,
  VoucherDenomination,
  VoucherOrder,
  VoucherProduct,
  VoucherPurchaseInput,
} from '@/types/marketplace';
import type { ActionResult, PageResponse } from '@/types/wallet';

const ACCESS_TOKEN_COOKIE = 'kape-access-token';

const getAccessToken = () => cookies().get(ACCESS_TOKEN_COOKIE)?.value;

const requireAccessToken = () => {
  const accessToken = getAccessToken();
  if (!accessToken) throw new Error('Your session has expired. Sign in again to continue.');
  return accessToken;
};

const getErrorMessage = (error: unknown) => {
  if (error instanceof ApiError || error instanceof Error) return error.message;
  return 'The request could not be completed.';
};

const emptyPage = <T>(pageSize = 20): PageResponse<T> => ({
  items: [],
  page: 1,
  pageSize,
  total: 0,
  totalPages: 1,
});

const refreshMarketplaceUi = () => {
  revalidatePath('/');
  revalidatePath('/wallet');
  revalidatePath('/transaction-history');
  revalidatePath('/marketplace');
  revalidatePath('/vouchers');
  revalidatePath('/prepaid');
  revalidatePath('/payment-requests');
};

export async function getVoucherCategories(): Promise<VoucherCategory[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    return await apiRequest<VoucherCategory[]>('/api/vouchers/categories', {}, accessToken);
  } catch (error) {
    console.error('Unable to load voucher categories', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function getVoucherProducts(
  categoryId?: string | null,
  page = 1,
  pageSize = 50
): Promise<PageResponse<VoucherProduct>> {
  const accessToken = getAccessToken();
  if (!accessToken) return emptyPage<VoucherProduct>(pageSize);

  try {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (categoryId) params.set('categoryId', categoryId);
    return await apiRequest<PageResponse<VoucherProduct>>(`/api/vouchers/products?${params}`, {}, accessToken);
  } catch (error) {
    console.error('Unable to load voucher products', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return emptyPage<VoucherProduct>(pageSize);
  }
}

export async function getVoucherProduct(productId: string): Promise<VoucherProduct | null> {
  const accessToken = getAccessToken();
  if (!accessToken) return null;

  try {
    return await apiRequest<VoucherProduct>(
      `/api/vouchers/products/${encodeURIComponent(productId)}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load voucher product', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
}

export async function getVoucherDenominations(
  productId: string
): Promise<ActionResult<VoucherDenomination[]>> {
  try {
    const accessToken = requireAccessToken();
    const denominations = await apiRequest<VoucherDenomination[]>(
      `/api/vouchers/products/${encodeURIComponent(productId)}/denominations`,
      {},
      accessToken
    );
    return { ok: true, data: denominations };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function previewVoucherPurchase(
  input: VoucherPurchaseInput
): Promise<ActionResult<MarketplaceQuote>> {
  try {
    const accessToken = requireAccessToken();
    const quote = await apiRequest<MarketplaceQuote>(
      '/api/voucher-orders/quote',
      {
        method: 'POST',
        body: JSON.stringify({
          voucherProductId: input.voucherProductId,
          voucherDenominationId: input.voucherDenominationId,
        }),
      },
      accessToken
    );
    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitVoucherPurchase(
  input: VoucherPurchaseInput
): Promise<ActionResult<VoucherOrder>> {
  try {
    const accessToken = requireAccessToken();
    const order = await apiRequest<VoucherOrder>(
      '/api/voucher-orders',
      {
        method: 'POST',
        body: JSON.stringify({
          voucherProductId: input.voucherProductId,
          voucherDenominationId: input.voucherDenominationId,
          idempotencyKey: input.idempotencyKey ?? `voucher-ui-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshMarketplaceUi();
    return { ok: true, data: order };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getVoucherOrders(
  page = 1,
  pageSize = 20
): Promise<PageResponse<VoucherOrder>> {
  const accessToken = getAccessToken();
  if (!accessToken) return emptyPage<VoucherOrder>(pageSize);

  try {
    return await apiRequest<PageResponse<VoucherOrder>>(
      `/api/voucher-orders?page=${page}&pageSize=${pageSize}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load voucher orders', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return emptyPage<VoucherOrder>(pageSize);
  }
}

export async function getVoucherOrder(orderId: string): Promise<VoucherOrder | null> {
  const accessToken = getAccessToken();
  if (!accessToken) return null;

  try {
    return await apiRequest<VoucherOrder>(
      `/api/voucher-orders/${encodeURIComponent(orderId)}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load voucher order', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
}

export async function pollVoucherOrder(orderId: string): Promise<ActionResult<VoucherOrder>> {
  try {
    const accessToken = requireAccessToken();
    const order = await apiRequest<VoucherOrder>(
      `/api/voucher-orders/${encodeURIComponent(orderId)}`,
      {},
      accessToken
    );
    return { ok: true, data: order };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getPrepaidOperators(productType?: string | null): Promise<PrepaidOperator[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    const query = productType ? `?productType=${encodeURIComponent(productType)}` : '';
    return await apiRequest<PrepaidOperator[]>(`/api/prepaid/operators${query}`, {}, accessToken);
  } catch (error) {
    console.error('Unable to load prepaid operators', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function getPrepaidProducts(
  operatorId?: string | null,
  productType?: string | null
): Promise<PrepaidProduct[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    const params = new URLSearchParams();
    if (operatorId) params.set('operatorId', operatorId);
    if (productType) params.set('productType', productType);
    const query = params.size ? `?${params}` : '';
    return await apiRequest<PrepaidProduct[]>(`/api/prepaid/products${query}`, {}, accessToken);
  } catch (error) {
    console.error('Unable to load prepaid products', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function validatePrepaidRecipient(
  productId: string,
  recipient: string
): Promise<ActionResult<PrepaidRecipientValidation>> {
  try {
    const accessToken = requireAccessToken();
    const validation = await apiRequest<PrepaidRecipientValidation>(
      '/api/prepaid/validate-recipient',
      {
        method: 'POST',
        body: JSON.stringify({ productId, recipient }),
      },
      accessToken
    );
    return validation.isValid
      ? { ok: true, data: validation }
      : { ok: false, error: validation.message ?? 'The recipient is invalid.' };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function previewPrepaidPurchase(
  input: PrepaidPurchaseInput
): Promise<ActionResult<MarketplaceQuote>> {
  try {
    const accessToken = requireAccessToken();
    const quote = await apiRequest<MarketplaceQuote>(
      '/api/prepaid/orders/quote',
      {
        method: 'POST',
        body: JSON.stringify({
          productId: input.productId,
          recipient: input.recipient,
          amount: input.amount,
        }),
      },
      accessToken
    );
    return { ok: true, data: quote };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function submitPrepaidPurchase(
  input: PrepaidPurchaseInput
): Promise<ActionResult<PrepaidOrder>> {
  try {
    const accessToken = requireAccessToken();
    const order = await apiRequest<PrepaidOrder>(
      '/api/prepaid/orders',
      {
        method: 'POST',
        body: JSON.stringify({
          productId: input.productId,
          recipient: input.recipient,
          amount: input.amount,
          idempotencyKey: input.idempotencyKey ?? `prepaid-ui-${randomUUID()}`,
        }),
      },
      accessToken
    );
    refreshMarketplaceUi();
    return { ok: true, data: order };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getPrepaidOrders(
  page = 1,
  pageSize = 20
): Promise<PageResponse<PrepaidOrder>> {
  const accessToken = getAccessToken();
  if (!accessToken) return emptyPage<PrepaidOrder>(pageSize);

  try {
    return await apiRequest<PageResponse<PrepaidOrder>>(
      `/api/prepaid/orders?page=${page}&pageSize=${pageSize}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load prepaid orders', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return emptyPage<PrepaidOrder>(pageSize);
  }
}

export async function getPrepaidOrder(orderId: string): Promise<PrepaidOrder | null> {
  const accessToken = getAccessToken();
  if (!accessToken) return null;

  try {
    return await apiRequest<PrepaidOrder>(
      `/api/prepaid/orders/${encodeURIComponent(orderId)}`,
      {},
      accessToken
    );
  } catch (error) {
    console.error('Unable to load prepaid order', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return null;
  }
}

export async function pollPrepaidOrder(orderId: string): Promise<ActionResult<PrepaidOrder>> {
  try {
    const accessToken = requireAccessToken();
    const order = await apiRequest<PrepaidOrder>(
      `/api/prepaid/orders/${encodeURIComponent(orderId)}`,
      {},
      accessToken
    );
    return { ok: true, data: order };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function getPaymentRequests(): Promise<PaymentRequest[]> {
  const accessToken = getAccessToken();
  if (!accessToken) return [];

  try {
    return await apiRequest<PaymentRequest[]>('/api/wallet/payment-requests', {}, accessToken);
  } catch (error) {
    console.error('Unable to load payment requests', {
      status: error instanceof ApiError ? error.status : undefined,
    });
    return [];
  }
}

export async function submitPaymentRequest(
  input: PaymentRequestInput
): Promise<ActionResult<PaymentRequest>> {
  try {
    const accessToken = requireAccessToken();
    const request = await apiRequest<PaymentRequest>(
      '/api/wallet/request-money',
      {
        method: 'POST',
        body: JSON.stringify({
          payerIdentifier: input.payerIdentifier?.trim() || null,
          amount: input.amount,
          message: input.message?.trim() || null,
          expiresAt: input.expiresAt ?? null,
        }),
      },
      accessToken
    );
    refreshMarketplaceUi();
    return { ok: true, data: request };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function payPaymentRequest(requestId: string): Promise<ActionResult<PaymentRequest>> {
  try {
    const accessToken = requireAccessToken();
    const request = await apiRequest<PaymentRequest>(
      `/api/wallet/payment-requests/${encodeURIComponent(requestId)}/pay`,
      { method: 'POST' },
      accessToken
    );
    refreshMarketplaceUi();
    return { ok: true, data: request };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}

export async function declinePaymentRequest(
  requestId: string
): Promise<ActionResult<PaymentRequest>> {
  try {
    const accessToken = requireAccessToken();
    const request = await apiRequest<PaymentRequest>(
      `/api/wallet/payment-requests/${encodeURIComponent(requestId)}/decline`,
      { method: 'POST' },
      accessToken
    );
    refreshMarketplaceUi();
    return { ok: true, data: request };
  } catch (error) {
    return { ok: false, error: getErrorMessage(error) };
  }
}
