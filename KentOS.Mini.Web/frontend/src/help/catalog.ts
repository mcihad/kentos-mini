import anaSayfa from './texts/ana-sayfa.md?raw';
import ajanda from './texts/ajanda.md?raw';
import etkinlikDetay from './texts/etkinlik-detay.md?raw';
import takvim from './texts/takvim.md?raw';
import talepler from './texts/talepler.md?raw';
import talepDetay from './texts/talep-detay.md?raw';
import halkGunu from './texts/halk-gunu.md?raw';
import halkGunuBasvurular from './texts/halk-gunu-basvurular.md?raw';
import halkGunuDetay from './texts/halk-gunu-detay.md?raw';
import halkGunuSalon from './texts/halk-gunu-salon.md?raw';
import ozgecmisler from './texts/ozgecmisler.md?raw';
import gonderim from './texts/gonderim.md?raw';
import protokol from './texts/protokol.md?raw';
import davetler from './texts/davetler.md?raw';
import istatistikler from './texts/istatistikler.md?raw';
import cicek from './texts/cicek.md?raw';
import yonetim from './texts/yonetim.md?raw';
import tanimlar from './texts/tanimlar.md?raw';
import kurum from './texts/kurum.md?raw';
import hatalar from './texts/hatalar.md?raw';
import bildirimler from './texts/bildirimler.md?raw';
import ayarlar from './texts/ayarlar.md?raw';
import mobil from './texts/mobil.md?raw';
import kurulum from './texts/kurulum.md?raw';

/** Yardım merkezinde konuların toplandığı başlıklar — menüyle AYNI gruplar. */
export type HelpGroup = 'Genel' | 'Halk Günü' | 'Özgeçmişler' | 'Program' | 'Yönetim';

export type HelpEntry = {
  /** Panel başlığı. */
  baslik: string;
  /** Başlığın altındaki tek cümlelik özet. */
  ozet: string;
  /**
   * Yardım merkezindeki grubu.
   *
   * Menüdeki grup adlarıyla AYNI: kullanıcı yardımı, ekranı menüde aradığı
   * yerde arıyor. Ayrı bir sınıflandırma uydurmak, iki farklı zihin haritası
   * ezberletmek olurdu.
   */
  grup: HelpGroup;
  metin: string;
};

/**
 * EKRAN → YARDIM eşlemesi.
 *
 * Anahtarlar rota kalıbı: `:id` yerine herhangi bir değer gelebilir. Sıra
 * ÖNEMLİ — daha ÖZEL olan yol önce yazılır, yoksa `/halk-gunu/:id` kalıbı
 * `/halk-gunu/basvurular` sayfasını da yakalar.
 *
 * Metinler `metinler/*.md` içinde düz markdown; güncellemek için React bilmek
 * gerekmiyor.
 */
const KATALOG: { kalip: string; kayit: HelpEntry }[] = [
  /*
    EKRANI OLMAYAN KONULAR.

    `kalip: ''` — hiçbir yola bağlı değiller, yani üst çubuktaki düğmede
    çıkmazlar; yalnızca Yardım Merkezi'nde listelenirler. Anlattıkları şey tek
    bir ekran değil, uygulamanın TAMAMINDA geçerli olan davranış: telefondaki
    yerleşim ve kurulum/bildirim akışı. Bunları her ekranın metnine tekrar
    yazmak, yirmi bir dosyayı aynı anda güncel tutmayı gerektirirdi.
  */
  {
    kalip: '',
    kayit: {
      grup: 'Genel',
      baslik: 'Telefonda Kullanım',
      ozet: 'Yuvarlak düğme, alt tabakalar ve arama/süzgeç düzeni',
      metin: mobil,
    },
  },
  {
    kalip: '',
    kayit: {
      grup: 'Genel',
      baslik: 'Kurulum ve Bildirimler',
      ozet: 'Uygulamayı ana ekrana ekleme ve bildirim izni',
      metin: kurulum,
    },
  },
  {
    kalip: '/',
    kayit: {
      grup: 'Genel',
      baslik: 'Ana Sayfa',
      ozet: 'Günün programı, bekleyen işler ve sıradaki etkinlik',
      metin: anaSayfa,
    },
  },
  {
    kalip: '/ajanda/:id',
    kayit: {
      grup: 'Genel',
      baslik: 'Etkinlik Detayı',
      ozet: 'Bir etkinliğin bütün bilgisi ve yapılabilecekler',
      metin: etkinlikDetay,
    },
  },
  {
    kalip: '/ajanda',
    kayit: {
      grup: 'Genel',
      baslik: 'Ajanda',
      ozet: 'Makam programını görme ve yönetme',
      metin: ajanda,
    },
  },
  {
    kalip: '/takvim',
    kayit: {
      grup: 'Genel',
      baslik: 'Takvim',
      ozet: 'Gün, hafta, ay ve yıl görünümleri',
      metin: takvim,
    },
  },
  {
    kalip: '/talepler/:id',
    kayit: {
      grup: 'Genel',
      baslik: 'Talep Detayı',
      ozet: 'Tek bir talebin geçmişi ve sonuçlandırılması',
      metin: talepDetay,
    },
  },
  {
    kalip: '/talepler',
    kayit: {
      grup: 'Genel',
      baslik: 'Talepler',
      ozet: 'Vatandaş taleplerini kaydetme, izleme ve havale etme',
      metin: talepler,
    },
  },
  {
    kalip: '/halk-gunu/basvurular',
    kayit: {
      grup: 'Halk Günü',
      baslik: 'Vatandaş Havuzu',
      ozet: 'Halk gününde görüşmek isteyenlerin bekleme listesi',
      metin: halkGunuBasvurular,
    },
  },
  {
    kalip: '/halk-gunu/:id/salon',
    kayit: {
      grup: 'Halk Günü',
      baslik: 'Salon Modu',
      ozet: 'Halk günü sırasında sırayı yürütme',
      metin: halkGunuSalon,
    },
  },
  {
    kalip: '/halk-gunu/:id',
    kayit: {
      grup: 'Halk Günü',
      baslik: 'Halk Günü Ayrıntısı',
      ozet: 'Zaman dilimleri, atama, toplu SMS ve çıktılar',
      metin: halkGunuDetay,
    },
  },
  {
    kalip: '/halk-gunu',
    kayit: {
      grup: 'Halk Günü',
      baslik: 'Halk Günleri',
      ozet: 'Halk günü oluşturma ve günün özetini görme',
      metin: halkGunu,
    },
  },
  {
    kalip: '/ozgecmisler',
    kayit: {
      grup: 'Özgeçmişler',
      baslik: 'Özgeçmiş Havuzu',
      ozet: 'İş başvurularını toplama, arama ve yönlendirme',
      metin: ozgecmisler,
    },
  },
  {
    kalip: '/gonderim',
    kayit: {
      grup: 'Genel',
      baslik: 'Dosya Gönderimi',
      ozet: 'Kurum içinde belge gönderme ve üzerinde yazışma',
      metin: gonderim,
    },
  },
  {
    kalip: '/protokol',
    kayit: {
      grup: 'Program',
      baslik: 'İl Protokolü',
      ozet: 'Tören ve davetlerde aranacak kişiler',
      metin: protokol,
    },
  },
  {
    kalip: '/davetler',
    kayit: {
      grup: 'Program',
      baslik: 'Davetler',
      ozet: 'Kimin çağrıldığı ve kimin geleceği',
      metin: davetler,
    },
  },
  {
    kalip: '/istatistikler',
    kayit: {
      grup: 'Yönetim',
      baslik: 'İstatistikler',
      ozet: 'Birimin işi sayılarla',
      metin: istatistikler,
    },
  },
  {
    kalip: '/cicek',
    kayit: {
      grup: 'Program',
      baslik: 'Çiçek Gönderi',
      ozet: 'Çiçek siparişlerinin takibi',
      metin: cicek,
    },
  },
  {
    kalip: '/yonetim',
    kayit: {
      grup: 'Yönetim',
      baslik: 'Yönetim',
      ozet: 'Kullanıcı, birim, rol ve oturum kayıtları',
      metin: yonetim,
    },
  },
  {
    kalip: '/tanimlar',
    kayit: {
      grup: 'Yönetim',
      baslik: 'Tanımlar',
      ozet: 'Açılır listeleri besleyen referans veriler',
      metin: tanimlar,
    },
  },
  {
    kalip: '/kurum',
    kayit: {
      grup: 'Yönetim',
      baslik: 'Kurum Bilgileri',
      ozet: 'Kurum adı, iletişim, uygulama adı, amblem ve kurumsal renkler',
      metin: kurum,
    },
  },
  {
    kalip: '/hatalar',
    kayit: {
      grup: 'Yönetim',
      baslik: 'Sistem Hataları',
      ozet: 'Beklenmeyen durumların kaydı',
      metin: hatalar,
    },
  },
  {
    kalip: '/bildirimler',
    kayit: {
      grup: 'Genel',
      baslik: 'Bildirimler',
      ozet: 'Size gelen haberler ve bildirim izni',
      metin: bildirimler,
    },
  },
  {
    kalip: '/ayarlar',
    kayit: {
      grup: 'Genel',
      baslik: 'Ayarlar',
      ozet: 'Tema, bildirim tercihleri ve şifre',
      metin: ayarlar,
    },
  },
];

/**
 * Bütün yardım konuları — gruplarıyla.
 *
 * <p>
 * Yardım Merkezi (<c>/yardim</c>) bunu okuyor. Kataloğu ikinci kez elle
 * listelemek, yeni bir ekranın yardımının merkeze eklenmeyi unutulması
 * demekti; tek kaynak burası.
 * </p>
 */
export function helpTopics(): { kalip: string; kayit: HelpEntry }[] {
  return KATALOG;
}

/** Verilen yol için yardım kaydı; eşleşme yoksa `null`. */
export function findHelp(yol: string): HelpEntry | null {
  const temiz = yol.replace(/\/+$/, '') || '/';
  // Boş kalıp = ekranı olmayan konu; hiçbir yolla eşleşmez.
  return KATALOG.find(({ kalip }) => kalip !== '' && kalipUyar(kalip, temiz))?.kayit ?? null;
}

function kalipUyar(kalip: string, yol: string): boolean {
  if (kalip === '/') return yol === '/';

  const k = kalip.split('/').filter(Boolean);
  const y = yol.split('/').filter(Boolean);
  if (k.length !== y.length) return false;

  return k.every((parca, i) => (parca.startsWith(':') ? y[i].length > 0 : parca === y[i]));
}
