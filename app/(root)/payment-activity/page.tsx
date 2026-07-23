import Link from 'next/link';
import { redirect } from 'next/navigation';

import HeaderBox from '@/components/HeaderBox';
import PaymentActivityPanel from '@/components/payments/PaymentActivityPanel';
import { getKapePayPayments } from '@/lib/actions/kape-pay.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';

export const dynamic = 'force-dynamic';

export default async function PaymentActivityPage() {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const payments = await getKapePayPayments(1, 100);

  return (
    <section className="kape-page marketplace-page">
      <header className="kape-page-header">
        <HeaderBox
          title="Kape Pay"
          subtext="Track direct linked-bank payments and synthetic wallet purchases without mixing their balances."
        />
        <div className="kape-page-actions">
          <Link href="/vouchers" className="kape-button kape-button--secondary">Buy voucher</Link>
          <Link href="/prepaid" className="kape-button kape-button--primary">Buy prepaid</Link>
        </div>
      </header>

      <div className="marketplace-message is-success" role="note">
        Demonstration environment — no real funds are held or transferred. Provider references, payment states and ledger operations are production-shaped simulations.
      </div>

      <PaymentActivityPanel initialPayments={payments.items} />
    </section>
  );
}
