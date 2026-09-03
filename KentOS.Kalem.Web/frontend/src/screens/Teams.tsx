import {
  Crown, Pencil, Plus, Search, SlidersHorizontal, Trash2, UserPlus, Users,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, IconButton } from '../components/Button';
import { Card } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { FormModal } from '../components/FormModal';
import { FieldWrapper, Input, SearchInput, Textarea } from '../components/Field';
import { Pagination } from '../components/Pagination';
import { SegmentedSelect } from '../components/Filters';
import { Segment, FilterSection, FilterSheet } from '../components/FilterSheet';
import { Fab } from '../shell/mobile/Fab';
import { SkeletonRows } from '../components/Skeleton';
import { Switch } from '../components/Switch';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { Avatar, PersonPicker } from '../components/PersonPicker';
import { useTeamMutations, useTeams } from '../data/tasks';
import type { Team, TeamSave } from '../data/types';
import { UnitScopePicker } from '../components/UnitScopePicker';

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
  const [suzgecAcik, setSuzgecAcik] = useState(false);

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
      {/* Telefonda üst şeritte YALNIZCA arama; gerisi FAB'ın tabakasında.
          Gerekçesi ve ölçümü `Projects.tsx` içinde. */}
      <div className="flex items-center gap-2">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Ekip ara"
          aria-label="Ekiplerde ara"
          ikon={<Search size={15} />}
          className="min-w-0 flex-1 md:max-w-[300px]"
        />

        <div className="hidden min-w-0 flex-wrap items-center gap-2 md:ml-auto md:flex md:flex-nowrap">
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
          />

          {yetkili && (
            <Button onClick={() => setDuzenlenen('yeni')}>
              <Plus size={14} />
              Ekip kur
            </Button>
          )}
        </div>
      </div>

      {/* ── Mobil: FAB ve süzgeç tabakası ── */}
      <Fab
        etiket="Ekip eylemleri"
        eylemler={[
          ...(yetkili
            ? [{
                etiket: 'Ekip kur',
                ikon: <Plus size={21} strokeWidth={2.2} />,
                onClick: () => setDuzenlenen('yeni'),
              }]
            : []),
          {
            etiket: 'Ara ve süz',
            ikon: <SlidersHorizontal size={19} strokeWidth={2} />,
            onClick: () => setSuzgecAcik(true),
          },
        ]}
      />

      <FilterSheet
        acik={suzgecAcik}
        kapat={() => setSuzgecAcik(false)}
        etkinSayisi={(arama ? 1 : 0) + (kapsam !== 'kendi' ? 1 : 0)}
        temizle={() => {
          setAramaGirdisi('');
          setKapsam('kendi');
          setSayfa(1);
        }}
      >
        <FilterSection baslik="Birim">
          <UnitScopePicker className="w-full" />
          <Segment<Kapsam>
            deger={kapsam}
            degistir={(d) => {
              setKapsam(d);
              setSayfa(1);
            }}
            secenekler={[
              { deger: 'kendi', etiket: 'Birimim' },
              { deger: 'alt', etiket: 'Alt birimler' },
            ]}
          />
        </FilterSection>
      </FilterSheet>

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
              <EkipKarti
                key={e.id}
                ekip={e}
                yetkili={yetkili}
                duzenle={() => setDuzenlenen(e)}
                sil={() => setSilinecek(e)}
              />
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
 * EKİP KARTI.
 *
 * <p>
 * <b>Üyeler kartın kendisinde.</b> Önceki hâlinde kart "0 üye" yazan gri bir
 * satırdan ibaretti ve üye eklemenin tek yolu, ne yaptığını söylemeyen bir
 * kalem ikonuydu. Kullanıcı ekibi kurup "ekibe kişi ekleme yok" dedi — haklı:
 * ekranda üye eklemeye davet eden hiçbir şey yoktu.
 * </p>
 *
 * <p>
 * Şimdi üyeler baş harf rozetleriyle görünüyor, lider taçla işaretli ve üyesi
 * olmayan ekipte rozetlerin yerini <b>"Üye ekle" düğmesi</b> alıyor. Boş
 * durum, kendisini dolduran eylemi taşımalı.
 * </p>
 */
function EkipKarti({
  ekip: e,
  yetkili,
  duzenle,
  sil,
}: {
  ekip: Team;
  yetkili: boolean;
  duzenle: () => void;
  sil: () => void;
}) {
  const uyeler = e.uyeler ?? [];
  const gosterilen = uyeler.slice(0, 6);
  const kalan = uyeler.length - gosterilen.length;

  return (
    <Card className="flex flex-col p-3.5">
      <div className="flex items-start gap-2">
        <div className="min-w-0 flex-1">
          <h3 className="truncate font-display text-sm font-semibold text-ink">
            {e.ad}
            {!e.kullanimda && (
              <span className="ml-2 text-2xs font-normal text-ink-3">kullanım dışı</span>
            )}
          </h3>
          <p className="mt-0.5 truncate text-2xs text-ink-3">{e.birimAd}</p>
        </div>

        {yetkili && (
          <>
            <IconButton etiket={`${e.ad} ekibini düzenle`} onClick={duzenle}>
              <Pencil size={16} />
            </IconButton>
            <IconButton etiket={`${e.ad} ekibini sil`} onClick={sil}>
              <Trash2 size={16} />
            </IconButton>
          </>
        )}
      </div>

      {e.aciklama && <p className="mt-2 line-clamp-2 text-xs text-text-2">{e.aciklama}</p>}

      {/* ── Üyeler ── */}
      <div className="mt-3">
        {uyeler.length === 0 ? (
          <div className="flex items-center justify-between gap-2 rounded-control border border-dashed border-line px-2.5 py-2">
            <span className="text-2xs text-ink-3">Henüz üye yok</span>
            {yetkili && (
              <Button varyant="sade" onClick={duzenle}>
                <UserPlus size={14} />
                Üye ekle
              </Button>
            )}
          </div>
        ) : (
          <button
            type="button"
            onClick={yetkili ? duzenle : undefined}
            disabled={!yetkili}
            aria-label={yetkili ? `${e.ad} ekibinin üyelerini düzenle` : undefined}
            className="flex w-full items-center gap-1.5 rounded-control py-1 text-left disabled:cursor-default enabled:hover:bg-sunken"
          >
            {gosterilen.map((u) => (
              <span key={u.kullaniciId} className="relative" title={u.ad ?? undefined}>
                <Avatar ad={u.ad ?? ''} boyut="kucuk" />
                {u.lider && (
                  <Crown
                    size={10}
                    className="absolute -right-0.5 -top-1 text-brand"
                    strokeWidth={2.6}
                  />
                )}
              </span>
            ))}
            {kalan > 0 && (
              <span className="text-2xs tabular-nums text-ink-3">+{kalan}</span>
            )}
            {yetkili && (
              <span className="ml-auto grid h-6 w-6 place-items-center rounded-full border border-dashed border-line text-ink-3">
                <UserPlus size={12} />
              </span>
            )}
          </button>
        )}
      </div>

      <div className="mt-2.5 flex items-center gap-3 text-2xs text-ink-3">
        <span>{e.uyeSayisi} üye</span>
        {!e.liderAd && uyeler.length > 0 && <span className="text-(--st-wait)">Lider yok</span>}
        {(e.acikGorevSayisi ?? 0) > 0 && (
          <span className="text-(--st-live)">{e.acikGorevSayisi} açık görev</span>
        )}
      </div>
    </Card>
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

  const [form, setForm] = useState<TeamSave>({
    ad: ekip?.ad ?? '',
    aciklama: ekip?.aciklama ?? null,
    liderId: ekip?.liderId ?? null,
    kullanimda: ekip?.kullanimda ?? true,
    uyeIdler: (ekip?.uyeler ?? []).map((u) => u.kullaniciId!),
  });

  const liderUye = !form.liderId || (form.uyeIdler ?? []).includes(form.liderId);
  const gecerli = form.ad.trim().length > 0 && liderUye;

  function uyeleriYaz(idler: number[]) {
    setForm({
      ...form,
      uyeIdler: idler,
      // Üyelikten çıkarılan kişi lider kalamaz: sunucu bu kaydı reddederdi ve
      // kullanıcı neden reddedildiğini formda göremezdi.
      liderId: form.liderId && !idler.includes(form.liderId) ? null : form.liderId,
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
        ipucu={
          (form.uyeIdler ?? []).length === 0
            ? 'Ekibe kimi koyacaksanız arayıp dokunun. Kendinizi de ekleyebilirsiniz.'
            : `${(form.uyeIdler ?? []).length} kişi seçili · listede olmayan ekipten çıkarılır`
        }
      >
        <PersonPicker
          id="ekip-uyeler"
          secili={form.uyeIdler ?? []}
          degistir={uyeleriYaz}
          liderId={form.liderId ?? null}
          liderDegistir={(id) => setForm({ ...form, liderId: id })}
        />
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
