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
  return (
    <div className="overflow-hidden rounded-3xl border border-[#eadfd8] bg-white shadow-sm">
      <div className="flex items-center justify-between border-b border-[#eee5df] px-5 py-4 sm:px-6">
        <div>
          <h3 className="text-base font-semibold text-[#2b1a14]">Recent activity</h3>
          <p className="mt-1 text-xs text-[#8a756b]">Latest SQL-backed demo transactions</p>
        </div>
        <span className="rounded-full bg-[#f3ebe6] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[#6b4435]">Demo</span>
      </div>

      <div className="overflow-x-auto">
        <Table>
          <TableHeader className="bg-[#fbf7f4]">
            <TableRow className="border-[#eee5df] hover:bg-[#fbf7f4]">
              <TableHead className="min-w-[220px] text-[#7b675e]">Transaction</TableHead>
              <TableHead className="text-[#7b675e]">Category</TableHead>
              <TableHead className="text-[#7b675e]">Date</TableHead>
              <TableHead className="text-[#7b675e]">Status</TableHead>
              <TableHead className="text-right text-[#7b675e]">Amount</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {(transactions?.length ?? 0) > 0 ? (
              (transactions ?? []).map((transaction) => {
                const status = transaction.status || getTransactionStatus(new Date(transaction.date));
                const isCredit = transaction.type === 'credit';
                return (
                  <TableRow key={transaction.id} className="border-[#f0e8e3] hover:bg-[#fdfaf8]">
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <span className={`flex size-10 shrink-0 items-center justify-center rounded-2xl text-sm font-bold ${
                          isCredit ? 'bg-emerald-50 text-emerald-700' : 'bg-[#f3ebe6] text-[#6b4435]'
                        }`}>
                          {isCredit ? '↙' : '↗'}
                        </span>
                        <div>
                          <p className="font-medium text-[#2b1a14]">{transaction.name}</p>
                          <p className="mt-1 text-xs text-[#9a8378]">{transaction.paymentChannel || transaction.channel}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="rounded-full bg-[#f8f3ef] px-3 py-1 text-xs font-medium text-[#6f5b52]">
                        {transaction.category}
                      </span>
                    </TableCell>
                    <TableCell className="whitespace-nowrap text-sm text-[#6f5b52]">
                      {formatDateTime(new Date(transaction.date)).dateOnly}
                    </TableCell>
                    <TableCell>
                      <span className={`rounded-full px-3 py-1 text-xs font-semibold ${
                        status.toLowerCase() === 'success' || status.toLowerCase() === 'completed'
                          ? 'bg-emerald-50 text-emerald-700'
                          : 'bg-amber-50 text-amber-700'
                      }`}>
                        {status}
                      </span>
                    </TableCell>
                    <TableCell className={`whitespace-nowrap text-right font-semibold tabular-nums ${
                      isCredit ? 'text-emerald-700' : 'text-[#2b1a14]'
                    }`}>
                      {isCredit ? '+' : '-'}{formatAmount(transaction.amount)}
                    </TableCell>
                  </TableRow>
                );
              })
            ) : (
              <TableRow>
                <TableCell colSpan={5} className="h-32 text-center text-sm text-[#8a756b]">
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
