import * as Tabs from '@radix-ui/react-tabs';
import { ArrowLeft } from 'lucide-react';
import { Link } from 'react-router-dom';
import { SekmeListesi, SekmeTetigi } from '../../components/Tabs';
import { BarChart3, CalendarCheck, CalendarX, Camera, Clock, FileText, TrendingUp } from 'lucide-react';
import { useState } from 'react';
import { EmptyState } from '../../components/EmptyState';
import { DonutChart } from '../../components/Chart';
import { DistributionBar, TimeSeries } from '../../components/StatisticsCharts';
import { Skeleton } from '../../components/Skeleton';
import { Card, CardHeader, StatTile } from '../../components/Card';
import { SegmentedSelect } from '../../components/Filters';
import { number, duration, date } from '../../data/format';
import { useEventStatistics } from '../../data/hooks';
import { aralikHesapla, ARALIK_ETIKETLERI, type Aralik } from './range';


/**
 * İstatistikler.
 *
 * Tek bir sunucu çağrısı 20+ dağılım döndürüyor; burada yapılan iş yalnızca
 * yerleştirme. Hesaplama İSTEMCİDE TEKRARLANMAZ — aksi hâlde ekrandaki sayı
 * ile rapordaki sayı zamanla ayrışırdı.
 */

export default function EventDashboard() {
  const [aralik, setAralik] = useState<Aralik>('buYil');

  const [bas, bit] = aralikHesapla(aralik);
  const { data, isLoading, isError, error } = useEventStatistics(bas, bit);

  const aralikSecimi = (
    <SegmentedSelect<Aralik>
      deger={aralik}
      degistir={setAralik}
      etiket="Zaman aralığı"
      secenekler={(Object.keys(ARALIK_ETIKETLERI) as Aralik[]).map((a) => ({
        deger: a,
        etiket: ARALIK_ETIKETLERI[a],
      }))}
    />
  );

  const baslik = (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
      <div className="min-w-0">
        <Link
          to="/istatistikler"
          className="inline-flex items-center gap-1 text-xs font-semibold text-text-3
                     transition-colors hover:text-brand"
        >
          <ArrowLeft size={13} />
          İstatistikler
        </Link>
        <h1 className="mt-0.5 font-display text-xl font-extrabold tracking-[-0.02em]">
          Etkinlikler
        </h1>
      </div>
      {aralikSecimi}
    </div>
  );

  if (isLoading) {
    return (
      <div className="space-y-4">
        {baslik}
        <Skeleton className="h-9 w-64" />
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
      <div className="space-y-4">
        {baslik}
        <EmptyState
          ikon={BarChart3}
          baslik="İstatistikler yüklenemedi"
          aciklama={(error as Error)?.message}
        />
      </div>
    );
  }

  const o = data.ozet!;

  return (
    <div className="space-y-4 md:space-y-5">
      {/* ── Pano ve aralık seçimi ── */}
      {baslik}
      <p className="text-sm text-text-2">
        {data.birimAdi} · {date(data.baslangicTarihi)} – {date(data.bitisTarihi)}
      </p>

      {/* ── Ana sayılar ── */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile
          etiket="Toplam etkinlik"
          deger={number(o.toplamEtkinlik)}
          ikon={<BarChart3 size={14} />}
          altMetin={`${number(o.aktifEtkinlik)} aktif`}
        />
        <StatTile
          etiket="Tamamlanan"
          deger={number(o.tamamlananEtkinlik)}
          ikon={<CalendarCheck size={14} />}
          vurgu="--st-done"
        />
        <StatTile
          etiket="İptal edilen"
          deger={number(o.iptalEdilenEtkinlik)}
          ikon={<CalendarX size={14} />}
          vurgu={(o.iptalEdilenEtkinlik ?? 0) > 0 ? '--st-cancel' : undefined}
        />
        <StatTile
          etiket="Ortalama süre"
          deger={duration(o.ortalamaSureDakika)}
          ikon={<Clock size={14} />}
        />
      </div>

      {/* ── Zaman göstergeleri ── */}
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <StatTile etiket="Bugün" deger={number(o.bugunkuEtkinlik)} />
        <StatTile etiket="Bu hafta" deger={number(o.buHaftaEtkinlik)} />
        <StatTile etiket="Bu ay" deger={number(o.buAyEtkinlik)} />
        <StatTile etiket="Gelecek" deger={number(o.gelecekEtkinlik)} />
      </div>

      {/* ── Aylık seyir ── */}
      <Card>
        <CardHeader baslik="Aylara göre dağılım" aciklama="Seçili aralık boyunca" />
        <div className="p-4">
          <TimeSeries
            noktalar={(data.aylaraGore ?? []) as never}
            etiket="Aylara göre etkinlik"
            yukseklik={190}
          />
        </div>
      </Card>

      {/* ── Oranlar ── */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card className="p-4">
          <DonutChart oran={o.tamamlanmaOrani ?? 0} etiket="Tamamlanma oranı" />
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <span className="grid h-[68px] w-[68px] shrink-0 place-items-center rounded-full bg-sunken">
              <FileText size={22} className="text-text-3" />
            </span>
            <div>
              <p className="font-display text-xl font-bold tabular-nums">
                {number(o.toplamNot)}
              </p>
              <p className="text-sm text-text-2">
                toplam not · etkinlik başına {(o.ortalamaNotSayisi ?? 0).toFixed(1)}
              </p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <span className="grid h-[68px] w-[68px] shrink-0 place-items-center rounded-full bg-sunken">
              <Camera size={22} className="text-text-3" />
            </span>
            <div>
              <p className="font-display text-xl font-bold tabular-nums">
                {number(o.toplamFotograf)}
              </p>
              <p className="text-sm text-text-2">
                toplam fotoğraf · etkinlik başına {(o.ortalamaFotografSayisi ?? 0).toFixed(1)}
              </p>
            </div>
          </div>
        </Card>
      </div>

      {/*
        ── Dağılımlar ──

        Sekmeye ALINDI: 16 dağılım kartı alt alta dizildiğinde mobilde
        11.000 pikselden uzun bir sayfa çıkıyordu ve alttaki kartlar pratikte
        hiç görülmüyordu. Gruplar, kullanıcının sorusuna göre ayrıldı:
        "ne yapıldı", "ne zaman yapıldı", "kim/nerede", "nasıl hazırlanıldı".
      */}
      <Tabs.Root defaultValue="ne">
        <SekmeListesi etiket="İstatistik grupları" className="mb-3.5">
          {[
            { d: 'ne', e: 'Ne yapıldı' },
            { d: 'zaman', e: 'Ne zaman' },
            { d: 'kim', e: 'Kim · Nerede' },
            { d: 'hazirlik', e: 'Hazırlık' },
            { d: 'seyir', e: 'Seyir' },
          ].map((s) => (
            <SekmeTetigi key={s.d} deger={s.d}>
              {s.e}
            </SekmeTetigi>
          ))}
        </SekmeListesi>

        <Tabs.Content value="ne" className="grid gap-4 lg:grid-cols-2">
          <Bolum baslik="Etkinlik tipine göre" dilimler={data.tipeGore} />
          <Bolum baslik="Duruma göre" dilimler={data.durumaGore} />
          <Bolum baslik="Statüye göre" dilimler={data.statuyeGore} />
          <Bolum baslik="Süre dağılımı" dilimler={data.sureDagilimi} />
        </Tabs.Content>

        <Tabs.Content value="zaman" className="grid gap-4 lg:grid-cols-2">
          <Bolum baslik="Haftanın gününe göre" dilimler={data.haftaGunineGore} />
          <Bolum baslik="Saat aralığına göre" dilimler={data.saatAraliginaGore} />
          <Bolum baslik="Günün bölümüne göre" dilimler={data.gunBolumuneGore} />
          <Bolum baslik="Yıllara göre" dilimler={data.yillaraGore} />
        </Tabs.Content>

        <Tabs.Content value="kim" className="grid gap-4 lg:grid-cols-2">
          <Bolum baslik="En yoğun konumlar" dilimler={data.konumaGore} enFazla={10} />
          <Bolum baslik="En çok etkinlik oluşturanlar" dilimler={data.olusturanaGore} enFazla={10} />
        </Tabs.Content>

        <Tabs.Content value="hazirlik" className="grid gap-4 lg:grid-cols-2">
          <Bolum baslik="Hazırlık durumu" dilimler={data.hazirlikDurumu} />
          <Bolum baslik="Basın katılımı" dilimler={data.basinKatilimi} />
          <Bolum baslik="Fotoğraf durumu" dilimler={data.fotografDurumu} />
          <Bolum baslik="Çiçek durumu" dilimler={data.cicekDurumu} />
          <Bolum baslik="Tekrar durumu" dilimler={data.tekrarDurumu} />
          <Bolum baslik="Tüm gün etkinlikleri" dilimler={data.tumGunDurumu} />
        </Tabs.Content>

        <Tabs.Content value="seyir" className="space-y-4">
          <Card>
            <CardHeader
              baslik="Günlük yoğunluk"
              aciklama="Son 60 gün"
              eylem={<TrendingUp size={15} className="text-text-3" />}
            />
            <div className="p-4">
              <TimeSeries
                noktalar={(data.gunlukYogunluk ?? []).slice(-60) as never}
                etiket="Günlük yoğunluk"
                yukseklik={180}
              />
            </div>
          </Card>

          <Card>
            <CardHeader baslik="Aylık tamamlanma oranı" aciklama="Yüzde" />
            <div className="p-4">
              <TimeSeries
                noktalar={(data.aylikTamamlanmaOrani ?? []) as never}
                etiket="Aylık tamamlanma oranı"
                tur="cizgi"
                yukseklik={180}
              />
            </div>
          </Card>
        </Tabs.Content>
      </Tabs.Root>
    </div>
  );
}

function Bolum({
  baslik,
  dilimler,
  enFazla,
}: {
  baslik: string;
  dilimler?: { etiket?: string | null; deger?: number; yuzde?: number; renk?: string | null }[] | null;
  enFazla?: number;
}) {
  return (
    <Card>
      <CardHeader baslik={baslik} />
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

/**
 * Aralığı sunucunun beklediği biçime çevirir.
 *
 * `tumZamanlar` için parametre GÖNDERİLMEZ; sunucunun kendi varsayılanı
 * devreye girer. Buradan uydurma bir "1900" tarihi göndermek, sorgunun
 * indeks kullanımını bozardı.
 */
