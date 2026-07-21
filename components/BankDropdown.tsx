'use client';

import Image from 'next/image';
import { useSearchParams, useRouter } from 'next/navigation';
import { useState } from 'react';
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { formUrlQuery, formatAmount } from '@/lib/utils';
import { Account, BankDropdownProps } from '@/types';

export const BankDropdown = ({ accounts = [], setValue, otherStyles }: BankDropdownProps) => {
  const searchParams = useSearchParams();
  const router = useRouter();
  const [selected, setSelected] = useState(accounts[0]);

  const handleBankChange = (id: string) => {
    const account = accounts.find((candidate) => candidate.id === id);
    if (!account) return;

    setSelected(account);
    const newUrl = formUrlQuery({
      params: searchParams.toString(),
      key: 'id',
      value: id,
    });
    router.push(newUrl, { scroll: false });
    setValue?.('senderBank', id);
  };

  const selectedLabel = selected
    ? `${selected.name} · •••• ${selected.mask}`
    : 'Select an account';

  return (
    <Select value={selected?.id} onValueChange={handleBankChange}>
      <SelectTrigger className={`flex h-12 w-full gap-3 rounded-xl border-[#ddcec5] bg-white px-3 text-[#2b1a14] shadow-none focus:ring-[#7a4a37] md:w-[340px] ${otherStyles}`}>
        <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-[#f3ebe6]">
          <Image src="/icons/credit-card.svg" width={17} height={17} alt="" className="opacity-75" />
        </span>
        <SelectValue placeholder="Select an account">{selectedLabel}</SelectValue>
      </SelectTrigger>

      <SelectContent className={`w-full rounded-2xl border-[#eadfd8] bg-white p-1 shadow-xl md:w-[340px] ${otherStyles}`} align="end">
        <SelectGroup>
          <SelectLabel className="px-3 py-2 text-xs font-semibold uppercase tracking-[0.14em] text-[#9a8378]">
            Select an account
          </SelectLabel>
          {accounts.map((account: Account) => (
            <SelectItem
              key={account.id}
              value={account.id}
              textValue={`${account.name} ending ${account.mask}`}
              className="cursor-pointer rounded-xl border-t-0 px-3 py-3 focus:bg-[#f8f3ef]"
            >
              <span className="flex w-full min-w-0 items-center justify-between gap-3">
                <span className="min-w-0">
                  <span className="block truncate text-sm font-semibold text-[#2b1a14]">{account.name}</span>
                  <span className="mt-1 block text-xs text-[#9a8378]">•••• {account.mask}</span>
                </span>
                <span className="shrink-0 text-sm font-semibold text-[#6b4435]">
                  {formatAmount(account.currentBalance)}
                </span>
              </span>
            </SelectItem>
          ))}
        </SelectGroup>
      </SelectContent>
    </Select>
  );
};
