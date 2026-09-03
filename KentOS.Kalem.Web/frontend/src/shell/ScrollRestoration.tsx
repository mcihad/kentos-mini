import { useEffect, useRef } from 'react';
import { useLocation, useNavigationType } from 'react-router-dom';

/** Konum başına kaydırma noktası. Sekme ömrü boyunca yaşar. */
const NOKTALAR = new Map<string, number>();

/**
 * KAYDIRMA KONUMUNU GERİ YÜKLER.
 *
 * <p>
 * Listede aşağı inip bir kayda giriyorsun, geri dönüyorsun ve liste
 * <b>en baştan</b> açılıyor — aradığın satırı bulmak için baştan aşağı
 * kaydırman gerekiyor. Uzun bir ajandada bu, her kayıt için ödenen bir
 * bedel.
 * </p>
 *
 * <h4>Neden tarayıcıya bırakılmıyor</h4>
 * <p>
 * Tarayıcının kendi geri yüklemesi, geri dönüldüğü anda sayfanın ESKİ
 * YÜKSEKLİĞİNDE olmasını varsayar. İstemci tarafı yönlendirmede öyle olmuyor:
 * liste verisi henüz gelmediği için belge kısa, tarayıcı 1400px'e kaydıramıyor
 * ve sessizce tepede bırakıyor. Bu yüzden geri yükleme, <b>içerik büyüdükçe
 * tekrar deneniyor</b>.
 * </p>
 *
 * <p>
 * <c>ScrollRestoration</c> kullanılmıyor: veri yönlendiricisi (data router)
 * istiyor, uygulama ise <c>BrowserRouter</c> üzerinde.
 * </p>
 */
export function ScrollRestoration() {
  const konum = useLocation();
  const tur = useNavigationType();
  const anahtarRef = useRef(konum.key);
  anahtarRef.current = konum.key;

  /*
    KONUM AYRILIRKEN DEĞİL, KAYDIRDIKÇA KAYDEDİLİR.

    Önce ayrılma anında (etki temizliğinde) okunuyordu ve HER ZAMAN 0
    çıkıyordu: React yeni sayfayı DOM'a yazdığı anda belge kısalıyor, tarayıcı
    kaydırmayı anında sıfıra KIRPIYOR ve bu, etkiler çalışmadan önce oluyor.
    Temizlik sırasında okunan değer artık kullanıcının bıraktığı yer değil,
    tarayıcının kırptığı sıfır.

    Kaydırma olayından okuyunca böyle bir yarış yok: değer her zaman en son
    GERÇEK konum. `requestAnimationFrame` ile kısılıyor — kaydırma olayı
    saniyede yüzlerce kez tetikleniyor ve her birinde `Map`'e yazmak gereksiz.
  */
  useEffect(() => {
    let bekleyen = false;
    const yaz = () => {
      bekleyen = false;
      NOKTALAR.set(anahtarRef.current, window.scrollY);
    };
    const dinle = () => {
      if (bekleyen) return;
      bekleyen = true;
      requestAnimationFrame(yaz);
    };
    window.addEventListener('scroll', dinle, { passive: true });
    return () => window.removeEventListener('scroll', dinle);
  }, []);

  useEffect(() => {
    // İLERİ gidiş (yeni sayfa) her zaman tepeden başlar; GERİ/İLERİ (POP)
    // ise kullanıcının bıraktığı yere döner.
    if (tur !== 'POP') {
      window.scrollTo(0, 0);
      NOKTALAR.set(konum.key, 0);
      return;
    }

    const hedef = NOKTALAR.get(konum.key);
    if (!hedef) return;

    /*
      İçerik gelene kadar dene. Tek seferlik `scrollTo` çoğu zaman işe
      yaramıyor: veri hâlâ yolda, belge kısa ve tarayıcı hedefe ulaşamıyor.
      Her karede yeniden deniyoruz; ya hedefe varıyoruz ya da 1.2 saniye
      sonra bırakıyoruz (veri gelmediyse zorlamanın anlamı yok).

      Geri yükleme sürerken kaydırma dinleyicisi de yazıyor; hedefe kilitli
      olduğumuz için yazdığı değer zaten hedefin kendisi.
    */
    let bitti = false;
    const sonAn = performance.now() + 1200;

    const dene = () => {
      if (bitti) return;
      window.scrollTo(0, hedef);
      if (Math.abs(window.scrollY - hedef) < 2 || performance.now() > sonAn) {
        bitti = true;
        NOKTALAR.set(konum.key, hedef);
        return;
      }
      requestAnimationFrame(dene);
    };
    requestAnimationFrame(dene);

    return () => {
      bitti = true;
    };
  }, [konum.key, tur]);

  return null;
}
