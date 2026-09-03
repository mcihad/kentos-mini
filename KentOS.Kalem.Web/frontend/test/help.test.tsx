import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Markdown } from '../src/help/Markdown';
import { findHelp, helpTopics } from '../src/help/catalog';
import { NAVIGATION } from '../src/shell/navigation';

/**
 * YARDIM SİSTEMİ.
 *
 * İki şeyi kilitler:
 * 1. Menüdeki HER ekranın yardımı var. Yeni bir ekran eklenip yardımı
 *    unutulursa burası kırmızıya döner — kullanıcının "yardım" düğmesini
 *    aradığı ama bulamadığı sayfa kalmasın.
 * 2. Metinler gerçekten çiziliyor: başlık, adım listesi, tablo ve uyarı
 *    kutusu.
 */
describe('yardım kataloğu', () => {
  const menuYollari = NAVIGATION.flatMap((g) => g.ogeler).map((o) => o.yol);

  it.each(menuYollari)('%s ekranının yardımı var', (yol) => {
    const kayit = findHelp(yol);
    expect(kayit, `${yol} için yardım metni yok`).not.toBeNull();
    expect(kayit!.metin.length).toBeGreaterThan(200);
    expect(kayit!.baslik).not.toBe('');
    expect(kayit!.ozet).not.toBe('');
  });

  it('detay ekranlarının kendi yardımı var', () => {
    expect(findHelp('/ajanda/42')?.baslik).toBe('Etkinlik Detayı');
    expect(findHelp('/talepler/7')?.baslik).toBe('Talep Detayı');
    expect(findHelp('/halk-gunu/3')?.baslik).toBe('Halk Günü Ayrıntısı');
    expect(findHelp('/halk-gunu/3/salon')?.baslik).toBe('Salon Modu');
  });

  it('ÖZEL yol, kimlik kalıbından önce gelir', () => {
    // `/halk-gunu/basvurular` yolu `/halk-gunu/:id` kalıbına da uyuyor;
    // sıralama bozulursa vatandaş havuzunda gün ayrıntısının yardımı açılır.
    expect(findHelp('/halk-gunu/basvurular')?.baslik).toBe('Vatandaş Havuzu');
  });

  it('tanımsız yolda yardım YOKTUR (düğme çizilmez)', () => {
    expect(findHelp('/bilinmeyen-ekran')).toBeNull();
  });

  /**
   * Grup adları MENÜDEN türetiliyor, elle yazılmıyor.
   *
   * Önce sabit bir diziydi ve menüye yeni bir grup eklendiğinde (İş Takip)
   * bu test, yardım metinleri doğru yazılmış olmasına rağmen kırmızıya döndü:
   * gerçek bir hatayı değil, kendi kopyasının bayatladığını bildiriyordu.
   * Menüden okununca kopya kalmıyor — yardım merkezi konuları zaten menü
   * gruplarına göre öbekliyor.
   */
  it('her konunun bir grubu var (yardım merkezi onları gruplar)', () => {
    const gecerli = NAVIGATION.map((g) => g.baslik);
    for (const { kalip, kayit } of helpTopics()) {
      expect(gecerli, `${kalip} tanımsız grupta`).toContain(kayit.grup);
    }
  });
});

/**
 * GEZİLEN HER YOLUN BİR ROTASI OLMALI.
 *
 * <p>
 * Menüdeki "Yardım" satırı <c>/yardim</c>'e gidiyordu ama o rota hiç
 * tanımlanmamıştı: düğmeye basan kullanıcı "Sayfa bulunamadı" görüyordu.
 * Hata derlemede de testte de görünmüyor — çünkü ölü bir bağlantı, geçerli
 * bir dizedir.
 * </p>
 *
 * <p>
 * Bu yüzden kaynak taranıyor: kabuktaki <c>git('/...')</c> çağrıları ve
 * menüdeki <c>yol</c> alanları, <c>App.tsx</c>'teki rota tablosuyla
 * karşılaştırılıyor.
 * </p>
 */
describe('gezinme hedefleri', () => {
  const oku = (yol: string) => {
    const fs = require('node:fs') as typeof import('node:fs');
    const path = require('node:path') as typeof import('node:path');
    return fs.readFileSync(path.join(process.cwd(), 'src', yol), 'utf8');
  };

  const rotalar = [...oku('App.tsx').matchAll(/path="([^"]+)"/g)]
    .map((m) => m[1].replace(/^\//, ''))
    .filter((r) => r !== '*');

  /** `/talepler/:id` gibi kalıpları da kapsayacak biçimde eşleştirir. */
  const rotaVar = (hedef: string) => {
    const parcalar = hedef.replace(/^\//, '').split('/');
    return rotalar.some((r) => {
      const k = r.split('/');
      if (k.length !== parcalar.length) return false;
      return k.every((p, i) => p.startsWith(':') || p === parcalar[i]);
    });
  };

  const kabukHedefleri = [...oku('shell/MobileMenu.tsx').matchAll(/git\('(\/[^']*)'\)/g)]
    .map((m) => m[1])
    .filter((y) => y !== '/');

  it.each(kabukHedefleri)('menüdeki %s hedefinin rotası var', (hedef) => {
    expect(rotaVar(hedef), `${hedef} için App.tsx'te rota yok`).toBe(true);
  });

  it.each(NAVIGATION.flatMap((g) => g.ogeler).map((o) => o.yol))(
    'menü öğesi %s için rota var',
    (yol) => {
      if (yol === '/') return;
      expect(rotaVar(yol), `${yol} için App.tsx'te rota yok`).toBe(true);
    },
  );
});

describe('markdown çizici', () => {
  it('başlık, adım, madde, tablo ve uyarıyı çizer', () => {
    render(
      <Markdown
        metin={[
          '# Başlık',
          '',
          'Bir paragraf **kalın** ile.',
          '',
          '## Bölüm',
          '',
          '1. Birinci adım',
          '2. İkinci adım',
          '',
          '- Madde bir',
          '- Madde iki',
          '',
          '| Sütun | Değer |',
          '| --- | --- |',
          '| Ali | 42 |',
          '',
          '> Dikkat edilecek şey.',
        ].join('\n')}
      />,
    );

    expect(screen.getByText('Başlık')).toBeInTheDocument();
    expect(screen.getByText('Bölüm')).toBeInTheDocument();
    expect(screen.getByText('kalın')).toBeInTheDocument();
    expect(screen.getByText('Birinci adım')).toBeInTheDocument();
    expect(screen.getByText('Madde iki')).toBeInTheDocument();
    expect(screen.getByText('Sütun')).toBeInTheDocument();
    expect(screen.getByText('Ali')).toBeInTheDocument();
    expect(screen.getByText('Dikkat edilecek şey.')).toBeInTheDocument();
  });

  it('ikinci satıra taşan madde BÖLÜNMEZ', () => {
    // Metin dosyalarında satırlar sarılıyor; devam satırı ayrı paragraf
    // olarak çizilince cümle ortadan ikiye bölünüyordu.
    render(<Markdown metin={'- Uzun bir madde satırı\n  devam ediyor.'} />);

    expect(screen.getByText('Uzun bir madde satırı devam ediyor.')).toBeInTheDocument();
  });
});
