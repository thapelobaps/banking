'use server';
import { ID, Query } from 'node-appwrite';
import { createAdminClient } from '../appwrite';
import { parseStringify } from '../utils';
import { CreateTransactionProps, getAccountsProps, getTransactionsByBankIdProps } from '@/types';

const {
  APPWRITE_DATABASE_ID: DATABASE_ID,
  APPWRITE_TRANSACTION_COLLECTION_ID: TRANSACTION_COLLECTION_ID,
  APPWRITE_BANK_COLLECTION_ID: BANK_COLLECTION_ID,
} = process.env;

export const getAccounts = async ({ userId }: getAccountsProps) => {
  try {
    const { database } = await createAdminClient();
    const accounts = await database.listDocuments(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      [Query.equal('userId', userId)]
    );

    const formattedAccounts = accounts.documents.map((bank: any) => ({
      id: bank.accountId,
      availableBalance: bank.balance,
      currentBalance: bank.balance,
      institutionId: 'mock_institution',
      name: bank.bankName,
      officialName: bank.bankName,
      mask: bank.accountNumber.slice(-4),
      type: 'depository',
      subtype: 'transaction',
      appwriteItemId: bank.$id,
      // Demo-only Appwrite document reference. This is not a real bank identifier.
      shareableId: bank.$id,
    }));

    const totalBanks = accounts.documents.length;
    const totalCurrentBalance = accounts.documents.reduce((total: number, account: any) => {
      return total + account.balance;
    }, 0);

    return parseStringify({ data: formattedAccounts, totalBanks, totalCurrentBalance });
  } catch (error: any) {
    console.error('An error occurred while getting the accounts:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return null;
  }
};

export const createTransaction = async (transaction: CreateTransactionProps) => {
  try {
    const { database } = await createAdminClient();
    const newTransaction = await database.createDocument(
      DATABASE_ID!,
      TRANSACTION_COLLECTION_ID!,
      ID.unique(),
      {
        channel: 'online',
        category: 'Transfer',
        ...transaction,
      }
    );
    return parseStringify(newTransaction);
  } catch (error: any) {
    console.error('Create transaction error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return null;
  }
};

export const getTransactionsByBankId = async ({ bankId }: getTransactionsByBankIdProps) => {
  try {
    const { database } = await createAdminClient();
    const senderTransactions = await database.listDocuments(
      DATABASE_ID!,
      TRANSACTION_COLLECTION_ID!,
      [Query.equal('senderBankId', bankId)]
    );
    const receiverTransactions = await database.listDocuments(
      DATABASE_ID!,
      TRANSACTION_COLLECTION_ID!,
      [Query.equal('receiverBankId', bankId)]
    );
    const transactions = {
      total: senderTransactions.total + receiverTransactions.total,
      documents: [...senderTransactions.documents, ...receiverTransactions.documents],
    };
    return parseStringify(transactions);
  } catch (error: any) {
    console.error('Get transactions error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return { total: 0, documents: [] };
  }
};

export const createMockTransfer = async ({
  senderBankId,
  receiverBankId,
  amount,
  name,
}: {
  senderBankId: string;
  receiverBankId: string;
  amount: number;
  name: string;
}) => {
  try {
    const { database } = await createAdminClient();

    if (senderBankId === receiverBankId) {
      throw new Error('Sender and receiver accounts must be different');
    }

    if (!Number.isFinite(amount) || amount <= 0) {
      throw new Error('Transfer amount must be greater than zero');
    }

    const senderBank = await database.getDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      senderBankId
    );
    const receiverBank = await database.getDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      receiverBankId
    );

    const senderBalance = Number(senderBank.balance);
    const receiverBalance = Number(receiverBank.balance);

    if (!Number.isFinite(senderBalance) || !Number.isFinite(receiverBalance)) {
      throw new Error('Demo account balance is invalid');
    }

    if (senderBalance < amount) {
      throw new Error('Insufficient demo balance');
    }

    let senderUpdated = false;
    let receiverUpdated = false;

    try {
      await database.updateDocument(
        DATABASE_ID!,
        BANK_COLLECTION_ID!,
        senderBankId,
        { balance: senderBalance - amount }
      );
      senderUpdated = true;

      await database.updateDocument(
        DATABASE_ID!,
        BANK_COLLECTION_ID!,
        receiverBankId,
        { balance: receiverBalance + amount }
      );
      receiverUpdated = true;

      const transaction = await database.createDocument(
        DATABASE_ID!,
        TRANSACTION_COLLECTION_ID!,
        ID.unique(),
        {
          senderBankId,
          receiverBankId,
          amount,
          name,
          channel: 'online',
          category: 'Transfer',
          date: new Date().toISOString(),
        }
      );

      return parseStringify(transaction);
    } catch (transferError) {
      // Appwrite document updates are not transactional. Restore the original
      // mock balances when a later step fails so demo data stays consistent.
      if (receiverUpdated) {
        await database
          .updateDocument(DATABASE_ID!, BANK_COLLECTION_ID!, receiverBankId, {
            balance: receiverBalance,
          })
          .catch(() => undefined);
      }

      if (senderUpdated) {
        await database
          .updateDocument(DATABASE_ID!, BANK_COLLECTION_ID!, senderBankId, {
            balance: senderBalance,
          })
          .catch(() => undefined);
      }

      throw transferError;
    }
  } catch (error: any) {
    console.error('Create mock transfer error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return null;
  }
};
