import { ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import { cn } from './utils';

/**
 * design.md §7.6 — veri listesi.
 *
 * MOBİL ÖNCELİKLİ: 768px altında her satır bir karttır; üstünde aynı veri
 * tabloya döner. İki ayrı bileşen değil, tek bileşenin iki görünümü —
 * satır tanımını bir kez yazarsın, ikisinde de aynı veriyi görürsün.
 */
export type Column<T> = {
  anahtar: string;
  baslik: string;
  /** Masaüstü hücresi. */
  hucre: (satir: T) => React.ReactNode;
  /** Sütun genişliği sınıfı (`w-32` gibi). */
  genislik?: string;
  /** Mobil kartta gösterilsin mi? Varsayılan: evet. */
  mobil?: boolean;
  /** Sağa hizala (sayısal sütunlar). */
  sag?: boolean;
};

export function DataList<T>({
  satirlar,
  sutunlar,
  anahtar,
  bagla,
  /** Mobil kartın üst satırı — başlık. */
  mobilBaslik,
  /** Mobil kartın ikinci satırı — kısa özet. */
  mobilAciklama,
  /** Mobil kartın sağ üst köşesi — rozet vb. */
  mobilRozet,
  /**
   * Mobil görünüm: sıkı liste (varsayılan) ya da ayrık kartlar.
   *
   * Kart görünümü her satırı çerçeveli, gölgeli ve 14px iç boşluklu bir
   * kutuya koyuyordu; telefonda ekrana üç satır sığıyor ve liste "kaba"
   * duruyordu. Varsayılan artık tek yüzey üzerinde saç teli çizgilerle
   * ayrılmış satırlar — yerel liste görünümü. Ajanda bunun DIŞINDA:
   * oradaki satır bir etkinliğin kartı, renk şeridi ve saat bloğuyla
   * birlikte anlam taşıyor.
   */
  mobilGorunum = 'liste',
  bos,
}: {
  satirlar: T[];
  sutunlar: Column<T>[];
  anahtar: (satir: T) => string | number;
  bagla?: (satir: T) => string;
  mobilBaslik: (satir: T) => React.ReactNode;
  mobilAciklama?: (satir: T) => React.ReactNode;
  mobilRozet?: (satir: T) => React.ReactNode;
  mobilGorunum?: 'liste' | 'kart';
  bos?: React.ReactNode;
}) {
  if (satirlar.length === 0) return <>{bos}</>;

  return (
    <>
      {/* ── Mobil ── */}
      <ul
        className={cn(
          'md:hidden',
          mobilGorunum === 'kart'
            ? 'space-y-2'
            : // Tek yüzey, satırlar saç teliyle ayrılır: telefonda liste
              // taramak için en az gürültülü düzen.
              'divide-y divide-border overflow-hidden rounded-card border border-border bg-surface',
        )}
      >
        {satirlar.map((s) => {
          const ayrintilar = sutunlar.filter((k) => k.mobil !== false);

          const icerik = (
            <>
              <div className="flex items-start justify-between gap-2">
                <div className="min-w-0 flex-1">
                  <p className="truncate font-display text-sm font-semibold">
                    {mobilBaslik(s)}
                  </p>
                  {mobilAciklama && (
                    <p className="mt-0.5 truncate text-sm text-text-2">
                      {mobilAciklama(s)}
                    </p>
                  )}
                </div>
                {mobilRozet?.(s)}
              </div>

              {ayrintilar.length > 0 &&
                (mobilGorunum === 'kart' ? (
                  <dl className="mt-2.5 grid grid-cols-2 gap-x-3 gap-y-1.5">
                    {ayrintilar.map((k) => (
                      <div key={k.anahtar} className="min-w-0">
                        <dt className="text-2xs uppercase tracking-[0.06em] text-text-3">
                          {k.baslik}
                        </dt>
                        <dd className="truncate text-sm text-text-2">{k.hucre(s)}</dd>
                      </div>
                    ))}
                  </dl>
                ) : (
                  // Etiket/değer ızgarası yerine TEK satır: "Meslek · Tarih ·
                  // Durum" biçiminde akıyor ve satır yüksekliği yarıya
                  // iniyor. Etiketler zaten değerin kendisinden anlaşılıyor.
                  <div className="mt-1 flex flex-wrap items-center gap-x-2.5 gap-y-0.5 text-xs text-text-3">
                    {ayrintilar.map((k) => (
                      <span key={k.anahtar} className="min-w-0 truncate">
                        {k.hucre(s)}
                      </span>
                    ))}
                  </div>
                ))}
            </>
          );

          const kartSinifi =
            mobilGorunum === 'kart'
              ? 'block rounded-card border border-border bg-surface p-3.5 shadow-1'
              : 'block px-3.5 py-2.5';

          return (
            <li key={anahtar(s)}>
              {bagla ? (
                <Link
                  to={bagla(s)}
                  className={cn(kartSinifi, 'transition-colors active:bg-surface-2')}
                >
                  {icerik}
                </Link>
              ) : (
                <div className={kartSinifi}>{icerik}</div>
              )}
            </li>
          );
        })}
      </ul>

      {/* ── Masaüstü: tablo ── */}
      <div className="hidden overflow-x-auto rounded-card border border-border bg-surface shadow-1 md:block">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-border bg-sunken">
              {sutunlar.map((k) => (
                <th
                  key={k.anahtar}
                  scope="col"
                  className={cn(
                    'px-3.5 py-2.5 text-left text-2xs font-semibold uppercase tracking-[0.06em] text-text-3',
                    k.genislik,
                    k.sag && 'text-right',
                  )}
                >
                  {k.baslik}
                </th>
              ))}
              {bagla && <th className="w-10" aria-label="Ayrıntı" />}
            </tr>
          </thead>
          <tbody>
            {satirlar.map((s) => (
              <tr
                key={anahtar(s)}
                className="border-b border-border last:border-0 hover:bg-surface-2"
              >
                {sutunlar.map((k) => (
                  <td
                    key={k.anahtar}
                    className={cn('px-3.5 py-2.5 align-middle', k.sag && 'text-right tabular-nums')}
                  >
                    {bagla && k === sutunlar[0] ? (
                      <Link to={bagla(s)} className="font-medium text-text hover:text-brand-2">
                        {k.hucre(s)}
                      </Link>
                    ) : (
                      k.hucre(s)
                    )}
                  </td>
                ))}
                {bagla && (
                  <td className="px-2">
                    <Link
                      to={bagla(s)}
                      aria-label="Ayrıntıyı aç"
                      className="grid h-7 w-7 place-items-center rounded-sm text-text-3 hover:bg-sunken hover:text-text"
                    >
                      <ChevronRight size={15} />
                    </Link>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
