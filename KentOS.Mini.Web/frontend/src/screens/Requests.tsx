import {
  ArrowDownWideNarrow, ArrowUpNarrowWide, ClipboardList, Plus, Printer, Search,
  SlidersHorizontal,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { SearchInput } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { ExportButtons } from '../components/ExportButtons';
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
import { relativeTime, shortDate } from '../data/format';
import { download } from '../data/download';
import { useRequestStatusCounts, useRequests } from '../data/hooks';
import type { RequestSummary } from '../data/types';

type RecurrenceScope = 'kendi' | 'alt';

/**
 * Talepler — design.md §6.
 *
 * <p>
 * Süzme ve sayfalama SUNUCUDA. Önceden liste tek seferde çekilip istemcide
 * süzülüyordu; iki yıllık veride bu binlerce satırı tarayıcıya yüklemek
 * demek. Arama 300 ms geciktirilir ki her tuş vuruşu istek üretmesin.
 * </p>
 */
export default function Requests() {
  // Düğmeler İZNE göre gizlenir; çalışmayan düğme göstermek kullanıcıyı
  // hata almaya davet etmek demek.
  const { hasPermission } = useSession();
  const gezin = useNavigate();
  /*
    AYNI KONTROL İKİ KEZ ÇİZİLMESİN.

    Mobil ve masaüstü sürümleri `md:hidden` / `hidden md:block` ile birlikte
    DOM'a giriyordu: aynı `aria-label`'a sahip İKİ arama alanı, iki liste
    ağacı. Ekran okuyucu ikisini de duyuruyor, testler "birden çok eşleşme"
    diyor ve gizli ağaç da boşuna render ediliyordu. Ölçüyü JavaScript'ten
    okuyup TEK tarafı çiziyoruz.
  */
  const masaustu = useIsDesktop();

  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [kapsam, setKapsam] = useState<RecurrenceScope>('kendi');
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [durumId, setDurumId] = useState<number | null>(null);
  const [sayfa, setSayfa] = useState(1);
  const [boyut, setBoyut] = useState(50);

  /**
   * Ajandaya eklenme süzgeci.
   *
   * Asıl işe yarayan bileşim "Onaylandı + eklenmemiş": onaylanmış ama henüz
   * takvime girmemiş talepler, yani sırada bekleyen iş. Ayrı bir çip yerine
   * kendi süzgeci çünkü her durumla birleşebiliyor.
   */
  const [ajandada, setAjandada] = useState<boolean | null>(null);

  /**
   * Tarih sıralaması. Varsayılan EN YENİ önce — talepler kronolojik okunuyor
   * ve kullanıcı önce bugün geleni arıyor.
   */
  const [azalan, setAzalan] = useState(true);

  // Arama geciktirmesi: "asfalt" yazmak 6 istek yerine 1 istek üretir.
  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const suzgec = {
    sayfa,
    boyut,
    ara: arama,
    altBirimlerDahil: kapsam === 'alt',
    durumId,
    ajandayaEklendi: ajandada,
    sirala: 'tarih',
    azalan,
  };

  const { data, isLoading, isError, error, isPlaceholderData } = useRequests(suzgec);
  const sayaclar = useRequestStatusCounts(kapsam === 'alt', ajandada);

  const satirlar = data?.veriler ?? [];
  const toplamSayac = (sayaclar.data ?? []).reduce((t, s) => t + (s.adet ?? 0), 0);

  /** Süzgeç değişince ilk sayfaya dön — 7. sayfada boş liste görmemek için. */
  const suzgecDegisti = (f: () => void) => {
    f();
    setSayfa(1);
  };

  const sutunlar: Column<RequestSummary>[] = [
    {
      anahtar: 'konu',
      baslik: 'Konu',
      hucre: (t) => <span className="line-clamp-1">{t.konu}</span>,
      mobil: false,
    },
    { anahtar: 'kisi', baslik: 'Talep eden', hucre: (t) => t.adSoyad || '—', mobil: false },
    { anahtar: 'meslek', baslik: 'Meslek', hucre: (t) => t.meslek || '—' },
    {
      anahtar: 'tarih',
      baslik: 'Tarih',
      genislik: 'w-32',
      hucre: (t) => <span title={relativeTime(t.baslangicTarih)}>{shortDate(t.baslangicTarih)}</span>,
    },
    {
      anahtar: 'durum',
      baslik: 'Durum',
      genislik: 'w-36',
      hucre: (t) => <ColoredBadge etiket={t.durumAd} renk={t.durumRenk} />,
      mobil: false,
    },
    {
      anahtar: 'tip',
      baslik: 'Tip',
      genislik: 'w-32',
      hucre: (t) => <ColoredBadge etiket={t.tipAd} renk={t.tipRenk} nokta={false} />,
    },
    {
      anahtar: 'ajanda',
      baslik: 'Ajandada',
      genislik: 'w-24',
      hucre: (t) =>
        t.ajandayaEklendi ? (
          <span className="text-(--st-ok)">Eklendi</span>
        ) : (
          <span className="text-text-3">Hayır</span>
        ),
    },
  ];

  /*
    BOŞ DURUM TEK YERDE.

    Önce yalnızca `Liste`'nin `bos` özelliğindeydi; masaüstü/mobil ağaçları
    ayrılınca mobilde liste boşken EKRANDA HİÇBİR ŞEY kalmadı — kullanıcı
    süzgeci daralttığında bomboş bir sayfa görüyordu ve neyin yanlış
    gittiğini söyleyen bir şey yoktu.
  */
  const suzuluyor = arama !== '' || durumId !== null || ajandada !== null;
  const bosDurum = (
    <EmptyState
      ikon={ClipboardList}
      baslik={suzuluyor ? 'Eşleşen talep yok' : 'Talep yok'}
      aciklama={
        suzuluyor
          ? 'Süzgeçleri temizleyerek tüm talepleri görebilirsiniz.'
          : 'Biriminize düşen bir talep bulunmuyor.'
      }
    />
  );

  return (
    <div className="space-y-3.5">
      {/*
        ── Araç çubuğu ──

        Tek satır. Önceden ikinci bir "ikincil eylemler" satırı vardı (arşiv,
        harita, halk günleri, Excel, PDF); listeden önce iki sıra düğme,
        ekranın asıl işini aşağı itiyordu. Arşiv, harita ve halk günleri
        kaldırıldı; çıktı düğmeleri kapsam seçiminin soluna, birleşik ikili
        olarak taşındı.
      */}
      <div className="flex items-center gap-2 md:flex-row">
        {/*
          MOBİLDE ARAMA VE ÇIKTI TEK KONTROL — ajandadaki gramerin aynısı.
          İkisi de aynı listeye ait: biri süzüyor, öteki süzülmüş hâlini
          basıyor. Ayrı kutular hâlinde durduklarında ilişkisiz iki düğme
          gibi okunuyorlardı.
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
            aria-label="Taleplerde ara"
            className="min-w-0 flex-1 bg-transparent pr-2 text-sm text-ink outline-hidden placeholder:text-ink-3"
          />
          <ExportButtons
            excel={() => download('/disa-aktar/talep/excel', { ara: arama, durumId })}
            pdf={() => download('/disa-aktar/talep/pdf', { ara: arama, durumId })}
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
            placeholder="Konu, ad soyad, meslek veya telefon ara"
            aria-label="Taleplerde ara"
            ikon={<Search size={15} />}
            className="md:max-w-[340px] md:flex-1"
          />
        )}

        {/*
          Dar ekranda SARILIR. Beş denetim (ajanda süzgeci · sıralama · Excel ·
          PDF · kapsam) 390px'de tek satıra sığmıyor, birbirinin üstüne biniyor
          ve "Alt birimler" ekranın dışında kalıyordu. Yatay taşma ölçümü bunu
          yakalamıyor: öğeler genişliği itmek yerine üst üste biniyor.
        */}
        <div className="hidden min-w-0 flex-wrap items-center gap-2 md:ml-auto md:flex md:flex-nowrap">
          <SelectMenu
            deger={ajandada === null ? null : ajandada ? 1 : 0}
            degistir={(d) => suzgecDegisti(() => setAjandada(d === null ? null : d === 1))}
            etiket="Ajandada"
            className="min-w-0 flex-1 md:flex-none"
            tumuEtiketi="Ajanda durumu farketmez"
            secenekler={[
              { deger: 0, etiket: 'Ajandaya eklenmemiş' },
              { deger: 1, etiket: 'Ajandaya eklenmiş' },
            ]}
          />

          {/* Tarih sıralaması tek düğmede: iki ayrı seçenek göstermek, aynı
              anda yalnızca biri geçerli olan bir şeyi liste gibi sunardı. */}
          <Button
            varyant="ikincil"
            className="h-9 shrink-0 px-2.5"
            onClick={() => suzgecDegisti(() => setAzalan((a) => !a))}
            title={azalan ? 'En yeni önce — tersine çevir' : 'En eski önce — tersine çevir'}
            aria-label={azalan ? 'Tarihe göre en yeni önce' : 'Tarihe göre en eski önce'}
          >
            {azalan ? <ArrowDownWideNarrow size={15} /> : <ArrowUpNarrowWide size={15} />}
            <span className="hidden text-sm lg:inline">
              {azalan ? 'En yeni' : 'En eski'}
            </span>
          </Button>

          <ExportButtons
            excel={() => download('/disa-aktar/talep/excel', { ara: arama, durumId })}
            pdf={() => download('/disa-aktar/talep/pdf', { ara: arama, durumId })}
          />

          <SegmentedSelect<RecurrenceScope>
            deger={kapsam}
            degistir={(d) => suzgecDegisti(() => setKapsam(d))}
            etiket="Kapsam"
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              {
                deger: 'alt',
                etiket: 'Alt birimler',
                ikon: <SlidersHorizontal size={13} className="hidden sm:block" />,
              },
            ]}
            // Dar ekranda KENDİ SATIRINDA: "Birimim / Alt birimler" ikilisi
            // öteki denetimlerin yanına sıkışınca metinler üst üste biniyordu.
            className="w-full justify-center md:w-auto md:flex-none"
          />
        </div>

        {hasPermission(PERMISSION.talepEkle) && (
          <Link to="/talepler/yeni" className="hidden md:block md:shrink-0">
            <Button>
              <Plus size={14} />
              Yeni talep
            </Button>
          </Link>
        )}
      </div>

      {/* ── Durum çipleri ── */}
      {/* Çip şeridi MASAÜSTÜNE özel: mobilde aynı seçim süzgeç tabakasında,
          orada tam genişlik ve 44px dokunma hedefiyle. */}
      {(sayaclar.data ?? []).length > 0 && (
        <ChipStrip className="hidden md:flex">
          <FilterChip
            secili={durumId === null}
            tikla={() => suzgecDegisti(() => setDurumId(null))}
            sayi={toplamSayac}
          >
            Tümü
          </FilterChip>
          {(sayaclar.data ?? []).map((d) => (
            <FilterChip
              key={d.durumId}
              secili={durumId === d.durumId}
              tikla={() => suzgecDegisti(() => setDurumId(durumId === d.durumId ? null : d.durumId!))}
              sayi={d.adet}
            >
              {d.durumAd}
            </FilterChip>
          ))}
        </ChipStrip>
      )}

      {/*
        ── MOBİL: FAB ve süzgeç tabakası ──
        Ajanda ile aynı gramer; iki ekran arasında kas hafızası bozulmasın.
      */}
      <Fab
        etiket="Talep eylemleri"
        eylemler={[
          ...(hasPermission(PERMISSION.talepEkle)
            ? [{
                etiket: 'Yeni talep',
                ikon: <Plus size={21} strokeWidth={2.2} />,
                onClick: () => gezin('/talepler/yeni'),
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
          (arama ? 1 : 0) + (durumId !== null ? 1 : 0) + (ajandada !== null ? 1 : 0) +
          (kapsam !== 'kendi' ? 1 : 0)
        }
        temizle={() =>
          suzgecDegisti(() => {
            setAramaGirdisi('');
            setDurumId(null);
            setAjandada(null);
            setKapsam('kendi');
          })
        }
      >
        <FilterSection baslik="Kapsam">
          <Segment
            deger={kapsam}
            degistir={(d) => suzgecDegisti(() => setKapsam(d))}
            secenekler={[
              { deger: 'kendi' as RecurrenceScope, etiket: 'Birimim' },
              { deger: 'alt' as RecurrenceScope, etiket: 'Alt birimler' },
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Ara">
          <SearchInput
            value={aramaGirdisi}
            onChange={(e) => setAramaGirdisi(e.target.value)}
            placeholder="Konu, ad soyad, meslek veya telefon"
            aria-label="Taleplerde ara"
            ikon={<Search size={15} />}
          />
        </FilterSection>

        {(sayaclar.data ?? []).length > 0 && (
          <FilterSection baslik="Durum">
            <FilterOptions
              deger={durumId}
              degistir={(d) => suzgecDegisti(() => setDurumId(d))}
              secenekler={[
                { deger: null as number | null, etiket: 'Tümü', sayi: toplamSayac },
                ...(sayaclar.data ?? []).map((d) => ({
                  deger: d.durumId! as number | null,
                  etiket: d.durumAd ?? '',
                  sayi: d.adet,
                  renk: d.renk,
                })),
              ]}
            />
          </FilterSection>
        )}

        <FilterSection baslik="Ajanda durumu">
          <FilterOptions
            deger={ajandada}
            degistir={(d) => suzgecDegisti(() => setAjandada(d))}
            secenekler={[
              { deger: null as boolean | null, etiket: 'Farketmez' },
              { deger: false as boolean | null, etiket: 'Eklenmemiş' },
              { deger: true as boolean | null, etiket: 'Eklenmiş' },
            ]}
          />
        </FilterSection>

        <FilterSection baslik="Sıralama">
          <Segment
            deger={azalan ? 'yeni' : 'eski'}
            degistir={(d) => suzgecDegisti(() => setAzalan(d === 'yeni'))}
            secenekler={[
              { deger: 'yeni', etiket: 'En yeni önce', ikon: <ArrowDownWideNarrow size={14} /> },
              { deger: 'eski', etiket: 'En eski önce', ikon: <ArrowUpNarrowWide size={14} /> },
            ]}
          />
        </FilterSection>
      </FilterSheet>

      {/* ── Liste ── */}
      {isLoading ? (
        <SkeletonRows adet={6} />
      ) : isError ? (
        <EmptyState
          ikon={ClipboardList}
          baslik="Talepler yüklenemedi"
          aciklama={(error as Error)?.message}
        />
      ) : (
        <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
          {/* Mobilde yerel liste: tek yüzey, saç teli ayırıcı, solda durum
              rengi taşıyan çip. Kart düzeni satır başına dört satır yüksekliğe
              çıkıyor ve ekrana üç kayıt sığıyordu. */}
          {!masaustu && satirlar.length === 0 && bosDurum}
          {!masaustu && satirlar.length > 0 && (
            <div>
              <InsetGroup>
                {satirlar.map((t, i) => (
                  <ListRow
                    key={t.id}
                    sira={i}
                    sonuncu={i === satirlar.length - 1}
                    yol={`/talepler/${t.id}`}
                    ikon={<ClipboardList size={15} strokeWidth={1.9} />}
                    ikonRengi={t.durumRenk ?? 'var(--brand-ui)'}
                    ust={
                      <>
                        <span className="font-medium tabular-nums text-ink-2">
                          {shortDate(t.baslangicTarih)}
                        </span>
                        {t.adSoyad && <span className="truncate">· {t.adSoyad}</span>}
                      </>
                    }
                    baslik={t.konu}
                    alt={
                      <>
                        {t.mahalleAd && <span className="truncate">{t.mahalleAd}</span>}
                        {t.birimAd && (
                          <span className="truncate text-ink-3">
                            {t.mahalleAd ? '· ' : ''}
                            {t.birimAd}
                          </span>
                        )}
                      </>
                    }
                    sag={
                      t.durumAd ? (
                        <span className="mt-2.5 shrink-0">
                          <ColoredBadge etiket={t.durumAd} renk={t.durumRenk} />
                        </span>
                      ) : undefined
                    }
                  />
                ))}
              </InsetGroup>
            </div>
          )}

          {masaustu && (
          <DataList
            satirlar={satirlar}
            sutunlar={sutunlar}
            anahtar={(t) => t.id!}
            bagla={(t) => `/talepler/${t.id}`}
            mobilBaslik={(t) => t.konu}
            mobilAciklama={(t) => t.adSoyad}
            mobilRozet={(t) => <ColoredBadge etiket={t.durumAd} renk={t.durumRenk} />}
            bos={bosDurum}
          />
          )}

          <Pagination
            sonuc={data}
            sayfaDegistir={setSayfa}
            boyutDegistir={(b) => suzgecDegisti(() => setBoyut(b))}
            birim="talep"
            className="mt-3"
          />
        </div>
      )}
    </div>
  );
}
