import Link from 'next/link';
import { redirect } from 'next/navigation';
import { ArrowRight, Gift, HandCoins, ReceiptText, ShieldCheck, Smartphone } from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import {
  getPaymentRequests,
  getPrepaidOrders,
  getPrepaidProducts,
  getVoucherOrders,
  getVoucherProducts,
} from '@/lib/actions/marketplace.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { getWallet } from '@/lib/actions/wallet.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

export const dynamic = 'force-dynamic';

export default async function MarketplacePage() {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [wallet, voucherProducts, voucherOrders, prepaidProducts, prepaidOrders, paymentRequests] = await Promise.all([
    getWallet(),
    getVoucherProducts(null, 1, 100),
    getVoucherOrders(1, 8),
    getPrepaidProducts(),
    getPrepaidOrders(1, 8),
    getPaymentRequests(),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page marketplace-page">
        <HeaderBox title="Marketplace" subtext="Your Kape Wallet could not be loaded." />
        <div className="kape-empty-state"><strong>Marketplace unavailable</strong><span>Confirm that the API is running and sign in again.</span></div>
      </section>
    );
  }

  const voucherMap = new Map(voucherProducts.items.map((product) => [product.id, product]));
  const prepaidMap = new Map(prepaidProducts.map((product) => [product.id, product]));
  const pendingFulfilment = [...voucherOrders.items, ...prepaidOrders.items].filter((order) => order.status !== 'fulfilled').length;
  const incomingRequests = paymentRequests.filter((request) => request.payeeUserId !== loggedIn.userId && request.status === 'pending').length;

  const recentOrders = [
    ...voucherOrders.items.map((order) => ({
      id: order.id,
      kind: 'voucher' as const,
      title: voucherMap.get(order.voucherProductId)?.productName ?? 'Digital voucher',
      brand: voucherMap.get(order.voucherProductId)?.brandName ?? 'Voucher',
      amount: order.amount,
      status: order.status,
      createdAt: order.createdAt,
      receipt: `/receipts/voucher/${order.id}`,
    })),
    ...prepaidOrders.items.map((order) => ({
      id: order.id,
      kind: 'prepaid' as const,
      title: prepaidMap.get(order.productId)?.name ?? 'Prepaid purchase',
      brand: prepaidMap.get(order.productId)?.productType ?? 'Prepaid',
      amount: order.amount,
      status: order.status,
      createdAt: order.createdAt,
      receipt: `/receipts/prepaid/${order.id}`,
    })),
  ]
    .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
    .slice(0, 6);

  return (
    <section className="kape-page marketplace-page">
      <header className="kape-page-header">
        <HeaderBox title="Kape Marketplace" subtext="Vouchers, prepaid services and payment requests, settled from one protected wallet." />
        <Link className="marketplace-header-link" href="/wallet">Open wallet</Link>
      </header>

      <section className="marketplace-hero">
        <div>
          <span>Spend from Kape Wallet</span>
          <h1>Everyday digital payments without leaving your money hub.</h1>
          <p>Preview every debit, prevent duplicate purchases and track provider fulfilment through the SQL-backed queue.</p>
          <div className="marketplace-hero__actions">
            <Link href="/vouchers"><Gift size={17} /> Browse vouchers</Link>
            <Link href="/prepaid" className="is-secondary"><Smartphone size={17} /> Buy prepaid</Link>
          </div>
        </div>
        <div className="marketplace-hero__balance">
          <span>Available wallet balance</span>
          <strong>{formatAmount(wallet.availableBalance)}</strong>
          <small>{wallet.status} · ZAR · double-entry ledger</small>
          <div><ShieldCheck size={18} /> Idempotent checkout and durable fulfilment</div>
        </div>
      </section>

      <section className="marketplace-service-grid">
        <Link href="/vouchers" className="marketplace-service-card is-voucher">
          <span><Gift size={24} /></span>
          <div><small>Digital catalogue</small><h2>Vouchers</h2><p>Netflix, Spotify, gaming, shopping and transport credit.</p></div>
          <strong>{voucherProducts.total} products <ArrowRight size={17} /></strong>
        </Link>
        <Link href="/prepaid" className="marketplace-service-card is-prepaid">
          <span><Smartphone size={24} /></span>
          <div><small>Everyday services</small><h2>Prepaid</h2><p>Airtime and electricity with recipient validation before debit.</p></div>
          <strong>{prepaidProducts.length} products <ArrowRight size={17} /></strong>
        </Link>
        <Link href="/payment-requests" className="marketplace-service-card is-request">
          <span><HandCoins size={24} /></span>
          <div><small>Money conversations</small><h2>Payment requests</h2><p>Request, pay or decline securely through the Kape Wallet.</p></div>
          <strong>{incomingRequests} need attention <ArrowRight size={17} /></strong>
        </Link>
      </section>

      <section className="marketplace-insight-grid">
        <article><small>Provider catalogue</small><strong>{voucherProducts.total + prepaidProducts.length}</strong><span>Available digital products</span></article>
        <article><small>Fulfilment queue</small><strong>{pendingFulfilment}</strong><span>Pending or processing orders</span></article>
        <article><small>Request centre</small><strong>{paymentRequests.length}</strong><span>Incoming and outgoing requests</span></article>
        <article><small>Wallet protection</small><strong>Active</strong><span>Quote and idempotency checks</span></article>
      </section>

      <section className="marketplace-orders-section">
        <div className="marketplace-section-heading">
          <div>
            <span>Recent fulfilment</span>
            <h2>Orders and receipts</h2>
            <p>Track purchases from wallet debit through provider completion.</p>
          </div>
          <Link className="marketplace-text-link" href="/transaction-history">View wallet activity <ArrowRight size={15} /></Link>
        </div>

        <div className="marketplace-activity-list">
          {recentOrders.length ? recentOrders.map((order) => {
            const date = formatDateTime(order.createdAt);
            return (
              <article key={`${order.kind}-${order.id}`}>
                <MarketplaceBrandMark name={order.brand} compact />
                <div><strong>{order.title}</strong><span>{date.dateOnly} · {date.timeOnly}</span></div>
                <span className={`wallet-status wallet-status--${order.status}`}>{order.status}</span>
                <strong>{formatAmount(order.amount)}</strong>
                <Link href={order.receipt}><ReceiptText size={16} /> Receipt</Link>
              </article>
            );
          }) : (
            <div className="kape-empty-state"><strong>No marketplace orders yet</strong><span>Buy a voucher or prepaid service to see fulfilment here.</span></div>
          )}
        </div>
      </section>
    </section>
  );
}
