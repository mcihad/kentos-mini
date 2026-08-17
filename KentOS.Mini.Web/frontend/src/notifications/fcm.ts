import { initializeApp, type FirebaseApp } from 'firebase/app';
import { deleteToken, getMessaging, getToken, onMessage, type Messaging } from 'firebase/messaging';
import { api } from '../data/client';
import { loadInstitution, type NotificationConfig } from '../institution/institution';

/*
 * FIREBASE YAPILANDIRMASI KODA YAZILMAZ.
 *
 * Değerler gizli değil (tarayıcıya nasıl olsa iniyorlar) ama KURUMA ÖZEL.
 * Derlemeye gömülselerdi her belediye için ayrı bir ön yüz derlemesi
 * gerekirdi. Kaynak: `GET /api/v2/institution` → `bildirim` alanı.
 *
 * Sunucu bu alanı yalnızca yapılandırma TAMSA döndürür; eksikse `null` gelir
 * ve bildirim kurulumu hiç denenmez — yarım yapılandırmayla `initializeApp`
 * çağırmak, kullanıcıya anlamsız bir Firebase hatası gösteriyordu.
 */
let bildirimAyari: NotificationConfig | null = null;

/** Son kaydedilen jetonu hatırlar — çıkışta sunucudan doğru jetonu sildirmek için. */
const JETON_ANAHTARI = 'sv-push-jetonu';

let uygulama: FirebaseApp | null = null;
let mesajlasma: Messaging | null = null;

/** Sunucunun `data.fcmData` içinde gönderdiği yönlendirme sözleşmesi. */
export type NotificationPayload = {
  entity: 'Ajanda' | 'Talep' | 'Oneri' | string;
  id: string;
  action: 'OpenDetails' | 'OpenNotes' | 'OpenImages' | 'None' | string;
};

export type NotificationState =
  | 'acik'
  | 'kapali'
  | 'engellendi'
  | 'desteklenmiyor'
  /** iOS: uygulama ana ekrana eklenmeden bildirim çalışmaz. */
  | 'kurulumGerekli'
  | 'bilinmiyor';

function destekleniyorMu(): boolean {
  return (
    typeof window !== 'undefined' &&
    'serviceWorker' in navigator &&
    'Notification' in window &&
    'PushManager' in window
  );
}

/**
 * iOS'ta bildirim yalnızca KURULU uygulamada çalışır.
 *
 * Safari 16.4+ web push destekliyor ama şartı var: uygulama ana ekrana
 * eklenmiş olmalı. Sekmede izin istemek sessizce başarısız oluyor ve
 * kullanıcı "izin verdim ama gelmiyor" diyor.
 */
export function iosNeedsInstall(): boolean {
  const ua = navigator.userAgent;
  const ios = /iPad|iPhone|iPod/.test(ua) ||
    (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1);
  if (!ios) return false;

  const kurulu =
    window.matchMedia('(display-mode: standalone)').matches ||
    (window.navigator as { standalone?: boolean }).standalone === true;

  return !kurulu;
}

/**
 * Firebase'i kurumun yapılandırmasıyla başlatır.
 *
 * Asenkron: yapılandırma sunucudan geliyor. `loadInstitution` tek istek
 * garantisi veriyor, dolayısıyla art arda çağrılar ek maliyet doğurmuyor.
 */
async function baslat(): Promise<Messaging | null> {
  if (!destekleniyorMu()) return null;

  if (!bildirimAyari) {
    const kurum = await loadInstitution();
    if (!kurum.bildirim) return null;
    bildirimAyari = kurum.bildirim;
  }

  uygulama ??= initializeApp({
    apiKey: bildirimAyari.apiKey,
    authDomain: bildirimAyari.authDomain,
    projectId: bildirimAyari.projectId,
    storageBucket: bildirimAyari.storageBucket,
    messagingSenderId: bildirimAyari.messagingSenderId,
    appId: bildirimAyari.appId,
  });
  mesajlasma ??= getMessaging(uygulama);
  return mesajlasma;
}

/**
 * Service worker'ı `/` KAPSAMINDA kaydeder.
 *
 * Kök kapsamda kaydedilirse ESKİ MVC uygulamasının tüm isteklerini de
 * yakalardı; oradaki bir önbellek hatası iki yıldır çalışan arayüzü
 * düşürebilirdi. Kapsam daraltması bunu yapısal olarak engeller.
 */
async function isciKaydet(): Promise<ServiceWorkerRegistration> {
  return navigator.serviceWorker.register('/firebase-messaging-sw.js', {
    scope: '/',
  });
}

/** Bildirimlerin bu tarayıcıdaki durumu. */
export async function bildirimDurumu(): Promise<NotificationState> {
  if (!destekleniyorMu()) return 'desteklenmiyor';
  if (iosNeedsInstall()) return 'kurulumGerekli';
  if (Notification.permission === 'denied') return 'engellendi';
  if (Notification.permission !== 'granted') return 'kapali';
  // İzin var ama jetonu sunucuya hiç göndermemiş olabiliriz.
  return localStorage.getItem(JETON_ANAHTARI) ? 'acik' : 'kapali';
}

/**
 * İzin ister, jetonu alır ve sunucuya kaydeder.
 *
 * Sunucu tarafında bu jeton başka bir kullanıcıdaysa ondan SÖKÜLÜR: web
 * jetonu tarayıcı profiline bağlıdır, kullanıcıya değil. Ortak bilgisayarda
 * A çıkış yapmadan B giriş yaparsa aynı jeton iki kullanıcıda kalır ve
 * A'nın gizli etkinlik bildirimleri B'nin ekranına düşerdi.
 */
export async function webJetonuKaydet(): Promise<string> {
  if (iosNeedsInstall()) {
    throw new Error(
      'iPhone/iPad’de bildirim alabilmek için uygulamayı önce ana ekrana ' +
      'eklemeniz gerekiyor: Safari’de Paylaş → Ana Ekrana Ekle.',
    );
  }

  const m = await baslat();
  if (!m) throw new Error('Bu tarayıcı web bildirimlerini desteklemiyor ya da bildirim yapılandırması eksik.');

  const izin = await Notification.requestPermission();
  if (izin !== 'granted') {
    throw new Error(
      izin === 'denied'
        ? 'Bildirim izni reddedildi. Adres çubuğundaki site izinlerinden açabilirsiniz.'
        : 'Bildirim izni verilmedi.',
    );
  }

  const kayit = await isciKaydet();
  const jeton = await getToken(m, { vapidKey: bildirimAyari!.vapidPublicKey, serviceWorkerRegistration: kayit });
  if (!jeton) throw new Error('Bildirim jetonu alınamadı.');

  await api.post('/bildirim/web-jeton', { jeton });
  localStorage.setItem(JETON_ANAHTARI, jeton);
  return jeton;
}

/**
 * Girişten sonra sessizce jetonu tazeler.
 *
 * İzin YOKSA hiçbir şey yapmaz — açılışta izin istemek, kullanıcının ne
 * istendiğini anlamadan "engelle"ye basmasına yol açar ve o karar kalıcıdır.
 * İzin daha önce verilmişse jeton yenilenir: FCM jetonları süresiz değildir
 * ve tarayıcı verisi temizlenince değişir.
 */
export async function jetonuTazele(): Promise<void> {
  if (!destekleniyorMu() || Notification.permission !== 'granted') return;
  try {
    await webJetonuKaydet();
  } catch {
    // Tazeleme başarısız olursa uygulama çalışmaya devam etmeli.
  }
}

/** Çıkışta jetonu hem sunucudan hem tarayıcıdan siler. */
export async function webJetonuSil(): Promise<void> {
  const jeton = localStorage.getItem(JETON_ANAHTARI);

  try {
    if (jeton) {
      // Eşleşme kontrollü silme: sunucu yalnızca bu jeton kullanıcının
      // üzerindeyse temizler, gecikmiş bir sekme yeni jetonu silemez.
      await api.delete('/bildirim/web-jeton', { jeton });
    }
  } finally {
    localStorage.removeItem(JETON_ANAHTARI);
    const m = await baslat();
    if (m) await deleteToken(m).catch(() => undefined);
  }
}

/**
 * Uygulama açıkken gelen bildirimleri dinler.
 *
 * <b>Senkron döner</b> — çağıranlar bunu `useEffect` temizleyicisi olarak
 * kullanıyor ve React oradan söz (promise) kabul etmiyor. Firebase kurulumu
 * artık asenkron (yapılandırma sunucudan geliyor), bu yüzden abonelik
 * hazır olunca kuruluyor; bileşen o arada sökülürse `iptal` bayrağı
 * aboneliğin hiç kurulmamasını sağlıyor.
 */
export function onForegroundMessage(
  geriCagir: (baslik: string, govde: string, veri?: NotificationPayload) => void,
): () => void {
  let birak: (() => void) | null = null;
  let iptal = false;

  void baslat().then((m) => {
    if (iptal || !m) return;

    birak = onMessage(m, (mesaj) => {
      const ham = mesaj.data?.fcmData;
      let veri: NotificationPayload | undefined;
      if (ham) {
        try {
          veri = JSON.parse(ham) as NotificationPayload;
        } catch {
          // Biçim bozuksa yönlendirme yapılmaz; bildirim yine gösterilir.
        }
      }
      geriCagir(mesaj.notification?.title ?? 'Bildirim', mesaj.notification?.body ?? '', veri);
    });
  });

  return () => {
    iptal = true;
    birak?.();
  };
}

/**
 * `fcmData` sözleşmesini uygulama içi yola çevirir.
 *
 * Mobildeki `routeFromTokenData` ile AYNI davranış: aynı bildirim iki
 * platformda farklı yere götürürse kullanıcı hangisine güveneceğini bilemez.
 */
export function notificationPath(veri?: NotificationPayload): string | null {
  if (!veri || veri.action === 'None') return null;
  const id = veri.id;
  switch (veri.entity?.toLowerCase()) {
    case 'ajanda':
      return `/ajanda/${id}`;
    case 'talep':
      return `/talepler/${id}`;
    case 'oneri':
      return `/oneriler/${id}`;
    case 'dosya':
      return `/gonderim/${id}`;
    default:
      return null;
  }
}
