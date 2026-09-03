/**
 * YETKİ MATRİSİ TURU — arayüz gerçekten izne göre kısıtlanıyor mu?
 *
 * Her rol için ayrı ayrı giriş yapar, her ekranı gezer ve şunları ÖLÇER:
 *   • menüde hangi öğeler var
 *   • ekranda hangi eylem düğmeleri var
 *   • rota kapıyı açıyor mu, yoksa "yetkiniz yok" mu diyor
 *   • listede kaç kayıt görünüyor (basın daraltması için)
 *
 * Ekran görüntüsü de alır ama asıl çıktı ÖLÇÜMDÜR: bir düğmenin görünüp
 * görünmediğini gözle karşılaştırmak, on ekran × altı rol için güvenilir
 * değil.
 *
 * Koşum:  node test/gorsel/yetki-turu.mjs
 */
import { mkdirSync, writeFileSync } from 'node:fs';
import { tarayiciAc } from './cdp.mjs';

const TABAN = process.env.TABAN ?? 'http://localhost:5097';
const CIKTI = '/tmp/workcollab-yetki';
const PAROLA = 'Gelistirme123.';

mkdirSync(CIKTI, { recursive: true });

/** Sınanacak kullanıcılar — dar yetkiden geniş yetkiye. */
const KULLANICILAR = [
  { ad: 'kullanici', etiket: 'Kullanıcı (yetkisiz)' },
  { ad: 'basin', etiket: 'Basın' },
  { ad: 'izintest', etiket: 'Talep Personeli', parola: 'Izin123.' },
  { ad: 'cicek', etiket: 'Çiçek' },
  { ad: 'sekreter', etiket: 'Sekreter' },
  { ad: 'baskan', etiket: 'Başkan' },
  { ad: 'admin', etiket: 'Admin', parola: 'Admin123.' },
];

/** Gezilecek ekranlar. */
const EKRANLAR = [
  { ad: 'anasayfa', yol: '/' },
  { ad: 'ajanda', yol: '/ajanda' },
  { ad: 'takvim', yol: '/takvim' },
  { ad: 'talepler', yol: '/talepler' },
  { ad: 'protokol', yol: '/protokol' },
  { ad: 'davetler', yol: '/davetler' },
  { ad: 'cicek', yol: '/cicek' },
  { ad: 'gonderim', yol: '/gonderim' },
  { ad: 'ozgecmisler', yol: '/ozgecmisler' },
  { ad: 'halk-gunu', yol: '/halk-gunu' },
  { ad: 'istatistikler', yol: '/istatistikler' },
  { ad: 'yonetim', yol: '/yonetim' },
  { ad: 'tanimlar', yol: '/tanimlar' },
  { ad: 'hatalar', yol: '/hatalar' },
];

const bildir = (t, m) =>
  console.log(`${t === 'ok' ? '  ✓' : t === 'x' ? '  ✗' : '  ·'} ${m}`);

const tarayici = await tarayiciAc({ port: 9345, profilDizini: '/tmp/wc-yetki-profil' });
const rapor = [];

try {
  await tarayici.boyutlandir(1440, 900, false);

  for (const k of KULLANICILAR) {
    console.log(`\n▸ ${k.etiket} (${k.ad})`);

    const yanit = await fetch(`${TABAN}/api/v2/oturum/giris`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ kullaniciAdi: k.ad, parola: k.parola ?? PAROLA }),
    });
    if (!yanit.ok) {
      bildir('x', `giriş başarısız (${yanit.status})`);
      continue;
    }
    const jeton = await yanit.json();

    // İzinleri sunucudan da oku: arayüzün gördüğüyle karşılaştıracağız.
    const ben = await (
      await fetch(`${TABAN}/api/v2/oturum/ben`, {
        headers: { Authorization: `Bearer ${jeton.jeton}` },
      })
    ).json();

    await tarayici.git(`${TABAN}/`);
    await tarayici.calistir(
      `localStorage.clear();
       localStorage.setItem('sv-jetonu', ${JSON.stringify(JSON.stringify(jeton))});
       localStorage.setItem('sv-tema', 'acik');`,
    );

    const kayit = {
      kullanici: k.ad,
      etiket: k.etiket,
      roller: ben.roller,
      izinler: (ben.izinler ?? []).sort(),
      menu: [],
      ekranlar: {},
    };

    // ── Menü ──
    await tarayici.git(`${TABAN}/`);
    await tarayici.bekleSecici('#kok > *');
    kayit.menu = await tarayici.calistir(`
      [...document.querySelectorAll('aside a[href]')]
        .map((a) => a.textContent.trim())
        .filter(Boolean)
    `);
    bildir('ok', `menü: ${kayit.menu.join(' · ') || '(boş)'}`);

    const yol = `${CIKTI}/${k.ad}-menu.png`;
    await tarayici.ekranGoruntusu(yol, { tamSayfa: false });

    // ── Ekranlar ──
    for (const e of EKRANLAR) {
      try {
        await tarayici.git(`${TABAN}${e.yol}`);
        await tarayici.bekleSecici('#kok > *');
        await new Promise((r) => setTimeout(r, 700));

        const olcum = await tarayici.calistir(`
          (() => {
            const govde = document.body.innerText;
            const engelli = govde.includes('erişim yetkiniz yok')
                         || govde.includes('Yetkiniz yok');
            // Eylem düğmeleri: ekranın üst şeridindeki ve satır sonundaki
            // tıklanabilir öğeler. Metni olanları alıyoruz; ikon düğmeler
            // aria-label taşıyor.
            const dugmeler = [...document.querySelectorAll('main button, main a[href]')]
              .map((d) => (d.textContent || '').trim() || d.getAttribute('aria-label') || '')
              .filter((t) => t && t.length < 40);
            return {
              engelli,
              dugmeler: [...new Set(dugmeler)],
              // Liste kayıtları farklı ekranlarda farklı düğümler: tablo
              // satırı, liste öğesi ya da kart. Üçünü de sayıyoruz, yoksa
              // "0 satır" ölçümü ekranın boş olduğunu sanmamıza yol açar.
              satir: document.querySelectorAll(
                'main tbody tr, main li[data-kayit], main [data-kayit]'
              ).length || document.querySelectorAll('main li').length,
            };
          })()
        `);

        kayit.ekranlar[e.ad] = olcum;
        bildir(
          olcum.engelli ? 'x' : 'ok',
          `${e.ad}: ${olcum.engelli ? 'ENGELLİ' : `${olcum.satir} satır, ${olcum.dugmeler.length} eylem`}`,
        );

        await tarayici.ekranGoruntusu(`${CIKTI}/${k.ad}-${e.ad}.png`, {
          tamSayfa: false,
        });
      } catch (h) {
        kayit.ekranlar[e.ad] = { hata: String(h.message ?? h) };
        bildir('x', `${e.ad}: ${h.message ?? h}`);
      }
    }

    // ── Mobil görünüm ──
    // Kısıt her iki düzende de geçerli olmalı: mobilde menü alt çubuğa
    // taşınıyor ve süzgeç ayrı bir kod yolundan geçiyor.
    await tarayici.boyutlandir(390, 844, true);
    for (const e of [EKRANLAR[0], EKRANLAR[1], EKRANLAR[3]]) {
      await tarayici.git(`${TABAN}${e.yol}`);
      await tarayici.bekleSecici('#kok > *');
      await new Promise((r) => setTimeout(r, 600));
      await tarayici.ekranGoruntusu(`${CIKTI}/${k.ad}-${e.ad}-mobil.png`, {
        tamSayfa: false,
      });
    }
    kayit.mobilSekmeler = await tarayici.calistir(`
      [...document.querySelectorAll('nav a[href], footer a[href]')]
        .map((a) => a.textContent.trim()).filter(Boolean)
    `);
    bildir('ok', `mobil sekmeler: ${(kayit.mobilSekmeler ?? []).join(' · ') || '(yok)'}`);
    await tarayici.boyutlandir(1440, 900, false);

    rapor.push(kayit);
  }

  writeFileSync(`${CIKTI}/rapor.json`, JSON.stringify(rapor, null, 2));
  console.log(`\n▸ Rapor: ${CIKTI}/rapor.json`);
  console.log(`▸ Görüntüler: ${CIKTI}/`);
} finally {
  await tarayici.kapat();
}
