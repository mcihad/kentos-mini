import { cn } from './utils';

/**
 * design.md §7.5 — kart yüzeyi. Renk yalnızca tokendan gelir.
 *
 * <p>
 * <b>`min-w-0` pazarlık konusu değil.</b> Izgara ve esnek kutu öğelerinin
 * varsayılan en küçük genişliği `auto`dur, yani içeriğin min-content
 * genişliğinin altına inemezler. İçeride `truncate` (yani `white-space:
 * nowrap`) olan bir başlık varsa o min-content, metnin TAMAMI kadar olur:
 * kart 814px'e şişip sayfayı yatay kaydırılır hâle getiriyordu.
 * </p>
 *
 * <p>
 * Bunun mobilde ikinci bir bedeli vardı: düzen alanı görüş alanından geniş
 * olunca <b>`position: fixed` alt çubuk da</b> düzen alanına göre
 * konumlanıyor ve ekranın altından kayıp gidiyordu. "Alt sekme kayboluyor"
 * ile "başlık taşıyor" aynı hatanın iki yüzü.
 * </p>
 */
export function Card({
  className,
  children,
  ...kalan
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('min-w-0 rounded-card border border-border bg-surface shadow-1', className)}
      {...kalan}
    >
      {children}
    </div>
  );
}

/** Kart başlığı — sol tarafta metin, sağda eylemler. */
export function CardHeader({
  baslik,
  aciklama,
  eylem,
  className,
}: {
  baslik: React.ReactNode;
  aciklama?: React.ReactNode;
  eylem?: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        'flex items-start justify-between gap-3 border-b border-border px-4 py-3',
        className,
      )}
    >
      <div className="min-w-0">
        <h2 className="truncate font-display text-sm font-bold tracking-[-0.01em]">
          {baslik}
        </h2>
        {aciklama && <p className="mt-0.5 truncate text-xs text-text-3">{aciklama}</p>}
      </div>
      {eylem && <div className="shrink-0">{eylem}</div>}
    </div>
  );
}

/**
 * design.md §7.4 — sayı karosu.
 *
 * Sayı `tabular-nums` ile hizalanır: yan yana duran karolarda rakamlar
 * kaymasın diye.
 */
export function StatTile({
  etiket,
  deger,
  ikon,
  vurgu,
  altMetin,
}: {
  etiket: string;
  deger: React.ReactNode;
  ikon?: React.ReactNode;
  /** Rengi token adı olarak ver (`--st-ok` gibi); hard-code renk YOK. */
  vurgu?: string;
  altMetin?: string;
}) {
  return (
    <Card className="p-3.5">
      <div className="flex items-start justify-between gap-2">
        <p className="text-xs font-medium text-text-3">{etiket}</p>
        {ikon && (
          <span
            className="grid h-[26px] w-[26px] shrink-0 place-items-center rounded-sm bg-sunken text-text-3"
            aria-hidden
          >
            {ikon}
          </span>
        )}
      </div>
      <p
        className="mt-1.5 font-display text-2xl font-bold leading-none tabular-nums tracking-[-0.02em]"
        style={vurgu ? { color: `var(${vurgu})` } : undefined}
      >
        {deger}
      </p>
      {altMetin && <p className="mt-1.5 text-xs text-text-3">{altMetin}</p>}
    </Card>
  );
}
