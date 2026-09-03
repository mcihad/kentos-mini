import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

afterEach(() => {
  cleanup();
  localStorage.clear();
});

/**
 * jsdom `matchMedia` sağlamıyor; tema sağlayıcısı onu okuyor.
 *
 * Varsayılan olarak "açık tema" döndürür. **Genişlik sorguları MASAÜSTÜ
 * döndürür**: ekranlar artık mobil ve masaüstü için ayrı ağaç çiziyor
 * (`useMasaustuMu`) ve testler tablo, sütun başlığı, çip şeridi gibi
 * masaüstü yapılarını denetliyor. Hepsi `false` dönseydi testler sessizce
 * mobil dalı ölçer, masaüstü hiç sınanmazdı.
 *
 * jsdom penceresi 1024px; yani bu, gerçek ölçüyle de tutarlı.
 */
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (sorgu: string) => ({
    matches: /min-width/.test(sorgu),
    media: sorgu,
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }),
});

/** Firebase'i testte hiç yüklemeyiz — ağ ve service worker gerektirir. */
vi.mock('../src/notifications/fcm', () => ({
  bildirimDurumu: async () => 'desteklenmiyor',
  webJetonuKaydet: async () => 'sahte-jeton',
  webJetonuSil: async () => undefined,
  jetonuTazele: async () => undefined,
  onForegroundMessage: () => () => undefined,
  notificationPath: () => null,
}));
