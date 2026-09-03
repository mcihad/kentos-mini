import * as ToggleGroup from '@radix-ui/react-toggle-group';
import { X } from 'lucide-react';
import { cn } from './utils';

/**
 * design.md §7.8 — bölümlü seçim (segmented control).
 *
 * Radix ToggleGroup üzerine kurulu: ok tuşlarıyla gezinme ve `aria-pressed`
 * bedava gelir, elle `div`+`onClick` yazınca gelmez.
 */
export function SegmentedSelect<T extends string>({
  deger,
  degistir,
  secenekler,
  etiket,
  className,
}: {
  deger: T;
  degistir: (d: T) => void;
  secenekler: { deger: T; etiket: string; ikon?: React.ReactNode }[];
  etiket: string;
  className?: string;
}) {
  return (
    <ToggleGroup.Root
      type="single"
      value={deger}
      onValueChange={(d) => d && degistir(d as T)}
      aria-label={etiket}
      className={cn(
        /*
          RAY DÖRT YANDAN EŞİT: `items-center` + sabit `h-8` öğe, 36 piksellik
          rayın içinde yanlarda 3 (1 kenarlık + 2 dolgu), üstte ve altta 2
          piksel bırakıyordu — pil kutusunun içinde yatay olarak yüzüyor,
          dikeyde kenara yapışıyordu. Öğe artık kendi yüksekliğini
          dayatmıyor, rayı `items-stretch` ile dolduruyor: iç boşluk her
          kenarda 3 piksel.
        */
        // Şartname §6.11: bölümlü seçim mobilde 44px (dokunma hedefi),
        // masaüstünde 36 — orada fare hassas, araç çubuğu yoğun.
        'inline-flex h-11 items-stretch gap-0.5 rounded-control border border-border bg-sunken p-0.5 md:h-9',
        className,
      )}
    >
      {secenekler.map((s) => (
        <ToggleGroup.Item
          key={s.deger}
          value={s.deger}
          className={cn(
            /*
              İÇ YARIÇAP DIŞTAN KÜÇÜK (globals.css'teki iç içe yarıçap kuralı):
              ikisi de `--r-sm` iken pilin köşeleri rayın köşelerinden dışarı
              taşıyor, aradaki 3 piksel köşelerde kapanıyordu.
            */
            'inline-flex items-center gap-1.5 rounded-xs px-3 text-sm font-medium',
            'text-text-2 transition-colors hover:text-text',
            'data-[state=on]:bg-surface data-[state=on]:text-text data-[state=on]:shadow-1',
          )}
        >
          {s.ikon}
          {s.etiket}
        </ToggleGroup.Item>
      ))}
    </ToggleGroup.Root>
  );
}

/**
 * design.md §7.7 — filtre çipi.
 *
 * Seçiliyken `×` gösterir: çipi kaldırmanın yolu, onu bulup yeniden
 * tıklamak değil, oradaki düğme olmalı.
 */
export function FilterChip({
  secili,
  tikla,
  children,
  sayi,
}: {
  secili?: boolean;
  tikla: () => void;
  children: React.ReactNode;
  sayi?: number;
}) {
  return (
    <button
      type="button"
      onClick={tikla}
      aria-pressed={secili}
      className={cn(
        // Şartname §6.11: süzgeç çipi 36px, tam yuvarlak; seçili = marka dolgusu.
        'bas-yay inline-flex h-9 shrink-0 items-center gap-1.5 rounded-full border px-3.5 text-sm font-semibold transition-colors',
        secili
          ? 'border-brand bg-brand text-on-brand'
          : 'border-border bg-surface text-text-2 hover:bg-surface-2 hover:text-text',
      )}
    >
      {children}
      {typeof sayi === 'number' && (
        <span
          className={cn(
            'rounded-full px-1.5 text-2xs tabular-nums',
            secili ? 'bg-[rgba(255,255,255,0.2)]' : 'bg-sunken text-text-3',
          )}
        >
          {sayi}
        </span>
      )}
      {secili && <X size={12} strokeWidth={2.5} aria-hidden />}
    </button>
  );
}

/** Yatay kaydırılabilir çip şeridi — mobilde taşmayı kaydırmaya çevirir. */
export function ChipStrip({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={cn('-mx-4 overflow-x-auto px-4 pb-0.5 md:mx-0 md:px-0 kaydirma-gizle', className)}>
      <div className="flex gap-2">{children}</div>
    </div>
  );
}
