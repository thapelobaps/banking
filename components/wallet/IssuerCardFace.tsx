'use client';

import { useId } from 'react';

type IssuerCardFaceProps = {
  institution: string;
  accountName: string;
  mask?: string;
  cardholderName?: string;
  kind: 'aggregate' | 'wallet' | 'bank';
};

type CardBrand = 'kape' | 'capitec' | 'standard-bank' | 'fnb' | 'absa' | 'nedbank' | 'generic';

const resolveBrand = (institution: string, kind: IssuerCardFaceProps['kind']): CardBrand => {
  if (kind !== 'bank') return 'kape';

  const value = institution.toLowerCase();
  if (value.includes('capitec')) return 'capitec';
  if (value.includes('standard')) return 'standard-bank';
  if (value.includes('fnb') || value.includes('first national')) return 'fnb';
  if (value.includes('absa')) return 'absa';
  if (value.includes('nedbank')) return 'nedbank';
  return 'generic';
};

const palette: Record<CardBrand, { from: string; to: string; foreground: string; muted: string; accent: string }> = {
  kape: { from: '#21110c', to: '#7b4b37', foreground: '#ffffff', muted: '#dbc8bd', accent: '#d6a875' },
  capitec: { from: '#111111', to: '#252525', foreground: '#ffffff', muted: '#b8b8b8', accent: '#2d78ff' },
  'standard-bank': { from: '#0033a0', to: '#1597d4', foreground: '#ffffff', muted: '#d7ebff', accent: '#70d1ff' },
  fnb: { from: '#f4ecd9', to: '#8fd0c8', foreground: '#173e42', muted: '#4b7172', accent: '#ef7c28' },
  absa: { from: '#8b0c24', to: '#ed3852', foreground: '#ffffff', muted: '#ffd5db', accent: '#ff8492' },
  nedbank: { from: '#083c2c', to: '#4f9c62', foreground: '#ffffff', muted: '#d7f0df', accent: '#b4dd75' },
  generic: { from: '#182a37', to: '#668b9c', foreground: '#ffffff', muted: '#d9e6ec', accent: '#9fd0e1' },
};

const CapitecSymbol = () => (
  <g transform="translate(24 20)" fill="#ffffff">
    <path d="M18 3h35c10 0 18 8 18 18v10H49V21H27c-5 0-9 4-9 9v9H0V21C0 11 8 3 18 3Z" />
    <path d="M18 34h22v10c0 5 4 9 9 9h22v18H36c-10 0-18-8-18-18V34Z" />
  </g>
);

const MastercardMark = ({ compact = false }: { compact?: boolean }) => {
  const radius = compact ? 12 : 22;
  const offset = compact ? 16 : 29;
  const x = compact ? 274 : 267;
  const y = compact ? 172 : 35;

  return (
    <g>
      <circle cx={x} cy={y} r={radius} fill="#eb001b" />
      <circle cx={x + offset} cy={y} r={radius} fill="#f79e1b" />
      <path
        d={compact
          ? `M${x + 8} ${y - 9}a12 12 0 0 1 0 18 12 12 0 0 1 0-18Z`
          : `M${x + 14} ${y - 17}a22 22 0 0 1 0 34 22 22 0 0 1 0-34Z`}
        fill="#ff5f00"
      />
    </g>
  );
};

const CapitecAnniversaryCard = ({ mask, uid }: { mask?: string; uid: string }) => {
  const metalId = `capitec-metal-${uid}`;
  const grainId = `capitec-grain-${uid}`;
  const edgeId = `capitec-edge-${uid}`;
  const lastFour = mask?.slice(-4) || '0000';

  return (
    <svg viewBox="0 0 340 214" role="img" aria-label="Capitec Personal debit card">
      <defs>
        <linearGradient id={metalId} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#272727" />
          <stop offset="0.42" stopColor="#111111" />
          <stop offset="1" stopColor="#202020" />
        </linearGradient>
        <linearGradient id={edgeId} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#4d8cff" />
          <stop offset="0.5" stopColor="#1f6fff" />
          <stop offset="1" stopColor="#0d48b6" />
        </linearGradient>
        <filter id={grainId} x="-10%" y="-10%" width="120%" height="120%">
          <feTurbulence type="fractalNoise" baseFrequency="0.7 0.035" numOctaves="2" seed="8" result="noise" />
          <feColorMatrix
            in="noise"
            type="matrix"
            values="0 0 0 0 0.7 0 0 0 0 0.7 0 0 0 0 0.7 0 0 0 .18 0"
            result="grain"
          />
          <feBlend in="SourceGraphic" in2="grain" mode="soft-light" />
        </filter>
      </defs>

      <rect x="1.5" y="1.5" width="337" height="211" rx="24" fill={`url(#${edgeId})`} />
      <rect x="4" y="4" width="332" height="206" rx="21" fill={`url(#${metalId})`} filter={`url(#${grainId})`} />
      <path d="M7 8H333" stroke="#7cb0ff" strokeWidth="1.4" opacity="0.7" />

      <CapitecSymbol />
      <text x="102" y="47" fill="#ffffff" fontSize="28" fontWeight="500" letterSpacing="1.4">
        CAPITEC
      </text>
      <text x="25" y="75" fill="#ffffff" fontSize="15" fontWeight="400">
        Personal <tspan fontWeight="800">debit</tspan>
      </text>

      <MastercardMark />

      <g opacity="0.16" transform="translate(183 84)">
        <text x="12" y="82" fill="#d7d7d7" fontSize="92" fontWeight="800" letterSpacing="-8">25</text>
        <path d="M13 89H42L48 79L55 97L63 69L72 104L82 75L92 95H111" fill="none" stroke="#dedede" strokeWidth="4" strokeLinecap="round" strokeLinejoin="round" />
        <path d="M104 81c0-10 13-15 20-7 7-8 20-3 20 7 0 12-20 25-20 25s-20-13-20-25Z" fill="#dedede" />
        <text x="99" y="126" fill="#cfcfcf" fontSize="19" fontWeight="700">years</text>
      </g>

      <text x="24" y="190" fill="#ffffff" fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace" fontSize="23" fontWeight="700" letterSpacing="1.1">
        ••{lastFour}
      </text>
    </svg>
  );
};

const BrandArtwork = ({ brand, foreground, accent }: { brand: CardBrand; foreground: string; accent: string }) => {
  if (brand === 'standard-bank') {
    return (
      <>
        <path d="M-10 170C72 116 163 118 350 27V214H-10Z" fill="#ffffff" opacity="0.09" />
        <path d="M-12 191C91 128 192 147 360 66" fill="none" stroke={accent} strokeWidth="15" opacity="0.36" />
        <circle cx="296" cy="43" r="58" fill="#ffffff" opacity="0.07" />
      </>
    );
  }

  if (brand === 'fnb') {
    return (
      <>
        <circle cx="292" cy="46" r="34" fill={accent} opacity="0.95" />
        <path d="M270 52C218 85 213 137 183 213" fill="none" stroke="#2e7977" strokeWidth="18" opacity="0.3" />
        <path d="M338 83C281 117 257 157 238 216" fill="none" stroke="#ffffff" strokeWidth="23" opacity="0.26" />
        <path d="M22 187C83 143 143 153 208 214H0V196Z" fill="#ffffff" opacity="0.22" />
      </>
    );
  }

  if (brand === 'absa') {
    return (
      <>
        <circle cx="285" cy="47" r="82" fill="none" stroke="#ffffff" strokeWidth="18" opacity="0.1" />
        <circle cx="285" cy="47" r="48" fill="none" stroke={accent} strokeWidth="13" opacity="0.28" />
        <path d="M-12 187C72 132 170 143 350 92V214H-12Z" fill="#4f0012" opacity="0.2" />
      </>
    );
  }

  if (brand === 'nedbank') {
    return (
      <>
        <path d="M258 14C311 36 333 75 326 127C274 122 242 88 238 40Z" fill={accent} opacity="0.42" />
        <path d="M259 15C273 61 291 95 324 126" fill="none" stroke="#ffffff" strokeWidth="5" opacity="0.44" />
        <path d="M-8 179C87 130 177 145 350 81V214H-8Z" fill="#001e15" opacity="0.2" />
      </>
    );
  }

  if (brand === 'kape') {
    return (
      <>
        <circle cx="300" cy="38" r="74" fill="#ffffff" opacity="0.06" />
        <circle cx="300" cy="38" r="47" fill="none" stroke={accent} strokeWidth="8" opacity="0.24" />
        <path d="M-10 190C84 128 172 147 354 80V214H-10Z" fill="#000000" opacity="0.12" />
      </>
    );
  }

  return (
    <>
      <circle cx="291" cy="45" r="70" fill="#ffffff" opacity="0.08" />
      <path d="M-8 181C84 129 188 147 351 74V214H-8Z" fill={foreground} opacity="0.08" />
    </>
  );
};

const Contactless = ({ colour }: { colour: string }) => (
  <g fill="none" stroke={colour} strokeLinecap="round" opacity="0.78">
    <path d="M87 82C93 88 93 98 87 104" strokeWidth="2.5" />
    <path d="M92 78C102 88 102 100 92 110" strokeWidth="2.5" />
    <path d="M98 73C112 87 112 102 98 116" strokeWidth="2.5" />
  </g>
);

const Chip = () => (
  <g>
    <rect x="27" y="76" width="45" height="32" rx="7" fill="#d4b46b" />
    <path d="M42 77V107M57 77V107M28 91H71M34 82H65M34 102H65" fill="none" stroke="#8f7134" strokeWidth="1.4" opacity="0.8" />
  </g>
);

const NetworkMark = ({ foreground }: { foreground: string }) => (
  <g transform="translate(281 171)">
    <circle cx="0" cy="0" r="13" fill="#f24b32" opacity="0.95" />
    <circle cx="17" cy="0" r="13" fill="#f2a31d" opacity="0.95" />
    <text x="8.5" y="22" textAnchor="middle" fill={foreground} fontSize="5.8" fontWeight="700" letterSpacing="0.7">DEBIT</text>
  </g>
);

export default function IssuerCardFace({
  institution,
  accountName,
  mask,
  cardholderName = 'KAPE MEMBER',
  kind,
}: IssuerCardFaceProps) {
  const brand = resolveBrand(institution, kind);
  const colours = palette[brand];
  const uid = useId().replaceAll(':', '');
  const gradientId = `card-gradient-${uid}`;
  const sheenId = `card-sheen-${uid}`;
  const displayInstitution = kind === 'aggregate' ? 'KAPE' : institution.toUpperCase();
  const displayAccount = kind === 'aggregate' ? 'UNIFIED MONEY' : accountName.toUpperCase();
  const displayMask = mask ? `••••  ••••  ••••  ${mask}` : kind === 'wallet' ? '••••  ••••  KAPE  WALLET' : 'ALL ACCOUNTS';

  if (brand === 'capitec') {
    return <CapitecAnniversaryCard mask={mask} uid={uid} />;
  }

  return (
    <svg viewBox="0 0 340 214" role="img" aria-label={`${institution} ${accountName} payment card`}>
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor={colours.from} />
          <stop offset="1" stopColor={colours.to} />
        </linearGradient>
        <linearGradient id={sheenId} x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#ffffff" stopOpacity="0.18" />
          <stop offset="0.42" stopColor="#ffffff" stopOpacity="0.02" />
          <stop offset="1" stopColor="#ffffff" stopOpacity="0.12" />
        </linearGradient>
      </defs>

      <rect width="340" height="214" rx="22" fill={`url(#${gradientId})`} />
      <BrandArtwork brand={brand} foreground={colours.foreground} accent={colours.accent} />
      <rect width="340" height="214" rx="22" fill={`url(#${sheenId})`} opacity="0.55" />

      <text x="27" y="34" fill={colours.foreground} fontSize="17" fontWeight="800" letterSpacing="0.3">
        {displayInstitution}
      </text>
      <text x="27" y="51" fill={colours.muted} fontSize="7.4" fontWeight="700" letterSpacing="1.05">
        {displayAccount}
      </text>
      <text x="310" y="33" textAnchor="end" fill={colours.muted} fontSize="6.6" fontWeight="700" letterSpacing="1.1">
        CONTACTLESS
      </text>

      <Chip />
      <Contactless colour={colours.foreground} />

      <text x="27" y="139" fill={colours.foreground} fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace" fontSize="15" fontWeight="650" letterSpacing="1.3">
        {displayMask}
      </text>
      <text x="27" y="162" fill={colours.muted} fontSize="6.4" fontWeight="700" letterSpacing="1">
        CARDHOLDER
      </text>
      <text x="27" y="179" fill={colours.foreground} fontSize="9.2" fontWeight="700" letterSpacing="0.8">
        {cardholderName.toUpperCase().slice(0, 28)}
      </text>

      <NetworkMark foreground={colours.foreground} />
    </svg>
  );
}
