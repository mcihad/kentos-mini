/**
 * Sunucu ile tarih alışverişi.
 *
 * KRİTİK: Sunucudaki tüm zaman damgaları `timestamp without time zone`,
 * yani KAYAN YEREL SAAT. Saat dilimi bilgisi taşımazlar. (Tekrar motoru da
 * bu yüzden elle yazıldı — hazır bir iCal kütüphanesi DST'de kaydırıyordu.)
 *
 * Bu yüzden `toISOString()` KULLANILMAZ: tarayıcı yerel saati UTC'ye çevirir
 * ve Türkiye için her etkinliği 3 saat kaydırır. Hata da sunucu hatası gibi
 * görünür, bulması zordur.
 */

/** Sunucudan gelen `2026-08-12T14:30:00` metnini yerel Date'e çevirir. */
export function serverToLocal(metin: string): Date {
  // Sondaki 'Z' veya offset varsa temizle — sunucu bunları göndermiyor,
  // gelirse de yerel saat olarak yorumlanmalı.
  const temiz = metin.replace(/(Z|[+-]\d{2}:?\d{2})$/, '');
  const [tarih, saat = '00:00:00'] = temiz.split('T');
  const [y, a, g] = tarih.split('-').map(Number);
  const [ss, dd, sn = 0] = saat.split(':').map(Number);
  return new Date(y, a - 1, g, ss, dd, Math.floor(sn));
}

/** Yerel Date'i sunucunun beklediği `2026-08-12T14:30:00` biçimine çevirir. */
export function localToServer(t: Date): string {
  const p = (n: number, u = 2) => String(n).padStart(u, '0');
  return `${t.getFullYear()}-${p(t.getMonth() + 1)}-${p(t.getDate())}` +
         `T${p(t.getHours())}:${p(t.getMinutes())}:${p(t.getSeconds())}`;
}

/** Günün başlangıcı (00:00:00). */
export function startOfDay(t: Date): Date {
  return new Date(t.getFullYear(), t.getMonth(), t.getDate());
}

/** Verilen dakikaya yuvarlar — takvimde 30 dakikalık ızgara için. */
export function roundToMinutes(t: Date, adim: number): Date {
  const d = new Date(t);
  d.setSeconds(0, 0);
  d.setMinutes(Math.round(d.getMinutes() / adim) * adim);
  return d;
}
