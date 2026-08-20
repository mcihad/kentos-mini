import * as RadixTabs from '@radix-ui/react-tabs';
import { useCallback, useEffect, useLayoutEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { cn } from './utils';
import { haptic } from '../data/haptics';

export type TabItem<T extends string> = {
  deger: T;
  etiket: ReactNode;
  /** Sağda küçük sayaç — "Bekleyenler 12". */
  sayi?: number;
  ikon?: ReactNode;
};

/**
 * SEKMELER — uygulamanın TEK sekme standardı.
 *
 * <h3>Neden yeniden yazıldı</h3>
 * <p>
 * Sekme şeridi <b>on ayrı yerde</b> vardı: bu bileşen ve dokuz ekranın elle
 * kopyaladığı, birbirinin aynı bir sınıf dizisi. Aynı görüntünün on kopyası,
 * hiçbirinin sahibi olmaması demek — biri düzeltilince ötekiler geride
 * kalıyordu.
 * </p>
 *
 * <h3>Neden görüntü değişti</h3>
 * <p>
 * Eski hâl <b>dolu marka hapıydı</b>: gölgeli, tam doygun lacivert. Üç
 * kusuru vardı ve kullanıcı "sekmeler çok kaba" derken bunları görüyordu.
 * </p>
 * <ol>
 *   <li><b>Ağırlık çakışması.</b> Bölüm değiştiren bir kontrol, aynı ekrandaki
 *       "Tamamla" düğmesiyle aynı görsel ağırlıktaydı. Sekme gezinmedir,
 *       taahhüt değil; birincil düğmeden yüksek sesle konuşmamalı.</li>
 *   <li><b>Yazı çok küçüktü.</b> Etiket <code>text-xs</code>, sayaç
 *       <code>text-3xs</code> — 14px tabanda 11.6px ve 9.5px. Ekranın en geniş
 *       etkileşimli öğesini gövde metninin iki kademe altına kurmak, onu
 *       süzgeç çipi sırasına benzetiyordu.</li>
 *   <li><b>Geometri.</b> 40px yüksekliğinde bir öğede 3.5px yarıçap
 *       "neredeyse kare ama tam değil" okunur — en kaba çözüm.</li>
 * </ol>
 *
 * <h3>Yeni anatomi</h3>
 * <p>
 * Etkin sekme artık <b>kayan altın çizgiyle</b> işaretleniyor: çukur yatağın
 * iç alt kenarına oturan 2.5px'lik bir kural, sekmeler arasında
 * <code>translate3d</code> ile kayıyor. Bu bir icat değil — uygulamanın
 * <b>mobil alt çubuğu</b> tam olarak böyle çalışıyor. İki sekme sistemi bu
 * sayede aynı dili konuşuyor: <b>altın, "buradasın" demenin yolu</b>.
 * </p>
 * <p>
 * <b>Aktif sekme YÜKSELİR, renklenmez.</b> Bir dönem zemini
 * <c>--brand-soft</c> idi ve yatak <c>--sunken</c>: ikisi de markanın ~%%9
 * karışımı, yani neredeyse aynı açıklıkta. Sonuç, kullanıcının tarifiyle,
 * "arka planla ön planın karışması"ydı — hangi sekmede olduğunuz ancak yazı
 * ağırlığından anlaşılıyordu. Artık aktif sekme <c>--surface</c> zeminle
 * çukur yataktan yükseliyor ve <c>--sh-2</c> ile ayrılıyor; aynı dil
 * <c>SegmentedSelect</c>'te zaten kullanılıyordu, iki kontrol nihayet aynı
 * şeyi söylüyor.
 * </p>
 *
 * <h3>Davranış</h3>
 * <ul>
 *   <li>Şerit tam genişliği doldurur; sığdıkları sürece eşit paylaşır.</li>
 *   <li>Taşarsa kaydırılır, çubuk gizlenir, kesilmiş sekme kalmaz.</li>
 *   <li>Etkin sekme kendi kendine görünür alana kayar.</li>
 *   <li><code>prefers-reduced-motion</code> açıkken gösterge kaymaz, atlar.</li>
 * </ul>
 */
export function Tabs<T extends string>({
  sekmeler,
  deger,
  degistir,
  className,
}: {
  sekmeler: TabItem<T>[];
  deger: T;
  degistir: (d: T) => void;
  className?: string;
}) {
  const { yatakRef, gosterge } = useSekmeGostergesi(deger);

  return (
    <SekmeYatagi ref={yatakRef} gosterge={gosterge} className={className} rol="tablist">
      {sekmeler.map((s) => {
        const aktif = s.deger === deger;
        return (
          <button
            key={s.deger}
            role="tab"
            aria-selected={aktif}
            data-aktif={aktif || undefined}
            onClick={() => {
              // Sekme değişimi bir SEÇİM: en hafif desen (8ms).
              haptic('secim');
              degistir(s.deger);
            }}
            className={sekmeSinifi(aktif)}
          >
            {s.ikon}
            {s.etiket}
            {s.sayi != null && <Sayac deger={s.sayi} aktif={aktif} />}
          </button>
        );
      })}
    </SekmeYatagi>
  );
}

/**
 * Radix <code>Tabs.List</code> karşılığı — panel yönetimini Radix'e bırakan
 * ekranlar için.
 *
 * <p>
 * Dokuz ekran <code>Tabs.Root/List/Trigger</code> kullanıyor ve panel
 * geçişini Radix yönetiyor. Onları kontrollü bileşene çevirmek, çalışan bir
 * erişilebilirlik uygulamasını elle yeniden yazmak olurdu. Bunun yerine
 * <b>aynı anatomi</b> Radix tetikleyicileri için de veriliyor: görüntü ve
 * gösterge tek yerden gelir, davranış Radix'te kalır.
 * </p>
 */
export function SekmeListesi({
  etiket,
  className,
  children,
}: {
  etiket: string;
  className?: string;
  children: ReactNode;
}) {
  /*
    ETKİN SEKME PROP OLARAK İSTENMİYOR.

    Dokuz ekranın yarısı Radix'i `defaultValue` ile, yani KONTROLSÜZ
    kullanıyor: etkin sekme React durumunda değil, yalnızca DOM'da. Prop
    istemek o ekranları gereksizce kontrollü hâle getirmek olurdu. Gösterge
    zaten DOM'u ölçüyor; değişimi de DOM'dan dinliyor.
  */
  const { yatakRef, gosterge } = useSekmeGostergesi();

  return (
    <RadixTabs.List asChild aria-label={etiket}>
      <SekmeYatagi ref={yatakRef} gosterge={gosterge} className={className}>
        {children}
      </SekmeYatagi>
    </RadixTabs.List>
  );
}

/** Radix tetikleyicisi — <see cref="Tabs"/> ile birebir aynı görüntü. */
export function SekmeTetigi({
  deger,
  ikon,
  sayi,
  children,
}: {
  deger: string;
  ikon?: ReactNode;
  sayi?: number;
  children: ReactNode;
}) {
  return (
    <RadixTabs.Trigger value={deger} className={sekmeSinifi(false)}>
      {ikon}
      {children}
      {sayi != null && <Sayac deger={sayi} />}
    </RadixTabs.Trigger>
  );
}

// ── ortak parçalar ───────────────────────────────────────────────────────

/**
 * Sekme sınıfı.
 *
 * <p>
 * Etkin durum HEM <code>data-[state=active]</code> (Radix) HEM
 * <code>aria-selected</code> (kontrollü bileşen) ile yazılıyor: iki API tek
 * sınıf dizisini paylaşsın diye. <code>aktif</code> parametresi yalnızca
 * kontrollü tarafta anlamlı; Radix kendi durumunu kendi yazıyor.
 * </p>
 */
function sekmeSinifi(aktif: boolean) {
  return cn(
    // `flex-1` + `basis-0`: sığdıkları sürece eşit bölüşürler. `min-w-max`
    // taşmada etiketin kırpılmasını engeller.
    'group relative flex min-w-max flex-1 basis-0 items-center justify-center gap-2',
    'h-11 rounded-sm px-3.5 text-base transition-colors',

    // Yazı AĞIRLIKLA da ayrışıyor, yalnızca renkle değil: güneş altındaki bir
    // telefonda renk farkı ilk kaybolan şey.
    'font-semibold tracking-[-0.005em]',
    'text-ink-3 hover:bg-surface/60 hover:text-ink-2',

    // Etkin sekme çukur yataktan YÜKSELİYOR: koyu temada marka tonu tek
    // başına yetiyor ama açık temada `brand-soft` ile `sunken` birbirine
    // çok yakın. Gölge, iki temada da aynı "kaldırılmış" hissi veriyor.
    'data-[state=active]:bg-surface data-[state=active]:font-bold data-[state=active]:text-brand data-[state=active]:shadow-2',
    'aria-selected:bg-surface aria-selected:font-bold aria-selected:text-brand aria-selected:shadow-2',

    // Odak halkası: klavyeyle gezenin nerede olduğu görünmeli.
    'outline-hidden focus-visible:ring-2 focus-visible:ring-brand focus-visible:ring-inset',
    aktif && 'bg-surface font-bold text-brand shadow-2',
  );
}

/**
 * Sayaç rozeti.
 *
 * Önceden çıplak bir sayıydı ve <code>text-3xs</code> (9.5px) ile
 * yazılıyordu — etiketin devamı mı, ayrı bir bilgi mi olduğu belli değildi.
 * Rozet hâlinde ikisi de çözülüyor.
 */
function Sayac({ deger, aktif }: { deger: number; aktif?: boolean }) {
  return (
    <span
      className={cn(
        'grid h-5 min-w-5 place-items-center rounded-pill px-1.5 text-2xs font-semibold tabular-nums',
        'bg-line/60 text-ink-3',
        'group-data-[state=active]:bg-brand group-data-[state=active]:text-on-brand',
        'group-aria-selected:bg-brand group-aria-selected:text-on-brand',
        aktif && 'bg-brand text-on-brand',
      )}
    >
      {deger}
    </span>
  );
}

/** Çukur yatak + kayan altın gösterge. */
const SekmeYatagi = ({
  ref,
  gosterge,
  className,
  rol,
  children,
}: {
  ref: React.Ref<HTMLDivElement>;
  gosterge: { sol: number; genislik: number } | null;
  className?: string;
  rol?: 'tablist';
  children: ReactNode;
}) => (
  <div
    ref={ref}
    role={rol}
    className={cn(
      // `sekme-serit`: kaydırma çubuğunu gizler ama kaydırmayı bırakır.
      'sekme-serit relative flex gap-1 overflow-x-auto rounded-md border border-line bg-sunken p-1',
      className,
    )}
  >
    {children}

    {/*
      KAYAN GÖSTERGE — mobil alt çubuğun aynısı.

      Her sekmede ayrı bir öğe belirip kaybolmuyor; TEK öğe kayıyor. Böylece
      hareket sürekli ve `translate3d` sayesinde ekran kartında çalışıyor —
      `left` canlandırmak her karede yeniden yerleşim demekti.

      Yatağın İÇİNDE ve kaydırma içeriğiyle birlikte hareket ediyor:
      `absolute` konumu `offsetLeft`ten geliyor, dolayısıyla taşan bir şerit
      kaydırıldığında gösterge sekmesinin altında kalıyor.
    */}
    {gosterge && (
      <span
        aria-hidden
        className="pointer-events-none absolute bottom-1 h-[2.5px] rounded-pill bg-accent transition-transform duration-260 ease-[cubic-bezier(.22,1,.36,1)] motion-reduce:transition-none"
        style={{
          left: 0,
          width: gosterge.genislik,
          transform: `translate3d(${gosterge.sol}px, 0, 0)`,
        }}
      />
    )}
  </div>
);

/**
 * Etkin sekmeyi ölçüp göstergeyi yerleştirir.
 *
 * <p>
 * Ölçüm <b>DOM'dan</b> okunuyor, indeksten hesaplanmıyor: sekmeler eşit
 * genişlikte olmayabilir (uzun etiket + sayaç rozeti) ve şerit taşabilir.
 * İndeks × sabit genişlik varsayımı, tam da bu iki durumda kayardı.
 * </p>
 */
function useSekmeGostergesi(deger?: string) {
  const yatakRef = useRef<HTMLDivElement>(null);
  const [gosterge, setGosterge] = useState<{ sol: number; genislik: number } | null>(null);

  const olc = useCallback(() => {
    const yatak = yatakRef.current;
    if (!yatak) return;

    const aktif = yatak.querySelector<HTMLElement>(
      '[data-state="active"], [aria-selected="true"], [data-aktif="true"]',
    );
    if (!aktif) return;

    // Çizgi sekmenin TAMAMI kadar değil, etiketin altına oturacak kadar
    // geniş: kenardan kenara bir kural, sekmeyi sekmeden ayıran boşluğu da
    // kaplayıp bitişik iki sekmeyi tek parça gösteriyordu.
    const genislik = Math.max(18, Math.round(aktif.offsetWidth * 0.42));
    const sol = Math.round(aktif.offsetLeft + (aktif.offsetWidth - genislik) / 2);

    setGosterge((eski) =>
      eski && eski.sol === sol && eski.genislik === genislik ? eski : { sol, genislik },
    );
  }, []);

  // `useLayoutEffect`: boyama ÖNCESİ ölçülüyor, yoksa gösterge ilk karede
  // sol kenarda bir kez çakıp yerine kayıyordu.
  useLayoutEffect(olc, [olc, deger]);

  useEffect(() => {
    const yatak = yatakRef.current;
    if (!yatak) return;

    /*
      GÖZLEMCİLER İSTEĞE BAĞLI.

      Gösterge bir İYİLEŞTİRME: yoksa sekmeler zemin ve yazı ağırlığıyla
      zaten ayrışıyor. `ResizeObserver` jsdom'da tanımlı değil ve doğrudan
      kurmak bütün ekran testlerini render sırasında düşürüyordu — bir
      süslemenin uygulamayı çökertebilmesi kabul edilemez. Aynı koruma
      eski tarayıcılar için de geçerli.
    */
    const gozlemci =
      typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(olc);

    if (gozlemci) {
      gozlemci.observe(yatak);
      for (const c of yatak.children) gozlemci.observe(c);
    }

    // Radix etkin sekmeyi `data-state` niteliğiyle yazıyor; kontrolsüz
    // kullanımda React'in haberi olmuyor. Niteliği doğrudan dinlemek, iki
    // kullanım biçimini de tek yoldan destekliyor.
    const nitelikGozlemcisi =
      typeof MutationObserver === 'undefined' ? null : new MutationObserver(olc);

    nitelikGozlemcisi?.observe(yatak, {
      subtree: true,
      attributes: true,
      attributeFilter: ['data-state', 'aria-selected', 'data-aktif'],
    });

    const aktif = yatak.querySelector<HTMLElement>(
      '[data-state="active"], [aria-selected="true"], [data-aktif="true"]',
    );
    // jsdom'da `scrollIntoView` de yok.
    aktif?.scrollIntoView?.({ inline: 'nearest', block: 'nearest', behavior: 'smooth' });

    return () => {
      gozlemci?.disconnect();
      nitelikGozlemcisi?.disconnect();
    };
  }, [olc, deger]);

  return { yatakRef, gosterge };
}
