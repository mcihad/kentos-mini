import {
  AlertTriangle, Calendar, FolderKanban, Plus, Search, SlidersHorizontal, Users,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { ColoredBadge } from '../components/Color';
import { EmptyState } from '../components/EmptyState';
import { SearchInput } from '../components/Field';
import { ChipStrip, FilterChip, SegmentedSelect } from '../components/Filters';
import { Segment, FilterSection, FilterSheet } from '../components/FilterSheet';
import { useIsDesktop } from '../components/screenSize';
import { Fab } from '../shell/mobile/Fab';
import { Pagination } from '../components/Pagination';
import { SkeletonRows } from '../components/Skeleton';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { shortDate } from '../data/format';
import { useProjects } from '../data/projects';
import { PROJECT_STATUS_LABELS, type ProjectSummary } from '../data/types';
import { UnitScopePicker } from './task/UnitScopePicker';

type Kapsam = 'kendi' | 'alt';

/** Süzgeç çipleri — açık projeler önce. */
const DURUM_SIRASI = [0, 1, 2, 3, 4];

/**
 * PROJELER — görevlerin çatısı.
 *
 * <p>
 * Kart düzeni, satır düzeni değil: bir projenin okunması için aynı anda beş
 * sayı gerekiyor (ilerleme, geciken iş, kilometre taşı, ekip, teslim tarihi)
 * ve bunlar tabloda ya sıkışıyor ya da satır yüksekliğini iki katına
 * çıkarıyordu.
 * </p>
 */
export default function Projects() {
  const { hasPermission } = useSession();
  const gezin = useNavigate();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [durum, setDurum] = useState<number | null>(null);
  const [kapsam, setKapsam] = useState<Kapsam>('kendi');
  const [sayfa, setSayfa] = useState(1);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const { data, isLoading, isError, error, isPlaceholderData } = useProjects({
    sayfa,
    boyut: 24,
    ara: arama,
    durumlar: durum === null ? undefined : [durum],
    altBirimlerDahil: kapsam === 'alt',
    sirala: 'bitis',
  });

  const projeler = data?.veriler ?? [];
  const masaustu = useIsDesktop();
  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const suzuluyor = arama !== '' || durum !== null;

  return (
    <div className="space-y-3.5">
      {/*
        ── Araç çubuğu ──

        Dört denetim TEK satırda sarmalanıyordu ve arama kutusu `flex-1` ile
        artakalanı alıyordu: 390px'lik bir ekranda birim seçici 231px, kapsam
        segmenti 179px, "Proje aç" 107px yer kaplayınca aramaya <b>119px</b>
        kalıyordu — ölçüldü. İçine iki kelime sığmayan bir arama kutusu.

        Çözüm ekranın kendi gramerinde zaten vardı: <code>Tasks.tsx</code>
        telefonda üst şeritte YALNIZCA aramayı bırakıyor, gerisini FAB'dan
        açılan süzgeç tabakasına alıyor. Projeler ve ekipler o düzenlemeyi
        hiç almamış.
      */}
      <div className="flex items-center gap-2">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder={masaustu ? 'Proje adı veya kodu ara' : 'Ara'}
          aria-label="Projelerde ara"
          ikon={<Search size={15} />}
          className="min-w-0 flex-1 md:max-w-[320px]"
        />

        <div className="hidden min-w-0 flex-wrap items-center gap-2 md:ml-auto md:flex md:flex-nowrap">
          <UnitScopePicker />

          <SegmentedSelect<Kapsam>
            deger={kapsam}
            degistir={(d) => {
              setKapsam(d);
              setSayfa(1);
            }}
            etiket="Kapsam"
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              { deger: 'alt', etiket: 'Alt birimler' },
            ]}
          />

          {hasPermission(PERMISSION.projeYonet) && (
            <Button onClick={() => gezin('/projeler/yeni')}>
              <Plus size={14} />
              Proje aç
            </Button>
          )}
        </div>
      </div>

      <ChipStrip>
        <FilterChip
          secili={durum === null}
          tikla={() => {
            setDurum(null);
            setSayfa(1);
          }}
        >
          Tümü
        </FilterChip>
        {DURUM_SIRASI.map((d) => (
          <FilterChip
            key={d}
            secili={durum === d}
            tikla={() => {
              setDurum(durum === d ? null : d);
              setSayfa(1);
            }}
          >
            {PROJECT_STATUS_LABELS[d]}
          </FilterChip>
        ))}
      </ChipStrip>

      {/* ── Mobil: FAB ve süzgeç tabakası ── */}
      <Fab
        etiket="Proje eylemleri"
        eylemler={[
          ...(hasPermission(PERMISSION.projeYonet)
            ? [{
                etiket: 'Proje aç',
                ikon: <Plus size={21} strokeWidth={2.2} />,
                onClick: () => gezin('/projeler/yeni'),
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
        etkinSayisi={(arama ? 1 : 0) + (durum !== null ? 1 : 0) + (kapsam !== 'kendi' ? 1 : 0)}
        temizle={() => {
          setAramaGirdisi('');
          setDurum(null);
          setKapsam('kendi');
          setSayfa(1);
        }}
      >
        <FilterSection baslik="Birim">
          <UnitScopePicker className="w-full" />
          <Segment<Kapsam>
            deger={kapsam}
            degistir={(d) => {
              setKapsam(d);
              setSayfa(1);
            }}
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              { deger: 'alt', etiket: 'Alt birimler' },
            ]}
          />
        </FilterSection>
      </FilterSheet>

      {isLoading ? (
        <SkeletonRows adet={4} />
      ) : isError ? (
        <EmptyState
          ikon={FolderKanban}
          baslik="Projeler yüklenemedi"
          aciklama={(error as Error)?.message}
        />
      ) : projeler.length === 0 ? (
        <EmptyState
          ikon={FolderKanban}
          baslik={suzuluyor ? 'Eşleşen proje yok' : 'Proje yok'}
          aciklama={
            suzuluyor
              ? 'Süzgeçleri temizleyerek tüm projeleri görebilirsiniz.'
              : 'Biriminizde tanımlı bir proje bulunmuyor.'
          }
          eylem={
            !suzuluyor && hasPermission(PERMISSION.projeYonet) ? (
              <Link to="/projeler/yeni">
                <Button>
                  <Plus size={14} />
                  Proje aç
                </Button>
              </Link>
            ) : undefined
          }
        />
      ) : (
        <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
          <div className="grid gap-2.5 md:grid-cols-2 xl:grid-cols-3">
            {projeler.map((p) => (
              <ProjeKarti key={p.id} proje={p} />
            ))}
          </div>

          <Pagination sonuc={data} sayfaDegistir={setSayfa} birim="proje" className="mt-3" />
        </div>
      )}
    </div>
  );
}

function ProjeKarti({ proje: p }: { proje: ProjectSummary }) {
  const toplam = p.gorevToplam ?? 0;
  const biten = p.gorevBiten ?? 0;

  /*
    YÜZDE SUNUCUDAN, "biten/toplam" ORANINDAN DEĞİL.

    Eskiden `biten / toplam` çiziliyordu: bir görev ONAYLANANA kadar sıfır
    sayılıyordu ve beş aşamalı işin dördünü bitiren ekip çubukta hiçbir
    hareket görmüyordu. Sunucu artık bağlı görevlerin ilerleme ortalamasını
    veriyor; kural `GorevDurumAkisi.Ilerleme` içinde tek yerde.
  */
  const oran = p.ilerleme ?? 0;

  return (
    <Link to={`/projeler/${p.id}`} className="block">
      <Card className="h-full p-3.5 transition-colors hover:border-brand">
        <div className="flex items-start gap-2">
          <span
            className="mt-1 h-2.5 w-2.5 flex-none rounded-full"
            style={{ background: p.renk ?? p.durumRenk ?? 'var(--brand-ui)' }}
            aria-hidden
          />
          <div className="min-w-0 flex-1">
            <h3 className="line-clamp-1 font-display text-sm font-semibold text-ink">{p.ad}</h3>
            <p className="mt-0.5 text-2xs text-ink-3">
              {p.kod && <span className="tabular-nums">{p.kod} · </span>}
              {p.birimAd}
            </p>
          </div>
          <ColoredBadge etiket={p.durumAd} renk={p.durumRenk} />
        </div>

        {/* İLERLEME görevlerden geliyor; projede saklanan bir yüzde yok. */}
        <div className="mt-3">
          <div className="flex items-baseline justify-between text-2xs text-ink-3">
            <span>
              {toplam === 0 ? 'Görev yok' : `${biten}/${toplam} görev kapandı`}
            </span>
            {toplam > 0 && <span className="tabular-nums">%{oran}</span>}
          </div>
          <span className="mt-1 block h-1.5 overflow-hidden rounded-full bg-sunken" aria-hidden>
            <span
              className="block h-full rounded-full bg-brand transition-[width]"
              style={{ width: `${oran}%` }}
            />
          </span>
        </div>

        <div className="mt-2.5 flex flex-wrap items-center gap-x-3 gap-y-1 text-2xs text-ink-3">
          {p.bitis && (
            <span className={`inline-flex items-center gap-1 ${p.gecikti ? 'text-(--st-no)' : ''}`}>
              <Calendar size={12} />
              {shortDate(p.bitis)}
              {p.gecikti && ' · gecikti'}
            </span>
          )}
          {(p.uyeSayisi ?? 0) > 0 && (
            <span className="inline-flex items-center gap-1">
              <Users size={12} />
              {p.uyeSayisi}
            </span>
          )}
          {(p.kilometreTasiToplam ?? 0) > 0 && (
            <span>
              {p.kilometreTasiBiten}/{p.kilometreTasiToplam} kilometre taşı
            </span>
          )}
          {/* GECİKEN İŞ projenin risk göstergesi — ayrı ve kırmızı. */}
          {(p.gorevGeciken ?? 0) > 0 && (
            <span className="inline-flex items-center gap-1 font-medium text-(--st-no)">
              <AlertTriangle size={12} />
              {p.gorevGeciken} geciken iş
            </span>
          )}
        </div>

        {p.yoneticiAd && (
          <p className="mt-2 truncate text-2xs text-text-3">Yönetici: {p.yoneticiAd}</p>
        )}
      </Card>
    </Link>
  );
}
