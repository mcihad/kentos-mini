import { useMutation, useQueryClient } from '@tanstack/react-query';
import { FileUser, Paperclip, Plus } from 'lucide-react';
import { useState } from 'react';
import { FieldWrapper, Textarea, Input } from '../../components/Field';
import { SearchSelect } from '../../components/SearchSelect';
import { Button } from '../../components/Button';
import { FormSection } from '../../components/FormSection';
import { FormModal } from '../../components/FormModal';
import { SkeletonRows } from '../../components/Skeleton';
import { useToast } from '../../components/Toast';
import { fileSize as fileSizeText } from '../../data/format';
import { tokenStore } from '../../data/client';
import { useNeighborhoodSearch, useOccupationSearch, useResume } from '../../data/hooks';
import type { ResumeDetail, ResumeSummary } from '../../data/types';

/**
 * Ekleme / düzenleme formu.
 *
 * <p>
 * Dosya ve alanlar TEK istekte gider (<c>multipart/form-data</c>): önce kaydı
 * açıp sonra dosya yüklemek, ikinci adım başarısız olduğunda havuzda dosyasız
 * kayıtlar bırakıyordu.
 * </p>
 *
 * <p>
 * <b>Düzenlerken önce AYRINTI çekilir.</b> Form listedeki özetten
 * dolduruluyordu ve <c>adres</c> yalnızca ayrıntı yanıtında var: kayıt her
 * düzenlendiğinde adres boş gidiyor, sunucu tam güncelleme yaptığı için de
 * <b>sessizce siliniyordu</b>. Alanları ayrıntı gelene kadar hiç çizmemek,
 * yarısı dolu bir formdan daha güvenli.
 * </p>
 */
export function ResumeForm({
  kayit,
  kapat,
}: {
  kayit: ResumeSummary | null;
  kapat: () => void;
}) {
  const detay = useResume(kayit?.id ?? 0);

  if (kayit && !detay.data) {
    return (
      <FormModal
        acik
        kapat={kapat}
        baslik="Özgeçmişi düzenle"
        ikon={<FileUser size={15} />}
        eylemler={
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
        }
      >
        <SkeletonRows adet={5} />
      </FormModal>
    );
  }

  return <Alanlar mevcut={kayit ? detay.data! : null} kapat={kapat} />;
}

function Alanlar({ mevcut, kapat }: { mevcut: ResumeDetail | null; kapat: () => void }) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [adSoyad, setAdSoyad] = useState(mevcut?.adSoyad ?? '');
  const [telefon, setTelefon] = useState(mevcut?.telefon ?? '');
  const [eposta, setEposta] = useState(mevcut?.eposta ?? '');
  const [meslekId, setMeslekId] = useState<number | null>(mevcut?.meslekId ?? null);
  const [meslekAdi, setMeslekAdi] = useState<string | null>(mevcut?.meslekAd ?? null);
  const [mahalleId, setMahalleId] = useState<number | null>(mevcut?.mahalleId ?? null);
  const [mahalleAdi, setMahalleAdi] = useState<string | null>(mevcut?.mahalleAd ?? null);
  const [adres, setAdres] = useState(mevcut?.adres ?? '');
  const [aciklama, setAciklama] = useState(mevcut?.aciklama ?? '');
  const [dosya, setDosya] = useState<File | null>(null);

  const [meslekArama, setMeslekArama] = useState('');
  const [mahalleArama, setMahalleArama] = useState('');
  const meslekler = useOccupationSearch(meslekArama);
  const mahalleler = useNeighborhoodSearch(mahalleArama);

  const gecerli = adSoyad.trim().length > 1 && (mevcut !== null || dosya !== null);

  const kaydet = useMutation({
    mutationFn: async () => {
      const govde = new FormData();
      govde.append('adSoyad', adSoyad.trim());
      govde.append('telefon', telefon);
      govde.append('eposta', eposta);
      if (meslekId) govde.append('meslekId', String(meslekId));
      if (!meslekId && meslekAdi) govde.append('meslekAd', meslekAdi);
      if (mahalleId) govde.append('mahalleId', String(mahalleId));
      govde.append('adres', adres);
      govde.append('aciklama', aciklama);
      if (dosya) govde.append('dosya', dosya);

      const jeton = tokenStore.read();
      const yanit = await fetch(`/api/v2/ozgecmis${mevcut ? `/${mevcut.id}` : ''}`, {
        method: mevcut ? 'PUT' : 'POST',
        headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : {},
        body: govde,
      });

      if (!yanit.ok) {
        const hata = await yanit.json().catch(() => null);
        throw new Error(hata?.detail ?? `Kaydedilemedi (${yanit.status}).`);
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['ozgecmis'] });
      bildir('basari', mevcut ? 'Özgeçmiş güncellendi' : 'Özgeçmiş eklendi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={mevcut ? 'Özgeçmişi düzenle' : 'Özgeçmiş ekle'}
      aciklama="Meslek ve açıklama, havuzda aramanın çalıştığı iki alan."
      ikon={<FileUser size={15} />}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            onClick={() => gecerli && kaydet.mutate()}
            disabled={!gecerli || kaydet.isPending}
          >
            {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </Button>
        </>
      }
    >
      <FormSection baslik="Kişi">
        <div className="grid gap-3.5 sm:grid-cols-2">
          <FieldWrapper etiket="Ad soyad" id="oz-ad" zorunlu className="sm:col-span-2">
            <Input id="oz-ad" value={adSoyad} onChange={(e) => setAdSoyad(e.target.value)} autoFocus />
          </FieldWrapper>

          <FieldWrapper etiket="Telefon" id="oz-tel" ipucu="05551112233 biçiminde">
            <Input
              id="oz-tel"
              type="tel"
              inputMode="tel"
              value={telefon}
              onChange={(e) => setTelefon(e.target.value)}
            />
          </FieldWrapper>

          <FieldWrapper etiket="E-posta" id="oz-eposta">
            <Input
              id="oz-eposta"
              type="email"
              value={eposta}
              onChange={(e) => setEposta(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </FormSection>

      <FormSection baslik="Nitelik ve adres">
        <div className="grid gap-3.5 sm:grid-cols-2">
          <FieldWrapper etiket="Meslek" id="oz-form-meslek">
            <SearchSelect
              id="oz-form-meslek"
              deger={meslekId}
              seciliAd={meslekAdi}
              degistir={(id, ad) => {
                setMeslekId(id);
                setMeslekAdi(ad);
              }}
              ogeler={meslekler.liste.map((m) => ({ id: m.id!, ad: m.ad! }))}
              ara={meslekArama}
              araDegistir={setMeslekArama}
              yukleniyor={meslekler.isFetching}
              yerTutucu="Meslek seçin"
            />
          </FieldWrapper>

          <FieldWrapper etiket="Mahalle" id="oz-form-mahalle">
            <SearchSelect
              id="oz-form-mahalle"
              deger={mahalleId}
              seciliAd={mahalleAdi}
              degistir={(id, ad) => {
                setMahalleId(id);
                setMahalleAdi(ad);
              }}
              ogeler={mahalleler.liste.map((m) => ({ id: m.id!, ad: m.ad! }))}
              ara={mahalleArama}
              araDegistir={setMahalleArama}
              yukleniyor={mahalleler.isFetching}
              yerTutucu="Mahalle seçin"
            />
          </FieldWrapper>

          <FieldWrapper etiket="Adres" id="oz-adres" className="sm:col-span-2">
            <Input id="oz-adres" value={adres} onChange={(e) => setAdres(e.target.value)} />
          </FieldWrapper>

          <FieldWrapper
            etiket="Açıklama"
            id="oz-aciklama"
            ipucu="Deneyim, referans, ehliyet… havuzda ARANAN metin burası"
            className="sm:col-span-2"
          >
            <Textarea
              id="oz-aciklama"
              value={aciklama}
              onChange={(e) => setAciklama(e.target.value)}
              rows={3}
              placeholder="8 yıl kaynakçılık, E sınıfı ehliyet, vardiyalı çalışabilir."
            />
          </FieldWrapper>
        </div>
      </FormSection>

      <FormSection baslik={mevcut ? 'Dosya' : 'Özgeçmiş dosyası'}>
        {mevcut?.dosyaAdi && (
          <p className="mb-2 flex items-center gap-1.5 text-2xs text-text-3">
            <Paperclip size={12} className="shrink-0" />
            <span className="min-w-0 truncate">
              Şu an: {mevcut.dosyaAdi} ({fileSizeText(mevcut.boyut ?? 0)})
            </span>
          </p>
        )}
        <label
          className="flex cursor-pointer items-center justify-center gap-2 rounded-control border border-dashed border-border
            bg-surface-2 px-4 py-4 text-sm text-text-2 transition-colors hover:border-brand-2 hover:text-text"
        >
          <Plus size={15} className="text-text-3" />
          <span className="min-w-0 truncate">
            {dosya
              ? `${dosya.name} (${fileSizeText(dosya.size)})`
              : mevcut
                ? 'Yeni dosya seç (isteğe bağlı)'
                : 'Dosya seç — PDF, DOC veya görsel'}
          </span>
          <input
            id="oz-dosya"
            type="file"
            className="hidden"
            accept=".pdf,.doc,.docx,.jpg,.jpeg,.png"
            onChange={(e) => setDosya(e.target.files?.[0] ?? null)}
          />
        </label>
      </FormSection>
    </FormModal>
  );
}
