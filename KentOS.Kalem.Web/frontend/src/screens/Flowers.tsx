import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Flower2, Pencil, Plus, Power, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { Switch } from '../components/Switch';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { FieldWrapper, Input } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { DataList, type Column } from '../components/DataList';
import { FormModal } from '../components/FormModal';
import { Button, IconButton } from '../components/Button';
import { SkeletonRows } from '../components/Skeleton';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { useToast } from '../components/Toast';
import { api } from '../data/client';
import type { Florist } from '../data/types';

/**
 * Çiçek Gönderi — çiçekçi kayıtlarının yönetimi.
 *
 * Etkinliğe çiçek talimatı bağlamak buradan YAPILMAZ; o, etkinlik detayının
 * işi ve kuralı (gizli etkinlikte çiçek çıkmaz) sunucuda. Burası yalnızca
 * "hangi çiçekçiyle çalışıyoruz" listesi.
 */
export default function Flowers() {
  // Yönetim düğmeleri İZNE göre gizlenir.
  const { hasPermission } = useSession();

  const qc = useQueryClient();
  const { bildir } = useToast();
  const [formAcik, setFormAcik] = useState(false);
  const [duzenlenen, setDuzenlenen] = useState<Florist | null>(null);
  const [silinecek, setSilinecek] = useState<Florist | null>(null);

  const cicekciler = useQuery({
    queryKey: ['cicek', 'cicekciler'] as const,
    queryFn: () => api.get<Florist[]>('/cicek/cicekciler'),
  });

  const kaydet = useMutation({
    mutationFn: (c: Florist) =>
      c.id
        ? api.put<Florist>(`/cicek/cicekciler/${c.id}`, c)
        : api.post<Florist>('/cicek/cicekciler', c),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cicek'] });
      setFormAcik(false);
      setDuzenlenen(null);
      bildir('basari', 'Çiçekçi kaydedildi');
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<boolean>(`/cicek/cicekciler/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cicek'] });
      setSilinecek(null);
      bildir('basari', 'Çiçekçi silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  const sutunlar: Column<Florist>[] = [
    {
      anahtar: 'ad',
      baslik: 'Çiçekçi',
      /*
        İÇ İÇE BAĞLANTI OLMAZ.

        Satırın tamamı `bagla` ile bir `<a>`; adı da ayrıca `<Link>` yapmak
        `<a>` içinde `<a>` üretiyordu. Bu geçersiz HTML ve tarayıcı davranışı
        tanımsız: mobilde çiçekçi dosyasına tıklamak HİÇBİR ŞEY yapmıyordu.
        Ad artık düz metin; dosyaya götüren şey satırın kendisi.
      */
      hucre: (c) => <span className="font-medium">{c.adSoyad}</span>,
      mobil: false,
    },
    {
      anahtar: 'telefon',
      baslik: 'Telefon',
      genislik: 'w-40',
      /*
        TELEFON DÜZ METİN.

        `tel:` bağlantısıydı; satırın tamamı çiçekçi dosyasına giden bir
        bağlantıya dönünce `<a>` içinde `<a>` oluştu. Bu geçersiz HTML ve
        tarayıcı davranışı tanımsız — mobilde satıra dokunmak hiçbir şey
        yapmıyordu. Numara aranabilir olarak DOSYADA duruyor; liste satırının
        işi oraya götürmek.
      */
      hucre: (c) => <span className="tabular-nums">{c.telefon}</span>,
    },
    {
      anahtar: 'adres',
      baslik: 'Adres',
      hucre: (c) => <span className="line-clamp-1">{c.adres || '—'}</span>,
      mobil: false,
    },
    {
      anahtar: 'durum',
      baslik: 'Durum',
      genislik: 'w-28',
      hucre: (c) => <StatusBadge aktif={c.aktif ?? false} />,
    },
    {
      anahtar: 'eylem',
      baslik: '',
      genislik: 'w-24',
      mobil: false,
      hucre: (c) => (
        <span className="flex justify-end gap-1">
          <IconButton
            etiket="Düzenle"
            onClick={() => {
              setDuzenlenen(c);
              setFormAcik(true);
            }}
          >
            <Pencil size={15} />
          </IconButton>
          <IconButton
            etiket="Sil"
            onClick={() => setSilinecek(c)}
            className="hover:text-(--st-no)"
          >
            <Trash2 size={15} />
          </IconButton>
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-3.5">
      {formAcik && (
        <CicekciFormu
          baslangic={duzenlenen}
          kaydet={(c) => kaydet.mutate(c)}
          beklemede={kaydet.isPending}
          vazgec={() => {
            setFormAcik(false);
            setDuzenlenen(null);
          }}
        />
      )}

      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center">
        <div>
          <h2 className="font-display text-lg font-bold">Çiçekçiler</h2>
          <p className="text-sm text-text-3">
            Çiçek talimatı bu listedeki <b>aktif</b> çiçekçilere gider.
          </p>
        </div>
{hasPermission(PERMISSION.cicekYonet) && (
        <Button
          className="sm:ml-auto"
          onClick={() => {
            setDuzenlenen(null);
            setFormAcik(true);
          }}
        >
          <Plus size={14} />
          Çiçekçi ekle
        </Button>
        )}
      </div>

      {/*
        Kart ızgarası yerine LİSTE.

        İki çiçekçi kaydı üç sütunluk bir ızgarada iki küçük kutu olarak
        duruyor, ekranın kalanı boş kalıyordu. Kayıtlar birkaç satır ve hepsi
        aynı alanları taşıyor — ad, telefon, adres, durum. Bu bir tablo işi;
        diğer yönetim ekranlarıyla da aynı dil (mobilde kart listesine döner).
      */}
      {cicekciler.isLoading ? (
        <SkeletonRows adet={4} />
      ) : (
        <DataList
          satirlar={cicekciler.data ?? []}
          sutunlar={sutunlar}
          anahtar={(c) => c.id!}
          /*
            SATIRIN TAMAMI DOSYAYA GİDİYOR.

            Ad sütunu bir `Link`ti ama o sütun `mobil: false` — yani telefonda
            hiç çizilmiyordu ve çiçekçi dosyasına ULAŞMANIN YOLU YOKTU.
            `bagla` satırın kendisini bağlantı yapıyor; masaüstünde addaki
            bağlantı da duruyor.
          */
          bagla={(c) => `/cicek/${c.id}`}
          mobilBaslik={(c) => c.adSoyad}
          mobilAciklama={(c) => c.telefon}
          mobilRozet={(c) => <StatusBadge aktif={c.aktif ?? false} />}
          bos={
            <EmptyState
              ikon={Flower2}
              baslik="Kayıtlı çiçekçi yok"
              aciklama="Çiçek talimatı gönderebilmek için önce bir çiçekçi ekleyin."
              eylem={
                // Boş durumdaki EKLEME düğmesi de izin ister; araç çubuğundaki
                // düğme kapıdan geçiyordu ama liste boşken çizilen bu ikinci
                // düğme kapının dışında kalmıştı.
                hasPermission(PERMISSION.cicekYonet) ? (
                  <Button onClick={() => setFormAcik(true)}>Çiçekçi ekle</Button>
                ) : undefined
              }
            />
          }
        />
      )}

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Çiçekçi silinsin mi?"
        aciklama={`"${silinecek?.adSoyad}" kaydı kalıcı olarak silinecek.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

/**
 * Aktif/pasif rozeti.
 *
 * Pasif çiçekçiye talimat GÖNDERİLMEZ; bu yüzden durum listede bir bakışta
 * okunmalı. Renkler durum tokenlarından — çiçekçi bir "durum" değil ama
 * "çalışıyor / çalışmıyor" ayrımı sistem geri bildirimiyle aynı dil.
 */
function StatusBadge({ aktif }: { aktif: boolean }) {
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-2xs font-semibold"
      style={
        aktif
          ? { color: 'var(--st-ok)', background: 'var(--st-ok-bg)' }
          : { color: 'var(--st-cancel)', background: 'var(--st-cancel-bg)' }
      }
    >
      <Power size={9} />
      {aktif ? 'Aktif' : 'Pasif'}
    </span>
  );
}

/**
 * Çiçekçi formu — diyalogda.
 *
 * <p>
 * Önce tam sayfaydı; gerekçesi "mobilde klavye açılınca kaydet düğmesi ekran
 * dışında kalıyor" idi. Bu sorunu <c>FormModal</c> çözdü: gövde kayar, alt
 * çubuk sabit kalır. Tam sayfa çözümün bedeli, kaydettikten sonra listedeki
 * yerin (sayfa, süzgeç, kaydırma) kaybolmasıydı.
 * </p>
 */
function CicekciFormu({
  baslangic,
  kaydet,
  beklemede,
  vazgec,
}: {
  baslangic: Florist | null;
  kaydet: (c: Florist) => void;
  beklemede: boolean;
  vazgec: () => void;
}) {
  const [form, setForm] = useState<Florist>(
    baslangic ?? { id: 0, adSoyad: '', telefon: '', adres: '', aktif: true },
  );

  const gecerli = form.adSoyad.trim() && form.telefon.trim() && form.adres.trim();

  return (
    <FormModal
      acik
      kapat={vazgec}
      baslik={baslangic ? 'Çiçekçiyi düzenle' : 'Yeni çiçekçi'}
      ikon={<Flower2 size={15} />}
      genislik="dar"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={vazgec}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && kaydet(form)}
            disabled={!gecerli || beklemede}
          >
            Kaydet
          </Button>
        </>
      }
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (gecerli) kaydet(form);
        }}
      >
        <div className="space-y-4">
          <FieldWrapper etiket="Ad Soyad" id="c-ad" zorunlu>
            <Input
              id="c-ad"
              value={form.adSoyad}
              onChange={(e) => setForm({ ...form, adSoyad: e.target.value })}
              maxLength={100}
              autoFocus
            />
          </FieldWrapper>

          <FieldWrapper etiket="Telefon" id="c-tel" zorunlu ipucu="SMS bu numaraya gider.">
            <Input
              id="c-tel"
              type="tel"
              inputMode="tel"
              value={form.telefon}
              onChange={(e) => setForm({ ...form, telefon: e.target.value })}
            />
          </FieldWrapper>

          <FieldWrapper etiket="Adres" id="c-adres" zorunlu>
            <Input
              id="c-adres"
              value={form.adres}
              onChange={(e) => setForm({ ...form, adres: e.target.value })}
            />
          </FieldWrapper>

          <Switch
            isaretli={form.aktif ?? true}
            degistir={(a) => setForm({ ...form, aktif: a })}
            etiket="Aktif"
            aciklama="Pasif çiçekçiye talimat gönderilmez."
          />
        </div>
      </form>
    </FormModal>
  );
}
