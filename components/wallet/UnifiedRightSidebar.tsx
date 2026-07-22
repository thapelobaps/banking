'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import {
  ArrowDownLeft,
  ArrowUpRight,
  ChevronLeft,
  ChevronRight,
  CreditCard,
  Landmark,
  ReceiptText,
  Scale,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';

import { formatAmount, formatDateTime } from '@/lib/utils';
import type { User } from '@/types';
import type { LinkedBankAccount, PaymentMethod } from '@/types/wallet';

export type UnifiedRailActivity = {
  id: string;
  title: string;
  detail: string;
  amount: number;
  direction: 'credit' | 'debit';
  status: string;
  occurredAt: string;
};

type UnifiedRightSidebarProps = {
  user: User;
  walletBalance: number;
  paymentMethods: PaymentMethod[];
  linkedAccounts: LinkedBankAccount[];
  activities: UnifiedRailActivity[];
  ledgerBalanced: boolean;
  activeConnectionCount: number;
  debitOrderCount: number;
};

type MoneyCard = {
  id: string;
  kind: 'wallet' | 'linked';
  institution: string;
  accountName: string;
  mask: string;
  balance: number;
  currency: string;
  badge: string;
};

const UnifiedRightSidebar = ({
  user,
  walletBalance,
  paymentMethods,
  linkedAccounts,
  activities,
  ledgerBalanced,
  activeConnectionCount,
  debitOrderCount,
}: UnifiedRightSidebarProps) => {
  const defaultPaymentMethod = paymentMethods.find((method) => method.isDefault) ?? paymentMethods[0];

  const cards = useMemo<MoneyCard[]>(
    () => [
      {
        id: 'kape-wallet',
        kind: 'wallet',
        institution: 'Kape',
        accountName: 'Digital wallet',
        mask: defaultPaymentMethod?.last4 ?? 'WALLET',
        balance: walletBalance,
        currency: 'ZAR',
        badge: 'Wallet',
      },
      ...linkedAccounts.map((account) => ({
        id: account.id,
        kind: 'linked' as const,
        institution: account.institutionName,
        accountName: account.accountName,
        mask: account.accountNumberMask,
        balance: account.availableBalance,
        currency: account.currency,
        badge: account.accountType,
      })),
    ],
    [defaultPaymentMethod?.last4, linkedAccounts, walletBalance]
  );

  const [activeCardIndex, setActiveCardIndex] = useState(0);
  const safeIndex = Math.min(activeCardIndex, Math.max(cards.length - 1, 0));
  const activeCard = cards[safeIndex];
  const linkedAvailable = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);

  const moveCard = (direction: -1 | 1) => {
    if (cards.length <= 1) return;
    setActiveCardIndex((current) => (current + direction + cards.length) % cards.length);
  };

  return (
    <aside className="kape-rail unified-rail">
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
          <strong>Unified money</strong>
        </div>
        <span className="kape-rail__status" aria-label="Wallet platform active" />
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading unified-rail__card-heading">
          <div>
            <h2>Your money cards</h2>
            <p>Wallet and connected accounts</p>
          </div>
          <Link href="/linked-banks">Manage</Link>
        </div>

        <div className="unified-rail__card-meta">
          <span>Active card</span>
          <strong>{safeIndex + 1} of {cards.length}</strong>
        </div>

        {activeCard ? (
          <article className={`unified-money-card unified-money-card--${activeCard.kind}`}>
            <div className="unified-money-card__top">
              <div>
                <span>{activeCard.badge}</span>
                <strong>{activeCard.institution}</strong>
              </div>
              {activeCard.kind === 'wallet' ? <WalletCards size={19} /> : <Landmark size={19} />}
            </div>

            <div className="unified-money-card__balance">
              <span>Available balance</span>
              <strong>{formatAmount(activeCard.balance)}</strong>
            </div>

            <div className="unified-money-card__number">
              <span>•••• •••• •••• {activeCard.mask}</span>
              <small>{activeCard.currency}</small>
            </div>

            <div className="unified-money-card__bottom">
              <span>{user.firstName} {user.lastName}</span>
              <span>{activeCard.accountName}</span>
            </div>
          </article>
        ) : null}

        <div className="unified-rail__card-controls">
          <button type="button" onClick={() => moveCard(-1)} aria-label="Previous money card" disabled={cards.length <= 1}>
            <ChevronLeft size={15} />
          </button>
          <div>
            {cards.map((card, index) => (
              <button
                key={card.id}
                type="button"
                aria-label={`Show ${card.institution} card`}
                className={index === safeIndex ? 'is-active' : ''}
                onClick={() => setActiveCardIndex(index)}
              />
            ))}
          </div>
          <button type="button" onClick={() => moveCard(1)} aria-label="Next money card" disabled={cards.length <= 1}>
            <ChevronRight size={15} />
          </button>
        </div>
      </section>

      <section className="unified-rail__actions" aria-label="Quick money actions">
        <Link href="/wallet"><WalletCards size={14} /><span>Add money</span></Link>
        <Link href="/payment-transfer"><ArrowUpRight size={14} /><span>Send</span></Link>
        <Link href="/linked-banks"><Landmark size={14} /><span>Banks</span></Link>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Money snapshot</h2>
            <p>Across the integrated platform</p>
          </div>
        </div>

        <div className="unified-rail__snapshot">
          <div><span><WalletCards size={13} /> Wallet</span><strong>{formatAmount(walletBalance)}</strong></div>
          <div><span><Landmark size={13} /> Linked</span><strong>{formatAmount(linkedAvailable)}</strong></div>
          <div><span><ReceiptText size={13} /> Debit orders</span><strong>{debitOrderCount}</strong></div>
          <div><span><Scale size={13} /> Ledger</span><strong className={ledgerBalanced ? 'is-positive' : 'is-warning'}>{ledgerBalanced ? 'Balanced' : 'Review'}</strong></div>
        </div>
      </section>

      <section className="kape-rail__section unified-rail__activity-section">
        <div className="kape-rail__heading">
          <div>
            <h2>Latest activity</h2>
            <p>Wallet and bank movement</p>
          </div>
          <Link href="/transaction-history">View all</Link>
        </div>

        <div className="unified-rail__activity-list">
          {activities.slice(0, 4).map((activity) => {
            const isCredit = activity.direction === 'credit';
            return (
              <div key={activity.id} className="unified-rail__activity-row">
                <div className={isCredit ? 'is-credit' : 'is-debit'}>
                  {isCredit ? <ArrowDownLeft size={13} /> : <ArrowUpRight size={13} />}
                </div>
                <div>
                  <strong>{activity.title}</strong>
                  <span>{formatDateTime(activity.occurredAt).dateOnly}</span>
                </div>
                <strong className={isCredit ? 'is-credit' : 'is-debit'}>
                  {isCredit ? '+' : '-'}{formatAmount(activity.amount)}
                </strong>
              </div>
            );
          })}

          {!activities.length ? <p className="kape-empty-small">No wallet or linked-bank activity yet.</p> : null}
        </div>
      </section>

      <section className="unified-rail__security">
        <ShieldCheck size={15} />
        <div>
          <strong>Protected money view</strong>
          <span>{activeConnectionCount} active bank connection{activeConnectionCount === 1 ? '' : 's'} · tokenised card data</span>
        </div>
      </section>
    </aside>
  );
};

export default UnifiedRightSidebar;
