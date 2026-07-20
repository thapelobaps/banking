import { TotalBalanceBoxProps } from '@/types';
import AnimatedCounter from './AnimatedCounter';
import { formatAmount } from '@/lib/utils';

const TotalBalanceBox = ({ accounts = [], totalBanks, totalCurrentBalance }: TotalBalanceBoxProps) => {
  const totalAvailableBalance = accounts.reduce(
    (sum, account) => sum + account.availableBalance,
    0
  );

  return (
    <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      <article className="rounded-3xl bg-[#4a2b20] p-6 text-white shadow-[0_18px_50px_-30px_rgba(74,43,32,0.9)]">
        <div className="flex items-center justify-between">
          <p className="text-sm font-medium text-white/65">Total balance</p>
          <span className="rounded-full bg-white/10 px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-white/75">ZAR</span>
        </div>
        <p className="mt-6 text-3xl font-semibold tracking-tight sm:text-4xl">
          <AnimatedCounter amount={totalCurrentBalance} />
        </p>
        <p className="mt-3 text-xs text-white/55">Across all Kape demo accounts</p>
      </article>

      <article className="rounded-3xl border border-[#eadfd8] bg-white p-6 shadow-sm">
        <p className="text-sm font-medium text-[#8a756b]">Available balance</p>
        <p className="mt-6 text-3xl font-semibold tracking-tight text-[#2b1a14] tabular-nums">
          {formatAmount(totalAvailableBalance)}
        </p>
        <div className="mt-4 h-1.5 overflow-hidden rounded-full bg-[#f1e8e3]">
          <div className="h-full w-[72%] rounded-full bg-[#8b5e4c]" />
        </div>
      </article>

      <article className="rounded-3xl border border-[#eadfd8] bg-[#fbf7f4] p-6 shadow-sm sm:col-span-2 xl:col-span-1">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-sm font-medium text-[#8a756b]">Active accounts</p>
            <p className="mt-5 text-4xl font-semibold tracking-tight text-[#2b1a14]">{totalBanks}</p>
          </div>
          <div className="flex size-11 items-center justify-center rounded-2xl bg-white text-[#5b382a] shadow-sm">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
              <rect x="3" y="5" width="18" height="14" rx="3" />
              <path d="M3 10h18" />
            </svg>
          </div>
        </div>
        <p className="mt-3 text-xs leading-5 text-[#8a756b]">SQL-backed demo banking environment</p>
      </article>
    </section>
  );
};

export default TotalBalanceBox;
