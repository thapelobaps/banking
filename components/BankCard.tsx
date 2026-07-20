import { formatAmount } from '@/lib/utils';
import Image from 'next/image';
import Link from 'next/link';
import Copy from './Copy';
import { CreditCardProps } from '@/types';

const BankCard = ({ account, userName, showBalance = true }: CreditCardProps) => {
  return (
    <div className="flex w-full max-w-[380px] flex-col gap-3">
      <Link
        href={`/transaction-history/?id=${account.id}`}
        className="group relative min-h-[230px] overflow-hidden rounded-[28px] bg-gradient-to-br from-[#2b1811] via-[#563326] to-[#7a4a37] p-6 text-white shadow-[0_24px_60px_-28px_rgba(61,34,24,0.75)] transition duration-300 hover:-translate-y-1 hover:shadow-[0_28px_70px_-24px_rgba(61,34,24,0.85)]"
      >
        <div className="absolute -right-16 -top-20 size-56 rounded-full border border-white/10 bg-white/5" />
        <div className="absolute -bottom-24 -left-16 size-64 rounded-full border border-white/10 bg-black/10" />

        <div className="relative z-10 flex h-full min-h-[182px] flex-col justify-between">
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-xs font-medium uppercase tracking-[0.2em] text-white/60">Kape demo account</p>
              <h2 className="mt-2 text-xl font-semibold tracking-tight">{account.name}</h2>
            </div>
            <span className="rounded-full border border-white/20 bg-white/10 px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] text-white/85 backdrop-blur">
              Demo
            </span>
          </div>

          <div>
            <p className="text-xs font-medium text-white/60">Current balance</p>
            <p className="mt-1 text-3xl font-semibold tracking-tight tabular-nums">
              {formatAmount(account.currentBalance)}
            </p>
          </div>

          <div className="flex items-end justify-between gap-4">
            <div>
              <p className="text-xs font-medium text-white/60">{userName}</p>
              <p className="mt-1 font-mono text-sm tracking-[0.18em] text-white/90">
                •••• •••• •••• {account.mask}
              </p>
            </div>
            <div className="flex items-center gap-3 opacity-85">
              <Image src="/icons/Paypass.svg" width={18} height={22} alt="Contactless" className="brightness-0 invert" />
              <Image src="/icons/mastercard.svg" width={40} height={28} alt="Card network" />
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
