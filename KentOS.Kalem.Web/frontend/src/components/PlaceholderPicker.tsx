import { useQuery } from '@tanstack/react-query';
import { Braces } from 'lucide-react';
import * as Popover from '@radix-ui/react-popover';
import { useState } from 'react';
import { api } from '../data/client';
import { useSwipeGestures } from './swipeGestures';

export type Placeholder = { ad: string; baslik: string; aciklama: string };

/**
 * Katalog SUNUCUDAN gelir.
 *
 * Web ve mobilde ayrı liste tutmak, birine yeni bir alan eklendiğinde
 * ötekinin sessizce eksik kalması demekti. Nadiren değişir, uzun süre taze.
 */
export function usePlaceholders() {
  return useQuery({
    queryKey: ['ayar', 'sms-yer-tutucular'] as const,
    queryFn: () => api.get<Placeholder[]>('/ayar/sms-yer-tutucular'),
    staleTime: 60 * 60_000,
  });
}

/**
 * Metni İMLEÇ KONUMUNA yer tutucu ekleyerek döndürür.
 *
 * Sona eklemek işe yaramıyor: kullanıcı "Sayın {alici}," yazmak istiyor, yani
 * yer tutucu cümlenin İÇİNE giriyor. Seçili bir aralık varsa üzerine yazılır.
 */
export function insertPlaceholder(
  alan: HTMLInputElement | HTMLTextAreaElement | null,
  metin: string,
  ad: string,
): { yeni: string; imlec: number } {
  const jeton = `{${ad}}`;
  const bas = alan?.selectionStart ?? metin.length;
  const son = alan?.selectionEnd ?? metin.length;

  const yeni = metin.slice(0, bas) + jeton + metin.slice(son);
  return { yeni, imlec: bas + jeton.length };
}

/**
 * Yer tutucu seçici.
 *
 * Düğme metin alanının hemen yanında durur; seçilen yer tutucu imleç
 * konumuna eklenir ve odak metin alanına GERİ döner — aksi hâlde kullanıcı
 * her eklemeden sonra alana yeniden tıklamak zorunda kalıyordu.
 */
export function PlaceholderPicker({
  ekle,
  className,
}: {
  ekle: (ad: string) => void;
  className?: string;
}) {
  const [acik, setAcik] = useState(false);
  const { data } = usePlaceholders();
  const { kap: listeRef, baglar } = useSwipeGestures<HTMLDivElement>();

  if (!data?.length) return null;

  return (
    <Popover.Root open={acik} onOpenChange={setAcik}>
      <Popover.Trigger asChild>
        <button
          type="button"
          className={
            'flex h-8 items-center gap-1.5 rounded-control border border-border bg-surface px-2.5 ' +
            'text-sm font-medium text-text-2 transition-colors hover:bg-surface-2 ' +
            'focus:border-brand focus:outline-hidden focus:ring-2 focus:ring-(--focus-ring) ' +
            (className ?? '')
          }
          aria-label="Yer tutucu ekle"
          title="Mesaja otomatik doldurulan alan ekle"
        >
          <Braces size={14} className="text-text-3" />
          Alan ekle
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={6}
          className="katman anim-katman z-400 w-[260px] rounded-card border border-border bg-surface p-1 shadow-3"
        >
          <p className="px-2 pb-1 pt-1.5 text-2xs uppercase tracking-wider text-text-3">
            Gönderirken doldurulur
          </p>
          <div
            ref={listeRef}
            {...baglar}
            className="max-h-[280px] touch-pan-y overflow-y-auto overscroll-contain"
          >
            {data.map((y) => (
              <button
                key={y.ad}
                type="button"
                onClick={() => {
                  ekle(y.ad);
                  setAcik(false);
                }}
                className="block w-full rounded-sm px-2.5 py-1.5 text-left transition-colors hover:bg-surface-2"
              >
                <span className="flex items-baseline gap-1.5">
                  <span className="text-sm font-semibold">{y.baslik}</span>
                  <code className="rounded bg-surface-2 px-1 text-2xs tabular-nums text-text-3">
                    {`{${y.ad}}`}
                  </code>
                </span>
                <span className="mt-0.5 block text-xs leading-[1.4] text-text-3">
                  {y.aciklama}
                </span>
              </button>
            ))}
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
