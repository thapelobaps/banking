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
  if (!loggedIn) {
    redirect('/sign-in');
  }

  const accounts = await getAccounts({ userId: loggedIn.userId });
  if (!accounts?.data.length) {
    return (
      <div className="transactions">
        <HeaderBox title="Transaction History" subtext="No demo accounts are available." />
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
    <div className="transactions">
      <div className="transactions-header">
        <HeaderBox
          title="Transaction History"
          subtext="Review SQL-backed South African demo transactions."
        />
      </div>

      <div className="space-y-6">
        <div className="transactions-account">
          <div className="flex flex-col gap-2">
            <h2 className="text-18 font-bold text-white">{selectedAccount.name}</h2>
            <p className="text-14 text-blue-25">{selectedAccount.officialName}</p>
            <p className="text-14 font-semibold tracking-[1.1px] text-white">
              ●●●● ●●●● ●●●● {selectedAccount.mask}
            </p>
          </div>

          <div className="transactions-account-balance">
            <p className="text-14">Current balance</p>
            <p className="text-24 text-center font-bold">
              {formatAmount(selectedAccount.currentBalance)}
            </p>
          </div>
        </div>

        <section className="flex w-full flex-col gap-6">
          <TransactionsTable transactions={currentTransactions} />
          {totalPages > 1 && (
            <div className="my-4 w-full">
              <Pagination totalPages={totalPages} page={currentPage} />
            </div>
          )}
        </section>
      </div>
    </div>
  );
};

export default TransactionHistory;
