import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ClipboardList, FileUp, Save, User,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Switch } from '../components/Switch';
import { FieldWrapper, Textarea, Input, Secim } from '../components/Field';
import { SearchSelect } from '../components/SearchSelect';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { FormModal } from '../components/FormModal';
import { Skeleton } from '../components/Skeleton';
import { FormSection } from '../components/FormSection';
import { colorOr } from '../components/Color';
import { DatePicker } from '../components/DatePicker';
import { useToast } from '../components/Toast';
import { queryKeys } from '../data/queryKeys';
import { unitLabel } from '../data/format';
import { api } from '../data/client';
import {
  useUnits, useNeighborhoodSearch, useOccupationSearch, useRequestStatuses, useEventTypes,
} from '../data/hooks';
import type { Request } from '../data/types';
import { serverToLocal, localToServer } from '../data/time';

/** Sunucu damgasından `yyyy-MM-dd`. */
function gun(sunucu?: string | null): string {
  if (!sunucu) return '';
  return localToServer(serverToLocal(sunucu)).slice(0, 10);
}

/**
 * Talep ekleme / düzenleme.
 *
 * <p>
 * Tam sayfa — etkinlikten farklı olarak <b>diyalog değil</b>: talep formu
 * vatandaş karşısında doldurulan uzun bir kayıt (kimlik, iletişim, adres,
 * konu, özgeçmiş) ve mobilde klavye açıkken diyalogda kaydet düğmesi ekran
 * dışında kalıyor.
 * </p>
 *
 * <p>
 * Mahalle ve meslek <b>sunucuda aranır</b> (bkz. <c>AramaSecici</c>): mahalle
 * listesi binlerce satır, hepsini indirmek ilk açılışı bekletiyordu.
 * </p>
 */
export default function RequestFormPage() {
  const { id } = useParams();
  const duzenleme = id !== undefined && id !== 'yeni';
  const talepId = duzenleme ? Number(id) : 0;

  const gezin = useNavigate();
  const qc = useQueryClient();
  const { bildir } = useToast();

  const mevcut = useQuery({
    queryKey: queryKeys.request.detail(talepId),
    queryFn: () => api.get<Request>(`/talep/${talepId}`),
    enabled: duzenleme,
  });

  const durumlar = useRequestStatuses();
  const tipler = useEventTypes();
  const birimler = useUnits();

  const [mahalleArama, setMahalleArama] = useState('');
  const [meslekArama, setMeslekArama] = useState('');
  const mahalleler = useNeighborhoodSearch(mahalleArama);
  const meslekler = useOccupationSearch(meslekArama);

  const [ad, setAd] = useState('');
  const [soyad, setSoyad] = useState('');
  const [telefon, setTelefon] = useState('');
  const [email, setEmail] = useState('');
  const [meslek, setMeslek] = useState('');
  const [mahalleId, setMahalleId] = useState<number | null>(null);
  const [mahalleAd, setMahalleAd] = useState<string | null>(null);
  const [adres, setAdres] = useState('');
  const [konu, setKonu] = useState('');
  const [aciklama, setAciklama] = useState('');
  const [yer, setYer] = useState('');
  const [koordinat, setKoordinat] = useState('');
  const [baslangic, setBaslangic] = useState('');
  const [bitis, setBitis] = useState('');
  const [tipId, setTipId] = useState<number | ''>('');
  const [durumId, setDurumId] = useState<number | ''>('');
  const [birimId, setBirimId] = useState<number | ''>('');
  const [ozgecmis, setOzgecmis] = useState(false);

  /** Var olan kaydı forma yükle. */
  useEffect(() => {
    const t = mevcut.data;
    if (!t) return;

    setAd(t.ad ?? '');
    setSoyad(t.soyad ?? '');
    setTelefon(t.telefon ?? '');
    setEmail(t.email ?? '');
    setMeslek(t.meslek ?? '');
    setMahalleId(t.mahalleId ?? null);
    setAdres(t.adres ?? '');
    setKonu(t.konu ?? '');
    setAciklama(t.aciklama ?? '');
    setYer(t.yer ?? '');
    setKoordinat(t.koordinat ?? '');
    setBaslangic(gun(t.baslangicTarih));
    setBitis(gun(t.bitisTarih));
    setTipId(t.randevuTipId ?? '');
    setDurumId(t.randevuDurumId ?? '');
    setBirimId(t.birimId ?? '');
    setOzgecmis(t.ozgecmisDurum ?? false);
  }, [mevcut.data]);

  /** Yeni kayıtta ilk durum/tip seçili gelsin — sunucu bunları bekliyor. */
  useEffect(() => {
    if (!duzenleme && durumId === '' && durumlar.liste.length > 0) setDurumId(durumlar.liste[0].id!);
  }, [duzenleme, durumId, durumlar.liste]);
  useEffect(() => {
    if (!duzenleme && tipId === '' && tipler.liste.length > 0) setTipId(tipler.liste[0].id!);
  }, [duzenleme, tipId, tipler.liste]);

  const kaydet = useMutation({
    mutationFn: () => {
      const govde: Record<string, unknown> = {
        id: talepId,
        konu,
        ad,
        soyad: soyad || null,
        meslek: meslek || null,
        telefon: telefon || null,
        email: email || null,
        adres: adres || null,
        yer: yer || null,
        koordinat: koordinat || null,
        aciklama: aciklama || null,
        // Saat taşımayan alanlar; sunucu damgası kayan yerel saat.
        baslangicTarih: baslangic ? `${baslangic}T00:00:00` : null,
        bitisTarih: bitis ? `${bitis}T00:00:00` : null,
        mahalleId,
        randevuTipId: tipId === '' ? null : tipId,
        randevuDurumId: durumId === '' ? null : durumId,
        birimId: birimId === '' ? null : birimId,
        ozgecmisDurum: ozgecmis,
      };

      return duzenleme
        ? api.put<Request>(`/talep/${talepId}`, govde)
        : api.post<Request>('/talep', govde);
    },
    onSuccess: (t) => {
      qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      bildir('basari', duzenleme ? 'Talep güncellendi' : 'Talep oluşturuldu');
      gezin(`/talepler/${t?.id ?? talepId}`);
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const gecerli = konu.trim().length > 0 && ad.trim().length > 0;

  if (duzenleme && mevcut.isLoading) {
    return (
      <div className="mx-auto w-full max-w-[900px] space-y-4">
        <Skeleton className="h-7 w-1/2" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (duzenleme && mevcut.isError) {
    return (
      <EmptyState
        ikon={User}
        baslik="Talep bulunamadı"
        aciklama={(mevcut.error as Error)?.message}
        eylem={
          <Button varyant="ikincil" onClick={() => gezin('/talepler')}>
            Taleplere dön
          </Button>
        }
      />
    );
  }

  const kapat = () => gezin(duzenleme ? `/talepler/${talepId}` : '/talepler');

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={duzenleme ? 'Talebi düzenle' : 'Yeni talep'}
      aciklama="Vatandaş bilgileri ve talebin konusu."
      ikon={<ClipboardList size={15} />}
      genislik="genis"
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
            <Save size={14} />
            {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
          </Button>
        </>
      }
    >
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          if (gecerli) kaydet.mutate();
        }}
      >

      {/* ── Başvuran ── */}
      <FormSection baslik="Başvuran">
        <div className="grid gap-4 sm:grid-cols-2">
          <FieldWrapper etiket="Ad" id="t-ad" zorunlu>
            <Input id="t-ad" value={ad} onChange={(e) => setAd(e.target.value)} autoFocus />
          </FieldWrapper>

          <FieldWrapper etiket="Soyad" id="t-soyad">
            <Input id="t-soyad" value={soyad} onChange={(e) => setSoyad(e.target.value)} />
          </FieldWrapper>

          <FieldWrapper etiket="Telefon" id="t-tel" ipucu="05551112233 biçiminde">
            <Input
              id="t-tel"
              type="tel"
              inputMode="tel"
              value={telefon}
              onChange={(e) => setTelefon(e.target.value)}
            />
          </FieldWrapper>

          <FieldWrapper etiket="E-posta" id="t-eposta">
            <Input
              id="t-eposta"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </FieldWrapper>

          <FieldWrapper etiket="Meslek" id="t-meslek">
            {/* Meslek metin olarak saklanıyor; seçici yalnızca yazımı
                birleştirmek için — listede olmayan bir meslek de yazılabilir. */}
            <SearchSelect
              id="t-meslek"
              deger={null}
              seciliAd={meslek || null}
              degistir={(_, secilenAd) => setMeslek(secilenAd ?? '')}
              ogeler={meslekler.liste.map((m) => ({ id: m.id!, ad: m.ad! }))}
              ara={meslekArama}
              araDegistir={setMeslekArama}
              yukleniyor={meslekler.isFetching}
              yerTutucu="Meslek seçin"
            />
          </FieldWrapper>

          <FieldWrapper etiket="Mahalle" id="t-mahalle">
            <SearchSelect
              id="t-mahalle"
              deger={mahalleId}
              seciliAd={mahalleAd}
              degistir={(secilenId, secilenAd) => {
                setMahalleId(secilenId);
                setMahalleAd(secilenAd);
              }}
              ogeler={mahalleler.liste.map((m) => ({ id: m.id!, ad: m.ad! }))}
              ara={mahalleArama}
              araDegistir={setMahalleArama}
              yukleniyor={mahalleler.isFetching}
              yerTutucu="Mahalle seçin"
            />
          </FieldWrapper>

          <div className="sm:col-span-2">
            <FieldWrapper etiket="Adres" id="t-adres">
              <Input id="t-adres" value={adres} onChange={(e) => setAdres(e.target.value)} />
            </FieldWrapper>
          </div>
        </div>
      </FormSection>

      {/* ── Request ── */}
      <FormSection baslik="Talep bilgileri">
        <div className="space-y-4">
          <FieldWrapper etiket="Konu" id="t-konu" zorunlu>
            <Input
              id="t-konu"
              value={konu}
              onChange={(e) => setKonu(e.target.value)}
              maxLength={200}
            />
          </FieldWrapper>

          <FieldWrapper etiket="Açıklama" id="t-aciklama">
            <Textarea
              id="t-aciklama"
              value={aciklama}
              onChange={(e) => setAciklama(e.target.value)}
            />
          </FieldWrapper>

          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Talep tipi" id="t-tip">
              <Secim
                id="t-tip"
                value={tipId}
                onChange={(e) => setTipId(e.target.value === '' ? '' : Number(e.target.value))}
              >
                {tipler.liste.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.ad}
                  </option>
                ))}
              </Secim>
            </FieldWrapper>

            <FieldWrapper etiket="Durum" id="t-durum" ipucu="Listedeki rengi belirler.">
              <div className="flex items-center gap-2.5">
                <span
                  className="h-9 w-2 shrink-0 rounded-full"
                  style={{
                    background: colorOr(
                      durumlar.liste.find((d) => d.id === durumId)?.renk,
                      'var(--border-2)',
                    ),
                  }}
                  aria-hidden
                />
                <Secim
                  id="t-durum"
                  value={durumId}
                  onChange={(e) => setDurumId(e.target.value === '' ? '' : Number(e.target.value))}
                >
                  {durumlar.liste.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.durumAd}
                    </option>
                  ))}
                </Secim>
              </div>
            </FieldWrapper>

            <FieldWrapper etiket="İlgili birim" id="t-birim">
              <Secim
                id="t-birim"
                value={birimId}
                onChange={(e) => setBirimId(e.target.value === '' ? '' : Number(e.target.value))}
              >
                <option value="">Seçilmedi</option>
                {birimler.liste.map((b) => (
                  <option key={b.id} value={b.id}>
                    {unitLabel(b)}
                  </option>
                ))}
              </Secim>
            </FieldWrapper>

            <FieldWrapper etiket="Yer" id="t-yer">
              <Input id="t-yer" value={yer} onChange={(e) => setYer(e.target.value)} />
            </FieldWrapper>

            <FieldWrapper etiket="Başlangıç tarihi" id="t-bas">
              <DatePicker id="t-bas" deger={baslangic} degistir={setBaslangic} temizlenebilir />
            </FieldWrapper>

            <FieldWrapper
              etiket="Bitiş tarihi"
              id="t-bit"
              hata={bitis && baslangic && bitis < baslangic ? 'Bitiş başlangıçtan önce olamaz.' : undefined}
            >
              <DatePicker
                id="t-bit"
                deger={bitis}
                degistir={setBitis}
                enAz={baslangic || undefined}
                temizlenebilir
              />
            </FieldWrapper>

            <div className="sm:col-span-2">
              <FieldWrapper
                etiket="Koordinat"
                id="t-koordinat"
                ipucu="Haritada göstermek için: 39.7477,37.0179"
              >
                <Input
                  id="t-koordinat"
                  value={koordinat}
                  onChange={(e) => setKoordinat(e.target.value)}
                  placeholder="enlem,boylam"
                />
              </FieldWrapper>
            </div>
          </div>

          <Switch
            isaretli={ozgecmis}
            degistir={setOzgecmis}
            etiket={
              <span className="inline-flex items-center gap-1.5">
                <FileUp size={14} className="text-text-3" />
                Özgeçmiş istendi
              </span>
            }
            aciklama="Belge, kayıt açıldıktan sonra detay ekranından yüklenir."
          />
        </div>
      </FormSection>

      </form>
    </FormModal>
  );
}
