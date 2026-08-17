import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CalendarPlus } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FieldWrapper, Secim } from '../../components/Field';
import { Switch } from '../../components/Switch';
import { Button } from '../../components/Button';
import { FormModal } from '../../components/FormModal';
import { DatePicker } from '../../components/DatePicker';
import { TimeRangePicker } from '../../components/TimeRangePicker';
import { useToast } from '../../components/Toast';
import { queryKeys } from '../../data/queryKeys';
import { api } from '../../data/client';
import { useEventStatuses } from '../../data/hooks';
import { localToServer } from '../../data/time';
import type { Request } from '../../data/types';

/**
 * Talebi ajandaya (etkinliğe) ekleme penceresi.
 *
 * <p>
 * Mobilde vardı, webde yoktu: web doğrudan <c>{ randevuId }</c> gönderiyor,
 * tarih hiç geçmiyordu. Sunucu <c>BaslangicTarih</c> bekliyor ve alan
 * gelmeyince <b>0001-01-01</b> kalıyor — etkinlik takvimde ulaşılamayacak bir
 * yere düşüyordu.
 * </p>
 *
 * <p>
 * Görüşme çoğu zaman <b>ileri bir tarihe</b> veriliyor: vatandaş bugün
 * başvuruyor, randevu haftaya oluyor. Bu yüzden tarih ve saat serbest ve
 * varsayılan olarak talebin kendi tarihi değil, <b>yarın mesai başı</b>
 * öneriliyor — geçmişe etkinlik açmak neredeyse hiç istenmiyor.
 * </p>
 */
export function AddToAgendaModal({
  talep,
  acik,
  kapat,
}: {
  talep: Request;
  acik: boolean;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();
  const durumlar = useEventStatuses();

  const [gun, setGun] = useState(() => {
    const yarin = new Date();
    yarin.setDate(yarin.getDate() + 1);
    // `toISOString()` YASAK: sunucudaki damgalar saat dilimi taşımıyor ve
    // Türkiye'de tarihi bir gün geriye kaydırırdı.
    return localToServer(yarin).slice(0, 10);
  });
  const [basSaat, setBasSaat] = useState('09:00');
  const [bitSaat, setBitSaat] = useState('09:30');
  const [durumId, setDurumId] = useState<number | ''>('');

  // Hazırlık bayrakları talepten devralınır: özgeçmiş yüklenmişse resim,
  // bilgi notu istenmişse not... ama karar ekleyenin.
  const [basinKatilsin, setBasinKatilsin] = useState(false);
  const [bilgiNotu, setBilgiNotu] = useState(false);
  const [konusmaMetni, setKonusmaMetni] = useState(false);

  const varsayilanDurum = durumId === '' ? durumlar.liste[0]?.id : durumId;

  const ekle = useMutation({
    mutationFn: () =>
      api.post<{ etkinlikId: number }>('/talep/ajandaya-ekle', {
        randevuId: talep.id,
        // Sunucudaki damgalar saat dilimi taşımıyor; `toISOString()` yasak.
        baslangicTarih: `${gun}T${basSaat}:00`,
        ajandaDurumId: varsayilanDurum,
        basinKatilsin,
        bilgiNotuEklensin: bilgiNotu,
        konusmaMetniEklensin: konusmaMetni,
        resimEklensin: talep.ozgecmisDurum === true,
      }),
    onSuccess: (sonuc) => {
      qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      bildir('basari', 'Talep ajandaya eklendi', 'İlgili birime bildirim gönderildi.');
      kapat();
      // Oluşan etkinliğe GİDİLİR: uç artık kimliği döndürüyor. Önceden
      // kullanıcı "eklendi" mesajını görüyor ama etkinliği takvimde elle
      // arıyordu.
      if (sonuc?.etkinlikId) gezin(`/ajanda/${sonuc.etkinlikId}`);
    },
    onError: (h: Error) => bildir('hata', 'Ajandaya eklenemedi', h.message),
  });

  const gecerli = gun.length > 0 && basSaat.length > 0 && varsayilanDurum !== undefined;

  return (
    <FormModal
      acik={acik}
      kapat={kapat}
      baslik="Ajandaya ekle"
      aciklama={talep.konu ?? undefined}
      ikon={<CalendarPlus size={15} />}
      genislik="orta"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && ekle.mutate()}
            disabled={!gecerli || ekle.isPending}
          >
            <CalendarPlus size={14} />
            {ekle.isPending ? 'Ekleniyor…' : 'Ajandaya ekle'}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <FieldWrapper
          etiket="Görüşme tarihi"
          id="ae-gun"
          zorunlu
          ipucu="Talebin kendi tarihi değil, görüşmenin yapılacağı tarih."
        >
          <DatePicker deger={gun} degistir={setGun} id="ae-gun" />
        </FieldWrapper>

        <FieldWrapper etiket="Saat" id="ae-saat" zorunlu>
          <TimeRangePicker
            id="ae-saat"
            baslangic={basSaat}
            bitis={bitSaat}
            degistir={(b, s) => {
              setBasSaat(b);
              setBitSaat(s);
            }}
          />
        </FieldWrapper>

        <FieldWrapper etiket="Etkinlik durumu" id="ae-durum" zorunlu>
          <Secim
            id="ae-durum"
            value={durumId}
            onChange={(e) => setDurumId(e.target.value === '' ? '' : Number(e.target.value))}
          >
            {durumlar.liste.map((d) => (
              <option key={d.id} value={d.id}>
                {d.ad}
              </option>
            ))}
          </Secim>
        </FieldWrapper>

        <div className="space-y-1 rounded-control border border-border bg-surface-2 p-3">
          <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-text-3">
            Hazırlık
          </p>
          <Switch
            isaretli={basinKatilsin}
            degistir={setBasinKatilsin}
            etiket="Basın katılacak"
          />
          <Switch
            isaretli={konusmaMetni}
            degistir={setKonusmaMetni}
            etiket="Konuşma metni hazırlanacak"
          />
          <Switch
            isaretli={bilgiNotu}
            degistir={setBilgiNotu}
            etiket="Bilgi notu hazırlanacak"
          />
        </div>
      </div>
    </FormModal>
  );
}
