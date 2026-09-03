import * as AlertDialog from '@radix-ui/react-alert-dialog';
import { Info, TriangleAlert } from 'lucide-react';
import { Button } from './Button';

/**
 * Yıkıcı eylemler için onay — design.md §6.19 diyalog anatomisi.
 *
 * Diyalog burada DOĞRU seçim: veri girişi değil, tek bir evet/hayır kararı
 * ve kullanıcının o an ne sildiğini görmesi gerekiyor. (Veri giren formlar
 * ayrı sayfa olarak açılır — mobilde klavye diyalogu yutuyor.)
 *
 * `AlertDialog` kullanılıyor, `Dialog` değil: odak otomatik olarak iptal
 * düğmesine gider ve dışarı tıklamak diyaloğu KAPATMAZ. Yanlışlıkla silmeyi
 * zorlaştıran şey bu iki davranış.
 *
 * Şartnameden gelen üç kural:
 * - 44px durum ikonu kutusu başlığın üstünde — diyalog "hangi tür karar"
 *   olduğunu renkten önce biçimle söylüyor.
 * - İki buton EŞİT genişlikte; onay sağda.
 * - Yıkıcı onay, uygulamadaki TEK dolu kırmızı buton. Her yerde çerçeveli
 *   olan yıkıcı stil burada dolguya döner: son adım, sesin en yüksek olduğu
 *   yer.
 */
export function ConfirmDialog({
  acik,
  kapat,
  baslik,
  aciklama,
  onayEtiketi = 'Onayla',
  yikici,
  onayla,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  aciklama?: string;
  onayEtiketi?: string;
  yikici?: boolean;
  onayla: () => void;
}) {
  const Ikon = yikici ? TriangleAlert : Info;
  return (
    <AlertDialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde-hafif backdrop-blur-[2px]" />
        <AlertDialog.Content
          className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(420px,calc(100vw-32px))] -translate-x-1/2 -translate-y-1/2
            rounded-win border border-border bg-surface p-5 shadow-3"
        >
          <span
            aria-hidden
            className="mb-3 grid size-11 place-items-center rounded-md"
            style={{
              background: `var(${yikici ? '--st-no-bg' : '--st-live-bg'})`,
              color: `var(${yikici ? '--st-no' : '--st-live'})`,
            }}
          >
            <Ikon size={22} strokeWidth={2.1} />
          </span>

          <AlertDialog.Title className="font-display text-lg font-bold">
            {baslik}
          </AlertDialog.Title>
          {aciklama && (
            <AlertDialog.Description className="mt-1.5 text-sm text-text-2 metin-guzel">
              {aciklama}
            </AlertDialog.Description>
          )}

          <div className="mt-5 flex gap-2">
            <AlertDialog.Cancel asChild>
              <Button varyant="ikincil" className="flex-1">
                Vazgeç
              </Button>
            </AlertDialog.Cancel>
            <AlertDialog.Action asChild>
              {yikici ? (
                <button
                  onClick={onayla}
                  className="bas-yay inline-flex h-ctrl flex-1 items-center justify-center rounded-md
                    bg-(--st-no) px-4 text-sm font-bold text-white transition-colors hover:brightness-95"
                >
                  {onayEtiketi}
                </button>
              ) : (
                <Button varyant="birincil" className="flex-1" onClick={onayla}>
                  {onayEtiketi}
                </Button>
              )}
            </AlertDialog.Action>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  );
}
