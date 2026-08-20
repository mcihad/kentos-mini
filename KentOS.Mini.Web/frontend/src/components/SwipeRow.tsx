import { useCallback, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { cn } from './utils';
import { haptic } from '../data/haptics';

/** Açılan panelin genişliği — şartname §6.14: 84dp. */
const PANEL = 84;
/** Panelin "açık" sayılması için gereken çekme. */
const ESIK = PANEL * 0.55;

export type SwipeAction = {
  etiket: string;
  ikon: ReactNode;
  calistir: () => void;
  /** Yıkıcı eylem: kırmızı zemin. Tam kaydırma yine de TETİKLEMEZ. */
  yikici?: boolean;
};

/**
 * KAYDIRARAK EYLEM — satırın altından çıkan hızlı işlemler.
 *
 * <p>
 * Telefonda bir liste satırını yana kaydırmak, o kayda ait en sık işlemi
 * açar; bu hareket native listelerin temel dili. Uygulamada karşılığı yoktu:
 * her eylem için satırı açıp detaya girmek gerekiyordu.
 * </p>
 *
 * <h4>Tam kaydırma yıkıcı işlemi TETİKLEMEZ</h4>
 * <p>
 * Şartname §6.14 bunu açıkça yasaklıyor ve sebebi şu: parmağın hızlanıp
 * ekranı geçmesi kaza değil, sık. Panel açılır, eylem ancak <b>düğmeye
 * dokununca</b> çalışır. Yıkıcı eylemin ayrıca kendi onay penceresi olur.
 * </p>
 *
 * <h4>Neden React durumu her karede güncellenmiyor</h4>
 * <p>
 * Sürükleme <c>transform</c>'a doğrudan yazılıp <c>requestAnimationFrame</c>'e
 * bağlanıyor. Her hareket olayında <c>setState</c> çağırmak listedeki bütün
 * satırları yeniden çizdiriyordu.
 * </p>
 *
 * <p>
 * Klavye ve ekran okuyucu yolu bozulmaz: eylemler panelde gerçek
 * <c>&lt;button&gt;</c>, satırın kendisi de dokunulabilir kalır. Masaüstünde
 * (fare) hareket hiç bağlanmaz — orada satır eylemleri zaten görünür.
 * </p>
 */
export function SwipeRow({
  eylemler,
  children,
  className,
}: {
  /** En fazla iki eylem: fazlası panelde okunmuyor. */
  eylemler: SwipeAction[];
  children: ReactNode;
  className?: string;
}) {
  const govdeRef = useRef<HTMLDivElement>(null);
  const basim = useRef<{ x: number; y: number; acikti: boolean } | null>(null);
  const kaydirma = useRef(0);
  const kare = useRef<number | null>(null);
  const esikGecildi = useRef(false);
  const [acik, setAcik] = useState(false);

  const panelGenisligi = Math.min(eylemler.length, 2) * PANEL;

  const ciz = useCallback(() => {
    kare.current = null;
    const g = govdeRef.current;
    if (!g) return;
    g.style.transform = kaydirma.current ? `translate3d(${-kaydirma.current}px, 0, 0)` : '';
  }, []);

  const kareyeBagla = useCallback(() => {
    if (kare.current == null) kare.current = requestAnimationFrame(ciz);
  }, [ciz]);

  const yerlestir = useCallback((acikMi: boolean) => {
    const g = govdeRef.current;
    kaydirma.current = acikMi ? panelGenisligi : 0;
    if (g) {
      g.style.transition = 'transform 220ms cubic-bezier(0.22, 1, 0.36, 1)';
      g.style.transform = acikMi ? `translate3d(${-panelGenisligi}px, 0, 0)` : '';
      window.setTimeout(() => { if (g) g.style.transition = ''; }, 240);
    }
    setAcik(acikMi);
  }, [panelGenisligi]);

  if (eylemler.length === 0) return <>{children}</>;

  return (
    <div className={cn('relative overflow-hidden', className)}>
      {/*
        PANEL SATIRIN ALTINDA DURUYOR, yanında değil: gövde üstünden kayınca
        ortaya çıkıyor. Akışa girseydi satır genişliği değişir ve kapalıyken
        de yer kaplardı.
      */}
      <div
        className="absolute inset-y-0 right-0 flex"
        aria-hidden={!acik}
        style={{ width: panelGenisligi }}
      >
        {eylemler.slice(0, 2).map((e) => (
          <button
            key={e.etiket}
            type="button"
            tabIndex={acik ? 0 : -1}
            onClick={() => {
              yerlestir(false);
              haptic(e.yikici ? 'uyari' : 'basari');
              e.calistir();
            }}
            className={cn(
              'flex w-[84px] flex-col items-center justify-center gap-1 text-2xs font-semibold transition-colors',
              e.yikici
                ? 'bg-(--st-no) text-white'
                : 'bg-brand text-on-brand',
            )}
          >
            {e.ikon}
            {e.etiket}
          </button>
        ))}
      </div>

      <div
        ref={govdeRef}
        className="relative touch-pan-y bg-surface"
        onPointerDown={(ev) => {
          if (ev.pointerType === 'mouse') return;
          basim.current = { x: ev.clientX, y: ev.clientY, acikti: acik };
          esikGecildi.current = false;
        }}
        onPointerMove={(ev) => {
          const b = basim.current;
          if (!b) return;

          const dx = b.x - ev.clientX;
          const dy = Math.abs(ev.clientY - b.y);

          // Dikey baskınsa bu bir kaydırma değil, listede gezinme.
          if (dy > Math.abs(dx)) {
            basim.current = null;
            return;
          }

          const taban = b.acikti ? panelGenisligi : 0;
          kaydirma.current = Math.max(0, Math.min(taban + dx, panelGenisligi));
          kareyeBagla();

          if (!esikGecildi.current && kaydirma.current >= ESIK) {
            esikGecildi.current = true;
            haptic('esik');
          }
        }}
        onPointerUp={() => {
          if (!basim.current) return;
          basim.current = null;
          yerlestir(kaydirma.current >= ESIK);
        }}
        onPointerCancel={() => {
          basim.current = null;
          yerlestir(false);
        }}
      >
        {children}
      </div>
    </div>
  );
}
