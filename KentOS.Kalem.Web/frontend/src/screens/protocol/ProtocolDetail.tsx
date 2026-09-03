import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft, AtSign, Landmark, MailCheck, MapPin, Phone, PhoneCall, Smartphone, Users,
} from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { EmptyState } from '../../components/EmptyState';
import { IconButton } from '../../components/Button';
import { SkeletonRows } from '../../components/Skeleton';
import { Card, CardHeader } from '../../components/Card';
import { InsetGroup, ListRow } from '../../components/ListRow';
import { useIsDesktop } from '../../components/screenSize';
import { date, shortDate, phone } from '../../data/format';
import { api } from '../../data/client';

type Protocol = {
  id: number;
  kategori: string;
  kurum?: string | null;
  adSoyad: string;
  unvan?: string | null;
  telefon?: string | null;
  cepTelefon?: string | null;
  eposta?: string | null;
  adres?: string | null;
  aciklama?: string | null;
  aktif: boolean;
};

type DavetGecmisi = {
  davetId: number;
  baslik?: string | null;
  tarih?: string | null;
  yer?: string | null;
  durum: number;
  durumAd?: string | null;
  arandi: boolean;
  arandiTarihi?: string | null;
  mesajGonderildi: boolean;
  mesajTarihi?: string | null;
  not?: string | null;
};

/** Cevap durumunun rengi — davet listesindeki çiplerle aynı eşleme. */
const DURUM_RENK: Record<number, string> = {
  0: 'wait',
  1: 'ok',
  2: 'no',
  3: 'cancel',
};

/**
 * PROTOKOL KİŞİ DOSYASI — bilgileri ve davet edildiği programlar.
 *
 * <p>
 * Telefonu elinde tutan kişi aramadan önce geçen seferi bilmek istiyor:
 * "geçen tören için de aramıştık, gelemedi" bilgisi konuşmanın tonunu
 * belirliyor. Sunucudaki <c>GET protokol/{id}/davetler</c> bu dökümü mobil
 * için zaten üretiyordu; web tarafında karşılığı yoktu ve liste ekranında
 * kişiye tıklamak hiçbir şey açmıyordu.
 * </p>
 */
export default function ProtocolDetail() {
  const { id } = useParams<{ id: string }>();
  const protokolId = Number(id);
  const masaustu = useIsDesktop();

  const kisi = useQuery({
    queryKey: ['protokol', protokolId] as const,
    queryFn: () => api.get<Protocol>(`/protokol/${protokolId}`),
    enabled: Number.isFinite(protokolId) && protokolId > 0,
  });

  const davetler = useQuery({
    queryKey: ['protokol', protokolId, 'davetler'] as const,
    queryFn: () => api.get<DavetGecmisi[]>(`/protokol/${protokolId}/davetler`),
    enabled: Number.isFinite(protokolId) && protokolId > 0,
  });

  if (kisi.isLoading) return <SkeletonRows adet={5} />;

  if (kisi.isError || !kisi.data) {
    return (
      <EmptyState
        ikon={Users}
        baslik="Protokol kaydı bulunamadı"
        aciklama={(kisi.error as Error)?.message}
      />
    );
  }

  const p = kisi.data;
  const liste = davetler.data ?? [];
  const katildi = liste.filter((d) => d.durum === 1).length;

  return (
    <div className="space-y-3.5">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-2.5">
        <Link to="/protokol" className="shrink-0">
          <IconButton etiket="Protokole dön">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-xl font-bold leading-[1.25] tracking-[-0.015em] metin-guzel md:text-2xl">
            {p.adSoyad}
          </h2>
          <p className="mt-0.5 text-sm text-ink-2">
            {[p.unvan, p.kurum].filter(Boolean).join(' · ')}
          </p>
          <p className="mt-1 flex flex-wrap items-center gap-2">
            <span className="rounded-full bg-sunken px-2 py-0.5 text-2xs font-semibold text-ink-2">
              {p.kategori}
            </span>
            {!p.aktif && (
              <span
                className="rounded-full px-2 py-0.5 text-2xs font-semibold"
                style={{ background: 'var(--st-cancel-bg)', color: 'var(--st-cancel)' }}
              >
                Görevde değil
              </span>
            )}
          </p>
        </div>
      </div>

      {/* ── İletişim ── */}
      <Card className="p-3.5">
        <dl className="grid gap-3 sm:grid-cols-2">
          <Bilgi ikon={<Phone size={13} />} etiket="Telefon" deger={phone(p.telefon)} tel />
          <Bilgi ikon={<Smartphone size={13} />} etiket="Cep telefonu" deger={p.cepTelefon} tel />
          <Bilgi ikon={<AtSign size={13} />} etiket="E-posta" deger={p.eposta} eposta />
          <Bilgi ikon={<Landmark size={13} />} etiket="Kurum" deger={p.kurum} />
          <Bilgi ikon={<MapPin size={13} />} etiket="Adres" deger={p.adres} />
        </dl>
        {p.aciklama && (
          <p className="mt-3 border-t border-line pt-3 text-sm leading-[1.55] text-ink-2 metin-guzel">
            {p.aciklama}
          </p>
        )}
      </Card>

      {/* ── Davet geçmişi ── */}
      {davetler.isLoading ? (
        <SkeletonRows adet={3} />
      ) : liste.length === 0 ? (
        <EmptyState
          ikon={Users}
          baslik="Davet geçmişi yok"
          aciklama="Bu kişi henüz bir davet listesine eklenmemiş."
        />
      ) : masaustu ? (
        <Card>
          <CardHeader
            baslik="Davet edildiği programlar"
            aciklama={`${liste.length} davet · ${katildi} katılım`}
          />
          <ul className="divide-y divide-line">
            {liste.map((d) => (
              <li key={d.davetId} className="flex items-start gap-3 p-3.5">
                <div className="min-w-0 flex-1">
                  <Link
                    to={`/davetler/${d.davetId}`}
                    className="text-sm font-semibold hover:text-brand hover:underline"
                  >
                    {d.baslik || 'Davet'}
                  </Link>
                  <p className="mt-0.5 flex flex-wrap items-center gap-x-3 text-xs text-ink-3">
                    {d.tarih && <span className="tabular-nums">{date(d.tarih)}</span>}
                    {d.yer && <span className="truncate">{d.yer}</span>}
                  </p>
                  {d.not && (
                    <p className="mt-1 text-xs leading-[1.45] text-ink-2">{d.not}</p>
                  )}
                  <p className="mt-1.5 flex items-center gap-3 text-2xs text-ink-3">
                    <span className="inline-flex items-center gap-1">
                      <PhoneCall size={11} className={d.arandi ? 'text-ok' : 'opacity-40'} />
                      {d.arandi
                        ? `Arandı${d.arandiTarihi ? ` · ${shortDate(d.arandiTarihi)}` : ''}`
                        : 'Aranmadı'}
                    </span>
                    <span className="inline-flex items-center gap-1">
                      <MailCheck
                        size={11}
                        className={d.mesajGonderildi ? 'text-ok' : 'opacity-40'}
                      />
                      {d.mesajGonderildi ? 'Mesaj gitti' : 'Mesaj yok'}
                    </span>
                  </p>
                </div>
                <CevapCipi durum={d.durum} etiket={d.durumAd} />
              </li>
            ))}
          </ul>
        </Card>
      ) : (
        <InsetGroup baslik={`Davetler · ${liste.length} · ${katildi} katılım`}>
          {liste.map((d, i) => (
            <ListRow
              key={d.davetId}
              sira={i}
              sonuncu={i === liste.length - 1}
              yol={`/davetler/${d.davetId}`}
              ikon={<Users size={15} strokeWidth={1.9} />}
              ikonRengi={`var(--st-${DURUM_RENK[d.durum] ?? 'wait'})`}
              ust={
                <>
                  {d.tarih && <span className="tabular-nums">{shortDate(d.tarih)}</span>}
                  {/* Arandı / mesaj: yapılmışsa dolu, değilse sönük — davet
                      listesindeki satırla aynı işaret dili. */}
                  <PhoneCall size={11} className={d.arandi ? 'text-ok' : 'opacity-35'} />
                  <MailCheck size={11} className={d.mesajGonderildi ? 'text-ok' : 'opacity-35'} />
                </>
              }
              baslik={d.baslik || 'Davet'}
              alt={
                <>
                  {d.yer && <span className="truncate">{d.yer}</span>}
                  {d.not && <span className="truncate text-ink-3">· {d.not}</span>}
                </>
              }
              sag={
                <span className="mt-2.5 shrink-0">
                  <CevapCipi durum={d.durum} etiket={d.durumAd} />
                </span>
              }
            />
          ))}
        </InsetGroup>
      )}
    </div>
  );
}

function Bilgi({
  ikon,
  etiket,
  deger,
  tel,
  eposta,
}: {
  ikon: React.ReactNode;
  etiket: string;
  deger?: string | null;
  tel?: boolean;
  eposta?: boolean;
}) {
  if (!deger) return null;
  const govde = tel ? (
    <a href={`tel:${deger}`} className="tabular-nums hover:text-brand">
      {deger}
    </a>
  ) : eposta ? (
    <a href={`mailto:${deger}`} className="hover:text-brand">
      {deger}
    </a>
  ) : (
    deger
  );

  return (
    <div className="min-w-0">
      <dt className="flex items-center gap-1.5 text-2xs uppercase tracking-[0.06em] text-ink-3">
        {ikon}
        {etiket}
      </dt>
      <dd className="mt-0.5 break-words text-sm text-ink">{govde}</dd>
    </div>
  );
}

function CevapCipi({ durum, etiket }: { durum: number; etiket?: string | null }) {
  const renk = DURUM_RENK[durum] ?? 'wait';
  return (
    <span
      className="inline-block shrink-0 rounded-full px-2 py-0.5 text-2xs font-semibold"
      style={{ background: `var(--st-${renk}-bg)`, color: `var(--st-${renk})` }}
    >
      {etiket || '—'}
    </span>
  );
}
