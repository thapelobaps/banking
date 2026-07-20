import { formatAmount } from '@/lib/utils';
import Image from 'next/image';
import Link from 'next/link';
import Copy from './Copy';
import { CreditCardProps } from '@/types';

const BankCard = ({ account, userName, showBalance = true }: CreditCardProps) => {
  return (
    <div className="kape-bank-card flex w-full max-w-none flex-col gap-2 sm:max-w-[320px] lg:max-w-[270px]">
      <Link
        href={`/transaction-history/?id=${account.id}`}
        className="kape-bank-card__surface group relative min-h-[190px] overflow-hidden rounded-[22px] bg-gradient-to-br from-[#2b1811] via-[#563326] to-[#7a4a37] p-[18px] text-white shadow-[0_15px_36px_-24px_rgba(61,34,24,0.75)] transition duration-300 hover:-translate-y-0.5 hover:shadow-[0_18px_42px_-22px_rgba(61,34,24,0.82)] sm:min-h-[170px] sm:rounded-[20px] sm:p-4 lg:min-h-[156px] lg:rounded-[18px] lg:p-[14px]"
      >
        <div className="absolute -right-12 -top-14 size-36 rounded-full border border-white/10 bg-white/5" />
        <div className="absolute -bottom-16 -left-12 size-44 rounded-full border border-white/10 bg-black/10" />

        <div className="kape-bank-card__content relative z-10 flex min-h-[154px] flex-col justify-between sm:min-h-[138px] lg:min-h-[128px]">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-[9px] font-medium uppercase tracking-[0.16em] text-white/60 md:text-[7px]">Kape demo account</p>
              <h2 className="mt-1 truncate text-base font-semibold tracking-tight md:text-sm">{account.name}</h2>
            </div>
            <span className="shrink-0 rounded-full border border-white/20 bg-white/10 px-2.5 py-1 text-[8px] font-semibold uppercase tracking-[0.14em] text-white/85 backdrop-blur md:px-2 md:py-0.5 md:text-[7px]">
              Demo
            </span>
          </div>

          <div>
            <p className="text-[10px] font-medium text-white/60 md:text-[8px]">Current balance</p>
            <p className="mt-0.5 truncate text-2xl font-semibold tracking-tight tabular-nums md:text-xl">
              {formatAmount(account.currentBalance)}
            </p>
          </div>

          <div className="flex items-end justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-[10px] font-medium text-white/60 md:text-[8px]">{userName}</p>
              <p className="mt-1 truncate font-mono text-[11px] tracking-[0.12em] text-white/90 md:mt-0.5 md:text-[9px]">
                •••• •••• •••• {account.mask}
              </p>
            </div>
            <div className="flex shrink-0 items-center gap-2 opacity-85 md:gap-1.5">
              <Image src="/icons/Paypass.svg" width={15} height={18} alt="Contactless" className="h-auto w-[15px] brightness-0 invert md:w-[12px]" />
              <Image src="/icons/mastercard.svg" width={34} height={24} alt="Card network" className="h-auto w-[34px] md:w-[28px]" />
            </div>
          </div>
        </div>
      </Link>

      {showBalance && account.demoReference && (
        <Copy title={account.demoReference} label="Demo account reference" />
      )}
    </div>
  );
};

export default BankCard;
