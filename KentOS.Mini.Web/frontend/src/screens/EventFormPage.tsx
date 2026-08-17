import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { serverToLocal } from '../data/time';
import Agenda from './Agenda';
import EventDetail from './EventDetail';
import { dilimdenOneri, type BaslangicOnerisi } from './event/EventFields';
import { EventModal } from './event/EventModal';

/**
 * <c>/ajanda/yeni</c> ve <c>/ajanda/:id/duzenle</c> rotalarının karşılığı.
 *
 * <p>
 * Form artık <b>diyalogda</b> yaşıyor (bkz. <c>EtkinlikModal</c>); uygulama
 * içinden açıldığında sayfa hiç değişmiyor. Rotalar yine de duruyor:
 * yer imi ya da paylaşılmış bir bağlantı doğrudan forma düşebilmeli.
 * </p>
 *
 * <p>
 * Diyaloğun <b>arkasına</b> gidilecek ekran çizilir (yeni → ajanda, düzenle →
 * etkinlik detayı). Boş bir gövdenin üstünde duran diyalog, kapatınca nereye
 * düşüleceğini gizliyordu; ayrıca bu ekranların verisi zaten kapanışta
 * gerekiyor.
 * </p>
 *
 * <p>
 * <c>?baslangic=</c> takvimden gelen dilimi taşır — sayfa yenilense bile
 * seçilen saat kaybolmaz.
 * </p>
 */
export default function EventFormPage() {
  const { id } = useParams();
  const [parametreler] = useSearchParams();
  const gezin = useNavigate();

  const duzenleme = id !== undefined && id !== 'yeni';
  const etkinlikId = duzenleme ? Number(id) : null;

  const ham = parametreler.get('baslangic');
  let oneri: BaslangicOnerisi | null = null;
  if (!duzenleme && ham) {
    const t = serverToLocal(ham);
    if (!Number.isNaN(t.getTime())) oneri = dilimdenOneri(t);
  }

  return (
    <>
      {duzenleme ? <EventDetail /> : <Agenda />}

      <EventModal
        acik
        etkinlikId={etkinlikId}
        oneri={oneri}
        onKapat={() => gezin(duzenleme ? `/ajanda/${etkinlikId}` : '/ajanda')}
        onKaydedildi={(kayitId) => gezin(`/ajanda/${kayitId}`)}
      />
    </>
  );
}
