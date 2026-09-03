import { clsx, type ClassValue } from 'clsx';
import { extendTailwindMerge } from 'tailwind-merge';

/**
 * TEMA TABANLI ÖLÇÜLER `tailwind-merge`E TANITILIR.
 *
 * <p>
 * `h-ctrl`, `h-field`, `h-row`… sınıfları Tailwind 4'ün <code>@theme</code>
 * bloğundaki <code>--height-*</code> anahtarlarından üretiliyor.
 * <code>tailwind-merge</code> bunları TANIMIYOR: kendi bildiği sınıf
 * listesi çekirdek Tailwind'den geliyor, projenin temasından değil.
 * </p>
 *
 * <p>
 * <b>Sonucu sessiz.</b> <code>cn('h-ctrl', 'h-[52px]')</code> ikisini de
 * bırakıyor, CSS'te özel utility kazanıyor ve <b>çağıranın verdiği boy hiç
 * uygulanmıyor</b> — istisna yok, uyarı yok, sınıf DOM'da duruyor. Ölçüldü:
 * giriş ekranının "Giriş yap" düğmesi <code>h-[52px]</code> yazdığı hâlde
 * <b>40px</b> çiziliyordu (yani <code>h-ctrl</code>), dokunma hedefi
 * şartnamedeki 48px'in altında kalıyordu.
 * </p>
 *
 * <p>
 * Bekçi: <code>test/utils.test.ts</code>. Yeni bir <code>--height-*</code> /
 * <code>--min-height-*</code> anahtarı eklenirse buraya da yazılmalı;
 * yazılmazsa aynı sessiz hata geri döner.
 * </p>
 */
const OLCU_ANAHTARLARI = [
  'ctrl', 'ctrl-lg', 'ctrl-xl',
  'field', 'appbar', 'bar-m', 'row', 'row-m', 'tab', 'tabbar',
];

const birlestir = extendTailwindMerge({
  extend: {
    classGroups: {
      h: OLCU_ANAHTARLARI.map((a) => `h-${a}`),
      'min-h': OLCU_ANAHTARLARI.map((a) => `min-h-${a}`),
    },
  },
});

/** Tailwind sınıflarını çakışmasız birleştirir. */
export function cn(...girdiler: ClassValue[]) {
  return birlestir(clsx(girdiler));
}
