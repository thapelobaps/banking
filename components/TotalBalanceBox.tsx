import { TotalBalanceBoxProps } from '@/types';
import AnimatedCounter from './AnimatedCounter';
import { formatAmount } from '@/lib/utils';

const TotalBalanceBox = ({ accounts = [], totalBanks, totalCurrentBalance }: TotalBalanceBoxProps) => {
  const totalAvailableBalance = accounts.reduce(
    (sum, account) => sum + account.availableBalance,
    0
  );

  return (
    <section className="grid grid-cols-1 gap-3 md:grid-cols-3">
      <article className="min-w-0 rounded-[20px] bg-[#4a2b20] p-4 text-white shadow-[0_14px_38px_-26px_rgba(74,43,32,0.9)]">
        <div className="flex items-center justify-between gap-3">
          <p className="truncate text-xs font-medium text-white/65">Total balance</p>
          <span className="shrink-0 rounded-full bg-white/10 px-2.5 py-1 text-[8px] font-semibold uppercase tracking-[0.16em] text-white/75">ZAR</span>
        </div>
        <p className="mt-4 truncate text-2xl font-semibold tracking-tight sm:text-3xl">
          <AnimatedCounter amount={totalCurrentBalance} />
        </p>
        <p className="mt-2 truncate text-[10px] text-white/55">Across all Kape demo accounts</p>
      </article>

      <article className="min-w-0 rounded-[20px] border border-[#eadfd8] bg-white p-4 shadow-sm">
        <p className="truncate text-xs font-medium text-[#8a756b]">Available balance</p>
        <p className="mt-4 truncate text-2xl font-semibold tracking-tight text-[#2b1a14] tabular-nums sm:text-3xl">
          {formatAmount(totalAvailableBalance)}
        </p>
        <div className="mt-3 h-1 overflow-hidden rounded-full bg-[#f1e8e3]">
          <div className="h-full w-[72%] rounded-full bg-[#8b5e4c]" />
        </div>
      </article>

      <article className="min-w-0 rounded-[20px] border border-[#eadfd8] bg-[#fbf7f4] p-4 shadow-sm">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="truncate text-xs font-medium text-[#8a756b]">Active accounts</p>
            <p className="mt-3 truncate text-3xl font-semibold tracking-tight text-[#2b1a14]">{totalBanks}</p>
          </div>
          <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-white text-[#5b382a] shadow-sm">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
              <rect x="3" y="5" width="18" height="14" rx="3" />
              <path d="M3 10h18" />
            </svg>
          </div>
        </div>
        <p className="mt-2 truncate text-[10px] leading-4 text-[#8a756b]">SQL-backed demo banking environment</p>
      </article>
    </section>
  );
};

export default TotalBalanceBox;
