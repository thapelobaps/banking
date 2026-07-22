'use client';

import Link from 'next/link';
import { useEffect, useMemo, useRef, useState } from 'react';

import IssuerCardFace from '@/components/wallet/IssuerCardFace';
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
  cardholderName?: string;
};

export default function MoneySourceCarousel({
  items,
  selectedId,
  cardholderName,
}: MoneySourceCarouselProps) {
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
          const isSelected = item.id === selectedId;

          return (
            <Link
              key={item.id}
              href={item.href}
              ref={(node) => {
                cardRefs.current[index] = node;
              }}
              className={`money-source-card money-source-card--svg${isSelected ? ' is-selected' : ''}`}
              aria-current={isSelected ? 'page' : undefined}
              onFocus={() => scrollToCard(index)}
            >
              <span className="money-source-card__face">
                <IssuerCardFace
                  institution={item.institution}
                  accountName={item.accountName}
                  mask={item.mask}
                  cardholderName={cardholderName}
                  kind={item.kind}
                />
              </span>

              <span className="money-source-card__summary">
                <span>
                  <small>Available</small>
                  <strong>{formatAmount(item.availableBalance)}</strong>
                </span>
                <span>
                  <small>{item.entryCount} transaction{item.entryCount === 1 ? '' : 's'}</small>
                  <strong>{item.mask ? `•••• ${item.mask}` : item.accountName}</strong>
                </span>
              </span>
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
