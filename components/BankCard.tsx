import { formatAmount } from '@/lib/utils';
import Image from 'next/image';
import Link from 'next/link';
import Copy from './Copy';
import { CreditCardProps } from '@/types';

const BankCard = ({ account, userName, showBalance = true }: CreditCardProps) => {
  return (
    <div className="kape-bank-card flex w-full max-w-[270px] flex-col gap-2">
      <Link
        href={`/transaction-history/?id=${account.id}`}
        className="kape-bank-card__surface group relative min-h-[156px] overflow-hidden rounded-[18px] bg-gradient-to-br from-[#2b1811] via-[#563326] to-[#7a4a37] p-[14px] text-white shadow-[0_15px_36px_-24px_rgba(61,34,24,0.75)] transition duration-300 hover:-translate-y-0.5 hover:shadow-[0_18px_42px_-22px_rgba(61,34,24,0.82)]"
      >
        <div className="absolute -right-12 -top-14 size-36 rounded-full border border-white/10 bg-white/5" />
        <div className="absolute -bottom-16 -left-12 size-44 rounded-full border border-white/10 bg-black/10" />

        <div className="kape-bank-card__content relative z-10 flex min-h-[128px] flex-col justify-between">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-[7px] font-medium uppercase tracking-[0.16em] text-white/60">Kape demo account</p>
              <h2 className="mt-1 truncate text-sm font-semibold tracking-tight">{account.name}</h2>
            </div>
            <span className="shrink-0 rounded-full border border-white/20 bg-white/10 px-2 py-0.5 text-[7px] font-semibold uppercase tracking-[0.14em] text-white/85 backdrop-blur">
              Demo
            </span>
          </div>

          <div>
            <p className="text-[8px] font-medium text-white/60">Current balance</p>
            <p className="mt-0.5 truncate text-xl font-semibold tracking-tight tabular-nums">
              {formatAmount(account.currentBalance)}
            </p>
          </div>

          <div className="flex items-end justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-[8px] font-medium text-white/60">{userName}</p>
              <p className="mt-0.5 truncate font-mono text-[9px] tracking-[0.12em] text-white/90">
                •••• •••• •••• {account.mask}
              </p>
            </div>
            <div className="flex shrink-0 items-center gap-1.5 opacity-85">
              <Image src="/icons/Paypass.svg" width={12} height={15} alt="Contactless" className="brightness-0 invert" />
              <Image src="/icons/mastercard.svg" width={28} height={20} alt="Card network" />
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
