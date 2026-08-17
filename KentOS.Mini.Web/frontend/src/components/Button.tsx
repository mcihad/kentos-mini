import { forwardRef } from 'react';
import { cn } from './utils';

type Varyant = 'birincil' | 'ikincil' | 'onay' | 'yikici' | 'sade';
type Boyut = 'normal' | 'mobil';

type Props = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  varyant?: Varyant;
  boyut?: Boyut;
};

/**
 * design.md §7.1 — buton varyantları.
 *
 * Ölçüler ve sınıf dizileri tasarım belgesinden birebir; renkler yalnızca
 * token üzerinden gelir.
 */
const varyantlar: Record<Varyant, string> = {
  birincil:
    'bg-brand text-on-brand font-display font-semibold shadow-1 hover:bg-brand-2',
  ikincil:
    'border border-border bg-surface text-text-2 font-medium hover:bg-surface-2 hover:text-text',
  onay:
    'bg-(--st-ok) text-(--on-ok) font-display font-semibold hover:opacity-90',
  // Yıkıcı eylem YUMUŞAK: dolu kırmızı buton, yanlışlıkla tıklamayı davet eder.
  yikici:
    'bg-(--st-no-bg) text-(--st-no) border border-(--st-no-bg) font-semibold hover:brightness-95',
  sade:
    'text-text-2 hover:bg-surface-2 hover:text-text',
};

export const Button = forwardRef<HTMLButtonElement, Props>(function Button(
  { varyant = 'birincil', boyut = 'normal', className, ...kalan },
  ref,
) {
  return (
    <button
      ref={ref}
      className={cn(
        'inline-flex items-center justify-center gap-[7px] rounded-control text-sm',
        'transition-colors duration-150 disabled:cursor-not-allowed disabled:opacity-55',
        boyut === 'normal' ? 'h-9 px-3.5' : 'h-12 rounded-md px-4 text-base shadow-2',
        varyantlar[varyant],
        className,
      )}
      {...kalan}
    />
  );
});

type IkonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  /** Erişilebilirlik için zorunlu — design.md §11. */
  etiket: string;
};

/** İkon butonu: 38px görsel, 44px dokunma hedefi (design.md §4). */
export const IconButton = forwardRef<HTMLButtonElement, IkonProps>(function IconButton(
  { etiket, className, children, ...kalan },
  ref,
) {
  return (
    <button
      ref={ref}
      aria-label={etiket}
      title={etiket}
      className={cn(
        'relative grid h-[38px] w-[38px] place-items-center rounded-control',
        'border border-border bg-surface-2 text-text-2 transition-colors hover:text-text',
        // Görsel 38px kalır, dokunma alanı 44px'e genişler.
        'after:absolute after:inset-[-3px] after:content-[""]',
        className,
      )}
      {...kalan}
    >
      {children}
    </button>
  );
});
