import * as RadixAccordion from '@radix-ui/react-accordion';
import { ChevronDown } from 'lucide-react';
import { cn } from './utils';

/**
 * Akordiyon — açılır/kapanır bölümler.
 *
 * <p>
 * Yükseklik canlandırması Radix'in ölçtüğü `--radix-accordion-content-height`
 * değişkeni üzerinden yapılır. `height: auto`ya geçiş CSS'te canlandırılamaz;
 * elle ölçüp piksel yazmak da her içerik değişiminde yeniden ölçmeyi
 * gerektirirdi.
 * </p>
 *
 * <p>
 * Açılırken içerik ayrıca hafifçe yukarı kayar (`translate3d`) — yalnızca
 * yükseklik büyümesi, uzun listelerde içeriğin "aşağı düşmesi" gibi
 * görünüyordu.
 * </p>
 */
export function Accordion({
  children,
  varsayilanAcik,
  className,
}: {
  children: React.ReactNode;
  /** Açık başlayacak bölümlerin değerleri. */
  varsayilanAcik?: string[];
  className?: string;
}) {
  return (
    <RadixAccordion.Root
      type="multiple"
      defaultValue={varsayilanAcik}
      className={cn('space-y-2.5', className)}
    >
      {children}
    </RadixAccordion.Root>
  );
}

export function AccordionSection({
  deger,
  baslik,
  ikon,
  sayac,
  eylem,
  children,
}: {
  deger: string;
  baslik: React.ReactNode;
  ikon?: React.ReactNode;
  /** Başlıkta gösterilecek sayı (katılımcı adedi gibi). */
  sayac?: number;
  /** Başlığın sağında duran düğme — açma/kapamayı TETİKLEMEZ. */
  eylem?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <RadixAccordion.Item
      value={deger}
      className="overflow-hidden rounded-card border border-border bg-surface"
    >
      <div className="flex items-center gap-1 pr-2">
        <RadixAccordion.Header className="min-w-0 flex-1">
          <RadixAccordion.Trigger
            className="group flex w-full items-center gap-2.5 px-3.5 py-3 text-left
              transition-colors hover:bg-surface-2
              focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-(--focus-ring)"
          >
            {ikon && (
              <span className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-sunken text-text-3" aria-hidden>
                {ikon}
              </span>
            )}

            {/*
              Başlık KIRPILMAZ, sarılır. Yan sütun dar ve sayaç + eylem
              düğmesiyle birlikte "Katılımcı biriml…" diye kesiliyordu;
              bölümün ne olduğunu söyleyen tek şey o metin.
            */}
            <span className="min-w-0 flex-1 font-display text-sm font-bold leading-tight tracking-[-0.01em]">
              {baslik}
            </span>

            {sayac !== undefined && sayac > 0 && (
              <span className="shrink-0 rounded-full bg-sunken px-2 py-0.5 text-2xs tabular-nums text-text-3">
                {sayac}
              </span>
            )}

            <ChevronDown
              size={16}
              aria-hidden
              className="shrink-0 text-text-3 transition-transform duration-200
                group-data-[state=open]:rotate-180 motion-reduce:transition-none"
            />
          </RadixAccordion.Trigger>
        </RadixAccordion.Header>

        {/* Eylem başlığın DIŞINDA: tetikleyicinin içinde olsaydı düğmeye
            basmak bölümü de açıp kapatırdı. */}
        {eylem}
      </div>

      <RadixAccordion.Content
        className="overflow-hidden
          data-[state=open]:animate-[akordiyonAc_220ms_cubic-bezier(.22,1,.36,1)]
          data-[state=closed]:animate-[akordiyonKapa_180ms_cubic-bezier(.4,0,1,1)]"
      >
        <div className="border-t border-border px-3.5 py-3">{children}</div>
      </RadixAccordion.Content>
    </RadixAccordion.Item>
  );
}
