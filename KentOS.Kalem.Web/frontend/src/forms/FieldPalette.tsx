import { Plus } from 'lucide-react';
import { PALETTE_GROUPS } from './fieldTypes';

/**
 * ALAN PALETİ — tasarımcının soru kaynağı.
 *
 * <p>
 * <b>Sürükle-bırak DEĞİL, tıkla-ekle.</b> Paletten tuvale sürüklemek
 * masaüstünde şık ama telefonda hiç çalışmıyor ve bu araç telefonda da
 * kullanılacak. Tıklama, alanı seçili grubun sonuna ekliyor; sıralama
 * tuvalde sürükleyerek yapılıyor (orada sürükleme doğal, çünkü hedef
 * görünür).
 * </p>
 */
export function FieldPalette({ ekle }: { ekle: (tip: number) => void }) {
  return (
    <div className="space-y-4">
      {PALETTE_GROUPS.map(({ grup, tipler }) => (
        <div key={grup}>
          <p className="mb-1.5 text-2xs font-semibold tracking-wide text-ink-3">
            {grup.toLocaleUpperCase('tr-TR')}
          </p>

          <div className="grid grid-cols-2 gap-1.5 lg:grid-cols-1">
            {tipler.map((t) => (
              <button
                key={t.tip}
                type="button"
                onClick={() => ekle(t.tip)}
                title={t.ipucu || t.ad}
                className="group flex min-h-11 items-center gap-2 rounded-md border border-line
                  bg-surface px-2.5 py-2 text-left transition-colors hover:border-brand-2 hover:bg-brand-soft"
              >
                <t.ikon size={16} className="shrink-0 text-ink-3 group-hover:text-brand" />
                <span className="min-w-0 flex-1 truncate text-xs font-medium">{t.ad}</span>
                <Plus size={13} className="shrink-0 text-ink-3 opacity-0 group-hover:opacity-100" />
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
