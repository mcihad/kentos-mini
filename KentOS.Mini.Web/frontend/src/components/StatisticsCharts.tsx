import { useMemo } from 'react';
import type { components } from '../data/types.generated';
import { EChart, palette } from './EChart';
import { EmptyState } from './EmptyState';
import { BarChart3 } from 'lucide-react';

type Dilim = components['schemas']['IstatistikDilimDto'];
type Nokta = components['schemas']['IstatistikSeriNoktasiDto'];

/**
 * İstatistik ekranının grafik dili — hepsi Apache ECharts.
 *
 * Panolar (etkinlik ve talep) AYNI bileşenleri kullanır: iki pano iki ayrı
 * grafik kütüphanesi kullansaydı aynı ekranda iki görsel dil olurdu ve
 * "%23,5" bir yerde çubuk, ötekinde halka olarak okunurdu.
 *
 * Renk önceliği her yerde aynı: kaydın KENDİ rengi (tip/durum tanımından
 * gelen) varsa o, yoksa kurumsal palet sırası.
 */

/**
 * Dilim renkleri.
 *
 * İKİ FARKLI DURUM, iki farklı kural:
 *
 *  • Kayıtların KENDİ rengi varsa (etkinlik tipi, talep durumu — tanım
 *    ekranından seçilmiş renkler) o kullanılır. Kullanıcı "kırmızı olan
 *    reddedilen" diye okuyor; paletle boyamak o bilgiyi siler.
 *  • Rengi olmayan SIRALI bir dağılımda (mahalle, meslek, kişi) hepsi TEK
 *    kurumsal renk olur. Sıralı bir listede her çubuğu ayrı renge boyamak
 *    var olmayan bir kategori farkı ima ediyor ve ekran alacalı duruyordu.
 */
function renkler(dilimler: Dilim[]): string[] {
  const p = palette();
  const kendiRengiVar = dilimler.some((d) => !!d.renk);
  if (!kendiRengiVar) return dilimler.map(() => p[0]);
  return dilimler.map((d, i) => d.renk || p[i % p.length]);
}

/** Boş veri için ortak karşılık — grafik yerine boş bir kutu çizmek yerine. */
function Bos({ mesaj }: { mesaj: string }) {
  return <EmptyState ikon={BarChart3} baslik={mesaj} />;
}

// ═══════════════════════════════════════════════════════ yatay çubuk

/**
 * Sıralı kategorik dağılım — mahalle, meslek, tip, durum…
 *
 * YATAY çubuk: kategori adları uzun (mahalle ve meslek adları) ve dikey
 * sütunda 45° eğik yazmak zorunda kalınıyordu; eğik etiket dar ekranda hem
 * okunmuyor hem de grafiğin yarısını yiyordu.
 */
export function DistributionBar({
  dilimler,
  yukseklik,
  etiket,
  enFazla = 12,
}: {
  dilimler: Dilim[];
  yukseklik?: number;
  etiket: string;
  enFazla?: number;
}) {
  const veri = useMemo(() => dilimler.slice(0, enFazla), [dilimler, enFazla]);

  const secenekler = useMemo(() => {
    // ECharts kategori eksenini AŞAĞIDAN yukarı çizdiği için dizi ters
    // çevriliyor. Renkler ise ÖNCE hesaplanır: ters diziden hesaplansaydı
    // renk sırası da tersine döner, en büyük çubuk paletin sonundaki rengi
    // alırdı (ekrandaki alacalı görüntünün sebebi tam olarak buydu).
    const renkSirasi = renkler(veri);
    const ters = [...veri].map((d, i) => ({ d, renk: renkSirasi[i] })).reverse();
    return {
      grid: { left: 8, right: 44, top: 8, bottom: 8, containLabel: true },
      xAxis: { type: 'value' as const, axisLabel: { show: false }, splitLine: { show: false } },
      yAxis: {
        type: 'category' as const,
        data: ters.map((x) => x.d.etiket),
        axisLabel: { width: 130, overflow: 'truncate' as const },
      },
      tooltip: {
        trigger: 'item' as const,
        formatter: (p: { name: string; value: number; dataIndex: number }) =>
          `${p.name}<br/><b>${p.value}</b> · %${ters[p.dataIndex]?.d.yuzde ?? 0}`,
      },
      series: [
        {
          type: 'bar' as const,
          data: ters.map((x) => ({
            value: x.d.deger,
            itemStyle: {
              color: x.renk,
              borderRadius: [0, 4, 4, 0] as [number, number, number, number],
            },
          })),
          barMaxWidth: 18,
          label: {
            show: true,
            position: 'right' as const,
            fontSize: 11,
            formatter: (p: { value: number }) => String(p.value),
          },
        },
      ],
    };
  }, [veri]);

  if (veri.length === 0) return <Bos mesaj="Veri yok" />;

  // Yükseklik satır sayısına göre: sabit yükseklikte 15 mahalle üst üste
  // binip okunmaz oluyordu.
  const h = yukseklik ?? Math.max(140, veri.length * 26 + 24);
  return <EChart secenekler={secenekler} yukseklik={h} etiket={etiket} />;
}

// ═══════════════════════════════════════════════════════ halka (pasta)

/** İki-üç dilimli oranlar — ajandaya eklendi/eklenmedi gibi. */
export function DonutDistribution({
  dilimler,
  etiket,
  yukseklik = 220,
}: {
  dilimler: Dilim[];
  etiket: string;
  yukseklik?: number;
}) {
  const secenekler = useMemo(
    () => ({
      grid: undefined,
      xAxis: undefined,
      yAxis: undefined,
      tooltip: {
        trigger: 'item' as const,
        formatter: (p: { name: string; value: number; percent: number }) =>
          `${p.name}<br/><b>${p.value}</b> · %${p.percent}`,
      },
      legend: {
        bottom: 0,
        icon: 'circle',
        itemWidth: 8,
        itemHeight: 8,
        textStyle: { fontSize: 11 },
      },
      series: [
        {
          type: 'pie' as const,
          // Halka: dolu pasta, ortadaki boşlukta toplamı gösteremiyor ve
          // küçük dilimleri okunmaz kılıyordu.
          radius: ['52%', '76%'],
          center: ['50%', '44%'],
          avoidLabelOverlap: true,
          label: { show: false },
          data: dilimler.map((d, i) => ({
            name: d.etiket,
            value: d.deger,
            itemStyle: { color: renkler(dilimler)[i] },
          })),
        },
      ],
    }),
    [dilimler],
  );

  if (dilimler.length === 0 || dilimler.every((d) => (d.deger ?? 0) === 0)) {
    return <Bos mesaj="Veri yok" />;
  }
  return <EChart secenekler={secenekler} yukseklik={yukseklik} etiket={etiket} />;
}

// ═══════════════════════════════════════════════════════ zaman serisi

/** Aylık/günlük seyir. `tur` çizgi ya da sütun. */
export function TimeSeries({
  noktalar,
  etiket,
  tur = 'sutun',
  yukseklik = 200,
}: {
  noktalar: Nokta[];
  etiket: string;
  tur?: 'cizgi' | 'sutun';
  yukseklik?: number;
}) {
  const secenekler = useMemo(() => {
    const p = palette();
    return {
      tooltip: { trigger: 'axis' as const },
      xAxis: {
        type: 'category' as const,
        data: noktalar.map((n) => n.etiket),
        // Nokta sayısı arttıkça etiketleri ECharts seyreltsin; elle
        // seyreltmek 60 günlük seride bazı ayları tamamen gizliyordu.
        axisLabel: { interval: 'auto' as const, hideOverlap: true },
      },
      yAxis: { type: 'value' as const, minInterval: 1 },
      series: [
        tur === 'cizgi'
          ? {
              type: 'line' as const,
              data: noktalar.map((n) => n.deger),
              smooth: true,
              symbolSize: 5,
              lineStyle: { width: 2 },
              areaStyle: { opacity: 0.12 },
              itemStyle: { color: p[0] },
            }
          : {
              type: 'bar' as const,
              data: noktalar.map((n) => n.deger),
              barMaxWidth: 22,
              itemStyle: {
                color: p[0],
                borderRadius: [4, 4, 0, 0] as [number, number, number, number],
              },
            },
      ],
    };
  }, [noktalar, tur]);

  if (noktalar.length === 0) return <Bos mesaj="Veri yok" />;
  return <EChart secenekler={secenekler} yukseklik={yukseklik} etiket={etiket} />;
}
