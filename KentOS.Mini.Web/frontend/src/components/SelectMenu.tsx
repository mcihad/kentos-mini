import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { Check, ChevronDown } from 'lucide-react';
import { Button } from './Button';
import { cn } from './utils';

export type MenuOption = {
  deger: number;
  etiket: string;
  sayi?: number;
  /** Solda gösterilen renk noktası — etkinlik tipi/durum rengi. */
  renk?: string | null;
};

/**
 * Tek seçimli süzgeç menüsü.
 *
 * <p>
 * Çip şeridinin yerini aldı. Çipler bir bakışta okunuyordu ama <b>kendi
 * satırını</b> istiyorlardı ve tip sayısı arttıkça yatay kayan, sonu
 * görünmeyen bir şeride dönüşüyordu. Menü tek bir düğmeye iniyor, seçili
 * süzgeç düğmenin üstünde yazıyor — yani "süzgeç açık mı" bilgisi
 * kaybolmuyor.
 * </p>
 *
 * <p>
 * Seçim varken düğme <b>marka rengine</b> döner. Süzgecin açık olduğunu
 * fark etmemek, "kayıtlarım kayboldu" diye gelen sorunun bir numaralı
 * sebebiydi.
 * </p>
 */
export function SelectMenu({
  deger,
  degistir,
  secenekler,
  etiket,
  tumuEtiketi = 'Tümü',
  tumuSayisi,
  className,
}: {
  deger: number | null;
  degistir: (d: number | null) => void;
  secenekler: MenuOption[];
  /** Hiçbir şey seçili değilken düğmede yazan ad ("Tip", "Durum"). */
  etiket: string;
  tumuEtiketi?: string;
  tumuSayisi?: number;
  className?: string;
}) {
  const secili = secenekler.find((s) => s.deger === deger) ?? null;

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <Button
          varyant="ikincil"
          className={cn(
            'h-ctrl min-w-0 shrink-0 px-2.5',
            secili && 'border-brand text-brand-2',
            className,
          )}
          title={`${etiket} süzgeci`}
          // Görünen metin seçiliyken tip adına dönüşüyor; erişilebilir ad
          // sabit kalmalı ki ekran okuyucu düğmenin NE OLDUĞUNU kaybetmesin.
          // Görünen metni de içerir — "label in name" kuralı korunuyor.
          aria-label={secili ? `${etiket} süzgeci: ${secili.etiket}` : `${etiket} süzgeci`}
        >
          {secili?.renk && (
            <span
              aria-hidden
              className="h-[7px] w-[7px] shrink-0 rounded-full"
              style={{ background: secili.renk }}
            />
          )}
          <span className="truncate text-sm">{secili ? secili.etiket : etiket}</span>
          <ChevronDown size={12} className="shrink-0 text-text-3" />
        </Button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align="end"
          sideOffset={6}
          className="katman anim-katman z-400 max-h-[320px] w-[240px] overflow-y-auto rounded-card border border-border bg-surface p-1 shadow-3"
        >
          <Satir
            etiket={tumuEtiketi}
            sayi={tumuSayisi}
            secili={deger === null}
            tikla={() => degistir(null)}
          />
          {secenekler.length > 0 && <DropdownMenu.Separator className="my-1 h-px bg-border" />}
          {secenekler.map((s) => (
            <Satir
              key={s.deger}
              etiket={s.etiket}
              sayi={s.sayi}
              renk={s.renk}
              secili={deger === s.deger}
              tikla={() => degistir(deger === s.deger ? null : s.deger)}
            />
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}

function Satir({
  etiket,
  sayi,
  renk,
  secili,
  tikla,
}: {
  etiket: string;
  sayi?: number;
  renk?: string | null;
  secili: boolean;
  tikla: () => void;
}) {
  return (
    <DropdownMenu.Item
      onSelect={tikla}
      className="flex cursor-default items-center gap-2 rounded-sm px-2.5 py-2 text-sm outline-hidden data-highlighted:bg-surface-2"
    >
      <span className="grid w-4 shrink-0 place-items-center">
        {secili ? (
          <Check size={14} className="text-brand-2" strokeWidth={2.4} />
        ) : renk ? (
          <span
            aria-hidden
            className="h-[7px] w-[7px] rounded-full"
            style={{ background: renk }}
          />
        ) : null}
      </span>
      <span className="min-w-0 flex-1 truncate">{etiket}</span>
      {sayi !== undefined && (
        <span className="shrink-0 tabular-nums text-xs text-text-3">{sayi}</span>
      )}
    </DropdownMenu.Item>
  );
}
