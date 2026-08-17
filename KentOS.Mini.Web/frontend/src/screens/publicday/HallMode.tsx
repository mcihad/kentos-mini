import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  Check,
  CheckCircle2,
  Flag,
  Phone,
  UserX,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Textarea } from '../../components/Field';
import { EmptyState } from '../../components/EmptyState';
import { Button } from '../../components/Button';
import { Skeleton } from '../../components/Skeleton';
import { PersonHistory } from '../../components/PersonHistory';
import { useToast } from '../../components/Toast';
import { cn } from '../../components/utils';
import { date } from '../../data/format';
import { api } from '../../data/client';
import { usePublicDay } from '../../data/hooks';
import { ATTENDANCE, type PublicDayAttendance } from '../../data/types';

/**
 * SALON MODU — günün KENDİSİNİ yürüten ekran.
 *
 * Salondaki personel tabletle sırayla ilerliyor: kim geldi, kim gelmedi,
 * ne konuşuldu, bu iş takip gerektiriyor mu. Kurma ekranından (dilim ekle,
 * atama yap, SMS gönder) ayrı tutuldu çünkü buradaki kişinin o düğmelere
 * ihtiyacı yok ve kalabalık, sırası gelen vatandaşı bekletiyor.
 *
 * Tasarım kararları hep aynı yerden çıktı — **bir elle, vatandaş karşısında**:
 *  • sıradaki kişi tek kartta ve büyük punto,
 *  • üç eylem tek dokunuş (Geldi · Gelmedi · Görüşüldü),
 *  • not alanı her zaman açık, ayrı bir pencere açtırmıyor,
 *  • kişinin geçmişi aynı kartta — "bu adam geçen ay da gelmişti" bilgisi
 *    konuşmanın ortasında aranmıyor.
 */
/** Geldi / Gelmedi — tek seçim; seçiliyken kendi rengiyle işaretlenir. */
function KatilimDugmesi({
  secili,
  renk,
  ikon,
  etiket,
  bas,
}: {
  secili: boolean;
  renk: string;
  ikon: React.ReactNode;
  etiket: string;
  bas: () => void;
}) {
  return (
    <button
      type="button"
      onClick={bas}
      aria-pressed={secili}
      className={cn(
        'flex min-h-[52px] items-center justify-center gap-2 rounded-control border text-base transition-colors',
        secili
          ? 'font-semibold'
          : 'border-border bg-surface font-medium text-text-2 hover:bg-surface-2',
      )}
      style={
        secili
          ? {
              color: `var(${renk})`,
              borderColor: `var(${renk})`,
              background: `color-mix(in srgb, var(${renk}) 10%, transparent)`,
            }
          : undefined
      }
    >
      {secili ? <CheckCircle2 size={16} /> : ikon}
      {etiket}
    </button>
  );
}

export default function HallMode() {
  const { id } = useParams();
  const halkGunuId = Number(id);
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [secili, setSecili] = useState<number | null>(null);
  const [not, setNot] = useState('');
  const [takip, setTakip] = useState(false);

  const gun = usePublicDay(halkGunuId);

  /** Dilim sırasıyla düzleştirilmiş tam liste — salonun gerçek sırası. */
  const sira = useMemo<PublicDayAttendance[]>(() => {
    const g = gun.data;
    if (!g) return [];
    return [
      ...(g.dilimler ?? []).flatMap((d) => d.kisiler ?? []),
      ...(g.atanmamislar ?? []),
    ];
  }, [gun.data]);

  const ham = sira.find((k) => k.id === secili) ?? sira.find(
    // Otomatik olarak sırada BEKLEYEN ilk kişiyi seçer: operatörün her
    // görüşmeden sonra listede kimin sırası olduğunu araması gerekmesin.
    (k) => k.durum === ATTENDANCE.waiting || k.durum === ATTENDANCE.arrived,
  );

  /**
   * İşaretlenen durum, sunucudan taze veri gelene kadar YERELDE gösterilir.
   *
   * Kaydetme ile listenin tazelenmesi arasında yarım saniye var ve o sürede
   * düğme hâlâ eski durumu gösteriyordu: operatör "Gelmedi"ye basıyor, hiçbir
   * şey değişmiyor, bir daha basıyordu.
   */
  const [bekleyen, setBekleyen] = useState<{ id: number; durum: number } | null>(
    null,
  );

  const aktif =
    ham && bekleyen && bekleyen.id === ham.id
      ? { ...ham, durum: bekleyen.durum }
      : ham;

  /**
   * Not ve işaret AKTİF KİŞİDEN yüklenir.
   *
   * Önce yalnızca listeden elle seçildiğinde yükleniyordu; sıra otomatik
   * ilerlediğinde kutuda bir öncekinin notu kalıyordu. Her kayıtta not da
   * gönderildiği için bu, bir kişinin notunun başkasının üstüne yazılması
   * demekti.
   */
  const aktifId = aktif?.id ?? null;

  // Taze veri geldiğinde (ya da başka kişiye geçildiğinde) yerel gösterim
  // bırakılır; sunucu tek doğruluk kaynağı kalsın.
  useEffect(() => {
    if (!bekleyen) return;
    if (!ham || ham.id !== bekleyen.id || ham.durum === bekleyen.durum) {
      setBekleyen(null);
    }
  }, [ham, bekleyen]);

  useEffect(() => {
    const k = sira.find((x) => x.id === aktifId);
    setNot(k?.gorusmeNotu ?? '');
    setTakip(k?.degerlendirmeyeEsas ?? false);
    // `sira` bilerek dışarıda: liste her tazelemede yeniden kurulur ve
    // kullanıcı yazarken kutuyu sıfırlardı.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [aktifId]);

  const kaydet = useMutation({
    mutationFn: (govde: {
      katilimId: number;
      durum?: number;
      gorusmeNotu?: string;
      degerlendirmeyeEsas?: boolean;
    }) =>
      api.post(`/halk-gunu/katilim/${govde.katilimId}/gorusme`, {
        durum: govde.durum,
        gorusmeNotu: govde.gorusmeNotu,
        degerlendirmeyeEsas: govde.degerlendirmeyeEsas,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  if (gun.isLoading) return <Skeleton className="h-72 w-full" />;
  if (!gun.data) {
    return <EmptyState ikon={UserX} baslik="Halk günü bulunamadı" />;
  }

  const g = gun.data;

  /**
   * Durumu kaydeder. [ilerle] YALNIZCA "Tamamlandı"da true.
   *
   * GELDİ/GELMEDİ SIRAYI İLERLETMEZ: vatandaş daha yeni oturdu, not yazılacak
   * ve "ilgilenilecek" işareti konacak. Eskiden bu iki düğme de sıradaki
   * kişiye geçiyordu; operatör kaydı düzeltmek için listeden geri seçmek
   * zorunda kalıyordu. Mobil uygulamayla aynı davranış.
   *
   * Not ve işaret HER kayıtla gider; sunucu kısmi güncelleme yapıyor, böylece
   * geldi/gelmedi işaretlerken yazılmış not kaybolmuyor.
   */
  const durumaAl = (durum: number, ilerle = false) => {
    if (!aktif?.id) return;

    const katilimId = aktif.id;

    kaydet.mutate({
      katilimId,
      durum,
      gorusmeNotu: not,
      degerlendirmeyeEsas: takip,
    });

    if (!ilerle) {
      // KİŞİYİ SABİTLE. `aktif`, seçim yokken "bekleyen ya da gelen ilk
      // kişi"ye düşüyordu; "Gelmedi" kaydedilip liste tazelenince o kişi bu
      // süzgeçten çıkıyor ve kart SESSİZCE bir sonraki vatandaşa atlıyordu.
      // Ekrandan görünen şey "Gelmedi seçili gelmiyor, kaydedilmedi" — oysa
      // sunucuya yazılmıştı; üstelik yazılmakta olan not da artık başkasının
      // satırına gidiyordu. "Geldi" bozuk görünmüyordu, çünkü o süzgeçte var.
      setSecili(katilimId);
      setBekleyen({ id: katilimId, durum });
      return;
    }

    // Sıradakine geç: operatör listeye dönüp elle seçmek zorunda kalmasın.
    setBekleyen(null);
    const i = sira.findIndex((k) => k.id === katilimId);
    const sonraki = sira.slice(i + 1).find((k) => k.durum === ATTENDANCE.waiting);
    setSecili(sonraki?.id ?? null);
  };

  return (
    <div className="space-y-4">
      {/* ── Başlık ── */}
      <div className="flex items-center gap-3">
        <Link to={`/halk-gunu/${halkGunuId}`} className="shrink-0">
          <Button varyant="ikincil">
            <ArrowLeft size={15} />
            Listeye dön
          </Button>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-lg font-bold md:text-xl">
            {g.baslik || date(g.tarih)}
          </h2>
          <p className="text-sm text-text-3">
            {g.gorusulenSayisi} / {g.kisiSayisi} görüşüldü
          </p>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
        {/* ── Sıradaki kişi ── */}
        {aktif ? (
          <div className="space-y-3">
            <div className="rounded-card border border-border bg-surface p-4 shadow-1 md:p-5">
              <span className="text-xs uppercase tracking-wider text-text-3">
                Sıra {aktif.siraNo}
              </span>

              {/* Ad BÜYÜK: vatandaş karşısında ekrana bakılacak tek şey. */}
              <h3 className="mt-0.5 font-display text-2xl font-bold leading-tight md:text-3xl">
                {aktif.adSoyad}
              </h3>

              <p className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-text-2">
                {aktif.telefon && (
                  <a
                    href={`tel:${aktif.telefon}`}
                    className="flex items-center gap-1.5 tabular-nums text-brand-2 hover:underline"
                  >
                    <Phone size={13} />
                    {aktif.telefon}
                  </a>
                )}
                {aktif.mahalleAd && <span>{aktif.mahalleAd}</span>}
              </p>

              {aktif.konu && (
                <p className="mt-2 rounded-control border border-border bg-surface-2 p-3 text-sm leading-normal">
                  {aktif.konu}
                </p>
              )}

              {/* Geçmiş AYNI KARTTA: "bu kişi geçen ay da gelmişti" bilgisi
                  konuşmanın ortasında aranmamalı. */}
              <PersonHistory
                telefon={aktif.telefon ?? undefined}
                haricKatilim={aktif.id ?? undefined}
                className="mt-3"
              />

              {/* ── Not ── */}
              <div className="mt-3">
                <label
                  htmlFor="salon-not"
                  className="mb-1 block text-xs font-semibold uppercase tracking-wider text-text-3"
                >
                  Görüşme notu
                </label>
                <Textarea
                  id="salon-not"
                  value={not}
                  onChange={(e) => setNot(e.target.value)}
                  rows={3}
                  placeholder="Ne konuşuldu, ne istendi…"
                />
              </div>

              <label className="mt-2.5 flex cursor-pointer items-start gap-2.5 rounded-control border border-border p-3">
                <input
                  type="checkbox"
                  checked={takip}
                  onChange={(e) => setTakip(e.target.checked)}
                  className="mt-0.5 h-[18px] w-[18px] accent-(--st-warn)"
                />
                <span className="text-sm">
                  <span className="flex items-center gap-1.5 font-medium">
                    <Flag size={13} className="text-(--st-warn)" />
                    İlgilenilecek
                  </span>
                  <span className="block text-xs text-text-3">
                    Özel Kalem bu kaydı görür ve talebe dönüştürebilir.
                  </span>
                </span>
              </label>

              {/* ── ATTENDANCE: tek seçim, sırayı ilerletmez ── */}
              <div className="mt-4 grid gap-2 sm:grid-cols-2">
                <KatilimDugmesi
                  secili={aktif.durum === ATTENDANCE.arrived}
                  renk="--st-ok"
                  ikon={<Check size={16} />}
                  etiket="Geldi"
                  bas={() => durumaAl(ATTENDANCE.arrived)}
                />
                <KatilimDugmesi
                  secili={aktif.durum === ATTENDANCE.noShow}
                  renk="--st-no"
                  ikon={<UserX size={16} />}
                  etiket="Gelmedi"
                  bas={() => durumaAl(ATTENDANCE.noShow)}
                />
              </div>

              {/* Sırayı ilerleten TEK düğme. "Görüşüldü" bir durum adıydı;
                  düğmenin üstünde yapılacak iş yazmalı. */}
              <button
                type="button"
                onClick={() => durumaAl(ATTENDANCE.met, true)}
                className="mt-2 flex min-h-[52px] w-full items-center justify-center gap-2 rounded-control bg-brand text-base font-semibold text-on-brand transition-colors hover:bg-brand-2"
              >
                <CheckCircle2 size={16} />
                Tamamlandı
              </button>
            </div>
          </div>
        ) : (
          <EmptyState
            ikon={CheckCircle2}
            baslik="Sıra bitti"
            aciklama="Bekleyen kimse kalmadı. Listeden bir kişi seçerek kaydı düzeltebilirsiniz."
          />
        )}

        {/* ── Sıra listesi ── */}
        <div className="rounded-card border border-border bg-surface">
          <p className="border-b border-border px-3 py-2 text-xs font-semibold uppercase tracking-wider text-text-3">
            Sıra
          </p>
          <ul className="max-h-[62vh] divide-y divide-border overflow-y-auto overscroll-contain">
            {sira.map((k) => (
              <li key={k.id}>
                <button
                  type="button"
                  onClick={() => {
                    setSecili(k.id!);
                    setNot(k.gorusmeNotu ?? '');
                    setTakip(k.degerlendirmeyeEsas ?? false);
                  }}
                  className={cn(
                    'flex w-full items-center gap-2.5 px-3 py-2.5 text-left transition-colors hover:bg-surface-2',
                    aktif?.id === k.id && 'bg-brand-tint',
                  )}
                >
                  <span
                    className={cn(
                      'grid h-6 w-6 shrink-0 place-items-center rounded-full border text-2xs tabular-nums',
                      k.durum === ATTENDANCE.met
                        ? 'border-(--st-ok) text-(--st-ok)'
                        : k.durum === ATTENDANCE.noShow
                          ? 'border-border text-text-3 line-through'
                          : 'border-border text-text-2',
                    )}
                  >
                    {k.siraNo}
                  </span>

                  <span className="min-w-0 flex-1">
                    <span className="line-clamp-1 text-sm font-medium">{k.adSoyad}</span>
                    <span className="text-xs text-text-3">{k.durumAd}</span>
                  </span>

                  {k.degerlendirmeyeEsas && (
                    <Flag size={12} className="shrink-0 text-(--st-warn)" />
                  )}
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
