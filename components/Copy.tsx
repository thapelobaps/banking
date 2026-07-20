'use client';

import { useState } from 'react';

 type CopyProps = {
  title: string;
  label?: string;
};

const Copy = ({ title, label = 'Reference' }: CopyProps) => {
  const [copyState, setCopyState] = useState<'idle' | 'copied' | 'failed'>('idle');

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(title);
      setCopyState('copied');
    } catch {
      setCopyState('failed');
    }

    window.setTimeout(() => setCopyState('idle'), 2000);
  };

  return (
    <button
      type="button"
      onClick={copyToClipboard}
      className="flex w-full items-center gap-3 rounded-2xl border border-[#eadfd8] bg-white px-4 py-3 text-left shadow-sm transition hover:border-[#cdb9ad] hover:bg-[#fdfaf8]"
      aria-label={`Copy ${label.toLowerCase()}`}
    >
      <span className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-[#f3ebe6] text-[#5b382a]">
        {copyState === 'copied' ? (
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        ) : (
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <rect width="14" height="14" x="8" y="8" rx="2" />
            <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2" />
          </svg>
        )}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block text-[10px] font-semibold uppercase tracking-[0.16em] text-[#9a8378]">{label}</span>
        <span className="mt-1 block truncate font-mono text-xs font-medium text-[#3b251d]">
          {copyState === 'copied' ? 'Copied to clipboard' : copyState === 'failed' ? 'Copy failed' : title}
        </span>
      </span>
      <span className="text-xs font-semibold text-[#7a4a37]">Copy</span>
    </button>
  );
};

export default Copy;
