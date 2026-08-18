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
  serit,
  className,
  children,
  ...kalan
}: React.HTMLAttributes<HTMLDivElement> & {
  /**
   * Telefonda KENARDAN KENARA çizilir; masaüstünde normal kart.
   *
   * <p>
   * Ölçüldü: 390px'lik bir ekranda detay sayfasının okunabilir içerik
   * genişliği <b>330px</b> — aradaki 60px sayfa dolgusu (2×16) ile kart
   * kenarlığı ve dolgusundan (2×14) gidiyor. Ekranın <b>%15'i</b> kutu
   * içinde kutu çizmeye harcanıyordu ve iç içe iki çerçeve, kullanıcının
   * "kalabalık" dediği görüntünün büyük kısmı.
   * </p>
   *
   * <p>
   * Şerit kipinde bölüm sayfanın iki kenarına dayanıyor: yan kenarlıklar ve
   * köşe yuvarlaması kalkıyor, üst-alt kenarlık bölümü ayırmaya devam
   * ediyor. Kazanç yaklaşık 32px içerik genişliği ve bir kat daha az
   * çerçeve. <code>design.md §5.2</code> mobil liste gramerini zaten böyle
   * tarif ediyor — kart GRUBU ekranı boydan boya kaplar.
   * </p>
   */
  serit?: boolean;
}) {
  return (
    <div
      className={cn(
        'min-w-0 border border-border bg-surface shadow-1',
        serit
          ? '-mx-4 rounded-none border-x-0 md:mx-0 md:rounded-card md:border-x'
          : 'rounded-card',
        className,
      )}
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
        {/* Şartname `title3` (16/700) — kart başlığının kademesi. */}
        <h2 className="truncate font-display text-lg font-bold">
          {baslik}
        </h2>
        {aciklama && <p className="mt-0.5 truncate text-xs font-medium text-text-3">{aciklama}</p>}
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
      {/*
        Şartname §6.15 StatCard: sayı `title1` boyunda ve TEK ARALIKLI —
        yan yana karolarda rakamlar hizalanır, "1" ile "8" aynı yeri kaplar.
        JetBrains Mono 500 (`font-mono`), sayısal verinin ailesi.
      */}
      <p
        className="mt-1.5 font-mono text-2xl font-medium leading-none tracking-[-0.02em]"
        style={vurgu ? { color: `var(${vurgu})` } : undefined}
      >
        {deger}
      </p>
      {altMetin && <p className="mt-1.5 text-xs text-text-3">{altMetin}</p>}
    </Card>
  );
}
