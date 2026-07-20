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
      <section className="kape-dashboard">
        <div className="kape-page">
          <HeaderBox
            type="greeting"
            title="Welcome"
            user={`${user.firstName} ${user.lastName}`}
            subtext="Your Kape App demo accounts will appear here."
          />
          <TotalBalanceBox accounts={[]} totalBanks={0} totalCurrentBalance={0} />
          <div className="kape-empty-state">
            <strong>No demo accounts are available</strong>
            <span>Create or seed an account through the Kape API.</span>
          </div>
        </div>
      </section>
    );
  }

  const selectedAccountId = searchParams.id ?? accounts.data[0].id;
  const account = accounts.data.find((candidate) => candidate.id === selectedAccountId) ?? accounts.data[0];
  const accountTransactionResults = await Promise.all(
    accounts.data.map(async (candidate) => ({
      accountId: candidate.id,
      result: await getTransactionsByBankId({ bankId: candidate.id }),
    }))
  );
  const transactions = accountTransactionResults.find((entry) => entry.accountId === account.id)?.result ?? {
    total: 0,
    documents: [],
  };
  const railTransactions = accountTransactionResults
    .flatMap((entry) => entry.result.documents)
    .sort((left, right) => new Date(right.date).getTime() - new Date(left.date).getTime())
    .slice(0, 12);

  return (
    <section className="kape-dashboard">
      <div className="kape-page">
        <header className="kape-page-header">
          <HeaderBox
            type="greeting"
            title="Welcome back"
            user={user.firstName}
            subtext="Here is a clear view of your Kape demo money today."
          />
          <div className="kape-page-actions">
            <Link href="/transaction-history" className="kape-button kape-button--secondary">
              View activity
            </Link>
            <Link href="/payment-transfer" className="kape-button kape-button--primary">
              Transfer money
            </Link>
          </div>
        </header>

        <TotalBalanceBox
          accounts={accounts.data}
          totalBanks={accounts.totalBanks}
          totalCurrentBalance={accounts.totalCurrentBalance}
        />

        <section className="kape-overview-grid">
          <div className="kape-primary-account">
            <div className="kape-section-heading">
              <div>
                <h2>Primary account</h2>
                <p>Your currently selected demo account</p>
              </div>
              <Link href="/my-banks">All accounts</Link>
            </div>
            <BankCard account={account} userName={`${user.firstName} ${user.lastName}`} showBalance />
          </div>

          <TransactionsTable transactions={transactions.documents.slice(0, 5)} />
        </section>
      </div>

      <RightSidebar
        user={user}
        transactions={railTransactions}
        banks={accounts.data.slice(0, 2)}
      />
    </section>
  );
}
