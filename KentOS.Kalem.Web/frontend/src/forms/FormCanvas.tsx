import {
  DndContext, KeyboardSensor, PointerSensor, closestCenter,
  useSensor, useSensors, type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext, sortableKeyboardCoordinates, useSortable, verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Copy, GripVertical, Plus, Trash2 } from 'lucide-react';
import { Button, IconButton } from '../components/Button';
import { Card } from '../components/Card';
import { EmptyState } from '../components/EmptyState';
import { Input } from '../components/Field';
import { cn } from '../components/utils';
import type { FormDefinition, FormField } from '../data/types';
import { FormFieldInput } from './FormFieldInput';
import { fieldTypeInfo, isBlock } from './fieldTypes';

/**
 * TASARIM TUVALİ — formun şekli.
 *
 * <p>
 * Her alan, <b>oynatıcının çizdiği görünümüyle</b> duruyor: tasarımcıda
 * gördüğün ile vatandaşın göreceği aynı bileşen. Ayrı bir "tasarım
 * görünümü" çizseydik ikisi zamanla ayrışırdı — anket araçlarında en pahalı
 * hata sınıfı bu.
 * </p>
 *
 * <p>
 * <b>Sıralama sürükleyerek</b> (<c>@dnd-kit/sortable</c>): tuvalde hedef
 * görünür olduğu için sürükleme doğal. Paletten tuvale sürükleme YOK —
 * o telefonda çalışmıyor ve bu araç telefonda da kullanılacak.
 * </p>
 */
export function FormCanvas({
  tanim, adim, secili, sec, siralaDegisti, kopyala, sil, grupEkle, grupYaz,
}: {
  tanim: FormDefinition;
  adim: number;
  secili: string | null;
  sec: (kimlik: string) => void;
  siralaDegisti: (grup: number, kaynak: string, hedefIndeks: number) => void;
  kopyala: (kimlik: string) => void;
  sil: (kimlik: string) => void;
  grupEkle: () => void;
  grupYaz: (grup: number, kismi: { baslik?: string; kolonSayisi?: number | null }) => void;
}) {
  const sensorler = useSensors(
    // 6px eşik: dokunmatikte kaydırma ile sürüklemeyi ayırıyor. Eşiksiz
    // bir sensör, listeyi kaydırmaya çalışan parmağı sürükleme sanıyor.
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const gruplar = (tanim.adimlar ?? [])[adim]?.gruplar ?? [];

  return (
    <div className="space-y-4">
      {gruplar.map((grup, gi) => {
        const alanlar = grup.alanlar ?? [];

        return (
          <Card key={grup.kimlik} className="overflow-visible p-3.5">
            <div className="mb-3 flex items-center gap-2">
              <Input
                className="h-9 flex-1 text-sm font-semibold"
                placeholder="Bölüm başlığı (isteğe bağlı)"
                value={grup.baslik ?? ''}
                onChange={(e) => grupYaz(gi, { baslik: e.target.value })}
              />
              <select
                aria-label="Bölüm kolon sayısı"
                className="h-9 shrink-0 rounded-md border border-line bg-surface-2 px-2 text-xs"
                value={String(grup.kolonSayisi ?? '')}
                onChange={(e) => grupYaz(gi, {
                  kolonSayisi: e.target.value ? Number(e.target.value) : null })}
              >
                <option value="">Form geneli</option>
                <option value="1">1 kolon</option>
                <option value="2">2 kolon</option>
                <option value="3">3 kolon</option>
              </select>
            </div>

            {alanlar.length === 0 ? (
              <EmptyState
                ikon={Plus}
                baslik="Bu bölüm boş"
                aciklama="Soldaki paletten bir alan seçin."
              />
            ) : (
              <DndContext
                sensors={sensorler}
                collisionDetection={closestCenter}
                onDragEnd={(o: DragEndEvent) => {
                  const kaynak = String(o.active.id);
                  const hedef = alanlar.findIndex((a) => a.kimlik === String(o.over?.id));
                  if (hedef >= 0 && o.over?.id !== o.active.id) siralaDegisti(gi, kaynak, hedef);
                }}
              >
                <SortableContext
                  items={alanlar.map((a) => a.kimlik ?? '')}
                  strategy={verticalListSortingStrategy}
                >
                  <div className="space-y-2">
                    {alanlar.map((alan) => (
                      <SiralanabilirAlan
                        key={alan.kimlik}
                        alan={alan}
                        secili={secili === alan.kimlik}
                        sec={() => sec(alan.kimlik ?? '')}
                        kopyala={() => kopyala(alan.kimlik ?? '')}
                        sil={() => sil(alan.kimlik ?? '')}
                      />
                    ))}
                  </div>
                </SortableContext>
              </DndContext>
            )}
          </Card>
        );
      })}

      <Button varyant="ikincil" className="w-full" onClick={grupEkle}>
        <Plus size={14} />
        Bölüm ekle
      </Button>
    </div>
  );
}

function SiralanabilirAlan({
  alan, secili, sec, kopyala, sil,
}: {
  alan: FormField;
  secili: boolean;
  sec: () => void;
  kopyala: () => void;
  sil: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: alan.kimlik ?? '' });

  const bilgi = fieldTypeInfo(alan.tip);

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn(
        'rounded-md border bg-surface transition-colors',
        secili ? 'border-brand ring-[3px] ring-(--focus-ring)' : 'border-line hover:border-line-2',
        isDragging && 'z-10 opacity-80 shadow-3',
      )}
    >
      <div className="flex items-center gap-1 border-b border-line px-2 py-1.5">
        {/*
          TUTAMAK ayrı bir düğme: bütün kartı sürüklenebilir yapmak, alanın
          içindeki girdiye dokunmayı da sürükleme sanıyordu.
        */}
        <button
          type="button"
          {...attributes}
          {...listeners}
          aria-label="Sırala"
          className="grid size-8 cursor-grab place-items-center rounded-sm text-ink-3 active:cursor-grabbing"
        >
          <GripVertical size={15} />
        </button>

        <button
          type="button"
          onClick={sec}
          className="flex min-w-0 flex-1 items-center gap-1.5 py-1 text-left"
        >
          <bilgi.ikon size={13} className="shrink-0 text-ink-3" />
          <span className="truncate text-2xs text-ink-3">{bilgi.ad}</span>
          {alan.zorunlu && <span className="text-2xs text-(--st-no)">zorunlu</span>}
          {(alan.kosul?.kurallar?.length ?? 0) > 0 && (
            <span className="rounded-sm bg-gold-tint px-1 text-2xs text-gold-2">koşullu</span>
          )}
        </button>

        {!isBlock(alan.tip) && (
          <IconButton etiket="Alanı kopyala" varyant="sade" onClick={kopyala}>
            <Copy size={14} />
          </IconButton>
        )}
        <IconButton etiket="Alanı sil" varyant="sade" onClick={sil}>
          <Trash2 size={14} />
        </IconButton>
      </div>

      {/*
        ALAN ÖNİZLEMESİ PASİF: tasarımcı formu DOLDURMUYOR, kuruyor.
        Etkileşimli bırakılsaydı bir seçeneği tıklamak "ayar mı değişti,
        cevap mı verdim" belirsizliği üretirdi.

        DIŞ KAP `div`, `button` DEĞİL: içeride girdi ve düğme var ve
        `button > button` geçersiz HTML — tarayıcı davranışı tanımsız
        (bu depoda ölçülmüş bir hata). Tıklama `onClick` ile alınıyor,
        `pointer-events-none` da önizlemenin kendi öğelerini geçirgen
        yapıyor.
      */}
      <div
        role="button"
        tabIndex={0}
        onClick={sec}
        onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); sec(); } }}
        aria-label={`${alan.etiket} alanını seç`}
        className="block w-full cursor-pointer px-3 py-2.5 text-left"
      >
        <div className="pointer-events-none">
          <FormFieldInput alan={alan} cevap={undefined} degistir={() => {}} pasif />
        </div>
      </div>
    </div>
  );
}
