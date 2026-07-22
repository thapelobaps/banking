import type { WalletFundingQuote } from '@/types/wallet';

export type VoucherCategory = {
  id: string;
  slug: string;
  name: string;
  sortOrder: number;
};

export type VoucherProvider = {
  id: string;
  providerKey: string;
  name: string;
  status: string;
  lastCatalogueSyncAt?: string | null;
};

export type VoucherProduct = {
  id: string;
  categoryId: string;
  providerId: string;
  slug: string;
  brandName: string;
  productName: string;
  description: string;
  currency: 'ZAR';
  fulfilmentType: string;
  isActive: boolean;
};

export type VoucherDenomination = {
  id: string;
  voucherProductId: string;
  amount: number;
  feeAmount: number;
  isActive: boolean;
};

export type VoucherOrder = {
  id: string;
  voucherProductId: string;
  amount: number;
  feeAmount: number;
  status: string;
  voucherCode?: string | null;
  externalOrderId: string;
  createdAt: string;
  fulfilledAt?: string | null;
};

export type PrepaidOperator = {
  id: string;
  operatorKey: string;
  name: string;
  productType: string;
  isActive: boolean;
};

export type PrepaidProduct = {
  id: string;
  operatorId: string;
  name: string;
  productType: string;
  fixedAmount?: number | null;
  minimumAmount: number;
  maximumAmount: number;
  feeAmount: number;
  isActive: boolean;
};

export type PrepaidRecipientValidation = {
  isValid: boolean;
  normalisedRecipient: string;
  productType: string;
  message?: string | null;
};

export type PrepaidOrder = {
  id: string;
  productId: string;
  recipient: string;
  amount: number;
  feeAmount: number;
  status: string;
  externalOrderId: string;
  fulfilmentReference?: string | null;
  createdAt: string;
  fulfilledAt?: string | null;
};

export type PaymentRequest = {
  id: string;
  payeeUserId: string;
  payerUserId?: string | null;
  amount: number;
  currency: 'ZAR';
  message: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  respondedAt?: string | null;
};

export type VoucherPurchaseInput = {
  voucherProductId: string;
  voucherDenominationId: string;
  idempotencyKey?: string | null;
};

export type PrepaidPurchaseInput = {
  productId: string;
  recipient: string;
  amount: number;
  idempotencyKey?: string | null;
};

export type PaymentRequestInput = {
  payerIdentifier?: string | null;
  amount: number;
  message?: string | null;
  expiresAt?: string | null;
};

export type MarketplaceQuote = WalletFundingQuote;
