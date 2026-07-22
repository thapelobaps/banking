import Link from 'next/link';
import { redirect } from 'next/navigation';
import { Search, ShieldCheck } from 'lucide-react';

import HeaderBox from '@/components/HeaderBox';
import MerchantMark from '@/components/wallet/MerchantMark';
import MoneySourceCarousel, { type MoneySourceCarouselItem } from '@/components/wallet/MoneySourceCarousel';
import {
  getLinkedAccountTransactions,
  getLinkedAccounts,
  getWallet,
  getWalletTransactions,
} from '@/lib/actions/wallet.actions';
import { getLoggedInUser } from '@/lib/actions/user.actions';
import { formatAmount, formatDateTime } from '@/lib/utils';

type TransactionHistoryProps = {
  searchParams: {
    source?: string | string[];
    page?: string | string[];
    q?: string | string[];
  };
};

type UnifiedActivity = {
  id: string;
  sourceId: string;
  sourceName: string;
  sourceDetail: string;
  title: string;
  category: string;
  direction: 'credit' | 'debit';
  amount: number;
  feeAmount: number;
  status: string;
  occurredAt: string;
  reference?: string | null;
};

const walletCreditTypes = new Set(['top_up', 'transfer_in', 'refund']);
const getParam = (value?: string | string[]) => Array.isArray(value) ? value[0] : value;

const TransactionHistory = async ({ searchParams }: TransactionHistoryProps) => {
  const loggedIn = await getLoggedInUser();
  if (!loggedIn) redirect('/sign-in');

  const [wallet, walletTransactions, linkedAccounts] = await Promise.all([
    getWallet(),
    getWalletTransactions(1, 200),
    getLinkedAccounts(),
  ]);

  if (!wallet) {
    return (
      <section className="kape-page unified-history-page">
        <HeaderBox title="Transactions" subtext="Your Kape Wallet activity could not be loaded." />
        <div className="kape-empty-state">
          <strong>Activity unavailable</strong>
          <span>Confirm that the API is running and sign in again.</span>
        </div>
      </section>
    );
  }

  const linkedResults = await Promise.all(
    linkedAccounts.map(async (account) => ({
      account,
      transactions: (await getLinkedAccountTransactions(account.id, 1, 200)).items,
    }))
  );

  const walletActivity: UnifiedActivity[] = walletTransactions.items.map((transaction) => ({
    id: `wallet-${transaction.id}`,
    sourceId: 'wallet',
    sourceName: 'Kape Wallet',
    sourceDetail: 'Double-entry wallet',
    title: transaction.reference,
    category: transaction.type.replaceAll('_', ' '),
    direction: walletCreditTypes.has(transaction.type) ? 'credit' : 'debit',
    amount: transaction.amount,
    feeAmount: transaction.feeAmount,
    status: transaction.status,
    occurredAt: transaction.completedAt ?? transaction.createdAt,
    reference: transaction.externalReference,
  }));

  const linkedActivity: UnifiedActivity[] = linkedResults.flatMap(({ account, transactions }) =>
    transactions.map((transaction) => ({
      id: `linked-${transaction.id}`,
      sourceId: account.id,
      sourceName: account.institutionName,
      sourceDetail: `${account.accountName} •••• ${account.accountNumberMask}`,
      title: transaction.merchantName ?? transaction.description,
      category: transaction.category,
      direction: transaction.direction,
      amount: transaction.amount,
      feeAmount: 0,
      status: transaction.status,
      occurredAt: transaction.postedAt,
      reference: transaction.description,
    }))
  );

  const selectedSource = getParam(searchParams.source) ?? 'all';
  const searchQuery = (getParam(searchParams.q) ?? '').trim();
  const currentPage = Math.max(1, Number(getParam(searchParams.page)) || 1);
  const pageSize = 12;

  const sourceFiltered = [...walletActivity, ...linkedActivity]
    .filter((activity) => selectedSource === 'all' || activity.sourceId === selectedSource)
    .sort((left, right) => new Date(right.occurredAt).getTime() - new Date(left.occurredAt).getTime());

  const normalizedQuery = searchQuery.toLowerCase();
  const filteredActivity = normalizedQuery
    ? sourceFiltered.filter((activity) =>
        [activity.title, activity.category, activity.sourceName, activity.sourceDetail, activity.reference, activity.status]
          .filter(Boolean)
          .some((value) => value!.toLowerCase().includes(normalizedQuery))
      )
    : sourceFiltered;

  const totalPages = Math.max(1, Math.ceil(filteredActivity.length / pageSize));
  const safePage = Math.min(currentPage, totalPages);
  const pageItems = filteredActivity.slice((safePage - 1) * pageSize, safePage * pageSize);

  const createHistoryHref = (source: string, page = 1) => {
    const params = new URLSearchParams();
    params.set('source', source);
    if (searchQuery) params.set('q', searchQuery);
    if (page > 1) params.set('page', String(page));
    return `/transaction-history?${params.toString()}`;
  };

  const linkedAvailableTotal = linkedAccounts.reduce((sum, account) => sum + account.availableBalance, 0);
  const linkedCurrentTotal = linkedAccounts.reduce((sum, account) => sum + account.currentBalance, 0);
  const totalActivityCount = walletActivity.length + linkedActivity.length;

  const sourceCards: MoneySourceCarouselItem[] = [
    {
      id: 'all',
      href: createHistoryHref('all'),
      kind: 'aggregate',
      institution: 'Kape',
      accountName: 'All money activity',
      availableBalance: wallet.availableBalance + linkedAvailableTotal,
      currentBalance: wallet.balance + linkedCurrentTotal,
      entryCount: totalActivityCount,
    },
    {
      id: 'wallet',
      href: createHistoryHref('wallet'),
      kind: 'wallet',
      institution: 'Kape',
      accountName: 'Digital Wallet',
      mask: 'WALLET',
      availableBalance: wallet.availableBalance,
      currentBalance: wallet.balance,
      entryCount: walletActivity.length,
    },
    ...linkedResults.map(({ account, transactions }) => ({
      id: account.id,
      href: createHistoryHref(account.id),
      kind: 'bank' as const,
      institution: account.institutionName,
      accountName: account.accountName,
      mask: account.accountNumberMask,
      availableBalance: account.availableBalance,
      currentBalance: account.currentBalance,
      entryCount: transactions.length,
    })),
  ];

  return (
    <section className="kape-page unified-history-page">
      <header className="kape-page-header">
        <HeaderBox
          title="Transactions"
          subtext="Search wallet and consent-based linked-bank activity from one unified statement."
        />
        <span className="kape-count-pill">{filteredActivity.length} transaction{filteredActivity.length === 1 ? '' : 's'}</span>
      </header>

      <MoneySourceCarousel items={sourceCards} selectedId={selectedSource} />

      <form className="history-search" action="/transaction-history" method="get">
        <input type="hidden" name="source" value={selectedSource} />
        <Search size={17} />
        <input name="q" defaultValue={searchQuery} placeholder="Search merchant, reference, category, status or account" />
        <button type="submit">Search</button>
        {searchQuery ? <Link href={createHistoryHref(selectedSource)}>Clear</Link> : null}
      </form>

      <section className="unified-history-panel">
        <div className="unified-history-panel__heading">
          <div>
            <span>Unified statement</span>
            <h2>Recent money movement</h2>
            <p>Wallet entries are ledger-backed. Linked-bank entries are imported through provider consent.</p>
          </div>
          <div><ShieldCheck size={18} /> Masked financial data</div>
        </div>

        {pageItems.length ? (
          <div className="unified-history-table-wrap">
            <table className="unified-history-table">
              <thead>
                <tr>
                  <th>Transaction</th>
                  <th>Source</th>
                  <th>Date</th>
                  <th>Status</th>
                  <th>Amount</th>
                </tr>
              </thead>
              <tbody>
                {pageItems.map((activity) => {
                  const isCredit = activity.direction === 'credit';
                  return (
                    <tr key={activity.id}>
                      <td>
                        <MerchantMark
                          title={activity.title}
                          category={activity.category}
                          sourceName={activity.sourceName}
                          direction={activity.direction}
                        />
                        <div>
                          <strong>{activity.title}</strong>
                          <span>{activity.category}</span>
                          {activity.reference ? <small>{activity.reference}</small> : null}
                        </div>
                      </td>
                      <td><strong>{activity.sourceName}</strong><span>{activity.sourceDetail}</span></td>
                      <td><strong>{formatDateTime(activity.occurredAt).dateOnly}</strong><span>{formatDateTime(activity.occurredAt).timeOnly}</span></td>
                      <td><span className={`wallet-status wallet-status--${activity.status}`}>{activity.status}</span></td>
                      <td>
                        <strong className={isCredit ? 'is-credit' : 'is-debit'}>
                          {isCredit ? '+' : '-'}{formatAmount(activity.amount)}
                        </strong>
                        {activity.feeAmount > 0 ? <span>Fee {formatAmount(activity.feeAmount)}</span> : null}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="kape-empty-state">
            <strong>No matching activity</strong>
            <span>Change the selected source or clear the search query.</span>
          </div>
        )}

        {totalPages > 1 ? (
          <div className="history-pagination">
            <Link
              href={createHistoryHref(selectedSource, Math.max(1, safePage - 1))}
              aria-disabled={safePage === 1}
              className={safePage === 1 ? 'is-disabled' : ''}
            >
              Previous
            </Link>
            <span>Page {safePage} of {totalPages}</span>
            <Link
              href={createHistoryHref(selectedSource, Math.min(totalPages, safePage + 1))}
              aria-disabled={safePage === totalPages}
              className={safePage === totalPages ? 'is-disabled' : ''}
            >
              Next
            </Link>
          </div>
        ) : null}
      </section>
    </section>
  );
};

export default TransactionHistory;
