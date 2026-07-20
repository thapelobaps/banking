import HeaderBox from '@/components/HeaderBox';
import PaymentTransferForm from '@/components/PaymentTransferForm';
import { getAccounts } from '@/lib/actions/bank.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { redirect } from 'next/navigation';

const Transfer = async () => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const accounts = await getAccounts({ userId: loggedIn.userId });
  const accountsData = accounts?.data ?? [];

  return (
    <section className="kape-page">
      <div className="kape-page-header">
        <HeaderBox
          title="Transfer money"
          subtext="Simulate a secure transfer between Kape App demo accounts."
        />
        <span className="kape-demo-pill">Demo environment</span>
      </div>

      <div className="kape-transfer-grid">
        <section className="kape-form-card">
          {accountsData.length > 0 ? (
            <PaymentTransferForm accounts={accountsData} />
          ) : (
            <div className="kape-empty-state">
              <strong>No demo account available</strong>
              <span>Add a demo account before simulating a transfer.</span>
            </div>
          )}
        </section>

        <aside className="kape-transfer-help">
          <article className="is-primary">
            <span>How it works</span>
            <h2>Safe demo transfers</h2>
            <p>
              Kape updates SQL Server demo balances and creates matching debit and credit records. No real bank is contacted.
            </p>
          </article>
          <article>
            <h3>Before you send</h3>
            <ul>
              <li>Use a different recipient account.</li>
              <li>Check the recipient demo reference.</li>
              <li>Confirm the amount in South African rand.</li>
            </ul>
          </article>
        </aside>
      </div>
    </section>
  );
};

export default Transfer;
