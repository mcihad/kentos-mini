/**
 * KÜRATÖRLÜ PALETLER — design_new/design.md §3.2.
 *
 * Renk seçimi serbest bir picker DEĞİL. Her seçenek gündüz+gece çifti olarak
 * elle dengelendi; böylece hiçbir seçim kontrast eşiğini (§11) bozamaz.
 * Serbest hex girişi eklenirse kontrast doğrulaması zorunlu hâle gelir —
 * o doğrulama olmadan kullanıcı, okunmayan bir arayüz üretebilir.
 *
 * Neden ÇİFT hex: tek hex'ten hem gündüz hem gece için kontrastı garanti eden
 * varyant `color-mix` ile üretilemiyor (beyazla karışım doygunluğu düşürür,
 * siyahla okunmaz olur). Gece modunda `--brand-ui: var(--brand-dk)` devreye
 * giriyor.
 */

export type ColorOption = { ad: string; acik: string; koyu: string };
export type NeutralOption = { ad: string; deger: string };
export type FontPair = { ad: string; baslik: string; govde: string };

export const BRAND_COLORS: ColorOption[] = [
  { ad: 'Lacivert', acik: '#002E6D', koyu: '#5B93E8' },
  { ad: 'Zümrüt', acik: '#0B5D45', koyu: '#4FBE94' },
  { ad: 'Bordo', acik: '#7A1F2B', koyu: '#E58189' },
  { ad: 'Petrol', acik: '#0E4C5C', koyu: '#4FB6C8' },
  { ad: 'Mor', acik: '#4B2E83', koyu: '#A98BE8' },
  { ad: 'Antrasit', acik: '#333A45', koyu: '#A6B3C6' },
];

export const ACCENT_COLORS: ColorOption[] = [
  { ad: 'Altın', acik: '#A78952', koyu: '#D8BB80' },
  { ad: 'Bakır', acik: '#A65A2E', koyu: '#E29A6B' },
  { ad: 'Turkuaz', acik: '#157F7F', koyu: '#5CC9C4' },
  { ad: 'Kiraz', acik: '#A8324A', koyu: '#EE8397' },
  { ad: 'Yeşil', acik: '#4A7A2B', koyu: '#97CE6E' },
  { ad: 'Gri', acik: '#7C8592', koyu: '#B3BDCB' },
];

export const NEUTRAL_COLORS: NeutralOption[] = [
  { ad: 'Sıcak kâğıt', deger: '#F5F4F0' },
  { ad: 'Nötr gri', deger: '#F4F4F5' },
  { ad: 'Soğuk mavi', deger: '#F1F4F9' },
];

/**
 * KURUMUN KENDİ RENKLERİ — ilk sıradaki seçeneğin üzerine yazılır.
 *
 * "Kurumsal" ön ayarı 0. indeksi kullanıyor. Kurum kaydında renk tanımlıysa o
 * indeksin DEĞERİ değiştirilir; böylece hem ön ayar hem tema panelindeki
 * "Kurumsal" seçeneği otomatik olarak o kurumun rengini gösterir. Diğer
 * paletler (Zümrüt, Bordo…) tasarlandığı gibi kalır — kullanıcı isterse
 * onlara geçebilir.
 *
 * Diziyi yerinde değiştirmek bilinçli: renkleri okuyan on kadar yer var
 * (tema motoru, panel, token çıktısı) ve hepsinin imzasını değiştirmek yerine
 * tek kaynak güncelleniyor. Değişiklikten sonra
 * `window.dispatchEvent(new Event(KURUM_TEMA_OLAYI))` ile tema motoru
 * uyarılır — CSS değişkenleri yeniden yazılsın diye.
 */
export const KURUM_TEMA_OLAYI = 'wc:kurum-teması-degisti';

/** Kurum renkleri gelmeden önceki fabrika değerleri — geri dönüş için. */
const FABRIKA = {
  marka: { ...BRAND_COLORS[0] },
  vurgu: { ...ACCENT_COLORS[0] },
  notr: { ...NEUTRAL_COLORS[0] },
};

/** `#RRGGBB` biçiminde mi? Bozuk değer paleti bozmasın. */
function gecerliRenk(deger: string | null | undefined): deger is string {
  return typeof deger === 'string' && /^#[0-9a-fA-F]{6}$/.test(deger.trim());
}

/**
 * Kurumun renklerini 0. palet seçeneğine yazar.
 *
 * Koyu karşılık verilmemişse fabrika değeri korunur: tek hex'ten gece modunda
 * okunabilir bir varyant türetmek `color-mix` ile güvenilir değil (beyazla
 * karışım doygunluğu düşürür, siyahla okunmaz olur) — bkz. dosya başı.
 */
export function markaPaletiniUygula(marka: {
  birincil?: string | null;
  birincilKoyu?: string | null;
  vurgu?: string | null;
  notr?: string | null;
}) {
  BRAND_COLORS[0] = {
    ad: 'Kurumsal',
    acik: gecerliRenk(marka.birincil) ? marka.birincil.trim() : FABRIKA.marka.acik,
    koyu: gecerliRenk(marka.birincilKoyu) ? marka.birincilKoyu.trim() : FABRIKA.marka.koyu,
  };

  ACCENT_COLORS[0] = {
    ad: 'Kurumsal vurgu',
    acik: gecerliRenk(marka.vurgu) ? marka.vurgu.trim() : FABRIKA.vurgu.acik,
    koyu: FABRIKA.vurgu.koyu,
  };

  if (gecerliRenk(marka.notr)) {
    NEUTRAL_COLORS[0] = { ad: 'Kurumsal zemin', deger: marka.notr.trim() };
  }
}

/**
 * Üç çiftin tamamı Türkçe diakritiklerini (İ ı Ğ ğ Ş ş Ö ö Ç ç Ü ü) tam
 * destekler. Gövdede `tabular-nums` zorunlu — tablo ve saat hizası için.
 */
export const FONTS: FontPair[] = [
  { ad: 'Kurumsal', baslik: 'Montserrat', govde: 'IBM Plex Sans' },
  { ad: 'Modern', baslik: 'Figtree', govde: 'Source Sans 3' },
  { ad: 'Editoryal', baslik: 'Archivo', govde: 'Karla' },
];
