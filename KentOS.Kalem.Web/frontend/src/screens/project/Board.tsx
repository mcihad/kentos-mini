import {
  DndContext, DragOverlay, PointerSensor, useDraggable, useDroppable,
  useSensor, useSensors, type DragEndEvent, type DragStartEvent,
} from '@dnd-kit/core';
import { AlertTriangle, GripVertical, Inbox, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { EmptyState } from '../../components/EmptyState';
import { Skeleton } from '../../components/Skeleton';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { useBoard, useProjectMutations } from '../../data/projects';
import type { TaskSummary } from '../../data/types';
import { SlaBadge, StageProgress } from '../task/TaskBits';

/**
 * KANBAN PANOSU.
 *
 * <p>
 * <b>Sütun bir görev durumuna eşli, ayrı bir durum kaynağı değil.</b> Kartı
 * bırakmak görevin durumunu değiştiriyor ve geçiş sunucudaki durum akışından
 * geçiyor — panoyu akışın dışında tutsaydık kartı sürükleyerek onay kapısı
 * atlanabilirdi.
 * </p>
 *
 * <p>
 * <b>İyimser güncelleme YOK.</b> Sunucu geçişi reddedebiliyor (atanmamış
 * görev, onay kapısı). Kartı önce taşıyıp sonra geri almak, kullanıcıya bir
 * an "oldu" dedirtip sonra sebepsizce geri alırdı; onun yerine sunucunun
 * cevabı bekleniyor ve ret bir bildirim olarak gösteriliyor.
 * </p>
 *
 * <p>
 * <b>Sütun içi sıralama yok</b> ve bu bilinçli: kart sırası sunucudan geliyor
 * (öncelik, sonra en az vakti kalan). Elle sıralama eklenseydi yazılacağı bir
 * yer olmazdı ve tazelemede kaybolurdu — <c>@dnd-kit/sortable</c> bu yüzden
 * eklenmedi.
 * </p>
 */
export function Board({ projeId, etkin }: { projeId: number; etkin: boolean }) {
  const { bildir } = useToast();
  const { hasPermission } = useSession();
  const { data: pano, isLoading } = useBoard(projeId, etkin);
  const m = useProjectMutations(projeId);

  const [surukleyen, setSurukleyen] = useState<TaskSummary | null>(null);

  const yetkili = hasPermission(PERMISSION.projeYonet);

  // Dokunarak kaydırmayı bozmamak için 6px eşik: eşiksiz bir işaretçi
  // duyarlayıcısı, panoyu yatay kaydırmak isteyen parmağı sürükleme
  // sanıyor ve kart yerinden oynuyordu.
  const duyarlayicilar = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
  );

  if (isLoading) {
    return (
      <div className="flex gap-3 overflow-x-auto">
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-64 w-72 flex-none" />
        ))}
      </div>
    );
  }

  // Swagger üretimi bütün alanları isteğe bağlı işaretliyor; sunucu her
  // zaman dolu gönderiyor ama tip düzeyinde varsayım yapmak yerine
  // normalleştiriliyor — boş listeyle çizmek, `undefined` ile patlamaktan
  // her zaman iyi.
  const sutunlar = pano?.sutunlar ?? [];
  const dagitilmayanlar = pano?.dagitilmayanlar ?? [];

  if (!pano || sutunlar.length === 0) {
    return (
      // Sütunlar proje formunda tanımlanıyor; boş panodan oraya bir yol
      // yoktu ve kullanıcı "sütun tanımlayın" cümlesiyle baş başa kalıyordu.
      <EmptyState
        ikon={Inbox}
        baslik="Pano kurulmamış"
        aciklama="Her sütun bir görev durumuna karşılık gelir; kartlar sürüklenince durum değişir."
        eylem={
          <Link to={`/projeler/${projeId}/duzenle`}>
            <Button>
              <Plus size={14} />
              Sütunları tanımla
            </Button>
          </Link>
        }
      />
    );
  }

  function baslangic(o: DragStartEvent) {
    const kart = (o.active.data.current as { kart?: TaskSummary } | undefined)?.kart;
    setSurukleyen(kart ?? null);
  }

  async function bitis(o: DragEndEvent) {
    setSurukleyen(null);

    const hedef = o.over?.id;
    const gorevId = Number(o.active.id);
    if (!hedef || !gorevId) return;

    const hedefSutunId = Number(String(hedef).replace('sutun-', ''));
    if (!hedefSutunId) return;

    try {
      await m.kartTasi.mutateAsync({ gorevId, hedefSutunId });
    } catch (h) {
      // Sunucu reddettiyse SEBEBİ gösteriliyor: "onay bekleyen görev
      // tamamlandıya taşınamaz" gibi bir kural, sessizce geri alınırsa
      // kullanıcı panonun bozuk olduğunu sanar.
      bildir('hata', 'Kart taşınamadı', (h as Error).message);
    }
  }

  return (
    <DndContext sensors={duyarlayicilar} onDragStart={baslangic} onDragEnd={bitis}>
      {/*
        `items-start`: sütunlar İÇERİĞİ kadar uzasın.

        Varsayılan `stretch` ile boş sütunlar kapsayıcı boyunca uzuyor ve
        ekran görüntüsünde yedi yüz piksellik boş kutular çıktı. Eşit
        yükseklik kanban'da alışıldık ama buradaki eşitlik içerikten değil
        kapsayıcıdan geliyordu; bırakma hedefi zaten `min-h` ile yeterince
        büyük.
      */}
      <div className="flex items-start gap-3 overflow-x-auto pb-2">
        {sutunlar.map((s) => (
          <Sutun
            key={s.sutun?.id}
            id={`sutun-${s.sutun?.id}`}
            baslik={s.sutun?.ad ?? ''}
            renk={s.sutun?.renk}
            sayi={(s.kartlar ?? []).length}
            surukleneblir={yetkili}
          >
            {(s.kartlar ?? []).map((k) => (
              <Kart key={k.id} kart={k} surukleneblir={yetkili} />
            ))}
          </Sutun>
        ))}

        {/*
          DAĞITILMAYANLAR — panoda karşılığı olmayan durumdaki görevler.

          Ayrı bir sütun olarak gösteriliyor ama hedef DEĞİL: buraya bırakmak
          bir duruma karşılık gelmiyor. Görünmeseydi bu işler panodan tamamen
          kaybolur ve pano eksik bir resim gösterirdi.
        */}
        {dagitilmayanlar.length > 0 && (
          <Sutun
            id="dagitilmayan"
            baslik="Sütunsuz"
            renk="#7C8592"
            sayi={dagitilmayanlar.length}
            surukleneblir={false}
            hedefDegil
            ipucu="Bu görevlerin durumuna karşılık gelen bir sütun yok."
          >
            {/*
              BURADAN SÜRÜKLENEBİLİR, BURAYA BIRAKILAMAZ.

              Kart sürüklenemez yapılmıştı ve tarayıcıda ölçünce görüldü:
              panoda karşılığı olmayan bir duruma düşen görev buraya
              geliyor ve bir daha çıkarılamıyordu. Çıkış yolu olmayan bir
              kutu, panoyu bir çöp kutusuna çevirir.
            */}
            {dagitilmayanlar.map((k) => (
              <Kart key={k.id} kart={k} surukleneblir={yetkili} />
            ))}
          </Sutun>
        )}
      </div>

      {/* Sürüklenen kartın hayaleti — parmağın altında ne olduğu görünsün. */}
      <DragOverlay>
        {surukleyen && (
          <div className="w-72 rotate-1 opacity-90">
            <KartGovdesi kart={surukleyen} />
          </div>
        )}
      </DragOverlay>
    </DndContext>
  );
}

function Sutun({
  id,
  baslik,
  renk,
  sayi,
  children,
  surukleneblir,
  hedefDegil,
  ipucu,
}: {
  id: string;
  baslik: string;
  renk?: string | null;
  sayi: number;
  children: React.ReactNode;
  surukleneblir: boolean;
  hedefDegil?: boolean;
  ipucu?: string;
}) {
  const { setNodeRef, isOver } = useDroppable({ id, disabled: hedefDegil || !surukleneblir });

  return (
    <section
      ref={setNodeRef}
      className={`flex w-72 flex-none flex-col rounded-lg border bg-sunken/40 transition-colors ${
        isOver ? 'border-brand bg-sunken' : 'border-line'
      }`}
      aria-label={baslik}
    >
      <header className="flex items-center gap-2 border-b border-line px-3 py-2">
        <span
          className="h-2 w-2 flex-none rounded-full"
          style={{ background: renk ?? 'var(--brand-ui)' }}
          aria-hidden
        />
        <span className="min-w-0 flex-1 truncate text-xs font-medium text-ink" title={ipucu}>
          {baslik}
        </span>
        <span className="text-2xs tabular-nums text-ink-3">{sayi}</span>
      </header>

      <div className="flex min-h-32 flex-1 flex-col gap-2 p-2">
        {sayi === 0 ? (
          <p className="px-1 py-3 text-center text-2xs text-ink-3">Boş</p>
        ) : (
          children
        )}
      </div>
    </section>
  );
}

function Kart({ kart, surukleneblir }: { kart: TaskSummary; surukleneblir: boolean }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: kart.id!,
    data: { kart },
    disabled: !surukleneblir,
  });

  return (
    <div ref={setNodeRef} className={isDragging ? 'opacity-40' : undefined}>
      <KartGovdesi kart={kart} tutamak={surukleneblir ? { ...listeners, ...attributes } : undefined} />
    </div>
  );
}

function KartGovdesi({
  kart,
  tutamak,
}: {
  kart: TaskSummary;
  tutamak?: Record<string, unknown>;
}) {
  return (
    <Card className="p-2.5">
      <div className="flex items-start gap-1.5">
        {tutamak && (
          <button
            type="button"
            {...tutamak}
            aria-label={`${kart.baslik} kartını taşı`}
            className="-ml-1 mt-0.5 cursor-grab touch-none text-ink-3 active:cursor-grabbing"
          >
            <GripVertical size={14} />
          </button>
        )}

        <div className="min-w-0 flex-1">
          <Link
            to={`/gorevler/${kart.id}`}
            className="line-clamp-2 text-xs leading-snug text-ink hover:underline"
          >
            {kart.baslik}
          </Link>

          <div className="mt-1.5 flex flex-wrap items-center gap-x-2 gap-y-1">
            <span className="font-mono text-2xs tabular-nums text-ink-3">{kart.takipNo}</span>
            {kart.oncelik !== 1 && kart.oncelikAd && (
              <span
                className={`text-2xs ${
                  (kart.oncelik ?? 1) > 1 ? 'font-medium text-(--st-wait)' : 'text-ink-3'
                }`}
              >
                {kart.oncelikAd}
              </span>
            )}
            <SlaBadge gecikti={!!kart.gecikti} kalanSaat={kart.kalanSaat} kisa />
          </div>

          <div className="mt-1.5 flex items-center gap-2">
            <StageProgress biten={kart.asamaBiten ?? 0} toplam={kart.asamaToplam ?? 0} ilerleme={kart.ilerleme} />
            {(kart.sorumlular ?? []).length > 0 && (
              <span className="truncate text-2xs text-ink-3">{(kart.sorumlular ?? [])[0]}</span>
            )}
          </div>
        </div>

        {kart.gecikti && (
          <AlertTriangle size={13} className="mt-0.5 flex-none text-(--st-no)" aria-hidden />
        )}
      </div>
    </Card>
  );
}
