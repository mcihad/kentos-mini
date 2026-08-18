import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PERMISSION } from '../../components/permissions';
import { Building2, ImagePlus, Lock, Newspaper, Save, Users, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { FieldWrapper, Textarea, Input, Secim } from '../../components/Field';
import { EmptyState } from '../../components/EmptyState';
import { Switch } from '../../components/Switch';
import { Button } from '../../components/Button';
import { Skeleton } from '../../components/Skeleton';
import { FormSection } from '../../components/FormSection';
import { colorOr } from '../../components/Color';
import { DatePicker } from '../../components/DatePicker';
import { useToast } from '../../components/Toast';
import { cn } from '../../components/utils';
import { clockToMinutes, TimeRangePicker } from '../../components/TimeRangePicker';
import { useSession } from '../../auth/SessionProvider';
import { queryKeys } from '../../data/queryKeys';
import { api } from '../../data/client';
import {
  PHOTO_MAX_BYTES,
  PHOTO_TYPES,
  uploadEventPhoto,
} from '../../data/photo';
import {
  useUnitUsers, useParticipantUnits, useEventStatuses, useEventTypes,
} from '../../data/hooks';
import { SCOPE, type Event } from '../../data/types';
import { serverToLocal, localToServer } from '../../data/time';
import { EMPTY_RECURRENCE, parseRrule, buildRrule, RecurrenceRule, type RecurrenceState } from '../agenda/RecurrenceRule';
import { ParticipantPicker } from './ParticipantPicker';

/** Takvimden gelen ön dolgu: tıklanan yarım saatlik dilim. */
export type BaslangicOnerisi = {
  /** `yyyy-MM-dd` */
  gun: string;
  /** `HH:mm` */
  bas: string;
  /** `HH:mm` */
  bit: string;
  tumGun?: boolean;
};

/**
 * Sunucu damgasını forma ayırır.
 *
 * Tarih ve saat AYRI tutulur: kullanıcı "hangi gün" ile "saat kaçta"yı ayrı
 * düşünüyor ve iki seçici de öyle çalışıyor. Tek `datetime-local` alanı hem
 * tarayıcıya göre değişiyordu hem Türkçe değildi.
 */
function parcala(sunucu?: string | null): { gun: string; saat: string } {
  if (!sunucu) return { gun: '', saat: '' };
  const t = localToServer(serverToLocal(sunucu));
  return { gun: t.slice(0, 10), saat: t.slice(11, 16) };
}

/** Bir sonraki yarım saat, 30 dakikalık etkinlik. */
export function varsayilanZaman(): BaslangicOnerisi {
  const simdi = new Date();
  simdi.setSeconds(0, 0);
  simdi.setMinutes(simdi.getMinutes() < 30 ? 30 : 60);
  const b = localToServer(simdi);
  return { gun: b.slice(0, 10), bas: b.slice(11, 16), bit: guneSigdir(simdi, 30) };
}

/** Takvim dilimini forma verilecek öneriye çevirir. */
export function dilimdenOneri(t: Date, sureDk = 30): BaslangicOnerisi {
  const b = localToServer(t);
  return { gun: b.slice(0, 10), bas: b.slice(11, 16), bit: guneSigdir(t, sureDk) };
}

/**
 * Bitiş saatini başlangıcın GÜNÜNE sığdırır.
 *
 * Etkinlik gövdesi bitişi her zaman başlangıcın gününe yazar
 * (`bitisTarihi: gun + bitSaat`). 23:30'dan sonra +30 dakika gece yarısını
 * aşıyor ve yalnızca saat dilimi alındığı için "23:30 – 00:00" üretiyordu:
 * bitişi başlangıcından ÖNCE, takvimde çizilemeyen bir kayıt. Gün taşarsa
 * bitiş 23:59'a kelepçelenir.
 */
function guneSigdir(baslangic: Date, sureDk: number): string {
  const bit = new Date(baslangic.getTime() + sureDk * 60_000);
  if (bit.getDate() !== baslangic.getDate()) return '23:59';
  return localToServer(bit).slice(11, 16);
}

/**
 * Etkinlik ekleme / düzenleme alanları.
 *
 * <p>
 * Kabuktan (sayfa mı, diyalog mu) bağımsızdır: <c>EtkinlikModal</c> bunu
 * diyalog içinde, <c>/ajanda/yeni</c> rotası da aynı diyalogla gösterir.
 * Böylece takvimde bir dilime tıklamakla menüden "Yeni etkinlik" demek
 * <b>aynı</b> formu açar.
 * </p>
 *
 * <p>
 * <b>Üç değişmez sunucuda zorlanıyor, form onları önden gösteriyor:</b>
 * gizli etkinlikte basın katılamaz, gizli etkinliğin katılımcısı olmalıdır
 * (yoksa oluşturan dışında kimse göremez) ve gizli etkinliği yalnızca yetkisi
 * olan kullanıcı oluşturabilir.
 * </p>
 */
export function EventFields({
  etkinlikId,
  oneri,
  onKaydedildi,
  onVazgec,
}: {
  /** Verilirse düzenleme, verilmezse yeni kayıt. */
  etkinlikId?: number | null;
  /** Yeni kayıtta ön dolgu — takvimden tıklanan dilim. */
  oneri?: BaslangicOnerisi | null;
  onKaydedildi: (id: number) => void;
  onVazgec: () => void;
}) {
  const duzenleme = typeof etkinlikId === 'number' && etkinlikId > 0;
  const kayitId = duzenleme ? etkinlikId! : 0;

  const qc = useQueryClient();
  const { bildir } = useToast();
  const { hasPermission } = useSession();

  const mevcut = useQuery({
    queryKey: queryKeys.event.detail(kayitId),
    queryFn: () => api.get<Event>(`/etkinlik/${kayitId}`),
    enabled: duzenleme,
  });

  const tipler = useEventTypes();
  const durumlar = useEventStatuses();
  const katilimciBirimler = useParticipantUnits();
  const birimKullanicilari = useUnitUsers();

  const z = useMemo(() => oneri ?? varsayilanZaman(), [oneri]);

  const [baslik, setBaslik] = useState('');
  const [aciklama, setAciklama] = useState('');
  const [konum, setKonum] = useState('');
  const [gun, setGun] = useState(z.gun);
  const [basSaat, setBasSaat] = useState(z.bas);
  const [bitSaat, setBitSaat] = useState(z.bit);
  const [tumGun, setTumGun] = useState(z.tumGun ?? false);
  const [tipId, setTipId] = useState<number | ''>('');
  const [durumId, setDurumId] = useState<number | ''>('');
  const [irtibatKisi, setIrtibatKisi] = useState('');
  const [irtibatTelefon, setIrtibatTelefon] = useState('');
  const [basinKatilsin, setBasinKatilsin] = useState(false);
  const [konusmaMetni, setKonusmaMetni] = useState(false);
  const [bilgiNotu, setBilgiNotu] = useState(false);
  const [gizli, setGizli] = useState(false);

  /**
   * İKİ AYRI LİSTE — birbirinin yerine geçmez.
   *
   * `katilimciBirimIdler`: etkinliğe KATILACAK departmanlar.
   * `gorebilecekler`: gizli etkinliği GÖREBİLECEK kişiler (kendi biriminden).
   *
   * Bir dönem tek liste vardı ve gizli bir toplantıya bir müdürlüğü davet
   * etmek, o müdürlükteki herkesi toplantının içeriğine ortak ediyordu.
   */
  const [katilimcilar, setKatilimcilar] = useState<number[]>([]);
  const [gorebilecekler, setGorebilecekler] = useState<number[]>([]);
  const [tekrar, setTekrar] = useState<RecurrenceState>(EMPTY_RECURRENCE);
  const [kapsam, setKapsam] = useState<number>(SCOPE.This);
  const [katilimciSecici, setKatilimciSecici] = useState(false);
  const [gorebilecekSecici, setGorebilecekSecici] = useState(false);

  /** Seçili katılımcıların tam kayıtları — çip listesinde gösterilir. */
  const secilenKisiler = useMemo(
    () => katilimciBirimler.liste.filter((b) => katilimcilar.includes(b.id!)),
    [katilimciBirimler.liste, katilimcilar],
  );

  const secilenGorebilecekler = useMemo(
    () => birimKullanicilari.liste.filter((k) => gorebilecekler.includes(k.id!)),
    [birimKullanicilari.liste, gorebilecekler],
  );

  /**
   * Gizlilik bölümü yalnızca yetkisi olana gösterilir.
   *
   * Zaten gizli olan bir kaydı düzenlerken bölüm HER HÂLÜKÂRDA görünür:
   * kullanıcı katılımcısı olduğu için kaydı açabiliyor ve alanın gizlenmesi
   * kaydı sessizce herkese açık hâle getirme riski taşır.
   */
  const gizliYetkisi = hasPermission(PERMISSION.ajandaGizliEtkinlik);
  const gizlilikGorunur = gizliYetkisi || gizli;

  /** Var olan kaydı forma yükle. */
  useEffect(() => {
    const e = mevcut.data;
    if (!e) return;

    setBaslik(e.baslik ?? '');
    setAciklama(e.aciklama ?? '');
    setKonum(e.konum ?? '');
    const b = parcala(e.baslangicTarihi);
    const s = parcala(e.bitisTarihi);
    setGun(b.gun);
    setBasSaat(b.saat);
    setBitSaat(s.saat);
    setTumGun(e.tumGun ?? false);
    setTipId(e.randevuTipId ?? '');
    setDurumId(e.durumId ?? '');
    setIrtibatKisi(e.irtibatKisi ?? '');
    setIrtibatTelefon(e.irtibatTelefon ?? '');
    setBasinKatilsin(e.basinKatilsin ?? false);
    setKonusmaMetni(e.konusmaMetniDurum ?? false);
    setBilgiNotu(e.bilgiNotuDurum ?? false);
    setGizli(e.gizli ?? false);
    // Okuma listesi İKİ TÜRÜ birlikte taşıyor; ayrım `birimId` ile:
    // dolu ise katılımcı birim, boş ise görebilecek kişi.
    setKatilimcilar(
      (e.katilimcilar ?? []).filter((k) => k.birimId != null).map((k) => k.birimId!),
    );
    setGorebilecekler(
      (e.katilimcilar ?? []).filter((k) => k.birimId == null).map((k) => k.id!),
    );
    setTekrar(parseRrule(e.tekrarKurali));
  }, [mevcut.data]);

  /** Varsayılan tip/durum: ilk kayıtlar — sunucu bu alanları NOT NULL istiyor. */
  useEffect(() => {
    if (!duzenleme && tipId === '' && tipler.liste.length > 0) setTipId(tipler.liste[0].id!);
  }, [duzenleme, tipId, tipler.liste]);
  useEffect(() => {
    if (!duzenleme && durumId === '' && durumlar.liste.length > 0) setDurumId(durumlar.liste[0].id!);
  }, [duzenleme, durumId, durumlar.liste]);

  /** Gizli işaretlenince basın katılımı otomatik kapanır (sunucu da reddeder). */
  useEffect(() => {
    if (gizli && basinKatilsin) setBasinKatilsin(false);
  }, [gizli, basinKatilsin]);

  const seriMi = (mevcut.data?.seriId ?? null) !== null;

  /**
   * Kayıttan ÖNCE seçilen fotoğraflar.
   *
   * Yükleme ucu etkinlik kimliği istiyor, yani dosyalar ancak kayıt
   * oluştuktan sonra gönderilebiliyor. Önce yalnızca detay ekranından
   * eklenebiliyordu: kullanıcı etkinliği kaydediyor, sonra detaya girip
   * fotoğrafları ayrıca yüklüyordu. Artık formda seçiliyor, kayıttan hemen
   * sonra gönderiliyor.
   */
  const [fotograflar, setFotograflar] = useState<File[]>([]);

  const kaydet = useMutation({
    mutationFn: () => {
      const govde: Record<string, unknown> = {
        id: kayitId,
        baslik,
        aciklama: aciklama || null,
        konum: konum || null,
        baslangicTarihi: `${gun}T${basSaat}:00`,
        bitisTarihi: bitSaat ? `${gun}T${bitSaat}:00` : null,
        tumGun,
        randevuTipId: tipId === '' ? null : tipId,
        durumId: durumId === '' ? null : durumId,
        irtibatKisi: irtibatKisi || null,
        irtibatTelefon: irtibatTelefon || null,
        basinKatilsin,
        konusmaMetniDurum: konusmaMetni,
        bilgiNotuDurum: bilgiNotu,
        gizli,
        katilimciBirimIdler: katilimcilar,
        // Gizlilik kapalıyken boş liste gönderilir; sunucu da temizliyor ama
        // istemcinin gönderdiği şey ekranda görünenle aynı olmalı.
        katilimciIdler: gizli ? gorebilecekler : [],
      };

      const kural = buildRrule(tekrar);

      if (duzenleme) {
        // Kural DEĞİŞMEDİYSE hiç gönderilmez. Aynı kuralı yeniden göndermek
        // sunucuda "kural değişti" yolunu tetikleyip seriyi bölebiliyor.
        const eskiKural = mevcut.data?.tekrarKurali ?? null;
        if (kural !== eskiKural) {
          govde.tekrar = kural ? { rrule: kural, sureDakika: sureDakika() } : null;
          govde.tekrarKaldir = seriMi && kural === null;
        }
        govde.kapsam = kapsam;
        return api.put<Event>(`/etkinlik/${kayitId}`, govde);
      }

      if (kural) govde.tekrar = { rrule: kural, sureDakika: sureDakika() };
      return api.post<Event>('/etkinlik', govde);
    },
    onSuccess: async (e) => {
      const id = e?.id ?? kayitId;

      /*
        Fotoğraf yüklemesi kaydı GERİ ALMAZ.

        Event oluştu; yükleme başarısız olursa (ağ koptu, dosya çok büyük)
        kullanıcıya ayrıca haber verilir ama kayıt yerinde kalır ve
        fotoğraflar detaydan eklenebilir. Yüklemeyi kaydın parçası saymak,
        tek bir büyük dosya yüzünden bütün etkinliği kaybettirirdi.
      */
      if (fotograflar.length > 0 && id) {
        try {
          await uploadEventPhoto(id, fotograflar);
        } catch (h) {
          bildir('uyari', 'Etkinlik kaydedildi, fotoğraflar yüklenemedi',
            (h as Error).message);
          qc.invalidateQueries({ queryKey: queryKeys.event.all() });
          onKaydedildi(id);
          return;
        }
      }

      qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      bildir('basari', duzenleme ? 'Etkinlik güncellendi' : 'Etkinlik oluşturuldu');
      onKaydedildi(id);
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  function sureDakika(): number {
    const b = clockToMinutes(basSaat);
    const s = clockToMinutes(bitSaat);
    if (b === null || s === null) return 30;
    const dk = s > b ? s - b : s + 1440 - b;
    return dk > 0 ? dk : 30;
  }

  // Gizli etkinlik için görebilecek kişi ZORUNLU DEĞİL: kimseyi seçmemek
  // "yalnızca ben göreceğim" demek ve bu geçerli bir kullanım. Önceden
  // katılımcı zorunluydu ama o liste artık davet listesi, görünürlük değil.
  /*
   * BİTİŞ, BAŞLANGIÇTAN ÖNCE OLAMAZ. Gövde bitişi başlangıcın gününe
   * yazdığı için 00:00'a sarmış bir bitiş "dünden önce biten" kayıt üretir;
   * sunucu bunu kabul ediyor (200) ve kayıt takvimde çizilemiyordu.
   * Elle seçilen ters aralık da aynı kapıdan döner.
   */
  const saatTers =
    !tumGun &&
    bitSaat.length > 0 &&
    (clockToMinutes(bitSaat) ?? 0) <= (clockToMinutes(basSaat) ?? 0);
  const gecerli =
    baslik.trim().length > 0 && gun.length > 0 && basSaat.length > 0 && !saatTers;

  if (duzenleme && mevcut.isLoading) {
    return (
      <div className="space-y-4 p-4">
        <Skeleton className="h-7 w-1/2" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (duzenleme && mevcut.isError) {
    return (
      <div className="p-4">
          <EmptyState
          ikon={Lock}
          baslik="Etkinlik bulunamadı"
          aciklama={(mevcut.error as Error)?.message}
          eylem={
            <Button varyant="ikincil" onClick={onVazgec}>
              Kapat
            </Button>
          }
        />
      </div>
    );
  }

  return (
    <form
      // Gövde kendi içinde kaydırılır, eylem çubuğu SABİT kalır: yarı saydam
      // bir çubuğun altından form alanlarının görünmesi kirli duruyordu ve
      // "Kaydet" uzun formda ekran dışında kalıyordu.
      className="flex min-h-0 flex-1 flex-col"
      onSubmit={(e) => {
        e.preventDefault();
        if (gecerli) kaydet.mutate();
      }}
    >
      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto p-4">
      {/* ── Temel bilgiler ── */}
      <FormSection baslik="Etkinlik bilgileri">
        <div className="space-y-4">
          {/*
            Başlıkta `maxLength` YOK: kolon `text`. Sınır bir dönem 100'dü ve
            sahada yetmiyordu — form kullanıcıyı sessizce durduruyor, yazdığı
            başlık ekranda tamamlanmıyordu. Sınırı yalnızca sunucudan
            kaldırmak yetmez; asıl duvar buradaydı.
          */}
          <FieldWrapper etiket="Başlık" id="e-baslik" zorunlu>
            <Input
              id="e-baslik"
              value={baslik}
              onChange={(e) => setBaslik(e.target.value)}
              autoFocus
            />
          </FieldWrapper>

          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Tarih" id="e-gun" zorunlu>
              <DatePicker id="e-gun" deger={gun} degistir={setGun} />
            </FieldWrapper>

            <FieldWrapper
              etiket="Saat aralığı"
              id="e-saat"
              hata={saatTers ? 'Bitiş, başlangıçtan sonra olmalı (etkinlik aynı gün içinde biter).' : undefined}
            >
              <TimeRangePicker
                id="e-saat"
                baslangic={basSaat}
                bitis={bitSaat}
                tumGun={tumGun}
                degistir={(b, s) => {
                  setBasSaat(b);
                  setBitSaat(s);
                }}
              />
            </FieldWrapper>
          </div>

          <Switch
            isaretli={tumGun}
            degistir={setTumGun}
            etiket="Tüm gün süren etkinlik"
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Etkinlik tipi" id="e-tip">
              <Secim
                id="e-tip"
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

            <FieldWrapper etiket="Durum" id="e-durum" ipucu="Takvimdeki rengi belirler.">
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
                  id="e-durum"
                  value={durumId}
                  onChange={(e) => setDurumId(e.target.value === '' ? '' : Number(e.target.value))}
                >
                  {durumlar.liste.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.ad}
                    </option>
                  ))}
                </Secim>
              </div>
            </FieldWrapper>
          </div>

          <FieldWrapper etiket="Konum" id="e-konum">
            <Input id="e-konum" value={konum} onChange={(e) => setKonum(e.target.value)} />
          </FieldWrapper>

          <FieldWrapper etiket="Açıklama" id="e-aciklama">
            <Textarea
              id="e-aciklama"
              value={aciklama}
              onChange={(e) => setAciklama(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </FormSection>

      {/* ── İrtibat ── */}
      <FormSection baslik="İrtibat">
        <div className="grid gap-4 sm:grid-cols-2">
          <FieldWrapper etiket="İrtibat kişisi" id="e-ikisi">
            <Input
              id="e-ikisi"
              value={irtibatKisi}
              onChange={(e) => setIrtibatKisi(e.target.value)}
            />
          </FieldWrapper>
          <FieldWrapper etiket="İrtibat telefonu" id="e-itel">
            <Input
              id="e-itel"
              type="tel"
              inputMode="tel"
              value={irtibatTelefon}
              onChange={(e) => setIrtibatTelefon(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </FormSection>

      {/* ── Hazırlık ── */}
      <FormSection baslik="Hazırlık" aciklama="Etkinlik öncesi tamamlanması gerekenler.">
        <div className="space-y-2.5">
          <label
            className={cn(
              'flex cursor-pointer items-center gap-2.5',
              gizli && 'cursor-not-allowed opacity-50',
            )}
          >
            <Switch
              isaretli={basinKatilsin}
              degistir={setBasinKatilsin}
              pasif={gizli}
              etiket={
                <span className="inline-flex items-center gap-1.5">
                  <Newspaper size={14} className="text-text-3" />
                  Basın katılacak
                </span>
              }
              aciklama={gizli ? 'Gizli etkinlikte kullanılamaz.' : undefined}
            />
          </label>

          <Switch
            isaretli={konusmaMetni}
            degistir={setKonusmaMetni}
            etiket="Konuşma metni hazırlanacak"
            aciklama="Metnin kendisi etkinlik detayından yazılır."
          />

          <Switch
            isaretli={bilgiNotu}
            degistir={setBilgiNotu}
            etiket="Bilgi notu hazırlanacak"
            aciklama="Notun kendisi etkinlik detayından yazılır."
          />
        </div>
      </FormSection>

      {/*
        ── Katılımcı birimler ──

        Etkinliğe KATILACAK departmanlar. Gizlilikten bağımsız: açık bir
        toplantının davetlilerini kaydetmek de anlamlı.
      */}
      <FormSection
        baslik="Katılımcı birimler"
        aciklama="Etkinliğe katılacak departmanlar. Kendi seviyeniz ve alt birimler seçilebilir."
      >
        <div>
          <div className="mb-2 flex items-center justify-between gap-3">
            <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
              Katılacak birimler
            </p>
            <Button
              type="button"
              varyant="ikincil"
              className="h-8 px-2.5 text-xs"
              onClick={() => setKatilimciSecici(true)}
              disabled={katilimciBirimler.liste.length === 0}
            >
              <Building2 size={13} />
              {katilimcilar.length > 0 ? 'Değiştir' : 'Birim seç'}
            </Button>
          </div>

          {katilimciBirimler.liste.length === 0 ? (
            <p className="text-sm text-text-3">Çağırabileceğiniz birim yok.</p>
          ) : secilenKisiler.length === 0 ? (
            <p className="text-sm text-text-3">Katılımcı birim seçilmedi.</p>
          ) : (
            <ul className="flex flex-wrap gap-1.5">
              {secilenKisiler.map((k) => (
                <li key={k.id}>
                  <span className="inline-flex items-center gap-1.5 rounded-full border border-border bg-surface-2 py-1 pl-1.5 pr-2.5 text-sm">
                    <span
                      className="grid h-6 w-6 place-items-center rounded-full bg-sunken text-text-3"
                      aria-hidden
                    >
                      <Building2 size={12} />
                    </span>
                    {k.ad}
                    <button
                      type="button"
                      aria-label={`${k.ad} katılımcılıktan çıkar`}
                      onClick={() => setKatilimcilar((s) => s.filter((x) => x !== k.id))}
                      className="ml-0.5 rounded-full p-0.5 text-text-3 transition-colors hover:text-(--st-no)"
                    >
                      <X size={12} />
                    </button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </FormSection>

      {/*
        ── Gizlilik ──

        Katılımcı birimlerden AYRI bir bölüm. Buradaki liste "kim görebilir"
        sorusunun cevabı; öteki "kim katılacak" sorusununki. İkisi bir arada
        durduğu sürece karışıyordu.
      */}
      {gizlilikGorunur && (
      <FormSection
        baslik="Gizlilik"
        aciklama="Gizli etkinliği yalnızca siz ve seçtiğiniz kişiler görebilir."
      >
        <div className="space-y-3.5">
          {/*
            Kutucuk YETKİSİ OLANA gösterilir; yetkisi olmayan bunu GÖRMEZ bile.
            Zaten gizli olan bir kaydı düzenlerken bölüm görünür kalır ama
            anahtar kilitlidir — alanı gizlemek, kaydı sessizce herkese açık
            hâle getirme riski taşır.
          */}
          <Switch
            isaretli={gizli}
            degistir={setGizli}
            pasif={!gizliYetkisi}
            etiket={
              <span className="inline-flex items-center gap-1.5">
                <Lock size={14} className="text-text-3" />
                Gizli etkinlik
              </span>
            }
            aciklama={
              gizliYetkisi ? undefined : 'Bu yetki sizde yok; gizlilik kaldırılamaz.'
            }
          />

          {gizli && (
            <>
              <p className="rounded-sm bg-(--st-wait-bg) px-3 py-2 text-xs leading-normal text-(--st-wait)">
                Gizli etkinlik havale edilemez, çiçek talimatı üretmez, birim SMS'i
                göndermez ve medya listesine girmez. Bildirim yalnızca aşağıdaki
                kişilere gider — <b>katılımcı birimlere gitmez</b>.
              </p>

              <div>
                <div className="mb-2 flex items-center justify-between gap-3">
                  <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
                    Görebilecek kişiler
                  </p>
                  <Button
                    type="button"
                    varyant="ikincil"
                    className="h-8 px-2.5 text-xs"
                    onClick={() => setGorebilecekSecici(true)}
                    disabled={birimKullanicilari.liste.length === 0}
                  >
                    <Users size={13} />
                    {gorebilecekler.length > 0 ? 'Değiştir' : 'Kişi seç'}
                  </Button>
                </div>

                {birimKullanicilari.liste.length === 0 ? (
                  <p className="text-sm text-text-3">
                    Biriminizde başka kullanıcı yok.
                  </p>
                ) : secilenGorebilecekler.length === 0 ? (
                  <p className="text-sm text-text-3">
                    Kimse seçilmedi — bu etkinliği yalnızca siz göreceksiniz.
                  </p>
                ) : (
                  <ul className="flex flex-wrap gap-1.5">
                    {secilenGorebilecekler.map((k) => (
                      <li key={k.id}>
                        <span className="inline-flex items-center gap-1.5 rounded-full border border-border bg-surface-2 py-1 pl-1.5 pr-2.5 text-sm">
                          <span
                            className="grid h-6 w-6 place-items-center rounded-full bg-sunken text-text-3"
                            aria-hidden
                          >
                            <Users size={12} />
                          </span>
                          {[k.ad, k.soyad].filter(Boolean).join(' ')}
                          <button
                            type="button"
                            aria-label={`${k.ad} listeden çıkar`}
                            onClick={() =>
                              setGorebilecekler((s) => s.filter((x) => x !== k.id))
                            }
                            className="ml-0.5 rounded-full p-0.5 text-text-3 transition-colors hover:text-(--st-no)"
                          >
                            <X size={12} />
                          </button>
                        </span>
                      </li>
                    ))}
                  </ul>
                )}

                <p className="mt-1.5 text-xs text-text-3">
                  Yalnızca kendi biriminizdeki kişiler seçilebilir.
                </p>
              </div>
            </>
          )}
        </div>
      </FormSection>
      )}

      {/*
        FOTOĞRAF — yalnızca YENİ kayıtta.

        Düzenlemede gösterilmiyor: orada zaten yüklenmiş fotoğraflar var ve
        detay ekranında silme/ekleme birlikte yönetiliyor. Formda ikinci bir
        yükleme kutusu, aynı işin iki yerden yapılması demekti.
      */}
      {!duzenleme && (
        <FormSection
          baslik="Fotoğraflar"
          aciklama="İsteğe bağlı · JPEG, PNG veya WEBP · dosya başına en fazla 5 MB"
        >
          <div>
            <label
              className="flex cursor-pointer items-center justify-center gap-2 rounded-control border border-dashed border-border
                bg-surface-2 px-4 py-5 text-sm text-text-2 transition-colors hover:border-brand-2 hover:text-text"
            >
              <ImagePlus size={16} className="text-text-3" />
              Fotoğraf seç
              <input
                type="file"
                multiple
                accept={PHOTO_TYPES}
                className="hidden"
                onChange={(e) => {
                  const secilen = Array.from(e.target.files ?? []);

                  // Boyut sınırı FORMDA da uygulanır: 30 MB'lık bir dosyayı
                  // yükleyip sunucudan 400 almak, kullanıcıyı bekletip sonra
                  // reddetmek demek.
                  const buyuk = secilen.filter((d) => d.size > PHOTO_MAX_BYTES);
                  if (buyuk.length > 0) {
                    bildir('uyari', 'Bazı dosyalar çok büyük',
                      `${buyuk.map((d) => d.name).join(', ')} · en fazla 5 MB`);
                  }

                  setFotograflar((s) => [
                    ...s,
                    ...secilen.filter((d) => d.size <= PHOTO_MAX_BYTES),
                  ]);
                  // Aynı dosya tekrar seçilebilsin diye girdi sıfırlanır.
                  e.target.value = '';
                }}
              />
            </label>

            {fotograflar.length > 0 && (
              <ul className="mt-3 grid grid-cols-3 gap-2 sm:grid-cols-4">
                {fotograflar.map((d, i) => (
                  <li key={`${d.name}-${i}`} className="relative">
                    <img
                      src={URL.createObjectURL(d)}
                      alt={d.name}
                      className="h-20 w-full rounded-control border border-border object-cover"
                    />
                    <button
                      type="button"
                      aria-label={`${d.name} kaldır`}
                      onClick={() => setFotograflar((s) => s.filter((_, x) => x !== i))}
                      className="absolute -right-1.5 -top-1.5 grid h-5 w-5 place-items-center rounded-full
                        border border-border bg-surface text-text-3 shadow-1 transition-colors hover:text-(--st-no)"
                    >
                      <X size={11} />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </FormSection>
      )}

      <ParticipantPicker
        acik={katilimciSecici}
        kapat={() => setKatilimciSecici(false)}
        kip="birim"
        ogeler={katilimciBirimler.liste}
        secili={katilimcilar}
        degistir={setKatilimcilar}
      />

      <ParticipantPicker
        acik={gorebilecekSecici}
        kapat={() => setGorebilecekSecici(false)}
        kip="kisi"
        ogeler={birimKullanicilari.liste}
        secili={gorebilecekler}
        degistir={setGorebilecekler}
      />

      {/* ── Tekrar ── */}
      <RecurrenceRule deger={tekrar} degistir={setTekrar} />

      {/* ── Kapsam (yalnızca seri düzenlemede) ── */}
      {duzenleme && seriMi && (
        <FormSection
          baslik="Değişiklik kapsamı"
          aciklama="Bu etkinlik bir tekrar serisinin parçası."
        >
          <div className="space-y-2">
            {[
              { d: SCOPE.This, e: 'Yalnızca bu etkinlik', a: 'Seri değişmez; bu tekrar seriden ayrılır.' },
              { d: SCOPE.ThisAndFollowing, e: 'Bu ve sonraki etkinlikler', a: 'Seri bu tarihten itibaren ikiye bölünür.' },
              { d: SCOPE.All, e: 'Tüm seri', a: 'Bireysel düzenlenmiş tekrarlar korunur.' },
            ].map((s) => (
              <label
                key={s.d}
                className={cn(
                  'flex cursor-pointer items-start gap-2.5 rounded-control border p-2.5 transition-colors',
                  kapsam === s.d ? 'border-brand bg-brand-tint' : 'border-border hover:bg-surface-2',
                )}
              >
                <input
                  type="radio"
                  name="kapsam"
                  checked={kapsam === s.d}
                  onChange={() => setKapsam(s.d)}
                  className="mt-0.5 h-[16px] w-[16px] accent-(--brand)"
                />
                <span>
                  <span className="block text-sm font-medium">{s.e}</span>
                  <span className="block text-xs text-text-3">{s.a}</span>
                </span>
              </label>
            ))}
          </div>
        </FormSection>
      )}

      </div>

      {/* ── Eylemler ── */}
      <div className="flex shrink-0 justify-end gap-2 border-t border-border bg-surface px-4 py-3">
        <Button type="button" varyant="ikincil" onClick={onVazgec}>
          Vazgeç
        </Button>
        <Button type="submit" disabled={!gecerli || kaydet.isPending}>
          <Save size={14} />
          {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
        </Button>
      </div>
    </form>
  );
}
