import { formatAmount } from '@/lib/utils';
import Image from 'next/image';
import Link from 'next/link';
import Copy from './Copy';
import { CreditCardProps } from '@/types';

const BankCard = ({ account, userName, showBalance = true }: CreditCardProps) => {
  return (
    <div className="flex w-full max-w-[320px] flex-col gap-2.5">
      <Link
        href={`/transaction-history/?id=${account.id}`}
        className="group relative min-h-[184px] overflow-hidden rounded-[22px] bg-gradient-to-br from-[#2b1811] via-[#563326] to-[#7a4a37] p-[18px] text-white shadow-[0_18px_44px_-26px_rgba(61,34,24,0.75)] transition duration-300 hover:-translate-y-0.5 hover:shadow-[0_22px_50px_-24px_rgba(61,34,24,0.82)]"
      >
        <div className="absolute -right-14 -top-16 size-44 rounded-full border border-white/10 bg-white/5" />
        <div className="absolute -bottom-20 -left-14 size-52 rounded-full border border-white/10 bg-black/10" />

        <div className="relative z-10 flex min-h-[148px] flex-col justify-between">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-[9px] font-medium uppercase tracking-[0.18em] text-white/60">Kape demo account</p>
              <h2 className="mt-1.5 text-base font-semibold tracking-tight">{account.name}</h2>
            </div>
            <span className="rounded-full border border-white/20 bg-white/10 px-2.5 py-1 text-[8px] font-semibold uppercase tracking-[0.16em] text-white/85 backdrop-blur">
              Demo
            </span>
          </div>

          <div>
            <p className="text-[10px] font-medium text-white/60">Current balance</p>
            <p className="mt-0.5 text-2xl font-semibold tracking-tight tabular-nums">
              {formatAmount(account.currentBalance)}
            </p>
          </div>

          <div className="flex items-end justify-between gap-3">
            <div>
              <p className="text-[10px] font-medium text-white/60">{userName}</p>
              <p className="mt-1 font-mono text-xs tracking-[0.14em] text-white/90">
                •••• •••• •••• {account.mask}
              </p>
            </div>
            <div className="flex items-center gap-2 opacity-85">
              <Image src="/icons/Paypass.svg" width={15} height={18} alt="Contactless" className="brightness-0 invert" />
              <Image src="/icons/mastercard.svg" width={34} height={24} alt="Card network" />
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
