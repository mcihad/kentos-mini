import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import Agenda from '../src/screens/Agenda';
import EventDetail from '../src/screens/EventDetail';
import Statistics from '../src/screens/Statistics';
import RequestDetail from '../src/screens/RequestDetail';
import Requests from '../src/screens/Requests';
import Administration from '../src/screens/Administration';
import { SAHTE_BEN, fetchTaklit, kur, sayfali } from './helpers';

/**
 * Oturum bağlamı taklit edilir.
 *
 * Ekranlar `useOturum`u yalnızca "kim bu kullanıcı, neye yetkisi var" için
 * okuyor; gerçek sağlayıcıyı kurmak her testte giriş isteği taklit etmeyi
 * gerektirirdi ve test ettiğimiz şey o değil.
 */
vi.mock('../src/auth/SessionProvider', async () => {
  const gercek = await vi.importActual<typeof import('../src/auth/SessionProvider')>(
    '../src/auth/SessionProvider',
  );
  return {
    ...gercek,
    useSession: () => ({
      me: SAHTE_BEN,
      ready: true,
      signIn: vi.fn(),
      signOut: vi.fn(),
      hasPolicy: (p: string) => SAHTE_BEN.yetkiler.includes(p),
      // İZİN de taklide girmeli: eksik kalırsa ekranlardaki bütün eylem
      // düğmeleri sessizce kaybolur ve testler "buton bulunamadı" der.
      hasPermission: (i: string | string[]) =>
        [i].flat().some((x) => SAHTE_BEN.izinler?.includes(x) ?? false),
    }),
  };
});

/**
 * Ekran testleri.
 *
 * Gerçek bileşen ağacı jsdom'da render edilir; yalnızca `fetch` taklit
 * edilir. Böylece testler "şu bileşen çağrıldı" değil, KULLANICININ EKRANDA
 * NE GÖRDÜĞÜ üzerinden konuşur — bileşen yeniden düzenlense de anlamlı kalır.
 */

const TALEP_DURUMLARI = [
  { id: 1, durumAd: 'Beklemede', renk: '#F0A202' },
  { id: 2, durumAd: 'Tamamlandı', renk: '#2E8B57' },
];

const TALEPLER = [
  {
    id: 10, konu: 'Yol asfaltlama talebi', ad: 'Ayşe', soyad: 'Yılmaz',
    adSoyad: 'Ayşe Yılmaz', meslek: 'Öğretmen',
    baslangicTarih: '2026-08-10T10:00:00',
    durumId: 1, durumAd: 'Beklemede', durumRenk: '#F0A202',
    tipId: 1, tipAd: 'Halk Günü', tipRenk: '#C9A227',
    ajandayaEklendi: false, arsivlendi: false,
  },
  {
    id: 11, konu: 'Park aydınlatması', ad: 'Mehmet', soyad: 'Demir',
    adSoyad: 'Mehmet Demir', meslek: 'Mühendis',
    baslangicTarih: '2026-08-11T11:00:00',
    durumId: 2, durumAd: 'Tamamlandı', durumRenk: '#2E8B57',
    tipId: 2, tipAd: 'Toplantı', tipRenk: '#0B1A3A',
    ajandayaEklendi: true, arsivlendi: false,
  },
];

/** Durum çipleri artık sunucudan sayaçla geliyor. */
const DURUM_SAYACLARI = [
  { durumId: 1, durumAd: 'Beklemede', renk: '#F0A202', adet: 1 },
  { durumId: 2, durumAd: 'Tamamlandı', renk: '#2E8B57', adet: 1 },
];

const TIPLER = [
  { id: 1, ad: 'Toplantı', renk: '#0B1A3A' },
  { id: 2, ad: 'Ziyaret', renk: '#C9A227' },
];

const ETKINLIK_DURUMLARI = [{ id: 1, ad: 'Planlandı', renk: '#0B1A3A' }];

const ETKINLIKLER = [
  {
    id: 100, baslik: 'Muhtarlar toplantısı', baslangic: '2026-08-20T10:00:00',
    bitis: '2026-08-20T11:00:00', tumGun: false, konum: 'Belediye Meclis Salonu',
    tipId: 1, tipAd: 'Toplantı', tipRenk: '#0B1A3A',
    durumId: 1, durumAd: 'Planlandı', durumRenk: '#0B1A3A',
    statu: 0, gizli: false, seriId: null, seriAyrik: false,
    resimVar: false, basinKatilsin: true,
  },
  {
    id: 101, baslik: 'Gizli görüşme', baslangic: '2026-08-21T14:00:00',
    bitis: '2026-08-21T15:00:00', tumGun: false, konum: null,
    tipId: 2, tipAd: 'Ziyaret', tipRenk: '#C9A227',
    durumId: 1, durumAd: 'Planlandı', durumRenk: '#0B1A3A',
    statu: 0, gizli: true, seriId: 5, seriAyrik: false,
    resimVar: false, basinKatilsin: false,
  },
];

beforeEach(() => {
  // Testler sabit bir "bugün" ister; aksi hâlde 6 aylık sorgu penceresi
  // kayar ve tarih iddiaları yarın kırılır.
  vi.useFakeTimers({ shouldAdvanceTime: true });
  vi.setSystemTime(new Date(2026, 7, 12, 9, 0, 0));
});

afterEach(() => {
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

// ══════════════════════════════════════════════════════════════ Talepler

describe('Talepler ekranı', () => {
  const yanitlar = {
    '/talep': sayfali(TALEPLER),
    '/talep/durum-sayaclari': DURUM_SAYACLARI,
    '/ayar/talep-durumlari': sayfali(TALEP_DURUMLARI),
  };

  it('talepleri listeler', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Requests />);

    expect(await screen.findAllByText('Yol asfaltlama talebi')).not.toHaveLength(0);
    expect(screen.getAllByText('Park aydınlatması').length).toBeGreaterThan(0);
  });

  it('arama SUNUCUYA gider — istemcide süzmez', async () => {
    // Süzme artık sunucuda; testin doğrulaması gereken şey listenin
    // istemcide filtrelenmesi değil, isteğin `ara=` ile gitmesi.
    const taklit = fetchTaklit(yanitlar);
    vi.stubGlobal('fetch', taklit);
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Requests />);

    await screen.findAllByText('Yol asfaltlama talebi');
    await kullanici.type(screen.getByLabelText('Taleplerde ara'), 'park');

    // Arama 300 ms geciktiriliyor — her tuş vuruşu istek üretmesin diye.
    await vi.advanceTimersByTimeAsync(400);

    await waitFor(() => {
      const cagrilar = taklit.mock.calls.map((c) => String(c[0]));
      expect(cagrilar.some((u) => u.includes('ara=park'))).toBe(true);
    });
  });

  it('durum çipi süzgeci sunucuya gönderir', async () => {
    const taklit = fetchTaklit(yanitlar);
    vi.stubGlobal('fetch', taklit);
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Requests />);

    await screen.findAllByText('Yol asfaltlama talebi');
    await kullanici.click(screen.getByRole('button', { name: /Tamamlandı/ }));

    await waitFor(() => {
      const cagrilar = taklit.mock.calls.map((c) => String(c[0]));
      expect(cagrilar.some((u) => u.includes('durumId=2'))).toBe(true);
    });
  });

  it('durum çiplerini sayaç ucundan kurar', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Requests />);

    // Sıfır kayıtlı durumlar da çip olarak görünmeli; kaybolan çip
    // "böyle bir durum yok" izlenimi verir.
    expect(await screen.findByRole('button', { name: /Beklemede/ })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Tamamlandı/ })).toBeInTheDocument();
  });

  it('durum rengini KAYITTAN alır, ayrı bir eşlemeden değil', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const { container } = kur(<Requests />);

    await screen.findAllByText('Yol asfaltlama talebi');

    // Rozet satırla birlikte gelen `durumRenk` ile boyanır; filtre ÇİPİ de
    // "Beklemede" yazdığı için `span` seçicisiyle ayrıştırılır.
    const rozetler = screen
      .getAllByText('Beklemede')
      .filter((e) => e.tagName === 'SPAN' && e.getAttribute('style'));
    expect(rozetler.length).toBeGreaterThan(0);
    // jsdom onaltılık rengi rgb()'ye çevirir; #F0A202 = rgb(240, 162, 2).
    expect(rozetler[0].getAttribute('style')).toContain('rgb(240, 162, 2)');

    // Tip adı da kayıttan geliyor.
    expect(container.textContent).toContain('Halk Günü');
  });

  it('liste boşsa anlaşılır bir mesaj verir', async () => {
    vi.stubGlobal('fetch', fetchTaklit({ ...yanitlar, '/talep': sayfali([]) }));
    kur(<Requests />);

    expect(await screen.findByText('Talep yok')).toBeInTheDocument();
  });
});

describe('Talep detayı', () => {
  const yanitlar = {
    '/talep/10': {
      id: 10, konu: 'Yol asfaltlama talebi', adSoyad: 'Ayşe Yılmaz',
      meslek: 'Öğretmen', telefon: '05551112233', adres: 'Merkez Mah.',
      aciklama: 'Sokağımızın asfaltı bozuk.', baslangicTarih: '2026-08-10T10:00:00',
      randevuDurumId: 1, ajandaDurum: false, birimId: 1, arsivlendi: false,
    },
    '/ayar/talep-durumlari': sayfali(TALEP_DURUMLARI),
    '/ayar/birimler': sayfali([{ id: 1, ad: 'Özel Kalem' }]),
  };

  it('talep bilgilerini gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<RequestDetail />, { yol: '/talepler/10', rotaYolu: '/talepler/:id' });

    expect(await screen.findByRole('heading', { name: 'Yol asfaltlama talebi' })).toBeInTheDocument();
    expect(screen.getByText('Ayşe Yılmaz')).toBeInTheDocument();
    expect(screen.getByText('05551112233')).toBeInTheDocument();
  });

  it('ajandaya eklenmemişse ekleme düğmesi çıkar', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<RequestDetail />, { yol: '/talepler/10', rotaYolu: '/talepler/:id' });

    expect(await screen.findByRole('button', { name: /Ajandaya ekle/ })).toBeInTheDocument();
  });

  it('kayıt yoksa hata ekranı gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit({}));
    kur(<RequestDetail />, { yol: '/talepler/99', rotaYolu: '/talepler/:id' });

    expect(await screen.findByText('Talep bulunamadı')).toBeInTheDocument();
  });
});

// ════════════════════════════════════════════════════════════════ Ajanda

describe('Ajanda ekranı', () => {
  const yanitlar = {
    '/takvim/aralik': ETKINLIKLER,
    '/ayar/tipler': sayfali(TIPLER),
    '/ayar/etkinlik-durumlari': sayfali(ETKINLIK_DURUMLARI),
  };

  it('etkinlikleri listeler', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Agenda />);

    expect(await screen.findAllByText('Muhtarlar toplantısı')).not.toHaveLength(0);
  });

  it('gizli etkinliği açıkça işaretler', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Agenda />);

    await screen.findAllByText('Muhtarlar toplantısı');
    // Sunucu gizliyi zaten süzüyor; buraya gelen kayıt görülebilir demektir,
    // ama kullanıcı bunun gizli olduğunu BİLMELİ. Program görünümünde işaret
    // ikon DEĞİL metin — ekran okuyucuda da, küçük ekranda da okunur.
    expect(screen.getAllByText('Gizli').length).toBeGreaterThan(0);
  });

  it('varsayılan sekme PROGRAM görünümüdür', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Agenda />);

    // Eski MVC ajandasıyla aynı düzen: kullanıcılar iki yıldır bunu kullanıyor.
    await screen.findAllByText('Muhtarlar toplantısı');
    const programSekmesi = screen.getByRole('tab', { name: /Program/ });
    expect(programSekmesi).toHaveAttribute('data-state', 'active');
  });

  it('program görünümü kartı DURUM rengiyle boyar', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Agenda />);

    const baslik = (await screen.findAllByText('Muhtarlar toplantısı'))[0];
    const kart = baslik.closest('a')!;

    // Eski arayüz de kart zeminini `Durum.Renk` ile boyuyordu; renk kayıtla
    // birlikte geliyor, istemcide eşleme yok.
    expect(kart.getAttribute('style')).toContain('color-mix');
  });

  // Tip süzgeci artık çip şeridi DEĞİL, açılır menü: araç çubuğu dört sıradan
  // ikiye indi. Süzme davranışı aynı kaldı, yolu değişti.
  it('tipe göre süzer', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Agenda />);

    await screen.findAllByText('Muhtarlar toplantısı');

    await kullanici.click(screen.getByRole('button', { name: /Tip süzgeci/ }));
    await kullanici.click(await screen.findByRole('menuitem', { name: /Ziyaret/ }));

    await waitFor(() => {
      expect(screen.queryByText('Muhtarlar toplantısı')).not.toBeInTheDocument();
    });
  });

  it('liste sekmesine geçilebilir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Agenda />);

    await screen.findAllByText('Muhtarlar toplantısı');
    await kullanici.click(screen.getByRole('tab', { name: /Liste/ }));

    // Tablo görünümü — sütun başlıkları program görünümünde yok.
    expect(await screen.findByRole('table')).toBeInTheDocument();
  });
});

describe('Etkinlik detayı', () => {
  const yanitlar = {
    '/etkinlik/100': {
      id: 100, baslik: 'Muhtarlar toplantısı', aciklama: 'Yıllık değerlendirme.',
      konum: 'Meclis Salonu', baslangicTarihi: '2026-08-20T10:00:00',
      bitisTarihi: '2026-08-20T11:00:00', tumGun: false, basinKatilsin: true,
      durumId: 1, randevuTipId: 1, status: 0, gizli: false,
      katilimcilar: [
        { id: 2, ad: 'Ali', soyad: 'Vural', unvan: 'Müdür', birimAd: 'Fen İşleri', tamAd: 'Ali Vural' },
      ],
      seriId: null, seriAyrik: false,
    },
    '/etkinlik/100/notlar': [
      { id: 1, not: 'Salon ayarlandı.', ajandaId: 100, olusturan: 'admin', olusturulmaTarihi: '2026-08-12T09:00:00' },
    ],
    '/etkinlik/100/olaylar': [
      { id: 1, ajandaId: 100, tip: 0, kullanici: 'admin', tarih: '2026-08-01T08:00:00', aciklama: null, degisiklikler: [] },
      { id: 2, ajandaId: 100, tip: 1, kullanici: 'admin', tarih: '2026-08-05T08:00:00', aciklama: null,
        degisiklikler: [{ alan: 'Konum', eski: 'A Salonu', yeni: 'Meclis Salonu' }] },
    ],
    '/etkinlik/100/fotograflar': [],
    '/ayar/tipler': sayfali(TIPLER),
    '/ayar/etkinlik-durumlari': sayfali(ETKINLIK_DURUMLARI),
  };

  it('etkinlik bilgilerini gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<EventDetail />, { yol: '/ajanda/100', rotaYolu: '/ajanda/:id' });

    expect(await screen.findByRole('heading', { name: 'Muhtarlar toplantısı' })).toBeInTheDocument();
    expect(screen.getByText('Meclis Salonu')).toBeInTheDocument();
    expect(screen.getByText('10:00 – 11:00')).toBeInTheDocument();
  });

  // KATILIMCI BİRİM ile GÖREBİLECEK KİŞİ ayrı bölümlerde: biri "kim
  // katılacak", öteki "kim görebilir". Tek listede durdukları sürece aynı şey
  // sanılıyorlardı.
  it('görebilecek kişiler KAPALI akordiyonda başlar, açılınca listelenir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<EventDetail />, { yol: '/ajanda/100', rotaYolu: '/ajanda/:id' });

    const baslik = await screen.findByRole('button', { name: /Görebilecek kişiler/ });
    expect(screen.queryByText('Ali Vural')).not.toBeInTheDocument();

    await kullanici.click(baslik);
    expect(await screen.findByText('Ali Vural')).toBeInTheDocument();
  });

  it('kişi katılımcı, KATILIMCI BİRİM bölümüne DÜŞMEZ', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<EventDetail />, { yol: '/ajanda/100', rotaYolu: '/ajanda/:id' });

    await kullanici.click(await screen.findByRole('button', { name: /Katılımcı birimler/ }));

    // Kişi burada görünürse "davet edilen departman" sanılır.
    expect(await screen.findByText('Katılımcı birim eklenmemiş.')).toBeInTheDocument();
    expect(screen.queryByText('Ali Vural')).not.toBeInTheDocument();
  });

  it('geçmiş sekmesinde alan değişikliğini eski → yeni gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<EventDetail />, { yol: '/ajanda/100', rotaYolu: '/ajanda/:id' });

    await screen.findByRole('heading', { name: 'Muhtarlar toplantısı' });
    await kullanici.click(screen.getByRole('tab', { name: 'Geçmiş' }));

    // "Güncellendi" demek denetim için yetersiz; NEYİN değiştiği asıl bilgi.
    const eski = await screen.findByText('A Salonu');
    expect(eski).toBeInTheDocument();

    // Eski değer üstü çizili, yeni değer onun hemen yanında.
    const satir = eski.parentElement!;
    expect(within(satir).getByText('Meclis Salonu')).toBeInTheDocument();
    expect(within(satir).getByText('Konum:')).toBeInTheDocument();
  });

  it('notları zaman çizelgesinde gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<EventDetail />, { yol: '/ajanda/100', rotaYolu: '/ajanda/:id' });

    await screen.findByRole('heading', { name: 'Muhtarlar toplantısı' });
    await kullanici.click(screen.getByRole('tab', { name: 'Notlar' }));

    expect(await screen.findByText('Salon ayarlandı.')).toBeInTheDocument();
  });

  it('boş not gönderilemez', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<EventDetail />, { yol: '/ajanda/100', rotaYolu: '/ajanda/:id' });

    await screen.findByRole('heading', { name: 'Muhtarlar toplantısı' });
    await kullanici.click(screen.getByRole('tab', { name: 'Notlar' }));

    expect(await screen.findByRole('button', { name: /Not ekle/ })).toBeDisabled();
  });
});

// ═══════════════════════════════════════════════════════════ İstatistik

describe('İstatistikler', () => {
  const istatistik = {
    birimId: 1, birimAdi: 'Özel Kalem', baslangicTarihi: '2026-01-01T00:00:00',
    bitisTarihi: '2026-12-31T23:59:59', uretilmeZamani: '2026-08-12T09:00:00',
    ozet: {
      toplamEtkinlik: 412, aktifEtkinlik: 400, silinmisEtkinlik: 12,
      tamamlananEtkinlik: 280, iptalEdilenEtkinlik: 14, bekleyenEtkinlik: 106,
      gecmisEtkinlik: 300, gelecekEtkinlik: 100, bugunkuEtkinlik: 3,
      buHaftaEtkinlik: 11, buAyEtkinlik: 42, ortalamaSureDakika: 75,
      ortalamaNotSayisi: 1.4, ortalamaFotografSayisi: 0.6,
      toplamNot: 560, toplamFotograf: 240, tamamlanmaOrani: 68,
    },
    aylaraGore: [{ etiket: 'Oca', tarih: '2026-01-01T00:00:00', deger: 30 }],
    tipeGore: [{ etiket: 'Toplantı', deger: 200, yuzde: 48.5, renk: '#0B1A3A' }],
    durumaGore: [], statuyeGore: [], haftaGunineGore: [], saatAraliginaGore: [],
    gunBolumuneGore: [], konumaGore: [], olusturanaGore: [], yillaraGore: [],
    basinKatilimi: [], fotografDurumu: [], cicekDurumu: [], tumGunDurumu: [],
    tekrarDurumu: [], hazirlikDurumu: [], gunlukYogunluk: [], sureDagilimi: [],
    enCokNotAlanEtkinlikler: [], aylikTamamlanmaOrani: [],
  };

  it('özet sayıları gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit({ '/istatistik': istatistik }));
    kur(<Statistics />);

    expect(await screen.findByText('412')).toBeInTheDocument();
    expect(screen.getByText('280')).toBeInTheDocument();
    expect(screen.getByText('%68')).toBeInTheDocument();
  });

  it('ortalama süreyi okunur biçimde yazar', async () => {
    vi.stubGlobal('fetch', fetchTaklit({ '/istatistik': istatistik }));
    kur(<Statistics />);

    // 75 dakika → "1 sa 15 dk"; ham sayı kullanıcıya bir şey söylemez.
    expect(await screen.findByText('1 sa 15 dk')).toBeInTheDocument();
  });

  it('boş dağılımda çökmez', async () => {
    vi.stubGlobal('fetch', fetchTaklit({ '/istatistik': istatistik }));
    kur(<Statistics />);

    await screen.findByText('412');
    expect(screen.getAllByText('Veri yok').length).toBeGreaterThan(0);
  });
});

// ═════════════════════════════════════════════════════════════ Yönetim

describe('Yönetim', () => {
  const yanitlar = {
    '/yonetim/kullanicilar': sayfali([
      {
        id: 1, kullaniciAdi: 'admin', ad: 'Sistem', soyad: 'Yöneticisi',
        unvan: 'Yönetici', eposta: 'admin@ornek.test', telefon: '05551112233',
        birimId: 1, birimAdi: 'Özel Kalem', roller: ['Admin'],
        mobilBagli: true, webBagli: false,
      },
    ]),
    '/yonetim/roller': [
      { ad: 'Admin', kullaniciSayisi: 1, korumali: false },
      { ad: 'Sistem', kullaniciSayisi: 0, korumali: true },
    ],
    '/yonetim/birimler': [
      {
        id: 1, ad: 'Özel Kalem', yetkili: 'A. Yılmaz', unvan: 'Müdür',
        ustBirimId: null, kullaniciSayisi: 4, altBirimler: [
          { id: 2, ad: 'Randevu Birimi', yetkili: 'B. Kaya', ustBirimId: 1, kullaniciSayisi: 2, altBirimler: [] },
        ],
      },
    ],
  };

  it('kullanıcıları listeler', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    kur(<Administration />);

    expect(await screen.findByText(/Sistem Yöneticisi/)).toBeInTheDocument();
    expect(screen.getByText('@admin')).toBeInTheDocument();
  });

  it('bildirim JETONUNU göstermez, yalnızca bağlı olup olmadığını', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const { container } = kur(<Administration />);

    await screen.findByText(/Sistem Yöneticisi/);

    // v1'in formu jetonu düz metin basıyordu; jeton, sahibinin cihazına
    // bildirim göndermeye yeter.
    expect(container.textContent).not.toMatch(/[A-Za-z0-9_-]{100,}/);
    expect(screen.getByLabelText('Mobil cihaz bağlı')).toBeInTheDocument();
  });

  it('birim ağacını iç içe gösterir', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Administration />);

    await screen.findByText(/Sistem Yöneticisi/);
    await kullanici.click(screen.getByRole('tab', { name: /Birimler/ }));

    expect(await screen.findByText('Özel Kalem')).toBeInTheDocument();
    expect(screen.getByText('Randevu Birimi')).toBeInTheDocument();
  });

  it('korumalı rolü işaretler', async () => {
    vi.stubGlobal('fetch', fetchTaklit(yanitlar));
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Administration />);

    await screen.findByText(/Sistem Yöneticisi/);
    await kullanici.click(screen.getByRole('tab', { name: /Roller/ }));

    const sistem = (await screen.findByText('Sistem')).closest('div')!;
    expect(within(sistem.parentElement!).getByText(/korumalı/)).toBeInTheDocument();
  });

  it('silme onayı sormadan silmez', async () => {
    const taklit = fetchTaklit(yanitlar);
    vi.stubGlobal('fetch', taklit);
    const kullanici = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    kur(<Administration />);

    await screen.findByText(/Sistem Yöneticisi/);
    await kullanici.click(screen.getByRole('tab', { name: /Birimler/ }));

    const silmeler = await screen.findAllByRole('button', { name: 'Sil' });
    await kullanici.click(silmeler[0]);

    // Onay diyaloğu açılmalı; DELETE isteği HENÜZ gitmemeli.
    expect(await screen.findByText('Birim silinsin mi?')).toBeInTheDocument();
    const silmeIstekleri = taklit.mock.calls.filter(
      (c) => (c[1] as RequestInit | undefined)?.method === 'DELETE',
    );
    expect(silmeIstekleri).toHaveLength(0);
  });
});
