import { useMutation, useQueryClient } from '@tanstack/react-query';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import {
  ArrowLeft,
  ArrowRightLeft,
  ChevronDown,
  ChevronUp,
  Clock,
  Flag,
  MessageSquare,
  MonitorPlay,
  Pencil,
  Plus,
  Trash2,
  UserPlus,
  Users,
  X,
  Printer,
} from 'lucide-react';
import { useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Link, useParams } from 'react-router-dom';
import { FieldWrapper, Textarea, Input } from '../../components/Field';
import { EmptyState } from '../../components/EmptyState';
import { Button, IconButton } from '../../components/Button';
import { RowActions } from '../../components/RowActions';
import { ActionSheet } from '../../components/ActionSheet';
import { useIsDesktop } from '../../components/screenSize';
import { FormModal } from '../../components/FormModal';
import { Skeleton } from '../../components/Skeleton';
import { Card, CardHeader, StatTile } from '../../components/Card';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useToast } from '../../components/Toast';
import { PlaceholderPicker, insertPlaceholder } from '../../components/PlaceholderPicker';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { time, number, date, phone } from '../../data/format';
import { ExportMenu } from './ExportMenu';
import { PublicDayForm, DeleteConfirm } from '../PublicDays';
import { api } from '../../data/client';
import { usePublicDayApplications, usePublicDay } from '../../data/hooks';
import type { PublicDaySlot, PublicDayAttendance } from '../../data/types';

/**
 * HALK GÜNÜ AYRINTISI — listeyi KURMA ekranı.
 *
 * Sekreterin ekranı: zaman dilimlerini tanımlar, bekleyenlerden atama yapar,
 * dilim içindeki sırayı düzenler, toplu SMS gönderir, Excel alır.
 *
 * Günün KENDİSİNİ yürütmek ayrı bir ekran (salon modu): orada büyük dokunma
 * hedefleri ve tek dokunuşla durum değişimi var. İkisini tek ekranda toplamak,
 * tablette çalışan kişiye ihtiyacı olmayan yönetim düğmelerini gösterirdi.
 */
export default function PublicDayDetail() {
  const { id } = useParams();
  const halkGunuId = Number(id);
  const { hasPermission } = useSession();
  const gezin = useNavigate();
  const masaustu = useIsDesktop();
  const { bildir } = useToast();
  const qc = useQueryClient();

  const [dilimForm, setDilimForm] = useState(false);
  const [atamaDilimi, setAtamaDilimi] = useState<number | null | 'yok'>(null);
  const [smsAcik, setSmsAcik] = useState(false);
  /** Mobilde çıktı menüsü FAB tabakasından açılır. */
  const [ciktiAcik, setCiktiAcik] = useState(false);
  const [silinecekDilim, setSilinecekDilim] = useState<number | null>(null);
  /** Günün KENDİSİNİ düzenleme / silme. */
  const [gunFormu, setGunFormu] = useState(false);
  const [silinecek, setSilinecek] = useState(false);

  const gun = usePublicDay(halkGunuId);

  const tazele = () => qc.invalidateQueries({ queryKey: ['halkgunu'] });

  const siralama = useMutation({
    mutationFn: (ogeler: { id: number; dilimId: number | null; siraNo: number }[]) =>
      api.post(`/halk-gunu/${halkGunuId}/siralama`, { ogeler }),
    onSuccess: tazele,
    onError: (h: Error) => bildir('hata', 'Sıra değiştirilemedi', h.message),
  });

  const cikar = useMutation({
    mutationFn: (katilimId: number) => api.delete(`/halk-gunu/katilim/${katilimId}`),
    onSuccess: () => {
      tazele();
      bildir('basari', 'Listeden çıkarıldı');
    },
    onError: (h: Error) => bildir('hata', 'Çıkarılamadı', h.message),
  });

  const dilimSil = useMutation({
    mutationFn: (dilimId: number) => api.delete(`/halk-gunu/dilim/${dilimId}`),
    onSuccess: () => {
      tazele();
      // Kişiler kaybolmadı, "atanmamışlar"a düştü — kullanıcı paniklemesin.
      bildir('bilgi', 'Dilim silindi', 'Atanmış kişiler "Atanmamışlar" bölümüne taşındı.');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  if (gun.isLoading) return <Skeleton className="h-72 w-full" />;

  if (gun.isError || !gun.data) {
    return (
      <EmptyState
        ikon={Users}
        baslik="Halk günü bulunamadı"
        aciklama={(gun.error as Error)?.message}
        eylem={
          <Link to="/halk-gunu">
            <Button varyant="ikincil">Halk günlerine dön</Button>
          </Link>
        }
      />
    );
  }

  const g = gun.data;

  /** Bir kişiyi dilim içinde yukarı/aşağı taşır. */
  const tasi = (dilimId: number | null, kisiler: PublicDayAttendance[], indeks: number, yon: -1 | 1) => {
    const hedef = indeks + yon;
    if (hedef < 0 || hedef >= kisiler.length) return;

    const yeni = [...kisiler];
    [yeni[indeks], yeni[hedef]] = [yeni[hedef], yeni[indeks]];

    siralama.mutate(
      yeni.map((k, i) => ({ id: k.id!, dilimId, siraNo: i + 1 })),
    );
  };

  /**
   * Kişiyi BAŞKA BİR DİLİME taşır.
   *
   * Aynı uç sırayı da yazıyor (`dilimId` + `siraNo`): "saatini değiştir"
   * ile "sırasını değiştir" tek işlem. Hedefteki en sona eklenir; kapasite
   * doluysa sunucu reddeder ve mesajı olduğu gibi gösterilir.
   */
  const dilimeTasi = (katilim: PublicDayAttendance, hedefDilimId: number | null) => {
    const hedefDoluluk =
      hedefDilimId === null
        ? (g?.atanmamislar?.length ?? 0)
        : (g?.dilimler?.find((d) => d.id === hedefDilimId)?.kisiler?.length ?? 0);

    siralama.mutate([
      { id: katilim.id!, dilimId: hedefDilimId, siraNo: hedefDoluluk + 1 },
    ]);
  };

  return (
    <div className="space-y-4">
      {/* ── Başlık ── */}
      <div className="flex flex-wrap items-start gap-3">
        <Link to="/halk-gunu" className="mt-0.5 shrink-0">
          <IconButton etiket="Geri">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>

        <div className="min-w-0 flex-1">
          <h2 className="font-display text-xl font-bold md:text-2xl">
            {g.baslik || date(g.tarih)}
          </h2>
          <p className="flex flex-wrap items-center gap-x-3 text-sm text-text-3">
            <span>{date(g.tarih)}</span>
            {g.konum && <span>{g.konum}</span>}
            <span>{g.durumAd}</span>
          </p>
        </div>

        {/*
          MOBİLDE HEPSİ FAB'DA.

          Şeritte beş eylem vardı (Salon modu · Toplu SMS · Çıktı ▾ · Düzenle ·
          Sil) ve telefonda İKİ SATIRA sarıyordu; altında dört sayı karosu,
          onun altında zaman dilimleri. Ekranı açan kişi asıl işi — kimin
          hangi saatte olduğunu — görmeden önce iki sıra düğme ve dört karo
          geçiyordu. Sayfa 1913px'ti ve ilk ekranda tek bir vatandaş adı bile
          yoktu.

          Eylemler sağ alttaki FAB'a, "Çıktı" da tabakanın içine taşındı.
        */}
        {!masaustu && (
          <ActionSheet
            baslik="Halk günü işlemleri"
            eylemler={[
              ...(hasPermission(PERMISSION.halkgunuGorusme)
                ? [{
                    etiket: 'Salon modu',
                    ikon: <MonitorPlay size={17} />,
                    onClick: () => gezin(`/halk-gunu/${halkGunuId}/salon`),
                  }]
                : []),
              ...(hasPermission(PERMISSION.halkgunuYonet)
                ? [
                    { etiket: 'Dilim ekle', ikon: <Plus size={17} />, onClick: () => setDilimForm(true) },
                    { etiket: 'Halk gününü düzenle', ikon: <Pencil size={17} />, onClick: () => setGunFormu(true) },
                  ]
                : []),
              ...(hasPermission(PERMISSION.halkgunuAtama)
                ? [{ etiket: 'Bekleyenlerden ekle', ikon: <UserPlus size={17} />, onClick: () => setAtamaDilimi('yok') }]
                : []),
              ...(hasPermission(PERMISSION.halkgunuSms)
                ? [{ etiket: 'Toplu SMS', ikon: <MessageSquare size={17} />, onClick: () => setSmsAcik(true) }]
                : []),
              ...(hasPermission(PERMISSION.halkgunuCiktiAl)
                ? [{ etiket: 'Çıktılar', ikon: <Printer size={17} />, onClick: () => setCiktiAcik(true) }]
                : []),
              ...(hasPermission(PERMISSION.halkgunuYonet)
                ? [{
                    etiket: 'Halk gününü sil',
                    ikon: <Trash2 size={17} />,
                    onClick: () => setSilinecek(true),
                    ton: 'tehlike' as const,
                  }]
                : []),
            ]}
          />
        )}

        <div className="hidden flex-wrap items-center gap-1.5 md:flex">
          {/*
            SALON MODU en görünür eylem: günün kendisi orada yürüyor ve
            tabletle gelen kişi bu ekranda kaybolmamalı.
          */}
          {hasPermission(PERMISSION.halkgunuGorusme) && (
            <Link to={`/halk-gunu/${halkGunuId}/salon`}>
              <Button>
                <MonitorPlay size={14} />
                Salon modu
              </Button>
            </Link>
          )}

          {hasPermission(PERMISSION.halkgunuSms) && (
            <Button varyant="ikincil" onClick={() => setSmsAcik(true)}>
              <MessageSquare size={14} />
              Toplu SMS
            </Button>
          )}

          {/*
            Üç ayrı kâğıt (program · katılım çizelgesi · sonuç raporu) ve her
            biri Excel + PDF. Tek bir "indir" düğmesi bunların hepsini tek
            biçime sıkıştırıyordu.
          */}
          {hasPermission(PERMISSION.halkgunuCiktiAl) && <ExportMenu halkGunuId={halkGunuId} />}

          {/*
            GÜNÜN KENDİSİNİ düzenleme ve silme buradaydı — hiç yoktu.
            Sunucudaki uçlar baştan beri duruyordu; tarihi yanlış girilen bir
            günü düzeltmenin ya da yanlışlıkla açılmış bir günü kaldırmanın
            arayüzde karşılığı bulunmuyordu.
          */}
          {hasPermission(PERMISSION.halkgunuYonet) && (
            <>
              <IconButton etiket="Halk gününü düzenle" onClick={() => setGunFormu(true)}>
                <Pencil size={15} />
              </IconButton>
              <IconButton etiket="Halk gününü sil" onClick={() => setSilinecek(true)}>
                <Trash2 size={15} />
              </IconButton>
            </>
          )}
        </div>
      </div>

      {/* ── Sayılar ── */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile etiket="Kişi" deger={number(g.kisiSayisi)} ikon={<Users size={14} />} />
        <StatTile
          etiket="Görüşülen"
          deger={number(g.gorusulenSayisi)}
          ikon={<Users size={14} />}
          vurgu="--st-ok"
        />
        <StatTile etiket="Gelmeyen" deger={number(g.gelmeyenSayisi)} ikon={<X size={14} />} />
        <StatTile
          etiket="Takip gerektiren"
          deger={number(g.takipSayisi)}
          ikon={<Flag size={14} />}
          vurgu={(g.takipSayisi ?? 0) > 0 ? '--st-warn' : undefined}
          altMetin="Talebe dönüştürülebilir"
        />
      </div>

      {/* ── Dilimler ── */}
      <div className="flex items-center justify-between">
        <h3 className="font-display text-base font-semibold">Zaman dilimleri</h3>
        {hasPermission(PERMISSION.halkgunuYonet) && (
          <Button varyant="ikincil" onClick={() => setDilimForm(true)}>
            <Plus size={14} />
            Dilim ekle
          </Button>
        )}
      </div>

      {(g.dilimler?.length ?? 0) === 0 ? (
        <EmptyState
          ikon={Clock}
          baslik="Zaman dilimi yok"
          aciklama="14:00–15:00 aralığını 10 dakikalık dilimlere bölebilir ya da tek bir aralığa sırayla kişi atayabilirsiniz."
          eylem={
            hasPermission(PERMISSION.halkgunuYonet) ? (
              <Button onClick={() => setDilimForm(true)}>
                <Plus size={14} />
                Dilim ekle
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="space-y-3">
          {g.dilimler!.map((d) => (
            <Card key={d.id}>
              <CardHeader
                baslik={`${(d.baslangic ?? '').slice(11, 16)} – ${(d.bitis ?? '').slice(11, 16)}`}
                aciklama={
                  d.kapasite
                    ? `${d.kisiler?.length ?? 0} / ${d.kapasite} kişi`
                    : `${d.kisiler?.length ?? 0} kişi`
                }
                eylem={
                  /*
                    Dilim başlığındaki üç düğme de TEK GRUPTA: çıktı · kişi
                    ata · sil. Önce üçü ayrı kutulardı ve saat bilgisiyle
                    aralarında sıkışıyorlardı; ortak kenarlık üçünü "bu
                    dilimin araç çubuğu" yapıyor.
                  */
                  <span className="inline-flex flex-none overflow-hidden rounded-sm border border-line bg-surface-2">
                    {/* Kapıdaki görevlinin kâğıdı: yalnızca BU grup. */}
                    {hasPermission(PERMISSION.halkgunuCiktiAl) && (d.kisiler?.length ?? 0) > 0 && (
                      <ExportMenu
                        halkGunuId={halkGunuId}
                        dilimId={d.id!}
                        etiket="Çıktı"
                        varyant="sade"
                      />
                    )}
                    {hasPermission(PERMISSION.halkgunuAtama) && (
                      <button
                        type="button"
                        aria-label="Bu dilime kişi ata"
                        title="Bu dilime kişi ata"
                        onClick={() => setAtamaDilimi(d.id!)}
                        className="grid h-9 w-9 place-items-center border-l border-line text-ink-2 active:bg-sunken"
                      >
                        <UserPlus size={16} />
                      </button>
                    )}
                    {hasPermission(PERMISSION.halkgunuYonet) && (
                      <button
                        type="button"
                        aria-label="Dilimi sil"
                        title="Dilimi sil"
                        onClick={() => setSilinecekDilim(d.id!)}
                        className="grid h-9 w-9 place-items-center border-l border-line text-danger active:bg-danger-soft"
                      >
                        <Trash2 size={16} />
                      </button>
                    )}
                  </span>
                }
              />
              <KisiListesi
                kisiler={d.kisiler ?? []}
                dilimId={d.id!}
                dilimler={g.dilimler ?? []}
                tasi={(i, y) => tasi(d.id!, d.kisiler ?? [], i, y)}
                dilimeTasi={dilimeTasi}
                cikar={(kid) => cikar.mutate(kid)}
                sirala={hasPermission(PERMISSION.halkgunuAtama)}
              />
            </Card>
          ))}
        </div>
      )}

      {/* ── Atanmamışlar ── */}
      {(g.atanmamislar?.length ?? 0) > 0 && (
        <Card>
          <CardHeader
            baslik="Atanmamışlar"
            aciklama="Güne alınmış ama bir zaman dilimine yerleştirilmemiş kişiler"
          />
          <KisiListesi
            kisiler={g.atanmamislar!}
            dilimId={null}
            dilimler={g.dilimler ?? []}
            tasi={(i, y) => tasi(null, g.atanmamislar!, i, y)}
            dilimeTasi={dilimeTasi}
            cikar={(kid) => cikar.mutate(kid)}
            sirala={hasPermission(PERMISSION.halkgunuAtama)}
          />
        </Card>
      )}

      {/* Güne kişi ekleme — dilim seçmeden. */}
      {hasPermission(PERMISSION.halkgunuAtama) && (
        <Button varyant="ikincil" onClick={() => setAtamaDilimi('yok')}>
          <UserPlus size={14} />
          Bekleyenlerden ekle
        </Button>
      )}

      {/* Mobil çıktı tabakası — FAB'daki "Çıktılar" satırından açılıyor. */}
      {!masaustu && hasPermission(PERMISSION.halkgunuCiktiAl) && (
        <ExportMenu halkGunuId={halkGunuId} acik={ciktiAcik} kapat={() => setCiktiAcik(false)} />
      )}

      {dilimForm && (
        <DilimFormu
          halkGunuId={halkGunuId}
          tarih={g.tarih!}
          kapat={() => setDilimForm(false)}
        />
      )}

      {atamaDilimi !== null && (
        <AtamaPenceresi
          halkGunuId={halkGunuId}
          dilimId={atamaDilimi === 'yok' ? null : atamaDilimi}
          kapat={() => setAtamaDilimi(null)}
        />
      )}

      {smsAcik && (
        <SmsPenceresi halkGunuId={halkGunuId} kapat={() => setSmsAcik(false)} />
      )}

      <ConfirmDialog
        acik={silinecekDilim !== null}
        kapat={() => setSilinecekDilim(null)}
        baslik="Zaman dilimi silinsin mi?"
        aciklama="Bu dilime atanmış kişiler listeden ÇIKMAZ, 'Atanmamışlar' bölümüne taşınır."
        onayEtiketi="Sil"
        onayla={() => {
          if (silinecekDilim) dilimSil.mutate(silinecekDilim);
          setSilinecekDilim(null);
        }}
      />

      {/* Günün kendisi — liste ekranıyla AYNI form ve aynı silme onayı. */}
      <PublicDayForm acik={gunFormu} kapat={() => setGunFormu(false)} duzenlenen={g} />
      <DeleteConfirm
        gun={silinecek ? g : null}
        kapat={() => setSilinecek(false)}
        silindi={() => gezin('/halk-gunu')}
      />
    </div>
  );
}

/**
 * Dilim içindeki sıralı kişi listesi.
 *
 * Sıralama SÜRÜKLEMEYLE değil yukarı/aşağı düğmeleriyle: `@dnd-kit/sortable`
 * kurulu değil ve yeni bir UI kütüphanesi eklemiyoruz; ayrıca tablette uzun
 * bir listede sürüklemek düğmeye basmaktan zor.
 */
function KisiListesi({
  kisiler,
  dilimId,
  dilimler,
  tasi,
  dilimeTasi,
  cikar,
  sirala,
}: {
  kisiler: PublicDayAttendance[];
  dilimId: number | null;
  /** Taşıma menüsündeki hedefler — kendi dilimi hariç. */
  dilimler: PublicDaySlot[];
  tasi: (indeks: number, yon: -1 | 1) => void;
  dilimeTasi: (katilim: PublicDayAttendance, hedefDilimId: number | null) => void;
  cikar: (katilimId: number) => void;
  sirala: boolean;
}) {
  if (kisiler.length === 0) {
    return (
      <p className="px-4 py-5 text-center text-sm text-text-3">
        Bu dilime henüz kimse atanmadı.
      </p>
    );
  }

  return (
    <ul className="divide-y divide-border">
      {kisiler.map((k, i) => (
        /*
          SATIR: isim ÖNCE, düğmeler İKİ GRUPTA.

          Önce dört düğme yan yana ayrı ayrı duruyordu (↑ ↓ ⇄ ✕) ve 390px'lik
          ekranda ~150px yiyordu. Kalan yere ne isim ne telefon sığıyor:
          "Ahm..." diye kırpılıyor, numara iki satıra bölünüyordu — oysa
          salonda okunan şey tam olarak o ikisi.

          Şimdi: sıra/isim/telefon tam genişlikte, sağda iki grup —
          **dikey** ↑/↓ (yön bilgisini düğmenin YERİ taşıyor) ve yatay ⇄/✕.
          İkisi birlikte ~90px; kazanılan 60px doğrudan isme gidiyor.
        */
        <li key={k.id} className="flex items-start gap-2.5 px-3.5 py-2.5">
          <span className="mt-0.5 grid h-6 w-6 shrink-0 place-items-center rounded-full border border-border text-xs tabular-nums text-text-2">
            {k.siraNo}
          </span>

          <span className="min-w-0 flex-1">
            <span className="flex items-center gap-1.5">
              <span className="truncate text-sm font-semibold">{k.adSoyad}</span>
              {k.degerlendirmeyeEsas && (
                <Flag size={12} className="shrink-0 text-(--st-warn)" aria-label="İlgilenilecek" />
              )}
            </span>

            {/* Telefon KENDİ SATIRINDA ve kırılmıyor: salonda çağıran kişi
                numarayı bir bakışta okuyor. */}
            {k.telefon && (
              <a
                href={`tel:${k.telefon}`}
                className="mt-0.5 block truncate text-xs tabular-nums text-ink-2 hover:underline"
              >
                {phone(k.telefon)}
              </a>
            )}

            <span className="mt-0.5 flex flex-wrap items-center gap-x-2 text-2xs text-text-3">
              {k.durumAd && <span className="font-medium text-ink-2">{k.durumAd}</span>}
              {k.mahalleAd && <span>{k.mahalleAd}</span>}
              {k.konu && <span className="truncate">{k.konu}</span>}
            </span>
          </span>

          {sirala && (
            <span className="flex shrink-0 items-start gap-1.5">
              <RowActions
                yon="dikey"
                boyut="kucuk"
                eylemler={[
                  { etiket: 'Yukarı taşı', ikon: ChevronUp, onClick: () => tasi(i, -1), pasif: i === 0 },
                  {
                    etiket: 'Aşağı taşı',
                    ikon: ChevronDown,
                    onClick: () => tasi(i, 1),
                    pasif: i === kisiler.length - 1,
                  },
                ]}
              />

              {/*
                BAŞKA SAATE TAŞI — listeyi kurarken en sık yapılan düzeltme:
                kişi yanlış dilime düşüyor ya da vatandaş "o saatte olamam"
                diyor. Çıkarıp yeniden atamak, sırayı ve notu kaybettiriyordu.
              */}
              <span className="inline-flex overflow-hidden rounded-sm border border-line bg-surface-2">
              <DropdownMenu.Root>
                <DropdownMenu.Trigger asChild>
                  <button
                    type="button"
                    aria-label={`${k.adSoyad} kaydını başka saate taşı`}
                    title="Başka saate taşı"
                    className="grid h-[58px] w-9 place-items-center text-ink-2 active:bg-sunken"
                  >
                    <ArrowRightLeft size={15} />
                  </button>
                </DropdownMenu.Trigger>
                <DropdownMenu.Portal>
                  <DropdownMenu.Content
                    align="end"
                    sideOffset={6}
                    className="katman anim-katman z-400 w-[230px] rounded-card border border-border bg-surface p-1 shadow-3"
                  >
                    <p className="px-2.5 py-1.5 text-2xs font-semibold uppercase tracking-wider text-text-3">
                      Taşınacak saat
                    </p>
                    {dilimler
                      .filter((d) => d.id !== dilimId)
                      .map((d) => (
                        <DropdownMenu.Item
                          key={d.id}
                          onSelect={() => dilimeTasi(k, d.id!)}
                          className="flex cursor-pointer items-center justify-between gap-2 rounded-control px-2.5 py-2 text-sm
                            outline-hidden data-highlighted:bg-brand-tint"
                        >
                          <span className="tabular-nums">
                            {time(d.baslangic)}–{time(d.bitis)}
                          </span>
                          <span className="text-xs text-text-3">
                            {d.kisiler?.length ?? 0}
                            {d.kapasite ? ` / ${d.kapasite}` : ''} kişi
                          </span>
                        </DropdownMenu.Item>
                      ))}
                    {dilimId !== null && (
                      <DropdownMenu.Item
                        onSelect={() => dilimeTasi(k, null)}
                        className="cursor-pointer rounded-control px-2.5 py-2 text-sm outline-hidden
                          data-highlighted:bg-brand-tint"
                      >
                        Saati belirlenmemişlere al
                      </DropdownMenu.Item>
                    )}
                  </DropdownMenu.Content>
                </DropdownMenu.Portal>
              </DropdownMenu.Root>

              <button
                type="button"
                aria-label="Listeden çıkar"
                title="Listeden çıkar"
                onClick={() => cikar(k.id!)}
                className="grid h-[58px] w-9 place-items-center border-l border-line text-danger active:bg-danger-soft"
              >
                <X size={15} />
              </button>
              </span>
            </span>
          )}
        </li>
      ))}
    </ul>
  );
}

/** Zaman dilimi — tek aralık ya da toplu üretim. */
function DilimFormu({
  halkGunuId,
  tarih: gunTarihi,
  kapat,
}: {
  halkGunuId: number;
  tarih: string;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [bas, setBas] = useState('14:00');
  const [bit, setBit] = useState('15:00');
  const [bol, setBol] = useState(true);
  const [dakika, setDakika] = useState('10');
  const [kapasite, setKapasite] = useState('');

  const gun = (gunTarihi ?? '').slice(0, 10);

  const kaydet = useMutation({
    mutationFn: () =>
      api.post(`/halk-gunu/${halkGunuId}/dilim`, {
        baslangic: `${gun}T${bas}:00`,
        bitis: `${gun}T${bit}:00`,
        dilimDakika: bol ? Number(dakika) : null,
        kapasite: kapasite ? Number(kapasite) : null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', 'Zaman dilimi eklendi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Eklenemedi', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Zaman dilimi"
      aciklama="Bir aralığı eşit dilimlere bölebilir ya da tek aralık olarak bırakıp sırayla kişi atayabilirsiniz."
      ikon={<Clock size={15} />}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={() => kaydet.mutate()} disabled={kaydet.isPending}>
            {kaydet.isPending ? 'Ekleniyor…' : 'Ekle'}
          </Button>
        </>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <FieldWrapper etiket="Başlangıç" id="dl-bas" zorunlu>
          <Input id="dl-bas" type="time" value={bas} onChange={(e) => setBas(e.target.value)} />
        </FieldWrapper>
        <FieldWrapper etiket="Bitiş" id="dl-bit" zorunlu>
          <Input id="dl-bit" type="time" value={bit} onChange={(e) => setBit(e.target.value)} />
        </FieldWrapper>
      </div>

      {/*
        Toplu üretim varsayılan AÇIK: sekreterin on dilimi tek tek girmesi
        işin en sıkıcı kısmıydı ve pratikte en çok istenen bu.
      */}
      <label className="flex cursor-pointer items-center gap-2.5 rounded-control border border-border bg-surface-2 px-3 py-2.5">
        <input
          type="checkbox"
          checked={bol}
          onChange={(e) => setBol(e.target.checked)}
          className="h-[16px] w-[16px] accent-(--brand)"
        />
        <span className="text-sm">Aralığı eşit dilimlere böl</span>
      </label>

      {bol ? (
        <FieldWrapper
          etiket="Dilim uzunluğu (dakika)"
          id="dl-dk"
          ipucu="14:00–15:00 / 10 dk → altı dilim, her birine bir kişi"
        >
          <Input
            id="dl-dk"
            type="number"
            min={1}
            value={dakika}
            onChange={(e) => setDakika(e.target.value)}
          />
        </FieldWrapper>
      ) : (
        <FieldWrapper
          etiket="Kapasite"
          id="dl-kap"
          ipucu="Boş bırakılırsa sınırsız — bu aralığa sırayla istediğiniz kadar kişi atanabilir"
        >
          <Input
            id="dl-kap"
            type="number"
            min={1}
            value={kapasite}
            onChange={(e) => setKapasite(e.target.value)}
            placeholder="Sınırsız"
          />
        </FieldWrapper>
      )}
    </FormModal>
  );
}

/** Bekleyenlerden seçip güne/dilime atama. */
function AtamaPenceresi({
  halkGunuId,
  dilimId,
  kapat,
}: {
  halkGunuId: number;
  dilimId: number | null;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [secili, setSecili] = useState<number[]>([]);
  const [ara, setAra] = useState('');

  // Yalnızca ATANMAMIŞLAR: zaten bir güne yerleştirilmiş kişiyi ikinci kez
  // önermek listeyi gereksiz kalabalıklaştırıyordu.
  const havuz = usePublicDayApplications({ boyut: 100, atanmamis: true, ara });

  const ata = useMutation({
    mutationFn: () =>
      api.post(`/halk-gunu/${halkGunuId}/katilim`, { basvuruIdler: secili, dilimId }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', `${secili.length} kişi listeye eklendi`);
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Eklenemedi', h.message),
  });

  const liste = havuz.data?.veriler ?? [];

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Bekleyenlerden ata"
      aciklama={dilimId ? 'Seçilenler bu zaman dilimine sırayla eklenir.' : 'Seçilenler güne eklenir; dilimi sonra verebilirsiniz.'}
      ikon={<UserPlus size={15} />}
      altBilgi={`${secili.length} kişi seçildi`}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            onClick={() => ata.mutate()}
            disabled={secili.length === 0 || ata.isPending}
          >
            {ata.isPending ? 'Ekleniyor…' : 'Ekle'}
          </Button>
        </>
      }
    >
      <Input
        value={ara}
        onChange={(e) => setAra(e.target.value)}
        placeholder="Ad veya telefon ara"
        aria-label="Bekleyenlerde ara"
      />

      {liste.length === 0 ? (
        <p className="py-6 text-center text-sm text-text-3">
          Atanmayı bekleyen kimse yok.{' '}
          <Link to="/halk-gunu/basvurular" className="text-brand-2 hover:underline">
            Vatandaş ekleyin
          </Link>
          .
        </p>
      ) : (
        <ul className="max-h-[320px] space-y-1 overflow-y-auto overscroll-contain rounded-control border border-border p-1.5">
          {liste.map((b) => {
            const isaretli = secili.includes(b.id!);
            return (
              <li key={b.id}>
                <label className="flex cursor-pointer items-start gap-2.5 rounded-sm px-2 py-1.5 hover:bg-surface-2">
                  <input
                    type="checkbox"
                    checked={isaretli}
                    onChange={() =>
                      setSecili((s) =>
                        isaretli ? s.filter((x) => x !== b.id) : [...s, b.id!],
                      )
                    }
                    className="mt-0.5 h-[16px] w-[16px] accent-(--brand)"
                  />
                  <span className="min-w-0">
                    <span className="block text-sm font-medium">{b.adSoyad}</span>
                    <span className="flex flex-wrap gap-x-2 text-xs text-text-3">
                      {b.telefon && <span className="tabular-nums">{phone(b.telefon)}</span>}
                      {b.mahalleAd && <span>{b.mahalleAd}</span>}
                      {b.konu && <span className="line-clamp-1">{b.konu}</span>}
                    </span>
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
      )}
    </FormModal>
  );
}

/** Toplu SMS — yer tutuculu metin ve önizleme. */
function SmsPenceresi({ halkGunuId, kapat }: { halkGunuId: number; kapat: () => void }) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const mesajRef = useRef<HTMLTextAreaElement>(null);

  const [mesaj, setMesaj] = useState(
    'Sayın {adSoyad}, {tarih} tarihli halk gününde saat {saat} ({sira}. sıra) ' +
      'olarak randevunuz oluşturulmuştur. {konum}',
  );
  const [tekrar, setTekrar] = useState(true);

  const onizleme = useMemo(
    () =>
      mesaj
        .replaceAll('{adSoyad}', 'Ahmet Yılmaz')
        .replaceAll('{ad}', 'Ahmet')
        .replaceAll('{soyad}', 'Yılmaz')
        .replaceAll('{tarih}', '10.09.2026')
        .replaceAll('{saat}', '14:10')
        .replaceAll('{sira}', '2')
        .replaceAll('{konum}', 'Başkanlık Makamı'),
    [mesaj],
  );

  const gonder = useMutation({
    mutationFn: () =>
      api.post<{ gonderilen: number; telefonsuz: number; atlanan: number }>(
        `/halk-gunu/${halkGunuId}/sms`,
        { mesaj, tekrarGonderme: tekrar },
      ),
    onSuccess: (s) => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir(
        'basari',
        `${s?.gonderilen ?? 0} SMS kuyruğa alındı`,
        // Telefonsuz kayıt SAYILIR ve söylenir: sekreterin eksik numarayı
        // tamamlaması gerekiyor.
        (s?.telefonsuz ?? 0) > 0
          ? `${s!.telefonsuz} kişinin telefonu yok, onlara gönderilemedi.`
          : undefined,
      );
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Gönderilemedi', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Toplu SMS"
      aciklama="Listedeki herkese randevu saatini içeren bilgilendirme gönderilir."
      ikon={<MessageSquare size={15} />}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={() => gonder.mutate()} disabled={!mesaj.trim() || gonder.isPending}>
            {gonder.isPending ? 'Gönderiliyor…' : 'Gönder'}
          </Button>
        </>
      }
    >
      <div>
        <div className="mb-1.5 flex items-center justify-between gap-2">
          <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
            Mesaj <span className="text-(--st-no)">*</span>
          </p>
          <PlaceholderPicker
            ekle={(adi) => {
              const alan = mesajRef.current;
              const { yeni, imlec } = insertPlaceholder(alan, mesaj, adi);
              setMesaj(yeni);
              requestAnimationFrame(() => {
                alan?.focus();
                alan?.setSelectionRange(imlec, imlec);
              });
            }}
          />
        </div>

        <Textarea
          ref={mesajRef}
          value={mesaj}
          onChange={(e) => setMesaj(e.target.value)}
          rows={4}
          maxLength={480}
        />

        <p className="mt-1 text-xs text-text-3">
          {mesaj.length} karakter · her 160 karakter bir SMS sayılır
        </p>

        <div className="mt-2 rounded-control border border-border bg-surface-2 p-2.5">
          <p className="mb-1 text-2xs uppercase tracking-wider text-text-3">Önizleme</p>
          <p className="text-sm leading-normal text-text-2">{onizleme}</p>
        </div>
      </div>

      <label className="flex cursor-pointer items-center gap-2.5 rounded-control border border-border px-3 py-2.5">
        <input
          type="checkbox"
          checked={tekrar}
          onChange={(e) => setTekrar(e.target.checked)}
          className="h-[16px] w-[16px] accent-(--brand)"
        />
        <span className="text-sm">
          Daha önce SMS gönderilenleri atla
          <span className="block text-xs text-text-3">
            Listeye sonradan eklenen kişilere ikinci kez mesaj gitmesin diye.
          </span>
        </span>
      </label>
    </FormModal>
  );
}
