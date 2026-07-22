'use server';

import Link from 'next/link';
import { redirect } from 'next/navigation';
import {
  ArrowRight,
  CreditCard,
  Landmark,
  Link2,
  RefreshCw,
  ShieldCheck,
  WalletCards,
} from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import BankConnectionPanel from '@/components/wallet/BankConnectionPanel';
import PaymentMethodPanel from '@/components/wallet/PaymentMethodPanel';
import { getConfiguredBankProvider } from '@/lib/actions/bank-connection.actions';
import {
  getBankConnections,
  getLinkedAccounts,
  getPaymentMethods,
  getWallet,
} from '@/lib/actions/wallet.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

const MyBanks = async () => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [providerId, wallet, linkedAccounts, connections, paymentMethods] = await Promise.all([
    getConfiguredBankProvider(),
    getWallet(),
    getLinkedAccounts(),
    getBankConnections(),
    getPaymentMethods(),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page money-accounts-page">
        <HeaderBox title="Money accounts" subtext="Your wallet account could not be loaded. Confirm the API is running and sign in again." />
        <div className="kape-empty-state">
          <strong>Accounts unavailable</strong>
          <span>Start the Kape API on port 5000 and refresh this page.</span>
        </div>
      </section>
    );
  }

  const linkedCurrent = linkedAccounts.reduce((sum, account) => sum + account.currentBalance, 0);
  const linkedAvailable = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const activeConnections = connections.filter((connection) => connection.status === 'active');
  const defaultMethod = paymentMethods.find((method) => method.isDefault) ?? paymentMethods[0];
  const latestSync = linkedAccounts
    .map((account) => account.lastSyncedAt)
    .filter((value): value is string => Boolean(value))
    .sort((left, right) => new Date(right).getTime() - new Date(left).getTime())[0];

  return (
    <section className="kape-page money-accounts-page">
      <header className="kape-page-header">
        <HeaderBox
          title="Money accounts"
          subtext="Manage your Kape Wallet, connected bank accounts and tokenised funding cards in one place."
        />
        <div className="kape-page-actions">
          <Link href="/transaction-history" className="kape-button kape-button--secondary">
            View activity
          </Link>
          <Link href="/wallet" className="kape-button kape-button--primary">
            Add money
          </Link>
        </div>
      </header>

      <section className="money-accounts-hero">
        <div>
          <span><WalletCards size={17} /> Total visible available balances</span>
          <strong>{formatAmount(wallet.availableBalance + linkedAvailable)}</strong>
          <p>Includes the Kape Wallet and provider-reported linked balances without counting connected money as new funds.</p>
        </div>
        <div>
          <article>
            <span>Kape Wallet</span>
            <strong>{formatAmount(wallet.availableBalance)}</strong>
            <small>{wallet.currency} · {wallet.status}</small>
          </article>
          <article>
            <span>Linked available</span>
            <strong>{formatAmount(linkedAvailable)}</strong>
            <small>{linkedAccounts.length} imported account{linkedAccounts.length === 1 ? '' : 's'}</small>
          </article>
          <article>
            <span>Linked current</span>
            <strong>{formatAmount(linkedCurrent)}</strong>
            <small>{activeConnections.length} active connection{activeConnections.length === 1 ? '' : 's'}</small>
          </article>
        </div>
      </section>

      <section className="money-account-metrics">
        <article>
          <div><CreditCard size={18} /></div>
          <span>Default funding card</span>
          <strong>{defaultMethod ? `${defaultMethod.bankName} •••• ${defaultMethod.last4}` : 'No card added'}</strong>
          <small>{paymentMethods.length} tokenised payment method{paymentMethods.length === 1 ? '' : 's'}</small>
        </article>
        <article>
          <div><Link2 size={18} /></div>
          <span>Bank connections</span>
          <strong>{activeConnections.length} active</strong>
          <small>Consent-based provider access</small>
        </article>
        <article>
          <div><RefreshCw size={18} /></div>
          <span>Last bank sync</span>
          <strong>{latestSync ? formatDateTime(latestSync).dateOnly : 'Not synced'}</strong>
          <small>{latestSync ? formatDateTime(latestSync).timeOnly : 'Connect a bank to begin'}</small>
        </article>
        <article>
          <div><ShieldCheck size={18} /></div>
          <span>Card security</span>
          <strong>Tokenised</strong>
          <small>Raw PAN and CVV are never stored</small>
        </article>
      </section>

      <section className="money-account-section">
        <div className="money-account-section__heading">
          <div>
            <span>Integrated balances</span>
            <h2>Your accounts</h2>
            <p>The Kape Wallet is spendable inside the platform. Linked accounts provide consent-based visibility and funding destinations.</p>
          </div>
          <Link href="/linked-banks">Open bank workspace <ArrowRight size={15} /></Link>
        </div>

        <div className="money-account-card-grid">
          <article className="money-account-card money-account-card--wallet">
            <div className="money-account-card__top">
              <div>
                <span>Digital wallet</span>
                <strong>Kape Wallet</strong>
              </div>
              <WalletCards size={22} />
            </div>
            <div className="money-account-card__balance">
              <span>Available balance</span>
              <strong>{formatAmount(wallet.availableBalance)}</strong>
              <small>Current {formatAmount(wallet.balance)}</small>
            </div>
            <div className="money-account-card__bottom">
              <span>{loggedIn.firstName} {loggedIn.lastName}</span>
              <span>{wallet.currency} · {wallet.status}</span>
            </div>
            <div className="money-account-card__actions">
              <Link href="/wallet">Manage wallet</Link>
              <Link href="/payment-transfer">Send money</Link>
            </div>
          </article>

          {linkedAccounts.map((account) => (
            <article key={account.id} className="money-account-card money-account-card--linked">
              <div className="money-account-card__top">
                <div>
                  <span>{account.accountType}</span>
                  <strong>{account.institutionName}</strong>
                </div>
                <Landmark size={22} />
              </div>
              <div className="money-account-card__balance">
                <span>Available balance</span>
                <strong>{formatAmount(account.availableBalance)}</strong>
                <small>Current {formatAmount(account.currentBalance)}</small>
              </div>
              <div className="money-account-card__bottom">
                <span>{account.accountName}</span>
                <span>•••• {account.accountNumberMask}</span>
              </div>
              <div className="money-account-card__actions">
                <Link href={`/transaction-history?source=${account.id}`}>View activity</Link>
                <Link href="/linked-banks">Manage connection</Link>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="money-account-management-grid">
        <BankConnectionPanel connections={connections} providerId={providerId} />
        <PaymentMethodPanel paymentMethods={paymentMethods} />
      </section>
    </section>
  );
};

export default MyBanks;
