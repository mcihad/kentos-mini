import * as Dialog from '@radix-ui/react-dialog';
import { useSession } from '../auth/SessionProvider';
import { PERMISSION } from '../components/permissions';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft, ChevronDown, FileDown, MapPin, MessageSquare, Phone, Plus,
  Trash2, UserPlus, Users, X,
  Check,
  SquarePen, Scissors,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { SearchInput, Textarea } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { ActionSheet } from '../components/ActionSheet';
import { NameCardExport } from './protocol/NameCardExport';
import { useIsDesktop } from '../components/screenSize';
import { BottomSheet, SheetDivider, SheetHeading, SheetRow } from '../shell/mobile/BottomSheet';
import { Skeleton } from '../components/Skeleton';
import { Card, CardHeader } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { date } from '../data/format';
import { download } from '../data/download';
import { api, queryString, type PagedResult } from '../data/client';

/** Sunucudaki `DavetDurumu` ile AYNI sayısal karşılıklar. */
const DURUM = {
  beklemede: 0,
  katilacak: 1,
  katilmayacak: 2,
  ulasilamadi: 3,
} as const;

const DURUM_SECENEKLERI = [
  { d: DURUM.beklemede, e: 'Beklemede', renk: 'wait' },
  { d: DURUM.katilacak, e: 'Katılacak', renk: 'ok' },
  { d: DURUM.katilmayacak, e: 'Katılmayacak', renk: 'no' },
  { d: DURUM.ulasilamadi, e: 'Ulaşılamadı', renk: 'cancel' },
] as const;

type DavetKisi = {
  id: number;
  protokolId: number;
  adSoyad: string;
  unvan?: string | null;
  kurum?: string | null;
  kategori: string;
  telefon?: string | null;
  cepTelefon?: string | null;
  durum: number;
  arandi: boolean;
  mesajGonderildi: boolean;
  not?: string | null;
};

type DavetDetayi = {
  id: number;
  baslik: string;
  tarih?: string | null;
  yer?: string | null;
  aciklama?: string | null;
  kisiSayisi: number;
  katilacak: number;
  katilmayacak: number;
  beklemede: number;
  arandi: number;
  kisiler: DavetKisi[];
};

type Protocol = {
  id: number;
  kategoriId: number;
  kategori: string;
  adSoyad: string;
  unvan?: string | null;
  kurum?: string | null;
};

type Kategori = { id: number; ad: string; adet: number };

/**
 * Davet detayı — kişiler ve takip.
 *
 * <p>
 * Takip iki ayrı eksende: <b>eylem</b> (arandı / mesaj gönderildi) ve
 * <b>cevap</b> (katılacak / katılmayacak / ulaşılamadı). Tek bir listeye
 * sıkıştırılsaydı "arandı ama henüz cevap yok" ile "hiç aranmadı" ayırt
 * edilemezdi — oysa listeyi takip edenin ilk sorusu tam olarak bu.
 * </p>
 */
export default function InvitationDetail() {
  const masaustu = useIsDesktop();
  /** Mobilde eylemleri açılan kişi. */
  const [secilenKisi, setSecilenKisi] = useState<DavetKisi | null>(null);
  const [ciktiAcik, setCiktiAcik] = useState(false);
  const [kartAcik, setKartAcik] = useState(false);
  // Ekleme düğmeleri izne bağlı.
  const { hasPermission } = useSession();

  const { id } = useParams();
  const davetId = Number(id);
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();

  const [kisiEkle, setKisiEkle] = useState(false);
  const [silinecek, setSilinecek] = useState(false);
  const [notYazilan, setNotYazilan] = useState<DavetKisi | null>(null);

  const davet = useQuery({
    queryKey: ['davet', 'detay', davetId] as const,
    queryFn: () => api.get<DavetDetayi>(`/davet/${davetId}`),
    enabled: Number.isFinite(davetId) && davetId > 0,
  });

  function tazele() {
    qc.invalidateQueries({ queryKey: ['davet'] });
  }

  const kisiGuncelle = useMutation({
    mutationFn: (v: { kisiId: number; yama: Record<string, unknown> }) =>
      api.put<DavetKisi>(`/davet/${davetId}/kisi/${v.kisiId}`, v.yama),
    onSuccess: tazele,
    onError: (h: Error) => bildir('hata', 'Güncellenemedi', h.message),
  });

  const kisiCikar = useMutation({
    mutationFn: (kisiId: number) => api.delete<void>(`/davet/${davetId}/kisi/${kisiId}`),
    onSuccess: () => {
      tazele();
      bildir('basari', 'Kişi listeden çıkarıldı');
    },
    onError: (h: Error) => bildir('hata', 'Çıkarılamadı', h.message),
  });

  const sil = useMutation({
    mutationFn: () => api.delete<void>(`/davet/${davetId}`),
    onSuccess: () => {
      tazele();
      bildir('basari', 'Davet silindi');
      gezin('/davetler');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  if (davet.isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-7 w-1/2" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (davet.isError || !davet.data) {
    return (
      <EmptyState
        ikon={Users}
        baslik="Davet bulunamadı"
        aciklama={(davet.error as Error)?.message}
        eylem={
          <Link to="/davetler">
            <Button varyant="ikincil">Davetlere dön</Button>
          </Link>
        }
      />
    );
  }

  const d = davet.data;

  // Kategoriye göre grupla — protokol listesi de böyle okunuyor.
  const gruplar = new Map<string, DavetKisi[]>();
  for (const k of d.kisiler) {
    (gruplar.get(k.kategori) ?? gruplar.set(k.kategori, []).get(k.kategori)!).push(k);
  }

  return (
    <div className="space-y-4">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-3">
        <Link to="/davetler" className="mt-0.5 shrink-0">
          <IconButton etiket="Geri">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-xl font-bold tracking-[-0.015em] md:text-2xl">
            {d.baslik}
          </h2>
          <p className="flex flex-wrap items-center gap-x-3 text-sm text-text-3">
            {d.tarih && <span>{date(d.tarih)}</span>}
            {d.yer && (
              <span className="inline-flex items-center gap-1">
                <MapPin size={11} />
                {d.yer}
              </span>
            )}
            <span>{d.kisiSayisi} kişi</span>
          </p>
        </div>
        <IconButton
          etiket="Daveti sil"
          onClick={() => setSilinecek(true)}
          className="hidden md:inline-grid"
        >
          <Trash2 size={16} />
        </IconButton>
      </div>

      {/* Mobilde davetin kendi eylemleri FAB'da. */}
      {!masaustu && (
        <ActionSheet
          baslik="Davet işlemleri"
          eylemler={[
            ...(hasPermission(PERMISSION.davetYonet)
              ? [{ etiket: 'Protokolden kişi ekle', ikon: <UserPlus size={17} />, onClick: () => setKisiEkle(true) }]
              : []),
            { etiket: 'PDF çıktı', ikon: <FileDown size={17} />, onClick: () => setCiktiAcik(true) },
            { etiket: 'İsim kartları', ikon: <Scissors size={17} />, onClick: () => setKartAcik(true) },
            ...(hasPermission(PERMISSION.davetYonet)
              ? [{ etiket: 'Daveti sil', ikon: <Trash2 size={17} />, onClick: () => setSilinecek(true), ton: 'tehlike' as const }]
              : []),
          ]}
        />
      )}

      {/*
        İSİM KARTLARI BURADAN BASILIR — kesme etiketi ve masa (çadır) kartı.

        Kaynak protokol defteri DEĞİL, bu davet: basılacak olan kurumun bütün
        protokol listesi değil, bu törene çağrılanlar. Kart penceresi protokol
        ekranından KALDIRILDI; defterin tamamını basmak yüzlerce gereksiz kart
        demekti ve isimlik zaten bir törene ait bir iş.
      */}
      <NameCardExport
        acik={kartAcik}
        kapat={() => setKartAcik(false)}
        davetId={davetId}
        davetBasligi={d.baslik}
      />

      {/* ── Eylemler ── */}
      <div className="hidden flex-wrap gap-2 md:flex">
        <Button onClick={() => setKisiEkle(true)}>
          <UserPlus size={14} />
          Protokolden kişi ekle
        </Button>

        <Button varyant="ikincil" onClick={() => setKartAcik(true)}>
          <Scissors size={14} />
          İsim kartları
        </Button>

        <DropdownMenu.Root>
          <DropdownMenu.Trigger asChild>
            <Button varyant="ikincil">
              <FileDown size={14} />
              PDF çıktı
              <ChevronDown size={12} />
            </Button>
          </DropdownMenu.Trigger>

          <DropdownMenu.Portal>
            <DropdownMenu.Content
              align="start"
              sideOffset={6}
              className="katman anim-katman z-400 min-w-[250px] rounded-card border border-border bg-surface p-1.5 shadow-3"
            >
              {[
                { t: 'Durumlu', e: 'Takip listesi', a: 'Katılım durumu, arandı/mesaj ve notlarla' },
                { t: 'Telefonlu', e: 'Telefon listesi', a: 'Arama yaparken kullanmak için' },
                { t: 'BosKatilim', e: 'Boş katılım listesi', a: 'Törende elle işaretlenir, imza sütunlu' },
                { t: 'BosProtokol', e: 'Boş protokol listesi', a: 'Yalnızca ad, unvan ve kurum' },
              ].map((s) => (
                <DropdownMenu.Item
                  key={s.t}
                  onSelect={() =>
                    download(`/davet/${davetId}/pdf${queryString({ tur: s.t })}`).catch(
                      (h: Error) => bildir('hata', 'İndirilemedi', h.message),
                    )
                  }
                  className="cursor-pointer rounded-sm px-2.5 py-2 outline-hidden data-highlighted:bg-surface-2"
                >
                  <span className="block text-sm font-medium">{s.e}</span>
                  <span className="block text-xs text-text-3">{s.a}</span>
                </DropdownMenu.Item>
              ))}
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>

        <span className="ml-auto flex flex-wrap items-center gap-1.5">
          <Sayac renk="ok" sayi={d.katilacak} etiket="katılacak" />
          <Sayac renk="no" sayi={d.katilmayacak} etiket="katılmayacak" />
          <Sayac renk="wait" sayi={d.beklemede} etiket="beklemede" />
        </span>
      </div>

      {/* ── Kişiler ── */}
      {d.kisiler.length === 0 ? (
        <EmptyState
          ikon={Users}
          baslik="Davet listesi boş"
          aciklama="Protokol listesinden kişi ekleyerek başlayın; kategorinin tamamını tek seferde ekleyebilirsiniz."
          eylem={
            // Boş durumdaki EKLEME düğmesi de izin ister; araç çubuğundaki
            // düğme kapıdan geçiyordu ama liste boşken çizilen bu ikinci
            // düğme kapının dışında kalmıştı.
            hasPermission(PERMISSION.davetYonet) ? (
              <Button onClick={() => setKisiEkle(true)}>
                <UserPlus size={14} />
                Protokolden kişi ekle
              </Button>
            ) : undefined
          }
        />
      ) : (
        [...gruplar.entries()].map(([kategori, kisiler]) => (
          <Card key={kategori}>
            <CardHeader baslik={kategori} aciklama={`${kisiler.length} kişi`} />
            <ul className="divide-y divide-border">
              {kisiler.map((k) => (
                <li key={k.id}>
                  {/*
                    ── MOBİLDE SATIR ÖZET, EYLEMLER TABAKADA ──

                    Satırda BEŞ denetim vardı: "Arandı" hapı, "Mesaj" hapı,
                    cevap için bir `<select>`, not düğmesi ve çıkarma düğmesi.
                    390px'te ikinci satıra sarıyor, kişi başına ~130px yer
                    kaplıyordu ve hiçbiri neyin ne olduğunu söylemiyordu:
                    iki yeşil hap, bir açılır kutu, iki gri kare.

                    Şimdi satır yalnızca OKUNUYOR — ad, unvan, not, cevap çipi
                    ve arandı/mesaj için iki küçük işaret. Dokununca kişinin
                    kendi tabakası açılıyor ve orada her eylemin adı yazıyor.
                    Satır 130px'ten ~78px'e indi, listede iki kat kişi görünüyor.
                  */}
                  {!masaustu ? (
                    <button
                      type="button"
                      onClick={() => setSecilenKisi(k)}
                      className="flex w-full items-start gap-3 p-3 text-left active:bg-sunken"
                    >
                      <span className="min-w-0 flex-1">
                        <span className="block truncate text-sm font-semibold">{k.adSoyad}</span>
                        <span className="mt-0.5 block truncate text-2xs text-ink-3">
                          {[k.unvan, k.kurum].filter(Boolean).join(' · ')}
                        </span>
                        {k.not && (
                          <span className="mt-1 block text-2xs leading-[1.45] text-ink-2 satir-2">
                            {k.not}
                          </span>
                        )}
                        {/* Arandı / mesaj: yapılmışsa dolu, değilse sönük.
                            İki hap yerine iki simge — aynı bilgi, dörtte bir yer. */}
                        <span className="mt-1.5 flex items-center gap-2.5">
                          <Phone
                            size={13}
                            className={k.arandi ? 'text-ok' : 'text-ink-3 opacity-35'}
                            aria-label={k.arandi ? 'Arandı' : 'Aranmadı'}
                          />
                          <MessageSquare
                            size={13}
                            className={k.mesajGonderildi ? 'text-ok' : 'text-ink-3 opacity-35'}
                            aria-label={k.mesajGonderildi ? 'Mesaj gönderildi' : 'Mesaj gönderilmedi'}
                          />
                        </span>
                      </span>

                      <span className="shrink-0">
                        <DurumCipi durum={k.durum} />
                      </span>
                    </button>
                  ) : (
                  <div className="flex flex-wrap items-start gap-3 p-3">
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-semibold">{k.adSoyad}</p>
                      <p className="text-xs text-text-3">
                        {[k.unvan, k.kurum].filter(Boolean).join(' · ')}
                      </p>
                      {k.not && (
                        <p className="mt-1 text-xs leading-normal text-text-2">{k.not}</p>
                      )}
                    </div>

                    <div className="flex flex-wrap items-center gap-1.5">
                      {/* Eylem: arandı / mesaj — cevaptan AYRI tutulur. */}
                      <IsaretDugmesi
                        aktif={k.arandi}
                        etiket="Arandı"
                        ikon={<Phone size={13} />}
                        tikla={() =>
                          kisiGuncelle.mutate({ kisiId: k.id, yama: { arandi: !k.arandi } })
                        }
                      />
                      <IsaretDugmesi
                        aktif={k.mesajGonderildi}
                        etiket="Mesaj"
                        ikon={<MessageSquare size={13} />}
                        tikla={() =>
                          kisiGuncelle.mutate({
                            kisiId: k.id,
                            yama: { mesajGonderildi: !k.mesajGonderildi },
                          })
                        }
                      />

                      {/* Cevap */}
                      <select
                        aria-label={`${k.adSoyad} katılım durumu`}
                        value={k.durum}
                        onChange={(e) =>
                          kisiGuncelle.mutate({
                            kisiId: k.id,
                            yama: { durum: Number(e.target.value) },
                          })
                        }
                        className="h-9 rounded-control border border-border bg-surface-2 px-2 text-sm outline-hidden focus:border-brand focus:ring-[3px] focus:ring-(--focus-ring)"
                      >
                        {DURUM_SECENEKLERI.map((s) => (
                          <option key={s.d} value={s.d}>
                            {s.e}
                          </option>
                        ))}
                      </select>

                      <IconButton etiket="Not yaz" onClick={() => setNotYazilan(k)}>
                        <MessageSquare size={14} />
                      </IconButton>
                      <IconButton
                        etiket="Listeden çıkar"
                        onClick={() => kisiCikar.mutate(k.id)}
                      >
                        <X size={14} />
                      </IconButton>
                    </div>
                  </div>
                  )}
                </li>
              ))}
            </ul>
          </Card>
        ))
      )}

      <KisiEkleDiyalogu
        acik={kisiEkle}
        kapat={() => setKisiEkle(false)}
        davetId={davetId}
        mevcutProtokolIdler={d.kisiler.map((k) => k.protokolId)}
      />

      <NotDiyalogu
        kisi={notYazilan}
        kapat={() => setNotYazilan(null)}
        kaydet={(metin) => {
          if (notYazilan) {
            kisiGuncelle.mutate({ kisiId: notYazilan.id, yama: { not: metin } });
          }
          setNotYazilan(null);
        }}
      />

      <ConfirmDialog
        acik={silinecek}
        baslik="Davet silinsin mi?"
        aciklama={`"${d.baslik}" ve listedeki ${d.kisiSayisi} kişi kaydı silinecek. Protokol listesi etkilenmez.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => sil.mutate()}
        kapat={() => setSilinecek(false)}
      />

    {/* Mobil PDF çıktı tabakası — FAB'daki "PDF çıktı" satırından. */}
    {!masaustu && (
      <BottomSheet acik={ciktiAcik} kapat={() => setCiktiAcik(false)} baslik="PDF çıktı">
        {[
          { t: 'Durumlu', e: 'Takip listesi', a: 'Katılım durumu, arandı/mesaj ve notlarla' },
          { t: 'Telefonlu', e: 'Telefon listesi', a: 'Arama yaparken kullanmak için' },
          { t: 'BosKatilim', e: 'Boş katılım listesi', a: 'Törende elle işaretlenir, imza sütunlu' },
          { t: 'BosProtokol', e: 'Boş protokol listesi', a: 'Yalnızca ad, unvan ve kurum' },
        ].map((c) => (
          <SheetRow
            key={c.t}
            ikon={<FileDown size={17} />}
            okYok
            onClick={() => {
              setCiktiAcik(false);
              download(`/davet/${davetId}/pdf${queryString({ tur: c.t })}`).catch((h: Error) =>
                bildir('hata', 'İndirilemedi', h.message),
              );
            }}
          >
            {c.e}
          </SheetRow>
        ))}
      </BottomSheet>
    )}

    {/* ── MOBİL: kişi eylem tabakası ── */}
    {!masaustu && (
      <BottomSheet
        acik={secilenKisi !== null}
        kapat={() => setSecilenKisi(null)}
        baslik={secilenKisi?.adSoyad ?? ''}
        aciklama={[secilenKisi?.unvan, secilenKisi?.kurum].filter(Boolean).join(' · ')}
      >
        {secilenKisi && (
          <>
            <SheetHeading>Cevap</SheetHeading>
            <div className="mb-3 flex flex-wrap gap-1.5">
              {DURUM_SECENEKLERI.map((d) => {
                const secili = secilenKisi.durum === d.d;
                return (
                  <button
                    key={d.d}
                    type="button"
                    onClick={() => {
                      kisiGuncelle.mutate({ kisiId: secilenKisi.id, yama: { durum: d.d } });
                      setSecilenKisi({ ...secilenKisi, durum: d.d });
                    }}
                    className={cn(
                      'h-ctrl-lg rounded-full border px-3.5 text-xs font-semibold transition-colors active:scale-[0.97]',
                      secili ? 'border-transparent' : 'border-line bg-surface text-ink-2',
                    )}
                    style={
                      secili
                        ? { background: `var(--st-${d.renk}-bg)`, color: `var(--st-${d.renk})` }
                        : { transitionTimingFunction: 'var(--ease-spring)' }
                    }
                  >
                    {d.e}
                  </button>
                );
              })}
            </div>

            <SheetHeading>İşlem</SheetHeading>
            {/* Arandı / mesaj AÇIK-KAPALI: satırın sağındaki tik, durumun
                kendisini gösteriyor — ayrı bir "aktif" rengine gerek yok. */}
            <SheetRow
              ikon={<Phone size={17} />}
              okYok
              sag={secilenKisi.arandi ? <Check size={18} className="text-ok" /> : undefined}
              onClick={() => {
                kisiGuncelle.mutate({
                  kisiId: secilenKisi.id,
                  yama: { arandi: !secilenKisi.arandi },
                });
                setSecilenKisi({ ...secilenKisi, arandi: !secilenKisi.arandi });
              }}
            >
              Arandı
            </SheetRow>
            <SheetRow
              ikon={<MessageSquare size={17} />}
              okYok
              sag={secilenKisi.mesajGonderildi ? <Check size={18} className="text-ok" /> : undefined}
              onClick={() => {
                kisiGuncelle.mutate({
                  kisiId: secilenKisi.id,
                  yama: { mesajGonderildi: !secilenKisi.mesajGonderildi },
                });
                setSecilenKisi({ ...secilenKisi, mesajGonderildi: !secilenKisi.mesajGonderildi });
              }}
            >
              Mesaj gönderildi
            </SheetRow>
            <SheetRow
              ikon={<SquarePen size={17} />}
              onClick={() => {
                const k = secilenKisi;
                setSecilenKisi(null);
                setNotYazilan(k);
              }}
            >
              {secilenKisi.not ? 'Notu düzenle' : 'Not yaz'}
            </SheetRow>

            <SheetDivider />
            <SheetRow
              ikon={<X size={17} />}
              ton="tehlike"
              okYok
              onClick={() => {
                kisiCikar.mutate(secilenKisi.id);
                setSecilenKisi(null);
              }}
            >
              Listeden çıkar
            </SheetRow>
          </>
        )}
      </BottomSheet>
    )}

    </div>
  );
}

/** Katılım cevabı — listede tek bakışta okunan renk. */
function DurumCipi({ durum }: { durum: number }) {
  const d = DURUM_SECENEKLERI.find((x) => x.d === durum) ?? DURUM_SECENEKLERI[0];
  return (
    <span
      className="inline-block rounded-full px-2 py-0.5 text-2xs font-semibold"
      style={{ background: `var(--st-${d.renk}-bg)`, color: `var(--st-${d.renk})` }}
    >
      {d.e}
    </span>
  );
}

function Sayac({ renk, sayi, etiket }: { renk: string; sayi: number; etiket: string }) {
  if (sayi === 0) return null;
  return (
    <span
      className="rounded-full px-2.5 py-1 text-xs font-medium"
      style={{ background: `var(--st-${renk}-bg)`, color: `var(--st-${renk})` }}
    >
      {sayi} {etiket}
    </span>
  );
}

function IsaretDugmesi({
  aktif,
  etiket,
  ikon,
  tikla,
}: {
  aktif: boolean;
  etiket: string;
  ikon: React.ReactNode;
  tikla: () => void;
}) {
  return (
    <button
      type="button"
      onClick={tikla}
      aria-pressed={aktif}
      title={etiket}
      className={cn(
        'inline-flex h-8 items-center gap-1.5 rounded-full border px-2.5 text-xs font-medium transition-colors',
        aktif
          ? 'border-(--st-ok) bg-(--st-ok-bg) text-(--st-ok)'
          : 'border-border bg-surface text-text-3 hover:bg-surface-2',
      )}
    >
      {ikon}
      {etiket}
    </button>
  );
}

/** Protokolden kişi ekleme — tek tek ya da kategorinin tamamı. */
function KisiEkleDiyalogu({
  acik,
  kapat,
  davetId,
  mevcutProtokolIdler,
}: {
  acik: boolean;
  kapat: () => void;
  davetId: number;
  mevcutProtokolIdler: number[];
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [arama, setArama] = useState('');
  const [secili, setSecili] = useState<number[]>([]);

  const protokol = useQuery({
    queryKey: ['protokol', 'davet-secim', arama] as const,
    queryFn: () =>
      api.get<PagedResult<Protocol>>(`/protokol${queryString({ boyut: 200, ara: arama })}`),
    enabled: acik,
  });

  const kategoriler = useQuery({
    queryKey: ['protokol', 'kategoriler'] as const,
    queryFn: () => api.get<Kategori[]>('/protokol/kategoriler'),
    enabled: acik,
  });

  const ekle = useMutation({
    mutationFn: (govde: { protokolIdler?: number[]; kategoriId?: number }) =>
      api.post<unknown>(`/davet/${davetId}/kisi`, govde),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['davet'] });
      setSecili([]);
      kapat();
      bildir('basari', 'Kişiler eklendi');
    },
    onError: (h: Error) => bildir('hata', 'Eklenemedi', h.message),
  });

  const liste = (protokol.data?.veriler ?? []).filter(
    (p) => !mevcutProtokolIdler.includes(p.id),
  );

  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde" />
        <Dialog.Content
          className="katman anim-tabaka fixed inset-x-0 bottom-0 z-50 flex max-h-[88dvh] flex-col rounded-t-win bg-surface shadow-3
            md:inset-x-auto md:bottom-auto md:left-1/2 md:top-1/2 md:max-h-[82dvh] md:w-[min(600px,calc(100vw-48px))]
            md:-translate-x-1/2 md:-translate-y-1/2 md:rounded-win"
        >
          <div className="flex items-center gap-2.5 border-b border-border px-4 py-3">
            <span className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-brand-tint text-brand-2">
              <UserPlus size={15} />
            </span>
            <Dialog.Title className="flex-1 font-display text-lg font-bold">
              Protokolden kişi ekle
            </Dialog.Title>
            <Dialog.Close asChild>
              <IconButton etiket="Kapat">
                <X size={16} />
              </IconButton>
            </Dialog.Close>
          </div>

          <Dialog.Description className="sr-only">
            Davet listesine eklenecek kişileri seçin.
          </Dialog.Description>

          {/* Kategorinin TAMAMINI ekle: davetler çoğu zaman "mülki idarenin
              tamamı" diye kuruluyor, tek tek seçtirmek kullanılmaz olurdu. */}
          <div className="border-b border-border p-3">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-text-3">
              Kategorinin tamamını ekle
            </p>
            <div className="flex flex-wrap gap-1.5">
              {(kategoriler.data ?? []).map((k) => (
                <Button
                  key={k.id}
                  varyant="ikincil"
                  className="h-8 px-2.5 text-xs"
                  onClick={() => ekle.mutate({ kategoriId: k.id })}
                  disabled={ekle.isPending}
                >
                  <Plus size={12} />
                  {k.ad}
                </Button>
              ))}
            </div>
          </div>

          <div className="border-b border-border p-3">
            <SearchInput
              value={arama}
              onChange={(e) => setArama(e.target.value)}
              placeholder="Ad, unvan veya kurum ara"
              aria-label="Protokolde ara"
            />
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto p-2">
            {protokol.isLoading ? (
              <Skeleton className="h-32 w-full" />
            ) : liste.length === 0 ? (
              <p className="px-2 py-6 text-center text-sm text-text-3">
                Eklenebilecek kayıt yok.
              </p>
            ) : (
              <ul className="space-y-1">
                {liste.map((p) => {
                  const isaretli = secili.includes(p.id);
                  return (
                    <li key={p.id}>
                      <button
                        type="button"
                        onClick={() =>
                          setSecili((s) =>
                            isaretli ? s.filter((x) => x !== p.id) : [...s, p.id],
                          )
                        }
                        className={cn(
                          'flex w-full items-center gap-2.5 rounded-control border px-2.5 py-2 text-left transition-colors',
                          isaretli
                            ? 'border-brand bg-brand-tint'
                            : 'border-transparent hover:bg-surface-2',
                        )}
                      >
                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-sm font-medium">
                            {p.adSoyad}
                          </span>
                          <span className="block truncate text-xs text-text-3">
                            {[p.unvan, p.kurum, p.kategori].filter(Boolean).join(' · ')}
                          </span>
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <div className="flex shrink-0 items-center gap-2 border-t border-border px-4 py-3">
            <span className="flex-1 text-sm text-text-3">{secili.length} kişi seçildi</span>
            <Button varyant="ikincil" onClick={kapat}>
              Vazgeç
            </Button>
            <Button
              onClick={() => ekle.mutate({ protokolIdler: secili })}
              disabled={secili.length === 0 || ekle.isPending}
            >
              Ekle
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

function NotDiyalogu({
  kisi,
  kapat,
  kaydet,
}: {
  kisi: DavetKisi | null;
  kapat: () => void;
  kaydet: (metin: string) => void;
}) {
  const [metin, setMetin] = useState('');
  const [sonKisi, setSonKisi] = useState<number | null>(null);

  if (kisi && kisi.id !== sonKisi) {
    setSonKisi(kisi.id);
    setMetin(kisi.not ?? '');
  }

  return (
    <Dialog.Root open={kisi !== null} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde" />
        <Dialog.Content className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(460px,calc(100vw-32px))] -translate-x-1/2 -translate-y-1/2 rounded-win bg-surface p-5 shadow-3">
          <Dialog.Title className="font-display text-lg font-bold">
            {kisi?.adSoyad} — not
          </Dialog.Title>
          <Dialog.Description className="mt-1 text-sm text-text-2">
            Örn. “Sekreterine bırakıldı”, “Yurt dışında”, “Eşiyle katılacak”.
          </Dialog.Description>

          <div className="mt-4 space-y-3.5">
            <Textarea
              value={metin}
              onChange={(e) => setMetin(e.target.value)}
              aria-label="Not"
              rows={4}
            />
            <div className="flex justify-end gap-2">
              <Button varyant="ikincil" onClick={kapat}>
                Vazgeç
              </Button>
              <Button onClick={() => kaydet(metin)}>Kaydet</Button>
            </div>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
