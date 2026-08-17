import { ChevronRight } from 'lucide-react';
import type { ReactNode } from 'react';
import { Drawer } from 'vaul';
import { cn } from '../../components/utils';

/**
 * ALT SHEET — mobilin temel katman gramerı (design_new §5.2).
 *
 * ## Neden `vaul`
 *
 * Önce Radix Dialog + elle yazılmış CSS animasyonu + kendi sürükleme
 * kancamızla kuruluydu. Üç ayrı şey aynı `transform`u yazmaya çalışıyordu ve
 * sonuç tutarsızdı: parmak sheet'i aşağı indiriyor, CSS kapanış animasyonu
 * `transform: none`dan başladığı için sheet **yukarı zıplayıp bir daha
 * iniyordu** — kullanıcının "iki kez kapanıyor, kapanırken bir daha açılıyor"
 * dediği şey buydu.
 *
 * `vaul` tam olarak bu problemi çözmek için yazılmış: sürükleme, hız eşiği,
 * kapanış animasyonu ve iç kaydırmayla çakışma tek yerde ve tek `transform`
 * zincirinde yönetiliyor. Radix Dialog'un üstüne kurulu olduğu için `Esc`,
 * odak tuzağı ve erişilebilirlik sözleşmesi (§11) aynen duruyor.
 *
 * ## Ölçüler — tasarım demosundan birebir
 *
 *  • Üst köşeler `--radius-tabaka` (kart yarıçapının iki katı, knob'a bağlı):
 *    kart yarıçapıyla aynı olsaydı sheet "büyük bir kart" gibi dururdu.
 *  • Tutamak 44×5px, `--line-2`. İncesi dekoratif duruyor, kalını kaba.
 *  • Perde `rgba(4,8,14,.5)` + 2px bulanıklık: tam siyah perde altındaki
 *    içeriğin bağlamını yok ediyor, kullanıcı nereden geldiğini kaybediyor.
 */
export function BottomSheet({
  acik,
  kapat,
  baslik,
  aciklama,
  children,
  className,
  baslikGizli,
}: {
  acik: boolean;
  kapat: () => void;
  /** Ekran okuyucu için zorunlu; `baslikGizli` ile görsel olarak gizlenir. */
  baslik: string;
  aciklama?: string;
  /**
   * Başlığı yalnızca ekran okuyucuya bırakır.
   *
   * Menü gibi içeriği kendini anlatan tabakalarda başlık şeridi fazladan bir
   * satır ve gereksiz bir bant demek. Süzgeç gibi kaplarda ise tabakanın ne
   * olduğunu söyleyen tek şey o.
   */
  baslikGizli?: boolean;
  children: ReactNode;
  className?: string;
}) {
  return (
    <Drawer.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Drawer.Portal>
        <Drawer.Overlay className="fixed inset-0 z-50 bg-perde backdrop-blur-[3px] md:hidden" />
        <Drawer.Content
          /* Anahtarın VARLIĞI Radix için anlamlı: koşulsuz `undefined`
             geçmek `Drawer.Description` bağını koparırdı. */
          {...(aciklama ? {} : { 'aria-describedby': undefined })}
          className={cn(
            'fixed inset-x-0 bottom-0 z-50 flex max-h-[86dvh] flex-col md:hidden',
            'rounded-t-tabaka bg-surface shadow-3 outline-none',
            className,
          )}
        >
          {/*
            TEPE TEK PARÇA: tutamak ve başlık aynı renkli bloğun içinde.
            Ayrı dursalar tabakanın tepesinde renksiz ince bir bant kalıyor,
            altındaki renkli başlıkla arasında sebepsiz bir kesik oluşuyordu.
            Ton `--brand-soft`: temayla dönen, sakin bir marka izi.
          */}
          <div
            className={cn(
              'relative shrink-0 overflow-hidden rounded-t-tabaka',
              baslikGizli ? 'bg-surface' : 'bg-brand-soft',
            )}
          >
            {/* Tutamak vaul'un kendi bileşeni: sürükleme alanını da o
                tanımlıyor, bizim ayrıca bir jest bağlamamız gerekmiyor. */}
            {/*
              BAŞLIK VARSA TUTAMAK AKIŞTA DEĞİL.

              5 piksellik bir çizgi için kendi satırını açmak tepeye ~19 piksel
              ekliyordu. Şeridin üzerine bindiriliyor, ona yer açan şey
              şeridin `pt-3.5`'i. Başlık gizliyken bindirecek bir şey yok:
              orada tutamak akışta kalır, yoksa blok tamamen çöker.
            */}
            <Drawer.Handle
              className={cn(
                '!h-1 !w-9 !bg-line-2',
                baslikGizli ? '!mb-2 !mt-2.5' : '!absolute !inset-x-0 !top-[9px] !z-10 !mx-auto !my-0',
              )}
            />

            {baslikGizli ? (
              <Drawer.Title className="sr-only">{baslik}</Drawer.Title>
            ) : (
              /* `pt-[21px]` tutamağa yer açıyor: tutamak akışta değil, bu
                 şeridin üzerine bindirilmiş. Kendi satırında dururken tepe,
                 biri boş iki ayrı banda bölünüyordu. */
              <div className="flex min-h-9 items-center gap-2.5 border-b border-line px-4 pb-2.5 pt-[21px]">
                <div className="min-w-0 flex-1">
                  <Drawer.Title asChild>
                    <h2 className="font-display text-base font-bold tracking-[var(--track-d)]">
                      {baslik}
                    </h2>
                  </Drawer.Title>
                </div>
              </div>
            )}
            {/* AÇIKLAMA GÖRÜNMÜYOR, yalnızca ekran okuyucuya gidiyor: iki
                satırlık bir tepe telefonda görünür alandan çok şey alıyor ve
                cümle formu doldurmak için gerekli değil. */}
            {aciklama && (
              <Drawer.Description className="sr-only">{aciklama}</Drawer.Description>
            )}
          </div>

          {/*
            İçerik AYRI kaydırma kabında: vaul, kabın `scrollTop > 0` olduğunu
            görünce sürüklemeyi listeye bırakıyor. Tek kapta olsaydı liste
            kaydırmak isteyen parmak sheet'i kapatırdı.
          */}
          {/*
            `pt-3.5`: içerik başlık şeridinin ALTINA yapışıyordu. Renkli
            başlığın ayırıcı çizgisiyle ilk satır arasında hiç nefes yoktu ve
            tabaka sıkışık duruyordu — yatay boşluk (px-3.5) vardı, dikeyi
            unutulmuştu.
          */}
          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-3.5 pb-8 pt-3.5 guvenli-alt">
            {children}
          </div>
        </Drawer.Content>
      </Drawer.Portal>
    </Drawer.Root>
  );
}

/** Sheet içindeki grup başlığı — `--fs-2xs` 600 `.12em` UPPERCASE. */
export function SheetHeading({ children }: { children: ReactNode }) {
  return (
    <div className="px-1 pb-2 pt-1 text-2xs font-semibold uppercase tracking-[0.12em] text-ink-3">
      {children}
    </div>
  );
}

/**
 * Sheet satırı — 56px, 38px ikon çipi, sağda chevron.
 *
 * 56px tesadüf değil: 44px erişilebilirlik alt sınırı, 56px ise başparmağın
 * bakmadan isabet ettirdiği yükseklik. Native menülerde standart ölçü budur
 * ve listeyi sıkışık göstermeden kaydırmayı kısa tutar.
 */
export function SheetRow({
  ikon,
  children,
  onClick,
  ton = 'normal',
  okYok,
  kapali,
  ipucu,
  sag,
}: {
  ikon: ReactNode;
  children: ReactNode;
  onClick?: () => void;
  ton?: 'normal' | 'tehlike';
  okYok?: boolean;
  /** Eylem şu an yapılamıyor. Satır GİZLENMEZ, sönük çizilir. */
  kapali?: boolean;
  /**
   * Kapalıysa sebebi — etiketin altında ikinci satır olarak yazılır.
   *
   * Masaüstünde bu bilgi `title` özniteliğindeydi; dokunmatikte `title`
   * hiç görünmüyor ve kullanıcı sönük bir satıra bakıp neden çalışmadığını
   * bilemiyordu.
   */
  ipucu?: string;
  /** Sağdaki yuva — onay işareti, sayaç, rozet. Chevron'un yerine geçer. */
  sag?: ReactNode;
}) {
  const tehlike = ton === 'tehlike';

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={kapali}
      title={ipucu}
      className={cn(
        // İpucu varsa satır iki satırlık içerik taşıyor; sabit yükseklik
        // yerine alt sınır.
        'flex min-h-[56px] w-full items-center gap-3.5 rounded-md px-2 py-2 text-left text-lg font-medium',
        'transition-colors',
        tehlike ? 'text-danger active:bg-danger-soft' : 'text-ink active:bg-sunken',
        kapali && 'cursor-not-allowed opacity-45',
      )}
    >
      <span
        className={cn(
          'grid h-[38px] w-[38px] flex-none place-items-center rounded-sm',
          tehlike ? 'bg-danger-soft text-danger' : 'bg-sunken text-ink-2',
        )}
      >
        {ikon}
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate">{children}</span>
        {kapali && ipucu && (
          <span className="mt-0.5 block truncate text-2xs font-normal text-ink-3">{ipucu}</span>
        )}
      </span>
      {sag}
      {!okYok && !kapali && !sag && (
        <ChevronRight size={18} strokeWidth={2} className="flex-none text-ink-3" />
      )}
    </button>
  );
}

/** Sheet içi ayırıcı — `--bw` kalınlık, `--sp-3` dikey boşluk. */
export function SheetDivider() {
  return <div aria-hidden className="my-3 h-px bg-line" />;
}
