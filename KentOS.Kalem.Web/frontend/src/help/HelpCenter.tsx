import { BookOpen, CircleHelp, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { SearchInput } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { InsetGroup, ListRow } from '../components/ListRow';
import { helpTopics, type HelpEntry } from './catalog';
import { HelpPanel } from './HelpPanel';

/**
 * YARDIM MERKEZİ — bütün ekranların yardımı tek listede.
 *
 * <p>
 * Menüdeki "Yardım" satırı <c>/yardim</c> adresine gidiyordu ama <b>o rota
 * hiç yoktu</b>: kullanıcı yardımı açmak isterken "Sayfa bulunamadı"
 * ekranına düşüyordu. Ölü bağlantıyı silmek de bir çözümdü; ama üst
 * çubuktaki düğme yalnızca <b>o anki ekranın</b> yardımını açıyor ve
 * "başka ekran nasıl çalışıyordu?" sorusunun hiçbir cevabı yoktu.
 * </p>
 *
 * <p>
 * Konular <b>menüyle aynı gruplarda</b> duruyor: kullanıcı yardımı, ekranı
 * menüde aradığı yerde arıyor. Arama hem başlıkta hem özette hem de metnin
 * kendisinde geçiyor — "kesme kartı nerede?" diye arayan kişi, o kelimenin
 * geçtiği yardım metnini bulur.
 * </p>
 */
export default function HelpCenter() {
  const [ara, setAra] = useState('');
  const [acilan, setAcilan] = useState<HelpEntry | null>(null);

  const gruplar = useMemo(() => {
    const k = ara.trim().toLocaleLowerCase('tr');
    const konular = helpTopics()
      .map((x) => x.kayit)
      .filter(
        (y) =>
          !k ||
          [y.baslik, y.ozet, y.metin].some((a) => a.toLocaleLowerCase('tr').includes(k)),
      );

    const sirali = new Map<string, HelpEntry[]>();
    for (const y of konular) {
      const dizi = sirali.get(y.grup) ?? [];
      dizi.push(y);
      sirali.set(y.grup, dizi);
    }
    return [...sirali.entries()];
  }, [ara]);

  const toplam = gruplar.reduce((t, [, liste]) => t + liste.length, 0);

  return (
    <div className="space-y-4">
      <div className="flex items-start gap-3">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-brand-soft text-brand">
          <BookOpen size={18} strokeWidth={1.9} />
        </span>
        <p className="min-w-0 flex-1 text-sm leading-[1.55] text-text-2 metin-guzel">
          Her ekranın kendi yardımı var. Bir ekrandayken üst çubuktaki{' '}
          <b>soru işareti</b> o ekranı anlatır; buradan ise hepsine
          ulaşabilirsiniz.
        </p>
      </div>

      <SearchInput
        value={ara}
        onChange={(e) => setAra(e.target.value)}
        placeholder="Konu, ekran adı veya aradığınız kelime"
        aria-label="Yardım konularında ara"
        ikon={<Search size={15} />}
        className="md:max-w-[420px]"
      />

      {toplam === 0 ? (
        <EmptyState
          ikon={CircleHelp}
          baslik="Eşleşen konu yok"
          aciklama="Başka bir kelime deneyin; arama, yardım metinlerinin içine de bakıyor."
        />
      ) : (
        <div className="space-y-4">
          {gruplar.map(([grup, liste]) => (
            <InsetGroup key={grup} baslik={grup}>
              {liste.map((y, i) => (
                <ListRow
                  key={y.baslik}
                  baslik={y.baslik}
                  alt={y.ozet}
                  onClick={() => setAcilan(y)}
                  sonuncu={i === liste.length - 1}
                  sira={i}
                />
              ))}
            </InsetGroup>
          ))}
        </div>
      )}

      {acilan && (
        <HelpPanel
          acik
          kapat={() => setAcilan(null)}
          baslik={acilan.baslik}
          ozet={acilan.ozet}
          metin={acilan.metin}
        />
      )}
    </div>
  );
}
