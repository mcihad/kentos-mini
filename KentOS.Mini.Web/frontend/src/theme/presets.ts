/**
 * HAZIR TEMALAR — design_new/design.md §3.2, demodaki değerlerle birebir.
 *
 * Preset bir "renk seçimi" değil, 14 knob'un tamamının tutarlı bir kombinasyonu:
 * Bordo klasik kimlikler için köşeleri sertleştirip kenarlığı kalınlaştırıyor,
 * Petrol yumuşak ve geniş, Yüksek Kontrast ise gölgeyi tamamen kapatıp
 * kenarlıkla çalışıyor. Tek tek knob çevirmek yerine kuruma en yakın preseti
 * seçip oradan ince ayar yapmak beklenen kullanım.
 */
export type ThemeKnobs = {
  mod: 'acik' | 'koyu';
  marka: number;
  vurgu: number;
  notr: number;
  font: number;
  r: number;
  sp: number;
  fs: number;
  fsd: number;
  track: number;
  bw: number;
  /** Gölge alfası — YÜZDE olarak saklanır (7 = 0.07). */
  sha: number;
  dur: number;
};

export type PresetKey =
  | 'kurumsal-acik' | 'kurumsal-koyu' | 'zumrut' | 'bordo'
  | 'petrol' | 'antrasit' | 'kontrast' | 'ozel';

/*
  v3 GEÇİŞİ: Kurumsal çiftin knob'ları mobil tasarım şartnamesine çekildi —
  fs 15 (`body`), r 12 (`radius/md`), sha 10 (`elev/1` alfası), dur 240
  (sayfa geçişi). Gece gölgesi artık preset'ten değil temadan kapanıyor
  (`tokens.css` → `--sh-scale: 0`), sha bu yüzden iki modda da aynı.
*/
export const PRESETS: Record<Exclude<PresetKey, 'ozel'>, { ad: string } & ThemeKnobs> = {
  'kurumsal-acik': { ad: 'Kurumsal Gündüz', mod: 'acik', marka: 0, vurgu: 0, notr: 0, r: 12, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1, sha: 10, dur: 240, font: 0 },
  'kurumsal-koyu': { ad: 'Kurumsal Gece', mod: 'koyu', marka: 0, vurgu: 0, notr: 0, r: 12, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1, sha: 10, dur: 240, font: 0 },
  zumrut: { ad: 'Zümrüt Belediye', mod: 'acik', marka: 1, vurgu: 0, notr: 0, r: 15, sp: 4.5, fs: 14, fsd: 1.05, track: 0, bw: 1, sha: 6, dur: 240, font: 1 },
  bordo: { ad: 'Bordo Belediye', mod: 'acik', marka: 2, vurgu: 1, notr: 0, r: 5, sp: 4, fs: 14, fsd: 1, track: 0, bw: 1.5, sha: 5, dur: 180, font: 2 },
  // `notr: 0` — zemin listesi v3'te yeniden sıralandı (soğuk kâğıt fabrika
  // varsayılanı oldu); Petrol'ün istediği soğuk ton artık 0. sırada.
  petrol: { ad: 'Petrol Mavisi', mod: 'acik', marka: 3, vurgu: 2, notr: 0, r: 17, sp: 4.5, fs: 15, fsd: 1, track: 0, bw: 1, sha: 8, dur: 260, font: 1 },
  antrasit: { ad: 'Antrasit Gece', mod: 'koyu', marka: 5, vurgu: 5, notr: 1, r: 8, sp: 4, fs: 14, fsd: 1, track: 0, bw: 1, sha: 10, dur: 200, font: 1 },
  kontrast: { ad: 'Yüksek Kontrast', mod: 'acik', marka: 4, vurgu: 3, notr: 1, r: 3, sp: 4, fs: 15, fsd: 1.05, track: 0.005, bw: 2, sha: 0, dur: 120, font: 2 },
};

export const DEFAULT_KNOBS: ThemeKnobs = PRESETS['kurumsal-acik'];

/** Kaydırıcıların aralıkları — §3.1 tablosu. */
export const RANGES = {
  r: { min: 0, max: 20, adim: 1, birim: 'px' },
  sha: { min: 0, max: 55, adim: 1, birim: '' },
  sp: { min: 3.2, max: 5.2, adim: 0.1, birim: 'px' },
  fs: { min: 12, max: 17, adim: 0.5, birim: 'px' },
  fsd: { min: 0.9, max: 1.3, adim: 0.05, birim: '×' },
  track: { min: -0.02, max: 0.04, adim: 0.005, birim: 'em' },
  dur: { min: 0, max: 400, adim: 10, birim: 'ms' },
} as const;
