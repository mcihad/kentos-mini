import { Camera, Check, CircleDashed, SkipForward } from 'lucide-react';
import { useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Button } from '../../components/Button';
import { Card, CardHeader } from '../../components/Card';
import { PhotoGrid } from '../../components/PhotoGrid';
import { Textarea } from '../../components/Field';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { dateTime } from '../../data/format';
import { taskKeys, uploadTaskFile, useTaskMutations } from '../../data/tasks';
import { TASK_STAGE_STATUS, type TaskDetail, type TaskStage } from '../../data/types';

/**
 * AŞAMALAR — işin kanıt zinciri.
 *
 * <p>
 * Aşamalar tipten <b>kopyalanmış</b> durumda; buradaki liste bir tanım değil,
 * yapılmış işin kaydı. Yalnızca <b>sıradaki</b> aşama işlem kabul ediyor:
 * üçüncü adımı ikinciden önce işaretlemek kanıtı gerçek sıradan koparırdı.
 * </p>
 *
 * <p>
 * Zorunlu alanlar (açıklama, fotoğraf) burada da denetleniyor ama <b>asıl kapı
 * sunucuda</b>. Buradaki denetim yalnızca kullanıcıyı boş yere sunucuya
 * göndermemek için: reddedileceğini bildiğimiz bir isteği atmak, kullanıcıya
 * gereksiz bir hata mesajı okutmak demek.
 * </p>
 */
export function TaskStages({ gorev }: { gorev: TaskDetail }) {
  const asamalar = gorev.asamalar ?? [];
  if (asamalar.length === 0) return null;

  const biten = asamalar.filter((a) => a.durum !== TASK_STAGE_STATUS.bekliyor).length;

  return (
    <Card serit>
      <CardHeader
        baslik="Aşamalar"
        aciklama={`${asamalar.length} aşamanın ${biten} tanesi kapandı`}
      />
      <ol className="divide-y divide-line">
        {asamalar.map((a) => (
          <AsamaSatiri key={a.id} gorevId={gorev.id!} asama={a} />
        ))}
      </ol>
    </Card>
  );
}

function AsamaSatiri({ gorevId, asama }: { gorevId: number; asama: TaskStage }) {
  const { bildir } = useToast();
  const qc = useQueryClient();
  const { hasPermission } = useSession();
  const m = useTaskMutations(gorevId);

  const [not, setNot] = useState('');
  const [yukleniyor, setYukleniyor] = useState(false);
  const dosyaAlani = useRef<HTMLInputElement>(null);

  const bekliyor = asama.durum === TASK_STAGE_STATUS.bekliyor;
  const acik = !!asama.sirada && hasPermission(PERMISSION.gorevAsama);

  const ekler = asama.ekler ?? [];
  const fotograflar = ekler
    .filter((e) => e.resimMi)
    .map((e) => ({
      yol: `/api/v2/gorev/ek/${e.id}`,
      baslik: e.ad,
      altBilgi: e.yukleyen ? `${e.yukleyen} · ${dateTime(e.tarih)}` : dateTime(e.tarih),
    }));
  const belgeler = ekler.filter((e) => !e.resimMi);

  const ekSayisi = asama.ekSayisi ?? 0;
  const fotografEksik = !!asama.fotografZorunlu && ekSayisi === 0;
  const aciklamaEksik = !!asama.aciklamaZorunlu && not.trim().length === 0;

  async function fotografSec(dosya: File) {
    setYukleniyor(true);
    try {
      await uploadTaskFile(gorevId, dosya, asama.id!);
      // Ek sayısı görevin detayında taşınıyor; yükleme sonrası tazelenmezse
      // "fotoğraf zorunlu" uyarısı yüklenmiş fotoğrafa rağmen kalırdı.
      qc.invalidateQueries({ queryKey: taskKeys.all() });
      bildir('basari', 'Fotoğraf yüklendi');
    } catch (h) {
      bildir('hata', 'Fotoğraf yüklenemedi', (h as Error).message);
    } finally {
      setYukleniyor(false);
      if (dosyaAlani.current) dosyaAlani.current.value = '';
    }
  }

  async function kapat(atla: boolean) {
    try {
      await m.asama.mutateAsync({
        asamaId: asama.id!,
        govde: { not: not.trim() || null, atla },
      });
      setNot('');
      bildir('basari', atla ? 'Aşama atlandı' : 'Aşama tamamlandı');
    } catch (h) {
      bildir('hata', 'Aşama kapatılamadı', (h as Error).message);
    }
  }

  return (
    <li className="px-3.5 py-3">
      <div className="flex items-start gap-2.5">
        <span
          className={`mt-0.5 grid h-6 w-6 flex-none place-items-center rounded-full text-2xs font-medium ${
            asama.durum === TASK_STAGE_STATUS.tamamlandi
              ? 'bg-(--st-ok-bg) text-(--st-ok)'
              : asama.durum === TASK_STAGE_STATUS.atlandi
                ? 'bg-sunken text-ink-3'
                : asama.sirada
                  ? 'bg-brand text-white'
                  : 'bg-sunken text-ink-3'
          }`}
          aria-hidden
        >
          {asama.durum === TASK_STAGE_STATUS.tamamlandi ? (
            <Check size={13} strokeWidth={2.6} />
          ) : asama.durum === TASK_STAGE_STATUS.atlandi ? (
            <SkipForward size={12} />
          ) : (
            asama.siraNo
          )}
        </span>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
            <span className={`text-sm ${bekliyor ? 'text-ink' : 'text-text-2'}`}>{asama.ad}</span>
            {!asama.zorunlu && <span className="text-3xs text-ink-3">isteğe bağlı</span>}
            {bekliyor && asama.fotografZorunlu && (
              <span className="text-3xs text-(--st-wait)">fotoğraf zorunlu</span>
            )}
            {bekliyor && asama.aciklamaZorunlu && (
              <span className="text-3xs text-(--st-wait)">açıklama zorunlu</span>
            )}
            {!bekliyor && (
              <span className="text-3xs text-ink-3">
                {asama.durumAd}
                {asama.tamamlayan && ` · ${asama.tamamlayan}`}
                {asama.tamamlanmaTarihi && ` · ${dateTime(asama.tamamlanmaTarihi)}`}
              </span>
            )}
          </div>

          {asama.not && (
            <p className="mt-1 whitespace-pre-wrap text-xs text-text-2">{asama.not}</p>
          )}

          {/*
            AŞAMANIN FOTOĞRAFLARI BURADA.

            Önceden yalnızca "2 dosya" yazan gri bir satır vardı: fotoğrafın
            ZORUNLU tutulduğu bir modülde, çekilen kanıtı görmenin hiçbir
            yolu yoktu — indirip işletim sisteminin görüntüleyicisinde açmak
            gerekiyordu. Kanıt, kanıtın adı değil kendisidir.
          */}
          {fotograflar.length > 0 && (
            <PhotoGrid
              fotograflar={fotograflar}
              boyut="kucuk"
              className="mt-2"
            />
          )}

          {belgeler.length > 0 && (
            <p className="mt-1 inline-flex items-center gap-1 text-3xs text-ink-3">
              <Camera size={11} />
              {belgeler.length} belge
            </p>
          )}

          {/* ── Sıradaki aşamanın işlem alanı ── */}
          {acik && (
            <div className="mt-2.5 space-y-2">
              <Textarea
                value={not}
                onChange={(e) => setNot(e.target.value)}
                rows={2}
                placeholder={asama.aciklamaZorunlu ? 'Ne yapıldı? (zorunlu)' : 'Not (isteğe bağlı)'}
                aria-label={`${asama.ad} notu`}
              />

              <div className="flex flex-wrap items-center gap-2">
                <input
                  ref={dosyaAlani}
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={(e) => {
                    const d = e.target.files?.[0];
                    if (d) fotografSec(d);
                  }}
                />
                <Button
                  varyant="ikincil"
                  onClick={() => dosyaAlani.current?.click()}
                  disabled={yukleniyor}
                >
                  <Camera size={15} />
                  Fotoğraf
                </Button>

                <Button
                  onClick={() => kapat(false)}
                  disabled={m.asama.isPending || fotografEksik || aciklamaEksik}
                  title={
                    fotografEksik
                      ? 'Bu aşamada fotoğraf zorunlu'
                      : aciklamaEksik
                        ? 'Bu aşamada açıklama zorunlu'
                        : undefined
                  }
                >
                  <Check size={15} />
                  {/* Görev düğmesi "Onaya gönder"; bu YALNIZCA bu adımı
                      kapatıyor ve adı bunu söylemeli. */}
                  Aşamayı bitir
                </Button>

                {/* Atlama YALNIZCA zorunlu olmayan aşamada. Zorunluda düğmeyi
                    göstermek, çalışmayan bir düğme sunmak olurdu. */}
                {!asama.zorunlu && (
                  <Button varyant="sade" onClick={() => kapat(true)} disabled={m.asama.isPending}>
                    <SkipForward size={15} />
                    Atla
                  </Button>
                )}
              </div>
            </div>
          )}

          {bekliyor && !asama.sirada && (
            <p className="mt-1 inline-flex items-center gap-1 text-3xs text-ink-3">
              <CircleDashed size={11} />
              Önceki aşamalar tamamlanınca açılır
            </p>
          )}
        </div>
      </div>
    </li>
  );
}
