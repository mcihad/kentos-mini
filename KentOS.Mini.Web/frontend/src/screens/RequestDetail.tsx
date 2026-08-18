import * as Tabs from '@radix-ui/react-tabs';
import { SekmeListesi, SekmeTetigi } from '../components/Tabs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft, ArrowUpFromLine, Briefcase, ChevronDown, FileText,
  MapPin, MessageSquarePlus, Paperclip, Phone, Trash2, Upload, User,
} from 'lucide-react';
import { useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Secim } from '../components/Field';
import { NoteComposer } from '../components/NoteComposer';
import { useIsDesktop } from '../components/screenSize';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { Skeleton } from '../components/Skeleton';
import { Card, CardHeader } from '../components/Card';
import { ImageViewer, useImageViewer } from '../components/ImageViewer';
import { useToast } from '../components/Toast';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Timeline } from '../components/Timeline';
import { RequestActions } from './request/RequestActions';
import { queryKeys } from '../data/queryKeys';
import { unitLabel, fileSize, date, dateTime } from '../data/format';
import { api, tokenStore, type PagedResult } from '../data/client';
import { toMap, useUnits, useRequestStatuses } from '../data/hooks';
import type { Request, RequestFile, RequestActivity, RequestNote } from '../data/types';
import { ColoredBadge } from '../components/Color';

/** Talep detayı — üst bilgi + sekmeler (notlar / hareketler / dosyalar). */
export default function RequestDetail() {
  // Talep eden bloğu mobilde üstte, masaüstünde yan sütunda — ikisi birden
  // çizilseydi aynı bilgi ekranda iki kez görünürdü.
  const masaustu = useIsDesktop();
  const { id } = useParams();
  const talepId = Number(id);
  const gezin = useNavigate();
  const { bildir } = useToast();
  const qc = useQueryClient();

  const talep = useQuery({
    queryKey: queryKeys.request.detail(talepId),
    queryFn: () => api.get<Request>(`/talep/${talepId}`),
    enabled: Number.isFinite(talepId),
  });

  const durumlar = useRequestStatuses();
  const durumHaritasi = toMap(durumlar.liste, (d) => d.id!);

  const durumDegistir = useMutation({
    mutationFn: (durumId: number) => api.post<Request>(`/talep/${talepId}/durum/${durumId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      bildir('basari', 'Durum güncellendi');
    },
    onError: (h: Error) => bildir('hata', 'Durum değiştirilemedi', h.message),
  });

  if (talep.isLoading) return <DetayIskeleti />;

  if (talep.isError || !talep.data) {
    return (
      <EmptyState
        ikon={FileText}
        baslik="Talep bulunamadı"
        aciklama={(talep.error as Error)?.message ?? 'Kayıt silinmiş veya erişiminiz yok olabilir.'}
        eylem={
          <Button varyant="ikincil" onClick={() => gezin('/talepler')}>
            Taleplere dön
          </Button>
        }
      />
    );
  }

  const t = talep.data;

  return (
    <div className="space-y-4">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-3">
        <Link to="/talepler" aria-label="Taleplere dön" className="mt-0.5 shrink-0">
          <IconButton etiket="Geri">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-xl font-bold leading-[1.3] tracking-[-0.015em] metin-guzel md:text-2xl">
            {t.konu}
          </h2>
          <div className="mt-1.5 flex flex-wrap items-center gap-2">
            <ColoredBadge etiket={durumHaritasi.get(t.randevuDurumId ?? -1)?.durumAd} renk={durumHaritasi.get(t.randevuDurumId ?? -1)?.renk} />
            {t.arsivlendi && (
              <span className="rounded-full bg-sunken px-2.5 py-0.5 text-2xs font-semibold text-text-3">
                Arşivlendi
              </span>
            )}
            <span className="text-xs text-text-3">{date(t.baslangicTarih)}</span>
          </div>
        </div>
      </div>

      {/* ── Eylemler ── */}
      <div className="flex flex-wrap gap-2">
        <RequestActions talep={t} />

        <DurumDegistirici
          durumlar={durumlar.liste}
          mevcut={t.randevuDurumId ?? null}
          degistir={(d) => durumDegistir.mutate(d)}
          beklemede={durumDegistir.isPending}
        />
      </div>

      {/*
        TALEP EDEN — MOBİLDE EN ÜSTTE.

        Bu blok yalnızca yan sütundaki kartta duruyordu. Izgara tek sütuna
        düşünce (telefon) kart, DÖRT sekmenin bütün içeriğinin ALTINA
        kayıyor: talebin asıl verisi — kimin, hangi numarayla başvurduğu —
        ekranın en görünmez yerinde kalıyordu. Oysa memur talebe bakarken
        önce vatandaşı arıyor.

        Mobilde başlığın hemen altında, tek satırda ve **arama düğmesiyle**;
        masaüstünde yan sütundaki kart aynen duruyor (orada zaten göz
        hizasında ve yer bol).
      */}
      {!masaustu && <TalepEdenSeridi talep={t} />}

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* ── Sekmeler ── */}
        <Tabs.Root defaultValue="ozet">
          {/* Tam genişlik hap — bkz. `Sekmeler`; detay sayfaları da aynı
              gramerde. */}
          <SekmeListesi etiket="Talep bölümleri">
            {[
              { d: 'ozet', e: 'Özet' },
              { d: 'notlar', e: 'Notlar' },
              { d: 'hareketler', e: 'Hareketler' },
              { d: 'dosyalar', e: 'Dosyalar' },
            ].map((s) => (
              <SekmeTetigi key={s.d} deger={s.d}>
                {s.e}
              </SekmeTetigi>
            ))}
          </SekmeListesi>

          <Tabs.Content value="ozet" className="pt-4">
            <Ozet talep={t} />
          </Tabs.Content>
          <Tabs.Content value="notlar" className="pt-4">
            <Notlar talepId={talepId} />
          </Tabs.Content>
          <Tabs.Content value="hareketler" className="pt-4">
            <Hareketler talepId={talepId} />
          </Tabs.Content>
          <Tabs.Content value="dosyalar" className="pt-4">
            <Dosyalar talepId={talepId} />
          </Tabs.Content>
        </Tabs.Root>

        {/* ── Yan bilgi (masaüstü) ── */}
        {masaustu && (
        <Card className="h-fit">
          <CardHeader baslik="Talep eden" />
          <dl className="space-y-3 p-4">
            <Satir ikon={<User size={13} />} etiket="Ad Soyad" deger={t.adSoyad} />
            <Satir ikon={<Briefcase size={13} />} etiket="Meslek" deger={t.meslek} />
            <Satir
              ikon={<Phone size={13} />}
              etiket="Telefon"
              deger={t.telefon}
              baglanti={t.telefon ? `tel:${t.telefon}` : undefined}
            />
            <Satir ikon={<MapPin size={13} />} etiket="Adres" deger={t.adres} />
          </dl>
        </Card>
        )}
      </div>
    </div>
  );
}

/**
 * Talep edenin mobil şeridi.
 *
 * <p>
 * Baş harfler + ad, altında meslek ve mahalle; sağda <b>ara düğmesi</b>.
 * Telefonda talebe bakan kişinin ilk işi vatandaşı aramak; numarayı bulup
 * kopyalamak yerine tek dokunuş yeter. Numara yoksa düğme hiç çizilmez —
 * çalışmayan bir düğme, olmayan bir düğmeden kötüdür.
 * </p>
 */
function TalepEdenSeridi({ talep }: { talep: Request }) {
  const ad = (talep.adSoyad ?? '').trim();
  const bas = ad
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toLocaleUpperCase('tr-TR'))
    .join('');

  const altSatir = [talep.meslek, talep.adres].filter(Boolean).join(' · ');

  return (
    <Card className="flex items-center gap-3 p-3">
      <span className="grid h-11 w-11 flex-none place-items-center rounded-full bg-brand-soft font-display text-sm font-bold text-brand">
        {bas || <User size={18} strokeWidth={1.9} />}
      </span>

      <span className="min-w-0 flex-1">
        <span className="block truncate font-display text-base font-semibold leading-[1.25]">
          {ad || 'Ad girilmemiş'}
        </span>
        {talep.telefon && (
          <span className="mt-0.5 block truncate text-sm tabular-nums text-ink-2">
            {talep.telefon}
          </span>
        )}
        {altSatir && (
          <span className="mt-0.5 block truncate text-2xs text-ink-3">{altSatir}</span>
        )}
      </span>

      {talep.telefon && (
        <a
          href={`tel:${talep.telefon}`}
          aria-label={`${ad || 'Request edeni'} ara`}
          className="grid h-11 w-11 flex-none place-items-center rounded-full bg-brand text-on-brand active:scale-95"
          style={{ transitionTimingFunction: 'var(--ease-spring)' }}
        >
          <Phone size={18} strokeWidth={2} />
        </a>
      )}
    </Card>
  );
}

function Satir({
  ikon,
  etiket,
  deger,
  baglanti,
}: {
  ikon: React.ReactNode;
  etiket: string;
  deger?: string | null;
  baglanti?: string;
}) {
  return (
    <div className="flex gap-2.5">
      <span className="mt-0.5 shrink-0 text-text-3" aria-hidden>
        {ikon}
      </span>
      <div className="min-w-0">
        <dt className="text-2xs uppercase tracking-[0.06em] text-text-3">{etiket}</dt>
        <dd className="text-sm text-text-2">
          {deger ? (
            baglanti ? (
              <a href={baglanti} className="hover:underline">
                {deger}
              </a>
            ) : (
              deger
            )
          ) : (
            <span className="text-text-3">—</span>
          )}
        </dd>
      </div>
    </div>
  );
}

function Ozet({ talep }: { talep: Request }) {
  const birimler = useUnits();
  const birim = birimler.liste.find((b) => b.id === talep.birimId);

  return (
    <Card className="p-4">
      <dl className="grid gap-3.5 sm:grid-cols-2">
        <Satir ikon={<MapPin size={13} />} etiket="Görüşme yeri" deger={talep.yer} />
        <Satir
          ikon={<Briefcase size={13} />}
          etiket="Birim"
          deger={birim ? unitLabel(birim) : undefined}
        />
        <Satir
          ikon={<FileText size={13} />}
          etiket="Talep tarihi"
          deger={dateTime(talep.baslangicTarih)}
        />
        <Satir
          ikon={<FileText size={13} />}
          etiket="Tamamlanma"
          deger={talep.tamamlanmaTarih ? dateTime(talep.tamamlanmaTarih) : null}
        />
      </dl>

      {talep.aciklama && (
        <div className="mt-4 border-t border-border pt-4">
          <p className="mb-1.5 text-2xs uppercase tracking-[0.06em] text-text-3">Açıklama</p>
          <p className="whitespace-pre-wrap text-sm leading-[1.6] text-text-2 metin-guzel">
            {talep.aciklama}
          </p>
        </div>
      )}

      {talep.ozgecmisDurum && talep.ozgecmisDosya && (
        <div className="mt-4 border-t border-border pt-4">
          <a
            href={talep.ozgecmisDosya}
            target="_blank"
            rel="noreferrer"
            className="inline-flex items-center gap-2 text-sm font-medium"
          >
            <Paperclip size={14} />
            Özgeçmiş dosyasını aç
          </a>
        </div>
      )}
    </Card>
  );
}

function Notlar({ talepId }: { talepId: number }) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  // Uç nokta SAYFALI döner (`{veriler, toplam, ...}`). Daha önce burada
  // `TalepNot[]` bekleniyordu; `.map` bir nesne üzerinde çağrılınca sekme
  // tamamen çöküyordu.
  const notlar = useQuery({
    queryKey: ['talep', 'notlar', talepId] as const,
    queryFn: () => api.get<PagedResult<RequestNote>>(`/talep/${talepId}/notlar?boyut=200`),
  });
  const notListesi = notlar.data?.veriler ?? [];

  const ekle = useMutation({
    mutationFn: (not: string) => api.post<RequestNote>(`/talep/${talepId}/not`, { not }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['talep', 'notlar', talepId] });
      bildir('basari', 'Not eklendi');
    },
    onError: (h: Error) => bildir('hata', 'Not eklenemedi', h.message),
  });

  return (
    <div className="space-y-4">
      <NoteComposer
        alanId="yeni-not"
        yerTutucu="Talebe not ekleyin…"
        bekliyor={ekle.isPending}
        gonder={(m) => ekle.mutateAsync(m)}
      />

      {notlar.isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : notListesi.length === 0 ? (
        <EmptyState
          ikon={MessageSquarePlus}
          baslik="Henüz not yok"
          aciklama="Bu talebe eklenmiş bir not bulunmuyor."
        />
      ) : (
        <Card className="p-4">
          <Timeline
            ogeler={notListesi.map((n, i) => ({
              id: i,
              baslik: n.olusturan || 'Bilinmiyor',
              zaman: dateTime(n.tarih),
              govde: (
                <p className="whitespace-pre-wrap text-sm leading-[1.55] text-text-2">
                  {n.not}
                </p>
              ),
            }))}
          />
        </Card>
      )}
    </div>
  );
}

function Hareketler({ talepId }: { talepId: number }) {
  const hareketler = useQuery({
    queryKey: ['talep', 'hareketler', talepId] as const,
    queryFn: () => api.get<PagedResult<RequestActivity>>(`/talep/${talepId}/hareketler?boyut=200`),
  });
  const hareketListesi = hareketler.data?.veriler ?? [];

  if (hareketler.isLoading) return <Skeleton className="h-24 w-full" />;
  if (hareketListesi.length === 0) {
    return (
      <EmptyState
        ikon={ArrowUpFromLine}
        baslik="Hareket yok"
        aciklama="Bu talep henüz başka bir birime havale edilmemiş."
      />
    );
  }

  return (
    <Card className="p-4">
      <Timeline
        ogeler={hareketListesi.map((h, i) => ({
          id: i,
          baslik: h.asagiHareket ? 'Alt birime havale' : 'Üst birime gönderildi',
          altBaslik: `${h.eskiBirim || '—'} → ${h.yeniBirim || '—'}`,
          zaman: dateTime(h.tarih),
          govde: h.kullanici ? (
            <p className="text-xs text-text-3">{h.kullanici}</p>
          ) : undefined,
          renk: h.asagiHareket ? '--st-live' : '--st-wait',
        }))}
      />
    </Card>
  );
}

/**
 * Talep dosyaları — listeleme, yükleme, silme.
 *
 * Yükleme `multipart/form-data` olduğu için ortak `api` sarmalayıcısı
 * kullanılmaz (o JSON gövdesi kurar); jeton elle eklenir.
 */
function Dosyalar({ talepId }: { talepId: number }) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const dosyaRef = useRef<HTMLInputElement>(null);
  const [silinecek, setSilinecek] = useState<RequestFile | null>(null);

  const dosyalar = useQuery({
    queryKey: ['talep', 'dosyalar', talepId] as const,
    queryFn: () => api.get<PagedResult<RequestFile>>(`/talep/${talepId}/dosyalar?boyut=200`),
  });
  const dosyaListesi = dosyalar.data?.veriler ?? [];

  const goruntuleyici = useImageViewer();
  const resimler = dosyaListesi
    .filter((d) => /\.(jpe?g|png|webp|gif|bmp|avif)$/i.test(d.ad ?? d.path ?? ''))
    .map((d) => ({ yol: d.path ?? '', baslik: d.ad, altBilgi: dateTime(d.olusturmaTarih) }));

  const yukle = useMutation({
    mutationFn: async (secilen: FileList) => {
      const govde = new FormData();
      for (const d of Array.from(secilen)) govde.append('dosyalar', d);

      const jeton = tokenStore.read();
      const yanit = await fetch(`/api/v2/talep/${talepId}/dosya`, {
        method: 'POST',
        headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : {},
        body: govde,
      });
      if (!yanit.ok) {
        const hata = await yanit.json().catch(() => null);
        throw new Error(hata?.detail ?? hata?.ayrinti ?? `Yüklenemedi (${yanit.status}).`);
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['talep', 'dosyalar', talepId] });
      qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      bildir('basari', 'Dosya yüklendi');
    },
    onError: (h: Error) => bildir('hata', 'Yüklenemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: (dosyaId: number) => api.delete<boolean>(`/talep/dosya/${dosyaId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['talep', 'dosyalar', talepId] });
      setSilinecek(null);
      bildir('basari', 'Dosya silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  return (
    <div className="space-y-3">
      <input
        ref={dosyaRef}
        type="file"
        multiple
        className="sr-only"
        onChange={(e) => {
          if (e.target.files?.length) yukle.mutate(e.target.files);
          // Aynı dosyayı ikinci kez seçebilmek için değeri sıfırla.
          e.target.value = '';
        }}
      />

      <Button
        varyant="ikincil"
        onClick={() => dosyaRef.current?.click()}
        disabled={yukle.isPending}
      >
        <Upload size={14} />
        {yukle.isPending ? 'Yükleniyor…' : 'Dosya yükle'}
      </Button>

      {dosyalar.isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : dosyaListesi.length === 0 ? (
        <EmptyState
          ikon={Paperclip}
          baslik="Dosya yok"
          aciklama="Bu talebe eklenmiş bir dosya bulunmuyor."
        />
      ) : (
        <ul className="space-y-2">
          {dosyaListesi.map((d, i) => {
            // Resimler görüntüleyicide açılır; belge/PDF tarayıcının kendi
            // görüntüleyicisine gider — onları modalda göstermenin anlamı yok.
            const resimSirasi = resimler.findIndex((r) => r.yol === d.path);
            const ozet = (
              <>
                <span className="block truncate text-sm font-medium">{d.ad}</span>
                <span className="block truncate text-xs text-text-3">
                  {fileSize(d.size)} · {dateTime(d.olusturmaTarih)}
                </span>
              </>
            );

            return (
              <li
                key={d.id ?? i}
                className="flex items-center gap-3 rounded-card border border-border bg-surface p-3 shadow-1"
              >
                <span className="grid h-9 w-9 shrink-0 place-items-center overflow-hidden rounded-md bg-sunken text-text-3">
                  {resimSirasi >= 0 ? (
                    <img src={d.path ?? ''} alt="" className="h-full w-full object-cover" />
                  ) : (
                    <Paperclip size={15} />
                  )}
                </span>

                {resimSirasi >= 0 ? (
                  <button
                    type="button"
                    onClick={() => goruntuleyici.ac(resimSirasi)}
                    className="min-w-0 flex-1 text-left hover:text-brand-2"
                  >
                    {ozet}
                  </button>
                ) : (
                  <a
                    href={d.path ?? '#'}
                    target="_blank"
                    rel="noreferrer"
                    className="min-w-0 flex-1 hover:text-brand-2"
                  >
                    {ozet}
                  </a>
                )}

                <IconButton etiket="Dosyayı sil" onClick={() => setSilinecek(d)}>
                  <Trash2 size={15} />
                </IconButton>
              </li>
            );
          })}
        </ul>
      )}

      <ImageViewer
        resimler={resimler}
        acikIndeks={goruntuleyici.acikIndeks}
        kapat={goruntuleyici.kapat}
        indeksDegistir={goruntuleyici.indeksDegistir}
      />

      <ConfirmDialog
        acik={silinecek !== null}
        baslik="Dosya silinsin mi?"
        aciklama={`"${silinecek?.ad}" kalıcı olarak silinecek.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
        kapat={() => setSilinecek(null)}
      />
    </div>
  );
}

/**
 * Durum değiştirici.
 *
 * `select` bilinçli: 8+ durum olabiliyor ve hepsini buton olarak dizmek
 * mobilde ekranı doldururdu. Değişiklik anında uygulanır — "Kaydet" adımı
 * eklemek tek alanlık bir işlem için gereksiz sürtünme.
 */
function DurumDegistirici({
  durumlar,
  mevcut,
  degistir,
  beklemede,
}: {
  durumlar: { id?: number; durumAd?: string | null }[];
  mevcut: number | null;
  degistir: (id: number) => void;
  beklemede: boolean;
}) {
  return (
    <div className="relative">
      <label htmlFor="durum-sec" className="sr-only">
        Request durumu
      </label>
      <Secim
        id="durum-sec"
        value={mevcut ?? ''}
        disabled={beklemede}
        onChange={(e) => e.target.value && degistir(Number(e.target.value))}
        className="h-9 w-[180px] text-sm"
      >
        <option value="" disabled>
          Durum seçin
        </option>
        {durumlar.map((d) => (
          <option key={d.id} value={d.id}>
            {d.durumAd}
          </option>
        ))}
      </Secim>
      <ChevronDown size={13} className="pointer-events-none absolute right-2.5 top-1/2 hidden -translate-y-1/2 text-text-3" />
    </div>
  );
}

function DetayIskeleti() {
  return (
    <div className="space-y-4">
      <Skeleton className="h-7 w-2/3" />
      <Skeleton className="h-5 w-40" />
      <Skeleton className="h-10 w-full max-w-md" />
      <Skeleton className="h-48 w-full" />
    </div>
  );
}
