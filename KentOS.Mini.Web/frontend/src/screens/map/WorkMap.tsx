import * as maplibregl from 'maplibre-gl';
import { useEffect, useRef } from 'react';
import 'maplibre-gl/dist/maplibre-gl.css';
import { HARITA_TEMASI, TURKIYE_MERKEZ, TURKIYE_YAKINLIK, webgl2Var } from './harita';
import type { WorkMapPoint } from '../../data/types';

/**
 * İŞ HARİTASI — görevler ve bekleyen bildirimler.
 *
 * <p>
 * <b>Kümeleme MapLibre'nin kendi kaynak seçeneğiyle</b>, ayrı bir kütüphane
 * ile değil: bin nokta tek tek çizildiğinde harita kayarken donuyor ve
 * üst üste binen işaretçiler sayıyı olduğundan az gösteriyor.
 * </p>
 *
 * <p>
 * <b>Renk sunucudan geliyor</b> (durum rengi). İstemcide yeniden eşlesek iki
 * yerde iki farklı renk skalası doğardı; liste ve harita aynı işi farklı
 * renkte gösterirdi.
 * </p>
 */
export function WorkMap({
  noktalar,
  yukseklik = 460,
  tiklandi,
}: {
  noktalar: WorkMapPoint[];
  yukseklik?: number;
  tiklandi?: (n: WorkMapPoint) => void;
}) {
  const kap = useRef<HTMLDivElement>(null);
  const harita = useRef<maplibregl.Map | null>(null);
  const hazir = useRef(false);

  const geriCagri = useRef(tiklandi);
  geriCagri.current = tiklandi;

  // MapLibre WebGL2 zorunlu tutuyor ve desteklenmediğinde kurulum anında
  // patlayarak EKRANIN TAMAMINI düşürüyor. Yedek: aynı noktalar liste
  // olarak. Ayrıntının tamamı `harita.ts` içinde yazılı.
  const haritaVar = webgl2Var();

  useEffect(() => {
    if (!haritaVar || !kap.current || harita.current) return;

    const h = new maplibregl.Map({
      container: kap.current,
      style: HARITA_TEMASI,
      center: TURKIYE_MERKEZ,
      zoom: TURKIYE_YAKINLIK,
      attributionControl: { compact: true },
    });

    h.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right');
    h.addControl(
      new maplibregl.GeolocateControl({ trackUserLocation: false }),
      'top-right',
    );

    h.on('load', () => {
      h.addSource('isler', {
        type: 'geojson',
        data: { type: 'FeatureCollection', features: [] },
        cluster: true,
        clusterRadius: 46,
        // 15'ten sonra kümeleme KAPANIYOR: sokak seviyesinde kullanıcı tek
        // tek işleri görmek istiyor, "3" yazan bir daire değil.
        clusterMaxZoom: 15,
      });

      h.addLayer({
        id: 'kumeler',
        type: 'circle',
        source: 'isler',
        filter: ['has', 'point_count'],
        paint: {
          'circle-color': '#002E6D',
          'circle-opacity': 0.85,
          // Daire yarıçapı sayıyla büyüyor; sabit yarıçap 3 ile 300'ü aynı
          // gösterirdi.
          'circle-radius': ['step', ['get', 'point_count'], 15, 10, 20, 50, 26],
          'circle-stroke-width': 2,
          'circle-stroke-color': '#FFFFFF',
        },
      });

      h.addLayer({
        id: 'kume-sayisi',
        type: 'symbol',
        source: 'isler',
        filter: ['has', 'point_count'],
        layout: {
          'text-field': ['get', 'point_count_abbreviated'],
          'text-size': 12,
        },
        paint: { 'text-color': '#FFFFFF' },
      });

      h.addLayer({
        id: 'noktalar',
        type: 'circle',
        source: 'isler',
        filter: ['!', ['has', 'point_count']],
        paint: {
          'circle-color': ['get', 'renk'],
          'circle-radius': 8,
          'circle-stroke-width': 2,
          // GECİKEN İŞİN halkası KIRMIZI: durum rengi zaten farklı ama
          // "gecikti" bilgisi durumdan bağımsız ve haritada en çok aranan şey.
          'circle-stroke-color': ['case', ['get', 'gecikti'], '#B3261E', '#FFFFFF'],
        },
      });

      // Kümeye dokunmak İÇİNE yakınlaştırıyor.
      h.on('click', 'kumeler', (o: maplibregl.MapLayerMouseEvent) => {
        const ozellik = h.queryRenderedFeatures(o.point, { layers: ['kumeler'] })[0];
        const kumeId = ozellik?.properties?.cluster_id;
        if (kumeId == null) return;

        const kaynak = h.getSource('isler') as maplibregl.GeoJSONSource;
        void kaynak.getClusterExpansionZoom(kumeId).then((z: number) => {
          h.easeTo({ center: (ozellik.geometry as GeoJSON.Point).coordinates as [number, number], zoom: z });
        });
      });

      h.on('click', 'noktalar', (o: maplibregl.MapLayerMouseEvent) => {
        const p = o.features?.[0]?.properties;
        if (!p) return;

        new maplibregl.Popup({ offset: 12, closeButton: false })
          .setLngLat((o.features![0].geometry as GeoJSON.Point).coordinates as [number, number])
          .setHTML(balonIcerigi(p as Record<string, unknown>))
          .addTo(h);

        geriCagri.current?.(p as unknown as WorkMapPoint);
      });

      for (const katman of ['kumeler', 'noktalar']) {
        h.on('mouseenter', katman, () => (h.getCanvas().style.cursor = 'pointer'));
        h.on('mouseleave', katman, () => (h.getCanvas().style.cursor = ''));
      }

      hazir.current = true;
      veriyiYaz(h, noktalar);
    });

    harita.current = h;

    return () => {
      h.remove();
      harita.current = null;
      hazir.current = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    const h = harita.current;
    if (!h || !hazir.current) return;
    veriyiYaz(h, noktalar);
  }, [noktalar]);

  if (!haritaVar) return <NoktaListesi noktalar={noktalar} />;

  return (
    <div
      ref={kap}
      style={{ height: yukseklik }}
      className="w-full overflow-hidden rounded-control border border-line"
      role="application"
      aria-label={`İş haritası — ${noktalar.length} nokta`}
    />
  );
}

/**
 * Harita çizilemediğinde noktaların liste hâli.
 *
 * <p>
 * Coğrafi ilişkiyi göstermiyor ama <b>hiçbir kaydı gizlemiyor</b>: kullanıcı
 * her işe yine ulaşabiliyor. Boş bir kutu göstermek, veriyi kaybetmiş gibi
 * okunurdu.
 * </p>
 */
function NoktaListesi({ noktalar }: { noktalar: WorkMapPoint[] }) {
  return (
    <div className="rounded-control border border-line">
      <p className="border-b border-line px-3 py-2 text-2xs text-text-3">
        Bu tarayıcı harita çizimini desteklemiyor; kayıtlar liste olarak
        gösteriliyor.
      </p>

      <ul className="max-h-[460px] divide-y divide-line overflow-y-auto">
        {noktalar.map((n) => (
          <li key={`${n.tur}-${n.id}`}>
            <a
              href={n.tur === 'bildirim' ? '/vatandas-bildirimleri' : `/gorevler/${n.id}`}
              className="flex items-center gap-2.5 px-3 py-2.5 hover:bg-sunken"
            >
              <span
                className="h-2.5 w-2.5 flex-none rounded-full"
                style={{
                  background: n.renk || '#7C8592',
                  outline: n.gecikti ? '2px solid var(--st-no)' : undefined,
                  outlineOffset: 1,
                }}
                aria-hidden
              />
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm text-ink">{n.baslik}</span>
                <span className="text-2xs text-ink-3">
                  {n.takipNo} · {n.durumAd}
                  {n.adres ? ` · ${n.adres}` : ''}
                </span>
              </span>
            </a>
          </li>
        ))}
      </ul>
    </div>
  );
}

/**
 * Noktaları kaynağa yazar ve görünümü ilk seferde noktalara sığdırır.
 */
function veriyiYaz(h: maplibregl.Map, noktalar: WorkMapPoint[]) {
  const kaynak = h.getSource('isler') as maplibregl.GeoJSONSource | undefined;
  if (!kaynak) return;

  kaynak.setData({
    type: 'FeatureCollection',
    features: noktalar.map((n) => ({
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [n.boylam!, n.enlem!] },
      properties: {
        id: n.id,
        tur: n.tur,
        takipNo: n.takipNo,
        baslik: n.baslik,
        renk: n.renk || '#7C8592',
        durumAd: n.durumAd,
        gecikti: !!n.gecikti,
        adres: n.adres ?? '',
      },
    })),
  });

  if (noktalar.length === 0) return;

  // GÖRÜNÜMÜ NOKTALARA SIĞDIR. Türkiye geneli açılış, üç noktası olan bir
  // birimde boş bir harita gibi görünüyordu.
  const sinir = new maplibregl.LngLatBounds();
  for (const n of noktalar) sinir.extend([n.boylam!, n.enlem!]);

  h.fitBounds(sinir, { padding: 48, maxZoom: 16, duration: 0 });
}

/** Balon içeriği HTML; başlıktaki `<` metni kırmasın diye kaçırılıyor. */
function balonIcerigi(p: Record<string, unknown>): string {
  const kacir = (m: string) =>
    m.replace(/[&<>"]/g, (k) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[k] ?? k);

  const yol = p.tur === 'bildirim' ? '/vatandas-bildirimleri' : `/gorevler/${p.id}`;

  return `
    <div style="font-family:IBM Plex Sans,sans-serif;min-width:180px">
      <div style="font-size:10px;color:#8A8A8C;font-variant-numeric:tabular-nums">
        ${kacir(String(p.takipNo ?? ''))}
      </div>
      <div style="font-size:13px;font-weight:600;margin:2px 0 4px">
        ${kacir(String(p.baslik ?? ''))}
      </div>
      <div style="font-size:11px;color:#4D4D4F">${kacir(String(p.durumAd ?? ''))}</div>
      ${p.adres ? `<div style="font-size:11px;color:#8A8A8C">${kacir(String(p.adres))}</div>` : ''}
      <a href="${yol}" style="display:inline-block;margin-top:6px;font-size:11px;color:#1E5FBF">
        Ayrıntı →
      </a>
    </div>`;
}
