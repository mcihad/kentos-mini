import * as Dialog from '@radix-ui/react-dialog';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Archive, ArchiveRestore, ArrowUpFromLine, CalendarPlus, ChevronDown, MoreHorizontal,
  Pencil, Share2, Tag, Trash2,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ActionSheet, type SheetAction } from '../../components/ActionSheet';
import { useIsDesktop } from '../../components/screenSize';
import { FieldWrapper, Textarea, Secim } from '../../components/Field';
import { unitLabel } from '../../data/format';
import { Button } from '../../components/Button';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useToast } from '../../components/Toast';
import { cn } from '../../components/utils';
import { queryKeys } from '../../data/queryKeys';
import { api } from '../../data/client';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { useUnits, useEventTypes } from '../../data/hooks';
import { AddToAgendaModal } from './AddToAgendaModal';
import type { Request } from '../../data/types';

type Pencere = 'yok' | 'havale' | 'tip' | 'ajanda';

/**
 * Talep eylem çubuğu.
 *
 * <p>
 * Sık kullanılan üç eylem düğme olarak dışarıda (ajandaya ekle, düzenle,
 * havale), gerisi "Diğer" menüsünde. Eski arayüzde on bir düğme yan yanaydı
 * ve mobilde iki satıra taşıyordu.
 * </p>
 */
export function RequestActions({ talep }: { talep: Request }) {
  const masaustu = useIsDesktop();
  const talepId = talep.id!;
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();

  /**
   * Düğmeler İZNE göre gizlenir.
   *
   * Sunucu zaten reddediyor ama kullanıcıya çalışmayan bir düğme göstermek,
   * onu tıklayıp hata almaya davet etmek demek. "Başkan onaylar, personel
   * ekler" ayrımı ancak düğmeler de ayrıldığında görünür oluyor.
   */
  const { hasPermission } = useSession();

  const [pencere, setPencere] = useState<Pencere>('yok');
  const [silinecek, setSilinecek] = useState(false);

  function tazele() {
    qc.invalidateQueries({ queryKey: queryKeys.request.all() });
  }

    /*
      DÖNÜŞ DEĞERİ DENETLENİR.

      Uç `bool` döndürüyor ve BAŞARISIZLIĞI DA 200 ile bildiriyor: üst birim
      yoksa ya da kayıt bulunamazsa gövde `false` oluyor. İstemci yalnızca
      HTTP durumuna baktığı için her koşulda "Üst birime gönderildi" yazıyor,
      kullanıcı işlemin olduğunu sanıp listeye bakınca kaydı yerinde
      buluyordu.
    */
  const ustBirime = useMutation({
    mutationFn: () => api.post<boolean>(`/talep/${talepId}/ust-birime-gonder`),
    onSuccess: (oldu) => {
      if (!oldu) {
        bildir('uyari', 'Gönderilemedi',
          'Bu birimin bağlı olduğu bir üst birim yok.');
        return;
      }
      tazele();
      bildir('basari', 'Üst birime gönderildi');
    },
    onError: (h: Error) => bildir('hata', 'Gönderilemedi', h.message),
  });

  const arsivle = useMutation({
    mutationFn: (arsive: boolean) =>
      api.post<boolean>(`/talep/${talepId}/${arsive ? 'arsivle' : 'arsivden-cikar'}`),
    onSuccess: (_, arsive) => {
      tazele();
      bildir('basari', arsive ? 'Talep arşivlendi' : 'Talep arşivden çıkarıldı');
    },
    onError: (h: Error) => bildir('hata', 'İşlem yapılamadı', h.message),
  });

  const sil = useMutation({
    mutationFn: () => api.delete<void>(`/talep/${talepId}`),
    onSuccess: () => {
      tazele();
      bildir('basari', 'Talep silindi');
      gezin('/talepler');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  /*
    EYLEMLER TEK LİSTEDE — masaüstü düğme sırası ve mobil alt tabaka aynı
    kaynaktan besleniyor. İzin koşulları iki yerde tekrarlansaydı biri
    unutulduğunda kullanıcı çalışmayacak bir eylem görürdü.
  */
  const eylemler: SheetAction[] = [
    ...(!talep.ajandaDurum && hasPermission(PERMISSION.talepAjandayaEkle)
      ? [{ etiket: 'Ajandaya ekle', ikon: <CalendarPlus size={17} />, onClick: () => setPencere('ajanda') }]
      : talep.ajandaId
        ? [{ etiket: 'Etkinliğe git', ikon: <CalendarPlus size={17} />, onClick: () => gezin(`/ajanda/${talep.ajandaId}`) }]
        : []),
    ...(hasPermission(PERMISSION.talepDuzenle)
      ? [
          { etiket: 'Düzenle', ikon: <Pencil size={17} />, onClick: () => gezin(`/talepler/${talepId}/duzenle`) },
          { etiket: 'Talep tipini değiştir', ikon: <Tag size={17} />, onClick: () => setPencere('tip') },
        ]
      : []),
    ...(hasPermission(PERMISSION.talepHavale)
      ? [
          { etiket: 'Havale et', ikon: <Share2 size={17} />, onClick: () => setPencere('havale') },
          { etiket: 'Üst birime gönder', ikon: <ArrowUpFromLine size={17} />, onClick: () => ustBirime.mutate() },
        ]
      : []),
    ...(hasPermission(PERMISSION.talepArsivle)
      ? [
          {
            etiket: talep.arsivlendi ? 'Arşivden çıkar' : 'Arşivle',
            ikon: talep.arsivlendi ? <ArchiveRestore size={17} /> : <Archive size={17} />,
            onClick: () => arsivle.mutate(!talep.arsivlendi),
          },
        ]
      : []),
    ...(hasPermission(PERMISSION.talepSil)
      ? [{ etiket: 'Talebi sil', ikon: <Trash2 size={17} />, onClick: () => setSilinecek(true), ton: 'tehlike' as const }]
      : []),
  ];

  return (
    <>
      {/* Mobilde eylemlerin tamamı sağ alttaki FAB'da — bkz. `EylemTabakasi`. */}
      {!masaustu && <ActionSheet eylemler={eylemler} baslik="Talep işlemleri" />}

      <div className="hidden flex-wrap gap-2 md:flex">
        {!talep.ajandaDurum && hasPermission(PERMISSION.talepAjandayaEkle) ? (
          // Doğrudan çağırmıyor: tarih ve saat SEÇİLMELİ. Önceden yalnızca
          // `randevuId` gönderiliyordu ve sunucudaki tarih 0001-01-01
          // kalıyordu — etkinlik takvimde ulaşılamayacak bir yere düşüyordu.
          <Button onClick={() => setPencere('ajanda')}>
            <CalendarPlus size={14} />
            Ajandaya ekle
          </Button>
        ) : talep.ajandaId ? (
          <Link to={`/ajanda/${talep.ajandaId}`}>
            <Button varyant="ikincil">
              <CalendarPlus size={14} />
              Etkinliğe git
            </Button>
          </Link>
        ) : null}

        {hasPermission(PERMISSION.talepDuzenle) && (
          <Link to={`/talepler/${talepId}/duzenle`}>
            <Button varyant="ikincil">
              <Pencil size={14} />
              Düzenle
            </Button>
          </Link>
        )}

        {hasPermission(PERMISSION.talepHavale) && (
          <Button varyant="ikincil" onClick={() => setPencere('havale')}>
            <Share2 size={14} />
            Havale et
          </Button>
        )}

        <DropdownMenu.Root>
          <DropdownMenu.Trigger asChild>
            <Button varyant="ikincil">
              <MoreHorizontal size={14} />
              Diğer
              <ChevronDown size={12} />
            </Button>
          </DropdownMenu.Trigger>

          <DropdownMenu.Portal>
            <DropdownMenu.Content
              align="start"
              sideOffset={6}
              className="katman anim-katman z-400 min-w-[220px] rounded-card border border-border bg-surface p-1.5 shadow-3"
            >
              {hasPermission(PERMISSION.talepDuzenle) && (
                <Menu ikon={<Tag size={14} />} tikla={() => setPencere('tip')}>
                  Request tipini değiştir
                </Menu>
              )}
              {hasPermission(PERMISSION.talepHavale) && (
                <Menu
                  ikon={<ArrowUpFromLine size={14} />}
                  tikla={() => ustBirime.mutate()}
                >
                  Üst birime gönder
                </Menu>
              )}
              {hasPermission(PERMISSION.talepArsivle) && (
                <Menu
                  ikon={talep.arsivlendi ? <ArchiveRestore size={14} /> : <Archive size={14} />}
                  tikla={() => arsivle.mutate(!talep.arsivlendi)}
                >
                  {talep.arsivlendi ? 'Arşivden çıkar' : 'Arşivle'}
                </Menu>
              )}

              {hasPermission(PERMISSION.talepSil) && (
                <>
                  <DropdownMenu.Separator className="my-1 h-px bg-border" />
                  <Menu ikon={<Trash2 size={14} />} tikla={() => setSilinecek(true)} yikici>
                    Talebi sil
                  </Menu>
                </>
              )}
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>
      </div>

      <HavaleDiyalogu
        acik={pencere === 'havale'}
        kapat={() => setPencere('yok')}
        talepId={talepId}
        mevcutBirimId={talep.birimId ?? null}
      />

      <TipDiyalogu
        acik={pencere === 'tip'}
        kapat={() => setPencere('yok')}
        talepId={talepId}
        mevcutTipId={talep.randevuTipId ?? null}
      />

      <AddToAgendaModal
        talep={talep}
        acik={pencere === 'ajanda'}
        kapat={() => setPencere('yok')}
      />

      <ConfirmDialog
        acik={silinecek}
        baslik="Talep silinsin mi?"
        aciklama={`"${talep.konu}" ve bağlı notlar, dosyalar, hareketler silinecek. Bu işlem geri alınamaz.`}
        onayEtiketi="Sil"
        yikici
        onayla={() => sil.mutate()}
        kapat={() => setSilinecek(false)}
      />
    </>
  );
}

function Menu({
  ikon,
  tikla,
  yikici,
  children,
}: {
  ikon: React.ReactNode;
  tikla: () => void;
  yikici?: boolean;
  children: React.ReactNode;
}) {
  return (
    <DropdownMenu.Item
      onSelect={tikla}
      className={cn(
        'flex cursor-pointer items-center gap-2.5 rounded-sm px-2.5 py-2 text-sm outline-hidden',
        yikici ? 'text-(--st-no) data-highlighted:bg-(--st-no-bg)' : 'data-highlighted:bg-surface-2',
      )}
    >
      {ikon}
      {children}
    </DropdownMenu.Item>
  );
}

/** Ortak diyalog kabuğu. */
function Cerceve({
  acik,
  kapat,
  baslik,
  aciklama,
  children,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  aciklama?: string;
  children: React.ReactNode;
}) {
  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde" />
        <Dialog.Content className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(460px,calc(100vw-32px))] -translate-x-1/2 -translate-y-1/2 rounded-win bg-surface p-5 shadow-3">
          <Dialog.Title className="font-display text-lg font-bold">{baslik}</Dialog.Title>
          {aciklama && (
            <Dialog.Description className="mt-1 text-sm text-text-2 metin-guzel">
              {aciklama}
            </Dialog.Description>
          )}
          <div className="mt-4">{children}</div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

/**
 * Havale — talebi başka bir birime aktarır.
 *
 * Not ZORUNLU değil ama şiddetle önerilir: havale edilen birim, talebin neden
 * kendisine geldiğini yalnızca nottan anlıyor.
 */
function HavaleDiyalogu({
  acik,
  kapat,
  talepId,
  mevcutBirimId,
}: {
  acik: boolean;
  kapat: () => void;
  talepId: number;
  mevcutBirimId: number | null;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const birimler = useUnits();

  const [birimId, setBirimId] = useState<number | ''>('');
  const [not, setNot] = useState('');

  const havale = useMutation({
    mutationFn: () =>
      api.post<boolean>('/talep/havale', { id: talepId, birimId, not: not || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      bildir('basari', 'Talep havale edildi');
      setNot('');
      setBirimId('');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Havale edilemedi', h.message),
  });

  return (
    <Cerceve
      acik={acik}
      kapat={kapat}
      baslik="Talebi havale et"
      aciklama="Seçilen birime bildirim gider ve talep o birimin listesine düşer."
    >
      <div className="space-y-3.5">
        <FieldWrapper etiket="Birim" id="h-birim" zorunlu>
          <Secim
            id="h-birim"
            value={birimId}
            onChange={(e) => setBirimId(e.target.value === '' ? '' : Number(e.target.value))}
          >
            <option value="">Birim seçin</option>
            {birimler.liste
              // Talebin ZATEN bulunduğu birime havale anlamsız.
              .filter((b) => b.id !== mevcutBirimId)
              .map((b) => (
                <option key={b.id} value={b.id}>
                  {unitLabel(b)}
                </option>
              ))}
          </Secim>
        </FieldWrapper>

        <FieldWrapper etiket="Havale notu" id="h-not" ipucu="Birim talebi bu notla değerlendirir.">
          <Textarea id="h-not" value={not} onChange={(e) => setNot(e.target.value)} />
        </FieldWrapper>

        <div className="flex justify-end gap-2">
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={() => havale.mutate()} disabled={birimId === '' || havale.isPending}>
            {havale.isPending ? 'Gönderiliyor…' : 'Havale et'}
          </Button>
        </div>
      </div>
    </Cerceve>
  );
}

/** Talep tipini değiştirir. */
function TipDiyalogu({
  acik,
  kapat,
  talepId,
  mevcutTipId,
}: {
  acik: boolean;
  kapat: () => void;
  talepId: number;
  mevcutTipId: number | null;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const tipler = useEventTypes();
  const [tipId, setTipId] = useState<number | ''>(mevcutTipId ?? '');

  const degistir = useMutation({
    mutationFn: () => api.post<boolean>(`/talep/${talepId}/tip/${tipId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.request.all() });
      bildir('basari', 'Talep tipi güncellendi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Değiştirilemedi', h.message),
  });

  return (
    <Cerceve acik={acik} kapat={kapat} baslik="Talep tipini değiştir">
      <div className="space-y-3.5">
        <FieldWrapper etiket="Tip" id="tp-tip">
          <Secim
            id="tp-tip"
            value={tipId}
            onChange={(e) => setTipId(e.target.value === '' ? '' : Number(e.target.value))}
          >
            <option value="">Tip seçin</option>
            {tipler.liste.map((t) => (
              <option key={t.id} value={t.id}>
                {t.ad}
              </option>
            ))}
          </Secim>
        </FieldWrapper>

        <div className="flex justify-end gap-2">
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={() => degistir.mutate()} disabled={tipId === '' || degistir.isPending}>
            Kaydet
          </Button>
        </div>
      </div>
    </Cerceve>
  );
}
