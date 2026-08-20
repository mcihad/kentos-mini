import { AlertTriangle, ChevronDown, Layers, Map as MapIcon, SlidersHorizontal } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { EmptyState } from '../components/EmptyState';
import { FilterSection, FilterSheet, Segment } from '../components/FilterSheet';
import { SegmentedSelect } from '../components/Filters';
import { Skeleton } from '../components/Skeleton';
import { Switch } from '../components/Switch';
import { PERMISSION } from '../components/permissions';
import { useIsDesktop } from '../components/screenSize';
import { cn } from '../components/utils';
import { useSession } from '../auth/SessionProvider';
import { useMapPoints } from '../data/citizen';
import { UnitScopePicker } from '../components/UnitScopePicker';
import { WorkMap } from './map/WorkMap';

type Kapsam = 'kendi' | 'alt';

/**
 * İŞ HARİTASI — birimin işi coğrafi olarak.
 *
 * <h3>Mobilde harita EKRANIN KENDİSİ</h3>
 * <p>
 * Önceki hâlinde ekranın üstü <b>dört sıra denetimdi</b> (birim seçici,
 * kapsam segmenti, iki anahtar) ve harita 460px'lik bir kutu olarak
 * altlarında duruyordu: 390px'lik bir telefonda haritanın yarısı kıvrımın
 * altında kalıyor, kalan yarısı da kenarlarda 16px boşlukla çevriliydi.
 * Harita ekranında haritadan başka her şeye yer vardı.
 * </p>
 * <p>
 * Şimdi harita <b>kenardan kenara</b> ve boydan boya: kabuğun yan dolgusu
 * iptal ediliyor, yükseklik görüş alanından üst şerit ile alt gezinme
 * çubuğu düşülerek hesaplanıyor. Denetimler haritanın <b>üzerine binen</b>
 * bir katmana taşındı — üstte tek bir süzgeç düğmesi, altta sayaç ve
 * gösterge. Süzgeçlerin tamamı alt tabakada; uygulamanın liste ekranlarında
 * zaten kurulmuş olan gramerin aynısı.
 * </p>
 *
 * <p>
 * <b>Bekleyen bildirimler isteğe bağlı katman.</b> Karşılama personeli için
 * değerli — aynı sokakta biriken üç bildirim haritada tek bakışta görünüyor
 * ve mükerrer olduğu anlaşılıyor. Varsayılan olarak kapalı: saha personeli
 * yalnızca kendi işlerine bakıyor.
 * </p>
 */
export default function WorkMapScreen() {
  const { hasPermission } = useSession();
  const masaustu = useIsDesktop();

  const [kapsam, setKapsam] = useState<Kapsam>('kendi');
  const [yalnizAcik, setYalnizAcik] = useState(true);
  const [bildirimler, setBildirimler] = useState(false);
  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [gostergeAcik, setGostergeAcik] = useState(false);
  /*
    Yükseklik İKİ GÖRÜNÜMDE DE ölçülüyor.

    Masaüstünde sabit bir `calc(100dvh - 280px)` yazılmıştı ve haritanın
    üstünde başka bir şey çizildiğinde (bildirim izni kartı) harita
    pencereden taşıyor, altındaki gösterge kesiliyordu. Ölçüm ikisini de
    çözüyor; fark yalnızca altta ne kadar yer ayrıldığı.
  */
  const { kap: haritaKabi, yukseklik: haritaYuksekligi } = useKalanYukseklik(
    masaustu ? { rezerv: 44 } : { tabbar: true },
  );

  const { data: noktalar, isLoading } = useMapPoints({
    altBirimlerDahil: kapsam === 'alt',
    yalnizAcik,
    bildirimlerDahil: bildirimler,
  });

  const liste = noktalar ?? [];
  const geciken = liste.filter((n) => n.gecikti).length;
  const bildirimYetkisi = hasPermission(PERMISSION.bildirimKarsila);

  /*
    ETKİN SÜZGEÇ SAYISI — düğmenin üzerindeki rozet.
    Varsayılandan SAPMA sayılıyor: "yalnızca açık işler" açık olması normal
    hâl, kapatılması bir seçim.
  */
  const etkinSuzgec =
    (kapsam === 'alt' ? 1 : 0) + (yalnizAcik ? 0 : 1) + (bildirimler ? 1 : 0);

  // ── Masaüstü: denetimler üstte, harita altta ve uzun ────────────────
  if (masaustu) {
    return (
      <div className="space-y-3">
        <div className="flex flex-wrap items-center gap-2">
          <UnitScopePicker />

          <SegmentedSelect<Kapsam>
            deger={kapsam}
            degistir={setKapsam}
            etiket="Kapsam"
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              { deger: 'alt', etiket: 'Alt birimler' },
            ]}
          />

          <Switch isaretli={yalnizAcik} degistir={setYalnizAcik} etiket="Yalnızca açık işler" />

          {bildirimYetkisi && (
            <Switch
              isaretli={bildirimler}
              degistir={setBildirimler}
              etiket="Bekleyen bildirimler"
            />
          )}

          <Sayac toplam={liste.length} geciken={geciken} className="ml-auto" />
        </div>

        {isLoading ? (
          <Skeleton className="h-[60dvh] min-h-[420px] w-full" />
        ) : liste.length === 0 ? (
          <BosHarita />
        ) : (
          <div ref={haritaKabi}>
            <WorkMap noktalar={liste} yukseklik={haritaYuksekligi} />

            {/*
              GÖSTERGE MASAÜSTÜNDE HARİTANIN ALTINDA, ÜZERİNDE DEĞİL.

              Üzerine bindirilmiş hâli ölçüldü ve kötüydü: sol alt köşede
              OpenStreetMap künyesiyle çakışıyor, haritanın kırpılan
              köşesinde yarısı kesiliyordu. Telefonda üste binmek zorunlu —
              orada yer yok; masaüstünde yer var ve bindirme yalnızca haritayı
              örtüyordu.
            */}
            <Gosterge bildirimler={bildirimler} className="mt-2.5" />
          </div>
        )}
      </div>
    );
  }

  // ── Mobil: harita tam ekran, denetimler üstünde ─────────────────────
  return (
    /*
      `-mx-4`: kabuğun yan dolgusunu iptal eder. `-mt-4` üst dolguyu, alttaki
      negatif marj ise ana içeriğin tabbar için ayırdığı boşluğu geri alır —
      harita gezinme çubuğunun hemen üstünde biter.
    */
    <div className="-mx-4 -mt-4 mb-[calc(-1*(var(--h-tabbar)+env(safe-area-inset-bottom,0px)+var(--sp-8)))]">
      <div ref={haritaKabi} className="relative" style={{ height: haritaYuksekligi }}>
        {isLoading ? (
          <Skeleton className="h-full w-full rounded-none" />
        ) : liste.length === 0 ? (
          <div className="p-4">
            <BosHarita />
          </div>
        ) : (
          <WorkMap noktalar={liste} yukseklik="100%" cerceve={false} />
        )}

        {/*
          ÜST KATMAN — tek düğme.

          Dört denetimi haritanın üzerine sermek, boşluğu geri kazanmakla
          aynı hatayı yapardı: harita yine örtülü olurdu. Üstte yalnızca
          "neye baktığımı değiştir" düğmesi ve kaç nokta olduğu duruyor;
          gerisi alt tabakada.
        */}
        <div className="pointer-events-none absolute inset-x-0 top-0 flex items-start gap-2 p-3">
          <button
            type="button"
            onClick={() => setSuzgecAcik(true)}
            className="pointer-events-auto inline-flex h-10 items-center gap-2 rounded-pill border border-line bg-surface/92 px-3.5 text-xs font-semibold text-ink shadow-2 backdrop-blur-md active:scale-[0.97]"
          >
            <SlidersHorizontal size={15} />
            Süz
            {etkinSuzgec > 0 && (
              <span className="grid h-5 min-w-5 place-items-center rounded-pill bg-brand px-1 text-3xs font-bold tabular-nums text-on-brand">
                {etkinSuzgec}
              </span>
            )}
          </button>

          <Sayac
            toplam={liste.length}
            geciken={geciken}
            className="pointer-events-auto h-10 rounded-pill border border-line bg-surface/92 px-3.5 shadow-2 backdrop-blur-md"
          />
        </div>

        {/*
          ALT KATMAN — gösterge, katlanmış.

          Renk açıklaması haritanın altında sabit bir satırdı ve tam ekranda
          yeri yok. Açılıp kapanan bir katman: ilk kez bakan bir kullanıcı
          bir kez açar, sonra kapalı kalır — ekranın alt şeridi haritaya
          döner.
        */}
        <div className="pointer-events-none absolute inset-x-0 bottom-0 flex justify-start p-3">
          <div className="pointer-events-auto max-w-full rounded-lg border border-line bg-surface/92 shadow-2 backdrop-blur-md">
            <button
              type="button"
              onClick={() => setGostergeAcik((a) => !a)}
              aria-expanded={gostergeAcik}
              className="flex h-9 w-full items-center gap-1.5 px-3 text-2xs font-semibold text-ink-2"
            >
              <Layers size={14} />
              Gösterge
              <ChevronDown
                size={14}
                className={cn('transition-transform', gostergeAcik && 'rotate-180')}
              />
            </button>

            {gostergeAcik && (
              <Gosterge bildirimler={bildirimler} className="border-t border-line px-3 py-2.5" />
            )}
          </div>
        </div>
      </div>

      {/* ── Süzgeç tabakası ── */}
      <FilterSheet
        acik={suzgecAcik}
        kapat={() => setSuzgecAcik(false)}
        etkinSayisi={etkinSuzgec}
        temizle={() => {
          setKapsam('kendi');
          setYalnizAcik(true);
          setBildirimler(false);
        }}
      >
        <FilterSection baslik="Birim">
          <UnitScopePicker />
        </FilterSection>

        <FilterSection baslik="Kapsam">
          <Segment<Kapsam>
            deger={kapsam}
            degistir={setKapsam}
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              { deger: 'alt', etiket: 'Alt birimler' },
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Katmanlar">
          <div className="space-y-3">
            <Switch
              isaretli={yalnizAcik}
              degistir={setYalnizAcik}
              etiket="Yalnızca açık işler"
              aciklama="Kapatılırsa tamamlanmış ve iptal edilmiş işler de basılır."
            />
            {bildirimYetkisi && (
              <Switch
                isaretli={bildirimler}
                degistir={setBildirimler}
                etiket="Bekleyen vatandaş bildirimleri"
                aciklama="Aynı sokakta biriken bildirimler tek bakışta görünür."
              />
            )}
          </div>
        </FilterSection>
      </FilterSheet>
    </div>
  );
}

/** Nokta sayısı ve gecikme — iki sayı, iki ağırlık. */
function Sayac({
  toplam,
  geciken,
  className,
}: {
  toplam: number;
  geciken: number;
  className?: string;
}) {
  return (
    <span className={cn('inline-flex items-center gap-2 text-2xs tabular-nums', className)}>
      <span className="font-semibold text-ink">{toplam} nokta</span>
      {geciken > 0 && (
        <span className="inline-flex items-center gap-1 font-semibold text-(--st-no)">
          <AlertTriangle size={12} strokeWidth={2.4} />
          {geciken} geciken
        </span>
      )}
    </span>
  );
}

/** Renklerin ne anlama geldiği. Renk durumdan, kırmızı halka gecikmeden. */
function Gosterge({ bildirimler, className }: { bildirimler: boolean; className?: string }) {
  return (
    <div
      className={cn(
        'flex flex-wrap items-center gap-x-4 gap-y-1.5 text-2xs text-ink-2',
        className,
      )}
    >
      {/* Göstergedeki biçim haritadakiyle AYNI: yuvarlak bir nokta, damla
          iğneyi anlatamaz ve iki dil arasında kullanıcıyı çeviri yapmaya
          bırakırdı. */}
      <span className="inline-flex items-center gap-1.5">
        <Damla renk="#1E5FBF" />
        Görev — rengi durumundan
      </span>
      <span className="inline-flex items-center gap-1.5">
        <Damla renk="#1E5FBF" gecikti />
        Süresi aşılmış
      </span>
      {bildirimler && (
        <span className="inline-flex items-center gap-1.5">
          <Damla renk="#A78952" />
          Bekleyen bildirim
        </span>
      )}
    </div>
  );
}

/**
 * Haritanın altında KALAN yerin yüksekliği.
 *
 * <p>
 * Görüş alanından üst şerit ile alt çubuğu düşmek yetmiyor: haritanın
 * üstünde başka bir şey çizilebiliyor (bildirim izni kartı gibi) ve o
 * durumda harita alt çubuğun ALTINA taşıyor. Ölçüldü — göstergenin durduğu
 * alt şerit ekranın dışında kalıyordu, yani kartın kendisi görünmez
 * oluyordu.
 * </p>
 *
 * <p>
 * Öğenin sayfadaki gerçek konumu ancak çalışma anında bilinebilir; salt CSS
 * ile ifade edilemiyor. Bu yüzden tek bir ölçüm yapılıyor ve pencere boyutu
 * değiştikçe (döndürme, klavye) yenileniyor.
 * </p>
 */
function useKalanYukseklik({ tabbar = false, rezerv = 0 }: { tabbar?: boolean; rezerv?: number }) {
  const kap = useRef<HTMLDivElement>(null);
  const [yukseklik, setYukseklik] = useState<string | number>('60dvh');

  useEffect(() => {
    const olc = () => {
      const el = kap.current;
      if (!el) return;

      const ust = el.getBoundingClientRect().top + window.scrollY;

      const cubuk = tabbar
        ? (parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--h-tabbar')) || 64)
          + (parseFloat(getComputedStyle(document.body).paddingBottom) || 0)
        : 0;

      // 280px alt sınır: dar bir pencerede haritanın tamamen ezilmesindense
      // sayfanın biraz kaydırılması yeğ.
      setYukseklik(Math.max(280, Math.round(window.innerHeight - ust - cubuk - rezerv)));
    };

    olc();
    window.addEventListener('resize', olc);

    // Üstteki kart kapatılınca harita büyümeli.
    const gozlemci =
      typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(olc);
    if (gozlemci && document.body) gozlemci.observe(document.body);

    return () => {
      window.removeEventListener('resize', olc);
      gozlemci?.disconnect();
    };
  }, [tabbar, rezerv]);

  return { kap, yukseklik };
}

/** Göstergedeki minyatür iğne — haritadakinin aynısı, küçültülmüş. */
function Damla({ renk, gecikti }: { renk: string; gecikti?: boolean }) {
  return (
    <span
      aria-hidden
      className="block h-3 w-3 rounded-full rounded-bl-none"
      style={{
        background: renk,
        transform: 'rotate(-45deg)',
        boxShadow: `0 0 0 ${gecikti ? 2 : 1.5}px ${gecikti ? 'var(--st-no)' : 'var(--surface)'}`,
      }}
    />
  );
}

function BosHarita() {
  return (
    <EmptyState
      ikon={MapIcon}
      baslik="Haritaya basılacak kayıt yok"
      aciklama="Konumu girilmiş bir görev ya da bildirim bulunmuyor."
    />
  );
}
