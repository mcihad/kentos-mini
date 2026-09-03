import { useMemo } from 'react';
import { addDays, startOfWeek } from './viewWindow';
import type { CalendarEvent } from './types';
import { TimeGrid, type DragResult } from './TimeGrid';

type OrtakOzellikler = {
  etkinlikler: CalendarEvent[];
  onEtkinlikAc: (e: CalendarEvent) => void;
  onZamanDegisti: (s: DragResult) => void;
  /** Boş bir yarım saatlik dilime tıklandı — o saatte etkinlik oluştur. */
  onBosDilim?: (baslangic: Date) => void;
};

/** Gün görünümü — tek sütunlu saat ızgarası. */
export function DayView({ gun, ...kalan }: OrtakOzellikler & { gun: Date }) {
  const gunler = useMemo(() => [gun], [gun.getTime()]);
  return <TimeGrid gunler={gunler} {...kalan} />;
}

/**
 * Hafta görünümü — pazartesiden başlayan yedi sütun.
 *
 * Sürükleme burada <b>gün de değiştirir</b>: bir etkinliği çarşambadan cumaya
 * taşımak, gün görünümünde üç kez ileri gitmeyi gerektiriyordu.
 */
export function WeekView({ imlec, ...kalan }: OrtakOzellikler & { imlec: Date }) {
  const gunler = useMemo(() => {
    const pzt = startOfWeek(imlec);
    return Array.from({ length: 7 }, (_, i) => addDays(pzt, i));
  }, [imlec.getTime()]);

  return <TimeGrid gunler={gunler} {...kalan} />;
}

export type { DragResult };
