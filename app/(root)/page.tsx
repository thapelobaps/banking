'use server';

import Link from 'next/link';
import { redirect } from 'next/navigation';
import { getAccounts, getTransactionsByBankId } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import BankCard from '@/components/BankCard';
import HeaderBox from '@/components/HeaderBox';
import RightSidebar from '@/components/RightSidebar';
import TotalBalanceBox from '@/components/TotalBalanceBox';
import TransactionsTable from '@/components/TransactionsTable';

export default async function Home({ searchParams }: { searchParams: { id?: string } }) {
  const user = await getLoggedInUser();
  if (!user) redirect('/sign-in');

  const accounts = await getAccounts({ userId: user.userId });
  if (!accounts?.data.length) {
    return (
      <section className="home">
        <div className="home-content">
          <HeaderBox
            type="greeting"
            title="Welcome"
            user={`${user.firstName} ${user.lastName}`}
            subtext="Your Kape App demo accounts will appear here."
          />
          <TotalBalanceBox accounts={[]} totalBanks={0} totalCurrentBalance={0} />
          <div className="rounded-3xl border border-dashed border-[#d8c8be] bg-white p-10 text-center">
            <p className="font-semibold text-[#2b1a14]">No demo accounts are available</p>
            <p className="mt-2 text-sm text-[#8a756b]">Create or seed an account through the Kape API.</p>
          </div>
        </div>
      </section>
    );
  }

  const selectedAccountId = searchParams.id ?? accounts.data[0].id;
  const account = accounts.data.find((candidate) => candidate.id === selectedAccountId) ?? accounts.data[0];
  const transactions = await getTransactionsByBankId({ bankId: account.id });

  return (
    <section className="home bg-[#f8f5f2]">
      <div className="home-content">
        <header className="flex flex-col gap-7">
          <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
            <HeaderBox
              type="greeting"
              title="Welcome back"
              user={user.firstName}
              subtext="Here is a clear view of your Kape demo money today."
            />
            <div className="flex gap-2">
              <Link href="/transaction-history" className="rounded-xl border border-[#d9c9bf] bg-white px-4 py-2.5 text-sm font-semibold text-[#4a2b20] transition hover:bg-[#fbf7f4]">
                View activity
              </Link>
              <Link href="/payment-transfer" className="rounded-xl bg-[#4a2b20] px-4 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-[#382017]">
                Transfer money
              </Link>
            </div>
          </div>

          <TotalBalanceBox
            accounts={accounts.data}
            totalBanks={accounts.totalBanks}
            totalCurrentBalance={accounts.totalCurrentBalance}
          />
        </header>

        <section className="grid gap-6 2xl:grid-cols-[minmax(0,420px)_1fr]">
          <div>
            <div className="mb-4 flex items-center justify-between">
              <div>
                <h2 className="text-lg font-semibold text-[#2b1a14]">Primary account</h2>
                <p className="mt-1 text-sm text-[#8a756b]">Your currently selected demo account</p>
              </div>
              <Link href="/my-banks" className="text-sm font-semibold text-[#7a4a37]">All accounts</Link>
            </div>
            <BankCard account={account} userName={`${user.firstName} ${user.lastName}`} showBalance />
          </div>

          <TransactionsTable transactions={transactions.documents.slice(0, 5)} />
        </section>
      </div>

      <RightSidebar
        user={user}
        transactions={transactions.documents.slice(0, 5)}
        banks={accounts.data.slice(0, 2)}
      />
    </section>
  );
}
