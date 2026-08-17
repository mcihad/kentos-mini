import { screen } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { PERMISSION } from '../src/components/permissions';
import { NAVIGATION, type NavigationItem } from '../src/shell/navigation';
import type { Me } from '../src/auth/SessionProvider';

/**
 * İZİN SİSTEMİ — arayüz tarafı.
 *
 * Yetki dağılımı artık veritabanında ve yönetim ekranından değiştirilebiliyor.
 * Arayüzün buna uymamasının iki ayrı bedeli var:
 *   • izni olmayana menü göstermek → kullanıcı 403 duvarına çarpar
 *   • izni OLANA menü göstermemek → yetki verildiği hâlde çalışmaz
 * İkisi de sessiz; bu yüzden burada tek tek kilitleniyor.
 */

// ═══════════════════════════════════════════════════ menü süzgeci

/**
 * `AppShell` içindeki süzgecin birebir kopyası.
 *
 * Bileşeni kurmak oturum, sorgu istemcisi ve yönlendirici ister; burada
 * denenen KURAL, çizim değil. Kural değişirse iki yerde değişmeli — o yüzden
 * aşağıda ayrıca `AppShell` ile aynı davrandığını doğrulayan bir test var.
 */
function gorunurMu(oge: NavigationItem, me: Me): boolean {
  const izinListesiVar = (me.izinler?.length ?? 0) > 0;
  // Çoklu izin VEYA ile — sunucudaki `[Izin(...)]` süzgeciyle aynı.
  const hasPermission = (i: string | string[]) =>
    Array.isArray(i)
      ? i.some((x) => me.izinler?.includes(x) ?? false)
      : (me.izinler?.includes(i) ?? false);
  const hasPolicy = (p: string) => me.yetkiler.includes(p);
  const rolVar = (r?: string) => !r || me.roller.some((x) => x === r || x === 'Sistem');

  if (oge.izin && izinListesiVar) return hasPermission(oge.izin);
  return (!oge.politika || hasPolicy(oge.politika)) && rolVar(oge.rol);
}

const TUM_OGELER = NAVIGATION.flatMap((g) => g.ogeler);

function oge(yol: string): NavigationItem {
  const o = TUM_OGELER.find((x) => x.yol === yol);
  if (!o) throw new Error(`Menü öğesi yok: ${yol}`);
  return o;
}

function kullanici(ekle: Partial<Me> = {}): Me {
  return {
    id: 1,
    kullaniciAdi: 'test',
    tamAd: 'Test Kullanıcı',
    roller: [],
    gizliEtkinlikEkleyebilir: false,
    dosyaGonderebilir: false,
    yetkiler: [],
    izinler: [],
    ...ekle,
  };
}

describe('Menü süzgeci — izin', () => {
  it('İZİN varsa öğe görünür', () => {
    const me = kullanici({ izinler: [PERMISSION.talepGoruntule] });
    expect(gorunurMu(oge('/talepler'), me)).toBe(true);
  });

  it('İZİN yoksa öğe GİZLENİR — rolü ne olursa olsun', () => {
    // Kullanıcı Admin ama `talep.goruntule` izni kısılmış. Rol adına bakmak,
    // yönetim ekranından yapılan kısıtlamayı görmezden gelmek olurdu.
    const me = kullanici({
      roller: ['Admin'],
      yetkiler: ['Ajanda'],
      izinler: [PERMISSION.ajandaGoruntule],
    });
    expect(gorunurMu(oge('/talepler'), me)).toBe(false);
  });

  it('İZİN varsa politika OLMASA da görünür', () => {
    // Yönetim ekranından oluşturulan yeni bir rol hiçbir politikada yok.
    // Politikaya bakılsaydı yeni roller hiçbir menüyü göremezdi.
    const me = kullanici({
      roller: ['TalepPersoneli'],
      yetkiler: [],
      izinler: [PERMISSION.talepGoruntule],
    });
    expect(gorunurMu(oge('/talepler'), me)).toBe(true);
  });

  it('izin listesi HİÇ gelmezse eski politikaya düşer', () => {
    // Eski sunucuya bağlanma yolu: liste boşsa izne göre süzmek bütün menüyü
    // kapatırdı.
    const me = kullanici({ roller: ['Sekreter'], yetkiler: ['Ajanda'], izinler: [] });
    expect(gorunurMu(oge('/talepler'), me)).toBe(true);
    expect(gorunurMu(oge('/ajanda'), me)).toBe(true);
  });

  it('izin listesi boşken rol tabanlı öğeler de eski kurala uyar', () => {
    const admin = kullanici({ roller: ['Admin'], yetkiler: [], izinler: [] });
    const duz = kullanici({ roller: ['Kullanici'], yetkiler: [], izinler: [] });

    expect(gorunurMu(oge('/yonetim'), admin)).toBe(true);
    expect(gorunurMu(oge('/yonetim'), duz)).toBe(false);
  });

  it('Sistem hataları YALNIZCA o iznin sahibine görünür', () => {
    // Kayıtlarda istek gövdeleri, IP adresleri ve yığın izleri var.
    const admin = kullanici({
      roller: ['Admin'],
      izinler: [PERMISSION.yonetimKullanici, PERMISSION.ajandaGoruntule],
    });
    const sistem = kullanici({ roller: ['Sistem'], izinler: [PERMISSION.sistemHata] });

    expect(gorunurMu(oge('/hatalar'), admin)).toBe(false);
    expect(gorunurMu(oge('/hatalar'), sistem)).toBe(true);
  });

  it('hiç izni olmayan kullanıcı yalnızca izinsiz öğeleri görür', () => {
    const me = kullanici({ izinler: ['zararsiz.izin'] });
    const gorunen = TUM_OGELER.filter((o) => gorunurMu(o, me)).map((o) => o.yol);

    // Ana sayfa, bildirimler ve ayarlar izin istemiyor — yetkisiz bir
    // kullanıcı da uygulamaya girip parolasını değiştirebilmeli.
    expect(gorunen).toEqual(['/', '/bildirimler', '/ayarlar']);
  });

  it('her izin ilan eden öğenin izni KATALOGDA var', () => {
    // Yazım hatası sessizce `false` döner ve menü hiç görünmez.
    const katalog = new Set<string>(Object.values(PERMISSION));
    for (const o of TUM_OGELER) {
      for (const i of [o.izin ?? []].flat()) {
        expect(katalog.has(i), `${o.yol} → ${i}`).toBe(true);
      }
    }
  });

  it('gizlenebilir her öğe İZİN de ilan eder', () => {
    // Yalnızca rol/politika ile süzülen bir öğe kalırsa, yönetim ekranından
    // verilen izin o menüyü açamaz — yetki verildiği hâlde çalışmaz.
    for (const o of TUM_OGELER) {
      if (o.politika || o.rol) {
        expect(o.izin, `${o.yol} izin ilan etmiyor`).toBeTruthy();
      }
    }
  });
});

// ═════════════════════════════════════════ oturum sağlayıcı: hasPermission

describe('hasPermission', () => {
  it('izin listesindeki değer için true, olmayan için false', () => {
    const me = kullanici({ izinler: [PERMISSION.ajandaSil] });
    // Çoklu izin VEYA ile — sunucudaki `[Izin(...)]` süzgeciyle aynı.
  const hasPermission = (i: string | string[]) =>
    Array.isArray(i)
      ? i.some((x) => me.izinler?.includes(x) ?? false)
      : (me.izinler?.includes(i) ?? false);

    expect(hasPermission(PERMISSION.ajandaSil)).toBe(true);
    expect(hasPermission(PERMISSION.ajandaEkle)).toBe(false);
  });

  it('liste HİÇ yoksa false döner — iyimser davranmaz', () => {
    // `yetkisiVar` boş listede `true` dönüyor ("sunucu söylemedi"), izinde
    // ise tersi doğru: izni olmayana çalışmayan düğme göstermektense
    // göstermemek.
    const me = kullanici({ izinler: undefined });
    // Çoklu izin VEYA ile — sunucudaki `[Izin(...)]` süzgeciyle aynı.
  const hasPermission = (i: string | string[]) =>
    Array.isArray(i)
      ? i.some((x) => me.izinler?.includes(x) ?? false)
      : (me.izinler?.includes(i) ?? false);

    expect(hasPermission(PERMISSION.ajandaSil)).toBe(false);
  });
});

// ═══════════════════════════════════════════════ katalog bütünlüğü

describe('İzin kataloğu', () => {
  it('mükerrer değer yok', () => {
    const degerler = Object.values(PERMISSION);
    expect(new Set(degerler).size).toBe(degerler.length);
  });

  it('hepsi `alan.eylem` biçiminde', () => {
    for (const d of Object.values(PERMISSION)) {
      expect(d, d).toMatch(/^[a-z]+\.[a-zA-Z]+$/);
    }
  });
});

// ══════════════════════════════════════════ AppShell gerçekten süzüyor mu

describe('AppShell menüyü izne göre çizer', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({}),
      text: async () => '{}',
    }));

    // Kabuk açılışta bildirim izni kartını çiziyor ve o da tarayıcı bildirim
    // API'sini yokluyor; jsdom'da yok. Burada denenen menü süzgeci, kartın
    // fırlattığı hata testi kirletiyordu.
    vi.doMock('../src/notifications/PermissionCard', () => ({
      NotificationPermissionCard: () => null,
    }));
    vi.doMock('../src/notifications/NotificationBridge', () => ({
      NotificationBridge: () => null,
    }));
    vi.doMock('../src/notifications/NotificationCenter', () => ({
      NotificationCenter: () => null,
      resolveNotificationPath: () => null,
      useUnreadCount: () => ({ data: 0 }),
    }));
  });

  /**
   * Kabuğu gerçek `OturumSaglayici` ile kurmak jeton ve ağ ister; onun yerine
   * bağlamı taklit ediyoruz. Denenen şey kabuğun ÇİZİMİ: doğru kural
   * uygulanınca ekranda ne var, ne yok.
   */
  async function kabukKur(me: Me) {
    vi.doMock('../src/auth/SessionProvider', async () => {
      const gercek = await vi.importActual<
        typeof import('../src/auth/SessionProvider')
      >('../src/auth/SessionProvider');
      return {
        ...gercek,
        useSession: () => ({
          me,
          ready: true,
          signIn: async () => {},
          signOut: async () => {},
          hasPolicy: (p: string) => me.yetkiler.includes(p),
          hasPermission: (i: string | string[]) =>
            [i].flat().some((x) => me.izinler?.includes(x) ?? false),
        }),
      };
    });

    vi.doMock('../src/data/hooks', async () => {
      const gercek = await vi.importActual<typeof import('../src/data/hooks')>(
        '../src/data/hooks',
      );
      return { ...gercek, useHasIncomingFile: () => false };
    });

    // Sağlayıcı yığını da TAZE grafikten alınmalı: `vi.resetModules()` sonrası
    // eskiden içe aktarılmış `TemaSaglayici` başka bir React bağlamı örneği
    // oluyor ve kabuk "TemaSaglayici içinde kullanılmalı" diye düşüyor.
    const { AppShell } = await import('../src/shell/AppShell');
    const { kur: ciz } = await import('./helpers');
    ciz(<AppShell />);
  }

  it('izni olmayan kullanıcı Talepler menüsünü GÖRMEZ', async () => {
    vi.resetModules();
    await kabukKur(
      kullanici({ roller: ['Admin'], izinler: [PERMISSION.ajandaGoruntule] }),
    );

    expect(screen.queryByRole('link', { name: /Talepler/ })).not.toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /Ajanda/ }).length).toBeGreaterThan(0);
  });

  it('izni olan kullanıcı Talepler menüsünü görür', async () => {
    vi.resetModules();
    await kabukKur(kullanici({ izinler: [PERMISSION.talepGoruntule] }));

    expect(screen.getAllByRole('link', { name: /Talepler/ }).length).toBeGreaterThan(0);
  });
});

// ═══════════════════════════════════ rota koruması ve ekran içi düğmeler

describe('ProtectedRoute', () => {
  /** `KorumaliRota` içindeki kuralın birebir kopyası. */
  function engelli(
    me: Me,
    { izin, politika, rol }:
      { izin?: string | string[]; politika?: string; rol?: string },
  ): boolean {
    const izinListesiVar = (me.izinler?.length ?? 0) > 0;
    if (izin && izinListesiVar) {
      return ![izin].flat().some((i) => me.izinler?.includes(i) ?? false);
    }
    const politikaEksik = politika && !me.yetkiler.includes(politika);
    const rolEksik = rol && !me.roller.some((r) => r === rol || r === 'Sistem');
    return Boolean(politikaEksik || rolEksik);
  }

  /**
   * Bulunan gerçek hata: yönetim ekranından oluşturulan rol hiçbir politikada
   * olmadığı için, İZNİ VERİLMİŞ bir ekran bile "erişim yetkiniz yok"
   * diyordu. Menü öğeyi gösteriyor, rota kapıdan çeviriyordu.
   */
  it('İZNİ olan kullanıcı politikası olmasa da GİREBİLİR', () => {
    const me = kullanici({
      roller: ['TalepPersoneli'],
      yetkiler: [],
      izinler: [PERMISSION.talepGoruntule],
    });

    expect(engelli(me, { izin: PERMISSION.talepGoruntule, politika: 'Ajanda' })).toBe(false);
  });

  it('İZNİ olmayan giremez — politikası olsa bile', () => {
    const me = kullanici({
      roller: ['Sekreter'],
      yetkiler: ['Ajanda'],
      izinler: [PERMISSION.ajandaGoruntule],
    });

    expect(engelli(me, { izin: PERMISSION.talepGoruntule, politika: 'Ajanda' })).toBe(true);
  });

  it('izin listesi gelmezse ESKİ politika kuralı geçerli', () => {
    const me = kullanici({ roller: ['Sekreter'], yetkiler: ['Ajanda'], izinler: [] });

    expect(engelli(me, { izin: PERMISSION.talepGoruntule, politika: 'Ajanda' })).toBe(false);
    expect(engelli(me, { izin: PERMISSION.sistemHata, rol: 'Sistem' })).toBe(true);
  });

  it('menüde görünen her ekrana GİRİLEBİLİR (menü ile rota tutarlı)', () => {
    // Menü öğeyi gösterip rota kapıdan çevirirse kullanıcı tıklayıp duvara
    // çarpar; tersi olursa yetki verilmiş ekran ulaşılamaz kalır.
    const me = kullanici({
      izinler: [PERMISSION.talepGoruntule, PERMISSION.ajandaGoruntule, PERMISSION.cicekGoruntule],
    });

    for (const o of TUM_OGELER) {
      if (!gorunurMu(o, me)) continue;
      expect(
        engelli(me, { izin: o.izin, politika: o.politika, rol: o.rol }),
        `${o.yol} menüde var ama rota engelliyor`,
      ).toBe(false);
    }
  });
});

describe('Ekran içi düğmeler', () => {
  /** Düğme koşullarının kaynaktan okunan hâli. */
  function kaynak(yol: string): string {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('node:fs') as typeof import('node:fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('node:path') as typeof import('node:path');
    return fs.readFileSync(path.join(process.cwd(), 'src', yol), 'utf8');
  }

  /**
   * Yıkıcı ve yazan eylemler izin kapısından geçmeli.
   *
   * Sunucu zaten reddediyor; buradaki mesele kullanıcıya çalışmayan bir düğme
   * göstermemek. "Başkan onaylar, personel ekler" ayrımı ancak düğmeler de
   * ayrıldığında görünür oluyor.
   */
  const BEKLENEN: [string, string[]][] = [
    ['screens/request/RequestActions.tsx', [
      'PERMISSION.talepAjandayaEkle', 'PERMISSION.talepDuzenle', 'PERMISSION.talepHavale',
      'PERMISSION.talepArsivle', 'PERMISSION.talepSil',
    ]],
    ['screens/agenda/EventActions.tsx', [
      'PERMISSION.ajandaDuzenle', 'PERMISSION.ajandaHavale', 'PERMISSION.ajandaSil',
      'PERMISSION.ajandaSmsGonder', 'PERMISSION.ajandaStatuDegistir', 'PERMISSION.cicekYonet',
    ]],
    ['screens/Requests.tsx', ['PERMISSION.talepEkle']],
    ['screens/Agenda.tsx', ['PERMISSION.ajandaEkle']],
    ['screens/Protocol.tsx', ['PERMISSION.protokolYonet']],
    ['screens/Invitations.tsx', ['PERMISSION.davetYonet']],
    ['screens/Flowers.tsx', ['PERMISSION.cicekYonet']],
    ['screens/PublicDays.tsx', ['PERMISSION.halkgunuYonet']],
    ['screens/publicday/Applications.tsx', ['PERMISSION.halkgunuBasvuru']],
    ['screens/publicday/PublicDayDetail.tsx', [
      'PERMISSION.halkgunuYonet', 'PERMISSION.halkgunuAtama', 'PERMISSION.halkgunuSms',
      'PERMISSION.halkgunuCiktiAl', 'PERMISSION.halkgunuGorusme',
    ]],
    // Özgeçmiş havuzu: paylaşma ve silme kişisel veriye dokunuyor, ekleme ve
    // düzenleme de kurum genelinde görünen bir kayıt üretiyor.
    ['screens/ResumePool.tsx', [
      'PERMISSION.ozgecmisEkle', 'PERMISSION.ozgecmisDuzenle', 'PERMISSION.ozgecmisSil',
      'PERMISSION.ozgecmisPaylas',
    ]],
  ];

  for (const [dosya, izinler] of BEKLENEN) {
    it(`${dosya} eylemleri izne bağlı`, () => {
      const metin = kaynak(dosya);
      for (const i of izinler) {
        expect(metin.includes(`hasPermission(${i})`), `${dosya} → ${i}`).toBe(true);
      }
    });
  }

  it('Protokol yazma yetkisi ROL adına bakmıyor', () => {
    // Önce `roller.some(r => r === 'Admin')` yazıyordu; `protokol.yonet` izni
    // verilen bir rol düğmeleri göremiyordu.
    const metin = kaynak('screens/Protocol.tsx');
    expect(metin).not.toMatch(/roller.*'Admin'/);
  });
});

describe('Oturum taklitleri', () => {
  /**
   * Oturum bağlamını taklit eden HER test dosyası `iznimVar` vermeli.
   *
   * Bulunan gerçek hata: `iznimVar` bağlama eklendiğinde `ekranlar.test.tsx`
   * içindeki elle kurulmuş taklit güncellenmemişti ve ilgisiz 20 ekran testi
   * "düğme bulunamadı" ile düştü. Eksik bir alan, o alanı hiç okumayan
   * testleri de düşürüyor — hangisinin bozuk olduğu ilk bakışta anlaşılmıyor.
   */
  it('useSession taklidi eden her dosya hasPermission sağlıyor', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('node:fs') as typeof import('node:fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('node:path') as typeof import('node:path');
    const dizin = path.join(process.cwd(), 'test');

    for (const ad of fs.readdirSync(dizin)) {
      if (!ad.endsWith('.tsx') && !ad.endsWith('.ts')) continue;
      const metin = fs.readFileSync(path.join(dizin, ad), 'utf8');
      if (!metin.includes('useSession: () =>')) continue;
      expect(metin.includes('hasPermission'), `${ad} taklidinde hasPermission yok`).toBe(true);

      // Taklit ÇOKLU izni de VEYA ile işlemeli. `(i: string)` imzalı bir
      // taklit, dizi ilan eden menü öğelerini sessizce gizliyordu: ekran
      // testleri "menü yok" diye düşüyor, sebep ise ilgisiz bir dosyadaki
      // eski taklit oluyordu.
      expect(
        /hasPermission: \(i: string \| string\[\]\)/.test(metin),
        `${ad} taklidi çoklu izni işlemiyor`,
      ).toBe(true);
    }
  });
});

describe('Boş durum eylemleri', () => {
  /**
   * `BosDurum` içindeki EKLEME düğmeleri de izin kapısından geçmeli.
   *
   * Bulunan gerçek hata: ajanda araç çubuğundaki "Yeni etkinlik" düğmesi
   * `iznimVar(PERMISSION.ajandaEkle)` ile korunuyordu ama liste BOŞKEN çizilen
   * ikinci bir "Yeni etkinlik" düğmesi vardı ve o kapının dışındaydı.
   * Basın kullanıcısında liste çoğu zaman boş — yani yetkisi olmayan
   * kullanıcının gördüğü TEK düğme korumasız olandı.
   *
   * Yönlendirme düğmeleri ("Taleplere dön") kapsam dışı: bir yetki
   * kullanmıyorlar, kullanıcıyı geldiği yere döndürüyorlar.
   */
  it('ekleme düğmesi içeren her boş durum izin kapısından geçiyor', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('node:fs') as typeof import('node:fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('node:path') as typeof import('node:path');

    /** Ekleme niyeti taşıyan etiketler. */
    const EKLEME = /(Yeni|ekle|Ekle|oluştur|Oluştur)/;

    const kok = path.join(process.cwd(), 'src', 'screens');
    const dosyalar: string[] = [];
    const gez = (d: string) => {
      for (const ad of fs.readdirSync(d)) {
        const tam = path.join(d, ad);
        if (fs.statSync(tam).isDirectory()) gez(tam);
        else if (ad.endsWith('.tsx')) dosyalar.push(tam);
      }
    };
    gez(kok);

    const acikta: string[] = [];

    for (const dosya of dosyalar) {
      const metin = fs.readFileSync(dosya, 'utf8');
      let i = metin.indexOf('eylem={');
      while (i >= 0) {
        // Yalnızca DÜĞME taşıyan `eylem` blokları. `eylem={ikon}` gibi salt
        // görsel bir başlık süsü, kapı istemiyor; onu ihlal saymak, düzgün
        // yazılmış kodu kırmızıya düşürüyordu (ilk sürüm tam bunu yaptı).
        const dugmeSonu = metin.indexOf('</Buton>', i);
        const sonrakiEylem = metin.indexOf('eylem={', i + 1);
        const dugmeVar =
          dugmeSonu > i && (sonrakiEylem < 0 || dugmeSonu < sonrakiEylem);
        if (!dugmeVar) {
          i = sonrakiEylem;
          continue;
        }
        const son = metin.indexOf('}', dugmeSonu);
        const blok = metin.slice(i, son > i ? son : i + 400);
        // Kapı doğrudan `iznimVar(...)` olabilir ya da ondan TÜRETİLMİŞ
        // yerel bir değişken (`const yazabilir = iznimVar(...)`). İkincisini
        // ihlal saymak, doğru yazılmış kodu yanlış işaretlerdi.
        const yerelKapilar = [...metin.matchAll(/const (\w+) = hasPermission\(/g)]
          .map((m) => m[1]);
        const korumali =
          blok.includes('hasPermission') || yerelKapilar.some((d) => blok.includes(d));

        if (EKLEME.test(blok) && !korumali) {
          acikta.push(`${path.basename(dosya)}: ${blok.slice(0, 60).replace(/\s+/g, ' ')}`);
        }
        i = metin.indexOf('eylem={', i + 1);
      }
    }

    expect(acikta, `izin kapısı olmayan ekleme düğmesi:\n${acikta.join('\n')}`).toEqual([]);
  });
});

// ═══════════════════════════════════════════════════ form diyaloglarında ritim

describe('FormModal dikey ritim', () => {
  /**
   * Alanlar arasındaki boşluk diyaloğun KENDİSİNDE tanımlı olmalı.
   *
   * Bulunan gerçek hata: `FormModal` gövdesinde `space-y-*` yoktu ve yalnızca
   * kendi sarmalayıcısında boşluk taşıyan formlar (talep, etkinlik) doğru
   * görünüyordu. Alanlarını doğrudan veren her diyalogda kutular BİTİŞİK
   * çiziliyordu: süzgeç tabakasında tarih alanı ile onay kutusu birbirine
   * yapışıktı. Ritmi her çağrı yerinde tekrar etmek, birini unutmak demek.
   */
  it('gövde alanlar arasında boşluk bırakıyor', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('node:fs') as typeof import('node:fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('node:path') as typeof import('node:path');
    const metin = fs.readFileSync(
      path.join(process.cwd(), 'src', 'components', 'FormModal.tsx'),
      'utf8',
    );

    // Çıpa `overflow-y-auto`: gövdeyi kayan kap yapan sınıf bu ve gövdenin
    // tanımı da bu. (Bir dönem çıpa `data-kaydirilabilir` niteliğiydi; o
    // nitelik elle yazılmış sürükleme kancasına aitti, `vaul`'a geçince
    // kanca kaldırıldı ve nitelik hiçbir şey ifade etmez oldu.)
    const govde = /className="(min-h-0 flex-1[^"]*overflow-y-auto[^"]*)"/.exec(metin);
    expect(govde, 'FormModal gövdesi bulunamadı').not.toBeNull();
    expect(govde![1]).toMatch(/space-y-\d/);
  });
});
