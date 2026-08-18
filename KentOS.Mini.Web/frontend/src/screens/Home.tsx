import { useQuery } from '@tanstack/react-query';
import {
  CalendarClock, CalendarDays, CheckCircle2, ClipboardList, Clock, Lock,
  MapPin, TrendingUp,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { Skeleton, SkeletonRows } from '../components/Skeleton';
import { Card, CardHeader, StatTile } from '../components/Card';
import { ColoredBadge, colorOr } from '../components/Color';
import { cn } from '../components/utils';
import { useSession } from '../auth/SessionProvider';
import { queryKeys } from '../data/queryKeys';
import { range, monthShort, relativeTime, dayName, dayNumber, number, date } from '../data/format';
import { api } from '../data/client';
import { toMap, useEventTypes, useRequestStatusCounts, useRequests } from '../data/hooks';
import type { EventSummary } from '../data/types';
import { startOfDay, serverToLocal, localToServer } from '../data/time';

/**
 * Ana Sayfa — design.md §6.
 *
 * Sıralama bir karardır: en üstte SIRADAKİ etkinlik (kullanıcının bugün
 * bilmesi gereken tek şey), sonra sayılar, sonra listeler. Mobilde ilk
 * ekranda yalnızca sıradaki etkinlik ve sayı karoları görünür.
 */
export default function Home() {
  const { me, hasPolicy } = useSession();
  const ajandaYetkisi = hasPolicy('Ajanda');

  // Bugünden 30 gün ileri: "sıradaki" ve "bu hafta" için tek sorgu yeter.
  const bugun = startOfDay(new Date());
  const ufuk = new Date(bugun);
  ufuk.setDate(ufuk.getDate() + 30);

  const etkinlikler = useQuery({
    queryKey: queryKeys.event.window(
      localToServer(bugun),
      localToServer(ufuk),
      'anasayfa',
    ),
    queryFn: () =>
      api.post<EventSummary[]>('/takvim/aralik', {
        baslangic: localToServer(bugun),
        bitis: localToServer(ufuk),
      }),
    enabled: ajandaYetkisi,
  });

  // Ana sayfa yalnızca ilk birkaç kaydı gösteriyor; tüm listeyi çekmeye gerek yok.
  const talepler = useRequests({ boyut: 6, sayfa: 1 });
  const durumSayaclari = useRequestStatusCounts(false);
  const tipler = useEventTypes();

  const tipHaritasi = toMap(tipler.liste, (t) => t.id!);

  const simdi = new Date();
  const liste = etkinlikler.data ?? [];

  const siradaki = liste
    .filter((e) => serverToLocal(e.baslangic!) >= simdi)
    .sort((a, b) => (a.baslangic! < b.baslangic! ? -1 : 1))[0];

  const bugunkuler = liste.filter((e) => e.baslangic?.slice(0, 10) === localToServer(bugun).slice(0, 10));

  const haftaSonu = new Date(bugun);
  haftaSonu.setDate(haftaSonu.getDate() + 7);
  const buHafta = liste.filter((e) => {
    const t = e.baslangic!.slice(0, 10);
    return t >= localToServer(bugun).slice(0, 10) && t < localToServer(haftaSonu).slice(0, 10);
  });

  const talepSatirlari = talepler.data?.veriler ?? [];
  const toplamTalep = talepler.data?.toplam ?? 0;
  const bekleyenTalepler = talepSatirlari.filter((t) => !t.ajandayaEklendi);

  return (
    <div className="space-y-4 md:space-y-5">
      {/* ── Karşılama ── */}
      <div>
        <h2 className="font-display text-2xl font-bold tracking-[var(--track-d)]">
          {selamla()}
          {me?.ad ? `, ${me.ad}` : ''}
        </h2>
        <p className="mt-0.5 text-sm text-text-2">
          {dayName(localToServer(simdi))}, {date(localToServer(simdi))}
        </p>
      </div>

      {/* ── Sıradaki etkinlik ── */}
      {ajandaYetkisi && (
        <SiradakiEtkinlik
          etkinlik={siradaki}
          yukleniyor={etkinlikler.isLoading}
          tipAdi={siradaki?.tipId ? tipHaritasi.get(siradaki.tipId)?.ad ?? null : null}
        />
      )}

      {/* ── Sayı karoları ── */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile
          etiket="Bugün"
          deger={etkinlikler.isLoading ? '–' : number(bugunkuler.length)}
          ikon={<CalendarDays size={14} />}
          altMetin="etkinlik"
        />
        <StatTile
          etiket="Bu hafta"
          deger={etkinlikler.isLoading ? '–' : number(buHafta.length)}
          ikon={<CalendarClock size={14} />}
          altMetin="etkinlik"
        />
        <StatTile
          etiket="Talepler"
          deger={talepler.isLoading ? '–' : number(toplamTalep)}
          ikon={<ClipboardList size={14} />}
          altMetin="birimimde"
        />
        <StatTile
          etiket="Bekleyen talep"
          deger={
            durumSayaclari.isLoading
              ? '–'
              : number((durumSayaclari.data ?? []).find((d) => /bekle/i.test(d.durumAd ?? ''))?.adet ?? bekleyenTalepler.length)
          }
          ikon={<Clock size={14} />}
          vurgu={bekleyenTalepler.length > 0 ? '--st-wait' : undefined}
          altMetin="talep"
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* ── Yaklaşan etkinlikler ── */}
        {ajandaYetkisi && (
          <Card>
            <CardHeader
              baslik="Yaklaşan etkinlikler"
              aciklama="Önümüzdeki 30 gün"
              eylem={
                <Link to="/takvim">
                  <Button varyant="sade" className="h-8 px-2.5">
                    Takvim
                  </Button>
                </Link>
              }
            />
            <div className="p-3">
              {etkinlikler.isLoading ? (
                <SkeletonRows adet={4} />
              ) : liste.length === 0 ? (
                <EmptyState
                  ikon={CalendarDays}
                  baslik="Yaklaşan etkinlik yok"
                  aciklama="Önümüzdeki 30 gün için planlanmış bir etkinlik bulunmuyor."
                />
              ) : (
                <ul className="space-y-1">
                  {liste.slice(0, 6).map((e) => (
                    <li key={e.id}>
                      <Link
                        to={`/ajanda/${e.id}`}
                        className="flex items-center gap-3 rounded-md px-2 py-2 transition-colors hover:bg-surface-2"
                      >
                        <span
                          className="w-[3px] shrink-0 self-stretch rounded-full"
                          style={{ background: colorOr(e.durumRenk ?? e.tipRenk, 'var(--border-2)') }}
                          aria-hidden
                        />
                        <span className="grid w-11 shrink-0 place-items-center rounded-sm bg-sunken py-1">
                          <span className="text-2xs font-semibold text-text-3">
                            {monthShort(e.baslangic)}
                          </span>
                          <span className="font-display text-lg font-bold leading-none tabular-nums">
                            {dayNumber(e.baslangic)}
                          </span>
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="flex items-center gap-1.5">
                            {e.gizli && <Lock size={11} className="shrink-0 text-text-3" aria-label="Gizli" />}
                            <span className="truncate text-sm font-medium">{e.baslik}</span>
                          </span>
                          <span className="mt-0.5 block truncate text-xs text-text-3">
                            {range(e.baslangic, e.bitis, e.tumGun ?? false)}
                            {e.konum ? ` · ${e.konum}` : ''}
                          </span>
                        </span>
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </Card>
        )}

        {/* ── Son talepler ── */}
        <Card>
          <CardHeader
            baslik="Son talepler"
            aciklama="Birimime düşenler"
            eylem={
              <Link to="/talepler">
                <Button varyant="sade" className="h-8 px-2.5">
                  Tümü
                </Button>
              </Link>
            }
          />
          <div className="p-3">
            {talepler.isLoading ? (
              <SkeletonRows adet={4} />
            ) : talepSatirlari.length === 0 ? (
              <EmptyState
                ikon={ClipboardList}
                baslik="Talep yok"
                aciklama="Biriminize düşen bir talep bulunmuyor."
              />
            ) : (
              <ul className="space-y-1">
                {talepSatirlari.map((t) => (
                  <li key={t.id}>
                    <Link
                      to={`/talepler/${t.id}`}
                      className="flex items-center gap-3 rounded-md px-2 py-2 transition-colors hover:bg-surface-2"
                    >
                      {/* Renk kayıtla birlikte geliyor; ayrı bir eşleme yok. */}
                      <span
                        className="h-8 w-[3px] shrink-0 rounded-full"
                        style={{ background: colorOr(t.durumRenk, 'var(--border-2)') }}
                        aria-hidden
                      />
                      <span className="min-w-0 flex-1">
                        <span className="block truncate text-sm font-medium">{t.konu}</span>
                        <span className="mt-0.5 block truncate text-xs text-text-3">
                          {t.adSoyad} · {relativeTime(t.baslangicTarih)}
                        </span>
                      </span>
                      <ColoredBadge etiket={t.durumAd} renk={t.durumRenk} nokta={false} />
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </Card>
      </div>
    </div>
  );
}

/** Saate göre selam — küçük bir incelik, ama sabah 8'de "İyi akşamlar" tuhaf kaçar. */
function selamla(): string {
  const s = new Date().getHours();
  if (s < 6) return 'İyi geceler';
  if (s < 12) return 'Günaydın';
  if (s < 18) return 'İyi günler';
  return 'İyi akşamlar';
}

/**
 * Sıradaki etkinlik kartı.
 *
 * Sayfanın en belirgin öğesi bilinçli olarak burada: "şimdi ne var?" sorusu,
 * bu uygulamanın günlük kullanımının %80'i.
 */
function SiradakiEtkinlik({
  etkinlik,
  yukleniyor,
  tipAdi,
}: {
  etkinlik?: EventSummary;
  yukleniyor: boolean;
  tipAdi: string | null;
}) {
  if (yukleniyor) {
    return (
      <Card className="p-4">
        <Skeleton className="h-3.5 w-28" />
        <Skeleton className="mt-2.5 h-6 w-3/4" />
        <Skeleton className="mt-2 h-3.5 w-1/2" />
      </Card>
    );
  }

  if (!etkinlik) {
    return (
      <Card className="flex items-center gap-3 p-4">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-(--st-ok-bg) text-(--st-ok)">
          <CheckCircle2 size={19} strokeWidth={1.9} />
        </span>
        <div>
          <p className="font-display text-base font-bold">Programınız boş</p>
          <p className="mt-0.5 text-sm text-text-2">
            Bugün ve sonrası için planlanmış bir etkinlik yok.
          </p>
        </div>
      </Card>
    );
  }

  const suAnda = suAndaMi(etkinlik);

  return (
    <Link to={`/ajanda/${etkinlik.id}`} className="block">
      <Card
        className={cn(
          'relative overflow-hidden p-4 transition-colors hover:bg-surface-2 md:p-5',
          suAnda && 'border-(--st-live)',
        )}
      >
        {/* Sol kenardaki altın şerit: kurumsal kimliğin imzası */}
        <span
          aria-hidden
          className="absolute inset-y-0 left-0 w-[3px]"
          style={{ background: suAnda ? 'var(--st-live)' : 'var(--gold)' }}
        />

        <div className="flex items-center gap-2">
          <span
            className={cn(
              'inline-flex h-[22px] items-center gap-1.5 rounded-full px-2.5 text-2xs font-bold uppercase tracking-[0.06em]',
            )}
            style={
              suAnda
                ? { color: 'var(--st-live)', background: 'var(--st-live-bg)' }
                : { color: 'var(--gold-strong)', background: 'var(--gold-tint)' }
            }
          >
            {suAnda && (
              <span className="h-[5px] w-[5px] animate-pulse rounded-full bg-current" aria-hidden />
            )}
            {suAnda ? 'Şu anda' : 'Sıradaki'}
          </span>
          <span className="text-xs text-text-3">{relativeTime(etkinlik.baslangic)}</span>
          {etkinlik.gizli && (
            <span className="ml-auto inline-flex items-center gap-1 text-xs text-text-3">
              <Lock size={11} />
              Gizli
            </span>
          )}
        </div>

        <h3 className="mt-2 font-display text-xl font-bold leading-tight tracking-[-0.015em] metin-guzel md:text-2xl">
          {etkinlik.baslik}
        </h3>

        <div className="mt-2.5 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-sm text-text-2">
          <span className="inline-flex items-center gap-1.5">
            <CalendarDays size={13} className="text-text-3" />
            {date(etkinlik.baslangic)}
          </span>
          <span className="inline-flex items-center gap-1.5">
            <Clock size={13} className="text-text-3" />
            {range(etkinlik.baslangic, etkinlik.bitis, etkinlik.tumGun ?? false)}
          </span>
          {etkinlik.konum && (
            <span className="inline-flex min-w-0 items-center gap-1.5">
              <MapPin size={13} className="shrink-0 text-text-3" />
              <span className="truncate">{etkinlik.konum}</span>
            </span>
          )}
          {tipAdi && (
            <span className="inline-flex items-center gap-1.5">
              <TrendingUp size={13} className="text-text-3" />
              {tipAdi}
            </span>
          )}
        </div>
      </Card>
    </Link>
  );
}

function suAndaMi(e: EventSummary): boolean {
  const simdi = Date.now();
  const bas = serverToLocal(e.baslangic!).getTime();
  const bit = e.bitis ? serverToLocal(e.bitis).getTime() : bas + 30 * 60_000;
  return bas <= simdi && simdi <= bit;
}
