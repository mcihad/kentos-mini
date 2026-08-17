import { useEffect, useState } from 'react';

/**
 * KURUM BİLGİSİ — çalışma anında sunucudan okunur.
 *
 * Uygulama başka belediyelere verilecek ve açık kaynak olacak. Kurum adı,
 * amblem ve kurumsal renkler ön yüz derlemesine GÖMÜLMEZ; gömülselerdi her
 * kurum için ayrı bir SPA derlemesi gerekirdi ve "kurumu değiştirmek tek bir
 * ayar" hedefi ölürdü.
 *
 * Kaynak: `GET /api/v2/institution` (anonim). Sunucu bunu veritabanındaki
 * tek satırlık `kurum_bilgileri` tablosundan üretir; yetkili kullanıcı
 * arayüzden düzenleyebilir.
 *
 * Bu modül React'ten BAĞIMSIZ: giriş ekranı boyanmadan önce, hatta tema
 * kurulmadan önce okunması gerekiyor.
 */

export type Brand = {
  birincil?: string | null;
  vurgu?: string | null;
  notr?: string | null;
  birincilKoyu?: string | null;
  amblem?: string | null;
  favicon?: string | null;
  uygulamaIkonu?: string | null;
};

/** Web push için tarayıcıya gereken Firebase alanları. */
export type NotificationConfig = {
  apiKey: string;
  authDomain: string;
  projectId: string;
  storageBucket: string;
  messagingSenderId: string;
  appId: string;
  vapidPublicKey: string;
};

export type Institution = {
  ad: string;
  kisaAd: string;
  gorunenAd: string;
  birim?: string | null;
  kunye?: string | null;
  webSitesi?: string | null;
  adres?: string | null;
  telefon?: string | null;
  eposta?: string | null;
  uygulamaAdi: string;
  uygulamaKisaAdi: string;
  uygulamaAciklamasi?: string | null;
  marka: Brand;
  bildirim?: NotificationConfig | null;
};

/**
 * Sunucuya ulaşılamadığında kullanılan asgari kimlik.
 *
 * KURUM ADI İÇERMEZ — bilinmeyen bir kurumun adını uydurmak, yanlış bir kurum
 * adı göstermekten farksız. Arayüz bu durumda yalnızca uygulama adını gösterir.
 */
const FALLBACK: Institution = {
  ad: '',
  kisaAd: '',
  gorunenAd: '',
  uygulamaAdi: 'WorkCollab',
  uygulamaKisaAdi: 'WorkCollab',
  marka: {},
};

const STORAGE_KEY = 'sv-kurum';

let cached: Institution | null = null;
let inflight: Promise<Institution> | null = null;

/**
 * Son başarılı yanıtı okur.
 *
 * Neden saklıyoruz: uygulama çevrimdışı açıldığında ya da sunucu yanıt
 * vermediğinde amblem ve renkler kaybolmasın. Marka bilgisinin bir açılışta
 * var, diğerinde yok olması "bozuldu" izlenimi veriyor.
 */
function readCache(): Institution | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Institution) : null;
  } catch {
    return null;
  }
}

function writeCache(value: Institution) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
  } catch {
    // Kota dolu ya da gizli sekme — marka bilgisi uğruna açılışı bozmayalım.
  }
}

/** Bellekte hazır olan kurum bilgisi; henüz yüklenmediyse önbellek ya da varsayılan. */
export function currentInstitution(): Institution {
  return cached ?? readCache() ?? FALLBACK;
}

/**
 * Kurum bilgisini yükler.
 *
 * Aynı anda birden çok çağrı gelirse tek istek yapılır — açılışta hem tema
 * hem başlık hem bildirim modülü bunu istiyor.
 */
export function loadInstitution(): Promise<Institution> {
  if (cached) return Promise.resolve(cached);
  if (inflight) return inflight;

  inflight = fetch('/api/v2/institution', { headers: { Accept: 'application/json' } })
    .then((r) => (r.ok ? (r.json() as Promise<Institution>) : Promise.reject(new Error(String(r.status)))))
    .then((veri) => {
      cached = veri;
      writeCache(veri);
      return veri;
    })
    .catch(() => currentInstitution())
    .finally(() => {
      inflight = null;
    });

  return inflight;
}

/**
 * Önbelleği atlayıp sunucudan yeniden okur.
 *
 * Kurum bilgisi düzenlendikten sonra gerekiyor: `loadInstitution` bir kez
 * okuduğunu bellekte tutuyor ve kaydeden yönetici eski değeri görürdü.
 */
export function refreshInstitution(): Promise<Institution> {
  cached = null;
  inflight = null;
  return loadInstitution();
}

/**
 * Kurum bilgisini bileşenlerde kullanmak için.
 *
 * İlk render'da önbellekteki (ya da varsayılan) değeri döndürür, ardından
 * sunucudan gelen değerle tazeler. Askıya alma (suspense) YOK: giriş ekranı
 * kurum bilgisi için beklememeli — amblem bir kare geç gelirse sorun değil,
 * boş ekran sorundur.
 */
export function useInstitution(): Institution {
  const [kurum, setKurum] = useState<Institution>(currentInstitution);

  useEffect(() => {
    let iptalEdildi = false;
    void loadInstitution().then((yeni) => {
      if (!iptalEdildi) setKurum(yeni);
    });
    return () => {
      iptalEdildi = true;
    };
  }, []);

  return kurum;
}

/** Türkçe büyük harf — `I/İ` ve `i/ı` ayrımı için kültür şart. */
export function buyukHarf(metin: string): string {
  return metin.toLocaleUpperCase('tr-TR');
}

/**
 * Belge başlığını, açıklamasını ve sekme simgesini kuruma göre günceller.
 *
 * `index.html` içinde bunların hiçbiri kuruma özel YAZILMAZ; hepsi burada
 * çalışma anında konur.
 */
export function applyDocumentIdentity(kurum: Institution) {
  const parcalar = [kurum.uygulamaAdi, kurum.gorunenAd].filter(Boolean);
  if (parcalar.length > 0) document.title = parcalar.join(' · ');

  if (kurum.uygulamaAciklamasi) {
    let etiket = document.querySelector<HTMLMetaElement>('meta[name="description"]');
    if (!etiket) {
      etiket = document.createElement('meta');
      etiket.name = 'description';
      document.head.appendChild(etiket);
    }
    etiket.content = kurum.uygulamaAciklamasi;
  }

  if (kurum.marka.favicon) {
    let simge = document.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (!simge) {
      simge = document.createElement('link');
      simge.rel = 'icon';
      document.head.appendChild(simge);
    }
    simge.href = kurum.marka.favicon;
  }
}
