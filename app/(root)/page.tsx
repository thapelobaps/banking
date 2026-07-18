'use server';

import { getAccounts, getTransactionsByBankId } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { redirect } from 'next/navigation';
import BankCard from '@/components/BankCard';
import HeaderBox from '@/components/HeaderBox';
import RightSidebar from '@/components/RightSidebar';
import TotalBalanceBox from '@/components/TotalBalanceBox';

export default async function Home({ searchParams }: { searchParams: { id?: string } }) {
  const user = await getLoggedInUser();
  if (!user) {
    redirect('/sign-in');
  }

  const accounts = await getAccounts({ userId: user.userId });
  if (!accounts?.data.length) {
    return (
      <section className="home">
        <div className="home-content">
          <header className="home-header">
            <HeaderBox
              type="greeting"
              title="Welcome"
              user={`${user.firstName} ${user.lastName}`}
              subtext="Your Kape App demo accounts will appear here."
            />
            <TotalBalanceBox accounts={[]} totalBanks={0} totalCurrentBalance={0} />
          </header>
          <p>No demo accounts are available.</p>
        </div>
        <RightSidebar user={user} transactions={[]} banks={[]} />
      </section>
    );
  }

  const selectedAccountId = searchParams.id ?? accounts.data[0].id;
  const account = accounts.data.find((candidate) => candidate.id === selectedAccountId) ?? accounts.data[0];
  const transactions = await getTransactionsByBankId({ bankId: account.id });

  return (
    <section className="home">
      <div className="home-content">
        <header className="home-header">
          <HeaderBox
            type="greeting"
            title="Welcome"
            user={`${user.firstName} ${user.lastName}`}
            subtext="View your South African demo accounts and recent activity."
          />
          <TotalBalanceBox
            accounts={accounts.data}
            totalBanks={accounts.totalBanks}
            totalCurrentBalance={accounts.totalCurrentBalance}
          />
        </header>
        <BankCard
          account={account}
          userName={`${user.firstName} ${user.lastName}`}
          showBalance
        />
      </div>
      <RightSidebar
        user={user}
        transactions={transactions.documents.slice(0, 5)}
        banks={accounts.data.slice(0, 2)}
      />
    </section>
  );
}
