import { Plus, X } from 'lucide-react';
import { useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '../../components/utils';
import { haptic } from '../../data/haptics';

export type FabAction = {
  etiket: string;
  ikon: ReactNode;
  onClick: () => void;
};

/**
 * FAB — mobilde ekranın birincil eylemi (design_new §5.2).
 *
 * <p>
 * 56px daire, <c>--r-xl</c> yarıçap, <c>--sh-3</c> gölge, basmada
 * <c>scale(.92)</c> yay. Alt ve sağ boşluğu EŞİT (<c>--sp-4</c>): düğme
 * köşeye oturuyor ve ekranın kendi kenar boşluğuyla aynı ritmi tutuyor.
 * </p>
 *
 * <h4>Tek eylem ya da açılan menü</h4>
 * <p>
 * <c>eylemler</c> verilirse FAB bir menüye dönüşür: dokununca eylemler
 * yukarı doğru, etiketleriyle birlikte açılır ve artı işareti çarpıya döner.
 * Bu, iki-üç eylem için alt tabakadan daha hızlı — tabaka açılıp kapanırken
 * geçen süre, tek dokunuşluk bir işi iki adıma çeviriyordu. Beşten fazla
 * eylem için doğru kap yine alt tabaka.
 * </p>
 *
 * <p>
 * Masaüstünde çizilmez: orada birincil eylem araç çubuğunda ve etiketli.
 * </p>
 */
export function Fab({
  etiket,
  yol,
  onClick,
  ikon,
  eylemler,
  ton = 'birincil',
  ustPay,
}: {
  etiket: string;
  yol?: string;
  onClick?: () => void;
  ikon?: ReactNode;
  /** Verilirse FAB açılan bir menüye dönüşür. */
  eylemler?: FabAction[];
  /**
   * Yıkıcı eylem tonu — silme gibi geri alınamaz işler için.
   *
   * <p>
   * <b>Dolu kırmızı DEĞİL.</b> Bu dosyanın komşusu <c>Button</c> kuralı açıkça
   * yazıyor: "yıkıcı eylem yumuşak; dolu kırmızı buton yanlışlıkla tıklamayı
   * davet eder". FAB, başparmağın en doğal yerinde duran 56px'lik bir hedef —
   * dolu kırmızı yapmak o uyarının tam olarak anlattığı hatayı, üstelik en
   * riskli noktada işlemek olurdu. Ton, uygulamanın kendi silme dilini
   * kullanıyor: yumuşak kırmızı zemin, kırmızı simge.
   * </p>
   */
  ton?: 'birincil' | 'yikici';
  /**
   * Alttan ek boşluk (CSS uzunluğu).
   *
   * Ekranda yapışkan bir eylem çubuğu varken FAB onun üstüne çıkmalı; aksi
   * hâlde ikisi üst üste biniyor ve çubuğun düğmesi kısmen örtülüyor.
   */
  ustPay?: string;
}) {
  const [acik, setAcik] = useState(false);

  // Menü açıkken geri tuşu ekranı DEĞİŞTİRMESİN, önce menüyü kapatsın —
  // Android'de beklenen davranış bu.
  useEffect(() => {
    if (!acik) return;
    const kapat = () => setAcik(false);
    window.addEventListener('popstate', kapat);
    return () => window.removeEventListener('popstate', kapat);
  }, [acik]);

  const dugmeSinifi = cn(
    'bas-yay fixed right-4 z-40 grid h-[56px] w-[56px] place-items-center md:hidden',
    'rounded-xl shadow-3',
    ton === 'yikici'
      ? 'border border-(--st-no-bg) bg-(--st-no-bg) text-(--st-no)'
      : 'border-0 bg-brand text-on-brand',
  );
  /*
    ALT BOŞLUK = SAĞ BOŞLUK.

    Düğme `right-4`, yani sağ kenardan `--sp-4` (16px) uzakta. Alt boşluk ise
    `--sp-10` (40px) idi ve FAB tabbar'ın epey yukarısında, sağa doğru
    asimetrik duruyordu. İki boşluk eşitlenince düğme köşeye oturuyor ve
    ekranın kendi kenar boşluğuyla aynı ritmi tutuyor.
  */
  // Şartname §6.6: FAB, ALT ÇUBUĞUN (64px) 16px üstünde durur. Eskiden
  // sekme ÖĞESİNİN boyu (`--h-tab`, 56) okunuyordu ve pay 8px'e düşüyordu.
  const tabanBosluk =
    `calc(var(--h-tabbar) + var(--sp-4) + env(safe-area-inset-bottom, 0px)`
    + (ustPay ? ` + ${ustPay})` : ')');

  const stil = { bottom: tabanBosluk };

  if (eylemler?.length) {
    return (
      <>
        {/*
          KATMAN SIRASI: perde `z-40`.

          Uygulama çubuğu `z-30`, alt sekme çubuğu `z-20`. Perde `z-20`
          olduğu sürece ikisi de PERDENİN ÜSTÜNDE kalıyordu: menü açıkken
          başlık ve sekmeler net duruyor, arkada kalmaları gereken şeyler
          öne çıkıyordu — açılan menü "üstte" okunmuyordu. Alt tabakalar
          `z-50`, yani FAB menüsünün üstünde kalmaya devam ediyor.

          Perde: menü açıkken dışarı dokunmak kapatır. Ayrıca arkadaki listeye
          yanlışlıkla dokunmayı engelliyor — menü küçük, hedefler birbirine
          yakın ve kapatmak için tam çarpıya basmak zorunda kalmak yoruyordu.
        */}
        {acik && (
          <button
            type="button"
            aria-label="Menüyü kapat"
            onClick={() => setAcik(false)}
            className="anim-perde fixed inset-0 z-40 bg-perde backdrop-blur-[2px] md:hidden"
          />
        )}

        <div
          className="fixed right-4 z-40 flex flex-col items-end gap-2.5 md:hidden"
          style={{
            // FAB'ın üstünden başlar: aynı taban + düğme boyu + bir nefes.
            bottom:
              `calc(var(--h-tabbar) + var(--sp-4) + 56px + var(--sp-3) + env(safe-area-inset-bottom, 0px)`
              + (ustPay ? ` + ${ustPay})` : ')'),
          }}
        >
          {acik &&
            eylemler.map((e, i) => (
              <button
                key={e.etiket}
                type="button"
                onClick={() => {
                  setAcik(false);
                  haptic('secim');
                  e.onClick();
                }}
                className="flex items-center gap-2.5 active:scale-[0.97]"
                style={{
                  // Kademeli giriş: hepsi aynı anda belirince menü "patlıyor",
                  // sırayla gelince göz listeyi takip ediyor.
                  animation: `fabIn 200ms var(--ease-spring) ${i * 45}ms both`,
                }}
              >
                <span className="rounded-sm bg-surface px-2.5 py-1.5 text-xs font-semibold text-ink shadow-2">
                  {e.etiket}
                </span>
                <span className="grid h-[46px] w-[46px] place-items-center rounded-lg bg-surface text-brand shadow-2">
                  {e.ikon}
                </span>
              </button>
            ))}
        </div>

        <button
          type="button"
          onClick={() => {
            haptic('secim');
            setAcik((a) => !a);
          }}
          aria-label={acik ? 'Menüyü kapat' : etiket}
          aria-expanded={acik}
          title={etiket}
          className={dugmeSinifi}
          style={stil}
        >
          <span
            // Şartname §6.6: yığın açıkken ikon çarpıya döner ve VURGU
            // rengine geçer — perde + renk değişimi "moddasın" diyor.
            className={cn('transition-transform duration-200', acik && 'text-(--accent-dk)')}
            style={{
              transform: acik ? 'rotate(90deg)' : 'none',
              transitionTimingFunction: 'var(--ease-spring)',
            }}
          >
            {acik ? <X size={26} strokeWidth={2.2} /> : (ikon ?? <Plus size={26} strokeWidth={2.2} />)}
          </span>
        </button>
      </>
    );
  }

  const icerik = ikon ?? <Plus size={26} strokeWidth={2.2} />;

  if (yol) {
    return (
      <Link to={yol} aria-label={etiket} title={etiket} className={dugmeSinifi} style={stil}>
        {icerik}
      </Link>
    );
  }
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={etiket}
      title={etiket}
      className={dugmeSinifi}
      style={stil}
    >
      {icerik}
    </button>
  );
}
