import { CalendarPlus, Pencil } from 'lucide-react';
import { OverlayShell } from '../../components/OverlayShell';
import { EventFields, type BaslangicOnerisi } from './EventFields';

/**
 * Etkinlik ekleme / düzenleme diyaloğu.
 *
 * <p>
 * Etkinlik <b>her yerden aynı diyalogla</b> açılır: takvimde bir yarım saatlik
 * dilime tıklamak, ajandadaki "Yeni etkinlik" düğmesi ve
 * <c>/ajanda/yeni</c> rotası hepsi buraya düşer. Takvimde bir dilime
 * tıklayınca sayfa değiştirmek, kullanıcının baktığı haftayı kaybettiriyordu.
 * </p>
 *
 * <p>
 * <b>Kabuk artık <c>TabakaKabi</c>.</b> Burası uzun süre ham bir Radix Dialog
 * + elle yazılmış CSS animasyonuydu; mobilde tabaka gibi davranmıyor,
 * parmakla kapanmıyor ve açılırken başlık alanına ELLE odaklanıyordu. O odak,
 * telefonun klavyesini giriş animasyonunun ortasında açtırıyor ve tabakayı
 * titretiyordu — form içeren tabakaların tutarsız görünmesinin sebebi buydu.
 * Formu olmayan tabakalar (halk günü menüsü) aynı sorunu yaşamıyordu.
 * </p>
 *
 * <p>
 * Diyalog <b>kendi içinde kaydırılır</b>; eylem çubuğu <c>EtkinlikAlanlari</c>
 * içinde ve sabit olduğu için "Kaydet" her zaman görünür kalır.
 * </p>
 */
export function EventModal({
  acik,
  onKapat,
  etkinlikId,
  oneri,
  onKaydedildi,
}: {
  acik: boolean;
  onKapat: () => void;
  /** Verilirse düzenleme. */
  etkinlikId?: number | null;
  /** Yeni kayıtta ön dolgu — takvimden tıklanan dilim. */
  oneri?: BaslangicOnerisi | null;
  /**
   * Kaydetmeden sonra çağrılır. <b>Verilirse kapatma sorumluluğu çağırana
   * geçer</b> — rota sarmalayıcısı kaydettikten sonra detaya gidiyor, ayrıca
   * "kapat" çağrılsaydı iki kez yönlendirme olurdu.
   */
  onKaydedildi?: (id: number) => void;
}) {
  const duzenleme = typeof etkinlikId === 'number' && etkinlikId > 0;

  return (
    <OverlayShell
      acik={acik}
      kapat={onKapat}
      baslik={duzenleme ? 'Etkinliği düzenle' : 'Yeni etkinlik'}
      ikon={duzenleme ? <Pencil size={15} /> : <CalendarPlus size={15} />}
      genislik="genis"
    >
      <EventFields
        etkinlikId={etkinlikId}
        oneri={oneri}
        onVazgec={onKapat}
        onKaydedildi={(id) => (onKaydedildi ? onKaydedildi(id) : onKapat())}
      />
    </OverlayShell>
  );
}
