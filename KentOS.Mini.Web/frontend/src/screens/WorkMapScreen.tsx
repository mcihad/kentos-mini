import { Map as MapIcon } from 'lucide-react';
import { useState } from 'react';
import { EmptyState } from '../components/EmptyState';
import { SegmentedSelect } from '../components/Filters';
import { Skeleton } from '../components/Skeleton';
import { Switch } from '../components/Switch';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { useMapPoints } from '../data/citizen';
import { UnitScopePicker } from './task/UnitScopePicker';
import { WorkMap } from './map/WorkMap';

type Kapsam = 'kendi' | 'alt';

/**
 * İŞ HARİTASI — birimin işi coğrafi olarak.
 *
 * <p>
 * Uzun vadede makam bütün birimlerin işini burada görecek; bugünkü kapsam
 * kullanıcının kendi birimi ve (isterse) alt ağacı.
 * </p>
 *
 * <p>
 * <b>Bekleyen bildirimler isteğe bağlı katman.</b> Karşılama personeli için
 * değerli — aynı sokakta biriken üç bildirim haritada tek bakışta görünüyor
 * ve mükerrer olduğu anlaşılıyor. Varsayılan olarak kapalı: saha personeli
 * yalnızca kendi işlerine bakıyor.
 * </p>
 */
export default function WorkMapScreen() {
  const { hasPermission } = useSession();

  const [kapsam, setKapsam] = useState<Kapsam>('kendi');
  const [yalnizAcik, setYalnizAcik] = useState(true);
  const [bildirimler, setBildirimler] = useState(false);

  const { data: noktalar, isLoading } = useMapPoints({
    altBirimlerDahil: kapsam === 'alt',
    yalnizAcik,
    bildirimlerDahil: bildirimler,
  });

  const liste = noktalar ?? [];

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        <UnitScopePicker />

        <SegmentedSelect<Kapsam>
          deger={kapsam}
          degistir={setKapsam}
          etiket="Kapsam"
          secenekler={[
            { deger: 'kendi', etiket: 'Birimim' },
            { deger: 'alt', etiket: 'Alt birimler' },
          ]}
        />

        <span className="ml-auto text-2xs tabular-nums text-ink-3">
          {liste.length} nokta
        </span>
      </div>

      <div className="flex flex-wrap gap-x-6 gap-y-2">
        <Switch
          isaretli={yalnizAcik}
          degistir={setYalnizAcik}
          etiket="Yalnızca açık işler"
        />

        {hasPermission(PERMISSION.bildirimKarsila) && (
          <Switch
            isaretli={bildirimler}
            degistir={setBildirimler}
            etiket="Bekleyen vatandaş bildirimleri"
          />
        )}
      </div>

      {isLoading ? (
        <Skeleton className="h-[460px] w-full" />
      ) : liste.length === 0 ? (
        <EmptyState
          ikon={MapIcon}
          baslik="Haritaya basılacak kayıt yok"
          aciklama="Konumu girilmiş bir görev ya da bildirim bulunmuyor."
        />
      ) : (
        <WorkMap noktalar={liste} />
      )}

      {/* Gösterge: renkler durumdan, kırmızı halka gecikmeden geliyor. */}
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-2xs text-ink-3">
        <span className="inline-flex items-center gap-1.5">
          <span
            className="h-2.5 w-2.5 rounded-full"
            style={{ background: '#1E5FBF' }}
            aria-hidden
          />
          Görev — rengi durumundan
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span
            className="h-2.5 w-2.5 rounded-full ring-2 ring-(--st-no)"
            style={{ background: '#1E5FBF' }}
            aria-hidden
          />
          Süresi aşılmış
        </span>
        {bildirimler && (
          <span className="inline-flex items-center gap-1.5">
            <span
              className="h-2.5 w-2.5 rounded-full"
              style={{ background: '#A78952' }}
              aria-hidden
            />
            Bekleyen bildirim
          </span>
        )}
      </div>
    </div>
  );
}
