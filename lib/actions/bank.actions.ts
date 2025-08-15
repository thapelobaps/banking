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
      [Query.equal('userId', userId)] // Query by userId, not $id
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
      subtype: 'checking',
      appwriteItemId: bank.$id,
      shareableId: bank.accountId,
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

    // Fetch sender and receiver bank documents
    const senderBank = await database.listDocuments(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      [Query.equal('$id', senderBankId)] // Single string for $id
    );
    const receiverBank = await database.listDocuments(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      [Query.equal('$id', receiverBankId)] // Single string for $id
    );

    if (!senderBank.documents[0] || !receiverBank.documents[0]) {
      throw new Error('Sender or receiver bank not found');
    }

    // Update sender balance
    await database.updateDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      senderBankId,
      { balance: senderBank.documents[0].balance - amount }
    );

    // Update receiver balance
    await database.updateDocument(
      DATABASE_ID!,
      BANK_COLLECTION_ID!,
      receiverBankId,
      { balance: receiverBank.documents[0].balance + amount }
    );

    // Create transaction
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
  } catch (error: any) {
    console.error('Create mock transfer error:', {
      message: error.message,
      code: error.code,
      type: error.type,
    });
    return null;
  }
};