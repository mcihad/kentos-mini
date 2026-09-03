import {
  CalendarDays, ClipboardList, FileText, FileUser, Flower2, Gauge, Landmark,
  ServerCog, Users, type LucideIcon,
} from 'lucide-react';
import { PERMISSION } from '../../components/permissions';

/**
 * İSTATİSTİK MERKEZİ KATALOĞU — kartların TEK kaynağı.
 *
 * <p>
 * Merkez ızgarası, rota tablosu ve her konunun uç adresi buradan türüyor.
 * Üçü ayrı yerlerde yazılsaydı yeni bir konu eklemek üç dosyaya dokunmak
 * olurdu ve biri unutulduğunda belirti sessiz olurdu: kart görünür, tıklanır,
 * boş sayfa açılır.
 * </p>
 */
export type StatTopic = {
  /**
   * Rota parçası VE uç adı.
   *
   * `/istatistikler/form` → `GET /api/v2/istatistik/form`. İkisinin aynı
   * olması tesadüf değil, sözleşme: ayrışırlarsa kart doğru sayfayı açar
   * ama sayfa 404 alır.
   */
  konu: string;
  baslik: string;
  aciklama: string;
  ikon: LucideIcon;
  /** Kartı görmek için gereken izin. Yoksa kart hiç çizilmez. */
  izin: string;
  /**
   * KENDİ ekranı olan konular.
   *
   * Etkinlik ve talep panoları genel şekle taşınmadı: ikisi çok daha zengin
   * ve çalışan iki ekranı yeniden yazmanın karşılığı yoktu.
   */
  ozel?: 'etkinlik' | 'talep';
  /**
   * Merkez dışına çıkan kart — hedef rota.
   *
   * Gecikme panosu zaten `/is-panosu` altında yaşıyor ve İş Takip menüsünden
   * de açılıyor. Kopyasını merkeze taşımak yerine merkez oraya gönderiyor:
   * iki ayrı yerde yaşayan bir pano zamanla ikiye ayrılırdı.
   */
  disRota?: string;
};

export type StatGroup = { baslik: string; konular: StatTopic[] };

/**
 * Gruplar MENÜDEKİ gruplamayı izler.
 *
 * Kullanıcı "Halk Günü"nü menüde nerede arıyorsa merkezde de orada bulmalı;
 * ayrı bir sınıflandırma icat etmek, aynı kurumu iki farklı haritayla
 * gezdirmek olurdu.
 */
export const STAT_GROUPS: StatGroup[] = [
  {
    baslik: 'Makam',
    konular: [
      {
        konu: 'etkinlik', ozel: 'etkinlik',
        baslik: 'Etkinlikler',
        aciklama: 'Makamın günü nasıl geçiyor: tip, durum, birim ve tamamlanma seyri.',
        ikon: CalendarDays,
        izin: PERMISSION.istatistikGoruntule,
      },
      {
        konu: 'talep', ozel: 'talep',
        baslik: 'Talepler',
        aciklama: 'Vatandaş neyi, nereden ve kim aracılığıyla istiyor.',
        ikon: ClipboardList,
        izin: PERMISSION.istatistikGoruntule,
      },
    ],
  },
  {
    baslik: 'İş Takip',
    konular: [
      {
        konu: 'is-panosu', disRota: '/is-panosu',
        baslik: 'Gecikme Panosu',
        aciklama: 'Açık, geciken ve onay bekleyen işler; birim karnesi.',
        ikon: Gauge,
        izin: PERMISSION.isIstatistik,
      },
    ],
  },
  {
    baslik: 'Vatandaş',
    konular: [
      {
        konu: 'halk-gunu',
        baslik: 'Halk Günü',
        aciklama: 'Görüşme sonuçları, gelmeyen oranı, mahalle dağılımı ve havuz.',
        ikon: Users,
        izin: PERMISSION.halkgunuGoruntule,
      },
      {
        konu: 'form',
        baslik: 'Form ve Anket',
        aciklama: 'Hangi form kaç kişi tarafından dolduruldu, hangisi yanıt almıyor.',
        ikon: FileText,
        izin: PERMISSION.formGoruntule,
      },
    ],
  },
  {
    baslik: 'Program',
    konular: [
      {
        konu: 'protokol',
        baslik: 'Protokol ve Davet',
        aciklama: 'Defterin kategori dağılımı, davet cevapları ve arama takibi.',
        ikon: Landmark,
        izin: PERMISSION.protokolGoruntule,
      },
      {
        konu: 'cicek',
        baslik: 'Çiçek Gönderi',
        aciklama: 'Talimat sayısı, teslim oranı ve çiçekçi kırılımı.',
        ikon: Flower2,
        izin: PERMISSION.cicekGoruntule,
      },
    ],
  },
  {
    baslik: 'Kurum',
    konular: [
      {
        konu: 'ozgecmis',
        baslik: 'Özgeçmiş Havuzu',
        aciklama: 'Meslek dağılımı, talepten gelenler ve paylaşımların okunma oranı.',
        ikon: FileUser,
        izin: PERMISSION.ozgecmisGoruntule,
      },
      {
        konu: 'sistem',
        baslik: 'Sistem Sağlığı',
        aciklama: 'Hata sayısı, en sık patlayan uçlar ve giriş denemeleri.',
        ikon: ServerCog,
        // Hata ekranının kapısıyla aynı: Admin bile göremez.
        izin: PERMISSION.sistemHata,
      },
    ],
  },
];

/** Rota parçasından konuyu bulur; bilinmeyen konu `undefined`. */
export function konuBul(konu: string | undefined): StatTopic | undefined {
  if (!konu) return undefined;
  return STAT_GROUPS.flatMap((g) => g.konular).find((k) => k.konu === konu);
}

/**
 * Genel şekli kullanan konular — merkezdeki tek çiziciye giden yol.
 *
 * `ozel` ve `disRota` taşıyanlar hariç: onların kendi ekranı var.
 */
export const GENEL_KONULAR = STAT_GROUPS
  .flatMap((g) => g.konular)
  .filter((k) => !k.ozel && !k.disRota);
