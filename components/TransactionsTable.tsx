'use client';
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Transaction } from '@/types';
import { formatAmount, formatDateTime, getTransactionStatus } from '@/lib/utils';

interface TransactionsTableProps {
  transactions?: Transaction[] | null; // Allow null or undefined
}

const TransactionsTable = ({ transactions = [] }: TransactionsTableProps) => {
  return (
    <Table>
      <TableCaption>Recent Transactions</TableCaption>
      <TableHeader>
        <TableRow>
          <TableHead>Transaction</TableHead>
          <TableHead>Amount</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Date</TableHead>
          <TableHead>Channel</TableHead>
          <TableHead>Category</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {(transactions?.length ?? 0) > 0 ? (
          (transactions ?? []).map((t: Transaction) => {
            const status = getTransactionStatus(new Date(t.date));
            const amount = formatAmount(t.amount);
            return (
              <TableRow key={t.id}>
                <TableCell className="font-medium">{t.name}</TableCell>
                <TableCell>{amount}</TableCell>
                <TableCell>{status}</TableCell>
                <TableCell>{formatDateTime(new Date(t.date)).dateTime}</TableCell>
                <TableCell>{t.paymentChannel}</TableCell>
                <TableCell>{t.category}</TableCell>
              </TableRow>
            );
          })
        ) : (
          <TableRow>
            <TableCell colSpan={6} className="text-center">
              No transactions found.
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
};

export default TransactionsTable;