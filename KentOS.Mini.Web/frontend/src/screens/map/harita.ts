import { setWorkerUrl } from 'maplibre-gl';
import type { StyleSpecification } from 'maplibre-gl';

/*
  ═══════════════════════════════════════════════════════════════════════════
  MAPLIBRE İŞÇİSİ — haritada hiçbir işaretçinin çıkmamasının sebebi buydu.
  ═══════════════════════════════════════════════════════════════════════════

  MapLibre v6 kendi işçi dosyasının adresini ŞÖYLE buluyor:

      new URL('./maplibre-gl-worker.mjs', import.meta.url)

  Yani "kendi modül dosyamın yanındaki kardeş dosya". Bu, paket olduğu gibi
  servis edildiğinde doğru. Ama Vite kütüphaneyi uygulama yığınının İÇİNE
  paketliyor: `import.meta.url` artık `/uygulama/index-XXXX.js` oluyor ve
  türetilen adres `/uygulama/maplibre-gl-worker.mjs`'e dönüşüyor. Böyle bir
  dosya derleme çıktısında YOK.

  ÖLÇÜLDÜ: o adres 404 dönüyor. İşçi hiç ayağa kalkmıyor, dolayısıyla
  MapLibre'nin GeoJSON kaynağı — kümelenmiş ya da değil — SONSUZA KADAR
  "yükleniyor" durumunda kalıyor. Arıza tamamen sessiz:

    · konsolda tek bir hata yok,
    · `map.on('error')` hiçbir şey yayınlamıyor,
    · raster altlık (ana iş parçacığında çizilir) sorunsuz görünüyor,
    · `isSourceLoaded('isler')` sürekli `false`, `sourcedata` olayı hiç
      gelmiyor, `idle` hiç tetiklenmiyor.

  Kullanıcının gördüğü şey tam olarak şuydu: harita açılıyor, sokaklar
  çiziliyor, ama üzerinde tek bir iş görünmüyor.

  ÇÖZÜM: işçiyi Vite'a AÇIKÇA bir işçi girdisi olarak tanıtmak.

  Ek `?worker&url` — yalnızca `?url` DEĞİL. İkisinin farkı ölçüldü: `?url`
  dosyayı olduğu gibi kopyalıyor, ama işçi kendi içinde
  `./maplibre-gl-shared.mjs` kardeşini içe aktarıyor ve o dosya çıktıya hiç
  girmiyor. Sonuç bir adım ileri, aynı yerde: işçi indiriliyor (200), ilk
  `import`unda 404 alıyor ve yine hiç cevap vermiyor.

  `?worker` Vite'a "bunu bir işçi GİRDİSİ olarak derle" diyor; bağımlılıklar
  da içeri katlanıyor ve tek, kendi kendine yeten bir dosya çıkıyor.
  `&url` ise sınıf yerine adresini veriyor — `setWorkerUrl` bir adres
  bekliyor.

  Bu satır kaldırılırsa harita yeniden sessizce boşalır;
  `test/harita-isci.test.ts` bekçilik ediyor.
*/
import maplibreIsciAdresi from 'maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url';

setWorkerUrl(maplibreIsciAdresi);

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


/* ══════════════════════════════════════════════════════════════════════════
   İŞARETÇİ — damla biçimli iğne
   ══════════════════════════════════════════════════════════════════════════ */

/** İğnenin çizim ölçüleri (CSS pikseli). Yükseklik ucun sivrildiği yere kadar. */
export const PIN_GENISLIK = 30;
export const PIN_YUKSEKLIK = 42;

/**
 * Durum rengine göre bir iğne görüntüsü üretir.
 *
 * <p>
 * <b>Neden SDF değil:</b> MapLibre veriye bağlı renk (<code>icon-color</code>)
 * yalnızca SDF görüntülerde veriyor, ama SDF tek kanallı bir mesafe alanı —
 * beyaz kenarlık, içteki delik ve gecikme halkası gibi <b>çok katmanlı</b> bir
 * biçim orada ifade edilemiyor. Durum renkleri sunucudan gelen KAPALI bir küme
 * (on kadar durum); her renk için bir görüntü üretip önbelleğe almak hem daha
 * basit hem de iğnenin görünümünü tamamen serbest bırakıyor.
 * </p>
 *
 * <p>
 * Çizim <code>oran</code> katıyla büyütülüyor: retina ekranda 1× üretilen bir
 * görüntü bulanık çıkıyor. MapLibre <code>pixelRatio</code> ile gerçek boyutu
 * öğreniyor, dolayısıyla iğne her ekranda aynı fiziksel büyüklükte duruyor.
 * </p>
 *
 * @param renk Dolgu rengi — görevin durum rengi.
 * @param gecikti Süresi aşılmışsa kenarlık kırmızıya döner.
 */
export function iğneGoruntusu(
  renk: string,
  gecikti: boolean,
  oran = Math.min(3, Math.max(1, Math.round(devicePixelRatio || 1))),
): { width: number; height: number; data: Uint8ClampedArray; pixelRatio: number } | null {
  const g = PIN_GENISLIK * oran;
  const y = PIN_YUKSEKLIK * oran;

  const tuval = document.createElement('canvas');
  tuval.width = g;
  tuval.height = y;

  const c = tuval.getContext('2d');
  if (!c) return null;

  c.scale(oran, oran);

  const merkezX = PIN_GENISLIK / 2;
  const merkezY = PIN_GENISLIK / 2;
  const yaricap = PIN_GENISLIK / 2 - 3;
  const ucY = PIN_YUKSEKLIK - 2;

  /*
    DAMLA TEK PARÇA ÇİZİLİYOR: baş kısmı bir yay, gövdesi o yayın iki
    ucundan sivri uca inen iki eğri. Daire + üçgen olarak çizmek, kenarlık
    konduğunda birleşme yerinde görünür bir dikiş bırakıyordu.
  */
  const yol = new Path2D();
  const omuz = Math.PI / 2.9;                 // gövdenin yaydan ayrıldığı açı
  yol.arc(merkezX, merkezY, yaricap, Math.PI - omuz, omuz, false);
  yol.quadraticCurveTo(
    merkezX + yaricap * 0.55, ucY - yaricap * 0.9,
    merkezX, ucY,
  );
  yol.quadraticCurveTo(
    merkezX - yaricap * 0.55, ucY - yaricap * 0.9,
    merkezX - yaricap * Math.cos(omuz), merkezY + yaricap * Math.sin(omuz),
  );
  yol.closePath();

  // Zeminden ayrılsın: haritanın üzerinde yüzen bir nesne, gölgesiz durunca
  // karonun bir parçası gibi okunuyor.
  c.shadowColor = 'rgba(12, 20, 34, 0.34)';
  c.shadowBlur = 4;
  c.shadowOffsetY = 1.5;

  c.fillStyle = renk;
  c.fill(yol);

  c.shadowColor = 'transparent';

  // GECİKEN İŞİN kenarlığı KIRMIZI: durum rengi zaten farklı ama "gecikti"
  // bilgisi durumdan bağımsız ve haritada en çok aranan şey.
  c.strokeStyle = gecikti ? '#B3261E' : '#FFFFFF';
  c.lineWidth = gecikti ? 3 : 2.5;
  c.stroke(yol);

  // İçteki delik iğneyi Leaflet'in klasik biçimine bağlıyor ve dolgu rengini
  // ince bir halkaya indirgemeden okunur tutuyor.
  c.beginPath();
  c.arc(merkezX, merkezY, yaricap * 0.38, 0, Math.PI * 2);
  c.fillStyle = '#FFFFFF';
  c.fill();

  return {
    width: g,
    height: y,
    data: c.getImageData(0, 0, g, y).data,
    pixelRatio: oran,
  };
}

/**
 * Küme rozeti — içinde sayı yazan dolu daire.
 *
 * <p>
 * Sayı <b>görüntüye çiziliyor</b>, metin katmanıyla değil. Sebep ölçüldü:
 * MapLibre'de metin katmanı <code>glyphs</code> (yazı tipi sunucusu) istiyor;
 * bu harita anahtarsız ve sağlayıcısız kurulmuş, stilde <code>glyphs</code>
 * yok. Dolayısıyla küme sayısını yazan katman hiçbir zaman çizilemezdi.
 * Rozeti tuvale çizmek, dışarıya yeni bir bağımlılık eklemeden aynı sonucu
 * veriyor.
 * </p>
 */
export function kumeGoruntusu(
  sayi: number,
  oran = Math.min(3, Math.max(1, Math.round(devicePixelRatio || 1))),
): { width: number; height: number; data: Uint8ClampedArray; pixelRatio: number } | null {
  const etiket = sayi > 999 ? '999+' : String(sayi);

  // Daire sayıyla büyüyor; sabit yarıçap 3 ile 300'ü aynı gösterirdi.
  const cap = sayi < 10 ? 34 : sayi < 100 ? 40 : 48;
  const k = cap * oran;

  const tuval = document.createElement('canvas');
  tuval.width = k;
  tuval.height = k;

  const c = tuval.getContext('2d');
  if (!c) return null;

  c.scale(oran, oran);

  c.beginPath();
  c.arc(cap / 2, cap / 2, cap / 2 - 3, 0, Math.PI * 2);
  c.shadowColor = 'rgba(12, 20, 34, 0.34)';
  c.shadowBlur = 4;
  c.shadowOffsetY = 1.5;
  c.fillStyle = '#002E6D';
  c.fill();
  c.shadowColor = 'transparent';
  c.strokeStyle = '#FFFFFF';
  c.lineWidth = 2.5;
  c.stroke();

  c.fillStyle = '#FFFFFF';
  c.font = `600 ${cap * 0.4}px "IBM Plex Sans", system-ui, sans-serif`;
  c.textAlign = 'center';
  c.textBaseline = 'middle';
  c.fillText(etiket, cap / 2, cap / 2 + 0.5);

  return {
    width: k,
    height: k,
    data: c.getImageData(0, 0, k, k).data,
    pixelRatio: oran,
  };
}
