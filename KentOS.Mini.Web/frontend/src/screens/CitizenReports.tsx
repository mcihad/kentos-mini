import {
  AlertTriangle, Camera, Inbox, MapPin, Phone, Search, Send, X,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { PhotoGrid } from '../components/PhotoGrid';
import { ColoredBadge } from '../components/Color';
import { EmptyState } from '../components/EmptyState';
import { FieldWrapper, SearchInput, Secim, Textarea } from '../components/Field';
import { ChipStrip, FilterChip } from '../components/Filters';
import { FormModal } from '../components/FormModal';
import { Pagination } from '../components/Pagination';
import { SkeletonRows } from '../components/Skeleton';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { dateTime } from '../data/format';
import { useUnits } from '../data/hooks';
import { useUsableTaskTypes } from '../data/tasks';
import {
  useCitizenReport, useCitizenReportAttachments, useCitizenReportMutations,
  useCitizenReports,
} from '../data/citizen';
import {
  REPORT_STATUS, REPORT_STATUS_LABELS, TASK_PRIORITY_LABELS, type CitizenReport,
} from '../data/types';

/**
 * VATANDAŞ BİLDİRİMLERİ — karşılama ekranı.
 *
 * <p>
 * Buradaki işin tamamı <b>ayıklamak ve yönlendirmek</b>: gelen kaydı okuyup
 * ilgili birime göndermek ya da gerekçesiyle işleme almamak. Bildirim bir
 * görev değil; görev ancak yönlendirmeyle doğuyor.
 * </p>
 *
 * <p>
 * <b>Mükerrer sayacı satırda.</b> Aynı numaradan gelen önceki kayıt sayısı
 * görünmeseydi personel her bildirimi sıfırdan değerlendirir ve aynı çukur
 * için beş ayrı görev açılırdı.
 * </p>
 */
export default function CitizenReports() {
  const { hasPermission } = useSession();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [durum, setDurum] = useState<number | null>(REPORT_STATUS.yeni);
  const [sayfa, setSayfa] = useState(1);
  const [acilan, setAcilan] = useState<number | null>(null);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const { data, isLoading } = useCitizenReports({ sayfa, boyut: 25, ara: arama, durum });
  const kayitlar = data?.veriler ?? [];

  return (
    <div className="space-y-3.5">
      <div className="flex flex-wrap items-center gap-2">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Konu, ad, telefon veya takip numarası"
          aria-label="Bildirimlerde ara"
          ikon={<Search size={15} />}
          className="min-w-0 flex-1 md:max-w-[360px]"
        />

        <Link to="/harita" className="md:ml-auto">
          <Button varyant="ikincil">
            <MapPin size={15} />
            Haritada gör
          </Button>
        </Link>
      </div>

      {/* Varsayılan BEKLEYENLER: karşılama ekranının işi zaten sıradakiler. */}
      <ChipStrip>
        {[REPORT_STATUS.yeni, REPORT_STATUS.yonlendirildi, REPORT_STATUS.reddedildi].map((d) => (
          <FilterChip
            key={d}
            secili={durum === d}
            tikla={() => {
              setDurum(d);
              setSayfa(1);
            }}
          >
            {REPORT_STATUS_LABELS[d]}
          </FilterChip>
        ))}
        <FilterChip
          secili={durum === null}
          tikla={() => {
            setDurum(null);
            setSayfa(1);
          }}
        >
          Tümü
        </FilterChip>
      </ChipStrip>

      {isLoading ? (
        <SkeletonRows adet={5} />
      ) : kayitlar.length === 0 ? (
        <EmptyState
          ikon={Inbox}
          baslik={arama ? 'Eşleşen bildirim yok' : 'Bekleyen bildirim yok'}
          aciklama={
            arama
              ? 'Aramayı temizleyerek tüm bildirimleri görebilirsiniz.'
              : 'Vatandaş portalından gelen yeni bir kayıt bulunmuyor.'
          }
        />
      ) : (
        <>
          <div className="space-y-2.5">
            {kayitlar.map((b) => (
              <BildirimKarti key={b.id} bildirim={b} ac={() => setAcilan(b.id!)} />
            ))}
          </div>

          <Pagination sonuc={data} sayfaDegistir={setSayfa} birim="bildirim" className="mt-3" />
        </>
      )}

      {acilan && (
        <BildirimDetayi
          id={acilan}
          kapat={() => setAcilan(null)}
          yetkili={hasPermission(PERMISSION.bildirimYonlendir)}
        />
      )}
    </div>
  );
}

/**
 * Bildirimin RESİM olan eklerini görüntüleyici biçimine çevirir.
 *
 * Adres bildirime özel uçtan geçiyor (`vatandas-bildirimi/{id}/ek/{ekId}`),
 * görev ek ucundan değil: karşılama personelinin görev izni olmak zorunda
 * değil ve o uç `gorev.goruntule` istiyor.
 */
function bildirimFotograflari(
  bildirimId: number,
  ekler: NonNullable<CitizenReport['ekler']>,
) {
  return ekler
    .filter((e) => e.resimMi)
    .map((e) => ({
      yol: `/api/v2/vatandas-bildirimi/${bildirimId}/ek/${e.id}`,
      baslik: e.ad,
      altBilgi: dateTime(e.tarih),
    }));
}

function BildirimKarti({ bildirim: b, ac }: { bildirim: CitizenReport; ac: () => void }) {
  return (
    <Card className="p-3.5">
      <div className="flex items-start gap-2.5">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-2xs tabular-nums text-ink-3">{b.takipNo}</span>
            <ColoredBadge etiket={b.durumAd} renk={b.durumRenk} />

            {/* MÜKERRER UYARISI: aynı numaradan önceki kayıtlar. */}
            {(b.ayniNumaradanOnceki ?? 0) > 0 && (
              <span className="inline-flex items-center gap-1 text-2xs font-medium text-(--st-wait)">
                <AlertTriangle size={12} />
                aynı numaradan {b.ayniNumaradanOnceki} kayıt daha
              </span>
            )}
          </div>

          <h3 className="mt-1 font-display text-sm font-semibold text-ink">{b.konu}</h3>
          <p className="mt-1 line-clamp-2 text-xs text-text-2">{b.aciklama}</p>

          <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-2xs text-ink-3">
            <span>{b.adSoyad}</span>
            <span className="inline-flex items-center gap-1">
              <Phone size={11} />
              {b.telefon}
            </span>
            {b.adres && (
              <span className="inline-flex items-center gap-1 truncate">
                <MapPin size={11} />
                {b.adres}
              </span>
            )}
            {(b.ekSayisi ?? 0) > 0 && (
              <span className="inline-flex items-center gap-1">
                <Camera size={11} />
                {b.ekSayisi}
              </span>
            )}
            <span>{dateTime(b.olusturmaTarihi)}</span>
          </div>

          {/*
            LİSTEDE DE ÖNİZLEME.

            Karşılama ekranının işi sırayla karar vermek; her kayıt için
            ayrıntıyı açıp kapatmak, otuz kayıtlık bir kuyrukta altmış
            dokunuş demek. İki küçük görsel çoğu kararı listede verdiriyor.
          */}
          {(b.ekler ?? []).some((e) => e.resimMi) && (
            <PhotoGrid
              fotograflar={bildirimFotograflari(b.id!, b.ekler!).slice(0, 3)}
              boyut="kucuk"
              className="mt-2"
            />
          )}

          {b.gorevTakipNo && (
            <p className="mt-2 text-2xs text-(--st-ok)">
              {b.birimAd} · <Link to={`/gorevler/${b.gorevId}`} className="underline">
                {b.gorevTakipNo}
              </Link>
            </p>
          )}
          {b.durum === REPORT_STATUS.reddedildi && b.islemNotu && (
            <p className="mt-2 text-2xs text-text-3">Gerekçe: {b.islemNotu}</p>
          )}
        </div>

        <Button varyant="ikincil" className="shrink-0" onClick={ac}>
          İncele
        </Button>
      </div>
    </Card>
  );
}

/**
 * Bildirim ayrıntısı ve karar.
 *
 * <p>
 * Yönlendirme ile ret <b>aynı kutuda</b>: ikisi de tek bir kararın iki
 * cevabı ve personel kaydı okurken hangisini vereceğine karar veriyor. Ayrı
 * ekranlarda olsalardı kayda iki kez bakmak gerekirdi.
 * </p>
 */
function BildirimDetayi({
  id,
  kapat,
  yetkili,
}: {
  id: number;
  kapat: () => void;
  yetkili: boolean;
}) {
  const { bildir } = useToast();
  const { data: b } = useCitizenReport(id);
  const { data: ekler } = useCitizenReportAttachments(id);
  const m = useCitizenReportMutations(id);
  const birimler = useUnits();
  const tipler = useUsableTaskTypes();

  const [birimId, setBirimId] = useState<number | null>(null);
  const [tipId, setTipId] = useState<number | null>(null);
  const [oncelik, setOncelik] = useState<number>(1);
  const [not, setNot] = useState('');
  const [retKipi, setRetKipi] = useState(false);

  if (!b) return null;

  const islenmis = b.durum !== REPORT_STATUS.yeni;

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={b.konu ?? 'Bildirim'}
      aciklama={`${b.takipNo} · ${b.adSoyad} · ${b.telefon}`}
      genislik="genis"
      eylemler={
        islenmis || !yetkili ? (
          <Button varyant="ikincil" onClick={kapat}>
            Kapat
          </Button>
        ) : retKipi ? (
          <>
            <Button varyant="ikincil" onClick={() => setRetKipi(false)}>
              Vazgeç
            </Button>
            <Button
              varyant="yikici"
              disabled={not.trim().length === 0 || m.reddet.isPending}
              onClick={async () => {
                try {
                  await m.reddet.mutateAsync(not.trim());
                  bildir('basari', 'Bildirim işleme alınmadı');
                  kapat();
                } catch (h) {
                  bildir('hata', 'İşlenemedi', (h as Error).message);
                }
              }}
            >
              <X size={15} />
              İşleme alma
            </Button>
          </>
        ) : (
          <>
            <Button varyant="ikincil" onClick={() => setRetKipi(true)}>
              İşleme alma
            </Button>
            <Button
              disabled={!birimId || m.yonlendir.isPending}
              onClick={async () => {
                try {
                  await m.yonlendir.mutateAsync({
                    birimId: birimId!,
                    gorevTipiId: tipId,
                    oncelik: oncelik as never,
                    not: not.trim() || null,
                  });
                  bildir('basari', 'Bildirim yönlendirildi ve görev açıldı');
                  kapat();
                } catch (h) {
                  bildir('hata', 'Yönlendirilemedi', (h as Error).message);
                }
              }}
            >
              <Send size={15} />
              Yönlendir
            </Button>
          </>
        )
      }
    >
      <p className="whitespace-pre-wrap rounded-control bg-sunken px-3 py-2.5 text-sm text-text-2">
        {b.aciklama}
      </p>

      <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-text-2">
        {b.adres && (
          <span className="inline-flex items-center gap-1.5">
            <MapPin size={13} className="text-text-3" />
            {b.adres}
          </span>
        )}
        {b.enlem != null && (
          <span className="font-mono text-2xs tabular-nums text-ink-3">
            {b.enlem.toFixed(5)}, {b.boylam?.toFixed(5)}
          </span>
        )}
        <span className="text-ink-3">{dateTime(b.olusturmaTarihi)}</span>
      </div>

      {(ekler ?? []).length > 0 && (
        <div>
          <p className="mb-2 text-xs font-medium text-ink-2">
            Vatandaşın fotoğrafları ({ekler!.length})
          </p>

          {/*
            FOTOĞRAFIN KENDİSİ.

            Burada uzun süre yalnızca dosya ADLARI listeleniyordu ve kodda
            gerekçesi de yazılıydı: "fotoğraflar özel alanda, önizleme için
            kimlik denetimli bir indirme akışı gerekiyor". O akış artık var
            (`korumaliMedya`) ve şikayeti değerlendirmenin en hızlı yolu resme
            bakmak: hangi birime gideceği, aciliyeti ve mükerrer olup olmadığı
            çoğu zaman tek karede görülüyor.
          */}
          <PhotoGrid fotograflar={bildirimFotograflari(b.id!, ekler!)} />
        </div>
      )}

      {islenmis ? (
        <p className="rounded-control border border-line px-3 py-2.5 text-xs text-text-2">
          <span className="font-medium text-ink-2">{b.durumAd}</span>
          {b.birimAd && ` · ${b.birimAd}`}
          {b.isleyen && ` · ${b.isleyen}`}
          {b.islemNotu && (
            <>
              <br />
              {b.islemNotu}
            </>
          )}
        </p>
      ) : yetkili && !retKipi ? (
        <>
          <FieldWrapper etiket="Hangi birime?" id="yonlendir-birim" zorunlu>
            <Secim
              id="yonlendir-birim"
              value={birimId ?? ''}
              onChange={(e) => setBirimId(e.target.value ? Number(e.target.value) : null)}
            >
              <option value="">Seçin</option>
              {birimler.liste.map((x) => (
                <option key={x.id} value={x.id!}>
                  {x.ad}
                </option>
              ))}
            </Secim>
          </FieldWrapper>

          <FieldWrapper
            etiket="Görev tipi"
            id="yonlendir-tip"
            ipucu="Seçilirse görev aşamalarını ve süre hedefini tipten devralır."
          >
            <Secim
              id="yonlendir-tip"
              value={tipId ?? ''}
              onChange={(e) => setTipId(e.target.value ? Number(e.target.value) : null)}
            >
              <option value="">Tipsiz</option>
              {tipler.liste.map((t) => (
                <option key={t.id} value={t.id!}>
                  {t.ad}
                </option>
              ))}
            </Secim>
          </FieldWrapper>

          <FieldWrapper etiket="Öncelik" id="yonlendir-oncelik">
            <Secim
              id="yonlendir-oncelik"
              value={oncelik}
              onChange={(e) => setOncelik(Number(e.target.value))}
            >
              {Object.entries(TASK_PRIORITY_LABELS).map(([d, e]) => (
                <option key={d} value={d}>
                  {e}
                </option>
              ))}
            </Secim>
          </FieldWrapper>

          <FieldWrapper etiket="Karşılama notu" id="yonlendir-not">
            <Textarea
              id="yonlendir-not"
              rows={2}
              value={not}
              onChange={(e) => setNot(e.target.value)}
              placeholder="Birime iletilecek ek bilgi"
            />
          </FieldWrapper>
        </>
      ) : yetkili ? (
        <FieldWrapper
          etiket="İşleme almama gerekçesi"
          id="ret-not"
          zorunlu
          ipucu="Vatandaşa ayrıntı gitmiyor; bu not kurum içi kayıt."
        >
          <Textarea
            id="ret-not"
            rows={3}
            value={not}
            onChange={(e) => setNot(e.target.value)}
            placeholder="Mükerrer kayıt, kurumun görev alanı dışında…"
          />
        </FieldWrapper>
      ) : null}
    </FormModal>
  );
}
