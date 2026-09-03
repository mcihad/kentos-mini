import * as AlertDialog from '@radix-ui/react-alert-dialog';
import { useState } from 'react';
import { Button } from '../components/Button';
import { RECURRENCE_SCOPE, RECURRENCE_SCOPE_OPTIONS, type RecurrenceScope } from './types';

/**
 * Tekrarlanan bir etkinlik taşındığında kapsamı sorar — design.md dilinde
 * Radix AlertDialog (Esc ve overlay ile kapanır, odak tuzaklı).
 *
 * Varsayılan "yalnızca bu": sunucudaki varsayılanla aynı ve en az yıkıcı olan.
 */
export function ScopePrompt({
  acik,
  baslik,
  onay,
  iptal,
}: {
  acik: boolean;
  baslik: string;
  onay: (kapsam: RecurrenceScope) => void;
  iptal: () => void;
}) {
  const [secim, setSecim] = useState<RecurrenceScope>(RECURRENCE_SCOPE.yalnizca);

  return (
    <AlertDialog.Root open={acik} onOpenChange={(a) => !a && iptal()}>
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde" />
        <AlertDialog.Content className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(440px,calc(100vw-32px))] -translate-x-1/2 -translate-y-1/2 rounded-win bg-surface p-5 shadow-3">
          <AlertDialog.Title className="font-display text-lg font-bold">
            Tekrarlanan etkinlik
          </AlertDialog.Title>
          <AlertDialog.Description className="mt-1 text-sm text-text-2 metin-guzel">
            “{baslik}” bir serinin parçası. Bu değişiklik nereye uygulansın?
          </AlertDialog.Description>

          <div className="my-4 space-y-1.5">
            {RECURRENCE_SCOPE_OPTIONS.map((s) => (
              <label
                key={s.deger}
                className="flex cursor-pointer items-start gap-2.5 rounded-control border border-border p-2.5 hover:bg-surface-2 has-checked:border-brand has-checked:bg-brand-tint"
              >
                <input
                  type="radio"
                  name="kapsam"
                  className="mt-0.5 accent-(--brand)"
                  checked={secim === s.deger}
                  onChange={() => setSecim(s.deger)}
                />
                <span>
                  <span className="block text-sm font-medium">{s.etiket}</span>
                  <span className="block text-xs text-text-3">{s.aciklama}</span>
                </span>
              </label>
            ))}
          </div>

          <div className="flex justify-end gap-2">
            <AlertDialog.Cancel asChild>
              <Button varyant="ikincil">Vazgeç</Button>
            </AlertDialog.Cancel>
            <Button onClick={() => onay(secim)}>Uygula</Button>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  );
}
