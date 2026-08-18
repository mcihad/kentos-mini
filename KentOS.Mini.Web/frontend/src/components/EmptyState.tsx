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
    /*
      TELEFONDA DAHA DERLİ TOPLU.

      `py-14` (56px alt+üst) masaüstünde doğru: geniş bir tabloda boş durum
      kaybolmasın diye nefes alıyor. 390px'lik bir ekranda ise aynı boşluk,
      "kayıt yok" demek için neredeyse bir ekran harcıyor — proje özetinde
      kilometre taşı bölümü tek başına katlamanın altını dolduruyordu.
    */
    <div className="grid place-items-center px-6 py-9 text-center md:py-14">
      <div className="grid h-11 w-11 place-items-center rounded-lg bg-sunken md:h-[52px] md:w-[52px]">
        <Ikon size={19} strokeWidth={1.8} className="text-text-3 md:size-[21px]" />
      </div>
      <p className="mt-3 font-display text-sm font-bold">{baslik}</p>
      {aciklama && (
        <p className="mt-1 max-w-[340px] text-sm text-text-2 metin-guzel">{aciklama}</p>
      )}
      {eylem && <div className="mt-4">{eylem}</div>}
    </div>
  );
}
