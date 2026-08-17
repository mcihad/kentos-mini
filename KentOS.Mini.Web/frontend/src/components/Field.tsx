import { forwardRef } from 'react';
import { useIsDesktop } from './screenSize';
import { cn } from './utils';

/**
 * design.md §7.2 — form alanları.
 *
 * Etiket her zaman input'a `htmlFor` ile bağlıdır; yalnızca yer tutucuya
 * dayanan form, ekran okuyucuda ve otomatik doldurmada adsız kalır.
 */
export function FieldWrapper({
  etiket,
  id,
  zorunlu,
  hata,
  ipucu,
  className,
  children,
}: {
  etiket: string;
  id: string;
  zorunlu?: boolean;
  hata?: string;
  ipucu?: string;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={cn('min-w-0', className)}>
      <label
        htmlFor={id}
        className="mb-1.5 block text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3"
      >
        {etiket}
        {zorunlu && <span className="ml-1 text-danger">*</span>}
      </label>
      {children}
      {hata ? (
        <p className="mt-1 text-2xs text-danger">{hata}</p>
      ) : ipucu ? (
        <p className="mt-1 text-2xs text-ink-3">{ipucu}</p>
      ) : null}
    </div>
  );
}

/**
 * Ortak girdi görünümü.
 *
 * Odakta <b>kenarlık markaya döner + yumuşak bir halka</b> çıkar
 * (`--focus-ring`). Halka `box-shadow` olduğu için yer kaplamaz; kenarlığı
 * kalınlaştırmak alanı 1px oynatıp yanındaki alanları da kaydırıyordu.
 * `transition-[color,background-color,border-color,box-shadow]` — halkanın
 * belirmesi de yumuşasın diye; yalın `transition-colors` gölgeyi kapsamıyor
 * ve halka bir anda "patlıyordu".
 */
const girdiSinifi =
  // Zemin `--surface-2`: şartname (§7.2) alanları yüzeyden BİR TON çukur
  // istiyor. Kart da beyaz, alan da beyazken form "çizgilerden ibaret"
  // görünüyor ve nereye yazılacağı ancak kenarlıktan anlaşılıyordu.
  'w-full rounded-sm border border-line bg-surface-2 px-3.5 text-sm text-ink ' +
  'placeholder:text-ink-3 outline-hidden ' +
  'transition-[color,background-color,border-color,box-shadow] duration-150 ' +
  'hover:border-line-2 ' +
  'focus:border-brand focus:ring-[3px] focus:ring-(--focus-ring) ' +
  'disabled:cursor-not-allowed disabled:opacity-55';

/**
 * MOBİLDE `autoFocus` YOK SAYILIR.
 *
 * Alt sayfa açılırken ilk alan odaklanınca telefon klavyeyi kaldırıyor,
 * görünür alan küçülüyor ve `vaul` giriş animasyonunun ORTASINDA tabakayı
 * yeniden konumlandırıyor — kullanıcının gördüğü şey titreme oluyor.
 * (Halk günü sayfası aynı sorunu yaşamıyordu; çünkü onun tabakasında
 * `autoFocus` alan yok. Fark buradan geliyordu.)
 *
 * Üstelik mobilde otomatik odak kendi başına da yanlış: klavye, kullanıcı
 * formu görmeden yarısını kapatıyor. Masaüstünde faydalı olduğu için orada
 * duruyor.
 */
export const Input = forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement> & { hatali?: boolean }>(
  function Input({ className, hatali, autoFocus, ...kalan }, ref) {
    const masaustu = useIsDesktop();
    return (
      <input
        ref={ref}
        autoFocus={masaustu ? autoFocus : undefined}
        className={cn(girdiSinifi, 'h-ctrl-lg md:h-10', hatali && 'border-danger', className)}
        {...kalan}
      />
    );
  },
);

export const Textarea = forwardRef<HTMLTextAreaElement, React.TextareaHTMLAttributes<HTMLTextAreaElement>>(
  function Textarea({ className, autoFocus, ...kalan }, ref) {
    // `Girdi` ile aynı kural: mobilde otomatik odak klavyeyi kaldırıp
    // tabakanın giriş animasyonunu bozuyor.
    const masaustu = useIsDesktop();
    return (
      <textarea
        ref={ref}
        autoFocus={masaustu ? autoFocus : undefined}
        className={cn(girdiSinifi, 'min-h-[92px] resize-y py-2.5 leading-[1.55]', className)}
        {...kalan}
      />
    );
  },
);

export const Secim = forwardRef<HTMLSelectElement, React.SelectHTMLAttributes<HTMLSelectElement>>(
  function Secim({ className, ...kalan }, ref) {
    return (
      <select
        ref={ref}
        className={cn(girdiSinifi, 'h-10 appearance-none bg-position-[right_0.7rem_center] bg-no-repeat pr-8', className)}
        style={{
          // Ok işareti: metin rengini izlesin diye currentColor ile SVG.
          backgroundImage:
            "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%23888' stroke-width='2.5' stroke-linecap='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E\")",
        }}
        {...kalan}
      />
    );
  },
);

/**
 * Arama kutusu — sol ikonlu.
 *
 * `type="search"` bilinçli: mobil klavyede "ara" tuşu çıkar ve tarayıcı
 * temizleme düğmesi verir.
 */
export function SearchInput({
  className,
  ikon,
  ...kalan
}: React.InputHTMLAttributes<HTMLInputElement> & { ikon?: React.ReactNode }) {
  return (
    <div className={cn('relative', className)}>
      {ikon && (
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-text-3">
          {ikon}
        </span>
      )}
      <input
        type="search"
        className={cn(girdiSinifi, 'h-ctrl-lg md:h-10', ikon && 'pl-9')}
        {...kalan}
      />
    </div>
  );
}
