import { startOfDay, localToServer } from '../data/time';
import type { CalendarView } from './types';

/**
 * Görünüme göre sunucudan çekilecek tarih penceresi.
 *
 * Gün görünümünde bir gün ÖNCE ve SONRA da alınır: gece yarısını aşan
 * sürüklemelerde komşu günün etkinlikleri de yerleşim hesabına girmeli.
 */
export function computeWindow(gorunum: CalendarView, imlec: Date): { bas: Date; bit: Date } {
  const g = startOfDay(imlec);

  switch (gorunum) {
    case 'gun':
      return { bas: addDays(g, -1), bit: addDays(g, 2) };

    case 'hafta': {
      // Bir gün öncesi/sonrası da alınır: gece yarısını aşan etkinlikler
      // komşu sütunda da çizilmeli.
      const pzt = startOfWeek(g);
      return { bas: addDays(pzt, -1), bit: addDays(pzt, 8) };
    }

    case 'ay': {
      const ayBasi = new Date(g.getFullYear(), g.getMonth(), 1);
      // Izgara pazartesiden başlar ve 6 hafta gösterir (design.md §7.7).
      const ilkGun = startOfWeek(ayBasi);
      return { bas: ilkGun, bit: addDays(ilkGun, 42) };
    }

    case 'yil': {
      const yilBasi = new Date(g.getFullYear(), 0, 1);
      return { bas: yilBasi, bit: new Date(g.getFullYear() + 1, 0, 1) };
    }

    case 'ajanda':
      return { bas: g, bit: addDays(g, 60) };
  }
}

export function addDays(t: Date, gun: number): Date {
  const d = new Date(t);
  d.setDate(d.getDate() + gun);
  return d;
}

/** Hafta pazartesiden başlar (design.md §9). */
export function startOfWeek(t: Date): Date {
  const d = startOfDay(t);
  const gun = d.getDay(); // 0 = pazar
  const geri = gun === 0 ? 6 : gun - 1;
  return addDays(d, -geri);
}

/** Query anahtarında kullanılacak kararlı pencere metni. */
export function windowKey(p: { bas: Date; bit: Date }): [string, string] {
  return [localToServer(p.bas), localToServer(p.bit)];
}

export function isSameDay(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear()
      && a.getMonth() === b.getMonth()
      && a.getDate() === b.getDate();
}
