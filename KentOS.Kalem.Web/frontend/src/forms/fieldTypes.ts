import {
  AlignLeft, AtSign, Calendar, CheckSquare, ChevronDownSquare, Clock, CircleDot,
  FileUp, Grid3x3, Hash, Heading, IdCard, Image, Link2, ListOrdered, MapPin,
  Minus, PenLine, Phone, SlidersHorizontal, Star, ToggleLeft, Type,
} from 'lucide-react';
import type { ComponentType } from 'react';

/**
 * ALAN TİPİ KATALOĞU — tasarımcı paleti, oynatıcı ve özet TEK yerden okur.
 *
 * <p>
 * Sunucudaki <c>FormAlanTipi</c> enum'unun aynası. Sayılar <b>kalıcı
 * sözleşme</b>: yayınlanmış formların JSONB tanımı bu sayılarla saklanıyor.
 * Bir değeri değiştirmek canlıdaki bütün soruları başka tiplere çevirir —
 * sessizce, hiçbir hata vermeden.
 * </p>
 *
 * <p>
 * Katalogda 29 tip var ama <b>paletten 19'u çıkıyor</b>. Kapsam dışı
 * bırakma tasarımcının kararı: sunucu hepsini doğruluyor, yalnızca
 * seçilebilir olanları kısıyoruz.
 * </p>
 */

export const FIELD_TYPE = {
  kisaMetin: 0,
  uzunMetin: 1,
  eposta: 2,
  telefon: 3,
  tcKimlik: 4,
  url: 5,
  sayi: 10,
  tarih: 11,
  saat: 12,
  tarihSaat: 13,
  tarihAraligi: 14,
  tekSecim: 20,
  cokSecim: 21,
  acilirListe: 22,
  cokluAcilirListe: 23,
  evetHayir: 24,
  olcek: 30,
  nps: 31,
  yildiz: 32,
  matrisTekSecim: 40,
  matrisCokSecim: 41,
  siralama: 42,
  dosya: 50,
  konum: 51,
  imza: 52,
  baslik: 60,
  aciklama: 61,
  ayirici: 62,
  gorsel: 63,
} as const;

export type FieldTypeValue = (typeof FIELD_TYPE)[keyof typeof FIELD_TYPE];

/** Alan tipinin tasarımcıdaki künyesi. */
export type FieldTypeInfo = {
  tip: FieldTypeValue;
  ad: string;
  ipucu: string;
  ikon: ComponentType<{ size?: number | string; className?: string }>;
  grup: 'Metin' | 'Seçim' | 'Sayı ve tarih' | 'Ölçek' | 'Gelişmiş' | 'İçerik';
  /** Paletten seçilebilir mi? */
  palette: boolean;
  /** Seçenek listesi ister mi? */
  secenekli?: boolean;
  /** Satır/sütun ister mi (matris)? */
  matris?: boolean;
  /** Yanıt üretmeyen içerik bloğu mu? */
  blok?: boolean;
};

export const FIELD_TYPES: FieldTypeInfo[] = [
  // ── metin ──
  { tip: FIELD_TYPE.kisaMetin, ad: 'Kısa metin', ipucu: 'Tek satır', ikon: Type, grup: 'Metin', palette: true },
  { tip: FIELD_TYPE.uzunMetin, ad: 'Uzun metin', ipucu: 'Çok satır', ikon: AlignLeft, grup: 'Metin', palette: true },
  { tip: FIELD_TYPE.eposta, ad: 'E-posta', ipucu: 'Biçim denetimli', ikon: AtSign, grup: 'Metin', palette: true },
  { tip: FIELD_TYPE.telefon, ad: 'Telefon', ipucu: 'Biçim denetimli', ikon: Phone, grup: 'Metin', palette: true },
  { tip: FIELD_TYPE.tcKimlik, ad: 'T.C. kimlik no', ipucu: 'Algoritma denetimli', ikon: IdCard, grup: 'Metin', palette: true },
  { tip: FIELD_TYPE.url, ad: 'İnternet adresi', ipucu: '', ikon: Link2, grup: 'Metin', palette: false },

  // ── sayı ve tarih ──
  { tip: FIELD_TYPE.sayi, ad: 'Sayı', ipucu: 'Alt/üst sınır verilebilir', ikon: Hash, grup: 'Sayı ve tarih', palette: true },
  { tip: FIELD_TYPE.tarih, ad: 'Tarih', ipucu: '', ikon: Calendar, grup: 'Sayı ve tarih', palette: true },
  { tip: FIELD_TYPE.saat, ad: 'Saat', ipucu: '', ikon: Clock, grup: 'Sayı ve tarih', palette: true },
  { tip: FIELD_TYPE.tarihSaat, ad: 'Tarih ve saat', ipucu: '', ikon: Calendar, grup: 'Sayı ve tarih', palette: false },
  { tip: FIELD_TYPE.tarihAraligi, ad: 'Tarih aralığı', ipucu: '', ikon: Calendar, grup: 'Sayı ve tarih', palette: false },

  // ── seçim ──
  { tip: FIELD_TYPE.tekSecim, ad: 'Tek seçim', ipucu: 'Radyo düğmeleri', ikon: CircleDot, grup: 'Seçim', palette: true, secenekli: true },
  { tip: FIELD_TYPE.cokSecim, ad: 'Çok seçim', ipucu: 'Onay kutuları', ikon: CheckSquare, grup: 'Seçim', palette: true, secenekli: true },
  { tip: FIELD_TYPE.acilirListe, ad: 'Açılır liste', ipucu: 'Uzun listeler için', ikon: ChevronDownSquare, grup: 'Seçim', palette: true, secenekli: true },
  { tip: FIELD_TYPE.cokluAcilirListe, ad: 'Çoklu açılır liste', ipucu: '', ikon: ChevronDownSquare, grup: 'Seçim', palette: false, secenekli: true },
  { tip: FIELD_TYPE.evetHayir, ad: 'Evet / Hayır', ipucu: 'Tek anahtar', ikon: ToggleLeft, grup: 'Seçim', palette: true },

  // ── ölçek ──
  { tip: FIELD_TYPE.olcek, ad: 'Ölçek', ipucu: '1–5, 1–10…', ikon: SlidersHorizontal, grup: 'Ölçek', palette: true },
  { tip: FIELD_TYPE.nps, ad: 'Tavsiye skoru', ipucu: '0–10', ikon: SlidersHorizontal, grup: 'Ölçek', palette: false },
  { tip: FIELD_TYPE.yildiz, ad: 'Yıldız', ipucu: 'Memnuniyet', ikon: Star, grup: 'Ölçek', palette: true },

  // ── gelişmiş ──
  { tip: FIELD_TYPE.matrisTekSecim, ad: 'Matris (tek seçim)', ipucu: 'Satır × sütun', ikon: Grid3x3, grup: 'Gelişmiş', palette: true, matris: true },
  { tip: FIELD_TYPE.matrisCokSecim, ad: 'Matris (çok seçim)', ipucu: '', ikon: Grid3x3, grup: 'Gelişmiş', palette: false, matris: true },
  { tip: FIELD_TYPE.siralama, ad: 'Sıralama', ipucu: '', ikon: ListOrdered, grup: 'Gelişmiş', palette: false, secenekli: true },
  { tip: FIELD_TYPE.dosya, ad: 'Dosya yükleme', ipucu: 'Belge ya da fotoğraf', ikon: FileUp, grup: 'Gelişmiş', palette: true },
  { tip: FIELD_TYPE.konum, ad: 'Konum', ipucu: '', ikon: MapPin, grup: 'Gelişmiş', palette: false },
  { tip: FIELD_TYPE.imza, ad: 'İmza', ipucu: '', ikon: PenLine, grup: 'Gelişmiş', palette: false },

  // ── içerik (yanıt üretmez) ──
  { tip: FIELD_TYPE.baslik, ad: 'Başlık', ipucu: 'Bölüm başlığı', ikon: Heading, grup: 'İçerik', palette: true, blok: true },
  { tip: FIELD_TYPE.aciklama, ad: 'Açıklama', ipucu: 'Bilgi metni', ikon: AlignLeft, grup: 'İçerik', palette: true, blok: true },
  { tip: FIELD_TYPE.ayirici, ad: 'Ayırıcı', ipucu: 'Yatay çizgi', ikon: Minus, grup: 'İçerik', palette: true, blok: true },
  { tip: FIELD_TYPE.gorsel, ad: 'Görsel', ipucu: '', ikon: Image, grup: 'İçerik', palette: false, blok: true },
];

const HARITA = new Map(FIELD_TYPES.map((t) => [t.tip, t]));

/** Tip künyesi; bilinmeyen tipte güvenli bir varsayılan döner. */
export function fieldTypeInfo(tip: number | undefined): FieldTypeInfo {
  return HARITA.get(tip as FieldTypeValue) ?? FIELD_TYPES[0];
}

/** Palette çıkan tipler, grup sırasıyla. */
export const PALETTE_GROUPS = (
  ['Metin', 'Seçim', 'Sayı ve tarih', 'Ölçek', 'Gelişmiş', 'İçerik'] as const
).map((grup) => ({
  grup,
  tipler: FIELD_TYPES.filter((t) => t.grup === grup && t.palette),
}));

/** Bu tip yanıt üretmeyen bir içerik bloğu mu? */
export const isBlock = (tip: number | undefined) => fieldTypeInfo(tip).blok === true;

/** Koşul karşılaştırmaları — sunucudaki `FormKosulOperatoru` aynası. */
export const CONDITION_OP = {
  esit: 0, esitDegil: 1, icerir: 2, icermez: 3,
  dolu: 4, bos: 5, buyuk: 6, kucuk: 7,
} as const;

export const CONDITION_OP_LABELS: { deger: number; etiket: string; degersiz?: boolean }[] = [
  { deger: CONDITION_OP.esit, etiket: 'şuna eşitse' },
  { deger: CONDITION_OP.esitDegil, etiket: 'şuna eşit değilse' },
  { deger: CONDITION_OP.icerir, etiket: 'şunu içeriyorsa' },
  { deger: CONDITION_OP.icermez, etiket: 'şunu içermiyorsa' },
  { deger: CONDITION_OP.dolu, etiket: 'doldurulduysa', degersiz: true },
  { deger: CONDITION_OP.bos, etiket: 'boş bırakıldıysa', degersiz: true },
  { deger: CONDITION_OP.buyuk, etiket: 'şundan büyükse' },
  { deger: CONDITION_OP.kucuk, etiket: 'şundan küçükse' },
];

/** Form durumu — sunucudaki `FormDurumu` aynası. */
export const FORM_STATUS = { taslak: 0, yayinda: 1, kapali: 2, arsiv: 3 } as const;

/** Erişim kipi — sunucudaki `FormErisimi` aynası. */
export const FORM_ACCESS = { anonim: 0, telefonDogrulamali: 1, personel: 2 } as const;

export const FORM_ACCESS_LABELS = [
  { deger: FORM_ACCESS.anonim, etiket: 'Herkese açık', ipucu: 'Bağlantıyı bilen herkes doldurur; kimlik sorulmaz.' },
  { deger: FORM_ACCESS.telefonDogrulamali, etiket: 'Telefon ister', ipucu: 'Telefon numarası zorunlu; tek yanıt kuralı buna dayanır.' },
  { deger: FORM_ACCESS.personel, etiket: 'Yalnızca personel', ipucu: 'Giriş yapmış kullanıcılar doldurur.' },
];
