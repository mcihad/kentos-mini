import { CalendarRange } from 'lucide-react';
import { useMemo } from 'react';
import { EChart } from '../../components/EChart';
import { EmptyState } from '../../components/EmptyState';
import { Skeleton } from '../../components/Skeleton';
import { useGantt } from '../../data/projects';
import type { GanttRow } from '../../data/types';

/** Satır yüksekliği (px) — çubuk kalınlığı ve grafik boyu bundan türüyor. */
const SATIR = 26;

/** Zaman ekseninin ay kısaltmaları. */
const AYLAR = [
  'Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz',
  'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara',
];

/**
 * GANTT — projenin zaman çizelgesi.
 *
 * <p>
 * ECharts'ta hazır bir gantt tipi yok; çubuklar <code>custom</code> serisiyle
 * çiziliyor. Çubuk grafiğini yatay çevirip kullanmak denenebilirdi ama bar
 * serisi her zaman eksenin sıfırından başlıyor — bir işin <b>başlangıcı</b>
 * ile <b>bitişi</b> arasındaki aralığı ancak dolgu rengiyle taklit edilebilen
 * ikinci bir seri gerektirirdi ve ipucu iki değeri ayrı ayrı gösterirdi.
 * </p>
 *
 * <p>
 * <b>Veri sunucudan hazır geliyor:</b> tarih türetme (planlanan → gerçekleşen
 * → SLA), ilerleme oranı ve gecikme orada hesaplanıyor. İstemcinin saatine
 * bakılsaydı gecikme çizgisi yanlış yere düşerdi.
 * </p>
 *
 * <p>
 * <b>Kilometre taşı bir NOKTA</b> (başlangıç = bitiş) ve elmas olarak
 * çiziliyor; sıfır genişlikte bir çubuk görünmez olurdu.
 * </p>
 */
export function Gantt({ projeId, etkin }: { projeId: number; etkin: boolean }) {
  const { data: satirlar, isLoading } = useGantt(projeId, etkin);

  const secenekler = useMemo(() => ganttSecenekleri(satirlar ?? []), [satirlar]);

  if (isLoading) return <Skeleton className="h-72 w-full" />;

  if (!satirlar || satirlar.length === 0) {
    return (
      <EmptyState
        ikon={CalendarRange}
        baslik="Çizelgeye yazılacak bir şey yok"
        aciklama="Gantt yalnızca tarihi olan kilometre taşlarını ve görevleri gösterir."
      />
    );
  }

  return (
    <EChart
      secenekler={secenekler}
      // Yükseklik satır sayısına göre: sabit bir yükseklikte 30 satırlık bir
      // proje okunamaz hâle geliyor, 3 satırlık proje ise boşlukta yüzüyordu.
      yukseklik={Math.max(200, satirlar.length * SATIR + 90)}
      etiket={`Proje zaman çizelgesi — ${satirlar.length} satır`}
    />
  );
}

/** ECharts seçeneklerini kurar. Test edilebilsin diye dışa açık. */
export function ganttSecenekleri(satirlar: GanttRow[]) {
  // Satırlar sunucudan başlangıca göre sıralı geliyor. Kategori ekseni
  // AŞAĞIDAN yukarı çizdiği için ters çevriliyor — ilk iş en üstte olmalı.
  const tersi = [...satirlar].reverse();
  const adlar = tersi.map((s) => s.ad ?? '');

  const veri = tersi.map((s, i) => ({
    value: [i, yeniTarih(s.baslangic), yeniTarih(s.bitis), s.ilerleme ?? 0],
    itemStyle: { color: s.renk || '#7C8592' },
    ham: s,
  }));

  return {
    grid: { left: 8, right: 20, top: 10, bottom: 40, containLabel: true },
    tooltip: {
      formatter: (p: { data?: { ham?: GanttRow } }) => {
        const h = p.data?.ham;
        if (!h) return '';
        const tur = h.tur === 'kilometreTasi' ? 'Kilometre taşı' : 'Görev';
        const tarih =
          h.tur === 'kilometreTasi'
            ? gunMetni(h.baslangic)
            : `${gunMetni(h.baslangic)} → ${gunMetni(h.bitis)}`;
        return [
          `<b>${kacir(h.ad ?? '')}</b>`,
          `${tur} · ${kacir(h.durumAd ?? '')}`,
          tarih,
          `İlerleme %${h.ilerleme ?? 0}`,
          h.gecikti ? '<b>Süre aşıldı</b>' : '',
        ]
          .filter(Boolean)
          .join('<br/>');
      },
    },
    xAxis: {
      type: 'time' as const,
      splitLine: { show: true, lineStyle: { type: 'dashed' as const } },
      /*
        AY ADLARI TÜRKÇE.

        ECharts'ın zaman ekseni varsayılan olarak İngilizce yazıyor ("Apr",
        "May") ve ekran görüntüsünde de öyle çıktı. Kütüphanenin yerel ayarını
        kaydetmek yerine etiket biçimlendiriliyor: `registerLocale` bütün
        grafikleri etkileyen küresel bir kayıt ve yalnızca bu eksen için
        gereken şeyi uygulama genelinde değiştirmek gereksiz risk.
      */
      axisLabel: {
        formatter: {
          year: '{yyyy}',
          month: (d: number) => AYLAR[new Date(d).getMonth()],
          day: (d: number) => `${new Date(d).getDate()} ${AYLAR[new Date(d).getMonth()]}`,
          hour: '{HH}:{mm}',
          minute: '{HH}:{mm}',
        },
      },
    },
    yAxis: {
      type: 'category' as const,
      data: adlar,
      axisLabel: {
        fontSize: 11,
        // Uzun görev adları ekseni sonsuza kadar itiyordu; kırpma sınırı
        // eksenin grafiği yemesini engelliyor. Tamamı ipucunda duruyor.
        width: 160,
        overflow: 'truncate' as const,
      },
    },
    // UZUN PROJELERDE tarih aralığı daraltılabilsin: bir yıllık çizelgede
    // haftalık ayrıntı okunamıyor.
    dataZoom: [
      { type: 'inside' as const, filterMode: 'weakFilter' as const },
      { type: 'slider' as const, height: 18, bottom: 8, filterMode: 'weakFilter' as const },
    ],
    series: [
      {
        type: 'custom' as const,
        renderItem: cubukCiz,
        encode: { x: [1, 2], y: 0 },
        data: veri,
        // BUGÜN ÇİZGİSİ: gecikmeyi çizgiye bakarak okumak, tarihleri tek tek
        // karşılaştırmaktan hızlı.
        markLine: {
          silent: true,
          symbol: 'none',
          label: { formatter: 'Bugün', position: 'insideEndTop' as const, fontSize: 10 },
          lineStyle: { color: '#B3261E', width: 1, type: 'dashed' as const },
          data: [{ xAxis: Date.now() }],
        },
      },
    ],
  };
}

/**
 * Tek bir satırı çizer.
 *
 * <p>
 * Görev: taban çubuk + üstünde ilerleme dolgusu. Kilometre taşı: elmas.
 * </p>
 */
function cubukCiz(
  _parametre: unknown,
  api: {
    value: (i: number) => number;
    coord: (v: [number, number]) => [number, number];
    size: (v: [number, number]) => [number, number];
    style: (o?: Record<string, unknown>) => Record<string, unknown>;
  },
) {
  const satir = api.value(0);
  const bas = api.coord([api.value(1), satir]);
  const bit = api.coord([api.value(2), satir]);
  const ilerleme = api.value(3);

  const yukseklik = (api.size([0, 1])[1] as number) * 0.55;
  const genislik = Math.max(bit[0] - bas[0], 0);

  // NOKTA (kilometre taşı): sıfır genişlikte bir çubuk görünmezdi.
  if (genislik < 3) {
    const r = Math.min(yukseklik / 1.6, 7);
    return {
      type: 'polygon',
      shape: {
        points: [
          [bas[0], bas[1] - r],
          [bas[0] + r, bas[1]],
          [bas[0], bas[1] + r],
          [bas[0] - r, bas[1]],
        ],
      },
      style: api.style(),
    };
  }

  return {
    type: 'group',
    children: [
      {
        type: 'rect',
        shape: {
          x: bas[0],
          y: bas[1] - yukseklik / 2,
          width: genislik,
          height: yukseklik,
          r: 3,
        },
        // Taban çubuk SOLGUN: dolu kısmın nerede bittiği ancak zıtlıkla
        // okunuyor. Aynı tonda iki dikdörtgen tek bir çubuk gibi görünürdü.
        style: api.style({ opacity: 0.28 }),
      },
      {
        type: 'rect',
        shape: {
          x: bas[0],
          y: bas[1] - yukseklik / 2,
          width: (genislik * Math.min(Math.max(ilerleme, 0), 100)) / 100,
          height: yukseklik,
          r: 3,
        },
        style: api.style(),
      },
    ],
  };
}

function yeniTarih(t?: string | null): number {
  return t ? new Date(t).getTime() : Date.now();
}

function gunMetni(t?: string | null): string {
  if (!t) return '—';
  const d = new Date(t);
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString('tr-TR');
}

/** İpucu HTML olarak basılıyor; başlıktaki `<` metni kırmazsın diye. */
function kacir(m: string): string {
  return m.replace(/[&<>"]/g, (k) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[k] ?? k,
  );
}
