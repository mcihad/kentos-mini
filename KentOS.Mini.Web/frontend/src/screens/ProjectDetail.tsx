import {
  ArrowLeft, Building2, Calendar, CheckCircle2, Circle, FolderKanban,
  MapPin, Pencil, Plus, Trash2, User, Wallet,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Button, IconButton } from '../components/Button';
import { Card, CardHeader } from '../components/Card';
import { ColoredBadge } from '../components/Color';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { FieldWrapper, Input, Textarea } from '../components/Field';
import { FormModal } from '../components/FormModal';
import { DatePicker } from '../components/DatePicker';
import { Skeleton } from '../components/Skeleton';
import { Tabs } from '../components/Tabs';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { shortDate } from '../data/format';
import { useProject, useProjectMutations } from '../data/projects';
import { useTasks } from '../data/tasks';
import { Board } from './project/Board';
import { Gantt } from './project/Gantt';
import { ProjectTeam } from './project/ProjectTeam';
import { Avatar } from '../components/PersonPicker';
import type { ProjectDetail as Proje } from '../data/types';
import { SlaBadge, StageProgress } from './task/TaskBits';

const PROJE_SEKMELERI = ['ozet', 'pano', 'gantt', 'gorevler'] as const;
type Sekme = (typeof PROJE_SEKMELERI)[number];

/**
 * PROJE DETAYI.
 *
 * <p>
 * Sekmeler <b>tembel</b>: pano ve gantt yalnızca açıldıklarında veri
 * çekiyor. Üçünü birden yüklemek, proje açılışında üç ağır sorgu demekti ve
 * kullanıcıların çoğu yalnızca özete bakıyor.
 * </p>
 */
export default function ProjectDetail() {
  /*
    ARA HEDEF EKLEME BU EKRANDA.

    Eskiden tek yol `/projeler/{id}/duzenle` idi: bütçe, tarih, ekip ve pano
    sütunlarıyla açılan bir form, tek satırlık bir iş için fazla — üstelik
    kaydetmek projenin geri kalanını da yeniden yazıyordu.
  */
  const [tasFormu, setTasFormu] = useState(false);
  const [tasAd, setTasAd] = useState('');
  const [tasAciklama, setTasAciklama] = useState('');
  const [tasTarih, setTasTarih] = useState('');
  const { id } = useParams();
  const projeId = Number(id);
  const gezin = useNavigate();
  const { bildir } = useToast();
  const { hasPermission } = useSession();

  /*
    ETKİN SEKME URL'DE (`?sekme=pano`).

    Bileşen içinde tutulan sekme, görsel turun ve derin bağlantının
    erişemediği bir ekran demek: pano ve gantt tek başına açılamıyor,
    dolayısıyla oradaki davranış hiç doğrulanamıyordu. Ajanda ve yönetim
    ekranlarındaki gerekçenin aynısı.
  */
  const [sorgu, setSorgu] = useSearchParams();
  const sekmeDegeri = sorgu.get('sekme') as Sekme | null;
  const sekme: Sekme =
    sekmeDegeri && PROJE_SEKMELERI.includes(sekmeDegeri) ? sekmeDegeri : 'ozet';

  const setSekme = (d: Sekme) => {
    if (d === 'ozet') sorgu.delete('sekme');
    else sorgu.set('sekme', d);
    setSorgu(sorgu, { replace: true });
  };
  const [silOnayi, setSilOnayi] = useState(false);

  const { data: proje, isLoading, isError, error } = useProject(projeId);
  const m = useProjectMutations(projeId);

  const gorevler = useTasks({
    projeId,
    boyut: 100,
    sirala: 'sla',
    azalan: false,
  });

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-28 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (isError || !proje) {
    return (
      <EmptyState
        ikon={FolderKanban}
        baslik="Proje bulunamadı"
        aciklama={(error as Error)?.message ?? 'Bu proje silinmiş ya da biriminizin dışında olabilir.'}
        eylem={
          <Link to="/projeler">
            <Button varyant="ikincil">
              <ArrowLeft size={14} />
              Projelere dön
            </Button>
          </Link>
        }
      />
    );
  }

  const toplam = proje.gorevToplam ?? 0;
  const biten = proje.gorevBiten ?? 0;

  // Yüzde SUNUCUDAN: bağlı görevlerin ilerleme ortalaması. Gerekçesi
  // `Projects.tsx` içinde ve `GorevDurumAkisi.Ilerleme` üzerinde yazılı.
  const oran = proje.ilerleme ?? 0;

  return (
    <div className="space-y-3.5">
      {/* ── Başlık ── */}
      <div className="flex items-start gap-2">
        <Link to="/projeler" className="mt-0.5">
          <IconButton etiket="Projelere dön">
            <ArrowLeft size={18} />
          </IconButton>
        </Link>

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            {proje.kod && (
              <span className="font-mono text-2xs tabular-nums text-ink-3">{proje.kod}</span>
            )}
            <ColoredBadge etiket={proje.durumAd} renk={proje.durumRenk} />
            {proje.gecikti && (
              <span className="text-2xs font-medium text-(--st-no)">Süre aşıldı</span>
            )}
          </div>
          <h1 className="mt-1 font-display text-lg font-bold leading-tight text-ink metin-guzel">
            {proje.ad}
          </h1>

          {/* "Bu proje kimde?" — görev detayındaki sorumlu satırının aynısı. */}
          {proje.yoneticiAd && (
            <div className="mt-1.5 flex items-center gap-1.5 text-xs text-text-2">
              <Avatar ad={proje.yoneticiAd} boyut="kucuk" />
              <span className="min-w-0 truncate">{proje.yoneticiAd}</span>
            </div>
          )}
        </div>

        {/* Düzenle/Sil masaüstünde ikon; telefonda başlık satırı zaten kod,
            durum ve gecikme rozetiyle dolu. Mobilde ikisi de Özet sekmesinin
            altındaki yönetim satırında. */}
        <div className="hidden items-center gap-2 lg:flex">
          {hasPermission(PERMISSION.projeYonet) && (
            <>
              <Link to={`/projeler/${projeId}/duzenle`}>
                <IconButton etiket="Düzenle">
                  <Pencil size={17} />
                </IconButton>
              </Link>
              <IconButton etiket="Sil" onClick={() => setSilOnayi(true)}>
                <Trash2 size={17} />
              </IconButton>
            </>
          )}
        </div>
      </div>

      {/*
        ── İlerleme şeridi ──

        Önceki hâlde yüzde, tarih aralığı, bütçe, birim, yönetici, adres,
        açıklama ve gecikme uyarısı TEK KARTIN içindeydi: dört ayrı soruya
        cevap veren dört farklı bilgi türü, aynı kutuda ve aynı ağırlıkta.
        Kullanıcının "kalabalık" dediği şeyin en somut örneği.

        Yüzde işin durumu — ekranın en üstünde ve tek başına. Künye bilgileri
        (birim, yönetici, bütçe, adres) referans; Özet sekmesine, etiketli bir
        tanım listesine taşındı — görev detayındaki gramerin aynısı.
      */}
      <Card serit className="p-3.5">
        <div className="flex items-baseline justify-between gap-3">
          <span className="font-display text-2xl font-bold tabular-nums leading-none text-ink">
            %{oran}
          </span>
          <span className="text-2xs text-ink-3">
            {toplam === 0 ? 'Görev bağlanmamış' : `${biten}/${toplam} görev kapandı`}
          </span>
        </div>

        <span className="mt-2 block h-1.5 overflow-hidden rounded-full bg-sunken" aria-hidden>
          <span
            className="block h-full rounded-full bg-brand transition-[width]"
            style={{ width: `${oran}%` }}
          />
        </span>

        {(proje.gorevGeciken ?? 0) > 0 && (
          <p className="mt-2 text-xs font-medium text-(--st-no)">
            {proje.gorevGeciken} görevde süre aşıldı.
          </p>
        )}
      </Card>

      {/* ── Sekmeler ── */}
      <Tabs<Sekme>
        deger={sekme}
        degistir={setSekme}
        sekmeler={[
          { deger: 'ozet', etiket: 'Özet' },
          { deger: 'pano', etiket: 'Pano' },
          { deger: 'gantt', etiket: 'Gantt' },
          { deger: 'gorevler', etiket: 'Görevler', sayi: toplam },
        ]}
      />

      {sekme === 'ozet' && (
        <div className="space-y-3.5">
          <ProjeKunyesi proje={proje} />

          {/* Yönetim eylemleri TELEFONDA burada: başlık satırında yer yok ve
              silme gibi geri alınamaz bir işlem, kazara basılabilecek bir
              köşede durmamalı. */}
          {hasPermission(PERMISSION.projeYonet) && (
            <div className="flex gap-2 lg:hidden">
              <Link to={`/projeler/${projeId}/duzenle`} className="flex-1">
                <Button varyant="ikincil" className="w-full">
                  <Pencil size={15} />
                  Düzenle
                </Button>
              </Link>
              <Button varyant="yikici" onClick={() => setSilOnayi(true)}>
                <Trash2 size={15} />
                Sil
              </Button>
            </div>
          )}

          <Card serit>
            <CardHeader
              baslik="Kilometre taşları"
              aciklama={
                (proje.kilometreTaslari ?? []).length === 0
                  ? undefined
                  : `${proje.kilometreTasiBiten}/${proje.kilometreTasiToplam} tamamlandı`
              }
              eylem={
                hasPermission(PERMISSION.projeYonet)
                  && (proje.kilometreTaslari ?? []).length > 0 ? (
                    <IconButton etiket="Kilometre taşı ekle" onClick={() => setTasFormu(true)}>
                      <Plus size={16} />
                    </IconButton>
                  ) : undefined
              }
            />

            {(proje.kilometreTaslari ?? []).length === 0 ? (
              <div className="px-3.5 pb-4">
                {/* Boş durum kendisini dolduran eylemi taşır: "projeyi
                    düzenleyerek tanımlayabilirsiniz" deyip düzenlemeye
                    götürmemek, kullanıcıyı sayfanın tepesindeki kalem
                    ikonunu aramaya bırakır. */}
                <EmptyState
                  ikon={Circle}
                  baslik="Kilometre taşı yok"
                  aciklama="Ara hedefler, gantt çizelgesinde ve ilerleme oranında görünür."
                  eylem={
                    hasPermission(PERMISSION.projeYonet) ? (
                      <Button onClick={() => setTasFormu(true)}>
                        <Plus size={14} />
                        Hedef ekle
                      </Button>
                    ) : undefined
                  }
                />
              </div>
            ) : (
              <ol className="divide-y divide-line">
                {proje.kilometreTaslari!.map((k) => (
                  <li key={k.id} className="flex items-center gap-2.5 px-3.5 py-2.5">
                    {/*
                      Tamamlanma ELLE işaretleniyor. "Bağlı görevlerin hepsi
                      bitince kendiliğinden" denebilirdi ama hiç görev
                      bağlanmamış bir taş açılır açılmaz tamamlanmış
                      görünürdü.
                    */}
                    <button
                      type="button"
                      disabled={!hasPermission(PERMISSION.projeYonet) || m.kilometreTasi.isPending}
                      onClick={async () => {
                        try {
                          await m.kilometreTasi.mutateAsync({
                            tasId: k.id!,
                            tamamlandi: !k.tamamlandi,
                          });
                        } catch (h) {
                          bildir('hata', 'Güncellenemedi', (h as Error).message);
                        }
                      }}
                      aria-label={k.tamamlandi ? `${k.ad} yeniden aç` : `${k.ad} tamamla`}
                      className="flex-none text-ink-3 disabled:cursor-default"
                    >
                      {k.tamamlandi ? (
                        <CheckCircle2 size={17} className="text-(--st-ok)" />
                      ) : (
                        <Circle size={17} />
                      )}
                    </button>

                    <span className="min-w-0 flex-1">
                      <span
                        className={`block truncate text-sm ${
                          k.tamamlandi ? 'text-text-3 line-through' : 'text-ink'
                        }`}
                      >
                        {k.ad}
                      </span>
                      <span className="text-2xs text-ink-3">
                        {k.hedefTarih ? shortDate(k.hedefTarih) : 'Tarihsiz'}
                        {(k.gorevToplam ?? 0) > 0 &&
                          ` · ${k.gorevBiten}/${k.gorevToplam} görev · %${k.ilerleme ?? 0}`}
                      </span>
                    </span>

                    {k.gecikti && (
                      <span className="shrink-0 text-2xs font-medium text-(--st-no)">gecikti</span>
                    )}

                    {/* Hedefe DOĞRUDAN iş bağlamak en sık işlem; görev
                        formunu proje ve taş seçili açıyor. */}
                    {hasPermission(PERMISSION.gorevEkle) && !k.tamamlandi && (
                      <Link
                        to={`/gorevler/yeni?proje=${projeId}&tas=${k.id}`}
                        className="shrink-0"
                        title={`${k.ad} hedefine görev ekle`}
                      >
                        <IconButton etiket={`${k.ad} hedefine görev ekle`}>
                          <Plus size={16} />
                        </IconButton>
                      </Link>
                    )}
                  </li>
                ))}
              </ol>
            )}
          </Card>

          <ProjectTeam proje={proje} />
        </div>
      )}

      {/* Pano ve gantt TEMBEL: yalnızca sekme açıkken veri çekiyor. */}
      {sekme === 'pano' && <Board projeId={projeId} etkin />}
      {sekme === 'gantt' && (
        <Card serit className="p-2">
          <Gantt projeId={projeId} etkin />
        </Card>
      )}

      {sekme === 'gorevler' && (
        <Card serit>
          <CardHeader
            baslik="Projenin görevleri"
            aciklama={`${toplam} görev`}
            eylem={
              hasPermission(PERMISSION.gorevEkle) ? (
                // Proje ÖNCEDEN SEÇİLİ açılıyor: kullanıcı zaten bu projenin
                // içinden "görev ekle" dedi, listeden yeniden bulmak zorunda
                // kalmamalı.
                <Link to={`/gorevler/yeni?proje=${projeId}`}>
                  <Button varyant="sade">
                    <Plus size={14} />
                    Görev ekle
                  </Button>
                </Link>
              ) : undefined
            }
          />
          {(gorevler.data?.veriler ?? []).length === 0 ? (
            <div className="px-3.5 pb-4">
              <EmptyState
                ikon={FolderKanban}
                baslik="Görev yok"
                aciklama="Projeye bağlı bir iş yok."
                eylem={
                  hasPermission(PERMISSION.gorevEkle) ? (
                    <Link to={`/gorevler/yeni?proje=${projeId}`}>
                      <Button>
                        <Plus size={14} />
                        Görev ekle
                      </Button>
                    </Link>
                  ) : undefined
                }
              />
            </div>
          ) : (
            <ul className="divide-y divide-line">
              {gorevler.data!.veriler.map((g) => (
                <li key={g.id}>
                  <Link
                    to={`/gorevler/${g.id}`}
                    className="flex items-center gap-2.5 px-3.5 py-2.5 hover:bg-sunken"
                  >
                    <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm text-ink">{g.baslik}</span>
                      <span className="flex items-center gap-2 text-2xs text-ink-3">
                        <span className="font-mono tabular-nums">{g.takipNo}</span>
                        <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} ilerleme={g.ilerleme} />
                      </span>
                    </span>
                    <SlaBadge gecikti={!!g.gecikti} kalanSaat={g.kalanSaat} kisa />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      )}

      <ConfirmDialog
        acik={silOnayi}
        kapat={() => setSilOnayi(false)}
        baslik="Proje silinsin mi?"
        aciklama={
          'Kilometre taşları, pano ve ekip silinir. GÖREVLER SİLİNMEZ — proje bağları ' +
          'boşalır ve görev listesinde durmaya devam ederler.'
        }
        onayEtiketi="Sil"
        yikici
        onayla={async () => {
          try {
            await m.sil.mutateAsync(projeId);
            bildir('basari', 'Proje silindi');
            gezin('/projeler');
          } catch (h) {
            bildir('hata', 'Proje silinemedi', (h as Error).message);
          }
        }}
      />

      {/*
        ARA HEDEF PENCERESİ — tek satırlık iş, tek pencere.

        Alanlar bilerek üç tane: ad zorunlu, açıklama ve hedef tarih isteğe
        bağlı. Bağlı görev, sıra numarası ve tamamlanma buraya girmiyor —
        sıra sunucuda veriliyor, tamamlanma listeden işaretleniyor.
      */}
      <FormModal
        acik={tasFormu}
        kapat={() => setTasFormu(false)}
        baslik="Kilometre taşı ekle"
        aciklama="Ara hedefler gantt çizelgesinde ve ilerleme oranında görünür."
        genislik="dar"
        eylemler={
          <>
            <Button varyant="ikincil" onClick={() => setTasFormu(false)}>
              Vazgeç
            </Button>
            <Button
              disabled={!tasAd.trim() || m.kilometreTasiEkle.isPending}
              onClick={async () => {
                try {
                  await m.kilometreTasiEkle.mutateAsync({
                    ad: tasAd.trim(),
                    aciklama: tasAciklama.trim() || undefined,
                    hedefTarih: tasTarih || null,
                  });
                  bildir('basari', 'Kilometre taşı eklendi', tasAd.trim());
                  setTasAd('');
                  setTasAciklama('');
                  setTasTarih('');
                  setTasFormu(false);
                } catch (h) {
                  bildir('hata', 'Eklenemedi', (h as Error).message);
                }
              }}
            >
              {m.kilometreTasiEkle.isPending ? 'Ekleniyor…' : 'Ekle'}
            </Button>
          </>
        }
      >
        <div className="space-y-3 p-3.5">
          <FieldWrapper etiket="Hedef adı" id="kt-ad" zorunlu>
            <Input
              id="kt-ad"
              value={tasAd}
              onChange={(e) => setTasAd(e.target.value)}
              placeholder="Örn. Zemin etüdü tamamlanacak"
            />
          </FieldWrapper>

          <FieldWrapper etiket="Hedef tarih" id="kt-tarih">
            <DatePicker id="kt-tarih" deger={tasTarih} degistir={setTasTarih} />
          </FieldWrapper>

          <FieldWrapper etiket="Açıklama" id="kt-aciklama">
            <Textarea
              id="kt-aciklama"
              rows={3}
              value={tasAciklama}
              onChange={(e) => setTasAciklama(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </FormModal>
    </div>
  );
}

/**
 * PROJENİN KÜNYESİ — etiketli tanım listesi.
 *
 * <p>
 * Önceden başlığın altında, sarmalanan ikon+metin çiftleri hâlinde
 * duruyordu: "Fen İşleri Müdürlüğü", "01.03.2026 → 30.09.2026",
 * "4.500.000 ₺" ve "Kent Meydanı" yan yana, aynı ağırlıkta, hangisinin neyi
 * anlattığı ancak ikondan tahmin edilebilir. Görev detayında düzeltilen
 * kusurun aynısı; çözüm de aynı.
 * </p>
 */
function ProjeKunyesi({ proje }: { proje: Proje }) {
  const satirVar =
    proje.aciklama || proje.birimAd || proje.baslangic || proje.bitis
    || proje.butce != null || proje.adres;

  if (!satirVar) return null;

  return (
    <Card serit>
      <CardHeader baslik="Künye" />
      <div className="space-y-3 p-3.5">
        {proje.aciklama && (
          <p className="whitespace-pre-wrap text-sm leading-relaxed text-text-2">
            {proje.aciklama}
          </p>
        )}

        <dl className="space-y-2 text-xs">
          <Satir ikon={<Building2 size={13} />} etiket="Birim" deger={proje.birimAd} />
          <Satir ikon={<User size={13} />} etiket="Yönetici" deger={proje.yoneticiAd} />
          <Satir
            ikon={<Calendar size={13} />}
            etiket="Süre"
            deger={
              proje.baslangic || proje.bitis
                ? `${proje.baslangic ? shortDate(proje.baslangic) : '—'} → ${
                    proje.bitis ? shortDate(proje.bitis) : '—'
                  }`
                : null
            }
          />
          <Satir
            ikon={<Wallet size={13} />}
            etiket="Bütçe"
            deger={proje.butce != null ? `${proje.butce.toLocaleString('tr-TR')} ₺` : null}
          />
          <Satir ikon={<MapPin size={13} />} etiket="Konum" deger={proje.adres} />
        </dl>
      </div>
    </Card>
  );
}

/** Künye satırı — boş değer hiç çizilmez. */
function Satir({
  ikon,
  etiket,
  deger,
}: {
  ikon: React.ReactNode;
  etiket: string;
  deger?: string | null;
}) {
  if (!deger) return null;

  return (
    <div className="flex items-start gap-2">
      <span className="mt-px text-text-3">{ikon}</span>
      <dt className="w-16 shrink-0 text-ink-3">{etiket}</dt>
      <dd className="min-w-0 flex-1 text-text-2">{deger}</dd>
    </div>
  );
}
