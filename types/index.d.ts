/* eslint-disable no-unused-vars */

export type SearchParamProps = {
  params: { [key: string]: string };
  searchParams: { [key: string]: string | string[] | undefined };
};

export type SignUpParams = {
  firstName: string;
  lastName: string;
  address1: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string;
  ssn: string;
  email: string;
  password: string;
};

export type LoginUser = {
  email: string;
  password: string;
};

export type User = {
  $id: string;
  email: string;
  userId: string;
  dwollaCustomerUrl: string;
  dwollaCustomerId: string;
  firstName: string;
  lastName: string;
  name: string;
  address1: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string;
  ssn: string;
};

export type NewUserParams = {
  userId: string;
  email: string;
  name: string;
  password: string;
};

export type Account = {
  [x: string]: string;
  id: string;
  availableBalance: number;
  currentBalance: number;
  officialName: string;
  mask: string;
  institutionId: string;
  name: string;
  type: string;
  subtype: string;
  appwriteItemId: string;
  shareableId: string;
};

export type Transaction = {
  id: string;
  $id: string;
  name: string;
  paymentChannel: string;
  type: string;
  accountId: string;
  amount: number;
  pending: boolean;
  category: string;
  date: string;
  image: string;
  $createdAt: string;
  channel: string;
  senderBankId: string;
  receiverBankId: string;
};

export type Bank = {
  $id: string;
  accountId: string;
  bankId: string;
  accessToken: string;
  fundingSourceUrl: string;
  userId: string;
  shareableId: string;
};

export type AccountTypes = 'depository' | 'credit' | 'loan' | 'investment' | 'other';

export type Category = 'Food and Drink' | 'Travel' | 'Transfer';

export type CategoryCount = {
  name: string;
  count: number;
  totalCount: number;
};

export type Receiver = {
  firstName: string;
  lastName: string;
};

export type TransferParams = {
  sourceFundingSourceUrl: string;
  destinationFundingSourceUrl: string;
  amount: string;
};

export type AddFundingSourceParams = {
  dwollaCustomerId: string;
  processorToken: string;
  bankName: string;
};

export type NewDwollaCustomerParams = {
  firstName: string;
  lastName: string;
  email: string;
  type: string;
  address1: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string;
  ssn: string;
};

export interface CreditCardProps {
  account: Account;
  userName: string;
  showBalance?: boolean;
}

export interface BankInfoProps {
  account: Account;
  appwriteItemId?: string;
  type: 'full' | 'card';
}

export interface HeaderBoxProps {
  type?: 'title' | 'greeting';
  title: string;
  subtext: string;
  user?: string;
}

export interface MobileNavProps {
  user: User;
}

export interface PageHeaderProps {
  topTitle: string;
  bottomTitle: string;
  topDescription: string;
  bottomDescription: string;
  connectBank?: boolean;
}

export interface PaginationProps {
  page: number;
  totalPages: number;
}

export interface PlaidLinkProps {
  user: User;
  variant?: 'primary' | 'ghost';
  dwollaCustomerId?: string;
}

export interface AuthFormProps {
  type: 'sign-in' | 'sign-up';
}

export interface BankDropdownProps {
  accounts: Account[];
  setValue?: any; // UseFormSetValue<any>;
  otherStyles?: string;
}

export interface BankTabItemProps {
  account: Account;
  appwriteItemId?: string;
}

export interface TotalBalanceBoxProps {
  accounts: Account[];
  totalBanks: number;
  totalCurrentBalance: number;
}

export interface FooterProps {
  user: User;
  type?: 'mobile' | 'desktop';
}

export interface RightSidebarProps {
  user: User;
  transactions: Transaction[];
  banks: Bank[] & Account[];
}

export interface SiderbarProps {
  user: User;
}

export interface RecentTransactionsProps {
  accounts: Account[];
  transactions: Transaction[];
  appwriteItemId: string;
  page: number;
}

export interface TransactionHistoryTableProps {
  transactions: Transaction[];
  page: number;
}

export interface CategoryBadgeProps {
  category: string;
}

export interface TransactionTableProps {
  transactions: Transaction[];
}

export interface CategoryProps {
  category: CategoryCount;
}

export interface DoughnutChartProps {
  accounts: Account[];
}

export interface PaymentTransferFormProps {
  accounts: Account[];
}

export interface getAccountsProps {
  userId: string;
}

export interface getAccountProps {
  appwriteItemId: string;
}

export interface getInstitutionProps {
  institutionId: string;
}

export interface getTransactionsProps {
  accessToken: string;
}

export interface CreateFundingSourceOptions {
  customerId: string;
  fundingSourceName: string;
  plaidToken: string;
  _links: object;
}

export interface CreateTransactionProps {
  name: string;
  amount: string;
  senderId: string;
  senderBankId: string;
  receiverId: string;
  receiverBankId: string;
  email: string;
}

export interface getTransactionsByBankIdProps {
  bankId: string;
}

export interface signInProps {
  email: string;
  password: string;
}

export interface getUserInfoProps {
  userId: string;
}

export interface exchangePublicTokenProps {
  publicToken: string;
  user: User;
}

export interface createBankAccountProps {
  accessToken: string;
  userId: string;
  accountId: string;
  bankId: string;
  fundingSourceUrl: string;
  shareableId: string;
}

export interface getBanksProps {
  userId: string;
}

export interface getBankProps {
  documentId: string;
}

export interface getBankByAccountIdProps {
  accountId: string;
}

export interface CategoryCount {
  name: string;
  count: number;
  totalCount: number;
}

export interface Transaction {
  id: string;
  $id: string;
  name: string;
  paymentChannel: string;
  type: string;
  accountId: string;
  amount: number;
  createdAt: string;
  category: string;
  date: string;
  image: string;
  typeIcon: string;
}

export interface User {
  $id: string;
  email: string;
  userId: string;
  dwollaCustomerId: string;
  dwollaCustomerUrl: string;
  firstName: string;
  lastName: string;
  name: string;
  address1: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string;
  ssn: string;
}

export interface SignUpParams {
  firstName: string;
  lastName: string;
  address1: string;
  city: string;
  state: string;
  postalCode: string;
  dateOfBirth: string;
  ssn: string;
  email: string;
  password: string;
}

export interface Account {
  id: string;
  availableBalance: number;
  currentBalance: number;
  institutionId: string;
  name: string;
  officialName: string;
  mask: string;
  type: string;
  subtype: string;
  appwriteItemId: string;
  shareableId: string;
}

export interface CreateTransactionProps {
  senderBankId: string;
  receiverBankId: string;
  amount: number;
  name: string;
}