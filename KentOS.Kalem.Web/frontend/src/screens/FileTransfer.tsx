import * as Tabs from '@radix-ui/react-tabs';
import { SekmeListesi, SekmeTetigi } from '../components/Tabs';
import { PERMISSION } from '../components/permissions';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft, Download, FileUp, Inbox, Lock, Paperclip, Search, Send, Trash2, Upload,
} from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { FieldWrapper, SearchInput, Textarea, Input } from '../components/Field';
import { Segment } from '../components/FilterSheet';
import { useIsDesktop } from '../components/screenSize';
import { Fab } from '../shell/mobile/Fab';
import { EmptyState } from '../components/EmptyState';
import { FormModal } from '../components/FormModal';
import { Button, IconButton } from '../components/Button';
import { Skeleton, SkeletonRows } from '../components/Skeleton';
import { Card, CardHeader } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { useSession } from '../auth/SessionProvider';
import { initials, dateTime } from '../data/format';
import { download } from '../data/download';
import { api, tokenStore, queryString, type PagedResult } from '../data/client';

/** Gönderim listesi öğesi. */
type GonderimOzet = {
  id: number;
  konu: string;
  dosyaAdi: string;
  boyut: number;
  tarih: string;
  gonderenId: number;
  gonderenAd?: string | null;
  aliciId: number;
  aliciAd?: string | null;
  benGonderdim: boolean;
  okundu: boolean;
  notSayisi: number;
  sonNot?: string | null;
  sonNotTarihi?: string | null;
};

type GonderimNotu = {
  id: number;
  yazanId: number;
  yazanAd?: string | null;
  benimMi: boolean;
  metin: string;
  tarih: string;
};

type GonderimDetayi = GonderimOzet & {
  icerikTuru?: string | null;
  notlar: GonderimNotu[];
};

type Alici = {
  id: number;
  adSoyad: string;
  kullaniciAdi: string;
  unvan?: string | null;
  birimAd?: string | null;
};

type Kutu = 'gelen' | 'giden';

/** `1,2 MB` */
export function fileSizeText(bayt: number): string {
  if (bayt < 1024) return `${bayt} B`;
  if (bayt < 1024 * 1024) return `${(bayt / 1024).toFixed(0)} KB`;
  return `${(bayt / (1024 * 1024)).toFixed(1)} MB`;
}

/**
 * Dosya gönderimi — gelen ve giden kutusu.
 *
 * <p>
 * Bir gönderimi yalnızca <b>gönderen ve alıcı</b> görür; rol bypass'ı yok ve
 * denetim sunucuda. Göndermek ayrı bir yetki ister (<c>dosyaGonderebilir</c>);
 * <b>almak istemez</b> — yoksa gönderilen dosya kimseye ulaşmazdı.
 * </p>
 */
export default function FileTransfer() {
  const masaustu = useIsDesktop();
  const { hasPermission } = useSession();
  // Yetki ROLDEN gelir (`gonderim.gonder`), kullanıcı kaydındaki bayraktan
  // değil: aynı yetkinin iki kaynağı olması, rol ekranından kısılan bir iznin
  // kullanıcı kaydından açık kalması demekti.
  const gonderebilir = hasPermission(PERMISSION.gonderimGonder);

  const [kutu, setKutu] = useState<Kutu>('gelen');
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [sayfa, setSayfa] = useState(1);
  const [formAcik, setFormAcik] = useState(false);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const liste = useQuery({
    queryKey: ['gonderim', 'liste', kutu, sayfa, arama] as const,
    queryFn: () =>
      api.get<PagedResult<GonderimOzet>>(
        `/gonderim${queryString({ kutu, sayfa, boyut: 25, ara: arama })}`,
      ),
    placeholderData: keepPreviousData,
  });

  const kayitlar = liste.data?.veriler ?? [];

  return (
    <div className="space-y-3.5">
      {formAcik && <GonderimFormu kapat={() => setFormAcik(false)} />}

      <Tabs.Root value={kutu} onValueChange={(d) => { setKutu(d as Kutu); setSayfa(1); }}>
        {/*
          Sekme şeridi MASAÜSTÜNE özel; mobilde aynı seçim aramanın yanındaki
          segmentte. Ekran üç sıra denetimle açılıyordu (sekmeler · arama ·
          tam genişlik "Dosya gönder") ve liste kıvrımın altında kalıyordu.
        */}
        <SekmeListesi etiket="Gönderim kutuları" className="hidden md:flex">
          {[
            { d: 'gelen' as const, e: 'Gelen', i: <Inbox size={14} /> },
            { d: 'giden' as const, e: 'Giden', i: <Send size={14} /> },
          ].map((s) => (
            <SekmeTetigi key={s.d} deger={s.d}>
              {s.i}
              {s.e}
            </SekmeTetigi>
          ))}
        </SekmeListesi>
      </Tabs.Root>

      {/* Mobilde gelen/giden seçimi ARAMANIN ÜSTÜNDE tek satır segment:
          iki seçenek için tabaka açtırmak gereksiz bir adım olurdu. */}
      {!masaustu && (
        <Segment
          deger={kutu}
          degistir={(d) => {
            setKutu(d);
            setSayfa(1);
          }}
          secenekler={[
            { deger: 'gelen' as Kutu, etiket: 'Gelen', ikon: <Inbox size={14} /> },
            { deger: 'giden' as Kutu, etiket: 'Giden', ikon: <Send size={14} /> },
          ]}
        />
      )}

      {/* Gönderme yetkisi olan mobil kullanıcı için FAB. */}
      {!masaustu && gonderebilir && (
        <Fab etiket="Dosya gönder" onClick={() => setFormAcik(true)} ikon={<Upload size={24} strokeWidth={2.2} />} />
      )}

      <div className="flex flex-col gap-2.5 md:flex-row md:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Konu ya da dosya adı ara"
          aria-label="Gönderimlerde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[320px] md:flex-1"
        />

        {gonderebilir ? (
          <Button className="hidden md:ml-auto md:inline-flex" onClick={() => setFormAcik(true)}>
            <Upload size={14} />
            Dosya gönder
          </Button>
        ) : (
          <p className="flex items-center gap-1.5 text-xs text-text-3 md:ml-auto">
            <Lock size={12} />
            Dosya gönderme yetkiniz yok — gelen dosyaları görebilirsiniz.
          </p>
        )}
      </div>

      {liste.isLoading ? (
        <SkeletonRows adet={5} />
      ) : liste.isError ? (
        <EmptyState
          ikon={Paperclip}
          baslik="Gönderimler yüklenemedi"
          aciklama={(liste.error as Error)?.message}
        />
      ) : kayitlar.length === 0 ? (
        <EmptyState
          ikon={kutu === 'gelen' ? Inbox : Send}
          baslik={
            arama
              ? 'Eşleşen gönderim yok'
              : kutu === 'gelen'
                ? 'Gelen dosya yok'
                : 'Gönderdiğiniz dosya yok'
          }
          aciklama={
            kutu === 'gelen'
              ? 'Size bir dosya gönderildiğinde burada ve bildirimlerde görünür.'
              : 'Gönderdiğiniz dosyalar ve karşı tarafla yazışmanız burada tutulur.'
          }
          eylem={
            gonderebilir && kutu === 'giden' ? (
              <Button onClick={() => setFormAcik(true)}>
                <Upload size={14} />
                Dosya gönder
              </Button>
            ) : undefined
          }
        />
      ) : (
        <>
          <ul className="space-y-2">
            {kayitlar.map((g) => (
              <li key={g.id}>
                <Link
                  to={`/gonderim/${g.id}`}
                  className={cn(
                    'flex items-start gap-3 rounded-card border border-border bg-surface p-3 transition-colors hover:bg-surface-2',
                    !g.okundu && 'border-l-[3px] border-l-brand',
                  )}
                >
                  <span
                    className="mt-0.5 grid h-9 w-9 shrink-0 place-items-center rounded-md bg-brand-tint text-brand-2"
                    aria-hidden
                  >
                    <Paperclip size={15} />
                  </span>

                  <span className="min-w-0 flex-1">
                    <span className="flex items-baseline gap-2">
                      <span
                        className={cn(
                          'truncate text-sm',
                          g.okundu ? 'font-medium' : 'font-bold',
                        )}
                      >
                        {g.konu}
                      </span>
                      <span className="ml-auto shrink-0 text-xs tabular-nums text-text-3">
                        {dateTime(g.tarih)}
                      </span>
                    </span>

                    <span className="block truncate text-sm text-text-2">
                      {g.benGonderdim ? `→ ${g.aliciAd}` : `← ${g.gonderenAd}`}
                      <span className="text-text-3">
                        {' · '}
                        {g.dosyaAdi} ({fileSizeText(g.boyut)})
                      </span>
                    </span>

                    {g.sonNot && (
                      <span className="mt-0.5 block truncate text-xs text-text-3">
                        {g.notSayisi} not · {g.sonNot}
                      </span>
                    )}
                  </span>
                </Link>
              </li>
            ))}
          </ul>

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="gönderim" />
        </>
      )}
    </div>
  );
}

/* ══════════════════════════════════════════════════════════ gönderme formu */

/**
 * Dosya gönderme formu.
 *
 * `multipart/form-data` gönderildiği için ortak `api` sarmalayıcısı
 * kullanılmaz (o JSON gövdesi kuruyor); jeton elle eklenir.
 */
function GonderimFormu({ kapat }: { kapat: () => void }) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [alici, setAlici] = useState<Alici | null>(null);
  const [konu, setKonu] = useState('');
  const [not, setNot] = useState('');
  const [dosya, setDosya] = useState<File | null>(null);
  const dosyaRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const z = setTimeout(() => setArama(aramaGirdisi), 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const alicilar = useQuery({
    queryKey: ['gonderim', 'alicilar', arama] as const,
    queryFn: () => api.get<Alici[]>(`/gonderim/alicilar${queryString({ ara: arama })}`),
  });

  const gonder = useMutation({
    mutationFn: async () => {
      const govde = new FormData();
      govde.append('aliciId', String(alici!.id));
      govde.append('konu', konu);
      if (not.trim()) govde.append('not', not.trim());
      govde.append('dosya', dosya!);

      const jeton = tokenStore.read();
      const yanit = await fetch('/api/v2/gonderim', {
        method: 'POST',
        headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : {},
        body: govde,
      });

      if (!yanit.ok) {
        const hata = await yanit.json().catch(() => null);
        throw new Error(hata?.detail ?? hata?.ayrinti ?? `Gönderilemedi (${yanit.status}).`);
      }
      return (await yanit.json()) as GonderimDetayi;
    },
    onSuccess: (g) => {
      qc.invalidateQueries({ queryKey: ['gonderim'] });
      bildir('basari', 'Dosya gönderildi', `${alici?.adSoyad} bilgilendirildi.`);
      gezin(`/gonderim/${g.id}`);
    },
    onError: (h: Error) => bildir('hata', 'Gönderilemedi', h.message),
  });

  const gecerli = alici !== null && konu.trim().length > 0 && dosya !== null;

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Dosya gönder"
      aciklama="Dosyayı yalnızca siz ve seçtiğiniz kişi görebilir."
      ikon={<Send size={15} />}
      genislik="orta"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && gonder.mutate()}
            disabled={!gecerli || gonder.isPending}
          >
            <Send size={14} />
            {gonder.isPending ? 'Gönderiliyor…' : 'Gönder'}
          </Button>
        </>
      }
    >
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          if (gecerli) gonder.mutate();
        }}
      >
        <div className="space-y-3">
          <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
            Alıcı
          </p>
          {alici ? (
            <div className="flex items-center gap-2.5 rounded-control border border-brand bg-brand-tint p-2.5">
              <span
                className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-surface font-display text-xs font-bold text-text-2"
                aria-hidden
              >
                {initials(alici.adSoyad.split(' ')[0], alici.adSoyad.split(' ').slice(-1)[0])}
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-semibold">{alici.adSoyad}</span>
                <span className="block truncate text-xs text-text-3">
                  {[alici.unvan, alici.birimAd].filter(Boolean).join(' · ')}
                </span>
              </span>
              <Button
                type="button"
                varyant="sade"
                className="h-8 px-2.5 text-xs"
                onClick={() => setAlici(null)}
              >
                Değiştir
              </Button>
            </div>
          ) : (
            <>
              <SearchInput
                value={aramaGirdisi}
                onChange={(e) => setAramaGirdisi(e.target.value)}
                placeholder="Ad ya da kullanıcı adı ile arayın"
                aria-label="Alıcı ara"
                ikon={<Search size={15} />}
              />

              {alicilar.isLoading ? (
                <Skeleton className="h-24 w-full" />
              ) : (alicilar.data?.length ?? 0) === 0 ? (
                <p className="text-sm text-text-3">Eşleşen kullanıcı yok.</p>
              ) : (
                <ul className="max-h-[280px] space-y-1 overflow-y-auto">
                  {(alicilar.data ?? []).map((k) => (
                    <li key={k.id}>
                      <button
                        type="button"
                        onClick={() => setAlici(k)}
                        className="flex w-full items-center gap-2.5 rounded-control border border-border bg-surface p-2 text-left transition-colors hover:bg-surface-2"
                      >
                        <span
                          className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-sunken font-display text-2xs font-bold text-text-2"
                          aria-hidden
                        >
                          {initials(k.adSoyad.split(' ')[0], k.adSoyad.split(' ').slice(-1)[0])}
                        </span>
                        <span className="min-w-0">
                          <span className="block truncate text-sm font-medium">
                            {k.adSoyad || k.kullaniciAdi}
                          </span>
                          <span className="block truncate text-2xs text-text-3">
                            {[k.unvan, k.birimAd].filter(Boolean).join(' · ')}
                          </span>
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}
        </div>

        <div className="space-y-4 border-t border-border pt-4">
          <FieldWrapper etiket="Konu" id="g-konu" zorunlu>
            <Input
              id="g-konu"
              value={konu}
              onChange={(e) => setKonu(e.target.value)}
              maxLength={150}
            />
          </FieldWrapper>

          <FieldWrapper etiket="Dosya" id="g-dosya" zorunlu ipucu="En fazla 25 MB.">
            <input
              ref={dosyaRef}
              id="g-dosya"
              type="file"
              className="sr-only"
              onChange={(e) => setDosya(e.target.files?.[0] ?? null)}
            />
            <button
              type="button"
              onClick={() => dosyaRef.current?.click()}
              className="flex w-full items-center gap-2.5 rounded-control border border-dashed border-border-2 bg-surface-2 px-3 py-3 text-left transition-colors hover:bg-sunken"
            >
              <FileUp size={16} className="shrink-0 text-text-3" />
              <span className="min-w-0 flex-1">
                {dosya ? (
                  <>
                    <span className="block truncate text-sm font-medium">{dosya.name}</span>
                    <span className="block text-xs text-text-3">
                      {fileSizeText(dosya.size)}
                    </span>
                  </>
                ) : (
                  <span className="text-sm text-text-3">Dosya seçmek için tıklayın</span>
                )}
              </span>
            </button>
          </FieldWrapper>

          <FieldWrapper
            etiket="Not"
            id="g-not"
            ipucu="Alıcı bu nota cevap yazabilir."
          >
            <Textarea id="g-not" value={not} onChange={(e) => setNot(e.target.value)} />
          </FieldWrapper>
        </div>
      </form>
    </FormModal>
  );
}

/* ═══════════════════════════════════════════════════════════════════ detay */

/**
 * Gönderim detayı — dosya + tek konulu yazışma.
 *
 * <p>
 * Sohbet DEĞİL: tek bir dosya etrafında iki tarafın not alışverişi. Bu yüzden
 * yeni bir konu açılamaz, yalnızca bu gönderime not eklenir.
 * </p>
 */
export function FileTransferDetail() {
  const { id } = useParams();
  const gonderimId = Number(id);
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();

  const [metin, setMetin] = useState('');
  const [silinecek, setSilinecek] = useState(false);

  const detay = useQuery({
    queryKey: ['gonderim', 'detay', gonderimId] as const,
    queryFn: () => api.get<GonderimDetayi>(`/gonderim/${gonderimId}`),
    enabled: Number.isFinite(gonderimId) && gonderimId > 0,
  });

  const notEkle = useMutation({
    mutationFn: () => api.post<GonderimNotu>(`/gonderim/${gonderimId}/not`, { metin }),
    onSuccess: () => {
      setMetin('');
      qc.invalidateQueries({ queryKey: ['gonderim'] });
    },
    onError: (h: Error) => bildir('hata', 'Not eklenemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: () => api.delete<void>(`/gonderim/${gonderimId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['gonderim'] });
      bildir('basari', 'Gönderim silindi');
      gezin('/gonderim');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  const g = detay.data;

  const karsiTaraf = useMemo(() => {
    if (!g) return '';
    return g.benGonderdim ? (g.aliciAd ?? '') : (g.gonderenAd ?? '');
  }, [g]);

  if (detay.isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-7 w-1/2" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  if (detay.isError || !g) {
    return (
      <EmptyState
        ikon={Paperclip}
        baslik="Gönderim bulunamadı"
        aciklama={
          (detay.error as Error)?.message ??
          'Kayıt silinmiş olabilir ya da bu gönderim size ait değil.'
        }
        eylem={
          <Link to="/gonderim">
            <Button varyant="ikincil">Gönderimlere dön</Button>
          </Link>
        }
      />
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-start gap-3">
        <Link to="/gonderim" className="mt-0.5 shrink-0">
          <IconButton etiket="Geri">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-xl font-bold tracking-[-0.015em] md:text-2xl">
            {g.konu}
          </h2>
          <p className="text-sm text-text-3">
            {g.benGonderdim ? 'Alıcı' : 'Gönderen'}: {karsiTaraf} · {dateTime(g.tarih)}
          </p>
        </div>
        {/* Yalnızca GÖNDEREN silebilir: alıcının, gönderenin bilgisi olmadan
            belgeyi yok etmesi çözülemez bir anlaşmazlık yaratır. */}
        {g.benGonderdim && (
          <IconButton etiket="Gönderimi sil" onClick={() => setSilinecek(true)}>
            <Trash2 size={16} />
          </IconButton>
        )}
      </div>

      <Card>
        <div className="flex items-center gap-3 p-4">
          <span
            className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-brand-tint text-brand-2"
            aria-hidden
          >
            <Paperclip size={18} />
          </span>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold">{g.dosyaAdi}</p>
            <p className="text-xs text-text-3">
              {fileSizeText(g.boyut)}
              {g.icerikTuru ? ` · ${g.icerikTuru}` : ''}
            </p>
          </div>
          <Button
            onClick={() =>
              download(`/gonderim/${g.id}/dosya`).catch((h: Error) =>
                bildir('hata', 'İndirilemedi', h.message),
              )
            }
          >
            <Download size={14} />
            İndir
          </Button>
        </div>
      </Card>

      <Card>
        <CardHeader
          baslik="Yazışma"
          aciklama="Notlar yalnızca gönderen ve alıcı tarafından görülür."
        />
        <div className="space-y-3 p-4">
          {g.notlar.length === 0 ? (
            <p className="text-sm text-text-3">Henüz not yok.</p>
          ) : (
            <ul className="space-y-2.5">
              {g.notlar.map((n) => (
                <li
                  key={n.id}
                  className={cn('flex', n.benimMi ? 'justify-end' : 'justify-start')}
                >
                  <div
                    className={cn(
                      'max-w-[85%] rounded-card px-3 py-2',
                      n.benimMi
                        ? 'bg-brand text-on-brand'
                        : 'border border-border bg-surface-2 text-text',
                    )}
                  >
                    <p
                      className={cn(
                        'text-2xs font-semibold',
                        n.benimMi ? 'text-white/75' : 'text-text-3',
                      )}
                    >
                      {n.benimMi ? 'Siz' : n.yazanAd}
                      <span className="ml-1.5 font-normal tabular-nums">{dateTime(n.tarih)}</span>
                    </p>
                    <p className="whitespace-pre-wrap text-sm leading-normal">{n.metin}</p>
                  </div>
                </li>
              ))}
            </ul>
          )}

          <form
            className="flex items-end gap-2 border-t border-border pt-3"
            onSubmit={(e) => {
              e.preventDefault();
              if (metin.trim()) notEkle.mutate();
            }}
          >
            <Textarea
              value={metin}
              onChange={(e) => setMetin(e.target.value)}
              placeholder="Not yazın…"
              aria-label="Not"
              rows={2}
              className="flex-1"
            />
            <Button type="submit" disabled={!metin.trim() || notEkle.isPending}>
              <Send size={14} />
              Gönder
            </Button>
          </form>
        </div>
      </Card>

      <ConfirmDialog
        acik={silinecek}
        baslik="Gönderim silinsin mi?"
        aciklama={`"${g.konu}" ve dosyası kalıcı olarak silinecek. Alıcı da erişemez.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => sil.mutate()}
        kapat={() => setSilinecek(false)}
      />
    </div>
  );
}
