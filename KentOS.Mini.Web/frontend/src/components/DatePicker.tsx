import * as Popover from '@radix-ui/react-popover';
import { CalendarDays, ChevronLeft, ChevronRight } from 'lucide-react';
import { useMemo, useState } from 'react';
import { cn } from './utils';

const GUN_BASLIKLARI = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
const AYLAR = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

const ik = (n: number) => String(n).padStart(2, '0');

/** `Date` → `yyyy-MM-dd`. Saat dilimi dönüşümü YOK (bkz. veri/zaman.ts). */
export function dayText(t: Date): string {
  return `${t.getFullYear()}-${ik(t.getMonth() + 1)}-${ik(t.getDate())}`;
}

/** `yyyy-MM-dd` → `Date` (yerel gece yarısı). */
export function parseDay(metin: string): Date | null {
  const e = /^(\d{4})-(\d{2})-(\d{2})$/.exec(metin);
  if (!e) return null;
  return new Date(Number(e[1]), Number(e[2]) - 1, Number(e[3]));
}

/** `12 Ağustos 2026 Çarşamba` */
function uzunGun(t: Date): string {
  const gunAdi = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'][t.getDay()];
  return `${t.getDate()} ${AYLAR[t.getMonth()]} ${t.getFullYear()} ${gunAdi}`;
}

/**
 * Tarih seçici.
 *
 * <p>
 * Tarayıcının yerleşik <c>&lt;input type="date"&gt;</c> KULLANILMIYOR:
 * görünümü tarayıcıya göre değişiyor, koyu temaya uymuyor ve Safari'de
 * gün adları İngilizce çıkıyor. Bu bileşen her yerde aynı ve Türkçe.
 * </p>
 *
 * <p>
 * Hafta <b>pazartesi</b> başlar (design.md §9) — Türkiye'de resmî takvim
 * böyle; pazar başlangıcı kullanıcıyı her seferinde bir gün şaşırtıyor.
 * </p>
 */
export function DatePicker({
  deger,
  degistir,
  id,
  enAz,
  enCok,
  temizlenebilir,
  className,
}: {
  /** `yyyy-MM-dd` ya da boş. */
  deger: string;
  degistir: (d: string) => void;
  id?: string;
  enAz?: string;
  enCok?: string;
  temizlenebilir?: boolean;
  className?: string;
}) {
  const secili = deger ? parseDay(deger) : null;
  const [acik, setAcik] = useState(false);
  const [gorunenAy, setGorunenAy] = useState(() => secili ?? new Date());

  return (
    <Popover.Root
      open={acik}
      onOpenChange={(a) => {
        setAcik(a);
        // Panel her açılışta seçili aya dönsün; kullanıcı geçen sefer nereye
        // gittiyse orada kalmak kafa karıştırıyor.
        if (a) setGorunenAy(secili ?? new Date());
      }}
    >
      <Popover.Trigger asChild>
        <button
          id={id}
          type="button"
          className={cn(
            'flex h-10 w-full items-center gap-2.5 rounded-control border border-border bg-surface px-3 text-left text-sm',
            'transition-colors hover:bg-surface-2 focus:border-brand focus:outline-hidden focus:ring-2 focus:ring-(--focus-ring)',
            className,
          )}
        >
          <CalendarDays size={15} className="shrink-0 text-text-3" />
          <span className={cn('min-w-0 flex-1 truncate', !secili && 'text-text-3')}>
            {secili ? uzunGun(secili) : 'Tarih seçin'}
          </span>
          {temizlenebilir && secili && (
            <span
              role="button"
              tabIndex={0}
              aria-label="Tarihi temizle"
              onClick={(e) => {
                e.stopPropagation();
                degistir('');
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.stopPropagation();
                  degistir('');
                }
              }}
              className="shrink-0 rounded px-1 text-2xs text-text-3 hover:text-text"
            >
              temizle
            </span>
          )}
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={6}
          className="katman anim-katman z-400 w-[290px] rounded-card border border-border bg-surface p-3 shadow-3"
        >
          <AyIzgarasi
            gorunenAy={gorunenAy}
            ayDegistir={setGorunenAy}
            secili={secili}
            sec={(t) => {
              degistir(dayText(t));
              setAcik(false);
            }}
            enAz={enAz}
            enCok={enCok}
          />
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}

/** Aylık ızgara — takvim panelinin gövdesi. */
function AyIzgarasi({
  gorunenAy,
  ayDegistir,
  secili,
  sec,
  enAz,
  enCok,
}: {
  gorunenAy: Date;
  ayDegistir: (t: Date) => void;
  secili: Date | null;
  sec: (t: Date) => void;
  enAz?: string;
  enCok?: string;
}) {
  const bugun = dayText(new Date());

  const gunler = useMemo(() => {
    const ilk = new Date(gorunenAy.getFullYear(), gorunenAy.getMonth(), 1);
    // Pazartesi = 0 olacak şekilde kaydır (JS'te pazar 0).
    const kaydirma = (ilk.getDay() + 6) % 7;
    const bas = new Date(ilk);
    bas.setDate(bas.getDate() - kaydirma);

    return Array.from({ length: 42 }, (_, i) => {
      const t = new Date(bas);
      t.setDate(t.getDate() + i);
      return t;
    });
  }, [gorunenAy]);

  const ayAtla = (adim: number) =>
    ayDegistir(new Date(gorunenAy.getFullYear(), gorunenAy.getMonth() + adim, 1));

  return (
    <>
      <div className="mb-2 flex items-center gap-1">
        <button
          type="button"
          onClick={() => ayAtla(-1)}
          aria-label="Önceki ay"
          className="grid h-8 w-8 place-items-center rounded-sm text-text-2 hover:bg-surface-2"
        >
          <ChevronLeft size={16} />
        </button>

        <p className="flex-1 text-center font-display text-sm font-bold">
          {AYLAR[gorunenAy.getMonth()]} {gorunenAy.getFullYear()}
        </p>

        <button
          type="button"
          onClick={() => ayAtla(1)}
          aria-label="Sonraki ay"
          className="grid h-8 w-8 place-items-center rounded-sm text-text-2 hover:bg-surface-2"
        >
          <ChevronRight size={16} />
        </button>
      </div>

      <div className="grid grid-cols-7 gap-0.5">
        {GUN_BASLIKLARI.map((g) => (
          <span
            key={g}
            className="grid h-7 place-items-center text-3xs font-semibold uppercase text-text-3"
          >
            {g}
          </span>
        ))}

        {gunler.map((t) => {
          const metin = dayText(t);
          const ayIcinde = t.getMonth() === gorunenAy.getMonth();
          const seciliMi = secili !== null && metin === dayText(secili);
          const bugunMu = metin === bugun;
          const kapali = (enAz && metin < enAz) || (enCok && metin > enCok);

          return (
            <button
              key={metin}
              type="button"
              disabled={!!kapali}
              onClick={() => sec(t)}
              aria-current={bugunMu ? 'date' : undefined}
              className={cn(
                'grid h-9 place-items-center rounded-sm text-sm tabular-nums transition-colors',
                ayIcinde ? 'text-text' : 'text-text-3',
                !seciliMi && !kapali && 'hover:bg-surface-2',
                seciliMi && 'bg-brand font-semibold text-on-brand',
                kapali && 'cursor-not-allowed opacity-35',
              )}
              // Bugün: altın halka — takvim ekranıyla aynı işaret.
              style={bugunMu && !seciliMi ? { boxShadow: 'inset 0 0 0 1.5px var(--gold)' } : undefined}
            >
              {t.getDate()}
            </button>
          );
        })}
      </div>

      <div className="mt-2 flex gap-1.5 border-t border-border pt-2">
        <button
          type="button"
          onClick={() => sec(new Date())}
          className="flex-1 rounded-sm py-1.5 text-xs font-medium text-brand-2 hover:bg-surface-2"
        >
          Bugün
        </button>
        <button
          type="button"
          onClick={() => {
            const y = new Date();
            y.setDate(y.getDate() + 1);
            sec(y);
          }}
          className="flex-1 rounded-sm py-1.5 text-xs font-medium text-text-2 hover:bg-surface-2"
        >
          Yarın
        </button>
      </div>
    </>
  );
}
