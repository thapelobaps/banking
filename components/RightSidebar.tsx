'use client';

import BankCard from './BankCard';
import { countTransactionCategories } from '@/lib/utils';
import Category from './Category';
import { CategoryCount, RightSidebarProps } from '@/types';

const RightSidebar = ({ user, transactions, banks }: RightSidebarProps) => {
  const categories: CategoryCount[] = countTransactionCategories(transactions);

  return (
    <aside className="right-sidebar">
      <section className="flex flex-col pb-8">
        <div className="profile-banner" />
        <div className="profile">
          <div className="profile-img">
            <span className="text-5xl font-bold text-blue-500">{user.firstName[0]}</span>
          </div>
          <div className="profile-details">
            <h1 className="profile-name">
              {user.firstName} {user.lastName}
            </h1>
            <p className="profile-email">{user.email}</p>
          </div>
        </div>
      </section>

      <section className="banks">
        <div className="flex w-full justify-between">
          <h2 className="header-2">Demo accounts</h2>
          <span className="text-12 font-medium text-gray-500">SQL Server</span>
        </div>

        {banks.length > 0 ? (
          <div className="relative flex flex-1 flex-col items-center justify-center gap-5">
            <div className="relative z-10">
              <BankCard
                key={banks[0].id}
                account={banks[0]}
                userName={`${user.firstName} ${user.lastName}`}
                showBalance={false}
              />
            </div>
            {banks[1] && (
              <div className="absolute right-0 top-8 z-0 w-[90%]">
                <BankCard
                  key={banks[1].id}
                  account={banks[1]}
                  userName={`${user.firstName} ${user.lastName}`}
                  showBalance={false}
                />
              </div>
            )}
          </div>
        ) : (
          <p className="mt-4 text-sm text-gray-500">No demo accounts available.</p>
        )}

        <div className="mt-10 flex flex-1 flex-col gap-6">
          <h2 className="header-2">Top categories</h2>
          <div className="space-y-5">
            {categories.length > 0 ? (
              categories.map((category) => (
                <Category key={category.name} category={category} />
              ))
            ) : (
              <p className="text-sm text-gray-500">No transaction categories yet.</p>
            )}
          </div>
        </div>
      </section>
    </aside>
  );
};

export default RightSidebar;
