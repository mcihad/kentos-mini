import { ArrowLeft } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { SegmentedSelect } from '../../components/Filters';
import { RequestDashboard } from './RequestDashboard';
import { aralikHesapla, ARALIK_ETIKETLERI, type Aralik } from './range';

/**
 * Talep panosunun EKRAN sarmalayıcısı.
 *
 * <p>
 * `RequestDashboard` yalnızca çiziyor ve aralığı dışarıdan alıyor; başlık,
 * dönem seçimi ve merkeze dönüş bağlantısı burada. Panolar tek sayfadayken
 * bu üçü ortak bir üst bileşendeydi; merkez ayrı ekranlara bölününce her
 * ekran kendi başlığını kurmak zorunda kaldı.
 * </p>
 */
export default function RequestScreen() {
  const [aralik, setAralik] = useState<Aralik>('buYil');
  const [bas, bit] = aralikHesapla(aralik);

  return (
    <div className="space-y-4 md:space-y-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="min-w-0">
          <Link
            to="/istatistikler"
            className="inline-flex items-center gap-1 text-xs font-semibold text-text-3
                       transition-colors hover:text-brand"
          >
            <ArrowLeft size={13} />
            İstatistikler
          </Link>
          <h1 className="mt-0.5 font-display text-xl font-extrabold tracking-[-0.02em]">
            Talepler
          </h1>
        </div>

        <SegmentedSelect<Aralik>
          deger={aralik}
          degistir={setAralik}
          etiket="Zaman aralığı"
          secenekler={(Object.keys(ARALIK_ETIKETLERI) as Aralik[]).map((a) => ({
            deger: a,
            etiket: ARALIK_ETIKETLERI[a],
          }))}
        />
      </div>

      <RequestDashboard bas={bas} bit={bit} />
    </div>
  );
}
