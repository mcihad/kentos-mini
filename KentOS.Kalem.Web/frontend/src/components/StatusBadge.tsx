import { cn } from './utils';

/** design.md §7.3 — altı durum, sabit eşleme. */
export const DURUMLAR = {
  beklemede: { etiket: 'Beklemede', renk: '--st-wait', zemin: '--st-wait-bg' },
  onaylandi: { etiket: 'Onaylandı', renk: '--st-ok', zemin: '--st-ok-bg' },
  devam: { etiket: 'Devam Ediyor', renk: '--st-live', zemin: '--st-live-bg' },
  reddedildi: { etiket: 'Reddedildi', renk: '--st-no', zemin: '--st-no-bg' },
  iptal: { etiket: 'İptal Edildi', renk: '--st-cancel', zemin: '--st-cancel-bg' },
  tamamlandi: { etiket: 'Tamamlandı', renk: '--st-done', zemin: '--st-done-bg' },
} as const;

export type StatusKey = keyof typeof DURUMLAR;

export function StatusBadge({
  durum,
  className,
}: {
  durum: StatusKey;
  className?: string;
}) {
  const d = DURUMLAR[durum];
  return (
    /*
      Şartname §6.11 durum etiketi: `radius/sm` köşe + `caption` (11/600,
      +0.04em — `text-2xs` bunları kendisi taşıyor) + fg/bg ikilisi. Nokta,
      "renk tek başına bilgi taşımaz" (§8.6) kuralının payı — renk körü
      kullanıcı çipin dolu/boş hâlini noktadan ayırt ediyor. Tam yuvarlak
      hap değil: hap biçimi süzgeç çiplerinin dili, durum etiketi köşeli.
    */
    <span
      className={cn(
        'inline-flex h-6 items-center gap-1.5 rounded-sm px-2 text-2xs font-semibold',
        className,
      )}
      style={{ color: `var(${d.renk})`, background: `var(${d.zemin})` }}
    >
      <span className="h-[5px] w-[5px] rounded-full bg-current" aria-hidden />
      {d.etiket}
    </span>
  );
}
