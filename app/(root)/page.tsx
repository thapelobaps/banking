'use server';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { getAccounts } from '@/lib/actions/bank.actions';
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
  if (!accounts || !accounts.data || accounts.data.length === 0) {
    return (
      <section className="home">
        <div className="home-content">
          <header className="home-header">
            <HeaderBox
              type="greeting"
              title="Welcome"
              user={`${user.firstName} ${user.lastName}` || 'Guest'}
              subtext="Access and manage your account and transactions efficiently."
            />
            <TotalBalanceBox accounts={[]} totalBanks={0} totalCurrentBalance={0} />
          </header>
          <p>No bank accounts found. Add a bank to get started.</p>
        </div>
        <RightSidebar user={user} transactions={[]} banks={[]} />
      </section>
    );
  }

  // Use the first account or select based on searchParams.id if needed
  const appwriteItemId = searchParams.id || accounts.data[0]?.appwriteItemId;
  const account = accounts.data.find((acc: any) => acc.appwriteItemId === appwriteItemId) || accounts.data[0];

  return (
    <section className="home">
      <div className="home-content">
        <header className="home-header">
          <HeaderBox
            type="greeting"
            title="Welcome"
            user={`${user.firstName} ${user.lastName}` || 'Guest'}
            subtext="Access and manage your account and transactions efficiently."
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
          showBalance={true}
        />
      </div>
      <RightSidebar
        user={user}
        transactions={[]}
        banks={accounts.data.slice(0, 2)} // Show up to 2 banks in sidebar
      />
    </section>
  );
}