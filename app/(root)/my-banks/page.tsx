'use server';

import { redirect } from 'next/navigation';
import { getAccounts } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import BankCard from '@/components/BankCard';
import HeaderBox from '@/components/HeaderBox';
import { formatAmount } from '@/lib/utils';
import { Account } from '@/types';

const MyBanks = async () => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const accounts = await getAccounts({ userId: loggedIn.userId });
  const accountList = accounts?.data ?? [];
  const availableBalance = accountList.reduce((sum, account) => sum + account.availableBalance, 0);

  return (
    <section className="my-banks">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-end">
        <HeaderBox
          title="Accounts"
          subtext="Manage and review your SQL-backed South African demo accounts."
        />
        <div className="rounded-2xl border border-[#eadfd8] bg-white px-5 py-3 shadow-sm">
          <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-[#9a8378]">Available across accounts</p>
          <p className="mt-1 text-xl font-semibold text-[#2b1a14] tabular-nums">{formatAmount(availableBalance)}</p>
        </div>
      </div>

      {accountList.length > 0 ? (
        <>
          <section className="grid gap-4 sm:grid-cols-3">
            <article className="rounded-3xl border border-[#eadfd8] bg-white p-5 shadow-sm">
              <p className="text-sm text-[#8a756b]">Total accounts</p>
              <p className="mt-3 text-3xl font-semibold text-[#2b1a14]">{accountList.length}</p>
            </article>
            <article className="rounded-3xl border border-[#eadfd8] bg-white p-5 shadow-sm">
              <p className="text-sm text-[#8a756b]">Account type</p>
              <p className="mt-3 text-lg font-semibold capitalize text-[#2b1a14]">{accountList[0]?.subtype ?? 'transaction'}</p>
            </article>
            <article className="rounded-3xl border border-[#eadfd8] bg-[#4a2b20] p-5 text-white shadow-sm">
              <p className="text-sm text-white/65">Environment</p>
              <p className="mt-3 text-lg font-semibold">Kape demo banking</p>
            </article>
          </section>

          <section>
            <div className="mb-5">
              <h2 className="text-lg font-semibold text-[#2b1a14]">Your accounts</h2>
              <p className="mt-1 text-sm text-[#8a756b]">Open an account to view its transaction history.</p>
            </div>
            <div className="grid gap-7 xl:grid-cols-2 2xl:grid-cols-3">
              {accountList.map((account: Account) => (
                <BankCard
                  key={account.id}
                  account={account}
                  userName={`${loggedIn.firstName} ${loggedIn.lastName}`}
                />
              ))}
            </div>
          </section>
        </>
      ) : (
        <div className="rounded-3xl border border-dashed border-[#d8c8be] bg-white p-12 text-center">
          <p className="font-semibold text-[#2b1a14]">No demo accounts are available</p>
          <p className="mt-2 text-sm text-[#8a756b]">Accounts created through the API will appear here.</p>
        </div>
      )}
    </section>
  );
};

export default MyBanks;
