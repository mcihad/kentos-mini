import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { ScrollRestoration } from './shell/ScrollRestoration';
import App from './App';
import { SessionProvider } from './auth/SessionProvider';
import './styles/globals.css';
import { ToastProvider } from './components/Toast';
import { ThemeProvider } from './theme/ThemeProvider';
import { startInstallListener, registerServiceWorker } from './pwa/install';
import { applyDocumentIdentity, currentInstitution, loadInstitution } from './institution/institution';
import { markaPaletiniUygula, KURUM_TEMA_OLAYI } from './theme/palettes';

/*
 * KURUM BİLGİSİ — koda YAZILMAZ, sunucudan gelir.
 *
 * Uygulama başka belediyelere verilecek; kurum adı, amblem ve kurumsal
 * renkler derlemeye gömülseydi her kurum için ayrı bir ön yüz derlemesi
 * gerekirdi. Kaynak: `GET /api/v2/institution`.
 *
 * İKİ AŞAMALI: önce ÖNBELLEKTEKİ değer senkron uygulanır (ilk kare doğru
 * renkle boyansın, ağ beklenmesin), sonra sunucudan tazelenir. Tazelenme
 * bir olayla tema motoruna duyurulur.
 */
markaPaletiniUygula(currentInstitution().marka);
applyDocumentIdentity(currentInstitution());

void loadInstitution().then((kurum) => {
  markaPaletiniUygula(kurum.marka);
  applyDocumentIdentity(kurum);
  window.dispatchEvent(new Event(KURUM_TEMA_OLAYI));
});

const sorguIstemcisi = new QueryClient({
  defaultOptions: {
    queries: {
      // Takvim çok kullanıcılı: 30 sn sonra bayat say, pencereye dönünce tazele.
      staleTime: 30_000,
      gcTime: 5 * 60_000,
      refetchOnWindowFocus: true,
      retry: (deneme, hata: unknown) => {
        // Yetki/doğrulama hatalarını tekrar denemek anlamsız.
        const status = (hata as { status?: number })?.status;
        if (status && status >= 400 && status < 500) return false;
        return deneme < 2;
      },
    },
  },
});

/*
 * PWA kurulumu.
 *
 * Service worker BAĞIMSIZ kaydediliyor — bildirim izni istenmeden de
 * çevrimdışı kabuk çalışsın diye. `beforeinstallprompt` sayfa yüklenirken
 * bir kez tetiklendiği için dinleyici React'ten ÖNCE kurulmalı.
 */
startInstallListener();
void registerServiceWorker();

/**
 * Açılış perdesini kaldırır.
 *
 * Perde `index.html` içinde, ilk boyamada hazır. React bağlandıktan sonra
 * kaldırılıyor; aksi hâlde ana ekrandan açılan uygulamada beyaz bir boşluk
 * görünüyor ve "çöktü" izlenimi veriyordu.
 */
function acilisPerdesiniKaldir() {
  const perde = document.getElementById('acilis');
  if (!perde) return;
  perde.classList.add('gizle');
  perde.addEventListener('transitionend', () => perde.remove(), { once: true });
  // Geçiş olayı gelmezse (hareket azaltma açıkken) yine de kaldır.
  setTimeout(() => perde.remove(), 600);
}

/*
  Tarayıcının kendi kaydırma geri yüklemesi KAPALI: bizimkiyle yarışıyor ve
  ikisi aynı anda farklı noktalara zıplatıyordu. Konumu `KaydirmaGeriYukle`
  yönetiyor — o, içerik gelene kadar tekrar deniyor.
*/
if ('scrollRestoration' in history) history.scrollRestoration = 'manual';

createRoot(document.getElementById('kok')!).render(
  <StrictMode>
    <QueryClientProvider client={sorguIstemcisi}>
      <ThemeProvider>
        <ToastProvider>
        {/* basename: kod içindeki yollar design.md §6'daki gibi kalır,
            adres çubuğunda /yeni öneki görünür. */}
        <BrowserRouter basename="/">
          {/* Listeden bir kayda girip geri dönünce kaldığın yer korunur. */}
          <ScrollRestoration />
          <SessionProvider>
            <App />
          </SessionProvider>
        </BrowserRouter>
        </ToastProvider>
      </ThemeProvider>
    </QueryClientProvider>
  </StrictMode>,
);

// İlk boyamadan sonra perdeyi kaldır.
requestAnimationFrame(() => requestAnimationFrame(acilisPerdesiniKaldir));
