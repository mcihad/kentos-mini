import { rmSync, readdirSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { defineConfig, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import tailwind from '@tailwindcss/vite';

/** Üretilmiş varlıkların TEK klasörü — `build.assetsDir` ile aynı olmalı. */
const VARLIK_DIZINI = 'uygulama';

/**
 * ESKİ DERLEME ÇIKTILARINI SİLER.
 *
 * <p>
 * `emptyOutDir` kapalı olmak ZORUNDA (çıktı doğrudan `wwwroot`a yazılıyor ve
 * orada `uploads` altında gerçek belgeler var), ama bunun bedeli her
 * derlemenin yeni karma adlı dosyalar bırakması ve eskilerin hiç
 * temizlenmemesiydi.
 * </p>
 *
 * <p>
 * <b>Ölçüldü:</b> <c>wwwroot/uygulama</c> altında <b>176 dosya · 293 MB</b>
 * birikmişti; güncel <c>index.html</c> bunlardan yalnızca <b>5</b>'ini
 * kullanıyordu. Klasör <c>.gitignore</c>'da olduğu için depoya girmiyor ama
 * <c>dotnet publish</c> onu olduğu gibi taşıyor — yayın paketi 293 MB ölü
 * JavaScript taşıyordu.
 * </p>
 *
 * <p>
 * <b>Yalnızca varlık klasörü siliniyor</b>, <c>wwwroot</c>un kendisi değil:
 * o klasörün içeriğinin tamamı üretilmiş çıktı. Yol beklenen sonla
 * bitmiyorsa silme YAPILMAZ — yanlış bir yapılandırma yüzünden kullanıcı
 * belgelerini silmek, biriken çöpten kat kat kötü.
 * </p>
 */
function eskiVarliklariTemizle(): Plugin {
  return {
    name: 'eski-varliklari-temizle',
    apply: 'build',
    buildStart() {
      const dizin = resolve(__dirname, '..', 'wwwroot', VARLIK_DIZINI);

      // GÜVENLİK KAPISI: beklenen yol değilse hiçbir şey silme.
      if (!dizin.endsWith(`wwwroot/${VARLIK_DIZINI}`) || !existsSync(dizin)) return;

      const adet = readdirSync(dizin).length;
      rmSync(dizin, { recursive: true, force: true });
      if (adet > 0) console.log(`  eski varlıklar silindi: ${adet} dosya`);
    },
  };
}

/**
 * Uygulama `/yeni` altında yayınlanır ve çıktı doğrudan sunucunun
 * `wwwroot/yeni` dizinine yazılır; `dotnet publish` bunu olduğu gibi taşır.
 *
 * Geliştirmede API'ye VEKİL (proxy) üzerinden gidilir. Sunucuya CORS eklemek
 * gerekmiyor: CORS, canlı mobil API'yi de taşıyan boru hattına yalnızca
 * geliştirme kolaylığı için politika eklemek olurdu. Vekil ayrıca üretimdeki
 * aynı-köken durumunu birebir taklit eder.
 */
export default defineConfig({
  // Tailwind 4 VITE eklentisiyle. PostCSS boru hattı da çalışıyor ama Vite
  // eklentisi CSS'i doğrudan Vite'ın grafiğine bağlıyor: HMR'da tam yeniden
  // derleme yerine yalnızca değişen katman güncelleniyor.
  plugins: [eskiVarliklariTemizle(), tailwind(), react()],
  base: '/',
  build: {
    outDir: '../wwwroot',
    // ÖNEMLİ: `emptyOutDir` KAPALI. Uygulama artık kökten yayınlanıyor ve
    // çıktı doğrudan `wwwroot`a yazılıyor; açık bırakmak `wwwroot/uploads`
    // altındaki GERÇEK belgeleri (özgeçmiş, talep eki, etkinlik fotoğrafı)
    // her derlemede silerdi.
    emptyOutDir: false,
    // Vite varlıkları kendi klasörüne yazsın: `wwwroot/assets` eski MVC'nin
    // dosyalarını taşıyor, ikisini karıştırmak temizliği imkânsız kılardı.
    assetsDir: 'uygulama',
    sourcemap: false,
    rolldownOptions: {
      output: {
        /**
         * Firebase'i ayrı bir yığına ayır.
         *
         * Tek dosyada ~580 kB çıkıyordu ve bunun yarısı Firebase. Bildirim
         * SDK'sı yalnızca izin verilmiş tarayıcılarda iş görüyor; ayrı yığın
         * olduğunda ilk boyama onu beklemez. Belediye ağında 3G tabletlerden
         * girilen bir uygulama için bu fark hissedilir.
         */
        manualChunks: (kimlik: string) => {
          if (kimlik.includes('node_modules/firebase') || kimlik.includes('node_modules/@firebase')) {
            return 'firebase';
          }
          if (kimlik.includes('node_modules/react') || kimlik.includes('node_modules/scheduler')) {
            return 'react';
          }
          return undefined;
        },
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5097', changeOrigin: false },
      '/uploads': { target: 'http://localhost:5097', changeOrigin: false },
      '/swagger': { target: 'http://localhost:5097', changeOrigin: false },
    },
  },
});
