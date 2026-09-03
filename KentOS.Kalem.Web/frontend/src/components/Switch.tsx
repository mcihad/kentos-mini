import * as RadixSwitch from '@radix-ui/react-switch';
import { cn } from './utils';

/**
 * Açık/kapalı anahtarı — onay kutusunun yerine.
 *
 * <p>
 * Yerleşik `<input type="checkbox">` iki sorun üretiyordu: görünümü tarayıcıya
 * göre değişiyor (Safari'de `accent-color` farklı davranıyor) ve dokunma hedefi
 * 16px kalıyordu. Anahtar hem kurumsal renkleri token üzerinden alıyor hem de
 * tüm satırı tıklanabilir yaparak hedefi 44px'in üstüne çıkarıyor.
 * </p>
 *
 * <p>
 * Kaydırma hareketi <c>translate3d</c> ile: ekran kartında çalışır, düşük
 * uçlu telefonda da takılmaz.
 * </p>
 */
export function Switch({
  isaretli,
  degistir,
  etiket,
  aciklama,
  pasif,
  /** Durum rengi — "işaretli ama içerik yok" gibi uyarılar için. */
  ton = 'marka',
  id,
  className,
}: {
  isaretli: boolean;
  degistir: (a: boolean) => void;
  etiket?: React.ReactNode;
  aciklama?: React.ReactNode;
  pasif?: boolean;
  ton?: 'marka' | 'uyari';
  id?: string;
  className?: string;
}) {
  const anahtar = (
    <RadixSwitch.Root
      id={id}
      checked={isaretli}
      onCheckedChange={degistir}
      disabled={pasif}
      className={cn(
        // Şartname §6.11: anahtar 46×28. Eski 42×24 dokunma hedefinin de
        // görünürlüğün de altındaydı; baş parmak boyu şartnameden geliyor.
        'relative h-[28px] w-[46px] shrink-0 rounded-full border border-border bg-sunken',
        'transition-colors duration-200 motion-reduce:transition-none',
        'focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-(--focus-ring)',
        'disabled:cursor-not-allowed disabled:opacity-45',
        isaretli && (ton === 'uyari'
          ? 'border-(--st-no) bg-(--st-no)'
          : 'border-brand bg-brand'),
      )}
    >
      <RadixSwitch.Thumb
        className={cn(
          'block h-[22px] w-[22px] rounded-full bg-surface shadow-1',
          'transition-transform duration-200 ease-[cubic-bezier(.22,1,.36,1)]',
          'motion-reduce:transition-none',
          'translate-x-[2px] data-[state=checked]:translate-x-[20px]',
        )}
      />
    </RadixSwitch.Root>
  );

  if (!etiket && !aciklama) return anahtar;

  return (
    // Etiketin tamamı tıklanabilir: 24px'lik anahtarı avlamak zorunda kalmamak
    // için. `htmlFor` yerine sarmalayan label — Radix kendi düğmesini basıyor.
    <label
      className={cn(
        'flex cursor-pointer items-start gap-3 py-1',
        pasif && 'cursor-not-allowed opacity-60',
        className,
      )}
    >
      {anahtar}
      <span className="min-w-0 flex-1">
        {etiket && <span className="block text-sm font-medium">{etiket}</span>}
        {aciklama && (
          <span className="mt-0.5 block text-xs leading-[1.45] text-text-3">
            {aciklama}
          </span>
        )}
      </span>
    </label>
  );
}
