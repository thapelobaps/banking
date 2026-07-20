'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, Loader2, ShieldCheck } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

import { createMockTransfer } from '@/lib/actions/bank.actions';
import { PaymentTransferFormProps } from '@/types';
import { BankDropdown } from './BankDropdown';
import { Button } from './ui/button';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from './ui/form';
import { Input } from './ui/input';
import { Textarea } from './ui/textarea';

const formSchema = z.object({
  name: z.string().trim().max(120, 'Transfer note must be 120 characters or fewer').optional().default(''),
  amount: z
    .string()
    .trim()
    .refine((value) => /^\d+(\.\d{1,2})?$/.test(value) && Number(value) > 0, 'Enter a valid amount greater than R0.00'),
  senderBank: z.string().uuid('Select a valid source account'),
  recipientReference: z.string().uuid('Enter a valid demo account reference'),
});

const PaymentTransferForm = ({ accounts }: PaymentTransferFormProps) => {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      amount: '',
      senderBank: accounts[0]?.id ?? '',
      recipientReference: '',
    },
  });

  const submit = async (data: z.infer<typeof formSchema>) => {
    setIsLoading(true);
    form.clearErrors('root');

    try {
      if (data.senderBank === data.recipientReference) {
        form.setError('recipientReference', { message: 'Choose a different recipient account' });
        return;
      }

      await createMockTransfer({
        senderBankId: data.senderBank,
        receiverBankId: data.recipientReference,
        amount: Number(data.amount),
        name: data.name || 'Demo transfer',
      });

      form.reset({
        name: '',
        amount: '',
        senderBank: accounts[0]?.id ?? '',
        recipientReference: '',
      });
      router.push('/');
      router.refresh();
    } catch (error) {
      form.setError('root', {
        message: error instanceof Error ? error.message : 'The simulated transfer failed. No real money was moved.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const sectionClass = 'rounded-xl border border-[#eee5df] bg-[#fdfaf8] p-3.5';
  const inputClass = 'h-10 rounded-lg border-[#ddcec5] bg-white text-sm text-[#2b1a14] placeholder:text-[#aa968c] focus-visible:ring-[#7a4a37]';

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(submit)} className="space-y-3.5">
        <div className="flex items-start gap-2.5 rounded-xl border border-amber-200 bg-amber-50 p-3 text-amber-900">
          <ShieldCheck className="mt-0.5 size-4 shrink-0" />
          <div>
            <p className="text-xs font-semibold">Demo transfer only</p>
            <p className="mt-0.5 text-[11px] leading-4 text-amber-800">
              This updates SQL Server demo balances only. No live bank connection or real money movement occurs.
            </p>
          </div>
        </div>

        <div className="grid gap-3.5 xl:grid-cols-2">
          <FormField
            control={form.control}
            name="senderBank"
            render={() => (
              <FormItem className={sectionClass}>
                <div className="mb-2.5">
                  <FormLabel className="text-xs font-semibold text-[#2b1a14]">From account</FormLabel>
                  <FormDescription className="mt-0.5 text-[11px] text-[#8a756b]">Select the demo account to debit.</FormDescription>
                </div>
                <FormControl>
                  <BankDropdown accounts={accounts} setValue={form.setValue} otherStyles="!w-full" />
                </FormControl>
                <FormMessage className="text-[10px] text-red-600" />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="recipientReference"
            render={({ field }) => (
              <FormItem className={sectionClass}>
                <FormLabel className="text-xs font-semibold text-[#2b1a14]">Recipient account reference</FormLabel>
                <FormDescription className="mt-0.5 text-[11px] text-[#8a756b]">Paste the reference from the recipient card.</FormDescription>
                <FormControl>
                  <Input placeholder="e.g. 3451f061-..." className={`mt-2.5 ${inputClass}`} autoComplete="off" {...field} />
                </FormControl>
                <FormMessage className="text-[10px] text-red-600" />
              </FormItem>
            )}
          />
        </div>

        <div className="grid gap-3.5 lg:grid-cols-2">
          <FormField
            control={form.control}
            name="amount"
            render={({ field }) => (
              <FormItem className={sectionClass}>
                <FormLabel className="text-xs font-semibold text-[#2b1a14]">Amount in rand</FormLabel>
                <FormDescription className="mt-0.5 text-[11px] text-[#8a756b]">Use up to two decimal places.</FormDescription>
                <div className="relative mt-2.5">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-[#6b4435]">R</span>
                  <FormControl>
                    <Input inputMode="decimal" placeholder="250.00" className={`${inputClass} pl-8`} {...field} />
                  </FormControl>
                </div>
                <FormMessage className="text-[10px] text-red-600" />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem className={sectionClass}>
                <FormLabel className="text-xs font-semibold text-[#2b1a14]">Payment reference</FormLabel>
                <FormDescription className="mt-0.5 text-[11px] text-[#8a756b]">Add an optional transfer note.</FormDescription>
                <FormControl>
                  <Textarea placeholder="e.g. Shared groceries" className="mt-2.5 min-h-10 rounded-lg border-[#ddcec5] bg-white text-sm text-[#2b1a14] placeholder:text-[#aa968c] focus-visible:ring-[#7a4a37]" {...field} />
                </FormControl>
                <FormMessage className="text-[10px] text-red-600" />
              </FormItem>
            )}
          />
        </div>

        {form.formState.errors.root?.message && (
          <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">
            {form.formState.errors.root.message}
          </p>
        )}

        <div className="flex justify-end pt-1">
          <Button
            type="submit"
            className="h-10 w-full rounded-lg bg-[#4a2b20] px-5 text-xs font-semibold text-white shadow-sm hover:bg-[#382017] sm:w-auto"
            disabled={isLoading || accounts.length === 0}
          >
            {isLoading ? (
              <>
                <Loader2 size={15} className="mr-2 animate-spin" /> Simulating transfer
              </>
            ) : (
              <>
                Review and transfer <ArrowRight size={15} className="ml-2" />
              </>
            )}
          </Button>
        </div>
      </form>
    </Form>
  );
};

export default PaymentTransferForm;
