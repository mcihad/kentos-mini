import * as Tabs from '@radix-ui/react-tabs';
import { SekmeListesi, SekmeTetigi } from '../components/Tabs';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  CalendarDays, ClipboardList, MapPin, Plus, Search, Tag, Trash2, Upload, Users, Pencil,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { FieldWrapper, SearchInput, Input } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { RowActions } from '../components/RowActions';
import { useIsDesktop } from '../components/screenSize';
import { Fab } from '../shell/mobile/Fab';
import { SkeletonRows } from '../components/Skeleton';
import { FormModal } from '../components/FormModal';
import { Card } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { ColorStrip, colorOr } from '../components/Color';
import { Pagination } from '../components/Pagination';
import { useToast } from '../components/Toast';
import { number } from '../data/format';
import { api } from '../data/client';
import {
  useNameRecords, useDefinitions, type NameRecordType, type DefinitionType,
} from '../data/hooks';
import type { NameRecord, Definition, BulkImportResult } from '../data/types';

/**
 * Tanımlar — eski arayüzdeki beş ayrı yönetim ekranının karşılığı
 * (`RandevuTip`, `AjandaDurum`, `RandevuDurum`, `Mahalle`, `Meslek`).
 *
 * <p>
 * Buradaki <b>renk</b> alanı arayüzün her yerine yayılır: takvim etkinlikleri,
 * ajanda listesi ve talep rozetleri bu değerden boyanır. Tema tokenı değildir —
 * kullanıcı burada değiştirir, uygulama her yerde ona uyar.
 * </p>
 */
export default function Definitions() {
  return (
    <Tabs.Root defaultValue="etkinlik-durumlari">
      <SekmeListesi etiket="Tanım grupları" className="mb-4">
        {[
          { d: 'etkinlik-durumlari', e: 'Etkinlik durumları', i: <CalendarDays size={14} /> },
          { d: 'etkinlik-tipleri', e: 'Tipler', i: <Tag size={14} /> },
          { d: 'talep-durumlari', e: 'Talep durumları', i: <ClipboardList size={14} /> },
          { d: 'mahalleler', e: 'Mahalleler', i: <MapPin size={14} /> },
          { d: 'meslekler', e: 'Meslekler', i: <Users size={14} /> },
        ].map((s) => (
          <SekmeTetigi key={s.d} deger={s.d}>
            {s.i}
            {s.e}
          </SekmeTetigi>
        ))}
      </SekmeListesi>

      <Tabs.Content value="etkinlik-durumlari">
        <TanimBolumu
          tur="etkinlik-durumlari"
          baslik="Etkinlik durumları"
          aciklama="Takvimdeki etkinlikler bu renklerle boyanır."
          simgeAlaniVar
        />
      </Tabs.Content>
      <Tabs.Content value="etkinlik-tipleri">
        <TanimBolumu
          tur="etkinlik-tipleri"
          baslik="Etkinlik ve talep tipleri"
          aciklama="Aynı liste hem etkinliklerde hem taleplerde kullanılır."
        />
      </Tabs.Content>
      <Tabs.Content value="talep-durumlari">
        <TanimBolumu
          tur="talep-durumlari"
          baslik="Talep durumları"
          aciklama="Talep listesindeki rozetler bu renklerden gelir."
          simgeAlaniVar
        />
      </Tabs.Content>
      <Tabs.Content value="mahalleler">
        <AdKaydiBolumu
          tur="mahalleler"
          baslik="Mahalleler"
          tekil="mahalle"
          aciklama="Talep formundaki mahalle listesi."
        />
      </Tabs.Content>
      <Tabs.Content value="meslekler">
        <AdKaydiBolumu
          tur="meslekler"
          baslik="Meslekler"
          tekil="meslek"
          aciklama="Talep formundaki meslek önerileri."
        />
      </Tabs.Content>
    </Tabs.Root>
  );
}

// ══════════════════════════════════════════════════ renkli tanımlar

function TanimBolumu({
  tur,
  baslik,
  aciklama,
  simgeAlaniVar,
}: {
  tur: DefinitionType;
  baslik: string;
  aciklama: string;
  simgeAlaniVar?: boolean;
}) {
  const masaustu = useIsDesktop();
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [sayfa, setSayfa] = useState(1);
  const [ara, setAra] = useState('');
  const [form, setForm] = useState<'kapali' | 'yeni' | Definition>('kapali');
  const [silinecek, setSilinecek] = useState<Definition | null>(null);

  const { data, isLoading } = useDefinitions(tur, { sayfa, boyut: 50, ara });

  const tazele = () => qc.invalidateQueries({ queryKey: ['tanim'] });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/tanim/${tur}/${id}`),
    onSuccess: () => {
      tazele();
      // Renkler her ekranda kullanılıyor; referans önbelleği de bayatladı.
      qc.invalidateQueries({ queryKey: ['ayar'] });
      setSilinecek(null);
      bildir('basari', 'Tanım silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  return (
    <div className="space-y-3.5">
      {form !== 'kapali' && (
        <TanimFormu
          tur={tur}
          mevcut={form === 'yeni' ? null : form}
          simgeAlaniVar={simgeAlaniVar}
          kapat={() => setForm('kapali')}
        />
      )}

      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center">
        <div>
          <h2 className="font-display text-lg font-bold">{baslik}</h2>
          <p className="text-sm text-text-3">{aciklama}</p>
        </div>
        <Button className="hidden sm:ml-auto sm:inline-flex" onClick={() => setForm('yeni')}>
          <Plus size={14} />
          Yeni tanım
        </Button>
      </div>

      {/* Tam genişlik ekleme düğmesi listeden bir kayıt boyu yer yiyordu. */}
      {!masaustu && <Fab etiket="Yeni tanım" onClick={() => setForm('yeni')} />}

      <SearchInput
        value={ara}
        onChange={(e) => {
          setAra(e.target.value);
          setSayfa(1);
        }}
        placeholder="Tanım ara"
        aria-label="Tanımlarda ara"
        ikon={<Search size={15} />}
        className="md:max-w-[320px]"
      />

      {isLoading ? (
        <SkeletonRows adet={5} />
      ) : (data?.veriler ?? []).length === 0 ? (
        <EmptyState ikon={Tag} baslik="Tanım yok" />
      ) : (
        <>
          {/*
            Mobilde TEK YÜZEY: ızgara telefonda tek sütuna düşünce her tanım
            ayrı bir kart oluyor ve aralarındaki boşluk listeyi gereksiz
            uzatıyordu. Masaüstünde ızgara duruyor — orada kartlar yan yana
            ve boşluk onları ayırmak için gerekli.
          */}
          <ul className="overflow-hidden rounded-card border border-line bg-surface divide-y divide-line
                         sm:grid sm:gap-2.5 sm:divide-y-0 sm:border-0 sm:bg-transparent sm:grid-cols-2 lg:grid-cols-3">
            {(data?.veriler ?? []).map((t) => (
              <li key={t.id}>
                <div className="flex items-stretch gap-3 p-3.5 sm:rounded-card sm:border sm:border-line sm:bg-surface sm:shadow-1">
                  <ColorStrip renk={t.renk} />

                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-semibold">{t.ad}</p>
                    <p className="mt-0.5 flex items-center gap-2 text-xs text-text-3">
                      <span
                        className="inline-block h-[10px] w-[10px] rounded-xs ring-1 ring-border"
                        style={{ background: colorOr(t.renk) }}
                        aria-hidden
                      />
                      {t.renk || 'renk yok'}
                      <span aria-hidden>·</span>
                      {number(t.kullanimSayisi)} kayıtta
                    </p>
                    {t.aciklama && (
                      <p className="mt-1 line-clamp-1 text-xs text-text-2">{t.aciklama}</p>
                    )}
                  </div>

                  <RowActions
                    boyut="kucuk"
                    className="shrink-0 self-start"
                    eylemler={[
                      { etiket: 'Düzenle', ikon: Pencil, onClick: () => setForm(t) },
                      {
                        etiket: 'Sil',
                        ikon: Trash2,
                        onClick: () => setSilinecek(t),
                        ton: 'tehlike',
                      },
                    ]}
                  />
                </div>
              </li>
            ))}
          </ul>

          <Pagination sonuc={data} sayfaDegistir={setSayfa} birim="tanım" />
        </>
      )}

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Tanım silinsin mi?"
        aciklama={
          (silinecek?.kullanimSayisi ?? 0) > 0
            ? `"${silinecek?.ad}" ${number(silinecek?.kullanimSayisi)} kayıtta kullanılıyor; sunucu silmeyi reddedecek.`
            : `"${silinecek?.ad}" kalıcı olarak silinecek.`
        }
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

/**
 * Tanım formu.
 *
 * Renk seçici hem `<input type="color">` hem metin kutusu: görsel seçim
 * kolay ama kurumsal paletten tam değer yapıştırmak da gerekiyor.
 */
function TanimFormu({
  tur,
  mevcut,
  simgeAlaniVar,
  kapat,
}: {
  tur: DefinitionType;
  mevcut: Definition | null;
  simgeAlaniVar?: boolean;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [ad, setAd] = useState(mevcut?.ad ?? '');
  const [renk, setRenk] = useState(mevcut?.renk ?? '#002E6D');
  const [simge, setSimge] = useState(mevcut?.simge ?? '');
  const [aciklama, setAciklama] = useState(mevcut?.aciklama ?? '');

  const kaydet = useMutation({
    mutationFn: () => {
      const govde = { ad, renk, simge: simge || null, aciklama: aciklama || null };
      return mevcut
        ? api.put(`/tanim/${tur}/${mevcut.id}`, govde)
        : api.post(`/tanim/${tur}`, govde);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tanim'] });
      qc.invalidateQueries({ queryKey: ['ayar'] });
      // Renk değiştiyse takvim ve listeler de yeniden boyanmalı.
      qc.invalidateQueries({ queryKey: ['etkinlik'] });
      qc.invalidateQueries({ queryKey: ['talep'] });
      bildir('basari', mevcut ? 'Tanım güncellendi' : 'Tanım oluşturuldu');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const gecerli = ad.trim().length > 0;

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={mevcut ? 'Tanımı düzenle' : 'Yeni tanım'}
      ikon={<Tag size={15} />}
      genislik="orta"
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            type="button"
            onClick={() => gecerli && kaydet.mutate()}
            disabled={!gecerli || kaydet.isPending}
          >
            Kaydet
          </Button>
        </>
      }
    >
      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (gecerli) kaydet.mutate();
        }}
      >
        <div className="space-y-4">
          <FieldWrapper etiket="Ad" id="t-ad" zorunlu>
            <Input id="t-ad" value={ad} onChange={(e) => setAd(e.target.value)} autoFocus maxLength={100} />
          </FieldWrapper>

          <FieldWrapper
            etiket="Renk"
            id="t-renk"
            ipucu="Takvimde ve listelerde bu renk kullanılır."
          >
            <div className="flex items-center gap-2.5">
              <input
                id="t-renk"
                type="color"
                value={/^#[0-9a-f]{6}$/i.test(renk) ? renk : '#002E6D'}
                onChange={(e) => setRenk(e.target.value)}
                className="h-10 w-12 shrink-0 cursor-pointer rounded-control border border-border bg-surface p-1"
                aria-label="Renk seçici"
              />
              <Input
                value={renk}
                onChange={(e) => setRenk(e.target.value)}
                placeholder="#002E6D"
                className="font-mono"
              />
            </div>
          </FieldWrapper>

          {simgeAlaniVar && (
            <FieldWrapper etiket="Simge" id="t-simge" ipucu="İsteğe bağlı ikon adı.">
              <Input id="t-simge" value={simge} onChange={(e) => setSimge(e.target.value)} />
            </FieldWrapper>
          )}

          <FieldWrapper etiket="Açıklama" id="t-aciklama">
            <Input id="t-aciklama" value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
          </FieldWrapper>

          {/* Önizleme: kaydetmeden önce rengin listede nasıl görüneceği */}
          <div className="rounded-md bg-sunken p-3">
            <p className="mb-2 text-2xs uppercase tracking-[0.06em] text-text-3">Önizleme</p>
            <span
              className="inline-flex h-6 items-center gap-1.5 rounded-full px-2.5 text-2xs font-semibold"
              style={{
                color: colorOr(renk),
                background: `color-mix(in srgb, ${colorOr(renk)} 14%, transparent)`,
              }}
            >
              <span className="h-[5px] w-[5px] rounded-full bg-current" aria-hidden />
              {ad || 'Tanım adı'}
            </span>
          </div>
        </div>
      </form>
    </FormModal>
  );
}

// ═════════════════════════════════════════════════ mahalle / meslek

function AdKaydiBolumu({
  tur,
  baslik,
  tekil,
  aciklama,
}: {
  tur: NameRecordType;
  baslik: string;
  tekil: string;
  aciklama: string;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const [sayfa, setSayfa] = useState(1);
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [ara, setAra] = useState('');
  const [yeniAd, setYeniAd] = useState('');
  const [duzenlenen, setDuzenlenen] = useState<NameRecord | null>(null);
  const [silinecek, setSilinecek] = useState<NameRecord | null>(null);
  const [iceAktarmaAcik, setIceAktarmaAcik] = useState(false);

  useEffect(() => {
    const z = setTimeout(() => {
      setAra(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const { data, isLoading } = useNameRecords(tur, { sayfa, boyut: 50, ara });

  const tazele = () => {
    qc.invalidateQueries({ queryKey: ['tanim'] });
    qc.invalidateQueries({ queryKey: ['ayar'] });
  };

  const ekle = useMutation({
    mutationFn: (ad: string) => api.post<NameRecord>(`/tanim/${tur}`, { ad }),
    onSuccess: () => {
      setYeniAd('');
      tazele();
      bildir('basari', 'Eklendi');
    },
    onError: (h: Error) => bildir('hata', 'Eklenemedi', h.message),
  });

  const guncelle = useMutation({
    mutationFn: (k: NameRecord) => api.put<NameRecord>(`/tanim/${tur}/${k.id}`, { ad: k.ad }),
    onSuccess: () => {
      setDuzenlenen(null);
      tazele();
      bildir('basari', 'Güncellendi');
    },
    onError: (h: Error) => bildir('hata', 'Güncellenemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/tanim/${tur}/${id}`),
    onSuccess: () => {
      setSilinecek(null);
      tazele();
      bildir('basari', 'Silindi');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  return (
    <div className="space-y-3.5">
      <div className="flex flex-col gap-2.5 sm:flex-row sm:items-center">
        <div>
          <h2 className="font-display text-lg font-bold">{baslik}</h2>
          <p className="text-sm text-text-3">{aciklama}</p>
        </div>
        <Button
          varyant="ikincil"
          className="sm:ml-auto"
          onClick={() => setIceAktarmaAcik((a) => !a)}
        >
          <Upload size={14} />
          Toplu ekle
        </Button>
      </div>

      {iceAktarmaAcik && (
        <TopluEkleme
          tur={tur}
          tekil={tekil}
          kapat={() => setIceAktarmaAcik(false)}
          bittiginde={tazele}
        />
      )}

      {/* Hızlı ekleme — tek satırlık kayıtlar için ayrı form sayfası fazla */}
      <Card className="flex gap-2 p-3">
        <label htmlFor="hizli-ekle" className="sr-only">
          Yeni {tekil}
        </label>
        <Input
          id="hizli-ekle"
          value={yeniAd}
          onChange={(e) => setYeniAd(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && yeniAd.trim()) {
              e.preventDefault();
              ekle.mutate(yeniAd.trim());
            }
          }}
          placeholder={`Yeni ${tekil} adı`}
        />
        <Button
          onClick={() => ekle.mutate(yeniAd.trim())}
          disabled={!yeniAd.trim() || ekle.isPending}
          className="shrink-0"
        >
          <Plus size={14} />
          Ekle
        </Button>
      </Card>

      <SearchInput
        value={aramaGirdisi}
        onChange={(e) => setAramaGirdisi(e.target.value)}
        placeholder={`${baslik} içinde ara`}
        aria-label={`${baslik} içinde ara`}
        ikon={<Search size={15} />}
        className="md:max-w-[320px]"
      />

      {isLoading ? (
        <SkeletonRows adet={6} />
      ) : (data?.veriler ?? []).length === 0 ? (
        <EmptyState ikon={MapPin} baslik={`${baslik} listesi boş`} />
      ) : (
        <>
          <Card className="divide-y divide-border">
            {(data?.veriler ?? []).map((k) => (
              <div key={k.id} className="flex items-center gap-3 px-3.5 py-2.5">
                {duzenlenen !== null && duzenlenen.id === k.id ? (
                  <>
                    <Input
                      value={duzenlenen.ad ?? ''}
                      onChange={(e) =>
                        setDuzenlenen((d) => (d ? { ...d, ad: e.target.value } : d))
                      }
                      className="h-8"
                      autoFocus
                    />
                    <Button
                      className="h-8 shrink-0 px-2.5 text-xs"
                      onClick={() => guncelle.mutate(duzenlenen)}
                      disabled={!duzenlenen.ad?.trim()}
                    >
                      Kaydet
                    </Button>
                    <Button
                      varyant="sade"
                      className="h-8 shrink-0 px-2.5 text-xs"
                      onClick={() => setDuzenlenen(null)}
                    >
                      Vazgeç
                    </Button>
                  </>
                ) : (
                  <>
                    <span className="min-w-0 flex-1 truncate text-sm">{k.ad}</span>
                    {(k.kullanimSayisi ?? 0) > 0 && (
                      <span className="shrink-0 rounded-full bg-sunken px-2 py-0.5 text-2xs tabular-nums text-text-3">
                        {number(k.kullanimSayisi)} talep
                      </span>
                    )}
                    <IconButton etiket="Düzenle" onClick={() => setDuzenlenen(k)}>
                      <Tag size={14} />
                    </IconButton>
                    <IconButton
                      etiket="Sil"
                      onClick={() => setSilinecek(k)}
                      className="hover:text-(--st-no)"
                    >
                      <Trash2 size={14} />
                    </IconButton>
                  </>
                )}
              </div>
            ))}
          </Card>

          <Pagination sonuc={data} sayfaDegistir={setSayfa} birim={tekil} />
        </>
      )}

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik={`${tekil} silinsin mi?`}
        aciklama={`"${silinecek?.ad}" kalıcı olarak silinecek.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

/**
 * Toplu ekleme.
 *
 * <p>
 * Eski arayüz `.txt` dosyası yüklüyordu. Burada hem dosya seçilebiliyor hem
 * doğrudan yapıştırılabiliyor — ayrıştırma istemcide, sunucu yalnızca satır
 * listesi görüyor. Aynı uç iki kullanım biçimine de hizmet ediyor.
 * </p>
 */
function TopluEkleme({
  tur,
  tekil,
  kapat,
  bittiginde,
}: {
  tur: NameRecordType;
  tekil: string;
  kapat: () => void;
  bittiginde: () => void;
}) {
  const { bildir } = useToast();
  const [metin, setMetin] = useState('');

  const satirlar = metin
    .split(/\r?\n/)
    .map((s) => s.trim())
    .filter(Boolean);

  const gonder = useMutation({
    mutationFn: () =>
      api.post<BulkImportResult>(`/tanim/${tur}/ice-aktar`, {
        satirlar,
        kopyalariAtla: true,
      }),
    onSuccess: (s) => {
      bildir('basari', 'İçe aktarıldı', s.mesaj ?? undefined);
      setMetin('');
      bittiginde();
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'İçe aktarılamadı', h.message),
  });

  async function dosyaOku(dosya: File) {
    const icerik = await dosya.text();
    setMetin(icerik);
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={`Toplu ${tekil} ekle`}
      aciklama="Her satıra bir ad. Var olanlar atlanır."
      ikon={<Upload size={15} />}
      genislik="orta"
      altBilgi={satirlar.length > 0 ? `${satirlar.length} satır okundu` : 'Henüz satır yok'}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            onClick={() => gonder.mutate()}
            disabled={satirlar.length === 0 || gonder.isPending}
          >
            <Upload size={14} />
            {satirlar.length} kaydı ekle
          </Button>
        </>
      }
    >
      <div className="space-y-3">
        <input
          type="file"
          accept=".txt,.csv,text/plain"
          onChange={(e) => {
            const d = e.target.files?.[0];
            if (d) void dosyaOku(d);
          }}
          className="block w-full text-sm text-text-2
            file:mr-3 file:rounded-control file:border file:border-border file:bg-surface-2
            file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-text-2"
          aria-label="Metin dosyası seç"
        />

        <textarea
          value={metin}
          onChange={(e) => setMetin(e.target.value)}
          placeholder={`Akdeğirmen\nAkkonak\nAlibaba…`}
          className="min-h-[140px] w-full rounded-md border border-border bg-surface-2 px-3.5 py-2.5 text-base outline-hidden focus:border-brand focus:ring-[3px] focus:ring-(--focus-ring)"
          aria-label="Eklenecek adlar"
        />

      </div>
    </FormModal>
  );
}
