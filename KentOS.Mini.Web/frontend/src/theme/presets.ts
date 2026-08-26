/**
 * HAZIR TEMALAR — hepsi tasarım sisteminin KENDİ merdivenlerinde.
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

  RAFİNE TURU: geçiş o gün YALNIZCA kurumsal çifte uygulanmıştı; kalan beş
  preset eski değerlerinde kaldı ve üç ayrı yerden sistemin dışına düşüyordu.

  1. YARIM PİKSEL KÖŞELER. Yarıçap merdiveni `--r`den TÜRETİLİYOR
     (`--r-sm: r × 0.667`, `--r-lg: r × 1.333`). Taban 15/17/5/8/3 iken
     merdiven kesirli çıkıyordu — ölçüldü: Bordo ve Petrol'de 6 basamağın
     4'ü, Antrasit'te 3'ü. Yarım piksellik köşe düşük yoğunluklu ekranda
     bulanık çiziliyor. Yeni tabanlar 6/12/18: üçü de merdivenin tamamını
     tam piksel veriyor.

  2. "BOŞLUK 4'ÜN KATIDIR" (şartname §4) — Zümrüt ve Petrol `sp: 4.5`
     kullanıyordu, yani HER boşluk adımı kesirli: 4.5/9/13.5/18/22.5.
     İkisi de 4'e alındı.

  3. YAZI TABANI 14'TE KALMIŞTI. v3'te 14 → 15 taşıması ölçülerek yapıldı
     ("kullanıcı 'fontlar minnacık' dediğinde şartname değil ALGI haklıydı")
     ama Zümrüt, Bordo ve Antrasit 14'te bırakılmıştı: kurumsal temadan
     birine geçmek yazıyı bir kademe küçültüyordu. Üçü de 15'e çekildi.
     Yüksek Kontrast 16'ya çıktı — erişilebilirlik temasının yazısı
     varsayılandan küçük olamaz.

  4. GEÇİŞ SÜRELERİ 40'IN KATI. 180/200/240/260/120 karışıktı; şimdi
     120/160/200/240/280 — aynı ritmin adımları. Süre presetin karakterinin
     parçası: Yüksek Kontrast en kısa (hareket dikkat dağıtmasın), Petrol en
     uzun (yumuşak ve geniş).

  Presetin İŞİ değişmedi: Bordo hâlâ en sert köşeli ve en kalın kenarlıklı,
  Zümrüt/Petrol hâlâ en yumuşak, Yüksek Kontrast hâlâ gölgesiz. Değişen,
  bunu sistemin dışına çıkmadan yapmaları.
*/
export const PRESETS: Record<Exclude<PresetKey, 'ozel'>, { ad: string } & ThemeKnobs> = {
  'kurumsal-acik': { ad: 'Kurumsal Gündüz', mod: 'acik', marka: 0, vurgu: 0, notr: 0, r: 12, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1, sha: 10, dur: 240, font: 0 },
  'kurumsal-koyu': { ad: 'Kurumsal Gece', mod: 'koyu', marka: 0, vurgu: 0, notr: 0, r: 12, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1, sha: 10, dur: 240, font: 0 },

  // YUMUŞAK: geniş yarıçap, hafif gölge. `r: 18` merdiveni 9/12/18/24/30/36
  // veriyor — hepsi tam piksel.
  zumrut: { ad: 'Zümrüt Belediye', mod: 'acik', marka: 1, vurgu: 0, notr: 0, r: 18, sp: 4, fs: 15, fsd: 1.05, track: 0, bw: 1, sha: 8, dur: 240, font: 1 },

  // KLASİK: köşe sertleşir, kenarlık kalınlaşır, geçiş kısalır. `r: 6` →
  // 3/4/6/8/10/12.
  bordo: { ad: 'Bordo Belediye', mod: 'acik', marka: 2, vurgu: 1, notr: 2, r: 6, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1.5, sha: 6, dur: 160, font: 2 },

  // `notr: 0` — zemin listesi v3'te yeniden sıralandı (soğuk kâğıt fabrika
  // varsayılanı oldu); Petrol'ün istediği soğuk ton artık 0. sırada.
  petrol: { ad: 'Petrol Mavisi', mod: 'acik', marka: 3, vurgu: 2, notr: 0, r: 18, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1, sha: 10, dur: 280, font: 1 },

  antrasit: { ad: 'Antrasit Gece', mod: 'koyu', marka: 5, vurgu: 5, notr: 1, r: 12, sp: 4, fs: 15, fsd: 1, track: 0, bw: 1, sha: 10, dur: 200, font: 1 },

  // ERİŞİLEBİLİRLİK: gölge tamamen kapalı, ayrım kenarlıkla. Köşe neredeyse
  // sert ama `r: 6` merdiveni tam piksel; `r: 3` yarım piksel üretiyordu ve
  // yarım piksellik köşe düşük yoğunluklu ekranda bulanık çiziliyor.
  kontrast: { ad: 'Yüksek Kontrast', mod: 'acik', marka: 4, vurgu: 3, notr: 1, r: 6, sp: 4, fs: 16, fsd: 1.05, track: 0.005, bw: 2, sha: 0, dur: 120, font: 2 },
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
