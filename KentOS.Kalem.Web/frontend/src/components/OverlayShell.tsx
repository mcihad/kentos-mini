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
  masaustuYerlesim = 'orta',
  disaTiklaKapatir = false,
  basligaEk,
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
   * MASAÜSTÜ yerleşimi. Mobil dal her zaman alttan gelen tabakadır — bu
   * prop oraya karışmaz.
   *
   * <p>
   * <c>yan</c>: sağa yaslı, tam boy panel. Yardım ve tema tasarımcısı
   * bunu istiyor: yardım okunurken arkadaki ekranın görünmesi gerekiyor
   * (kullanıcı anlatılan düğmeyi aynı anda görebilsin) ve ortalanmış bir
   * pencere tam da onu kapatıyor.
   * </p>
   */
  masaustuYerlesim?: 'orta' | 'yan';
  /**
   * Perdeye tıklamak katmanı KAPATSIN mı?
   *
   * <p>
   * <b>Varsayılan `false` ve bu bilinçli.</b> Katmanların çoğu FORM ve yarısı
   * doldurulmuş bir formu yanlış bir tıkla kaybetmek, diyalogla çalışmanın en
   * sinir bozucu tarafı. Kapının açık olduğu yerler, kaybedilecek bir girdisi
   * OLMAYAN panellerdir: tema tasarımcısı değişikliği anında uyguluyor,
   * yardım paneli yalnızca okunuyor. Oralarda perdeye tıklamak "kapat"
   * demenin en doğal yolu ve kapalı olması kullanıcıyı düğme aramaya
   * zorluyordu.
   * </p>
   * <p>
   * Mobil dalı etkilemez: orada kapatma zaten parmakla aşağı kaydırarak
   * yapılıyor (bkz. <i>MOBİLDE HER TABAKA PARMAKLA KAPANIR</i>).
   * </p>
   */
  disaTiklaKapatir?: boolean;
  /**
   * Başlık şeridine, kapat düğmesinin SOLUNA giren ek eylem (ör. tema
   * panelindeki "varsayılana sıfırla").
   */
  basligaEk?: ReactNode;
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
  const baslikSeridi = (Baslik: typeof Dialog.Title, aciklamaGoster: boolean) => (
    <div
      /*
        Şerit MASAÜSTÜNDE de `cursor-grab` taşımaz; sürükleme mobile ait.
        `vaul` sürüklemeyi kaydırılabilir gövdeye bırakıyor, bu yüzden
        tabakayı parmakla indirmenin güvenilir yeri bu şerit.
      */
      className={cn(
        /*
          Şerit `items-center`: eskiden `items-start` idi ve açıklaması
          olmayan tabakalarda başlık ile kapatma düğmesi hizasız kalıyordu.
        */
        'flex shrink-0 items-center gap-2.5 border-b border-line px-4 py-2.5',
        // Zemin ve köşe ŞERİDİN DEĞİL, sarmalayıcısının işi: mobilde tutamak
        // da aynı renkli bloğun içinde olmalı.
        //
        // MOBİLDE ÜST BOŞLUK DAHA FAZLA çünkü tutamak akışta değil, şeridin
        // üzerine bindirilmiş; `pt-[21px]` ona hem yer açıyor hem de başlıkla
        // arasında nefes bırakıyor. Tutamağı kendi satırına koymak tepeyi iki
        // ayrı banda bölüyordu.
        !masaustu && 'cursor-grab pt-[21px] active:cursor-grabbing',
      )}
    >
      {ikon && (
        <span className="mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-sm bg-brand-soft text-brand">
          {ikon}
        </span>
      )}
      <div className="min-w-0 flex-1">
        <Baslik asChild>
          {/* Şartname §6.5: tabaka başlığı `title2` (19/700). */}
          <h2 className="font-display text-xl font-bold">{baslik}</h2>
        </Baslik>
        {aciklama && aciklamaGoster && (
          <p className="mt-0.5 text-2xs leading-[1.45] text-ink-3">{aciklama}</p>
        )}
      </div>
      {/* Kapatma düğmesi sürükleme alanının DIŞINDA: aksi hâlde düğmeye
          basmak tabakayı aşağı çekmeye başlıyor ve tık kaybediliyordu.

          SIRA AÇIKÇA YATAY. Kap düz bir `span`ken tek düğmeyle sorun
          yoktu; `basligaEk` ikinci bir düğme koyunca ikisi ALT ALTA
          düşüyordu (tema panelinde sıfırla ve kapat böyle kaydı). */}
      <span data-vaul-no-drag className="flex shrink-0 items-center gap-0.5">
        {/*
          Kenarlıksız ve YUVARLAK: başlığın yanındaki tek düğme çerçeveliyken
          tabakanın tepesine yapıştırılmış bir kutu gibi duruyordu. Çerçeve
          gidince görünür ağırlığı ikonun KENDİSİ taşımak zorunda — 16
          piksellik çarpı şeridin içinde kayboluyordu.
        */}
        {basligaEk}
        <IconButton
          etiket="Kapat"
          varyant="sade"
          onClick={kapat}
          className="-mr-1 h-9 w-9 rounded-full"
        >
          <X size={18} />
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
            /*
              `aria-describedby`'ı ANCAK açıklama yoksa siliyoruz. Radix,
              anahtarın VARLIĞINA bakıyor (değerine değil): koşulsuz
              `undefined` geçmek, aşağıdaki `Drawer.Description` bağını da
              koparırdı.
            */
            {...(aciklama ? {} : { 'aria-describedby': undefined })}
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

                Kendi satırında dururken tepe iki ayrı banda bölünüyordu:
                üstte renkli ama boş bir şerit, altında başlık. Artık başlık
                şeridinin üzerine bindirilmiş; ona yer açan şey şeridin
                `pt-[21px]`'i.
              */}
              <Drawer.Handle className="!absolute !inset-x-0 !top-[9px] !z-10 !mx-auto !my-0 !h-1 !w-9 !bg-line-2" />
              {/*
                AÇIKLAMA MOBİLDE BAŞLIĞA GİRMİYOR — yalnızca ekran okuyucuya.

                İki satırlık bir tepe, telefonda görünür alanın onda birini
                yardım metnine ayırmak demekti. Cümle formu açıklıyor ama
                formu doldurmak için gerekli değil; masaüstünde yer bol,
                orada duruyor.
              */}
              {aciklama && (
                <Drawer.Description className="sr-only">{aciklama}</Drawer.Description>
              )}
              {baslikSeridi(Drawer.Title, false)}
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
          onPointerDownOutside={(e) => { if (!disaTiklaKapatir) e.preventDefault(); }}
          onInteractOutside={(e) => { if (!disaTiklaKapatir) e.preventDefault(); }}
          aria-describedby={undefined}
          className={cn(
            'katman fixed z-50 flex flex-col bg-surface shadow-3',
            masaustuYerlesim === 'yan'
              // Sağdan kayarak gelir: panel kenara yapışık olduğu için
              // ortadan büyüyen `anim-tabaka` yanlış yerden geliyormuş gibi
              // duruyordu.
              ? 'anim-panel anim-yan inset-y-0 right-0 rounded-l-win border-l border-line w-[min(460px,100vw)]'
              : 'anim-tabaka left-1/2 top-1/2 max-h-[88dvh] -translate-x-1/2 -translate-y-1/2 rounded-win',
            masaustuYerlesim === 'orta' && genislik === 'dar' && 'w-[min(460px,calc(100vw-48px))]',
            masaustuYerlesim === 'orta' && genislik === 'orta' && 'w-[min(620px,calc(100vw-48px))]',
            masaustuYerlesim === 'orta' && genislik === 'genis' && 'w-[min(860px,calc(100vw-48px))]',
          )}
        >
          {/* Masaüstünde tutamak yok; blok yalnızca şeridi taşıyor ama zemin
              ve köşe yine burada — iki dal aynı görünsün. */}
          <div
            className={cn(
              'shrink-0 overflow-hidden bg-brand-soft',
              masaustuYerlesim === 'yan' ? 'rounded-tl-win' : 'rounded-t-win',
            )}
          >
            {/* Masaüstünde açıklama şeritte duruyor: orada yer kıt değil. */}
            {baslikSeridi(Dialog.Title, true)}
          </div>
          {children}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
