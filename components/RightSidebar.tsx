'use client';

import Link from 'next/link';

import { countTransactionCategories, formatAmount } from '@/lib/utils';
import { CategoryCount, RightSidebarProps } from '@/types';
import AccountCardSwitcher from './AccountCardSwitcher';

const RightSidebar = ({ user, transactions, banks }: RightSidebarProps) => {
  const categories: CategoryCount[] = countTransactionCategories(transactions).slice(0, 4);
  const moneyIn = transactions
    .filter((transaction) => transaction.type === 'credit')
    .reduce((total, transaction) => total + transaction.amount, 0);
  const moneyOut = transactions
    .filter((transaction) => transaction.type === 'debit')
    .reduce((total, transaction) => total + transaction.amount, 0);
  const netMovement = moneyIn - moneyOut;
  const totalAvailable = banks.reduce((total, account) => total + account.availableBalance, 0);

  return (
    <aside className="kape-rail">
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
          <strong>Personal demo</strong>
        </div>
        <span className="kape-rail__status" aria-label="Demo workspace active" />
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Your cards</h2>
            <p>Switch between active accounts</p>
          </div>
          <Link href="/my-banks">View all</Link>
        </div>

        <AccountCardSwitcher
          accounts={banks}
          userName={`${user.firstName} ${user.lastName}`}
        />
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Money intelligence</h2>
            <p>Across your recent activity</p>
          </div>
        </div>

        <div className="grid gap-1.5">
          <div className="rounded-xl border border-[#eee5df] bg-[#fdfaf8] p-2.5">
            <div className="flex items-center justify-between gap-2">
              <span className="text-[7px] font-semibold uppercase tracking-[0.13em] text-[#9a8378]">Available now</span>
              <span className="rounded-full bg-[#f3ebe6] px-1.5 py-0.5 text-[7px] font-semibold text-[#6b4435]">
                {banks.length} accounts
              </span>
            </div>
            <strong className="mt-1.5 block text-[13px] text-[#2b1a14]">{formatAmount(totalAvailable)}</strong>
          </div>

          <div className="grid grid-cols-2 gap-1.5">
            <div className="rounded-xl border border-emerald-100 bg-emerald-50 p-2.5">
              <span className="text-[7px] font-semibold uppercase tracking-[0.12em] text-emerald-700">Money in</span>
              <strong className="mt-1 block truncate text-[10px] text-emerald-900">{formatAmount(moneyIn)}</strong>
            </div>
            <div className="rounded-xl border border-rose-100 bg-rose-50 p-2.5">
              <span className="text-[7px] font-semibold uppercase tracking-[0.12em] text-rose-700">Money out</span>
              <strong className="mt-1 block truncate text-[10px] text-rose-900">{formatAmount(moneyOut)}</strong>
            </div>
          </div>

          <div className="flex items-center justify-between gap-2 rounded-xl border border-[#eee5df] bg-white px-2.5 py-2">
            <div>
              <span className="block text-[7px] font-semibold uppercase tracking-[0.12em] text-[#9a8378]">Net movement</span>
              <small className="mt-0.5 block text-[7px] text-[#8a756b]">From loaded recent transactions</small>
            </div>
            <strong className={`text-[10px] ${netMovement >= 0 ? 'text-emerald-700' : 'text-rose-700'}`}>
              {netMovement >= 0 ? '+' : '-'}{formatAmount(Math.abs(netMovement))}
            </strong>
          </div>
        </div>
      </section>

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Spending categories</h2>
            <p>Based on recent activity</p>
          </div>
        </div>

        <div className="kape-category-list">
          {categories.length > 0 ? (
            categories.map((category, index) => {
              const percentage = Math.max(12, Math.round((category.count / category.totalCount) * 100));
              return (
                <div key={category.name} className="kape-category-row">
                  <span className="kape-category-row__number">{index + 1}</span>
                  <div className="kape-category-row__content">
                    <div>
                      <strong>{category.name}</strong>
                      <span>{category.count}</span>
                    </div>
                    <div className="kape-category-row__track">
                      <div style={{ width: `${percentage}%` }} />
                    </div>
                  </div>
                </div>
              );
            })
          ) : (
            <p className="kape-empty-small">No transaction categories yet.</p>
          )}
        </div>
      </section>
    </aside>
  );
};

export default RightSidebar;
