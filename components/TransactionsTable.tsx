'use client';

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Transaction } from '@/types';
import { formatAmount, formatDateTime, getTransactionStatus } from '@/lib/utils';

interface TransactionsTableProps {
  transactions?: Transaction[] | null;
}

const TransactionsTable = ({ transactions = [] }: TransactionsTableProps) => {
  const transactionList = transactions ?? [];

  return (
    <div className="overflow-hidden rounded-[20px] border border-[#eadfd8] bg-white shadow-sm">
      <details className="kape-activity-accordion md:hidden">
        <summary className="flex min-h-16 cursor-pointer list-none items-center gap-3 px-4 py-3 [&::-webkit-details-marker]:hidden">
          <div className="min-w-0 flex-1">
            <h3 className="truncate text-sm font-semibold text-[#2b1a14]">Recent activity</h3>
            <p className="mt-0.5 truncate text-[10px] text-[#8a756b]">Tap to view recent transactions</p>
          </div>
          <span className="shrink-0 rounded-full bg-[#f3ebe6] px-2.5 py-1 text-[8px] font-semibold uppercase tracking-[0.14em] text-[#6b4435]">Demo</span>
          <svg
            width="18"
            height="18"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.8"
            className="kape-activity-chevron shrink-0 text-[#8a756b] transition-transform duration-200"
            aria-hidden="true"
          >
            <path d="m6 9 6 6 6-6" />
          </svg>
        </summary>

        <div className="border-t border-[#eee5df]">
          {transactionList.length > 0 ? (
            transactionList.map((transaction) => {
              const status = transaction.status || getTransactionStatus(new Date(transaction.date));
              const isCredit = transaction.type === 'credit';
              const isComplete = status.toLowerCase() === 'success' || status.toLowerCase() === 'completed';

              return (
                <details key={transaction.id} className="kape-transaction-accordion border-b border-[#f0e8e3] last:border-b-0">
                  <summary className="flex min-h-14 cursor-pointer list-none items-center gap-3 px-3 py-3 [&::-webkit-details-marker]:hidden">
                    <span className={`flex size-8 shrink-0 items-center justify-center rounded-xl text-xs font-bold ${
                      isCredit ? 'bg-emerald-50 text-emerald-700' : 'bg-[#f3ebe6] text-[#6b4435]'
                    }`}>
                      {isCredit ? '↙' : '↗'}
                    </span>
                    <span className="min-w-0 flex-1 truncate text-sm font-semibold text-[#2b1a14]">
                      {transaction.name}
                    </span>
                    <svg
                      width="18"
                      height="18"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.8"
                      className="kape-transaction-chevron shrink-0 text-[#8a756b] transition-transform duration-200"
                      aria-hidden="true"
                    >
                      <path d="m6 9 6 6 6-6" />
                    </svg>
                  </summary>

                  <div className="grid grid-cols-2 gap-x-4 gap-y-3 bg-[#fdfaf8] px-4 py-3">
                    <div className="min-w-0">
                      <p className="text-[9px] font-semibold uppercase tracking-[0.12em] text-[#9a8378]">Channel</p>
                      <p className="mt-1 break-words text-xs font-medium text-[#3b251d]">
                        {transaction.paymentChannel || transaction.channel}
                      </p>
                    </div>
                    <div className="min-w-0">
                      <p className="text-[9px] font-semibold uppercase tracking-[0.12em] text-[#9a8378]">Category</p>
                      <p className="mt-1 break-words text-xs font-medium text-[#3b251d]">{transaction.category}</p>
                    </div>
                    <div className="min-w-0">
                      <p className="text-[9px] font-semibold uppercase tracking-[0.12em] text-[#9a8378]">Date</p>
                      <p className="mt-1 text-xs font-medium text-[#3b251d]">
                        {formatDateTime(new Date(transaction.date)).dateOnly}
                      </p>
                    </div>
                    <div className="min-w-0">
                      <p className="text-[9px] font-semibold uppercase tracking-[0.12em] text-[#9a8378]">Status</p>
                      <span className={`mt-1 inline-flex rounded-full px-2 py-1 text-[10px] font-semibold ${
                        isComplete ? 'bg-emerald-50 text-emerald-700' : 'bg-amber-50 text-amber-700'
                      }`}>
                        {status}
                      </span>
                    </div>
                    <div className="col-span-2 flex items-center justify-between border-t border-[#eee5df] pt-3">
                      <span className="text-[9px] font-semibold uppercase tracking-[0.12em] text-[#9a8378]">Amount</span>
                      <span className={`text-sm font-bold tabular-nums ${isCredit ? 'text-emerald-700' : 'text-[#2b1a14]'}`}>
                        {isCredit ? '+' : '-'}{formatAmount(transaction.amount)}
                      </span>
                    </div>
                  </div>
                </details>
              );
            })
          ) : (
            <div className="px-4 py-10 text-center text-xs text-[#8a756b]">No transactions found.</div>
          )}
        </div>
      </details>

      <div className="hidden items-center justify-between gap-3 border-b border-[#eee5df] px-4 py-3 md:flex">
        <div className="min-w-0">
          <h3 className="truncate text-sm font-semibold text-[#2b1a14]">Recent activity</h3>
          <p className="mt-0.5 truncate text-[10px] text-[#8a756b]">Latest SQL-backed demo transactions</p>
        </div>
        <span className="shrink-0 rounded-full bg-[#f3ebe6] px-2.5 py-1 text-[8px] font-semibold uppercase tracking-[0.14em] text-[#6b4435]">Demo</span>
      </div>

      <div className="hidden overflow-x-auto md:block">
        <Table>
          <TableHeader className="bg-[#fbf7f4]">
            <TableRow className="h-9 border-[#eee5df] hover:bg-[#fbf7f4]">
              <TableHead className="min-w-[170px] px-3 text-[10px] text-[#7b675e]">Transaction</TableHead>
              <TableHead className="px-3 text-[10px] text-[#7b675e]">Category</TableHead>
              <TableHead className="px-3 text-[10px] text-[#7b675e]">Date</TableHead>
              <TableHead className="px-3 text-[10px] text-[#7b675e]">Status</TableHead>
              <TableHead className="px-3 text-right text-[10px] text-[#7b675e]">Amount</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {transactionList.length > 0 ? (
              transactionList.map((transaction) => {
                const status = transaction.status || getTransactionStatus(new Date(transaction.date));
                const isCredit = transaction.type === 'credit';
                return (
                  <TableRow key={transaction.id} className="h-12 border-[#f0e8e3] hover:bg-[#fdfaf8]">
                    <TableCell className="px-3 py-2.5">
                      <div className="flex items-center gap-2.5">
                        <span className={`flex size-8 shrink-0 items-center justify-center rounded-xl text-xs font-bold ${
                          isCredit ? 'bg-emerald-50 text-emerald-700' : 'bg-[#f3ebe6] text-[#6b4435]'
                        }`}>
                          {isCredit ? '↙' : '↗'}
                        </span>
                        <div className="min-w-0">
                          <p className="truncate text-xs font-medium text-[#2b1a14]">{transaction.name}</p>
                          <p className="mt-0.5 truncate text-[10px] text-[#9a8378]">{transaction.paymentChannel || transaction.channel}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell className="px-3 py-2.5">
                      <span className="rounded-full bg-[#f8f3ef] px-2.5 py-1 text-[10px] font-medium text-[#6f5b52]">
                        {transaction.category}
                      </span>
                    </TableCell>
                    <TableCell className="whitespace-nowrap px-3 py-2.5 text-[11px] text-[#6f5b52]">
                      {formatDateTime(new Date(transaction.date)).dateOnly}
                    </TableCell>
                    <TableCell className="px-3 py-2.5">
                      <span className={`rounded-full px-2.5 py-1 text-[10px] font-semibold ${
                        status.toLowerCase() === 'success' || status.toLowerCase() === 'completed'
                          ? 'bg-emerald-50 text-emerald-700'
                          : 'bg-amber-50 text-amber-700'
                      }`}>
                        {status}
                      </span>
                    </TableCell>
                    <TableCell className={`whitespace-nowrap px-3 py-2.5 text-right text-xs font-semibold tabular-nums ${
                      isCredit ? 'text-emerald-700' : 'text-[#2b1a14]'
                    }`}>
                      {isCredit ? '+' : '-'}{formatAmount(transaction.amount)}
                    </TableCell>
                  </TableRow>
                );
              })
            ) : (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-xs text-[#8a756b]">
                  No transactions found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
};

export default TransactionsTable;
