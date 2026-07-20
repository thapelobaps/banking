'use client';

import Link from 'next/link';
import BankCard from './BankCard';
import { countTransactionCategories } from '@/lib/utils';
import { CategoryCount, RightSidebarProps } from '@/types';

const RightSidebar = ({ user, transactions, banks }: RightSidebarProps) => {
  const categories: CategoryCount[] = countTransactionCategories(transactions).slice(0, 4);

  return (
    <aside className="right-sidebar bg-white">
      <section className="border-b border-[#eee5df] p-6">
        <div className="rounded-3xl bg-[#fbf7f4] p-5">
          <div className="flex items-center gap-4">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-[#4a2b20] text-lg font-bold text-white shadow-sm">
              {user.firstName[0]}{user.lastName[0]}
            </div>
            <div className="min-w-0">
              <p className="truncate font-semibold text-[#2b1a14]">{user.firstName} {user.lastName}</p>
              <p className="mt-1 truncate text-xs text-[#8a756b]">{user.email}</p>
            </div>
          </div>
          <div className="mt-5 flex items-center justify-between rounded-2xl bg-white px-4 py-3 shadow-sm">
            <div>
              <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-[#9a8378]">Workspace</p>
              <p className="mt-1 text-sm font-semibold text-[#4a2b20]">Personal demo</p>
            </div>
            <span className="size-2 rounded-full bg-emerald-500" />
          </div>
        </div>
      </section>

      <section className="space-y-8 p-6">
        <div>
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h2 className="font-semibold text-[#2b1a14]">Primary account</h2>
              <p className="mt-1 text-xs text-[#8a756b]">SQL Server demo data</p>
            </div>
            <Link href="/my-banks" className="text-xs font-semibold text-[#7a4a37]">View all</Link>
          </div>

          {banks.length > 0 ? (
            <BankCard
              key={banks[0].id}
              account={banks[0]}
              userName={`${user.firstName} ${user.lastName}`}
              showBalance={false}
            />
          ) : (
            <p className="rounded-2xl bg-[#fbf7f4] p-4 text-sm text-[#8a756b]">No demo accounts available.</p>
          )}
        </div>

        <div>
          <div className="mb-4 flex items-center justify-between">
            <div>
              <h2 className="font-semibold text-[#2b1a14]">Spending categories</h2>
              <p className="mt-1 text-xs text-[#8a756b]">Based on recent activity</p>
            </div>
          </div>

          <div className="space-y-3">
            {categories.length > 0 ? (
              categories.map((category, index) => {
                const percentage = Math.max(12, Math.round((category.count / category.totalCount) * 100));
                return (
                  <div key={category.name} className="rounded-2xl border border-[#eee5df] bg-white p-4">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex min-w-0 items-center gap-3">
                        <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-[#f3ebe6] text-sm font-semibold text-[#6b4435]">
                          {index + 1}
                        </span>
                        <p className="truncate text-sm font-medium text-[#3b251d]">{category.name}</p>
                      </div>
                      <span className="text-xs font-semibold text-[#8a756b]">{category.count}</span>
                    </div>
                    <div className="mt-3 h-1.5 overflow-hidden rounded-full bg-[#f1e8e3]">
                      <div className="h-full rounded-full bg-[#8b5e4c]" style={{ width: `${percentage}%` }} />
                    </div>
                  </div>
                );
              })
            ) : (
              <p className="rounded-2xl bg-[#fbf7f4] p-4 text-sm text-[#8a756b]">No transaction categories yet.</p>
            )}
          </div>
        </div>
      </section>
    </aside>
  );
};

export default RightSidebar;
