import {
  AlertTriangle, ArrowDownWideNarrow, ArrowUpNarrowWide, ClipboardCheck,
  Plus, Search, SlidersHorizontal,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { SearchInput } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { SkeletonRows } from '../components/Skeleton';
import { DataList, type Column } from '../components/DataList';
import { ColoredBadge } from '../components/Color';
import { Pagination } from '../components/Pagination';
import { SelectMenu } from '../components/SelectMenu';
import { useIsDesktop } from '../components/screenSize';
import { InsetGroup, ListRow } from '../components/ListRow';
import { SegmentedSelect, ChipStrip, FilterChip } from '../components/Filters';
import { Segment, FilterSection, FilterOptions, FilterSheet } from '../components/FilterSheet';
import { Fab } from '../shell/mobile/Fab';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { shortDate } from '../data/format';
import { useTasks, useUsableTaskTypes } from '../data/tasks';
import { TASK_PRIORITY_LABELS, TASK_STATUS_LABELS, type TaskSummary } from '../data/types';
import { UnitScopePicker } from './task/UnitScopePicker';
import { SlaBadge, StageProgress } from './task/TaskBits';

type Kapsam = 'kendi' | 'alt';

/** Süzgeç çipleri — açık işler önce, kapananlar sonda. */
const DURUM_SIRASI = [0, 1, 2, 3, 4, 5, 7, 8, 6, 9];

/**
 * GÖREVLER — birimin iş listesi.
 *
 * <p>
 * Süzme ve sayfalama SUNUCUDA; gecikme hesabı da öyle. İstemcinin saati
 * yanlışsa gecikme rozetleri de yanlış olurdu ve bu, ölçümün kendisini
 * anlamsız kılar.
 * </p>
 *
 * <p>
 * Liste varsayılan olarak <b>yalnızca kök görevleri</b> gösteriyor: alt
 * görevler ağacın parçası ve üst görevin detayında açılıyor. Düz listede
 * ikisi yan yana dursaydı aynı iş iki kez sayılmış gibi görünürdü.
 * </p>
 */
export default function Tasks() {
  const { hasPermission } = useSession();
  const gezin = useNavigate();
  const masaustu = useIsDesktop();

  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [kapsam, setKapsam] = useState<Kapsam>('kendi');
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [durum, setDurum] = useState<number | null>(null);
  const [oncelik, setOncelik] = useState<number | null>(null);
  const [tipId, setTipId] = useState<number | null>(null);
  const [yalnizGeciken, setYalnizGeciken] = useState(false);
  const [sirala, setSirala] = useState<'tarih' | 'sla'>('tarih');
  const [azalan, setAzalan] = useState(true);
  const [sayfa, setSayfa] = useState(1);
  const [boyut, setBoyut] = useState(50);

  // Arama geciktirmesi: "asfalt" yazmak 6 istek yerine 1 istek üretir.
  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const tipler = useUsableTaskTypes();

  const { data, isLoading, isError, error, isPlaceholderData } = useTasks({
    sayfa,
    boyut,
    ara: arama,
    altBirimlerDahil: kapsam === 'alt',
    durumlar: durum === null ? undefined : [durum],
    oncelikler: oncelik === null ? undefined : [oncelik],
    gorevTipiId: tipId,
    yalnizGeciken,
    yalnizKok: true,
    sirala,
    azalan,
  });

  const satirlar = data?.veriler ?? [];

  const suzgecDegisti = (f: () => void) => {
    f();
    setSayfa(1);
  };

  const sutunlar: Column<TaskSummary>[] = [
    {
      anahtar: 'takipNo',
      baslik: 'Takip no',
      genislik: 'w-36',
      hucre: (g) => <span className="tabular-nums text-ink-2">{g.takipNo}</span>,
      mobil: false,
    },
    {
      anahtar: 'baslik',
      baslik: 'Görev',
      hucre: (g) => (
        <span className="line-clamp-1">
          {g.baslik}
          {(g.altGorevSayisi ?? 0) > 0 && (
            <span className="ml-1.5 text-2xs text-ink-3">+{g.altGorevSayisi} alt</span>
          )}
        </span>
      ),
    },
    {
      anahtar: 'tip',
      baslik: 'Tip',
      genislik: 'w-40',
      hucre: (g) => g.gorevTipiAd || '—',
      mobil: false,
    },
    {
      anahtar: 'sorumlu',
      baslik: 'Sorumlu',
      genislik: 'w-44',
      hucre: (g) =>
        (g.sorumlular ?? []).length > 0 ? (
          <span className="line-clamp-1">{(g.sorumlular ?? []).join(', ')}</span>
        ) : (
          <span className="text-ink-3">Atanmadı</span>
        ),
      mobil: false,
    },
    {
      anahtar: 'asama',
      baslik: 'Aşama',
      genislik: 'w-28',
      hucre: (g) => <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} />,
      mobil: false,
    },
    {
      anahtar: 'sla',
      baslik: 'Süre',
      genislik: 'w-32',
      hucre: (g) => <SlaBadge gecikti={!!g.gecikti} kalanSaat={g.kalanSaat} />,
    },
    {
      anahtar: 'durum',
      baslik: 'Durum',
      genislik: 'w-36',
      hucre: (g) => <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />,
    },
  ];

  const suzuluyor =
    arama !== '' || durum !== null || oncelik !== null || tipId !== null || yalnizGeciken;

  const bosDurum = (
    <EmptyState
      ikon={ClipboardCheck}
      baslik={suzuluyor ? 'Eşleşen görev yok' : 'Görev yok'}
      aciklama={
        suzuluyor
          ? 'Süzgeçleri temizleyerek tüm görevleri görebilirsiniz.'
          : 'Biriminizde açık bir görev bulunmuyor.'
      }
      eylem={
        !suzuluyor && hasPermission(PERMISSION.gorevEkle) ? (
          <Link to="/gorevler/yeni">
            <Button>
              <Plus size={14} />
              Görev aç
            </Button>
          </Link>
        ) : undefined
      }
    />
  );

  return (
    <div className="space-y-3.5">
      {/* ── Araç çubuğu ── */}
      <div className="flex items-center gap-2">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder={masaustu ? 'Başlık, takip numarası veya adres ara' : 'Ara'}
          aria-label="Görevlerde ara"
          ikon={<Search size={15} />}
          className="min-w-0 flex-1 md:max-w-[320px]"
        />

        <div className="hidden min-w-0 flex-wrap items-center gap-2 md:ml-auto md:flex md:flex-nowrap">
          <UnitScopePicker />

          {tipler.liste.length > 0 && (
            <SelectMenu
              deger={tipId}
              degistir={(d) => suzgecDegisti(() => setTipId(d))}
              etiket="Tip"
              className="min-w-0"
              tumuEtiketi="Tüm tipler"
              secenekler={tipler.liste.map((t) => ({ deger: t.id!, etiket: t.ad ?? '' }))}
            />
          )}

          <SelectMenu
            deger={oncelik}
            degistir={(d) => suzgecDegisti(() => setOncelik(d))}
            etiket="Öncelik"
            className="min-w-0"
            tumuEtiketi="Tüm öncelikler"
            secenekler={Object.entries(TASK_PRIORITY_LABELS).map(([d, e]) => ({
              deger: Number(d),
              etiket: e,
            }))}
          />

          {/*
            GECİKENLER tek düğmede ve AÇIK/KAPALI.
            Üç durumlu bir seçim ("farketmez / geciken / gecikmeyen") sunmak,
            gerçekte hiç kullanılmayan üçüncü bir hâl uydurmak olurdu:
            gecikmeyenleri ayrıca listelemek isteyen yok.
          */}
          <Button
            varyant={yalnizGeciken ? 'yikici' : 'ikincil'}
            className="h-9 shrink-0 px-2.5"
            onClick={() => suzgecDegisti(() => setYalnizGeciken((g) => !g))}
            aria-pressed={yalnizGeciken}
            title="Süresi aşılmış görevler"
          >
            <AlertTriangle size={15} />
            <span className="hidden text-sm lg:inline">Gecikenler</span>
          </Button>

          <Button
            varyant="ikincil"
            className="h-9 shrink-0 px-2.5"
            onClick={() =>
              suzgecDegisti(() => {
                // Süreye göre sıralamada VARSAYILAN artan: en az vakti kalan
                // iş en üstte olmalı. Tarihte tersi geçerli — en yeni önce.
                if (sirala === 'tarih') {
                  setSirala('sla');
                  setAzalan(false);
                } else {
                  setSirala('tarih');
                  setAzalan(true);
                }
              })
            }
            title={sirala === 'sla' ? 'Süreye göre sıralı' : 'Tarihe göre sıralı'}
          >
            {azalan ? <ArrowDownWideNarrow size={15} /> : <ArrowUpNarrowWide size={15} />}
            <span className="hidden text-sm lg:inline">
              {sirala === 'sla' ? 'Süre' : 'Tarih'}
            </span>
          </Button>

          <SegmentedSelect<Kapsam>
            deger={kapsam}
            degistir={(d) => suzgecDegisti(() => setKapsam(d))}
            etiket="Kapsam"
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              { deger: 'alt', etiket: 'Alt birimler' },
            ]}
            className="w-full justify-center md:w-auto md:flex-none"
          />
        </div>

        {hasPermission(PERMISSION.gorevEkle) && (
          <Link to="/gorevler/yeni" className="hidden md:block md:shrink-0">
            <Button>
              <Plus size={14} />
              Görev aç
            </Button>
          </Link>
        )}
      </div>

      {/* ── Durum çipleri (masaüstü) ── */}
      <ChipStrip className="hidden md:flex">
        <FilterChip secili={durum === null} tikla={() => suzgecDegisti(() => setDurum(null))}>
          Tümü
        </FilterChip>
        {DURUM_SIRASI.map((d) => (
          <FilterChip
            key={d}
            secili={durum === d}
            tikla={() => suzgecDegisti(() => setDurum(durum === d ? null : d))}
          >
            {TASK_STATUS_LABELS[d]}
          </FilterChip>
        ))}
      </ChipStrip>

      {/* ── Mobil: FAB ve süzgeç tabakası ── */}
      <Fab
        etiket="Görev eylemleri"
        eylemler={[
          ...(hasPermission(PERMISSION.gorevEkle)
            ? [{
                etiket: 'Görev aç',
                ikon: <Plus size={21} strokeWidth={2.2} />,
                onClick: () => gezin('/gorevler/yeni'),
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
        etkinSayisi={
          (arama ? 1 : 0) + (durum !== null ? 1 : 0) + (oncelik !== null ? 1 : 0) +
          (tipId !== null ? 1 : 0) + (yalnizGeciken ? 1 : 0) + (kapsam !== 'kendi' ? 1 : 0)
        }
        temizle={() =>
          suzgecDegisti(() => {
            setAramaGirdisi('');
            setDurum(null);
            setOncelik(null);
            setTipId(null);
            setYalnizGeciken(false);
            setKapsam('kendi');
          })
        }
      >
        <FilterSection baslik="Birim">
          <UnitScopePicker className="w-full" />
          <Segment
            deger={kapsam}
            degistir={(d) => suzgecDegisti(() => setKapsam(d))}
            secenekler={[
              { deger: 'kendi' as Kapsam, etiket: 'Birimim' },
              { deger: 'alt' as Kapsam, etiket: 'Alt birimler' },
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Ara">
          <SearchInput
            value={aramaGirdisi}
            onChange={(e) => setAramaGirdisi(e.target.value)}
            placeholder="Başlık, takip numarası veya adres"
            aria-label="Görevlerde ara"
            ikon={<Search size={15} />}
          />
        </FilterSection>

        <FilterSection baslik="Durum">
          <FilterOptions
            deger={durum}
            degistir={(d) => suzgecDegisti(() => setDurum(d))}
            secenekler={[
              { deger: null as number | null, etiket: 'Tümü' },
              ...DURUM_SIRASI.map((d) => ({
                deger: d as number | null,
                etiket: TASK_STATUS_LABELS[d],
              })),
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Öncelik">
          <FilterOptions
            deger={oncelik}
            degistir={(d) => suzgecDegisti(() => setOncelik(d))}
            secenekler={[
              { deger: null as number | null, etiket: 'Farketmez' },
              ...Object.entries(TASK_PRIORITY_LABELS).map(([d, e]) => ({
                deger: Number(d) as number | null,
                etiket: e,
              })),
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Süre">
          <Segment
            deger={yalnizGeciken ? 'geciken' : 'hepsi'}
            degistir={(d) => suzgecDegisti(() => setYalnizGeciken(d === 'geciken'))}
            secenekler={[
              { deger: 'hepsi', etiket: 'Tümü' },
              { deger: 'geciken', etiket: 'Yalnızca gecikenler' },
            ]}
          />
        </FilterSection>
      </FilterSheet>

      {/* ── Liste ── */}
      {isLoading ? (
        <SkeletonRows adet={6} />
      ) : isError ? (
        <EmptyState
          ikon={ClipboardCheck}
          baslik="Görevler yüklenemedi"
          aciklama={(error as Error)?.message}
        />
      ) : (
        <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
          {!masaustu && satirlar.length === 0 && bosDurum}
          {!masaustu && satirlar.length > 0 && (
            <InsetGroup>
              {satirlar.map((g, i) => (
                <ListRow
                  key={g.id}
                  sira={i}
                  sonuncu={i === satirlar.length - 1}
                  yol={`/gorevler/${g.id}`}
                  ikon={<ClipboardCheck size={15} strokeWidth={1.9} />}
                  ikonRengi={g.durumRenk ?? 'var(--brand-ui)'}
                  ust={
                    <>
                      <span className="font-medium tabular-nums text-ink-2">{g.takipNo}</span>
                      {g.gorevTipiAd && <span className="truncate">· {g.gorevTipiAd}</span>}
                    </>
                  }
                  baslik={g.baslik}
                  alt={
                    <>
                      <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} />
                      {(g.sorumlular ?? []).length > 0 && (
                        <span className="truncate">· {(g.sorumlular ?? [])[0]}</span>
                      )}
                      <SlaBadge gecikti={!!g.gecikti} kalanSaat={g.kalanSaat} kisa />
                    </>
                  }
                  sag={
                    <span className="mt-2.5 shrink-0">
                      <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />
                    </span>
                  }
                />
              ))}
            </InsetGroup>
          )}

          {masaustu && (
            <DataList
              satirlar={satirlar}
              sutunlar={sutunlar}
              anahtar={(g) => g.id!}
              bagla={(g) => `/gorevler/${g.id}`}
              mobilBaslik={(g) => g.baslik ?? ''}
              mobilAciklama={(g) => g.takipNo}
              mobilRozet={(g) => <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />}
              bos={bosDurum}
            />
          )}

          <Pagination
            sonuc={data}
            sayfaDegistir={setSayfa}
            boyutDegistir={(b) => suzgecDegisti(() => setBoyut(b))}
            birim="görev"
            className="mt-3"
          />
        </div>
      )}
    </div>
  );
}

/** Tarih hücresi — listede kullanılmıyor ama detay kartları paylaşıyor. */
export function taskDate(t?: string | null) {
  return t ? shortDate(t) : '—';
}
