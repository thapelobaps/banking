'use client';

import Link from 'next/link';
import { Landmark, Layers3, WalletCards } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';

import { formatAmount } from '@/lib/utils';

export type MoneySourceCarouselItem = {
  id: string;
  href: string;
  kind: 'aggregate' | 'wallet' | 'bank';
  institution: string;
  accountName: string;
  mask?: string;
  availableBalance: number;
  currentBalance?: number;
  entryCount: number;
};

type MoneySourceCarouselProps = {
  items: MoneySourceCarouselItem[];
  selectedId: string;
};

type IssuerCardArtwork = {
  url: string;
  alt: string;
  fit: 'cover' | 'contain';
  background: string;
};

const brandKey = (institution: string) => {
  const value = institution.toLowerCase();
  if (value.includes('capitec')) return 'capitec';
  if (value.includes('standard')) return 'standard-bank';
  if (value.includes('fnb') || value.includes('first national')) return 'fnb';
  if (value.includes('absa')) return 'absa';
  if (value.includes('nedbank')) return 'nedbank';
  if (value.includes('kape')) return 'kape';
  return 'bank';
};

const resolveIssuerCardArtwork = (item: MoneySourceCarouselItem): IssuerCardArtwork | null => {
  if (item.kind !== 'bank') return null;

  const institution = item.institution.toLowerCase();
  const account = item.accountName.toLowerCase();

  if (institution.includes('capitec') && (account.includes('global one') || account.includes('main'))) {
    return {
      url: 'https://www.capitecbank.co.za/globalassets/approved-images/transact/onecard---ad-card.jpg',
      alt: 'Official Capitec Global One physical card artwork',
      fit: 'contain',
      background: '#f2f5f7',
    };
  }

  if (institution.includes('standard') && account.includes('mymo')) {
    return {
      url: 'https://www.standardbank.co.za/static_file/SBG/Assets/Img/SA/BankCards/MyMo-Gold_337x213.png',
      alt: 'Official Standard Bank MyMo Gold physical card artwork',
      fit: 'contain',
      background: '#f3e1a0',
    };
  }

  return null;
};

const SourceIcon = ({ kind }: { kind: MoneySourceCarouselItem['kind'] }) => {
  if (kind === 'aggregate') return <Layers3 size={21} />;
  if (kind === 'wallet') return <WalletCards size={21} />;
  return <Landmark size={21} />;
};

export default function MoneySourceCarousel({ items, selectedId }: MoneySourceCarouselProps) {
  const viewportRef = useRef<HTMLDivElement | null>(null);
  const cardRefs = useRef<Array<HTMLAnchorElement | null>>([]);
  const selectedIndex = Math.max(0, items.findIndex((item) => item.id === selectedId));
  const [activeIndex, setActiveIndex] = useState(selectedIndex);

  const stableItems = useMemo(() => items, [items]);

  const scrollToCard = (index: number, behavior: ScrollBehavior = 'smooth') => {
    const card = cardRefs.current[index];
    const viewport = viewportRef.current;
    if (!card || !viewport) return;

    const targetLeft = card.offsetLeft - Math.max(0, (viewport.clientWidth - card.clientWidth) / 2);
    viewport.scrollTo({ left: targetLeft, behavior });
    setActiveIndex(index);
  };

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => scrollToCard(selectedIndex, 'auto'));
    return () => window.cancelAnimationFrame(frame);
    // The selected source controls the initial carousel position.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedIndex]);

  const updateActiveCard = () => {
    const viewport = viewportRef.current;
    if (!viewport) return;

    const viewportCentre = viewport.scrollLeft + viewport.clientWidth / 2;
    let closestIndex = 0;
    let closestDistance = Number.POSITIVE_INFINITY;

    cardRefs.current.forEach((card, index) => {
      if (!card) return;
      const cardCentre = card.offsetLeft + card.clientWidth / 2;
      const distance = Math.abs(cardCentre - viewportCentre);
      if (distance < closestDistance) {
        closestDistance = distance;
        closestIndex = index;
      }
    });

    setActiveIndex(closestIndex);
  };

  return (
    <section className="money-source-carousel" aria-label="Money sources">
      <div ref={viewportRef} className="money-source-carousel__viewport" onScroll={updateActiveCard}>
        {stableItems.map((item, index) => {
          const bankBrand = brandKey(item.institution);
          const issuerArtwork = resolveIssuerCardArtwork(item);
          const isSelected = item.id === selectedId;

          return (
            <Link
              key={item.id}
              href={item.href}
              ref={(node) => {
                cardRefs.current[index] = node;
              }}
              className={`money-source-card money-source-card--${item.kind} money-source-card--${bankBrand}${issuerArtwork ? ' has-issuer-artwork' : ''}${isSelected ? ' is-selected' : ''}`}
              aria-current={isSelected ? 'page' : undefined}
              onFocus={() => scrollToCard(index)}
            >
              {issuerArtwork ? (
                <>
                  <span
                    className="money-source-card__issuer-artwork"
                    role="img"
                    aria-label={issuerArtwork.alt}
                    style={{
                      backgroundColor: issuerArtwork.background,
                      backgroundImage: `url("${issuerArtwork.url}")`,
                      backgroundSize: issuerArtwork.fit,
                    }}
                  />

                  <div className="money-source-card__issuer-meta">
                    <div>
                      <strong>{item.institution}</strong>
                      <span>{item.accountName} · {item.entryCount} transaction{item.entryCount === 1 ? '' : 's'}</span>
                    </div>
                    <div>
                      <strong>{formatAmount(item.availableBalance)}</strong>
                      <span>{item.mask ? `•••• ${item.mask}` : 'Card source'}</span>
                    </div>
                  </div>
                </>
              ) : (
                <>
                  <span className="money-source-card__glow" aria-hidden="true" />

                  <div className="money-source-card__top">
                    <div className={`money-source-brand money-source-brand--${bankBrand}`} aria-label={`${item.institution} logo`}>
                      <span className="money-source-brand__image" aria-hidden="true" />
                      <span className="money-source-brand__fallback">{item.institution}</span>
                    </div>
                    <span className="money-source-card__type"><SourceIcon kind={item.kind} /></span>
                  </div>

                  <div className="money-source-card__balance">
                    <span>Available balance</span>
                    <strong>{formatAmount(item.availableBalance)}</strong>
                    <small>
                      {item.currentBalance == null
                        ? `${item.entryCount} entr${item.entryCount === 1 ? 'y' : 'ies'}`
                        : `Current ${formatAmount(item.currentBalance)}`}
                    </small>
                  </div>

                  <div className="money-source-card__bottom">
                    <div>
                      <strong>{item.accountName}</strong>
                      <span>{item.entryCount} transaction{item.entryCount === 1 ? '' : 's'}</span>
                    </div>
                    <span>{item.mask ? `•••• ${item.mask}` : 'UNIFIED'}</span>
                  </div>
                </>
              )}
            </Link>
          );
        })}
      </div>

      <div className="money-source-carousel__dots" aria-label="Money source position">
        {stableItems.map((item, index) => (
          <button
            key={item.id}
            type="button"
            className={index === activeIndex ? 'is-active' : ''}
            aria-label={`Show ${item.institution} ${item.accountName}`}
            aria-current={index === activeIndex ? 'true' : undefined}
            onClick={() => scrollToCard(index)}
          />
        ))}
      </div>
    </section>
  );
}
