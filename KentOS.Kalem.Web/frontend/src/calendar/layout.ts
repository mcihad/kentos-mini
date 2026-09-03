import { serverToLocal } from '../data/time';
import { SLOT_MINUTES, DEFAULT_DURATION_MINUTES, type CalendarEvent } from './types';

/** Bir 30 dakikalık dilimin piksel yüksekliği. Gün ızgarasının ölçeği budur. */
export const SLOT_HEIGHT = 28;
export const HOUR_HEIGHT = SLOT_HEIGHT * 2;
export const DAY_HEIGHT = HOUR_HEIGHT * 24;

/** Gece yarısından itibaren geçen dakika. */
export function minuteOffset(t: Date): number {
  return t.getHours() * 60 + t.getMinutes();
}

export function minutesToPixels(dk: number): number {
  return (dk / SLOT_MINUTES) * SLOT_HEIGHT;
}

export function pixelsToMinutes(px: number): number {
  return (px / SLOT_HEIGHT) * SLOT_MINUTES;
}

/** Piksel deltasını 30 dakikalık ızgaraya oturtur. */
export function snapToSlot(px: number): number {
  return Math.round(px / SLOT_HEIGHT) * SLOT_HEIGHT;
}

export function eventRange(e: CalendarEvent): { bas: Date; bit: Date } {
  const bas = serverToLocal(e.baslangic);
  const bit = e.bitis
    ? serverToLocal(e.bitis)
    : new Date(bas.getTime() + DEFAULT_DURATION_MINUTES * 60_000);
  return { bas, bit };
}

export type EventLayout = {
  etkinlik: CalendarEvent;
  ustPx: number;
  yukseklikPx: number;
  /** Çakışan grup içindeki sütun sırası. */
  sutun: number;
  /** Gruptaki toplam sütun sayısı. */
  sutunSayisi: number;
};

/**
 * Çakışan etkinlikleri yan yana sütunlara paketler.
 *
 * Çakışma ENGELLENMEZ — mevcut sistemde aynı saate birden fazla etkinlik
 * konabiliyor ve bu meşru bir durum. Yapılan tek şey, hepsini görünür kılmak.
 *
 * Algoritma: zamana göre sırala, kesişen ardışık etkinlikleri bir "küme"de
 * topla, kümedeki her etkinliği boş olan ilk sütuna yerleştir.
 */
export function layoutDay(etkinlikler: CalendarEvent[], gun: Date): EventLayout[] {
  const gunBas = new Date(gun.getFullYear(), gun.getMonth(), gun.getDate());
  const gunBit = new Date(gunBas.getTime() + 24 * 60 * 60_000);

  const parcalar = etkinlikler
    .filter((e) => !e.tumGun)
    .map((e) => ({ e, ...eventRange(e) }))
    // Bu güne değen her şey (gece yarısını aşanlar dahil)
    .filter((p) => p.bas < gunBit && p.bit > gunBas)
    .map((p) => {
      // Gün sınırlarına kırp; taşan kısım komşu günde çizilir.
      const bas = p.bas < gunBas ? gunBas : p.bas;
      const bit = p.bit > gunBit ? gunBit : p.bit;
      return { e: p.e, bas, bit };
    })
    .sort((a, b) => a.bas.getTime() - b.bas.getTime() || b.bit.getTime() - a.bit.getTime());

  const sonuc: EventLayout[] = [];
  let kume: typeof parcalar = [];
  let kumeSonu = 0;

  const kumeyiYerlestir = () => {
    if (kume.length === 0) return;

    // Her sütunun o an dolu olduğu bitiş zamanı
    const sutunBitisleri: number[] = [];
    const atamalar = kume.map((p) => {
      let s = sutunBitisleri.findIndex((bit) => bit <= p.bas.getTime());
      if (s === -1) { s = sutunBitisleri.length; }
      sutunBitisleri[s] = p.bit.getTime();
      return { p, sutun: s };
    });

    const sutunSayisi = sutunBitisleri.length;
    for (const { p, sutun } of atamalar) {
      const basDk = minuteOffset(p.bas);
      const bitDk = p.bit.getTime() >= gunBit.getTime() ? 24 * 60 : minuteOffset(p.bit);
      sonuc.push({
        etkinlik: p.e,
        ustPx: minutesToPixels(basDk),
        // En az bir dilim yüksekliğinde çiz — 15 dakikalık bir kayıt kaybolmasın.
        yukseklikPx: Math.max(minutesToPixels(bitDk - basDk), SLOT_HEIGHT - 2),
        sutun,
        sutunSayisi,
      });
    }
    kume = [];
  };

  for (const p of parcalar) {
    if (kume.length > 0 && p.bas.getTime() >= kumeSonu) {
      kumeyiYerlestir();
      kumeSonu = 0;
    }
    kume.push(p);
    kumeSonu = Math.max(kumeSonu, p.bit.getTime());
  }
  kumeyiYerlestir();

  return sonuc;
}

/**
 * Verilen aralıkla çakışan diğer etkinlikler.
 *
 * Kaydetmeyi ENGELLEMEZ; yalnızca kullanıcıyı uyarmak için. Yüklenmiş veri
 * üzerinden hesaplanır, yani tavsiye niteliğindedir.
 */
export function overlapping(
  etkinlikler: CalendarEvent[],
  bas: Date,
  bit: Date,
  haricId?: number,
): CalendarEvent[] {
  return etkinlikler.filter((e) => {
    if (e.id === haricId || e.tumGun) return false;
    const a = eventRange(e);
    return a.bas < bit && a.bit > bas;
  });
}
