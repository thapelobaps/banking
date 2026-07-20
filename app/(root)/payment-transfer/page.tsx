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
    <section className="payment-transfer">
      <div className="flex flex-col justify-between gap-5 lg:flex-row lg:items-end">
        <HeaderBox
          title="Transfer money"
          subtext="Simulate a secure transfer between Kape App demo accounts."
        />
        <span className="w-fit rounded-full border border-amber-200 bg-amber-50 px-3 py-1.5 text-xs font-semibold text-amber-800">
          Demo environment
        </span>
      </div>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_320px]">
        <section className="rounded-3xl border border-[#eadfd8] bg-white p-5 shadow-sm sm:p-7">
          {accountsData.length > 0 ? (
            <PaymentTransferForm accounts={accountsData} />
          ) : (
            <div className="rounded-2xl border border-dashed border-[#d8c8be] bg-[#fbf7f4] p-10 text-center text-sm text-[#7b675e]">
              Add a demo account before simulating a transfer.
            </div>
          )}
        </section>

        <aside className="space-y-4">
          <article className="rounded-3xl bg-[#4a2b20] p-6 text-white shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/55">How it works</p>
            <h2 className="mt-3 text-xl font-semibold">Safe demo transfers</h2>
            <p className="mt-3 text-sm leading-6 text-white/65">
              Kape updates SQL Server demo balances and creates matching debit and credit records. No real bank is contacted.
            </p>
          </article>
          <article className="rounded-3xl border border-[#eadfd8] bg-[#fbf7f4] p-6">
            <p className="text-sm font-semibold text-[#2b1a14]">Before you send</p>
            <ul className="mt-4 space-y-3 text-sm text-[#7b675e]">
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
