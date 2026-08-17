import * as AlertDialog from '@radix-ui/react-alert-dialog';
import { Button } from './Button';

/**
 * Yıkıcı eylemler için onay.
 *
 * Diyalog burada DOĞRU seçim: veri girişi değil, tek bir evet/hayır kararı
 * ve kullanıcının o an ne sildiğini görmesi gerekiyor. (Veri giren formlar
 * ayrı sayfa olarak açılır — mobilde klavye diyalogu yutuyor.)
 *
 * `AlertDialog` kullanılıyor, `Dialog` değil: odak otomatik olarak iptal
 * düğmesine gider ve dışarı tıklamak diyaloğu KAPATMAZ. Yanlışlıkla silmeyi
 * zorlaştıran şey bu iki davranış.
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
  return (
    <AlertDialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde-hafif backdrop-blur-[2px]" />
        <AlertDialog.Content
          className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(420px,calc(100vw-32px))] -translate-x-1/2 -translate-y-1/2
            rounded-card border border-border bg-surface p-5 shadow-2"
        >
          <AlertDialog.Title className="font-display text-lg font-bold">
            {baslik}
          </AlertDialog.Title>
          {aciklama && (
            <AlertDialog.Description className="mt-1.5 text-sm leading-[1.55] text-text-2 metin-guzel">
              {aciklama}
            </AlertDialog.Description>
          )}

          <div className="mt-5 flex justify-end gap-2">
            <AlertDialog.Cancel asChild>
              <Button varyant="ikincil">Vazgeç</Button>
            </AlertDialog.Cancel>
            <AlertDialog.Action asChild>
              <Button varyant={yikici ? 'yikici' : 'birincil'} onClick={onayla}>
                {onayEtiketi}
              </Button>
            </AlertDialog.Action>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  );
}
