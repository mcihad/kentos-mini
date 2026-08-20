import { ArrowRightLeft, Check, Inbox as InboxIcon, MapPin, X } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { ColoredBadge } from '../components/Color';
import { EmptyState } from '../components/EmptyState';
import { FieldWrapper, Secim, Textarea } from '../components/Field';
import { ChipStrip, FilterChip, SegmentedSelect } from '../components/Filters';
import { FormModal } from '../components/FormModal';
import { Pagination } from '../components/Pagination';
import { SkeletonRows } from '../components/Skeleton';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { dateTime } from '../data/format';
import { useInbox, useInboxMutations } from '../data/citizen';
import { useUsableTaskTypes } from '../data/tasks';
import {
  INBOX_STATUS, INBOX_STATUS_LABELS, TASK_PRIORITY_LABELS, type InboxItem,
} from '../data/types';
import { UnitScopePicker } from '../components/UnitScopePicker';

type Kapsam = 'kendi' | 'alt';

/**
 * BİRİM GELEN KUTUSU — başka birimlerden gelen iş.
 *
 * <p>
 * Kayıtlar bir görev tamamlandığında tipteki devir kuralıyla kendiliğinden
 * düşüyor; buradan yalnızca <b>karar</b> veriliyor. Kabul birimde görev
 * açar, ret kaynak birime gerekçeli bildirim gönderir.
 * </p>
 *
 * <p>
 * <b>İş talebi ile bilgilendirme ayrı.</b> Bilgilendirme karar istemiyor,
 * okundu işaretlenip kapanıyor; ayırmasaydık her bilgilendirme için de karar
 * vermek gerekir ve kutu hızla kullanılamaz hâle gelirdi.
 * </p>
 */
export default function Inbox() {
  const [durum, setDurum] = useState<number | null>(INBOX_STATUS.bekliyor);
  const [kapsam, setKapsam] = useState<Kapsam>('kendi');
  const [sayfa, setSayfa] = useState(1);
  const [acilan, setAcilan] = useState<InboxItem | null>(null);

  const { data, isLoading } = useInbox({
    sayfa,
    boyut: 25,
    durum,
    altBirimlerDahil: kapsam === 'alt',
  });

  const kayitlar = data?.veriler ?? [];

  return (
    <div className="space-y-3.5">
      <div className="flex flex-wrap items-center gap-2">
        <UnitScopePicker />

        <SegmentedSelect<Kapsam>
          deger={kapsam}
          degistir={(d) => {
            setKapsam(d);
            setSayfa(1);
          }}
          etiket="Kapsam"
          secenekler={[
            { deger: 'kendi', etiket: 'Birimim' },
            { deger: 'alt', etiket: 'Alt birimler' },
          ]}
          className="md:ml-auto"
        />
      </div>

      <ChipStrip>
        {[INBOX_STATUS.bekliyor, INBOX_STATUS.kabul, INBOX_STATUS.ret, INBOX_STATUS.okundu].map(
          (d) => (
            <FilterChip
              key={d}
              secili={durum === d}
              tikla={() => {
                setDurum(d);
                setSayfa(1);
              }}
            >
              {INBOX_STATUS_LABELS[d]}
            </FilterChip>
          ),
        )}
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
        <SkeletonRows adet={4} />
      ) : kayitlar.length === 0 ? (
        <EmptyState
          ikon={InboxIcon}
          baslik="Bekleyen kayıt yok"
          aciklama="Başka birimlerden gelen bir iş talebi ya da bilgilendirme bulunmuyor."
        />
      ) : (
        <>
          <div className="space-y-2.5">
            {kayitlar.map((k) => (
              <Card key={k.id} className="p-3.5">
                <div className="flex items-start gap-2.5">
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <ColoredBadge etiket={k.durumAd} renk={k.durumRenk} />
                      <span className="text-2xs text-ink-3">
                        {k.isTalebi ? 'İş talebi' : 'Bilgilendirme'}
                      </span>
                      {k.kaynakBirimAd && (
                        <span className="inline-flex items-center gap-1 text-2xs text-ink-3">
                          <ArrowRightLeft size={11} />
                          {k.kaynakBirimAd}
                        </span>
                      )}
                    </div>

                    <h3 className="mt-1 font-display text-sm font-semibold text-ink">{k.konu}</h3>
                    {k.aciklama && (
                      <p className="mt-1 line-clamp-2 whitespace-pre-wrap text-xs text-text-2">
                        {k.aciklama}
                      </p>
                    )}

                    <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-2xs text-ink-3">
                      {k.kaynakTakipNo && (
                        <Link
                          to={`/gorevler/${k.kaynakGorevId}`}
                          className="font-mono tabular-nums underline"
                        >
                          {k.kaynakTakipNo}
                        </Link>
                      )}
                      {k.adres && (
                        <span className="inline-flex items-center gap-1 truncate">
                          <MapPin size={11} />
                          {k.adres}
                        </span>
                      )}
                      <span>{dateTime(k.olusturmaTarihi)}</span>
                    </div>

                    {k.gorevTakipNo && (
                      <p className="mt-2 text-2xs text-(--st-ok)">
                        <Link to={`/gorevler/${k.gorevId}`} className="underline">
                          {k.gorevTakipNo}
                        </Link>{' '}
                        açıldı
                      </p>
                    )}
                    {k.gerekce && (
                      <p className="mt-2 text-2xs text-text-3">Gerekçe: {k.gerekce}</p>
                    )}
                  </div>

                  {k.durum === INBOX_STATUS.bekliyor && (
                    <Button varyant="ikincil" className="shrink-0" onClick={() => setAcilan(k)}>
                      Karar ver
                    </Button>
                  )}
                </div>
              </Card>
            ))}
          </div>

          <Pagination sonuc={data} sayfaDegistir={setSayfa} birim="kayıt" className="mt-3" />
        </>
      )}

      {acilan && <KararKutusu kayit={acilan} kapat={() => setAcilan(null)} />}
    </div>
  );
}

/** Kabul / ret / okundu kararı. */
function KararKutusu({ kayit, kapat }: { kayit: InboxItem; kapat: () => void }) {
  const { bildir } = useToast();
  const { hasPermission } = useSession();
  const m = useInboxMutations(kayit.id!);
  const tipler = useUsableTaskTypes();

  const [tipId, setTipId] = useState<number | null>(kayit.hedefGorevTipiId ?? null);
  const [oncelik, setOncelik] = useState(1);
  const [gerekce, setGerekce] = useState('');
  const [retKipi, setRetKipi] = useState(false);

  const yetkili = hasPermission(PERMISSION.gelenKutusuKarar);

  async function calistir(is: () => Promise<unknown>, mesaj: string) {
    try {
      await is();
      bildir('basari', mesaj);
      kapat();
    } catch (h) {
      bildir('hata', 'İşlem tamamlanamadı', (h as Error).message);
    }
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={kayit.konu ?? 'Gelen kayıt'}
      aciklama={`${kayit.kaynakBirimAd ?? 'Bilinmeyen birim'} → ${kayit.hedefBirimAd ?? ''}`}
      eylemler={
        !yetkili ? (
          <Button varyant="ikincil" onClick={kapat}>
            Kapat
          </Button>
        ) : !kayit.isTalebi ? (
          <>
            <Button varyant="ikincil" onClick={kapat}>
              Vazgeç
            </Button>
            <Button
              disabled={m.okundu.isPending}
              onClick={() => calistir(() => m.okundu.mutateAsync(), 'Okundu olarak işaretlendi')}
            >
              <Check size={15} />
              Okudum
            </Button>
          </>
        ) : retKipi ? (
          <>
            <Button varyant="ikincil" onClick={() => setRetKipi(false)}>
              Vazgeç
            </Button>
            <Button
              varyant="yikici"
              disabled={gerekce.trim().length === 0 || m.reddet.isPending}
              onClick={() =>
                calistir(() => m.reddet.mutateAsync(gerekce.trim()), 'Devir reddedildi')
              }
            >
              <X size={15} />
              Reddet
            </Button>
          </>
        ) : (
          <>
            <Button varyant="ikincil" onClick={() => setRetKipi(true)}>
              Reddet
            </Button>
            <Button
              disabled={m.kabul.isPending}
              onClick={() =>
                calistir(
                  () => m.kabul.mutateAsync({ gorevTipiId: tipId, oncelik: oncelik as never }),
                  'Kabul edildi ve görev açıldı',
                )
              }
            >
              <Check size={15} />
              Kabul et
            </Button>
          </>
        )
      }
    >
      {kayit.aciklama && (
        <p className="whitespace-pre-wrap rounded-control bg-sunken px-3 py-2.5 text-sm text-text-2">
          {kayit.aciklama}
        </p>
      )}

      {!kayit.isTalebi ? (
        <p className="text-xs text-text-2">
          Bu bir <b>bilgilendirme</b> kaydı; görev açılmaz. Okuduğunuzu
          işaretleyerek kapatabilirsiniz.
        </p>
      ) : retKipi ? (
        <FieldWrapper
          etiket="Ret gerekçesi"
          id="devir-gerekce"
          zorunlu
          ipucu="Kaynak birime aynen iletilir."
        >
          <Textarea
            id="devir-gerekce"
            rows={3}
            value={gerekce}
            onChange={(e) => setGerekce(e.target.value)}
            placeholder="Bu iş bizim görev alanımızda değil…"
          />
        </FieldWrapper>
      ) : (
        <>
          <FieldWrapper
            etiket="Görev tipi"
            id="devir-tip"
            ipucu="Seçilirse görev aşamalarını ve süre hedefini tipten devralır."
          >
            <Secim
              id="devir-tip"
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

          <FieldWrapper etiket="Öncelik" id="devir-oncelik">
            <Secim
              id="devir-oncelik"
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
        </>
      )}
    </FormModal>
  );
}
