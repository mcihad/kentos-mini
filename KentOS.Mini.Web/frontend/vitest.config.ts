import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

/**
 * Test yapılandırması ana `vite.config.ts`'ten AYRI.
 *
 * Ana yapılandırma `build.outDir`'i sunucunun `wwwroot/yeni` dizinine
 * çeviriyor ve `emptyOutDir: true` taşıyor; test koşumu onu yanlışlıkla
 * tetiklerse yayınlanmış uygulamayı siler.
 */
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./test/setup.ts'],
    include: ['test/**/*.test.{ts,tsx}'],
  },
});
