export type SouthAfricanBankName =
  | 'Capitec'
  | 'FNB'
  | 'Absa'
  | 'Standard Bank'
  | 'Nedbank'
  | 'TymeBank'
  | 'Discovery Bank';

export type DemoAccountScenario =
  | 'salary-current'
  | 'everyday-spending'
  | 'family-budget'
  | 'starter-savings'
  | 'side-hustle'
  | 'digital-everyday'
  | 'premium-current';

export type DemoTransactionChannel =
  | 'card'
  | 'EFT'
  | 'debit order'
  | 'cash'
  | 'internal transfer';

export interface SupportedSouthAfricanBank {
  id: string;
  name: SouthAfricanBankName;
  branchCode: string;
  isDemo: true;
}

export interface CreateDemoAccountInput {
  email: string;
}

export interface DemoBankAccount {
  providerId: string;
  bankId: string;
  accountId: string;
  accountNumber: string;
  branchCode: string;
  bankName: SouthAfricanBankName;
  accountType: 'transaction' | 'savings';
  currentBalance: number;
  availableBalance: number;
  currency: 'ZAR';
  linkedAt: string;
  scenario: DemoAccountScenario;
  isDemo: true;
}

export interface DemoBankTransaction {
  id: string;
  accountId: string;
  name: string;
  statementDescription: string;
  beneficiary?: string;
  amount: number;
  direction: 'credit' | 'debit';
  category: string;
  channel: DemoTransactionChannel;
  date: string;
  status: 'completed' | 'pending';
  isDemo: true;
}

export interface BankingProvider {
  readonly id: string;
  listSupportedBanks(): readonly SupportedSouthAfricanBank[];
  createDemoAccount(input: CreateDemoAccountInput): DemoBankAccount;
  createDemoTransactions(account: DemoBankAccount): DemoBankTransaction[];
}
