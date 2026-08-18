import {
  ArrowLeft, Building2, Calendar, CheckCircle2, ClipboardCheck, ClipboardList,
  Flag, MapPin, MessageSquare, Paperclip, Pencil, Play, Trash2, User, Users,
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
import { TASK_STAGE_STATUS, TASK_STATUS, type TaskDetail as Gorev } from '../data/types';
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
 *
 * <h3>Yerleşim — ölçülmüş üç kusur üzerine yeniden kuruldu</h3>
 * <ol>
 *   <li><b>İşin ne kadarının bittiği hiç yazmıyordu.</b> Aşamaların
 *       tamamlandığı ekranda ilerleme çubuğu yoktu; tek ipucu bir kart
 *       başlığındaki gri "4 aşamanın 2 tanesi kapandı" satırıydı. Artık
 *       başlığın hemen altında yüzde, çubuk ve <b>sıradaki adım</b> var.</li>
 *   <li><b>Birincil eylem telefonda kayboluyordu.</b> "Başlat" ve "Tamamla"
 *       künye kartıyla sekmelerin arasında, sarmalanan bir sırada duruyordu:
 *       390px'te açıklamalı bir görevde katlamanın altında kalıyor,
 *       aşamalara kaydırınca büsbütün ekrandan çıkıyordu. <code>design.md
 *       §5.2</code> detay ekranları için <b>yapışkan alt eylem çubuğu</b>
 *       tarif ediyor — hiç yapılmamıştı; şimdi var.</li>
 *   <li><b>Masaüstünde ekranın yarısı boştu.</b> <code>design.md §5.3</code>
 *       detay ekranını <code>1.85fr 1fr</code> olarak veriyor; bu ekran tek
 *       dar sütundu. 1440px'te aşama listesi 1200px genişliğe yayılıp 40px
 *       içerik taşıyordu. Artık talep ve etkinlik detaylarıyla aynı iki
 *       sütunlu gramerde.</li>
 * </ol>
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
        <Skeleton className="h-24 w-full" />
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

  async function onayaGonder() {
    try {
      await m.tamamlanmayaGonder.mutateAsync();
      bildir('basari', 'Görev onaya gönderildi');
    } catch (h) {
      bildir('hata', 'Onaya gönderilemedi', (h as Error).message);
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

  /*
    EYLEMLER TEK YERDE TANIMLANIP İKİ YERDE ÇİZİLİYOR: masaüstünde yan
    sütunun üstünde, mobilde yapışkan alt çubukta. İkisini ayrı ayrı yazmak,
    yeni bir düğme eklendiğinde birinde unutulmasını garanti ederdi.
  */
  const engel =
    eksikZorunlu.length > 0
      ? `Önce tamamlanmalı: ${eksikZorunlu.map((a) => a.ad).join(', ')}`
      : null;

  const birincil = tamamlanabilir && hasPermission(PERMISSION.gorevAsama) ? (
    <Button
      onClick={onayaGonder}
      disabled={m.tamamlanmayaGonder.isPending || !!engel}
      className="flex-1"
      title={engel ?? 'Görevi onaya gönderir'}
    >
      <CheckCircle2 size={15} />
      Tamamla
    </Button>
  ) : null;

  const onayDugmeleri = onayda && hasPermission(PERMISSION.gorevOnayla) ? (
    <>
      <Button
        varyant="onay"
        onClick={() => durumUygula(TASK_STATUS.tamamlandi)}
        disabled={m.onay.isPending}
        className="flex-1"
      >
        <CheckCircle2 size={15} />
        Onayla
      </Button>
      <Button
        varyant="yikici"
        onClick={() => setDurumIstegi({ durum: TASK_STATUS.iadeEdildi, ad: 'İade et' })}
      >
        İade et
      </Button>
    </>
  ) : null;

  const durumSecenekleri = hasPermission(PERMISSION.gorevDuzenle)
    ? durumDugmeleri.map((d) => (
        <Button
          key={d.durum}
          /*
            BAŞLATMA BİRİNCİL, GERİSİ İKİNCİL.

            Bütün geçişler aynı gri düğmeydi: "Başlat" ile "İptal et" aynı
            ağırlıkta görünüyordu ve ekranda hiçbir şey "şimdi ne yapmalıyım"
            sorusunu cevaplamıyordu.
          */
          varyant={
            d.durum === TASK_STATUS.iptal
              ? 'yikici'
              : d.durum === TASK_STATUS.basladi && !birincil
                ? 'birincil'
                : 'ikincil'
          }
          onClick={() => setDurumIstegi({ durum: d.durum!, ad: d.eylem || d.ad || '' })}
        >
          {d.durum === TASK_STATUS.basladi && <Play size={14} />}
          {/*
            EYLEM ADI, DURUM ADI DEĞİL. `ad` alanı durumun kendi adını
            veriyor ve düğmelerde "İptal edildi", "Beklemede" gibi geçmiş
            zamanlı beyanlar çıkıyordu: basılmamış bir düğmenin üzerinde
            "İptal edildi" yazması, işin zaten iptal olduğunu söylüyordu.
          */}
          {d.eylem || d.ad}
        </Button>
      ))
    : [];

  const eylemVar = !kapali && (birincil || onayDugmeleri || durumSecenekleri.length > 0);

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
              <span className="inline-flex items-center gap-1 text-2xs text-ink-3">
                <Flag size={11} />
                {gorev.oncelikAd}
              </span>
            )}
            <SlaBadge gecikti={!!gorev.gecikti} kalanSaat={gorev.kalanSaat} />
          </div>
          <h1 className="mt-1 font-display text-lg font-bold leading-tight text-ink metin-guzel md:text-xl">
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

      <IlerlemeSeridi gorev={gorev} />

      <div className="grid gap-3.5 lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* ── Sol sütun: işin kendisi ── */}
        <div className="min-w-0 space-y-3.5">
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
                        <span className="min-w-0 flex-1 truncate text-sm text-ink">
                          {a.baslik}
                        </span>
                        <StageProgress
                          biten={a.asamaBiten ?? 0}
                          toplam={a.asamaToplam ?? 0}
                          ilerleme={a.ilerleme}
                        />
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
        </div>

        {/* ── Sağ sütun: kim, ne, nerede ── */}
        <div className="min-w-0 space-y-3.5">
          {/*
            EYLEMLER MASAÜSTÜNDE BURADA, MOBİLDE YAPIŞKAN ALT ÇUBUKTA.
            İki yerde birden çizilseydi telefonda aynı düğme iki kez
            görünürdü.
          */}
          {eylemVar && (
            <div className="hidden lg:block">
              <div className="flex flex-wrap gap-2">
                {birincil}
                {onayDugmeleri}
                {durumSecenekleri}
              </div>
              {birincil && engel && (
                <p className="mt-1.5 text-2xs text-(--st-wait)">{engel}</p>
              )}
            </div>
          )}

          <Kunye gorev={gorev} />
          <TaskAssignments gorev={gorev} />
        </div>
      </div>

      {/* ── Mobil yapışkan eylem çubuğu (design.md §5.2) ── */}
      {eylemVar && (
        <div
          className="sticky z-10 -mx-4 mt-1 border-t border-border bg-surface/95 px-4 py-2.5 backdrop-blur-sm lg:hidden
            bottom-[calc(var(--h-tabbar)+env(safe-area-inset-bottom,0px))]
            shadow-[0_-2px_12px_rgba(16,26,45,.06)] dark:shadow-none"
        >
          {/*
            ENGELİN SEBEBİ YAZILI DURUYOR.

            Sebep yalnızca `title` içindeydi; dokunmatik ekranda ipucu diye
            bir şey yok. Sonuç: telefonda tam genişlikte, soluk ve basılmayan
            bir "Tamamla" düğmesi — neden çalışmadığını söylemeyen bir çıkmaz.
          */}
          {birincil && engel && (
            <p className="mb-1.5 text-2xs text-(--st-wait)">{engel}</p>
          )}

          <div className="flex items-center gap-2">
          {birincil}
          {onayDugmeleri}
          {/*
            Alt çubukta YALNIZCA bir tane ikincil geçiş duruyor (genelde
            "Başlat" ya da "Beklemeye al"). Gerisi künyenin altındaki tam
            listede: dört düğmeyi 390px'e sığdırmak, hepsini okunamayacak
            kadar küçültmek olurdu.
          */}
          {!birincil && !onayDugmeleri && durumSecenekleri.slice(0, 2)}
          </div>
        </div>
      )}

      {/* Alt çubuğa sığmayan geçişler — mobilde tam liste. */}
      {eylemVar && (birincil || onayDugmeleri) && durumSecenekleri.length > 0 && (
        <div className="flex flex-wrap gap-2 lg:hidden">{durumSecenekleri}</div>
      )}
      {eylemVar && !birincil && !onayDugmeleri && durumSecenekleri.length > 2 && (
        <div className="flex flex-wrap gap-2 lg:hidden">{durumSecenekleri.slice(2)}</div>
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

/**
 * İLERLEME ŞERİDİ — "işin ne kadarı bitti, sırada ne var?"
 *
 * <p>
 * Bu blok yoktu. Aşamaların tamamlandığı ekranda ilerlemeyi söyleyen tek şey
 * bir kart başlığındaki gri alt satırdı; yüzdeyi görmek için listeye geri
 * dönmek gerekiyordu. Sahadaki personelin telefonda ilk sorduğu iki soru
 * burada: <b>ne kadarı bitti</b> ve <b>şimdi ne yapacağım</b>.
 * </p>
 */
function IlerlemeSeridi({ gorev }: { gorev: Gorev }) {
  const asamalar = gorev.asamalar ?? [];
  const toplam = asamalar.length;
  const biten = asamalar.filter((a) => a.durum !== TASK_STAGE_STATUS.bekliyor).length;
  const oran = gorev.ilerleme ?? 0;
  const sirada = asamalar.find((a) => a.sirada);

  const bitti = gorev.durum === TASK_STATUS.tamamlandi;

  return (
    <Card className="p-3.5">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-display text-2xl font-bold tabular-nums leading-none text-ink">
          %{oran}
        </span>
        <span className="text-2xs text-ink-3">
          {toplam > 0 ? `${biten}/${toplam} aşama` : gorev.durumAd}
        </span>
      </div>

      <span className="mt-2 block h-1.5 overflow-hidden rounded-full bg-sunken" aria-hidden>
        <span
          className="block h-full rounded-full transition-[width]"
          style={{
            width: `${oran}%`,
            // Kapanmış görev YEŞİL, süren iş kurum rengi: dolu bir çubuğun
            // "bitti" mi yoksa "neredeyse bitti" mi olduğu renkten okunmalı.
            background: bitti ? 'var(--st-ok)' : 'var(--brand-ui)',
          }}
        />
      </span>

      {sirada ? (
        <p className="mt-2 text-xs text-text-2">
          <span className="text-ink-3">Sırada: </span>
          {sirada.ad}
          {sirada.fotografZorunlu && (
            <span className="ml-1.5 text-2xs text-(--st-wait)">fotoğraf zorunlu</span>
          )}
        </p>
      ) : (
        oran >= 95 &&
        !bitti && (
          <p className="mt-2 text-xs text-(--st-wait)">
            Bütün adımlar bitti — yönetici onayı bekleniyor.
          </p>
        )
      )}
    </Card>
  );
}

/** Görevin künyesi — tanım listesi olarak. */
function Kunye({ gorev }: { gorev: Gorev }) {
  return (
    <Card>
      <CardHeader baslik="Künye" />
      <div className="space-y-3 p-3.5">
        {gorev.aciklama && (
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-text-2">
            {gorev.aciklama}
          </p>
        )}

        {/*
          KÜNYE TANIM LİSTESİ, SARMALANAN BİR ÇİP SIRASI DEĞİL.

          Önceden altı bilgi (tip, birim, adres, hedef, açan, kaynak) aynı
          ağırlıkta, ikon+metin çiftleri hâlinde satır sonuna göre
          sarmalanıyordu: hangi metnin neyi anlattığı ancak ikondan tahmin
          edilebiliyordu ve "Fen İşleri Müdürlüğü" ile "Atatürk Cad. No:12"
          yan yana aynı görünüyordu. Etiketli satır, tahmini ortadan
          kaldırıyor.
        */}
        <dl className="space-y-2 text-xs">
          <Satir ikon={<ClipboardList size={13} />} etiket="Tip" deger={gorev.gorevTipiAd} />
          <Satir ikon={<Building2 size={13} />} etiket="Birim" deger={gorev.birimAd} />
          <Satir
            ikon={<MapPin size={13} />}
            etiket="Konum"
            deger={gorev.adres ?? gorev.mahalleAd}
          />
          <Satir
            ikon={<Calendar size={13} />}
            etiket="Hedef"
            deger={gorev.planlananBitis ? shortDate(gorev.planlananBitis) : null}
          />
          <Satir ikon={<User size={13} />} etiket="Açan" deger={gorev.olusturan} />
          <Satir ikon={<Flag size={13} />} etiket="Kaynak" deger={gorev.kaynakAd} />
        </dl>

        {/*
          VEKÂLET İZİ. Görev başka bir birim adına açıldıysa bunu göstermek
          gerekiyor: "bu işi bize kim yazdı?" sorusunun cevabı kaydın kendi
          alanlarında değil, burada.
        */}
        {gorev.olusturanBirimAd && gorev.olusturanBirimId !== gorev.birimId && (
          <p className="text-2xs text-ink-3">
            {gorev.olusturanBirimAd} tarafından bu birim adına açıldı.
          </p>
        )}

        {gorev.gerekce && (
          <p className="rounded-sm bg-sunken px-2.5 py-2 text-xs text-text-2">
            <span className="font-medium text-ink-2">Gerekçe: </span>
            {gorev.gerekce}
          </p>
        )}

        {gorev.onaylayan && (
          <p className="text-2xs text-(--st-ok)">
            {gorev.onaylayan} onayladı
            {gorev.tamamlanmaTarihi && ` · ${dateTime(gorev.tamamlanmaTarihi)}`}
          </p>
        )}
      </div>
    </Card>
  );
}

/** Künye satırı — boş değer hiç çizilmez. */
function Satir({
  ikon,
  etiket,
  deger,
}: {
  ikon: React.ReactNode;
  etiket: string;
  deger?: string | null;
}) {
  if (!deger) return null;

  return (
    <div className="flex items-start gap-2">
      <span className="mt-px text-text-3">{ikon}</span>
      <dt className="w-14 shrink-0 text-ink-3">{etiket}</dt>
      <dd className="min-w-0 flex-1 text-text-2">{deger}</dd>
    </div>
  );
}

/** Dosya sekmesinde kullanılan ikonlar dışa aktarılıyor — tek import noktası. */
export { Paperclip as TaskFileIcon };
