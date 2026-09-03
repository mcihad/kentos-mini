import * as echarts from 'echarts/core';
import { BarChart, CustomChart, LineChart, PieChart } from 'echarts/charts';
import {
  DataZoomComponent,
  DatasetComponent,
  GridComponent,
  LegendComponent,
  MarkLineComponent,
  TitleComponent,
  TooltipComponent,
} from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import { useEffect, useMemo, useRef } from 'react';
import { useTheme } from '../theme/ThemeProvider';

/**
 * Apache ECharts sarmalayıcısı.
 *
 * ## Neden ince bir sarmalayıcı
 *
 * `echarts-for-react` eklenmedi: tek işi `useEffect` + `resize` bağlamak ve
 * bunun için ikinci bir bakım yüzeyi (kendi sürüm döngüsü, kendi React
 * uyumluluğu) taşımak gereksiz. Buradaki yüz satır aynı işi yapıyor ve
 * **temayı token'lardan** okuyor — hazır sarmalayıcı bunu bilmezdi.
 *
 * ## Yalnızca gereken modüller
 *
 * `echarts/core` + tek tek grafik/bileşen kaydı kullanılıyor (`echarts` kök
 * paketi değil): kök paket bütün grafik tiplerini, haritaları ve SVG
 * yazıcısını da paketliyor. Bu ekranda çubuk, çizgi ve halka var.
 *
 * ## Renkler token'dan
 *
 * design.md'nin bağlayıcı kuralı: hiçbir bileşen renk yazmaz. ECharts CSS
 * değişkeni anlamadığı için değerler çalışma anında `getComputedStyle` ile
 * çözülüp seçeneklere gömülür ve **tema değişince yeniden** kurulur.
 */
echarts.use([
  BarChart,
  LineChart,
  PieChart,
  // GANTT: hazır bir gantt tipi yok, çubuklar `CustomChart` ile çiziliyor.
  // `DataZoom` uzun projelerde tarih aralığını daraltmak, `MarkLine` ise
  // "bugün" çizgisini basmak için — gecikmeyi çizgiye bakarak okumak,
  // tarihleri tek tek karşılaştırmaktan hızlı.
  CustomChart,
  DataZoomComponent,
  MarkLineComponent,
  GridComponent,
  TooltipComponent,
  LegendComponent,
  TitleComponent,
  DatasetComponent,
  CanvasRenderer,
]);

/** Bir CSS değişkenini gerçek renge çevirir. */
function token(ad: string, yedek: string): string {
  if (typeof window === 'undefined') return yedek;
  const d = getComputedStyle(document.documentElement).getPropertyValue(ad).trim();
  return d || yedek;
}

/**
 * Kurumsal palet.
 *
 * Kategorik dağılımda kaydın KENDİ rengi varsa (tip/durum renkleri) o
 * kullanılır; yoksa bu sıradan seçilir. Altın yalnızca ilk sıradadır —
 * kurumsal kimlikte vurgu rengi, geniş dolgu değil (bkz. design.md).
 */
export function palette(): string[] {
  return [
    /*
      `--brand-ui`, `--brand-2` DEĞİL: `--brand-2` diye bir token hiç yoktu.
      `token()` sessizce yedeğe düşüyor ve serinin EN ÇOK kullanılan rengi
      her iki temada da sabit `#1E5FBF` kalıyordu — ne koyu temaya ne de
      marka düğmesine tepki veriyordu. Paletin geri kalanı dönerken ilk
      rengin dönmemesi, grafikleri temadan kopuk gösteriyordu.
    */
    token('--brand-ui', '#002E6D'),
    token('--gold', '#A78952'),
    token('--st-ok', '#2E7D5B'),
    token('--st-warn', '#B07D2B'),
    token('--st-no', '#B3261E'),
    '#6B7FA8',
    '#8A6FA8',
    '#3F9C9C',
    '#C2703D',
    '#7A8B99',
  ];
}

export type EChartProps = {
  /** ECharts seçenekleri. Renkler `palet()` ve `token()` ile verilmeli. */
  secenekler: Record<string, unknown>;
  yukseklik?: number;
  className?: string;
  /** Erişilebilirlik: grafiğin ne anlattığı. */
  etiket: string;
};

export function EChart({ secenekler, yukseklik = 260, className, etiket }: EChartProps) {
  const kap = useRef<HTMLDivElement>(null);
  const ornek = useRef<echarts.ECharts | null>(null);
  const { tema } = useTheme();

  // Ortak yerleşim: eksen ve ipucu renkleri her grafikte aynı olmalı.
  const temelSecenekler = useMemo(() => {
    const metin = token('--text-2', '#4D4D4F');
    const solgun = token('--text-3', '#8A8A8C');
    const cizgi = token('--border', '#E2E5EA');
    const yuzey = token('--surface', '#FFFFFF');

    return {
      color: palette(),
      textStyle: { fontFamily: 'IBM Plex Sans, sans-serif', color: metin },
      // `animation` kapalı DEĞİL ama kısa: uzun animasyon, sekme değiştirip
      // hemen okumak isteyen kullanıcıyı bekletiyor.
      animationDuration: 320,
      grid: { left: 8, right: 12, top: 16, bottom: 8, containLabel: true },
      tooltip: {
        backgroundColor: yuzey,
        borderColor: cizgi,
        textStyle: { color: metin, fontSize: 12 },
        // Varsayılan gölge çok yumuşak; kart kenarlığıyla aynı dili kullansın.
        extraCssText: 'box-shadow: 0 6px 16px rgba(0,0,0,.10); border-radius: 10px;',
      },
      xAxis: {
        axisLine: { lineStyle: { color: cizgi } },
        axisTick: { show: false },
        axisLabel: { color: solgun, fontSize: 11 },
        splitLine: { show: false },
      },
      yAxis: {
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: { color: solgun, fontSize: 11 },
        splitLine: { lineStyle: { color: cizgi, type: 'dashed' as const } },
      },
    };
  }, [tema]);

  useEffect(() => {
    const dugum = kap.current;
    if (!dugum) return;

    /**
     * BOYUTSUZ KAPTA KURULMAZ.
     *
     * ECharts sıfır genişlik/yükseklikte `Can't get DOM width or height`
     * fırlatıyor ve hata bileşen ağacında yukarı çıkıp BÜTÜN ekranı
     * düşürüyor. Bu yalnızca testlerde değil sahada da olur: `display:none`
     * bir sekmenin içinde ya da henüz yerleşmemiş bir kapta çizilmeye
     * çalışan tek bir grafik, istatistik ekranını komple boş bırakırdı.
     *
     * Kurulum boyut GELİNCE yapılır; gözcü zaten dinliyor.
     */
    const kurulabilir = () => dugum.clientWidth > 0 && dugum.clientHeight > 0;

    const kur = () => {
      if (ornek.current || !kurulabilir()) return;
      // Tema değişince örnek yok edilip yeniden kurulur: ECharts renkleri
      // kurulum anında okuyor, `setOption` ile güncellemek eski paleti
      // bırakıyordu (koyu temada beyaz zeminli ipucu gibi).
      ornek.current = echarts.init(dugum, undefined, { renderer: 'canvas' });
      ornek.current.setOption({ ...temelSecenekler, ...secenekler }, { notMerge: true });
    };

    kur();

    // `ResizeObserver` jsdom'da yok; testler grafiksiz de anlamlı kalmalı.
    const gozcu =
      typeof ResizeObserver === 'undefined'
        ? null
        : new ResizeObserver(() => {
            kur();
            ornek.current?.resize();
          });
    gozcu?.observe(dugum);

    return () => {
      gozcu?.disconnect();
      ornek.current?.dispose();
      ornek.current = null;
    };
    // `secenekler`/`temelSecenekler` yalnızca İLK kurulumda okunur; sonraki
    // güncellemeler aşağıdaki etkide. Bağımlılığa eklemek her seçenek
    // değişiminde grafiği yok edip yeniden kurardı.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tema]);

  useEffect(() => {
    // Örnek yoksa (kap henüz boyutsuz) sessizce geç: kurulum anında
    // seçenekler zaten uygulanıyor.
    if (!ornek.current) return;
    // `notMerge` şart: seçenek sayısı azalan bir güncellemede (ör. süzgeç
    // daraldı) eski seriler ekranda kalıyordu.
    ornek.current.setOption(
      { ...temelSecenekler, ...secenekler },
      { notMerge: true },
    );
  }, [secenekler, temelSecenekler]);

  return (
    <div
      ref={kap}
      role="img"
      aria-label={etiket}
      style={{ height: yukseklik }}
      className={className}
    />
  );
}
