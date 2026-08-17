import { cn } from './utils';

/**
 * Form bölümü — diyalog içindeki alan gruplaması.
 *
 * <p>
 * Gruplar önce <b>kart</b> olarak çiziliyordu: çerçeve + 16px iç boşluk. Bu
 * masaüstünde iyi duruyor ama mobilde form zaten alttan açılan bir tabakanın
 * içinde ve tabakanın kendi 16px'i var — üstüne kartın çerçevesi ve boşluğu
 * binince alan her iki yandan ~34px daralıyordu. 390px'lik bir ekranda iki
 * sütunlu ızgara tek sütuna düşüyor, uzun etiketler kırılıyordu.
 * </p>
 *
 * <p>
 * Mobilde çerçeve <b>kalkar</b> ve bölüm başlığı iki yanından altın saç teli
 * çizgiyle yazılır: <code>──── BAŞVURAN ────</code>. Yatay çizgi bölümün
 * nerede BAŞLADIĞINI söyler ve hiç yer kaplamaz; alanların tamamı tabakanın
 * genişliğini kullanır. (Bir denemede aynı iş dikey bir şeritle yapılmıştı;
 * şerit metnin solundan yer yiyor, üstelik başlıkla aynı satırda olmadığı için
 * bölüm başlangıcını da vurgulamıyordu.)
 * </p>
 *
 * <p>
 * Masaüstünde kart geri gelir — orada genişlik sorun değil ve yüzeyler
 * ekranın kalanından ayrışmalı.
 * </p>
 */
export function FormSection({
  baslik,
  aciklama,
  eylem,
  className,
  children,
}: {
  baslik: React.ReactNode;
  aciklama?: React.ReactNode;
  /** Başlığın sağındaki düğme (ör. "Ekle"). */
  eylem?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <section
      className={cn(
        'min-w-0',
        'md:rounded-card md:border md:border-border md:bg-surface md:shadow-1',
        className,
      )}
    >
      <div className="flex items-start justify-between gap-3 md:border-b md:border-line md:px-3.5 md:py-2.5">
        <div className="min-w-0 flex-1">
          {/* Mobil: ──── BAŞLIK ──── · Masaüstü: düz kart başlığı */}
          <h2
            className="flex items-center gap-3 font-display text-2xs font-bold uppercase tracking-[0.12em] text-accent-ink
              before:h-px before:flex-1 before:bg-gold before:opacity-50 before:content-['']
              after:h-px after:flex-1 after:bg-gold after:opacity-50 after:content-['']
              md:gap-0 md:text-2xs md:tracking-[0.08em] md:text-ink-3
              md:before:hidden md:after:hidden"
          >
            <span className="truncate md:w-full md:text-left">{baslik}</span>
          </h2>
          {aciklama && (
            <p className="mt-1.5 text-center text-xs text-text-3 md:mt-0.5 md:text-left">
              {aciklama}
            </p>
          )}
        </div>
        {eylem && <div className="shrink-0">{eylem}</div>}
      </div>
      {/* İç boşluk YALNIZCA masaüstünde: mobilde sınırı başlık çizgisi çiziyor. */}
      <div className="mt-2.5 md:mt-0 md:p-3.5">{children}</div>
    </section>
  );
}
