/**
 * Tarayıcı üzerinden uçtan uca görsel doğrulama.
 *
 * Çalışan sunucuya karşı GERÇEK bir Chrome açar, gerçek JWT ile giriş yapar
 * ve her ekranı hem masaüstü hem 390px genişlikte, açık ve koyu temada
 * gezip ekran görüntüsü alır. Konsol hatası çıkarsa koşum düşer.
 *
 *   node test/gorsel/tur.mjs [taban-url] [cikti-dizini]
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { tarayiciAc } from './cdp.mjs';

const TABAN = process.argv[2] ?? 'http://localhost:5097';
const CIKTI = process.argv[3] ?? '/tmp/workcollab-gorsel';
const KULLANICI = process.env.E2E_KULLANICI ?? 'admin';
const PAROLA = process.env.E2E_PAROLA ?? 'Admin123.';

/**
 * Ekran tanımları.
 *
 * `bekle` görünür bir METİN, `secici` ise bir CSS seçicidir. İkisi de
 * "veri geldi" anını yakalamak için; hangisinin daha sağlam olduğu ekrana
 * göre değişir. Örneğin tablo BAŞLIKLARI `innerText` içinde güvenilir
 * biçimde görünmüyor, o yüzden liste ekranlarında satır seçicisi kullanılır.
 */
const EKRANLAR = [
  // NOT: rozet metni CSS ile büyük harfe çevriliyor (SIRADAKİ); dönüşümden
  // etkilenmeyen bir başlık beklenir.
  { ad: 'ana-sayfa', yol: '/', bekle: 'Yaklaşan etkinlikler' },
  { ad: 'talepler', yol: '/talepler', secici: 'table tbody tr, ul li a' },
  /*
    ÇAPA "Program" DEĞİL — o metin MASAÜSTÜNE ait.

    "Program/Liste/Silinmiş" sekme şeridi `hidden md:flex` taşıyor, yani
    390px'te `display: none`. `bekleMetin` `innerText` okuyor ve gizli metni
    GÖRMEZ, dolayısıyla mobil geçişi hiçbir zaman doğru sebeple geçemezdi.
    Aynı ders bu satırın hemen altındaki iki kardeşinde zaten yazılıydı;
    yalnızca bu satır düzeltilmeden kalmış.
  */
  { ad: 'ajanda', yol: '/ajanda', secici: 'main input[type="search"]' },
  // İŞ TAKİP. Liste boş açılabilir; çıpa iki görünümde de duran arama alanı.
  { ad: 'gorevler', yol: '/gorevler', secici: 'main input[type="search"]' },
  { ad: 'gorev-detay', yol: '/gorevler/5', bekle: 'Aşamalar' },
  { ad: 'gorev-detay-dosya', yol: '/gorevler/5?sekme=tartisma', bekle: 'Yorumlar' },
  { ad: 'gorev-form', yol: '/gorevler/yeni', bekle: 'Vazgeç' },
  { ad: 'gorev-tipleri', yol: '/gorevler/tipler', bekle: 'Tip ekle' },
  // "Ekip kur" telefonda artık FAB'ın tabakasında — üst şeritte yalnızca
  // arama var (bkz. `Teams.tsx`). Çapa iki görünümde de duran arama alanı.
  { ad: 'ekipler', yol: '/ekipler', secici: 'main input[type="search"]' },
  // PROJE. Liste kart düzeninde; çıpa iki görünümde de duran arama alanı.
  { ad: 'projeler', yol: '/projeler', secici: 'main input[type="search"]' },
  { ad: 'proje-detay', yol: '/projeler/2', bekle: 'Kilometre taşları' },
  { ad: 'proje-pano', yol: '/projeler/2?sekme=pano', bekle: 'Sütunsuz' },
  { ad: 'proje-gantt', yol: '/projeler/2?sekme=gantt', bekle: 'Gantt' },
  { ad: 'proje-form', yol: '/projeler/yeni', bekle: 'Vazgeç' },
  // VATANDAŞ ve SAHA — kabuksuz yerleşim, harita içeriyor.
  { ad: 'vatandas-bildirimleri', yol: '/vatandas-bildirimleri', secici: 'main input[type="search"]' },
  { ad: 'harita', yol: '/harita', bekle: 'nokta' },
  { ad: 'gelen-kutusu', yol: '/gelen-kutusu', bekle: 'Bekliyor' },
  { ad: 'is-panosu', yol: '/is-panosu', bekle: 'Birim karnesi' },
  // Çapa alt sekme çubuğunda: "İşlerim" saha kabuğunun her ekranında var,
  // ekranın içeriği ise kullanıcının izinlerine göre değişiyor.
  { ad: 'saha', yol: '/saha', bekle: 'İşlerim' },
  // NOT: alan etiketleri CSS ile BÜYÜK HARFE çevriliyor ("NE GÖRDÜNÜZ?") ve
  // `innerText` dönüşmüş hâli veriyor; çıpa dönüşümden etkilenmeyen bir
  // düğme metni.
  { ad: 'saha-tespit', yol: '/saha/tespit', bekle: 'Tespiti kaydet' },
  /*
    ÇAPA "Yeni etkinlik" DEĞİL — o metin ekranın kendisine ait değildi.

    Bu iki satır uzun süre yeşil geçti ama YANLIŞ SEBEPLE: "Yeni etkinlik"
    düğmesi mobilde FAB'ın içinde ve kapalıyken görünmüyor. Eşleşen şey,
    her ekranın tepesinde çizilen bildirim izni kartının gövdesiydi —
    "Yeni etkinlik, havale ve size gönderilen dosyalar…". Kart yalnızca ana
    sayfaya alınınca çapa düştü ve testin aslında ajandayı hiç
    doğrulamadığı ortaya çıktı.

    Yerine iki görünümde de duran arama alanı konuyor; öteki liste
    ekranlarındaki çapayla aynı.
  */
  { ad: 'ajanda-liste', yol: '/ajanda?sekme=liste', secici: 'main input[type="search"]' },
  { ad: 'ajanda-silinmis', yol: '/ajanda?sekme=silinmis', secici: 'main input[type="search"]' },
  { ad: 'etkinlik-form', yol: '/ajanda/yeni', bekle: 'Yeni etkinlik' },
  { ad: 'takvim', yol: '/takvim', bekle: null },
  { ad: 'takvim-hafta', yol: '/takvim?gorunum=hafta', bekle: null },
  { ad: 'takvim-gun', yol: '/takvim?gorunum=gun', bekle: null },
  // "Yeni kayıt" MOBİLDE FAB'da; çıpa iki görünümde de duran bir metin.
  { ad: 'protokol', yol: '/protokol', bekle: 'kayıt' },
  { ad: 'protokol-detay', yol: '/protokol/1', bekle: 'Davet' },
  // Sayı karolarının etiketleri CSS ile BÜYÜK HARFE çevriliyor ("TALİMAT")
  // ve `innerText` dönüşmüş hâli veriyor; çıpa seçici olmalı.
  { ad: 'cicekci-detay', yol: '/cicek/1', secici: 'main a[href="/cicek"]' },
  // NOT: "Bekleyenler" ve "Salon modu" MOBİLDE FAB tabakasının içinde;
  // bekleme çıpası iki görünümde de duran bir şey olmalı.
  { ad: 'halk-gunu', yol: '/halk-gunu', secici: 'main a[href^="/halk-gunu/"]' },
  { ad: 'halk-gunu-basvurular', yol: '/halk-gunu/basvurular', bekle: 'Bekleyen' },
  { ad: 'halk-gunu-detay', yol: '/halk-gunu/1', bekle: 'Zaman dilimleri' },
  { ad: 'halk-gunu-salon', yol: '/halk-gunu/1/salon', bekle: 'Listeye dön' },
  { ad: 'gonderim', yol: '/gonderim', bekle: 'Gelen' },
  // "Süzgeç" düğmesi MOBİLDE FAB'da; çıpa iki görünümde de duran arama alanı.
  { ad: 'ozgecmisler', yol: '/ozgecmisler', secici: 'main input[type="search"]' },
  { ad: 'davetler', yol: '/davetler', bekle: 'Yeni davet' },
  // "PDF çıktı" MOBİLDE FAB tabakasında; çıpa iki görünümde de duran bir metin.
  { ad: 'davet-detay', yol: '/davetler/1', bekle: 'kişi' },
  // NOT: bölüm başlıkları CSS ile büyük harfe çevriliyor ve `innerText` de
  // dönüşmüş hâli veriyor; dönüşümden etkilenmeyen bir metin beklenir.
  { ad: 'talep-form', yol: '/talepler/yeni', bekle: 'Vazgeç' },
  { ad: 'istatistikler', yol: '/istatistikler', bekle: 'İstatistikler ve Raporlar' },
  // Merkez artık dokuz konuya açılıyor; tur ikisini örnekliyor: biri KENDİ
  // ekranı olan (etkinlik), biri genel çiziciyi kullanan (sistem). İkisi
  // farklı kod yolları — yalnızca birini gezmek ötekini ölçüsüz bırakırdı.
  { ad: 'istatistik-etkinlik', yol: '/istatistikler/etkinlik', bekle: 'Toplam etkinlik' },
  { ad: 'istatistik-sistem', yol: '/istatistikler/sistem', bekle: 'Sistem Sağlığı' },
  { ad: 'yonetim', yol: '/yonetim', bekle: 'Kullanıcılar' },
  { ad: 'yonetim-kullanici-form', yol: '/yonetim?bolum=kullanicilar&kullanici=yeni',
    bekle: 'Birim' },
  { ad: 'yonetim-birimler', yol: '/yonetim?bolum=birimler', bekle: 'Yeni birim' },
  { ad: 'yonetim-birim-form', yol: '/yonetim?bolum=birimler&birim=yeni', bekle: 'kök birim' },
  { ad: 'yonetim-roller', yol: '/yonetim?bolum=roller', bekle: 'kullanıcı' },
  { ad: 'yonetim-oturumlar', yol: '/yonetim?bolum=oturumlar', bekle: 'Yalnızca başarısız' },
  { ad: 'yonetim-birim-detay', yol: '/yonetim/birimler/1', bekle: 'Birimdeki kullanıcılar' },
  { ad: 'yonetim-rol-detay', yol: '/yonetim/roller/Admin', bekle: 'role sahip' },
  // Bekleme metni VERİDEN bağımsız olmalı: "N kayıt" satırı kaldırıldı ve
  // bütün hatalar çözülünce liste zaten boş açılıyor. Anahtar etiketi her
  // durumda ekranda.
  { ad: 'hatalar', yol: '/hatalar', bekle: 'Yalnızca çözülmemiş' },
  { ad: 'yardim', yol: '/yardim', bekle: 'Telefonda Kullanım' },
  { ad: 'tanimlar', yol: '/tanimlar', bekle: 'Etkinlik durumları' },
  // Kurum bilgileri — marka rengi ve amblem buradan geliyor; ekranın kendisi
  // de o değerlerle boyandığı için turda görünmesi anlamlı.
  { ad: 'kurum', yol: '/kurum', bekle: 'KURUMSAL RENKLER' },
  { ad: 'cicek', yol: '/cicek', bekle: 'çiçekçi' },
  { ad: 'bildirimler', yol: '/bildirimler', bekle: 'Son 30 gün' },
  { ad: 'ayarlar', yol: '/ayarlar', bekle: 'Görünüm' },
];

const GORUNUMLER = [
  { ad: 'masaustu', g: 1440, y: 900, mobil: false },
  { ad: 'mobil', g: 390, y: 844, mobil: true },
];

const TEMALAR = ['acik', 'koyu'];

const sonuclar = [];
let hataVar = false;

function bildir(durum, mesaj) {
  const isaret = durum === 'ok' ? '✓' : durum === 'uyari' ? '!' : '✗';
  console.log(`  ${isaret} ${mesaj}`);
  if (durum === 'hata') hataVar = true;
}

const tarayici = await tarayiciAc();

try {
  // ── 1. Giriş: gerçek JWT al ──
  console.log(`\n▸ Sunucu: ${TABAN}`);
  const girisYaniti = await fetch(`${TABAN}/api/v2/oturum/giris`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ kullaniciAdi: KULLANICI, parola: PAROLA }),
  });

  if (!girisYaniti.ok) {
    throw new Error(`Giriş başarısız (${girisYaniti.status}). Sunucu ayakta mı?`);
  }
  const jeton = await girisYaniti.json();
  bildir('ok', `Giriş yapıldı: ${KULLANICI}`);

  // ── 2. Giriş ekranı (oturumsuz) ──
  console.log('\n▸ Giriş ekranı');

  // Tarayıcı profili /tmp altında KALICI; önceki koşumdan kalan jeton
  // giriş ekranını atlatır ve test "form yok" diye düşerdi.
  await tarayici.git(`${TABAN}/`);
  await tarayici.calistir('localStorage.clear()');

  for (const g of GORUNUMLER) {
    await tarayici.boyutlandir(g.g, g.y, g.mobil);
    await tarayici.git(`${TABAN}/giris`);
    await tarayici.bekleSecici('input[type="password"]');
    const yol = await tarayici.ekranGoruntusu(`${CIKTI}/giris-${g.ad}.png`);
    sonuclar.push({ ekran: 'giris', gorunum: g.ad, tema: 'acik', yol });
    bildir('ok', `giris · ${g.ad}`);
  }

  // ── 3. Jetonu tarayıcıya yerleştir ──
  await tarayici.calistir(`
    localStorage.setItem('sv-jetonu', ${JSON.stringify(JSON.stringify(jeton))});
  `);

  // ── 4. Her ekran × görünüm × tema ──
  for (const tema of TEMALAR) {
    console.log(`\n▸ Tema: ${tema}`);
    await tarayici.calistir(`localStorage.setItem('sv-tema', '${tema}')`);

    for (const g of GORUNUMLER) {
      await tarayici.boyutlandir(g.g, g.y, g.mobil);

      for (const e of EKRANLAR) {
        try {
          await tarayici.git(`${TABAN}${e.yol}`);
          await tarayici.bekleSecici('#kok > *');

          // İskeletler kaybolana kadar bekle (veri geldi demektir).
          if (e.secici) await tarayici.bekleSecici(e.secici);
          else if (e.bekle) await tarayici.bekleMetin(e.bekle);
          else await tarayici.bekleSecici('main');

          // Yatay taşma kontrolü — mobil-öncelikli tasarımın temel şartı.
          const tasma = await tarayici.calistir(
            'document.documentElement.scrollWidth - document.documentElement.clientWidth',
          );

          /*
            MOBİLDE GÖRÜNTÜ ALANI, masaüstünde tam sayfa.

            Tam sayfa yakalama `position: fixed` öğeleri sayfanın ortasına
            çiziyor: mobil tabbar ekran görüntülerinde içeriğin ortasında
            "sünmüş" gibi duruyor ve uygulamada bir hata varmış izlenimi
            veriyordu — oysa gerçek cihazda çubuk yerli yerinde. Mobil
            görüntüler artık kullanıcının GÖRDÜĞÜ kareyi alıyor.
          */
          // Diyalog/tabaka açılış canlandırması bitsin: yarı saydam
          // yakalanan kareler tasarımı soluk gösteriyordu.
          await tarayici.animasyonBitsin();

          const yol = await tarayici.ekranGoruntusu(
            `${CIKTI}/${e.ad}-${g.ad}-${tema}.png`,
            { tamSayfa: !g.mobil },
          );
          sonuclar.push({ ekran: e.ad, gorunum: g.ad, tema, yol, tasma });

          if (tasma > 2) {
            bildir('hata', `${e.ad} · ${g.ad} · ${tema} — YATAY TAŞMA ${tasma}px`);
          } else {
            bildir('ok', `${e.ad} · ${g.ad} · ${tema}`);
          }
        } catch (h) {
          bildir('hata', `${e.ad} · ${g.ad} · ${tema} — ${h.message.split('\n')[0]}`);
          sonuclar.push({ ekran: e.ad, gorunum: g.ad, tema, hata: h.message });
        }
      }
    }
  }

  // ── 5. Tema gerçekten uygulanmış mı? ──
  console.log('\n▸ Tema doğrulaması');
  for (const tema of TEMALAR) {
    await tarayici.calistir(`localStorage.setItem('sv-tema', '${tema}')`);
    await tarayici.git(`${TABAN}/ayarlar`);
    await tarayici.bekleMetin('Görünüm');

    const nitelik = await tarayici.calistir(
      "document.documentElement.getAttribute('data-tema')",
    );
    const zemin = await tarayici.calistir(
      "getComputedStyle(document.body).backgroundColor",
    );

    if (nitelik === tema) bildir('ok', `data-tema="${tema}" · body zemini ${zemin}`);
    else bildir('hata', `data-tema "${nitelik}" bekleniyordu "${tema}"`);
  }

  // ── 6. Derin bağlantı (sayfa yenilemeli) ──
  console.log('\n▸ Derin bağlantı');
  await tarayici.git(`${TABAN}/istatistikler`);
  await tarayici.bekleMetin('İstatistikler ve Raporlar');
  bildir('ok', '/istatistikler doğrudan açılıyor (MapFallbackToFile)');

  /*
    İKİ SEVİYELİ derin bağlantı da denenir.

    `MapFallbackToFile` kalıbı `{*yol:nonfile}`; tek seviyeli bir yolun
    çalışması iki seviyelinin çalıştığını GÖSTERMEZ ve istatistik merkezi
    artık bütün konularını ikinci seviyede tutuyor. Bozulsaydı belirti
    yalnızca YENİLEMEDE görünürdü — uygulama içinden tıklayan hiç fark
    etmezdi.
  */
  await tarayici.git(`${TABAN}/istatistikler/etkinlik`);
  await tarayici.bekleMetin('Toplam etkinlik');
  bildir('ok', '/istatistikler/etkinlik (iki seviyeli) doğrudan açılıyor');

  // ── 7. Konsol hataları ──
  console.log('\n▸ Konsol');
  // React'in geliştirme uyarıları üretimde çıkmaz; gerçek hataları süz.
  const onemliHatalar = tarayici.konsolHatalari.filter(
    (h) => !/favicon|manifest|firebase|messaging|ServiceWorker|permission/i.test(h),
  );
  if (onemliHatalar.length === 0) {
    bildir('ok', 'JavaScript hatası yok');
  } else {
    for (const h of onemliHatalar.slice(0, 10)) bildir('hata', h.slice(0, 200));
  }

  mkdirSync(CIKTI, { recursive: true });
  writeFileSync(`${CIKTI}/ozet.json`, JSON.stringify(sonuclar, null, 2));

  console.log(`\n${sonuclar.length} ekran görüntüsü → ${CIKTI}`);
  console.log(hataVar ? '\nSONUÇ: HATA VAR\n' : '\nSONUÇ: TEMİZ\n');
} finally {
  await tarayici.kapat();
}

process.exit(hataVar ? 1 : 0);
