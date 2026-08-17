import { ArrowDown, ArrowUp, Plus, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, IconButton } from '../../components/Button';
import { FieldWrapper, Input, Secim, Textarea } from '../../components/Field';
import { FormModal } from '../../components/FormModal';
import { useToast } from '../../components/Toast';
import { useProject, useProjectMutations } from '../../data/projects';
import {
  PROJECT_STATUS_LABELS, TASK_STATUS_LABELS, type Milestone, type ProjectSave,
} from '../../data/types';

/** Yeni bir kilometre taşı satırı. */
const BOS_TAS: Milestone = { ad: '', tamamlandi: false };

/**
 * PROJE FORMU.
 *
 * <p>
 * <b>Kilometre taşları kimlikleriyle gönderiliyor.</b> Görevler
 * <code>kilometre_tasi_id</code> ile taşlara bağlı; kimlik düşürülseydi
 * sunucu onları yeni kayıt sayar ve bağlı görevlerin hepsi sahipsiz kalırdı —
 * projeyi düzenlemek, işlerin hangi hedefe ait olduğunu silmek olurdu.
 * </p>
 *
 * <p>
 * Pano sütunları burada YOK. Varsayılan pano proje açılırken kuruluyor ve
 * sütun düzenlemek ayrı bir iş; forma koymak, projeyi ilk kez açan kişiyi
 * hiç ihtiyacı olmayan bir kararla karşılaştırırdı.
 * </p>
 */
export default function ProjectForm() {
  const { id } = useParams();
  const projeId = id ? Number(id) : undefined;
  const gezin = useNavigate();
  const { bildir } = useToast();

  const { data: mevcut } = useProject(projeId);
  const m = useProjectMutations(projeId);

  const [form, setForm] = useState<ProjectSave>({
    ad: '',
    kod: null,
    aciklama: null,
    renk: null,
    durum: 0 as never,
    yoneticiId: null,
    baslangic: null,
    bitis: null,
    butce: null,
    adres: null,
    enlem: null,
    boylam: null,
    uyeler: [],
    kilometreTaslari: [],
    panoSutunlari: [],
  });

  useEffect(() => {
    if (!mevcut) return;
    setForm({
      ad: mevcut.ad ?? '',
      kod: mevcut.kod ?? null,
      aciklama: mevcut.aciklama ?? null,
      renk: mevcut.renk ?? null,
      durum: mevcut.durum,
      yoneticiId: mevcut.yoneticiId ?? null,
      baslangic: mevcut.baslangic ?? null,
      bitis: mevcut.bitis ?? null,
      butce: mevcut.butce ?? null,
      adres: mevcut.adres ?? null,
      enlem: mevcut.enlem ?? null,
      boylam: mevcut.boylam ?? null,

      // Üyeler ve pano bu formdan YAZILMIYOR ama tam kaydetme ucu onları da
      // yazıyor: mevcut hâlleri geri gönderilmezse silinirlerdi.
      uyeler: (mevcut.uyeler ?? []).map((u) => ({ kullaniciId: u.kullaniciId!, rol: u.rol })),
      panoSutunlari: mevcut.panoSutunlari ?? [],
      kilometreTaslari: mevcut.kilometreTaslari ?? [],
    });
  }, [mevcut]);

  const duzenleme = !!projeId;
  const taslar = form.kilometreTaslari ?? [];

  const tarihHatasi =
    form.baslangic && form.bitis && form.bitis < form.baslangic
      ? 'Bitiş tarihi başlangıçtan önce olamaz.'
      : undefined;

  const gecerli =
    form.ad.trim().length > 0 &&
    !tarihHatasi &&
    taslar.every((t) => t.ad.trim().length > 0);

  function tasYaz(i: number, alan: Partial<Milestone>) {
    const yeni = [...taslar];
    yeni[i] = { ...yeni[i], ...alan };
    setForm({ ...form, kilometreTaslari: yeni });
  }

  function tasi(i: number, yon: -1 | 1) {
    const hedef = i + yon;
    if (hedef < 0 || hedef >= taslar.length) return;
    const yeni = [...taslar];
    [yeni[i], yeni[hedef]] = [yeni[hedef], yeni[i]];
    setForm({ ...form, kilometreTaslari: yeni });
  }

  function kapat() {
    gezin(duzenleme ? `/projeler/${projeId}` : '/projeler');
  }

  async function kaydet() {
    try {
      if (duzenleme) {
        await m.guncelle.mutateAsync({ id: projeId!, govde: form });
        bildir('basari', 'Proje güncellendi');
        gezin(`/projeler/${projeId}`);
      } else {
        const yeni = await m.olustur.mutateAsync(form);
        bildir('basari', 'Proje açıldı');
        gezin(`/projeler/${yeni.id}`);
      }
    } catch (h) {
      bildir('hata', 'Proje kaydedilemedi', (h as Error).message);
    }
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={duzenleme ? 'Projeyi düzenle' : 'Proje aç'}
      aciklama={duzenleme ? undefined : 'Kanban panosu varsayılan sütunlarla kurulur.'}
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
      <FieldWrapper etiket="Proje adı" id="proje-ad" zorunlu>
        <Input
          id="proje-ad"
          value={form.ad}
          onChange={(e) => setForm({ ...form, ad: e.target.value })}
          placeholder="Kent Meydanı Düzenlemesi"
          maxLength={300}
        />
      </FieldWrapper>

      <div className="grid grid-cols-2 gap-3">
        <FieldWrapper etiket="Kod" id="proje-kod" ipucu="Yazışmada kullanılır.">
          <Input
            id="proje-kod"
            value={form.kod ?? ''}
            onChange={(e) => setForm({ ...form, kod: e.target.value || null })}
            placeholder="KMD-2026"
            maxLength={40}
          />
        </FieldWrapper>

        <FieldWrapper etiket="Durum" id="proje-durum">
          <Secim
            id="proje-durum"
            value={form.durum ?? 0}
            onChange={(e) => setForm({ ...form, durum: Number(e.target.value) as never })}
          >
            {Object.entries(PROJECT_STATUS_LABELS).map(([d, e]) => (
              <option key={d} value={d}>
                {e}
              </option>
            ))}
          </Secim>
        </FieldWrapper>
      </div>

      <FieldWrapper etiket="Açıklama" id="proje-aciklama">
        <Textarea
          id="proje-aciklama"
          rows={3}
          value={form.aciklama ?? ''}
          onChange={(e) => setForm({ ...form, aciklama: e.target.value || null })}
        />
      </FieldWrapper>

      <div className="grid grid-cols-2 gap-3">
        <FieldWrapper etiket="Başlangıç" id="proje-bas">
          <Input
            id="proje-bas"
            type="date"
            value={(form.baslangic ?? '').slice(0, 10)}
            onChange={(e) => setForm({ ...form, baslangic: e.target.value || null })}
          />
        </FieldWrapper>
        <FieldWrapper etiket="Bitiş" id="proje-bit" hata={tarihHatasi}>
          <Input
            id="proje-bit"
            type="date"
            value={(form.bitis ?? '').slice(0, 10)}
            onChange={(e) => setForm({ ...form, bitis: e.target.value || null })}
            hatali={!!tarihHatasi}
          />
        </FieldWrapper>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <FieldWrapper etiket="Bütçe (₺)" id="proje-butce">
          <Input
            id="proje-butce"
            type="number"
            min={0}
            step="0.01"
            value={form.butce ?? ''}
            onChange={(e) =>
              setForm({ ...form, butce: e.target.value ? Number(e.target.value) : null })
            }
          />
        </FieldWrapper>
        <FieldWrapper etiket="Renk" id="proje-renk">
          <Input
            id="proje-renk"
            type="color"
            value={form.renk ?? '#002E6D'}
            onChange={(e) => setForm({ ...form, renk: e.target.value })}
            className="h-10 w-24 p-1"
          />
        </FieldWrapper>
      </div>

      <FieldWrapper etiket="Adres" id="proje-adres">
        <Input
          id="proje-adres"
          value={form.adres ?? ''}
          onChange={(e) => setForm({ ...form, adres: e.target.value || null })}
          maxLength={500}
        />
      </FieldWrapper>

      {/* ── Kilometre taşları ── */}
      <div>
        <div className="mb-2 flex items-center justify-between">
          <span className="text-xs font-medium text-ink-2">Kilometre taşları</span>
          <Button
            varyant="sade"
            onClick={() =>
              setForm({ ...form, kilometreTaslari: [...taslar, { ...BOS_TAS }] })
            }
          >
            <Plus size={14} />
            Ekle
          </Button>
        </div>

        {taslar.length === 0 ? (
          <p className="rounded-control border border-dashed border-line px-3 py-4 text-center text-2xs text-ink-3">
            Ara hedef tanımlamak zorunlu değil; gantt çizelgesinde yalnızca
            tarihi olanlar görünür.
          </p>
        ) : (
          <ol className="space-y-2">
            {taslar.map((t, i) => (
              <li key={t.id ?? `yeni-${i}`} className="rounded-control border border-line p-2.5">
                <div className="flex items-center gap-2">
                  <span className="w-5 shrink-0 text-center text-2xs tabular-nums text-ink-3">
                    {i + 1}
                  </span>
                  <Input
                    value={t.ad}
                    onChange={(e) => tasYaz(i, { ad: e.target.value })}
                    placeholder="Kilometre taşı adı"
                    aria-label={`${i + 1}. kilometre taşı adı`}
                    className="min-w-0 flex-1"
                    maxLength={300}
                  />
                  <Input
                    type="date"
                    value={(t.hedefTarih ?? '').slice(0, 10)}
                    onChange={(e) => tasYaz(i, { hedefTarih: e.target.value || null })}
                    aria-label={`${i + 1}. kilometre taşı hedef tarihi`}
                    className="w-36 shrink-0"
                  />
                  <IconButton etiket="Yukarı taşı" onClick={() => tasi(i, -1)} disabled={i === 0}>
                    <ArrowUp size={15} />
                  </IconButton>
                  <IconButton
                    etiket="Aşağı taşı"
                    onClick={() => tasi(i, 1)}
                    disabled={i === taslar.length - 1}
                  >
                    <ArrowDown size={15} />
                  </IconButton>
                  <IconButton
                    etiket="Kilometre taşını kaldır"
                    onClick={() =>
                      setForm({
                        ...form,
                        kilometreTaslari: taslar.filter((_, x) => x !== i),
                      })
                    }
                  >
                    <X size={15} />
                  </IconButton>
                </div>

                {/* Kaldırma UYARISI: bağlı görevler silinmiyor ama bağları
                    boşalıyor ve bu geri alınamaz bir düzenleme. */}
                {(t.gorevToplam ?? 0) > 0 && (
                  <p className="mt-1.5 pl-7 text-3xs text-ink-3">
                    {t.gorevToplam} görev bu hedefe bağlı — kaldırırsanız bağları boşalır.
                  </p>
                )}
              </li>
            ))}
          </ol>
        )}
      </div>

      {/* Pano sütunları düzenlemesi burada değil; hangi durumların panoda
          göründüğü ayrı bir karar ve varsayılanı çoğu kurum için yeterli. */}
      {duzenleme && (form.panoSutunlari ?? []).length > 0 && (
        <p className="text-2xs text-ink-3">
          Pano sütunları:{' '}
          {(form.panoSutunlari ?? [])
            .map((s) => `${s.ad} (${TASK_STATUS_LABELS[s.gorevDurumu ?? 0]})`)
            .join(' · ')}
        </p>
      )}
    </FormModal>
  );
}
