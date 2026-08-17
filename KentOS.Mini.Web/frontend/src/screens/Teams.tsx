import { Pencil, Plus, Search, Trash2, Users } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, IconButton } from '../components/Button';
import { Card } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { FormModal } from '../components/FormModal';
import { FieldWrapper, Input, SearchInput, Textarea } from '../components/Field';
import { Pagination } from '../components/Pagination';
import { SegmentedSelect } from '../components/Filters';
import { SkeletonRows } from '../components/Skeleton';
import { Switch } from '../components/Switch';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { useUnitUsers } from '../data/hooks';
import { useTeamMutations, useTeams } from '../data/tasks';
import type { Team, TeamSave } from '../data/types';
import { UnitScopePicker } from './task/UnitScopePicker';

type Kapsam = 'kendi' | 'alt';

/**
 * EKİPLER — birimin kalıcı çalışma grupları.
 *
 * <p>
 * Ekip <b>birimin</b> yapısı, projenin değil: park bahçelerin budama ekibi her
 * projede aynı ekip. Göreve ekip atandığında bildirim önce <b>lidere</b>
 * gidiyor ve iş dağıtımını lider yapıyor.
 * </p>
 */
export default function Teams() {
  const { hasPermission } = useSession();
  const { bildir } = useToast();
  const m = useTeamMutations();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [kapsam, setKapsam] = useState<Kapsam>('kendi');
  const [sayfa, setSayfa] = useState(1);

  const [duzenlenen, setDuzenlenen] = useState<Team | 'yeni' | null>(null);
  const [silinecek, setSilinecek] = useState<Team | null>(null);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const { data, isLoading } = useTeams({
    sayfa,
    boyut: 50,
    ara: arama,
    altBirimlerDahil: kapsam === 'alt',
  });

  const ekipler = data?.veriler ?? [];
  const yetkili = hasPermission(PERMISSION.ekipYonet);

  return (
    <div className="space-y-3.5">
      <div className="flex flex-wrap items-center gap-2">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Ekip ara"
          aria-label="Ekiplerde ara"
          ikon={<Search size={15} />}
          className="min-w-0 flex-1 md:max-w-[300px]"
        />

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

        {yetkili && (
          <Button onClick={() => setDuzenlenen('yeni')}>
            <Plus size={14} />
            Ekip kur
          </Button>
        )}
      </div>

      {isLoading ? (
        <SkeletonRows adet={4} />
      ) : ekipler.length === 0 ? (
        <EmptyState
          ikon={Users}
          baslik={arama ? 'Eşleşen ekip yok' : 'Ekip yok'}
          aciklama={
            arama
              ? 'Aramayı temizleyerek tüm ekipleri görebilirsiniz.'
              : 'Biriminizde tanımlı bir çalışma ekibi bulunmuyor.'
          }
        />
      ) : (
        <>
          <div className="grid gap-2.5 md:grid-cols-2 xl:grid-cols-3">
            {ekipler.map((e) => (
              <Card key={e.id} className="p-3.5">
                <div className="flex items-start gap-2">
                  <div className="min-w-0 flex-1">
                    <h3 className="truncate font-display text-sm font-semibold text-ink">
                      {e.ad}
                      {!e.kullanimda && (
                        <span className="ml-2 text-2xs font-normal text-ink-3">
                          kullanım dışı
                        </span>
                      )}
                    </h3>
                    <p className="mt-0.5 text-2xs text-ink-3">
                      {e.birimAd}
                      {e.liderAd ? ` · Lider: ${e.liderAd}` : ' · Lidersiz'}
                    </p>
                  </div>

                  {yetkili && (
                    <>
                      <IconButton etiket="Düzenle" onClick={() => setDuzenlenen(e)}>
                        <Pencil size={16} />
                      </IconButton>
                      <IconButton etiket="Sil" onClick={() => setSilinecek(e)}>
                        <Trash2 size={16} />
                      </IconButton>
                    </>
                  )}
                </div>

                {e.aciklama && (
                  <p className="mt-2 line-clamp-2 text-xs text-text-2">{e.aciklama}</p>
                )}

                <div className="mt-2.5 flex items-center gap-3 text-2xs text-ink-3">
                  <span>{e.uyeSayisi} üye</span>
                  {(e.acikGorevSayisi ?? 0) > 0 && (
                    <span className="text-(--st-live)">{e.acikGorevSayisi} açık görev</span>
                  )}
                </div>

                {(e.uyeler ?? []).length > 0 && (
                  <p className="mt-2 line-clamp-2 text-2xs text-text-3">
                    {(e.uyeler ?? []).map((u) => u.ad).join(', ')}
                  </p>
                )}
              </Card>
            ))}
          </div>

          <Pagination sonuc={data} sayfaDegistir={setSayfa} birim="ekip" className="mt-3" />
        </>
      )}

      {duzenlenen && (
        <EkipFormu
          ekip={duzenlenen === 'yeni' ? null : duzenlenen}
          kapat={() => setDuzenlenen(null)}
        />
      )}

      <ConfirmDialog
        acik={!!silinecek}
        kapat={() => setSilinecek(null)}
        baslik={`"${silinecek?.ad}" silinsin mi?`}
        aciklama={
          'Üzerinde açık görevi olan ekip silinemez. Görevleri devredin ya da ekibi ' +
          'kullanımdan kaldırın.'
        }
        onayEtiketi="Sil"
        yikici
        onayla={async () => {
          try {
            await m.sil.mutateAsync(silinecek!.id!);
            bildir('basari', 'Ekip silindi');
            setSilinecek(null);
          } catch (h) {
            bildir('hata', 'Ekip silinemedi', (h as Error).message);
          }
        }}
      />
    </div>
  );
}

/**
 * Ekip formu.
 *
 * <p>
 * Üye listesi <b>tam liste</b> olarak gönderiliyor: kutusu işaretli olmayan
 * kişi ekipten çıkarılır. Lider mutlaka üyeler arasında olmalı — dışarıdan
 * biri ekibi yönetemez; sunucu da bunu reddediyor.
 * </p>
 */
function EkipFormu({ ekip, kapat }: { ekip: Team | null; kapat: () => void }) {
  const { bildir } = useToast();
  const m = useTeamMutations();
  const kullanicilar = useUnitUsers();

  const [form, setForm] = useState<TeamSave>({
    ad: ekip?.ad ?? '',
    aciklama: ekip?.aciklama ?? null,
    liderId: ekip?.liderId ?? null,
    kullanimda: ekip?.kullanimda ?? true,
    uyeIdler: (ekip?.uyeler ?? []).map((u) => u.kullaniciId!),
  });

  const uyeler = new Set(form.uyeIdler ?? []);
  const liderUye = !form.liderId || uyeler.has(form.liderId);
  const gecerli = form.ad.trim().length > 0 && liderUye;

  function uyeDegistir(id: number, secili: boolean) {
    const yeni = new Set(uyeler);
    if (secili) yeni.add(id);
    else yeni.delete(id);

    setForm({
      ...form,
      uyeIdler: [...yeni],
      // Üyelikten çıkarılan kişi lider kalamaz: sunucu bu kaydı reddederdi ve
      // kullanıcı neden reddedildiğini formda göremezdi.
      liderId: form.liderId && !yeni.has(form.liderId) ? null : form.liderId,
    });
  }

  async function kaydet() {
    try {
      if (ekip) await m.guncelle.mutateAsync({ id: ekip.id!, govde: form });
      else await m.olustur.mutateAsync(form);
      bildir('basari', ekip ? 'Ekip güncellendi' : 'Ekip kuruldu');
      kapat();
    } catch (h) {
      bildir('hata', 'Ekip kaydedilemedi', (h as Error).message);
    }
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={ekip ? 'Ekibi düzenle' : 'Ekip kur'}
      aciklama="Göreve ekip atandığında bildirim önce lidere gider."
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
      <FieldWrapper etiket="Ekip adı" id="ekip-ad" zorunlu>
        <Input
          id="ekip-ad"
          value={form.ad}
          onChange={(e) => setForm({ ...form, ad: e.target.value })}
          placeholder="Budama Ekibi"
          maxLength={200}
        />
      </FieldWrapper>

      <FieldWrapper etiket="Açıklama" id="ekip-aciklama">
        <Textarea
          id="ekip-aciklama"
          rows={2}
          value={form.aciklama ?? ''}
          onChange={(e) => setForm({ ...form, aciklama: e.target.value || null })}
        />
      </FieldWrapper>

      <FieldWrapper
        etiket="Üyeler"
        id="ekip-uyeler"
        ipucu="Listede olmayan kişi ekipten çıkarılır."
      >
        <div
          id="ekip-uyeler"
          className="max-h-64 divide-y divide-line overflow-y-auto rounded-control border border-line"
        >
          {kullanicilar.liste.map((k) => {
            const secili = uyeler.has(k.id!);
            return (
              <label
                key={k.id}
                className="flex min-h-11 cursor-pointer items-center gap-2.5 px-3 py-2 hover:bg-sunken"
              >
                <input
                  type="checkbox"
                  checked={secili}
                  onChange={(e) => uyeDegistir(k.id!, e.target.checked)}
                  className="h-4 w-4 accent-[var(--brand-ui)]"
                />
                <span className="min-w-0 flex-1 truncate text-sm text-ink">{k.ad}</span>

                {/* Lider seçimi ÜYE SATIRINDA: ayrı bir açılır liste, kişinin
                    üye olup olmadığını iki yerden okumayı gerektiriyordu. */}
                {secili && (
                  <button
                    type="button"
                    onClick={(e) => {
                      e.preventDefault();
                      setForm({
                        ...form,
                        liderId: form.liderId === k.id ? null : k.id!,
                      });
                    }}
                    className={`rounded-full px-2 py-0.5 text-3xs ${
                      form.liderId === k.id
                        ? 'bg-brand-ui text-white'
                        : 'bg-sunken text-ink-3 hover:text-ink-2'
                    }`}
                  >
                    {form.liderId === k.id ? 'Lider' : 'Lider yap'}
                  </button>
                )}
              </label>
            );
          })}
        </div>
      </FieldWrapper>

      <Switch
        isaretli={form.kullanimda ?? true}
        degistir={(a) => setForm({ ...form, kullanimda: a })}
        etiket="Kullanımda"
        aciklama="Kapatılan ekip yeni görevlerde seçilemez; mevcut görevleri durur."
      />
    </FormModal>
  );
}
