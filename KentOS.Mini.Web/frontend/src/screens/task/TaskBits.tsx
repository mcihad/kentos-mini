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
 * AŞAMA İLERLEMESİ — "kaç adım bitti?"
 *
 * Aşaması olmayan görevde hiç çizilmez; "0/0" bir bilgi değil, bir gürültü.
 */
export function StageProgress({ biten, toplam }: { biten: number; toplam: number }) {
  if (toplam === 0) return null;

  const oran = Math.round((biten / toplam) * 100);

  return (
    <span
      className="inline-flex items-center gap-1.5 whitespace-nowrap"
      title={`${toplam} aşamanın ${biten} tanesi tamamlandı`}
    >
      <span className="h-1 w-10 overflow-hidden rounded-full bg-sunken" aria-hidden>
        <span
          className="block h-full rounded-full bg-brand-ui transition-[width]"
          style={{ width: `${oran}%` }}
        />
      </span>
      <span className="text-2xs tabular-nums text-ink-3">
        {biten}/{toplam}
      </span>
    </span>
  );
}
