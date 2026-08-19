import * as Dialog from '@radix-ui/react-dialog';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ArrowDown, ArrowUp, AtSign, Landmark, MapPin, Pencil, Phone, Plus, Search, Smartphone, Trash2, SlidersHorizontal,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Switch } from '../components/Switch';
import { FieldWrapper, SearchInput, Textarea, Input, Secim } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { FormModal } from '../components/FormModal';
import { Button, IconButton } from '../components/Button';
import { SkeletonRows } from '../components/Skeleton';
import { Card, CardHeader } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Pagination } from '../components/Pagination';
import { ChipStrip, FilterChip } from '../components/Filters';
import { Link } from 'react-router-dom';
import { RowActions } from '../components/RowActions';
import { FilterSection, FilterSheet } from '../components/FilterSheet';
import { useIsDesktop } from '../components/screenSize';
import { Fab } from '../shell/mobile/Fab';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { api, queryString, type PagedResult } from '../data/client';
import { phone } from '../data/format';

type Protocol = {
  id: number;
  kategoriId: number;
  kategori: string;
  kurum?: string | null;
  adSoyad: string;
  unvan?: string | null;
  siraNo: number;
  telefon?: string | null;
  cepTelefon?: string | null;
  eposta?: string | null;
  adres?: string | null;
  aciklama?: string | null;
  aktif: boolean;
};

type Kategori = { id: number; ad: string; siraNo: number; aktif: boolean; adet: number };

const BOS: Protocol = {
  id: 0,
  kategoriId: 0,
  kategori: '',
  adSoyad: '',
  siraNo: 0,
  aktif: true,
};

/**
 * İl protokol listesi.
 *
 * <p>
 * Protokol sırası törenlerde oturma düzenini ve konuşma sırasını belirler;
 * yanlış sıra kurumsal bir hatadır. Bu yüzden liste <b>sıra numarasına göre
 * gruplanmış</b> gösterilir ve sıra yalnızca ok düğmeleriyle, komşusuyla
 * yer değiştirerek değişir — serbest sürükleme, uzun listede yanlışlıkla
 * bambaşka bir yere bırakmayı kolaylaştırıyor.
 * </p>
 *
 * <p>
 * Okumak ajanda yetkisi olan herkese açık, <b>yazmak yalnızca Admin</b>
 * (sunucuda da böyle zorlanıyor).
 * </p>
 */
export default function Protocol() {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const { hasPermission } = useSession();
  /**
   * Yazma yetkisi İZİNDEN gelir.
   *
   * Önce rol adına bakılıyordu (`Admin` ya da `Sistem`); yönetim ekranından
   * `protokol.yonet` izni verilen bir rol, o izne sahip olduğu hâlde düğmeleri
   * göremiyordu. Rol adı yetkiyi ifade etmiyor.
   */
  const yazabilir = hasPermission(PERMISSION.protokolYonet);

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [kategoriId, setKategoriId] = useState<number | null>(null);
  const [pasifDahil, setPasifDahil] = useState(false);
  const masaustu = useIsDesktop();
  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [sayfa, setSayfa] = useState(1);

  const [form, setForm] = useState<Protocol | null>(null);
  const [silinecek, setSilinecek] = useState<Protocol | null>(null);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const liste = useQuery({
    queryKey: ['protokol', 'liste', sayfa, arama, kategoriId, pasifDahil] as const,
    queryFn: () =>
      api.get<PagedResult<Protocol>>(
        `/protokol${queryString({ sayfa, boyut: 50, ara: arama, kategoriId, pasifDahil })}`,
      ),
    placeholderData: keepPreviousData,
  });

  const kategoriler = useQuery({
    queryKey: ['protokol', 'kategoriler'] as const,
    queryFn: () => api.get<Kategori[]>('/protokol/kategoriler'),
  });

  const kaydet = useMutation({
    mutationFn: (p: Protocol) =>
      p.id
        ? api.put<Protocol>(`/protokol/${p.id}`, p)
        : api.post<Protocol>('/protokol', p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['protokol'] });
      setForm(null);
      bildir('basari', 'Protokol kaydı kaydedildi');
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/protokol/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['protokol'] });
      setSilinecek(null);
      bildir('basari', 'Kayıt silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  /** İki kaydın sıra numarasını takas eder — tek istekte. */
  const siralamaDegistir = useMutation({
    mutationFn: (ogeler: { id: number; siraNo: number }[]) =>
      api.post<void>('/protokol/siralama', { ogeler }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['protokol'] }),
    onError: (h: Error) => bildir('hata', 'Sıralama değiştirilemedi', h.message),
  });

  const kayitlar = liste.data?.veriler ?? [];

  /**
   * Kategoriye göre grupla.
   *
   * Sunucu zaten sıra numarasına göre sıralı gönderiyor; burada yalnızca
   * başlıklandırma yapılır, yeniden sıralama YAPILMAZ.
   */
  const gruplar = useMemo(() => {
    const harita = new Map<string, Protocol[]>();
    for (const p of kayitlar) {
      const anahtar = p.kategori || 'Diğer';
      (harita.get(anahtar) ?? harita.set(anahtar, []).get(anahtar)!).push(p);
    }
    return [...harita.entries()];
  }, [kayitlar]);

  function takas(grup: Protocol[], indeks: number, yon: -1 | 1) {
    const digerIndeks = indeks + yon;
    if (digerIndeks < 0 || digerIndeks >= grup.length) return;

    const a = grup[indeks];
    const b = grup[digerIndeks];

    // Numaralar EŞİTSE düz takas hiçbir şeyi değiştirmez (elle girilen sıra
    // numaralarında sık görülüyor); taşınan kayda komşusunun bir eksiği ya da
    // fazlası verilir, komşu yerinde kalır.
    siralamaDegistir.mutate([
      { id: a.id, siraNo: a.siraNo === b.siraNo ? b.siraNo + yon : b.siraNo },
      { id: b.id, siraNo: a.siraNo },
    ]);
  }

  return (
    <div className="space-y-3.5">
      {/*
        Form MODAL: liste ekranı arkada duruyor. Önceden tam sayfaya geçiyordu
        ve kaydettikten sonra kullanıcı listedeki yerini kaybediyordu — hangi
        sayfada, hangi süzgeçle çalıştığını yeniden kurmak gerekiyordu.
      */}
      <ProtokolFormu
        baslangic={form}
        kategoriler={kategoriler.data ?? []}
        beklemede={kaydet.isPending}
        kaydet={(p) => kaydet.mutate(p)}
        vazgec={() => setForm(null)}
      />
      {/* ── Araç çubuğu ── */}
      <div className="flex flex-col gap-2.5 md:flex-row md:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Ad, unvan, kurum ara"
          aria-label="Protokolde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[320px] md:flex-1"
        />

        {/*
          MOBİLDE ÜSTTE YALNIZCA ARAMA.

          Ekran DÖRT sıra denetimle açılıyordu: arama, "Görevden ayrılanlar"
          anahtarı, "Yeni kayıt" düğmesi ve kategori çipleri. Ekranın üst
          yarısı kontrol, ilk protokol kaydı kıvrımın altındaydı. Anahtar ve
          kategoriler süzgeç tabakasına, ekleme FAB'a taşındı.
        */}
        {!masaustu && (
          <>
            <Fab
              etiket="Protokol eylemleri"
              eylemler={[
                ...(yazabilir
                  ? [{ etiket: 'Yeni kayıt', ikon: <Plus size={21} strokeWidth={2.2} />, onClick: () => setForm({ ...BOS }) }]
                  : []),
                { etiket: 'Süz', ikon: <SlidersHorizontal size={19} strokeWidth={2} />, onClick: () => setSuzgecAcik(true) },
              ]}
            />

            <FilterSheet
              acik={suzgecAcik}
              kapat={() => setSuzgecAcik(false)}
              etkinSayisi={(kategoriId !== null ? 1 : 0) + (pasifDahil ? 1 : 0)}
              temizle={() => {
                setKategoriId(null);
                setPasifDahil(false);
                setSayfa(1);
              }}
            >
              {(kategoriler.data?.length ?? 0) > 0 && (
                <FilterSection baslik="Kategori">
                  <div className="flex flex-wrap gap-1.5">
                    <FilterChip
                      secili={kategoriId === null}
                      tikla={() => {
                        setKategoriId(null);
                        setSayfa(1);
                      }}
                      sayi={liste.data?.toplam ?? 0}
                    >
                      Tümü
                    </FilterChip>
                    {(kategoriler.data ?? []).map((k) => (
                      <FilterChip
                        key={k.id}
                        secili={kategoriId === k.id}
                        tikla={() => {
                          setKategoriId(kategoriId === k.id ? null : k.id);
                          setSayfa(1);
                        }}
                        sayi={k.adet}
                      >
                        {k.ad}
                      </FilterChip>
                    ))}
                  </div>
                </FilterSection>
              )}

              <FilterSection baslik="Kapsam">
                <Switch
                  isaretli={pasifDahil}
                  degistir={(a) => {
                    setPasifDahil(a);
                    setSayfa(1);
                  }}
                  etiket="Görevden ayrılanlar da görünsün"
                />
              </FilterSection>
            </FilterSheet>
          </>
        )}

        <div className="hidden md:ml-auto md:block">
          <Switch
            isaretli={pasifDahil}
            degistir={(a) => {
              setPasifDahil(a);
              setSayfa(1);
            }}
            etiket="Görevden ayrılanlar"
          />
        </div>


        {yazabilir && (
          <Button className="hidden md:inline-flex" onClick={() => setForm({ ...BOS })}>
            <Plus size={14} />
            Yeni kayıt
          </Button>
        )}
      </div>


      {/* ── Kategori çipleri (masaüstü) ── */}
      {(kategoriler.data?.length ?? 0) > 0 && (
        <ChipStrip className="hidden md:flex">
          <FilterChip
            secili={kategoriId === null}
            tikla={() => {
              setKategoriId(null);
              setSayfa(1);
            }}
            sayi={liste.data?.toplam ?? 0}
          >
            Tümü
          </FilterChip>
          {(kategoriler.data ?? []).map((k) => (
            <FilterChip
              key={k.id}
              secili={kategoriId === k.id}
              tikla={() => {
                setKategoriId(kategoriId === k.id ? null : k.id);
                setSayfa(1);
              }}
              sayi={k.adet}
            >
              {k.ad}
            </FilterChip>
          ))}
        </ChipStrip>
      )}

      {/* ── İçerik ── */}
      {liste.isLoading ? (
        <SkeletonRows adet={6} />
      ) : liste.isError ? (
        <EmptyState
          ikon={Landmark}
          baslik="Protokol listesi yüklenemedi"
          aciklama={(liste.error as Error)?.message}
        />
      ) : kayitlar.length === 0 ? (
        <EmptyState
          ikon={Landmark}
          baslik={arama || kategoriId ? 'Eşleşen kayıt yok' : 'Protokol listesi boş'}
          aciklama={
            arama || kategoriId
              ? 'Aramayı ya da kategori süzgecini değiştirin.'
              : 'Vali, belediye başkanı, kurum müdürleri gibi protokol kayıtları burada tutulur.'
          }
          eylem={
            yazabilir ? (
              <Button onClick={() => setForm({ ...BOS })}>
                <Plus size={14} />
                Yeni kayıt
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="space-y-4">
          {gruplar.map(([ad, grup]) => (
            <Card key={ad}>
              <CardHeader
                baslik={ad}
                aciklama={`${grup.length} kişi`}
              />
              <ul className="divide-y divide-border">
                {grup.map((p, i) => (
                  <li key={p.id} className={cn('p-3 md:p-3.5', !p.aktif && 'opacity-55')}>
                    <div className="flex items-start gap-3">
                      <span
                        className="mt-0.5 grid h-7 w-7 shrink-0 place-items-center rounded-sm bg-sunken font-display text-xs font-bold tabular-nums text-text-2"
                        aria-label={`Sıra ${p.siraNo}`}
                      >
                        {p.siraNo}
                      </span>

                      <div className="min-w-0 flex-1">
                        <p className="flex flex-wrap items-center gap-x-2 text-sm font-semibold">
                          {/* Ad KİŞİ DOSYASINA bağlantı: bilgileri ve davet
                              edildiği programlar orada. Liste ekranında ada
                              tıklamak hiçbir şey açmıyordu. */}
                          <Link to={`/protokol/${p.id}`} className="hover:text-brand hover:underline">
                            {p.adSoyad}
                          </Link>
                          {!p.aktif && (
                            <span className="rounded-full bg-(--st-cancel-bg) px-2 py-px text-2xs font-medium text-(--st-cancel)">
                              Görevde değil
                            </span>
                          )}
                        </p>
                        {(p.unvan || p.kurum) && (
                          <p className="text-sm text-text-2">
                            {[p.unvan, p.kurum].filter(Boolean).join(' · ')}
                          </p>
                        )}

                        <div className="mt-1.5 flex flex-wrap gap-x-4 gap-y-1 text-xs text-text-3">
                          {p.telefon && (
                            <a
                              href={`tel:${p.telefon}`}
                              className="inline-flex items-center gap-1.5 hover:text-brand-2"
                            >
                              <Phone size={12} />
                              {phone(p.telefon)}
                            </a>
                          )}
                          {p.cepTelefon && (
                            <a
                              href={`tel:${p.cepTelefon}`}
                              className="inline-flex items-center gap-1.5 hover:text-brand-2"
                            >
                              <Smartphone size={12} />
                              {p.cepTelefon}
                            </a>
                          )}
                          {p.eposta && (
                            <a
                              href={`mailto:${p.eposta}`}
                              className="inline-flex items-center gap-1.5 hover:text-brand-2"
                            >
                              <AtSign size={12} />
                              {p.eposta}
                            </a>
                          )}
                          {p.adres && (
                            <span className="inline-flex items-center gap-1.5">
                              <MapPin size={12} />
                              {p.adres}
                            </span>
                          )}
                        </div>

                        {p.aciklama && (
                          <p className="mt-1 text-xs leading-normal text-text-3">{p.aciklama}</p>
                        )}
                      </div>

                      {yazabilir && (
                        /*
                          DÖRT AYRI DÜĞME → İKİ GRUP.

                          ↑ ↓ ✏️ 🗑 yan yana ayrı kutulardı ve 390px'lik
                          ekranda ~180px yiyordu; kalan yere unvan sığmıyor,
                          "Cumhuriyet Başsavcısı / Başsavcı · İl Adliyesi"
                          diye ÜÇ satıra kırılıyordu.

                          Sıralama DİKEY grup (yön bilgisini düğmenin yeri
                          taşıyor), düzenle/sil yatay grup. İkisi ~90px;
                          kazanılan 90px doğrudan unvana gidiyor.
                        */
                        <div className="flex shrink-0 items-start gap-1.5">
                          <RowActions
                            yon="dikey"
                            boyut="kucuk"
                            eylemler={[
                              {
                                etiket: 'Yukarı taşı',
                                ikon: ArrowUp,
                                onClick: () => takas(grup, i, -1),
                                pasif: i === 0 || siralamaDegistir.isPending,
                              },
                              {
                                etiket: 'Aşağı taşı',
                                ikon: ArrowDown,
                                onClick: () => takas(grup, i, 1),
                                pasif: i === grup.length - 1 || siralamaDegistir.isPending,
                              },
                            ]}
                          />
                          <RowActions
                            yon="dikey"
                            boyut="kucuk"
                            eylemler={[
                              { etiket: 'Düzenle', ikon: Pencil, onClick: () => setForm(p) },
                              {
                                etiket: 'Sil',
                                ikon: Trash2,
                                onClick: () => setSilinecek(p),
                                ton: 'tehlike',
                              },
                            ]}
                          />
                        </div>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            </Card>
          ))}

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="kayıt" />
        </div>
      )}

      <ConfirmDialog
        acik={silinecek !== null}
        baslik="Protokol kaydı silinsin mi?"
        aciklama={`"${silinecek?.adSoyad}" listeden kaldırılacak. Bu işlem geri alınamaz.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek && sil.mutate(silinecek.id)}
        kapat={() => setSilinecek(null)}
      />
    </div>
  );
}

/** Protokol kaydı formu. */
function ProtokolFormu({
  baslangic,
  kategoriler,
  kaydet,
  vazgec,
  beklemede,
}: {
  /** `null` = diyalog kapalı. */
  baslangic: Protocol | null;
  kategoriler: Kategori[];
  kaydet: (p: Protocol) => void;
  vazgec: () => void;
  beklemede: boolean;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [p, setP] = useState<Protocol>(baslangic ?? BOS);
  const [kategoriEkle, setKategoriEkle] = useState(false);
  const [yeniKategori, setYeniKategori] = useState('');

  // Diyalog her açılışta gelen kayıttan başlar; kapalıyken durum korunur ama
  // bir sonraki açılışta ezilir. Aksi hâlde "yeni kayıt" düğmesi bir önceki
  // düzenlemenin alanlarıyla açılıyordu.
  const [sonBaslangic, setSonBaslangic] = useState(baslangic);
  if (baslangic !== sonBaslangic) {
    setSonBaslangic(baslangic);
    if (baslangic) setP(baslangic);
  }

  const alan = <A extends keyof Protocol>(ad: A, deger: Protocol[A]) =>
    setP((o) => ({ ...o, [ad]: deger }));

  /** Yeni kategori — eklendikten sonra forma OTOMATİK seçilir. */
  const kategoriKaydet = useMutation({
    mutationFn: () =>
      api.post<Kategori>('/protokol/kategoriler', { ad: yeniKategori.trim(), aktif: true }),
    onSuccess: (k) => {
      qc.invalidateQueries({ queryKey: ['protokol'] });
      // Kullanıcı kategoriyi bu kayıt için açtı; ayrıca seçmesini beklemek
      // gereksiz bir adım olurdu.
      if (k?.id) alan('kategoriId', k.id);
      setKategoriEkle(false);
      setYeniKategori('');
      bildir('basari', 'Kategori eklendi');
    },
    onError: (h: Error) => bildir('hata', 'Kategori eklenemedi', h.message),
  });

  const gecerli = p.adSoyad.trim().length > 0 && p.kategoriId > 0;

  return (
    <FormModal
      acik={baslangic !== null}
      kapat={vazgec}
      baslik={p.id ? 'Protokol kaydını düzenle' : 'Yeni protokol kaydı'}
      aciklama="Kategori ve sıra numarası, listedeki yeri belirler."
      ikon={<Landmark size={15} />}
      genislik="genis"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={vazgec}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && kaydet(p)}
            disabled={!gecerli || beklemede}
          >
            {beklemede ? 'Kaydediliyor…' : 'Kaydet'}
          </Button>
        </>
      }
    >
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          if (gecerli) kaydet(p);
        }}
      >
        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Kategori" id="p-kategori" zorunlu>
              {/*
                Kategori artık AYRI TABLO. Serbest metin olduğu sürece aynı
                kategori üç farklı yazımla üç grup üretiyordu. Listede yoksa
                yandaki + ile yerinde açılır — kullanıcı kaydı bırakıp ayrı
                bir tanım ekranına gitmek zorunda kalmasın.
              */}
              <div className="flex items-center gap-2">
                <Secim
                  id="p-kategori"
                  value={p.kategoriId || ''}
                  onChange={(e) => alan('kategoriId', Number(e.target.value) || 0)}
                  className="flex-1"
                >
                  <option value="">Kategori seçin</option>
                  {kategoriler
                    .filter((k) => k.aktif || k.id === p.kategoriId)
                    .map((k) => (
                      <option key={k.id} value={k.id}>
                        {k.ad}
                      </option>
                    ))}
                </Secim>

                <IconButton
                  etiket="Yeni kategori ekle"
                  onClick={() => setKategoriEkle(true)}
                >
                  <Plus size={16} />
                </IconButton>
              </div>
            </FieldWrapper>

            <FieldWrapper etiket="Sıra numarası" id="p-sira" ipucu="Küçük numara üstte yer alır.">
              <Input
                id="p-sira"
                type="number"
                inputMode="numeric"
                value={String(p.siraNo)}
                onChange={(e) => alan('siraNo', Number(e.target.value) || 0)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Ad soyad" id="p-ad" zorunlu>
              <Input
                id="p-ad"
                value={p.adSoyad}
                onChange={(e) => alan('adSoyad', e.target.value)}
                maxLength={120}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Unvan" id="p-unvan">
              <Input
                id="p-unvan"
                value={p.unvan ?? ''}
                onChange={(e) => alan('unvan', e.target.value)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Kurum" id="p-kurum">
              <Input
                id="p-kurum"
                value={p.kurum ?? ''}
                onChange={(e) => alan('kurum', e.target.value)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Telefon" id="p-tel">
              <Input
                id="p-tel"
                type="tel"
                inputMode="tel"
                value={p.telefon ?? ''}
                onChange={(e) => alan('telefon', e.target.value)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Cep telefonu" id="p-cep">
              <Input
                id="p-cep"
                type="tel"
                inputMode="tel"
                value={p.cepTelefon ?? ''}
                onChange={(e) => alan('cepTelefon', e.target.value)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="E-posta" id="p-eposta">
              <Input
                id="p-eposta"
                type="email"
                value={p.eposta ?? ''}
                onChange={(e) => alan('eposta', e.target.value)}
              />
            </FieldWrapper>
          </div>

          <FieldWrapper etiket="Adres" id="p-adres">
            <Input
              id="p-adres"
              value={p.adres ?? ''}
              onChange={(e) => alan('adres', e.target.value)}
            />
          </FieldWrapper>

          <FieldWrapper etiket="Açıklama" id="p-aciklama">
            <Textarea
              id="p-aciklama"
              value={p.aciklama ?? ''}
              onChange={(e) => alan('aciklama', e.target.value)}
            />
          </FieldWrapper>

          <Switch
            isaretli={p.aktif}
            degistir={(a) => setP({ ...p, aktif: a })}
            etiket="Görevde"
            aciklama="Kapatılırsa kayıt listede görünmez ama silinmez."
          />
        </div>
      </form>

      <Dialog.Root open={kategoriEkle} onOpenChange={(a) => !a && setKategoriEkle(false)}>
        <Dialog.Portal>
          <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde" />
          <Dialog.Content className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(420px,calc(100vw-32px))] -translate-x-1/2 -translate-y-1/2 rounded-win bg-surface p-5 shadow-3">
            <Dialog.Title className="font-display text-lg font-bold">
              Yeni protokol kategorisi
            </Dialog.Title>
            <Dialog.Description className="mt-1 text-sm text-text-2 metin-guzel">
              Kategori adları tekildir; aynı ad büyük/küçük harf farkıyla bile
              ikinci kez açılamaz.
            </Dialog.Description>

            <div className="mt-4 space-y-3.5">
              <FieldWrapper etiket="Kategori adı" id="yeni-kategori" zorunlu>
                <Input
                  id="yeni-kategori"
                  value={yeniKategori}
                  onChange={(e) => setYeniKategori(e.target.value)}
                  placeholder="Örn. Askerî Erkân"
                  autoFocus
                />
              </FieldWrapper>

              <div className="flex justify-end gap-2">
                <Button
                  type="button"
                  varyant="ikincil"
                  onClick={() => setKategoriEkle(false)}
                >
                  Vazgeç
                </Button>
                <Button
                  type="button"
                  onClick={() => kategoriKaydet.mutate()}
                  disabled={!yeniKategori.trim() || kategoriKaydet.isPending}
                >
                  {kategoriKaydet.isPending ? 'Ekleniyor…' : 'Ekle'}
                </Button>
              </div>
            </div>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </FormModal>
  );
}
