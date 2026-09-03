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
    /*
      `group/alan`: etiket, İÇİNDEKİ alan odaklanınca markaya döner —
      şartname §6.8 "etiket `primary` rengine geçer". Renk tek başına
      taşımıyor; kenarlık ve halka da aynı anda dönüyor.
    */
    <div className={cn('group/alan min-w-0', className)}>
      {/*
        Etiket şartname `label` kademesi: 12px/600, CÜMLE düzeni. Eski hâl
        11px BÜYÜK HARF + 0.08em idi — Türkçe metinde büyük harf dönüşümü
        diakritik hatalarına açık ve şartname etiketi büyük harfe çevirmeyi
        yasaklıyor (§6.7 "metni büyük harfe çevirme" ailesinden).
      */}
      <label
        htmlFor={id}
        className="mb-2 block text-xs font-semibold text-ink-2 transition-colors group-focus-within/alan:text-brand"
      >
        {etiket}
        {zorunlu && (
          <span className="ml-1 text-danger" aria-hidden>
            *
          </span>
        )}
      </label>
      {children}
      {/*
        YARDIM SATIRI YALNIZCA GEREKTİĞİNDE YER KAPLAR.

        Şartname §6.8 "hata metni satır kaydırmaz, alan altında sabit yer
        ayrılır" diyor ve ilk uygulamada bu HER alana uygulanmıştı: ipucusuz
        alanların altında da 16px boş bir satır duruyordu. Ölçüldü (390px,
        dokuz alanlı kullanıcı formu): alanlar arası görsel boşluk 16px değil
        **32px** çıkıyordu (16 boş yardım satırı + 16 form aralığı) ve mobil
        tabakada form gereksizce uzuyordu.

        Kural artık şu: **ipucu olan alanda satır her zaman çizilir** — hata
        geldiğinde metin değişir, yükseklik aynı kalır, hiçbir şey zıplamaz.
        İpucusuz alanda satır yalnızca hata varken çıkar; o an kullanıcı
        zaten o alana odaklanmış durumda ve tek seferlik 16px'lik kayma,
        her formda taşınan yüzlerce boş pikselden ucuz.
      */}
      {(hata || ipucu) && (
        <p className={cn('mt-1 min-h-4 text-2xs', hata ? 'text-danger' : 'text-ink-3')}>
          {hata ?? ipucu}
        </p>
      )}
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
  // Zemin `--surface-2`: alanlar yüzeyden BİR TON çukur. Kart da beyaz,
  // alan da beyazken form "çizgilerden ibaret" görünüyor ve nereye
  // yazılacağı ancak kenarlıktan anlaşılıyordu.
  //
  // Yarıçap `--r-md` (12), metin `body` (15) — şartname §6.8. Odak: kenarlık
  // markaya döner + 3px yumuşak halka (`--focus-ring`, %12 marka). Halka
  // `box-shadow` olduğu için yer kaplamaz; kenarlığı kalınlaştırmak alanı
  // 1px oynatıp yanındaki alanları da kaydırıyordu.
  'w-full rounded-md border border-line bg-surface-2 px-3.5 text-base text-ink ' +
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
        className={cn(girdiSinifi, 'h-field md:h-ctrl', hatali && 'border-danger', className)}
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
        className={cn(girdiSinifi, 'h-field md:h-ctrl appearance-none bg-position-[right_0.7rem_center] bg-no-repeat pr-8', className)}
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
        className={cn(girdiSinifi, 'h-field md:h-ctrl', ikon && 'pl-9')}
        {...kalan}
      />
    </div>
  );
}
