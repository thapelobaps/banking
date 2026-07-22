'use client';

import Link from 'next/link';
import {
  ChevronLeft,
  ChevronRight,
  CreditCard,
  Eye,
  EyeOff,
  Landmark,
  ReceiptText,
  Scale,
  Send,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';
import { useMemo, useState } from 'react';

import { formatAmount } from '@/lib/utils';
import type { User } from '@/types';
import type {
  LedgerReconciliation,
  LinkedBankAccount,
  PaymentMethod,
  WalletSummary,
} from '@/types/wallet';

type RailActivity = {
  id: string;
  category: string;
  amount: number;
  direction: 'credit' | 'debit';
};

type UnifiedOverviewRailProps = {
  user: User;
  wallet: WalletSummary;
  linkedAccounts: LinkedBankAccount[];
  paymentMethods: PaymentMethod[];
  reconciliation: LedgerReconciliation | null;
  activity: RailActivity[];
  activeConnections: number;
};

type RailCard = {
  id: string;
  kind: 'wallet' | 'linked';
  institution: string;
  accountName: string;
  mask: string;
  currentBalance: number;
  availableBalance: number;
  status: string;
};

export default function UnifiedOverviewRail({
  user,
  wallet,
  linkedAccounts,
  paymentMethods,
  reconciliation,
  activity,
  activeConnections,
}: UnifiedOverviewRailProps) {
  const [activeIndex, setActiveIndex] = useState(0);
  const [showBalance, setShowBalance] = useState(true);

  const cards = useMemo<RailCard[]>(
    () => [
      {
        id: wallet.id,
        kind: 'wallet',
        institution: 'Kape',
        accountName: 'Wallet',
        mask: paymentMethods.find((method) => method.isDefault)?.last4 ?? 'WALT',
        currentBalance: wallet.balance,
        availableBalance: wallet.availableBalance,
        status: wallet.status,
      },
      ...linkedAccounts.map((account) => ({
        id: account.id,
        kind: 'linked' as const,
        institution: account.institutionName,
        accountName: account.accountName,
        mask: account.accountNumberMask,
        currentBalance: account.currentBalance,
        availableBalance: account.availableBalance,
        status: account.isActive ? 'active' : 'inactive',
      })),
    ],
    [linkedAccounts, paymentMethods, wallet]
  );

  const safeIndex = Math.min(activeIndex, Math.max(cards.length - 1, 0));
  const card = cards[safeIndex];
  const totalAvailable = wallet.availableBalance + linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const moneyIn = activity.filter((item) => item.direction === 'credit').reduce((sum, item) => sum + item.amount, 0);
  const moneyOut = activity.filter((item) => item.direction === 'debit').reduce((sum, item) => sum + item.amount, 0);
  const netMovement = moneyIn - moneyOut;

  const categories = Object.entries(
    activity.reduce<Record<string, number>>((summary, item) => {
      summary[item.category] = (summary[item.category] ?? 0) + 1;
      return summary;
    }, {})
  )
    .sort((left, right) => right[1] - left[1])
    .slice(0, 4);

  const move = (direction: -1 | 1) => {
    if (!cards.length) return;
    setActiveIndex((current) => (current + direction + cards.length) % cards.length);
  };

  return (
    <aside className="overview-integrated-rail">
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
            <p>Wallet and connected accounts</p>
          </div>
          <Link href="/linked-banks">Manage</Link>
        </div>

        {card ? (
          <div className="overview-rail-card-stack">
            <div className="overview-rail-card-nav">
              <div>
                <span>Active card</span>
                <strong>{safeIndex + 1} of {cards.length}</strong>
              </div>
              <div>
                <button type="button" onClick={() => move(-1)} disabled={cards.length < 2} aria-label="Previous money card">
                  <ChevronLeft size={14} />
                </button>
                <button type="button" onClick={() => move(1)} disabled={cards.length < 2} aria-label="Next money card">
                  <ChevronRight size={14} />
                </button>
              </div>
            </div>

            <article className={`overview-money-card overview-money-card--${card.kind}`}>
              <div className="overview-money-card__top">
                <div>
                  <span>{card.kind === 'wallet' ? 'Kape wallet' : 'Linked bank account'}</span>
                  <strong>{card.institution}</strong>
                  <small>{card.accountName}</small>
                </div>
                <div>
                  <em>{card.status}</em>
                  <button type="button" onClick={() => setShowBalance((current) => !current)} aria-label={showBalance ? 'Hide balance' : 'Show balance'}>
                    {showBalance ? <EyeOff size={12} /> : <Eye size={12} />}
                  </button>
                </div>
              </div>

              <div className="overview-money-card__balance">
                <span>Available balance</span>
                <strong>{showBalance ? formatAmount(card.availableBalance) : 'R ••••••'}</strong>
                <small>Current {showBalance ? formatAmount(card.currentBalance) : 'R ••••••'}</small>
              </div>

              <div className="overview-money-card__bottom">
                <span>{user.firstName} {user.lastName}</span>
                <strong>•••• •••• •••• {card.mask}</strong>
              </div>
            </article>

            {cards.length > 1 ? (
              <div className="overview-rail-card-dots" aria-label="Money card position">
                {cards.map((candidate, index) => (
                  <button
                    type="button"
                    key={candidate.id}
                    onClick={() => setActiveIndex(index)}
                    className={index === safeIndex ? 'is-active' : ''}
                    aria-label={`Show ${candidate.institution} ${candidate.accountName}`}
                  />
                ))}
              </div>
            ) : null}

            <div className="overview-rail-actions">
              <Link href="/payment-transfer"><Send size={12} /> Send</Link>
              <Link href="/wallet"><WalletCards size={12} /> Wallet</Link>
              <Link href="/transaction-history"><ReceiptText size={12} /> Activity</Link>
            </div>
          </div>
        ) : (
          <p className="kape-empty-small">No money cards available.</p>
        )}
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Money intelligence</h2>
            <p>Across wallet and linked banks</p>
          </div>
        </div>

        <div className="overview-rail-intelligence">
          <article>
            <div><span>Available now</span><em>{cards.length} sources</em></div>
            <strong>{formatAmount(totalAvailable)}</strong>
          </article>
          <div>
            <article className="is-credit"><span>Money in</span><strong>{formatAmount(moneyIn)}</strong></article>
            <article className="is-debit"><span>Money out</span><strong>{formatAmount(moneyOut)}</strong></article>
          </div>
          <article className="overview-rail-net">
            <div><span>Net movement</span><small>Loaded recent activity</small></div>
            <strong className={netMovement >= 0 ? 'is-credit' : 'is-debit'}>{netMovement >= 0 ? '+' : '-'}{formatAmount(Math.abs(netMovement))}</strong>
          </article>
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading"><div><h2>Platform controls</h2><p>Live wallet safeguards</p></div></div>
        <div className="overview-rail-health">
          <div><span><Scale size={14} /> Ledger</span><strong>{reconciliation?.isBalanced ? 'Balanced' : 'Review'}</strong></div>
          <div><span><CreditCard size={14} /> Cards</span><strong>{paymentMethods.length} tokenised</strong></div>
          <div><span><Landmark size={14} /> Bank sync</span><strong>{activeConnections ? 'Active' : 'Not linked'}</strong></div>
          <div><span><ShieldCheck size={14} /> Raw PAN/CVV</span><strong>Never stored</strong></div>
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading"><div><h2>Activity categories</h2><p>Recent unified movement</p></div></div>
        <div className="kape-category-list">
          {categories.length ? categories.map(([name, count], index) => (
            <div key={name} className="kape-category-row">
              <span className="kape-category-row__number">{index + 1}</span>
              <div className="kape-category-row__content">
                <div><strong>{name}</strong><span>{count}</span></div>
                <div className="kape-category-row__track"><div style={{ width: `${Math.max(16, Math.round((count / activity.length) * 100))}%` }} /></div>
              </div>
            </div>
          )) : <p className="kape-empty-small">No recent activity categories.</p>}
        </div>
      </section>
    </aside>
  );
}
