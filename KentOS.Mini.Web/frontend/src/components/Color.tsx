import { cn } from './utils';

/**
 * Kullanıcı tanımlı renklerin arayüzde kullanımı.
 *
 * <p>
 * Etkinlik durumu, etkinlik tipi ve talep durumu renkleri <b>veritabanından</b>
 * gelir — tema tokenı değildir, kullanıcı yönetim ekranından değiştirir.
 * Eski arayüz de aynı kaynağı kullanıyordu (takvimde <c>Durum.Renk</c> + `80`
 * saydamlığı).
 * </p>
 *
 * <p>
 * Bu dosya o renklerin <b>tek geçiş noktası</b>: doğrudan
 * <code>style=&#123;&#123;background: renk&#125;&#125;</code> yazmak yerine buradaki
 * yardımcılar kullanılır. Sebep: koyu temada ham renk okunmuyor, saydamlık
 * her yerde farklı hesaplanıyordu ve renk yoksa (tanımı silinmiş kayıt)
 * arayüz kırılıyordu.
 * </p>
 */

/** Renk verilmemişse kullanılacak nötr değer. */
const YEDEK = 'var(--text-3)';

/** Ham rengi güvenli bir CSS değerine çevirir. */
export function colorOr(renk?: string | null, yedek = YEDEK): string {
  if (!renk) return yedek;
  const t = renk.trim();
  // Yalnızca `#RGB`, `#RRGGBB`, `#RRGGBBAA` kabul edilir. Veritabanındaki
  // eski kayıtlarda "red", "rgb(1,2,3)" gibi değerler de var; bunlar CSS'te
  // çalışır ama `color-mix` içinde öngörülemez sonuç veriyor.
  return /^#([0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$/i.test(t) ? t : yedek;
}

/**
 * Rozet/şerit zemini: rengin düşük opaklıklı hâli.
 *
 * `color-mix` kullanılır çünkü `#RRGGBB80` gibi elle alfa eklemek koyu temada
 * zeminle karışıp okunmaz oluyor; `color-mix` zemin rengiyle harmanlar.
 */
export function colorSurface(renk?: string | null, oran = 14): string {
  return `color-mix(in srgb, ${colorOr(renk)} ${oran}%, transparent)`;
}

/**
 * Kullanıcı tanımlı renkli rozet.
 *
 * Metin rengi ham renk, zemin onun soluk hâli — iki değer aynı kaynaktan
 * türediği için kontrast her zaman aynı yönde çalışır.
 */
export function ColoredBadge({
  etiket,
  renk,
  className,
  nokta = true,
}: {
  etiket?: string | null;
  renk?: string | null;
  className?: string;
  /** Sol taraftaki renk noktası. */
  nokta?: boolean;
}) {
  if (!etiket) return <span className="text-text-3">—</span>;

  return (
    <span
      className={cn(
        'inline-flex h-6 shrink-0 items-center gap-1.5 rounded-full px-2.5 text-2xs font-semibold',
        className,
      )}
      style={{ color: colorOr(renk), background: colorSurface(renk) }}
    >
      {nokta && <span className="h-[5px] w-[5px] rounded-full bg-current" aria-hidden />}
      {etiket}
    </span>
  );
}

/**
 * Kart/satır başındaki renk şeridi.
 *
 * Listelerde durumu bir bakışta okunur kılar; rozeti okumadan önce göz
 * rengi yakalar.
 */
export function ColorStrip({
  renk,
  className,
  yatay,
}: {
  renk?: string | null;
  className?: string;
  /** Üstte yatay şerit (kart başlığı) — varsayılan dikey (satır solu). */
  yatay?: boolean;
}) {
  return (
    <span
      aria-hidden
      className={cn(
        'shrink-0 rounded-full',
        yatay ? 'h-[3px] w-full' : 'w-[3px] self-stretch',
        className,
      )}
      style={{ background: colorOr(renk, 'var(--border-2)') }}
    />
  );
}

/**
 * Takvim etkinliğinin renk kümesi.
 *
 * Gün ızgarasında etkinlik kartı dolu zemin ister; ay görünümünde ince çip.
 * İkisi de aynı kaynaktan türetilir ki bir etkinlik iki görünümde farklı
 * renkte görünmesin.
 */
export function eventColors(durumRenk?: string | null, tipRenk?: string | null) {
  // Durum önce gelir: eski sistem de takvimi duruma göre boyuyordu. Durum
  // yoksa tipe düşülür — hiç renk olmamasındansa kategori rengi daha bilgilendirici.
  const ana = colorOr(durumRenk, colorOr(tipRenk, 'var(--brand-hover)'));

  return {
    /** Şerit / kenarlık. */
    kenar: ana,
    /** Kart zemini (gün görünümü). */
    zemin: `color-mix(in srgb, ${ana} 16%, var(--surface))`,
    /** Çip zemini (ay görünümü). */
    cipZemini: `color-mix(in srgb, ${ana} 12%, transparent)`,
    /** Metin — ham renk yeterince koyu olmayabilir; token metin rengi kullanılır. */
    metin: 'var(--text)',
  };
}
