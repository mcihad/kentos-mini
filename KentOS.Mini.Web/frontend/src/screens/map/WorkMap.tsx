import * as maplibregl from 'maplibre-gl';
import { useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Link } from 'react-router-dom';
import { AlertTriangle, ArrowRight, MapPin } from 'lucide-react';
import { useKorumaliAdres } from '../../data/korumaliMedya';
import 'maplibre-gl/dist/maplibre-gl.css';
import {
  HARITA_TEMASI, PIN_YUKSEKLIK, TURKIYE_MERKEZ, TURKIYE_YAKINLIK, iğneGoruntusu,
  kumeGoruntusu, webgl2Var,
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

  const [secili, setSecili] = useState<{ nokta: WorkMapPoint; konum: [number, number] } | null>(
    null,
  );

  /*
    BALONUN KABI BİR KEZ ÜRETİLİYOR.

    Her seçimde yeni bir düğüm üretmek, React'in portalı her seferinde
    yeniden bağlaması ve resmin baştan indirilmesi demekti.
  */
  const balonKabi = useRef<HTMLDivElement | null>(null);
  if (balonKabi.current === null && typeof document !== 'undefined') {
    balonKabi.current = document.createElement('div');
  }
  const balon = useRef<maplibregl.Popup | null>(null);

  useEffect(() => {
    const h = harita.current;
    const kap = balonKabi.current;
    if (!h || !kap) return;

    if (!secili) {
      balon.current?.remove();
      return;
    }

    if (!balon.current) {
      balon.current = new maplibregl.Popup({
        offset: [0, -PIN_YUKSEKLIK + 6],
        closeButton: false,
        closeOnClick: true,
        maxWidth: '280px',
        className: 'harita-balonu',
      });

      // Kullanıcı boşluğa dokunup kapattığında React durumu da temizlenmeli;
      // yoksa aynı noktaya ikinci dokunuş hiçbir şey açmıyordu.
      balon.current.on('close', () => setSecili(null));
      balon.current.setDOMContent(kap);
    }

    balon.current.setLngLat(secili.konum).addTo(h);
  }, [secili]);

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

        /*
          BALON ARTIK REACT — HTML DİZESİ DEĞİL.

          Eski hâli `setHTML` ile satır içi stil basıyordu: `#8A8A8C`,
          `#4D4D4F`, `#1E5FBF`. Yani balon kurumsal kimlik temasını da gece
          modunu da hiç bilmiyordu — koyu temada açık gri zeminde açık gri
          yazı çıkıyordu. Ayrıntı bağlantısı da düz bir `<a href>`ti ve
          uygulamayı BAŞTAN yüklüyordu: harita, yakınlaştırma ve süzgeçler
          gidiyordu.

          Konumlandırmayı (çapa, ok, kenara sığdırma) yine MapLibre yapıyor;
          içerik bir portalla React'e bırakılıyor.
        */
        setSecili({
          nokta: p as unknown as WorkMapPoint,
          konum: (o.features![0].geometry as GeoJSON.Point).coordinates as [number, number],
        });

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
    <>
      {balonKabi.current && secili
        && createPortal(<HaritaBalonu nokta={secili.nokta} />, balonKabi.current)}

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
    </>
  );
}

/**
 * HARİTA BALONU — bir noktaya dokunulduğunda açılan kart.
 *
 * <p>
 * Eski hâli <code>setHTML</code> ile üretilen bir dize ve satır içi
 * <code>#8A8A8C</code> gibi sabit renklerdi: kurumsal kimlik temasını da gece
 * modunu da bilmiyordu ve "Ayrıntı" bağlantısı uygulamayı baştan
 * yüklüyordu — harita, yakınlaştırma ve süzgeçler gidiyordu.
 * </p>
 *
 * <p>
 * <b>Fotoğraf en üstte.</b> Haritada bir noktaya dokunan kişinin sorusu
 * "burada ne var?"; adres ve durum bunu kısmen anlatıyor, çukurun fotoğrafı
 * tek karede anlatıyor. Görselin adresi sunucudan geliyor — hangi indirme
 * ucundan okunacağı kayıt türüne göre değişiyor ve bu bilgi istemcide
 * tekrarlanmamalı.
 * </p>
 */
function HaritaBalonu({ nokta }: { nokta: WorkMapPoint }) {
  const bildirim = nokta.tur === 'bildirim';
  const yol = bildirim ? '/vatandas-bildirimleri' : `/gorevler/${nokta.id}`;
  const { adres: fotograf } = useKorumaliAdres(nokta.fotograf);

  return (
    <div className="w-[248px] overflow-hidden rounded-lg bg-surface text-ink shadow-2">
      {fotograf && (
        <img
          src={fotograf}
          alt=""
          className="h-28 w-full border-b border-line object-cover"
        />
      )}

      <div className="p-3">
        <div className="flex items-center gap-1.5">
          {/* Durum rengi noktanın kendi renginden: haritadaki iğneyle
              balondaki rozet aynı şeyi söylemeli. */}
          <span
            className="h-2 w-2 flex-none rounded-full"
            style={{ background: nokta.renk || 'var(--brand-ui)' }}
            aria-hidden
          />
          <span className="min-w-0 flex-1 truncate text-2xs font-medium text-text-2">
            {nokta.durumAd}
          </span>
          {nokta.gecikti && (
            <span className="inline-flex shrink-0 items-center gap-1 text-2xs font-semibold text-(--st-no)">
              <AlertTriangle size={11} strokeWidth={2.4} />
              gecikti
            </span>
          )}
        </div>

        <p className="mt-1.5 line-clamp-2 font-display text-sm font-bold leading-snug">
          {nokta.baslik}
        </p>

        <p className="mt-0.5 font-mono text-3xs tabular-nums text-ink-3">{nokta.takipNo}</p>

        {nokta.adres && (
          <p className="mt-1.5 flex items-start gap-1 text-2xs text-text-2">
            <MapPin size={11} className="mt-px shrink-0 text-text-3" />
            <span className="line-clamp-2">{nokta.adres}</span>
          </p>
        )}

        {/*
          `Link` — düz `<a href>` DEĞİL. Balon MapLibre'nin düğümünde ama
          portal sayesinde React ağacının içinde, dolayısıyla yönlendirici
          çalışıyor ve uygulama yeniden yüklenmiyor.
        */}
        <Link
          to={yol}
          className="mt-2.5 flex h-9 items-center justify-center gap-1 rounded-control bg-brand text-2xs font-semibold text-on-brand"
        >
          {bildirim ? 'Bildirimi incele' : 'Görevi aç'}
          <ArrowRight size={13} />
        </Link>
      </div>
    </div>
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


