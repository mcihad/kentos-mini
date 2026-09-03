import { Briefcase, FileUser } from 'lucide-react';
import { cn } from '../../components/utils';
import type { ResumeSummary } from '../../data/types';

/**
 * Kaydın NEREDEN geldiği — havuza doğrudan mı yüklendi, bir talebe mi eklendi.
 *
 * <p>
 * Havuzun iki kaynağı var ve ikisi aynı listede duruyor. Ayrım işe yarıyor:
 * talepten gelen bir özgeçmiş, arkasında bir başvuru ve bir konu taşıyor —
 * "bu kişi zaten bize iş için yazmış" demek. Altın çerçeve kılavuzdaki işini
 * yapıyor: saç teli vurgu, geniş dolgu değil.
 * </p>
 */
export function SourceBadge({
  kayit,
  className,
}: {
  kayit: Pick<ResumeSummary, 'talepId' | 'talepKonusu' | 'kaynakAd'>;
  className?: string;
}) {
  const talepten = Boolean(kayit.talepId);
  const Ikon = talepten ? Briefcase : FileUser;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 whitespace-nowrap rounded-full border px-2 py-0.5 text-2xs',
        talepten ? 'border-(--gold) text-gold' : 'border-border text-text-2',
        className,
      )}
      title={talepten ? (kayit.talepKonusu ?? 'Talepten geldi') : 'Doğrudan havuza eklendi'}
    >
      <Ikon size={11} strokeWidth={1.9} />
      {kayit.kaynakAd}
    </span>
  );
}

/**
 * Baş harf çipi.
 *
 * <p>
 * Havuz bir <b>kişi</b> listesi; satırın solunda bir yüz olması listeyi
 * taramayı hızlandırıyor. Talepten gelen kayıt altın halkalı: rozeti okumaya
 * gerek kalmadan hangi kaydın arkasında bir başvuru olduğu görünüyor.
 * </p>
 */
export function InitialsChip({
  harfler,
  talepten,
  buyuk,
}: {
  harfler: string;
  talepten?: boolean;
  buyuk?: boolean;
}) {
  return (
    <span
      aria-hidden
      className={cn(
        'grid flex-none place-items-center rounded-full bg-brand-soft font-display font-bold text-brand',
        buyuk ? 'h-11 w-11 text-sm' : 'h-9 w-9 text-2xs',
        talepten && 'ring-1 ring-(--gold)',
      )}
    >
      {harfler}
    </span>
  );
}
