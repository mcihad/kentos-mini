import { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Button } from '../../components/Button';
import { FormModal } from '../../components/FormModal';
import { FieldWrapper, Input, Secim, Textarea } from '../../components/Field';
import { useToast } from '../../components/Toast';
import { useUsableTaskTypes, useTask, useTaskMutations } from '../../data/tasks';
import { TASK_PRIORITY_LABELS, type TaskSave } from '../../data/types';

/**
 * GÖREV FORMU — açma ve düzenleme.
 *
 * <p>
 * Modal olarak açılıyor ve arkasına liste çiziliyor (rota tablosunda): kapatan
 * kullanıcı boş bir ekranla değil, bıraktığı listeyle karşılaşmalı.
 * </p>
 *
 * <p>
 * <b>Tip yalnızca AÇILIŞTA seçilebiliyor.</b> Aşamalar tipten kopyalanıyor ve
 * bir kısmı tamamlanmış olabilir; tipi sonradan değiştirmek ya yapılmış işin
 * kanıtını silmek ya da yeni tipin aşamalarını hiç uygulamamak olurdu. Sunucu
 * da bunu reddediyor.
 * </p>
 */
export default function TaskForm() {
  const { id } = useParams();
  const [sorgu] = useSearchParams();
  const gezin = useNavigate();
  const { bildir } = useToast();

  const gorevId = id ? Number(id) : undefined;
  const ustGorevId = sorgu.get('ust') ? Number(sorgu.get('ust')) : undefined;

  const { data: mevcut } = useTask(gorevId);
  const tipler = useUsableTaskTypes();
  const m = useTaskMutations(gorevId);

  const [form, setForm] = useState<TaskSave>({
    baslik: '',
    aciklama: null,
    gorevTipiId: null,
    oncelik: 1 as never,
    ustGorevId: ustGorevId ?? null,
    adres: null,
    enlem: null,
    boylam: null,
    planlananBitis: null,
    atamalar: [],
  });

  // Düzenlemede alanlar sunucudan gelen kayıtla doldurulur. Bağımlılık
  // `mevcut` — form her tuş vuruşunda sıfırlanmasın diye state'e bakılmıyor.
  useEffect(() => {
    if (!mevcut) return;
    setForm({
      baslik: mevcut.baslik ?? '',
      aciklama: mevcut.aciklama ?? null,
      gorevTipiId: mevcut.gorevTipiId ?? null,
      oncelik: mevcut.oncelik,
      ustGorevId: mevcut.ustGorevId ?? null,
      adres: mevcut.adres ?? null,
      enlem: mevcut.enlem ?? null,
      boylam: mevcut.boylam ?? null,
      planlananBitis: mevcut.planlananBitis ?? null,
      atamalar: [],
    });
  }, [mevcut]);

  const duzenleme = !!gorevId;
  const secilenTip = tipler.liste.find((t) => t.id === form.gorevTipiId);
  const konumZorunlu = !!secilenTip?.konumZorunlu;
  const konumEksik = konumZorunlu && (form.enlem == null || form.boylam == null);

  const gecerli = form.baslik.trim().length > 0 && !konumEksik;

  function kapat() {
    gezin(duzenleme ? `/gorevler/${gorevId}` : '/gorevler');
  }

  async function kaydet() {
    try {
      if (duzenleme) {
        await m.guncelle.mutateAsync(form);
        bildir('basari', 'Görev güncellendi');
        gezin(`/gorevler/${gorevId}`);
      } else {
        const yeni = await m.olustur.mutateAsync(form);
        bildir('basari', `Görev açıldı — ${yeni.takipNo}`);
        gezin(`/gorevler/${yeni.id}`);
      }
    } catch (h) {
      bildir('hata', duzenleme ? 'Görev güncellenemedi' : 'Görev açılamadı', (h as Error).message);
    }
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={duzenleme ? 'Görevi düzenle' : ustGorevId ? 'Alt görev aç' : 'Görev aç'}
      aciklama={
        duzenleme
          ? undefined
          : 'Görev, tipinden aşamalarını ve süre hedefini devralır.'
      }
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            disabled={!gecerli || m.olustur.isPending || m.guncelle.isPending}
            onClick={kaydet}
          >
            {duzenleme ? 'Kaydet' : 'Aç'}
          </Button>
        </>
      }
    >
      <FieldWrapper etiket="Başlık" id="gorev-baslik" zorunlu>
        <Input
          id="gorev-baslik"
          value={form.baslik}
          onChange={(e) => setForm({ ...form, baslik: e.target.value })}
          placeholder="Kısa ve tanımlayıcı"
          maxLength={300}
        />
      </FieldWrapper>

      <FieldWrapper
        etiket="Görev tipi"
        id="gorev-tip"
        ipucu={
          duzenleme
            ? 'Tip sonradan değiştirilemez — aşamalar ondan kopyalandı.'
            : secilenTip
              ? sureIpucu(secilenTip.hizmetStandardiGun, secilenTip.slaSaat)
              : 'Tip seçilmezse görev aşamasız açılır.'
        }
      >
        <Secim
          id="gorev-tip"
          value={form.gorevTipiId ?? ''}
          disabled={duzenleme && !!mevcut?.gorevTipiId}
          onChange={(e) =>
            setForm({ ...form, gorevTipiId: e.target.value ? Number(e.target.value) : null })
          }
        >
          <option value="">Tipsiz</option>
          {tipler.liste.map((t) => (
            <option key={t.id} value={t.id!}>
              {t.ad}
            </option>
          ))}
        </Secim>
      </FieldWrapper>

      <FieldWrapper etiket="Öncelik" id="gorev-oncelik">
        <Secim
          id="gorev-oncelik"
          value={form.oncelik ?? 1}
          onChange={(e) => setForm({ ...form, oncelik: Number(e.target.value) as never })}
        >
          {Object.entries(TASK_PRIORITY_LABELS).map(([d, e]) => (
            <option key={d} value={d}>
              {e}
            </option>
          ))}
        </Secim>
      </FieldWrapper>

      <FieldWrapper etiket="Açıklama" id="gorev-aciklama">
        <Textarea
          id="gorev-aciklama"
          rows={3}
          value={form.aciklama ?? ''}
          onChange={(e) => setForm({ ...form, aciklama: e.target.value || null })}
          placeholder="İşin ayrıntısı, ölçüsü, dikkat edilecekler"
        />
      </FieldWrapper>

      <FieldWrapper etiket="Adres" id="gorev-adres">
        <Input
          id="gorev-adres"
          value={form.adres ?? ''}
          onChange={(e) => setForm({ ...form, adres: e.target.value || null })}
          placeholder="Mahalle, cadde, tarif"
          maxLength={500}
        />
      </FieldWrapper>

      {/*
        KOORDİNAT şimdilik elle giriliyor.

        Harita seçici (MapLibre) planın 3. fazında; o gelene kadar alan boş
        bırakılabiliyor. Tipi "konum zorunlu" olan görevlerde ise sunucu
        koordinatsız kaydı reddediyor, bu yüzden alan burada da zorunlu
        işaretleniyor — kullanıcının reddedilecek bir formu doldurup
        göndermesi gereksiz.
      */}
      <div className="grid grid-cols-2 gap-3">
        <FieldWrapper etiket="Enlem" id="gorev-enlem" zorunlu={konumZorunlu}>
          <Input
            id="gorev-enlem"
            type="number"
            step="0.000001"
            value={form.enlem ?? ''}
            onChange={(e) =>
              setForm({ ...form, enlem: e.target.value ? Number(e.target.value) : null })
            }
            placeholder="39.747700"
          />
        </FieldWrapper>
        <FieldWrapper etiket="Boylam" id="gorev-boylam" zorunlu={konumZorunlu}>
          <Input
            id="gorev-boylam"
            type="number"
            step="0.000001"
            value={form.boylam ?? ''}
            onChange={(e) =>
              setForm({ ...form, boylam: e.target.value ? Number(e.target.value) : null })
            }
            placeholder="37.017900"
          />
        </FieldWrapper>
      </div>

      <FieldWrapper
        etiket="Planlanan bitiş"
        id="gorev-bitis"
        ipucu="Boş bırakılırsa tipin hizmet standardından hesaplanır."
      >
        <Input
          id="gorev-bitis"
          type="date"
          value={(form.planlananBitis ?? '').slice(0, 10)}
          onChange={(e) =>
            setForm({ ...form, planlananBitis: e.target.value || null })
          }
        />
      </FieldWrapper>
    </FormModal>
  );
}

function sureIpucu(gun?: number | null, saat?: number | null): string | undefined {
  const parcalar: string[] = [];
  if (gun) parcalar.push(`hizmet standardı ${gun} gün`);
  if (saat) parcalar.push(`SLA ${saat} saat`);
  return parcalar.length ? parcalar.join(' · ') : undefined;
}
