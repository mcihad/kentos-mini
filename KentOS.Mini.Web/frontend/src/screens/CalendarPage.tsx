import { CalendarDays, Check, ChevronDown, ChevronLeft, ChevronRight, Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button, IconButton } from '../components/Button';
import { useIsDesktop } from '../components/screenSize';
import { BottomSheet, SheetHeading, SheetRow } from '../shell/mobile/BottomSheet';
import { Fab } from '../shell/mobile/Fab';
import { SkeletonRows } from '../components/Skeleton';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { EventModal } from './event/EventModal';
import { dilimdenOneri, type BaslangicOnerisi } from './event/EventFields';
import { AgendaView } from '../calendar/AgendaView';
import { MonthView } from '../calendar/MonthView';
import { DayView, WeekView, type DragResult } from '../calendar/DayView';
import { ScopePrompt } from '../calendar/ScopePrompt';
import { addDays, startOfWeek, computeWindow } from '../calendar/viewWindow';
import { VIEW_LABELS, RECURRENCE_SCOPE, type CalendarView, type RecurrenceScope } from '../calendar/types';
import { useRange, useDayCounts, useClock } from '../calendar/queries';
import { YearView } from '../calendar/YearView';
import { startOfDay, localToServer } from '../data/time';

const AY_ADLARI = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık',
];

/** Dört görünümlü takvim. Görünüm ve tarih adres çubuğunda tutulur. */
export default function CalendarScreen() {
  const [parametreler, setParametreler] = useSearchParams();
  const gezin = useNavigate();
  const { bildir } = useToast();

  const gorunum = (parametreler.get('gorunum') as CalendarView) || 'ay';
  const imlec = useMemo(() => {
    const t = parametreler.get('tarih');
    const d = t ? new Date(t) : new Date();
    return startOfDay(Number.isNaN(d.getTime()) ? new Date() : d);
  }, [parametreler]);

  const pencere = useMemo(() => computeWindow(gorunum, imlec), [gorunum, imlec]);
  const { data: etkinlikler, isPending } = useRange(pencere);
  const { data: sayaclar } = useDayCounts(imlec.getFullYear());
  const zamanGuncelle = useClock(pencere);

  const [bekleyen, setBekleyen] = useState<DragResult | null>(null);
  const [gorunumTabakasi, setGorunumTabakasi] = useState(false);

  /**
   * Etkinlik diyaloğu.
   *
   * <c>null</c> = kapalı. Takvimden ekleme <b>sayfa değiştirmez</b>: kullanıcı
   * baktığı haftayı kaybetmeden ekleyip kaldığı yerden devam eder.
   */
  const [modal, setModal] = useState<BaslangicOnerisi | null>(null);

  /**
   * Bir güne etkinlik ekle (ay ve yıl görünümlerinde saat bilinmiyor).
   *
   * Bugün ise bir sonraki yarım saat, değilse 09:00 önerilir — gece yarısı
   * hiçbir zaman istenen saat değil.
   */
  function guneEkle(g: Date) {
    const t = new Date(g);
    const bugunMu = startOfDay(g).getTime() === startOfDay(new Date()).getTime();
    if (bugunMu) {
      const simdi = new Date();
      t.setHours(simdi.getHours(), simdi.getMinutes() < 30 ? 30 : 60, 0, 0);
    } else {
      t.setHours(9, 0, 0, 0);
    }
    setModal(dilimdenOneri(t));
  }

  function ayarla(y: Partial<{ gorunum: CalendarView; tarih: Date }>) {
    const p = new URLSearchParams(parametreler);
    if (y.gorunum) p.set('gorunum', y.gorunum);
    if (y.tarih) p.set('tarih', localToServer(y.tarih).slice(0, 10));
    setParametreler(p, { replace: true });
  }

  function kaydir(yon: -1 | 1) {
    const t = new Date(imlec);
    if (gorunum === 'gun') t.setDate(t.getDate() + yon);
    else if (gorunum === 'hafta') t.setDate(t.getDate() + yon * 7);
    else if (gorunum === 'ay') t.setMonth(t.getMonth() + yon);
    else if (gorunum === 'yil') t.setFullYear(t.getFullYear() + yon);
    else t.setDate(t.getDate() + yon * 30);
    ayarla({ tarih: t });
  }

  /** Sürükleme bitti: seri ise kapsam sor, değilse doğrudan kaydet. */
  function zamanDegisti(s: DragResult) {
    if (s.etkinlik.seriId) {
      setBekleyen(s);
      return;
    }
    uygula(s, RECURRENCE_SCOPE.yalnizca);
  }

  function uygula(s: DragResult, kapsam: RecurrenceScope) {
    zamanGuncelle.mutate(
      { id: s.etkinlik.id, baslangic: s.yeniBaslangic, bitis: s.yeniBitis, kapsam },
      {
        onSuccess: () => {
          if (s.cakisanAdet > 0) {
            // Çakışma ENGELLENMEZ, yalnızca bildirilir.
            bildir('uyari', 'Çakışma var',
              `Bu saatte ${s.cakisanAdet} etkinlik daha bulunuyor.`);
          } else {
            bildir('basari', 'Etkinlik taşındı');
          }
        },
        onError: (h) => bildir('hata', 'Taşınamadı', (h as Error).message),
      },
    );
    setBekleyen(null);
  }

  /*
    MOBİLDE KISA AY ADI.

    Tek satıra sığdırmanın bedeli başlıktan çıkıyor: "15 Ağustos 2026" 390px'te
    okları ve görünüm düğmesini dışarı itiyordu. Üç harfli kısaltma yeri yarıya
    indiriyor ve hiçbir bilgi kaybetmiyor — hangi ay olduğu üç harften de
    anlaşılıyor. Masaüstünde yer bol, tam ad duruyor.
  */
  const masaustu = useIsDesktop();
  const ay = (i: number) => (masaustu ? AY_ADLARI[i] : AY_ADLARI[i].slice(0, 3));

  const baslik = useMemo(() => {
    if (gorunum === 'yil') return String(imlec.getFullYear());
    if (gorunum === 'gun') {
      return `${imlec.getDate()} ${ay(imlec.getMonth())} ${imlec.getFullYear()}`;
    }
    if (gorunum === 'hafta') {
      const pzt = startOfWeek(imlec);
      const paz = addDays(pzt, 6);
      // Ay sınırını aşan haftada iki ay da yazılır: "29 Aralık – 4 Ocak 2026".
      return pzt.getMonth() === paz.getMonth()
        ? `${pzt.getDate()}–${paz.getDate()} ${ay(pzt.getMonth())} ${paz.getFullYear()}`
        : `${pzt.getDate()} ${ay(pzt.getMonth())} – ${paz.getDate()} ${ay(paz.getMonth())} ${paz.getFullYear()}`;
    }
    return `${ay(imlec.getMonth())} ${imlec.getFullYear()}`;
    // `ay` yalnızca `masaustu`ya bağlı; bağımlılığa o giriyor.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [gorunum, imlec, masaustu]);

  return (
    <div className="space-y-3">
      {/*
        TEK SATIR — mobilde de.

        Şerit `flex-wrap` idi ve 390px'te ÜÇ satıra bölünüyordu: oklar,
        altında tarih, altında görünüm seçici. Takvim ekranının üst yarısı
        kontrollere gidiyor, asıl iş olan ızgaraya yer kalmıyordu.

        Tek satıra sığdırmanın yolu, mobilde her kontrolü en kısa hâline
        indirmek: "Bugün" ikona, beş sekmelik segment tek bir düğmeye
        (dokununca alt tabaka açılıyor), "Yeni etkinlik" ise FAB'a taşındı.
        Masaüstünde yer bol; orada üçü de etiketli ve segment açık duruyor.
      */}
      <div className="flex items-center gap-1.5 md:gap-2">
        <IconButton etiket="Önceki" onClick={() => kaydir(-1)}>
          <ChevronLeft size={17} strokeWidth={1.8} />
        </IconButton>
        <IconButton etiket="Sonraki" onClick={() => kaydir(1)}>
          <ChevronRight size={17} strokeWidth={1.8} />
        </IconButton>

        {/* Bugün: mobilde ikon, masaüstünde etiketli. */}
        <IconButton
          etiket="Bugün"
          onClick={() => ayarla({ tarih: new Date() })}
          className="md:hidden"
        >
          <CalendarDays size={17} strokeWidth={1.8} />
        </IconButton>
        <Button
          varyant="ikincil"
          onClick={() => ayarla({ tarih: new Date() })}
          className="hidden md:inline-flex"
        >
          <CalendarDays size={15} strokeWidth={1.8} />
          Bugün
        </Button>

        {/* `min-w-0` + `truncate`: başlık uzayınca kontrolleri dışarı itmesin,
            kendisi kısalsın. */}
        <h2 className="ml-0.5 min-w-0 flex-1 truncate font-display text-base font-bold tracking-[-0.01em] md:ml-1 md:flex-none md:text-2xl">
          {baslik}
        </h2>

        <div className="flex flex-none items-center gap-2 md:ml-auto">
          {/* Görünüm — mobilde tek düğme + alt tabaka. */}
          <button
            type="button"
            onClick={() => setGorunumTabakasi(true)}
            className="flex h-ctrl items-center gap-1 rounded-sm border border-line bg-surface px-2.5 text-xs font-semibold text-ink md:hidden"
          >
            {VIEW_LABELS[gorunum]}
            <ChevronDown size={14} strokeWidth={2} className="text-ink-3" />
          </button>

          {/* Segment kontrolü — design.md §7.8 */}
          <div className="hidden rounded-md border border-border bg-sunken p-[3px] md:flex">
            {(Object.keys(VIEW_LABELS) as CalendarView[]).map((g) => (
              <button
                key={g}
                onClick={() => ayarla({ gorunum: g })}
                className={cn(
                  'h-[30px] rounded-sm px-2.5 text-xs font-medium transition-colors',
                  gorunum === g ? 'bg-surface text-text shadow-1' : 'text-text-2 hover:text-text',
                )}
              >
                {VIEW_LABELS[g]}
              </button>
            ))}
          </div>

          <Button onClick={() => guneEkle(imlec)} className="hidden md:inline-flex">
            <Plus size={15} strokeWidth={2} />
            Yeni Etkinlik
          </Button>
        </div>
      </div>

      {/* Mobil görünüm seçici. */}
      <BottomSheet
        acik={gorunumTabakasi}
        kapat={() => setGorunumTabakasi(false)}
        baslik="Takvim görünümü"
        baslikGizli
      >
        <SheetHeading>Görünüm</SheetHeading>
        {(Object.keys(VIEW_LABELS) as CalendarView[]).map((g) => (
          <SheetRow
            key={g}
            okYok
            ikon={
              gorunum === g ? (
                <Check size={18} strokeWidth={2.4} />
              ) : (
                <CalendarDays size={17} strokeWidth={1.8} />
              )
            }
            onClick={() => {
              ayarla({ gorunum: g });
              setGorunumTabakasi(false);
            }}
          >
            {VIEW_LABELS[g]}
          </SheetRow>
        ))}
      </BottomSheet>

      {/* Mobilde birincil eylem FAB'da: üst şeritte yer yok ve başparmağın
          doğal yeri zaten sağ alt köşe. */}
      <Fab etiket="Yeni etkinlik" onClick={() => guneEkle(imlec)} />

      {isPending ? (
        <SkeletonRows adet={6} />
      ) : gorunum === 'gun' ? (
        <DayView
          gun={imlec}
          etkinlikler={etkinlikler ?? []}
          onEtkinlikAc={(e) => gezin(`/ajanda/${e.id}`)}
          onZamanDegisti={zamanDegisti}
          onBosDilim={(t) => setModal(dilimdenOneri(t))}
        />
      ) : gorunum === 'hafta' ? (
        <WeekView
          imlec={imlec}
          etkinlikler={etkinlikler ?? []}
          onEtkinlikAc={(e) => gezin(`/ajanda/${e.id}`)}
          onZamanDegisti={zamanDegisti}
          onBosDilim={(t) => setModal(dilimdenOneri(t))}
        />
      ) : gorunum === 'ay' ? (
        <MonthView
          imlec={imlec}
          etkinlikler={etkinlikler ?? []}
          onGunSec={(g) => ayarla({ gorunum: 'gun', tarih: g })}
          onEtkinlikAc={(e) => gezin(`/ajanda/${e.id}`)}
          onGunEkle={guneEkle}
        />
      ) : gorunum === 'yil' ? (
        <YearView
          yil={imlec.getFullYear()}
          sayaclar={sayaclar ?? []}
          onGunSec={(g) => ayarla({ gorunum: 'gun', tarih: g })}
        />
      ) : (
        <AgendaView
          etkinlikler={etkinlikler ?? []}
          onEtkinlikAc={(e) => gezin(`/ajanda/${e.id}`)}
        />
      )}

      <EventModal acik={modal !== null} oneri={modal} onKapat={() => setModal(null)} />

      <ScopePrompt
        acik={bekleyen !== null}
        baslik={bekleyen?.etkinlik.baslik ?? ''}
        onay={(k) => bekleyen && uygula(bekleyen, k)}
        iptal={() => setBekleyen(null)}
      />
    </div>
  );
}
