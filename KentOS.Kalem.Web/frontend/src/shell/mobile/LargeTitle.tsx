import { createContext, useContext, useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';

/**
 * BÜYÜK BAŞLIK → APPBAR DEVRİ (design_new §5.2).
 *
 * Native kök ekranlarda başlık appbar'da DEĞİL, içeriğin en üstünde ve büyük
 * puntoyla durur; kullanıcı kaydırınca yukarı çıkar ve appbar onu devralır.
 * Bu iki şey birden yapıyor: ekran açıldığında "neredeyim" sorusu güçlü bir
 * tipografiyle cevaplanıyor, kaydırırken de aynı cevap küçük bir satır olarak
 * elde kalıyor.
 *
 * Eşik 34px: daha küçük bir değerde başlık, listeyi hafifçe iteklerken bile
 * titriyor; daha büyüğünde devir geç kalıyor ve iki başlık bir an birlikte
 * görünüyor.
 *
 * Devir SAYFA ÖLÇÜSÜNDEN okunuyor, kaydırma kabından değil: bu uygulamada
 * kaydırma belge düzeyinde (iç kap `sticky` appbar'ı bozuyordu).
 */
type Baglam = {
  /** Ekran büyük başlık bildirmiş mi? Appbar buna göre başlığını gizler. */
  buyukVar: boolean;
  /** Eşik aşıldı mı? */
  devredildi: boolean;
  bildir: (v: boolean) => void;
};

const Ctx = createContext<Baglam>({ buyukVar: false, devredildi: true, bildir: () => {} });

const ESIK = 34;

export function LargeTitleProvider({ children }: { children: ReactNode }) {
  const [buyukVar, setBuyukVar] = useState(false);
  const [devredildi, setDevredildi] = useState(false);

  useEffect(() => {
    if (!buyukVar) {
      setDevredildi(true);
      return;
    }
    const oku = () => setDevredildi(window.scrollY > ESIK);
    oku();
    window.addEventListener('scroll', oku, { passive: true });
    return () => window.removeEventListener('scroll', oku);
  }, [buyukVar]);

  return (
    <Ctx.Provider value={{ buyukVar, devredildi, bildir: setBuyukVar }}>
      {children}
    </Ctx.Provider>
  );
}

export function useLargeTitle() {
  return useContext(Ctx);
}

/**
 * Ekranın en üstüne konur; mobilde büyük başlığı çizer, masaüstünde hiçbir şey
 * yapmaz (orada appbar zaten kalıcı başlığı taşıyor).
 */
export function LargeTitle({
  baslik,
  altBaslik,
}: {
  baslik: string;
  altBaslik?: ReactNode;
}) {
  const { bildir } = useLargeTitle();
  const bildirRef = useRef(bildir);
  bildirRef.current = bildir;

  useEffect(() => {
    bildirRef.current(true);
    return () => bildirRef.current(false);
  }, []);

  return (
    <div className="pb-2 md:hidden">
      <h1 className="font-display text-3xl font-bold leading-[1.15] tracking-[var(--track-d)] text-ink">
        {baslik}
      </h1>
      {altBaslik && <div className="mt-1.5 text-sm text-ink-3">{altBaslik}</div>}
    </div>
  );
}
