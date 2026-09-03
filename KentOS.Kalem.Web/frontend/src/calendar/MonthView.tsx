import { Lock, Plus, Repeat } from 'lucide-react';
import { useMemo } from 'react';
import { eventColors, colorOr } from '../components/Color';
import { cn } from '../components/utils';
import { startOfDay } from '../data/time';
import { isSameDay, addDays, startOfWeek } from './viewWindow';
import type { CalendarEvent } from './types';
import { eventRange } from './layout';

const GUN_ADLARI = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
const AY_ADLARI = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

/**
 * Ay görünümü — design.md §7.7.
 *
 * Hücre yüksekliği SABİT: taşan etkinlikler "+N daha" ile özetlenir. Eski
 * arayüzde etiketler hücreden taşıp ızgarayı bozuyordu.
 */
export function MonthView({
  imlec,
  etkinlikler,
  onGunSec,
  onEtkinlikAc,
  onGunEkle,
}: {
  imlec: Date;
  etkinlikler: CalendarEvent[];
  onGunSec: (g: Date) => void;
  onEtkinlikAc: (e: CalendarEvent) => void;
  /** Hücredeki + ile o güne etkinlik ekle. */
  onGunEkle?: (g: Date) => void;
}) {
  const ilkGun = startOfWeek(new Date(imlec.getFullYear(), imlec.getMonth(), 1));
  const bugun = startOfDay(new Date());

  const gunler = useMemo(
    () => Array.from({ length: 42 }, (_, i) => addDays(ilkGun, i)),
    [ilkGun],
  );

  const gunlereGore = useMemo(() => {
    const harita = new Map<string, CalendarEvent[]>();
    for (const e of etkinlikler) {
      const { bas } = eventRange(e);
      const anahtar = `${bas.getFullYear()}-${bas.getMonth()}-${bas.getDate()}`;
      (harita.get(anahtar) ?? harita.set(anahtar, []).get(anahtar)!).push(e);
    }
    return harita;
  }, [etkinlikler]);

  return (
    <div className="overflow-hidden rounded-card border border-border bg-surface">
      <div className="grid grid-cols-7 border-b border-border bg-surface-2">
        {GUN_ADLARI.map((g, i) => (
          <div
            key={g}
            className={cn(
              'p-2 text-center text-2xs font-semibold uppercase tracking-[0.09em] text-text-3',
              i >= 5 && 'text-brand-2',
            )}
          >
            {g}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7">
        {gunler.map((g) => {
          const anahtar = `${g.getFullYear()}-${g.getMonth()}-${g.getDate()}`;
          const gunEtkinlikleri = (gunlereGore.get(anahtar) ?? []).sort(
            (a, b) => eventRange(a).bas.getTime() - eventRange(b).bas.getTime(),
          );
          const ayIcinde = g.getMonth() === imlec.getMonth();
          const bugunMu = isSameDay(g, bugun);
          const gosterilecek = gunEtkinlikleri.slice(0, 3);
          const kalan = gunEtkinlikleri.length - gosterilecek.length;

          return (
            <button
              key={anahtar}
              onClick={() => onGunSec(g)}
              className={cn(
                'group relative min-h-[58px] border-b border-r border-border p-1.5 text-left md:min-h-[104px] md:p-2',
                // <button> içeriğini dikey ORTALAR: satırdaki hücreler farklı
                // sayıda etkinlik taşıyınca gün numaraları birbirini tutmuyordu.
                'flex flex-col overflow-hidden',
                !ayIcinde && 'bg-surface-2',
                bugunMu && 'bg-brand-tint',
              )}
              style={bugunMu ? { boxShadow: 'inset 0 0 0 1.5px var(--gold)' } : undefined}
            >
              <span
                className={cn(
                  'mb-1 block text-2xs font-semibold tabular-nums',
                  ayIcinde ? 'text-text' : 'text-text-3',
                )}
              >
                {g.getDate()}
              </span>

              {/*
                Hücre üzerine gelince "+": günün boş olduğu yerlerde bile
                etkinlik eklenebildiğini gösteren tek işaret. Hücrenin kendisi
                gün görünümüne gider, + ise doğrudan formu açar.
              */}
              {onGunEkle && (
                <span
                  role="button"
                  tabIndex={-1}
                  aria-label={`${g.getDate()} ${AY_ADLARI[g.getMonth()]} için etkinlik ekle`}
                  title="Etkinlik ekle"
                  onClick={(ev) => { ev.stopPropagation(); onGunEkle(g); }}
                  className="absolute right-1 top-1 hidden h-[18px] w-[18px] place-items-center rounded-sm bg-brand text-on-brand
                    opacity-0 shadow-1 transition-opacity group-hover:opacity-100 md:grid"
                >
                  <Plus size={11} strokeWidth={3} />
                </span>
              )}

              {/* Mobilde nokta, masaüstünde çip (design.md §7.7) */}
              <span className="flex gap-0.5 md:hidden">
                {gunEtkinlikleri.slice(0, 3).map((e) => (
                  <span
                    key={e.id}
                    className="h-[5px] w-[5px] rounded-full"
                    style={{ background: colorOr(e.durumRenk ?? e.tipRenk, 'var(--brand-hover)') }}
                  />
                ))}
              </span>

              <span className="hidden md:block">
                {gosterilecek.map((e) => {
                  const { bas } = eventRange(e);
                  const renkler = eventColors(e.durumRenk, e.tipRenk);
                  return (
                    <span
                      key={e.id}
                      onClick={(ev) => { ev.stopPropagation(); onEtkinlikAc(e); }}
                      title={[e.baslik, e.durumAd, e.tipAd].filter(Boolean).join(' · ')}
                      style={{ background: renkler.cipZemini, borderLeftColor: renkler.kenar }}
                      className="mb-0.5 block cursor-pointer rounded-sm border-l-2 px-1.5 py-[3px]"
                    >
                      <span className="flex items-center gap-1 font-display text-2xs font-semibold tabular-nums text-text-2">
                        {String(bas.getHours()).padStart(2, '0')}:{String(bas.getMinutes()).padStart(2, '0')}
                        {e.gizli && <Lock size={8} strokeWidth={2.4} />}
                        {e.seriId && <Repeat size={8} strokeWidth={2.4} />}
                      </span>
                      <span className="line-clamp-2 text-2xs leading-[1.2]">{e.baslik}</span>
                    </span>
                  );
                })}
                {kalan > 0 && (
                  <span className="block text-2xs font-medium text-brand-2">+{kalan} daha</span>
                )}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
