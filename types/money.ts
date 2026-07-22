export type ResolvedKapeUser = {
  userId: string;
  displayName: string;
  maskedIdentifier: string;
};

export type WalletTransferInput = {
  recipientUserId: string;
  amount: number;
  reference?: string | null;
  idempotencyKey?: string | null;
};

export type UnifiedMoneyActivity = {
  id: string;
  sourceId: string;
  sourceLabel: string;
  sourceType: 'wallet' | 'linked_bank';
  title: string;
  category: string;
  direction: 'credit' | 'debit';
  amount: number;
  status: string;
  occurredAt: string;
  detail?: string | null;
};
