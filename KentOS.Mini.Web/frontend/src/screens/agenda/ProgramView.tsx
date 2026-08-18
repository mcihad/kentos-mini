import { Building2, Camera, FileText, Lock, MapPin, Newspaper, Repeat } from 'lucide-react';
import { Link } from 'react-router-dom';
import { EmptyState } from '../../components/EmptyState';
import { eventColors, ColoredBadge } from '../../components/Color';
import { cn } from '../../components/utils';
import { range, monthShort, dayName, dayNumber } from '../../data/format';
import type { EventSummary } from '../../data/types';
import { serverToLocal } from '../../data/time';
import { useSession } from '../../auth/SessionProvider';

/**
 * Program (ajanda) görünümü — eski MVC ajanda sayfasının karşılığı.
 *
 * <p>
 * Yerleşim eski arayüzden birebir alındı: solda tarih işaretçisi
 * (gün / ay / gün adı), sağda o güne ait etkinlik kartları. Kart zemini
 * <b>etkinlik durumunun rengi</b>nden türetiliyor — eskisi de öyleydi
 * (<c>Durum.Renk + "80"</c>).
 * </p>
 *
 * <p>
 * Fark: eskisi ham rengi %50 saydamlıkla doğrudan zemine basıyordu ve koyu
 * temada okunmaz oluyordu. Burada <c>color-mix</c> ile yüzey rengiyle
 * harmanlanıyor; renk aynı, okunurluk her iki temada korunuyor.
 * </p>
 */
export function ProgramView({
  etkinlikler,
  bos,
}: {
  etkinlikler: EventSummary[];
  bos?: React.ReactNode;
}) {
  if (etkinlikler.length === 0) {
    return <>{bos ?? <EmptyState ikon={FileText} baslik="Etkinlik yok" />}</>;
  }

  // Güne göre grupla — sıra çağıran tarafta belirlenmiş kabul edilir.
  const gruplar = new Map<string, EventSummary[]>();
  for (const e of etkinlikler) {
    const anahtar = (e.baslangic ?? '').slice(0, 10);
    const mevcut = gruplar.get(anahtar);
    if (mevcut) mevcut.push(e);
    else gruplar.set(anahtar, [e]);
  }

  const bugun = new Date();
  const bugunAnahtari = `${bugun.getFullYear()}-${String(bugun.getMonth() + 1).padStart(2, '0')}-${String(bugun.getDate()).padStart(2, '0')}`;

  return (
    <div className="space-y-5">
      {[...gruplar.entries()].map(([gun, liste]) => {
        const bugunMu = gun === bugunAnahtari;
        const ilk = liste[0].baslangic!;

        return (
          <section key={gun} className="md:flex md:gap-4">
            {/*
              ── MOBİLDE: YAPIŞKAN TARİH AYRACI ──

              Soldaki tarih kutusu masaüstünde güzel duruyor ama telefonda
              58px'i kalıcı olarak yiyordu: 390px ekranın %15'i, üstelik her
              grup için tekrar tekrar. Kartlara kalan genişlik başlıkları iki
              satıra kırıyor, konum ve rozetler sıkışıyordu.

              Mobilde tarih ARAYA giriyor: tam genişlik, ince bir ayraç
              çizgisiyle. Kaydırınca üstte YAPIŞIYOR — uzun bir listede
              "hangi gündeyim" sorusu ancak böyle cevaplı kalıyor; ayraç
              yukarı kayıp gitseydi kullanıcı tarihi bulmak için geri
              kaydırmak zorundaydı.

              Bulanık zemin şart: yapışkan şerit opak olsaydı altından geçen
              kart bıçak gibi kesilirdi, saydam olsaydı yazı okunmazdı.
            */}
            <div
              className="sticky z-10 -mx-4 mb-2 flex items-center gap-2.5 bg-bg/90 px-4 py-2 backdrop-blur-xl md:hidden"
              // Üst çubuğun ALTINA yapışır. Ölçü token'dan geliyor: boşluk
              // knob'u değişince çubuk da büyüyor, sabit bir piksel yazsaydık
              // ayraç ya çubuğun altında kaybolur ya da havada dururdu.
              style={{ top: 'var(--h-bar-m)' }}
            >
              <span
                className={cn(
                  'inline-flex h-[26px] shrink-0 items-baseline gap-1 rounded-full px-2.5',
                  bugunMu ? 'bg-gold-tint text-(--gold-2)' : 'bg-sunken text-ink',
                )}
              >
                <span className="font-display text-sm font-bold leading-[26px] tabular-nums">
                  {dayNumber(ilk)}
                </span>
                <span className="text-2xs font-semibold uppercase tracking-[0.06em]">
                  {monthShort(ilk)}
                </span>
              </span>
              <span
                className={cn(
                  'shrink-0 text-2xs font-semibold uppercase tracking-[0.1em]',
                  bugunMu ? 'text-(--gold-2)' : 'text-ink-3',
                )}
              >
                {bugunMu ? 'Bugün' : dayName(ilk)}
              </span>
              {/* Kalan genişliği dolduran saç teli: tarihi bir başlık yapan
                  şey bu çizgi — yoksa listenin içindeki bir satır gibi
                  okunuyordu. */}
              <span aria-hidden className="h-px min-w-0 flex-1 bg-line" />
              <span className="shrink-0 text-2xs tabular-nums text-ink-3">{liste.length}</span>
            </div>

            {/* ── Tarih işaretçisi (masaüstü) ── */}
            <div className="hidden w-[58px] shrink-0 md:block md:w-[68px]">
              <div
                className={cn(
                  'sticky top-[92px] overflow-hidden rounded-card border text-center',
                  bugunMu ? 'border-gold bg-gold-tint' : 'border-border bg-surface',
                )}
              >
                <p className="pt-2 text-2xs font-semibold uppercase tracking-[0.08em] text-text-3">
                  {monthShort(ilk)}
                </p>
                <p
                  className={cn(
                    'font-display text-2xl font-bold leading-none tabular-nums md:text-3xl',
                    bugunMu && 'text-(--gold-2)',
                  )}
                >
                  {dayNumber(ilk)}
                </p>
                <p
                  className={cn(
                    'px-1 pb-2 pt-1 text-2xs font-medium',
                    bugunMu ? 'text-(--gold-2)' : 'text-text-3',
                  )}
                >
                  {bugunMu ? 'BUGÜN' : dayName(ilk).slice(0, 3)}
                </p>
              </div>
            </div>

            {/* ── O günün etkinlikleri ── */}
            <ul className="min-w-0 flex-1 space-y-2">
              {liste.map((e) => (
                <li key={e.id}>
                  <EtkinlikKarti etkinlik={e} />
                </li>
              ))}
            </ul>
          </section>
        );
      })}
    </div>
  );
}

function EtkinlikKarti({ etkinlik: e }: { etkinlik: EventSummary }) {
  const renkler = eventColors(e.durumRenk, e.tipRenk);
  const suAnda = suAndaMi(e);

  /** Hazırlık bayrakları — eski arayüzdeki sağ üst ikon kümesi. */
  const bayraklar = [
    e.basinKatilsin && { ikon: Newspaper, etiket: 'Basın katılacak' },
    e.resimVar && { ikon: Camera, etiket: 'Fotoğraf var' },
  ].filter(Boolean) as { ikon: typeof Camera; etiket: string }[];

  return (
    <Link
      to={`/ajanda/${e.id}`}
      className="relative flex overflow-hidden rounded-card border border-border shadow-1 transition-[border-color,box-shadow] hover:border-border-2 hover:shadow-2"
      style={{ background: renkler.zemin }}
    >
      {/* Sol kategori çubuğu — durum rengi */}
      <span className="w-[4px] shrink-0" style={{ background: renkler.kenar }} aria-hidden />

      <span className="min-w-0 flex-1 p-3 md:p-3.5">
        {/* Üst satır: saat + işaretler */}
        <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
          <span className="font-display text-sm font-bold tabular-nums text-text">
            {range(e.baslangic, e.bitis, e.tumGun ?? false)}
          </span>

          {suAnda && (
            <span
              className="inline-flex h-[19px] items-center gap-1 rounded-full px-2 text-3xs font-bold uppercase tracking-[0.06em]"
              style={{ color: 'var(--st-live)', background: 'var(--st-live-bg)' }}
            >
              <span className="h-[4px] w-[4px] animate-pulse rounded-full bg-current" aria-hidden />
              Şu anda
            </span>
          )}

          {e.seriId && (
            <span className="inline-flex items-center gap-1 text-2xs text-text-3">
              <Repeat size={11} strokeWidth={2} aria-hidden />
              Tekrar
            </span>
          )}

          {e.gizli && (
            <span className="inline-flex items-center gap-1 text-2xs text-text-3">
              <Lock size={11} strokeWidth={2} aria-hidden />
              Gizli
            </span>
          )}

          {/* Hazırlık ikonları sağa yaslı */}
          {bayraklar.length > 0 && (
            <span className="ml-auto flex items-center gap-1.5 text-text-3">
              {bayraklar.map((b, i) => (
                <b.ikon key={i} size={13} strokeWidth={1.8} aria-label={b.etiket} />
              ))}
            </span>
          )}
        </span>

        {/* Başlık */}
        <span className="mt-1 block font-display text-base font-semibold leading-[1.3] metin-guzel">
          {e.baslik}
        </span>

        {/* Konum + rozetler */}
        <span className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-1.5">
          {e.konum && (
            <span className="inline-flex min-w-0 items-center gap-1 text-xs text-text-2">
              <MapPin size={12} strokeWidth={1.8} className="shrink-0" />
              <span className="truncate">{e.konum}</span>
            </span>
          )}
          {e.durumAd && <ColoredBadge etiket={e.durumAd} renk={e.durumRenk} />}
          {e.tipAd && <ColoredBadge etiket={e.tipAd} renk={e.tipRenk} nokta={false} />}
          <InviteeBadge etkinlik={e} />
        </span>
      </span>
    </Link>
  );
}

/**
 * BAŞKA BİR BİRİMİN AJANDASINDAN gelen etkinlik.
 *
 * Davet edilen birim, etkinliği kendi kayıtlarıyla yan yana görüyor ve
 * ikisini ayırt edemiyordu — "kendi etkinliğim gibi görüyorum ve
 * karıştırıyorum". Rozet yalnızca sahip birim FARKLIYSA çıkar; kendi
 * kayıtlarında gereksiz gürültü olurdu.
 */
export function InviteeBadge({ etkinlik }: { etkinlik: EventSummary }) {
  const { me } = useSession();
  const sahipBaskasi =
    etkinlik.birimId != null && me?.birimId != null && etkinlik.birimId !== me.birimId;

  if (!sahipBaskasi) return null;

  return (
    <span
      className="inline-flex min-w-0 items-center gap-1 rounded-full border border-(--gold) px-2 py-px text-2xs text-text-2"
      title={`${etkinlik.birimAd ?? 'Başka birim'} ajandasından — katılımcı olarak davet edildiniz`}
    >
      <Building2 size={11} strokeWidth={1.9} className="shrink-0" aria-hidden />
      <span className="truncate">{etkinlik.birimAd ?? 'Davetli'}</span>
    </span>
  );
}

function suAndaMi(e: EventSummary): boolean {
  if (!e.baslangic) return false;
  const simdi = Date.now();
  const bas = serverToLocal(e.baslangic).getTime();
  const bit = e.bitis ? serverToLocal(e.bitis).getTime() : bas + 30 * 60_000;
  return bas <= simdi && simdi <= bit;
}
