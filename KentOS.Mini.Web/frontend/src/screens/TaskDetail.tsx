import {
  ArrowLeft, Building2, Calendar, CheckCircle2, ClipboardCheck, MapPin,
  MessageSquare, Paperclip, Pencil, Trash2, User, Users,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Button, IconButton } from '../components/Button';
import { Card, CardHeader } from '../components/Card';
import { ColoredBadge } from '../components/Color';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { Skeleton } from '../components/Skeleton';
import { Tabs } from '../components/Tabs';
import { Timeline, DegisiklikSatiri, type TimelineItem } from '../components/Timeline';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { dateTime, shortDate } from '../data/format';
import { useTask, useTaskEvents, useTaskMutations } from '../data/tasks';
import { TASK_STAGE_STATUS, TASK_STATUS } from '../data/types';
import { SlaBadge, StageProgress } from './task/TaskBits';
import { TaskStages } from './task/TaskStages';
import { TaskAssignments } from './task/TaskAssignments';
import { TaskDiscussion } from './task/TaskDiscussion';
import { StatusDialog } from './task/StatusDialog';

const GOREV_SEKMELERI = ['akis', 'tartisma', 'gecmis'] as const;
type Sekme = (typeof GOREV_SEKMELERI)[number];

/**
 * GÖREV DETAYI — işin yürütüldüğü ekran.
 *
 * <p>
 * <b>Düğmeler sunucudan geliyor.</b> Hangi duruma geçilebileceğini
 * <code>sonrakiDurumlar</code> söylüyor; istemci kendi kurallarını
 * hesaplasaydı iki istemci farklı düğme gösterir ve biri her zaman sunucudan
 * geri çevrilirdi.
 * </p>
 *
 * <p>
 * <b>Onay ve iade ayrı bir kapı.</b> Durum düğmeleri arasında yer almıyorlar
 * çünkü ayrı bir izne bağlılar (<code>gorev.onayla</code>). Aynı yerde
 * dursalardı görevi yürüten kişi kendi işini onaylıyormuş gibi görünürdü.
 * </p>
 */
export default function TaskDetail() {
  const { id } = useParams();
  const gorevId = Number(id);
  const gezin = useNavigate();
  const { bildir } = useToast();
  const { hasPermission } = useSession();

  /*
    ETKİN SEKME URL'DE (`?sekme=pano`).

    Bileşen içinde tutulan sekme, görsel turun ve derin bağlantının
    erişemediği bir ekran demek: pano ve gantt tek başına açılamıyor,
    dolayısıyla oradaki davranış hiç doğrulanamıyordu. Ajanda ve yönetim
    ekranlarındaki gerekçenin aynısı.
  */
  const [sorgu, setSorgu] = useSearchParams();
  const sekmeDegeri = sorgu.get('sekme') as Sekme | null;
  const sekme: Sekme =
    sekmeDegeri && GOREV_SEKMELERI.includes(sekmeDegeri) ? sekmeDegeri : 'akis';

  const setSekme = (d: Sekme) => {
    if (d === 'akis') sorgu.delete('sekme');
    else sorgu.set('sekme', d);
    setSorgu(sorgu, { replace: true });
  };
  const [silOnayi, setSilOnayi] = useState(false);
  const [durumIstegi, setDurumIstegi] = useState<{ durum: number; ad: string } | null>(null);

  const { data: gorev, isLoading, isError, error } = useTask(gorevId);
  const olaylar = useTaskEvents(gorevId);
  const m = useTaskMutations(gorevId);

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-8 w-52" />
        <Skeleton className="h-32 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (isError || !gorev) {
    return (
      <EmptyState
        ikon={ClipboardCheck}
        baslik="Görev bulunamadı"
        aciklama={(error as Error)?.message ?? 'Bu görev silinmiş ya da biriminizin dışında olabilir.'}
        eylem={
          <Link to="/gorevler">
            <Button varyant="ikincil">
              <ArrowLeft size={14} />
              Görevlere dön
            </Button>
          </Link>
        }
      />
    );
  }

  const kapali = gorev.durum === TASK_STATUS.tamamlandi || gorev.durum === TASK_STATUS.iptal;
  const onayda = gorev.durum === TASK_STATUS.onayBekliyor;

  /*
    "Tamamla" düğmesi ONAY KAPISINDAN ÖNCE.

    Sunucu bu geçişi `sonrakiDurumlar` içinde `Onay bekliyor` olarak veriyor
    ama kullanıcıya "durumu Onay bekliyor yap" demek işin ne olduğunu
    anlatmıyor: personel "bitirdim" diyor, kabul yöneticinin işi. Bu yüzden
    o geçiş listeden ÇIKARILIP kendi düğmesine alınıyor.
  */
  const durumDugmeleri = (gorev.sonrakiDurumlar ?? []).filter(
    (d) => d.durum !== TASK_STATUS.onayBekliyor,
  );
  const tamamlanabilir = (gorev.sonrakiDurumlar ?? []).some(
    (d) => d.durum === TASK_STATUS.onayBekliyor,
  );

  /*
    ZORUNLU AŞAMA KALDIYSA "Tamamla" ÇALIŞMAZ.

    Sunucu bu geçişi `sonrakiDurumlar`da veriyor çünkü DURUM olarak geçerli;
    zorunlu aşama denetimi çağrı anında yapılıyor. Düğmeyi buna bakmadan
    etkin çizmek, tıklayınca kesinlikle hata alacak bir düğme sunmak olurdu —
    ekran görüntüsünde tam olarak öyle görünüyordu: ikinci aşama fotoğraf
    beklerken üstteki Tamamla düğmesi etkindi.

    Kural yine SUNUCUDA; buradaki hesap yalnızca düğmenin doğru şeyi
    söylemesini sağlıyor.
  */
  const eksikZorunlu = (gorev.asamalar ?? []).filter(
    (a) => a.zorunlu && a.durum === TASK_STAGE_STATUS.bekliyor,
  );

  async function durumUygula(durum: number, gerekce?: string) {
    try {
      // Onay ve iade AYRI uçtan: sunucu da bunu zorunlu tutuyor.
      if (durum === TASK_STATUS.tamamlandi || durum === TASK_STATUS.iadeEdildi) {
        await m.onay.mutateAsync({ durum: durum as never, gerekce });
      } else {
        await m.durum.mutateAsync({ durum: durum as never, gerekce });
      }
      setDurumIstegi(null);
      bildir('basari', 'Görev güncellendi');
    } catch (h) {
      bildir('hata', 'Durum değiştirilemedi', (h as Error).message);
    }
  }

  const cizelge: TimelineItem[] = (olaylar.data ?? []).map((o) => ({
    id: o.id!,
    baslik: o.tipAd ?? '',
    altBaslik: o.kullanici ?? undefined,
    zaman: dateTime(o.tarih),
    govde:
      (o.degisiklikler ?? []).length > 0 || o.aciklama ? (
        <>
          {o.aciklama && <p className="text-xs text-text-2">{o.aciklama}</p>}
          {(o.degisiklikler ?? []).map((d, i) => (
            <DegisiklikSatiri key={i} alan={d.alan ?? ''} eski={d.eski} yeni={d.yeni} />
          ))}
        </>
      ) : undefined,
  }));

  return (
    <div className="space-y-3.5">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-2">
        <Link to="/gorevler" className="mt-0.5">
          <IconButton etiket="Görevlere dön">
            <ArrowLeft size={18} />
          </IconButton>
        </Link>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-2xs tabular-nums text-ink-3">{gorev.takipNo}</span>
            <ColoredBadge etiket={gorev.durumAd} renk={gorev.durumRenk} />
            {gorev.oncelikAd && gorev.oncelik !== 1 && (
              <span className="text-2xs text-ink-3">{gorev.oncelikAd} öncelik</span>
            )}
            <SlaBadge gecikti={!!gorev.gecikti} kalanSaat={gorev.kalanSaat} />
          </div>
          <h1 className="mt-1 font-display text-lg font-bold leading-tight text-ink">
            {gorev.baslik}
          </h1>
        </div>

        {!kapali && hasPermission(PERMISSION.gorevDuzenle) && (
          <Link to={`/gorevler/${gorevId}/duzenle`}>
            <IconButton etiket="Düzenle">
              <Pencil size={17} />
            </IconButton>
          </Link>
        )}
        {hasPermission(PERMISSION.gorevSil) && (
          <IconButton etiket="Sil" onClick={() => setSilOnayi(true)}>
            <Trash2 size={17} />
          </IconButton>
        )}
      </div>

      {/* ── Künye ── */}
      <Card className="p-3.5">
        {gorev.aciklama && (
          <p className="mb-3 whitespace-pre-wrap text-sm leading-relaxed text-text-2">
            {gorev.aciklama}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs">
          {gorev.gorevTipiAd && (
            <Kunye ikon={<ClipboardCheck size={14} />}>{gorev.gorevTipiAd}</Kunye>
          )}
          {gorev.birimAd && <Kunye ikon={<Building2 size={14} />}>{gorev.birimAd}</Kunye>}
          {gorev.adres && <Kunye ikon={<MapPin size={14} />}>{gorev.adres}</Kunye>}
          {gorev.planlananBitis && (
            <Kunye ikon={<Calendar size={14} />}>
              Hedef {shortDate(gorev.planlananBitis)}
            </Kunye>
          )}
          {gorev.olusturan && <Kunye ikon={<User size={14} />}>{gorev.olusturan}</Kunye>}
          {gorev.kaynakAd && <span className="text-ink-3">{gorev.kaynakAd}</span>}
        </div>

        {/*
          VEKÂLET İZİ. Görev başka bir birim adına açıldıysa bunu göstermek
          gerekiyor: "bu işi bize kim yazdı?" sorusunun cevabı kaydın kendi
          alanlarında değil, burada.
        */}
        {gorev.olusturanBirimAd && gorev.olusturanBirimId !== gorev.birimId && (
          <p className="mt-2.5 text-2xs text-ink-3">
            {gorev.olusturanBirimAd} tarafından bu birim adına açıldı.
          </p>
        )}

        {gorev.gerekce && (
          <p className="mt-3 rounded-sm bg-sunken px-2.5 py-2 text-xs text-text-2">
            <span className="font-medium text-ink-2">Gerekçe: </span>
            {gorev.gerekce}
          </p>
        )}

        {gorev.onaylayan && (
          <p className="mt-2 text-2xs text-(--st-ok)">
            {gorev.onaylayan} onayladı
            {gorev.tamamlanmaTarihi && ` · ${dateTime(gorev.tamamlanmaTarihi)}`}
          </p>
        )}
      </Card>

      {/* ── Eylemler ── */}
      {!kapali && (
        <div className="flex flex-wrap items-center gap-2">
          {hasPermission(PERMISSION.gorevDuzenle) &&
            durumDugmeleri.map((d) => (
              <Button
                key={d.durum}
                varyant={d.durum === TASK_STATUS.iptal ? 'yikici' : 'ikincil'}
                onClick={() => setDurumIstegi({ durum: d.durum!, ad: d.ad ?? '' })}
              >
                {d.ad}
              </Button>
            ))}

          {tamamlanabilir && hasPermission(PERMISSION.gorevAsama) && (
            <Button
              onClick={async () => {
                try {
                  await m.tamamlanmayaGonder.mutateAsync();
                  bildir('basari', 'Görev onaya gönderildi');
                } catch (h) {
                  bildir('hata', 'Onaya gönderilemedi', (h as Error).message);
                }
              }}
              disabled={m.tamamlanmayaGonder.isPending || eksikZorunlu.length > 0}
              title={
                eksikZorunlu.length > 0
                  ? `Önce şu zorunlu aşamalar tamamlanmalı: ${eksikZorunlu
                      .map((a) => a.ad)
                      .join(', ')}`
                  : 'Görevi onaya gönderir'
              }
            >
              <CheckCircle2 size={15} />
              Tamamla
            </Button>
          )}

          {/*
            ONAY KAPISI. Yalnızca görev onay beklerken ve yalnızca
            `gorev.onayla` izniyle. Modülün en önemli tek kuralı burada
            görünür oluyor: beyan ile kabul ayrı işler.
          */}
          {onayda && hasPermission(PERMISSION.gorevOnayla) && (
            <>
              <Button
                varyant="onay"
                onClick={() => durumUygula(TASK_STATUS.tamamlandi)}
                disabled={m.onay.isPending}
              >
                <CheckCircle2 size={15} />
                Onayla
              </Button>
              <Button
                varyant="yikici"
                onClick={() =>
                  setDurumIstegi({ durum: TASK_STATUS.iadeEdildi, ad: 'İade et' })
                }
              >
                İade et
              </Button>
            </>
          )}
        </div>
      )}

      {/* ── Sekmeler ── */}
      <Tabs<Sekme>
        deger={sekme}
        degistir={setSekme}
        sekmeler={[
          { deger: 'akis', etiket: 'Akış' },
          { deger: 'tartisma', etiket: 'Dosya ve yorum' },
          { deger: 'gecmis', etiket: 'Geçmiş', sayi: cizelge.length },
        ]}
      />

      {sekme === 'akis' && (
        <div className="space-y-3.5">
          <TaskStages gorev={gorev} />
          <TaskAssignments gorev={gorev} />

          {(gorev.altGorevler ?? []).length > 0 && (
            <Card>
              <CardHeader
                baslik="Alt görevler"
                aciklama={`${gorev.altGorevler!.length} parça`}
              />
              <div className="divide-y divide-line">
                {gorev.altGorevler!.map((a) => (
                  <Link
                    key={a.id}
                    to={`/gorevler/${a.id}`}
                    className="flex items-center gap-2.5 px-3.5 py-2.5 hover:bg-sunken"
                  >
                    <ColoredBadge etiket={a.durumAd} renk={a.durumRenk} />
                    <span className="min-w-0 flex-1 truncate text-sm text-ink">{a.baslik}</span>
                    <StageProgress biten={a.asamaBiten ?? 0} toplam={a.asamaToplam ?? 0} />
                  </Link>
                ))}
              </div>
            </Card>
          )}

          {!kapali && hasPermission(PERMISSION.gorevEkle) && (
            <Link to={`/gorevler/yeni?ust=${gorevId}`}>
              <Button varyant="ikincil" className="w-full md:w-auto">
                <Users size={15} />
                Alt görev aç
              </Button>
            </Link>
          )}
        </div>
      )}

      {sekme === 'tartisma' && <TaskDiscussion gorevId={gorevId} kapali={kapali} />}

      {sekme === 'gecmis' && (
        <Card className="p-3.5">
          {cizelge.length === 0 ? (
            <EmptyState ikon={MessageSquare} baslik="Kayıt yok" />
          ) : (
            <Timeline ogeler={cizelge} />
          )}
        </Card>
      )}

      {/* ── Kutular ── */}
      <StatusDialog
        istek={durumIstegi}
        kapat={() => setDurumIstegi(null)}
        onayla={durumUygula}
        bekliyor={m.durum.isPending || m.onay.isPending}
      />

      <ConfirmDialog
        acik={silOnayi}
        kapat={() => setSilOnayi(false)}
        baslik="Görev silinsin mi?"
        aciklama={
          'Görev, alt görevleri, dosyaları, yorumları ve geçmişi kalıcı olarak silinir. ' +
          'Yapılmayacak bir işi kapatmak için silmek yerine İPTAL kullanın.'
        }
        onayEtiketi="Sil"
        yikici
        onayla={async () => {
          try {
            await m.sil.mutateAsync(gorevId);
            bildir('basari', 'Görev silindi');
            gezin('/gorevler');
          } catch (h) {
            bildir('hata', 'Görev silinemedi', (h as Error).message);
          }
        }}
      />
    </div>
  );
}

function Kunye({ ikon, children }: { ikon: React.ReactNode; children: React.ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-text-2">
      <span className="text-text-3">{ikon}</span>
      {children}
    </span>
  );
}

/** Dosya sekmesinde kullanılan ikonlar dışa aktarılıyor — tek import noktası. */
export { Paperclip as TaskFileIcon };
