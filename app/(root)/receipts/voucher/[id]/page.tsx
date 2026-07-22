import Link from 'next/link';
import { notFound, redirect } from 'next/navigation';
import { CheckCircle2, Clock3, ShieldCheck } from 'lucide-react';

import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import ReceiptActions from '@/components/marketplace/ReceiptActions';
import { getVoucherOrder, getVoucherProduct } from '@/lib/actions/marketplace.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

export const dynamic = 'force-dynamic';

export default async function VoucherReceiptPage({ params }: { params: { id: string } }) {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const order = await getVoucherOrder(params.id);
  if (!order) notFound();
  const product = await getVoucherProduct(order.voucherProductId);
  const created = formatDateTime(order.createdAt);
  const fulfilled = order.fulfilledAt ? formatDateTime(order.fulfilledAt) : null;
  const reference = order.externalOrderId || order.id;

  return (
    <section className="marketplace-receipt-page">
      <div className="marketplace-receipt">
        <header>
          <div><span>KAPE</span><small>Digital marketplace receipt</small></div>
          <span className={`wallet-status wallet-status--${order.status}`}>{order.status}</span>
        </header>

        <div className="marketplace-receipt__hero">
          <MarketplaceBrandMark name={product?.brandName ?? 'Voucher'} />
          <h1>{product?.productName ?? 'Digital voucher'}</h1>
          <p>{product?.description ?? 'Secure digital voucher purchase.'}</p>
          <strong>{formatAmount(order.amount + order.feeAmount)}</strong>
        </div>

        <div className="marketplace-receipt__rows">
          <span><small>Voucher amount</small><strong>{formatAmount(order.amount)}</strong></span>
          <span><small>Fee</small><strong>{formatAmount(order.feeAmount)}</strong></span>
          <span><small>Total wallet debit</small><strong>{formatAmount(order.amount + order.feeAmount)}</strong></span>
          <span><small>Ordered</small><strong>{created.dateOnly} · {created.timeOnly}</strong></span>
          <span><small>Fulfilled</small><strong>{fulfilled ? `${fulfilled.dateOnly} · ${fulfilled.timeOnly}` : 'Processing'}</strong></span>
          <span><small>Order reference</small><strong>{reference}</strong></span>
        </div>

        {order.voucherCode ? (
          <div className="marketplace-receipt__token">
            <span>Voucher code</span>
            <strong>{order.voucherCode}</strong>
            <small>Keep this code private. It is shown only inside your authenticated Kape account.</small>
          </div>
        ) : (
          <div className="marketplace-receipt__processing">
            <Clock3 size={20} />
            <span><strong>Provider fulfilment in progress</strong><small>Refresh this receipt after a moment to reveal the protected voucher code.</small></span>
          </div>
        )}

        <div className="marketplace-receipt__assurance">
          {order.status === 'fulfilled' ? <CheckCircle2 size={20} /> : <ShieldCheck size={20} />}
          <span><strong>Ledger-backed payment</strong><small>This purchase was settled once through Kape&apos;s idempotent double-entry wallet.</small></span>
        </div>

        <ReceiptActions reference={reference} />

        <footer>
          <Link href="/vouchers">Back to vouchers</Link>
          <Link href="/marketplace">Marketplace home</Link>
        </footer>
      </div>
    </section>
  );
}
