'use client';

import Link from 'next/link';
import BankCard from './BankCard';
import { countTransactionCategories } from '@/lib/utils';
import { CategoryCount, RightSidebarProps } from '@/types';

const RightSidebar = ({ user, transactions, banks }: RightSidebarProps) => {
  const categories: CategoryCount[] = countTransactionCategories(transactions).slice(0, 4);

  return (
    <aside className="right-sidebar bg-white">
      <section className="border-b border-[#eee5df] p-4">
        <div className="rounded-[18px] bg-[#fbf7f4] p-3.5">
          <div className="flex items-center gap-3">
            <div className="flex size-9 items-center justify-center rounded-xl bg-[#4a2b20] text-sm font-bold text-white shadow-sm">
              {user.firstName[0]}{user.lastName[0]}
            </div>
            <div className="min-w-0">
              <p className="truncate text-xs font-semibold text-[#2b1a14]">{user.firstName} {user.lastName}</p>
              <p className="mt-0.5 truncate text-[10px] text-[#8a756b]">{user.email}</p>
            </div>
          </div>
          <div className="mt-3 flex items-center justify-between rounded-xl bg-white px-3 py-2 shadow-sm">
            <div>
              <p className="text-[8px] font-semibold uppercase tracking-[0.14em] text-[#9a8378]">Workspace</p>
              <p className="mt-0.5 text-[11px] font-semibold text-[#4a2b20]">Personal demo</p>
            </div>
            <span className="size-1.5 rounded-full bg-emerald-500" />
          </div>
        </div>
      </section>

      <section className="space-y-5 p-4">
        <div>
          <div className="mb-2.5 flex items-center justify-between">
            <div>
              <h2 className="text-xs font-semibold text-[#2b1a14]">Primary account</h2>
              <p className="mt-0.5 text-[10px] text-[#8a756b]">SQL Server demo data</p>
            </div>
            <Link href="/my-banks" className="text-[10px] font-semibold text-[#7a4a37]">View all</Link>
          </div>

          {banks.length > 0 ? (
            <BankCard
              key={banks[0].id}
              account={banks[0]}
              userName={`${user.firstName} ${user.lastName}`}
              showBalance={false}
            />
          ) : (
            <p className="rounded-xl bg-[#fbf7f4] p-3 text-[11px] text-[#8a756b]">No demo accounts available.</p>
          )}
        </div>

        <div>
          <div className="mb-2.5">
            <h2 className="text-xs font-semibold text-[#2b1a14]">Spending categories</h2>
            <p className="mt-0.5 text-[10px] text-[#8a756b]">Based on recent activity</p>
          </div>

          <div className="space-y-2">
            {categories.length > 0 ? (
              categories.map((category, index) => {
                const percentage = Math.max(12, Math.round((category.count / category.totalCount) * 100));
                return (
                  <div key={category.name} className="rounded-xl border border-[#eee5df] bg-white p-2.5">
                    <div className="flex items-center justify-between gap-2">
                      <div className="flex min-w-0 items-center gap-2">
                        <span className="flex size-7 shrink-0 items-center justify-center rounded-lg bg-[#f3ebe6] text-[10px] font-semibold text-[#6b4435]">
                          {index + 1}
                        </span>
                        <p className="truncate text-[11px] font-medium text-[#3b251d]">{category.name}</p>
                      </div>
                      <span className="text-[10px] font-semibold text-[#8a756b]">{category.count}</span>
                    </div>
                    <div className="mt-2 h-1 overflow-hidden rounded-full bg-[#f1e8e3]">
                      <div className="h-full rounded-full bg-[#8b5e4c]" style={{ width: `${percentage}%` }} />
                    </div>
                  </div>
                );
              })
            ) : (
              <p className="rounded-xl bg-[#fbf7f4] p-3 text-[11px] text-[#8a756b]">No transaction categories yet.</p>
            )}
          </div>
        </div>
      </section>
    </aside>
  );
};

export default RightSidebar;
