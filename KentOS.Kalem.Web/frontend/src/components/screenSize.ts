import { useEffect, useState } from 'react';

/** design_new §5.3 — tek kırılım noktası. */
const MASAUSTU = '(min-width: 768px)';

/**
 * Ekran masaüstü genişliğinde mi?
 *
 * CSS ile çözülemeyen tek durum için var: mobilde ve masaüstünde FARKLI
 * BİLEŞEN render edilmesi gerektiğinde (alt sheet ↔ ortalanmış pencere).
 * Yalnızca sınıf farkı olan her yerde `md:` kullanılır — JavaScript'e
 * taşımak, sunucudan gelen ilk boyamada yanlış tarafı çizmek demek.
 */
export function useIsDesktop(): boolean {
  const [masaustu, setMasaustu] = useState(
    () => typeof window !== 'undefined' && window.matchMedia(MASAUSTU).matches,
  );

  useEffect(() => {
    const mq = window.matchMedia(MASAUSTU);
    const dinle = () => setMasaustu(mq.matches);
    dinle();
    mq.addEventListener('change', dinle);
    return () => mq.removeEventListener('change', dinle);
  }, []);

  return masaustu;
}
