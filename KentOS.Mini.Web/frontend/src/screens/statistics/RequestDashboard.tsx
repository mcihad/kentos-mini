import * as Tabs from '@radix-ui/react-tabs';
import {
  Briefcase,
  CalendarPlus,
  ClipboardList,
  Clock,
  MapPin,
  TrendingUp,
} from 'lucide-react';
import { EmptyState } from '../../components/EmptyState';
import { Skeleton } from '../../components/Skeleton';
import {
  DistributionBar,
  DonutDistribution,
  TimeSeries,
} from '../../components/StatisticsCharts';
import { Card, CardHeader, StatTile } from '../../components/Card';
import { number, date } from '../../data/format';
import { useRequestStatistics } from '../../data/hooks';

/**
 * TALEP PANOSU — vatandaş neyi, nereden, kim aracılığıyla istiyor.
 *
 * Etkinlik panosundan ayrı: o "makamın günü nasıl geçiyor" der. Buranın asıl
 * sebebi **mahalle** ve **meslek** dağılımları; talebin nereden ve kimden
 * geldiğini gösteren tek iki alan bunlar ve hiçbir yerde toplanmıyorlardı.
 *
 * Hesaplama sunucuda; burada yapılan iş yalnızca yerleştirme. İstemcide
 * yeniden hesaplamak, ekrandaki sayı ile rapordaki sayının zamanla
 * ayrışması demekti.
 */
export function RequestDashboard({ bas, bit }: { bas?: string; bit?: string }) {
  const { data, isLoading, isError, error } = useRequestStatistics(bas, bit);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-[92px] w-full" />
          ))}
        </div>
        <Skeleton className="h-56 w-full" />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <EmptyState
        ikon={ClipboardList}
        baslik="Talep istatistikleri yüklenemedi"
        aciklama={(error as Error)?.message}
      />
    );
  }

  const o = data.ozet!;

  return (
    <div className="space-y-4 md:space-y-5">
      <p className="text-sm text-text-2">
        {data.birimAdi} · {date(data.baslangicTarihi)} – {date(data.bitisTarihi)}
      </p>

      {/* ── Ana sayılar ── */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile
          etiket="Toplam talep"
          deger={number(o.toplamTalep)}
          ikon={<ClipboardList size={14} />}
          altMetin={`${number(o.aktifTalep)} aktif · ${number(o.arsivlenmisTalep)} arşiv`}
        />
        <StatTile
          etiket="Ajandaya eklenen"
          deger={number(o.ajandayaEklenen)}
          ikon={<CalendarPlus size={14} />}
          vurgu="--st-done"
        />
        {/*
          Panodaki en işe yarar sayı: sırada bekleyen iş. "Başkan onaylar,
          personel ekler" ayrımı yüzünden bu kümenin büyümesi tıkanma demek —
          bu yüzden sıfırdan büyükken uyarı rengiyle çıkıyor.
        */}
        <StatTile
          etiket="Onaylı, eklenmemiş"
          deger={number(o.onayliAmaEklenmemis)}
          ikon={<Clock size={14} />}
          vurgu={(o.onayliAmaEklenmemis ?? 0) > 0 ? '--st-warn' : undefined}
          altMetin="Ajandaya alınmayı bekliyor"
        />
        <StatTile
          etiket="Bu ay gelen"
          deger={number(o.buAyGelen)}
          ikon={<TrendingUp size={14} />}
          altMetin={`Bugün ${number(o.bugunGelen)} · Bu hafta ${number(o.buHaftaGelen)}`}
        />
      </div>

      {/* ── Seyir ── */}
      <Card>
        <CardHeader
          baslik="Aylara göre gelen talep"
          aciklama={
            (o.ortalamaAjandaGunu ?? 0) > 0
              ? `Talepten randevuya ortalama ${o.ortalamaAjandaGunu} gün`
              : undefined
          }
        />
        <div className="p-4">
          <TimeSeries
            noktalar={(data.aylaraGore ?? []) as never}
            etiket="Aylara göre gelen talep"
            yukseklik={200}
          />
        </div>
      </Card>

      {/* ── Gruplar ── */}
      <Tabs.Root defaultValue="nereden">
        <Tabs.List
          className="mb-3.5 sekme-serit flex gap-1 overflow-x-auto rounded-sm border border-line bg-sunken p-1"
          aria-label="Talep istatistik grupları"
        >
          {[
            { d: 'nereden', e: 'Nereden · Kimden' },
            { d: 'ne', e: 'Ne isteniyor' },
            { d: 'zaman', e: 'Ne zaman' },
            { d: 'akis', e: 'Akış' },
          ].map((s) => (
            <Tabs.Trigger
              key={s.d}
              value={s.d}
              className="flex min-w-max flex-1 basis-0 items-center justify-center gap-1.5 h-ctrl-lg rounded-xs px-3 text-xs font-semibold transition-colors
                  text-ink-2 hover:bg-surface hover:text-ink
                  data-[state=active]:bg-brand data-[state=active]:text-on-brand data-[state=active]:shadow-1"
            >
              {s.e}
            </Tabs.Trigger>
          ))}
        </Tabs.List>

        {/* Panonun asıl sebebi bu sekme. */}
        <Tabs.Content value="nereden" className="grid gap-4 lg:grid-cols-2">
          <Bolum
            baslik="Mahalleye göre"
            aciklama="Talep nereden geliyor"
            ikon={<MapPin size={15} className="text-text-3" />}
            dilimler={data.mahalleyeGore}
            enFazla={16}
          />
          <Bolum
            baslik="Mesleğe göre"
            aciklama="Talep kimden geliyor"
            ikon={<Briefcase size={15} className="text-text-3" />}
            dilimler={data.meslegeGore}
            enFazla={16}
          />
          <Bolum baslik="Talebi giren birim" dilimler={data.birimeGore} enFazla={12} />
          <Bolum baslik="Talebi kaydeden" dilimler={data.olusturanaGore} enFazla={12} />
        </Tabs.Content>

        <Tabs.Content value="ne" className="grid gap-4 lg:grid-cols-2">
          <Bolum baslik="Talep tipine göre" dilimler={data.tipeGore} />
          <Bolum baslik="Duruma göre" dilimler={data.durumaGore} />
        </Tabs.Content>

        <Tabs.Content value="zaman" className="grid gap-4 lg:grid-cols-2">
          <Bolum baslik="Haftanın gününe göre" dilimler={data.haftaGunineGore} />
          <Card>
            <CardHeader baslik="Günlük yoğunluk" aciklama="Son 60 gün" />
            <div className="p-4">
              <TimeSeries
                noktalar={(data.gunlukYogunluk ?? []).slice(-60) as never}
                etiket="Günlük talep yoğunluğu"
                yukseklik={190}
              />
            </div>
          </Card>
        </Tabs.Content>

        <Tabs.Content value="akis" className="grid gap-4 lg:grid-cols-3">
          <DonutChart baslik="Ajanda durumu" dilimler={data.ajandaDurumu} />
          <DonutChart baslik="Özgeçmiş durumu" dilimler={data.ozgecmisDurumu} />
          <DonutChart baslik="Arşiv durumu" dilimler={data.arsivDurumu} />
        </Tabs.Content>
      </Tabs.Root>
    </div>
  );
}

type DilimListesi =
  | { etiket?: string | null; deger?: number; yuzde?: number; renk?: string | null }[]
  | null
  | undefined;

function Bolum({
  baslik,
  aciklama,
  ikon,
  dilimler,
  enFazla,
}: {
  baslik: string;
  aciklama?: string;
  ikon?: React.ReactNode;
  dilimler: DilimListesi;
  enFazla?: number;
}) {
  return (
    <Card>
      <CardHeader baslik={baslik} aciklama={aciklama} eylem={ikon} />
      <div className="p-4">
        <DistributionBar
          dilimler={(dilimler ?? []) as never}
          enFazla={enFazla}
          etiket={baslik}
        />
      </div>
    </Card>
  );
}

/** İkili oranlar halka olarak — "eklendi / eklenmedi" gibi. */
function DonutChart({ baslik, dilimler }: { baslik: string; dilimler: DilimListesi }) {
  return (
    <Card>
      <CardHeader baslik={baslik} />
      <div className="p-4">
        <DonutDistribution dilimler={(dilimler ?? []) as never} etiket={baslik} />
      </div>
    </Card>
  );
}
