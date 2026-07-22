import { Bolt, Gamepad2, Gift, Headphones, ShoppingBag, Tv, UtensilsCrossed } from 'lucide-react';

const brandKey = (value: string) =>
  value.toLowerCase().replaceAll('&', 'and').replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');

const BrandIcon = ({ name }: { name: string }) => {
  const value = name.toLowerCase();
  if (value.includes('netflix')) return <Tv size={22} />;
  if (value.includes('spotify')) return <Headphones size={22} />;
  if (value.includes('playstation') || value.includes('xbox') || value.includes('steam')) return <Gamepad2 size={22} />;
  if (value.includes('uber')) return <UtensilsCrossed size={22} />;
  if (value.includes('electric')) return <Bolt size={22} />;
  if (value.includes('amazon') || value.includes('google')) return <ShoppingBag size={22} />;
  return <Gift size={22} />;
};

export default function MarketplaceBrandMark({ name, compact = false }: { name: string; compact?: boolean }) {
  return (
    <span className={`marketplace-brand marketplace-brand--${brandKey(name)}${compact ? ' is-compact' : ''}`} aria-label={name}>
      <BrandIcon name={name} />
      <strong>{name}</strong>
    </span>
  );
}
