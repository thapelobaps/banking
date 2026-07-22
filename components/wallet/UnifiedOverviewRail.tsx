'use client';

import Link from 'next/link';
import {
  Activity,
  CreditCard,
  Landmark,
  ReceiptText,
  Scale,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';

import { formatAmount } from '@/lib/utils';
import type { User } from '@/types';
import type {
  LedgerReconciliation,
  LinkedBankAccount,
  PaymentMethod,
  WalletSummary,
  WalletTransaction,
} from '@/types/wallet';

type UnifiedOverviewRailProps = {
  user: User;
  wallet: WalletSummary;
  paymentMethods: PaymentMethod[];
  linkedAccounts: LinkedBankAccount[];
  walletTransactions: WalletTransaction[];
  reconciliation: LedgerReconciliation | null;
  debitOrderCount: number;
};

const creditTypes = new Set(['top_up', 'transfer_in', 'refund']);

export default function UnifiedOverviewRail({
  user,
  wallet,
  paymentMethods,
  linkedAccounts,
  walletTransactions,
  reconciliation,
  debitOrderCount,
}: UnifiedOverviewRailProps) {
  const moneyIn = walletTransactions
    .filter((transaction) => creditTypes.has(transaction.type))
    .reduce((sum, transaction) => sum + transaction.netAmount, 0);
  const moneyOut = walletTransactions
    .filter((transaction) => !creditTypes.has(transaction.type))
    .reduce((sum, transaction) => sum + transaction.netAmount, 0);
  const linkedAvailable = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const defaultCard = paymentMethods.find((method) => method.isDefault) ?? paymentMethods[0];

  const cards = [
    {
      id: `wallet-${wallet.id}`,
      eyebrow: 'Kape wallet',
      title: 'Available balance',
      balance: wallet.availableBalance,
      meta: `${wallet.currency} • ${wallet.status}`,
      footer: `${walletTransactions.length} recent wallet entries`,
      className: 'is-wallet',
      icon: <WalletCards size={17} />,
    },
    ...linkedAccounts.map((account) => ({
      id: `linked-${account.id}`,
      eyebrow: account.institutionName,
      title: account.accountName,
      balance: account.availableBalance,
      meta: `${account.accountType} •••• ${account.accountNumberMask}`,
      footer: 'Provider-reported available balance',
      className: 'is-linked',
      icon: <Landmark size={17} />,
    })),
  ];

  return (
    <aside className="kape-rail unified-overview-rail">
      <section className="kape-rail__profile">
        <div className="kape-rail__avatar" aria-hidden="true">
          {user.firstName[0]}{user.lastName[0]}
        </div>
        <div className="kape-rail__identity">
          <strong>{user.firstName} {user.lastName}</strong>
          <span>{user.email}</span>
        </div>
      </section>

      <section className="kape-rail__workspace">
        <div>
          <span>Workspace</span>
          <strong>Unified wallet</strong>
        </div>
        <span className="kape-rail__status" aria-label="Wallet platform active" />
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Your money cards</h2>
            <p>Wallet and connected balances</p>
          </div>
          <Link href="/linked-banks">Manage</Link>
        </div>

        <div className="unified-card-carousel" aria-label="Wallet and linked account cards">
          {cards.map((card) => (
            <article key={card.id} className={`unified-money-card ${card.className}`}>
              <div className="unified-money-card__top">
                <span>{card.icon}{card.eyebrow}</span>
                <small>Active</small>
              </div>
              <div>
                <span className="unified-money-card__label">{card.title}</span>
                <strong>{formatAmount(card.balance)}</strong>
              </div>
              <div className="unified-money-card__bottom">
                <span>{card.meta}</span>
                <small>{card.footer}</small>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Funding card</h2>
            <p>Tokenised payment method</p>
          </div>
          <Link href="/wallet">Wallet</Link>
        </div>

        <div className="unified-funding-card">
          <div className="unified-funding-card__icon"><CreditCard size={18} /></div>
          <div>
            <strong>{defaultCard ? defaultCard.bankName : 'No card added'}</strong>
            <span>{defaultCard ? `${defaultCard.brand} •••• ${defaultCard.last4}` : 'Add a tokenised card'}</span>
          </div>
          {defaultCard ? <small>Default</small> : null}
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Money intelligence</h2>
            <p>Across wallet activity</p>
          </div>
        </div>

        <div className="unified-rail-metrics">
          <article>
            <span>Visible available</span>
            <strong>{formatAmount(wallet.availableBalance + linkedAvailable)}</strong>
            <small>{linkedAccounts.length + 1} money sources</small>
          </article>
          <div>
            <article className="is-positive">
              <span>Money in</span>
              <strong>{formatAmount(moneyIn)}</strong>
            </article>
            <article className="is-negative">
              <span>Money out</span>
              <strong>{formatAmount(moneyOut)}</strong>
            </article>
          </div>
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Platform controls</h2>
            <p>Live wallet health</p>
          </div>
        </div>

        <div className="unified-control-list">
          <div><span><Scale size={15} /> Ledger</span><strong>{reconciliation?.isBalanced ? 'Balanced' : 'Review'}</strong></div>
          <div><span><ReceiptText size={15} /> Debit orders</span><strong>{debitOrderCount}</strong></div>
          <div><span><Activity size={15} /> Wallet activity</span><strong>{walletTransactions.length}</strong></div>
          <div><span><ShieldCheck size={15} /> Card security</span><strong>Tokenised</strong></div>
        </div>
      </section>
    </aside>
  );
}
