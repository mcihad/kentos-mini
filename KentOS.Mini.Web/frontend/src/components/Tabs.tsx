import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import { cn } from './utils';

export type TabItem<T extends string> = {
  deger: T;
  etiket: ReactNode;
  /** Sağda küçük sayaç — "Bekleyenler 12". */
  sayi?: number;
};

/**
 * SEKMELER — uygulamanın TEK sekme standardı.
 *
 * NEDEN ORTAK: sekmeler her ekranda ayrı yazılmıştı; kimi alt çizgili, kimi
 * hap, kimi düğme kılığında. Mobilde hiçbiri tam genişliği doldurmuyor,
 * taşan sekmeler de kesiliyordu — kullanıcı üçüncü sekmenin varlığını
 * göremiyordu.
 *
 * DAVRANIŞ:
 *  • Sekmeler yatağı **tam genişlik** doldurur; sığdıkları sürece eşit paylaşır
 *    (`flex-1`). Bu, iki sekmeli bir ekranda da üç sekmeli bir ekranda da
 *    aynı ritmi verir.
 *  • **Taşarsa kaydırılır**: yatay kaydırma açılır, kaydırma çubuğu gizlenir
 *    ve parmakla/fareyle sürüklenebilir. Kesilmiş bir sekme kalmaz.
 *  • **Aktif sekme kendi kendine görünür alana kayar** — kaydırılmış bir
 *    şeritte seçili sekmenin ekran dışında kalması, "hangisindeyim"
 *    sorusunu cevapsız bırakıyordu.
 *  • Hap biçimi ve renkler tamamen token'dan; tema değişince sekmeler de döner.
 */
export function Tabs<T extends string>({
  sekmeler,
  deger,
  degistir,
  className,
}: {
  sekmeler: TabItem<T>[];
  deger: T;
  degistir: (d: T) => void;
  className?: string;
}) {
  const seritRef = useRef<HTMLDivElement>(null);
  const aktifRef = useRef<HTMLButtonElement>(null);

  // Seçili sekme her zaman görünür alanda olsun.
  useEffect(() => {
    const d = aktifRef.current;
    if (!d) return;
    d.scrollIntoView({ inline: 'nearest', block: 'nearest', behavior: 'smooth' });
  }, [deger]);

  return (
    <div
      ref={seritRef}
      role="tablist"
      className={cn(
        // `sekme-serit`: kaydırma çubuğunu gizler ama kaydırmayı bırakır.
        'sekme-serit flex gap-1 overflow-x-auto rounded-sm border border-line bg-sunken p-1',
        className,
      )}
    >
      {sekmeler.map((s) => {
        const aktif = s.deger === deger;
        return (
          <button
            key={s.deger}
            ref={aktif ? aktifRef : undefined}
            role="tab"
            aria-selected={aktif}
            onClick={() => degistir(s.deger)}
            className={cn(
              // `flex-1` + `basis-0`: sığdıkları sürece eşit bölüşürler.
              // `min-w-max` taşma durumunda etiketin kırpılmasını engeller;
              // ikisi birlikte "sığarsa doldur, sığmazsa kaydır" davranışını
              // tek satırda kuruyor.
              'flex min-w-max flex-1 basis-0 items-center justify-center gap-1.5',
              'h-ctrl-lg rounded-xs px-3 text-xs font-semibold transition-colors',
              'active:scale-[0.97]',
              aktif
                ? 'bg-brand text-on-brand shadow-1'
                : 'text-ink-2 hover:bg-surface hover:text-ink',
            )}
            style={{ transitionTimingFunction: 'var(--ease-spring)' }}
          >
            {s.etiket}
            {s.sayi != null && (
              <span
                className={cn(
                  'tabular-nums text-3xs',
                  aktif ? 'opacity-70' : 'text-ink-3',
                )}
              >
                {s.sayi}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
