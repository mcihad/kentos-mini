import {
  DndContext, PointerSensor, pointerWithin, useDndMonitor, useDraggable, useDroppable,
  useSensor, useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import { Lock, Plus, Repeat } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { eventColors } from '../components/Color';
import { cn } from '../components/utils';
import { isSameDay } from './viewWindow';
import type { CalendarEvent } from './types';
import {
  SLOT_HEIGHT, DAY_HEIGHT, HOUR_HEIGHT,
  overlapping, snapToSlot, eventRange, layoutDay, pixelsToMinutes,
} from './layout';

const GUN_ADLARI = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'];
const DILIM_MS = 30 * 60_000;

export type DragResult = {
  etkinlik: CalendarEvent;
  yeniBaslangic: Date;
  yeniBitis: Date;
  cakisanAdet: number;
};

type SurukleVerisi = {
  etkinlik: CalendarEvent;
  tur: 'tasi' | 'ust' | 'alt';
  gunIndeksi: number;
};

/**
 * Saat ızgarası — gün ve hafta görünümlerinin ortak gövdesi.
 *
 * <p>
 * Tek bir bileşen: hafta görünümü "yedi sütunlu gün görünümü"dür. İki ayrı
 * kopya, sürükleme ve yerleşim hatalarının yalnızca birinde düzeltilmesine
 * yol açardı.
 * </p>
 *
 * <h4>Etkileşim</h4>
 * <ul>
 *   <li>Gövdeden sürükle → taşı (süre korunur; haftada <b>gün de değişir</b>)</li>
 *   <li>Üst/alt kenardan sürükle → yalnızca o kenarı oynat</li>
 *   <li>Her ikisi de 30 dakikaya yuvarlanır</li>
 *   <li>Boş dilimin üzerine gelince <b>+</b> çıkar; tıklayınca o saate etkinlik</li>
 *   <li>Çakışma engellenmez; bırakınca uyarılır</li>
 * </ul>
 */
export function TimeGrid({
  gunler,
  etkinlikler,
  onEtkinlikAc,
  onZamanDegisti,
  onBosDilim,
}: {
  /** Gösterilecek günler; 1 = gün görünümü, 7 = hafta görünümü. */
  gunler: Date[];
  etkinlikler: CalendarEvent[];
  onEtkinlikAc: (e: CalendarEvent) => void;
  onZamanDegisti: (s: DragResult) => void;
  onBosDilim?: (baslangic: Date) => void;
}) {
  const coklu = gunler.length > 1;
  const tumGunler = useMemo(() => etkinlikler.filter((e) => e.tumGun), [etkinlikler]);
  const kaydirmaRef = useRef<HTMLDivElement>(null);

  // 4px'lik eşik: kısa dokunuş hâlâ "tıklama" sayılsın, detay açılabilsin.
  const sensorler = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 4 } }));

  /**
   * Açılışta çalışma saatlerine kaydır.
   *
   * 24 saatlik ızgara gece yarısından başlıyor; kullanıcıyı boş gecelerin
   * ortasında bırakmamak için 07:00 hizasına gidilir. Bugün görünüyorsa
   * "şu an" çizgisi hedeflenir.
   */
  useEffect(() => {
    const kutu = kaydirmaRef.current;
    if (!kutu) return;

    const bugunIndeksi = gunler.findIndex((g) => isSameDay(g, new Date()));
    const dk =
      bugunIndeksi >= 0 ? new Date().getHours() * 60 + new Date().getMinutes() - 90 : 7 * 60;
    kutu.scrollTop = Math.max(0, (dk / 30) * SLOT_HEIGHT);

    // Dar ekranda ızgara yatay da kayıyor; bugünün sütunu görünsün.
    if (bugunIndeksi >= 0) {
      const sutun = kutu.querySelector<HTMLElement>(`#gun-${bugunIndeksi}`);
      if (sutun && sutun.offsetLeft + sutun.offsetWidth > kutu.clientWidth) {
        kutu.scrollLeft = Math.max(0, sutun.offsetLeft - 52);
      }
    }
    // Yalnızca ilk yerleşimde: kullanıcı kaydırdıktan sonra geri zıplamamalı.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gunler[0]?.getTime(), gunler.length]);

  function birakildi(olay: DragEndEvent) {
    const veri = olay.active.data.current as SurukleVerisi | undefined;
    if (!veri) return;

    const hedef = olay.over?.data.current as { gunIndeksi?: number } | undefined;
    const gunFarki =
      veri.tur === 'tasi' && typeof hedef?.gunIndeksi === 'number'
        ? hedef.gunIndeksi - veri.gunIndeksi
        : 0;

    const kaydirmaPx = snapToSlot(olay.delta.y);
    if (kaydirmaPx === 0 && gunFarki === 0) return;

    const kaydirmaDk = pixelsToMinutes(kaydirmaPx);
    const { bas, bit } = eventRange(veri.etkinlik);

    let yeniBas = bas;
    let yeniBit = bit;

    if (veri.tur === 'tasi') {
      yeniBas = gunEkle(new Date(bas.getTime() + kaydirmaDk * 60_000), gunFarki);
      yeniBit = gunEkle(new Date(bit.getTime() + kaydirmaDk * 60_000), gunFarki);
    } else if (veri.tur === 'ust') {
      yeniBas = new Date(bas.getTime() + kaydirmaDk * 60_000);
      // En az bir dilim kalsın.
      if (yeniBas >= new Date(bit.getTime() - DILIM_MS)) return;
    } else {
      yeniBit = new Date(bit.getTime() + kaydirmaDk * 60_000);
      if (yeniBit <= new Date(bas.getTime() + DILIM_MS)) return;
    }

    onZamanDegisti({
      etkinlik: veri.etkinlik,
      yeniBaslangic: yeniBas,
      yeniBitis: yeniBit,
      cakisanAdet: overlapping(etkinlikler, yeniBas, yeniBit, veri.etkinlik.id).length,
    });
  }

  /**
   * Tek ızgara, üç satır: başlıklar · tüm gün · saatler.
   *
   * <p>
   * Ayrı <c>flex</c> satırları yerine TEK CSS grid: satırlar böyle her zaman
   * hizalı kalıyor ve dar ekranda ızgara <b>yatay kayarken</b> başlıklar
   * sütunlarıyla birlikte gidiyor. 390px'te yedi sütun 46px'e düşüyor ve
   * etkinlik başlığı "Günlü" diye kırpılıyordu; en az 104px genişlik
   * garantisiyle metin okunur kalıyor, kullanıcı yana kaydırıyor.
   * </p>
   */
  /*
    IZGARA DEĞİL, ESNEK SATIRLAR — saat sütunu yapışsın diye.

    Önce hepsi TEK bir CSS grid'iydi ve saat sütunu `sticky left-0` taşıyordu.
    Çalışmıyordu: **yapışkan bir grid öğesinin kapsayıcı bloğu kendi grid
    alanıdır**, yani 52px'lik hücresinin dışına çıkamıyor. Hafta görünümünde
    ızgara 780px, telefonda görünen 356px; yana kaydırınca saat sütunu da
    kayıp gidiyor ve kullanıcı hangi saate baktığını göremiyordu.

    Her satır kendi esnek kutusu olunca yapışkan hücrenin kapsayıcı bloğu
    SATIRIN TAMAMI oluyor ve `sticky left-0` beklendiği gibi çalışıyor.
    Sütun hizası korunuyor çünkü bütün satırlar aynı ölçüleri kullanıyor:
    52px sabit + günler `flex-1 basis-0`, en az `enAzGenislik`.
  */
  const enAzGenislik = coklu ? 104 : 0;
  const gunHucresi = 'min-w-0 flex-1 basis-0';
  const gunStili = { minWidth: enAzGenislik || undefined } as const;
  const saatSutunu = 'sticky left-0 z-20 w-[52px] flex-none bg-surface';

  return (
    <div className="overflow-hidden rounded-card border border-border bg-surface">
      <DndContext sensors={sensorler} collisionDetection={pointerWithin} onDragEnd={birakildi}>
        <div ref={kaydirmaRef} className="max-h-[68dvh] overflow-auto">
          {/* `min-w-max`: satırlar aynı genişlikte kalsın; biri daralırsa
              sütunlar satırdan satıra kayardı. */}
          <div className="min-w-max">
            {/* ── Satır 1: gün başlıkları (yalnızca hafta) ── */}
            {coklu && (
              <div className="sticky top-0 z-30 flex">
                <div className={cn(saatSutunu, 'z-40 border-b border-r border-border bg-surface-2')} />
                {gunler.map((g) => {
                  const bugunMu = isSameDay(g, new Date());
                  const haftaSonu = g.getDay() === 0 || g.getDay() === 6;
                  return (
                    <div
                      key={g.getTime()}
                      style={gunStili}
                      className={cn(
                        gunHucresi,
                        'border-b border-r border-border py-1.5 text-center last:border-r-0',
                        bugunMu ? 'bg-brand-tint' : 'bg-surface-2',
                      )}
                    >
                      <span
                        className={cn(
                          'block text-2xs font-semibold uppercase tracking-[0.08em]',
                          haftaSonu ? 'text-brand-2' : 'text-text-3',
                        )}
                      >
                        {GUN_ADLARI[g.getDay()]}
                      </span>
                      <span
                        className={cn(
                          'mx-auto mt-0.5 grid h-6 w-6 place-items-center rounded-full font-display text-sm font-bold tabular-nums',
                          bugunMu ? 'bg-brand text-on-brand' : 'text-text',
                        )}
                      >
                        {g.getDate()}
                      </span>
                    </div>
                  );
                })}
              </div>
            )}

            {/* ── Satır 2: tüm gün şeridi ── */}
            {tumGunler.length > 0 && (
              <div className="flex">
                <div className={cn(saatSutunu, 'grid place-items-center border-b border-r border-border text-2xs uppercase tracking-[0.06em] text-text-3')}>
                  Tüm gün
                </div>
                {gunler.map((g) => (
                  <div
                    key={g.getTime()}
                    style={gunStili}
                    className={cn(gunHucresi, 'space-y-1 border-b border-r border-border p-1 last:border-r-0')}
                  >
                    {tumGunler
                      .filter((e) => isSameDay(eventRange(e).bas, g))
                      .map((e) => {
                        const renkler = eventColors(e.durumRenk, e.tipRenk);
                        return (
                          <button
                            key={e.id}
                            onClick={() => onEtkinlikAc(e)}
                            style={{ background: renkler.cipZemini, borderLeftColor: renkler.kenar }}
                            className="block w-full truncate rounded-sm border-l-2 px-1.5 py-1 text-left text-2xs font-medium"
                          >
                            {e.baslik}
                          </button>
                        );
                      })}
                  </div>
                ))}
              </div>
            )}

            {/* ── Satır 3: saat sütunu + gün sütunları ── */}
            <div className="flex">
            <div
              className={cn(saatSutunu, 'border-r border-border')}
              style={{ height: DAY_HEIGHT }}
            >
              {Array.from({ length: 24 }).map((_, s) => (
                <div key={s} style={{ height: HOUR_HEIGHT }} className="relative">
                  <span className="absolute top-[-7px] right-2 text-2xs tabular-nums text-text-3">
                    {s === 0 ? '' : `${String(s).padStart(2, '0')}:00`}
                  </span>
                </div>
              ))}
            </div>

            {gunler.map((g, i) => (
              <GunSutunu
                key={g.getTime()}
                gun={g}
                gunIndeksi={i}
                ilkDegil={i > 0}
                enAzGenislik={enAzGenislik}
                etkinlikler={etkinlikler}
                onEtkinlikAc={onEtkinlikAc}
                onBosDilim={onBosDilim}
              />
            ))}
            </div>
          </div>
        </div>
      </DndContext>
    </div>
  );
}

/** Bir günün sütunu: ızgara hücreleri + etkinlik blokları + şu an çizgisi. */
function GunSutunu({
  gun,
  gunIndeksi,
  ilkDegil,
  enAzGenislik,
  etkinlikler,
  onEtkinlikAc,
  onBosDilim,
}: {
  gun: Date;
  gunIndeksi: number;
  ilkDegil: boolean;
  /** Hafta görünümünde sütunun daralabileceği alt sınır. */
  enAzGenislik: number;
  etkinlikler: CalendarEvent[];
  onEtkinlikAc: (e: CalendarEvent) => void;
  onBosDilim?: (baslangic: Date) => void;
}) {
  const yerlesimler = useMemo(() => layoutDay(etkinlikler, gun), [etkinlikler, gun]);

  const { setNodeRef } = useDroppable({
    id: `gun-${gunIndeksi}`,
    data: { gunIndeksi },
  });

  return (
    <div
      ref={setNodeRef}
      // Kimlik DOM'da da duruyor: dnd-kit yalnızca kendi kaydını tutuyor,
      // açılışta bugünün sütununa yatay kaydırma bu seçiciyle yapılıyor.
      id={`gun-${gunIndeksi}`}
      className={cn('relative min-w-0 flex-1 basis-0', ilkDegil && 'border-l border-border')}
      style={{ height: DAY_HEIGHT, minWidth: enAzGenislik || undefined }}
    >
      {Array.from({ length: 48 }).map((_, i) => (
        <BosDilim
          key={i}
          gun={gun}
          dilim={i}
          onBosDilim={onBosDilim}
        />
      ))}

      <SuAnCizgisi gun={gun} />

      {yerlesimler.map((y) => (
        <EtkinlikBlogu
          key={y.etkinlik.id}
          yerlesim={y}
          gunIndeksi={gunIndeksi}
          onAc={onEtkinlikAc}
        />
      ))}
    </div>
  );
}

/**
 * Boş yarım saatlik hücre.
 *
 * <p>
 * Üzerine gelince <b>+</b> beliriyor: tıklanabilir olduğunu başka türlü
 * anlatmanın yolu yok, kalıcı bir işaret ise 48 hücrede gürültü olurdu.
 * </p>
 *
 * <p>
 * <c>tabIndex={-1}</c>: hafta görünümünde 336 hücre var, hepsi sekme durağı
 * olsaydı klavye kullanıcısı ızgarayı geçemezdi. Klavyeyle etkinlik ekleme
 * yolu araç çubuğundaki "Yeni etkinlik" düğmesi.
 * </p>
 */
function BosDilim({
  gun,
  dilim,
  onBosDilim,
}: {
  gun: Date;
  dilim: number;
  onBosDilim?: (baslangic: Date) => void;
}) {
  const saat = Math.floor(dilim / 2);
  const dakika = (dilim % 2) * 30;
  const etiket = `${String(saat).padStart(2, '0')}:${String(dakika).padStart(2, '0')}`;

  if (!onBosDilim) {
    return (
      <div
        style={{ height: SLOT_HEIGHT }}
        className={cn('border-b', dilim % 2 === 1 ? 'border-border' : 'border-border/40')}
      />
    );
  }

  return (
    <button
      type="button"
      tabIndex={-1}
      aria-label={`${etiket} için etkinlik ekle`}
      title={`${etiket} · etkinlik ekle`}
      onClick={() => {
        const t = new Date(gun);
        t.setHours(saat, dakika, 0, 0);
        onBosDilim(t);
      }}
      style={{ height: SLOT_HEIGHT }}
      className={cn(
        'group relative block w-full cursor-copy border-b text-left transition-colors hover:bg-brand-tint',
        dilim % 2 === 1 ? 'border-border' : 'border-border/40',
      )}
    >
      <span
        className="pointer-events-none absolute left-1 top-1/2 grid h-[15px] w-[15px] -translate-y-1/2 place-items-center
          rounded-sm bg-brand text-on-brand opacity-0 shadow-1 transition-opacity group-hover:opacity-100"
        aria-hidden
      >
        <Plus size={10} strokeWidth={3} />
      </span>
      <span className="pointer-events-none absolute right-1.5 top-1/2 -translate-y-1/2 text-2xs tabular-nums text-text-3 opacity-0 transition-opacity group-hover:opacity-100">
        {etiket}
      </span>
    </button>
  );
}

function EtkinlikBlogu({
  yerlesim,
  gunIndeksi,
  onAc,
}: {
  yerlesim: ReturnType<typeof layoutDay>[number];
  gunIndeksi: number;
  onAc: (e: CalendarEvent) => void;
}) {
  const { etkinlik, ustPx, yukseklikPx, sutun, sutunSayisi } = yerlesim;

  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: `tasi-${etkinlik.id}`,
    data: { etkinlik, tur: 'tasi', gunIndeksi } satisfies SurukleVerisi,
  });

  /*
    BOYUTLANDIRMA CANLI ÇİZİLİR.

    Kenar tutamacı AYRI bir `useDraggable` (`ust-…` / `alt-…`); bloğun kendi
    `transform`u yalnızca TAŞIMA sürüklemesinden geliyor. Dolayısıyla kenardan
    çekerken ekranda hiçbir şey kıpırdamıyor, kullanıcı parmağını bırakana
    kadar ne yaptığını göremiyordu — sürükleme çalışıyor ama "tutmuyor" gibi
    hissettiriyordu.

    `useDndMonitor` sürüklemeyi blok içinden dinler ve yalnızca KENDİ kenarı
    çekilen blok tepki verir. Sapma dilime yuvarlanır: serbest piksel takibi
    30 dakikalık ızgarada yalan söylerdi — parmak arada bir yerdeyken blok
    ordaymış gibi durur, bırakınca zıplardı. Yuvarlayınca blok kilitlenerek
    ilerliyor; native takvimlerin verdiği his bu.
  */
  const [boyut, setBoyut] = useState<{ kenar: 'ust' | 'alt'; dy: number } | null>(null);
  useDndMonitor({
    onDragMove(o) {
      const v = o.active.data.current as SurukleVerisi | undefined;
      if (!v || v.tur === 'tasi' || v.etkinlik.id !== etkinlik.id) return;
      setBoyut({ kenar: v.tur, dy: snapToSlot(o.delta.y) });
    },
    onDragEnd: () => setBoyut(null),
    onDragCancel: () => setBoyut(null),
  });

  const genislik = 100 / sutunSayisi;
  const { bas: hamBas, bit: hamBit } = eventRange(etkinlik);
  const saat = (t: Date) =>
    `${String(t.getHours()).padStart(2, '0')}:${String(t.getMinutes()).padStart(2, '0')}`;

  // Önizleme geometrisi — en az bir dilim kalır.
  let ust = ustPx;
  let yuk = yukseklikPx;
  let sapmaDk = 0;
  if (boyut) {
    if (boyut.kenar === 'ust') {
      const d = Math.min(boyut.dy, yukseklikPx - SLOT_HEIGHT);
      ust = ustPx + d;
      yuk = yukseklikPx - d;
      sapmaDk = pixelsToMinutes(d);
    } else {
      const d = Math.max(boyut.dy, SLOT_HEIGHT - yukseklikPx);
      yuk = yukseklikPx + d;
      sapmaDk = pixelsToMinutes(d);
    }
  }

  // Saat etiketi de önizlemeyle birlikte döner; blok uzarken "09:00–09:30"
  // yazmaya devam etseydi hangi aralığa geldiğin görünmezdi.
  const bas = boyut?.kenar === 'ust' ? new Date(hamBas.getTime() + sapmaDk * 60_000) : hamBas;
  const bit = boyut?.kenar === 'alt' ? new Date(hamBit.getTime() + sapmaDk * 60_000) : hamBit;

  // Renk kaynağı: önce etkinlik DURUMU, yoksa tip. Eski arayüzle aynı kural.
  const renkler = eventColors(etkinlik.durumRenk, etkinlik.tipRenk);
  const kisa = yuk < SLOT_HEIGHT * 1.5;

  return (
    <div
      ref={setNodeRef}
      style={{
        top: ust,
        height: yuk,
        left: `calc(${sutun * genislik}% + 3px)`,
        width: `calc(${genislik}% - 6px)`,
        transform: transform
          ? `translate3d(${transform.x}px, ${transform.y}px, 0)`
          : undefined,
        zIndex: isDragging ? 30 : 1,
        background: renkler.zemin,
        borderLeftColor: renkler.kenar,
        /*
          SÜRÜKLEME İKİ ŞEY OLMADAN ÇALIŞMIYOR — dnd-kit bunları kendi
          vermiyor, öğeye bizim koymamız gerekiyor:
          • `touchAction: none` — yoksa dokunmatikte tarayıcı hareketi
            KAYDIRMA sayıp olayı kendine alıyor; parmak etkinliği taşımıyor,
            ızgara kayıyor.
          • `userSelect: none` — yoksa fareyle sürüklerken tarayıcı metni
            SEÇİYOR; kullanıcı etkinliği taşımaya çalışırken elinde mavi bir
            seçim kalıyordu.
        */
        touchAction: 'none',
        userSelect: 'none',
        WebkitUserSelect: 'none',
      }}
      title={[etkinlik.baslik, etkinlik.durumAd, etkinlik.tipAd].filter(Boolean).join(' · ')}
      className={cn(
        'group/etkinlik absolute overflow-hidden rounded-sm border-l-2 shadow-1',
        'transition-[box-shadow,outline-color,transform] duration-150',
        // TAŞINABİLDİĞİ BELLİ OLSUN.
        //
        // Blok sürüklenebiliyordu ama bunu söyleyen hiçbir şey yoktu:
        // kullanıcı basıp tutuyor, ekranda bir değişiklik olmuyor, taşınıp
        // taşınamayacağını denemeden anlayamıyordu.
        //  • fare üstündeyken: hafif yükselme + marka rengi hâle
        //  • basılıyken: `scale(.98)` — dokunuşun karşılığı anında
        //  • sürüklenirken: kalın hâle + güçlü gölge, blok "havalanıyor"
        'outline outline-2 outline-offset-[-2px] outline-transparent',
        'hover:shadow-2 hover:outline-brand-line',
        'active:scale-[0.98]',
        isDragging && 'z-30 scale-[1.02] shadow-3 outline-brand opacity-95',
        // Boyutlandırma sırasında blok da öne çıkar — ama ÖLÇEKLENMEZ:
        // kenar hizası tam da kullanıcının baktığı şey, `scale` onu kaydırır.
        boyut && 'z-30 shadow-3 outline-brand',
      )}
      {...attributes}
    >
      {/* Üst kenar tutamacı */}
      <BoyutTutamaci etkinlik={etkinlik} gunIndeksi={gunIndeksi} kenar="ust" kisa={kisa} />

      {/*
        Yarım saatlik blok 28px: saat ve başlık ALT ALTA sığmıyor, başlık
        alttan kırpılıyordu. Kısa blokta ikisi tek satıra alınır.
      */}
      <button
        {...listeners}
        onClick={() => onAc(etkinlik)}
        className={cn(
          'block h-full w-full cursor-grab text-left active:cursor-grabbing',
          kisa ? 'px-1.5 py-[3px]' : 'px-1.5 py-1',
        )}
      >
        {kisa ? (
          <span className="flex items-center gap-1 overflow-hidden text-2xs leading-[1.2]">
            <span className="shrink-0 font-display text-2xs font-semibold tabular-nums text-text-2">
              {saat(bas)}
            </span>
            {etkinlik.gizli && <Lock size={9} strokeWidth={2.2} className="shrink-0" />}
            {etkinlik.seriId && <Repeat size={9} strokeWidth={2.2} className="shrink-0" />}
            <span className="truncate">{etkinlik.baslik}</span>
          </span>
        ) : (
          <>
            <span className="flex items-center gap-1 font-display text-2xs font-semibold tabular-nums text-text-2">
              {saat(bas)}–{saat(bit)}
              {etkinlik.gizli && <Lock size={9} strokeWidth={2.2} />}
              {etkinlik.seriId && <Repeat size={9} strokeWidth={2.2} />}
            </span>
            <span className="line-clamp-2 text-2xs leading-tight">{etkinlik.baslik}</span>
          </>
        )}
      </button>

      <BoyutTutamaci etkinlik={etkinlik} gunIndeksi={gunIndeksi} kenar="alt" kisa={kisa} />
    </div>
  );
}

/**
 * Kenar tutamacı.
 *
 * <h4>Kısa blokta tutamak KÖŞEYE çekilir</h4>
 * <p>
 * Dokunmatikte tutamaklar 14px'ti; 30 dakikalık blok da 28px. İki tutamak
 * bloğun <b>tamamını</b> kaplıyor ve taşımaya tek piksel kalmıyordu — kısa
 * etkinlikler telefonda yalnızca boyutlandırılabiliyor, taşınamıyordu.
 * </p>
 * <p>
 * Kısa blokta (tek dilim) dokunmatik davranış şu: <b>üst tutamak yok</b> —
 * başlangıcı değiştirmek istiyorsan bloğu taşırsın, aynı kapıya çıkar. Alt
 * tutamak ise tam genişlik yerine <b>sağ alt köşede</b> 56px'lik bir tutamağa
 * iner. Böylece bloğun geri kalanı baştan sona taşıma alanı olarak kalıyor ve
 * uzatma da elden gitmiyor.
 * </p>
 * <p>
 * Masaüstünde ikisi de tam genişlik ve 8px: fare hassas, üstelik tutamaklar
 * yalnızca imleç blokta beliriyor.
 * </p>
 */
function BoyutTutamaci({
  etkinlik,
  gunIndeksi,
  kenar,
  kisa,
}: {
  etkinlik: CalendarEvent;
  gunIndeksi: number;
  kenar: 'ust' | 'alt';
  /** Tek dilimlik blok — dokunmatikte yer kavgası burada çıkıyor. */
  kisa: boolean;
}) {
  const { attributes, listeners, setNodeRef } = useDraggable({
    id: `${kenar}-${etkinlik.id}`,
    data: { etkinlik, tur: kenar, gunIndeksi } satisfies SurukleVerisi,
  });

  return (
    <div
      ref={setNodeRef}
      {...listeners}
      {...attributes}
      aria-label={kenar === 'ust' ? 'Başlangıcı değiştir' : 'Bitişi değiştir'}
      // Taşıma bloğuyla aynı sebep; ayrıca 6px'lik tutamak dokunmatikte
      // avlanamıyordu — mobilde 12px'e çıkıyor (hedef yine 44px değil ama
      // burası ikincil bir eylem ve blok zaten taşınabiliyor).
      style={{ touchAction: 'none', userSelect: 'none', WebkitUserSelect: 'none' }}
      className={cn(
        // GÖRÜNÜR TUTAMAK.
        //
        // Önce 6px'lik saydam bir şeritti: ne olduğu görünmüyordu, dokunmatikte
        // de avlanamıyordu. Artık blokla etkileşime girildiğinde kısa bir çubuk
        // beliriyor — "buradan uzatabilirsin" demenin en kısa yolu.
        'absolute z-20 grid place-items-center cursor-ns-resize',
        kenar === 'ust' ? 'top-0' : 'bottom-0',
        kisa
          ? // TEK DİLİMLİK BLOK (28px): üst tutamak YOK, alt tutamak SAĞ KÖŞEDE.
            //
            // İki tam genişlik tutamak 28px'lik bloğun tamamını yiyordu —
            // mobilde 14+14, masaüstünde 8+8 üstelik ortada kalan 12px de
            // avlanabilir bir hedef değil. Kısa etkinlik boyutlandırılabiliyor
            // ama TAŞINAMIYORDU. Kural işaretçi türünden bağımsız: fareyle de
            // 12px'lik bir şeridi tutturmak zordu.
            //
            // Başlangıcı değiştirmek için bloğu taşımak yeterli; sonu uzatmak
            // ise köşedeki tutamakla. Geri kalan yüzeyin tamamı taşıma alanı.
            kenar === 'ust'
            ? 'hidden'
            : 'right-0 h-[12px] w-14'
          : 'inset-x-0 h-[8px] pointer-coarse:h-[14px]',
      )}
    >
      <span
        aria-hidden
        className={cn(
          'h-[3px] w-7 rounded-full bg-ink-3 opacity-0 transition-opacity',
          'group-hover/etkinlik:opacity-70 group-active/etkinlik:opacity-90',
          /*
            DOKUNMATİKTE HOVER YOK. Tutamak yalnızca `group-hover` ile
            görünüyordu, yani telefonda HİÇ görünmüyordu: uzatılabildiğini
            söyleyen tek işaret yoktu. `pointer-coarse` kaba işaretçili
            (parmak) cihazlarda hep açık tutar — masaüstünde ise her blokta
            duran çubuklar gürültü olurdu, orada üstüne gelince beliriyor.
          */
          'pointer-coarse:opacity-45',
        )}
      />
    </div>
  );
}

/**
 * "Şu an" çizgisi.
 *
 * <p>
 * <b>Kırmızı</b> — altın, "bugün" halkasının ve etkin sekmenin rengi; aynı
 * rengi "şu an" için de kullanmak ikisini karıştırıyordu. Kırmızı bu ekranda
 * başka hiçbir şeyi işaretlemiyor, dolayısıyla tek anlamı var.
 * </p>
 */
function SuAnCizgisi({ gun }: { gun: Date }) {
  const [simdi, setSimdi] = useState(() => new Date());

  // Dakikada bir tazele: açık bırakılan ekranda çizgi yerinde donuyordu.
  useEffect(() => {
    const z = setInterval(() => setSimdi(new Date()), 60_000);
    return () => clearInterval(z);
  }, []);

  if (!isSameDay(simdi, gun)) return null;

  const ust = ((simdi.getHours() * 60 + simdi.getMinutes()) / 30) * SLOT_HEIGHT;
  const etiket = `${String(simdi.getHours()).padStart(2, '0')}:${String(simdi.getMinutes()).padStart(2, '0')}`;

  return (
    <div
      className="pointer-events-none absolute inset-x-0 z-20"
      style={{ top: ust }}
      aria-label={`Şu an ${etiket}`}
    >
      <div className="relative h-[1.5px] bg-(--simdi)">
        <span className="absolute left-[-4px] top-[-4px] h-[9px] w-[9px] rounded-full bg-(--simdi) ring-2 ring-(--surface)" />
        <span className="absolute top-[-8px] right-1 rounded-xs bg-(--simdi) px-1 py-px font-display text-2xs font-bold tabular-nums text-white">
          {etiket}
        </span>
      </div>
    </div>
  );
}

/** Takvim günü ekler — saat/dakika korunur. */
function gunEkle(t: Date, gun: number): Date {
  const d = new Date(t);
  d.setDate(d.getDate() + gun);
  return d;
}
