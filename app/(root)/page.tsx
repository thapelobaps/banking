'use server';

import Link from 'next/link';
import { redirect } from 'next/navigation';
import {
  Activity,
  ArrowDownLeft,
  ArrowUpRight,
  CreditCard,
  Landmark,
  Link2,
  ReceiptText,
  RefreshCw,
  Scale,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import {
  getBankConnections,
  getLedgerReconciliation,
  getLinkedAccountDebitOrders,
  getLinkedAccountTransactions,
  getLinkedAccounts,
  getPaymentMethods,
  getWallet,
  getWalletTransactions,
} from '@/lib/actions/wallet.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

const walletCreditTypes = new Set(['top_up', 'transfer_in']);

export default async function Home() {
  const user = await getLoggedInUser();
  if (!user) redirect('/sign-in');

  const [wallet, walletTransactions, paymentMethods, linkedAccounts, connections, reconciliation] = await Promise.all([
    getWallet(),
    getWalletTransactions(1, 10),
    getPaymentMethods(),
    getLinkedAccounts(),
    getBankConnections(),
    getLedgerReconciliation(),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page overview-page">
        <HeaderBox
          type="greeting"
          title="Welcome back"
          user={user.firstName}
          subtext="Your unified Kape dashboard could not be loaded. Confirm that the API is running and sign in again."
        />
        <div className="kape-empty-state">
          <strong>Financial overview unavailable</strong>
          <span>Start the Kape API on port 5000 and refresh the page.</span>
        </div>
      </section>
    );
  }

  const linkedAccountData = await Promise.all(
    linkedAccounts.map(async (account) => {
      const [transactions, debitOrders] = await Promise.all([
        getLinkedAccountTransactions(account.id, 1, 8),
        getLinkedAccountDebitOrders(account.id),
      ]);

      return { account, transactions: transactions.items, debitOrders };
    })
  );

  const linkedCurrentBalance = linkedAccounts.reduce((sum, account) => sum + account.currentBalance, 0);
  const linkedAvailableBalance = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const visibleAvailableBalance = wallet.availableBalance + linkedAvailableBalance;
  const debitOrderCount = linkedAccountData.reduce((sum, entry) => sum + entry.debitOrders.length, 0);
  const activeConnectionCount = connections.filter((connection) => connection.status === 'active').length;
  const defaultPaymentMethod = paymentMethods.find((method) => method.isDefault) ?? paymentMethods[0];

  const walletActivity = walletTransactions.items.map((transaction) => ({
    id: `wallet-${transaction.id}`,
    title: transaction.reference,
    detail: `Kape wallet • ${transaction.type.replaceAll('_', ' ')}`,
    amount: transaction.netAmount,
    direction: walletCreditTypes.has(transaction.type) ? ('credit' as const) : ('debit' as const),
    status: transaction.status,
    occurredAt: transaction.createdAt,
  }));

  const linkedActivity = linkedAccountData.flatMap(({ account, transactions }) =>
    transactions.map((transaction) => ({
      id: `linked-${transaction.id}`,
      title: transaction.merchantName ?? transaction.description,
      detail: `${account.institutionName} • ${transaction.category}`,
      amount: transaction.amount,
      direction: transaction.direction,
      status: transaction.status,
      occurredAt: transaction.postedAt,
    }))
  );

  const recentActivity = [...walletActivity, ...linkedActivity]
    .sort((left, right) => new Date(right.occurredAt).getTime() - new Date(left.occurredAt).getTime())
    .slice(0, 7);

  const latestSync = linkedAccounts
    .map((account) => account.lastSyncedAt)
    .filter((value): value is string => Boolean(value))
    .sort((left, right) => new Date(right).getTime() - new Date(left).getTime())[0];

  return (
    <section className="kape-page overview-page">
      <header className="kape-page-header overview-header">
        <HeaderBox
          type="greeting"
          title="Welcome back"
          user={user.firstName}
          subtext="Your Kape wallet, linked banks, cards and money activity in one trusted view."
        />
        <div className="kape-page-actions">
          <Link href="/linked-banks" className="kape-button kape-button--secondary">
            <Link2 size={15} /> Linked banks
          </Link>
          <Link href="/wallet" className="kape-button kape-button--secondary">
            <WalletCards size={15} /> Add money
          </Link>
          <Link href="/payment-transfer" className="kape-button kape-button--primary">
            <ArrowUpRight size={15} /> Send money
          </Link>
        </div>
      </header>

      <section className="overview-hero">
        <div className="overview-hero__balance">
          <span className="overview-eyebrow"><WalletCards size={16} /> Visible available balances</span>
          <strong>{formatAmount(visibleAvailableBalance)}</strong>
          <p>Kape wallet plus provider-reported linked balances. This consolidated view does not treat connected balances as new money.</p>
          <div className="overview-hero__badges">
            <span>{wallet.status}</span>
            <span>{wallet.currency}</span>
            <span>{reconciliation?.isBalanced ? 'Ledger balanced' : 'Ledger review needed'}</span>
          </div>
        </div>

        <div className="overview-hero__breakdown">
          <article>
            <span>Kape wallet</span>
            <strong>{formatAmount(wallet.availableBalance)}</strong>
            <small>{walletTransactions.total} wallet transaction{walletTransactions.total === 1 ? '' : 's'}</small>
          </article>
          <article>
            <span>Linked available</span>
            <strong>{formatAmount(linkedAvailableBalance)}</strong>
            <small>{linkedAccounts.length} linked account{linkedAccounts.length === 1 ? '' : 's'}</small>
          </article>
          <article>
            <span>Linked current</span>
            <strong>{formatAmount(linkedCurrentBalance)}</strong>
            <small>{activeConnectionCount} active connection{activeConnectionCount === 1 ? '' : 's'}</small>
          </article>
        </div>
      </section>

      <section className="overview-metric-grid">
        <article>
          <div className="overview-metric__icon"><CreditCard size={18} /></div>
          <span>Default funding card</span>
          <strong>{defaultPaymentMethod ? `${defaultPaymentMethod.bankName} •••• ${defaultPaymentMethod.last4}` : 'No card added'}</strong>
          <small>{paymentMethods.length} tokenised payment method{paymentMethods.length === 1 ? '' : 's'}</small>
        </article>
        <article>
          <div className="overview-metric__icon"><Landmark size={18} /></div>
          <span>Connected banking</span>
          <strong>{linkedAccounts.length} account{linkedAccounts.length === 1 ? '' : 's'}</strong>
          <small>{latestSync ? `Synced ${formatDateTime(latestSync).dateTime}` : 'Connect a bank to begin'}</small>
        </article>
        <article>
          <div className="overview-metric__icon"><ReceiptText size={18} /></div>
          <span>Debit orders</span>
          <strong>{debitOrderCount}</strong>
          <small>Recurring instructions imported from linked banks</small>
        </article>
        <article>
          <div className="overview-metric__icon"><Scale size={18} /></div>
          <span>Ledger difference</span>
          <strong>{formatAmount(reconciliation?.difference ?? 0)}</strong>
          <small>{reconciliation?.isBalanced ? 'Double-entry books agree' : 'Reconciliation needs attention'}</small>
        </article>
      </section>

      <section className="overview-workspace">
        <article className="overview-panel overview-activity-panel">
          <div className="overview-panel__heading">
            <div>
              <span className="overview-eyebrow">Unified activity</span>
              <h2>Recent money movement</h2>
              <p>Wallet and linked-bank transactions ordered by their latest activity.</p>
            </div>
            <Link href="/transaction-history">All activity</Link>
          </div>

          {recentActivity.length ? (
            <div className="overview-activity-list">
              {recentActivity.map((transaction) => {
                const isCredit = transaction.direction === 'credit';
                return (
                  <div key={transaction.id} className="overview-activity-row">
                    <div className={`overview-activity-row__icon ${isCredit ? 'is-credit' : 'is-debit'}`}>
                      {isCredit ? <ArrowDownLeft size={17} /> : <ArrowUpRight size={17} />}
                    </div>
                    <div className="overview-activity-row__copy">
                      <strong>{transaction.title}</strong>
                      <span>{transaction.detail} • {formatDateTime(transaction.occurredAt).dateTime}</span>
                    </div>
                    <div className="overview-activity-row__amount">
                      <strong className={isCredit ? 'is-credit' : 'is-debit'}>
                        {isCredit ? '+' : '-'}{formatAmount(transaction.amount)}
                      </strong>
                      <span>{transaction.status}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="overview-empty-inline">
              <Activity size={20} />
              <div>
                <strong>No activity yet</strong>
                <span>Fund the wallet or connect a bank to begin building your overview.</span>
              </div>
            </div>
          )}
        </article>

        <aside className="overview-side-stack">
          <article className="overview-panel">
            <div className="overview-panel__heading">
              <div>
                <span className="overview-eyebrow">Connected money</span>
                <h2>Linked accounts</h2>
              </div>
              <Link href="/linked-banks">Manage</Link>
            </div>
            <div className="overview-account-list">
              {linkedAccounts.length ? linkedAccounts.map((account) => (
                <div key={account.id} className="overview-account-row">
                  <div className="overview-account-row__mark"><Landmark size={17} /></div>
                  <div>
                    <strong>{account.institutionName}</strong>
                    <span>{account.accountName} •••• {account.accountNumberMask}</span>
                  </div>
                  <strong>{formatAmount(account.availableBalance)}</strong>
                </div>
              )) : (
                <div className="overview-empty-inline overview-empty-inline--compact">
                  <Link2 size={18} />
                  <div><strong>No linked banks</strong><span>Connect a bank to import balances.</span></div>
                </div>
              )}
            </div>
          </article>

          <article className="overview-panel overview-health-panel">
            <div className="overview-panel__heading">
              <div>
                <span className="overview-eyebrow">Platform health</span>
                <h2>Money controls</h2>
              </div>
              <ShieldCheck size={20} />
            </div>
            <div className="overview-health-list">
              <div><span><Scale size={16} /> Ledger</span><strong className={reconciliation?.isBalanced ? 'is-positive' : 'is-warning'}>{reconciliation?.isBalanced ? 'Balanced' : 'Review'}</strong></div>
              <div><span><CreditCard size={16} /> Card data</span><strong>Tokenised</strong></div>
              <div><span><RefreshCw size={16} /> Bank sync</span><strong>{activeConnectionCount ? 'Active' : 'Not linked'}</strong></div>
              <div><span><ShieldCheck size={16} /> Raw PAN/CVV</span><strong>Never stored</strong></div>
            </div>
          </article>
        </aside>
      </section>
    </section>
  );
}
