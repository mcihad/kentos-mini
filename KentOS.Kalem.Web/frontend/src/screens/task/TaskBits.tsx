import { AlertTriangle, Clock } from 'lucide-react';

/**
 * SLA ROZETİ — "ne kadar vakit kaldı?"
 *
 * <p>
 * Gecikme ve kalan süre SUNUCUDAN geliyor; burada hesaplanmıyor. İstemcinin
 * saati yanlışsa rozet de yanlış olurdu ve gecikme ölçümü kurumun personeline
 * yazdığı bir şey — tahmine bırakılamaz.
 * </p>
 *
 * <p>
 * SLA'sı olmayan ya da KAPANMIŞ görevde hiç çizilmez: kapanan işin ölçümü
 * bitti, listede kırmızı bir rozet bırakmak yalnızca gürültü olurdu.
 * Sunucu bu durumda <code>kalanSaat</code>'i <code>null</code> gönderiyor.
 * </p>
 */
export function SlaBadge({
  gecikti,
  kalanSaat,
  kisa,
}: {
  gecikti: boolean;
  kalanSaat?: number | null;
  kisa?: boolean;
}) {
  if (kalanSaat == null) return null;

  const metin = sureMetni(kalanSaat);

  if (gecikti) {
    return (
      <span
        className="inline-flex items-center gap-1 whitespace-nowrap text-2xs font-medium text-(--st-no)"
        title={`Süre ${metin} aşıldı`}
      >
        <AlertTriangle size={12} strokeWidth={2.2} />
        {kisa ? metin : `${metin} gecikme`}
      </span>
    );
  }

  // 24 saatin altı UYARI rengi: "yarın dolacak" ile "iki hafta var" aynı
  // görünürse rozet hiçbir şey söylemiyor demektir.
  const yakin = kalanSaat < 24;

  return (
    <span
      className={`inline-flex items-center gap-1 whitespace-nowrap text-2xs ${
        yakin ? 'font-medium text-(--st-wait)' : 'text-ink-3'
      }`}
      title={`Süre bitimine ${metin}`}
    >
      <Clock size={12} strokeWidth={2} />
      {metin}
    </span>
  );
}

/**
 * Saat cinsinden süreyi okunabilir yazar.
 *
 * Mutlak değer alınıyor: işaret zaten "gecikti mi" bilgisinde; metne eksi
 * koymak "−36 saat gecikme" gibi iki kez olumsuzlanmış bir ifade üretirdi.
 */
function sureMetni(saat: number): string {
  const m = Math.abs(saat);
  if (m < 1) return `${Math.round(m * 60)} dk`;
  if (m < 48) return `${Math.round(m)} sa`;
  return `${Math.round(m / 24)} gün`;
}

/**
 * İLERLEME ÇUBUĞU — "işin ne kadarı bitti?"
 *
 * <p>
 * Yüzde SUNUCUDAN geliyor (<code>ilerleme</code>) ve aşama sayısıyla
 * hesaplanmıyor: kural <code>GorevDurumAkisi.Ilerleme</code> içinde tek
 * yerde yazılı. Aşaması olmayan görevlerde bile bir sayı veriyor —
 * modüldeki görevlerin çoğunun aşaması yok ve onlar eskiden çubuk hiç
 * görmüyordu.
 * </p>
 *
 * <p>
 * <b>%100 yalnızca ONAYLANMIŞ görevde.</b> Bütün aşamaları biten ama onay
 * bekleyen iş %95'te durur; çubuğun dolması, kabul edilmemiş bir işi bitmiş
 * göstermek olurdu.
 * </p>
 *
 * <p>
 * Aşama sayısı (<code>3/4</code>) varsa yanında duruyor: yüzde "ne kadarı",
 * kesir "hangi adımda" sorusunu cevaplıyor ve ikisi farklı şeyler.
 * </p>
 */
export function StageProgress({
  biten,
  toplam,
  ilerleme,
  genis,
}: {
  biten: number;
  toplam: number;
  ilerleme?: number | null;
  /** Tam genişlik çubuk — kart içinde tek başına durduğunda. */
  genis?: boolean;
}) {
  // Aşaması olmayan ve hiç başlamamış görevde çizilmez: %0'lık boş bir çubuk
  // bir bilgi değil, her satırda tekrar eden bir gürültü.
  const oran = ilerleme ?? (toplam === 0 ? 0 : Math.round((biten / toplam) * 100));
  if (toplam === 0 && !oran) return null;

  const baslik =
    toplam > 0
      ? `${toplam} aşamanın ${biten} tanesi kapandı — %${oran}`
      : `İşin %${oran}'i tamamlandı`;

  return (
    <span
      // Dar kipte `shrink-0`: satırdaki esneyen komşusu (sorumlu adı)
      // sıkışırken ilerleme çubuğunu da daraltıyordu.
      className={genis ? 'flex items-center gap-2' : 'inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap'}
      title={baslik}
    >
      <span
        className={`h-1 overflow-hidden rounded-full bg-sunken ${genis ? 'min-w-0 flex-1' : 'w-10'}`}
        aria-hidden
      >
        <span
          className="block h-full rounded-full bg-brand transition-[width]"
          style={{ width: `${oran}%` }}
        />
      </span>
      <span className="shrink-0 text-2xs tabular-nums text-ink-3">
        {toplam > 0 ? `${biten}/${toplam}` : `%${oran}`}
      </span>
    </span>
  );
}
