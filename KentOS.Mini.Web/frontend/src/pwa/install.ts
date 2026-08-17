/**
 * PWA kurulumu ve cihaz yetenekleri.
 *
 * <p>
 * Uygulama telefona kurulabildiğinde <b>bildirimler de gerçekten çalışır</b> —
 * özellikle iOS'ta: Safari 16.4'ten beri web push destekleniyor ama
 * <b>yalnızca ana ekrana eklenmiş</b> uygulamalarda. Tarayıcı sekmesinde
 * izin istemek orada hiçbir zaman sonuç vermez. Bu yüzden kurulum durumu ve
 * platform bilgisi bildirim akışının bir parçası.
 * </p>
 *
 * <h3>Kurulu mu? — tahmin, kesinlik değil</h3>
 *
 * <p>
 * Hiçbir tarayıcı "bu uygulama bu cihazda kurulu mu?" sorusuna doğrudan cevap
 * vermiyor. Elimizde dört sinyal var ve hepsinin kör noktası ayrı:
 * </p>
 *
 * <ol>
 *   <li><b>`display-mode: standalone`</b> — yalnızca KURULU pencerenin
 *       içindeyken doğru. Tarayıcı sekmesinde her zaman false, oysa uygulama
 *       ana ekranda duruyor olabilir.</li>
 *   <li><b>`appinstalled` olayı</b> — kurulum ANINI verir; sonrasını
 *       hatırlamak bize kalıyor (kalıcı işaret).</li>
 *   <li><b>`beforeinstallprompt`</b> — tarayıcı bunu yalnızca uygulama KURULU
 *       DEĞİLKEN gönderiyor. Yani kalıcı işaretimiz "kurulu" derken bu olay
 *       gelirse, uygulama <b>kaldırılmış</b> demektir.</li>
 *   <li><b>`getInstalledRelatedApps()`</b> — sekmenin içinden de doğru cevap
 *       verebiliyor ama her tarayıcıda yok ve manifest'in kendini
 *       `related_applications` altında ilan etmesini şart koşuyor.</li>
 * </ol>
 *
 * <p>
 * <b>Düzeltilen hata buradaydı:</b> kurulum işareti kalıcı yazılıyor ama
 * hiçbir zaman silinmiyordu. Kullanıcı uygulamayı telefonuna kurup sonra
 * kaldırdığında kurulum düğmesi bir daha ASLA görünmüyordu — "kurulu" diyen
 * tek bir `localStorage` satırı yüzünden. Artık üçüncü sinyal işareti
 * temizliyor, dördüncüsü doğruluyor ve kullanıcının elinde her zaman
 * {@link kuruluIsaretiniSil} kaçış kapısı var.
 * </p>
 */

import { useSyncExternalStore } from 'react';

/** Tarayıcının kurulum istemini sakladığımız olay tipi. */
type KurulumOlayi = Event & {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
};

export type Platform = 'ios' | 'android' | 'masaustu';

export type InstallState = {
  /** Elimizdeki sinyallere göre uygulama bu cihazda kurulu. */
  kurulu: boolean;
  /** Tarayıcı bize bir kurulum istemi verdi: tek dokunuşla kurulur. */
  istemVar: boolean;
  /** Kurulum bu cihazda mümkün — ya istem var ya da elle eklenebiliyor. */
  kurulabilir: boolean;
  platform: Platform;
  /** Apple cihazında Safari mi? Ana ekrana ekleme menüsü ona ait. */
  safari: boolean;
  /** Kullanıcı kurulum kartını erteledi; süresi dolunca kendiliğinden döner. */
  ertelendi: boolean;
  /**
   * "Kurulu" bilgisi yalnızca KALICI İŞARETTEN geliyor — yani şu an tarayıcı
   * sekmesindeyiz ve uygulamayı kurduğumuzu hatırlıyoruz. Kaldırılmış
   * olabilir; arayüz bu durumda "kaldırdıysanız yeniden kurun" kapısını
   * gösterir.
   */
  isaretten: boolean;
};

/* ────────────────────────── kalıcı işaretler ────────────────────────── */

const ANAHTAR_KURULU = 'sv-pwa-kurulu';
const ANAHTAR_ERTELE = 'sv-pwa-ertelendi';
/**
 * Eski KALICI gizleme anahtarı.
 *
 * "Bir daha gösterme" demekti ve kurulum başarılı olduğunda da yazılıyordu:
 * kaldırıldıktan sonra kartın dönmemesinin ikinci sebebi. Göç tek yönlü —
 * anahtar siliniyor, yerine süreli erteleme geçiyor.
 */
const ESKI_GIZLE = 'sv-kurulum-gizlendi';

const ERTELEME_SURESI = 14 * 24 * 60 * 60 * 1000;

function oku(anahtar: string): string | null {
  try {
    return localStorage.getItem(anahtar);
  } catch {
    // Safari gizli gezinti `localStorage`ı kotasız açıyor ve yazma atıyor.
    return null;
  }
}

function yaz(anahtar: string, deger: string | null) {
  try {
    if (deger === null) localStorage.removeItem(anahtar);
    else localStorage.setItem(anahtar, deger);
  } catch {
    /* Kalıcılık yoksa oturum boyunca bellekteki durum yeterli. */
  }
}

/* ──────────────────────────── platform ──────────────────────────────── */

export function isIos(): boolean {
  if (typeof navigator === 'undefined') return false;
  const ua = navigator.userAgent;
  // iPadOS 13+ kendini macOS gibi tanıtıyor; dokunma noktası sayısı ayırt eder.
  return /iPad|iPhone|iPod/.test(ua) ||
    (/Macintosh/.test(ua) && navigator.maxTouchPoints > 1);
}

function platformBul(): Platform {
  if (typeof navigator === 'undefined') return 'masaustu';
  if (isIos()) return 'ios';
  if (/Android/i.test(navigator.userAgent)) return 'android';
  return 'masaustu';
}

/**
 * Apple cihazında Safari mi?
 *
 * iOS'ta bütün tarayıcılar WebKit ama "Ana Ekrana Ekle" menüsü tarayıcıya
 * göre başka yerde — Chrome'da `⋯`, Safari'de Paylaş. Ayrımı kullanıcıya
 * doğru adımı göstermek için yapıyoruz. macOS Safari 17+ da "Dock'a Ekle" ile
 * gerçek bir PWA kuruyor, o yüzden aynı dal.
 */
function safariMi(): boolean {
  if (typeof navigator === 'undefined') return false;
  const ua = navigator.userAgent;
  if (!/iPad|iPhone|iPod|Macintosh/.test(ua)) return false;
  return /Safari/.test(ua) && !/CriOS|FxiOS|EdgiOS|OPiOS|Chrome|Chromium|Android/.test(ua);
}

/**
 * Bu tarayıcı uygulamayı hiç kuramıyor mu?
 *
 * <p>
 * Tek net örnek <b>masaüstü Firefox</b>: PWA kurulumu desteği kaldırıldı ve
 * hiçbir menüde karşılığı yok. Android Firefox kurabiliyor, o yüzden ayrım
 * yalnızca masaüstünde. Orada kurulum düğmesi göstermek, kullanıcıyı
 * bulunmayan bir menüyü aramaya göndermek olurdu.
 * </p>
 */
function kurulumDesteklenmiyorMu(): boolean {
  if (typeof navigator === 'undefined') return false;
  const ua = navigator.userAgent;
  return /Firefox/.test(ua) && !/Android/.test(ua);
}

/** Uygulama ŞU AN ana ekrandan (standalone) mı açıldı? */
function bagimsizMi(): boolean {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return false;

  return (
    window.matchMedia('(display-mode: standalone)').matches ||
    window.matchMedia('(display-mode: window-controls-overlay)').matches ||
    window.matchMedia('(display-mode: fullscreen)').matches ||
    // iOS Safari standart API'yi desteklemiyor; kendi bayrağını kullanıyor.
    (window.navigator as { standalone?: boolean }).standalone === true
  );
}

/* ───────────────────────────── durum ────────────────────────────────── */

let bekleyenIstem: KurulumOlayi | null = null;
/** `getInstalledRelatedApps()` bizi bulduysa: kurulu olduğunun KANITI. */
let iliskiliBulundu = false;
/** Bir kez bulunup sonra kaybolduysa: kaldırıldığının kanıtı. */
let iliskiliDahaOnceBulundu = false;

const dinleyiciler = new Set<() => void>();

/**
 * Anlık görüntü ÖNBELLEĞE ALINIR.
 *
 * `useSyncExternalStore` her boyamada `getSnapshot` çağırıyor ve dönen
 * değeri `Object.is` ile karşılaştırıyor. Her çağrıda yeni bir nesne
 * üretilseydi React "değişti" deyip sonsuz döngüye girerdi.
 */
let anlik: InstallState | null = null;

function hesapla(): InstallState {
  const platform = platformBul();
  const safari = safariMi();
  const bagimsiz = bagimsizMi();
  const isaret = oku(ANAHTAR_KURULU) === '1';

  const kurulu = bagimsiz || iliskiliBulundu || isaret;
  const istemVar = bekleyenIstem !== null;

  const erteleDamga = Number(oku(ANAHTAR_ERTELE) ?? 0);
  const ertelendi = erteleDamga > 0 && Date.now() - erteleDamga < ERTELEME_SURESI;

  /*
    KURULABİLİRLİK, İSTEME BAĞLANMAZ.

    Önce `kurulabilir = istemVar` idi ve iki durumda kurulum yolu tamamen
    kayboluyordu: (1) iOS'ta `beforeinstallprompt` hiç gelmiyor, (2) Chrome
    istemi tek kullanımlık — kullanıcı tarayıcının penceresini kapatınca
    düğme de kayboluyor ve sayfa yenilenene kadar geri gelmiyordu. Oysa her
    iki durumda da kurulum PEKÂLÂ mümkün, yalnızca elle. Kapıyı yalnızca
    kurulumu gerçekten desteklemeyen tarayıcıda kapatıyoruz.
  */
  return {
    kurulu,
    istemVar,
    kurulabilir: !kurulu && !kurulumDesteklenmiyorMu(),
    platform,
    safari,
    ertelendi,
    isaretten: kurulu && !bagimsiz && !iliskiliBulundu && isaret,
  };
}

function esitMi(a: InstallState, b: InstallState): boolean {
  return (
    a.kurulu === b.kurulu &&
    a.istemVar === b.istemVar &&
    a.kurulabilir === b.kurulabilir &&
    a.platform === b.platform &&
    a.safari === b.safari &&
    a.ertelendi === b.ertelendi &&
    a.isaretten === b.isaretten
  );
}

/** Durumu yeniden hesaplar; gerçekten değiştiyse aboneleri uyandırır. */
function tazele() {
  const yeni = hesapla();
  if (anlik && esitMi(anlik, yeni)) return;
  anlik = yeni;
  for (const d of dinleyiciler) d();
}

export function installState(): InstallState {
  anlik ??= hesapla();
  return anlik;
}

export function onInstallStateChange(geriCagir: () => void): () => void {
  dinleyiciler.add(geriCagir);
  return () => {
    dinleyiciler.delete(geriCagir);
  };
}

/** Kurulum durumunu React'e bağlar. */
export function useInstall(): InstallState {
  return useSyncExternalStore(onInstallStateChange, installState, installState);
}

/* ─────────────────── ilişkili uygulama yoklaması ────────────────────── */

type IliskiliUygulama = { platform?: string; id?: string; url?: string };

/**
 * `navigator.getInstalledRelatedApps()` ile kurulu olup olmadığımızı sorar.
 *
 * <p>
 * Sekmenin içinden doğru cevap verebilen TEK API bu. Karşılığında manifest'in
 * kendini `related_applications` altında ilan etmesi gerekiyor. Desteklemeyen
 * tarayıcıda ve manifest eşleşmediğinde boş dizi dönüyor — yani <b>boş sonuç
 * "kurulu değil" demek DEĞİL</b>, "bilmiyorum" demek. Bu yüzden yalnızca
 * <b>olumlu</b> sonuç kurulu sayılır; olumsuza dönüş ise ancak daha önce
 * olumlu cevap almışsak kaldırılma sayılır.
 * </p>
 */
async function iliskiliYokla(): Promise<void> {
  const nav = navigator as Navigator & {
    getInstalledRelatedApps?: () => Promise<IliskiliUygulama[]>;
  };
  if (typeof nav.getInstalledRelatedApps !== 'function') return;

  try {
    const liste = await nav.getInstalledRelatedApps();
    const bulundu = liste.some((u) => u.platform === 'webapp');

    if (bulundu) {
      iliskiliBulundu = true;
      iliskiliDahaOnceBulundu = true;
      yaz(ANAHTAR_KURULU, '1');
    } else if (iliskiliDahaOnceBulundu) {
      // Bir kez bulunmuştu, artık yok: kaldırılmış.
      iliskiliBulundu = false;
      kaldirildiginiIsle();
    }
    tazele();
  } catch {
    /* API var ama bu bağlamda çalışmadı; diğer sinyaller yeterli. */
  }
}

/**
 * Uygulamanın kaldırıldığını işler.
 *
 * Ertelemeyi de siler: kullanıcı kartı "sonra" diye kapatmış olabilir ama
 * o karar KURULUYKEN verilmişti. Kaldırdıktan sonra kurulum yeniden gündemde.
 */
function kaldirildiginiIsle() {
  yaz(ANAHTAR_KURULU, null);
  yaz(ANAHTAR_ERTELE, null);
}

/* ──────────────────────────── başlatma ──────────────────────────────── */

let baslatildi = false;

/**
 * Kurulum istemini yakalar ve durumu canlı tutar.
 *
 * Chrome `beforeinstallprompt`'u sayfa yüklenirken tetikler ve
 * `preventDefault()` çağrılmazsa kendi çubuğunu gösterir. İstemi saklayıp
 * <b>kullanıcı hazır olduğunda</b> göstermek, kabul oranını da artırıyor.
 */
export function startInstallListener() {
  if (typeof window === 'undefined' || baslatildi) return;
  baslatildi = true;

  // Göç: kalıcı gizleme anahtarı artık yok.
  if (oku(ESKI_GIZLE)) yaz(ESKI_GIZLE, null);

  window.addEventListener('beforeinstallprompt', (olay) => {
    olay.preventDefault();
    bekleyenIstem = olay as KurulumOlayi;

    /*
      İSTEM GELDİYSE UYGULAMA KURULU DEĞİL.

      Tarayıcı bu olayı yalnızca kurulabilir ve kurulu olmayan durumda
      gönderiyor. Elimizde "kurulu" işareti varken gelmesi, uygulamanın
      kaldırıldığı anlamına gelir — kurulum düğmesinin geri gelmesini
      sağlayan sinyal tam olarak bu.
    */
    if (oku(ANAHTAR_KURULU) === '1') kaldirildiginiIsle();
    iliskiliBulundu = false;

    tazele();
  });

  window.addEventListener('appinstalled', () => {
    bekleyenIstem = null;
    yaz(ANAHTAR_KURULU, '1');
    // Kurulum, ertelemeyi anlamsız kılar.
    yaz(ANAHTAR_ERTELE, null);
    tazele();
  });

  /*
    PENCEREYE DÖNÜNCE YENİDEN BAK.

    Kullanıcı uygulamayı ana ekrandan kaldırıp sekmeye geri dönüyor; sayfa
    yeniden yüklenmiyor, dolayısıyla hiçbir olay tetiklenmiyordu. Görünürlük
    değişimi bu boşluğu kapatan tek kanca.
  */
  const yenidenBak = () => {
    if (document.visibilityState !== 'visible') return;
    void iliskiliYokla();
    tazele();
  };
  document.addEventListener('visibilitychange', yenidenBak);
  window.addEventListener('pageshow', yenidenBak);
  window.addEventListener('focus', yenidenBak);

  // Görünüm kipi değişimi: tarayıcıdan kurulu pencereye geçiş (ya da tersi).
  if (typeof window.matchMedia === 'function') {
    for (const sorgu of ['(display-mode: standalone)', '(display-mode: window-controls-overlay)']) {
      const mq = window.matchMedia(sorgu);
      mq.addEventListener?.('change', tazele);
    }
  }

  // Başka sekmede kurulum/kaldırma olduysa bu sekme de öğrensin.
  window.addEventListener('storage', (olay) => {
    if (olay.key === ANAHTAR_KURULU || olay.key === ANAHTAR_ERTELE) tazele();
  });

  void iliskiliYokla();
  tazele();
}

/* ───────────────────────────── eylemler ─────────────────────────────── */

/** Kurulum istemini gösterir; kullanıcının kararını döndürür. */
export async function promptInstall(): Promise<'kuruldu' | 'vazgecildi' | 'yok'> {
  if (!bekleyenIstem) return 'yok';

  const istem = bekleyenIstem;
  // İstem TEK KULLANIMLIK; tarayıcı yenisini gönderene kadar tekrar
  // çağrılamaz. Önce düşürülüyor ki çift dokunuş ikinci kez `prompt()`
  // çağırıp `InvalidStateError` atmasın.
  bekleyenIstem = null;
  tazele();

  try {
    await istem.prompt();
    const { outcome } = await istem.userChoice;

    if (outcome === 'accepted') {
      // `appinstalled` genelde arkasından geliyor ama her tarayıcıda değil.
      yaz(ANAHTAR_KURULU, '1');
      yaz(ANAHTAR_ERTELE, null);
      tazele();
      return 'kuruldu';
    }
    return 'vazgecildi';
  } catch {
    return 'yok';
  }
}

/** Kurulum kartını süreli olarak erteler (kalıcı gizleme YOK). */
export function snoozeInstall() {
  yaz(ANAHTAR_ERTELE, String(Date.now()));
  tazele();
}

/**
 * "Kurulu" işaretini siler — kullanıcının kaçış kapısı.
 *
 * Tarayıcı kaldırma olayını bize hiçbir zaman bildirmiyor ve
 * `beforeinstallprompt`'u kendi katılım ölçütlerine göre geciktirebiliyor.
 * Kullanıcı "kaldırdım ama düğme gelmedi" durumunda kaldığında elinde
 * çalışan bir kapı olmalı.
 */
export function clearInstalledFlag() {
  kaldirildiginiIsle();
  iliskiliBulundu = false;
  iliskiliDahaOnceBulundu = false;
  tazele();
}

/* ─────────────────────── bildirim ve worker ─────────────────────────── */

/** Uygulama ana ekrandan (standalone) mı açıldı? */
export function isInstalled(): boolean {
  return installState().kurulu;
}

/**
 * Bildirim izni bu cihazda istenebilir mi?
 *
 * <p>
 * iOS'ta yalnızca kurulu PWA'da mümkün. Kurulmadan izin istemek sessizce
 * başarısız oluyor ve kullanıcı "bildirim açtım ama gelmiyor" diyor —
 * arayüz bu durumda izin butonu yerine <b>kurulum talimatı</b> göstermeli.
 * </p>
 *
 * <p>
 * Burada <b>kalıcı işaret değil, gerçek pencere kipi</b> sorulur: iOS'ta
 * belirleyici olan uygulamanın kurulu olması değil, ŞU AN ana ekrandan
 * açılmış olması. Sekmede açıkken izin isteği yine sessizce düşerdi.
 * </p>
 */
export function canRequestNotifications(): boolean {
  if (typeof window === 'undefined' || !('Notification' in window)) return false;
  if (isIos() && !bagimsizMi()) return false;
  return true;
}

/** Service worker'ı kaydeder (çevrimdışı kabuk + push aynı worker). */
export async function registerServiceWorker(): Promise<void> {
  if (typeof navigator === 'undefined' || !('serviceWorker' in navigator)) return;

  try {
    /*
      KAPSAM `/`: uygulama kökten yayınlanıyor ve kurulabilirliğin şartı,
      `start_url`i çevrimdışı karşılayabilen bir worker'ın AYNI kapsamda
      olması. Dar kapsamlı bir worker `start_url`i denetlemediği için Chrome
      uygulamayı "kurulabilir" saymaz ve `beforeinstallprompt` hiç gelmez.
    */
    await navigator.serviceWorker.register('/firebase-messaging-sw.js', {
      scope: '/',
    });
  } catch {
    // Kayıt başarısızsa uygulama çevrimiçi olarak çalışmaya devam eder.
  }
}
