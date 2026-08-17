import * as ToastRadix from '@radix-ui/react-toast';
import { ChevronRight, CircleAlert, CircleCheck, Info, TriangleAlert } from 'lucide-react';
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';

type Tur = 'basari' | 'hata' | 'uyari' | 'bilgi';

/** `bildir`in dördüncü, isteğe bağlı parametresi. */
export type ToastOptions = {
  /**
   * Şeride dokununca çalışacak iş — genelde bir gezinme.
   *
   * Yol yerine geri çağrı alınmasının sebebi konum: `ToastSaglayici`
   * router'ın DIŞINDA (`main.tsx`), yani burada `useNavigate` yok. Çağıran
   * zaten router'ın içinde ve hedefi de o biliyor.
   */
  eylem?: () => void;
  /** Ekran okuyucuya ve ipucuna yazılan eylem adı. */
  eylemEtiketi?: string;
};

type Bildirim = {
  id: number;
  tur: Tur;
  baslik: string;
  aciklama?: string;
} & ToastOptions;

const gorunum: Record<Tur, { ikon: typeof Info; renk: string; zemin: string }> = {
  basari: { ikon: CircleCheck, renk: '--st-ok', zemin: '--st-ok-bg' },
  hata: { ikon: CircleAlert, renk: '--st-no', zemin: '--st-no-bg' },
  uyari: { ikon: TriangleAlert, renk: '--st-wait', zemin: '--st-wait-bg' },
  bilgi: { ikon: Info, renk: '--st-live', zemin: '--st-live-bg' },
};

/**
 * Bildirim kabuğunun sınıfları.
 *
 * Animasyon ve kaydırma kuralları `globals.css` → `.bildirim` içinde. Önce
 * Tailwind'in `animate-in` / `animate-out` yardımcıları yazılıydı ama bu
 * projede `tailwindcss-animate` YOK: sınıflar hiçbir kurala karşılık
 * gelmiyordu, yani bildirim animasyonsuz belirip animasyonsuz kayboluyordu.
 */
const KABUK =
  'bildirim relative flex items-start gap-2.5 rounded-card border border-border ' +
  'bg-surface p-3 shadow-2';

const Baglam = createContext<{
  bildir: (
    tur: Tur,
    baslik: string,
    aciklama?: string,
    secenek?: ToastOptions,
  ) => void;
} | null>(null);

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [liste, setListe] = useState<Bildirim[]>([]);

  /**
   * Kaydırma yönü ekrana göre değişir.
   *
   * Masaüstünde bildirim sağ altta duruyor; onu sağa itmek doğal. Mobilde
   * ise alt çubuğun hemen üstünde ve tam genişlikte: orada doğal hareket
   * AŞAĞI. Tek yön dayatmak, mobilde kullanıcının denediği hareketin hiçbir
   * şey yapmaması demekti.
   */
  const [yon, setYon] = useState<'right' | 'down'>('right');

  useEffect(() => {
    const sorgu = window.matchMedia('(min-width: 768px)');
    const uygula = () => setYon(sorgu.matches ? 'right' : 'down');
    uygula();
    sorgu.addEventListener('change', uygula);
    return () => sorgu.removeEventListener('change', uygula);
  }, []);

  const bildir = useCallback(
    (tur: Tur, baslik: string, aciklama?: string, secenek?: ToastOptions) => {
      setListe((l) => [
        ...l,
        { id: Date.now() + Math.random(), tur, baslik, aciklama, ...secenek },
      ]);
    },
    [],
  );

  const deger = useMemo(() => ({ bildir }), [bildir]);

  /**
   * Parmağın bastığı nokta — kaydırmayı tıklamadan ayırmak için.
   *
   * Şeridi kapatmak için yana/aşağı kaydırmak, `pointerup` aynı öğede
   * bittiği için farede TIKLAMA da üretiyor. Eşiksiz bırakılsaydı kullanıcı
   * bildirimi kapatmak isterken hedefine gitmiş olurdu — bu, otomatik
   * yönlendirmeden bile beter: kullanıcı kaydırmayı reddetme hareketi olarak
   * yapıyor.
   */
  const basim = useRef<{ x: number; y: number } | null>(null);

  return (
    <Baglam.Provider value={deger}>
      <ToastRadix.Provider swipeDirection={yon} duration={4500} swipeThreshold={56}>
        {children}

        {liste.map((b) => {
          const g = gorunum[b.tur];
          const Ikon = g.ikon;
          const tiklanir = typeof b.eylem === 'function';
          const etiket = b.eylemEtiketi ?? 'Aç';

          return (
            <ToastRadix.Root
              key={b.id}
              onOpenChange={(a) => !a && setListe((l) => l.filter((x) => x.id !== b.id))}
              className={KABUK}
            >
              <span
                className="mt-0.5 grid h-[22px] w-[22px] shrink-0 place-items-center rounded-full"
                style={{ background: `var(${g.zemin})`, color: `var(${g.renk})` }}
              >
                <Ikon size={13} strokeWidth={2.2} />
              </span>
              <span className="min-w-0 flex-1">
                <ToastRadix.Title asChild>
                  <span className="block text-sm font-semibold">{b.baslik}</span>
                </ToastRadix.Title>
                {b.aciklama && (
                  <ToastRadix.Description asChild>
                    <span className="mt-0.5 block text-sm text-text-2">{b.aciklama}</span>
                  </ToastRadix.Description>
                )}
              </span>

              {tiklanir && (
                <>
                  {/* Gidilebileceğini söyleyen tek işaret. */}
                  <ChevronRight
                    aria-hidden
                    size={16}
                    className="mt-0.5 shrink-0 self-center text-text-3"
                  />

                  {/*
                    TÜM ŞERİDİ KAPLAYAN DÜĞME.

                    `Root`a `onClick` asmak yerine gerçek bir düğme
                    konuyor: şerit `role="status"` taşıyor ve tıklanabilir
                    bir durum metni ne klavyeyle ne ekran okuyucuyla
                    erişilebilir olurdu. `Toast.Action` ayrıca `altText` ile
                    eylemi ekran okuyucuya duyuruyor ve tıklandığında şeridi
                    kendisi kapatıyor.
                  */}
                  <ToastRadix.Action asChild altText={etiket}>
                    <button
                      type="button"
                      aria-label={`${b.baslik} — ${etiket}`}
                      title={etiket}
                      className="absolute inset-0 rounded-card focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-2"
                      onPointerDown={(e) => {
                        basim.current = { x: e.clientX, y: e.clientY };
                      }}
                      onClick={(e) => {
                        const b0 = basim.current;
                        basim.current = null;
                        // Kaydırarak kapatma da tıklama üretir; 10px'ten
                        // uzağa giden hareket gezinme sayılmaz.
                        if (b0 && Math.hypot(e.clientX - b0.x, e.clientY - b0.y) > 10) {
                          e.preventDefault();
                          return;
                        }
                        b.eylem?.();
                      }}
                    />
                  </ToastRadix.Action>
                </>
              )}
            </ToastRadix.Root>
          );
        })}

        {/* Mobilde tabbar'ın üstünde kalsın */}
        <ToastRadix.Viewport className="fixed bottom-[76px] right-3 z-500 flex w-[min(360px,calc(100vw-24px))] flex-col gap-2 md:bottom-4 md:right-4" />
      </ToastRadix.Provider>
    </Baglam.Provider>
  );
}

export function useToast() {
  const b = useContext(Baglam);
  if (!b) throw new Error('useToast, ToastSaglayici içinde kullanılmalı.');
  return b;
}
