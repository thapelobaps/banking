import Link from 'next/link';
import HeaderBox from '@/components/HeaderBox';
import { Pagination } from '@/components/Pagination';
import TransactionsTable from '@/components/TransactionsTable';
import { getAccounts, getTransactionsByBankId } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount } from '@/lib/utils';
import { SearchParamProps } from '@/types';
import { redirect } from 'next/navigation';

const TransactionHistory = async ({ searchParams: { id, page } }: SearchParamProps) => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const accounts = await getAccounts({ userId: loggedIn.userId });
  if (!accounts?.data.length) {
    return (
      <div className="kape-page">
        <HeaderBox title="Transactions" subtext="No demo accounts are available." />
      </div>
    );
  }

  const requestedAccountId = Array.isArray(id) ? id[0] : id;
  const selectedAccount =
    accounts.data.find((account) => account.id === requestedAccountId) ?? accounts.data[0];
  const result = await getTransactionsByBankId({ bankId: selectedAccount.id });

  const currentPage = Math.max(1, Number(Array.isArray(page) ? page[0] : page) || 1);
  const rowsPerPage = 10;
  const totalPages = Math.max(1, Math.ceil(result.documents.length / rowsPerPage));
  const indexOfFirstTransaction = (currentPage - 1) * rowsPerPage;
  const currentTransactions = result.documents.slice(
    indexOfFirstTransaction,
    indexOfFirstTransaction + rowsPerPage
  );

  return (
    <div className="kape-page">
      <div className="kape-page-header">
        <HeaderBox
          title="Transactions"
          subtext="Search through your latest South African demo account activity."
        />
        <span className="kape-count-pill">{result.total} transactions</span>
      </div>

      <div className="kape-account-tabs">
        {accounts.data.map((account) => {
          const active = account.id === selectedAccount.id;
          return (
            <Link
              key={account.id}
              href={`/transaction-history?id=${account.id}`}
              className={active ? 'is-active' : ''}
            >
              <strong>{account.name}</strong>
              <span>•••• {account.mask}</span>
            </Link>
          );
        })}
      </div>

      <section className="kape-selected-account">
        <div>
          <span>Selected account</span>
          <h2>{selectedAccount.name}</h2>
          <p>•••• •••• •••• {selectedAccount.mask}</p>
        </div>
        <div>
          <span>Current balance</span>
          <strong>{formatAmount(selectedAccount.currentBalance)}</strong>
          <small>Branch code {selectedAccount.branchCode}</small>
        </div>
      </section>

      <section className="kape-transaction-section">
        <TransactionsTable transactions={currentTransactions} />
        {totalPages > 1 && (
          <div className="kape-pagination-wrap">
            <Pagination totalPages={totalPages} page={currentPage} />
          </div>
        )}
      </section>
    </div>
  );
};

export default TransactionHistory;
