'use server';

import Link from 'next/link';
import { redirect } from 'next/navigation';
import {
  ArrowDownLeft,
  ArrowUpRight,
  CalendarClock,
  Landmark,
  RefreshCw,
  ShieldCheck,
} from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import BankConnectionPanel from '@/components/wallet/BankConnectionPanel';
import { getConfiguredBankProvider } from '@/lib/actions/bank-connection.actions';
import {
  getBankConnections,
  getLinkedAccountDebitOrders,
  getLinkedAccounts,
  getLinkedAccountTransactions,
} from '@/lib/actions/wallet.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

export default async function LinkedBanksPage() {
  const user = await getLoggedInUser();
  if (!user) redirect('/sign-in');

  const [providerId, connections, linkedAccounts] = await Promise.all([
    getConfiguredBankProvider(),
    getBankConnections(),
    getLinkedAccounts(),
  ]);

  const accountDetails = await Promise.all(
    linkedAccounts.map(async (account) => {
      const [transactions, debitOrders] = await Promise.all([
        getLinkedAccountTransactions(account.id, 1, 5),
        getLinkedAccountDebitOrders(account.id),
      ]);
      return { account, transactions, debitOrders };
    })
  );

  const totalCurrentBalance = linkedAccounts.reduce((sum, account) => sum + account.currentBalance, 0);
  const totalAvailableBalance = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const activeConnections = connections.filter((connection) => connection.status === 'active').length;

  return (
    <section className="kape-page linked-banks-page">
      <header className="kape-page-header">
        <HeaderBox
          title="Linked Banks"
          subtext="Consent-based balances, transactions and debit orders from external institutions."
        />
        <div className="kape-page-actions">
          <Link href="/wallet" className="kape-button kape-button--secondary">Kape wallet</Link>
          <Link href="/transaction-history" className="kape-button kape-button--primary">All activity</Link>
        </div>
      </header>

      <section className="wallet-metric-grid linked-bank-metrics">
        <article>
          <div className="wallet-metric__icon"><Landmark size={18} /></div>
          <span>Current balance</span>
          <strong>{formatAmount(totalCurrentBalance)}</strong>
          <small>Across all linked institutions</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><ShieldCheck size={18} /></div>
          <span>Available balance</span>
          <strong>{formatAmount(totalAvailableBalance)}</strong>
          <small>Provider-reported spendable money</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><RefreshCw size={18} /></div>
          <span>Active connections</span>
          <strong>{activeConnections}</strong>
          <small>{linkedAccounts.length} imported account{linkedAccounts.length === 1 ? '' : 's'}</small>
        </article>
        <article>
          <div className="wallet-metric__icon"><CalendarClock size={18} /></div>
          <span>Debit orders</span>
          <strong>{accountDetails.reduce((sum, item) => sum + item.debitOrders.length, 0)}</strong>
          <small>Recurring provider instructions</small>
        </article>
      </section>

      <section className="linked-bank-layout">
        <BankConnectionPanel connections={connections} providerId={providerId} />

        <section className="wallet-panel linked-bank-overview-panel">
          <div className="wallet-panel__heading">
            <span className="wallet-eyebrow">Aggregated accounts</span>
            <h2>Your linked balances</h2>
            <p>Account numbers remain masked; Kape stores only provider references and imported financial data.</p>
          </div>

          {linkedAccounts.length ? (
            <div className="linked-account-summary-list">
              {linkedAccounts.map((account) => (
                <article key={account.id} className="linked-account-summary">
                  <div className="linked-account-summary__bank">
                    <div className="linked-account-summary__icon"><Landmark size={18} /></div>
                    <div>
                      <strong>{account.institutionName}</strong>
                      <span>{account.accountName} •••• {account.accountNumberMask}</span>
                    </div>
                  </div>
                  <div className="linked-account-summary__balances">
                    <div>
                      <span>Current</span>
                      <strong>{formatAmount(account.currentBalance)}</strong>
                    </div>
                    <div>
                      <span>Available</span>
                      <strong>{formatAmount(account.availableBalance)}</strong>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <div className="wallet-empty-inline">
              <Landmark size={20} />
              <div>
                <strong>No linked accounts</strong>
                <span>Connect a bank to import balances and activity.</span>
              </div>
            </div>
          )}
        </section>
      </section>

      <section className="linked-account-detail-grid">
        {accountDetails.map(({ account, transactions, debitOrders }) => (
          <article key={account.id} className="wallet-panel linked-account-detail-card">
            <div className="linked-account-detail-card__header">
              <div>
                <span className="wallet-eyebrow">{account.accountType}</span>
                <h2>{account.institutionName} {account.accountName}</h2>
                <p>•••• {account.accountNumberMask} • Last synced {account.lastSyncedAt ? formatDateTime(account.lastSyncedAt).dateTime : 'not yet'}</p>
              </div>
              <div className="linked-account-detail-card__balance">
                <span>Available</span>
                <strong>{formatAmount(account.availableBalance)}</strong>
              </div>
            </div>

            <div className="linked-account-subgrid">
              <section>
                <div className="linked-account-subgrid__heading">
                  <h3>Recent transactions</h3>
                  <span>{transactions.total}</span>
                </div>
                {transactions.items.length ? (
                  <div className="linked-mini-list">
                    {transactions.items.map((transaction) => (
                      <div key={transaction.id} className="linked-mini-row">
                        <div className={`linked-mini-row__icon ${transaction.direction === 'credit' ? 'is-credit' : 'is-debit'}`}>
                          {transaction.direction === 'credit' ? <ArrowDownLeft size={15} /> : <ArrowUpRight size={15} />}
                        </div>
                        <div>
                          <strong>{transaction.merchantName ?? transaction.description}</strong>
                          <span>{transaction.category} • {formatDateTime(transaction.postedAt).dateOnly}</span>
                        </div>
                        <strong className={transaction.direction === 'credit' ? 'is-credit' : 'is-debit'}>
                          {transaction.direction === 'credit' ? '+' : '-'}{formatAmount(transaction.amount)}
                        </strong>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="linked-empty-copy">No imported activity for this account.</p>
                )}
              </section>

              <section>
                <div className="linked-account-subgrid__heading">
                  <h3>Debit orders</h3>
                  <span>{debitOrders.length}</span>
                </div>
                {debitOrders.length ? (
                  <div className="linked-mini-list">
                    {debitOrders.map((order) => (
                      <div key={order.id} className="linked-mini-row linked-mini-row--debit-order">
                        <div className="linked-mini-row__icon"><CalendarClock size={15} /></div>
                        <div>
                          <strong>{order.merchantName}</strong>
                          <span>{order.frequency} • {order.status}</span>
                        </div>
                        <strong>{order.amount == null ? 'Variable' : formatAmount(order.amount)}</strong>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="linked-empty-copy">No debit orders attached to this account.</p>
                )}
              </section>
            </div>
          </article>
        ))}
      </section>
    </section>
  );
}
