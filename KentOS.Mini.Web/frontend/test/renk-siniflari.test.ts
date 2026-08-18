import { readFileSync, readdirSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

/**
 * TANIMSIZ RENK SINIFI BEKÇİSİ.
 *
 * <p>
 * Tailwind v4'te renk yardımcıları <code>--color-*</code> değişkenlerinden
 * üretiliyor. Karşılığı olmayan bir ad (<code>bg-brand-ui</code>) <b>hata
 * vermez</b>: sınıf hiç üretilmez, öğe rengi olmadan çizilir. Yazı rengi
 * beyazsa sonuç beyaz üstüne beyazdır — görünmez, ama sessiz.
 * </p>
 *
 * <p>
 * <b>Bu test ölçülmüş bir arıza üzerine yazıldı.</b> Uygulamada
 * <code>--color-brand-ui</code> hiç tanımlı değildi; doğru ad
 * <code>brand</code>. Buna rağmen <code>bg-brand-ui</code> yirmi yerde
 * kullanılıyordu ve aralarında <b>iş takip modülünün BÜTÜN ilerleme çubuğu
 * dolguları</b> vardı: çubuğun yatağı çiziliyor, dolgusu görünmüyordu.
 * Kullanıcının "aşamaları tamamlasak bile progress ilerlemiyor" demesinin
 * en somut sebebi buydu — sayı doğru olsa bile çubuk boş görünüyordu.
 * Tarayıcıda ölçüldü: seçili satırın rozeti
 * <code>background-color: rgba(0,0,0,0)</code>, yazısı beyaz.
 * </p>
 */
describe('renk sınıfları', () => {
  // `import.meta.url` bir file: URL'i; `pathname` Windows'ta ve boşluklu
  // yollarda bozuk çıkıyor — dönüşüm `fileURLToPath` ile yapılır.
  const KOK = join(dirname(fileURLToPath(import.meta.url)), '..', 'src');

  /** `globals.css` içinde `--color-<ad>: …` olarak tanımlı her ad. */
  const tanimli = new Set(
    [...readFileSync(join(KOK, 'styles/globals.css'), 'utf8')
      .matchAll(/--color-([a-z0-9-]+)\s*:/g)].map((m) => m[1]),
  );

  /**
   * Tanımlı adların İLK parçası — "renk sözlüğünün kökleri".
   *
   * Bekçi bu kökler üzerinden çalışıyor: bir sınıfın ilk parçası sözlükte
   * varsa o sınıf RENK olmak istiyor demektir; tam adı sözlükte yoksa
   * yazılırken kayılmış bir addır. Kural böyle kurulunca `border-l-2`,
   * `text-sm`, `bg-cover` gibi renk OLMAYAN yardımcılar hiç incelenmiyor ve
   * bekçi gürültü üretmiyor.
   */
  const KOKLER = new Set([...tanimli].map((ad) => ad.split('-')[0]));

  /** Yön ekleri renk adının parçası değil: `border-l-brand` → `brand`. */
  const YON = /^(t|b|l|r|x|y|s|e)-/;

  /** Tailwind'in KENDİ paletinden gelen, `--color-*` gerektirmeyen adlar. */
  const YERLESIK = new Set([
    'white', 'black', 'transparent', 'current', 'inherit',
    'red', 'green', 'blue', 'amber', 'slate', 'gray', 'zinc', 'neutral', 'stone',
    'orange', 'yellow', 'lime', 'emerald', 'teal', 'cyan', 'sky', 'indigo',
    'violet', 'purple', 'fuchsia', 'pink', 'rose',
  ]);

  function kaynakDosyalari(dizin: string): string[] {
    return readdirSync(dizin).flatMap((ad) => {
      const yol = join(dizin, ad);
      if (statSync(yol).isDirectory()) return kaynakDosyalari(yol);
      return /\.(tsx|ts)$/.test(ad) ? [yol] : [];
    });
  }

  it('bg-/text-/border- sınıflarının hepsinin bir --color-* karşılığı var', () => {
    const bilinmeyenler: string[] = [];

    for (const dosya of kaynakDosyalari(KOK)) {
      const metin = readFileSync(dosya, 'utf8');

      /*
        Yalnızca DÜZ sınıf adları taranıyor. Rastgele değer sözdizimi
        (`bg-(--nav-bg)`, `text-(--st-ok)`) değişkeni doğrudan veriyor ve
        `--color-*` gerektirmiyor; opaklık eki (`/60`) ve `hover:` gibi
        değiştiriciler ayrıştırmanın dışında.
      */
      for (const [, ad] of metin.matchAll(
        /(?:^|[\s'"`:])(?:bg|text|border)-([a-z][a-z0-9]*(?:-[a-z0-9]+)*)(?=[\s'"`/]|$)/gm,
      )) {
        const kok = ad.replace(YON, '');

        if (tanimli.has(kok) || YERLESIK.has(kok.split('-')[0])) continue;

        // İlk parçası renk sözlüğünde OLMAYAN ad, renk yardımcısı değil
        // (`text-sm`, `border-2`, `bg-cover`). İncelenmez.
        if (!KOKLER.has(kok.split('-')[0])) continue;

        bilinmeyenler.push(`${dosya.slice(KOK.length + 1)} → ${ad}`);
      }
    }

    expect(
      [...new Set(bilinmeyenler)],
      'Bu sınıflar hiçbir --color-* değişkenine karşılık gelmiyor; '
        + 'Tailwind onları ÜRETMEZ ve öğe renksiz çizilir (hata vermeden).',
    ).toEqual([]);
  });
});
