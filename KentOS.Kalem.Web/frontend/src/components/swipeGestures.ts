import { useCallback, useRef } from 'react';

/**
 * Bir listeyi TEKERLEK ve PARMAKLA kaydırılabilir yapar.
 *
 * Saat seçici `overflow-y-auto` bir kutuydu, yani teoride zaten kaydırılabilir.
 * Pratikte değildi: Radix Popover içeriğini `react-remove-scroll` ile sarıyor
 * ve panel açıkken tekerlek olaylarını yutuyor; dokunmada da Radix'in işaretçi
 * yakalaması listeyi kilitliyordu. Sonuç: masaüstünde tekerlek, mobilde parmak
 * hiçbir şey yapmıyor, kullanıcı saati bulamıyordu.
 *
 * Bu yüzden iki hareket de AÇIKÇA bağlanıyor:
 *  • `wheel` → `scrollTop`a doğrudan yazılır, olay yukarı çıkmadan durdurulur.
 *  • işaretçi sürükleme → parmak/kalem için; fare sürüklemesi metin seçimiyle
 *    çakıştığı için dışarıda bırakıldı.
 *
 * Klavye yolu bozulmaz: öğeler yine düğme, `Tab` ve `Enter` çalışmaya devam
 * eder.
 */
export function useSwipeGestures<T extends HTMLElement>() {
  const kap = useRef<T | null>(null);
  const suruklemeBasi = useRef<{ y: number; scroll: number } | null>(null);

  const onWheel = useCallback((e: React.WheelEvent<T>) => {
    const el = kap.current;
    if (!el) return;

    const oncekiUst = el.scrollTop;
    el.scrollTop += e.deltaY;

    // Liste HAREKET ETTİYSE olayı tüketiyoruz. Etmediyse (uçtayız) olay
    // yukarı çıksın; aksi hâlde panel kenarında tekerlek tamamen ölürdü.
    if (el.scrollTop !== oncekiUst) {
      e.stopPropagation();
    }
  }, []);

  const onPointerDown = useCallback((e: React.PointerEvent<T>) => {
    if (e.pointerType === 'mouse') return;
    const el = kap.current;
    if (!el) return;
    suruklemeBasi.current = { y: e.clientY, scroll: el.scrollTop };
  }, []);

  const onPointerMove = useCallback((e: React.PointerEvent<T>) => {
    const bas = suruklemeBasi.current;
    const el = kap.current;
    if (!bas || !el) return;
    // Parmak yukarı → liste aşağı: doğal yön.
    el.scrollTop = bas.scroll - (e.clientY - bas.y);
  }, []);

  const bitir = useCallback(() => {
    suruklemeBasi.current = null;
  }, []);

  return {
    kap,
    baglar: {
      onWheel,
      onPointerDown,
      onPointerMove,
      onPointerUp: bitir,
      onPointerCancel: bitir,
    },
  };
}
