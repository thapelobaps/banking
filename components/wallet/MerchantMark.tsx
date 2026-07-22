import {
  BriefcaseBusiness,
  PiggyBank,
  ReceiptText,
  Smartphone,
  WalletCards,
} from 'lucide-react';

type MerchantMarkProps = {
  title: string;
  category: string;
  sourceName: string;
  direction: 'credit' | 'debit';
};

const merchantKey = (title: string, category: string, sourceName: string) => {
  const value = `${title} ${category} ${sourceName}`.toLowerCase();
  if (value.includes('netflix')) return 'netflix';
  if (value.includes('pick n pay') || value.includes('picknpay')) return 'pick-n-pay';
  if (value.includes('vodacom')) return 'vodacom';
  if (value.includes('salary') || value.includes('employer') || value.includes('income')) return 'salary';
  if (value.includes('saving')) return 'savings';
  if (value.includes('wallet') || value.includes('transfer') || value.includes('top up')) return 'wallet';
  return 'generic';
};

export default function MerchantMark({ title, category, sourceName, direction }: MerchantMarkProps) {
  const key = merchantKey(title, category, sourceName);

  if (key === 'netflix' || key === 'pick-n-pay') {
    return (
      <div className={`merchant-mark merchant-mark--${key}`} aria-label={`${title} logo`}>
        <span aria-hidden="true" />
      </div>
    );
  }

  if (key === 'vodacom') {
    return <div className="merchant-mark merchant-mark--vodacom" aria-label="Vodacom"><Smartphone size={17} /></div>;
  }

  if (key === 'salary') {
    return <div className="merchant-mark merchant-mark--salary" aria-label="Salary"><BriefcaseBusiness size={17} /></div>;
  }

  if (key === 'savings') {
    return <div className="merchant-mark merchant-mark--savings" aria-label="Savings"><PiggyBank size={17} /></div>;
  }

  if (key === 'wallet') {
    return <div className={`merchant-mark merchant-mark--wallet is-${direction}`} aria-label="Kape Wallet"><WalletCards size={17} /></div>;
  }

  return <div className={`merchant-mark merchant-mark--generic is-${direction}`} aria-label={category}><ReceiptText size={17} /></div>;
}
