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
    <section className="kape-page">
      <div className="kape-page-header">
        <HeaderBox
          title="Accounts"
          subtext="Manage and review your SQL-backed South African demo accounts."
        />
        <div className="kape-balance-pill">
          <span>Available across accounts</span>
          <strong>{formatAmount(availableBalance)}</strong>
        </div>
      </div>

      {accountList.length > 0 ? (
        <>
          <section className="kape-account-metrics">
            <article>
              <span>Total accounts</span>
              <strong>{accountList.length}</strong>
            </article>
            <article>
              <span>Account type</span>
              <strong className="capitalize">{accountList[0]?.subtype ?? 'transaction'}</strong>
            </article>
            <article className="is-primary">
              <span>Environment</span>
              <strong>Kape demo banking</strong>
            </article>
          </section>

          <section>
            <div className="kape-section-heading">
              <div>
                <h2>Your accounts</h2>
                <p>Open an account to view its transaction history.</p>
              </div>
            </div>
            <div className="kape-account-grid">
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
        <div className="kape-empty-state">
          <strong>No demo accounts are available</strong>
          <span>Accounts created through the API will appear here.</span>
        </div>
      )}
    </section>
  );
};

export default MyBanks;
