import { AlertTriangle, ClipboardCheck, Gauge, Inbox, UserX } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardHeader, StatTile } from '../components/Card';
import { ColoredBadge } from '../components/Color';
import { EChart, palette } from '../components/EChart';
import { EmptyState } from '../components/EmptyState';
import { SegmentedSelect } from '../components/Filters';
import { Skeleton } from '../components/Skeleton';
import { useWorkStatistics } from '../data/citizen';
import { SlaBadge, StageProgress } from './task/TaskBits';
import { UnitScopePicker } from '../components/UnitScopePicker';

type Kapsam = 'kendi' | 'alt';

/**
 * GECİKME PANOSU — birim karnesi ve süre aşımları.
 *
 * <p>
 * <b>Bu bir sıralama değil bir uyarı ekranı.</b> Birimler en çok gecikene
 * göre sıralı; amaç kim daha iyi sorusunu değil "nereye bakmak gerekiyor"
 * sorusunu cevaplamak.
 * </p>
 *
 * <p>
 * <b>Kişi bazlı ölçüm YOK.</b> Kim kaç iş bitirdi tablosu, kurumda ölçmek
 * istediğimiz şeyi (hizmetin süresinde verilip verilmediğini) değil personel
 * kıyaslamasını üretirdi.
 * </p>
 */
export default function WorkDashboard() {
  const [kapsam, setKapsam] = useState<Kapsam>('alt');
  const { data: pano, isLoading } = useWorkStatistics(kapsam === 'alt');

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!pano) return <EmptyState ikon={Gauge} baslik="Pano yüklenemedi" />;

  const dagilim = pano.durumDagilimi ?? [];

  return (
    <div className="space-y-3.5">
      <div className="flex flex-wrap items-center gap-2">
        <UnitScopePicker />

        <SegmentedSelect<Kapsam>
          deger={kapsam}
          degistir={setKapsam}
          etiket="Kapsam"
          secenekler={[
            { deger: 'kendi', etiket: 'Birimim' },
            { deger: 'alt', etiket: 'Alt birimler' },
          ]}
          className="md:ml-auto"
        />
      </div>

      {/* ── Sayı karoları ── */}
      <div className="grid grid-cols-2 gap-2.5 lg:grid-cols-4">
        <StatTile
          etiket="Açık iş"
          deger={pano.acik ?? 0}
          ikon={<ClipboardCheck size={16} />}
        />
        <StatTile
          etiket="Süresi aşılan"
          deger={pano.geciken ?? 0}
          ikon={<AlertTriangle size={16} />}
          vurgu={(pano.geciken ?? 0) > 0 ? 'var(--st-no)' : undefined}
        />
        {/* ATANMAMIŞ ayrı bir karo: gecikmenin en sık sebebi kimseye
            verilmemiş iş ve bu, "açık iş" içinde görünmez kalıyordu. */}
        <StatTile
          etiket="Atanmamış"
          deger={pano.atanmamis ?? 0}
          ikon={<UserX size={16} />}
          vurgu={(pano.atanmamis ?? 0) > 0 ? 'var(--st-wait)' : undefined}
        />
        <StatTile
          etiket="Onay bekleyen"
          deger={pano.onayBekleyen ?? 0}
          ikon={<ClipboardCheck size={16} />}
          altMetin={`Bugün ${pano.bugunTamamlanan ?? 0} tamamlandı`}
        />
      </div>

      {/* Kuyruklar: karşılama ve devir. */}
      {((pano.bekleyenBildirim ?? 0) > 0 || (pano.bekleyenDevir ?? 0) > 0) && (
        <div className="flex flex-wrap gap-2.5">
          {(pano.bekleyenBildirim ?? 0) > 0 && (
            <Link to="/vatandas-bildirimleri" className="flex-1">
              <Card className="flex items-center gap-2.5 p-3 hover:border-brand">
                <Inbox size={18} className="text-(--st-wait)" />
                <span className="text-sm text-ink">
                  <b>{pano.bekleyenBildirim}</b> vatandaş bildirimi bekliyor
                </span>
              </Card>
            </Link>
          )}
          {(pano.bekleyenDevir ?? 0) > 0 && (
            <Link to="/gelen-kutusu" className="flex-1">
              <Card className="flex items-center gap-2.5 p-3 hover:border-brand">
                <Inbox size={18} className="text-(--st-wait)" />
                <span className="text-sm text-ink">
                  <b>{pano.bekleyenDevir}</b> gelen kutusu kaydı bekliyor
                </span>
              </Card>
            </Link>
          )}
        </div>
      )}

      {/* ── Durum dağılımı ── */}
      {dagilim.length > 0 && (
        <Card className="p-3.5">
          <CardHeader baslik="Durum dağılımı" className="mb-2" />
          <EChart
            etiket="Görevlerin durum dağılımı"
            yukseklik={220}
            secenekler={{
              tooltip: { trigger: 'item' },
              series: [
                {
                  type: 'pie',
                  radius: ['48%', '72%'],
                  itemStyle: { borderWidth: 2, borderColor: 'var(--surface)' },
                  label: { formatter: '{b}\n{c}' },
                  data: dagilim.map((d, i) => ({
                    name: d.etiket,
                    value: d.deger,
                    // Renk SUNUCUDAN (durum rengi); yoksa palete düşülüyor.
                    itemStyle: { color: d.renk || palette()[i % palette().length] },
                  })),
                },
              ],
            }}
          />
        </Card>
      )}

      {/* ── Birim karnesi ── */}
      <Card>
        <CardHeader
          baslik="Birim karnesi"
          aciklama="En çok geciken üstte — bu bir sıralama değil, nereye bakılacağı."
        />

        {(pano.birimler ?? []).length === 0 ? (
          <div className="px-3.5 pb-4">
            <EmptyState ikon={Gauge} baslik="Ölçülecek iş yok" />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-line text-2xs uppercase tracking-wide text-ink-3">
                  <th className="px-3.5 py-2 text-left font-medium">Birim</th>
                  <th className="px-2 py-2 text-right font-medium">Açık</th>
                  <th className="px-2 py-2 text-right font-medium">Geciken</th>
                  <th className="px-2 py-2 text-right font-medium">Tamamlanan</th>
                  <th className="px-2 py-2 text-right font-medium">Zamanında</th>
                  <th className="px-3.5 py-2 text-right font-medium">Ort. süre</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {(pano.birimler ?? []).map((b) => (
                  <tr key={b.birimId}>
                    <td className="px-3.5 py-2.5 text-ink">{b.birimAd}</td>
                    <td className="px-2 py-2.5 text-right tabular-nums text-text-2">{b.acik}</td>
                    <td
                      className={`px-2 py-2.5 text-right tabular-nums ${
                        (b.geciken ?? 0) > 0 ? 'font-medium text-(--st-no)' : 'text-text-3'
                      }`}
                    >
                      {b.geciken}
                    </td>
                    <td className="px-2 py-2.5 text-right tabular-nums text-text-2">
                      {b.tamamlanan}
                    </td>
                    <td className="px-2 py-2.5 text-right tabular-nums text-text-2">
                      {/* SLA'sı olmayan iş orana girmiyor; ölçülmemiş bir şeyi
                          "zamanında" saymak sayıyı şişirirdi. */}
                      {b.zamanindaOran == null ? (
                        <span className="text-text-3" title="Ölçülebilir iş yok">—</span>
                      ) : (
                        `%${b.zamanindaOran}`
                      )}
                    </td>
                    <td className="px-3.5 py-2.5 text-right tabular-nums text-text-2">
                      {b.ortalamaSaat == null ? (
                        <span className="text-text-3">—</span>
                      ) : (
                        `${b.ortalamaSaat} sa`
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* ── Geciken işler ── */}
      <Card>
        <CardHeader
          baslik="Süresi aşılan işler"
          aciklama={
            (pano.gecikenler ?? []).length === 0
              ? undefined
              : 'En uzun gecikenler önce'
          }
        />

        {(pano.gecikenler ?? []).length === 0 ? (
          <div className="px-3.5 pb-4">
            <EmptyState ikon={ClipboardCheck} baslik="Süresi aşılan iş yok" />
          </div>
        ) : (
          <ul className="divide-y divide-line">
            {(pano.gecikenler ?? []).map((g) => (
              <li key={g.id}>
                <Link
                  to={`/gorevler/${g.id}`}
                  className="flex items-center gap-2.5 px-3.5 py-2.5 hover:bg-sunken"
                >
                  <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm text-ink">{g.baslik}</span>
                    <span className="flex items-center gap-2 text-2xs text-ink-3">
                      <span className="font-mono tabular-nums">{g.takipNo}</span>
                      {g.birimAd && <span className="truncate">· {g.birimAd}</span>}
                      <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} ilerleme={g.ilerleme} />
                    </span>
                  </span>
                  <SlaBadge gecikti={!!g.gecikti} kalanSaat={g.kalanSaat} kisa />
                </Link>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
