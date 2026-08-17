import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwind from '@tailwindcss/vite';

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
  plugins: [tailwind(), react()],
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
