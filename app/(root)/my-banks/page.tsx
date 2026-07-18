'use server';

import { getAccounts } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import BankCard from '@/components/BankCard';
import HeaderBox from '@/components/HeaderBox';
import { redirect } from 'next/navigation';
import { Account } from '@/types';

const MyBanks = async () => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) {
    redirect('/sign-in');
  }

  const accounts = await getAccounts({ userId: loggedIn.userId });
  if (!accounts?.data.length) {
    return (
      <section className="flex">
        <div className="my-banks">
          <HeaderBox title="Demo Accounts" subtext="Your South African demo bank accounts." />
          <p>No demo accounts are available.</p>
        </div>
      </section>
    );
  }

  return (
    <section className="flex">
      <div className="my-banks">
        <HeaderBox
          title="Demo Accounts"
          subtext="Review SQL-backed demo accounts. No live bank connection is active."
        />
        <div className="space-y-4">
          <h2 className="header-2">Your accounts</h2>
          <div className="flex flex-wrap gap-6">
            {accounts.data.map((account: Account) => (
              <BankCard key={account.id} account={account} userName={loggedIn.firstName} />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
};

export default MyBanks;
