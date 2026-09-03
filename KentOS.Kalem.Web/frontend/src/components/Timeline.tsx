import { cn } from './utils';

export type TimelineItem = {
  id: string | number;
  baslik: string;
  altBaslik?: string;
  zaman: string;
  govde?: React.ReactNode;
  ikon?: React.ReactNode;
  /** Nokta rengi — token adı (`--st-ok` gibi). */
  renk?: string;
};

/**
 * design.md §7.11 — zaman çizelgesi.
 *
 * Dikey çizgi son öğede kesilir; aksi hâlde listenin altında boşluğa doğru
 * uzayıp "devamı var" izlenimi verir.
 */
export function Timeline({ ogeler }: { ogeler: TimelineItem[] }) {
  return (
    <ol className="relative">
      {ogeler.map((o, i) => (
        <li key={o.id} className="relative flex gap-3 pb-4 last:pb-0">
          {/* Bağlantı çizgisi */}
          {/* Şartname §6.16: 2px bağlantı çizgisi, 24–26px durum dairesi. */}
          {i < ogeler.length - 1 && (
            <span
              aria-hidden
              className="absolute left-[11.5px] top-7 h-[calc(100%-20px)] w-[2px] rounded-pill bg-border"
            />
          )}

          <span
            className="relative z-10 mt-1 grid h-[25px] w-[25px] shrink-0 place-items-center rounded-full border-2 border-surface bg-sunken text-text-3"
            style={o.renk ? { color: `var(${o.renk})` } : undefined}
            aria-hidden
          >
            {o.ikon ?? <span className="h-[6px] w-[6px] rounded-full bg-current" />}
          </span>

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-baseline gap-x-2">
              <p className="text-sm font-semibold">{o.baslik}</p>
              <time className="text-xs text-text-3">{o.zaman}</time>
            </div>
            {o.altBaslik && (
              <p className="mt-0.5 text-sm text-text-2">{o.altBaslik}</p>
            )}
            {o.govde && <div className="mt-1.5">{o.govde}</div>}
          </div>
        </li>
      ))}
    </ol>
  );
}

/** Alan değişikliği satırı: eski → yeni. */
export function DegisiklikSatiri({
  alan,
  eski,
  yeni,
}: {
  alan: string;
  eski?: string | null;
  yeni?: string | null;
}) {
  return (
    <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs">
      <span className="text-text-3">{alan}:</span>
      {eski ? (
        <span className="rounded-sm bg-(--st-no-bg) px-1.5 py-0.5 text-(--st-no) line-through decoration-1">
          {eski}
        </span>
      ) : (
        <span className="text-text-3">—</span>
      )}
      <span className="text-text-3" aria-label="değişti">
        →
      </span>
      <span className={cn('rounded-sm bg-(--st-ok-bg) px-1.5 py-0.5 text-(--st-ok)')}>
        {yeni || '—'}
      </span>
    </div>
  );
}
