import { ArrowDown, ArrowUp, ClipboardList, Pencil, Plus, Trash2, X } from 'lucide-react';
import { useState } from 'react';
import { Button, IconButton } from '../../components/Button';
import { Card } from '../../components/Card';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { FormModal } from '../../components/FormModal';
import { FieldWrapper, Input, Textarea } from '../../components/Field';
import { SkeletonRows } from '../../components/Skeleton';
import { Switch } from '../../components/Switch';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { useTaskTypeMutations, useTaskTypes } from '../../data/tasks';
import type { TaskType, TaskTypeSave, TaskTypeStage } from '../../data/types';

/**
 * GÖREV TİPLERİ — hizmet standardının tanımlandığı yer.
 *
 * <p>
 * Tip bir etiket değil bir <b>sözleşme</b>: kaç aşamadan geçileceğini, her
 * aşamada ne kanıt isteneceğini ve işin kaç saatte bitmesi gerektiğini o
 * söylüyor. Görev açılırken bunların hepsi <b>kopyalanıyor</b> — bu yüzden
 * tanımı sonradan değiştirmek açılmış görevleri etkilemiyor.
 * </p>
 */
export default function TaskTypes() {
  const { hasPermission } = useSession();
  const { bildir } = useToast();
  const m = useTaskTypeMutations();

  const [duzenlenen, setDuzenlenen] = useState<TaskType | 'yeni' | null>(null);
  const [silinecek, setSilinecek] = useState<TaskType | null>(null);

  const { data, isLoading } = useTaskTypes({ boyut: 200 });
  const tipler = data?.veriler ?? [];
  const yetkili = hasPermission(PERMISSION.gorevTipYonet);

  return (
    <div className="space-y-3.5">
      <div className="flex items-center justify-between gap-2">
        <p className="text-xs text-text-2">
          Tanım değişikliği <b>yeni</b> açılan görevleri etkiler; açılmış
          görevler aşamalarını kopya olarak taşır.
        </p>
        {yetkili && (
          <Button onClick={() => setDuzenlenen('yeni')} className="shrink-0">
            <Plus size={14} />
            Tip ekle
          </Button>
        )}
      </div>

      {isLoading ? (
        <SkeletonRows adet={4} />
      ) : tipler.length === 0 ? (
        <EmptyState
          ikon={ClipboardList}
          baslik="Görev tipi yok"
          aciklama="Tip tanımlanmadan da görev açılabilir ama aşama ve süre hedefi olmaz."
        />
      ) : (
        <div className="grid gap-2.5 md:grid-cols-2">
          {tipler.map((t) => (
            <Card key={t.id} className="p-3.5">
              <div className="flex items-start gap-2">
                <span
                  className="mt-1 h-2.5 w-2.5 flex-none rounded-full"
                  style={{ background: t.renk ?? 'var(--brand-ui)' }}
                  aria-hidden
                />
                <div className="min-w-0 flex-1">
                  <h3 className="truncate font-display text-sm font-semibold text-ink">
                    {t.ad}
                    {!t.kullanimda && (
                      <span className="ml-2 text-2xs font-normal text-ink-3">kullanım dışı</span>
                    )}
                  </h3>
                  <p className="mt-0.5 text-2xs text-ink-3">
                    {(t.asamalar ?? []).length} aşama
                    {t.hizmetStandardiGun ? ` · ${t.hizmetStandardiGun} gün standart` : ''}
                    {t.slaSaat ? ` · ${t.slaSaat} sa SLA` : ''}
                    {t.konumZorunlu ? ' · konum zorunlu' : ''}
                  </p>
                </div>

                {yetkili && (
                  <>
                    <IconButton etiket="Düzenle" onClick={() => setDuzenlenen(t)}>
                      <Pencil size={16} />
                    </IconButton>
                    <IconButton etiket="Sil" onClick={() => setSilinecek(t)}>
                      <Trash2 size={16} />
                    </IconButton>
                  </>
                )}
              </div>

              {(t.asamalar ?? []).length > 0 && (
                <ol className="mt-2.5 space-y-1">
                  {t.asamalar!.map((a) => (
                    <li key={a.id} className="flex items-baseline gap-1.5 text-2xs text-text-2">
                      <span className="tabular-nums text-ink-3">{a.siraNo}.</span>
                      <span className="truncate">{a.ad}</span>
                      {!a.zorunlu && <span className="text-ink-3">(isteğe bağlı)</span>}
                      {a.fotografZorunlu && <span className="text-(--st-wait)">fotoğraf</span>}
                      {a.aciklamaZorunlu && <span className="text-(--st-wait)">açıklama</span>}
                    </li>
                  ))}
                </ol>
              )}

              {(t.gorevSayisi ?? 0) > 0 && (
                <p className="mt-2 text-3xs text-ink-3">
                  {t.gorevSayisi} görev bu tiple açılmış
                </p>
              )}
            </Card>
          ))}
        </div>
      )}

      {duzenlenen && (
        <TipFormu
          tip={duzenlenen === 'yeni' ? null : duzenlenen}
          kapat={() => setDuzenlenen(null)}
        />
      )}

      <ConfirmDialog
        acik={!!silinecek}
        kapat={() => setSilinecek(null)}
        baslik={`"${silinecek?.ad}" silinsin mi?`}
        aciklama={
          'Bu tiple görev açılmışsa silinemez — o görevlerin hangi hizmet standardına ' +
          'göre ölçüldüğü kaybolurdu. Yeni görevlerde seçilmesini istemiyorsanız ' +
          'KULLANIMDAN KALDIRIN.'
        }
        onayEtiketi="Sil"
        yikici
        onayla={async () => {
          try {
            await m.sil.mutateAsync(silinecek!.id!);
            bildir('basari', 'Görev tipi silindi');
            setSilinecek(null);
          } catch (h) {
            bildir('hata', 'Görev tipi silinemedi', (h as Error).message);
          }
        }}
      />
    </div>
  );
}

/** Boş bir aşama satırı. */
const BOS_ASAMA: TaskTypeStage = {
  ad: '',
  zorunlu: true,
  aciklamaZorunlu: false,
  fotografZorunlu: false,
};

/**
 * Görev tipi formu.
 *
 * <p>
 * Aşama listesi <b>tam liste</b>: gövdede olmayan aşama silinir. Sıra numarası
 * gönderilmiyor bile — sunucu listedeki sıraya göre 1'den yeniden
 * numaralandırıyor, böylece taşıma sonrası boşluklu ya da çakışan numaralar
 * oluşamıyor.
 * </p>
 */
function TipFormu({ tip, kapat }: { tip: TaskType | null; kapat: () => void }) {
  const { bildir } = useToast();
  const m = useTaskTypeMutations();

  const [form, setForm] = useState<TaskTypeSave>({
    ad: tip?.ad ?? '',
    aciklama: tip?.aciklama ?? null,
    renk: tip?.renk ?? null,
    hizmetStandardiGun: tip?.hizmetStandardiGun ?? null,
    slaSaat: tip?.slaSaat ?? null,
    varsayilanOncelik: tip?.varsayilanOncelik ?? (1 as never),
    konumZorunlu: tip?.konumZorunlu ?? false,
    kullanimda: tip?.kullanimda ?? true,
    asamalar: tip?.asamalar ?? [],
    birimIdler: tip?.birimIdler ?? [],
    devirler: tip?.devirler ?? [],
  });

  const asamalar = form.asamalar ?? [];
  const gecerli =
    form.ad.trim().length > 0 && asamalar.every((a) => a.ad.trim().length > 0);

  function asamaYaz(i: number, alan: Partial<TaskTypeStage>) {
    const yeni = [...asamalar];
    yeni[i] = { ...yeni[i], ...alan };
    setForm({ ...form, asamalar: yeni });
  }

  function tasi(i: number, yon: -1 | 1) {
    const hedef = i + yon;
    if (hedef < 0 || hedef >= asamalar.length) return;
    const yeni = [...asamalar];
    [yeni[i], yeni[hedef]] = [yeni[hedef], yeni[i]];
    setForm({ ...form, asamalar: yeni });
  }

  async function kaydet() {
    try {
      if (tip) await m.guncelle.mutateAsync({ id: tip.id!, govde: form });
      else await m.olustur.mutateAsync(form);
      bildir('basari', tip ? 'Görev tipi güncellendi' : 'Görev tipi eklendi');
      kapat();
    } catch (h) {
      bildir('hata', 'Görev tipi kaydedilemedi', (h as Error).message);
    }
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={tip ? 'Görev tipini düzenle' : 'Görev tipi ekle'}
      genislik="genis"
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            disabled={!gecerli || m.olustur.isPending || m.guncelle.isPending}
            onClick={kaydet}
          >
            Kaydet
          </Button>
        </>
      }
    >
      <FieldWrapper etiket="Tip adı" id="tip-ad" zorunlu>
        <Input
          id="tip-ad"
          value={form.ad}
          onChange={(e) => setForm({ ...form, ad: e.target.value })}
          placeholder="Yol Onarımı"
          maxLength={200}
        />
      </FieldWrapper>

      <FieldWrapper etiket="Açıklama" id="tip-aciklama">
        <Textarea
          id="tip-aciklama"
          rows={2}
          value={form.aciklama ?? ''}
          onChange={(e) => setForm({ ...form, aciklama: e.target.value || null })}
        />
      </FieldWrapper>

      <div className="grid grid-cols-2 gap-3">
        <FieldWrapper
          etiket="Hizmet standardı (gün)"
          id="tip-standart"
          ipucu="Vatandaşa taahhüt edilen süre."
        >
          <Input
            id="tip-standart"
            type="number"
            min={0}
            value={form.hizmetStandardiGun ?? ''}
            onChange={(e) =>
              setForm({
                ...form,
                hizmetStandardiGun: e.target.value ? Number(e.target.value) : null,
              })
            }
          />
        </FieldWrapper>

        <FieldWrapper
          etiket="SLA (saat)"
          id="tip-sla"
          ipucu="İç hedef. Sayaç görev BAŞLADIĞINDA işlemeye başlar."
        >
          <Input
            id="tip-sla"
            type="number"
            min={0}
            value={form.slaSaat ?? ''}
            onChange={(e) =>
              setForm({ ...form, slaSaat: e.target.value ? Number(e.target.value) : null })
            }
          />
        </FieldWrapper>
      </div>

      <FieldWrapper etiket="Renk" id="tip-renk">
        <Input
          id="tip-renk"
          type="color"
          value={form.renk ?? '#1E5FBF'}
          onChange={(e) => setForm({ ...form, renk: e.target.value })}
          className="h-10 w-24 p-1"
        />
      </FieldWrapper>

      <Switch
        isaretli={!!form.konumZorunlu}
        degistir={(a) => setForm({ ...form, konumZorunlu: a })}
        etiket="Konum zorunlu"
        aciklama="Bu tipte görev açarken koordinat girilmeden kaydedilemez."
      />

      <Switch
        isaretli={form.kullanimda ?? true}
        degistir={(a) => setForm({ ...form, kullanimda: a })}
        etiket="Kullanımda"
        aciklama="Kapatılan tip yeni görevlerde seçilemez; açılmış görevler etkilenmez."
      />

      {/* ── Aşamalar ── */}
      <div>
        <div className="mb-2 flex items-center justify-between">
          <span className="text-xs font-medium text-ink-2">Aşamalar</span>
          <Button
            varyant="sade"
            onClick={() => setForm({ ...form, asamalar: [...asamalar, { ...BOS_ASAMA }] })}
          >
            <Plus size={14} />
            Aşama
          </Button>
        </div>

        {asamalar.length === 0 ? (
          <p className="rounded-control border border-dashed border-line px-3 py-4 text-center text-2xs text-ink-3">
            Aşamasız tip de olur — görev tek adımda tamamlanır.
          </p>
        ) : (
          <ol className="space-y-2">
            {asamalar.map((a, i) => (
              <li key={i} className="rounded-control border border-line p-2.5">
                <div className="flex items-center gap-2">
                  <span className="w-5 shrink-0 text-center text-2xs tabular-nums text-ink-3">
                    {i + 1}
                  </span>
                  <Input
                    value={a.ad}
                    onChange={(e) => asamaYaz(i, { ad: e.target.value })}
                    placeholder="Aşama adı"
                    aria-label={`${i + 1}. aşama adı`}
                    className="min-w-0 flex-1"
                    maxLength={200}
                  />
                  <IconButton
                    etiket="Yukarı taşı"
                    onClick={() => tasi(i, -1)}
                    disabled={i === 0}
                  >
                    <ArrowUp size={15} />
                  </IconButton>
                  <IconButton
                    etiket="Aşağı taşı"
                    onClick={() => tasi(i, 1)}
                    disabled={i === asamalar.length - 1}
                  >
                    <ArrowDown size={15} />
                  </IconButton>
                  <IconButton
                    etiket="Aşamayı kaldır"
                    onClick={() =>
                      setForm({ ...form, asamalar: asamalar.filter((_, x) => x !== i) })
                    }
                  >
                    <X size={15} />
                  </IconButton>
                </div>

                <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1.5 pl-7">
                  <Kutu
                    etiket="Zorunlu"
                    isaretli={!!a.zorunlu}
                    degistir={(v) => asamaYaz(i, { zorunlu: v })}
                  />
                  <Kutu
                    etiket="Açıklama zorunlu"
                    isaretli={!!a.aciklamaZorunlu}
                    degistir={(v) => asamaYaz(i, { aciklamaZorunlu: v })}
                  />
                  <Kutu
                    etiket="Fotoğraf zorunlu"
                    isaretli={!!a.fotografZorunlu}
                    degistir={(v) => asamaYaz(i, { fotografZorunlu: v })}
                  />
                </div>
              </li>
            ))}
          </ol>
        )}
      </div>
    </FormModal>
  );
}

function Kutu({
  etiket,
  isaretli,
  degistir,
}: {
  etiket: string;
  isaretli: boolean;
  degistir: (v: boolean) => void;
}) {
  return (
    <label className="inline-flex min-h-11 cursor-pointer items-center gap-1.5 text-2xs text-text-2">
      <input
        type="checkbox"
        checked={isaretli}
        onChange={(e) => degistir(e.target.checked)}
        className="h-4 w-4 accent-[var(--brand-ui)]"
      />
      {etiket}
    </label>
  );
}
