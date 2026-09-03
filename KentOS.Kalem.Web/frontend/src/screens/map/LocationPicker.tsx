import { Crosshair, Loader2, MapPin } from 'lucide-react';
import * as maplibregl from 'maplibre-gl';
import { useEffect, useRef, useState } from 'react';
import 'maplibre-gl/dist/maplibre-gl.css';
import { Button } from '../../components/Button';
import { Input } from '../../components/Field';
import {
  HARITA_TEMASI, NOKTA_YAKINLIK, TURKIYE_MERKEZ, TURKIYE_YAKINLIK, konumIste, webgl2Var,
} from './harita';

export type Konum = { enlem: number; boylam: number };

/**
 * KONUM SEÇİCİ — haritada tek nokta.
 *
 * <p>
 * Hem vatandaş portalında hem saha tespitinde kullanılıyor. İki ayrı seçici
 * yazmak, birinde düzeltilen bir davranışın ötekinde kalmasına yol açardı.
 * </p>
 *
 * <p>
 * <b>Konum izni istemek zorunlu değil.</b> Kullanıcı reddedebilir ve bu
 * tamamen meşru; harita elle işaretlemeye açık kalıyor. Portalda izin
 * kendiliğinden İSTENMİYOR — sayfa açılır açılmaz gelen bir izin kutusu,
 * kurumla ilk teması bir engelle başlatırdı.
 * </p>
 */
export function LocationPicker({
  deger,
  degistir,
  yukseklik = 240,
  otomatikKonum,
}: {
  deger: Konum | null;
  degistir: (k: Konum | null) => void;
  yukseklik?: number;
  /** Açılışta konum izni iste — SAHA ekranlarında mantıklı, portalda değil. */
  otomatikKonum?: boolean;
}) {
  const kap = useRef<HTMLDivElement>(null);
  const harita = useRef<maplibregl.Map | null>(null);
  const isaret = useRef<maplibregl.Marker | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  // Harita çizilemiyorsa konum girişi TAMAMEN kapanmamalı: "konumumu bul"
  // ve elle koordinat hâlâ çalışıyor.
  const haritaVar = webgl2Var();

  // `degistir` her render'da yeni bir işlev; olay dinleyicisine doğrudan
  // bağlansaydı harita her render'da yeniden kurulurdu.
  const geriCagri = useRef(degistir);
  geriCagri.current = degistir;

  useEffect(() => {
    if (!haritaVar || !kap.current || harita.current) return;

    const h = new maplibregl.Map({
      container: kap.current,
      style: HARITA_TEMASI,
      center: deger ? [deger.boylam, deger.enlem] : TURKIYE_MERKEZ,
      zoom: deger ? NOKTA_YAKINLIK : TURKIYE_YAKINLIK,
      attributionControl: { compact: true },
    });

    h.addControl(new maplibregl.NavigationControl({ showCompass: false }), 'top-right');

    // Haritaya dokunmak noktayı TAŞIR. Ayrı bir "işaretle" kipi koymak,
    // tek elle kullanan biri için fazladan bir adım olurdu.
    h.on('click', (o: maplibregl.MapMouseEvent) => {
      geriCagri.current({ enlem: o.lngLat.lat, boylam: o.lngLat.lng });
    });

    harita.current = h;

    return () => {
      h.remove();
      harita.current = null;
      isaret.current = null;
    };
    // Yalnızca bir kez kurulur; `deger` değişimi aşağıdaki etkide işleniyor.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // İşaretçi değerle senkron: dışarıdan gelen değişim de (konumumu bul)
  // haritaya yansımalı.
  useEffect(() => {
    const h = harita.current;
    if (!h) return;

    if (!deger) {
      isaret.current?.remove();
      isaret.current = null;
      return;
    }

    const konum: [number, number] = [deger.boylam, deger.enlem];

    if (!isaret.current) {
      isaret.current = new maplibregl.Marker({ color: '#B3261E', draggable: true })
        .setLngLat(konum)
        .addTo(h);

      // Sürükleyerek ince ayar: dokunmayla konan nokta çoğu zaman birkaç
      // metre kayıyor.
      isaret.current.on('dragend', () => {
        const k = isaret.current!.getLngLat();
        geriCagri.current({ enlem: k.lat, boylam: k.lng });
      });
    } else {
      isaret.current.setLngLat(konum);
    }

    h.easeTo({ center: konum, zoom: Math.max(h.getZoom(), NOKTA_YAKINLIK - 2), duration: 400 });
  }, [deger]);

  async function konumumuBul() {
    setBekliyor(true);
    try {
      const k = await konumIste();
      if (k) degistir(k);
    } finally {
      setBekliyor(false);
    }
  }

  // Saha ekranlarında açılışta konum isteniyor: personel zaten olayın
  // yerinde ve haritayı elle sürüklemesi gereksiz iş.
  useEffect(() => {
    if (otomatikKonum && !deger) void konumumuBul();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [otomatikKonum]);

  return (
    <div className="space-y-2">
      {haritaVar ? (
        <div
          ref={kap}
          style={{ height: yukseklik }}
          className="w-full overflow-hidden rounded-control border border-line"
          role="application"
          aria-label="Konum seçme haritası"
        />
      ) : (
        /*
          YEDEK: tarayıcı WebGL2 desteklemiyor.

          Haritayı gösteremiyoruz ama konum girişini kapatmıyoruz —
          "konumumu bul" ve elle koordinat çalışmaya devam ediyor. Eski bir
          cihazda formu hiç doldurulamaz yapmak, haritayı kaybetmekten çok
          daha kötü olurdu.
        */
        <div className="rounded-control border border-dashed border-line px-3 py-3">
          <p className="text-2xs text-text-3">
            Bu tarayıcı harita çizimini desteklemiyor. Konumunuzu aşağıdaki
            düğmeyle alabilirsiniz.
          </p>
        </div>
      )}

      <div className="flex items-center gap-2">
        <Button
          varyant="ikincil"
          className="h-11 flex-1"
          onClick={konumumuBul}
          disabled={bekliyor}
        >
          {bekliyor ? <Loader2 size={16} className="animate-spin" /> : <Crosshair size={16} />}
          Konumumu bul
        </Button>

        {deger && (
          <Button varyant="sade" className="h-11 shrink-0" onClick={() => degistir(null)}>
            Temizle
          </Button>
        )}
      </div>

      {deger ? (
        <p className="flex items-center gap-1.5 text-2xs text-text-2">
          <MapPin size={13} className="shrink-0 text-brand" />
          Konum işaretlendi
          {/* Koordinat GİZLENMİYOR ama ikincil: doğrulanabilir olmalı,
              kullanıcıdan yazması beklenmemeli. */}
          <span className="font-mono tabular-nums text-ink-3">
            {deger.enlem.toFixed(5)}, {deger.boylam.toFixed(5)}
          </span>
        </p>
      ) : (
        haritaVar && (
          <p className="text-2xs text-text-3">Haritaya dokunarak da işaretleyebilirsiniz.</p>
        )
      )}

      {/*
        ELLE KOORDİNAT — açılır kapanır ve VARSAYILAN KAPALI.

        Kimse bir noktanın enlemini boylamını ezbere bilmez; bu alanların
        formun ortasında durması "böyle doldurulur" demekti. Yine de
        tamamen kaldırmak, hem haritanın çizilemediği hem konum izninin
        reddedildiği bir cihazda "konum zorunlu" bir görevi açılamaz
        yapardı. Kaçış yolu duruyor, sadece yoldan çekildi.
      */}
      <details className="group">
        <summary className="cursor-pointer list-none text-2xs text-ink-3 underline-offset-2 hover:underline">
          Koordinatı elle gir
        </summary>
        <div className="mt-2 grid grid-cols-2 gap-2">
          <Input
            type="number"
            step="0.000001"
            value={deger?.enlem ?? ''}
            onChange={(e) =>
              degistir(
                e.target.value
                  ? { enlem: Number(e.target.value), boylam: deger?.boylam ?? 0 }
                  : null,
              )
            }
            placeholder="Enlem"
            aria-label="Enlem"
          />
          <Input
            type="number"
            step="0.000001"
            value={deger?.boylam ?? ''}
            onChange={(e) =>
              degistir(
                e.target.value
                  ? { enlem: deger?.enlem ?? 0, boylam: Number(e.target.value) }
                  : null,
              )
            }
            placeholder="Boylam"
            aria-label="Boylam"
          />
        </div>
      </details>
    </div>
  );
}
