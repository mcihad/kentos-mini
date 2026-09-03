import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * ALT TABAKA GRAMERİ — kabuk TEK yerde kurulur.
 *
 * <p>
 * Mobilde alttan açılan her pencere parmakla aşağı kaydırılarak
 * kapanabilmeli. Bunu <c>OverlayShell</c> sağlıyor (mobil dalı
 * <c>vaul</c>). Ham Radix Dialog ile elle kurulan bir tabaka görünüş
 * olarak aynı çıkıyor ama <b>kaydırmaya yanıt vermiyor</b> — ve bu
 * SESSİZ: hata yok, uyarı yok, tabaka açılıyor, yalnızca kapanmıyor.
 * </p>
 *
 * <p>
 * Sahada beş tane vardı: yardım paneli, tema tasarımcısı, etkinlik not
 * düzenleyici, davete kişi ekleme ve katılımcı seçici. Yardım panelinin
 * tepesinde <b>tutamak bile çiziliyordu</b> — "beni aşağı çek" diyen,
 * çekilince hiçbir şey yapmayan bir şerit.
 * </p>
 *
 * Ölçüm (390px, gerçek dokunma olaylarıyla): düzeltmeden önce yardım ve
 * tema paneli kaydırma sonrası AÇIK kalıyordu, aynı jest vaul tabanlı
 * süzgeç tabakasını KAPATIYORDU — yani jest doğruydu, tabakalar sağırdı.
 */
describe('alt tabaka gramerı', () => {
  const kok = join(__dirname, '..', 'src');

  function tsxDosyalari(dizin: string): string[] {
    return readdirSync(dizin).flatMap((ad) => {
      const yol = join(dizin, ad);
      if (statSync(yol).isDirectory()) return tsxDosyalari(yol);
      return ad.endsWith('.tsx') ? [yol] : [];
    });
  }

  it('mobil alt tabaka OverlayShell dışında elle kurulmuyor', () => {
    const suclular = tsxDosyalari(kok)
      .filter((yol) => !yol.endsWith(join('components', 'OverlayShell.tsx')))
      .filter((yol) => {
        const kaynak = readFileSync(yol, 'utf8');
        if (!kaynak.includes('@radix-ui/react-dialog')) return false;

        /*
          Alt tabakanın imzası: ekranın DİBİNE yapışık (`bottom-0`,
          `inset-x-0`) ve yalnızca ÜST köşeleri yuvarlak (`rounded-t-`).
          Ortalanmış bir pencere bu ikisini birden taşımaz.
        */
        return /bottom-0/.test(kaynak)
          && /inset-x-0/.test(kaynak)
          && /rounded-t-/.test(kaynak)
          && /Dialog\.Content/.test(kaynak);
      })
      .map((yol) => yol.slice(kok.length + 1));

    expect(
      suclular,
      'Bu dosya(lar) mobil alt tabakayı elle kuruyor; kaydırarak kapatma '
      + 'çalışmaz. OverlayShell kullanın:\n  ' + suclular.join('\n  '),
    ).toEqual([]);
  });

  it('vaul yalnızca ortak kabukta kullanılıyor', () => {
    const vaulKullananlar = tsxDosyalari(kok)
      .filter((yol) => readFileSync(yol, 'utf8').includes("from 'vaul'"))
      .map((yol) => yol.slice(kok.length + 1));

    /*
      İki dosya: `OverlayShell` (form/detay tabakaları) ve `BottomSheet`
      (menü/liste tabakaları). Üçüncü bir kopya, sürükleme davranışının
      bir yerde düzeltilip diğerinde unutulması demek.
    */
    expect(vaulKullananlar.sort()).toEqual([
      join('components', 'OverlayShell.tsx'),
      join('shell', 'mobile', 'BottomSheet.tsx'),
    ]);
  });
});
