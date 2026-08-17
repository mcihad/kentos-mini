import type { LucideIcon } from 'lucide-react';

/** design.md §7.12 — boş durum. */
export function EmptyState({
  ikon: Ikon,
  baslik,
  aciklama,
  eylem,
}: {
  ikon: LucideIcon;
  baslik: string;
  aciklama?: string;
  eylem?: React.ReactNode;
}) {
  return (
    <div className="grid place-items-center px-6 py-14 text-center">
      <div className="grid h-[52px] w-[52px] place-items-center rounded-lg bg-sunken">
        <Ikon size={21} strokeWidth={1.8} className="text-text-3" />
      </div>
      <p className="mt-3.5 font-display text-sm font-bold">{baslik}</p>
      {aciklama && (
        <p className="mt-1 max-w-[340px] text-sm text-text-2 metin-guzel">{aciklama}</p>
      )}
      {eylem && <div className="mt-4">{eylem}</div>}
    </div>
  );
}
