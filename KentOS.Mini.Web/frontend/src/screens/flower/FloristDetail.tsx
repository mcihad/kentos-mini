import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft, CalendarDays, FileSpreadsheet, FileText, Flower2, MapPin, Phone,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { FieldWrapper } from '../../components/Field';
import { EmptyState } from '../../components/EmptyState';
import { Button, IconButton } from '../../components/Button';
import { ActionSheet } from '../../components/ActionSheet';
import { SkeletonRows } from '../../components/Skeleton';
import { Card, CardHeader } from '../../components/Card';
import { InsetGroup, ListRow } from '../../components/ListRow';
import { DatePicker } from '../../components/DatePicker';
import { useIsDesktop } from '../../components/screenSize';
import { cn } from '../../components/utils';
import { date, shortDate } from '../../data/format';
import { download } from '../../data/download';
import { api, queryString } from '../../data/client';
import type { FloristDetail as CicekciDetayTipi } from '../../data/types';

/**
 * ÇİÇEKÇİ DOSYASI — gönderilen çiçekler ve bağlı oldukları programlar.
 *
 * <p>
 * Ay sonunda çiçekçiyle hesaplaşılıyor: "şu tarihler arasında kaç çiçek
 * gönderdin, hangi programlara?" Bu bilgi sistemde vardı ama dağınıktı —
 * talimatlar çiçekçi kaydında, program bilgisi etkinlikte ve arada tarih
 * süzgeci yoktu. Liste artık tek yerde, dönemi seçilebiliyor ve Excel/PDF
 * olarak çıkıyor.
 * </p>
 */
export default function FloristDetail() {
  const { id } = useParams<{ id: string }>();
  const cicekciId = Number(id);
  const masaustu = useIsDesktop();

  /*
    Dönem BOŞ başlar: kullanıcı önce bütün geçmişi görsün, sonra daraltsın.
    Varsayılan "bu ay" olsaydı ekran açılışta çoğu zaman boş görünür ve
    "kayıt yok" sanılırdı.
  */
  const [baslangic, setBaslangic] = useState('');
  const [bitis, setBitis] = useState('');

  const sorgu = queryString({ baslangic: baslangic || undefined, bitis: bitis || undefined });

  const detay = useQuery({
    queryKey: ['cicekci', cicekciId, 'detay', baslangic, bitis] as const,
    queryFn: () => api.get<CicekciDetayTipi>(`/cicek/cicekciler/${cicekciId}/detay${sorgu}`),
    enabled: Number.isFinite(cicekciId) && cicekciId > 0,
  });

  const c = detay.data;
  const talimatlar = c?.talimatlar ?? [];

  const excelIndir = () => download(`/cicek/cicekciler/${cicekciId}/excel${sorgu}`);
  const pdfIndir = () => download(`/cicek/cicekciler/${cicekciId}/pdf${sorgu}`);

  if (detay.isLoading) return <SkeletonRows adet={6} />;

  if (detay.isError || !c) {
    return (
      <EmptyState
        ikon={Flower2}
        baslik="Çiçekçi bulunamadı"
        aciklama={(detay.error as Error)?.message}
      />
    );
  }

  return (
    <div className="space-y-3.5">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-2.5">
        <Link to="/cicek" className="shrink-0">
          <IconButton etiket="Çiçekçilere dön">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="truncate font-display text-xl font-bold tracking-[-0.015em] md:text-2xl">
            {c.adSoyad}
          </h2>
          <p className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-ink-3">
            {c.telefon && (
              <a href={`tel:${c.telefon}`} className="inline-flex items-center gap-1 tabular-nums hover:text-brand">
                <Phone size={11} />
                {c.telefon}
              </a>
            )}
            {c.adres && (
              <span className="inline-flex min-w-0 items-center gap-1">
                <MapPin size={11} className="shrink-0" />
                <span className="truncate">{c.adres}</span>
              </span>
            )}
            <span className={c.aktif ? 'text-ok' : 'text-danger'}>
              {c.aktif ? 'Aktif' : 'Pasif'}
            </span>
          </p>
        </div>

        {/* Masaüstünde çıktılar başlıkta; mobilde FAB'da. */}
        <div className="hidden shrink-0 gap-2 md:flex">
          <Button varyant="ikincil" onClick={excelIndir}>
            <FileSpreadsheet size={14} />
            Excel
          </Button>
          <Button varyant="ikincil" onClick={pdfIndir}>
            <FileText size={14} />
            PDF
          </Button>
        </div>
      </div>

      {!masaustu && (
        <ActionSheet
          baslik="Çiçekçi çıktıları"
          eylemler={[
            { etiket: 'Excel indir', ikon: <FileSpreadsheet size={17} />, onClick: excelIndir },
            { etiket: 'PDF indir', ikon: <FileText size={17} />, onClick: pdfIndir },
          ]}
        />
      )}

      {/* ── Dönem ── */}
      <Card className="p-3">
        <div className="grid gap-2.5 sm:grid-cols-2">
          <FieldWrapper etiket="Başlangıç" id="cd-bas">
            <DatePicker id="cd-bas" deger={baslangic} degistir={setBaslangic} temizlenebilir />
          </FieldWrapper>
          <FieldWrapper etiket="Bitiş" id="cd-bit">
            <DatePicker id="cd-bit" deger={bitis} degistir={setBitis} temizlenebilir />
          </FieldWrapper>
        </div>
        {(baslangic || bitis) && (
          <button
            type="button"
            onClick={() => {
              setBaslangic('');
              setBitis('');
            }}
            className="mt-2 text-xs text-ink-3 underline-offset-2 hover:underline"
          >
            Dönemi temizle
          </button>
        )}
      </Card>

      {/* ── Sayılar ── */}
      <div className="grid grid-cols-2 gap-2.5 lg:grid-cols-4">
        <Sayi etiket="Talimat" deger={c.toplam ?? 0} />
        <Sayi etiket="Gönderildi" deger={c.gonderilen ?? 0} ton="ok" />
        <Sayi etiket="Bekliyor" deger={c.bekleyen ?? 0} ton="wait" />
        <Sayi etiket="Program" deger={c.programSayisi ?? 0} />
      </div>

      {/* ── Talimatlar ── */}
      {talimatlar.length === 0 ? (
        <EmptyState
          ikon={Flower2}
          baslik="Bu dönemde talimat yok"
          aciklama="Tarih aralığını genişletebilir ya da temizleyebilirsiniz."
        />
      ) : masaustu ? (
        <Card>
          <CardHeader
            baslik="Çiçek talimatları"
            aciklama={`${talimatlar.length} kayıt`}
          />
          <ul className="divide-y divide-line">
            {talimatlar.map((t) => (
              <li key={t.id} className="flex items-start gap-3 p-3.5">
                <span className="mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-sm bg-brand-soft text-brand">
                  <Flower2 size={15} />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-semibold">{t.ad || '—'}</p>
                  <p className="mt-0.5 text-xs text-ink-2">
                    {t.etkinlikBaslik ? (
                      <Link to={`/ajanda/${t.etkinlikId}`} className="hover:text-brand hover:underline">
                        {t.etkinlikBaslik}
                      </Link>
                    ) : (
                      <span className="text-ink-3">Programa bağlı değil</span>
                    )}
                    {t.etkinlikTarihi && (
                      <span className="text-ink-3"> · {date(t.etkinlikTarihi)}</span>
                    )}
                  </p>
                  {t.adres && <p className="mt-0.5 text-2xs text-ink-3">{t.adres}</p>}
                </div>
                <div className="shrink-0 text-right">
                  <DurumCipi gonderildi={t.gonderildi ?? false} />
                  <p className="mt-1 text-2xs tabular-nums text-ink-3">
                    {shortDate(t.olusturulmaTarihi)}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        </Card>
      ) : (
        <InsetGroup baslik={`Çiçek talimatları · ${talimatlar.length}`}>
          {talimatlar.map((t, i) => (
            <ListRow
              key={t.id}
              sira={i}
              sonuncu={i === talimatlar.length - 1}
              yol={t.etkinlikId ? `/ajanda/${t.etkinlikId}` : undefined}
              ikon={<Flower2 size={15} strokeWidth={1.9} />}
              ikonRengi={t.gonderildi ? 'var(--ok)' : 'var(--warn)'}
              ust={
                <>
                  <span className="tabular-nums">{shortDate(t.olusturulmaTarihi)}</span>
                  <span>{t.gonderildi ? 'Gönderildi' : 'Bekliyor'}</span>
                </>
              }
              baslik={t.ad || 'Çiçek talimatı'}
              alt={
                <>
                  {t.etkinlikBaslik ? (
                    <span className="truncate">{t.etkinlikBaslik}</span>
                  ) : (
                    <span className="text-ink-3">Programa bağlı değil</span>
                  )}
                  {t.etkinlikTarihi && (
                    <span className="shrink-0 tabular-nums">· {shortDate(t.etkinlikTarihi)}</span>
                  )}
                </>
              }
            />
          ))}
        </InsetGroup>
      )}
    </div>
  );
}

function Sayi({
  etiket,
  deger,
  ton,
}: {
  etiket: string;
  deger: number;
  ton?: 'ok' | 'wait';
}) {
  return (
    <Card className="p-3">
      <p className="text-2xs uppercase tracking-[0.08em] text-ink-3">{etiket}</p>
      <p
        className={cn(
          'mt-1 font-display text-2xl font-bold tabular-nums leading-none',
          ton === 'ok' && 'text-ok',
          ton === 'wait' && 'text-warn',
        )}
      >
        {deger}
      </p>
    </Card>
  );
}

function DurumCipi({ gonderildi }: { gonderildi: boolean }) {
  return (
    <span
      className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-2xs font-semibold"
      style={
        gonderildi
          ? { background: 'var(--ok-soft)', color: 'var(--ok)' }
          : { background: 'var(--warn-soft)', color: 'var(--warn)' }
      }
    >
      <CalendarDays size={10} />
      {gonderildi ? 'Gönderildi' : 'Bekliyor'}
    </span>
  );
}
