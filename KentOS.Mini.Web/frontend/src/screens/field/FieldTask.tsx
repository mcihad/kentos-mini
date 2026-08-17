import {
  Camera, Check, CheckCircle2, ClipboardCheck, Loader2, MapPin, Navigation, Phone, SkipForward,
} from 'lucide-react';
import { useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { ColoredBadge } from '../../components/Color';
import { EmptyState } from '../../components/EmptyState';
import { Textarea } from '../../components/Field';
import { Skeleton } from '../../components/Skeleton';
import { useToast } from '../../components/Toast';
import { taskKeys, uploadTaskFile, useTask, useTaskMutations } from '../../data/tasks';
import { TASK_STAGE_STATUS, TASK_STATUS, type TaskStage } from '../../data/types';
import { SlaBadge } from '../task/TaskBits';

/**
 * SAHA GÖREV EKRANI — aşama aşama tamamlama.
 *
 * <p>
 * Masaüstü görev detayının küçültülmüş hâli DEĞİL: burada yalnızca sahada
 * gereken şey var — sıradaki aşama, fotoğraf düğmesi ve yol tarifi.
 * Atamalar, yorumlar, zaman çizelgesi ve alt görevler yok; hepsi ofiste
 * bakılan şeyler ve tek elle kullanılan bir ekranı kalabalıklaştırıyorlar.
 * </p>
 *
 * <p>
 * <b>Sıradaki aşama en üstte ve açık.</b> Öteki aşamalar altında sıkışık
 * duruyor: personelin şu an yapacağı tek bir iş var.
 * </p>
 */
export default function FieldTask() {
  const { id } = useParams();
  const gorevId = Number(id);
  const { data: gorev, isLoading } = useTask(gorevId);
  const m = useTaskMutations(gorevId);
  const { bildir } = useToast();

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (!gorev) {
    return (
      <EmptyState
        ikon={ClipboardCheck}
        baslik="Görev bulunamadı"
        eylem={
          <Link to="/saha">
            <Button varyant="ikincil">Saha ekranına dön</Button>
          </Link>
        }
      />
    );
  }

  const asamalar = gorev.asamalar ?? [];
  const sirada = asamalar.find((a) => a.sirada);
  const eksikZorunlu = asamalar.filter(
    (a) => a.zorunlu && a.durum === TASK_STAGE_STATUS.bekliyor,
  );

  const baslatilabilir = (gorev.sonrakiDurumlar ?? []).some(
    (d) => d.durum === TASK_STATUS.basladi,
  );
  const tamamlanabilir =
    (gorev.sonrakiDurumlar ?? []).some((d) => d.durum === TASK_STATUS.onayBekliyor) &&
    eksikZorunlu.length === 0;

  return (
    <div className="space-y-3.5">
      {/* ── Künye ── */}
      <Card className="p-4">
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-mono text-2xs tabular-nums text-ink-3">{gorev.takipNo}</span>
          <ColoredBadge etiket={gorev.durumAd} renk={gorev.durumRenk} />
          <SlaBadge gecikti={!!gorev.gecikti} kalanSaat={gorev.kalanSaat} />
        </div>

        <h1 className="mt-1.5 font-display text-lg font-bold leading-tight text-ink">
          {gorev.baslik}
        </h1>

        {gorev.aciklama && (
          <p className="mt-2 whitespace-pre-wrap text-sm text-text-2">{gorev.aciklama}</p>
        )}

        {gorev.adres && (
          <p className="mt-2 inline-flex items-center gap-1.5 text-xs text-text-2">
            <MapPin size={13} className="text-text-3" />
            {gorev.adres}
          </p>
        )}

        {/*
          YOL TARİFİ cihazın kendi harita uygulamasına devrediliyor.

          Uygulama içine rota çizmek, sürüş sırasında kullanılacak bir şeyi
          yeniden yazmak olurdu; personelin telefonunda zaten sesli
          yönlendirme yapan bir uygulama var.
        */}
        {gorev.enlem != null && gorev.boylam != null && (
          <a
            href={`geo:${gorev.enlem},${gorev.boylam}?q=${gorev.enlem},${gorev.boylam}`}
            className="mt-3 block"
          >
            <Button varyant="ikincil" className="h-12 w-full text-base">
              <Navigation size={17} />
              Yol tarifi
            </Button>
          </a>
        )}

        {/* Vatandaş bildiriminden doğduysa telefon açıklamada; sahadaki kişi
            yeri sormak için arayabilmeli. */}
        {gorev.kaynakAd === 'Vatandaş bildirimi' && (
          <p className="mt-2 inline-flex items-center gap-1.5 text-2xs text-ink-3">
            <Phone size={12} />
            Bildirenin iletişimi açıklamada
          </p>
        )}
      </Card>

      {/* ── Başlat ── */}
      {baslatilabilir && (
        <Button
          className="h-12 w-full text-base"
          disabled={m.durum.isPending}
          onClick={async () => {
            try {
              await m.durum.mutateAsync({ durum: TASK_STATUS.basladi as never });
              bildir('basari', 'Görev başlatıldı');
            } catch (h) {
              bildir('hata', 'Başlatılamadı', (h as Error).message);
            }
          }}
        >
          {m.durum.isPending ? <Loader2 size={17} className="animate-spin" /> : null}
          İşe başla
        </Button>
      )}

      {/* ── Sıradaki aşama ── */}
      {sirada && <SiradakiAsama gorevId={gorevId} asama={sirada} />}

      {/* ── Öteki aşamalar ── */}
      {asamalar.length > 0 && (
        <Card>
          <ol className="divide-y divide-line">
            {asamalar.map((a) => (
              <li key={a.id} className="flex items-center gap-2.5 px-3.5 py-2.5">
                <span
                  className={`grid h-6 w-6 flex-none place-items-center rounded-full text-2xs font-medium ${
                    a.durum === TASK_STAGE_STATUS.tamamlandi
                      ? 'bg-(--st-ok-bg) text-(--st-ok)'
                      : a.durum === TASK_STAGE_STATUS.atlandi
                        ? 'bg-sunken text-ink-3'
                        : a.sirada
                          ? 'bg-brand-ui text-white'
                          : 'bg-sunken text-ink-3'
                  }`}
                  aria-hidden
                >
                  {a.durum === TASK_STAGE_STATUS.tamamlandi ? <Check size={13} /> : a.siraNo}
                </span>
                <span
                  className={`min-w-0 flex-1 truncate text-sm ${
                    a.durum === TASK_STAGE_STATUS.bekliyor ? 'text-ink' : 'text-text-3'
                  }`}
                >
                  {a.ad}
                </span>
                {a.durum !== TASK_STAGE_STATUS.bekliyor && (
                  <span className="shrink-0 text-2xs text-ink-3">{a.durumAd}</span>
                )}
              </li>
            ))}
          </ol>
        </Card>
      )}

      {/* ── Tamamla ── */}
      {(gorev.sonrakiDurumlar ?? []).some((d) => d.durum === TASK_STATUS.onayBekliyor) && (
        <>
          <Button
            className="h-12 w-full text-base"
            disabled={!tamamlanabilir || m.tamamlanmayaGonder.isPending}
            onClick={async () => {
              try {
                await m.tamamlanmayaGonder.mutateAsync();
                bildir('basari', 'Görev onaya gönderildi');
              } catch (h) {
                bildir('hata', 'Gönderilemedi', (h as Error).message);
              }
            }}
          >
            <CheckCircle2 size={17} />
            İşi bitirdim
          </Button>

          {/* Düğme neden kapalı, AÇIKÇA yazılıyor: sahadaki kişi kapalı bir
              düğmeye bakıp uygulamanın bozulduğunu düşünmemeli. */}
          {!tamamlanabilir && (
            <p className="-mt-2 text-center text-2xs text-(--st-wait)">
              Önce şu aşamalar bitmeli: {eksikZorunlu.map((a) => a.ad).join(', ')}
            </p>
          )}
        </>
      )}

      {gorev.durum === TASK_STATUS.onayBekliyor && (
        <p className="rounded-control bg-sunken px-3 py-2.5 text-center text-xs text-text-2">
          İş bitirildi olarak bildirildi; yöneticinin onayı bekleniyor.
        </p>
      )}
    </div>
  );
}

/**
 * Sıradaki aşamanın işlem kartı.
 *
 * <p>
 * Tek elle kullanılıyor: not alanı büyük, fotoğraf düğmesi tam genişlik ve
 * tamamla düğmesi 48 piksel yüksekliğinde.
 * </p>
 */
function SiradakiAsama({ gorevId, asama }: { gorevId: number; asama: TaskStage }) {
  const { bildir } = useToast();
  const qc = useQueryClient();
  const m = useTaskMutations(gorevId);

  const [not, setNot] = useState('');
  const [yukleniyor, setYukleniyor] = useState(false);
  const dosyaAlani = useRef<HTMLInputElement>(null);

  const ekSayisi = asama.ekSayisi ?? 0;
  const fotografEksik = !!asama.fotografZorunlu && ekSayisi === 0;
  const aciklamaEksik = !!asama.aciklamaZorunlu && not.trim().length === 0;

  return (
    <Card className="border-brand-ui p-4">
      <p className="text-2xs uppercase tracking-wide text-ink-3">Sıradaki aşama</p>
      <h2 className="mt-0.5 font-display text-base font-bold text-ink">{asama.ad}</h2>

      <div className="mt-1.5 flex flex-wrap gap-2 text-2xs">
        {asama.fotografZorunlu && (
          <span className={ekSayisi > 0 ? 'text-(--st-ok)' : 'text-(--st-wait)'}>
            fotoğraf {ekSayisi > 0 ? `eklendi (${ekSayisi})` : 'zorunlu'}
          </span>
        )}
        {asama.aciklamaZorunlu && <span className="text-(--st-wait)">açıklama zorunlu</span>}
        {!asama.zorunlu && <span className="text-ink-3">isteğe bağlı</span>}
      </div>

      <Textarea
        value={not}
        onChange={(e) => setNot(e.target.value)}
        rows={3}
        placeholder={asama.aciklamaZorunlu ? 'Ne yapıldı? (zorunlu)' : 'Not (isteğe bağlı)'}
        aria-label={`${asama.ad} notu`}
        className="mt-3 text-base"
      />

      <input
        ref={dosyaAlani}
        type="file"
        accept="image/*"
        capture="environment"
        className="hidden"
        onChange={async (e) => {
          const d = e.target.files?.[0];
          if (!d) return;

          setYukleniyor(true);
          try {
            await uploadTaskFile(gorevId, d, asama.id!);
            qc.invalidateQueries({ queryKey: taskKeys.all() });
            bildir('basari', 'Fotoğraf yüklendi');
          } catch (h) {
            bildir('hata', 'Fotoğraf yüklenemedi', (h as Error).message);
          } finally {
            setYukleniyor(false);
            if (dosyaAlani.current) dosyaAlani.current.value = '';
          }
        }}
      />

      <Button
        varyant="ikincil"
        className="mt-2.5 h-12 w-full text-base"
        disabled={yukleniyor}
        onClick={() => dosyaAlani.current?.click()}
      >
        {yukleniyor ? <Loader2 size={17} className="animate-spin" /> : <Camera size={17} />}
        Fotoğraf çek
      </Button>

      <div className="mt-2 flex gap-2">
        <Button
          className="h-12 flex-1 text-base"
          disabled={m.asama.isPending || fotografEksik || aciklamaEksik}
          onClick={async () => {
            try {
              await m.asama.mutateAsync({
                asamaId: asama.id!,
                govde: { not: not.trim() || null, atla: false },
              });
              setNot('');
              bildir('basari', 'Aşama tamamlandı');
            } catch (h) {
              bildir('hata', 'Tamamlanamadı', (h as Error).message);
            }
          }}
        >
          <Check size={17} />
          Tamamla
        </Button>

        {!asama.zorunlu && (
          <Button
            varyant="sade"
            className="h-12 px-4"
            disabled={m.asama.isPending}
            onClick={async () => {
              try {
                await m.asama.mutateAsync({
                  asamaId: asama.id!,
                  govde: { not: not.trim() || null, atla: true },
                });
                setNot('');
                bildir('basari', 'Aşama atlandı');
              } catch (h) {
                bildir('hata', 'Atlanamadı', (h as Error).message);
              }
            }}
          >
            <SkipForward size={17} />
            Atla
          </Button>
        )}
      </div>

      {(fotografEksik || aciklamaEksik) && (
        <p className="mt-2 text-center text-2xs text-(--st-wait)">
          {fotografEksik ? 'Önce fotoğraf çekin.' : 'Önce ne yapıldığını yazın.'}
        </p>
      )}
    </Card>
  );
}
