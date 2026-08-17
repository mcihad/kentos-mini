import type { StyleSpecification } from 'maplibre-gl';

/**
 * HARİTA TEMELİ.
 *
 * <p>
 * <b>Anahtar gerektiren bir sağlayıcı YOK.</b> Mapbox/Google gibi bir servis
 * hem hesap ve fatura hem de her kurulumda ayarlanacak bir anahtar demekti;
 * uygulama başka belediyelere de verilecek ve "haritayı açmak için önce
 * hesap açın" adımı kurulumun en kırılgan yeri olurdu.
 * </p>
 *
 * <p>
 * Bunun yerine OpenStreetMap karo sunucusu doğrudan kullanılıyor. Karşılığı
 * şu: OSM'nin kullanım politikası <b>ağır trafiğe uygun değil</b>. Belediye
 * içi kullanım bu ölçekte kalıyor; yoğun bir kurulumda kendi karo sunucusunu
 * (ya da anahtarlı bir sağlayıcıyı) buradan tek noktadan değiştirmek yeterli.
 * </p>
 */
export const HARITA_TEMASI: StyleSpecification = {
  version: 8,
  sources: {
    osm: {
      type: 'raster',
      tiles: ['https://tile.openstreetmap.org/{z}/{x}/{y}.png'],
      tileSize: 256,
      maxzoom: 19,
      attribution: '© OpenStreetMap katkıcıları',
    },
  },
  layers: [
    {
      id: 'osm',
      type: 'raster',
      source: 'osm',
      // Karo görüntüsü doygunluğu DÜŞÜRÜLÜYOR: üstüne basılan durum
      // renkleri (kırmızı gecikme, yeşil tamamlandı) tam doygun bir harita
      // üzerinde okunmuyordu.
      paint: { 'raster-saturation': -0.35, 'raster-contrast': -0.05 },
    },
  ],
};

/**
 * Haritanın açılış merkezi.
 *
 * <p>
 * Kuruma özel bir koordinat KODA YAZILMIYOR — uygulama başka belediyelere de
 * verilecek. İlk nokta varsa oraya, yoksa Türkiye'nin tamamı gösteriliyor ve
 * kullanıcı zaten kendi konumuna gidiyor.
 * </p>
 */
export const TURKIYE_MERKEZ: [number, number] = [35.0, 39.0];
export const TURKIYE_YAKINLIK = 5.2;

/** Tek nokta için makul yakınlık — sokak seviyesinde. */
export const NOKTA_YAKINLIK = 16;

/**
 * Tarayıcıdan konum ister.
 *
 * <p>
 * Reddedilme bir hata değil: kullanıcı izin vermeyebilir ve bu tamamen
 * meşru. Çağıran <code>null</code> alıyor ve haritayı elle işaretlemeye
 * devam ediyor.
 * </p>
 */
export function konumIste(): Promise<{ enlem: number; boylam: number } | null> {
  if (typeof navigator === 'undefined' || !navigator.geolocation) {
    return Promise.resolve(null);
  }

  return new Promise((coz) => {
    navigator.geolocation.getCurrentPosition(
      (k) => coz({ enlem: k.coords.latitude, boylam: k.coords.longitude }),
      () => coz(null),
      // Yüksek doğruluk saha için önemli: sokak tarifi yapılacak.
      { enableHighAccuracy: true, timeout: 8000, maximumAge: 30_000 },
    );
  });
}

/**
 * Tarayıcı MapLibre'yi çalıştırabilir mi?
 *
 * <p>
 * MapLibre <b>WebGL2 zorunlu</b> tutuyor ve desteklenmediğinde kurulum
 * anında istisna fırlatıyor. React'te bu, haritayı içeren EKRANIN TAMAMINI
 * düşürüyor: kullanıcı harita yerine beyaz bir sayfa görüyor.
 * </p>
 *
 * <p>
 * Ölçümde tam olarak bu çıktı — GPU'suz başsız tarayıcıda saha tespiti
 * ekranı hiç açılmadı. Eski bir cihazda ya da GPU'su devre dışı bırakılmış
 * bir kurumsal tarayıcıda aynı şey olurdu ve kullanıcı formu hiç göremezdi.
 * </p>
 *
 * <p>
 * Kontrol BİR KEZ yapılıp saklanıyor: her çizimde bağlam oluşturmak pahalı.
 * </p>
 */
let _webgl2: boolean | null = null;

export function webgl2Var(): boolean {
  if (_webgl2 !== null) return _webgl2;

  try {
    const tuval = document.createElement('canvas');
    _webgl2 = !!tuval.getContext('webgl2');
  } catch {
    _webgl2 = false;
  }

  return _webgl2;
}
