import { CalendarDays, Lock, MapPin, Repeat } from 'lucide-react';
import { useMemo } from 'react';
import { EmptyState } from '../components/EmptyState';
import { ColoredBadge, colorOr } from '../components/Color';
import { cn } from '../components/utils';
import { startOfDay } from '../data/time';
import { isSameDay } from './viewWindow';
import type { CalendarEvent } from './types';
import { eventRange } from './layout';

const GUN_ADLARI = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'];
const AY_KISA = ['OCA', 'ŞUB', 'MAR', 'NIS', 'MAY', 'HAZ', 'TEM', 'AĞU', 'EYL', 'EKI', 'KAS', 'ARA'];

/** Ajanda görünümü — kronolojik liste, "şu an" çizgisiyle (design.md §7.6). */
export function AgendaView({
  etkinlikler,
  onEtkinlikAc,
}: {
  etkinlikler: CalendarEvent[];
  onEtkinlikAc: (e: CalendarEvent) => void;
}) {
  const bugun = startOfDay(new Date());

  const gunler = useMemo(() => {
    const harita = new Map<number, CalendarEvent[]>();
    for (const e of etkinlikler) {
      const g = startOfDay(eventRange(e).bas).getTime();
      (harita.get(g) ?? harita.set(g, []).get(g)!).push(e);
    }
    return [...harita.entries()]
      .sort((a, b) => a[0] - b[0])
      .map(([zaman, liste]) => ({
        gun: new Date(zaman),
        liste: liste.sort(
          (a, b) => eventRange(a).bas.getTime() - eventRange(b).bas.getTime(),
        ),
      }));
  }, [etkinlikler]);

  if (gunler.length === 0) {
    return (
      <EmptyState
        ikon={CalendarDays}
        baslik="Bu aralıkta etkinlik yok"
        aciklama="Farklı bir tarih aralığı seçin ya da yeni etkinlik oluşturun."
      />
    );
  }

  const simdi = new Date();

  return (
    <div className="space-y-4">
      {gunler.map(({ gun, liste }) => (
        <section key={gun.getTime()}>
          <h3 className="mb-2 flex items-baseline gap-2">
            <span className="font-display text-lg font-bold tabular-nums">
              {gun.getDate()} {AY_KISA[gun.getMonth()]}
            </span>
            <span className="text-xs text-text-3">{GUN_ADLARI[gun.getDay()]}</span>
            {isSameDay(gun, bugun) && (
              <span className="rounded-full bg-gold-tint px-2 py-0.5 text-2xs font-semibold text-gold-2">
                Bugün
              </span>
            )}
          </h3>

          <div className="space-y-1.5">
            {liste.map((e, i) => {
              const { bas, bit } = eventRange(e);
              const oncekiBitti = i > 0 && eventRange(liste[i - 1]).bit <= simdi;
              const suAnCizgisi = isSameDay(gun, bugun) && oncekiBitti && bas > simdi;

              return (
                <div key={e.id}>
                  {suAnCizgisi && <SuAnSatiri />}
                  <button
                    onClick={() => onEtkinlikAc(e)}
                    className={cn(
                      'flex w-full gap-3 rounded-md border border-border bg-surface p-3 text-left shadow-1',
                      'transition-colors hover:border-border-2',
                    )}
                  >
                    <span className="w-[62px] shrink-0 text-right">
                      <span className="block font-display text-sm font-bold tabular-nums">
                        {saat(bas)}
                      </span>
                      <span className="block text-2xs tabular-nums text-text-3">{saat(bit)}</span>
                    </span>

                    {/* Durum rengi şeridi — listede durumu bir bakışta okutur. */}
                    <span
                      className="w-[3px] shrink-0 self-stretch rounded-full"
                      style={{ background: colorOr(e.durumRenk ?? e.tipRenk, 'var(--brand-ui)') }}
                      aria-hidden
                    />

                    <span className="min-w-0 flex-1">
                      <span className="flex items-center gap-1.5">
                        {e.gizli && <Lock size={12} strokeWidth={2.2} className="shrink-0 text-text-3" />}
                        {e.seriId && <Repeat size={12} strokeWidth={2.2} className="shrink-0 text-text-3" />}
                        <span className="truncate font-display text-sm font-bold metin-guzel">
                          {e.baslik}
                        </span>
                      </span>
                      {(e.durumAd || e.tipAd) && (
                        <span className="mt-1 flex flex-wrap items-center gap-1.5">
                          {e.durumAd && <ColoredBadge etiket={e.durumAd} renk={e.durumRenk} />}
                          {e.tipAd && (
                            <ColoredBadge etiket={e.tipAd} renk={e.tipRenk} nokta={false} />
                          )}
                        </span>
                      )}

                      {e.konum && (
                        <span className="mt-0.5 flex items-center gap-1 text-xs text-text-2">
                          <MapPin size={12} strokeWidth={1.8} />
                          {e.konum}
                        </span>
                      )}
                    </span>
                  </button>
                </div>
              );
            })}
          </div>
        </section>
      ))}
    </div>
  );
}

function saat(t: Date) {
  return `${String(t.getHours()).padStart(2, '0')}:${String(t.getMinutes()).padStart(2, '0')}`;
}

function SuAnSatiri() {
  const simdi = new Date();
  return (
    <div className="my-2 flex items-center gap-2" aria-hidden>
      <span className="font-display text-2xs font-bold tabular-nums text-gold-2">
        {saat(simdi)} ŞU AN
      </span>
      <span className="h-px flex-1 bg-gold" />
      <span className="h-[7px] w-[7px] rounded-full bg-gold" />
    </div>
  );
}
