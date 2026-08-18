import * as maplibregl from 'maplibre-gl';
import { useEffect, useRef } from 'react';
import 'maplibre-gl/dist/maplibre-gl.css';
import {
  HARITA_TEMASI, TURKIYE_MERKEZ, TURKIYE_YAKINLIK, iğneGoruntusu, kumeGoruntusu, webgl2Var,
} from './harita';
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
  cerceve = true,
  tiklandi,
}: {
  noktalar: WorkMapPoint[];
  /** Piksel sayısı ya da CSS yüksekliği (`'100%'`, `'calc(100dvh - 260px)'`). */
  yukseklik?: number | string;
  /** Kenarlık ve köşe yuvarlaması — tam ekran kullanımda kapatılır. */
  cerceve?: boolean;
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

    /*
      HARİTA HATALARI ARTIK SESSİZ DEĞİL.

      Bu ekran uzun süre boş bir harita gösterdi ve hiçbir yerde tek bir
      hata yoktu; sebebi ancak tarayıcıda ölçerek bulundu. MapLibre kendi
      hatalarını `error` olayıyla yayınlıyor — dinleyicisi yoksa hiçbir yere
      gitmiyorlar. Stil, karo ya da katman hatası bir daha sessizce
      kaybolmasın.
    */
    h.on('error', (o) => console.error('[harita]', o.error?.message ?? o));

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

      /*
        KÜME ROZETİ TEK KATMAN.

        Önceden daire (circle) + sayı (symbol/text) olarak İKİ katmandı ve
        sayı hiçbir zaman çizilmiyordu: MapLibre'de metin katmanı `glyphs`
        (yazı tipi sunucusu) istiyor, bu harita ise anahtarsız ve
        sağlayıcısız kurulmuş — stilde `glyphs` yok. Rozet artık sayısıyla
        birlikte tuvale çiziliyor; dışarıya yeni bağımlılık eklenmiyor.
      */
      h.addLayer({
        id: 'kumeler',
        type: 'symbol',
        source: 'isler',
        filter: ['has', 'point_count'],
        layout: {
          'icon-image': ['concat', 'kume-', ['to-string', ['get', 'point_count']]],
          'icon-allow-overlap': true,
          'icon-ignore-placement': true,
        },
      });

      /*
        İŞ İĞNESİ — damla biçimli, ucu konumun tam üstünde.

        Daire yerine iğne: bir daire "buralarda bir yerde" der, iğnenin
        SİVRİ UCU tam noktayı gösterir. Sahadaki personel adresi haritadan
        okuyor; on metrelik belirsizlik yanlış sokak demek.

        `icon-anchor: 'bottom'` bu yüzden şart — varsayılan `center` olsaydı
        iğne gerçek konumun yarım boy yukarısında dururdu.

        `icon-allow-overlap`: MapLibre varsayılan olarak çakışan simgeleri
        GİZLİYOR. Yan yana iki iş varken birinin haritadan silinmesi, "işim
        görünmüyor" demenin bir başka yolu olurdu.
      */
      h.addLayer({
        id: 'noktalar',
        type: 'symbol',
        source: 'isler',
        filter: ['!', ['has', 'point_count']],
        layout: {
          'icon-image': [
            'concat',
            'igne-',
            ['get', 'renk'],
            ['case', ['==', ['get', 'gecikti'], true], '-gecikti', ''],
          ],
          'icon-anchor': 'bottom',
          'icon-allow-overlap': true,
          'icon-ignore-placement': true,
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

      /*
        EKSİK SİMGE ÇÖZÜCÜSÜ — küme rozetleri tam gerektiği anda üretiliyor.

        `styleimagemissing` OLAYI DEĞİL: MapLibre v6'da o olay yalnızca
        çözücü de başarısız olduktan SONRA yayınlanıyor ve olay içinde
        eklenen görüntü o çizim turunda kullanılmıyor — rozetler hiç
        görünmüyordu. Çözücü ise MapLibre tarafından BEKLENİYOR, dolayısıyla
        görüntü aynı turda hazır oluyor.

        Kümeler yakınlaştırmaya göre yeniden hesaplanıyor; hangi sayıların
        ekranda belireceği önceden bilinemez. Kullanılmayacak yüzlerce rozeti
        peşin çizmek yerine istendiğinde üretiliyor.
      */
      h.setMissingStyleImageResolver(async (kimlik: string) => {
        eksikSimgeyiUret(h, kimlik);
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
      className={
        cerceve
          ? 'w-full overflow-hidden rounded-control border border-line'
          : 'h-full w-full overflow-hidden'
      }
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

      <ul className="max-h-[60dvh] divide-y divide-line overflow-y-auto">
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
 * Bir iğne görüntüsünü haritaya kaydeder (yoksa).
 *
 * Görüntü kimliği doğrudan renkten türüyor, çünkü katmanın `icon-image`
 * ifadesi de aynı adı üretiyor: `igne-#1E5FBF-gecikti`. Böylece durum renkleri
 * sunucuda değişse bile istemcide eşlenecek bir tablo tutmak gerekmiyor.
 */
function igneyiKaydet(h: maplibregl.Map, renk: string, gecikti: boolean) {
  const kimlik = `igne-${renk}${gecikti ? '-gecikti' : ''}`;
  if (h.hasImage(kimlik)) return;

  const gorsel = iğneGoruntusu(renk, gecikti);
  if (gorsel) {
    h.addImage(
      kimlik,
      { width: gorsel.width, height: gorsel.height, data: gorsel.data },
      { pixelRatio: gorsel.pixelRatio },
    );
  }
}

/**
 * Küme rozetlerini hazırlar.
 *
 * <p>
 * Kümeler <b>yakınlaştırmaya göre yeniden hesaplanıyor</b>: hangi sayıların
 * ekranda belireceği önceden bilinemez. Bu yüzden görüntüler `styleimagemissing`
 * olayında, yani MapLibre "böyle bir simge bulamadım" dediği anda üretiliyor —
 * kullanılmayacak yüzlerce rozeti peşin çizmek yerine.
 * </p>
 */
function eksikSimgeyiUret(h: maplibregl.Map, kimlik: string) {
  if (h.hasImage(kimlik)) return;

  if (kimlik.startsWith('kume-')) {
    const sayi = Number(kimlik.slice(5));
    if (!Number.isFinite(sayi)) return;

    const gorsel = kumeGoruntusu(sayi);
    if (gorsel) {
      h.addImage(
        kimlik,
        { width: gorsel.width, height: gorsel.height, data: gorsel.data },
        { pixelRatio: gorsel.pixelRatio },
      );
    }
    return;
  }

  if (kimlik.startsWith('igne-')) {
    const gecikti = kimlik.endsWith('-gecikti');
    const renk = kimlik.slice(5, gecikti ? -8 : undefined);
    igneyiKaydet(h, renk, gecikti);
  }
}

/**
 * Noktaları kaynağa yazar ve görünümü ilk seferde noktalara sığdırır.
 */
function veriyiYaz(h: maplibregl.Map, noktalar: WorkMapPoint[]) {
  const kaynak = h.getSource('isler') as maplibregl.GeoJSONSource | undefined;
  if (!kaynak) return;

  // İğneler veriden ÖNCE kaydediliyor: katman çizime başladığında simge
  // hazır olmazsa ilk kare boş geçiyor ve harita bir an işaretçisiz duruyor.
  for (const n of noktalar) igneyiKaydet(h, n.renk || '#7C8592', !!n.gecikti);

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
