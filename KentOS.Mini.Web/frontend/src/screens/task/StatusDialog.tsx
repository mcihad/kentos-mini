import { useEffect, useState } from 'react';
import { Button } from '../../components/Button';
import { FormModal } from '../../components/FormModal';
import { FieldWrapper, Textarea } from '../../components/Field';
import { TASK_STATUS } from '../../data/types';

/** Gerekçe ZORUNLU olan durumlar — sunucudaki kuralın aynası. */
const GEREKCE_ZORUNLU: number[] = [
  TASK_STATUS.iadeEdildi,
  TASK_STATUS.reddedildi,
  TASK_STATUS.iptal,
];

/**
 * DURUM DEĞİŞTİRME KUTUSU.
 *
 * <p>
 * İade, ret ve iptalde <b>gerekçe zorunlu</b>: üçü de birinin işini geri
 * çeviriyor ve gerekçesiz bir ret, personelin neyi düzelteceğini bilmemesi
 * demek. Kural sunucuda da var; buradaki denetim yalnızca reddedileceğini
 * bildiğimiz bir isteği atmamak için.
 * </p>
 *
 * <p>
 * Gerekçe istemeyen geçişlerde (başlat, beklemeye al) kutu yine açılıyor ama
 * yalnızca bir onay adımı olarak: durum değişimi listede ve çizelgede iz
 * bırakıyor, yanlışlıkla tıklanabilen bir düğme olmamalı.
 * </p>
 */
export function StatusDialog({
  istek,
  kapat,
  onayla,
  bekliyor,
}: {
  istek: { durum: number; ad: string } | null;
  kapat: () => void;
  onayla: (durum: number, gerekce?: string) => void;
  bekliyor: boolean;
}) {
  const [gerekce, setGerekce] = useState('');

  // Kutu her açılışta TEMİZ başlar: bir öncekinin gerekçesi kalırsa yanlış
  // metin yanlış karara iliştirilir ve çizelgeye o hâliyle yazılır.
  useEffect(() => {
    if (istek) setGerekce('');
  }, [istek]);

  if (!istek) return null;

  const zorunlu = GEREKCE_ZORUNLU.includes(istek.durum);
  const gecerli = !zorunlu || gerekce.trim().length > 0;

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={istek.ad}
      aciklama={
        zorunlu
          ? 'Gerekçe zorunlu — görevi yürüten kişi neyin eksik olduğunu görecek.'
          : 'Bu değişiklik görevin geçmişine yazılır.'
      }
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            varyant={zorunlu ? 'yikici' : 'birincil'}
            disabled={!gecerli || bekliyor}
            onClick={() => onayla(istek.durum, gerekce.trim() || undefined)}
          >
            {istek.ad}
          </Button>
        </>
      }
    >
      <FieldWrapper etiket="Gerekçe" id="durum-gerekce" zorunlu={zorunlu}>
        <Textarea
          id="durum-gerekce"
          value={gerekce}
          onChange={(e) => setGerekce(e.target.value)}
          rows={3}
          placeholder={zorunlu ? 'Neden geri çevriliyor?' : 'İsteğe bağlı not'}
        />
      </FieldWrapper>
    </FormModal>
  );
}
