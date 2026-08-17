import { useMemo } from 'react';
import { cn } from '../components/utils';
import { startOfDay } from '../data/time';
import { isSameDay, addDays, startOfWeek } from './viewWindow';

const AY_ADLARI = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

/**
 * Yıl görünümü — 12 mini ay, yoğunluğa göre renklenmiş günler.
 *
 * Yılın tüm etkinliklerini çekmek yerine gün başına SAYAÇ kullanılır
 * (`/takvim/sayac`): 365 günlük tam veri hem ağır hem gereksiz, bu görünümde
 * zaten tek tek etkinlik gösterilmiyor.
 */
export function YearView({
  yil,
  sayaclar,
  onGunSec,
}: {
  yil: number;
  sayaclar: { gun: string; adet: number }[];
  onGunSec: (g: Date) => void;
}) {
  const bugun = startOfDay(new Date());

  const harita = useMemo(() => {
    const m = new Map<string, number>();
    for (const s of sayaclar) {
      const t = new Date(s.gun);
      m.set(`${t.getFullYear()}-${t.getMonth()}-${t.getDate()}`, s.adet);
    }
    return m;
  }, [sayaclar]);

  const enYogun = useMemo(
    () => Math.max(1, ...sayaclar.map((s) => s.adet)),
    [sayaclar],
  );

  return (
    /*
      YIL GÖRÜNÜMÜ YENİDEN ÖLÇEKLENDİ.
      Önce 8.5–9px puntolarla çiziliyordu: on iki ay bir ekrana sığsın diye
      her şey küçültülmüş, sonuçta gün numaraları okunmuyordu. Yıl görünümünün
      işi "hangi ay yoğun" sorusunu cevaplamak; bunun için on iki ayın aynı
      anda görünmesi gerekmiyor, üç sütun yeterli ve kaydırma zaten var.

      Punto artık token'dan (`--fs-3xs` / `--fs-2xs`), yani yazı boyutu
      knob'unu takip ediyor — 17px temel puntoda yıl görünümü de büyüyor.
    */
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {AY_ADLARI.map((ad, ayIndex) => {
        const ilk = startOfWeek(new Date(yil, ayIndex, 1));
        const gunler = Array.from({ length: 42 }, (_, i) => addDays(ilk, i));

        return (
          <div key={ad} className="rounded-card border border-line bg-surface p-3.5">
            <p className="mb-2.5 font-display text-sm font-bold tracking-[var(--track-d)] text-ink">{ad}</p>
            <div className="grid grid-cols-7 gap-1">
              {['P', 'S', 'Ç', 'P', 'C', 'C', 'P'].map((g, i) => (
                <span
                  key={i}
                  className="pb-0.5 text-center text-3xs font-semibold uppercase tracking-[0.06em] text-ink-3"
                >
                  {g}
                </span>
              ))}
              {gunler.map((g) => {
                const ayIcinde = g.getMonth() === ayIndex;
                const adet = harita.get(`${g.getFullYear()}-${g.getMonth()}-${g.getDate()}`) ?? 0;
                // Yoğunluk: 0 → boş, arttıkça marka rengine doygunlaşır.
                const oran = adet === 0 ? 0 : 0.25 + (adet / enYogun) * 0.75;

                return (
                  <button
                    key={g.toISOString()}
                    onClick={() => onGunSec(g)}
                    title={adet > 0 ? `${g.getDate()} ${ad}: ${adet} etkinlik` : undefined}
                    className={cn(
                      // Dokunma hedefi: 28px alt sınır. `aspect-square` tek
                      // başına, dar sütunda kareyi 14px'e kadar küçültüyordu.
                      'grid aspect-square min-h-[28px] place-items-center rounded-xs',
                      'text-2xs tabular-nums transition-colors',
                      adet === 0 && 'text-ink-2 hover:bg-sunken',
                      !ayIcinde && 'invisible',
                      isSameDay(g, bugun) && 'ring-[1.5px] ring-accent',
                    )}
                    style={
                      adet > 0
                        ? { background: `color-mix(in srgb, var(--brand-ui) ${oran * 100}%, transparent)`,
                            color: oran > 0.6 ? 'var(--on-brand)' : 'var(--ink)' }
                        : undefined
                    }
                  >
                    {g.getDate()}
                  </button>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
}
