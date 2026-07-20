/* eslint-disable no-unused-vars */

export type SearchParamProps = {
  params: { [key: string]: string };
  searchParams: { [key: string]: string | string[] | undefined };
};

export type SouthAfricanProvince =
  | 'Eastern Cape'
  | 'Free State'
  | 'Gauteng'
  | 'KwaZulu-Natal'
  | 'Limpopo'
  | 'Mpumalanga'
  | 'North West'
  | 'Northern Cape'
  | 'Western Cape';

export type SignUpParams = {
  firstName: string;
  lastName: string;
  email: string;
  mobileNumber: string;
  password: string;
  confirmPassword: string;
  address1: string;
  suburb: string;
  city: string;
  province: SouthAfricanProvince;
  postalCode: string;
  dateOfBirth: string;
  country: 'South Africa';
  termsAccepted: boolean;
  privacyAccepted: boolean;
};

export type LoginUser = {
  email: string;
  password: string;
};

export type User = {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  name?: string;
  mobileNumber?: string;
  address1?: string;
  suburb?: string;
  city?: string;
  province?: SouthAfricanProvince;
  postalCode?: string;
  dateOfBirth?: string;
  country?: 'South Africa';
  termsAcceptedAt?: string;
  privacyAcceptedAt?: string;
};

export type NewUserParams = {
  userId: string;
  email: string;
  name: string;
  password: string;
};

export type Account = {
  id: string;
  availableBalance: number;
  currentBalance: number;
  officialName: string;
  mask: string;
  institutionId: string;
  name: string;
  type: string;
  subtype: string;
  branchCode: string;
  accountNumber: string;
  currency: 'ZAR';
  demoReference: string;
  isDemo: boolean;
};

export type Transaction = {
  id: string;
  name: string;
  amount: number;
  category: string;
  date: string;
  paymentChannel?: string;
  channel?: string;
  type?: string;
  accountId?: string;
  relatedAccountId?: string;
  status?: string;
  statementDescription?: string;
  beneficiary?: string;
  isDemo?: boolean;
  pending?: boolean;
  image?: string;
  typeIcon?: string;
  createdAt?: string;
};

export type Bank = {
  id: string;
  userId: string;
  accountId: string;
  bankId: string;
  accountNumber: string;
  branchCode: string;
  bankName: string;
  currentBalance: number;
  availableBalance: number;
  currency: 'ZAR';
  createdAt: string;
  isDemo: boolean;
};

export type AccountTypes = 'depository' | 'credit' | 'loan' | 'investment' | 'other';

export type Category = 'Food and Drink' | 'Travel' | 'Transfer' | 'Shopping' | string;

export type CategoryCount = {
  name: string;
  count: number;
  totalCount: number;
};

export type Receiver = {
  firstName: string;
  lastName: string;
};

export interface CreditCardProps {
  account: Account;
  userName: string;
  showBalance?: boolean;
}

export interface BankInfoProps {
  account: Account;
  accountId?: string;
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

export interface AuthFormProps {
  type: 'sign-in' | 'sign-up';
}

export interface BankDropdownProps {
  accounts: Account[];
  setValue?: (...args: any[]) => void;
  otherStyles?: string;
}

export interface BankTabItemProps {
  account: Account;
  accountId?: string;
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
  banks: Account[];
}

export interface SiderbarProps {
  user: User;
}

export interface RecentTransactionsProps {
  accounts: Account[];
  transactions: Transaction[];
  accountId: string;
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
  accountId: string;
}

export interface getInstitutionProps {
  institutionId: string;
}

export interface getTransactionsProps {
  accountId: string;
}

export interface CreateTransactionProps {
  name: string;
  amount: number | string;
  senderId?: string;
  senderBankId: string;
  receiverId?: string;
  receiverBankId: string;
  email?: string;
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

export interface getBanksProps {
  userId: string;
}

export interface getBankProps {
  accountId: string;
}

export interface getBankByAccountIdProps {
  accountId: string;
}
