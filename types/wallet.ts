export type PageResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
};

export type WalletSummary = {
  id: string;
  currency: 'ZAR';
  status: string;
  balance: number;
  availableBalance: number;
  createdAt: string;
  updatedAt: string;
};

export type WalletTransaction = {
  id: string;
  type: string;
  amount: number;
  feeAmount: number;
  netAmount: number;
  status: string;
  reference: string;
  externalReference?: string | null;
  relatedUserId?: string | null;
  createdAt: string;
  completedAt?: string | null;
};

export type PaymentMethod = {
  id: string;
  providerId: string;
  brand: string;
  bankName: string;
  last4: string;
  expiryMonth: number;
  expiryYear: number;
  isDefault: boolean;
  status: string;
  createdAt: string;
  verifiedAt?: string | null;
};

export type WalletFundingOperation = 'top_up' | 'withdrawal';

export type WalletFundingInput = {
  operation: WalletFundingOperation;
  amount: number;
  paymentMethodId?: string | null;
  linkedBankAccountId?: string | null;
  reference?: string | null;
  idempotencyKey?: string | null;
};

export type WalletFundingQuote = {
  operation: WalletFundingOperation;
  amount: number;
  feeAmount: number;
  totalAmount: number;
  currency: 'ZAR';
  status: string;
  expiresAt: string;
};

export type LedgerReconciliation = {
  walletId: string;
  ledgerBalance: number;
  postedWalletTransactions: number;
  difference: number;
  isBalanced: boolean;
  reconciledAt: string;
};

export type BankConnection = {
  id: string;
  providerId: string;
  institutionId: string;
  institutionName: string;
  status: string;
  consentExpiresAt?: string | null;
  lastSyncedAt?: string | null;
  createdAt: string;
};

export type BankConnectionSync = {
  connectionId: string;
  status: string;
  linkedAccounts: number;
  importedTransactions: number;
  importedDebitOrders: number;
  syncedAt: string;
};

export type LinkedBankAccount = {
  id: string;
  bankConnectionId: string;
  institutionName: string;
  accountName: string;
  accountType: string;
  accountNumberMask: string;
  currency: 'ZAR';
  currentBalance: number;
  availableBalance: number;
  isActive: boolean;
  lastSyncedAt?: string | null;
};

export type LinkedBankTransaction = {
  id: string;
  linkedBankAccountId: string;
  description: string;
  merchantName?: string | null;
  amount: number;
  direction: 'credit' | 'debit';
  category: string;
  status: string;
  postedAt: string;
};

export type DebitOrder = {
  id: string;
  linkedBankAccountId: string;
  merchantName: string;
  amount?: number | null;
  frequency: string;
  status: string;
  nextRunAt?: string | null;
  lastRunAt?: string | null;
};

export type DemoCardInput = {
  bankName: 'Capitec' | 'Standard Bank' | 'FNB' | 'Absa' | 'Nedbank';
  brand: 'Mastercard' | 'Visa';
  last4: string;
  expiryMonth: number;
  expiryYear: number;
};

export type ActionResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: string };
