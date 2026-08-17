import { MoreHorizontal } from 'lucide-react';
import { useState, type ReactNode } from 'react';
import { BottomSheet, SheetDivider, SheetRow } from '../shell/mobile/BottomSheet';
import { Fab } from '../shell/mobile/Fab';

export type SheetAction = {
  etiket: string;
  ikon: ReactNode;
  onClick: () => void;
  /** Silme gibi geri alınamaz eylemler ayrı tonda ve listenin SONUNDA. */
  ton?: 'normal' | 'tehlike';
  kapali?: boolean;
  /** Kapalıysa sebebi — satırın altında tek satır olarak yazılır. */
  ipucu?: string;
};

/**
 * EYLEM TABAKASI — detay sayfalarının mobil eylem menüsü.
 *
 * <p>
 * Detay ekranlarında eylemler başlığın altında bir düğme sırasıydı:
 * "Düzenle", "Havale et", "Diğer ▾". Telefonda bu sıra ya sarılıp iki satır
 * oluyor ya da içeriği aşağı itiyordu; üstelik asıl menü ("Diğer") bir
 * açılır liste olarak çıkıyor ve 14px'lik satırlarıyla parmakla kullanılmaya
 * uygun değildi.
 * </p>
 *
 * <p>
 * Mobilde hepsi <b>sağ alttaki FAB</b>'a taşındı: dokununca 56px'lik
 * satırlardan oluşan bir alt tabaka açılıyor. Başparmağın doğal yeri orası ve
 * ekranın üst tarafı tamamen kayda kalıyor. Masaüstünde düğme sırası aynen
 * duruyor — orada yer bol ve fare hassas.
 * </p>
 *
 * <p>
 * <b>Kapalı eylem gizlenmez, sebebiyle gösterilir.</b> "Gizli etkinlik havale
 * edilemez" bilgisi, düğmenin hiç olmamasından daha çok şey anlatıyor:
 * kullanıcı aradığı şeyin var olduğunu ama neden çalışmadığını görüyor.
 * </p>
 */
export function ActionSheet({
  eylemler,
  baslik = 'İşlemler',
  ikon,
}: {
  eylemler: SheetAction[];
  baslik?: string;
  /** FAB simgesi — varsayılan üç nokta. */
  ikon?: ReactNode;
}) {
  const [acik, setAcik] = useState(false);
  if (eylemler.length === 0) return null;

  const normal = eylemler.filter((e) => e.ton !== 'tehlike');
  const tehlikeli = eylemler.filter((e) => e.ton === 'tehlike');

  const satir = (e: SheetAction) => (
    <SheetRow
      key={e.etiket}
      ikon={e.ikon}
      ton={e.ton}
      okYok
      onClick={
        e.kapali
          ? undefined
          : () => {
              setAcik(false);
              e.onClick();
            }
      }
      kapali={e.kapali}
      ipucu={e.ipucu}
    >
      {e.etiket}
    </SheetRow>
  );

  return (
    <>
      <Fab
        etiket={baslik}
        ikon={ikon ?? <MoreHorizontal size={24} strokeWidth={2.2} />}
        onClick={() => setAcik(true)}
      />

      <BottomSheet acik={acik} kapat={() => setAcik(false)} baslik={baslik}>
        {normal.map(satir)}
        {tehlikeli.length > 0 && normal.length > 0 && <SheetDivider />}
        {tehlikeli.map(satir)}
      </BottomSheet>
    </>
  );
}
