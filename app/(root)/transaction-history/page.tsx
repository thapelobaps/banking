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
      <div className="transactions">
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
    <div className="transactions">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-end">
        <HeaderBox
          title="Transactions"
          subtext="Search through your latest South African demo account activity."
        />
        <span className="w-fit rounded-full border border-[#dfd0c7] bg-white px-3 py-1.5 text-xs font-semibold text-[#6b4435]">
          {result.total} transactions
        </span>
      </div>

      <div className="flex gap-2 overflow-x-auto pb-1">
        {accounts.data.map((account) => {
          const active = account.id === selectedAccount.id;
          return (
            <Link
              key={account.id}
              href={`/transaction-history?id=${account.id}`}
              className={`min-w-fit rounded-2xl border px-4 py-3 transition ${
                active
                  ? 'border-[#4a2b20] bg-[#4a2b20] text-white shadow-sm'
                  : 'border-[#eadfd8] bg-white text-[#6f5b52] hover:border-[#cdb9ad]'
              }`}
            >
              <p className="text-sm font-semibold">{account.name}</p>
              <p className={`mt-1 text-xs ${active ? 'text-white/60' : 'text-[#9a8378]'}`}>•••• {account.mask}</p>
            </Link>
          );
        })}
      </div>

      <section className="overflow-hidden rounded-3xl bg-gradient-to-r from-[#2b1811] via-[#4a2b20] to-[#724634] p-6 text-white shadow-[0_22px_55px_-32px_rgba(61,34,24,0.9)]">
        <div className="flex flex-col justify-between gap-6 md:flex-row md:items-end">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/55">Selected account</p>
            <h2 className="mt-3 text-2xl font-semibold">{selectedAccount.name}</h2>
            <p className="mt-2 font-mono text-sm tracking-[0.15em] text-white/70">•••• •••• •••• {selectedAccount.mask}</p>
          </div>
          <div className="md:text-right">
            <p className="text-sm text-white/60">Current balance</p>
            <p className="mt-2 text-3xl font-semibold tabular-nums">{formatAmount(selectedAccount.currentBalance)}</p>
            <p className="mt-2 text-xs text-white/50">Branch code {selectedAccount.branchCode}</p>
          </div>
        </div>
      </section>

      <section className="flex w-full flex-col gap-6">
        <TransactionsTable transactions={currentTransactions} />
        {totalPages > 1 && (
          <div className="my-4 w-full">
            <Pagination totalPages={totalPages} page={currentPage} />
          </div>
        )}
      </section>
    </div>
  );
};

export default TransactionHistory;
