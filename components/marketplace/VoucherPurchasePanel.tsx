'use client';

import Link from 'next/link';
import { Check, Clipboard, Loader2, ReceiptText, RefreshCw, ShieldCheck } from 'lucide-react';
import { useEffect, useMemo, useState, useTransition } from 'react';
import { useRouter } from 'next/navigation';

import MarketplaceBrandMark from '@/components/marketplace/MarketplaceBrandMark';
import {
  getVoucherDenominations,
  pollVoucherOrder,
  previewVoucherPurchase,
  submitVoucherPurchase,
} from '@/lib/actions/marketplace.actions';
import { formatAmount } from '@/lib/utils';
import type {
  MarketplaceQuote,
  VoucherCategory,
  VoucherDenomination,
  VoucherOrder,
  VoucherProduct,
} from '@/types/marketplace';

const activeFulfilmentStatuses = new Set(['pending', 'processing']);

export default function VoucherPurchasePanel({
  categories,
  products,
  walletAvailable,
}: {
  categories: VoucherCategory[];
  products: VoucherProduct[];
  walletAvailable: number;
}) {
  const router = useRouter();
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [selectedProductId, setSelectedProductId] = useState(products[0]?.id ?? '');
  const [denominations, setDenominations] = useState<VoucherDenomination[]>([]);
  const [selectedDenominationId, setSelectedDenominationId] = useState('');
  const [quote, setQuote] = useState<MarketplaceQuote | null>(null);
  const [order, setOrder] = useState<VoucherOrder | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [isPending, startTransition] = useTransition();

  const filteredProducts = useMemo(
    () => products.filter((product) => selectedCategory === 'all' || product.categoryId === selectedCategory),
    [products, selectedCategory]
  );
  const selectedProduct = products.find((product) => product.id === selectedProductId) ?? null;
  const selectedDenomination = denominations.find((item) => item.id === selectedDenominationId) ?? null;

  useEffect(() => {
    const firstVisible = filteredProducts[0];
    if (firstVisible && !filteredProducts.some((product) => product.id === selectedProductId)) {
      setSelectedProductId(firstVisible.id);
    }
  }, [filteredProducts, selectedProductId]);

  useEffect(() => {
    if (!selectedProductId) {
      setDenominations([]);
      setSelectedDenominationId('');
      return;
    }

    setQuote(null);
    setOrder(null);
    setError(null);
    startTransition(() => {
      void getVoucherDenominations(selectedProductId).then((result) => {
        if (!result.ok) {
          setDenominations([]);
          setSelectedDenominationId('');
          setError(result.error);
          return;
        }
        setDenominations(result.data);
        setSelectedDenominationId(result.data[0]?.id ?? '');
      });
    });
  }, [selectedProductId]);

  useEffect(() => {
    if (!order || !activeFulfilmentStatuses.has(order.status)) return;

    const timer = window.setInterval(() => {
      void pollVoucherOrder(order.id).then((result) => {
        if (!result.ok) return;
        setOrder(result.data);
        if (result.data.status === 'fulfilled') {
          setMessage('Your voucher is ready. The code is protected and shown only in your signed-in account.');
          router.refresh();
        }
      });
    }, 1500);

    return () => window.clearInterval(timer);
  }, [order, router]);

  const preview = () => {
    if (!selectedProduct || !selectedDenomination) return;
    setError(null);
    setMessage(null);
    startTransition(() => {
      void previewVoucherPurchase({
        voucherProductId: selectedProduct.id,
        voucherDenominationId: selectedDenomination.id,
      }).then((result) => {
        if (!result.ok) {
          setQuote(null);
          setError(result.error);
          return;
        }
        setQuote(result.data);
      });
    });
  };

  const purchase = () => {
    if (!selectedProduct || !selectedDenomination || !quote) return;
    setError(null);
    setMessage(null);
    startTransition(() => {
      void submitVoucherPurchase({
        voucherProductId: selectedProduct.id,
        voucherDenominationId: selectedDenomination.id,
        idempotencyKey: `voucher-ui-${crypto.randomUUID()}`,
      }).then((result) => {
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setOrder(result.data);
        setMessage('Payment completed. Kape is securely fulfilling the voucher now.');
        setQuote(null);
        router.refresh();
      });
    });
  };

  const copyVoucherCode = async () => {
    if (!order?.voucherCode) return;
    await navigator.clipboard.writeText(order.voucherCode);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  };

  return (
    <section className="marketplace-purchase-shell">
      <div className="marketplace-catalogue">
        <div className="marketplace-section-heading">
          <div>
            <span>Digital catalogue</span>
            <h2>Choose a voucher</h2>
            <p>Entertainment, gaming, shopping and transport credit fulfilled through the Kape provider queue.</p>
          </div>
          <span className="marketplace-wallet-pill">Available {formatAmount(walletAvailable)}</span>
        </div>

        <div className="marketplace-category-tabs" role="tablist" aria-label="Voucher categories">
          <button type="button" className={selectedCategory === 'all' ? 'is-active' : ''} onClick={() => setSelectedCategory('all')}>All</button>
          {categories.map((category) => (
            <button
              key={category.id}
              type="button"
              className={selectedCategory === category.id ? 'is-active' : ''}
              onClick={() => setSelectedCategory(category.id)}
            >
              {category.name}
            </button>
          ))}
        </div>

        <div className="marketplace-product-grid">
          {filteredProducts.map((product) => (
            <button
              key={product.id}
              type="button"
              className={`marketplace-product-card${selectedProductId === product.id ? ' is-selected' : ''}`}
              onClick={() => setSelectedProductId(product.id)}
            >
              <MarketplaceBrandMark name={product.brandName} />
              <span>{product.productName}</span>
              <small>{product.description}</small>
            </button>
          ))}
        </div>
      </div>

      <aside className="marketplace-checkout-card">
        <div className="marketplace-section-heading is-compact">
          <div>
            <span>Secure checkout</span>
            <h2>{selectedProduct?.productName ?? 'Select a voucher'}</h2>
          </div>
          <ShieldCheck size={21} />
        </div>

        {selectedProduct ? <MarketplaceBrandMark name={selectedProduct.brandName} compact /> : null}

        <label className="marketplace-field">
          <span>Voucher amount</span>
          <div className="marketplace-denominations">
            {denominations.map((denomination) => (
              <button
                key={denomination.id}
                type="button"
                className={selectedDenominationId === denomination.id ? 'is-active' : ''}
                onClick={() => {
                  setSelectedDenominationId(denomination.id);
                  setQuote(null);
                  setOrder(null);
                }}
              >
                {formatAmount(denomination.amount)}
              </button>
            ))}
          </div>
        </label>

        {quote ? (
          <div className="marketplace-quote">
            <span><small>Voucher</small><strong>{formatAmount(quote.amount)}</strong></span>
            <span><small>Fee</small><strong>{formatAmount(quote.feeAmount)}</strong></span>
            <span className="is-total"><small>Total debit</small><strong>{formatAmount(quote.totalAmount)}</strong></span>
          </div>
        ) : null}

        {order ? (
          <div className={`marketplace-fulfilment marketplace-fulfilment--${order.status}`}>
            <div>
              {order.status === 'fulfilled' ? <Check size={20} /> : <RefreshCw size={20} className="is-spinning" />}
              <span><strong>{order.status === 'fulfilled' ? 'Voucher ready' : 'Fulfilling voucher'}</strong><small>Order {order.id.slice(0, 8).toUpperCase()}</small></span>
            </div>
            {order.voucherCode ? (
              <button type="button" onClick={copyVoucherCode}>
                <span>{order.voucherCode}</span>
                {copied ? <Check size={16} /> : <Clipboard size={16} />}
              </button>
            ) : null}
            <Link href={`/receipts/voucher/${order.id}`}><ReceiptText size={16} /> View receipt</Link>
          </div>
        ) : null}

        {message ? <p className="marketplace-message is-success">{message}</p> : null}
        {error ? <p className="marketplace-message is-error">{error}</p> : null}

        <div className="marketplace-checkout-actions">
          <button type="button" className="is-secondary" disabled={isPending || !selectedDenomination} onClick={preview}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Preview
          </button>
          <button type="button" disabled={isPending || !quote} onClick={purchase}>
            {isPending ? <Loader2 size={17} className="is-spinning" /> : null} Buy voucher
          </button>
        </div>

        <p className="marketplace-fine-print">The wallet is debited once. Idempotency prevents duplicate purchases, and fulfilment runs through the durable SQL queue.</p>
      </aside>
    </section>
  );
}
