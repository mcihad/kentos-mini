import { ChevronRight } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { cn } from './utils';

/**
 * INSET GRUP — mobilin temel liste kabı (design_new §5.2).
 *
 * Native listeler tek tek kart değil, **tek yüzeyde saç teli çizgilerle
 * ayrılmış satırlardır**. Her satırı ayrı karta koymak telefonda ekrana üç
 * satır sığdırıyor ve göz her satırda yeniden çerçeve arıyor.
 *
 * Ayırıcı `border-bottom` DEĞİL, satır içi gölge (`inset 0 -1px`): kenarlık
 * grubun yuvarlatılmış köşelerini içeriden keserdi.
 */
export function InsetGroup({
  baslik,
  eylem,
  children,
  className,
}: {
  baslik?: ReactNode;
  /** Başlığın sağındaki bağlantı ("Tümü", "Takvim"…). */
  eylem?: ReactNode;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section className={className}>
      {(baslik || eylem) && (
        <header className="flex items-baseline justify-between px-1 pb-2">
          {baslik && (
            <h2 className="font-display text-base font-bold tracking-[var(--track-d)] text-ink">
              {baslik}
            </h2>
          )}
          {eylem}
        </header>
      )}
      <div className="overflow-hidden rounded-lg border border-line bg-surface shadow-1">
        {children}
      </div>
    </section>
  );
}

/**
 * LİSTE SATIRI — `rowMin` 64px (şartname §4), sol durum çipi, iki satır başlık, sağda chevron.
 *
 * `sira` verilirse satırlar kademeli girer (`rowIn`, index × 28ms). Kademe
 * listeye "geldi" hissi veriyor; hepsi aynı anda belirince ekran bir anda
 * doluyor ve göz nereye bakacağını bilmiyor.
 */
export function ListRow({
  ikon,
  ikonRengi,
  ikonZemini,
  ust,
  baslik,
  alt,
  sag,
  yol,
  onClick,
  sira,
  sonuncu,
}: {
  ikon?: ReactNode;
  /** Durum rengi — `var(--ok)` gibi bir token. */
  ikonRengi?: string;
  ikonZemini?: string;
  /** Üst meta satırı (durum · tarih). */
  ust?: ReactNode;
  baslik: ReactNode;
  alt?: ReactNode;
  /** Sağdaki rozet/etiket; verilirse chevron yine çizilir. */
  sag?: ReactNode;
  yol?: string;
  onClick?: () => void;
  sira?: number;
  sonuncu?: boolean;
}) {
  const icerik = (
    <>
      {ikon && (
        <span
          className="mt-0.5 grid h-[30px] w-[30px] flex-none place-items-center rounded-sm"
          style={{
            color: ikonRengi,
            background: ikonZemini ?? (ikonRengi ? `color-mix(in oklab, ${ikonRengi} 13%, var(--surface))` : undefined),
          }}
        >
          {ikon}
        </span>
      )}

      <span className="min-w-0 flex-1">
        {ust && (
          <span className="mb-1 flex items-center gap-1.5 text-2xs text-ink-3">{ust}</span>
        )}
        <span className="line-clamp-2 block text-sm leading-[1.45] text-ink">{baslik}</span>
        {alt && (
          <span className="mt-1.5 flex items-center gap-1.5 text-2xs text-ink-3">{alt}</span>
        )}
      </span>

      {sag}
      <ChevronRight
        size={17}
        strokeWidth={2.2}
        className="mt-3 flex-none text-ink-3 opacity-60"
      />
    </>
  );

  const sinif = cn(
    'anim-satir flex min-h-row-m w-full items-start gap-2.5 px-3.5 py-3 text-left transition-colors',
    'active:bg-sunken md:hover:bg-surface-2',
    !sonuncu && 'shadow-[inset_0_calc(var(--bw)*-1)_0_var(--line)]',
  );
  const stil = sira != null ? { animationDelay: `${sira * 28}ms` } : undefined;

  if (yol) {
    return (
      <Link to={yol} className={sinif} style={stil}>
        {icerik}
      </Link>
    );
  }
  return (
    <button type="button" onClick={onClick} className={sinif} style={stil}>
      {icerik}
    </button>
  );
}

/** Grup sonundaki tam genişlik bağlantı satırı ("Tüm talepler (114)"). */
export function GroupFooterLink({ yol, children }: { yol: string; children: ReactNode }) {
  return (
    <Link
      to={yol}
      className="flex h-[46px] w-full items-center justify-center text-sm font-medium text-brand transition-colors active:bg-sunken"
    >
      {children}
    </Link>
  );
}
