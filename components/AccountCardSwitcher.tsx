'use client';

import Image from 'next/image';
import Link from 'next/link';
import {
  Check,
  ChevronLeft,
  ChevronRight,
  Copy,
  Eye,
  EyeOff,
  ReceiptText,
  Send,
  ShieldCheck,
} from 'lucide-react';
import { useState } from 'react';

import { formatAmount } from '@/lib/utils';
import { Account } from '@/types';

type AccountCardSwitcherProps = {
  accounts: Account[];
  userName: string;
};

const AccountCardSwitcher = ({ accounts, userName }: AccountCardSwitcherProps) => {
  const [activeIndex, setActiveIndex] = useState(0);
  const [showBalance, setShowBalance] = useState(true);
  const [copyState, setCopyState] = useState<'idle' | 'copied' | 'failed'>('idle');

  if (accounts.length === 0) {
    return <p className="kape-empty-small">No demo accounts available.</p>;
  }

  const safeIndex = Math.min(activeIndex, accounts.length - 1);
  const account = accounts[safeIndex];

  const move = (direction: -1 | 1) => {
    setActiveIndex((current) => (current + direction + accounts.length) % accounts.length);
    setCopyState('idle');
  };

  const copyReference = async () => {
    try {
      await navigator.clipboard.writeText(account.demoReference);
      setCopyState('copied');
    } catch {
      setCopyState('failed');
    }

    window.setTimeout(() => setCopyState('idle'), 1800);
  };

  return (
    <div className="space-y-2.5">
      <div className="flex items-center justify-between gap-2">
        <div>
          <p className="text-[8px] font-semibold uppercase tracking-[0.16em] text-[#9a8378]">
            Active card
          </p>
          <p className="mt-0.5 text-[9px] font-semibold text-[#3b251d]">
            {safeIndex + 1} of {accounts.length}
          </p>
        </div>

        <div className="flex items-center gap-1">
          <button
            type="button"
            onClick={() => move(-1)}
            disabled={accounts.length < 2}
            className="flex size-7 items-center justify-center rounded-lg border border-[#eadfd8] bg-white text-[#6b4435] transition hover:bg-[#f8f3ef] disabled:cursor-not-allowed disabled:opacity-40"
            aria-label="Show previous account"
          >
            <ChevronLeft size={14} />
          </button>
          <button
            type="button"
            onClick={() => move(1)}
            disabled={accounts.length < 2}
            className="flex size-7 items-center justify-center rounded-lg border border-[#eadfd8] bg-white text-[#6b4435] transition hover:bg-[#f8f3ef] disabled:cursor-not-allowed disabled:opacity-40"
            aria-label="Show next account"
          >
            <ChevronRight size={14} />
          </button>
        </div>
      </div>

      <article className="relative min-h-[144px] overflow-hidden rounded-2xl bg-gradient-to-br from-[#24130e] via-[#563326] to-[#8a5a46] p-3.5 text-white shadow-[0_16px_35px_rgba(74,43,32,0.22)]">
        <div className="absolute -right-8 -top-10 size-28 rounded-full border border-white/10 bg-white/5" />
        <div className="absolute -bottom-12 -left-8 size-32 rounded-full border border-white/10 bg-black/5" />

        <div className="relative flex min-h-[116px] flex-col justify-between">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="text-[7px] font-semibold uppercase tracking-[0.16em] text-white/55">
                Kape demo account
              </p>
              <h3 className="mt-1 truncate text-[11px] font-semibold">{account.name}</h3>
              <p className="mt-0.5 text-[8px] capitalize text-white/60">{account.subtype} account</p>
            </div>
            <div className="flex items-center gap-1">
              <span className="rounded-full border border-white/15 bg-white/10 px-2 py-1 text-[7px] font-bold uppercase tracking-[0.08em]">
                Demo
              </span>
              <button
                type="button"
                onClick={() => setShowBalance((current) => !current)}
                className="flex size-6 items-center justify-center rounded-full border border-white/15 bg-white/10 text-white/75 transition hover:bg-white/20"
                aria-label={showBalance ? 'Hide account balance' : 'Show account balance'}
              >
                {showBalance ? <EyeOff size={12} /> : <Eye size={12} />}
              </button>
            </div>
          </div>

          <div className="mt-3">
            <p className="text-[7px] font-medium uppercase tracking-[0.12em] text-white/50">Current balance</p>
            <p className="mt-1 text-[18px] font-semibold tracking-tight">
              {showBalance ? formatAmount(account.currentBalance) : 'R ••••••'}
            </p>
            <p className="mt-1 text-[8px] text-white/55">
              Available {showBalance ? formatAmount(account.availableBalance) : 'R ••••••'}
            </p>
          </div>

          <div className="mt-3 flex items-end justify-between gap-2">
            <div className="min-w-0">
              <p className="truncate text-[8px] font-medium text-white/72">{userName}</p>
              <p className="mt-1 text-[8px] tracking-[0.12em] text-white/65">•••• •••• •••• {account.mask}</p>
            </div>
            <span className="flex items-center gap-1.5">
              <Image src="/icons/Paypass.svg" width={12} height={15} alt="Contactless" className="brightness-0 invert opacity-80" />
              <Image src="/icons/mastercard.svg" width={25} height={17} alt="Card network" />
            </span>
          </div>
        </div>
      </article>

      {accounts.length > 1 && (
        <div className="flex items-center justify-center gap-1.5" aria-label="Account card position">
          {accounts.map((candidate, index) => (
            <button
              key={candidate.id}
              type="button"
              onClick={() => setActiveIndex(index)}
              className={`h-1.5 rounded-full transition-all ${index === safeIndex ? 'w-5 bg-[#6b4435]' : 'w-1.5 bg-[#d9c9bf]'}`}
              aria-label={`Show ${candidate.name}`}
              aria-current={index === safeIndex ? 'true' : undefined}
            />
          ))}
        </div>
      )}

      <div className="grid grid-cols-2 gap-1.5">
        <Link
          href={`/payment-transfer?id=${account.id}`}
          className="flex min-h-8 items-center justify-center gap-1.5 rounded-lg bg-[#4a2b20] px-2 text-[8px] font-semibold text-white transition hover:bg-[#382017]"
        >
          <Send size={11} /> Transfer
        </Link>
        <Link
          href={`/transaction-history?id=${account.id}`}
          className="flex min-h-8 items-center justify-center gap-1.5 rounded-lg border border-[#d9c9bf] bg-white px-2 text-[8px] font-semibold text-[#5b382a] transition hover:bg-[#f8f3ef]"
        >
          <ReceiptText size={11} /> Activity
        </Link>
      </div>

      <button
        type="button"
        onClick={copyReference}
        className="flex w-full items-center gap-2 rounded-xl border border-[#eadfd8] bg-[#fdfaf8] px-2.5 py-2 text-left transition hover:border-[#cdb9ad]"
      >
        <span className="flex size-6 shrink-0 items-center justify-center rounded-lg bg-[#f3ebe6] text-[#6b4435]">
          {copyState === 'copied' ? <Check size={12} /> : <Copy size={12} />}
        </span>
        <span className="min-w-0 flex-1">
          <span className="block text-[7px] font-semibold uppercase tracking-[0.14em] text-[#9a8378]">Demo reference</span>
          <span className="mt-0.5 block truncate font-mono text-[8px] font-medium text-[#3b251d]">
            {copyState === 'copied' ? 'Copied to clipboard' : copyState === 'failed' ? 'Copy failed' : account.demoReference}
          </span>
        </span>
      </button>

      <div className="flex items-center gap-2 rounded-xl border border-emerald-100 bg-emerald-50 px-2.5 py-2 text-emerald-800">
        <ShieldCheck size={13} className="shrink-0" />
        <p className="text-[7px] font-medium leading-3">Protected demo account · SQL Server backed</p>
      </div>
    </div>
  );
};

export default AccountCardSwitcher;
