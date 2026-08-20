import {
  ArrowLeft, Building2, Calendar, CheckCircle2, ClipboardCheck, ClipboardList,
  Flag, MapPin, MessageSquare, MoreHorizontal, Paperclip, Pencil, Play, Trash2,
  User, Users, XCircle,
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
import { cn } from '../components/utils';
import { useSession } from '../auth/SessionProvider';
import { BottomSheet, SheetRow } from '../shell/mobile/BottomSheet';
import { Fab } from '../shell/mobile/Fab';
import { haptic } from '../data/haptics';
import { dateTime, shortDate } from '../data/format';
import { useTask, useTaskEvents, useTaskMutations } from '../data/tasks';
import { TASK_STAGE_STATUS, TASK_STATUS, type TaskDetail as Gorev } from '../data/types';
import { Avatar } from '../components/PersonPicker';
import { SlaBadge, StageProgress } from './task/TaskBits';
import { TaskStages } from './task/TaskStages';
import { TaskAssignments } from './task/TaskAssignments';
import { TaskDiscussion } from './task/TaskDiscussion';
import { StatusDialog } from './task/StatusDialog';

const GOREV_SEKMELERI = ['akis', 'detay', 'tartisma', 'gecmis'] as const;
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
  const [tabakaAcik, setTabakaAcik] = useState(false);
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
      haptic('basari');
      bildir('basari', 'Görev güncellendi');
    } catch (h) {
      haptic('hata');
      bildir('hata', 'Durum değiştirilemedi', (h as Error).message);
    }
  }

  async function onayaGonder() {
    try {
      await m.tamamlanmayaGonder.mutateAsync();
      haptic('basari');
      bildir('basari', 'Görev onaya gönderildi');
    } catch (h) {
      haptic('hata');
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
      {/*
        "ONAYA GÖNDER", "TAMAMLA" DEĞİL.

        Aşama satırının kendi düğmesi de "Tamamla" yazıyordu: aynı ekranda,
        aynı anda, iki farklı işi yapan iki aynı sözcük. Biri bir ADIMI
        kapatıyor, öteki bütün GÖREVİ yönetici onayına yolluyor. İkincisi
        adını yaptığı işten alıyor — modülün en önemli kuralı da zaten beyan
        ile kabulün ayrı olması.
      */}
      Onaya gönder
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

  /*
    ÇUBUĞA NE ÇIKAR?

    Yalnızca ŞU AN basılabilen birincil eylem. Onay kapısı (Onayla / İade et)
    yöneticinin o an vereceği karar; "Onaya gönder" ise ancak zorunlu
    aşamalar bittiğinde anlamlı. İkisi de yoksa alt çubuk hiç çizilmiyor —
    geçişler, düzenleme ve silme zaten başlıktaki "⋯" tabakasında.
  */
  const cubukEylemi = kapali
    ? null
    : onayDugmeleri ?? (birincil && !engel ? birincil : null);

  /*
    "⋯" TABAKASININ İÇERİĞİ.

    Çubukta yer bulamayan her şey: birincil eylem varken bütün durum
    geçişleri, yokken ilki dışındakiler — artı düzenleme ve silme. Tek yerde
    tanımlanıyor ki çubuk ile tabaka arasında bir eylem kaybolmasın.
  */
  const tabakaEylemleri: {
    etiket: string;
    ikon: React.ReactNode;
    ton?: 'normal' | 'tehlike';
    calistir: () => void;
  }[] = [
    ...(!kapali && hasPermission(PERMISSION.gorevDuzenle)
      ? durumDugmeleri
          .slice(birincil || onayDugmeleri ? 0 : 1)
          .map((d) => ({
            etiket: d.eylem || d.ad || '',
            ikon: d.durum === TASK_STATUS.iptal ? <XCircle size={18} /> : <Play size={18} />,
            ton: (d.durum === TASK_STATUS.iptal ? 'tehlike' : 'normal') as 'normal' | 'tehlike',
            calistir: () => setDurumIstegi({ durum: d.durum!, ad: d.eylem || d.ad || '' }),
          }))
      : []),

    ...(!kapali && hasPermission(PERMISSION.gorevDuzenle)
      ? [{
          etiket: 'Düzenle',
          ikon: <Pencil size={18} />,
          calistir: () => gezin(`/gorevler/${gorevId}/duzenle`),
        }]
      : []),

    /*
      SİL BU LİSTEDE DEĞİL — kendi FAB'ında.

      Tabakanın içinde, durum geçişlerinin arasında bir satırdı: geri
      alınamaz tek eylem, geri alınabilir dört eylemle aynı görünürlükte ve
      aynı dokunuş mesafesinde. Ayrı bir düğme hem onu ayırıyor hem de
      aramadan bulunmasını sağlıyor.
    */
  ];

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

          {/*
            "BU İŞ KİMDE?" BAŞLIĞIN HEMEN ALTINDA VE ONUNLA AYNI HİZADA.

            Cevap yalnızca Atamalar kartındaydı ve o kart telefonda sayfanın
            en altındaydı. Sahadaki personelin ve müdürün ilk sorusu bu; bir
            kartın arkasında durmamalı. Tam yönetim (ekle/çıkar/rol) yine
            "Detay" sekmesinde.
          */}
          {(gorev.sorumlular ?? []).length > 0 && (
            <div className="mt-1.5 flex items-center gap-1.5 text-xs text-text-2">
              <Avatar ad={gorev.sorumlular![0]} boyut="kucuk" />
              <span className="min-w-0 truncate">
                {gorev.sorumlular![0]}
                {gorev.sorumlular!.length > 1 && (
                  <span className="text-ink-3"> +{gorev.sorumlular!.length - 1}</span>
                )}
              </span>
            </div>
          )}
        </div>

        {/*
          DÜZENLE/SİL MASAÜSTÜNDE İKON, TELEFONDA TABAKADA.

          390px'lik başlık satırında geri düğmesi, takip numarası, durum
          rozeti, öncelik, SLA ve iki ikon aynı anda duruyordu; ekranda
          sayılan 25 düğme/bağlantının ilk altısı daha başlıkta bitiyordu.
          Telefonda ikisi de alt çubuktaki "⋯" tabakasında — uygulamanın
          öteki detay ekranlarında (talep, davet, çiçekçi) zaten kurulmuş
          olan gramer.
        */}
        <div className="hidden items-center gap-2 lg:flex">
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

        {/*
          Telefonda TEK düğme: geçişler, düzenleme ve silme aynı tabakada.
          Alt çubuk artık kalıcı olmadığı için bu düğmenin başlıkta durması
          şart — aksi hâlde engellenmiş bir görevde silme yolu kalmıyordu.
        */}
        {tabakaEylemleri.length > 0 && (
          <div className="lg:hidden">
            <IconButton etiket="Diğer işlemler" onClick={() => setTabakaAcik(true)}>
              <MoreHorizontal size={18} />
            </IconButton>
          </div>
        )}
      </div>

      <IlerlemeSeridi gorev={gorev} />

      <div className="grid gap-3.5 lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* ── Sol sütun: işin kendisi ── */}
        <div
          className={cn(
            'min-w-0 space-y-3.5',
            // "Detay" mobilde sağ sütunu gösteriyor; sol sütun o an sekme
            // şeridinden ibaret kalmalı.
            sekme === 'detay' ? 'lg:block' : '',
          )}
        >
          <Tabs<Sekme>
            deger={sekme}
            degistir={setSekme}
            sekmeler={[
              { deger: 'akis', etiket: 'Akış' },
              { deger: 'detay', etiket: 'Detay' },
              { deger: 'tartisma', etiket: 'Dosya' },
              { deger: 'gecmis', etiket: 'Geçmiş', sayi: cizelge.length },
            ]}
          />

          {sekme === 'akis' && (
            <div className="space-y-3.5">
              <TaskStages gorev={gorev} />

              {(gorev.altGorevler ?? []).length > 0 && (
                <Card serit>
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
            <Card serit className="p-3.5">
              {cizelge.length === 0 ? (
                <EmptyState ikon={MessageSquare} baslik="Kayıt yok" />
              ) : (
                <Timeline ogeler={cizelge} />
              )}
            </Card>
          )}
        </div>

        {/*
          ── Sağ sütun: kim, ne, nerede ──

          MASAÜSTÜNDE HER ZAMAN GÖRÜNÜR, TELEFONDA "Detay" SEKMESİNDE.

          Telefonda bu blok akışın ALTINA yığılıyordu: kullanıcı aşamaları,
          alt görevleri ve "alt görev aç" düğmesini geçtikten sonra künyeye
          ulaşıyordu — ölçüldü, sayfa 2.1 ekran boyuydu ve altı ayrı kart
          taşıyordu. Oysa künye bir REFERANS: işi yaparken değil, bir şeyi
          doğrulamak isterken bakılıyor. Sekmeye alınması varsayılan görünümü
          yarıya indiriyor.
        */}
        <div className={cn('min-w-0 space-y-3.5', sekme === 'detay' ? '' : 'hidden lg:block')}>
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

      {/*
        ── Mobil yapışkan eylem çubuğu (design.md §5.2) ──

        ÇUBUK YALNIZCA BASILABİLİR BİR EYLEM VARKEN ÇİZİLİYOR.

        Önceki hâli her zaman duruyordu ve ölçüldüğünde şu çıktı: 89px'lik
        çubuk + 65px'lik alt gezinme = <b>154px</b>, yani 844px'lik ekranın
        %18'i kalıcı olarak iki üst üste çubuğa gidiyordu. Üstelik içindeki
        düğme çoğu zaman devre dışıydı (zorunlu aşama bitmeden görev onaya
        gönderilemiyor) ve yanındaki uyarı — "Önce tamamlanmalı: Uygulama" —
        ilerleme şeridinin üç satır yukarıda zaten yazdığı şeydi:
        "Sırada: Uygulama · fotoğraf zorunlu".

        Yani ekranın altıda biri, basılamayan bir düğmeyle ekranda zaten
        yazan bir cümleyi tekrar etmeye ayrılmıştı. Şimdi: engelliyse çubuk
        yok, sebep ilerleme şeridinde; açıldığında tek satır, tek eylem.
      */}
      {cubukEylemi && (
        <div
          className="sticky z-10 -mx-4 mt-1 border-t border-border bg-surface/95 px-4 py-2 backdrop-blur-sm lg:hidden
            bottom-[calc(var(--h-tabbar)+env(safe-area-inset-bottom,0px))]
            shadow-[0_-2px_12px_rgba(16,26,45,.06)] dark:shadow-none"
        >
          <div className="flex items-center gap-2">{cubukEylemi}</div>
        </div>
      )}

      {/*
        ── Silme FAB'ı (mobil) ──

        Silme telefonda "⋯" tabakasının içinde, durum geçişlerinin arasında
        bir satırdı: geri alınamaz TEK eylem, geri alınabilir dördüyle aynı
        görünürlükte. Kendi düğmesine alınınca hem ayrışıyor hem de
        aranmadan bulunuyor.

        RENK, UYGULAMANIN SİLME DİLİ: yumuşak kırmızı zemin + kırmızı simge.
        Dolu kırmızı değil — `Button` içindeki kural bunu açıkça yasaklıyor
        ("dolu kırmızı buton yanlışlıkla tıklamayı davet eder") ve FAB
        başparmağın en doğal yerinde duran 56px'lik bir hedef; o uyarının
        en çok geçerli olduğu nokta burası. Onay kutusu da yerinde duruyor:
        tek dokunuşla hiçbir şey silinmiyor.

        `ustPay`: yapışkan eylem çubuğu varken FAB onun üstüne çıkıyor,
        yoksa çubuğun düğmesini örtüyordu.
      */}
      {hasPermission(PERMISSION.gorevSil) && (
        <Fab
          etiket="Görevi sil"
          ton="yikici"
          ikon={<Trash2 size={22} strokeWidth={2} />}
          onClick={() => setSilOnayi(true)}
          ustPay={cubukEylemi ? '60px' : undefined}
        />
      )}

      {/* ── "⋯" tabakası: geçişler ve düzenleme ── */}
      <BottomSheet acik={tabakaAcik} kapat={() => setTabakaAcik(false)} baslik="İşlemler">
        {tabakaEylemleri.map((e) => (
          <SheetRow
            key={e.etiket}
            ikon={e.ikon}
            ton={e.ton}
            okYok
            onClick={() => {
              setTabakaAcik(false);
              e.calistir();
            }}
          >
            {e.etiket}
          </SheetRow>
        ))}
      </BottomSheet>

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
    <Card serit className="p-3.5">
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
    <Card serit>
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
