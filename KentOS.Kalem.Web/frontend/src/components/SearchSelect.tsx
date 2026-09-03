import * as Popover from '@radix-ui/react-popover';
import { useSwipeGestures } from './swipeGestures';
import { Check, ChevronDown, Search, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { cn } from './utils';

export type PickerItem = { id: number; ad: string; aciklama?: string | null };

/**
 * Sunucuda arayan seçici.
 *
 * <p>
 * <c>&lt;select&gt;</c> KULLANILMIYOR: mahalle listesi binlerce satır,
 * meslek listesi yüzlerce. Hepsini indirip tarayıcıda süzmek hem ilk açılışı
 * yavaşlatıyor hem de mobilde kaydırılamaz bir liste üretiyor. Burada
 * kullanıcı yazar, sunucu süzer.
 * </p>
 *
 * <p>
 * Seçili kaydın ADI dışarıdan verilir (<c>seciliAd</c>): düzenlemede kayıt
 * arama sonuçlarında olmayabilir ve alan boş görünürdü.
 * </p>
 */
export function SearchSelect({
  deger,
  seciliAd,
  degistir,
  ogeler,
  ara,
  araDegistir,
  yukleniyor,
  yerTutucu = 'Seçin',
  bosMetin = 'Kayıt bulunamadı.',
  id,
}: {
  deger: number | null;
  seciliAd?: string | null;
  degistir: (id: number | null, ad: string | null) => void;
  ogeler: PickerItem[];
  ara: string;
  araDegistir: (a: string) => void;
  yukleniyor?: boolean;
  yerTutucu?: string;
  bosMetin?: string;
  id?: string;
}) {
  const [acik, setAcik] = useState(false);

  // Tekerlek ve parmak AÇIKÇA bağlanır: Radix panel içindeki
  // kaydırmayı yutuyor ve uzun mahalle/meslek listeleri
  // kaydırılamıyordu.
  const { kap: listeRef, baglar: kaydirmaBaglari } =
    useSwipeGestures<HTMLDivElement>();
  const [yerelArama, setYerelArama] = useState(ara);

  // Yazarken her tuşta istek atmamak için geciktir.
  useEffect(() => {
    const z = setTimeout(() => araDegistir(yerelArama), 250);
    return () => clearTimeout(z);
  }, [yerelArama, araDegistir]);

  const seciliOge = ogeler.find((o) => o.id === deger);
  const gosterilen = seciliOge?.ad ?? seciliAd ?? null;

  return (
    <Popover.Root open={acik} onOpenChange={setAcik}>
      <Popover.Trigger asChild>
        <button
          id={id}
          type="button"
          className="flex h-10 w-full items-center gap-2 rounded-control border border-border bg-surface px-3 text-left text-sm
            transition-colors hover:bg-surface-2 focus:border-brand focus:outline-hidden focus:ring-2 focus:ring-(--focus-ring)"
        >
          <span className={cn('min-w-0 flex-1 truncate', !gosterilen && 'text-text-3')}>
            {gosterilen ?? yerTutucu}
          </span>
          {gosterilen && (
            <span
              role="button"
              tabIndex={0}
              aria-label="Seçimi temizle"
              onClick={(e) => {
                e.stopPropagation();
                degistir(null, null);
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                  e.stopPropagation();
                  degistir(null, null);
                }
              }}
              className="shrink-0 rounded p-0.5 text-text-3 hover:text-text"
            >
              <X size={13} />
            </span>
          )}
          <ChevronDown size={14} className="shrink-0 text-text-3" />
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={6}
          className="katman anim-katman z-400 w-(--radix-popover-trigger-width) min-w-[240px] rounded-card border border-border bg-surface p-1.5 shadow-3"
        >
          <div className="relative mb-1.5">
            <Search
              size={14}
              className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-text-3"
            />
            <input
              autoFocus
              value={yerelArama}
              onChange={(e) => setYerelArama(e.target.value)}
              placeholder="Yazarak arayın"
              className="h-9 w-full rounded-control border border-border bg-surface-2 pl-8 pr-2.5 text-sm outline-hidden focus:border-brand"
            />
          </div>

          <div
            ref={listeRef}
            {...kaydirmaBaglari}
            className="max-h-[260px] touch-pan-y overflow-y-auto overscroll-contain"
          >
            {yukleniyor ? (
              <p className="px-2.5 py-3 text-sm text-text-3">Aranıyor…</p>
            ) : ogeler.length === 0 ? (
              <p className="px-2.5 py-3 text-sm text-text-3">{bosMetin}</p>
            ) : (
              ogeler.map((o) => {
                const secili = o.id === deger;
                return (
                  <button
                    key={o.id}
                    type="button"
                    onClick={() => {
                      degistir(o.id, o.ad);
                      setAcik(false);
                    }}
                    className={cn(
                      'flex w-full items-center gap-2 rounded-sm px-2.5 py-2 text-left text-sm transition-colors',
                      secili ? 'bg-brand-tint font-medium' : 'hover:bg-surface-2',
                    )}
                  >
                    <span className="min-w-0 flex-1 truncate">
                      {o.ad}
                      {o.aciklama && (
                        <span className="block truncate text-2xs text-text-3">{o.aciklama}</span>
                      )}
                    </span>
                    {secili && <Check size={14} className="shrink-0 text-brand-2" />}
                  </button>
                );
              })
            )}
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
