import type { ReactNode } from 'react';
import { BottomSheet, SheetHeading } from '../shell/mobile/BottomSheet';
import { cn } from './utils';

/**
 * SÜZGEÇ TABAKASI — mobilde arama ve filtrelerin tek kabı.
 *
 * <p>
 * Liste ekranlarının üstü denetim yığınına dönüşmüştü: sekmeler, arama,
 * dönem gezinmesi, tip menüsü, çıktı düğmeleri, ekleme düğmesi. Telefonda
 * ilk ekranın yarısı kontrol, asıl liste kıvrımın altındaydı.
 * </p>
 *
 * <p>
 * Yeni gramer: <b>üstte yalnızca arama</b>, geri kalan her şey FAB'dan açılan
 * bu tabakada. Kullanıcı süzmediği sürece ekranın tamamı veriye ait —
 * amaç zaten daha çok kayıt göstermek.
 * </p>
 */
export function FilterSheet({
  acik,
  kapat,
  children,
  temizle,
  etkinSayisi,
}: {
  acik: boolean;
  kapat: () => void;
  children: ReactNode;
  /** Verilirse "Süzgeçleri temizle" düğmesi çıkar. */
  temizle?: () => void;
  /** Kaç süzgeç etkin — sıfırsa temizleme düğmesi anlamsız. */
  etkinSayisi?: number;
}) {
  return (
    <BottomSheet acik={acik} kapat={kapat} baslik="Ara ve süz">
      <div className="space-y-4 pt-1">{children}</div>

      {/*
        Eylemler EN ALTTA ve tam genişlikte: tabaka açıkken başparmak ekranın
        alt üçte birinde duruyor, en sık basılacak düğmenin oraya gelmesi
        gerekiyor.
      */}
      <div className="mt-5 flex gap-2">
        {temizle && (etkinSayisi ?? 0) > 0 && (
          <button
            type="button"
            onClick={temizle}
            className="h-ctrl-lg flex-1 rounded-sm border border-line bg-surface text-sm font-semibold text-ink-2 active:scale-[0.98]"
          >
            Temizle
          </button>
        )}
        <button
          type="button"
          onClick={kapat}
          className="h-ctrl-lg flex-1 rounded-sm bg-brand text-sm font-semibold text-on-brand active:scale-[0.98]"
        >
          Uygula
        </button>
      </div>
    </BottomSheet>
  );
}

/** Tabaka içinde bir süzgeç bölümü — başlık + içerik. */
export function FilterSection({ baslik, children }: { baslik: string; children: ReactNode }) {
  return (
    <div>
      <SheetHeading>{baslik}</SheetHeading>
      {children}
    </div>
  );
}

export type SegmentOption<T extends string> = {
  deger: T;
  etiket: string;
  ikon?: ReactNode;
};

/**
 * SEGMENT — birbirini dışlayan az sayıda seçenek (sekmeler, görünümler).
 *
 * <p>
 * Alt çizgili sekme şeridi tabakanın içinde yanlış duruyordu: sekme "sayfa
 * değiştirir", oysa burada yapılan şey <b>süzmek</b>. Segment kontrolü
 * seçeneklerin bir arada ve eşit ağırlıkta olduğunu gösteriyor ve tek
 * dokunuşta değiştiriliyor.
 * </p>
 */
export function Segment<T extends string>({
  secenekler,
  deger,
  degistir,
}: {
  secenekler: SegmentOption<T>[];
  deger: T;
  degistir: (d: T) => void;
}) {
  return (
    <div role="tablist" className="flex gap-1 rounded-sm border border-line bg-sunken p-1">
      {secenekler.map((s) => {
        const aktif = s.deger === deger;
        return (
          <button
            key={s.deger}
            role="tab"
            aria-selected={aktif}
            onClick={() => degistir(s.deger)}
            className={cn(
              'h-ctrl flex min-w-0 flex-1 basis-0 items-center justify-center gap-1.5 rounded-xs px-2',
              'text-xs font-semibold transition-colors active:scale-[0.97]',
              aktif ? 'bg-brand text-on-brand shadow-1' : 'text-ink-2',
            )}
            style={{ transitionTimingFunction: 'var(--ease-spring)' }}
          >
            {s.ikon}
            <span className="truncate">{s.etiket}</span>
          </button>
        );
      })}
    </div>
  );
}

/**
 * Tabaka içi seçenek listesi — tek seçim.
 *
 * <c>SecimMenusu</c> masaüstü açılır menüsü; telefonda 44px'lik dokunma
 * hedefi ve tam genişlik satır gerekiyor.
 */
export function FilterOptions<T>({
  secenekler,
  deger,
  degistir,
}: {
  secenekler: { deger: T; etiket: string; sayi?: number; renk?: string | null }[];
  deger: T;
  degistir: (d: T) => void;
}) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {secenekler.map((s) => {
        const aktif = s.deger === deger;
        return (
          <button
            key={String(s.deger)}
            type="button"
            onClick={() => degistir(s.deger)}
            className={cn(
              'inline-flex h-ctrl-lg items-center gap-1.5 rounded-full border px-3 text-xs font-medium',
              'transition-colors active:scale-[0.97]',
              aktif
                ? 'border-brand bg-brand-soft text-brand'
                : 'border-line bg-surface text-ink-2',
            )}
            style={{ transitionTimingFunction: 'var(--ease-spring)' }}
          >
            {s.renk && (
              <span
                aria-hidden
                className="h-[7px] w-[7px] shrink-0 rounded-full"
                style={{ background: s.renk }}
              />
            )}
            {s.etiket}
            {s.sayi != null && <span className="tabular-nums text-ink-3">{s.sayi}</span>}
          </button>
        );
      })}
    </div>
  );
}
