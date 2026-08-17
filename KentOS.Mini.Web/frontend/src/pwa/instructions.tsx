import {
  AppWindow, ArrowDownToLine, Compass, EllipsisVertical, MonitorDown, Share, SquarePlus,
} from 'lucide-react';
import type { ReactNode } from 'react';
import type { InstallState } from './install';

export type InstructionStep = { ikon: typeof Share; metin: ReactNode };

export type Instructions = {
  /** Tek satırlık özet — tabakanın başlığı altında ve kartta kullanılır. */
  ozet: string;
  adimlar: InstructionStep[];
  /** Adımlar bu tarayıcıda işe yaramıyorsa söylenecek şey. */
  not?: string;
};

/**
 * "Ana ekrana nasıl eklenir?" — tarayıcıya göre.
 *
 * <p>
 * Tek bir talimat yazmak mümkün değil: menünün adı da yeri de her tarayıcıda
 * başka. iOS Safari'de <b>Paylaş</b> düğmesinin altında, iOS Chrome'da
 * <b>⋯</b> menüsünde, Android'de <b>⋮</b> menüsünde, masaüstünde adres
 * çubuğunun sağında. Yanlış yeri tarif etmek, hiç tarif etmemekten daha kötü:
 * kullanıcı olmayan bir düğmeyi arayıp "bu uygulama kurulmuyor" diye bırakıyor.
 * </p>
 *
 * <p>
 * Kurulum istemi elimizdeyse buraya hiç gelinmez — orada tek bir düğme var.
 * Bu metinler <b>yalnızca elle kurulan</b> durumlar için.
 * </p>
 */
export function findInstructions(durum: InstallState): Instructions {
  const { platform, safari } = durum;

  if (platform === 'ios' && safari) {
    return {
      ozet: 'Safari’de üç adım, yaklaşık on saniye.',
      adimlar: [
        { ikon: Share, metin: <>Ekranın altındaki <b>Paylaş</b> düğmesine dokunun</> },
        { ikon: SquarePlus, metin: <>Listeyi kaydırıp <b>Ana Ekrana Ekle</b>’yi seçin</> },
        { ikon: ArrowDownToLine, metin: <>Sağ üstteki <b>Ekle</b> ile onaylayın</> },
      ],
    };
  }

  if (platform === 'ios') {
    return {
      ozet: 'Bu tarayıcıda ekleme menüsü farklı bir yerde.',
      adimlar: [
        { ikon: EllipsisVertical, metin: <>Adres çubuğundaki <b>⋯</b> menüsünü açın</> },
        { ikon: SquarePlus, metin: <><b>Ana Ekrana Ekle</b>’yi seçin</> },
      ],
      not:
        'Menüde bu seçenek yoksa aynı adresi Safari’de açın: iPhone ve iPad’de ' +
        'ana ekrana ekleme Safari’nin işi.',
    };
  }

  if (platform === 'android') {
    return {
      ozet: 'Tarayıcı menüsünden tek adım.',
      adimlar: [
        { ikon: EllipsisVertical, metin: <>Sağ üstteki <b>⋮</b> menüsünü açın</> },
        { ikon: ArrowDownToLine, metin: <><b>Uygulamayı yükle</b> ya da <b>Ana ekrana ekle</b>’ye dokunun</> },
      ],
      not:
        'Seçenek görünmüyorsa sayfayı yenileyip birkaç saniye bekleyin; Chrome ' +
        'kurulumu ancak sayfayı denetledikten sonra öneriyor.',
    };
  }

  if (safari) {
    return {
      ozet: 'Safari uygulamayı Dock’a kurar.',
      adimlar: [
        { ikon: Compass, metin: <>Menü çubuğundan <b>Dosya</b>’yı açın</> },
        { ikon: AppWindow, metin: <><b>Dock’a Ekle</b>’yi seçin</> },
      ],
      not: 'Bu seçenek macOS Sonoma ve üzeri Safari sürümlerinde var.',
    };
  }

  return {
    ozet: 'Tarayıcı uygulamayı kendi penceresinde açar.',
    adimlar: [
      { ikon: MonitorDown, metin: <>Adres çubuğunun sağındaki <b>yükleme</b> simgesine tıklayın</> },
      { ikon: EllipsisVertical, metin: <>Simge yoksa <b>⋮</b> → <b>Yayınla, kaydet ve paylaş</b> → <b>Sayfayı uygulama olarak yükle</b></> },
    ],
  };
}
