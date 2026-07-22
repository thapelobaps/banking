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
  capitec: { from: '#f8fbfd', to: '#dce8ef', foreground: '#123653', muted: '#547087', accent: '#e51b2b' },
  'standard-bank': { from: '#0033a0', to: '#1597d4', foreground: '#ffffff', muted: '#d7ebff', accent: '#70d1ff' },
  fnb: { from: '#f4ecd9', to: '#8fd0c8', foreground: '#173e42', muted: '#4b7172', accent: '#ef7c28' },
  absa: { from: '#8b0c24', to: '#ed3852', foreground: '#ffffff', muted: '#ffd5db', accent: '#ff8492' },
  nedbank: { from: '#083c2c', to: '#4f9c62', foreground: '#ffffff', muted: '#d7f0df', accent: '#b4dd75' },
  generic: { from: '#182a37', to: '#668b9c', foreground: '#ffffff', muted: '#d9e6ec', accent: '#9fd0e1' },
};

const BrandArtwork = ({ brand, foreground, accent }: { brand: CardBrand; foreground: string; accent: string }) => {
  if (brand === 'capitec') {
    return (
      <>
        <path d="M250 -8H350V222H314L258 148Z" fill="#003b70" opacity="0.96" />
        <path d="M287 -8H319L245 214H215Z" fill="#e51b2b" opacity="0.96" />
        <path d="M0 176C83 143 145 154 214 191V214H0Z" fill="#ffffff" opacity="0.42" />
      </>
    );
  }

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
