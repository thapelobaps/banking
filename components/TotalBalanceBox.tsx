import { TotalBalanceBoxProps } from '@/types';
import AnimatedCounter from './AnimatedCounter';
import { formatAmount } from '@/lib/utils';

const TotalBalanceBox = ({ accounts = [], totalBanks, totalCurrentBalance }: TotalBalanceBoxProps) => {
  const totalAvailableBalance = accounts.reduce(
    (sum, account) => sum + account.availableBalance,
    0
  );

  return (
    <section className="kape-balance-strip grid grid-cols-3 gap-2 md:gap-3">
      <article className="kape-balance-card min-w-0 rounded-[14px] bg-[#4a2b20] p-2.5 text-white shadow-[0_14px_38px_-26px_rgba(74,43,32,0.9)] md:rounded-[20px] md:p-4">
        <div className="flex items-center justify-between gap-1.5">
          <p className="min-w-0 text-[8px] font-medium leading-3 text-white/65 md:text-xs">Total balance</p>
          <span className="shrink-0 rounded-full bg-white/10 px-1.5 py-0.5 text-[6px] font-semibold uppercase tracking-[0.12em] text-white/75 md:px-2.5 md:py-1 md:text-[8px] md:tracking-[0.16em]">ZAR</span>
        </div>
        <p className="mt-3 whitespace-nowrap text-[12px] font-semibold leading-tight tracking-tight md:mt-4 md:text-2xl">
          <AnimatedCounter amount={totalCurrentBalance} />
        </p>
        <p className="mt-2 text-[7px] leading-[10px] text-white/55 md:text-[10px] md:leading-4">Across all Kape demo accounts</p>
      </article>

      <article className="kape-balance-card min-w-0 rounded-[14px] border border-[#eadfd8] bg-white p-2.5 shadow-sm md:rounded-[20px] md:p-4">
        <p className="text-[8px] font-medium leading-3 text-[#8a756b] md:text-xs">Available balance</p>
        <p className="mt-3 whitespace-nowrap text-[12px] font-semibold leading-tight tracking-tight text-[#2b1a14] tabular-nums md:mt-4 md:text-2xl">
          {formatAmount(totalAvailableBalance)}
        </p>
        <div className="mt-3 h-1 overflow-hidden rounded-full bg-[#f1e8e3]">
          <div className="h-full w-[72%] rounded-full bg-[#8b5e4c]" />
        </div>
      </article>

      <article className="kape-balance-card min-w-0 rounded-[14px] border border-[#eadfd8] bg-[#fbf7f4] p-2.5 shadow-sm md:rounded-[20px] md:p-4">
        <div className="flex items-start justify-between gap-1.5">
          <div className="min-w-0">
            <p className="text-[8px] font-medium leading-3 text-[#8a756b] md:text-xs">Active accounts</p>
            <p className="mt-2 text-xl font-semibold leading-none tracking-tight text-[#2b1a14] md:mt-3 md:text-3xl">{totalBanks}</p>
          </div>
          <div className="hidden size-9 shrink-0 items-center justify-center rounded-xl bg-white text-[#5b382a] shadow-sm md:flex">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
              <rect x="3" y="5" width="18" height="14" rx="3" />
              <path d="M3 10h18" />
            </svg>
          </div>
        </div>
        <p className="mt-2 text-[7px] leading-[10px] text-[#8a756b] md:text-[10px] md:leading-4">SQL-backed demo banking environment</p>
      </article>
    </section>
  );
};

export default TotalBalanceBox;
