'use client';

import { Check, Clipboard, Printer } from 'lucide-react';
import { useState } from 'react';

export default function ReceiptActions({ reference }: { reference: string }) {
  const [copied, setCopied] = useState(false);

  const copy = async () => {
    await navigator.clipboard.writeText(reference);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  };

  return (
    <div className="marketplace-receipt-actions">
      <button type="button" onClick={() => window.print()}><Printer size={16} /> Print receipt</button>
      <button type="button" className="is-secondary" onClick={copy}>
        {copied ? <Check size={16} /> : <Clipboard size={16} />}
        {copied ? 'Copied' : 'Copy reference'}
      </button>
    </div>
  );
}
