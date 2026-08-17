import * as Dialog from '@radix-ui/react-dialog';
import { BookOpen, X } from 'lucide-react';
import { IconButton } from '../components/Button';
import { Markdown } from './Markdown';

/**
 * YARDIM PANELİ.
 *
 * Masaüstünde SAĞDAN açılan bir tabaka, telefonda alttan gelen bir sayfa.
 * Ekranın üstünü kapatan ortalanmış bir pencere değil: yardım okunurken
 * arkadaki ekranın görünmesi gerekiyor — kullanıcı anlatılan düğmeyi aynı anda
 * görebilsin.
 *
 * İçerik `metinler/*.md` dosyalarından geliyor; yazan kişi React bilmeden
 * güncelleyebilsin diye düz markdown.
 */
export function HelpPanel({
  acik,
  kapat,
  baslik,
  ozet,
  metin,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  ozet?: string;
  metin: string;
}) {
  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-[rgba(11,26,58,0.42)] backdrop-blur-[2px]" />
        <Dialog.Content
          aria-describedby={undefined}
          className="katman anim-tabaka anim-yan fixed inset-x-0 bottom-0 z-50 flex max-h-[86dvh] flex-col rounded-t-xl border border-border bg-surface shadow-2
            md:inset-y-0 md:left-auto md:right-0 md:max-h-none md:w-[min(460px,100vw)] md:rounded-none md:rounded-l-xl md:border-y-0 md:border-r-0"
        >
          {/* Telefonda tutamak: sayfanın aşağı çekilebileceğini söyler. */}
          <span
            aria-hidden
            className="mx-auto mt-2.5 h-[4px] w-[38px] shrink-0 rounded-full bg-border-2 md:hidden"
          />

          <div className="flex items-start gap-3 border-b border-border p-4 md:p-5">
            <span
              className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-gold-tint text-gold-2"
              aria-hidden
            >
              <BookOpen size={17} />
            </span>
            <div className="min-w-0 flex-1">
              <Dialog.Title className="font-display text-lg font-bold tracking-[-0.01em]">
                {baslik}
              </Dialog.Title>
              {ozet && <p className="mt-0.5 text-xs leading-normal text-text-3">{ozet}</p>}
            </div>
            <Dialog.Close asChild>
              <IconButton etiket="Yardımı kapat">
                <X size={17} />
              </IconButton>
            </Dialog.Close>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain p-4 md:p-5">
            <Markdown metin={metin} />

            <p className="mt-8 border-t border-border pt-3 text-xs leading-[1.6] text-text-3">
              Anlatılanı ekranda bulamadıysanız yetkiniz kapalı olabilir; yetki
              tanımları Akıllı Şehir ve Kent Bilgi Sistemleri Müdürlüğü'nden açılır.
            </p>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
