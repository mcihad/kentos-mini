import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

/**
 * TOKEN BÜTÜNLÜĞÜ.
 *
 * Bu dosyanın varlık sebebi: **CSS tanımsız bir değişkende sessizdir.**
 * `background-color: var(--yok)` bir hata vermez, konsola bir şey yazmaz;
 * yalnızca hiçbir şey boyamaz. Sonuç, "bozuk" değil "eksik" görünen bir
 * arayüz oluyor ve nereye bakacağını bilmiyorsun.
 *
 * Sahada dört tane çıktı, dördü de aylardır oradaydı:
 *
 *  1. `--color-perde: var(--perde)` eşlemesi vardı, `--perde` YOKTU →
 *     uygulamadaki **her** diyalog ve alt tabakanın perdesi tamamen
 *     saydamdı; arkadaki ekran tam parlaklıkta duruyor, katmanlar "üstte"
 *     okunmuyordu.
 *  2. `@theme` içinde `--ease-spring: var(--ease-spring)` — kendine referans.
 *     CSS bunu "hesaplanmış değerde geçersiz" sayıp `tokenlar.css`'teki
 *     gerçek eğriyi siliyordu; her basma yayı varsayılan yumuşatmaya
 *     düşüyordu.
 *  3. `ring-(--brand-2)` / `bg-(--brand-2)` — `-(--x)` sözdizimi HAM
 *     değişkeni okur, Tailwind rengi `--color-brand-2`'yi değil. `--brand-2`
 *     hiç tanımlı olmadığı için üç odak halkası ve okunmamış bildirim
 *     noktası görünmüyordu.
 *  4. `EGrafik` paletinin ilk rengi `token('--brand-2', '#1E5FBF')` ile
 *     sabit yedeğe düşüyor, grafikler temayla dönmüyordu.
 *
 * Testler kaynağı okur; tarayıcı gerektirmezler.
 */

const kok = process.cwd();
/** Yorumlar ayıklanır: bu dosyaların açıklamaları token adlarından geçiyor ve
 *  tarayıcı onları gerçek tanım/kullanım sanıyordu. */
const yorumsuz = (m: string) => m.replace(/\/\*[\s\S]*?\*\//g, '');
const globals = yorumsuz(readFileSync(join(kok, 'src', 'styles', 'globals.css'), 'utf8'));
const tokenlar = yorumsuz(readFileSync(join(kok, 'src', 'styles', 'tokens.css'), 'utf8'));
const cssler = globals + '\n' + tokenlar;

/**
 * Bizim tanımlamadığımız, ÇALIŞMA ANINDA enjekte edilen değişkenler.
 * Radix konumlandırırken tetikleyicinin ölçüsünü böyle geçiriyor; Tailwind de
 * kendi iç durumunu `--tw-*` ile taşıyor. Bunları aramak yanlış alarm olur.
 */
const DISARIDAN = /^--(radix|tw)-/;

/** Bir özel özellik bu dosyalarda GERÇEKTEN tanımlanmış mı? */
function tanimliMi(ad: string): boolean {
  // Tanım = satırın başında `--ad:` — `var(--ad)` okuması değil.
  return new RegExp(`(^|[;{\\s])${ad.replace(/-/g, '\\-')}\\s*:`, 'm').test(cssler);
}

describe('token bütünlüğü', () => {
  it('@theme eşlemelerinin kaynağı tanımlı', () => {
    const blok = /@theme\s+inline\s*\{([\s\S]*?)\n\}/.exec(globals);
    expect(blok, '@theme inline bloğu bulunamadı').not.toBeNull();

    const eslemeler = [...blok![1].matchAll(/(--[\w-]+)\s*:\s*var\((--[\w-]+)\)/g)];
    expect(eslemeler.length, 'hiç eşleme okunamadı — regex bozulmuş olabilir').toBeGreaterThan(50);

    const eksik = eslemeler
      .filter(([, , kaynak]) => !tanimliMi(kaynak))
      .map(([, hedef, kaynak]) => `${hedef} → var(${kaynak})`);

    expect(eksik, `Kaynağı tanımsız eşleme:\n  ${eksik.join('\n  ')}`).toEqual([]);
  });

  it('@theme eşlemesi kendine referans vermiyor', () => {
    const blok = /@theme\s+inline\s*\{([\s\S]*?)\n\}/.exec(globals)![1];
    const kendine = [...blok.matchAll(/(--[\w-]+)\s*:\s*var\((--[\w-]+)\)/g)]
      .filter(([, hedef, kaynak]) => hedef === kaynak)
      .map(([, hedef]) => hedef);

    expect(
      kendine,
      `Kendine referans veren token (CSS bunu geçersiz sayar ve gerçek tanımı SİLER):\n  ${kendine.join('\n  ')}`,
    ).toEqual([]);
  });

  it('bileşenlerden okunan her CSS değişkeni tanımlı', () => {
    // Tailwind'in `bg-(--x)` / `ring-(--x)` biçimi HAM değişkeni okur;
    // `--color-x` eşlemesinin varlığı bunu kurtarmaz.
    const kaynaklar = import.meta.glob('../src/**/*.{ts,tsx}', {
      eager: true,
      query: '?raw',
      import: 'default',
    }) as Record<string, string>;

    const eksik: string[] = [];
    for (const [yol, metin] of Object.entries(kaynaklar)) {
      // İki biçim birden: Tailwind'in `bg-(--x)` sözdizimi VE satır içi
      // stildeki `var(--x)`. İkincisi eklenmeseydi `--gold-strong`ın ana
      // sayfadaki üçüncü çağrı yeri gözden kaçıyordu.
      const adlar = [
        ...[...metin.matchAll(/[\w-]+-\((--[\w-]+)\)/g)].map((m) => m[1]),
        ...[...metin.matchAll(/var\((--[\w-]+)[,)]/g)].map((m) => m[1]),
      ];
      for (const ad of adlar) {
        if (DISARIDAN.test(ad)) continue;
        if (!tanimliMi(ad)) eksik.push(`${yol.replace('../', '')} → var(${ad})`);
      }
    }

    expect(eksik, `Tanımsız ham değişken okunuyor:\n  ${[...new Set(eksik)].join('\n  ')}`).toEqual([]);
  });

  it('perde token"u iki modda da tanımlı', () => {
    // Perdesiz bir katman "üstte" okunmuyor; bu ikisi eksikse tabakalar
    // sessizce düz zemine oturuyor.
    for (const ad of ['--perde', '--perde-hafif', '--perde-rgb']) {
      expect(tanimliMi(ad), `${ad} tanımlı değil`).toBe(true);
    }
    // Gece bloğunda da kendi değeri olmalı — yoksa koyu temada gündüz
    // yoğunluğu kullanılır ve koyu zeminde hiçbir şey karartmaz.
    const gece = /:root\[data-tema='koyu'\]\s*\{([\s\S]*?)\n\}/.exec(tokenlar);
    expect(gece, 'gece bloğu bulunamadı').not.toBeNull();
    expect(gece![1]).toMatch(/--perde\s*:/);
  });
});
