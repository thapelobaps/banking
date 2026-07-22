import Link from 'next/link';
import { redirect } from 'next/navigation';
import { CheckCircle2, Clock3, ReceiptText } from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import PrepaidPurchasePanel from '@/components/marketplace/PrepaidPurchasePanel';
import {
  getPrepaidOperators,
  getPrepaidOrders,
  getPrepaidProducts,
} from '@/lib/actions/marketplace.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { getWallet } from '@/lib/actions/wallet.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

export const dynamic = 'force-dynamic';

export default async function PrepaidPage() {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [wallet, operators, products, ordersPage] = await Promise.all([
    getWallet(),
    getPrepaidOperators(),
    getPrepaidProducts(),
    getPrepaidOrders(1, 12),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page marketplace-page">
        <HeaderBox title="Prepaid" subtext="Your Kape Wallet could not be loaded." />
        <div className="kape-empty-state"><strong>Prepaid services unavailable</strong><span>Confirm that the API is running and sign in again.</span></div>
      </section>
    );
  }

  const productMap = new Map(products.map((product) => [product.id, product]));
  const operatorMap = new Map(operators.map((operator) => [operator.id, operator]));

  return (
    <section className="kape-page marketplace-page">
      <header className="kape-page-header">
        <HeaderBox title="Prepaid services" subtext="Buy airtime and prepaid electricity with recipient validation and durable fulfilment." />
        <Link className="marketplace-header-link" href="/marketplace">Marketplace home</Link>
      </header>

      <PrepaidPurchasePanel operators={operators} products={products} walletAvailable={wallet.availableBalance} />

      <section className="marketplace-orders-section">
        <div className="marketplace-section-heading">
          <div>
            <span>Purchase history</span>
            <h2>Your prepaid orders</h2>
            <p>Provider tokens and references become available when fulfilment completes.</p>
          </div>
          <span className="marketplace-wallet-pill">{ordersPage.total} orders</span>
        </div>

        <div className="marketplace-order-grid">
          {ordersPage.items.length ? ordersPage.items.map((order) => {
            const product = productMap.get(order.productId);
            const operator = product ? operatorMap.get(product.operatorId) : null;
            const date = formatDateTime(order.createdAt);
            return (
              <article key={order.id} className={`marketplace-order-card marketplace-order-card--${order.status}`}>
                <div>
                  <MarketplaceBrandMark name={operator?.name ?? product?.name ?? 'Prepaid'} compact />
                  <span className={`wallet-status wallet-status--${order.status}`}>{order.status}</span>
                </div>
                <h3>{product?.name ?? 'Prepaid purchase'}</h3>
                <strong>{formatAmount(order.amount)}</strong>
                <p>{order.recipient} · {date.dateOnly}</p>
                <div className="marketplace-order-card__footer">
                  <span>{order.status === 'fulfilled' ? <CheckCircle2 size={15} /> : <Clock3 size={15} />} {order.status === 'fulfilled' ? 'Ready' : 'Provider queue'}</span>
                  <Link href={`/receipts/prepaid/${order.id}`}><ReceiptText size={15} /> Receipt</Link>
                </div>
              </article>
            );
          }) : (
            <div className="kape-empty-state"><strong>No prepaid orders yet</strong><span>Your completed and processing purchases will appear here.</span></div>
          )}
        </div>
      </section>
    </section>
  );
}
