/**
 * Küçük bir Chrome DevTools Protocol sürücüsü.
 *
 * Neden Playwright/Puppeteer değil: bu depoya yüzlerce megabaytlık bir
 * tarayıcı indirici eklemek, yalnızca ekran görüntüsü almak için ağır kaçıyor.
 * Sistemde zaten kurulu olan Chrome'u uzaktan hata ayıklama portundan
 * sürmek aynı işi görüyor ve CI'da da `CHROME` değişkeniyle yönlendirilebilir.
 *
 * Node 22+ küresel `WebSocket` taşıdığı için ek bağımlılık gerekmiyor.
 */
import { spawn } from 'node:child_process';
import { mkdirSync, writeFileSync } from 'node:fs';
import { setTimeout as bekle } from 'node:timers/promises';

const CHROME =
  process.env.CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';

export async function tarayiciAc({ port = 9333, profilDizini } = {}) {
  const surec = spawn(
    CHROME,
    [
      '--headless=new',
      `--remote-debugging-port=${port}`,
      `--user-data-dir=${profilDizini ?? `/tmp/workcollab-cdp-${port}`}`,
      '--no-first-run',
      '--no-default-browser-check',
      '--disable-gpu',
      '--hide-scrollbars',
      '--force-device-scale-factor=2',
      'about:blank',
    ],
    { stdio: 'ignore', detached: false },
  );

  // Hata ayıklama uç noktası hazır olana kadar bekle.
  let hedef = null;
  for (let i = 0; i < 100; i++) {
    try {
      const y = await fetch(`http://127.0.0.1:${port}/json/list`);
      const liste = await y.json();
      hedef = liste.find((h) => h.type === 'page');
      if (hedef) break;
    } catch {
      /* henüz açılmadı */
    }
    await bekle(100);
  }
  if (!hedef) {
    surec.kill();
    throw new Error(`Chrome ${port} portunda hazır olmadı.`);
  }

  const soket = new WebSocket(hedef.webSocketDebuggerUrl);
  await new Promise((coz, at) => {
    soket.onopen = coz;
    soket.onerror = () => at(new Error('CDP soketi açılamadı.'));
  });

  let siradaki = 1;
  const bekleyenler = new Map();

  soket.onmessage = (olay) => {
    const m = JSON.parse(olay.data);
    if (m.id && bekleyenler.has(m.id)) {
      const { coz, at } = bekleyenler.get(m.id);
      bekleyenler.delete(m.id);
      m.error ? at(new Error(m.error.message)) : coz(m.result);
    }
  };

  const gonder = (yontem, parametreler = {}) =>
    new Promise((coz, at) => {
      const id = siradaki++;
      bekleyenler.set(id, { coz, at });
      soket.send(JSON.stringify({ id, method: yontem, params: parametreler }));
    });

  await gonder('Page.enable');
  await gonder('Runtime.enable');
  await gonder('Network.enable');

  const konsolHatalari = [];
  soket.addEventListener('message', (olay) => {
    const m = JSON.parse(olay.data);
    if (m.method === 'Runtime.exceptionThrown') {
      konsolHatalari.push(m.params.exceptionDetails.text ?? 'bilinmeyen istisna');
    }
    if (m.method === 'Runtime.consoleAPICalled' && m.params.type === 'error') {
      konsolHatalari.push(m.params.args.map((a) => a.value ?? a.description).join(' '));
    }
  });

  return {
    gonder,
    konsolHatalari,

    async boyutlandir(genislik, yukseklik, mobil = false) {
      await gonder('Emulation.setDeviceMetricsOverride', {
        width: genislik,
        height: yukseklik,
        deviceScaleFactor: 2,
        mobile: mobil,
      });
    },

    async git(url) {
      await gonder('Page.navigate', { url });
      // `load` olayı SPA için yeterli değil; ilk veri çekimini de bekleriz.
      await bekle(150);
    },

    async calistir(ifade) {
      const s = await gonder('Runtime.evaluate', {
        expression: ifade,
        awaitPromise: true,
        returnByValue: true,
      });
      if (s.exceptionDetails) {
        throw new Error(s.exceptionDetails.text + ' :: ' + (s.exceptionDetails.exception?.description ?? ''));
      }
      return s.result.value;
    },

    /** Bir CSS seçici görünene kadar bekler. */
    async bekleSecici(secici, zamanAsimi = 8000) {
      const son = Date.now() + zamanAsimi;
      while (Date.now() < son) {
        const varMi = await this.calistir(
          `!!document.querySelector(${JSON.stringify(secici)})`,
        );
        if (varMi) return true;
        await bekle(80);
      }
      throw new Error(`Seçici bulunamadı: ${secici}`);
    },

    /** Metin sayfada görünene kadar bekler. */
    async bekleMetin(metin, zamanAsimi = 8000) {
      const son = Date.now() + zamanAsimi;
      while (Date.now() < son) {
        const varMi = await this.calistir(
          `document.body.innerText.includes(${JSON.stringify(metin)})`,
        );
        if (varMi) return true;
        await bekle(80);
      }
      const govde = await this.calistir('document.body.innerText.slice(0, 600)');
      throw new Error(`Metin bulunamadı: "${metin}"\nSayfada görünen:\n${govde}`);
    },

    /**
     * Çalışan CSS geçişleri/animasyonları bitene kadar bekler.
     *
     * Diyaloglar ve alt tabakalar açılırken saydamlıkla giriyor; bekleme
     * koşulu (metin/seçici) sağlandığı anda kare alınınca ekran görüntüsü
     * YARI SAYDAM çıkıyor ve tasarım "soluk" görünüyordu. Hata sanılıp
     * aranan bir şeydi.
     */
    async animasyonBitsin(zamanAsimi = 1200) {
      const son = Date.now() + zamanAsimi;
      while (Date.now() < son) {
        const calisan = await this.calistir(
          'document.getAnimations().filter((a) => a.playState === "running").length',
        );
        if (!calisan) return true;
        await bekle(60);
      }
      return false;
    },

    async ekranGoruntusu(dosyaYolu, { tamSayfa = false } = {}) {
      mkdirSync(dosyaYolu.replace(/\/[^/]+$/, ''), { recursive: true });
      const s = await gonder('Page.captureScreenshot', {
        format: 'png',
        captureBeyondViewport: tamSayfa,
      });
      writeFileSync(dosyaYolu, Buffer.from(s.data, 'base64'));
      return dosyaYolu;
    },

    /**
     * GERÇEK fare tıklaması.
     *
     * `element.click()` Radix menülerini açmıyor: bileşen `pointerdown`
     * dinliyor ve sentetik `click` o olayı üretmiyor. Görsel doğrulama bu
     * yüzden menü içeriğine hiç ulaşamıyordu.
     */
    async tikla(x, y) {
      for (const type of ['mousePressed', 'mouseReleased']) {
        await gonder('Input.dispatchMouseEvent', {
          type, x, y, button: 'left', clickCount: 1,
          buttons: type === 'mousePressed' ? 1 : 0,
        });
      }
    },

    async kapat() {
      soket.close();
      surec.kill();
    },
  };
}
