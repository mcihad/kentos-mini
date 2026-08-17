import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  CalendarPlus,
  CheckCircle2,
  Clock,
  Flag,
  Pencil,
  Plus,
  Search,
  Trash2,
  SlidersHorizontal,
  Users,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FieldWrapper, SearchInput, Textarea, Input } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { FormModal } from '../components/FormModal';
import { SkeletonRows } from '../components/Skeleton';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Card } from '../components/Card';
import { Pagination } from '../components/Pagination';
import { SelectMenu } from '../components/SelectMenu';
import { DatePicker, parseDay, dayText } from '../components/DatePicker';
import { useToast } from '../components/Toast';
import { RowActions } from '../components/RowActions';
import { FilterSection, FilterOptions, FilterSheet } from '../components/FilterSheet';
import { useIsDesktop } from '../components/screenSize';
import { Fab } from '../shell/mobile/Fab';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { date } from '../data/format';
import { api } from '../data/client';
import { usePublicDays } from '../data/hooks';
import { PUBLIC_DAY_STATUS } from '../data/types';

/**
 * HALK GÜNÜ — gün listesi.
 *
 * Vatandaşın makamda sırayla dinlendiği günler. Liste "hangi gün ne kadar iş
 * çıktı" sorusuna bakıyor: kaç kişi atandı, kaçıyla görüşüldü, kaçı takip
 * gerektiriyor. Ayrıntı ekranı listeyi kurmaya, salon modu ise günün kendisini
 * yürütmeye ait.
 */
export default function PublicDays() {
  const { hasPermission } = useSession();
  const gezin = useNavigate();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [durum, setDurum] = useState<number | null>(null);
  const [sayfa, setSayfa] = useState(1);
  const [formAcik, setFormAcik] = useState(false);
  /** Düzenlenen gün — `null` ise form YENİ kayıt açar. */
  const [duzenlenen, setDuzenlenen] = useState<PublicDayRecord | null>(null);
  const [silinecek, setSilinecek] = useState<PublicDayRecord | null>(null);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const masaustu = useIsDesktop();
  const [suzgecAcik, setSuzgecAcik] = useState(false);

  const liste = usePublicDays({ sayfa, boyut: 20, ara: arama, durum });

  return (
    <div className="space-y-4">
      {/* ── Araç çubuğu ── */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Başlık veya konum ara"
          aria-label="Halk günlerinde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[280px] md:flex-1"
        />

        {/*
          MOBİLDE ÜSTTE YALNIZCA ARAMA.

          Üç denetim (Durum · Bekleyenler · Yeni halk günü) 390px'e sığmıyor,
          "Yeni halk günü" iki satıra kırılıyordu. Hepsi FAB'a taşındı;
          durum süzgeci de tabakada, tam genişlik çiplerle.
        */}
        {!masaustu && (
          <>
            <Fab
              etiket="Halk günü eylemleri"
              eylemler={[
                ...(hasPermission(PERMISSION.halkgunuYonet)
                  ? [{
                      etiket: 'Yeni halk günü',
                      ikon: <Plus size={21} strokeWidth={2.2} />,
                      onClick: () => setFormAcik(true),
                    }]
                  : []),
                {
                  etiket: 'Bekleyenler',
                  ikon: <Users size={19} strokeWidth={2} />,
                  onClick: () => gezin('/halk-gunu/basvurular'),
                },
                {
                  etiket: 'Süz',
                  ikon: <SlidersHorizontal size={19} strokeWidth={2} />,
                  onClick: () => setSuzgecAcik(true),
                },
              ]}
            />

            <FilterSheet
              acik={suzgecAcik}
              kapat={() => setSuzgecAcik(false)}
              etkinSayisi={durum !== null ? 1 : 0}
              temizle={() => {
                setDurum(null);
                setSayfa(1);
              }}
            >
              <FilterSection baslik="Durum">
                <FilterOptions
                  deger={durum}
                  degistir={(d) => {
                    setDurum(d);
                    setSayfa(1);
                  }}
                  secenekler={[
                    { deger: null as number | null, etiket: 'Tümü' },
                    { deger: PUBLIC_DAY_STATUS.planning as number | null, etiket: 'Planlanıyor' },
                    { deger: PUBLIC_DAY_STATUS.live as number | null, etiket: 'Yayında' },
                    { deger: PUBLIC_DAY_STATUS.completed as number | null, etiket: 'Tamamlandı' },
                    { deger: PUBLIC_DAY_STATUS.cancelled as number | null, etiket: 'İptal' },
                  ]}
                />
              </FilterSection>
            </FilterSheet>
          </>
        )}

        <div className="hidden items-center gap-1.5 md:ml-auto md:flex">
          <SelectMenu
            deger={durum}
            degistir={(d) => {
              setDurum(d);
              setSayfa(1);
            }}
            etiket="Durum"
            tumuEtiketi="Tüm durumlar"
            secenekler={[
              { deger: PUBLIC_DAY_STATUS.planning, etiket: 'Planlanıyor' },
              { deger: PUBLIC_DAY_STATUS.live, etiket: 'Yayında' },
              { deger: PUBLIC_DAY_STATUS.completed, etiket: 'Tamamlandı' },
              { deger: PUBLIC_DAY_STATUS.cancelled, etiket: 'İptal' },
            ]}
          />

          <Button varyant="ikincil" onClick={() => gezin('/halk-gunu/basvurular')}>
            <Users size={14} />
            Bekleyenler
          </Button>

          {hasPermission(PERMISSION.halkgunuYonet) && (
            <Button onClick={() => setFormAcik(true)}>
              <Plus size={14} />
              Yeni halk günü
            </Button>
          )}
        </div>
      </div>

      {/* ── İçerik ── */}
      {liste.isLoading ? (
        <SkeletonRows adet={4} />
      ) : liste.isError ? (
        <EmptyState
          ikon={CalendarPlus}
          baslik="Halk günleri yüklenemedi"
          aciklama={(liste.error as Error)?.message}
        />
      ) : (liste.data?.veriler.length ?? 0) === 0 ? (
        <EmptyState
          ikon={CalendarPlus}
          baslik="Halk günü yok"
          aciklama="Bir gün oluşturup zaman dilimlerini tanımlayın, sonra bekleyenlerden atama yapın."
          eylem={
            hasPermission(PERMISSION.halkgunuYonet) ? (
              <Button onClick={() => setFormAcik(true)}>
                <Plus size={14} />
                Yeni halk günü
              </Button>
            ) : undefined
          }
        />
      ) : (
        <>
          <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {liste.data!.veriler.map((g) => (
              <li key={g.id} className="relative">
                {/*
                  DÜZENLE / SİL kartın ÜSTÜNDE, bağlantının DIŞINDA duruyor:
                  bağlantının içine düğme koymak hem erişilebilirlik açısından
                  yanlış hem de her tıklamada gün ayrıntısına gidiyordu.
                */}
                {hasPermission(PERMISSION.halkgunuYonet) && (
                  /*
                    TEK GRUP, İKİ DÜĞME.

                    Önce iki ayrı kenarlıklı düğme yan yana duruyordu; ikisi
                    de kendi kutusunda, aralarında boşluk. Kartın köşesinde
                    "iki ayrı şey" gibi okunuyor ve ikisi birlikte fazladan
                    yer kaplıyordu. Ortak kenarlık, aralarında saç teli:
                    uygulamanın geri kalanındaki satır eylemleriyle aynı
                    gramer, silme ayrı tonda.
                  */
                  <span className="absolute right-2.5 top-2.5 z-10">
                    <RowActions
                      boyut="kucuk"
                      eylemler={[
                        {
                          etiket: `${g.baslik || date(g.tarih)} — düzenle`,
                          ikon: Pencil,
                          onClick: () => {
                            setDuzenlenen(g);
                            setFormAcik(true);
                          },
                        },
                        {
                          etiket: `${g.baslik || date(g.tarih)} — sil`,
                          ikon: Trash2,
                          onClick: () => setSilinecek(g),
                          ton: 'tehlike',
                        },
                      ]}
                    />
                  </span>
                )}
                <Link to={`/halk-gunu/${g.id}`} className="block">
                  <Card className="h-full transition-colors hover:border-brand-2">
                    <div className="flex items-start gap-3 p-4">
                      {/* Tarih kutusu — listede aranan ilk şey gün. */}
                      <div className="grid shrink-0 place-items-center rounded-control border border-border bg-surface-2 px-2.5 py-1.5">
                        <span className="font-display text-xl font-bold leading-none tabular-nums">
                          {new Date(g.tarih!).getDate()}
                        </span>
                        <span className="mt-0.5 text-2xs uppercase tracking-wider text-text-3">
                          {new Date(g.tarih!).toLocaleDateString('tr-TR', { month: 'short' })}
                        </span>
                      </div>

                      <div className="min-w-0 flex-1">
                        <p className="line-clamp-1 font-display text-base font-semibold">
                          {g.baslik || date(g.tarih)}
                        </p>
                        <p className="mt-0.5 line-clamp-1 text-xs text-text-3">
                          {g.konum || 'Konum belirtilmemiş'}
                        </p>

                        <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-text-2">
                          <span className="flex items-center gap-1">
                            <Users size={12} className="text-text-3" />
                            <b className="tabular-nums">{g.kisiSayisi}</b> kişi
                          </span>
                          <span className="flex items-center gap-1">
                            <Clock size={12} className="text-text-3" />
                            <b className="tabular-nums">{g.dilimSayisi}</b> dilim
                          </span>
                          {(g.gorusulenSayisi ?? 0) > 0 && (
                            <span className="flex items-center gap-1 text-(--st-ok)">
                              <CheckCircle2 size={12} />
                              <b className="tabular-nums">{g.gorusulenSayisi}</b> görüşüldü
                            </span>
                          )}
                          {/*
                            Takip sayısı listede DURUYOR: Özel Kalem'in bir
                            sonraki işi tam olarak bu ve günün içine girmeden
                            görülebilmeli.
                          */}
                          {(g.takipSayisi ?? 0) > 0 && (
                            <span className="flex items-center gap-1 text-(--st-warn)">
                              <Flag size={12} />
                              <b className="tabular-nums">{g.takipSayisi}</b> takip
                            </span>
                          )}
                        </div>
                      </div>

                      <span className="mt-8 shrink-0 rounded-full border border-border px-2 py-0.5 text-2xs text-text-2">
                        {g.durumAd}
                      </span>
                    </div>
                  </Card>
                </Link>
              </li>
            ))}
          </ul>

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="gün" />
        </>
      )}

      <PublicDayForm
        acik={formAcik}
        kapat={() => {
          setFormAcik(false);
          setDuzenlenen(null);
        }}
        duzenlenen={duzenlenen}
      />

      <DeleteConfirm gun={silinecek} kapat={() => setSilinecek(null)} />
    </div>
  );
}

/**
 * Halk günü formu — YENİ ve DÜZENLE aynı form.
 *
 * Gün oluşturulduktan sonra tarihi, başlığı ya da konumu değiştirmenin hiçbir
 * yolu yoktu; sunucuda uç (`PUT /halk-gunu/{id}`) baştan beri duruyordu ama
 * arayüzde karşılığı yoktu. Aynı alanlar iki kez yazılmasın diye tek bileşen.
 */
export type PublicDayRecord = {
  id?: number;
  tarih?: string;
  baslik?: string | null;
  konum?: string | null;
  aciklama?: string | null;
};

export function PublicDayForm({
  acik,
  kapat,
  duzenlenen,
}: {
  acik: boolean;
  kapat: () => void;
  duzenlenen: PublicDayRecord | null;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();

  const [gun, setGun] = useState(() => dayText(new Date()));
  const [baslik, setBaslik] = useState('');
  const [konum, setKonum] = useState('Başkanlık Makamı');
  const [aciklama, setAciklama] = useState('');

  // Form AÇILDIĞINDA doldurulur; düzenlenen kayıt değişince alanlar da değişir.
  useEffect(() => {
    if (!acik) return;
    setGun(dayText(duzenlenen?.tarih ? new Date(duzenlenen.tarih) : new Date()));
    setBaslik(duzenlenen?.baslik ?? '');
    setKonum(duzenlenen?.konum ?? 'Başkanlık Makamı');
    // Özet DTO açıklama TAŞIMIYOR; liste ekranından düzenlerken alan boş
    // başlar. Gün ayrıntısından açıldığında dolu gelir.
    setAciklama(duzenlenen?.aciklama ?? '');
  }, [acik, duzenlenen]);

  const kaydet = useMutation({
    mutationFn: () => {
      const govde = {
        tarih: parseDay(gun)?.toISOString().slice(0, 10) + 'T00:00:00',
        baslik: baslik || null,
        konum: konum || null,
        aciklama: aciklama || null,
      };
      return duzenlenen
        ? api.put<{ id: number }>(`/halk-gunu/${duzenlenen.id}`, govde)
        : api.post<{ id: number }>('/halk-gunu', govde);
    },
    onSuccess: (g) => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', duzenlenen ? 'Halk günü güncellendi' : 'Halk günü oluşturuldu');
      kapat();
      // YENİ kayıtta doğrudan ayrıntıya gidilir: bir sonraki adım her zaman
      // zaman dilimlerini tanımlamak. Düzenlemede kullanıcı listede kalır.
      if (!duzenlenen && g?.id) gezin(`/halk-gunu/${g.id}`);
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  return (
    <FormModal
      acik={acik}
      kapat={kapat}
      baslik={duzenlenen ? 'Halk gününü düzenle' : 'Yeni halk günü'}
      aciklama={
        duzenlenen
          ? 'Tarih, başlık ve konum güncellenir; dilimler ve atamalar korunur.'
          : 'Günü oluşturduktan sonra zaman dilimlerini tanımlayıp bekleyenlerden atama yaparsınız.'
      }
      ikon={<CalendarPlus size={15} />}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={() => kaydet.mutate()} disabled={!gun || kaydet.isPending}>
            {kaydet.isPending
              ? 'Kaydediliyor…'
              : duzenlenen
                ? 'Güncelle'
                : 'Oluştur'}
          </Button>
        </>
      }
    >
      <FieldWrapper etiket="Tarih" id="hg-tarih" zorunlu>
        <DatePicker id="hg-tarih" deger={gun} degistir={setGun} />
      </FieldWrapper>

      <FieldWrapper etiket="Başlık" id="hg-baslik" ipucu="Boş bırakılırsa tarih başlık olur">
        <Input
          id="hg-baslik"
          value={baslik}
          onChange={(e) => setBaslik(e.target.value)}
          placeholder="Eylül Halk Günü"
        />
      </FieldWrapper>

      <FieldWrapper etiket="Konum" id="hg-konum">
        <Input id="hg-konum" value={konum} onChange={(e) => setKonum(e.target.value)} />
      </FieldWrapper>

      <FieldWrapper etiket="Açıklama" id="hg-aciklama">
        <Textarea
          id="hg-aciklama"
          value={aciklama}
          onChange={(e) => setAciklama(e.target.value)}
          rows={3}
        />
      </FieldWrapper>
    </FormModal>
  );
}

/**
 * HALK GÜNÜ SİLME.
 *
 * Sunucu görüşme kaydı girilmiş bir günü SİLDİRMEZ (salondaki notlar tek
 * kopya); o durumda gelen iş kuralı hatası kullanıcıya olduğu gibi gösterilir
 * ve "durumunu İptal yapın" der.
 */
export function DeleteConfirm({
  gun,
  kapat,
  silindi,
}: {
  gun: PublicDayRecord | null;
  kapat: () => void;
  /** Silme başarılıysa çağrılır — ayrıntı ekranı listeye döner. */
  silindi?: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();

  const sil = useMutation({
    mutationFn: () => api.delete(`/halk-gunu/${gun!.id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['halkgunu'] });
      bildir('basari', 'Halk günü silindi');
      kapat();
      silindi?.();
    },
    onError: (h: Error) => {
      bildir('hata', 'Silinemedi', h.message);
      kapat();
    },
  });

  return (
    <ConfirmDialog
      acik={gun !== null}
      kapat={kapat}
      baslik="Halk günü silinsin mi?"
      aciklama={
        gun
          ? `${gun.baslik || date(gun.tarih)} — zaman dilimleri ve atamalar da silinir. ` +
            'Görüşme kaydı girilmişse silme yerine durumu İptal yapılır.'
          : undefined
      }
      onayEtiketi="Sil"
      yikici
      onayla={() => sil.mutate()}
    />
  );
}
