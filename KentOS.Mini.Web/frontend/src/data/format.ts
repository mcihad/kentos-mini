import { serverToLocal } from './time';

/**
 * Sunucudan gelen tarih metinlerini ekrana basmak için biçimlendiriciler.
 *
 * Hepsi `serverToLocal` üzerinden geçer — doğrudan `new Date(metin)`
 * KULLANILMAZ: tarayıcı `2026-08-12T14:30:00` biçimini UTC sayıp saati
 * kaydırabilir. (Sunucudaki tüm damgalar saat dilimsiz yerel zamandır.)
 */

const MONTHS = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

const DAYS = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'];

const pad2 = (n: number) => String(n).padStart(2, '0');

/** `12 Ağustos 2026` */
export function date(metin?: string | null): string {
  if (!metin) return '—';
  const t = serverToLocal(metin);
  return `${t.getDate()} ${MONTHS[t.getMonth()]} ${t.getFullYear()}`;
}

/** `12 Ağu 2026` — dar alanlar için. */
export function shortDate(metin?: string | null): string {
  if (!metin) return '—';
  const t = serverToLocal(metin);
  return `${pad2(t.getDate())}.${pad2(t.getMonth() + 1)}.${t.getFullYear()}`;
}

/** `14:30` */
export function time(metin?: string | null): string {
  if (!metin) return '—';
  const t = serverToLocal(metin);
  return `${pad2(t.getHours())}:${pad2(t.getMinutes())}`;
}

/** `12 Ağustos 2026, 14:30` */
export function dateTime(metin?: string | null): string {
  if (!metin) return '—';
  return `${date(metin)}, ${time(metin)}`;
}

/** `AĞU` — takvim rozetlerinde. */
export function monthShort(metin?: string | null): string {
  if (!metin) return '';
  return MONTHS[serverToLocal(metin).getMonth()].slice(0, 3).toLocaleUpperCase('tr-TR');
}

/** `12` — ayın günü. */
export function dayNumber(metin?: string | null): string {
  if (!metin) return '';
  return String(serverToLocal(metin).getDate());
}

/** `Çarşamba` */
export function dayName(metin?: string | null): string {
  if (!metin) return '';
  return DAYS[serverToLocal(metin).getDay()];
}

/**
 * `3 gün sonra` / `2 saat önce` / `az önce`
 *
 * Mutlak tarihin YERİNE değil, YANINDA kullanılır: "3 gün sonra" tek başına
 * hangi gün olduğunu söylemez.
 */
export function relativeTime(metin?: string | null, simdi = new Date()): string {
  if (!metin) return '';
  const t = serverToLocal(metin);
  const fark = t.getTime() - simdi.getTime();
  const mutlak = Math.abs(fark);
  const gelecek = fark > 0;

  const dk = Math.round(mutlak / 60_000);
  if (dk < 1) return 'az önce';
  if (dk < 60) return gelecek ? `${dk} dakika sonra` : `${dk} dakika önce`;

  const sa = Math.round(dk / 60);
  if (sa < 24) return gelecek ? `${sa} saat sonra` : `${sa} saat önce`;

  const gun = Math.round(sa / 24);
  if (gun === 1) return gelecek ? 'yarın' : 'dün';
  if (gun < 30) return gelecek ? `${gun} gün sonra` : `${gun} gün önce`;

  const ay = Math.round(gun / 30);
  if (ay < 12) return gelecek ? `${ay} ay sonra` : `${ay} ay önce`;

  const yil = Math.round(ay / 12);
  return gelecek ? `${yil} yıl sonra` : `${yil} yıl önce`;
}

/** `14:30 – 15:00` (aynı gün) veya `12 Ağu 14:30 – 13 Ağu 09:00` */
export function range(bas?: string | null, bit?: string | null, tumGun?: boolean): string {
  if (!bas) return '—';
  if (tumGun) return 'Tüm gün';
  if (!bit) return time(bas);

  const b = serverToLocal(bas);
  const s = serverToLocal(bit);
  const ayniGun =
    b.getFullYear() === s.getFullYear() &&
    b.getMonth() === s.getMonth() &&
    b.getDate() === s.getDate();

  return ayniGun
    ? `${time(bas)} – ${time(bit)}`
    : `${shortDate(bas)} ${time(bas)} – ${shortDate(bit)} ${time(bit)}`;
}

/** `1 sa 30 dk` */
export function duration(dakika?: number | null): string {
  if (!dakika || dakika <= 0) return '—';
  const sa = Math.floor(dakika / 60);
  const dk = Math.round(dakika % 60);
  if (sa === 0) return `${dk} dk`;
  if (dk === 0) return `${sa} sa`;
  return `${sa} sa ${dk} dk`;
}

/** `1.234` — binlik ayracı Türkçe. */
export function number(n?: number | null): string {
  if (n === null || n === undefined) return '—';
  return n.toLocaleString('tr-TR');
}

/** `%67` */
export function percent(n?: number | null, basamak = 0): string {
  if (n === null || n === undefined) return '—';
  return `%${n.toFixed(basamak)}`;
}

/** `2,4 MB` */
export function fileSize(bayt?: number | null): string {
  if (!bayt) return '—';
  const birimler = ['B', 'KB', 'MB', 'GB'];
  let d = bayt;
  let i = 0;
  while (d >= 1024 && i < birimler.length - 1) {
    d /= 1024;
    i++;
  }
  return `${d.toFixed(i === 0 ? 0 : 1).replace('.', ',')} ${birimler[i]}`;
}

/** Ad Soyad baş harfleri — avatar yerine. */
export function initials(...parcalar: (string | null | undefined)[]): string {
  return parcalar
    .filter(Boolean)
    .map((p) => p!.trim()[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toLocaleUpperCase('tr-TR');
}

/**
 * Birim etiketini yetkilisiyle birlikte üretir.
 *
 * <p>
 * Yalnızca birim adı yazmak ayırt etmiyordu: kurumda <b>altı ayrı "Başkan
 * Yardımcısı"</b> birimi var ve listede hepsi aynı görünüyordu — kullanıcı
 * hangisine havale ettiğini bilemiyordu. Yetkilinin adı ayırt edici tek bilgi.
 * </p>
 */
export function unitLabel(
  birim: { ad?: string | null; yetkili?: string | null } | null | undefined,
): string {
  if (!birim) return '';
  const ad = (birim.ad ?? '').trim();
  const yetkili = (birim.yetkili ?? '').trim();

  // Yetkili yoksa ya da adın kendisiyle aynıysa tekrar etme.
  if (!yetkili || yetkili === ad) return ad;
  return `${ad} — ${yetkili}`;
}
