import {
  Bell, Bug, Building2, CalendarDays, ChartColumn, ClipboardCheck, ClipboardList,
  Flower2, Landmark, ListChecks,
  FileUser, LayoutDashboard, MailCheck, Paperclip, Settings, SlidersHorizontal, UserPlus, Users,
  type LucideIcon,
} from 'lucide-react';
import { PERMISSION } from '../components/permissions';

export type NavigationItem = {
  yol: string;
  etiket: string;
  ikon: LucideIcon;
  /**
   * Bu İZİN yoksa öğe gizlenir.
   *
   * Rol ve politika yerine izin: yetki dağılımı artık veritabanında ve
   * yönetim ekranından değiştirilebiliyor. Menüyü role bağlamak, yeni
   * oluşturulan bir rolün hiçbir menüyü görememesi demekti.
   */
  izin?: string | string[];
  /** ESKİ — politika. Yalnızca izin listesi gelmeyen sunucular için. */
  politika?: string;
  /** ESKİ — rol. Yalnızca izin listesi gelmeyen sunucular için. */
  rol?: string;
  /**
   * Kullanıcıya özel koşul. `false` dönerse öğe gizlenir.
   *
   * Politika ve rol sunucudan gelir; bu ise VERİYE bağlı bir koşul —
   * "gelen dosyan varsa göster" gibi.
   */
  kosul?: 'gelenDosya';
  /** Alt rotalar da bu öğeyi aktif tutar (design.md §6). */
  altYollar?: string[];
  /**
   * `altYollar` kapsamında olup KENDİ menü öğesi bulunan yollar.
   *
   * Vatandaş havuzu `/halk-gunu/basvurular` altında ama menüde ayrı bir
   * öğesi var; dışlanmasaydı o sayfada iki öğe birden vurgulanırdı.
   */
  haric?: string[];
  /**
   * Mobil tabbar'da görünsün mü (en fazla 4 sekme + Menü).
   *
   * Değer aynı zamanda SIRADIR: tabbar düzeni menü gruplamasından bağımsız.
   * Menüde Halk Günü, Talepler'in altında duruyor (ikisi de vatandaştan gelen
   * iş) ama alt çubukta gün içinde en sık açılan ekran önce gelmeli.
   */
  tabbar?: number;
};

export type NavigationGroup = { baslik: string; ogeler: NavigationItem[] };

/**
 * design.md §6 rota tablosu + §5.1 sidebar grupları.
 *
 * `politika` alanı ÖNEMLİ: sunucudaki `Ajanda` politikası yalnızca
 * Admin/Sekreter/Yonetici/Baskan'a açık. Menüyü role göre süzmezsek
 * `Kullanici`, `Basin`, `Medya` rolleri takvime tıklayıp 403 duvarına çarpar.
 * Yetkinin kaynağı yine sunucudur; bu yalnızca arayüzü şekillendirir.
 */
export const NAVIGATION: NavigationGroup[] = [
  {
    // GÜN İÇİNDE EN ÇOK AÇILAN ÜÇLÜ ÖNCE: makamın günü ajandayla başlıyor,
    // takvim onun başka bir görünümü. Ajanda bir dönem "Program" başlığı
    // altında, taleplerin ve dosya gönderiminin ALTINDA duruyordu.
    baslik: 'Genel',
    ogeler: [
      { yol: '/', etiket: 'Ana Sayfa', ikon: LayoutDashboard, tabbar: 1 },
      {
        yol: '/ajanda', etiket: 'Ajanda', ikon: CalendarDays,
        // Basın kullanıcısı da görür; listeyi sunucu daraltır.
        izin: [PERMISSION.ajandaGoruntule, PERMISSION.ajandaBasinGoruntule], politika: 'Ajanda',
        altYollar: ['/ajanda/'], tabbar: 2,
      },
      {
        yol: '/takvim', etiket: 'Takvim', ikon: CalendarDays,
        // Basın kullanıcısı da görür; listeyi sunucu daraltır.
        izin: [PERMISSION.ajandaGoruntule, PERMISSION.ajandaBasinGoruntule], politika: 'Ajanda', tabbar: 5,
      },
      {
        yol: '/talepler', etiket: 'Talepler', ikon: ClipboardList,
        izin: PERMISSION.talepGoruntule, politika: 'Ajanda',
        altYollar: ['/talepler/'], tabbar: 3,
      },
      // Yetki İSTEMEZ: gönderme yetkisi olmayan da kendine gelen dosyayı
      // görmeli; menüyü gizlemek gelen dosyayı ulaşılmaz kılardı.
      //
      // Ama gönderme yetkisi de OLMAYAN ve kendisine hiç dosya GELMEMİŞ
      // kullanıcı için menü boş bir ekrandan ibaret; o durumda gizlenir.
      {
        yol: '/gonderim', etiket: 'Dosya Gönderimi', ikon: Paperclip,
        izin: PERMISSION.gonderimGoruntule,
        altYollar: ['/gonderim/'], kosul: 'gelenDosya',
      },
    ],
  },
  {
    // HALK GÜNÜ KENDİ GRUBU: iki ekranı var ve ikisi ardışık iki iş —
    // vatandaş havuza yazılır, sonra bir güne atanır. Genel'in içinde
    // dururken hangisinin hangisi olduğu menüden anlaşılmıyordu.
    baslik: 'Halk Günü',
    ogeler: [
      {
        // VATANDAŞ HAVUZU ÖNCE: akış havuzla BAŞLIYOR — vatandaş makama gelir,
        // kaydı açılır ve bekler; hangi güne alınacağı sonra belli olur.
        yol: '/halk-gunu/basvurular', etiket: 'Vatandaş Havuzu',
        ikon: UserPlus, izin: PERMISSION.halkgunuGoruntule, politika: 'Ajanda',
      },
      {
        yol: '/halk-gunu', etiket: 'Halk Günleri', ikon: Users,
        izin: PERMISSION.halkgunuGoruntule, politika: 'Ajanda',
        altYollar: ['/halk-gunu/'], haric: ['/halk-gunu/basvurular'],
        tabbar: 4,
      },
    ],
  },
  {
    // İŞ TAKİP KENDİ GRUBU: makamın işi (ajanda, talep, halk günü) ile
    // birimlerin işi ayrı şeyler. Aynı grupta dursalardı park bahçelerin
    // çim biçimi ile başkanın protokol randevusu yan yana okunurdu.
    baslik: 'İş Takip',
    ogeler: [
      {
        yol: '/gorevler', etiket: 'Görevler', ikon: ClipboardCheck,
        izin: PERMISSION.gorevGoruntule,
        altYollar: ['/gorevler/'], haric: ['/gorevler/tipler'],
      },
      {
        yol: '/ekipler', etiket: 'Ekipler', ikon: Users,
        izin: [PERMISSION.ekipYonet, PERMISSION.gorevGoruntule],
      },
      {
        yol: '/gorevler/tipler', etiket: 'Görev Tipleri', ikon: ListChecks,
        izin: PERMISSION.gorevTipYonet,
      },
    ],
  },
  {
    // Özgeçmiş havuzu kendi başına bir modül: iş arayanlar burada birikiyor
    // ve soru tek — "elimizde şu işi yapacak biri var mı?".
    baslik: 'Özgeçmişler',
    ogeler: [
      {
        yol: '/ozgecmisler', etiket: 'Özgeçmiş Havuzu', ikon: FileUser,
        izin: PERMISSION.ozgecmisGoruntule, politika: 'Ajanda',
      },
    ],
  },
  {
    baslik: 'Program',
    ogeler: [
      {
        yol: '/protokol', etiket: 'Protokol', ikon: Landmark,
        izin: PERMISSION.protokolGoruntule, politika: 'Ajanda',
      },
      {
        yol: '/davetler', etiket: 'Davetler', ikon: MailCheck,
        izin: PERMISSION.davetGoruntule, politika: 'Ajanda',
        altYollar: ['/davetler/'],
      },
    ],
  },
  {
    baslik: 'Yönetim',
    ogeler: [
      // İstatistikler birim geneli sayıları gösteriyor (kim kaç etkinlik
      // açtı, kaçı iptal oldu); bu bir yönetim görünümü, günlük kullanım
      // ekranı değil. Yalnızca Admin.
      {
        yol: '/istatistikler', etiket: 'İstatistikler', ikon: ChartColumn,
        izin: PERMISSION.istatistikGoruntule,
        // `rol` YALNIZCA geri düşüş: sunucu izin listesi göndermezse
        // (v1 yüzeyi / eski sürüm) devreye girer. İzin listesi geldiğinde
        // hiç bakılmaz — canlı karar her zaman izne dayanır. Kaldırılırsa o
        // yolda bu menü HERKESE açılırdı.
        rol: 'Admin',
      },
      {
        yol: '/cicek', etiket: 'Çiçek Gönderi', ikon: Flower2,
        izin: PERMISSION.cicekGoruntule, politika: 'Cicek',
      },
      {
        yol: '/yonetim', etiket: 'Yönetim', ikon: Users,
        izin: PERMISSION.yonetimKullanici,
        // `rol` YALNIZCA geri düşüş: sunucu izin listesi göndermezse
        // (v1 yüzeyi / eski sürüm) devreye girer. İzin listesi geldiğinde
        // hiç bakılmaz — canlı karar her zaman izne dayanır. Kaldırılırsa o
        // yolda bu menü HERKESE açılırdı.
        rol: 'Admin',
      },
      {
        yol: '/tanimlar', etiket: 'Tanımlar', ikon: SlidersHorizontal,
        izin: PERMISSION.tanimYonet,
        // `rol` YALNIZCA geri düşüş: sunucu izin listesi göndermezse
        // (v1 yüzeyi / eski sürüm) devreye girer. İzin listesi geldiğinde
        // hiç bakılmaz — canlı karar her zaman izne dayanır. Kaldırılırsa o
        // yolda bu menü HERKESE açılırdı.
        rol: 'Admin',
      },
      // Sistem hataları YALNIZCA `Sistem` rolüne açık. `rolVar` `Sistem`i her
      // yere sokuyor ama tersi geçerli değil: Admin buraya giremez.
      {
        yol: '/hatalar', etiket: 'Sistem Hataları', ikon: Bug,
        izin: PERMISSION.sistemHata, rol: 'Sistem', altYollar: ['/hatalar/'],
      },
      {
        yol: '/kurum', etiket: 'Kurum Bilgileri', ikon: Building2,
        izin: PERMISSION.sistemKurum,
        // `rol` YALNIZCA geri düşüş: sunucu izin listesi göndermezse devreye
        // girer. İzin listesi geldiğinde hiç bakılmaz.
        rol: 'Admin',
      },
      { yol: '/bildirimler', etiket: 'Bildirimler', ikon: Bell },
      { yol: '/ayarlar', etiket: 'Ayarlar', ikon: Settings },
    ],
  },
];

/** Verilen yol için hangi gezinme öğesi aktif sayılmalı? */
/**
 * MENÜDE OLMAYAN ekranların başlıkları.
 *
 * <p>
 * Üst çubuktaki başlık menüden okunuyor; menüde karşılığı olmayan bir ekran
 * açıldığında "Randevu Takip Sistemi" yazıyor ve kullanıcı nerede olduğunu
 * göremiyordu. Yardım Merkezi tam olarak böyle bir ekran: menünün öğesi
 * değil, menünün <b>altındaki</b> bir satırdan açılıyor.
 * </p>
 *
 * <p>
 * Detay sayfaları buraya GİRMEZ — onlar <c>altYollar</c> ile üst öğenin
 * başlığını devralıyor ("Etkinlik" değil "Ajanda" yazması bilinçli: kullanıcı
 * hangi bölümde olduğunu görüyor).
 * </p>
 */
export const EXTRA_TITLES: Record<string, string> = {
  '/yardim': 'Yardım Merkezi',
  '/bilesenler': 'Bileşen Kütüphanesi',
};

/** Menüde ya da ek başlıklarda tanımlı sayfa adı. */
export function pageTitle(gruplar: NavigationGroup[], yol: string): string | null {
  const oge = gruplar.flatMap((g) => g.ogeler).find((o) => isActive(o, yol));
  if (oge) return oge.etiket;
  return EXTRA_TITLES[yol.replace(/\/+$/, '') || '/'] ?? null;
}

export function isActive(oge: NavigationItem, yol: string): boolean {
  if (oge.yol === '/') return yol === '/';
  if (yol === oge.yol) return true;
  // Kendi menü öğesi olan alt sayfa, üst öğeyi AKTİF ETMEZ.
  if ((oge.haric ?? []).some((h) => yol === h || yol.startsWith(`${h}/`))) {
    return false;
  }
  return (oge.altYollar ?? []).some((a) => yol.startsWith(a));
}
