import { useCallback, useRef, useState } from 'react';
import { RefreshCw } from 'lucide-react';
import { cn } from './utils';
import { haptic } from '../data/haptics';

/** Eşik: parmağın bu kadar aşağı çekilmesi yenilemeyi tetikler. */
const ESIK = 68;
/** Lastik direnci — 68px'lik eşiğe ulaşmak için ~2 katı hareket gerekir. */
const DIRENC = 0.45;

/**
 * AŞAĞI ÇEKİP BIRAK — listeyi yenilemenin native yolu.
 *
 * <p>
 * Telefonda listenin tepesinden aşağı çekmek "yenile" demektir; bu hareket o
 * kadar yerleşmiş ki, karşılığı olmayan bir liste "donmuş" hissettiriyor.
 * Uygulamada yenilemenin tek yolu ekrandan çıkıp geri gelmekti.
 * </p>
 *
 * <h4>Neden React durumu her karede güncellenmiyor</h4>
 * <p>
 * Çekme mesafesi doğrudan öğenin <c>transform</c>'una yazılıyor ve
 * <c>requestAnimationFrame</c>'e bağlanıyor. Her <c>pointermove</c>'da
 * <c>setState</c> çağırmak uzun listede tüm satırları yeniden çizdiriyor ve
 * hareket takılıyordu — aynı tuzağa alt tabakanın sürükleme kodunda da
 * düşülmüştü (<c>kaydirmaKapat.ts</c>).
 * </p>
 *
 * <h4>Ne zaman devreye girmez</h4>
 * <ul>
 *   <li>Liste tepede değilse (<c>scrollTop &gt; 0</c>) — kaydırmayı çalmaz.</li>
 *   <li>Fare ile — masaüstünde bu hareket yok, metin seçimini bozardı.</li>
 *   <li>Yatay hareket baskınsa — çip şeridi kaydırmak yenilemeyi tetiklemez.</li>
 * </ul>
 */
export function PullToRefresh({
  yenile,
  yenileniyor,
  children,
  className,
}: {
  /** Yenileme işi — söz döndürürse gösterge o bitene kadar döner. */
  yenile: () => void | Promise<unknown>;
  /** Dışarıdan gelen yükleme durumu (TanStack `isFetching` gibi). */
  yenileniyor?: boolean;
  children: React.ReactNode;
  className?: string;
}) {
  const kapRef = useRef<HTMLDivElement>(null);
  const govdeRef = useRef<HTMLDivElement>(null);
  const basim = useRef<{ y: number; x: number } | null>(null);
  const mesafe = useRef(0);
  const kare = useRef<number | null>(null);
  const esikGecildi = useRef(false);
  const [calisiyor, setCalisiyor] = useState(false);

  const gorunum = calisiyor || yenileniyor;

  const ciz = useCallback(() => {
    kare.current = null;
    const g = govdeRef.current;
    if (!g) return;
    g.style.transform = mesafe.current > 0 ? `translate3d(0, ${mesafe.current}px, 0)` : '';
  }, []);

  const kareyeBagla = useCallback(() => {
    if (kare.current == null) kare.current = requestAnimationFrame(ciz);
  }, [ciz]);

  /** Kaydırma kabı: en yakın kaydırılabilir ata ya da belgenin kendisi. */
  const tepedeMi = () => {
    const el = kapRef.current;
    if (!el) return false;
    // Sayfa gövdesi kaydırılıyor (kabuk `overflow` kullanmıyor).
    return (document.scrollingElement?.scrollTop ?? window.scrollY) <= 0;
  };

  function basla(e: React.PointerEvent<HTMLDivElement>) {
    if (e.pointerType === 'mouse' || gorunum || !tepedeMi()) return;
    basim.current = { y: e.clientY, x: e.clientX };
    esikGecildi.current = false;
  }

  function hareket(e: React.PointerEvent<HTMLDivElement>) {
    const b = basim.current;
    if (!b) return;

    const dy = e.clientY - b.y;
    const dx = e.clientX - b.x;

    // Yatay baskınsa bu bir kaydırma değil, şerit gezinmesi.
    if (Math.abs(dx) > Math.abs(dy)) {
      basim.current = null;
      return;
    }
    if (dy <= 0) {
      mesafe.current = 0;
      kareyeBagla();
      return;
    }

    mesafe.current = Math.min(dy * DIRENC, ESIK * 1.6);
    kareyeBagla();

    // Eşiğe VARIŞTA bir kez titre: parmak kalkmadan "bırakırsan olur" demek.
    if (!esikGecildi.current && mesafe.current >= ESIK) {
      esikGecildi.current = true;
      haptic('esik');
    }
  }

  async function bitir() {
    if (!basim.current) return;
    basim.current = null;

    const tetikle = mesafe.current >= ESIK;
    mesafe.current = 0;

    const g = govdeRef.current;
    if (g) {
      // Yerine YAYLANARAK döner; anında sıçramak hareketi kesiyordu.
      g.style.transition = 'transform 260ms cubic-bezier(0.22, 1, 0.36, 1)';
      g.style.transform = '';
      window.setTimeout(() => { if (g) g.style.transition = ''; }, 280);
    }

    if (!tetikle) return;

    setCalisiyor(true);
    try {
      await yenile();
      haptic('basari');
    } finally {
      setCalisiyor(false);
    }
  }

  return (
    <div
      ref={kapRef}
      className={cn('relative', className)}
      onPointerDown={basla}
      onPointerMove={hareket}
      onPointerUp={() => void bitir()}
      onPointerCancel={() => void bitir()}
    >
      {/*
        GÖSTERGE AKIŞTA DEĞİL: içeriğin arkasında duruyor ve içerik onun
        üstünden kayarak açılıyor. Akışa girseydi liste her çekişte aşağı
        itilir, düzen yeniden hesaplanırdı.
      */}
      <div
        aria-hidden={!gorunum}
        className={cn(
          'pointer-events-none absolute inset-x-0 -top-1 z-0 flex justify-center',
          'transition-opacity duration-200',
          gorunum ? 'opacity-100' : 'opacity-0',
        )}
      >
        <span className="grid size-9 place-items-center rounded-full border border-line bg-surface text-brand shadow-2">
          <RefreshCw size={16} className={gorunum ? 'animate-spin' : undefined} />
        </span>
      </div>

      <div ref={govdeRef} className="relative z-10 touch-pan-y">
        {children}
      </div>
    </div>
  );
}
