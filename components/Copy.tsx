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
      className="flex w-full min-w-0 items-center gap-3 rounded-xl border border-[#eadfd8] bg-white px-3 py-3 text-left shadow-sm transition hover:border-[#cdb9ad] hover:bg-[#fdfaf8] md:gap-2 md:py-2"
      aria-label={`Copy ${label.toLowerCase()}`}
    >
      <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-[#f3ebe6] text-[#5b382a] md:size-7">
        {copyState === 'copied' ? (
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        ) : (
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
            <rect width="14" height="14" x="8" y="8" rx="2" />
            <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2" />
          </svg>
        )}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block text-[9px] font-semibold uppercase tracking-[0.12em] text-[#9a8378] md:text-[8px] md:tracking-[0.14em]">{label}</span>
        <span className="mt-1 block truncate font-mono text-[11px] font-medium text-[#3b251d] md:mt-0.5 md:text-[9px]">
          {copyState === 'copied' ? 'Copied to clipboard' : copyState === 'failed' ? 'Copy failed' : title}
        </span>
      </span>
      <span className="shrink-0 text-[11px] font-semibold text-[#7a4a37] md:text-[9px]">Copy</span>
    </button>
  );
};

export default Copy;
