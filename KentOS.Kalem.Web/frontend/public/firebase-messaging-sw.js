/*
 * Web push service worker.
 *
 * KAPSAM: `/` — uygulama kökten yayınlanıyor ve kurulabilirliğin şartı,
 * `start_url`i karşılayabilen bir worker'ın AYNI kapsamda olması. Bunun
 * bedeli, eski MVC arayüzünün (/Ajanda, /Randevu, /Modules ...) isteklerinin
 * de buradan geçmesi: o yüzden `fetch` dalında yalnızca BİZİM ürettiğimiz
 * statik varlıklar önbelleğe alınır ve gezinme istekleri ağa gider (bkz.
 * `onbellegeAlinirMi`).
 *
 * Bu dosya Vite tarafından İŞLENMEZ (public/ altında), bu yüzden `import`
 * kullanılamaz — compat SDK'ları importScripts ile yüklenir.
 */
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.14.1/firebase-messaging-compat.js');

/*
 * YAPILANDIRMA KODA YAZILMAZ — sunucudan alınır.
 *
 * Uygulama başka belediyelere verilecek; her kurumun kendi Firebase projesi
 * var. Değerler `GET /api/v2/institution` yanıtındaki `bildirim` alanından
 * geliyor (gizli değiller, ama kuruma özeller).
 *
 * Service worker'da üst düzey `await` kullanılabiliyor ama `importScripts`
 * sonrası akışı bloklamamak için kurulum bir söze bağlanıyor; arka plan
 * mesajı geldiğinde `hazir` beklenir. Yapılandırma alınamazsa bildirim
 * aboneliği kurulmaz — yarım yapılandırmayla `initializeApp` çağırmak
 * anlamsız hatalar üretiyor.
 */
let messaging = null;

const hazir = fetch('/api/v2/institution', { headers: { Accept: 'application/json' } })
  .then((y) => (y.ok ? y.json() : null))
  .then((kurum) => {
    if (!kurum || !kurum.bildirim) return null;
    firebase.initializeApp({
      apiKey: kurum.bildirim.apiKey,
      authDomain: kurum.bildirim.authDomain,
      projectId: kurum.bildirim.projectId,
      storageBucket: kurum.bildirim.storageBucket,
      messagingSenderId: kurum.bildirim.messagingSenderId,
      appId: kurum.bildirim.appId,
    });
    messaging = firebase.messaging();
    messaging.onBackgroundMessage(arkaPlanMesaji);
    return messaging;
  })
  .catch(() => null);

// Bir yerde tutulması gerekiyor: `hazir` çözülene kadar kimse kullanmıyor
// ama kullanılmayan değişken uyarısı vermesin.
void hazir;

function arkaPlanMesaji(yuk) {
  const baslik = (yuk.notification && yuk.notification.title) || 'Bildirim';
  const govde = (yuk.notification && yuk.notification.body) || '';
  const ham = yuk.data && yuk.data.fcmData;

  /*
   * `tag`: aynı KAYDA ait bildirimler üst üste yığılmaz, sonuncusu öncekinin
   * yerini alır. Bir etkinlik arka arkaya güncellenirse kullanıcı işletim
   * sistemi bildirim merkezinde beş ayrı satır bulmuyor.
   *
   * `renotify: false`: yerine geçen bildirim yeniden ses/titreşim üretmez.
   */
  var etiket = 'workcollab';
  try {
    var v = JSON.parse(ham || '{}');
    if (v.entity && v.id) etiket = 'wc-' + String(v.entity).toLowerCase() + '-' + v.id;
  } catch (e) { /* biçim bozuk — genel etiket */ }

  return self.clients
    .matchAll({ type: 'window', includeUncontrolled: true })
    .then(function (pencereler) {
      // Uygulama AÇIK ve odaktaysa sistem bildirimi gösterme: ön plandaki
      // sekme zaten toast gösteriyor ve bildirim merkezine düşüyor. İkisi
      // birden çıkınca kullanıcı aynı şeyi iki kez temizlemek zorunda kalıyordu.
      var odakta = pencereler.some(function (p) {
        return p.url.indexOf('/') !== -1 && p.focused;
      });
      if (odakta) return;

      return self.registration.showNotification(baslik, {
        body: govde,
        icon: '/amblem.png',
        badge: '/amblem.png',
        tag: etiket,
        renotify: false,
        data: { fcmData: ham },
      });
    });
}

self.addEventListener('notificationclick', (olay) => {
  olay.notification.close();

  let yol = '/';
  try {
    const veri = JSON.parse((olay.notification.data && olay.notification.data.fcmData) || '{}');

    /*
      `action !== 'None'` KOŞULU KALDIRILDI.

      Sunucu yeni varlıkları (Gorev, Ozgecmis, Dosya) bilerek `None` ile
      gönderiyor; bu, yayındaki eski MOBİL sürümlere "hiçbir yere gitme"
      demek — web'e "detayı açma" demek değil. Koşul burada dururken arka
      planda gelen bir görev bildirimine dokunmak kullanıcıyı ana sayfaya
      atıyordu. Aynı düzeltme `fcm.ts` ve `NotificationCenter.tsx`
      içinde de var; bu eşleme DÖRT yerde birden tutuluyor ve sunucu
      tarafındaki `BildirimYoluTests` dördünü de denetliyor.
    */
    const varlik = String(veri.entity || '').toLowerCase();
    const kimlikVar = veri.id != null && Number(veri.id) > 0;

    if (kimlikVar) {
      if (varlik === 'ajanda') yol = '/ajanda/' + veri.id;
      else if (varlik === 'talep') yol = '/talepler/' + veri.id;
      else if (varlik === 'oneri') yol = '/oneriler/' + veri.id;
      else if (varlik === 'dosya') yol = '/gonderim/' + veri.id;
      else if (varlik === 'ozgecmis') yol = '/ozgecmisler/' + veri.id;
      else if (varlik === 'gorev') yol = '/gorevler/' + veri.id;
      else if (varlik === 'proje') yol = '/projeler/' + veri.id;
    }

    // Gelen kutusu bir LİSTE ekranı: kimlik olmadan da gidilebiliyor.
    if (varlik === 'gelenkutusu') yol = '/gelen-kutusu';
  } catch (e) { /* biçim bozuk — ana ekrana git */ }

  olay.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then((pencereler) => {
      // Açık bir sekme varsa onu öne al ve yönlendir; yenisini açma.
      for (const p of pencereler) {
        if (p.url.includes('/') && 'focus' in p) {
          p.postMessage({ tur: 'bildirim-yolu', yol: yol });
          return p.focus();
        }
      }
      return clients.openWindow(yol);
    }),
  );
});


/* ═══════════════════════════════════════════════════════════════════
 * Çevrimdışı kabuk
 *
 * Bu service worker HEM push HEM önbellek işini görüyor. İki ayrı worker
 * yazılamaz: bir kapsamı yalnızca TEK worker denetleyebilir; ikincisi
 * birincinin yerini alır ve push sessizce çalışmayı bırakır.
 *
 * Strateji bilinçli olarak DAR:
 *  - Uygulama kabuğu (HTML/JS/CSS/ikon) → önbellekten, arkada tazelenir.
 *  - `/api/**` → HİÇ önbelleğe alınmaz. Gizli etkinlik taşıyan bir sistemde
 *    yanıtları diske yazmak, ortak bilgisayarda çıkış yapıldıktan sonra da
 *    okunabilir veri bırakır.
 * ═══════════════════════════════════════════════════════════════════ */

var SURUM = 'wc-kabuk-v2';
var KABUK = [
  '/',
  '/index.html',
  '/manifest.webmanifest',
  '/ikon/ikon-192.png',
  '/ikon/ikon-512.png',
  '/ikon/apple-touch-icon.png',
];

/*
 * ÖNBELLEĞE ALINACAK YOLLAR — beyaz liste.
 *
 * Burada bir dönem tek satırlık bir kapı vardı: `/yeni` ile başlamayan her
 * isteği geçir. SPA kökten yayınlanmaya başlayınca o kapı HİÇBİR isteği
 * içeri almaz oldu — çevrimdışı kabuk sessizce tamamen devre dışı kaldı,
 * uygulama ağsız açıldığında tarayıcının "bağlantı yok" sayfasını gösteriyordu.
 *
 * Yerine beyaz liste geldi: yalnızca BİZİM ürettiğimiz statik varlıklar.
 * Eski MVC arayüzünün dosyaları (`/assets`, `/lib`, `/css`) ve yüklenen
 * belgeler (`/uploads`) dışarıda kalıyor — kara liste yazsaydık, yarın
 * eklenen bir MVC klasörü sessizce önbelleğe düşerdi.
 */
function onbellegeAlinirMi(yol) {
  return (
    yol.indexOf('/uygulama/') === 0 ||
    yol.indexOf('/ikon/') === 0 ||
    yol === '/amblem.png' ||
    yol === '/manifest.webmanifest'
  );
}

self.addEventListener('install', function (olay) {
  olay.waitUntil(
    caches.open(SURUM).then(function (onbellek) {
      /*
       * TEK TEK, HATAYA DAYANIKLI.
       *
       * `addAll` hep-ya-hiç: listedeki tek bir dosya 404 dönse ya da ağ o an
       * takılsa KURULUM TAMAMEN düşüyor ve worker hiç etkinleşmiyor — yani
       * bir ikonun eksikliği bildirimleri de sessizce kapatıyordu.
       * `reload`: kurulum sırasında HTTP önbelleğinden bayat dosya alınmasın.
       */
      return Promise.all(
        KABUK.map(function (u) {
          return onbellek
            .add(new Request(u, { cache: 'reload' }))
            .catch(function () { /* bu dosya çevrimdışı kullanılamayacak */ });
        })
      );
    }).then(function () {
      // Yeni sürüm beklemeden devralsın; kullanıcı sekmeyi kapatıp açmak
      // zorunda kalmıyor.
      return self.skipWaiting();
    })
  );
});

self.addEventListener('activate', function (olay) {
  olay.waitUntil(
    caches.keys().then(function (anahtarlar) {
      return Promise.all(
        anahtarlar.filter(function (a) {
          return a.indexOf('wc-kabuk-') === 0 && a !== SURUM;
        }).map(function (a) { return caches.delete(a); })
      );
    }).then(function () { return self.clients.claim(); })
  );
});

self.addEventListener('fetch', function (olay) {
  var istek = olay.request;

  // Yalnızca GET; POST/PUT/DELETE önbelleğe alınamaz.
  if (istek.method !== 'GET') return;

  var url = new URL(istek.url);

  // Başka kökenler (Google Fonts, gstatic) ve API'ye DOKUNULMAZ.
  if (url.origin !== self.location.origin) return;
  if (url.pathname.indexOf('/api/') === 0) return;
  // Yüklenen belgeler: gizli veri, diske yazılmaz.
  if (url.pathname.indexOf('/uploads/') === 0) return;

  /*
   * Gezinme istekleri: AĞ ÖNCE, çevrimdışıysa kabuk.
   *
   * Ağ önce olması şart — kapsam kökte ve eski MVC arayüzünün sayfaları da
   * buradan geçiyor. Önbellek önce olsaydı `/Randevu` istendiğinde SPA'nın
   * index.html'i dönebilirdi. Çevrimiçiyken bu dal, isteği hiç
   * yakalamamakla aynı davranıyor.
   */
  if (istek.mode === 'navigate') {
    olay.respondWith(
      fetch(istek).catch(function () {
        return caches.match('/index.html').then(function (yanit) {
          return yanit || Response.error();
        });
      })
    );
    return;
  }

  if (!onbellegeAlinirMi(url.pathname)) return;

  // Varlıklar: önbellekten ver, arkada tazele (stale-while-revalidate).
  olay.respondWith(
    caches.match(istek).then(function (onbellekten) {
      var agdan = fetch(istek).then(function (yanit) {
        // Yalnızca başarılı, aynı kökenli yanıtlar saklanır. Opak yanıtları
        // saklamak, hatayı kalıcı hâle getirir.
        if (yanit && yanit.status === 200 && yanit.type === 'basic') {
          var kopya = yanit.clone();
          caches.open(SURUM).then(function (o) { o.put(istek, kopya); });
        }
        return yanit;
      }).catch(function () { return onbellekten; });

      return onbellekten || agdan;
    })
  );
});
