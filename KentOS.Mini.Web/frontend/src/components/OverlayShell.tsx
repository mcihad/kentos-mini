import * as Dialog from '@radix-ui/react-dialog';
import { X } from 'lucide-react';
import type { ReactNode } from 'react';
import { Drawer } from 'vaul';
import { IconButton } from './Button';
import { useIsDesktop } from './screenSize';
import { cn } from './utils';

export type OverlayWidth = 'dar' | 'orta' | 'genis';

/**
 * TABAKA KABI — uygulamadaki TEK "üstte açılan pencere" gramerı.
 *
 * <p>
 * Mobilde alttan gelen bir tabaka (<c>vaul</c>), masaüstünde ortalanmış bir
 * pencere (Radix Dialog). Perde, köşe yarıçapı, tutamak, başlık şeridi ve
 * kapatma düğmesi burada bir kez tanımlı; içerik tamamen çağırana ait.
 * </p>
 *
 * <p>
 * <b>Neden ayrı bir bileşen:</b> aynı yapı iki yerde elle yazılıydı ve ikisi
 * ayrışmıştı. Etkinlik penceresi ham Radix Dialog + elle yazılmış CSS
 * animasyonuyla kalmış, mobilde tabaka gibi davranmıyordu: sürüklenerek
 * kapanmıyor, kapanışta zıplıyordu. Aynı düzeltmeyi iki dosyada tekrar etmek,
 * birini unutmayı davet eder — nitekim edilmişti.
 * </p>
 *
 * <p>
 * <b>Açılışta odak bir alana DÜŞMEZ.</b> Radix, açılan katmandaki ilk
 * odaklanabilir öğeye gider; o bir metin alanıysa telefon klavyeyi kaldırır,
 * görünür alan küçülür ve <c>vaul</c> tabakayı giriş animasyonunun ortasında
 * yeniden konumlandırır. Kullanıcının gördüğü şey titremedir. Alana
 * <b>dokunulduğunda</b> aynı yeniden konumlandırma doğru zamanda ve doğru
 * yerden olur; sorun yalnızca istenmeden gelen odaktı.
 * </p>
 *
 * <p>
 * <b>Dışarı tıklayınca kapanmaz</b> (masaüstü): yarısı doldurulmuş bir formu
 * yanlış bir tıkla kaybetmek, diyalogla çalışmanın en sinir bozucu tarafıydı.
 * </p>
 */
export function OverlayShell({
  acik,
  kapat,
  baslik,
  aciklama,
  ikon,
  genislik = 'orta',
  children,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  /** Başlığın altındaki tek satır; ekran okuyucu açıklaması da bundan gelir. */
  aciklama?: string;
  ikon?: ReactNode;
  genislik?: OverlayWidth;
  /**
   * Başlık şeridinin ALTINA gelen her şey. Kendi kaydırma kabını ve varsa
   * eylem çubuğunu içermeli; kap yalnızca dikey bir esnek sütun verir.
   */
  children: ReactNode;
}) {
  const masaustu = useIsDesktop();

  /*
    Başlık şeridi iki dalda da AYNI, tek fark erişilebilirlik başlığının
    hangi bileşenden geldiği: `vaul` kendi Radix Dialog kopyasını taşıyor ve
    bizim ayrıca içe aktardığımız `Dialog.Title` onun bağlamını görmüyor.
    Görünen başlığın KENDİSİ `Title` oluyor; ayrıca `sr-only` bir kopya
    koymak, ekran okuyucuya başlığı iki kez okuturdu.
  */
  const baslikSeridi = (Baslik: typeof Dialog.Title) => (
    <div
      /*
        Şerit MASAÜSTÜNDE de `cursor-grab` taşımaz; sürükleme mobile ait.
        `vaul` sürüklemeyi kaydırılabilir gövdeye bırakıyor, bu yüzden
        tabakayı parmakla indirmenin güvenilir yeri bu şerit.
      */
      className={cn(
        /*
          BAŞLIK ŞERİDİ ALÇALDI.

          `py-2.5` + tutamağın `my-2.5`'i üst üste binince tabakanın tepesi
          neredeyse 70 piksel kaplıyordu ve asıl içerik ekranın aşağısına
          itiliyordu — küçük ekranda görünen alanın beşte biri başlıktı.

          Şerit artık `items-center`: eskiden `items-start` idi ve açıklaması
          olmayan tabakalarda başlık ile kapatma düğmesi hizasız kalıyordu.
        */
        'flex shrink-0 items-center gap-2.5 border-b border-line px-3.5 py-2',
        // Zemin ve köşe ŞERİDİN DEĞİL, `baslikBlogu` sarmalayıcısının işi:
        // mobilde tutamak da aynı renkli bloğun içinde olmalı.
        //
        // Üstteki fazladan boşluk TUTAMAĞA ait: tutamak artık akıştan
        // çıkarılıp şeridin üzerine bindiriliyor, kendi satırını kaplamıyor.
        !masaustu && 'cursor-grab pt-3.5 active:cursor-grabbing',
      )}
    >
      {ikon && (
        <span className="mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-sm bg-brand-soft text-brand">
          {ikon}
        </span>
      )}
      <div className="min-w-0 flex-1">
        <Baslik asChild>
          <h2 className="font-display text-base font-bold tracking-[var(--track-d)]">{baslik}</h2>
        </Baslik>
        {aciklama && <p className="mt-0.5 text-2xs leading-[1.45] text-ink-3">{aciklama}</p>}
      </div>
      {/* Kapatma düğmesi sürükleme alanının DIŞINDA: aksi hâlde düğmeye
          basmak tabakayı aşağı çekmeye başlıyor ve tık kaybediliyordu. */}
      <span data-vaul-no-drag>
        {/* Kenarlıksız: başlığın yanındaki tek düğme çerçeveliyken tabakanın
            tepesine yapıştırılmış bir kutu gibi duruyordu. Kenarlık kalkınca
            16 piksellik çarpı şeridin içinde kayboldu — çerçeve gittiği için
            ikonun KENDİSİ görünür ağırlığı taşımak zorunda. */}
        <IconButton etiket="Kapat" varyant="sade" onClick={kapat}>
          <X size={19} />
        </IconButton>
      </span>
    </div>
  );

  if (!masaustu) {
    return (
      <Drawer.Root open={acik} onOpenChange={(a) => !a && kapat()}>
        <Drawer.Portal>
          {/* Perde bulanık: arkadaki ekran tanınır kalıyor ama okunmuyor —
              kullanıcı nereden geldiğini kaybetmeden dikkatini tabakaya
              veriyor. Düz karartma bağlamı tamamen siliyordu. */}
          <Drawer.Overlay className="fixed inset-0 z-50 bg-perde backdrop-blur-[3px]" />
          <Drawer.Content
            aria-describedby={undefined}
            onOpenAutoFocus={(e) => e.preventDefault()}
            className="fixed inset-x-0 bottom-0 z-50 flex max-h-[92dvh] flex-col rounded-t-tabaka bg-surface shadow-3 outline-none"
          >
            {/*
              TUTAMAK RENKLİ BLOĞUN İÇİNDE.

              Önce tutamak şeridin ÜSTÜNDE, düz yüzey zemininde duruyordu:
              tabakanın tepesinde ince, renksiz bir bant kalıyor ve altındaki
              renkli başlıkla arasında sebepsiz bir kesik oluşuyordu. Tepe
              tek parça olmalı — tutamak başlığın bir parçası, ayrı bir şerit
              değil.
            */}
            <div className="relative shrink-0 overflow-hidden rounded-t-tabaka bg-brand-soft">
              {/*
                TUTAMAK AKIŞTAN ÇIKTI.

                Kendi satırında dururken tepeye ~19 piksel ekliyordu; oysa
                sadece 5 piksellik bir çizgi. Artık şeridin üzerine bindirilmiş
                ve ona yer açan şey şeridin `pt-3.5`'i — aynı görüntü, yarısı
                kadar yükseklik.
              */}
              <Drawer.Handle className="!absolute !inset-x-0 !top-[7px] !z-10 !mx-auto !my-0 !h-[5px] !w-10 !bg-line-2" />
              {baslikSeridi(Drawer.Title)}
            </div>
            {children}
          </Drawer.Content>
        </Drawer.Portal>
      </Drawer.Root>
    );
  }

  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde backdrop-blur-[3px]" />
        <Dialog.Content
          onPointerDownOutside={(e) => e.preventDefault()}
          onInteractOutside={(e) => e.preventDefault()}
          aria-describedby={undefined}
          className={cn(
            'katman anim-tabaka fixed left-1/2 top-1/2 z-50 flex max-h-[88dvh] flex-col rounded-win bg-surface shadow-3',
            '-translate-x-1/2 -translate-y-1/2',
            genislik === 'dar' && 'w-[min(460px,calc(100vw-48px))]',
            genislik === 'orta' && 'w-[min(620px,calc(100vw-48px))]',
            genislik === 'genis' && 'w-[min(860px,calc(100vw-48px))]',
          )}
        >
          {/* Masaüstünde tutamak yok; blok yalnızca şeridi taşıyor ama zemin
              ve köşe yine burada — iki dal aynı görünsün. */}
          <div className="shrink-0 overflow-hidden rounded-t-win bg-brand-soft">
            {baslikSeridi(Dialog.Title)}
          </div>
          {children}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
