import { useSession } from '../auth/SessionProvider';
import { PERMISSION } from '../components/permissions';
import * as Tabs from '@radix-ui/react-tabs';
import { SekmeListesi, SekmeTetigi } from '../components/Tabs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, ArrowLeft, Building2, CalendarDays, Camera, Check, Clock, FileText, Flower2, History, Info, Lock, MapPin, MessageSquarePlus, Newspaper, Pencil, Plus, Repeat, Trash2, Users } from 'lucide-react';
import { OverlayShell } from '../components/OverlayShell';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Textarea } from '../components/Field';
import { NoteComposer } from '../components/NoteComposer';
import { EmptyState } from '../components/EmptyState';
import { Button, IconButton } from '../components/Button';
import { Skeleton } from '../components/Skeleton';
import { cn } from '../components/utils';
import { Accordion, AccordionSection } from '../components/Accordion';
import { Card } from '../components/Card';
import { ImageViewer, useImageViewer } from '../components/ImageViewer';
import { useToast } from '../components/Toast';
import { DegisiklikSatiri, Timeline } from '../components/Timeline';
import { queryKeys } from '../data/queryKeys';
import { range, dayName, date, dateTime } from '../data/format';
import { api } from '../data/client';
import { uploadEventPhoto } from '../data/photo';
import { toMap, useFlorists, useEventStatuses, useEventTypes, useParticipantUnits } from '../data/hooks';
import { EventActions } from './agenda/EventActions';
import { ParticipantPicker } from './event/ParticipantPicker';
import {
  EVENT_ACTIVITY_LABELS, STATUS,
  type Event, type EventPhoto, type EventNote, type EventActivity,
} from '../data/types';

/**
 * Etkinlik detayı.
 *
 * Gizlilik ve tekrar kuralları SUNUCUDA; bu ekran onları yalnızca gösterir.
 * Örneğin gizli bir etkinlik buraya kadar geldiyse kullanıcı onu görmeye
 * zaten yetkilidir — burada ek bir kontrol YOK, olsaydı iki ayrı doğruluk
 * kaynağı olurdu.
 */
export default function EventDetail() {
  // Eylem düğmeleri izne bağlı.
  const { me, hasPermission } = useSession();

  const { id } = useParams();
  const etkinlikId = Number(id);
  const gezin = useNavigate();

  const etkinlik = useQuery({
    queryKey: queryKeys.event.detail(etkinlikId),
    queryFn: () => api.get<Event>(`/etkinlik/${etkinlikId}`),
    enabled: Number.isFinite(etkinlikId),
  });

  const qc = useQueryClient();
  const { bildir } = useToast();
  const [katilimciSecici, setKatilimciSecici] = useState(false);
  const katilimciBirimler = useParticipantUnits();

  /**
   * Katılımcı birimleri kaydeder.
   *
   * Yalnızca katılımcı listesi gönderilir; diğer alanlar `undefined` kalınca
   * sunucu onlara DOKUNMUYOR. Tam kaydı geri göndermek, detay ekranında
   * görünmeyen bir alanı (tekrar kuralı gibi) yanlışlıkla ezme riski taşırdı.
   */
  const katilimciKaydet = useMutation({
    mutationFn: (birimIdler: number[]) =>
      api.put(`/etkinlik/${etkinlikId}`, {
        ...etkinlik.data,
        katilimciBirimIdler: birimIdler,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['etkinlik'] });
      setKatilimciSecici(false);
      bildir('basari', 'Katılımcılar güncellendi');
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const tipler = useEventTypes();
  const durumlar = useEventStatuses();
  const tipHaritasi = toMap(tipler.liste, (t) => t.id!);
  const durumHaritasi = toMap(durumlar.liste, (d) => d.id!);

  const cicekciler = useFlorists();

  // Okuma listesi iki türü birlikte taşıyor; ayrım `birimId` ile.
  const katilimciBirimListesi = (etkinlik.data?.katilimcilar ?? []).filter(
    (k) => k.birimId != null,
  );
  const gorebilecekListesi = (etkinlik.data?.katilimcilar ?? []).filter(
    (k) => k.birimId == null,
  );

  if (etkinlik.isLoading) return <DetayIskeleti />;

  if (etkinlik.isError || !etkinlik.data) {
    return (
      <EmptyState
        ikon={CalendarDays}
        baslik="Etkinlik bulunamadı"
        aciklama={
          (etkinlik.error as Error)?.message ?? 'Kayıt silinmiş veya erişiminiz yok olabilir.'
        }
        eylem={
          <Button varyant="ikincil" onClick={() => gezin('/ajanda')}>
            Ajandaya dön
          </Button>
        }
      />
    );
  }

  const e = etkinlik.data;
  const durum = durumHaritasi.get(e.durumId ?? -1);
  const tip = tipHaritasi.get(e.randevuTipId ?? -1);

  /**
   * Kayıt BAŞKA bir birimin ajandasında — biz yalnızca katılımcıyız.
   *
   * Davet edilen birim etkinliği kendi kayıtlarıyla yan yana görüyordu ve
   * ayırt edemiyordu; rozet bunu söylüyor, "Geçmiş" sekmesi de bu durumda
   * çizilmiyor.
   */
  const baskaBirimin =
    e.birimId != null && me?.birimId != null && e.birimId !== me.birimId;

  return (
    <div className="space-y-4">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-3">
        <Link to="/ajanda" aria-label="Ajandaya dön" className="mt-0.5 shrink-0">
          <IconButton etiket="Geri">
            <ArrowLeft size={17} />
          </IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-xl font-bold leading-[1.3] tracking-[-0.015em] metin-guzel md:text-2xl">
            {e.baslik}
          </h2>
          <div className="mt-1.5 flex flex-wrap items-center gap-2">
            {durum && (
              /*
                Şartname §7.3: durum etiketi KÖŞELİ (`radius/sm`) ve renk tek
                başına bilgi taşımaz — yanına nokta gelir. Tam yuvarlak hap
                biçimi süzgeç çiplerinin dili; ikisini aynı biçimde çizmek
                "tıklanabilir süzgeç" ile "kaydın durumu"nu karıştırıyordu.
              */
              <span
                className="inline-flex h-6 items-center gap-1.5 rounded-sm px-2 text-2xs font-semibold"
                style={{
                  color: durum.renk || 'var(--text-2)',
                  background: `color-mix(in srgb, ${durum.renk || 'var(--border-2)'} 14%, transparent)`,
                }}
              >
                <span className="h-[5px] w-[5px] rounded-full bg-current" aria-hidden />
                {durum.ad}
              </span>
            )}
            {/*
              STATÜ ÇİPİ, DURUMU TEKRARLAMIYORSA çizilir.

              İki ayrı kavram: `durum` kurumun kendi tanımladığı liste
              (Beklemede · Onaylandı · Devam Ediyor…), `status` ise sistemin
              üç sabit değeri. Çoğu kayıtta ikisi aynı kelimeye düşüyor ve
              başlığın altında yan yana İKİ "Beklemede" çipi çıkıyordu —
              okuyan kişiye hiçbir şey söylemeyen, yalnızca yer kaplayan bir
              tekrar. Karşılaştırma Türkçe küçük harfle: "İptal Edildi" ile
              "İptal edildi" aynı şeydir.
            */}
            {!ayniAd(durum?.ad, statuAdi(e.status ?? STATUS.Pending)) && (
              <StatuRozeti statu={e.status ?? STATUS.Pending} />
            )}
            {e.gizli && (
              <span className="inline-flex items-center gap-1 rounded-full bg-sunken px-2.5 py-0.5 text-2xs font-semibold text-text-2">
                <Lock size={10} />
                Gizli
              </span>
            )}
            {e.seriId && (
              <span className="inline-flex items-center gap-1 rounded-full bg-sunken px-2.5 py-0.5 text-2xs font-semibold text-text-2">
                <Repeat size={10} />
                {e.seriAyrik ? 'Seriden ayrı' : 'Tekrar eden'}
              </span>
            )}
            {/*
              BAŞKA BİR BİRİMİN KAYDI. Davet edilen birim etkinliği kendi
              kayıtlarıyla yan yana görüyor; hangisinin kendi işi olduğu
              ayırt edilemiyordu. Rozet yalnızca sahip birim farklıysa çıkar.
            */}
            {baskaBirimin && (
              <span
                className="inline-flex items-center gap-1 rounded-full border border-(--gold) px-2.5 py-0.5 text-2xs font-semibold text-text-2"
                title="Katılımcı olarak davet edildiniz; kayıt bu birimin ajandasında"
              >
                <Building2 size={10} />
                {e.birimAd ?? 'Başka birim'} ajandası
              </span>
            )}
          </div>
        </div>
      </div>

      {/* ── Zaman şeridi ── */}
      <Card className="relative overflow-hidden p-4">
        <span
          aria-hidden
          className="absolute inset-y-0 left-0 w-[3px]"
          style={{ background: durum?.renk || 'var(--gold)' }}
        />
        {/*
          Tarih ve saat BÜYÜK: bu ekranın tek sorusu "ne zaman". Önceden
          diğer alanlarla aynı puntodaydı ve göz onları ayırt edemiyordu.
        */}
        <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
          <span className="inline-flex items-baseline gap-2">
            <CalendarDays size={17} className="translate-y-[2px] text-brand-2" />
            <span className="font-baslik text-xl font-semibold leading-tight tracking-[-0.01em]">
              {date(e.baslangicTarihi)}
            </span>
            <span className="text-sm text-text-3">{dayName(e.baslangicTarihi)}</span>
          </span>

          <span className="inline-flex items-baseline gap-2">
            <Clock size={17} className="translate-y-[2px] text-brand-2" />
            <span className="font-baslik text-xl font-semibold leading-tight tabular-nums tracking-[-0.01em]">
              {range(e.baslangicTarihi, e.bitisTarihi, e.tumGun ?? false)}
            </span>
          </span>
        </div>

        <div className="mt-2.5 flex flex-wrap items-center gap-x-5 gap-y-2 text-sm">
          {e.konum && (
            <span className="inline-flex min-w-0 items-center gap-1.5">
              <MapPin size={14} className="shrink-0 text-text-3" />
              <span className="truncate">{e.konum}</span>
            </span>
          )}
          {tip && (
            <span className="inline-flex items-center gap-1.5 text-text-2">
              <FileText size={14} className="text-text-3" />
              {tip.ad}
            </span>
          )}
        </div>

        {/*
          Alan adı `tekrarOzeti` — İngilizce `recurrenceSummary` DEĞİL.

          Ekran yıllardır olmayan bir alanı okuyordu: koşul her zaman
          yanlıştı ve tekrar rozeti HİÇ görünmüyordu. Üretilen tipler bayat
          olduğu için derleyici de susuyordu; iş takip modülü için tipler
          yenilenince ortaya çıktı.
        */}
        {e.tekrarOzeti && (
          <p className="mt-3 inline-flex items-center gap-1.5 rounded-sm bg-sunken px-2.5 py-1.5 text-xs text-text-2">
            <Repeat size={12} className="text-text-3" />
            {e.tekrarOzeti}
            {e.tekrarBitisi && ` · ${date(e.tekrarBitisi)} tarihine kadar`}
          </p>
        )}
      </Card>

      {/*
        ── Eylemler ──

        SİLİNMİŞ etkinlikte hiçbir eylem gösterilmez. Detay salt okunur
        açılabiliyor (arşivden bir kayda dokunulduğunda "bulunamadı" demek
        yerine içeriği göstermek doğru), ama üzerinde düzenleme, havale,
        çiçek, SMS ya da durum değişikliği yapılamaz — sunucu da yazma
        yollarında silinmiş kaydı görmüyor, buradaki gizleme kullanıcıyı
        reddedilecek bir işlemden kurtarıyor.
      */}
      {e.isDeleted ? (
        <div className="flex items-start gap-2.5 rounded-card border border-border bg-surface-2 p-3.5">
          <Trash2 size={16} className="mt-0.5 shrink-0 text-text-3" />
          <div>
            <p className="text-sm font-semibold">Bu etkinlik silinmiş</p>
            <p className="text-sm leading-normal text-text-2 metin-guzel">
              Kayıt geçmiş amacıyla görüntüleniyor. Üzerinde düzenleme yapılamaz.
            </p>
          </div>
        </div>
      ) : (
        <EventActions etkinlik={e} cicekciler={cicekciler.liste} />
      )}

      {/* Kayıt BAŞKA bir birimin ajandasında mı? */}
      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_300px]">
        <Tabs.Root defaultValue="ozet">
          {/*
            SEKMELER TAM GENİŞLİK HAP.

            Alt çizgili şerit telefonda sola yaslanıp sağda boşluk bırakıyor,
            dördüncü sekme de kırpılıyordu. Uygulamanın geri kalanı
            `Sekmeler` gramerini kullanıyor (eşit paylaşan haplar, taşarsa
            kayan şerit, seçili sekme kendi kendine görünür alana geliyor);
            detay sayfası tek başına farklı duruyordu.
          */}
          <SekmeListesi etiket="Etkinlik bölümleri">
            {[
              { d: 'ozet', e: 'Özet' },
              { d: 'notlar', e: 'Notlar' },
              { d: 'fotograflar', e: 'Fotoğraflar' },
              // GEÇMİŞ YALNIZCA SAHİP BİRİME. "Kim neyi değiştirdi, kim
              // erteledi, kime havale etti" dökümü kaydın sahibinin iç kaydı;
              // davet edilen birim etkinliği görür ve not ekler ama bu dökümü
              // okumaz (sunucu da boş liste döndürüyor).
              ...(baskaBirimin ? [] : [{ d: 'gecmis', e: 'Geçmiş' }]),
            ].map((s) => (
              <SekmeTetigi key={s.d} deger={s.d}>
                {s.e}
              </SekmeTetigi>
            ))}
          </SekmeListesi>

          <Tabs.Content value="ozet" className="pt-4">
            <Ozet etkinlik={e} />
          </Tabs.Content>
          <Tabs.Content value="notlar" className="pt-4">
            <Notlar etkinlikId={etkinlikId} />
          </Tabs.Content>
          <Tabs.Content value="fotograflar" className="pt-4">
            <Fotograflar etkinlikId={etkinlikId} />
          </Tabs.Content>
          <Tabs.Content value="gecmis" className="pt-4">
            <Gecmis etkinlikId={etkinlikId} />
          </Tabs.Content>
        </Tabs.Root>

        {/*
          ── Katılımcılar ve metaveri ──

          İkisi de KAPALI başlar: bu ekranın asıl işi etkinliğin kendisi.
          Katılımcı listesi ve "kim ekledi" bilgisi merak edilince açılıyor,
          sürekli yer kaplamıyor.
        */}
        <Accordion className="h-fit">
          {/*
            İKİ AYRI LİSTE, iki ayrı bölüm. Katılımcı birim "kim katılacak",
            görebilecek kişi "kim görebilir" demek; tek listede gösterildikleri
            sürece aynı şey sanılıyorlardı.
          */}
          <AccordionSection
            deger="katilimcilar"
            baslik="Katılımcı birimler"
            ikon={<Building2 size={15} />}
            sayac={katilimciBirimListesi.length}
            eylem={
              // Katılımcı birim eklemek ayrı bir izin (`ajanda.katilimciEkle`):
              // bir müdürlüğü makamın toplantısına çağırmak, etkinliği
              // görüntülemekle aynı yetki değil.
              hasPermission(PERMISSION.ajandaKatilimciEkle) ? (
              <IconButton
                etiket="Katılımcı birim ekle"
                onClick={() => setKatilimciSecici(true)}
              >
                <Plus size={16} />
              </IconButton>
              ) : undefined
            }
          >
            {katilimciBirimListesi.length === 0 ? (
              <p className="py-2 text-sm text-text-3">
                Katılımcı birim eklenmemiş.
              </p>
            ) : (
              <ul className="space-y-1">
                {katilimciBirimListesi.map((k) => (
                  <li key={k.id} className="flex items-center gap-2.5 py-1.5">
                    <span
                      className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-sunken text-text-3"
                      aria-hidden
                    >
                      <Building2 size={15} />
                    </span>
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-medium">
                        {k.tamAd}
                      </span>
                      <span className="block truncate text-xs text-text-3">
                        {k.unvan}
                      </span>
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </AccordionSection>

          {/*
            Gizli DEĞİLKEN de çizilir EĞER kayıt kişi satırı taşıyorsa:
            sunucu gizlilik kapanınca listeyi temizliyor ama elde kalmış bir
            satırı hiç göstermemek, onu görünmez bir yetki hâline getirirdi.
          */}
          {(e.gizli || gorebilecekListesi.length > 0) && (
            <AccordionSection
              deger="gorebilecekler"
              baslik="Görebilecek kişiler"
              ikon={<Lock size={15} />}
              sayac={gorebilecekListesi.length}
            >
              <p className="mb-2.5 rounded-sm bg-(--st-wait-bg) px-2.5 py-1.5 text-xs leading-normal text-(--st-wait)">
                Bu etkinliği yalnızca oluşturan ve aşağıdaki kişiler görebilir.
                Katılımcı birimler <b>göremez</b>.
              </p>

              {gorebilecekListesi.length === 0 ? (
                <p className="py-2 text-sm text-text-3">
                  Kimse eklenmemiş — etkinliği yalnızca oluşturan görüyor.
                </p>
              ) : (
                <ul className="space-y-1">
                  {gorebilecekListesi.map((k) => (
                    <li key={k.id} className="flex items-center gap-2.5 py-1.5">
                      <span
                        className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-sunken text-text-3"
                        aria-hidden
                      >
                        <Users size={15} />
                      </span>
                      <span className="min-w-0">
                        <span className="block truncate text-sm font-medium">
                          {k.tamAd}
                        </span>
                        <span className="block truncate text-xs text-text-3">
                          {[k.unvan, k.birimAd].filter(Boolean).join(' · ')}
                        </span>
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </AccordionSection>
          )}

          <AccordionSection
            deger="metaveri"
            baslik="Kayıt bilgileri"
            ikon={<Info size={15} />}
          >
            <dl className="space-y-2 text-sm">
              <Satir etiket="Ekleyen" deger={e.kullaniciId} />
              <Satir etiket="Oluşturma" deger={dateTime(e.olusturmaTarihi)} />
              {e.guncellemeTarihi && (
                <Satir etiket="Son güncelleme" deger={dateTime(e.guncellemeTarihi)} />
              )}
              <Satir etiket="Kayıt no" deger={`#${e.id}`} />
              {e.seriId && <Satir etiket="Seri no" deger={`#${e.seriId}`} />}
            </dl>
          </AccordionSection>
        </Accordion>

        <ParticipantPicker
          acik={katilimciSecici}
          kapat={() => setKatilimciSecici(false)}
          kip="birim"
          ogeler={katilimciBirimler.liste}
          secili={katilimciBirimListesi.map((k) => k.birimId!)}
          degistir={(idler) => katilimciKaydet.mutate(idler)}
        />
      </div>
    </div>
  );
}

/**
 * Hazırlık durumu — işaretlenen belgelerin GERÇEKTEN var olup olmadığı.
 *
 * <p>
 * ÖNCEKİ DAVRANIŞ YANILTICIYDI: "konuşma metni hazırlanacak" kutucuğu
 * işaretliyse rozet, metin yazılmamış olsa bile yeşil çıkıyordu. Yani
 * kutucuk "hazırlanacak" demek, rozet ise "hazır" diyordu; toplantı sabahı
 * metnin olmadığı anlaşılıyordu.
 * </p>
 *
 * <p>
 * Artık üç durum var: <b>istenmemiş</b> (rozet yok), <b>istenmiş ama boş</b>
 * (kırmızı, tıklayınca yazılır), <b>hazır</b> (yeşil, tıklayınca düzenlenir).
 * </p>
 */
function Hazirlik({ etkinlik: e }: { etkinlik: Event }) {
  const [duzenlenen, setDuzenlenen] = useState<'konusma' | 'bilgi' | null>(null);

  /*
    SORGU BAYRAĞA BAĞLI DEĞİL.

    Önce `enabled: e.resimVar === true` idi — yani "fotoğraf eklenecek"
    kutusu işaretlenmemişse istek HİÇ atılmıyordu. Kutuyu işaretlemeden
    fotoğraf yükleyen kullanıcının dosyaları bu yüzden hiçbir yerde
    görünmüyordu: ne rozette, ne listede, ne de burada. Uç zaten gizlilik
    süzgecinden geçiyor ve yetkisiz çağrıya boş liste dönüyor.
  */
  const fotograflar = useQuery({
    queryKey: ['etkinlik', 'fotograflar', e.id!] as const,
    queryFn: () => api.get<EventPhoto[]>(`/etkinlik/${e.id}/fotograflar`),
    enabled: e.id != null,
  });

  const ogeler = [
    {
      anahtar: 'konusma' as const,
      istendi: e.konusmaMetniDurum === true,
      dolu: !!e.konusmaMetni?.trim(),
      ikon: <FileText size={13} />,
      etiket: 'Konuşma metni',
      duzenlenebilir: true,
    },
    {
      anahtar: 'bilgi' as const,
      istendi: e.bilgiNotuDurum === true,
      dolu: !!e.bilgiNotu?.trim(),
      ikon: <FileText size={13} />,
      etiket: 'Bilgi notu',
      duzenlenebilir: true,
    },
    {
      anahtar: 'resim' as const,
      istendi: e.resimVar === true,
      dolu: (fotograflar.data ?? []).length > 0,
      ikon: <Camera size={13} />,
      etiket: 'Fotoğraf',
      duzenlenebilir: false,
    },
    /*
      İSTENDİ **VEYA** DOLU.

      Filtre yalnızca `istendi`ye bakıyordu: hazırlık kutusu işaretlenmemiş
      ama içeriği olan bir kayıt (yüklenmiş fotoğraf, sonradan yazılmış
      konuşma metni) rozetsiz kalıyor ve kullanıcı elindeki dosyayı
      göremiyordu. Var olan bir şeyi göstermemek, istenen bir şeyi
      göstermemekten daha kötü.
    */
  ].filter((o) => o.istendi || o.dolu);

  const basin = e.basinKatilsin === true;
  /*
    ÇİÇEK ÜÇ DURUMLU — hazırlık rozetleriyle aynı kural.

    Rozet "Çiçek talimatı verildi" deyip HER ZAMAN yeşil çiziliyordu: talimat
    verilmiş ama çiçek daha teslim edilmemişken de "hazır" görünüyordu, yani
    ekranda takip edilmesi gereken tek şey görünmez kalıyordu. Teslim bilgisi
    zaten yanıtta (`cicek.gonderildi`), ek istek gerekmiyor.
  */
  const cicekTalimati = !!e.cicekId;
  const cicekTeslim = e.cicek?.gonderildi === true;

  /*
    TESLİM FOTOĞRAFI — çiçekçinin yüklediği kanıt.

    Makam "çiçek gitti mi, nasıl gitti" sorusunu çiçekçiyi aramadan
    görebilmeli. Fotoğraf isteğe bağlı olduğu için yoksa hiçbir şey
    çizilmiyor — boş bir çerçeve "fotoğraf bekleniyor" gibi okunurdu.
  */
  const cicekFotografi = e.cicek?.resim ?? null;
  const cicekGoruntuleyici = useImageViewer();

  if (ogeler.length === 0 && !basin && !cicekTalimati) return null;

  return (
    <>
      <Card className="p-4">
        <p className="mb-2.5 text-2xs uppercase tracking-[0.06em] text-text-3">Hazırlık</p>
        <ul className="flex flex-wrap gap-2">
          {ogeler.map((o) => {
            const icerik = (
              <>
                {o.dolu ? <Check size={13} strokeWidth={3} /> : <AlertTriangle size={13} />}
                {o.ikon}
                {o.etiket}
                <span className="opacity-75">{o.dolu ? '· hazır' : '· eksik'}</span>
              </>
            );

            const sinif = cn(
              'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium',
              o.dolu
                ? 'bg-(--st-ok-bg) text-(--st-ok)'
                : 'bg-(--st-no-bg) text-(--st-no)',
            );

            return (
              <li key={o.anahtar}>
                {o.duzenlenebilir ? (
                  <button
                    type="button"
                    onClick={() => setDuzenlenen(o.anahtar as 'konusma' | 'bilgi')}
                    className={cn(sinif, 'transition-opacity hover:opacity-80')}
                    title={o.dolu ? 'Düzenle' : 'Yaz'}
                  >
                    {icerik}
                    <Pencil size={11} className="ml-0.5 opacity-70" />
                  </button>
                ) : (
                  <span className={sinif}>{icerik}</span>
                )}
              </li>
            );
          })}

          {basin && (
            <li className="inline-flex items-center gap-1.5 rounded-full bg-(--st-ok-bg) px-2.5 py-1 text-xs font-medium text-(--st-ok)">
              <Newspaper size={13} />
              Basın katılacak
            </li>
          )}
          {cicekTalimati && (
            <li
              className={cn(
                'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium',
                cicekTeslim
                  ? 'bg-(--st-ok-bg) text-(--st-ok)'
                  : 'bg-(--st-wait-bg) text-(--st-wait)',
              )}
              title={
                cicekTeslim
                  ? 'Çiçekçi teslim ettiğini doğrulama koduyla bildirdi'
                  : 'Talimat verildi, çiçekçi henüz teslim bildirimi yapmadı'
              }
            >
              {cicekTeslim ? <Check size={13} strokeWidth={3} /> : <AlertTriangle size={13} />}
              <Flower2 size={13} />
              Çiçek
              <span className="opacity-75">
                {cicekTeslim ? '· teslim edildi' : '· bekliyor'}
              </span>
            </li>
          )}
        </ul>

        {cicekTeslim && cicekFotografi && (
          <button
            type="button"
            onClick={() => cicekGoruntuleyici.ac(0)}
            className="mt-3 block w-full overflow-hidden rounded-md border border-border
              bg-sunken transition-opacity hover:opacity-90"
            title="Teslim fotoğrafını büyüt"
          >
            <img
              src={cicekFotografi}
              alt="Çiçekçinin yüklediği teslim fotoğrafı"
              loading="lazy"
              className="block max-h-64 w-full object-contain"
            />
          </button>
        )}
      </Card>

      {cicekFotografi && (
        <ImageViewer
          resimler={[{ yol: cicekFotografi, baslik: 'Çiçek teslim fotoğrafı' }]}
          acikIndeks={cicekGoruntuleyici.acikIndeks}
          kapat={cicekGoruntuleyici.kapat}
          indeksDegistir={cicekGoruntuleyici.indeksDegistir}
        />
      )}

      <MetinDuzenleyici
        etkinlik={e}
        alan={duzenlenen}
        kapat={() => setDuzenlenen(null)}
      />
    </>
  );
}

/**
 * Konuşma metni / bilgi notu yazma diyaloğu.
 *
 * Metinler etkinliğin kendi alanları; ayrı bir uç yok, tam kayıt geri
 * gönderilir. Yalnızca ilgili alan değiştirilir.
 */
function MetinDuzenleyici({
  etkinlik: e,
  alan,
  kapat,
}: {
  etkinlik: Event;
  alan: 'konusma' | 'bilgi' | null;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [metin, setMetin] = useState('');

  // Diyalog her açılışta mevcut metinden başlar.
  const [sonAlan, setSonAlan] = useState(alan);
  if (alan !== sonAlan) {
    setSonAlan(alan);
    if (alan) {
      setMetin((alan === 'konusma' ? e.konusmaMetni : e.bilgiNotu) ?? '');
    }
  }

  const kaydet = useMutation({
    mutationFn: () =>
      api.put(`/etkinlik/${e.id}`, {
        ...e,
        ...(alan === 'konusma'
          ? { konusmaMetni: metin, konusmaMetniDurum: true }
          : { bilgiNotu: metin, bilgiNotuDurum: true }),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['etkinlik'] });
      bildir('basari', 'Kaydedildi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const baslik = alan === 'konusma' ? 'Konuşma metni' : 'Bilgi notu';

  return (
    // Kabuk elle kurulmuyordu ve mobilde parmakla kapanmıyordu; artık
    // `OverlayShell` (mobilde `vaul`, masaüstünde ortalanmış pencere).
    <OverlayShell
      acik={alan !== null}
      kapat={kapat}
      baslik={baslik}
      ikon={<FileText size={15} />}
      genislik="orta"
    >
          <div className="min-h-0 flex-1 overflow-y-auto p-4">
            <Textarea
              value={metin}
              onChange={(t) => setMetin(t.target.value)}
              rows={14}
              autoFocus
              placeholder={`${baslik} metnini buraya yazın…`}
              className="min-h-[280px] leading-[1.65] metin-guzel"
            />
          </div>

          <div className="flex shrink-0 items-center gap-2 border-t border-border px-4 py-3">
            <span className="flex-1 text-xs tabular-nums text-text-3">
              {metin.trim().length} karakter
            </span>
            <Button type="button" varyant="ikincil" onClick={kapat}>
              Vazgeç
            </Button>
            <Button onClick={() => kaydet.mutate()} disabled={kaydet.isPending}>
              {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
            </Button>
          </div>
    </OverlayShell>
  );
}

/** Kayıt bilgileri akordiyonundaki tek satır. */
function Satir({ etiket, deger }: { etiket: string; deger?: string | null }) {
  if (!deger) return null;
  return (
    <div className="flex items-baseline gap-3">
      <dt className="w-[104px] shrink-0 text-text-3">{etiket}</dt>
      <dd className="min-w-0 flex-1 wrap-break-word font-medium">{deger}</dd>
    </div>
  );
}

/** Sistem statüsünün görünümü — etiket + renk çifti. */
const STATU_GORUNUMU: Record<number, { e: string; r: string; z: string }> = {
  [STATUS.Pending]: { e: 'Beklemede', r: '--st-wait', z: '--st-wait-bg' },
  [STATUS.Completed]: { e: 'Tamamlandı', r: '--st-done', z: '--st-done-bg' },
  [STATUS.Cancelled]: { e: 'İptal edildi', r: '--st-cancel', z: '--st-cancel-bg' },
};

/** Statünün kullanıcıya görünen adı — tekrar denetimi için de kullanılır. */
function statuAdi(statu: number): string | undefined {
  return STATU_GORUNUMU[statu]?.e;
}

/** İki etiket AYNI şeyi mi söylüyor? Türkçe küçük harfle karşılaştırır. */
function ayniAd(a?: string | null, b?: string | null): boolean {
  if (!a || !b) return false;
  const d = (m: string) => m.trim().toLocaleLowerCase('tr-TR');
  return d(a) === d(b);
}

function StatuRozeti({ statu }: { statu: number }) {
  const g = STATU_GORUNUMU[statu];

  if (!g) return null;

  return (
    <span
      className="inline-flex h-6 items-center gap-1.5 rounded-sm px-2 text-2xs font-semibold"
      style={{ color: `var(${g.r})`, background: `var(${g.z})` }}
    >
      <span className="h-[5px] w-[5px] rounded-full bg-current" aria-hidden />
      {g.e}
    </span>
  );
}

function Ozet({ etkinlik: e }: { etkinlik: Event }) {
  return (
    <div className="space-y-4">
      {e.aciklama && (
        <Card className="p-4">
          <p className="mb-1.5 text-2xs uppercase tracking-[0.06em] text-text-3">Açıklama</p>
          <p className="whitespace-pre-wrap text-sm leading-[1.6] text-text-2 metin-guzel">
            {e.aciklama}
          </p>
        </Card>
      )}

      <Hazirlik etkinlik={e} />

      {(e.irtibatKisi || e.irtibatTelefon) && (
        <Card className="p-4">
          <p className="mb-2 text-2xs uppercase tracking-[0.06em] text-text-3">İrtibat</p>
          <p className="text-sm">{e.irtibatKisi || '—'}</p>
          {e.irtibatTelefon && (
            <a href={`tel:${e.irtibatTelefon}`} className="text-sm">
              {e.irtibatTelefon}
            </a>
          )}
        </Card>
      )}

      {e.bilgiNotu && (
        <Card className="p-4">
          <p className="mb-1.5 text-2xs uppercase tracking-[0.06em] text-text-3">Bilgi notu</p>
          <p className="whitespace-pre-wrap text-sm leading-[1.6] text-text-2">
            {e.bilgiNotu}
          </p>
        </Card>
      )}

      {e.konusmaMetni && (
        <Card className="p-4">
          <p className="mb-1.5 text-2xs uppercase tracking-[0.06em] text-text-3">
            Konuşma metni
          </p>
          <p className="whitespace-pre-wrap text-sm leading-[1.6] text-text-2">
            {e.konusmaMetni}
          </p>
        </Card>
      )}

      {!e.aciklama && !e.bilgiNotu && !e.konusmaMetni && (
        <EmptyState
          ikon={FileText}
          baslik="Ek bilgi yok"
          aciklama="Bu etkinlik için açıklama veya hazırlık notu girilmemiş."
        />
      )}
    </div>
  );
}

function Notlar({ etkinlikId }: { etkinlikId: number }) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const notlar = useQuery({
    queryKey: queryKeys.event.notes(etkinlikId),
    queryFn: () => api.get<EventNote[]>(`/etkinlik/${etkinlikId}/notlar`),
  });

  const ekle = useMutation({
    mutationFn: (not: string) =>
      api.post<boolean>(`/etkinlik/${etkinlikId}/not`, { not, ajandaId: etkinlikId }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.event.notes(etkinlikId) });
      qc.invalidateQueries({ queryKey: queryKeys.event.activity(etkinlikId) });
      bildir('basari', 'Not eklendi');
    },
    onError: (h: Error) => bildir('hata', 'Not eklenemedi', h.message),
  });

  return (
    <div className="space-y-4">
      <NoteComposer
        alanId="etkinlik-not"
        yerTutucu="Etkinliğe not ekleyin…"
        bekliyor={ekle.isPending}
        gonder={(m) => ekle.mutateAsync(m)}
      />

      {notlar.isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : (notlar.data ?? []).length === 0 ? (
        <EmptyState ikon={MessageSquarePlus} baslik="Henüz not yok" />
      ) : (
        <Card className="p-4">
          <Timeline
            ogeler={(notlar.data ?? []).map((n) => ({
              id: n.id!,
              baslik: n.olusturan || 'Bilinmiyor',
              zaman: dateTime(n.olusturulmaTarihi),
              govde: (
                <p className="whitespace-pre-wrap text-sm leading-[1.55] text-text-2">
                  {n.not}
                </p>
              ),
            }))}
          />
        </Card>
      )}
    </div>
  );
}

function Fotograflar({ etkinlikId }: { etkinlikId: number }) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [yukleniyor, setYukleniyor] = useState(false);

  const fotograflar = useQuery({
    queryKey: ['etkinlik', 'fotograflar', etkinlikId] as const,
    queryFn: () => api.get<EventPhoto[]>(`/etkinlik/${etkinlikId}/fotograflar`),
  });

  const goruntuleyici = useImageViewer();
  const resimler = (fotograflar.data ?? []).map((f, i) => ({
    yol: fotografYolu(f.filename),
    baslik: `Fotoğraf ${i + 1} / ${(fotograflar.data ?? []).length}`,
  }));

  /** Fotoğraf yükleme — ortak yardımcı (`veri/fotograf.ts`). */
  async function yukle(dosyalar: FileList) {
    setYukleniyor(true);
    try {
      await uploadEventPhoto(etkinlikId, dosyalar);
      qc.invalidateQueries({ queryKey: ['etkinlik'] });
      bildir('basari', 'Fotoğraflar yüklendi');
    } catch (h) {
      bildir('hata', 'Yüklenemedi', (h as Error).message);
    } finally {
      setYukleniyor(false);
    }
  }

  const sil = async (fotografId: number) => {
    try {
      await api.delete(`/etkinlik/fotograf/${fotografId}`);
      qc.invalidateQueries({ queryKey: ['etkinlik'] });
      bildir('basari', 'Fotoğraf silindi');
    } catch (h) {
      bildir('hata', 'Silinemedi', (h as Error).message);
    }
  };

  const yukleyici = (
    <Card className="p-3.5">
      <label className="flex cursor-pointer items-center gap-3">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-brand text-on-brand">
          <Camera size={17} strokeWidth={1.9} />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block text-sm font-medium">Fotoğraf ekle</span>
          <span className="block text-xs text-text-3">
            JPEG, PNG veya WEBP · en fazla 5 MB
          </span>
        </span>
        <input
          type="file"
          accept="image/jpeg,image/png,image/webp"
          multiple
          disabled={yukleniyor}
          onChange={(e) => e.target.files?.length && void yukle(e.target.files)}
          className="sr-only"
        />
        <Button type="button" varyant="ikincil" className="pointer-events-none">
          {yukleniyor ? 'Yükleniyor…' : 'Seç'}
        </Button>
      </label>
    </Card>
  );

  if (fotograflar.isLoading) return <Skeleton className="h-40 w-full" />;

  if ((fotograflar.data ?? []).length === 0) {
    return (
      <div className="space-y-4">
        {yukleyici}
        <EmptyState
          ikon={Camera}
          baslik="Fotoğraf yok"
          aciklama="Bu etkinliğe eklenmiş bir fotoğraf bulunmuyor."
        />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {yukleyici}
      <ul className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-4">
      {(fotograflar.data ?? []).map((f, i) => {
        const yol = fotografYolu(f.filename);
        return (
          <li key={f.id} className="group relative">
            <button
              type="button"
              onClick={() => goruntuleyici.ac(i)}
              aria-label={`${i + 1}. fotoğrafı büyüt`}
              className="block w-full overflow-hidden rounded-card border border-border bg-sunken"
            >
              <img
                src={yol}
                alt="Etkinlik fotoğrafı"
                loading="lazy"
                className="aspect-4/3 w-full object-cover transition-transform duration-200 hover:scale-[1.03]"
              />
            </button>
            <button
              type="button"
              aria-label="Fotoğrafı sil"
              title="Sil"
              onClick={() => f.id && void sil(f.id)}
              className="absolute right-1.5 top-1.5 hidden h-8 w-8 place-items-center rounded-sm bg-surface/90 text-text-2 shadow-1 hover:text-(--st-no) group-hover:grid"
            >
              <Trash2 size={14} />
            </button>
          </li>
        );
      })}
      </ul>

      <ImageViewer
        resimler={resimler}
        acikIndeks={goruntuleyici.acikIndeks}
        kapat={goruntuleyici.kapat}
        indeksDegistir={goruntuleyici.indeksDegistir}
      />
    </div>
  );
}

/**
 * Fotoğrafın genel adresi.
 *
 * Sunucu yalnızca DOSYA ADINI döndürüyor; dosyalar `wwwroot/uploads/ajanda/`
 * altında duruyor ve `UseStaticFiles` ile yayınlanıyor. Yolu burada kurmak,
 * v1 sözleşmesine dokunmadan çalışmanın tek yolu.
 */
function fotografYolu(dosyaAdi?: string | null): string {
  return dosyaAdi ? `/uploads/ajanda/${dosyaAdi}` : '';
}

/**
 * Etkinlik geçmişi (denetim izi).
 *
 * Alan değişiklikleri `eski → yeni` olarak gösterilir; "güncellendi" demek
 * denetim için yetersiz — neyin değiştiği asıl bilgi.
 */
function Gecmis({ etkinlikId }: { etkinlikId: number }) {
  const olaylar = useQuery({
    queryKey: queryKeys.event.activity(etkinlikId),
    queryFn: () => api.get<EventActivity[]>(`/etkinlik/${etkinlikId}/olaylar`),
  });

  if (olaylar.isLoading) return <Skeleton className="h-40 w-full" />;
  if ((olaylar.data ?? []).length === 0) {
    return <EmptyState ikon={History} baslik="Kayıt yok" />;
  }

  return (
    <Card className="p-4">
      <Timeline
        ogeler={(olaylar.data ?? []).map((o) => ({
          id: o.id!,
          baslik: EVENT_ACTIVITY_LABELS[o.tip as number] ?? 'İşlem',
          altBaslik: [o.kullanici, o.aciklama].filter(Boolean).join(' · ') || undefined,
          zaman: dateTime(o.tarih),
          renk: renkSec(o.tip as number),
          govde:
            (o.degisiklikler ?? []).length > 0 ? (
              <div className="space-y-1">
                {(o.degisiklikler ?? []).map((d, i) => (
                  <DegisiklikSatiri key={i} alan={d.alan ?? ''} eski={d.eski} yeni={d.yeni} />
                ))}
              </div>
            ) : undefined,
        }))}
      />
    </Card>
  );
}

function renkSec(tip: number): string | undefined {
  if (tip === 0) return '--st-ok';        // oluşturuldu
  if (tip === 2) return '--st-no';        // silindi
  if (tip === 5 || tip === 6) return '--st-live'; // havale / üst birim
  return undefined;
}

function DetayIskeleti() {
  return (
    <div className="space-y-4">
      <Skeleton className="h-7 w-2/3" />
      <Skeleton className="h-5 w-48" />
      <Skeleton className="h-20 w-full" />
      <Skeleton className="h-48 w-full" />
    </div>
  );
}
