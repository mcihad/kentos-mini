import type { LucideIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { cn } from './utils';

export type RowAction = {
  etiket: string;
  ikon: LucideIcon;
  onClick?: () => void;
  yol?: string;
  /** Silme gibi geri alınamaz eylemler tehlike renginde. */
  ton?: 'normal' | 'tehlike' | 'marka';
  pasif?: boolean;
};

/**
 * SATIR EYLEMLERİ — liste/kart satırındaki ikon düğmeleri (düzenle, sil,
 * yukarı, aşağı…) için TEK bileşen.
 *
 * NEDEN ORTAK: aynı düğme grubu protokolde, davetlerde, halk gününde ve
 * tanımlarda ayrı ayrı yazılmıştı; her biri kendi boyutunu, yarıçapını ve
 * dokunma alanını uyduruyordu. Sonuç, aynı işi yapan düğmelerin ekrandan
 * ekrana farklı görünmesiydi — ve hiçbiri mobil dokunma hedefini
 * tutturmuyordu.
 *
 * NATIVE HİS NEREDEN GELİYOR:
 *  • **Dokunma hedefi 40px** (mobil 44px): 28px'lik ikonları avlamak
 *    telefonda üç denemede bir tutuyordu.
 *  • **Basmada `scale(.9)` yay** — dokunuşun karşılığı anında görünüyor.
 *  • **Gruplanmış yüzey**: düğmeler tek bir `--surface-2` şeridin içinde,
 *    aralarında saç teli ayırıcı. Havada duran ayrı ayrı ikonlar "araç
 *    çubuğu" gibi okunmuyordu.
 *  • Tehlike eylemi **görsel olarak ayrı**: rengi ve basma zemini farklı,
 *    yanlışlıkla silme riski böyle düşüyor.
 */
export function RowActions({
  eylemler,
  boyut = 'normal',
  yon = 'yatay',
  className,
}: {
  eylemler: RowAction[];
  boyut?: 'normal' | 'kucuk';
  /**
   * Grubun yönü.
   *
   * <p>
   * <b>Dikey</b>, yalnızca "yukarı / aşağı" gibi <b>eksen taşıyan</b> ikili
   * için: sıralama düğmeleri yan yana dururken hangisinin yukarı taşıdığı
   * okla anlaşılıyor ama el hangi yöne gideceğini düşünüyor. Üst üste
   * dizilince düğmenin YERİ de yönü söylüyor ve dar satırda 44px genişlik
   * kazanılıyor — halk günü sırasında isimler bu yüzden "Ahm..." diye
   * kırpılıyordu.
   * </p>
   */
  yon?: 'yatay' | 'dikey';
  className?: string;
}) {
  const dikey = yon === 'dikey';
  const olcu = dikey
    ? // Dikeyde yükseklik ikiye bölünüyor; dokunma hedefi genişlikten geliyor.
      boyut === 'kucuk' ? 'h-7 w-9' : 'h-8 w-11 md:h-7 md:w-9'
    : boyut === 'kucuk' ? 'h-8 w-8 md:h-8 md:w-8' : 'h-11 w-11 md:h-9 md:w-9';
  const ikonBoyu = boyut === 'kucuk' ? 14 : 16;

  return (
    <div
      className={cn(
        'inline-flex flex-none overflow-hidden rounded-sm border border-line bg-surface-2',
        dikey ? 'flex-col items-stretch' : 'items-center',
        className,
      )}
    >
      {eylemler.map((e, i) => {
        const Ikon = e.ikon;
        const ortak = cn(
          'grid place-items-center transition-[transform,background-color,color]',
          'active:scale-90 disabled:cursor-not-allowed disabled:opacity-40',
          olcu,
          i > 0 && (dikey ? 'border-t border-line' : 'border-l border-line'),
          e.ton === 'tehlike'
            ? 'text-danger hover:bg-danger-soft'
            : e.ton === 'marka'
              ? 'text-brand hover:bg-brand-soft'
              : 'text-ink-2 hover:bg-surface hover:text-ink',
        );
        const stil = { transitionTimingFunction: 'var(--ease-spring)' };

        if (e.yol && !e.pasif) {
          return (
            <Link key={e.etiket} to={e.yol} aria-label={e.etiket} title={e.etiket} className={ortak} style={stil}>
              <Ikon size={ikonBoyu} strokeWidth={1.9} />
            </Link>
          );
        }
        return (
          <button
            key={e.etiket}
            type="button"
            onClick={e.onClick}
            disabled={e.pasif}
            aria-label={e.etiket}
            title={e.etiket}
            className={ortak}
            style={stil}
          >
            <Ikon size={ikonBoyu} strokeWidth={1.9} />
          </button>
        );
      })}
    </div>
  );
}
