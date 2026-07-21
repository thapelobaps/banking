'use client';

import { Account } from '@/types';
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectSeparator,
  SelectTrigger,
  SelectValue,
} from './ui/select';

export const demoRecipients = [
  { id: '11111111-1111-4111-8111-111111111111', label: 'FNB Demo Recipient', mask: '9021', accountType: 'transaction' },
  { id: '22222222-2222-4222-8222-222222222222', label: 'Absa Demo Recipient', mask: '4816', accountType: 'savings' },
  { id: '33333333-3333-4333-8333-333333333333', label: 'Standard Bank Demo Recipient', mask: '7754', accountType: 'transaction' },
] as const;

type RecipientDropdownProps = {
  accounts: Account[];
  senderAccountId: string;
  value: string;
  onChange: (value: string) => void;
};

const RecipientDropdown = ({
  accounts,
  senderAccountId,
  value,
  onChange,
}: RecipientDropdownProps) => {
  const ownRecipients = accounts.filter((account) => account.id !== senderAccountId);

  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger className="mt-2.5 h-12 w-full rounded-xl border-[#ddcec5] bg-white px-3 text-left text-[#2b1a14] shadow-none focus:ring-[#7a4a37]">
        <SelectValue placeholder="Select a recipient" />
      </SelectTrigger>
      <SelectContent className="rounded-2xl border-[#eadfd8] bg-white p-1 shadow-xl">
        {ownRecipients.length > 0 && (
          <SelectGroup>
            <SelectLabel className="px-3 py-2 text-xs font-semibold uppercase tracking-[0.14em] text-[#9a8378]">
              My Kape accounts
            </SelectLabel>
            {ownRecipients.map((account) => (
              <SelectItem
                key={account.id}
                value={account.id}
                textValue={`${account.name} ending ${account.mask}`}
                className="cursor-pointer rounded-xl px-3 py-3 focus:bg-[#f8f3ef]"
              >
                <span className="flex min-w-0 flex-col">
                  <span className="truncate text-sm font-semibold text-[#2b1a14]">{account.name}</span>
                  <span className="mt-1 text-xs text-[#9a8378]">
                    •••• {account.mask} · {account.subtype}
                  </span>
                </span>
              </SelectItem>
            ))}
          </SelectGroup>
        )}

        {ownRecipients.length > 0 && <SelectSeparator className="my-1 bg-[#eadfd8]" />}

        <SelectGroup>
          <SelectLabel className="px-3 py-2 text-xs font-semibold uppercase tracking-[0.14em] text-[#9a8378]">
            Demo recipients
          </SelectLabel>
          {demoRecipients.map((recipient) => (
            <SelectItem
              key={recipient.id}
              value={recipient.id}
              textValue={`${recipient.label} ending ${recipient.mask}`}
              className="cursor-pointer rounded-xl px-3 py-3 focus:bg-[#f8f3ef]"
            >
              <span className="flex min-w-0 flex-col">
                <span className="truncate text-sm font-semibold text-[#2b1a14]">{recipient.label}</span>
                <span className="mt-1 text-xs text-[#9a8378]">
                  •••• {recipient.mask} · {recipient.accountType}
                </span>
              </span>
            </SelectItem>
          ))}
        </SelectGroup>
      </SelectContent>
    </Select>
  );
};

export default RecipientDropdown;
