import {
  ArrowLeft, Building2, Calendar, CheckCircle2, Circle, FolderKanban,
  MapPin, Pencil, Trash2, User, Wallet,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Button, IconButton } from '../components/Button';
import { Card, CardHeader } from '../components/Card';
import { ColoredBadge } from '../components/Color';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
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
  const oran = toplam === 0 ? 0 : Math.round((biten / toplam) * 100);

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
          <h1 className="mt-1 font-display text-lg font-bold leading-tight text-ink">
            {proje.ad}
          </h1>
        </div>

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

      {/* ── Künye ── */}
      <Card className="p-3.5">
        {proje.aciklama && (
          <p className="mb-3 whitespace-pre-wrap text-sm leading-relaxed text-text-2">
            {proje.aciklama}
          </p>
        )}

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs">
          {proje.birimAd && <Kunye ikon={<Building2 size={14} />}>{proje.birimAd}</Kunye>}
          {proje.yoneticiAd && <Kunye ikon={<User size={14} />}>{proje.yoneticiAd}</Kunye>}
          {(proje.baslangic || proje.bitis) && (
            <Kunye ikon={<Calendar size={14} />}>
              {proje.baslangic ? shortDate(proje.baslangic) : '—'}
              {' → '}
              {proje.bitis ? shortDate(proje.bitis) : '—'}
            </Kunye>
          )}
          {proje.butce != null && (
            <Kunye ikon={<Wallet size={14} />}>
              {proje.butce.toLocaleString('tr-TR')} ₺
            </Kunye>
          )}
          {proje.adres && <Kunye ikon={<MapPin size={14} />}>{proje.adres}</Kunye>}
        </div>

        {/* İlerleme GÖREVLERDEN; projede saklanan bir yüzde yok. */}
        <div className="mt-3">
          <div className="flex items-baseline justify-between text-2xs text-ink-3">
            <span>{toplam === 0 ? 'Henüz görev bağlanmamış' : `${biten}/${toplam} görev`}</span>
            {toplam > 0 && <span className="tabular-nums">%{oran}</span>}
          </div>
          <span className="mt-1 block h-1.5 overflow-hidden rounded-full bg-sunken" aria-hidden>
            <span
              className="block h-full rounded-full bg-brand-ui transition-[width]"
              style={{ width: `${oran}%` }}
            />
          </span>
        </div>

        {(proje.gorevGeciken ?? 0) > 0 && (
          <p className="mt-2 text-2xs font-medium text-(--st-no)">
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
          <Card>
            <CardHeader
              baslik="Kilometre taşları"
              aciklama={
                (proje.kilometreTaslari ?? []).length === 0
                  ? undefined
                  : `${proje.kilometreTasiBiten}/${proje.kilometreTasiToplam} tamamlandı`
              }
            />

            {(proje.kilometreTaslari ?? []).length === 0 ? (
              <div className="px-3.5 pb-4">
                <EmptyState
                  ikon={Circle}
                  baslik="Kilometre taşı yok"
                  aciklama="Projeyi düzenleyerek ara hedefler tanımlayabilirsiniz."
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
                        {(k.gorevToplam ?? 0) > 0 && ` · ${k.gorevBiten}/${k.gorevToplam} görev`}
                      </span>
                    </span>

                    {k.gecikti && (
                      <span className="shrink-0 text-2xs font-medium text-(--st-no)">gecikti</span>
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
        <Card className="p-2">
          <Gantt projeId={projeId} etkin />
        </Card>
      )}

      {sekme === 'gorevler' && (
        <Card>
          <CardHeader baslik="Projenin görevleri" aciklama={`${toplam} görev`} />
          {(gorevler.data?.veriler ?? []).length === 0 ? (
            <div className="px-3.5 pb-4">
              <EmptyState
                ikon={FolderKanban}
                baslik="Görev yok"
                aciklama="Görev açarken proje seçerek buraya bağlayabilirsiniz."
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
                        <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} />
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
    </div>
  );
}

function Kunye({ ikon, children }: { ikon: React.ReactNode; children: React.ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-text-2">
      <span className="text-text-3">{ikon}</span>
      {children}
    </span>
  );
}
