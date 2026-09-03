import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft, Building2, CalendarDays, Inbox, Mail, MapPin, Pencil, Phone, Users,
} from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { EmptyState } from '../../components/EmptyState';
import { Button, IconButton } from '../../components/Button';
import { Skeleton, SkeletonRows } from '../../components/Skeleton';
import { Card, CardHeader } from '../../components/Card';
import { initials, number } from '../../data/format';
import { api } from '../../data/client';
import type { UnitDetail as BirimDetayDto } from '../../data/types';


/**
 * Birim detayı.
 *
 * <p>
 * Eski sistemde birim yalnızca bir satırdı: adı, yetkilisi, sil düğmesi.
 * "Bu birimde kim çalışıyor, kaç etkinliği var" sorusunun cevabı hiçbir
 * ekranda yoktu; birim silinmeden önce ne kaybedileceği de görülmüyordu.
 * </p>
 */
export default function UnitDetailScreen() {
  const { id } = useParams<{ id: string }>();
  const git = useNavigate();
  const birimId = Number(id);

  const birim = useQuery({
    queryKey: ['yonetim', 'birim', birimId] as const,
    queryFn: () => api.get<BirimDetayDto>(`/yonetim/birimler/${birimId}`),
    enabled: Number.isFinite(birimId),
  });

  if (birim.isLoading) {
    return (
      <div className="space-y-3.5">
        <Skeleton className="h-24 w-full" />
        <SkeletonRows adet={5} />
      </div>
    );
  }

  if (birim.isError || !birim.data) {
    return (
      <EmptyState
        ikon={Building2}
        baslik="Birim bulunamadı"
        aciklama={(birim.error as Error)?.message}
        eylem={
          <Button varyant="ikincil" onClick={() => git('/yonetim')}>
            Yönetime dön
          </Button>
        }
      />
    );
  }

  const b = birim.data;
  const kullanicilar = b.kullanicilar ?? [];

  return (
    <div className="space-y-3.5">
      <div className="flex items-start gap-2.5">
        <IconButton etiket="Geri" onClick={() => git('/yonetim')}>
          <ArrowLeft size={17} />
        </IconButton>

        <div className="min-w-0 flex-1">
          <h1 className="truncate font-baslik text-xl font-semibold leading-tight">{b.ad}</h1>
          <p className="truncate text-sm text-text-3">
            {b.yetkili}
            {b.unvan ? ` · ${b.unvan}` : ''}
            {b.ustBirimAd ? ` · ${b.ustBirimAd} altında` : ''}
          </p>
        </div>

        <Button varyant="ikincil" onClick={() => git(`/yonetim?bolum=birimler&birim=${b.id}`)}>
          <Pencil size={14} />
          Düzenle
        </Button>
      </div>

      {/* İstatistikler — birimin ağırlığı tek bakışta. */}
      <div className="grid grid-cols-2 gap-2.5 lg:grid-cols-4">
        <Sayac ikon={<Users size={15} />} etiket="Kullanıcı" deger={b.kullaniciSayisi ?? 0} />
        <Sayac ikon={<Building2 size={15} />} etiket="Alt birim" deger={b.altBirimSayisi ?? 0} />
        <Sayac ikon={<CalendarDays size={15} />} etiket="Etkinlik" deger={b.etkinlikSayisi ?? 0} />
        <Sayac ikon={<Inbox size={15} />} etiket="Talep" deger={b.talepSayisi ?? 0} />
      </div>

      {(b.telefon || b.eposta || b.adres || b.aciklama) && (
        <Card>
          <CardHeader baslik="İletişim" />
          <div className="grid gap-3 p-4 sm:grid-cols-2">
            {b.telefon && <Bilgi ikon={<Phone size={14} />} etiket="Telefon" deger={b.telefon} />}
            {b.eposta && <Bilgi ikon={<Mail size={14} />} etiket="E-posta" deger={b.eposta} />}
            {b.adres && <Bilgi ikon={<MapPin size={14} />} etiket="Adres" deger={b.adres} />}
            {b.aciklama && (
              <div className="sm:col-span-2">
                <p className="text-xs uppercase tracking-wide text-text-3">Açıklama</p>
                <p className="whitespace-pre-wrap text-sm leading-[1.6] text-text-2">
                  {b.aciklama}
                </p>
              </div>
            )}
          </div>
        </Card>
      )}

      <Card>
        <CardHeader
          baslik="Birimdeki kullanıcılar"
          aciklama={`${number(kullanicilar.length)} kişi`}
        />
        {kullanicilar.length === 0 ? (
          <div className="p-4">
            <EmptyState
              ikon={Users}
              baslik="Kullanıcı yok"
              aciklama="Bu birime bağlı bir kullanıcı bulunmuyor."
            />
          </div>
        ) : (
          <ul className="divide-y divide-border">
            {kullanicilar.map((k) => (
              <li key={k.id} className="flex items-center gap-3 px-4 py-2.5">
                <span
                  className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-brand-tint font-baslik text-xs font-semibold text-brand-2"
                  aria-hidden
                >
                  {initials(`${k.ad ?? ''} ${k.soyad ?? ''}`.trim() || k.kullaniciAdi)}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">
                    {`${k.ad ?? ''} ${k.soyad ?? ''}`.trim() || k.kullaniciAdi}
                    <span className="ml-2 font-normal text-text-3">@{k.kullaniciAdi}</span>
                  </p>
                  <p className="truncate text-xs text-text-3">
                    {k.unvan ?? '—'}
                    {(k.roller ?? []).length > 0 ? ` · ${(k.roller ?? []).join(', ')}` : ''}
                  </p>
                </div>
                {k.telefon && (
                  <span className="hidden shrink-0 text-xs tabular-nums text-text-3 sm:block">
                    {k.telefon}
                  </span>
                )}
              </li>
            ))}
          </ul>
        )}
      </Card>

      <p className="text-xs text-text-3">
        Kullanıcı eklemek / birimini değiştirmek için{' '}
        <Link to="/yonetim" className="font-medium text-brand-2 hover:underline">
          Yönetim → Kullanıcılar
        </Link>{' '}
        bölümünü kullanın.
      </p>
    </div>
  );
}

function Sayac({
  ikon,
  etiket,
  deger,
}: {
  ikon: React.ReactNode;
  etiket: string;
  deger: number;
}) {
  return (
    <Card className="flex items-center gap-3 p-3.5">
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-sunken text-text-3" aria-hidden>
        {ikon}
      </span>
      <div className="min-w-0">
        <p className="font-baslik text-xl font-semibold leading-none tabular-nums">
          {number(deger)}
        </p>
        <p className="mt-1 truncate text-xs text-text-3">{etiket}</p>
      </div>
    </Card>
  );
}

function Bilgi({
  ikon,
  etiket,
  deger,
}: {
  ikon: React.ReactNode;
  etiket: string;
  deger: string;
}) {
  return (
    <div className="flex items-start gap-2.5">
      <span className="mt-0.5 shrink-0 text-text-3" aria-hidden>
        {ikon}
      </span>
      <div className="min-w-0">
        <p className="text-xs uppercase tracking-wide text-text-3">{etiket}</p>
        <p className="wrap-break-word text-sm">{deger}</p>
      </div>
    </div>
  );
}
