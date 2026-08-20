import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * BİRİM SEÇİMİ HER EKRANDA AYNI GÖRÜNÜR.
 *
 * <p>
 * İki ayrı tutarsızlık vardı ve ikisi de "düzensiz duruyor" diye
 * bildirildi:
 * </p>
 *
 * <ol>
 *   <li><b>Ölçü.</b> Etkin birim seçici kendi <code>&lt;select&gt;</code>'ini
 *       kurup girdi sınıflarını kopyalıyordu ve hep <code>h-ctrl</code>
 *       (40px) idi; yanındaki arama alanı ve düğmeler mobilde
 *       <code>h-field</code> (50px). Araç çubuğu her ekranda hizasızdı.</li>
 *   <li><b>Etiket.</b> Birim bir ekranda "Ad — Yetkili", ötekinde düz "Ad"
 *       yazıyordu. Kurumda altı ayrı "Başkan Yardımcısı" birimi var;
 *       yetkilisiz listede hangisinin seçildiği anlaşılmıyor.</li>
 * </ol>
 */
describe('birim seçimi', () => {
  const kok = join(__dirname, '..', 'src');

  function tsxDosyalari(dizin: string): string[] {
    return readdirSync(dizin).flatMap((ad) => {
      const yol = join(dizin, ad);
      if (statSync(yol).isDirectory()) return tsxDosyalari(yol);
      return ad.endsWith('.tsx') ? [yol] : [];
    });
  }

  it('etkin birim seçici ortak Secim bileşenini kullanır', () => {
    const kaynak = readFileSync(join(kok, 'components', 'UnitScopePicker.tsx'), 'utf8');

    // Kendi <select>'ini kurmuyor: ölçü/köşe/odak tek yerden gelmeli.
    expect(kaynak).toContain("from './Field'");
    expect(kaynak).toMatch(/<Secim\b/);
    expect(kaynak).not.toMatch(/<select\b/);
  });

  it('birim listeleri yetkilisiyle yazılır', () => {
    const suclular = tsxDosyalari(kok)
      .filter((yol) => {
        const kaynak = readFileSync(yol, 'utf8');
        if (!/birimler\.liste\.map/.test(kaynak)) return false;

        /*
          `birimler.liste.map((x) => <option>{x.ad}</option>)` kalıbı: birim
          listesini düz adla çiziyor. `unitLabel()` "Ad — Yetkili" veriyor
          ve kural her yerde geçerli (bkz. frontend/CLAUDE.md).
        */
        return /birimler\.liste\.map\([\s\S]{0,220}?\{\s*\w+\.ad\s*\}/.test(kaynak);
      })
      .map((yol) => yol.slice(kok.length + 1));

    expect(
      suclular,
      'Birim listesi yetkilisiz çiziliyor; unitLabel() kullanın:\n  ' + suclular.join('\n  '),
    ).toEqual([]);
  });
});
