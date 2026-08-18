import * as Tabs from '@radix-ui/react-tabs';
import { SekmeListesi, SekmeTetigi } from '../components/Tabs';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import {
  CalendarDays, Camera, ChevronLeft, ChevronRight, LayoutList, ListTree, Lock,
  Plus, Printer, Repeat, Search, SlidersHorizontal, Trash2,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { SearchInput } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { SkeletonRows } from '../components/Skeleton';
import { DataList, type Column } from '../components/DataList';
import { useIsDesktop } from '../components/screenSize';
import { InsetGroup, ListRow } from '../components/ListRow';
import { ColoredBadge } from '../components/Color';
import { Pagination } from '../components/Pagination';
import { SelectMenu } from '../components/SelectMenu';
import { Segment, FilterSection, FilterOptions, FilterSheet } from '../components/FilterSheet';
import { Fab } from '../shell/mobile/Fab';
import { queryKeys } from '../data/queryKeys';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { range, relativeTime, date, shortDate } from '../data/format';
import { download } from '../data/download';
import { api, queryString, type PagedResult } from '../data/client';
import { useEventTypes } from '../data/hooks';
import type { EventSummary } from '../data/types';
import { startOfDay, localToServer } from '../data/time';
import { InviteeBadge, ProgramView } from './agenda/ProgramView';
import { ProgramMenu } from './agenda/ProgramMenu';
import { dilimdenOneri, type BaslangicOnerisi } from './event/EventFields';
import { EventModal } from './event/EventModal';

type TabItem = 'program' | 'liste' | 'silinmis';

/**
 * Ajanda — sekmeli.
 *
 * <p>
 * Varsayılan sekme <b>Program</b>: eski MVC ajanda sayfasının karşılığı olan
 * tarih işaretçili akış görünümü. Kullanıcılar iki yıldır o düzeni
 * kullanıyor; yeni arayüzün ilk açılışta tanıdık gelmesi öğrenme maliyetini
 * sıfırlıyor. Tablo görünümü isteyen "Liste" sekmesine geçiyor.
 * </p>
 */
const SEKMELER: TabItem[] = ['program', 'liste', 'silinmis'];

export default function Agenda() {
  const { hasPermission } = useSession();
  // Aynı kontrolü iki kez çizmemek için — bkz. `Talepler`'deki aynı not.
  const masaustu = useIsDesktop();

  /**
   * Etkin sekme URL'de (`?sekme=liste`).
   *
   * Yönetim ekranıyla aynı gerekçe: bileşen içinde tutulan sekme, görsel
   * turun ve derin bağlantının erişemediği bir ekran demek. "Silinmiş"
   * sekmesi tek başına açılamadığı için oradaki davranış hiç doğrulanamıyordu.
   */
  const [sorgu, setSorgu] = useSearchParams();
  const sekmeDegeri = sorgu.get('sekme') as TabItem | null;
  const sekme: TabItem = sekmeDegeri && SEKMELER.includes(sekmeDegeri) ? sekmeDegeri : 'program';

  const setSekme = (d: TabItem) => {
    if (d === 'program') sorgu.delete('sekme');
    else sorgu.set('sekme', d);
    setSorgu(sorgu, { replace: true });
  };

  /** Etkinlik diyaloğu — <c>null</c> kapalı. */
  const [ekleme, setEkleme] = useState<BaslangicOnerisi | null>(null);

  /** Program sekmesinin gezindiği gün penceresi (kaç hafta ileri/geri). */
  const [haftaKaymasi, setHaftaKaymasi] = useState(0);

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [tipId, setTipId] = useState<number | null>(null);

  /**
   * Silinmiş sekmesinin dönemi — SİLİNME tarihine göre. 0 = sınır yok.
   *
   * VARSAYILAN "tüm zamanlar". Bir dönem 30 gün ile açılıyordu ve sebebi
   * listenin karışık görünmesiydi; asıl sorun ise sıralamaydı (kayıtlar kendi
   * ETKİNLİK tarihine göre diziliyordu). Sıralama silinme tarihine
   * çekildikten sonra sınır yalnızca kayıt gizliyor kaldı: aynı hesapta eski
   * arayüz ve mobil 80 kayıt gösterirken bu ekran 8 gösteriyordu. Dönem
   * seçici duruyor — daraltmak kullanıcının işi, varsayılan olarak
   * saklamak bizim işimiz değil.
   */
  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [silinmisGun, setSilinmisGun] = useState(0);
  const [sayfa, setSayfa] = useState(1);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const bugun = startOfDay(new Date());

  /**
   * Program ve Liste **AYNI** pencereyi kullanır: bugünden itibaren 2 haftalık
   * dilim, gezinmeyle kayar.
   *
   * Önce iki sekme iki ayrı aralık kuruyordu — Program 2 hafta, Liste 6 ay —
   * ve arayüzde bunu söyleyen bir şey yoktu. Sonuç: aynı ekranda "Program: 1
   * etkinlik / Liste: 2 etkinlik". Kullanıcı haklı olarak birinin kayıt
   * kaybettiğini düşündü; oysa ikisi de farklı soruya doğru cevap veriyordu.
   *
   * Sekme, kaydın GÖRÜNÜMÜNÜ değiştirir (güne göre gruplu program ya da
   * tablo), KÜMESİNİ değil. Aralık ikisinde de yazılı duruyor.
   */
  const [bas, bit] = useMemo(() => {
    const b = new Date(bugun);
    b.setDate(b.getDate() + haftaKaymasi * 14);
    const s = new Date(b);
    s.setDate(s.getDate() + 14);
    return [b, s];
  }, [haftaKaymasi, bugun.getTime()]);

  const basM = localToServer(bas);
  const bitM = localToServer(bit);

  /*
    ÖNCEKİ VERİ EKRANDA KALIR (`keepPreviousData`).

    Dönemi bir hafta ileri almak ya da sekme değiştirmek listeyi tamamen
    boşaltıp yerine iskelet koyuyordu: ekran zıplıyor, kaydırma başa dönüyor
    ve kullanıcı her dokunuşta "takıldı mı?" diye bekliyordu. Oysa yeni veri
    genellikle birkaç yüz milisaniyede geliyor.

    Artık eski liste yerinde duruyor, üstüne ince bir "yenileniyor" işareti
    biniyor ve veri gelince yerine geçiyor. İskelet yalnızca ELDE HİÇ VERİ
    YOKKEN — yani ilk açılışta — çiziliyor.
  */
  const aralikSorgusu = useQuery({
    queryKey: queryKeys.event.window(basM, bitM, sekme),
    queryFn: () => api.post<EventSummary[]>('/takvim/aralik', { baslangic: basM, bitis: bitM }),
    enabled: sekme !== 'silinmis',
    placeholderData: keepPreviousData,
  });

  const silinmis = useQuery({
    queryKey: ['etkinlik', 'silinmis', sayfa, arama, silinmisGun] as const,
    queryFn: () =>
      api.get<PagedResult<EventSummary>>(
        `/etkinlik/silinmis${queryString({ sayfa, boyut: 50, ara: arama, gun: silinmisGun })}`,
      ),
    enabled: sekme === 'silinmis',
    placeholderData: keepPreviousData,
  });

  const tipler = useEventTypes();

  const ham = sekme === 'silinmis' ? (silinmis.data?.veriler ?? []) : (aralikSorgusu.data ?? []);
  // `isLoading` = elde hiç veri yok. `isFetching` = arka planda tazeleniyor;
  // o durumda liste ekranda kalır, yalnızca üstünde bir ipucu belirir.
  const yukleniyor = sekme === 'silinmis' ? silinmis.isLoading : aralikSorgusu.isLoading;
  const tazeleniyor = sekme === 'silinmis' ? silinmis.isFetching : aralikSorgusu.isFetching;
  const hata = sekme === 'silinmis' ? silinmis.error : aralikSorgusu.error;

  const sayimlar = useMemo(() => {
    const m = new Map<number, number>();
    for (const e of ham) if (e.tipId) m.set(e.tipId, (m.get(e.tipId) ?? 0) + 1);
    return m;
  }, [ham]);

  const suzulmus = useMemo(() => {
    const a = sekme === 'silinmis' ? '' : arama.trim().toLocaleLowerCase('tr-TR');

    const liste = ham.filter((e) => {
      if (tipId !== null && e.tipId !== tipId) return false;
      if (!a) return true;
      return [e.baslik, e.konum, e.durumAd, e.tipAd]
        .filter(Boolean)
        .some((m) => m!.toLocaleLowerCase('tr-TR').includes(a));
    });

    if (sekme === 'silinmis') return liste;

    return [...liste].sort((x, y) => (x.baslangic! < y.baslangic! ? -1 : 1));
  }, [ham, arama, tipId, sekme]);

  /*
    Liste/Silinmiş sekmelerinin boş durumu TEK YERDE: masaüstü ve mobil
    ağaçları ayrıldıktan sonra yalnızca `Liste`'nin içinde kalsaydı, mobilde
    boş liste hiçbir şey göstermezdi.
  */
  const listeBosDurumu = (
    <EmptyState
      ikon={sekme === 'silinmis' ? Trash2 : CalendarDays}
      baslik={
        arama || tipId !== null
          ? 'Eşleşen etkinlik yok'
          : sekme === 'silinmis'
            ? 'Silinmiş etkinlik yok'
            : 'Etkinlik yok'
      }
      aciklama={
        sekme === 'silinmis'
          ? 'Silinen etkinlikler burada listelenir.'
          : 'Önümüzdeki 6 ay için planlanmış bir etkinlik bulunmuyor.'
      }
    />
  );

  const sutunlar: Column<EventSummary>[] = [
    {
      anahtar: 'baslik',
      baslik: 'Etkinlik',
      hucre: (e) => (
        <span className="flex items-center gap-1.5">
          {e.gizli && <Lock size={11} className="shrink-0 text-text-3" aria-label="Gizli" />}
          <span className="line-clamp-1">{e.baslik}</span>
          {/* Başka birimin ajandasından gelen kayıt; kendi kaydımızla karışmasın. */}
          <InviteeBadge etkinlik={e} />
        </span>
      ),
      mobil: false,
    },
    { anahtar: 'tarih', baslik: 'Tarih', genislik: 'w-28', hucre: (e) => shortDate(e.baslangic) },
    {
      anahtar: 'saat',
      baslik: 'Saat',
      genislik: 'w-36',
      hucre: (e) => range(e.baslangic, e.bitis, e.tumGun ?? false),
    },
    {
      anahtar: 'durum',
      baslik: 'Durum',
      genislik: 'w-36',
      hucre: (e) => <ColoredBadge etiket={e.durumAd} renk={e.durumRenk} />,
    },
    {
      anahtar: 'tip',
      baslik: 'Tip',
      genislik: 'w-32',
      hucre: (e) => <ColoredBadge etiket={e.tipAd} renk={e.tipRenk} nokta={false} />,
      mobil: false,
    },
    {
      anahtar: 'konum',
      baslik: 'Konum',
      hucre: (e) => <span className="line-clamp-1">{e.konum || '—'}</span>,
    },
    {
      anahtar: 'isaretler',
      baslik: '',
      genislik: 'w-24',
      mobil: false,
      hucre: (e) => (
        <span className="flex items-center gap-1.5 text-text-3">
          {e.seriId && <Repeat size={12} aria-label="Tekrar eden" />}
          {e.resimVar && <Camera size={12} aria-label="Fotoğraf var" />}
        </span>
      ),
    },
  ];

  /**
   * Silinmiş sekmesinin sütunları AYRI.
   *
   * Ekranda yalnızca etkinliğin kendi tarihi görünüyordu ve liste "geçmiş
   * etkinlikler" gibi okunuyordu — bakan kişi kayıtların silinmiş olduğunu
   * ancak sekmenin adından çıkarabiliyordu. Artık ne zaman silindiği de
   * yazıyor ve o sütun BAŞA alındı: listenin sıralaması da ona göre.
   */
  const silinmisSutunlari: Column<EventSummary>[] = [
    {
      anahtar: 'silinme',
      baslik: 'Silinme',
      genislik: 'w-36',
      hucre: (e) =>
        e.silinmeTarihi ? (
          <span className="text-(--st-no)" title={relativeTime(e.silinmeTarihi)}>
            {shortDate(e.silinmeTarihi)}
          </span>
        ) : (
          '—'
        ),
    },
    ...sutunlar.filter((s) => s.anahtar !== 'isaretler'),
  ];

  /** Gezinilen pencerenin ilk gününe, mesai başına öneri. */
  function oneriHazirla(): BaslangicOnerisi {
    const t = new Date(haftaKaymasi === 0 ? new Date() : bas);
    if (haftaKaymasi === 0) {
      t.setMinutes(t.getMinutes() < 30 ? 30 : 60, 0, 0);
    } else {
      t.setHours(9, 0, 0, 0);
    }
    return dilimdenOneri(t);
  }

  return (
    // Mobilde dikey ritim SIKI: her boşluk listeden bir kayıt çalıyor.
    // Masaüstünde yer bol, eski ritim duruyor.
    <div className="relative space-y-2 md:space-y-3.5">
      <EventModal acik={ekleme !== null} oneri={ekleme} onKapat={() => setEkleme(null)} />

      {/*
        TAZELENME İPUCU — liste yerinde kalırken üstte ince bir çizgi akar.
        Tam ekran iskelet yerine bu: kullanıcı bir şeyin çalıştığını görüyor
        ama okuduğu içerik kaybolmuyor.
      */}
      {tazeleniyor && !yukleniyor && (
        <span
          aria-hidden
          className="pointer-events-none absolute inset-x-0 -top-1 z-20 h-[2px] overflow-hidden rounded-full bg-brand-soft"
        >
          <span className="block h-full w-1/3 rounded-full bg-brand" style={{ animation: 'seritKay 1.1s ease-in-out infinite' }} />
        </span>
      )}
      {/* ── Sekmeler ── */}
      <Tabs.Root value={sekme} onValueChange={(d) => { setSekme(d as TabItem); setSayfa(1); setTipId(null); }}>
        {/*
          Sekme şeridi MASAÜSTÜNE ÖZEL. Mobilde aynı seçim süzgeç tabakasında
          segment olarak duruyor: üst şeritte yalnızca arama ve yazıcı kaldı,
          böylece ilk ekranın tamamı listeye ait.
        */}
        <SekmeListesi etiket="Ajanda görünümleri" className="hidden md:flex">
          {[
            { d: 'program' as const, e: 'Program', i: <LayoutList size={14} /> },
            { d: 'liste' as const, e: 'Liste', i: <ListTree size={14} /> },
            { d: 'silinmis' as const, e: 'Silinmiş', i: <Trash2 size={14} /> },
          ].map((s) => (
            <SekmeTetigi key={s.d} deger={s.d}>
              {s.i}
              {s.e}
            </SekmeTetigi>
          ))}
        </SekmeListesi>
      </Tabs.Root>

      {/*
        ── Araç çubuğu ──

        Ekran önceden DÖRT sıra denetimle açılıyordu: sekmeler, arama+gezinme,
        çıktı düğmeleri ve tip çipleri. Asıl içerik — programın kendisi —
        mobilde ilk ekrana hiç girmiyordu. Artık iki sıra: sekmeler, sonra tek
        bir araç çubuğu. Çıktılar yazıcı düğmesinin altına, tip süzgeci de
        kendi menüsüne girdi.
      */}
      <div className="flex items-center gap-2 md:flex-row">
        {/*
          MOBİLDE ARAMA VE YAZICI TEK KONTROL.

          İkisi ayrı kenarlıklı kutulardı ve aralarındaki boşluk şeridi "iki
          ayrı şey" gibi gösteriyordu; oysa ikisi de aynı listeye ait — biri
          süzüyor, öteki süzülmüş hâlini basıyor. Ortak kenarlık ve aradaki
          saç teli onları tek bir araç yapıyor. Masaüstünde ikisi zaten uzak
          duruyor (yazıcı sağ uçta, öteki denetimlerle birlikte).
        */}
        {!masaustu && (
        <div className="flex h-ctrl-lg min-w-0 flex-1 items-stretch overflow-hidden rounded-control border border-line bg-surface">
          <span className="grid w-9 flex-none place-items-center text-ink-3">
            <Search size={15} />
          </span>
          <input
            type="search"
            value={aramaGirdisi}
            onChange={(e) => setAramaGirdisi(e.target.value)}
            placeholder="Ara"
            aria-label="Etkinliklerde ara"
            className="min-w-0 flex-1 bg-transparent pr-2 text-sm text-ink outline-hidden placeholder:text-ink-3"
          />
          <ProgramMenu
            tarih={basM.slice(0, 10)}
            excel={() =>
              download('/disa-aktar/etkinlik/excel', { ara: arama, tipId, baslangic: basM, bitis: bitM })
            }
            pdf={() =>
              download('/disa-aktar/etkinlik/pdf', { ara: arama, tipId, baslangic: basM, bitis: bitM })
            }
            tetikleyici={
              <button
                type="button"
                title="Çıktılar"
                aria-label="Çıktılar"
                className="grid w-11 flex-none place-items-center border-l border-line text-ink-2 active:bg-sunken"
              >
                <Printer size={17} strokeWidth={1.9} />
              </button>
            }
          />
        </div>
        )}

        {masaustu && (
          <SearchInput
            value={aramaGirdisi}
            onChange={(e) => setAramaGirdisi(e.target.value)}
            placeholder="Başlık, konum, durum veya tip ara"
            aria-label="Etkinliklerde ara"
            ikon={<Search size={15} />}
            className="md:max-w-[300px] md:flex-1"
          />
        )}

        <div className="hidden min-w-0 items-center gap-1.5 md:ml-auto md:flex">
          {sekme !== 'silinmis' && (
            <>
              {/*
                Gezinme LİSTE sekmesinde de var: iki sekme aynı pencereyi
                paylaşıyor, dolayısıyla pencereyi kaydırma yolu ikisinde de
                bulunmalı. Yalnızca programda olsaydı liste sekmesindeki
                kullanıcı sabit bir aralığa kilitlenirdi.

                Tarih aralığı ORTADA yazıyor. Önce sağda, yalnızca `lg`
                üstünde görünen bir metindi; mobilde kullanıcı ileri geri
                gidip hangi haftada olduğunu bilmiyordu.
              */}
              <div className="flex min-w-0 items-center rounded-control border border-border bg-surface shadow-1">
                <IconButton
                  etiket="Önceki iki hafta"
                  onClick={() => setHaftaKaymasi((k) => k - 1)}
                  className="h-[34px] w-[34px] shrink-0 rounded-sm border-0 bg-transparent hover:bg-surface-2"
                >
                  <ChevronLeft size={16} />
                </IconButton>
                <span className="min-w-0 flex-1 truncate px-1 text-center text-xs tabular-nums text-text-2">
                  {date(basM)} – {date(bitM)}
                </span>
                <IconButton
                  etiket="Sonraki iki hafta"
                  onClick={() => setHaftaKaymasi((k) => k + 1)}
                  className="h-[34px] w-[34px] shrink-0 rounded-sm border-0 bg-transparent hover:bg-surface-2"
                >
                  <ChevronRight size={16} />
                </IconButton>
              </div>

              {/* "Bugün" yalnızca bugünden UZAKTAYKEN çıkar; sürekli sönük
                  duran bir düğme yer kaplamaktan başka iş yapmıyordu. */}
              {haftaKaymasi !== 0 && (
                <Button
                  varyant="ikincil"
                  className="h-9 shrink-0 px-2.5 text-sm"
                  onClick={() => setHaftaKaymasi(0)}
                >
                  Bugün
                </Button>
              )}
            </>
          )}

          {sekme === 'silinmis' && (
            <SelectMenu
              deger={silinmisGun === 0 ? null : silinmisGun}
              degistir={(d) => {
                setSilinmisGun(d ?? 0);
                setSayfa(1);
              }}
              etiket="Dönem"
              tumuEtiketi="Tüm zamanlar"
              secenekler={[
                { deger: 7, etiket: 'Son 7 gün' },
                { deger: 30, etiket: 'Son 30 gün' },
                { deger: 90, etiket: 'Son 3 ay' },
                { deger: 365, etiket: 'Son 1 yıl' },
              ]}
            />
          )}

          {sekme !== 'silinmis' && (
            <>
              <SelectMenu
                deger={tipId}
                degistir={setTipId}
                etiket="Tip"
                tumuEtiketi="Tüm tipler"
                tumuSayisi={ham.length}
                secenekler={tipler.liste
                  .filter((t) => (sayimlar.get(t.id!) ?? 0) > 0)
                  .map((t) => ({
                    deger: t.id!,
                    etiket: t.ad ?? '',
                    sayi: sayimlar.get(t.id!) ?? 0,
                    renk: t.renk,
                  }))}
              />

              <ProgramMenu
                tarih={basM.slice(0, 10)}
                excel={() =>
                  download('/disa-aktar/etkinlik/excel', {
                    ara: arama, tipId, baslangic: basM, bitis: bitM,
                  })
                }
                pdf={() =>
                  download('/disa-aktar/etkinlik/pdf', {
                    ara: arama, tipId, baslangic: basM, bitis: bitM,
                  })
                }
              />
            </>
          )}
        </div>

        {hasPermission(PERMISSION.ajandaEkle) && (
          <Button className="hidden shrink-0 md:inline-flex" onClick={() => setEkleme(oneriHazirla())}>
            <Plus size={14} />
            Yeni etkinlik
          </Button>
        )}
      </div>

      {/*
        SAYIM + ARALIK SATIRI KALDIRILDI.

        "84 etkinlik · 15.08.2026 – 29.08.2026" bilgisi artık iki yerde zaten
        var: masaüstünde aralık gezgini şeritte yazıyor, mobilde süzgeç
        tabakasında; adet ise her gün ayracının sağında. Üstte üçüncü bir kez
        tekrar etmesi yalnızca listeden bir satır çalıyordu.
      */}

      {/*
        ── MOBİL: FAB ve süzgeç tabakası ──

        İki eylem: kayıt eklemek ve süzmek. İkisi de üst şeritte yer
        kaplıyordu; başparmağın doğal yeri zaten sağ alt köşe ve oraya
        taşındıklarında listeye bir ekran boyu yer açılıyor.
      */}
      <Fab
        etiket="Ajanda eylemleri"
        eylemler={[
          ...(hasPermission(PERMISSION.ajandaEkle)
            ? [{
                etiket: 'Yeni etkinlik',
                ikon: <Plus size={21} strokeWidth={2.2} />,
                onClick: () => setEkleme(oneriHazirla()),
              }]
            : []),
          {
            etiket: 'Ara ve süz',
            ikon: <SlidersHorizontal size={19} strokeWidth={2} />,
            onClick: () => setSuzgecAcik(true),
          },
        ]}
      />

      <FilterSheet
        acik={suzgecAcik}
        kapat={() => setSuzgecAcik(false)}
        etkinSayisi={(arama ? 1 : 0) + (tipId !== null ? 1 : 0) + (haftaKaymasi !== 0 ? 1 : 0)}
        temizle={() => {
          setAramaGirdisi('');
          setTipId(null);
          setHaftaKaymasi(0);
          setSayfa(1);
        }}
      >
        <FilterSection baslik="Görünüm">
          <Segment
            deger={sekme}
            degistir={(d) => {
              setSekme(d);
              setSayfa(1);
              setTipId(null);
            }}
            secenekler={[
              { deger: 'program' as TabItem, etiket: 'Program', ikon: <LayoutList size={14} /> },
              { deger: 'liste' as TabItem, etiket: 'Liste', ikon: <ListTree size={14} /> },
              { deger: 'silinmis' as TabItem, etiket: 'Silinmiş', ikon: <Trash2 size={14} /> },
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Ara">
          <SearchInput
            value={aramaGirdisi}
            onChange={(e) => setAramaGirdisi(e.target.value)}
            placeholder="Başlık, konum, durum veya tip"
            aria-label="Etkinliklerde ara"
            ikon={<Search size={15} />}
          />
        </FilterSection>

        {sekme !== 'silinmis' ? (
          <>
            <FilterSection baslik="Dönem">
              {/* Tarih penceresi tabakada da gezilebilir: kullanıcı süzgeci
                  açtığında "hangi aralığa bakıyorum" sorusunun cevabı ve
                  değiştirme yolu aynı yerde olmalı. */}
              {/*
                TEK PARÇA GEZGİN.

                Önce üç ayrı kutuydu: iki kenarlıklı düğme ve ortada başka bir
                zeminde duran tarih. Üçü de farklı yüzey, farklı köşe, aralarında
                boşluk — tek bir kontrol gibi okunmuyordu. Şimdi tek kenarlık,
                içeride saç teli ayırıcılar; tarih ile oklar aynı yüzeyde ve
                aynı yükseklikte.
              */}
              <div className="flex h-ctrl-lg items-stretch overflow-hidden rounded-sm border border-line bg-surface">
                <button
                  type="button"
                  aria-label="Önceki iki hafta"
                  onClick={() => setHaftaKaymasi((k) => k - 1)}
                  className="grid w-12 flex-none place-items-center border-r border-line text-ink-2 active:bg-sunken"
                >
                  <ChevronLeft size={18} strokeWidth={2} />
                </button>
                <span className="grid min-w-0 flex-1 place-items-center px-2 text-center text-xs font-medium tabular-nums text-ink">
                  {shortDate(basM)} – {shortDate(bitM)}
                </span>
                <button
                  type="button"
                  aria-label="Sonraki iki hafta"
                  onClick={() => setHaftaKaymasi((k) => k + 1)}
                  className="grid w-12 flex-none place-items-center border-l border-line text-ink-2 active:bg-sunken"
                >
                  <ChevronRight size={18} strokeWidth={2} />
                </button>
              </div>

              {/* Bugüne dönüş: gezgin uzağa gittiyse geri gelmenin tek dokunuşluk
                  yolu olmalı; ok ok basarak dönmek yoruyordu. */}
              {haftaKaymasi !== 0 && (
                <button
                  type="button"
                  onClick={() => setHaftaKaymasi(0)}
                  className="mt-2 h-ctrl w-full rounded-sm bg-sunken text-xs font-semibold text-ink-2 active:scale-[0.98]"
                >
                  Bugüne dön
                </button>
              )}
            </FilterSection>

            <FilterSection baslik="Etkinlik tipi">
              <FilterOptions
                deger={tipId}
                degistir={setTipId}
                secenekler={[
                  { deger: null as number | null, etiket: 'Tümü', sayi: ham.length },
                  ...tipler.liste
                    .filter((t) => (sayimlar.get(t.id!) ?? 0) > 0)
                    .map((t) => ({
                      deger: t.id! as number | null,
                      etiket: t.ad ?? '',
                      sayi: sayimlar.get(t.id!) ?? 0,
                      renk: t.renk,
                    })),
                ]}
              />
            </FilterSection>
          </>
        ) : (
          <FilterSection baslik="Silinme dönemi">
            <FilterOptions
              deger={silinmisGun}
              degistir={(d) => {
                setSilinmisGun(d);
                setSayfa(1);
              }}
              secenekler={[
                { deger: 0, etiket: 'Tüm zamanlar' },
                { deger: 7, etiket: 'Son 7 gün' },
                { deger: 30, etiket: 'Son 30 gün' },
                { deger: 90, etiket: 'Son 3 ay' },
                { deger: 365, etiket: 'Son 1 yıl' },
              ]}
            />
          </FilterSection>
        )}
      </FilterSheet>

      {/* ── İçerik ── */}
      {yukleniyor ? (
        <SkeletonRows adet={6} />
      ) : hata ? (
        <EmptyState
          ikon={CalendarDays}
          baslik="Etkinlikler yüklenemedi"
          aciklama={(hata as Error)?.message}
        />
      ) : sekme === 'program' ? (
        <ProgramView
          etkinlikler={suzulmus}
          bos={
            <EmptyState
              ikon={CalendarDays}
              baslik={arama || tipId !== null ? 'Eşleşen etkinlik yok' : 'Bu dönemde etkinlik yok'}
              aciklama="Başka bir tarih aralığına geçebilir ya da yeni etkinlik ekleyebilirsiniz."
              eylem={
                // Boş durumdaki EKLEME düğmesi de izin ister. Araç çubuğundaki
                // düğme kapıdan geçiyordu ama liste boşken çizilen bu ikinci
                // düğme kapının dışında kalmıştı: yetkisi olmayan kullanıcı boş
                // listede düğmeyi görüyor, basınca 403 alıyordu.
                hasPermission(PERMISSION.ajandaEkle) ? (
                  <Button onClick={() => setEkleme(oneriHazirla())}>
                    <Plus size={14} />
                    Yeni etkinlik
                  </Button>
                ) : undefined
              }
            />
          }
        />
      ) : (
        <>
          {/*
            ── MOBİLDE GERÇEK LİSTE ──

            Liste ve Silinmiş sekmeleri telefonda KART çiziyordu; yani
            Program'dan farkları kalmıyor, "liste" sekmesi listeye
            benzemiyordu. Üstelik kart, tablo verisini etiket/değer
            ızgarasına açtığı için satır başına dört-beş satır yüksekliğe
            çıkıyor ve ekrana üç kayıt sığıyordu.

            Yerel gramer: tek yüzey, saç teli ayırıcılar, solda durum rengini
            taşıyan çip, sağda chevron. Satır başına bir kayıt — aynı ekranda
            üç kat daha fazla veri.
          */}
          {/* `Liste` boş durumu kendi çiziyor; burada boşsa hiç kap açma,
              yoksa aynı ekranda iki "kayıt yok" görünürdü. */}
          {!masaustu && suzulmus.length === 0 && listeBosDurumu}
          {!masaustu && suzulmus.length > 0 && (
            <InsetGroup>
              {suzulmus.map((e, i) => (
                <ListRow
                  key={e.id}
                  sira={i}
                  sonuncu={i === suzulmus.length - 1}
                  yol={`/ajanda/${e.id}`}
                  ikon={
                    sekme === 'silinmis' ? (
                      <Trash2 size={15} strokeWidth={1.9} />
                    ) : (
                      <CalendarDays size={15} strokeWidth={1.9} />
                    )
                  }
                  ikonRengi={sekme === 'silinmis' ? 'var(--st-no)' : (e.durumRenk ?? 'var(--brand-ui)')}
                  ust={
                    <>
                      <span className="font-medium tabular-nums text-ink-2">
                        {shortDate(e.baslangic)}
                      </span>
                      {!e.tumGun && <span>{range(e.baslangic, e.bitis, false)}</span>}
                      {e.gizli && <Lock size={10} strokeWidth={2.2} />}
                      {e.seriId && <Repeat size={10} strokeWidth={2.2} />}
                    </>
                  }
                  baslik={
                    <span className="flex flex-wrap items-center gap-1.5">
                      {e.baslik}
                      <InviteeBadge etkinlik={e} />
                    </span>
                  }
                  alt={
                    sekme === 'silinmis' && e.silinmeTarihi ? (
                      <span>{shortDate(e.silinmeTarihi)} tarihinde silindi</span>
                    ) : (
                      <>
                        {e.konum && <span className="truncate">{e.konum}</span>}
                        {e.tipAd && (
                          <span className="truncate text-ink-3">
                            {e.konum ? '· ' : ''}
                            {e.tipAd}
                          </span>
                        )}
                      </>
                    )
                  }
                  sag={
                    e.durumAd ? (
                      <span className="mt-2.5 shrink-0">
                        <ColoredBadge etiket={e.durumAd} renk={e.durumRenk} />
                      </span>
                    ) : undefined
                  }
                />
              ))}
            </InsetGroup>
          )}

          {masaustu && (
          <DataList
            satirlar={suzulmus}
            sutunlar={sekme === 'silinmis' ? silinmisSutunlari : sutunlar}
            anahtar={(e) => e.id!}
            // AJANDA KART KALIR: satır bir etkinliğin kartı — renk şeridi,
            // saat bloğu ve hazırlık rozetleriyle birlikte anlam taşıyor.
            // Öteki listeler telefonda sıkı liste görünümünde.
            mobilGorunum="kart"
            bagla={(e) => `/ajanda/${e.id}`}
            mobilBaslik={(e) => (
              <span className="flex flex-wrap items-center gap-1.5">
                {e.gizli && <Lock size={11} className="shrink-0 text-text-3" />}
                {e.baslik}
                <InviteeBadge etkinlik={e} />
              </span>
            )}
            mobilAciklama={(e) =>
              sekme === 'silinmis' && e.silinmeTarihi
                ? `${shortDate(e.baslangic)} · ${shortDate(e.silinmeTarihi)} tarihinde silindi`
                : range(e.baslangic, e.bitis, e.tumGun ?? false)
            }
            mobilRozet={(e) => <ColoredBadge etiket={e.durumAd} renk={e.durumRenk} />}
            bos={listeBosDurumu}
          />
          )}

          {sekme === 'silinmis' && (
            <Pagination
              sonuc={silinmis.data}
              sayfaDegistir={setSayfa}
              birim="etkinlik"
              className="mt-3"
            />
          )}
        </>
      )}
    </div>
  );
}
