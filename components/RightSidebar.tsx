'use client';

import Link from 'next/link';
import { countTransactionCategories, formatAmount } from '@/lib/utils';
import { CategoryCount, RightSidebarProps } from '@/types';

const RightSidebar = ({ user, transactions, banks }: RightSidebarProps) => {
  const categories: CategoryCount[] = countTransactionCategories(transactions).slice(0, 4);
  const primaryAccount = banks[0];

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

      <section className="kape-rail__section">
        <div className="kape-rail__heading">
          <div>
            <h2>Primary account</h2>
            <p>SQL Server demo data</p>
          </div>
          <Link href="/my-banks">View all</Link>
        </div>

        {primaryAccount ? (
          <Link href={`/transaction-history?id=${primaryAccount.id}`} className="kape-account-summary">
            <div className="kape-account-summary__top">
              <span>{primaryAccount.name}</span>
              <small>Demo</small>
            </div>
            <strong>{formatAmount(primaryAccount.currentBalance)}</strong>
            <div className="kape-account-summary__bottom">
              <span>•••• {primaryAccount.mask}</span>
              <span>{primaryAccount.currency}</span>
            </div>
          </Link>
        ) : (
          <p className="kape-empty-small">No demo accounts available.</p>
        )}
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
