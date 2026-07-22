import Link from 'next/link';
import { notFound, redirect } from 'next/navigation';
import { CheckCircle2, Clock3, ShieldCheck } from 'lucide-react';

import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import ReceiptActions from '@/components/marketplace/ReceiptActions';
import {
  getPrepaidOperators,
  getPrepaidOrder,
  getPrepaidProducts,
} from '@/lib/actions/marketplace.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

export const dynamic = 'force-dynamic';

export default async function PrepaidReceiptPage({ params }: { params: { id: string } }) {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [order, products, operators] = await Promise.all([
    getPrepaidOrder(params.id),
    getPrepaidProducts(),
    getPrepaidOperators(),
  ]);
  if (!order) notFound();

  const product = products.find((item) => item.id === order.productId) ?? null;
  const operator = product ? operators.find((item) => item.id === product.operatorId) ?? null : null;
  const created = formatDateTime(order.createdAt);
  const fulfilled = order.fulfilledAt ? formatDateTime(order.fulfilledAt) : null;
  const reference = order.externalOrderId || order.id;

  return (
    <section className="marketplace-receipt-page">
      <div className="marketplace-receipt">
        <header>
          <div><span>KAPE</span><small>Prepaid services receipt</small></div>
          <span className={`wallet-status wallet-status--${order.status}`}>{order.status}</span>
        </header>

        <div className="marketplace-receipt__hero">
          <MarketplaceBrandMark name={operator?.name ?? product?.name ?? 'Prepaid'} />
          <h1>{product?.name ?? 'Prepaid purchase'}</h1>
          <p>{product?.productType === 'electricity' ? 'Prepaid electricity token purchase.' : 'South African mobile prepaid purchase.'}</p>
          <strong>{formatAmount(order.amount + order.feeAmount)}</strong>
        </div>

        <div className="marketplace-receipt__rows">
          <span><small>Purchase amount</small><strong>{formatAmount(order.amount)}</strong></span>
          <span><small>Fee</small><strong>{formatAmount(order.feeAmount)}</strong></span>
          <span><small>Total wallet debit</small><strong>{formatAmount(order.amount + order.feeAmount)}</strong></span>
          <span><small>Recipient</small><strong>{order.recipient}</strong></span>
          <span><small>Ordered</small><strong>{created.dateOnly} · {created.timeOnly}</strong></span>
          <span><small>Fulfilled</small><strong>{fulfilled ? `${fulfilled.dateOnly} · ${fulfilled.timeOnly}` : 'Processing'}</strong></span>
          <span><small>Order reference</small><strong>{reference}</strong></span>
        </div>

        {order.fulfilmentReference ? (
          <div className="marketplace-receipt__token">
            <span>{product?.productType === 'electricity' ? 'Electricity token' : 'Provider reference'}</span>
            <strong>{order.fulfilmentReference}</strong>
            <small>Keep this token or provider reference safe until it has been used.</small>
          </div>
        ) : (
          <div className="marketplace-receipt__processing">
            <Clock3 size={20} />
            <span><strong>Provider fulfilment in progress</strong><small>Refresh this receipt after a moment to reveal the fulfilment reference.</small></span>
          </div>
        )}

        <div className="marketplace-receipt__assurance">
          {order.status === 'fulfilled' ? <CheckCircle2 size={20} /> : <ShieldCheck size={20} />}
          <span><strong>Validated and ledger-backed</strong><small>The recipient was validated before Kape settled this purchase once.</small></span>
        </div>

        <ReceiptActions reference={reference} />

        <footer>
          <Link href="/prepaid">Back to prepaid</Link>
          <Link href="/marketplace">Marketplace home</Link>
        </footer>
      </div>
    </section>
  );
}
