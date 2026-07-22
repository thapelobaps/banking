import Link from 'next/link';
import {
  Activity,
  CreditCard,
  Landmark,
  RefreshCw,
  Scale,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';

import { formatAmount, formatDateTime } from '@/lib/utils';
import type { User } from '@/types';
import type {
  BankConnection,
  LedgerReconciliation,
  LinkedBankAccount,
  PaymentMethod,
  WalletSummary,
  WalletTransaction,
} from '@/types/wallet';

type IntegratedOverviewRailProps = {
  user: User;
  wallet: WalletSummary;
  walletTransactions: WalletTransaction[];
  paymentMethods: PaymentMethod[];
  linkedAccounts: LinkedBankAccount[];
  connections: BankConnection[];
  reconciliation: LedgerReconciliation | null;
};

const creditTypes = new Set(['top_up', 'transfer_in', 'refund']);

export default function IntegratedOverviewRail({
  user,
  wallet,
  walletTransactions,
  paymentMethods,
  linkedAccounts,
  connections,
  reconciliation,
}: IntegratedOverviewRailProps) {
  const defaultMethod = paymentMethods.find((method) => method.isDefault) ?? paymentMethods[0];
  const linkedAvailable = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const activeConnections = connections.filter((connection) => connection.status === 'active');
  const moneyIn = walletTransactions
    .filter((transaction) => creditTypes.has(transaction.type))
    .reduce((sum, transaction) => sum + transaction.netAmount, 0);
  const moneyOut = walletTransactions
    .filter((transaction) => !creditTypes.has(transaction.type))
    .reduce((sum, transaction) => sum + transaction.netAmount, 0);
  const latestSync = linkedAccounts
    .map((account) => account.lastSyncedAt)
    .filter((value): value is string => Boolean(value))
    .sort((left, right) => new Date(right).getTime() - new Date(left).getTime())[0];

  return (
    <aside className="kape-rail integrated-rail">
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
            <h2>Your cards</h2>
            <p>Wallet and tokenised funding</p>
          </div>
          <Link href="/wallet">Manage</Link>
        </div>

        <div className="integrated-rail__cards">
          <article className="integrated-rail__wallet-card">
            <div className="integrated-rail__card-top">
              <span><WalletCards size={14} /> Kape wallet</span>
              <small>{wallet.status}</small>
            </div>
            <strong>{formatAmount(wallet.availableBalance)}</strong>
            <div className="integrated-rail__card-bottom">
              <span>{user.firstName} {user.lastName}</span>
              <span>{wallet.currency}</span>
            </div>
          </article>

          {defaultMethod ? (
            <article className="integrated-rail__funding-card">
              <div className="integrated-rail__card-top">
                <span><CreditCard size={14} /> {defaultMethod.bankName}</span>
                <small>{defaultMethod.isDefault ? 'Default' : 'Active'}</small>
              </div>
              <strong>•••• {defaultMethod.last4}</strong>
              <div className="integrated-rail__card-bottom">
                <span>{defaultMethod.brand}</span>
                <span>{String(defaultMethod.expiryMonth).padStart(2, '0')}/{String(defaultMethod.expiryYear).slice(-2)}</span>
              </div>
            </article>
          ) : (
            <Link href="/wallet" className="integrated-rail__empty-card">
              <CreditCard size={17} />
              <span>Add a tokenised funding card</span>
            </Link>
          )}
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Connected accounts</h2>
            <p>Provider-reported balances</p>
          </div>
          <Link href="/linked-banks">View all</Link>
        </div>

        <div className="integrated-rail__accounts">
          {linkedAccounts.slice(0, 3).map((account) => (
            <div key={account.id} className="integrated-rail__account-row">
              <div className="integrated-rail__account-icon"><Landmark size={14} /></div>
              <div>
                <strong>{account.institutionName}</strong>
                <span>{account.accountName} •••• {account.accountNumberMask}</span>
              </div>
              <strong>{formatAmount(account.availableBalance)}</strong>
            </div>
          ))}
          {!linkedAccounts.length ? (
            <Link href="/linked-banks" className="integrated-rail__empty-row">
              <Landmark size={15} /> Connect your first bank
            </Link>
          ) : null}
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Money intelligence</h2>
            <p>Across wallet activity</p>
          </div>
        </div>

        <div className="integrated-rail__intelligence">
          <div className="integrated-rail__available">
            <span>Available across visible sources</span>
            <strong>{formatAmount(wallet.availableBalance + linkedAvailable)}</strong>
            <small>{linkedAccounts.length + 1} money source{linkedAccounts.length ? 's' : ''}</small>
          </div>
          <div className="integrated-rail__movement-grid">
            <div className="is-credit"><span>Wallet money in</span><strong>{formatAmount(moneyIn)}</strong></div>
            <div className="is-debit"><span>Wallet money out</span><strong>{formatAmount(moneyOut)}</strong></div>
          </div>
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Platform health</h2>
            <p>Live safety and sync status</p>
          </div>
        </div>

        <div className="integrated-rail__health">
          <div><span><Scale size={13} /> Ledger</span><strong className={reconciliation?.isBalanced ? 'is-positive' : 'is-warning'}>{reconciliation?.isBalanced ? 'Balanced' : 'Review'}</strong></div>
          <div><span><ShieldCheck size={13} /> Card security</span><strong>Tokenised</strong></div>
          <div><span><RefreshCw size={13} /> Bank sync</span><strong>{activeConnections.length ? 'Active' : 'Not linked'}</strong></div>
          <div><span><Activity size={13} /> Wallet activity</span><strong>{walletTransactions.length}</strong></div>
        </div>
        {latestSync ? <p className="integrated-rail__sync">Last bank sync {formatDateTime(latestSync).dateTime}</p> : null}
      </section>
    </aside>
  );
}
