import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { PERMISSION } from '../src/components/permissions';
import { render, type RenderResult } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import { ToastProvider } from '../src/components/Toast';
import { ThemeProvider } from '../src/theme/ThemeProvider';
import type { Me } from '../src/auth/SessionProvider';

/**
 * Ekran testleri için ortak kurulum.
 *
 * Gerçek bileşen ağacı render edilir; yalnızca `fetch` taklit edilir. Amaç
 * "bileşen çağrıldı mı" değil, KULLANICININ EKRANDA NE GÖRDÜĞÜ.
 */

export const SAHTE_BEN: Me = {
  id: 1,
  kullaniciAdi: 'admin',
  ad: 'Sistem',
  soyad: 'Yöneticisi',
  tamAd: 'Sistem Yöneticisi',
  unvan: 'Yönetici',
  eposta: 'admin@ornek.test',
  birimId: 1,
  birimAd: 'Özel Kalem Müdürlüğü',
  roller: ['Admin'],
  yetkiler: ['Ajanda', 'OzelKalem'],
  // Ekran testlerindeki kullanıcı Admin: TÜM izinler açık. Ekranların izin
  // kısıtını burada değil `izin.test.tsx` içinde sınıyoruz; bu listeyi eksik
  // bırakmak, ilgisiz onlarca ekran testini "düğme yok" diye düşürürdü.
  izinler: Object.values(PERMISSION),
};

/**
 * Listeyi sunucunun sayfalı zarfına sarar.
 *
 * v2'de hiçbir liste ucu çıplak dizi döndürmüyor; test verisi de aynı şekli
 * taşımalı, yoksa testler gerçekte olmayan bir sözleşmeyi doğrular.
 */
export function sayfali<T>(veriler: T[], toplam = veriler.length) {
  const boyut = 50;
  return {
    veriler,
    sayfa: 1,
    boyut,
    toplam,
    toplamSayfa: Math.ceil(toplam / boyut),
    oncekiVar: false,
    sonrakiVar: toplam > boyut,
  };
}

/** Yol → yanıt eşlemesi ile `fetch`i taklit eder. */
export function fetchTaklit(yanitlar: Record<string, unknown>) {
  return vi.fn(async (girdi: RequestInfo | URL) => {
    const url = typeof girdi === 'string' ? girdi : girdi.toString();
    const yol = url.replace(/^.*\/api\/v2/, '');

    // En uzun eşleşen anahtar kazanır: `/talep` ile `/talep/sayi` çakışmasın.
    const anahtar = Object.keys(yanitlar)
      .filter((a) => yol === a || yol.startsWith(`${a}?`) || yol.startsWith(`${a}/`))
      .sort((a, b) => b.length - a.length)[0];

    if (!anahtar) {
      return new Response(
        JSON.stringify({ title: 'Bulunamadı', status: 404, detail: `Taklit yok: ${yol}` }),
        { status: 404, headers: { 'Content-Type': 'application/json' } },
      );
    }

    return new Response(JSON.stringify(yanitlar[anahtar]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  });
}

export function kur(
  ekran: React.ReactNode,
  { yol = '/', rotaYolu }: { yol?: string; rotaYolu?: string } = {},
): RenderResult {
  const istemci = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });

  return render(
    <QueryClientProvider client={istemci}>
      <ThemeProvider>
        <ToastProvider>
          <MemoryRouter initialEntries={[yol]}>
            {rotaYolu ? (
              <Routes>
                <Route path={rotaYolu} element={ekran} />
              </Routes>
            ) : (
              ekran
            )}
          </MemoryRouter>
        </ToastProvider>
      </ThemeProvider>
    </QueryClientProvider>,
  );
}

/** Oturum bağlamını sahte kullanıcıyla değiştirir. */
export function oturumTaklit(me: Me | null = SAHTE_BEN) {
  vi.doMock('../src/auth/SessionProvider', async () => {
    const gercek = await vi.importActual<typeof import('../src/auth/SessionProvider')>(
      '../src/auth/SessionProvider',
    );
    return {
      ...gercek,
      useSession: () => ({
        me,
        ready: true,
        signIn: vi.fn(),
        signOut: vi.fn(),
        hasPolicy: (p: string) => me?.yetkiler.includes(p) ?? false,
        // Gerçek bağlamdaki gibi KARAMSAR (liste yoksa izin yok) ve çoklu
        // izni VEYA ile değerlendirir. Tek metin beklemek, dizi ilan eden
        // menü öğelerini sessizce gizliyordu.
        hasPermission: (i: string | string[]) =>
          [i].flat().some((x) => me?.izinler?.includes(x) ?? false),
      }),
    };
  });
}
