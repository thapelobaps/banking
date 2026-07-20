'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import {
  ArrowLeft,
  ArrowRight,
  CheckCircle2,
  Download,
  Loader2,
  ReceiptText,
  ShieldCheck,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

import {
  createMockTransfer,
  getDemoRecipientPreview,
  RecipientPreview,
} from '@/lib/actions/bank.actions';
import { formatAmount } from '@/lib/utils';
import { PaymentTransferFormProps, Transaction } from '@/types';
import { BankDropdown } from './BankDropdown';
import RecipientDropdown, { demoRecipients } from './RecipientDropdown';
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
  recipientReference: z.string().uuid('Select a valid recipient account'),
});

type FormValues = z.infer<typeof formSchema>;
type Stage = 'details' | 'review' | 'receipt';

type TransferReview = {
  values: FormValues;
  recipient: RecipientPreview;
};

type TransferReceipt = {
  transaction: Transaction;
  recipient: RecipientPreview;
  senderName: string;
  senderMask: string;
  paymentReference: string;
};

const PaymentTransferForm = ({ accounts }: PaymentTransferFormProps) => {
  const router = useRouter();
  const initialSenderId = accounts[0]?.id ?? '';
  const initialRecipientId = accounts.find((account) => account.id !== initialSenderId)?.id ?? demoRecipients[0].id;

  const [stage, setStage] = useState<Stage>('details');
  const [isLoading, setIsLoading] = useState(false);
  const [reviewData, setReviewData] = useState<TransferReview | null>(null);
  const [receipt, setReceipt] = useState<TransferReceipt | null>(null);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      amount: '',
      senderBank: initialSenderId,
      recipientReference: initialRecipientId,
    },
  });

  const selectedSenderId = form.watch('senderBank');

  useEffect(() => {
    const currentRecipientId = form.getValues('recipientReference');
    const isValidOwnRecipient = accounts.some(
      (account) => account.id === currentRecipientId && account.id !== selectedSenderId
    );
    const isValidDemoRecipient = demoRecipients.some(
      (recipient) => recipient.id === currentRecipientId
    );

    if (
      !currentRecipientId ||
      currentRecipientId === selectedSenderId ||
      (!isValidOwnRecipient && !isValidDemoRecipient)
    ) {
      const fallbackRecipientId =
        accounts.find((account) => account.id !== selectedSenderId)?.id ??
        demoRecipients[0].id;

      form.setValue('recipientReference', fallbackRecipientId, {
        shouldDirty: true,
        shouldValidate: true,
      });
    }
  }, [accounts, form, selectedSenderId]);

  const senderAccount = useMemo(
    () => accounts.find((account) => account.id === reviewData?.values.senderBank),
    [accounts, reviewData]
  );

  const reviewTransfer = async (values: FormValues) => {
    setIsLoading(true);
    form.clearErrors('root');

    try {
      if (values.senderBank === values.recipientReference) {
        form.setError('recipientReference', { message: 'Choose a different recipient account' });
        return;
      }

      const source = accounts.find((account) => account.id === values.senderBank);
      if (!source) {
        form.setError('senderBank', { message: 'Select a valid source account' });
        return;
      }

      if (Number(values.amount) > source.availableBalance) {
        form.setError('amount', { message: 'The amount is greater than the available demo balance' });
        return;
      }

      const recipient = await getDemoRecipientPreview(values.recipientReference);
      setReviewData({ values, recipient });
      setStage('review');
    } catch (error) {
      form.setError('recipientReference', {
        message: error instanceof Error ? error.message : 'The recipient demo account could not be verified.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const confirmTransfer = async () => {
    if (!reviewData || !senderAccount) return;

    setIsLoading(true);
    form.clearErrors('root');

    try {
      const transaction = await createMockTransfer({
        senderBankId: reviewData.values.senderBank,
        receiverBankId: reviewData.values.recipientReference,
        amount: Number(reviewData.values.amount),
        name: reviewData.values.name || 'Demo transfer',
      });

      setReceipt({
        transaction,
        recipient: reviewData.recipient,
        senderName: senderAccount.name,
        senderMask: senderAccount.mask,
        paymentReference: reviewData.values.name || 'Demo transfer',
      });
      setStage('receipt');
      router.refresh();
    } catch (error) {
      form.setError('root', {
        message: error instanceof Error ? error.message : 'The simulated transfer failed. No real money was moved.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const resetTransfer = () => {
    const senderId = accounts[0]?.id ?? '';
    const recipientId = accounts.find((account) => account.id !== senderId)?.id ?? demoRecipients[0].id;

    form.reset({
      name: '',
      amount: '',
      senderBank: senderId,
      recipientReference: recipientId,
    });
    setReviewData(null);
    setReceipt(null);
    setStage('details');
  };

  const downloadReceipt = () => {
    if (!receipt) return;

    const completedAt = new Date(receipt.transaction.date);
    const content = [
      'KAPE APP - DEMO PROOF OF TRANSFER',
      '---------------------------------',
      `Status: ${receipt.transaction.status}`,
      `Transaction reference: ${receipt.transaction.id}`,
      `Date: ${completedAt.toLocaleString('en-ZA')}`,
      `From: ${receipt.senderName} •••• ${receipt.senderMask}`,
      `To: ${receipt.recipient.bankName} •••• ${receipt.recipient.accountMask}`,
      `Amount: ${formatAmount(receipt.transaction.amount)}`,
      `Payment reference: ${receipt.paymentReference}`,
      '',
      'This is a simulated Kape demo transfer. No real money was moved.',
    ].join('\n');

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `kape-demo-transfer-${receipt.transaction.id}.txt`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  };

  const sectionClass = 'rounded-xl border border-[#eee5df] bg-[#fdfaf8] p-3.5';
  const inputClass = 'h-10 rounded-lg border-[#ddcec5] bg-white text-sm text-[#2b1a14] placeholder:text-[#aa968c] focus-visible:ring-[#7a4a37]';

  if (stage === 'receipt' && receipt) {
    return (
      <section className="space-y-4" aria-live="polite">
        <div className="rounded-2xl border border-emerald-200 bg-emerald-50 p-5 text-center">
          <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-emerald-100 text-emerald-700">
            <CheckCircle2 size={25} />
          </span>
          <p className="mt-3 text-xs font-semibold uppercase tracking-[0.16em] text-emerald-700">Transfer completed</p>
          <h2 className="mt-2 text-3xl font-semibold tracking-tight text-[#2b1a14]">
            {formatAmount(receipt.transaction.amount)}
          </h2>
          <p className="mt-2 text-xs text-[#6f5b52]">
            Sent to {receipt.recipient.bankName} •••• {receipt.recipient.accountMask}
          </p>
        </div>

        <div className="overflow-hidden rounded-2xl border border-[#eadfd8] bg-white">
          <div className="flex items-center gap-2 border-b border-[#eee5df] px-4 py-3">
            <ReceiptText size={17} className="text-[#6b4435]" />
            <h3 className="text-sm font-semibold text-[#2b1a14]">Demo transfer receipt</h3>
          </div>
          <dl className="divide-y divide-[#f0e8e3] px-4">
            {[
              ['Status', receipt.transaction.status],
              ['Transaction reference', receipt.transaction.id],
              ['Date', new Date(receipt.transaction.date).toLocaleString('en-ZA')],
              ['From', `${receipt.senderName} •••• ${receipt.senderMask}`],
              ['To', `${receipt.recipient.bankName} •••• ${receipt.recipient.accountMask}`],
              ['Payment reference', receipt.paymentReference],
            ].map(([label, value]) => (
              <div key={label} className="grid grid-cols-[120px_minmax(0,1fr)] gap-3 py-3 text-xs">
                <dt className="text-[#8a756b]">{label}</dt>
                <dd className="min-w-0 break-words text-right font-semibold text-[#2b1a14]">{value}</dd>
              </div>
            ))}
          </dl>
        </div>

        <p className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-[11px] leading-4 text-amber-800">
          This proof relates to a simulated SQL Server demo transfer. No real bank was contacted and no real money moved.
        </p>

        <div className="grid gap-2 sm:grid-cols-3">
          <Button type="button" variant="outline" className="h-10" onClick={resetTransfer}>
            New transfer
          </Button>
          <Button type="button" variant="outline" className="h-10" onClick={downloadReceipt}>
            <Download size={15} className="mr-2" /> Download proof
          </Button>
          <Button type="button" className="h-10 bg-[#4a2b20] text-white hover:bg-[#382017]" onClick={() => router.push('/transaction-history')}>
            View activity <ArrowRight size={15} className="ml-2" />
          </Button>
        </div>
      </section>
    );
  }

  if (stage === 'review' && reviewData && senderAccount) {
    return (
      <section className="space-y-4">
        <div className="flex items-start gap-2.5 rounded-xl border border-amber-200 bg-amber-50 p-3 text-amber-900">
          <ShieldCheck className="mt-0.5 size-4 shrink-0" />
          <div>
            <p className="text-xs font-semibold">Review before confirming</p>
            <p className="mt-0.5 text-[11px] leading-4 text-amber-800">Check the recipient and amount carefully. The balance changes only after confirmation.</p>
          </div>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <article className={sectionClass}>
            <p className="text-[9px] font-semibold uppercase tracking-[0.14em] text-[#9a8378]">From</p>
            <p className="mt-2 text-sm font-semibold text-[#2b1a14]">{senderAccount.name}</p>
            <p className="mt-1 text-xs text-[#8a756b]">•••• {senderAccount.mask} · Available {formatAmount(senderAccount.availableBalance)}</p>
          </article>
          <article className={sectionClass}>
            <p className="text-[9px] font-semibold uppercase tracking-[0.14em] text-[#9a8378]">Recipient</p>
            <p className="mt-2 text-sm font-semibold text-[#2b1a14]">{reviewData.recipient.bankName}</p>
            <p className="mt-1 text-xs text-[#8a756b]">•••• {reviewData.recipient.accountMask} · {reviewData.recipient.accountType}</p>
          </article>
        </div>

        <div className="rounded-2xl bg-[#4a2b20] p-5 text-white">
          <p className="text-[9px] font-semibold uppercase tracking-[0.16em] text-white/55">Amount to transfer</p>
          <p className="mt-2 text-3xl font-semibold tracking-tight">{formatAmount(Number(reviewData.values.amount))}</p>
          <p className="mt-3 text-xs text-white/65">Reference: {reviewData.values.name || 'Demo transfer'}</p>
        </div>

        {form.formState.errors.root?.message && (
          <p role="alert" className="rounded-xl border border-red-200 bg-red-50 p-3 text-xs text-red-700">
            {form.formState.errors.root.message}
          </p>
        )}

        <div className="grid gap-2 sm:grid-cols-2">
          <Button type="button" variant="outline" className="h-10" onClick={() => setStage('details')} disabled={isLoading}>
            <ArrowLeft size={15} className="mr-2" /> Edit details
          </Button>
          <Button type="button" className="h-10 bg-[#4a2b20] text-white hover:bg-[#382017]" onClick={confirmTransfer} disabled={isLoading}>
            {isLoading ? (
              <><Loader2 size={15} className="mr-2 animate-spin" /> Confirming transfer</>
            ) : (
              <>Confirm demo transfer <ArrowRight size={15} className="ml-2" /></>
            )}
          </Button>
        </div>
      </section>
    );
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(reviewTransfer)} className="space-y-3.5">
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
                <FormLabel className="text-xs font-semibold text-[#2b1a14]">To account</FormLabel>
                <FormDescription className="mt-0.5 text-[11px] text-[#8a756b]">
                  Choose your other Kape account or a demo recipient.
                </FormDescription>
                <FormControl>
                  <RecipientDropdown
                    accounts={accounts}
                    senderAccountId={selectedSenderId}
                    value={field.value}
                    onChange={field.onChange}
                  />
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
              <><Loader2 size={15} className="mr-2 animate-spin" /> Verifying recipient</>
            ) : (
              <>Review transfer <ArrowRight size={15} className="ml-2" /></>
            )}
          </Button>
        </div>
      </form>
    </Form>
  );
};

export default PaymentTransferForm;
