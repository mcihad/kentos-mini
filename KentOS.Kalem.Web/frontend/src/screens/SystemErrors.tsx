import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle, ArrowLeft, Bug, Check, CheckCircle2, Copy, Search, Trash2,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { FieldWrapper, SearchInput, Textarea } from '../components/Field';
import { Switch } from '../components/Switch';
import { Accordion, AccordionSection } from '../components/Accordion';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { Skeleton, SkeletonRows } from '../components/Skeleton';
import { Card, CardHeader } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { relativeTime, number, dateTime } from '../data/format';
import { api, queryString, type PagedResult } from '../data/client';
import type { ErrorDetail, ErrorSummary } from '../data/types';

/**
 * Sunucu hata kayıtları — listesi.
 *
 * <p>
 * Sistem iki yıldır canlıda ve bugüne kadar hatalar YALNIZCA konsol günlüğüne
 * düşüyordu: sunucu yeniden başlayınca kayboluyor, kullanıcı "hata aldım"
 * dediğinde geriye bakacak hiçbir şey kalmıyordu.
 * </p>
 *
 * <p>
 * Aynı hata için yeni satır açılmaz; sayaç artar. Döngüye giren tek bir hata
 * listeyi binlerce satırla doldurup diğerlerini görünmez kılardı.
 * </p>
 */
export default function SystemErrors() {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [sayfa, setSayfa] = useState(1);
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [ara, setAra] = useState('');
  const [cozulmemis, setCozulmemis] = useState(true);
  const [temizlensin, setTemizlensin] = useState(false);

  useEffect(() => {
    const z = setTimeout(() => {
      setAra(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const liste = useQuery({
    queryKey: ['hata', 'liste', sayfa, ara, cozulmemis] as const,
    queryFn: () =>
      api.get<PagedResult<ErrorSummary>>(
        `/hata${queryString({
          sayfa,
          boyut: 30,
          ara,
          cozuldu: cozulmemis ? false : undefined,
        })}`,
      ),
    placeholderData: keepPreviousData,
    // Hata listesi canlı bir pano; arka planda tazelensin.
    refetchInterval: 60_000,
  });

  const temizle = useMutation({
    mutationFn: () => api.delete<number>('/hata/cozulenler'),
    onSuccess: (adet) => {
      qc.invalidateQueries({ queryKey: ['hata'] });
      setTemizlensin(false);
      bildir('basari', `${number(adet ?? 0)} çözülmüş kayıt silindi`);
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  const satirlar = liste.data?.veriler ?? [];

  return (
    <div className="space-y-3.5">
      {/*
        ARAÇ ÇUBUĞU KARTTAN ÇIKARILDI.

        Üç denetim bir <c>Kart</c>'ın içinde alt alta duruyordu: arama,
        anahtar ve "Çözülenleri temizle" — sonuncusu esnek sütunda kendini
        ORTALIYOR, altına bir de "N kayıt" satırı geliyordu. 390px'te kutu
        370px yükseliyor, yarısı boş kalıyor ve liste ilk ekrandan tamamen
        çıkıyordu. Kayıt sayısı zaten listenin altındaki sayfalayıcıda yazılı;
        aynı sayıyı iki kez yazmak yer harcamanın en sessiz yolu.

        Diğer liste ekranlarının (talepler, özgeçmişler) araç çubuğu da
        kartsız: denetimler doğrudan sayfa zemininde duruyor.
      */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Mesaj, tür veya uç ara"
          aria-label="Hatalarda ara"
          ikon={<Search size={15} />}
          className="sm:max-w-[340px] sm:flex-1"
        />

        <div className="flex items-center justify-between gap-2 sm:ml-auto sm:justify-end">
          <Switch
            isaretli={cozulmemis}
            degistir={(a) => {
              setCozulmemis(a);
              setSayfa(1);
            }}
            etiket="Yalnızca çözülmemiş"
          />
          {/* Etiket dar ekranda kısalır ama KAYBOLMAZ: silme eyleminin
              yalnızca çöp kutusu simgesiyle durması riskli. */}
          <Button varyant="ikincil" onClick={() => setTemizlensin(true)} className="shrink-0">
            <Trash2 size={14} />
            <span className="sm:hidden">Temizle</span>
            <span className="hidden sm:inline">Çözülenleri temizle</span>
          </Button>
        </div>
      </div>

      {liste.isLoading ? (
        <SkeletonRows adet={8} />
      ) : satirlar.length === 0 ? (
        <EmptyState
          ikon={CheckCircle2}
          baslik={cozulmemis ? 'Çözülmemiş hata yok' : 'Kayıt yok'}
          aciklama="Sunucuda beklenmeyen bir hata oluştuğunda burada listelenir."
        />
      ) : (
        <>
          <Card className="divide-y divide-border">
            {satirlar.map((h) => (
              <Link
                key={h.id}
                to={`/hatalar/${h.id}`}
                className="flex items-start gap-3 px-3.5 py-3 transition-colors hover:bg-surface-2"
              >
                <span
                  className={cn(
                    'mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-md',
                    h.cozuldu
                      ? 'bg-(--st-ok-bg) text-(--st-ok)'
                      : 'bg-(--st-no-bg) text-(--st-no)',
                  )}
                  aria-hidden
                >
                  {h.cozuldu ? <Check size={15} /> : <AlertTriangle size={15} />}
                </span>

                {/*
                  ÖNCE UÇ, SONRA MESAJ.

                  Başta mesaj üstteydi ve tek satıra kırpılıyordu: aynı
                  istisna dört ayrı uçtan geldiğinde liste dört özdeş satıra
                  dönüyor ("An exception has been raised that is lik…"),
                  hangisinin nerede olduğunu ayırt etmek imkânsızlaşıyordu.
                  Kaydı ayıran şey UÇ; mesaj onun açıklaması.
                */}
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium tabular-nums">
                    <span className="text-text-3">{h.yontem}</span> {h.yol}
                  </p>
                  <p className="mt-0.5 text-xs leading-[1.45] text-text-2 satir-2 wrap-anywhere">
                    {h.mesaj}
                  </p>
                  {/* Tam damga DEĞİL, bağıl zaman: hata konsolunda önemli
                      olan "ne zamandı" değil "ne kadar yeni". Tam tarih
                      ayrıntı sayfasında yazılı. */}
                  <p className="mt-0.5 truncate text-2xs text-text-3">
                    {h.kisaTur}
                    {h.kullaniciAdi ? ` · ${h.kullaniciAdi}` : ''}
                    {' · '}
                    <span title={dateTime(h.sonGorulme)}>{relativeTime(h.sonGorulme)}</span>
                  </p>
                </div>

                {(h.adet ?? 0) > 1 && (
                  <span
                    className="mt-0.5 shrink-0 rounded-full bg-(--st-no-bg) px-2 py-0.5 text-2xs font-semibold tabular-nums text-(--st-no)"
                    title={`${h.adet} kez oluştu`}
                  >
                    ×{h.adet}
                  </span>
                )}
              </Link>
            ))}
          </Card>

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="kayıt" />
        </>
      )}

      <ConfirmDialog
        acik={temizlensin}
        kapat={() => setTemizlensin(false)}
        baslik="Çözülenler silinsin mi?"
        aciklama="Yalnızca çözüldü işaretli kayıtlar silinir; üzerinde çalışılanlara dokunulmaz."
        onayEtiketi="Temizle"
        yikici
        onayla={() => temizle.mutate()}
      />
    </div>
  );
}

/**
 * Hata detayı.
 *
 * <p>
 * Ekranın en önemli düğmesi <b>"AI için kopyala"</b>: metin sunucuda üretiliyor
 * ve ham veri dökümü değil — ne olduğu, nerede olduğu, hangi istekle
 * tetiklendiği ve ne beklendiği sırayla yazılı. Doğrudan bir ajana
 * yapıştırılabilir.
 * </p>
 */
export function SystemErrorDetail() {
  const { id } = useParams<{ id: string }>();
  const hataId = Number(id);
  const gezin = useNavigate();
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [notlar, setNotlar] = useState('');
  const [cozuldu, setCozuldu] = useState(false);
  const [kopyalandi, setKopyalandi] = useState(false);
  const [silinsin, setSilinsin] = useState(false);

  const hata = useQuery({
    queryKey: ['hata', 'detay', hataId] as const,
    queryFn: () => api.get<ErrorDetail>(`/hata/${hataId}`),
    enabled: Number.isFinite(hataId),
  });

  // Kayıt gelince form alanlarını bir kez doldur.
  const [yuklendi, setYuklendi] = useState(false);
  if (hata.data && !yuklendi) {
    setYuklendi(true);
    setNotlar(hata.data.notlar ?? '');
    setCozuldu(hata.data.cozuldu === true);
  }

  const kaydet = useMutation({
    mutationFn: () => api.put<ErrorDetail>(`/hata/${hataId}`, { notlar, cozuldu }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['hata'] });
      bildir('basari', 'Kaydedildi');
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: () => api.delete<void>(`/hata/${hataId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['hata'] });
      bildir('basari', 'Kayıt silindi');
      gezin('/hatalar');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  async function kopyala(metin: string) {
    try {
      await navigator.clipboard.writeText(metin);
      setKopyalandi(true);
      setTimeout(() => setKopyalandi(false), 2000);
    } catch {
      // Pano izni yoksa (HTTP üzerinden açıldığında olur) kullanıcı metni
      // elle seçebilsin diye sessizce geçiyoruz; sahte bir başarı göstermek
      // kopyalandığını sanıp yapıştırmaya çalışmasına yol açardı.
      bildir('uyari', 'Panoya kopyalanamadı', 'Metni elle seçip kopyalayın.');
    }
  }

  if (hata.isLoading) {
    return (
      <div className="space-y-3.5">
        <Skeleton className="h-24 w-full" />
        <SkeletonRows adet={5} />
      </div>
    );
  }

  if (hata.isError || !hata.data) {
    return (
      <EmptyState
        ikon={Bug}
        baslik="Kayıt bulunamadı"
        aciklama={(hata.error as Error)?.message}
        eylem={
          <Button varyant="ikincil" onClick={() => gezin('/hatalar')}>
            Listeye dön
          </Button>
        }
      />
    );
  }

  const h = hata.data;

  return (
    <div className="space-y-3.5">
      <div className="flex items-start gap-2.5">
        <IconButton etiket="Geri" onClick={() => gezin('/hatalar')}>
          <ArrowLeft size={17} />
        </IconButton>

        <div className="min-w-0 flex-1">
          <h1 className="wrap-anywhere font-baslik text-lg font-semibold leading-tight md:text-xl">
            {h.mesaj}
          </h1>
          <p className="truncate text-sm text-text-3">
            {h.tur}
          </p>
        </div>

        <Button
          onClick={() => kopyala(h.aiRaporu ?? '')}
          className={kopyalandi ? 'pointer-events-none' : undefined}
        >
          {kopyalandi ? <Check size={14} /> : <Copy size={14} />}
          {kopyalandi ? 'Kopyalandı' : 'AI için kopyala'}
        </Button>
      </div>

      {/* ── Künye ── */}
      <Card className="p-4">
        {/*
          `min-w-0` ŞART: ızgara hücresinin varsayılan en küçük genişliği
          `auto`, yani içeriğin en-küçük-içerik ölçüsü. Yığın izindeki dosya
          yolu gibi boşluksuz uzun bir metin hücreyi itiyordu ve etki
          yukarı doğru yayılıp BELGEYİ 390px'ten 628px'e çıkarıyordu —
          kullanıcının "menüler sağa taşıyor, tüm düzen bozuluyor" dediği şey
          buydu: sayfanın tamamı yatay kayıyordu.
        */}
        <dl className="grid gap-x-6 gap-y-2 text-sm sm:grid-cols-2 [&>*]:min-w-0">
          <Satir etiket="Uç" deger={`${h.yontem ?? ''} ${h.yol ?? ''}${h.sorguDizesi ?? ''}`} />
          <Satir etiket="Durum kodu" deger={String(h.durumKodu ?? '')} />
          <Satir
            etiket="Konum"
            deger={h.dosya ? `${h.dosya}${h.satir ? `:${h.satir}` : ''}` : null}
          />
          <Satir etiket="Kaç kez" deger={`${number(h.adet ?? 0)} kez`} />
          <Satir etiket="İlk görülme" deger={dateTime(h.ilkGorulme)} />
          <Satir etiket="Son görülme" deger={dateTime(h.sonGorulme)} />
          <Satir etiket="Kullanıcı" deger={h.kullaniciAdi} />
          <Satir etiket="IP" deger={h.ipAdresi} />
          <Satir etiket="İstemci" deger={h.istemci} />
          <Satir etiket="İz kimliği" deger={h.izKimligi} />
        </dl>
      </Card>

      {/* ── Çözüm takibi ── */}
      <Card>
        <CardHeader
          baslik="Çözüm"
          aciklama="Not ekleyin; kayıt yeniden görülürse çözüldü işareti otomatik kalkar."
        />
        <div className="space-y-3 p-4">
          <Switch
            isaretli={cozuldu}
            degistir={setCozuldu}
            etiket="Çözüldü"
            aciklama={
              h.cozulmeTarihi
                ? `${dateTime(h.cozulmeTarihi)} · ${h.cozenKullanici ?? ''}`
                : undefined
            }
          />

          <FieldWrapper etiket="Notlar" id="h-not">
            <Textarea
              id="h-not"
              value={notlar}
              onChange={(e) => setNotlar(e.target.value)}
              rows={4}
              placeholder="Kök neden, yapılan değişiklik, açılan iş kaydı…"
            />
          </FieldWrapper>

          <div className="flex justify-end gap-2">
            <Button varyant="yikici" onClick={() => setSilinsin(true)}>
              <Trash2 size={14} />
              Sil
            </Button>
            <Button onClick={() => kaydet.mutate()} disabled={kaydet.isPending}>
              {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </Button>
          </div>
        </div>
      </Card>

      {/* ── Teknik ayrıntı ── */}
      <Accordion>
        {h.icMesaj && (
          <AccordionSection deger="ic" baslik="İç hata" ikon={<AlertTriangle size={15} />}>
            <Blok metin={h.icMesaj} />
          </AccordionSection>
        )}

        {h.govde && (
          <AccordionSection deger="govde" baslik="İstek gövdesi" ikon={<Bug size={15} />}>
            <Blok metin={h.govde} kopyala={kopyala} />
          </AccordionSection>
        )}

        {h.basliklar && (
          <AccordionSection deger="basliklar" baslik="İstek başlıkları" ikon={<Bug size={15} />}>
            <Blok metin={h.basliklar} />
          </AccordionSection>
        )}

        <AccordionSection deger="yigin" baslik="Yığın izi" ikon={<Bug size={15} />}>
          <Blok metin={h.yiginIzi ?? '(yok)'} kopyala={kopyala} />
        </AccordionSection>

        <AccordionSection deger="rapor" baslik="AI raporu (önizleme)" ikon={<Copy size={15} />}>
          <Blok metin={h.aiRaporu ?? ''} kopyala={kopyala} />
        </AccordionSection>
      </Accordion>

      <ConfirmDialog
        acik={silinsin}
        kapat={() => setSilinsin(false)}
        baslik="Kayıt silinsin mi?"
        aciklama="Hata kaydı ve notları kalıcı olarak silinir."
        onayEtiketi="Sil"
        yikici
        onayla={() => sil.mutate()}
      />
    </div>
  );
}

function Satir({ etiket, deger }: { etiket: string; deger?: string | null }) {
  if (!deger || !deger.trim()) return null;
  return (
    /*
      `wrap-anywhere`, `wrap-break-word` DEĞİL.

      İkisi de uzun kelimeyi bölüyor ama yalnızca `anywhere` **en-küçük-içerik
      ölçüsünü** de küçültüyor. `break-word` ile satır görsel olarak bölünüyor,
      esnek kutu ise hâlâ bölünmemiş genişliği talep ediyor ve kap taşıyordu.
      Etiket de mobilde daralıyor: 92px sabit sütun, 390px'te değere 250px
      bırakıyordu.
    */
    <div className="flex min-w-0 items-baseline gap-2 sm:gap-3">
      <dt className="w-[76px] shrink-0 text-text-3 sm:w-[92px]">{etiket}</dt>
      <dd className="min-w-0 flex-1 wrap-anywhere font-medium">{deger}</dd>
    </div>
  );
}

/** Tek aralıklı, yatay kayan metin bloğu. */
function Blok({ metin, kopyala }: { metin: string; kopyala?: (m: string) => void }) {
  return (
    <div className="relative">
      {kopyala && (
        <IconButton
          etiket="Kopyala"
          onClick={() => kopyala(metin)}
          className="absolute right-1 top-1 z-10 bg-surface/90"
        >
          <Copy size={14} />
        </IconButton>
      )}
      <pre className="max-h-[420px] overflow-auto rounded-sm bg-sunken p-3 text-xs leading-[1.55]">
        {metin}
      </pre>
    </div>
  );
}
