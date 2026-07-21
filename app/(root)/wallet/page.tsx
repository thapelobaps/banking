'use server';

import Link from 'next/link';
import { redirect } from 'next/navigation';
import {
  Activity,
  ArrowDownToLine,
  ArrowUpFromLine,
  Landmark,
  Scale,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import PaymentMethodPanel from '@/components/wallet/PaymentMethodPanel';
import WalletFundingPanel from '@/components/wallet/WalletFundingPanel';
import {
  getLedgerReconciliation,
  getLinkedAccounts,
  getPaymentMethods,
  getWallet,
  getWalletTransactions,
} from '@/lib/actions/wallet.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

const creditTypes = new Set(['top_up', 'transfer_in']);

export default async function WalletPage() {
  const user = await getLoggedInUser();
  if (!user) redirect('/sign-in');

  const [wallet, transactions, paymentMethods, linkedAccounts, reconciliation] = await Promise.all([
    getWallet(),
    getWalletTransactions(1, 20),
    getPaymentMethods(),
    getLinkedAccounts(),
    getLedgerReconciliation(),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page wallet-page">
        <HeaderBox title="Kape Wallet" subtext="Your wallet could not be loaded. Sign in again or confirm the API is running." />
        <div className="kape-empty-state">
          <strong>Wallet unavailable</strong>
          <span>Start the Kape API on port 5000 and refresh this page.</span>
        </div>
      </section>
    );
  }

  return (
    <section className="kape-page wallet-page">
      <header className="kape-page-header">
        <HeaderBox
          title="Kape Wallet"
          subtext="Fund, withdraw and track money through a SQL-backed double-entry wallet."
        />
        <div className="kape-page-actions">
          <Link href="/linked-banks" className="kape-button kape-button--secondary">
            Linked banks
          </Link>
          <Link href="/payment-transfer" className="kape-button kape-button--primary">
            Send money
          </Link>
        </div>
      </header>

      <section className="wallet-hero">
        <div className="wallet-hero__content">
          <span className="wallet-hero__eyebrow"><WalletCards size={16} /> Available wallet balance</span>
          <strong>{formatAmount(wallet.availableBalance)}</strong>
          <p>Kape wallet • {wallet.currency} • {wallet.status}</p>
        </div>
        <div className="wallet-hero__meta">
          <div>
            <span>Funding sources</span>
            <strong>{paymentMethods.length + linkedAccounts.length}</strong>
          </div>
          <div>
            <span>Wallet activity</span>
            <strong>{transactions.total}</strong>
          </div>
          <div>
            <span>Ledger status</span>
            <strong className={reconciliation?.isBalanced ? 'is-positive' : 'is-warning'}>
              {reconciliation?.isBalanced ? 'Balanced' : 'Review'}
            </strong>
          </div>
        </div>
      </section>

      <section className="wallet-metric-grid">
        <article>
          <div className="wallet-metric__icon"><ArrowDownToLine size={18} /></div>
          <span>Top-up ready</span>
          <strong>{paymentMethods.length ? 'Card enabled' : 'Add a card'}</strong>
          <small>{paymentMethods.length} tokenised payment method{paymentMethods.length === 1 ? '' : 's'}</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><Landmark size={18} /></div>
          <span>Connected money</span>
          <strong>{formatAmount(linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0))}</strong>
          <small>{linkedAccounts.length} linked bank account{linkedAccounts.length === 1 ? '' : 's'}</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><Scale size={18} /></div>
          <span>Ledger difference</span>
          <strong>{formatAmount(reconciliation?.difference ?? 0)}</strong>
          <small>{reconciliation?.isBalanced ? 'Double-entry books agree' : 'Reconciliation needs attention'}</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><ShieldCheck size={18} /></div>
          <span>Security model</span>
          <strong>Tokenised</strong>
          <small>No raw card number or CVV stored</small>
        </article>
      </section>

      <section className="wallet-workspace-grid">
        <WalletFundingPanel paymentMethods={paymentMethods} linkedAccounts={linkedAccounts} />
        <PaymentMethodPanel paymentMethods={paymentMethods} />
      </section>

      <section className="wallet-panel wallet-activity-panel">
        <div className="wallet-panel__heading wallet-panel__heading--row">
          <div>
            <span className="wallet-eyebrow">Immutable activity</span>
            <h2>Wallet transactions</h2>
            <p>Every completed operation is backed by balanced ledger entries.</p>
          </div>
          <Activity size={20} />
        </div>

        {transactions.items.length ? (
          <div className="wallet-transaction-list">
            {transactions.items.map((transaction) => {
              const isCredit = creditTypes.has(transaction.type);
              return (
                <article key={transaction.id} className="wallet-transaction-row">
                  <div className={`wallet-transaction-row__icon ${isCredit ? 'is-credit' : 'is-debit'}`}>
                    {isCredit ? <ArrowDownToLine size={17} /> : <ArrowUpFromLine size={17} />}
                  </div>
                  <div className="wallet-transaction-row__details">
                    <div>
                      <strong>{transaction.reference}</strong>
                      <span className={`wallet-status wallet-status--${transaction.status}`}>{transaction.status}</span>
                    </div>
                    <p>{transaction.type.replaceAll('_', ' ')} • {formatDateTime(transaction.createdAt).dateTime}</p>
                    {transaction.externalReference ? <small>{transaction.externalReference}</small> : null}
                  </div>
                  <div className="wallet-transaction-row__amount">
                    <strong className={isCredit ? 'is-credit' : 'is-debit'}>
                      {isCredit ? '+' : '-'}{formatAmount(transaction.netAmount)}
                    </strong>
                    {transaction.feeAmount > 0 ? <span>Fee {formatAmount(transaction.feeAmount)}</span> : null}
                  </div>
                </article>
              );
            })}
          </div>
        ) : (
          <div className="wallet-empty-inline">
            <Activity size={20} />
            <div>
              <strong>No wallet transactions yet</strong>
              <span>Add money to create the first ledger-backed wallet entry.</span>
            </div>
          </div>
        )}
      </section>
    </section>
  );
}
