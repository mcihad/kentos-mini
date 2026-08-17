import {
  ArrowLeft,
  Building2,
  CalendarDays,
  ClipboardList,
  Flag,
  Landmark,
  MapPin,
  Phone,
  UserSearch,
} from 'lucide-react';
import { useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { EmptyState } from '../../components/EmptyState';
import { Button } from '../../components/Button';
import { Skeleton } from '../../components/Skeleton';
import { shortDate, dateTime } from '../../data/format';
import { usePersonFile } from '../../data/hooks';
import type { PersonFile as Dosya } from '../../data/types';

/**
 * VATANDAŞ DOSYASI — bir kişinin kurumla bütün geçmişi.
 *
 * NEDEN: Salonda vatandaşın karşısında oturan kişi (çoğu zaman başkanın
 * kendisi) "siz daha önce ne için gelmiştiniz?" sorusunun cevabını istiyor.
 * Görüşme kartındaki "2 talep · 3 görüşme" özeti soruyu açıyor ama
 * cevaplamıyor: NEYİ istemişti, NE oldu, HANGİ birim baktı?
 *
 * Bu sayfa onu tek bir zaman çizgisinde döküyor. "Geçen mart kaldırım için
 * gelmiştiniz, o iş Fen İşleri'ne havale edildi" diyebilmek, vatandaşa
 * hatırlandığını göstermenin en somut hâli.
 *
 * Kapsam KURUM GENELİ: vatandaş geçen sefer başka bir müdürlüğe gitmiş
 * olabilir ve yarım bir geçmiş yanlış konuşturur. Her satır hangi birime ait
 * olduğunu yazar.
 */
export function PersonFileScreen() {
  const [params] = useSearchParams();
  const telefon = params.get('telefon') ?? undefined;
  const ad = params.get('ad') ?? undefined;
  const haric = Number(params.get('haric')) || undefined;
  const donus = params.get('donus') ?? '/halk-gunu';

  const { data, isLoading } = usePersonFile(telefon, ad, haric);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Link to={donus} className="shrink-0">
          <Button varyant="ikincil">
            <ArrowLeft size={15} />
            Geri
          </Button>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-lg font-bold md:text-xl">
            {ad || 'Vatandaş dosyası'}
          </h2>
          {telefon && (
            <a
              href={`tel:${telefon}`}
              className="flex items-center gap-1 text-sm text-brand"
            >
              <Phone size={12} />
              {telefon}
            </a>
          )}
        </div>
      </div>

      {isLoading && <Skeleton className="h-64 w-full" />}

      {!isLoading && !data?.kayitVar && (
        <EmptyState
          ikon={UserSearch}
          baslik="Kayıt bulunamadı"
          aciklama="Bu numarayla kurumda daha önce bir işlem görünmüyor."
        />
      )}

      {!isLoading && data?.kayitVar && <Icerik dosya={data} />}
    </div>
  );
}

function Icerik({ dosya }: { dosya: Dosya }) {
  /**
   * Üç kaynak TEK zaman çizgisinde birleşir.
   *
   * Ayrı ayrı üç liste, "önce ne oldu sonra ne oldu" sorusunu okuyucuya
   * hesaplattırıyordu. Vatandaşın hikâyesi tek bir sıra: geldi, istedi, şu
   * oldu.
   */
  const satirlar = useMemo(() => {
    const t = (dosya.talepler ?? []).map((x) => ({
      tur: 'talep' as const,
      anahtar: `t-${x.id}`,
      tarih: x.tarih,
      veri: x,
    }));
    const h = (dosya.halkGunleri ?? []).map((x) => ({
      tur: 'halkgunu' as const,
      anahtar: `h-${x.katilimId}`,
      tarih: x.tarih,
      veri: x,
    }));
    const e = (dosya.etkinlikler ?? []).map((x) => ({
      tur: 'etkinlik' as const,
      anahtar: `e-${x.id}`,
      tarih: x.tarih,
      veri: x,
    }));
    return [...t, ...h, ...e].sort(
      (a, b) => new Date(b.tarih ?? 0).getTime() - new Date(a.tarih ?? 0).getTime(),
    );
  }, [dosya]);

  return (
    <>
      <Kunye dosya={dosya} />

      <ol className="relative space-y-3 pl-6">
        {/* Zaman çizgisi: tek dikey saç teli, olaylar üzerinde nokta. */}
        <span
          aria-hidden
          className="absolute left-[7px] top-2 bottom-2 w-px bg-border"
        />
        {satirlar.map((s) => (
          <li key={s.anahtar} className="relative">
            <span
              aria-hidden
              className="absolute left-[-22px] top-3.5 size-[9px] rounded-full border-2 border-surface bg-(--gold)"
            />
            {s.tur === 'talep' && <TalepSatiri v={s.veri} />}
            {s.tur === 'halkgunu' && <HalkGunuSatiri v={s.veri} />}
            {s.tur === 'etkinlik' && <EtkinlikSatiri v={s.veri} />}
          </li>
        ))}
      </ol>
    </>
  );
}

/** Üstteki künye: kaç kayıt, ne zamandır tanışıyoruz. */
function Kunye({ dosya }: { dosya: Dosya }) {
  const kutular = [
    { ikon: ClipboardList, sayi: dosya.talepSayisi ?? 0, etiket: 'talep' },
    { ikon: UserSearch, sayi: dosya.halkGunuSayisi ?? 0, etiket: 'halk günü' },
    { ikon: CalendarDays, sayi: dosya.etkinlikSayisi ?? 0, etiket: 'etkinlik' },
  ].filter((k) => k.sayi > 0);

  return (
    <div className="rounded-card border border-(--gold) bg-gold-tint p-4">
      <div className="flex flex-wrap items-center gap-x-6 gap-y-3">
        {kutular.map((k) => (
          <div key={k.etiket} className="flex items-center gap-2">
            <k.ikon size={16} className="text-(--gold-2)" />
            <span className="font-display text-2xl font-bold tabular-nums leading-none">
              {k.sayi}
            </span>
            <span className="text-sm text-text-2">{k.etiket}</span>
          </div>
        ))}

        {(dosya.gorusulenSayisi ?? 0) > 0 && (
          <span className="text-sm text-text-2">
            bunların <b className="tabular-nums">{dosya.gorusulenSayisi}</b> tanesinde
            görüşüldü
          </span>
        )}

        {dosya.protokolAd && (
          <span className="flex items-center gap-1.5 text-sm text-text-2">
            <Landmark size={14} className="text-(--gold-2)" />
            Protokolde kayıtlı: {dosya.protokolAd}
          </span>
        )}
      </div>

      {dosya.ilkTemas && (
        <p className="mt-3 border-t border-(--gold) pt-2.5 text-sm text-text-3">
          İlk kayıt <b className="text-text-2">{shortDate(dosya.ilkTemas)}</b>, son kayıt{' '}
          <b className="text-text-2">{shortDate(dosya.sonTemas)}</b>.
        </p>
      )}
    </div>
  );
}

function Card({
  etiket,
  tarih,
  baslik,
  birimAd,
  renk,
  children,
  bagi,
}: {
  etiket: string;
  tarih?: string | null;
  baslik?: string | null;
  birimAd?: string | null;
  renk?: string | null;
  children?: React.ReactNode;
  bagi?: string;
}) {
  const govde = (
    <div className="rounded-card border border-border bg-surface p-3 transition-colors hover:bg-surface-2">
      <div className="flex flex-wrap items-center gap-x-2.5 gap-y-1">
        <span
          className="rounded-pill px-2 py-0.5 text-2xs font-semibold uppercase tracking-[0.04em]"
          style={
            renk
              ? { color: renk, backgroundColor: `${renk}1a` }
              : undefined
          }
        >
          {etiket}
        </span>
        <span className="text-xs tabular-nums text-text-3">{dateTime(tarih)}</span>
        {birimAd && (
          <span className="flex items-center gap-1 text-xs text-text-3">
            <Building2 size={11} />
            {birimAd}
          </span>
        )}
      </div>

      {baslik && (
        <p className="mt-1.5 text-base font-semibold leading-snug">{baslik}</p>
      )}
      {children}
    </div>
  );

  return bagi ? (
    <Link to={bagi} className="block">
      {govde}
    </Link>
  ) : (
    govde
  );
}

function TalepSatiri({ v }: { v: NonNullable<Dosya['talepler']>[number] }) {
  return (
    <Card
      etiket="Talep"
      tarih={v.tarih}
      baslik={v.konu}
      birimAd={v.birimAd}
      renk={v.durumRenk}
      bagi={`/talepler/${v.id}`}
    >
      {v.aciklama && (
        <p className="mt-1 line-clamp-2 text-sm text-text-2">{v.aciklama}</p>
      )}
      <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-text-3">
        {v.durumAd && (
          <span className="font-medium" style={v.durumRenk ? { color: v.durumRenk } : undefined}>
            {v.durumAd}
          </span>
        )}
        {v.tipAd && <span>{v.tipAd}</span>}
        {v.mahalleAd && (
          <span className="flex items-center gap-1">
            <MapPin size={11} />
            {v.mahalleAd}
          </span>
        )}
        {v.ajandayaEklendi && <span className="text-(--st-ok)">Ajandaya eklendi</span>}
        {v.arsivlendi && <span>Arşivde</span>}
      </div>
    </Card>
  );
}

function HalkGunuSatiri({ v }: { v: NonNullable<Dosya['halkGunleri']>[number] }) {
  return (
    <Card
      etiket="Halk günü"
      tarih={v.tarih}
      baslik={v.konu || v.gunBaslik}
      birimAd={v.birimAd}
      bagi={`/halk-gunu/${v.halkGunuId}`}
    >
      {v.gorusmeNotu && (
        <p className="mt-1 whitespace-pre-line text-sm text-text-2">
          {v.gorusmeNotu}
        </p>
      )}
      <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-text-3">
        {v.durumAd && <span className="font-medium">{v.durumAd}</span>}
        {v.degerlendirmeyeEsas && (
          <span className="flex items-center gap-1 text-(--st-warn)">
            <Flag size={11} />
            İlgilenilecek
          </span>
        )}
        {v.olusanRandevuId && <span>Talebe dönüştürüldü</span>}
      </div>
    </Card>
  );
}

function EtkinlikSatiri({ v }: { v: NonNullable<Dosya['etkinlikler']>[number] }) {
  return (
    <Card
      etiket="Etkinlik"
      tarih={v.tarih}
      baslik={v.baslik}
      birimAd={v.birimAd}
      bagi={`/ajanda/${v.id}`}
    >
      {v.konum && (
        <p className="mt-1 flex items-center gap-1 text-sm text-text-3">
          <MapPin size={11} />
          {v.konum}
        </p>
      )}
    </Card>
  );
}
