'use server';

// Compatibility entry point while transaction logic is consolidated in
// bank.actions.ts. Existing imports can continue to use this path without
// maintaining a second implementation.
export {
  createMockTransfer,
  createTransaction,
  getTransactionsByBankId,
} from './bank.actions';
