import * as Tabs from '@radix-ui/react-tabs';
import { SekmeListesi, SekmeTetigi } from '../components/Tabs';
import { PERMISSION } from '../components/permissions';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Building2, History, KeyRound, Monitor, Pencil, Plus, Search, ShieldCheck, Smartphone, Trash2, Users, X,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FieldWrapper, SearchInput, Textarea, Input, Secim } from '../components/Field';
import { DatePicker } from '../components/DatePicker';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { RowActions } from '../components/RowActions';
import { useIsDesktop } from '../components/screenSize';
import { Fab } from '../shell/mobile/Fab';
import { SkeletonRows } from '../components/Skeleton';
import { Card } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { useSession } from '../auth/SessionProvider';
import { Switch } from '../components/Switch';
import { FormModal } from '../components/FormModal';
import { initials, unitLabel, number, dateTime } from '../data/format';
import { api, queryString, type PagedResult } from '../data/client';
import type { UnitNode, PermissionRecord, UserSummary, SessionRecord, Role } from '../data/types';

/**
 * Sistem yönetimi — kullanıcılar, birimler, roller, giriş kayıtları.
 *
 * <p>
 * Etkin sekme URL'de (<c>?bolum=birimler</c>): birim detayından geri dönen
 * kullanıcı ağacı açık buluyor, sayfa yenilemek sekmeyi sıfırlamıyor ve
 * "şu ekrana bak" derken bağlantı paylaşılabiliyor.
 * </p>
 */
export default function Administration() {
  const [sorgu, setSorgu] = useSearchParams();
  const bolum = sorgu.get('bolum') ?? 'kullanicilar';

  return (
    <Tabs.Root
      value={bolum}
      onValueChange={(d) => {
        // Sekme değişince eski sekmenin süzgeç/form parametreleri düşer.
        setSorgu(d === 'kullanicilar' ? {} : { bolum: d });
      }}
    >
      <SekmeListesi etiket="Yönetim bölümleri" className="mb-4">
        {[
          { d: 'kullanicilar', e: 'Kullanıcılar', i: <Users size={14} /> },
          { d: 'birimler', e: 'Birimler', i: <Building2 size={14} /> },
          { d: 'roller', e: 'Roller', i: <ShieldCheck size={14} /> },
          { d: 'oturumlar', e: 'Giriş kayıtları', i: <History size={14} /> },
        ].map((s) => (
          <SekmeTetigi key={s.d} deger={s.d}>
            {s.i}
            {s.e}
          </SekmeTetigi>
        ))}
      </SekmeListesi>

      <Tabs.Content value="kullanicilar">
        <Kullanicilar />
      </Tabs.Content>
      <Tabs.Content value="birimler">
        <Birimler />
      </Tabs.Content>
      <Tabs.Content value="roller">
        <Roller />
      </Tabs.Content>
      <Tabs.Content value="oturumlar">
        <OturumKayitlari />
      </Tabs.Content>
    </Tabs.Root>
  );
}

// ══════════════════════════════════════════════════════════ kullanıcılar

function Kullanicilar() {
  const masaustu = useIsDesktop();
  const qc = useQueryClient();
  const { bildir } = useToast();
  const { me } = useSession();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [sayfa, setSayfa] = useState(1);
  const [parolaIcin, setParolaIcin] = useState<UserSummary | null>(null);
  const [silinecek, setSilinecek] = useState<UserSummary | null>(null);

  // Arama sunucuda; 300 ms geciktirilir ki her tuş vuruşu istek üretmesin.
  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  /**
   * Form durumu URL'de — birim formuyla aynı gerekçe.
   *
   * Bileşen içinde tutulduğu sürece görsel tur bu ekranı hiç açamıyordu:
   * birim açılır listesinin yetkiliyi göstermediği hata tam da bu yüzden
   * gözden kaçtı. Artık `?bolum=kullanicilar&kullanici=yeni` tek başına
   * gezilebilir.
   */
  const [sorgu, setSorgu] = useSearchParams();
  const formDegeri = sorgu.get('kullanici');

  const formuKapat = () => {
    sorgu.delete('kullanici');
    setSorgu(sorgu, { replace: true });
  };

  const kullanicilar = useQuery({
    queryKey: ['yonetim', 'kullanicilar', sayfa, arama] as const,
    queryFn: () =>
      api.get<PagedResult<UserSummary>>(
        `/yonetim/kullanicilar${queryString({ sayfa, boyut: 50, ara: arama })}`,
      ),
    placeholderData: keepPreviousData,
  });

  const roller = useQuery({
    queryKey: ['yonetim', 'roller'] as const,
    queryFn: () => api.get<Role[]>('/yonetim/roller'),
  });

  const birimler = useQuery({
    queryKey: ['yonetim', 'birimler'] as const,
    queryFn: () => api.get<UnitNode[]>('/yonetim/birimler'),
  });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/yonetim/kullanicilar/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim'] });
      setSilinecek(null);
      bildir('basari', 'Kullanıcı silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  const duzenlenenId =
    formDegeri && formDegeri !== 'yeni' ? Number(formDegeri) : null;

  const duzenlenen = useQuery({
    queryKey: ['yonetim', 'kullanicilar', 'tek', duzenlenenId] as const,
    queryFn: () => api.get<UserSummary>(`/yonetim/kullanicilar/${duzenlenenId}`),
    enabled: duzenlenenId !== null,
  });

  const duzKapsam = useMemo(() => duzles(birimler.data ?? []), [birimler.data]);

  const satirlar = kullanicilar.data?.veriler ?? [];

  // Kayıt gelmeden formu çizmek, alanları boş doldurup kaydete basıldığında
  // mevcut bilgileri silmek demekti.
  const formHazir = formDegeri !== null && !(duzenlenenId !== null && duzenlenen.isLoading);

  return (
    <div className="space-y-3.5">
      {formHazir && (
        <KullaniciFormu
          mevcut={duzenlenen.data ?? null}
          roller={roller.data ?? []}
          birimler={duzKapsam}
          kapat={formuKapat}
        />
      )}

      {parolaIcin && (
        <ParolaFormu kullanici={parolaIcin} kapat={() => setParolaIcin(null)} />
      )}

      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Ad, kullanıcı adı, unvan veya birim ara"
          aria-label="Kullanıcılarda ara"
          ikon={<Search size={15} />}
          className="sm:max-w-[320px] sm:flex-1"
        />
        {/* Ekleme mobilde FAB'da: tam genişlik düğme, listeden bir kayıt
            boyu yer yiyordu ve başparmağın doğal yeri zaten sağ alt köşe. */}
        <Button
          className="hidden sm:ml-auto sm:inline-flex"
          onClick={() => setSorgu({ bolum: 'kullanicilar', kullanici: 'yeni' })}
        >
          <Plus size={14} />
          Kullanıcı ekle
        </Button>
      </div>

      {!masaustu && (
        <Fab
          etiket="Kullanıcı ekle"
          onClick={() => setSorgu({ bolum: 'kullanicilar', kullanici: 'yeni' })}
        />
      )}

      {kullanicilar.isLoading ? (
        <SkeletonRows adet={6} />
      ) : satirlar.length === 0 ? (
        <EmptyState ikon={Users} baslik="Kullanıcı bulunamadı" />
      ) : (
        /*
          TEK YÜZEY, SAÇ TELİ AYIRICI.

          Her kullanıcı ayrı bir karttı ve aralarında 8px boşluk vardı:
          telefonda ekrana altı kayıt sığıyor, listeyi taramak sürekli
          kaydırmak demekti. Uygulamanın geri kalanındaki liste grameriyle
          aynı yüzeye alındı.
        */
        <ul className="divide-y divide-line overflow-hidden rounded-card border border-line bg-surface">
          {satirlar.map((k) => (
            <li key={k.id}>
              <div className="flex items-center gap-3 p-3">
                <span
                  className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-sunken font-display text-xs font-bold text-text-2"
                  aria-hidden
                >
                  {initials(k.ad, k.soyad) || initials(k.kullaniciAdi)}
                </span>

                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold">
                    {[k.ad, k.soyad].filter(Boolean).join(' ') || k.kullaniciAdi}
                    <span className="ml-2 font-normal text-text-3">@{k.kullaniciAdi}</span>
                  </p>
                  <p className="truncate text-xs text-text-3">
                    {[k.unvan, k.birimAdi].filter(Boolean).join(' · ') || '—'}
                  </p>
                  <ul className="mt-1.5 flex flex-wrap gap-1">
                    {(k.roller ?? []).map((r) => (
                      <li
                        key={r}
                        className="rounded-full bg-sunken px-2 py-0.5 text-2xs font-medium text-text-2"
                      >
                        {r}
                      </li>
                    ))}
                  </ul>
                </div>

                {/* Cihaz bağlantısı — jetonun kendisi ASLA gösterilmez. */}
                <div className="hidden shrink-0 gap-1.5 text-text-3 sm:flex">
                  {k.mobilBagli && <Smartphone size={13} aria-label="Mobil cihaz bağlı" />}
                  {k.webBagli && <Monitor size={13} aria-label="Tarayıcı bağlı" />}
                </div>

                {/* Üç ayrı kutu yerine TEK GRUP — satır eylemlerinin
                    uygulama genelindeki gramerı. Silme ayrı tonda. */}
                <RowActions
                  boyut="kucuk"
                  className="shrink-0"
                  eylemler={[
                    { etiket: 'Parola sıfırla', ikon: KeyRound, onClick: () => setParolaIcin(k) },
                    {
                      etiket: 'Düzenle',
                      ikon: Pencil,
                      onClick: () => setSorgu({ bolum: 'kullanicilar', kullanici: String(k.id) }),
                    },
                    ...(k.id !== me?.id
                      ? [{
                          etiket: 'Sil',
                          ikon: Trash2,
                          onClick: () => setSilinecek(k),
                          ton: 'tehlike' as const,
                        }]
                      : []),
                  ]}
                />
              </div>
            </li>
          ))}
        </ul>
      )}

      <Pagination sonuc={kullanicilar.data} sayfaDegistir={setSayfa} birim="kullanıcı" />

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Kullanıcı silinsin mi?"
        aciklama={`"${silinecek?.kullaniciAdi}" hesabı kalıcı olarak silinecek. Bu işlem geri alınamaz.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

function KullaniciFormu({
  mevcut,
  roller,
  birimler,
  kapat,
}: {
  mevcut: UserSummary | null;
  roller: Role[];
  birimler: { id: number; ad: string; etiket: string; derinlik: number }[];
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const { me } = useSession();
  const sistemYetkisi = (me?.roller ?? []).includes('Sistem');

  const [kullaniciAdi, setKullaniciAdi] = useState(mevcut?.kullaniciAdi ?? '');
  const [parola, setParola] = useState('');
  const [ad, setAd] = useState(mevcut?.ad ?? '');
  const [soyad, setSoyad] = useState(mevcut?.soyad ?? '');
  const [unvan, setUnvan] = useState(mevcut?.unvan ?? '');
  const [eposta, setEposta] = useState(mevcut?.eposta ?? '');
  const [telefon, setTelefon] = useState(mevcut?.telefon ?? '');
  const [birimId, setBirimId] = useState<number | ''>(mevcut?.birimId ?? '');
  const [secili, setSecili] = useState<string[]>(mevcut?.roller ?? []);
  const [sahaPersoneli, setSahaPersoneli] = useState(mevcut?.sahaPersoneli ?? false);
  const [smsGonder, setSmsGonder] = useState(true);

  const kaydet = useMutation({
    mutationFn: () => {
      const govde = {
        kullaniciAdi,
        ad: ad || null,
        soyad: soyad || null,
        unvan: unvan || null,
        eposta: eposta || null,
        telefon: telefon || null,
        birimId: birimId === '' ? null : birimId,
        roller: secili,
        sahaPersoneli,
      };
      return mevcut
        ? api.put(`/yonetim/kullanicilar/${mevcut.id}`, govde)
        : api.post('/yonetim/kullanicilar', { ...govde, parola, smsGonder });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim'] });
      bildir('basari', mevcut ? 'Kullanıcı güncellendi' : 'Kullanıcı oluşturuldu');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const gecerli =
    kullaniciAdi.trim().length > 0 &&
    secili.length > 0 &&
    (mevcut !== null || parola.length >= 6) &&
    (!smsGonder || mevcut !== null || telefon.trim().length > 0);

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={mevcut ? 'Kullanıcıyı düzenle' : 'Yeni kullanıcı'}
      ikon={<Users size={15} />}
      genislik="genis"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && kaydet.mutate()}
            disabled={!gecerli || kaydet.isPending}
          >
            Kaydet
          </Button>
        </>
      }
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (gecerli) kaydet.mutate();
        }}
      >
        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Kullanıcı adı" id="k-kadi" zorunlu>
              <Input
                id="k-kadi"
                value={kullaniciAdi}
                onChange={(e) => setKullaniciAdi(e.target.value)}
                autoComplete="off"
                autoFocus
              />
            </FieldWrapper>

            {!mevcut && (
              <FieldWrapper
                etiket="Parola"
                id="k-parola"
                zorunlu
                ipucu="En az 6 karakter. Kullanıcıya SMS ile bildirilir."
              >
                <Input
                  id="k-parola"
                  type="text"
                  value={parola}
                  onChange={(e) => setParola(e.target.value)}
                  autoComplete="new-password"
                />
              </FieldWrapper>
            )}

            <FieldWrapper etiket="Ad" id="k-ad">
              <Input id="k-ad" value={ad} onChange={(e) => setAd(e.target.value)} />
            </FieldWrapper>

            <FieldWrapper etiket="Soyad" id="k-soyad">
              <Input id="k-soyad" value={soyad} onChange={(e) => setSoyad(e.target.value)} />
            </FieldWrapper>

            <FieldWrapper etiket="Unvan" id="k-unvan">
              <Input id="k-unvan" value={unvan} onChange={(e) => setUnvan(e.target.value)} />
            </FieldWrapper>

            <FieldWrapper etiket="E-posta" id="k-eposta">
              <Input
                id="k-eposta"
                type="email"
                value={eposta}
                onChange={(e) => setEposta(e.target.value)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Telefon" id="k-tel" ipucu="05551112233 biçiminde">
              <Input
                id="k-tel"
                type="tel"
                inputMode="tel"
                value={telefon}
                onChange={(e) => setTelefon(e.target.value)}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Birim" id="k-birim">
              <Secim
                id="k-birim"
                value={birimId}
                onChange={(e) => setBirimId(e.target.value === '' ? '' : Number(e.target.value))}
              >
                <option value="">Seçilmedi</option>
                {birimler.map((b) => (
                  <option key={b.id} value={b.id}>
                    {'\u00A0'.repeat(b.derinlik * 3)}
                    {b.etiket}
                  </option>
                ))}
              </Secim>
            </FieldWrapper>
          </div>

          {/* ── Roller ── */}
          <div>
            <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-text-3">
              Roller <span className="text-(--st-no)">*</span>
            </p>
            <ul className="flex flex-wrap gap-2">
              {roller.map((r) => {
                const kilitli = r.korumali && !sistemYetkisi;
                const acik = secili.includes(r.ad!);
                return (
                  <li key={r.ad}>
                    <button
                      type="button"
                      disabled={kilitli}
                      title={
                        kilitli ? 'Bu rolü yalnızca Sistem yetkisi olanlar atayabilir.' : undefined
                      }
                      onClick={() =>
                        setSecili((s) =>
                          s.includes(r.ad!) ? s.filter((x) => x !== r.ad) : [...s, r.ad!],
                        )
                      }
                      className={cn(
                        'inline-flex h-8 items-center gap-1.5 rounded-full border px-3 text-sm font-medium transition-colors',
                        acik
                          ? 'border-brand bg-brand text-on-brand'
                          : 'border-border bg-surface text-text-2 hover:bg-surface-2',
                        kilitli && 'cursor-not-allowed opacity-45',
                      )}
                    >
                      {r.korumali && <ShieldCheck size={11} />}
                      {r.ad}
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>

          {/*
            EK YETKİLER BÖLÜMÜ KALDIRILDI.
            "Gizli etkinlik ekleyebilir" ve "Dosya gönderebilir" artık
            kullanıcıya değil ROLE bağlı: `ajanda.gizliEtkinlik` ve
            `gonderim.gonder` izinleri. Aynı yetkinin iki kaynağı olması,
            rol ekranından kısılan bir iznin kullanıcı kaydından açık kalması
            demekti ve hangisinin geçerli olduğu ekrandan anlaşılmıyordu.
            Sütunlar veritabanında duruyor ama okunmuyor.
          */}

          {/*
            SAHA PERSONELİ ROLE BAĞLANMADI ve bu bilinçli. Yukarıdaki iki alan
            birer YETKİYDİ ve yetkinin kaynağı roldür; bu ise kullanıcıya özel
            bir tercih: aynı rolün iki üyesinden biri sahada olabilir, öteki
            masada. Rolle ifade edilemeyen tek şey bu yüzden burada duruyor.
          */}
          <Switch
            isaretli={sahaPersoneli}
            degistir={setSahaPersoneli}
            etiket="Saha personeli"
            aciklama="Giriş yapınca panele değil saha ekranına iner. Yetkilerini değiştirmez; izinleri varsa panele geçebilir."
          />

          {!mevcut && (
            <Switch
              isaretli={smsGonder}
              degistir={setSmsGonder}
              etiket="Giriş bilgilerini SMS ile gönder"
              aciklama="Telefon numarası gerekir."
            />
          )}
        </div>
      </form>
    </FormModal>
  );
}

function ParolaFormu({ kullanici, kapat }: { kullanici: UserSummary; kapat: () => void }) {
  const { bildir } = useToast();
  const [yeniParola, setYeniParola] = useState('');
  const [smsGonder, setSmsGonder] = useState(true);

  const sifirla = useMutation({
    mutationFn: () =>
      api.post<void>(`/yonetim/kullanicilar/${kullanici.id}/parola`, { yeniParola, smsGonder }),
    onSuccess: () => {
      bildir('basari', 'Parola sıfırlandı');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Parola sıfırlanamadı', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Parola sıfırla"
      aciklama={`@${kullanici.kullaniciAdi} hesabı için yeni parola belirleyin.`}
      ikon={<KeyRound size={15} />}
      genislik="dar"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => yeniParola.length >= 6 && sifirla.mutate()}
            disabled={yeniParola.length < 6 || sifirla.isPending}
          >
            Parolayı değiştir
          </Button>
        </>
      }
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (yeniParola.length >= 6) sifirla.mutate();
        }}
      >
        <div className="space-y-4">
          <FieldWrapper etiket="Yeni parola" id="p-yeni" zorunlu ipucu="En az 6 karakter.">
            <Input
              id="p-yeni"
              type="text"
              value={yeniParola}
              onChange={(e) => setYeniParola(e.target.value)}
              autoComplete="new-password"
              autoFocus
            />
          </FieldWrapper>

          <Switch
            isaretli={smsGonder}
            degistir={setSmsGonder}
            etiket="Yeni parolayı SMS ile bildir"
          />
        </div>
      </form>
    </FormModal>
  );
}

// ══════════════════════════════════════════════════════════════ birimler

function Birimler() {
  // Ekleme düğmeleri izne bağlı.
  const { hasPermission } = useSession();

  const qc = useQueryClient();
  const { bildir } = useToast();
  const [silinecek, setSilinecek] = useState<UnitNode | null>(null);

  /**
   * Form durumu URL'de (`?birim=12` / `?birim=yeni`).
   *
   * Birim detay sayfasındaki "Düzenle" düğmesi buraya dönüyor; durumu
   * bileşen içinde tutsaydık geri gelen kullanıcı formu kapalı bulurdu.
   */
  const [sorgu, setSorgu] = useSearchParams();
  const formDegeri = sorgu.get('birim');
  const formAcik = formDegeri !== null;

  const birimler = useQuery({
    queryKey: ['yonetim', 'birimler'] as const,
    queryFn: () => api.get<UnitNode[]>('/yonetim/birimler'),
  });

  const duz = useMemo(() => duzles(birimler.data ?? []), [birimler.data]);
  const duzenlenen =
    formDegeri && formDegeri !== 'yeni'
      ? bul(birimler.data ?? [], Number(formDegeri))
      : null;

  const formuKapat = () => {
    sorgu.delete('birim');
    setSorgu(sorgu, { replace: true });
  };

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/yonetim/birimler/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim'] });
      setSilinecek(null);
      bildir('basari', 'Birim silindi');
    },
    // Sunucu "alt birimi var" / "kullanıcısı var" gibi anlaşılır Türkçe
    // mesajlar döndürüyor; olduğu gibi gösterilir.
    onError: (h: Error) => bildir('hata', 'Birim silinemedi', h.message),
  });

  if (birimler.isLoading) return <SkeletonRows adet={6} />;

  return (
    <div className="space-y-3.5">
      {formAcik && (
        <BirimFormu
          mevcut={duzenlenen}
          birimler={duz}
          vazgec={formuKapat}
          bitti={() => {
            qc.invalidateQueries({ queryKey: ['yonetim'] });
            formuKapat();
          }}
        />
      )}

      <div className="flex items-center justify-between gap-2.5">
        <p className="text-sm text-text-3">
          {number(duz.length)} birim · satıra tıklayarak detayını açın
        </p>
        <Button onClick={() => setSorgu({ bolum: 'birimler', birim: 'yeni' })}>
          <Plus size={14} />
          Yeni birim
        </Button>
      </div>

      {(birimler.data ?? []).length === 0 ? (
        <EmptyState
          ikon={Building2}
          baslik="Birim yok"
          aciklama="İlk birimi ekleyerek başlayın."
          eylem={
            // Boş durumdaki EKLEME düğmesi de izin ister; araç çubuğundaki
            // düğme kapıdan geçiyordu ama liste boşken çizilen bu ikinci
            // düğme kapının dışında kalmıştı.
            hasPermission(PERMISSION.yonetimBirim) ? (
              <Button onClick={() => setSorgu({ bolum: 'birimler', birim: 'yeni' })}>
                <Plus size={14} />
                Yeni birim
              </Button>
            ) : undefined
          }
        />
      ) : (
        <Card className="p-2">
          <BirimAgaci
            dugumler={birimler.data ?? []}
            derinlik={0}
            sil={setSilinecek}
            duzenle={(b) => setSorgu({ bolum: 'birimler', birim: String(b.id) })}
          />
        </Card>
      )}

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Birim silinsin mi?"
        aciklama={`"${unitLabel(silinecek)}" birimi silinecek.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

function BirimAgaci({
  dugumler,
  derinlik,
  sil,
  duzenle,
}: {
  dugumler: UnitNode[];
  derinlik: number;
  sil: (b: UnitNode) => void;
  duzenle: (b: UnitNode) => void;
}) {
  return (
    <ul>
      {dugumler.map((b) => (
        <li key={b.id}>
          <div
            className="group flex items-center gap-2.5 rounded-md hover:bg-surface-2"
            style={{ paddingLeft: derinlik * 18 }}
          >
            <Link
              to={`/yonetim/birimler/${b.id}`}
              className="flex min-w-0 flex-1 items-center gap-2.5 px-2 py-2 text-text"
            >
              <Building2 size={14} className="shrink-0 text-text-3" />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{b.ad}</p>
                <p className="truncate text-xs text-text-3">
                  {b.yetkili}
                  {b.unvan ? ` · ${b.unvan}` : ''}
                </p>
              </div>
              <span className="shrink-0 rounded-full bg-sunken px-2 py-0.5 text-2xs tabular-nums text-text-3">
                {number(b.kullaniciSayisi)} kişi
              </span>
            </Link>

            <div className="flex shrink-0 items-center gap-0.5 pr-1">
              <IconButton etiket="Düzenle" onClick={() => duzenle(b)}>
                <Pencil size={14} />
              </IconButton>
              <IconButton etiket="Sil" onClick={() => sil(b)} className="hover:text-(--st-no)">
                <Trash2 size={14} />
              </IconButton>
            </div>
          </div>

          {(b.altBirimler ?? []).length > 0 && (
            <BirimAgaci
              dugumler={b.altBirimler ?? []}
              derinlik={derinlik + 1}
              sil={sil}
              duzenle={duzenle}
            />
          )}
        </li>
      ))}
    </ul>
  );
}

/** Ağaçta kimliğe göre düğüm arar. */
function bul(dugumler: UnitNode[], id: number): UnitNode | null {
  for (const b of dugumler) {
    if (b.id === id) return b;
    const alt = bul(b.altBirimler ?? [], id);
    if (alt) return alt;
  }
  return null;
}

/**
 * Birim ekleme / düzenleme.
 *
 * <p>
 * Üst birim seçiminde <b>kendisi ve alt ağacı listelenmez</b>: bir birimi
 * kendi altına almak ağacı döngüye sokar ve liste sonsuza kadar
 * özyinelenirdi.
 * </p>
 */
function BirimFormu({
  mevcut,
  birimler,
  vazgec,
  bitti,
}: {
  mevcut: UnitNode | null;
  birimler: { id: number; ad: string; etiket: string; derinlik: number }[];
  vazgec: () => void;
  bitti: () => void;
}) {
  const { bildir } = useToast();

  const [ad, setAd] = useState(mevcut?.ad ?? '');
  const [yetkili, setYetkili] = useState(mevcut?.yetkili ?? '');
  const [unvan, setUnvan] = useState(mevcut?.unvan ?? '');
  const [telefon, setTelefon] = useState(mevcut?.telefon ?? '');
  const [eposta, setEposta] = useState(mevcut?.eposta ?? '');
  const [adres, setAdres] = useState(mevcut?.adres ?? '');
  const [aciklama, setAciklama] = useState(mevcut?.aciklama ?? '');
  const [ustBirimId, setUstBirimId] = useState<string>(
    mevcut?.ustBirimId != null ? String(mevcut.ustBirimId) : '',
  );

  // Kendisi ve altları eleniyor — döngüyü baştan imkânsız kılar.
  const yasakli = useMemo(() => {
    if (!mevcut?.id) return new Set<number>();
    const kume = new Set<number>();
    const gez = (d: UnitNode) => {
      if (d.id) kume.add(d.id);
      (d.altBirimler ?? []).forEach(gez);
    };
    gez(mevcut);
    return kume;
  }, [mevcut]);

  const kaydet = useMutation({
    mutationFn: (govde: Record<string, unknown>) =>
      mevcut?.id
        ? api.put(`/yonetim/birimler/${mevcut.id}`, govde)
        : api.post('/yonetim/birimler', govde),
    onSuccess: () => {
      bildir('basari', mevcut ? 'Birim güncellendi' : 'Birim eklendi');
      bitti();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const gonder = () => {
    if (!ad.trim() || !yetkili.trim()) return;
    kaydet.mutate({
      ad: ad.trim(),
      yetkili: yetkili.trim(),
      unvan: unvan.trim() || null,
      telefon: telefon.trim() || null,
      eposta: eposta.trim() || null,
      adres: adres.trim() || null,
      aciklama: aciklama.trim() || null,
      ustBirimId: ustBirimId ? Number(ustBirimId) : null,
    });
  };

  return (
    <FormModal
      acik
      kapat={vazgec}
      baslik={mevcut ? 'Birimi düzenle' : 'Yeni birim'}
      aciklama="Yetkili adı listelerde birim adının yanında görünür."
      ikon={<Building2 size={15} />}
      genislik="genis"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={vazgec}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={gonder}
            disabled={!ad.trim() || !yetkili.trim() || kaydet.isPending}
          >
            {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </Button>
        </>
      }
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          gonder();
        }}
      >
        <div className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Birim adı" id="b-ad" zorunlu>
              <Input id="b-ad" value={ad} onChange={(e) => setAd(e.target.value)} autoFocus maxLength={200} />
            </FieldWrapper>

            <FieldWrapper etiket="Üst birim" id="b-ust">
              <Secim id="b-ust" value={ustBirimId} onChange={(e) => setUstBirimId(e.target.value)}>
                <option value="">— Yok (kök birim) —</option>
                {birimler
                  .filter((b) => !yasakli.has(b.id))
                  .map((b) => (
                    <option key={b.id} value={b.id}>
                      {'\u00A0'.repeat(b.derinlik * 3)}
                      {b.etiket}
                    </option>
                  ))}
              </Secim>
            </FieldWrapper>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Yetkili" id="b-yetkili" zorunlu>
              <Input
                id="b-yetkili"
                value={yetkili}
                onChange={(e) => setYetkili(e.target.value)}
                maxLength={150}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Unvan" id="b-unvan">
              <Input id="b-unvan" value={unvan} onChange={(e) => setUnvan(e.target.value)} maxLength={150} />
            </FieldWrapper>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Telefon" id="b-telefon">
              <Input
                id="b-telefon"
                value={telefon}
                onChange={(e) => setTelefon(e.target.value)}
                inputMode="tel"
                maxLength={30}
              />
            </FieldWrapper>

            <FieldWrapper etiket="E-posta" id="b-eposta">
              <Input
                id="b-eposta"
                type="email"
                value={eposta}
                onChange={(e) => setEposta(e.target.value)}
                maxLength={150}
              />
            </FieldWrapper>
          </div>

          <FieldWrapper etiket="Adres" id="b-adres">
            <Input id="b-adres" value={adres} onChange={(e) => setAdres(e.target.value)} maxLength={300} />
          </FieldWrapper>

          <FieldWrapper etiket="Açıklama" id="b-aciklama">
            <Textarea id="b-aciklama" value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
          </FieldWrapper>
        </div>
      </form>
    </FormModal>
  );
}

/** Ağacı `<select>` için düz listeye çevirir; girinti derinlikten gelir. */
/**
 * Birim ağacını açılır liste için düzleştirir.
 *
 * `etiket` yetkiliyi de taşır: kurumda altı ayrı "Başkan Yardımcısı" birimi
 * var ve yalnızca adla listelendiğinde hangisinin seçildiği anlaşılmıyordu.
 */
function duzles(
  dugumler: UnitNode[],
  derinlik = 0,
): { id: number; ad: string; etiket: string; derinlik: number }[] {
  return dugumler.flatMap((b) => [
    { id: b.id!, ad: b.ad!, etiket: unitLabel(b), derinlik },
    ...duzles(b.altBirimler ?? [], derinlik + 1),
  ]);
}

// ═════════════════════════════════════════════════════════════════ roller

/**
 * Roller ve izinleri.
 *
 * <p>
 * Rol artık yalnızca bir kap; <b>ne yapabildiğini izinleri belirler</b>.
 * Önceden yetki dağılımı <c>PolicyRegistrar.cs</c> içinde sabit rol
 * listeleriydi ve her değişiklik bir yayın demekti.
 * </p>
 */
function Roller() {
  const masaustuRol = useIsDesktop();
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [formAcik, setFormAcik] = useState(false);
  const [duzenlenen, setDuzenlenen] = useState<Role | null>(null);
  const [izinRolu, setIzinRolu] = useState<Role | null>(null);
  const [silinecek, setSilinecek] = useState<Role | null>(null);

  const roller = useQuery({
    queryKey: ['yonetim', 'roller'] as const,
    queryFn: () => api.get<Role[]>('/yonetim/roller'),
  });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/yonetim/roller/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim', 'roller'] });
      setSilinecek(null);
      bildir('basari', 'Rol silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  if (roller.isLoading) return <SkeletonRows adet={5} />;

  return (
    <div className="space-y-3.5">
      {formAcik && (
        <RolFormu
          mevcut={duzenlenen}
          kapat={() => {
            setFormAcik(false);
            setDuzenlenen(null);
          }}
        />
      )}

      {izinRolu && (
        <RolIzinleriModal rol={izinRolu} kapat={() => setIzinRolu(null)} />
      )}

      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center">
        <div>
          <h2 className="font-display text-lg font-bold">Roller</h2>
          <p className="text-sm text-text-3">
            Role bir kaptır; <b>ne yapabildiğini izinleri belirler</b>.
          </p>
        </div>
        <Button
          className="hidden sm:ml-auto sm:inline-flex"
          onClick={() => {
            setDuzenlenen(null);
            setFormAcik(true);
          }}
        >
          <Plus size={14} />
          Role ekle
        </Button>
      </div>

      {!masaustuRol && (
        <Fab
          etiket="Rol ekle"
          onClick={() => {
            setDuzenlenen(null);
            setFormAcik(true);
          }}
        />
      )}

      {/* Mobilde tek yüzey: her rol ayrı bir kart olunca 12 rol 4800px'lik
          bir sayfaya çıkıyordu. Masaüstünde ızgara duruyor. */}
      <ul className="overflow-hidden rounded-card border border-line bg-surface divide-y divide-line
                     sm:grid sm:gap-2.5 sm:divide-y-0 sm:border-0 sm:bg-transparent sm:grid-cols-2 lg:grid-cols-3">
        {(roller.data ?? []).map((r) => (
          <li key={r.ad}>
            <div className="flex h-full flex-col p-3.5 sm:rounded-card sm:border sm:border-line sm:bg-surface sm:shadow-1">
              <div className="flex items-start gap-3">
                <span
                  className={cn(
                    'grid h-9 w-9 shrink-0 place-items-center rounded-md',
                    r.korumali ? 'bg-(--gold-tint) text-(--gold-strong)' : 'bg-sunken text-text-3',
                  )}
                  aria-hidden
                >
                  <ShieldCheck size={16} />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold">{r.ad}</p>
                  <p className="text-xs text-text-3">
                    {number(r.kullaniciSayisi)} kullanıcı · {number(r.izinSayisi)} yetki
                    {r.korumali && ' · korumalı'}
                  </p>
                </div>
              </div>

              {/* Açıklama ZORUNLU bir alan değil ama olmadığında rol adı tek
                  başına ne yapabildiğini söylemiyor: "BaskanOzel" nedir? */}
              <p className="mt-2 line-clamp-2 min-h-[32px] text-xs leading-[1.45] text-text-2">
                {r.aciklama || (
                  <span className="text-text-3">Açıklama girilmemiş.</span>
                )}
              </p>

              <div className="mt-2.5 flex items-center gap-1.5 border-t border-border pt-2.5">
                <Button
                  varyant="ikincil"
                  className="h-8 flex-1 px-2 text-xs"
                  onClick={() => setIzinRolu(r)}
                >
                  <KeyRound size={13} />
                  Yetkiler
                </Button>
                <Link
                  to={`/yonetim/roller/${encodeURIComponent(r.ad!)}`}
                  className="flex-1"
                >
                  <Button varyant="sade" className="h-8 w-full px-2 text-xs">
                    <Users size={13} />
                    Kullanıcılar
                  </Button>
                </Link>
                {/* Korumalı roller sistemin kendi rolleri; sunucu da reddeder
                    ama silme düğmesini göstermek boşuna bir hata mesajı
                    demekti. */}
                <RowActions
                  boyut="kucuk"
                  className="shrink-0"
                  eylemler={[
                    {
                      etiket: 'Düzenle',
                      ikon: Pencil,
                      onClick: () => {
                        setDuzenlenen(r);
                        setFormAcik(true);
                      },
                    },
                    ...(!r.korumali
                      ? [{
                          etiket: 'Sil',
                          ikon: Trash2,
                          onClick: () => setSilinecek(r),
                          ton: 'tehlike' as const,
                        }]
                      : []),
                  ]}
                />
              </div>
            </div>
          </li>
        ))}
      </ul>

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Rol silinsin mi?"
        aciklama={`"${silinecek?.ad}" rolü ve yetki bağları kaldırılacak.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

/** Rol oluşturma / açıklama düzenleme. */
function RolFormu({ mevcut, kapat }: { mevcut: Role | null; kapat: () => void }) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [ad, setAd] = useState(mevcut?.ad ?? '');
  const [aciklama, setAciklama] = useState(mevcut?.aciklama ?? '');

  const kaydet = useMutation({
    mutationFn: () =>
      mevcut
        ? api.put<Role>(`/yonetim/roller/${mevcut.id}`, { ad: mevcut.ad, aciklama })
        : api.post<Role>('/yonetim/roller', { ad: ad.trim(), aciklama }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim', 'roller'] });
      bildir(
        'basari',
        mevcut ? 'Rol güncellendi' : 'Rol oluşturuldu',
        mevcut ? undefined : 'Şimdi "Yetkiler" ile ne yapabileceğini seçin.',
      );
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const gecerli = mevcut !== null || ad.trim().length >= 2;

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={mevcut ? 'Rolü düzenle' : 'Yeni rol'}
      aciklama={
        mevcut
          ? 'Rol adı değiştirilemez; yalnızca açıklama güncellenir.'
          : 'Rol izinsiz doğar — oluşturduktan sonra yetkilerini seçin.'
      }
      ikon={<ShieldCheck size={15} />}
      genislik="orta"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && kaydet.mutate()}
            disabled={!gecerli || kaydet.isPending}
          >
            Kaydet
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <FieldWrapper
          etiket="Rol adı"
          id="r-ad"
          zorunlu
          ipucu={
            mevcut
              ? 'Ad DEĞİŞTİRİLEMEZ: kodda ve eski sayfalarda bu adla eşleşen denetimler var.'
              : 'Boşluksuz, kısa bir ad seçin — örn. TalepPersoneli.'
          }
        >
          <Input
            id="r-ad"
            value={ad}
            onChange={(e) => setAd(e.target.value)}
            disabled={mevcut !== null}
            autoFocus={mevcut === null}
            maxLength={60}
          />
        </FieldWrapper>

        <FieldWrapper
          etiket="Açıklama"
          id="r-aciklama"
          ipucu="Bu rolün kime verileceğini ve ne yaptığını yazın; rol adı tek başına söylemiyor."
        >
          <Textarea
            id="r-aciklama"
            value={aciklama}
            onChange={(e) => setAciklama(e.target.value)}
            maxLength={300}
            autoFocus={mevcut !== null}
          />
        </FieldWrapper>
      </div>
    </FormModal>
  );
}

/**
 * Rolün izinlerini seçme.
 *
 * <p>
 * Onay kutularıyla çalışır ve "kaydet" dendiğinde <b>ekrandaki durum ne ise o
 * yazılır</b>. Tek tek ekle/çıkar uçları, iki sekmede açık iki yöneticinin
 * birbirinin değişikliğini sessizce geri alması demekti.
 * </p>
 */
function RolIzinleriModal({ rol, kapat }: { rol: Role; kapat: () => void }) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const katalog = useQuery({
    queryKey: ['yonetim', 'izinler'] as const,
    queryFn: () => api.get<PermissionRecord[]>('/yonetim/izinler'),
    staleTime: 5 * 60_000,
  });

  const mevcut = useQuery({
    queryKey: ['yonetim', 'roller', rol.id, 'izinler'] as const,
    queryFn: () => api.get<string[]>(`/yonetim/roller/${rol.id}/izinler`),
  });

  const [secili, setSecili] = useState<string[] | null>(null);
  const liste = secili ?? mevcut.data ?? [];

  const kaydet = useMutation({
    mutationFn: () =>
      api.put<void>(`/yonetim/roller/${rol.id}/izinler`, { izinler: liste }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim'] });
      bildir('basari', 'Yetkiler kaydedildi', `${rol.ad} rolü güncellendi.`);
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  function degistir(ad: string) {
    setSecili(liste.includes(ad) ? liste.filter((x) => x !== ad) : [...liste, ad]);
  }

  // Katalog sunucudan SIRALI geliyor; gruplar da o sırayı korur.
  const gruplar = (katalog.data ?? []).reduce<Record<string, PermissionRecord[]>>((h, i) => {
    (h[i.grup!] ??= []).push(i);
    return h;
  }, {});

  const yukleniyor = katalog.isLoading || mevcut.isLoading;

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={`${rol.ad} — yetkiler`}
      aciklama="İşaretli olanlar bu rolün yapabilecekleri."
      ikon={<KeyRound size={15} />}
      genislik="genis"
      altBilgi={`${liste.length} yetki seçili`}
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => kaydet.mutate()}
            disabled={yukleniyor || kaydet.isPending}
          >
            Kaydet
          </Button>
        </>
      }
    >
      {yukleniyor ? (
        <SkeletonRows adet={8} />
      ) : (
        <div className="space-y-4">
          {Object.entries(gruplar).map(([grup, izinler]) => {
            const hepsiSecili = izinler.every((i) => liste.includes(i.ad!));
            return (
              <div key={grup}>
                <div className="mb-1.5 flex items-center justify-between gap-3">
                  <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
                    {grup}
                  </p>
                  {/* Grup başına toplu seçim: ajanda modülünün 12 izni var ve
                      tek tek işaretlemek rol kurmayı bir sabır sınavına
                      çeviriyordu. */}
                  <button
                    type="button"
                    onClick={() =>
                      setSecili(
                        hepsiSecili
                          ? liste.filter((x) => !izinler.some((i) => i.ad === x))
                          : [...new Set([...liste, ...izinler.map((i) => i.ad!)])],
                      )
                    }
                    className="text-xs font-medium text-brand-2 hover:underline"
                  >
                    {hepsiSecili ? 'Hiçbiri' : 'Tümü'}
                  </button>
                </div>

                <div className="space-y-1 rounded-control border border-border bg-surface-2 p-2.5">
                  {izinler.map((i) => (
                    <Switch
                      key={i.ad}
                      isaretli={liste.includes(i.ad!)}
                      degistir={() => degistir(i.ad!)}
                      etiket={
                        <span className={cn(!i.kullanimda && 'text-text-3 line-through')}>
                          {i.baslik}
                        </span>
                      }
                      aciklama={i.aciklama}
                    />
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </FormModal>
  );
}

// ═════════════════════════════════════════════════ oturum kayıtları

/**
 * Kullanıcı giriş / çıkış denetim kayıtları.
 *
 * <p>
 * Sistem iki yıldır canlıda ve bugüne kadar kimin ne zaman girdiğine dair
 * <b>hiçbir kayıt tutulmuyordu</b>. Gizli etkinlik taşıyan bir sistemde
 * "bu kayda kim baktı" sorusunun ilk adımı budur.
 * </p>
 *
 * <p>
 * Başarısız denemeler de listelenir: arka arkaya gelen başarısızlık, hesap
 * kilitlenmeden önce görülmesi gereken tek sinyaldir.
 * </p>
 */
function OturumKayitlari() {
  const [sayfa, setSayfa] = useState(1);
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [ara, setAra] = useState('');
  const [yalnizcaBasarisiz, setYalnizcaBasarisiz] = useState(false);

  const [ipGirdisi, setIpGirdisi] = useState('');
  const [ip, setIp] = useState('');
  const [kullaniciId, setKullaniciId] = useState('');
  const [baslangic, setBaslangic] = useState('');
  const [bitis, setBitis] = useState('');

  const kullanicilar = useQuery({
    queryKey: ['yonetim', 'kullanicilar', 'suzgec'] as const,
    queryFn: () =>
      api.get<PagedResult<UserSummary>>(
        `/yonetim/kullanicilar${queryString({ sayfa: 1, boyut: 500 })}`,
      ),
  });

  useEffect(() => {
    const z = setTimeout(() => {
      setAra(aramaGirdisi);
      setIp(ipGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi, ipGirdisi]);

  const kayitlar = useQuery({
    queryKey: [
      'oturum', 'kayitlar', sayfa, ara, yalnizcaBasarisiz, ip, kullaniciId, baslangic, bitis,
    ] as const,
    queryFn: () =>
      api.get<PagedResult<SessionRecord>>(
        `/oturum/kayitlar${queryString({
          sayfa,
          boyut: 50,
          ara,
          basarili: yalnizcaBasarisiz ? false : undefined,
          ipAdresi: ip || undefined,
          kullaniciId: kullaniciId ? Number(kullaniciId) : undefined,
          // Gün seçilir, aralık günün tamamını kapsar: 14:00'te "bugün"ü
          // süzen kullanıcı öğleden sonraki kayıtları da görmeli.
          baslangic: baslangic ? `${baslangic}T00:00:00` : undefined,
          bitis: bitis ? `${bitis}T23:59:59` : undefined,
        })}`,
      ),
    placeholderData: keepPreviousData,
  });

  const satirlar = kayitlar.data?.veriler ?? [];
  const suzgecVar =
    ara !== '' || ip !== '' || kullaniciId !== '' || baslangic !== '' || bitis !== '' || yalnizcaBasarisiz;

  const temizle = () => {
    setAramaGirdisi('');
    setIpGirdisi('');
    setKullaniciId('');
    setBaslangic('');
    setBitis('');
    setYalnizcaBasarisiz(false);
    setSayfa(1);
  };

  return (
    <div className="space-y-3.5">
      <Card className="space-y-3 p-3.5">
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <FieldWrapper etiket="Ara" id="o-ara">
            <SearchInput
              id="o-ara"
              value={aramaGirdisi}
              onChange={(e) => setAramaGirdisi(e.target.value)}
              placeholder="Kullanıcı adı, ad soyad"
              aria-label="Oturum kayıtlarında ara"
              ikon={<Search size={15} />}
            />
          </FieldWrapper>

          <FieldWrapper etiket="Kullanıcı" id="o-kullanici">
            <Secim
              id="o-kullanici"
              value={kullaniciId}
              onChange={(e) => {
                setKullaniciId(e.target.value);
                setSayfa(1);
              }}
            >
              <option value="">— Tümü —</option>
              {(kullanicilar.data?.veriler ?? []).map((k) => (
                <option key={k.id} value={k.id}>
                  {`${k.ad ?? ''} ${k.soyad ?? ''}`.trim() || k.kullaniciAdi} (@{k.kullaniciAdi})
                </option>
              ))}
            </Secim>
          </FieldWrapper>

          {/* Ön ek eşleşiyor: "192.168." yazmak bir ağ bloğunu süzer. */}
          <FieldWrapper etiket="IP adresi" id="o-ip">
            <Input
              id="o-ip"
              value={ipGirdisi}
              onChange={(e) => setIpGirdisi(e.target.value)}
              placeholder="Tam ya da ön ek — 192.168."
              inputMode="numeric"
            />
          </FieldWrapper>

          <div className="grid grid-cols-2 gap-2">
            <FieldWrapper etiket="Başlangıç" id="o-bas">
              <DatePicker
                id="o-bas"
                deger={baslangic}
                degistir={(d) => {
                  setBaslangic(d);
                  setSayfa(1);
                }}
                temizlenebilir
              />
            </FieldWrapper>
            <FieldWrapper etiket="Bitiş" id="o-bit">
              <DatePicker
                id="o-bit"
                deger={bitis}
                degistir={(d) => {
                  setBitis(d);
                  setSayfa(1);
                }}
                temizlenebilir
              />
            </FieldWrapper>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            varyant={yalnizcaBasarisiz ? 'birincil' : 'ikincil'}
            onClick={() => {
              setYalnizcaBasarisiz((b) => !b);
              setSayfa(1);
            }}
          >
            Yalnızca başarısız
          </Button>

          {suzgecVar && (
            <Button varyant="sade" onClick={temizle}>
              <X size={14} />
              Süzgeçleri temizle
            </Button>
          )}

          <span className="ml-auto text-xs tabular-nums text-text-3">
            {number(kayitlar.data?.toplam ?? 0)} kayıt
          </span>
        </div>
      </Card>

      {kayitlar.isLoading ? (
        <SkeletonRows adet={8} />
      ) : satirlar.length === 0 ? (
        <EmptyState ikon={History} baslik="Kayıt yok" />
      ) : (
        <>
          <Card className="divide-y divide-border">
            {satirlar.map((k) => (
              <div key={k.id} className="flex items-center gap-3 px-3.5 py-2.5">
                <span
                  className="h-8 w-[3px] shrink-0 rounded-full"
                  style={{ background: k.basarili ? 'var(--st-ok)' : 'var(--st-no)' }}
                  aria-hidden
                />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">
                    {k.adSoyad || k.kullaniciAdi}
                    <span className="ml-2 font-normal text-text-3">@{k.kullaniciAdi}</span>
                  </p>
                  <p className="truncate text-xs text-text-3">
                    {k.olay}
                    {k.aciklama ? ` · ${k.aciklama}` : ''}
                    {k.ipAdresi ? ` · ${k.ipAdresi}` : ''}
                  </p>
                </div>
                <span
                  className="shrink-0 rounded-full px-2 py-0.5 text-2xs font-semibold"
                  style={
                    k.basarili
                      ? { color: 'var(--st-ok)', background: 'var(--st-ok-bg)' }
                      : { color: 'var(--st-no)', background: 'var(--st-no-bg)' }
                  }
                >
                  {k.basarili ? 'Başarılı' : 'Başarısız'}
                </span>
                <time className="hidden shrink-0 text-xs tabular-nums text-text-3 sm:block">
                  {dateTime(k.tarih)}
                </time>
              </div>
            ))}
          </Card>

          <Pagination sonuc={kayitlar.data} sayfaDegistir={setSayfa} birim="kayıt" />
        </>
      )}
    </div>
  );
}
