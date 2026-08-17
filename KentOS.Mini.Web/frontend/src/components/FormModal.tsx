import { OverlayShell, type OverlayWidth } from './OverlayShell';

/**
 * Form diyaloğu — kayıt ekleme/düzenleme için ortak kabuk.
 *
 * <p>
 * Kabın kendisi (perde, mobil tabaka / masaüstü pencere, başlık şeridi,
 * açılış odağı) <c>TabakaKabi</c>'nda. Burada kalan tek şey <b>form
 * düzeni</b>: kayan gövde + sabit eylem çubuğu.
 * </p>
 *
 * <p>
 * Gövde kayar, <b>alt çubuk sabit kalır</b>. Bu şart: talep formu uzun ve
 * mobilde klavye açıkken kaydet düğmesi ekran dışında kalıyordu. Sabit alt
 * çubuk, formun neresinde olursan ol düğmeyi görünür tutar.
 * </p>
 */
export function FormModal({
  acik,
  kapat,
  baslik,
  aciklama,
  ikon,
  genislik = 'orta',
  altBilgi,
  eylemler,
  children,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  aciklama?: string;
  ikon?: React.ReactNode;
  genislik?: OverlayWidth;
  /** Alt çubuğun solunda duran metin (karakter sayacı gibi). */
  altBilgi?: React.ReactNode;
  eylemler: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <OverlayShell
      acik={acik}
      kapat={kapat}
      baslik={baslik}
      aciklama={aciklama}
      ikon={ikon}
      genislik={genislik}
    >
      {/*
        `space-y-4`: alanlar arasındaki dikey ritim BURADA tanımlı.
        Yoktu ve yalnızca kendi sarmalayıcısında `space-y-*` taşıyan
        formlar (talep, etkinlik) doğru görünüyordu; alanlarını doğrudan
        veren her diyalogda kutular BİTİŞİK çiziliyordu — süzgeç
        tabakasında "BİTİŞ" alanı ile onay kutusu birbirine yapışıktı.
        Ritmi her çağrı yerinde tekrar etmek, birini unutmayı davet eder.
      */}
      <div className="min-h-0 flex-1 space-y-3.5 overflow-y-auto overscroll-contain p-3.5">
        {children}
      </div>

      <div className="flex shrink-0 items-center gap-2 border-t border-line px-3.5 py-2.5">
        <span className="min-w-0 flex-1 truncate text-2xs text-ink-3">{altBilgi}</span>
        {eylemler}
      </div>
    </OverlayShell>
  );
}
