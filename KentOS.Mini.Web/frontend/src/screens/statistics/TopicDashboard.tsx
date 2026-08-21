import { ArrowLeft, BarChart3, Download, FileSpreadsheet } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button } from '../../components/Button';
import { Card, CardHeader, StatTile } from '../../components/Card';
import { DistributionBar, DonutDistribution, TimeSeries } from '../../components/StatisticsCharts';
import { EmptyState } from '../../components/EmptyState';
import { SegmentedSelect } from '../../components/Filters';
import { Skeleton } from '../../components/Skeleton';
import { useToast } from '../../components/Toast';
import { download } from '../../data/download';
import { useTopicStatistics } from '../../data/hooks';
import { konuBul } from './catalog';
import { aralikHesapla, ARALIK_ETIKETLERI, type Aralik } from './range';

/**
 * KONU PANOSU — altı konu, TEK çizici.
 *
 * <p>
 * Halk günü, form, protokol, çiçek, özgeçmiş ve sistem panoları sunucudan
 * aynı şekli (`KonuIstatistigiDto`) alıyor; burada yapılan iş yalnızca
 * yerleştirme. Her konuya ayrı bir ekran yazılsaydı altı neredeyse birebir
 * kopya olurdu ve biri değiştiğinde ötekiler geride kalırdı.
 * </p>
 *
 * <p>
 * <b>Hesaplama İSTEMCİDE TEKRARLANMAZ.</b> Yüzdeler ve karo metinleri
 * sunucudan biçimlenmiş geliyor; ekrandaki sayı ile Excel'deki sayı aynı
 * yerden çıksın diye.
 * </p>
 */
export default function TopicDashboard() {
  const { konu } = useParams<{ konu: string }>();
  const tanim = konuBul(konu);
  const [aralik, setAralik] = useState<Aralik>('son12Ay');
  const [indiriliyor, setIndiriliyor] = useState(false);
  const { bildir } = useToast();

  const [bas, bit] = aralikHesapla(aralik);
  const { data, isLoading, isError, error } = useTopicStatistics(konu ?? '', bas, bit);

  if (!tanim) {
    return (
      <EmptyState
        ikon={BarChart3}
        baslik="Böyle bir istatistik yok"
        aciklama="Bağlantı eski olabilir; merkezden bir başlık seçin."
      />
    );
  }

  const ciktiAl = async () => {
    setIndiriliyor(true);

    try {
      await download(`/istatistik/${tanim.konu}/excel`, { baslangic: bas, bitis: bit });
    } catch (h) {
      bildir('hata', 'Çıktı alınamadı', (h as Error).message);
    } finally {
      setIndiriliyor(false);
    }
  };

  return (
    <div className="space-y-4 md:space-y-5">
      {/* ── başlık ve dönem ── */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="min-w-0">
          {/* Geri bağlantısı BAŞLIĞIN ÜSTÜNDE: merkez bir ekran değil bir
              menü, kullanıcı oraya sık dönüyor. */}
          <Link
            to="/istatistikler"
            className="inline-flex items-center gap-1 text-xs font-semibold text-text-3
                       transition-colors hover:text-brand"
          >
            <ArrowLeft size={13} />
            İstatistikler
          </Link>
          <h1 className="mt-0.5 font-display text-xl font-extrabold tracking-[-0.02em]">
            {data?.baslik ?? tanim.baslik}
          </h1>
        </div>

        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <SegmentedSelect<Aralik>
            deger={aralik}
            degistir={setAralik}
            etiket="Zaman aralığı"
            secenekler={(Object.keys(ARALIK_ETIKETLERI) as Aralik[]).map((a) => ({
              deger: a,
              etiket: ARALIK_ETIKETLERI[a],
            }))}
          />

          <Button
            varyant="ikincil"
            onClick={ciktiAl}
            disabled={indiriliyor || isLoading || !data}
          >
            {indiriliyor ? <Download size={15} /> : <FileSpreadsheet size={15} />}
            {indiriliyor ? 'Hazırlanıyor…' : 'Excel'}
          </Button>
        </div>
      </div>

      {isLoading && (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-[92px] w-full" />
            ))}
          </div>
          <Skeleton className="h-56 w-full" />
        </div>
      )}

      {isError && (
        <EmptyState
          ikon={BarChart3}
          baslik="İstatistikler yüklenemedi"
          aciklama={(error as Error)?.message}
        />
      )}

      {data && (
        <>
          {data.not && (
            <p className="rounded-md bg-sunken px-3 py-2 text-xs font-medium text-text-2">
              {data.not}
            </p>
          )}

          {/* ── sayı karoları ── */}
          {data.karolar && data.karolar.length > 0 && (
            <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
              {data.karolar.map((k, i) => (
                <StatTile
                  key={i}
                  etiket={k.etiket ?? ''}
                  deger={k.deger ?? '—'}
                  altMetin={k.altMetin ?? undefined}
                  vurgu={tonRengi(k.ton)}
                />
              ))}
            </div>
          )}

          {/* ── aylık seyir ── */}
          {data.seyir && data.seyir.length > 0 && (
            <Card>
              <CardHeader
                baslik={data.seyirEtiketi ?? 'Aylık seyir'}
                aciklama="Seçili aralık boyunca"
              />
              <div className="p-4">
                <TimeSeries
                  noktalar={data.seyir.map((n) => ({
                    etiket: n.etiket ?? '',
                    deger: n.deger ?? 0,
                  }))}
                  etiket={data.seyirEtiketi ?? 'Aylık seyir'}
                  tur="sutun"
                />
              </div>
            </Card>
          )}

          {/* ── dağılımlar ── */}
          {data.bolumler && data.bolumler.length > 0 && (
            <div className="grid gap-4 lg:grid-cols-2">
              {data.bolumler.map((b, i) => {
                const dilimler = (b.dilimler ?? []).map((d) => ({
                  etiket: d.etiket ?? '—',
                  deger: d.deger ?? 0,
                  yuzde: d.yuzde ?? 0,
                  renk: d.renk ?? undefined,
                }));

                return (
                  <Card key={i}>
                    <CardHeader baslik={b.baslik ?? ''} aciklama={b.aciklama ?? undefined} />
                    <div className="p-4">
                      {dilimler.length === 0 ? (
                        <p className="text-sm text-text-3">Bu dönemde kayıt yok.</p>
                      ) : b.gorunum === 'halka' ? (
                        <DonutDistribution dilimler={dilimler} etiket={b.baslik ?? ''} />
                      ) : (
                        <DistributionBar dilimler={dilimler} etiket={b.baslik ?? ''} />
                      )}
                    </div>
                  </Card>
                );
              })}
            </div>
          )}

          {bosPano(data) && (
            <EmptyState
              ikon={BarChart3}
              baslik="Bu dönemde kayıt yok"
              aciklama="Zaman aralığını genişletmeyi deneyin."
            />
          )}
        </>
      )}
    </div>
  );
}

/**
 * Sunucudan gelen TON ADI karo rengine çevrilir.
 *
 * <p>
 * Sunucu `#RRGGBB` göndermiyor: renkler kurum kaydından geliyor ve sunucudan
 * renk kodu yollamak beyaz etiket sözleşmesini bozardı. Karşılık burada,
 * durum token'ları üzerinden kuruluyor.
 * </p>
 *
 * <p>
 * <b>Dönen şey token ADI, sarmalanmış değer DEĞİL</b> — `StatTile` değeri
 * kendisi sarıyor. İlk yazımda sarmalanmış ve depoda BULUNMAYAN bir token
 * adı döndürülmüştü; renk sessizce hiç uygulanmıyordu. `tokens.test.ts`
 * yakaladı — o bekçi kaynağı ham tarıyor, yorum satırları dahil.
 * </p>
 */
function tonRengi(ton: string | null | undefined): string | undefined {
  if (ton === 'iyi') return '--st-ok';
  if (ton === 'uyari') return '--st-warn';
  if (ton === 'kotu') return '--st-no';
  return undefined;
}

/** Karolar sıfır, dağılım yok ve seyir düz sıfırsa pano gerçekten boştur. */
function bosPano(d: { bolumler?: unknown[] | null; seyir?: { deger?: number | null }[] | null }) {
  const dagilimVar = (d.bolumler ?? []).length > 0;
  const seyirVar = (d.seyir ?? []).some((n) => (n.deger ?? 0) > 0);
  return !dagilimVar && !seyirVar;
}
