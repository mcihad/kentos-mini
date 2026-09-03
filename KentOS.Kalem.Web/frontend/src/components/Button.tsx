import { forwardRef } from 'react';
import { cn } from './utils';

type Varyant = 'birincil' | 'ikincil' | 'ucuncul' | 'onay' | 'yikici' | 'sade';
type Boyut = 'normal' | 'mobil';

type Props = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  varyant?: Varyant;
  boyut?: Boyut;
};

/**
 * design.md §6 — buton varyantları, şartnamenin tablosundan birebir:
 *
 * | varyant  | zemin        | metin      | kenarlık |
 * |----------|--------------|------------|----------|
 * | birincil | `primary`    | `onPrimary`| —        |
 * | ikincil  | `surface`    | `primary`  | 1.5px `primary` |
 * | ucuncul  | marka tonu   | `primary`  | —        |
 * | yikici   | `surface`    | `danger`   | 1.5px `danger` |
 * | sade     | şeffaf       | `primary`  | —        |
 *
 * YIKICI DOLU DEĞİL: dolu kırmızı buton yanlışlıkla tıklamayı davet ediyor.
 * Şartnamenin tek istisnası onay diyaloğunun son butonu — o dolgu
 * `ConfirmDialog` içinde, burada değil.
 *
 * `onay` şartnamede yok; bu uygulamanın kendi ihtiyacı (talep onayı gibi
 * doğrudan "olumlu sonuç" eylemleri). Durum rengi + beyaz metinle kurulur.
 *
 * Basış geri bildirimi `bas-yay` (90ms, `scale(.97)`) — şartname §4.
 */
const varyantlar: Record<Varyant, string> = {
  birincil:
    'bg-brand text-on-brand font-bold shadow-1 hover:bg-brand-hover active:bg-brand-hover',
  ikincil:
    'border-[1.5px] border-brand text-brand bg-surface font-bold hover:bg-brand-soft',
  ucuncul:
    'bg-brand-soft text-brand font-bold hover:bg-brand-line/40',
  onay:
    'bg-(--st-ok) text-(--on-ok) font-bold hover:opacity-90',
  yikici:
    'border-[1.5px] border-(--st-no) text-(--st-no) bg-surface font-bold hover:bg-(--st-no-bg)',
  sade:
    'text-brand font-semibold hover:bg-brand-soft',
};

export const Button = forwardRef<HTMLButtonElement, Props>(function Button(
  { varyant = 'birincil', boyut = 'normal', className, ...kalan },
  ref,
) {
  return (
    <button
      ref={ref}
      className={cn(
        'bas-yay inline-flex items-center justify-center gap-2 rounded-md',
        'transition-colors duration-150 disabled:cursor-not-allowed disabled:opacity-55',
        // Şartname boyları: sm 40 (masaüstü yoğunluğu) · md 48 (mobil, touch.min).
        boyut === 'normal' ? 'h-ctrl px-4 text-sm' : 'h-ctrl-lg px-5 text-base',
        varyantlar[varyant],
        className,
      )}
      {...kalan}
    />
  );
});

/**
 * İkon butonunun görünümü.
 *
 * <p>
 * <b>cerceveli</b> — kenarlıklı ve dolgulu. İçerik arasında tek başına duran
 * eylemler için: kart köşesindeki sil düğmesi gibi, çevresinde onu buton
 * olarak okutacak başka bir ipucu yoksa.
 * </p>
 * <p>
 * <b>sade</b> — kenarlıksız, zemini yalnızca üzerine gelince beliriyor.
 * Düğmelerin YAN YANA dizildiği yerler için: appbar ve tabaka başlığında
 * dört-beş kenarlıklı kutu, şeridi bir araç çubuğuna değil bir kutu
 * ızgarasına çeviriyordu.
 * </p>
 */
type IkonVaryanti = 'cerceveli' | 'sade';

type IkonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  /** Erişilebilirlik için zorunlu — design.md §8. */
  etiket: string;
  varyant?: IkonVaryanti;
};

const ikonVaryantlari: Record<IkonVaryanti, string> = {
  cerceveli: 'border border-border bg-surface-2 text-text-2 hover:text-text',
  // Zemin ÜZERİNE GELİNCE beliriyor; boştayken şerit tek bir yüzey gibi
  // okunuyor. `active:` dokunmatikte de geri bildirim veriyor — mobilde
  // hover diye bir şey yok.
  sade: 'text-text-2 hover:bg-surface-2 hover:text-text active:bg-surface-2',
};

/**
 * İkon butonu: 40px görsel, 48px dokunma hedefi (şartname `touch.min` 48).
 *
 * Görsel boy `size-10` ile boşluk knob'una bağlı (4×10 = 40); eski sabit
 * 38px hem knob'u dinlemiyordu hem 44'lük dokunma hedefi şartnamenin 48
 * eşiğinin altındaydı.
 */
export const IconButton = forwardRef<HTMLButtonElement, IkonProps>(function IconButton(
  { etiket, varyant = 'cerceveli', className, children, ...kalan },
  ref,
) {
  return (
    <button
      ref={ref}
      aria-label={etiket}
      title={etiket}
      className={cn(
        'bas-yay relative grid size-10 place-items-center rounded-control transition-colors',
        ikonVaryantlari[varyant],
        // Görsel 40px kalır, dokunma alanı 48px'e genişler (şartname §8.3).
        'after:absolute after:inset-[-4px] after:content-[""]',
        className,
      )}
      {...kalan}
    >
      {children}
    </button>
  );
});
