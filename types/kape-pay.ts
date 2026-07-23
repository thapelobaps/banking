import type { PrepaidOrder, VoucherOrder } from '@/types/marketplace';

export type KapePaySource = 'wallet' | 'linked_bank';
export type DemoPaymentScenario =
  | 'success'
  | 'awaiting_approval'
  | 'pending'
  | 'failed'
  | 'cancelled'
  | 'insufficient_funds';

export type KapePayQuote = {
  orderType: 'voucher' | 'prepaid';
  paymentSource: KapePaySource;
  amount: number;
  feeAmount: number;
  totalAmount: number;
  currency: 'ZAR';
  linkedBankAccountId?: string | null;
  status: string;
  expiresAt: string;
  disclaimer: string;
};

export type PaymentStatusHistory = {
  id: string;
  previousStatus?: string | null;
  status: string;
  source: string;
  reason?: string | null;
  externalEventId?: string | null;
  createdAt: string;
};

export type PaymentAttempt = {
  id: string;
  orderType: 'voucher' | 'prepaid';
  orderId: string;
  providerId: string;
  paymentSource: KapePaySource;
  externalPaymentId: string;
  amount: number;
  feeAmount: number;
  currency: 'ZAR';
  status: string;
  scenario: string;
  reference: string;
  linkedBankAccountId?: string | null;
  walletId?: string | null;
  walletTransactionId?: string | null;
  redirectUrl?: string | null;
  failureCode?: string | null;
  createdAt: string;
  updatedAt: string;
  expiresAt?: string | null;
  completedAt?: string | null;
  history: PaymentStatusHistory[];
};

export type VoucherCheckout = {
  order: VoucherOrder;
  payment: PaymentAttempt;
};

export type PrepaidCheckout = {
  order: PrepaidOrder;
  payment: PaymentAttempt;
};

export type KapePayVoucherInput = {
  voucherProductId: string;
  voucherDenominationId: string;
  paymentSource: KapePaySource;
  linkedBankAccountId?: string | null;
  scenario?: DemoPaymentScenario;
  returnUrl?: string | null;
  idempotencyKey?: string | null;
};

export type KapePayPrepaidInput = {
  productId: string;
  recipient: string;
  amount: number;
  paymentSource: KapePaySource;
  linkedBankAccountId?: string | null;
  scenario?: DemoPaymentScenario;
  returnUrl?: string | null;
  idempotencyKey?: string | null;
};

export type PaymentRefund = {
  id: string;
  paymentAttemptId: string;
  providerId: string;
  externalRefundId: string;
  amount: number;
  currency: 'ZAR';
  status: string;
  reason: string;
  createdAt: string;
  completedAt?: string | null;
};

export type PaymentReconciliationIssue = {
  id: string;
  paymentAttemptId?: string | null;
  issueType: string;
  description: string;
  severity: string;
  createdAt: string;
  resolvedAt?: string | null;
};

export type PaymentReconciliation = {
  runId: string;
  checkedPayments: number;
  matchedPayments: number;
  issueCount: number;
  status: string;
  startedAt: string;
  completedAt: string;
  issues: PaymentReconciliationIssue[];
};
