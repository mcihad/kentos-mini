import { cn } from './utils';

/**
 * Hafif, bağımlılıksız grafikler.
 *
 * Grafik kütüphanesi EKLENMEDİ: ihtiyacımız iki şekil (yatay çubuk ve
 * sütun serisi) ve her ikisi de birkaç div ile çiziliyor. Recharts/Chart.js
 * paket boyutunu ~150 kB büyütür, tema tokenlarına uyması için ayrıca
 * sarmalanması gerekir ve dokunmatik davranışı bu ekranlarda gereksizdir.
 */

export type Dilim = { etiket?: string | null; deger?: number; yuzde?: number; renk?: string | null };

/** Yatay çubuklu dağılım — kategorik veriler için. */
export function BarList({
  dilimler,
  enFazla = 8,
  bosMetin = 'Veri yok',
}: {
  dilimler: Dilim[] | null;
  enFazla?: number;
  bosMetin?: string;
}) {
  const liste = (dilimler ?? []).filter((d) => (d.deger ?? 0) > 0).slice(0, enFazla);
  if (liste.length === 0) {
    return <p className="py-6 text-center text-sm text-text-3">{bosMetin}</p>;
  }

  const tepe = Math.max(...liste.map((d) => d.deger ?? 0));

  return (
    <ul className="space-y-2.5">
      {liste.map((d, i) => (
        <li key={i}>
          <div className="flex items-baseline justify-between gap-3">
            <span className="min-w-0 truncate text-sm text-text-2">{d.etiket}</span>
            <span className="shrink-0 text-sm font-semibold tabular-nums">
              {d.deger?.toLocaleString('tr-TR')}
              {typeof d.yuzde === 'number' && (
                <span className="ml-1.5 font-normal text-text-3">%{d.yuzde.toFixed(0)}</span>
              )}
            </span>
          </div>
          <div className="mt-1 h-[6px] overflow-hidden rounded-full bg-sunken">
            <div
              className="h-full rounded-full transition-[width] duration-300"
              style={{
                width: `${tepe > 0 ? ((d.deger ?? 0) / tepe) * 100 : 0}%`,
                background: d.renk || 'var(--brand-hover)',
              }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}

/**
 * Sütun serisi — zaman ekseni için.
 *
 * Etiketler yoğunlaşınca birbirine girmesin diye yalnızca her n'inci
 * etiket basılır; n, sütun sayısından türetilir.
 */
export function ColumnSeries({
  noktalar,
  yukseklik = 132,
}: {
  noktalar: { etiket?: string | null; deger?: number }[] | null;
  yukseklik?: number;
}) {
  const liste = noktalar ?? [];
  if (liste.length === 0) {
    return <p className="py-6 text-center text-sm text-text-3">Veri yok</p>;
  }

  const tepe = Math.max(1, ...liste.map((n) => n.deger ?? 0));
  const atla = Math.ceil(liste.length / 12);

  return (
    <div>
      {/*
        `items-stretch` (varsayılan) ZORUNLU: `items-end` verildiğinde sütun
        kapsayıcıları içeriklerine göre 0 yüksekliğe iner ve içerideki
        yüzdelik yükseklikler hesaplanamaz — grafik boş görünür. Çubuk zaten
        `justify-end` ile tabana yaslanıyor.
      */}
      <div className="flex gap-[3px]" style={{ height: yukseklik }} role="img"
        aria-label={`Sütun grafiği, ${liste.length} nokta, en yüksek değer ${tepe}`}>
        {liste.map((n, i) => {
          const oran = (n.deger ?? 0) / tepe;
          return (
            <div key={i} className="group relative flex flex-1 flex-col justify-end">
              <div
                className={cn(
                  'w-full rounded-t-xs bg-brand-2 transition-[height] duration-300',
                  (n.deger ?? 0) === 0 && 'bg-sunken',
                )}
                style={{ height: `${Math.max(oran * 100, 2)}%` }}
              />
              {/* Değer ipucu — hover'da */}
              <span className="pointer-events-none absolute -top-6 left-1/2 hidden -translate-x-1/2 whitespace-nowrap rounded-sm bg-nav-bg px-1.5 py-0.5 text-2xs font-semibold text-nav-strong group-hover:block">
                {n.deger?.toLocaleString('tr-TR')}
              </span>
            </div>
          );
        })}
      </div>

      <div className="mt-1.5 flex gap-[3px]">
        {liste.map((n, i) => (
          <span
            key={i}
            className="flex-1 overflow-hidden text-center text-3xs text-text-3"
          >
            {i % atla === 0 ? n.etiket : ''}
          </span>
        ))}
      </div>
    </div>
  );
}

/**
 * Halka gösterge — tek bir oran için.
 *
 * `conic-gradient` ile çizilir; SVG'ye göre daha az işaretleme, aynı sonuç.
 */
export function DonutChart({
  oran,
  etiket,
  renk = 'var(--st-ok)',
}: {
  oran: number;
  etiket: string;
  renk?: string;
}) {
  const guvenli = Math.max(0, Math.min(100, oran));
  return (
    <div className="flex items-center gap-3">
      <div
        className="grid h-[68px] w-[68px] shrink-0 place-items-center rounded-full"
        style={{
          background: `conic-gradient(${renk} ${guvenli * 3.6}deg, var(--sunken) 0deg)`,
        }}
        role="img"
        aria-label={`${etiket}: %${guvenli.toFixed(0)}`}
      >
        <div className="grid h-[52px] w-[52px] place-items-center rounded-full bg-surface">
          <span className="font-display text-lg font-bold tabular-nums">
            %{guvenli.toFixed(0)}
          </span>
        </div>
      </div>
      <p className="text-sm text-text-2">{etiket}</p>
    </div>
  );
}
