import Link from 'next/link';
import { redirect } from 'next/navigation';

import HeaderBox from '@/components/HeaderBox';
import PaymentRequestsPanel from '@/components/marketplace/PaymentRequestsPanel';
import { getPaymentRequests } from '@/lib/actions/marketplace.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { getWallet } from '@/lib/actions/wallet.actions';

export const dynamic = 'force-dynamic';

export default async function PaymentRequestsPage() {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [wallet, requests] = await Promise.all([
    getWallet(),
    getPaymentRequests(),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page marketplace-page">
        <HeaderBox title="Payment requests" subtext="Your Kape Wallet could not be loaded." />
        <div className="kape-empty-state"><strong>Requests unavailable</strong><span>Confirm that the API is running and sign in again.</span></div>
      </section>
    );
  }

  return (
    <section className="kape-page marketplace-page">
      <header className="kape-page-header">
        <HeaderBox title="Payment requests" subtext="Request money, respond to incoming requests and settle them through the Kape Wallet." />
        <Link className="marketplace-header-link" href="/marketplace">Marketplace home</Link>
      </header>

      <PaymentRequestsPanel
        currentUserId={loggedIn.userId}
        initialRequests={requests}
        walletAvailable={wallet.availableBalance}
      />
    </section>
  );
}
