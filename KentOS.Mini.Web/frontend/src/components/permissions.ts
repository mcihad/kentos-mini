/**
 * İzin adları — sunucudaki `Izinler` sınıfının istemci karşılığı.
 *
 * <p>
 * Elle metin yazmak yerine sabit kullanılır: `iznimVar('ajanda.sıl')` gibi bir
 * yazım hatası sessizce `false` döner ve düğme <b>hiç görünmez</b>; kimse de
 * fark etmez. Sabit kullanınca derleme kırılır.
 * </p>
 *
 * <p>
 * <b>Bu liste yetkilendirme DEĞİLDİR.</b> Yalnızca arayüzü şekillendirir;
 * her uç kendi iznini sunucuda ayrıca denetler.
 * </p>
 */
export const PERMISSION = {
  ajandaGoruntule: 'ajanda.goruntule',
  /**
   * DARALTAN izin: ajandayı açar ama yalnızca "basın katılacak" etkinlikleri
   * gösterir. Süzme SUNUCUDA; arayüz bu izne yalnızca menüyü açmak ve
   * ekleme/düzenleme düğmelerini gizli tutmak için bakar.
   */
  ajandaBasinGoruntule: 'ajanda.basinGoruntule',
  ajandaEkle: 'ajanda.ekle',
  ajandaDuzenle: 'ajanda.duzenle',
  ajandaSil: 'ajanda.sil',
  ajandaKatilimciEkle: 'ajanda.katilimciEkle',
  ajandaGizliEtkinlik: 'ajanda.gizliEtkinlik',
  ajandaHavale: 'ajanda.havale',
  ajandaStatuDegistir: 'ajanda.statuDegistir',
  ajandaNotEkle: 'ajanda.notEkle',
  ajandaFotografEkle: 'ajanda.fotografEkle',
  ajandaSmsGonder: 'ajanda.smsGonder',
  ajandaCiktiAl: 'ajanda.ciktiAl',

  talepGoruntule: 'talep.goruntule',
  talepEkle: 'talep.ekle',
  talepDuzenle: 'talep.duzenle',
  talepSil: 'talep.sil',
  talepDurumDegistir: 'talep.durumDegistir',
  talepHavale: 'talep.havale',
  talepAjandayaEkle: 'talep.ajandayaEkle',
  talepArsivle: 'talep.arsivle',
  talepNotEkle: 'talep.notEkle',
  talepDosyaYukle: 'talep.dosyaYukle',
  talepCiktiAl: 'talep.ciktiAl',

  protokolGoruntule: 'protokol.goruntule',
  protokolYonet: 'protokol.yonet',

  davetGoruntule: 'davet.goruntule',
  davetYonet: 'davet.yonet',
  davetCiktiAl: 'davet.ciktiAl',

  // Halk günü — alan adı bitişik ve küçük harf (sunucudaki senkron testinin
  // deseni gereği).
  halkgunuGoruntule: 'halkgunu.goruntule',
  halkgunuYonet: 'halkgunu.yonet',
  halkgunuBasvuru: 'halkgunu.basvuru',
  halkgunuAtama: 'halkgunu.atama',
  halkgunuGorusme: 'halkgunu.gorusme',
  halkgunuSms: 'halkgunu.sms',
  halkgunuCiktiAl: 'halkgunu.ciktiAl',
  halkgunuTalepOlustur: 'halkgunu.talepOlustur',

  ozgecmisGoruntule: 'ozgecmis.goruntule',
  ozgecmisEkle: 'ozgecmis.ekle',
  ozgecmisDuzenle: 'ozgecmis.duzenle',
  ozgecmisSil: 'ozgecmis.sil',
  ozgecmisPaylas: 'ozgecmis.paylas',

  cicekGoruntule: 'cicek.goruntule',
  cicekYonet: 'cicek.yonet',

  gonderimGoruntule: 'gonderim.goruntule',
  gonderimGonder: 'gonderim.gonder',

  oneriGoruntule: 'oneri.goruntule',
  oneriYanitla: 'oneri.yanitla',

  istatistikGoruntule: 'istatistik.goruntule',

  yonetimKullanici: 'yonetim.kullanici',
  yonetimBirim: 'yonetim.birim',
  yonetimRol: 'yonetim.rol',
  yonetimOturumKaydi: 'yonetim.oturumKaydi',
  tanimYonet: 'tanim.yonet',

  gorevBirimKapsam: 'gorev.birimKapsam',
  gorevGoruntule: 'gorev.goruntule',
  gorevEkle: 'gorev.ekle',
  gorevDuzenle: 'gorev.duzenle',
  gorevSil: 'gorev.sil',
  gorevAtama: 'gorev.atama',
  gorevAsama: 'gorev.asama',
  gorevOnayla: 'gorev.onayla',
  gorevTipYonet: 'gorev.tipYonet',
  ekipYonet: 'ekip.yonet',
  projeGoruntule: 'proje.goruntule',
  projeYonet: 'proje.yonet',
  projeUyeYonet: 'proje.uyeYonet',
  bildirimKarsila: 'bildirim.karsila',
  bildirimYonlendir: 'bildirim.yonlendir',
  sahaTespit: 'saha.tespit',

  sistemHata: 'sistem.hata',
  sistemKurum: 'sistem.kurum',
} as const;

export type PermissionName = (typeof PERMISSION)[keyof typeof PERMISSION];
