import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft, Pencil, Phone, Plus, RotateCcw, Search, UserPlus, Users, UserX,
  SlidersHorizontal,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FieldWrapper, SearchInput, Textarea, Input } from '../../components/Field';
import { ExportButtons } from '../../components/ExportButtons';
import { download } from '../../data/download';
import { EmptyState } from '../../components/EmptyState';
import { Button, IconButton } from '../../components/Button';
import { RowActions } from '../../components/RowActions';
import { Segment, FilterSection, FilterSheet } from '../../components/FilterSheet';
import { useIsDesktop } from '../../components/screenSize';
import { Fab } from '../../shell/mobile/Fab';
import { FormModal } from '../../components/FormModal';
import { SkeletonRows } from '../../components/Skeleton';
import { PersonHistory } from '../../components/PersonHistory';
import { DataList, type Column } from '../../components/DataList';
import { Pagination } from '../../components/Pagination';
import { SegmentedSelect } from '../../components/Filters';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { shortDate, phone } from '../../data/format';
import { api } from '../../data/client';
import { usePublicDayApplications } from '../../data/hooks';
import type { PublicDayApplication } from '../../data/types';

type RecurrenceScope = 'bekleyen' | 'reddedilen' | 'tumu';

/** Sunucudaki `BasvuruDurumu` ile birebir. */
const DURUM_REDDEDILDI = 4;

/**
 * BEKLEYENLER HAVUZU.
 *
 * Vatandaş makama gelir, başkanla görüşemez; "halk gününde görüşmek ister
 * misiniz?" diye sorulur ve isterse buraya yazılır. Kayıt bir güne atanmadan
 * da yaşar — vatandaş bugün başvurur, üç hafta sonraki güne atanır.
 */
export default function Applications() {
  const { hasPermission } = useSession();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [kapsam, setKapsam] = useState<RecurrenceScope>('bekleyen');
  const masaustu = useIsDesktop();
  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [sayfa, setSayfa] = useState(1);
  const [form, setForm] = useState<PublicDayApplication | 'yeni' | null>(null);
  const [reddedilecek, setReddedilecek] = useState<PublicDayApplication | null>(null);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  // Süzgeç TEK nesnede: liste ve Excel aynı yerden okur (bkz. Tasks.tsx).
  const suzgec = {
    ara: arama,
    // "Bekleyen" = henüz hiçbir güne atanmamış VE reddedilmemiş. Atama
    // ekranının aradığı küme bu; "Reddedilenler" geri çevrilenleri, "Tümü"
    // geçmişi gösterir.
    atanmamis: kapsam === 'bekleyen' ? true : undefined,
    durum: kapsam === 'reddedilen' ? DURUM_REDDEDILDI : undefined,
  };

  const liste = usePublicDayApplications({ sayfa, boyut: 25, ...suzgec });

  const qc = useQueryClient();
  const { bildir } = useToast();

  /**
   * Havuza geri alma.
   *
   * Ret bir KARAR ve geri alınabilir olmalı: yanlış kişiyi reddetmek,
   * kaydı yeniden yazmayı gerektirmemeli.
   */
  const geriAl = useMutation({
    mutationFn: (id: number) => api.post(`/halk-gunu/basvuru/${id}/geri-al`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', 'Kayıt havuza geri alındı');
    },
    onError: (h: Error) => bildir('hata', 'Geri alınamadı', h.message),
  });

  const sutunlar: Column<PublicDayApplication>[] = [
    {
      anahtar: 'adSoyad',
      baslik: 'Vatandaş',
      // Mobil kartta bu sütun BAŞLIK olarak zaten yazılıyor; ikinci kez
      // göstermek aynı adı üst üste iki kez basıyordu.
      mobil: false,
      hucre: (b) => (
        <span className="flex flex-col">
          <span className="line-clamp-1 font-medium">{b.adSoyad}</span>
          {b.meslek && <span className="text-xs text-text-3">{b.meslek}</span>}
        </span>
      ),
    },
    {
      anahtar: 'telefon',
      baslik: 'Telefon',
      genislik: '150px',
      hucre: (b) =>
        b.telefon ? (
          // Telefon TIKLANABİLİR: sekreter numarayı elle çevirmek yerine
          // doğrudan arayabilsin.
          <a
            href={`tel:${b.telefon}`}
            onClick={(e) => e.stopPropagation()}
            className="flex items-center gap-1.5 tabular-nums text-brand-2 hover:underline"
          >
            <Phone size={12} />
            {phone(b.telefon)}
          </a>
        ) : (
          <span className="text-text-3">—</span>
        ),
      mobil: false,
    },
    {
      anahtar: 'mahalleAd',
      baslik: 'Mahalle',
      genislik: '140px',
      hucre: (b) => b.mahalleAd ?? <span className="text-text-3">—</span>,
      mobil: false,
    },
    {
      anahtar: 'konu',
      baslik: 'Konu',
      hucre: (b) => <span className="line-clamp-1">{b.konu}</span>,
      mobil: false,
    },
    {
      anahtar: 'durumAd',
      baslik: 'Durum',
      genislik: '110px',
      // Durum mobilde ROZET olarak sağ üstte; sütun olarak da yazmak
      // tekrardı. Ret gerekçesi mobil açıklamada gösteriliyor.
      mobil: false,
      hucre: (b) => (
        <span className="flex flex-col">
          <span className={b.durum === DURUM_REDDEDILDI ? 'text-(--st-no)' : undefined}>
            {b.durumAd}
          </span>
          {/* Ret GEREKÇESİ listede duruyor: aynı kişi bir sonraki ay yeniden
              başvurduğunda okunacak tek şey o. */}
          {b.redNedeni && (
            <span className="line-clamp-2 text-2xs text-text-3">{b.redNedeni}</span>
          )}
          {(b.halkGunleri?.length ?? 0) > 0 && (
            <span className="text-2xs text-text-3">
              {shortDate(b.halkGunleri![0])}
            </span>
          )}
        </span>
      ),
    },
    {
      anahtar: 'eylem',
      baslik: '',
      genislik: '48px',
      sag: true,
      hucre: (b) =>
        hasPermission(PERMISSION.halkgunuBasvuru) ? (
          <span className="flex items-center justify-end gap-0.5">
            <IconButton etiket={`${b.adSoyad} kaydını düzenle`} onClick={() => setForm(b)}>
              <Pencil size={14} />
            </IconButton>
            {b.durum === DURUM_REDDEDILDI ? (
              <IconButton
                etiket={`${b.adSoyad} kaydını havuza geri al`}
                onClick={() => geriAl.mutate(b.id!)}
              >
                <RotateCcw size={14} />
              </IconButton>
            ) : (
              <IconButton
                etiket={`${b.adSoyad} görüşmesini reddet`}
                onClick={() => setReddedilecek(b)}
              >
                <UserX size={14} />
              </IconButton>
            )}
          </span>
        ) : null,
    },
  ];

  return (
    <div className="space-y-4">
      {/* ── Başlık ── */}
      <div className="flex items-center gap-2">
        <Link to="/halk-gunu" className="shrink-0">
          <IconButton etiket="Halk günlerine dön">
            <ArrowLeft size={16} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h1 className="font-display text-xl font-bold">Bekleyenler</h1>
          <p className="text-xs text-text-3">
            Halk gününde görüşmek isteyen vatandaşlar
          </p>
        </div>
      </div>

      {/* ── Araç çubuğu ── */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Ad, telefon veya konu ara"
          aria-label="Bekleyenlerde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[300px] md:flex-1"
        />

        {hasPermission(PERMISSION.halkgunuCiktiAl) && (
          <ExportButtons
            className="hidden md:inline-flex"
            excel={() => download('/halk-gunu/basvuru/excel', suzgec)}
          />
        )}

        {/*
          Üç bölümlü seçim + "Vatandaş ekle" 390px'e SIĞMIYOR (görsel tur 4px
          yatay taşma yakaladı). Dar ekranda satır sarılır; seçim kendi içinde
          kayar, düğme alta iner.
        */}
        {/*
          MOBİLDE ÜSTTE YALNIZCA ARAMA.

          Ekran DÖRT sıra denetimle açılıyordu: başlık+açıklama, arama, üç
          bölümlü kapsam seçimi ve "Vatandaş ekle". 844px'lik ekranın ilk
          yarısında tek bir vatandaş adı yoktu. Kapsam süzgeç tabakasına,
          ekleme FAB'a taşındı.
        */}
        {!masaustu && (
          <>
            <Fab
              etiket="Havuz eylemleri"
              eylemler={[
                ...(hasPermission(PERMISSION.halkgunuBasvuru)
                  ? [{ etiket: 'Vatandaş ekle', ikon: <Plus size={21} strokeWidth={2.2} />, onClick: () => setForm('yeni') }]
                  : []),
                { etiket: 'Süz', ikon: <SlidersHorizontal size={19} strokeWidth={2} />, onClick: () => setSuzgecAcik(true) },
              ]}
            />

            <FilterSheet
              acik={suzgecAcik}
              kapat={() => setSuzgecAcik(false)}
              etkinSayisi={kapsam !== 'bekleyen' ? 1 : 0}
              temizle={() => {
                setKapsam('bekleyen');
                setSayfa(1);
              }}
            >
              <FilterSection baslik="Kapsam">
                <Segment
                  deger={kapsam}
                  degistir={(k) => {
                    setKapsam(k);
                    setSayfa(1);
                  }}
                  secenekler={[
                    { deger: 'bekleyen' as RecurrenceScope, etiket: 'Bekleyenler' },
                    { deger: 'reddedilen' as RecurrenceScope, etiket: 'Reddedilenler' },
                    { deger: 'tumu' as RecurrenceScope, etiket: 'Tümü' },
                  ]}
                />
              </FilterSection>
            </FilterSheet>
          </>
        )}

        <div className="hidden min-w-0 flex-wrap items-center gap-1.5 md:ml-auto md:flex md:flex-nowrap">
          <SegmentedSelect<RecurrenceScope>
            deger={kapsam}
            degistir={(k) => {
              setKapsam(k);
              setSayfa(1);
            }}
            etiket="Kapsam"
            className="min-w-0 max-w-full overflow-x-auto"
            secenekler={[
              { deger: 'bekleyen', etiket: 'Bekleyenler' },
              { deger: 'reddedilen', etiket: 'Reddedilenler' },
              { deger: 'tumu', etiket: 'Tümü' },
            ]}
          />

          {hasPermission(PERMISSION.halkgunuBasvuru) && (
            <Button onClick={() => setForm('yeni')}>
              <Plus size={14} />
              Vatandaş ekle
            </Button>
          )}
        </div>
      </div>

      {/* ── İçerik ── */}
      {liste.isLoading ? (
        <SkeletonRows adet={6} />
      ) : (liste.data?.veriler.length ?? 0) === 0 ? (
        <EmptyState
          ikon={Users}
          baslik={kapsam === 'bekleyen' ? 'Bekleyen yok' : 'Kayıt yok'}
          aciklama="Görüşmek isteyen vatandaşı ekleyin; sonra bir halk gününe atarsınız."
          eylem={
            hasPermission(PERMISSION.halkgunuBasvuru) ? (
              <Button onClick={() => setForm('yeni')}>
                <Plus size={14} />
                Vatandaş ekle
              </Button>
            ) : undefined
          }
        />
      ) : (
        <>
          {/*
            MOBİLDE YEREL LİSTE.

            `Liste`'nin mobil dalı adı, uzun bir "telefon · mahalle · konu ·
            ret gerekçesi" zincirini ve düğmeleri alt alta koyuyordu: satır
            ~130px, düğmeler metnin altında havada duran iki ayrı kare.
            Şimdi tek yüzey, saç teli ayırıcı, sağda durum çipi ve altında
            **tek grup** hâlinde düzenle/reddet.
          */}
          {!masaustu ? (
            <div className="overflow-hidden rounded-card border border-line bg-surface">
              <ul className="divide-y divide-line">
                {liste.data!.veriler.map((b) => (
                  <li key={b.id} className="flex items-start gap-3 p-3">
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-semibold">{b.adSoyad}</span>
                      {b.telefon && (
                        <a
                          href={`tel:${b.telefon}`}
                          className="mt-0.5 block truncate text-xs tabular-nums text-ink-2 hover:underline"
                        >
                          {phone(b.telefon)}
                        </a>
                      )}
                      <span className="mt-0.5 block truncate text-2xs text-ink-3">
                        {[b.mahalleAd, b.konu].filter(Boolean).join(' · ')}
                      </span>
                      {b.redNedeni && (
                        <span className="mt-1 block text-2xs leading-[1.4] text-danger satir-2">
                          {b.redNedeni}
                        </span>
                      )}
                    </span>

                    <span className="flex shrink-0 flex-col items-end gap-2">
                      <span
                        className="rounded-full px-2 py-0.5 text-2xs font-semibold"
                        style={
                          b.durum === DURUM_REDDEDILDI
                            ? { background: 'var(--st-no-bg)', color: 'var(--st-no)' }
                            : { background: 'var(--sunken)', color: 'var(--ink-2)' }
                        }
                      >
                        {b.durumAd}
                      </span>

                      {hasPermission(PERMISSION.halkgunuBasvuru) && (
                        <RowActions
                          boyut="kucuk"
                          eylemler={[
                            {
                              etiket: `${b.adSoyad} kaydını düzenle`,
                              ikon: Pencil,
                              onClick: () => setForm(b),
                            },
                            b.durum === DURUM_REDDEDILDI
                              ? {
                                  etiket: `${b.adSoyad} kaydını havuza geri al`,
                                  ikon: RotateCcw,
                                  onClick: () => geriAl.mutate(b.id!),
                                }
                              : {
                                  etiket: `${b.adSoyad} görüşmesini reddet`,
                                  ikon: UserX,
                                  onClick: () => setReddedilecek(b),
                                  ton: 'tehlike' as const,
                                },
                          ]}
                        />
                      )}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          ) : (
            <DataList
              satirlar={liste.data!.veriler}
              sutunlar={sutunlar}
              anahtar={(b) => b.id!}
              mobilBaslik={(b) => b.adSoyad}
              mobilAciklama={(b) =>
                [b.telefon, b.mahalleAd, b.konu, b.redNedeni].filter(Boolean).join(' · ')
              }
              mobilRozet={(b) => b.durumAd}
            />
          )}
          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="kişi" />
        </>
      )}

      {form && (
        <BasvuruFormu
          mevcut={form === 'yeni' ? null : form}
          kapat={() => setForm(null)}
        />
      )}

      {reddedilecek && (
        <RedPenceresi kayit={reddedilecek} kapat={() => setReddedilecek(null)} />
      )}
    </div>
  );
}

/**
 * Görüşmeyi uygun görmeme.
 *
 * <p>
 * Kayıt SİLİNMEZ, havuzda kalır: "kaç kişi geri çevrildi" ve "bunu neden
 * çevirmiştik?" sorularının cevabı ancak öyle duruyor. Kişi bir güne
 * atanmışsa sunucu o atamayı kaldırır — ret kararı liste kurulduktan sonra
 * geldiğinde vatandaş salonda çağrılmaya devam ediyordu.
 * </p>
 */
function RedPenceresi({
  kayit,
  kapat,
}: {
  kayit: PublicDayApplication;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [neden, setNeden] = useState('');

  const reddet = useMutation({
    mutationFn: () =>
      api.post(`/halk-gunu/basvuru/${kayit.id}/reddet`, { neden: neden || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', 'Görüşme uygun görülmedi olarak işaretlendi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'İşaretlenemedi', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Görüşme uygun görülmedi"
      aciklama={`${kayit.adSoyad} havuzda kalır; atandığı gün varsa listeden düşer.`}
      ikon={<UserX size={15} />}
      genislik="dar"
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button varyant="yikici" onClick={() => reddet.mutate()} disabled={reddet.isPending}>
            {reddet.isPending ? 'İşleniyor…' : 'Uygun görülmedi'}
          </Button>
        </>
      }
    >
      <FieldWrapper
        etiket="Gerekçe"
        id="red-neden"
        ipucu="Aynı kişi yeniden başvurduğunda okunacak tek şey bu"
      >
        <Textarea
          id="red-neden"
          value={neden}
          onChange={(e) => setNeden(e.target.value)}
          rows={3}
          autoFocus
          placeholder="Konu belediyeyi ilgilendirmiyor; ilgili kuruma yönlendirildi."
        />
      </FieldWrapper>
    </FormModal>
  );
}

/**
 * Vatandaş formu — **TELEFON ÖNCE**.
 *
 * Alan sırası bilinçli: telefon en üstte, çünkü numara girilir girilmez
 * geçmiş kartı beliriyor ("3 talep · 2 etkinlik · 1 halk günü"). Kullanıcı
 * ayrıntıya bakabilir ya da doğrudan kaydetmeye devam edebilir; kart hiçbir
 * şeyi engellemiyor.
 *
 * Ad alttan yukarı taşınsaydı kullanıcı kişiyi tanımadan yazmaya başlar,
 * mükerrer kayıt açardı.
 */
function BasvuruFormu({
  mevcut,
  kapat,
}: {
  mevcut: PublicDayApplication | null;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [telefon, setTelefon] = useState(mevcut?.telefon ?? '');
  const [ad, setAd] = useState(mevcut?.ad ?? '');
  const [soyad, setSoyad] = useState(mevcut?.soyad ?? '');
  const [adres, setAdres] = useState(mevcut?.adres ?? '');
  const [meslek, setMeslek] = useState(mevcut?.meslek ?? '');
  const [konu, setKonu] = useState(mevcut?.konu ?? '');
  const [not, setNot] = useState(mevcut?.not ?? '');

  const kaydet = useMutation({
    mutationFn: () => {
      const govde = {
        ad,
        soyad: soyad || null,
        telefon: telefon || null,
        adres: adres || null,
        meslek: meslek || null,
        konu: konu || null,
        not: not || null,
      };
      return mevcut
        ? api.put(`/halk-gunu/basvuru/${mevcut.id}`, govde)
        : api.post('/halk-gunu/basvuru', govde);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', mevcut ? 'Kayıt güncellendi' : 'Vatandaş eklendi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={mevcut ? 'Vatandaş kaydı' : 'Vatandaş ekle'}
      aciklama="Halk gününde görüşmek isteyen kişi. Önce telefonu girin — daha önce kaydı varsa görürsünüz."
      ikon={<UserPlus size={15} />}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={() => kaydet.mutate()} disabled={!ad.trim() || kaydet.isPending}>
            {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </Button>
        </>
      }
    >
      <FieldWrapper
        etiket="Telefon"
        id="bv-telefon"
        ipucu="Numarayı girince geçmiş kaydı aranır"
      >
        <Input
          id="bv-telefon"
          value={telefon}
          onChange={(e) => setTelefon(e.target.value)}
          placeholder="0541 298 34 50"
          inputMode="tel"
          autoFocus={!mevcut}
        />
      </FieldWrapper>

      {/* Geçmiş kartı telefonun HEMEN ALTINDA: kullanıcı numarayı yazdıktan
          sonra gözünü aşağı kaydırmadan görsün. */}
      <PersonHistory telefon={telefon} ad={ad.length >= 3 ? `${ad} ${soyad}`.trim() : undefined} />

      <div className="grid gap-4 sm:grid-cols-2">
        <FieldWrapper etiket="Ad" id="bv-ad" zorunlu>
          <Input id="bv-ad" value={ad} onChange={(e) => setAd(e.target.value)} />
        </FieldWrapper>
        <FieldWrapper etiket="Soyad" id="bv-soyad">
          <Input id="bv-soyad" value={soyad} onChange={(e) => setSoyad(e.target.value)} />
        </FieldWrapper>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <FieldWrapper etiket="Meslek" id="bv-meslek">
          <Input id="bv-meslek" value={meslek} onChange={(e) => setMeslek(e.target.value)} />
        </FieldWrapper>
        <FieldWrapper etiket="Adres" id="bv-adres">
          <Input id="bv-adres" value={adres} onChange={(e) => setAdres(e.target.value)} />
        </FieldWrapper>
      </div>

      <FieldWrapper etiket="Konu" id="bv-konu" ipucu="Ne için görüşmek istiyor">
        <Textarea
          id="bv-konu"
          value={konu}
          onChange={(e) => setKonu(e.target.value)}
          rows={2}
        />
      </FieldWrapper>

      <FieldWrapper etiket="Not" id="bv-not">
        <Textarea id="bv-not" value={not} onChange={(e) => setNot(e.target.value)} rows={2} />
      </FieldWrapper>
    </FormModal>
  );
}
