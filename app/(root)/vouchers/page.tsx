import Link from 'next/link';
import { redirect } from 'next/navigation';
import { CheckCircle2, Clock3, ReceiptText } from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import KapePayVoucherPanel from '@/components/marketplace/KapePayVoucherPanel';
import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import {
  getVoucherCategories,
  getVoucherOrders,
  getVoucherProducts,
} from '@/lib/actions/marketplace.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { getLinkedAccounts, getWallet } from '@/lib/actions/wallet.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

export const dynamic = 'force-dynamic';

export default async function VouchersPage() {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [wallet, linkedAccounts, categories, productsPage, ordersPage] = await Promise.all([
    getWallet(),
    getLinkedAccounts(),
    getVoucherCategories(),
    getVoucherProducts(null, 1, 100),
    getVoucherOrders(1, 12),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page marketplace-page">
        <HeaderBox title="Vouchers" subtext="Your Kape Wallet could not be loaded." />
        <div className="kape-empty-state"><strong>Marketplace unavailable</strong><span>Confirm that the API is running and sign in again.</span></div>
      </section>
    );
  }

  const productMap = new Map(productsPage.items.map((product) => [product.id, product]));

  return (
    <section className="kape-page marketplace-page">
      <header className="kape-page-header">
        <HeaderBox title="Digital vouchers" subtext="Pay directly from a linked sandbox bank or use synthetic Kape Wallet funds." />
        <Link className="marketplace-header-link" href="/marketplace">Marketplace home</Link>
      </header>

      <div className="marketplace-message is-success" role="note">
        Demonstration environment — linked-bank payments use provider orchestration and never deposit money into the Kape Wallet.
      </div>

      <KapePayVoucherPanel
        categories={categories}
        products={productsPage.items}
        walletAvailable={wallet.availableBalance}
        linkedAccounts={linkedAccounts}
      />

      <section className="marketplace-orders-section">
        <div className="marketplace-section-heading">
          <div>
            <span>Fulfilment history</span>
            <h2>Your voucher orders</h2>
            <p>Codes appear only after payment completion and durable provider fulfilment.</p>
          </div>
          <span className="marketplace-wallet-pill">{ordersPage.total} orders</span>
        </div>

        <div className="marketplace-order-grid">
          {ordersPage.items.length ? ordersPage.items.map((order) => {
            const product = productMap.get(order.voucherProductId);
            const date = formatDateTime(order.createdAt);
            return (
              <article key={order.id} className={`marketplace-order-card marketplace-order-card--${order.status}`}>
                <div>
                  <MarketplaceBrandMark name={product?.brandName ?? 'Voucher'} compact />
                  <span className={`wallet-status wallet-status--${order.status}`}>{order.status}</span>
                </div>
                <h3>{product?.productName ?? 'Digital voucher'}</h3>
                <strong>{formatAmount(order.amount)}</strong>
                <p>{date.dateOnly} · {date.timeOnly}</p>
                <div className="marketplace-order-card__footer">
                  <span>{order.status === 'fulfilled' ? <CheckCircle2 size={15} /> : <Clock3 size={15} />} {order.status === 'fulfilled' ? 'Ready' : 'Payment or provider queue'}</span>
                  <Link href={`/receipts/voucher/${order.id}`}><ReceiptText size={15} /> Receipt</Link>
                </div>
              </article>
            );
          }) : (
            <div className="kape-empty-state"><strong>No voucher orders yet</strong><span>Your completed and processing orders will appear here.</span></div>
          )}
        </div>
      </section>
    </section>
  );
}
