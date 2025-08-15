// app/(root)/my-banks/page.tsx
'use server';
import { getAccounts } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import BankCard from '@/components/BankCard';
import HeaderBox from '@/components/HeaderBox';
import { redirect } from 'next/navigation';
import { Account } from '@/types';
import React from 'react';

const MyBanks = async () => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) {
    redirect('/sign-in');
  }

  const accounts = await getAccounts({ userId: loggedIn.userId });
  if (!accounts) {
    return (
      <section className="flex">
        <div className="my-banks">
          <HeaderBox title="My Bank Accounts" subtext="Effortlessly manage your banking activities." />
          <p>No bank accounts found. Add a bank to get started.</p>
        </div>
      </section>
    );
  }

  return (
    <section className="flex">
      <div className="my-banks">
        <HeaderBox title="My Bank Accounts" subtext="Effortlessly manage your banking activities." />
        <div className="space-y-4">
          <h2 className="header-2">Your cards</h2>
          <div className="flex flex-wrap gap-6">
            {accounts.data.map((a: Account) => (
              <BankCard key={a.appwriteItemId} account={a} userName={loggedIn.firstName} />
            ))}
          </div>
        </div>
      </div>
    </section>
  );
};

export default MyBanks;