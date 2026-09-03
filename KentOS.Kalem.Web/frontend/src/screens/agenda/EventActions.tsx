import * as Dialog from '@radix-ui/react-dialog';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowUpFromLine, CalendarClock, ChevronDown, Flower2, MessageSquare, MoreHorizontal, Pencil, Send, Trash2, X, XCircle,
} from 'lucide-react';
import { useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FieldWrapper, Textarea, Input, Secim } from '../../components/Field';
import { Button } from '../../components/Button';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { useToast } from '../../components/Toast';
import { cn } from '../../components/utils';
import { queryKeys } from '../../data/queryKeys';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { ActionSheet, type SheetAction } from '../../components/ActionSheet';
import { useIsDesktop } from '../../components/screenSize';
import { PlaceholderPicker, usePlaceholders, insertPlaceholder } from '../../components/PlaceholderPicker';
import { unitLabel } from '../../data/format';
import { api } from '../../data/client';
import { useUnits } from '../../data/hooks';
import type { SmsResult } from '../../data/types';
import { SCOPE, STATUS, type Florist, type Event } from '../../data/types';
import { EventModal } from '../event/EventModal';

type Pencere = 'yok' | 'ertele' | 'havale' | 'cicek' | 'sms' | 'sil';

/**
 * Etkinlik detayının eylem çubuğu.
 *
 * <p>
 * Eski arayüzde bu işlemler sayfaya dağılmış düğmelerdi. Burada üç grupta:
 * <b>statü</b> (tamamlandı/iptal — en sık kullanılanlar, doğrudan),
 * <b>düzenle</b> (form), ve <b>diğer</b> (menü altında). Yıkıcı olan
 * (sil) menünün en altında ve ayrı bir bölümde.
 * </p>
 */
export function EventActions({
  etkinlik,
  cicekciler,
}: {
  etkinlik: Event;
  cicekciler: Florist[];
}) {
  const qc = useQueryClient();
  const gezin = useNavigate();

  // Düğmeler İZNE göre gizlenir; sunucu zaten reddediyor ama çalışmayan bir
  // düğme göstermek kullanıcıyı hata almaya davet etmek demek.
  const { me, hasPermission } = useSession();

  /**
   * Kullanıcı bu etkinliğin SAHİBİ birimde mi, yoksa ÇAĞRILAN birimde mi?
   *
   * Çağrılan birim etkinliği görür ve not ekleyebilir ama düzenleyemez,
   * silemez, havale edemez — sunucu da bunu reddediyor (yazma yolları
   * etkinliğin sahibi birime bağlı). Düğmeyi göstermek, kullanıcıyı
   * çalışmayacak bir eyleme davet etmek olurdu.
   *
   * Birim bilgisi gelmiyorsa (eski yanıt) kısıtlama UYGULANMAZ: mevcut
   * davranışı bozmamak, gereksiz bir düğme göstermekten daha önemli.
   */
  const sahipBirim =
    etkinlik.birimId == null || me?.birimId == null || etkinlik.birimId === me.birimId;
  const { bildir } = useToast();
  const masaustu = useIsDesktop();
  const [pencere, setPencere] = useState<Pencere>('yok');
  const [duzenle, setDuzenle] = useState(false);

  const etkinlikId = etkinlik.id!;
  const seriMi = (etkinlik.seriId ?? null) !== null;

  const tazele = () => {
    qc.invalidateQueries({ queryKey: queryKeys.event.all() });
  };

  const statuDegistir = useMutation({
    mutationFn: (statu: number) =>
      api.post<boolean>('/etkinlik/statu', { id: etkinlikId, newStatus: statu }),
    onSuccess: () => {
      tazele();
      bildir('basari', 'Etkinlik durumu güncellendi');
    },
    onError: (h: Error) => bildir('hata', 'Güncellenemedi', h.message),
  });

    /*
      DÖNÜŞ DEĞERİ DENETLENİR.

      Uç `bool` döndürüyor ve BAŞARISIZLIĞI DA 200 ile bildiriyor: üst birim
      yoksa ya da kayıt bulunamazsa gövde `false` oluyor. İstemci yalnızca
      HTTP durumuna baktığı için her koşulda "Üst birime gönderildi" yazıyor,
      kullanıcı işlemin olduğunu sanıp listeye bakınca kaydı yerinde
      buluyordu.
    */
  const ustBirime = useMutation({
    mutationFn: () => api.post<boolean>(`/etkinlik/${etkinlikId}/ust-birime-gonder`),
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

  const sil = useMutation({
    mutationFn: (kapsam: number) => api.delete<boolean>(`/etkinlik/${etkinlikId}?kapsam=${kapsam}`),
    onSuccess: () => {
      tazele();
      bildir('basari', 'Etkinlik silindi', 'Silinmiş sekmesinden görebilirsiniz.');
      gezin('/ajanda');
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  /*
    Menüdeki HER eylem `sahipBirim` istiyor: davet edilen birim etkinliği
    görür ama düzenleyemez, silemez, havale edemez. O kullanıcıda "Diğer"
    düğmesi BOŞ bir menü açıyordu — tıklayıp hiçbir şey görmemek, yetkisi
    olmadığını anlamaktan daha kafa karıştırıcı.
  */
  const menuVar =
    sahipBirim &&
    (hasPermission(PERMISSION.ajandaDuzenle) || hasPermission(PERMISSION.ajandaHavale) ||
     hasPermission(PERMISSION.cicekYonet) || hasPermission(PERMISSION.ajandaSmsGonder) ||
     hasPermission(PERMISSION.ajandaStatuDegistir) || hasPermission(PERMISSION.ajandaSil));

  /*
    EYLEMLER TEK LİSTEDE.

    Aynı küme iki yerde çiziliyor: masaüstünde düğme sırası + açılır menü,
    mobilde FAB'dan açılan alt tabaka. Kaynağı tek tutmak şart — koşullar
    (izin, sahip birim, gizlilik) altı ayrı yerde tekrarlansaydı biri
    unutulduğunda arayüz kullanıcıya çalışmayacak bir eylem gösterirdi.
  */
  const eylemler: SheetAction[] = [
    ...(sahipBirim && hasPermission(PERMISSION.ajandaDuzenle)
      ? [
          { etiket: 'Düzenle', ikon: <Pencil size={17} />, onClick: () => setDuzenle(true) },
          { etiket: 'Ertele', ikon: <CalendarClock size={17} />, onClick: () => setPencere('ertele') },
        ]
      : []),
    ...(sahipBirim && hasPermission(PERMISSION.ajandaHavale)
      ? [
          {
            etiket: 'Başka birime havale et',
            ikon: <Send size={17} />,
            onClick: () => setPencere('havale'),
            kapali: etkinlik.gizli ?? false,
            ipucu: etkinlik.gizli ? 'Gizli etkinlik havale edilemez' : undefined,
          },
          {
            etiket: 'Üst birime gönder',
            ikon: <ArrowUpFromLine size={17} />,
            onClick: () => ustBirime.mutate(),
          },
        ]
      : []),
    ...(sahipBirim && hasPermission(PERMISSION.cicekYonet)
      ? [
          {
            etiket: 'Çiçek talimatı',
            ikon: <Flower2 size={17} />,
            onClick: () => setPencere('cicek'),
            kapali: (etkinlik.gizli ?? false) || cicekciler.length === 0,
            ipucu: etkinlik.gizli
              ? 'Gizli etkinlikte çiçek talimatı çıkmaz'
              : cicekciler.length === 0
                ? 'Kayıtlı çiçekçi yok'
                : undefined,
          },
        ]
      : []),
    ...(sahipBirim && hasPermission(PERMISSION.ajandaSmsGonder)
      ? [
          {
            etiket: 'Birime SMS gönder',
            ikon: <MessageSquare size={17} />,
            onClick: () => setPencere('sms'),
            kapali: etkinlik.gizli ?? false,
            ipucu: etkinlik.gizli ? 'Gizli etkinlikte birim SMS’i gönderilmez' : undefined,
          },
        ]
      : []),
    ...(etkinlik.status !== STATUS.Cancelled && sahipBirim && hasPermission(PERMISSION.ajandaStatuDegistir)
      ? [
          {
            etiket: 'İptal et',
            ikon: <XCircle size={17} />,
            onClick: () => statuDegistir.mutate(STATUS.Cancelled),
          },
        ]
      : []),
    ...(sahipBirim && hasPermission(PERMISSION.ajandaSil)
      ? [
          {
            etiket: 'Sil',
            ikon: <Trash2 size={17} />,
            onClick: () => setPencere('sil'),
            ton: 'tehlike' as const,
          },
        ]
      : []),
  ];

  return (
    <>
      {/* Düzenleme aynı diyalogla açılır: detaydan ayrılmadan kaydedilir. */}
      <EventModal acik={duzenle} etkinlikId={etkinlikId} onKapat={() => setDuzenle(false)} />

      {/* Mobilde eylemlerin tamamı sağ alttaki FAB'da. */}
      {!masaustu && <ActionSheet eylemler={eylemler} baslik="Etkinlik işlemleri" />}

      <div className="hidden flex-wrap gap-2 md:flex">
        {/*
          "Tamamlandı" düğmesi KALDIRILDI. Ekranın en görünür, en yeşil
          düğmesiydi ve tek işi statüyü değiştirmekti; kullanıcılar düzenlemek
          isterken ona basıp etkinliği yanlışlıkla kapatıyordu. Statü artık
          "Diğer" menüsünden, diğer statülerle aynı yerden seçiliyor.
        */}
        {sahipBirim && hasPermission(PERMISSION.ajandaDuzenle) && (
          <Button varyant="ikincil" onClick={() => setDuzenle(true)}>
            <Pencil size={14} />
            Düzenle
          </Button>
        )}

        {menuVar && (
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
              className="katman anim-katman z-400 w-[230px] overflow-hidden rounded-card border border-border bg-surface p-1 shadow-3"
            >
              {sahipBirim && hasPermission(PERMISSION.ajandaDuzenle) && (
                <Oge ikon={CalendarClock} onSelect={() => setPencere('ertele')}>
                  Ertele
                </Oge>
              )}

              {/* Gizli etkinlik havale EDİLEMEZ — sunucu da reddeder. */}
              {sahipBirim && hasPermission(PERMISSION.ajandaHavale) && (
                <>
                  <Oge
                    ikon={Send}
                    onSelect={() => setPencere('havale')}
                    kapali={etkinlik.gizli ?? false}
                    ipucu={etkinlik.gizli ? 'Gizli etkinlik havale edilemez' : undefined}
                  >
                    Başka birime havale et
                  </Oge>

                  <Oge ikon={ArrowUpFromLine} onSelect={() => ustBirime.mutate()}>
                    Üst birime gönder
                  </Oge>
                </>
              )}

              {sahipBirim && hasPermission(PERMISSION.cicekYonet) && (
                <Oge
                  ikon={Flower2}
                  onSelect={() => setPencere('cicek')}
                  kapali={(etkinlik.gizli ?? false) || cicekciler.length === 0}
                  ipucu={
                    etkinlik.gizli
                      ? 'Gizli etkinlikte çiçek talimatı çıkmaz'
                      : cicekciler.length === 0
                        ? 'Kayıtlı çiçekçi yok'
                        : undefined
                  }
                >
                  Çiçek talimatı
                </Oge>
              )}

              {sahipBirim && hasPermission(PERMISSION.ajandaSmsGonder) && (
                <Oge
                  ikon={MessageSquare}
                  onSelect={() => setPencere('sms')}
                  kapali={etkinlik.gizli ?? false}
                  ipucu={etkinlik.gizli ? 'Gizli etkinlikte birim SMS’i gönderilmez' : undefined}
                >
                  Birime SMS gönder
                </Oge>
              )}

              {etkinlik.status !== STATUS.Cancelled && sahipBirim && hasPermission(PERMISSION.ajandaStatuDegistir) && (
                <>
                  <DropdownMenu.Separator className="my-1 h-px bg-border" />
                  <Oge ikon={XCircle} onSelect={() => statuDegistir.mutate(STATUS.Cancelled)}>
                    İptal et
                  </Oge>
                </>
              )}

              {sahipBirim && hasPermission(PERMISSION.ajandaSil) && (
                <>
                  <DropdownMenu.Separator className="my-1 h-px bg-border" />
                  <Oge ikon={Trash2} onSelect={() => setPencere('sil')} yikici>
                    Sil
                  </Oge>
                </>
              )}
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>
        )}
      </div>

      {/* ── Pencereler ── */}
      <ErtelePenceresi
        acik={pencere === 'ertele'}
        kapat={() => setPencere('yok')}
        etkinlikId={etkinlikId}
        mevcutTarih={etkinlik.baslangicTarihi}
      />
      <HavalePenceresi
        acik={pencere === 'havale'}
        kapat={() => setPencere('yok')}
        etkinlikId={etkinlikId}
      />
      <CicekPenceresi
        acik={pencere === 'cicek'}
        kapat={() => setPencere('yok')}
        etkinlikId={etkinlikId}
        cicekciler={cicekciler}
      />
      <SmsPenceresi
        acik={pencere === 'sms'}
        kapat={() => setPencere('yok')}
        etkinlikId={etkinlikId}
        baslik={etkinlik.baslik ?? ''}
      />

      <SilOnayi
        acik={pencere === 'sil'}
        kapat={() => setPencere('yok')}
        seriMi={seriMi}
        sil={(kapsam) => sil.mutate(kapsam)}
      />
    </>
  );
}

function Oge({
  ikon: Ikon,
  onSelect,
  children,
  kapali,
  ipucu,
  yikici,
}: {
  ikon: typeof Send;
  onSelect: () => void;
  children: React.ReactNode;
  kapali?: boolean;
  ipucu?: string;
  yikici?: boolean;
}) {
  return (
    <DropdownMenu.Item
      disabled={kapali}
      onSelect={onSelect}
      title={ipucu}
      className={cn(
        'flex cursor-default items-center gap-2.5 rounded-sm px-2.5 py-2 text-sm outline-hidden',
        'data-highlighted:bg-surface-2 data-disabled:cursor-not-allowed data-disabled:opacity-45',
        yikici && 'text-(--st-no) data-highlighted:bg-(--st-no-bg)',
      )}
    >
      <Ikon size={14} className={yikici ? undefined : 'text-text-3'} />
      <span className="min-w-0 flex-1">{children}</span>
    </DropdownMenu.Item>
  );
}

/** Ortak pencere kabuğu — veri girişi diyalogları burada bilinçli bir istisna. */
function Pencere({
  acik,
  kapat,
  baslik,
  aciklama,
  children,
  onayEtiketi,
  onayla,
  gecerli,
  beklemede,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  aciklama?: string;
  children: React.ReactNode;
  onayEtiketi: string;
  onayla: () => void;
  gecerli: boolean;
  beklemede: boolean;
}) {
  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde-hafif backdrop-blur-[2px]" />
        <Dialog.Content
          className="katman anim-orta fixed left-1/2 top-1/2 z-50 w-[min(460px,calc(100vw-32px))] max-h-[calc(100vh-64px)] -translate-x-1/2 -translate-y-1/2
            overflow-y-auto rounded-card border border-border bg-surface shadow-2"
        >
          <div className="flex items-start justify-between gap-3 border-b border-border px-4 py-3">
            <div>
              <Dialog.Title className="font-display text-base font-bold">{baslik}</Dialog.Title>
              {aciklama && (
                <Dialog.Description className="mt-0.5 text-sm text-text-3">
                  {aciklama}
                </Dialog.Description>
              )}
            </div>
            <Dialog.Close asChild>
              <button
                type="button"
                aria-label="Kapat"
                className="grid h-8 w-8 shrink-0 place-items-center rounded-sm text-text-3 hover:bg-sunken hover:text-text"
              >
                <X size={15} />
              </button>
            </Dialog.Close>
          </div>

          <div className="space-y-4 p-4">{children}</div>

          <div className="flex justify-end gap-2 border-t border-border p-4">
            <Button varyant="ikincil" onClick={kapat}>
              Vazgeç
            </Button>
            <Button onClick={onayla} disabled={!gecerli || beklemede}>
              {onayEtiketi}
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

function ErtelePenceresi({
  acik, kapat, etkinlikId, mevcutTarih,
}: {
  acik: boolean; kapat: () => void; etkinlikId: number; mevcutTarih?: string | null;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [tarih, setTarih] = useState('');
  const [not, setNot] = useState('');

  const ertele = useMutation({
    mutationFn: () =>
      api.post('/etkinlik/ertele', { id: etkinlikId, tarih: `${tarih}:00`, not: not || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      bildir('basari', 'Etkinlik ertelendi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Ertelenemedi', h.message),
  });

  return (
    <Pencere
      acik={acik}
      kapat={kapat}
      baslik="Etkinliği ertele"
      aciklama="Yeni tarih seçin; katılımcılar bilgilendirilir."
      onayEtiketi="Ertele"
      onayla={() => ertele.mutate()}
      gecerli={tarih.length > 0}
      beklemede={ertele.isPending}
    >
      <FieldWrapper
        etiket="Yeni tarih ve saat"
        id="er-tarih"
        zorunlu
        ipucu={mevcutTarih ? `Mevcut: ${mevcutTarih.slice(0, 16).replace('T', ' ')}` : undefined}
      >
        <Input
          id="er-tarih"
          type="datetime-local"
          value={tarih}
          onChange={(e) => setTarih(e.target.value)}
          step={1800}
        />
      </FieldWrapper>

      <FieldWrapper etiket="Erteleme nedeni" id="er-not">
        <Textarea id="er-not" value={not} onChange={(e) => setNot(e.target.value)} />
      </FieldWrapper>
    </Pencere>
  );
}

function HavalePenceresi({
  acik, kapat, etkinlikId,
}: {
  acik: boolean; kapat: () => void; etkinlikId: number;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const birimler = useUnits();
  const [birimId, setBirimId] = useState<number | ''>('');
  const [not, setNot] = useState('');

  const havale = useMutation({
    mutationFn: () =>
      api.post('/etkinlik/havale', { id: etkinlikId, birimId, not: not || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      bildir('basari', 'Etkinlik havale edildi');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Havale edilemedi', h.message),
  });

  return (
    <Pencere
      acik={acik}
      kapat={kapat}
      baslik="Başka birime havale et"
      aciklama="Etkinlik seçilen birime taşınır."
      onayEtiketi="Havale et"
      onayla={() => havale.mutate()}
      gecerli={birimId !== ''}
      beklemede={havale.isPending}
    >
      <FieldWrapper etiket="Hedef birim" id="hv-birim" zorunlu>
        <Secim
          id="hv-birim"
          value={birimId}
          onChange={(e) => setBirimId(e.target.value === '' ? '' : Number(e.target.value))}
        >
          <option value="">Birim seçin</option>
          {birimler.liste.map((b) => (
            <option key={b.id} value={b.id}>
              {unitLabel(b)}
            </option>
          ))}
        </Secim>
      </FieldWrapper>

      <FieldWrapper etiket="Havale notu" id="hv-not">
        <Textarea id="hv-not" value={not} onChange={(e) => setNot(e.target.value)} />
      </FieldWrapper>
    </Pencere>
  );
}

function CicekPenceresi({
  acik, kapat, etkinlikId, cicekciler,
}: {
  acik: boolean; kapat: () => void; etkinlikId: number; cicekciler: Florist[];
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [cicekciId, setCicekciId] = useState<number | ''>('');
  const [not, setNot] = useState('');

  const gonder = useMutation({
    mutationFn: () =>
      api.post('/etkinlik/cicek', { ajandaId: etkinlikId, cicekciId, not: not || null }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      bildir('basari', 'Çiçek talimatı gönderildi', 'Çiçekçiye SMS iletildi.');
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Talimat gönderilemedi', h.message),
  });

  const aktifler = cicekciler.filter((c) => c.aktif);

  return (
    <Pencere
      acik={acik}
      kapat={kapat}
      baslik="Çiçek talimatı"
      aciklama="Seçilen çiçekçiye SMS ile talimat gider."
      onayEtiketi="Talimat gönder"
      onayla={() => gonder.mutate()}
      gecerli={cicekciId !== ''}
      beklemede={gonder.isPending}
    >
      <FieldWrapper etiket="Çiçekçi" id="ck-cicekci" zorunlu>
        <Secim
          id="ck-cicekci"
          value={cicekciId}
          onChange={(e) => setCicekciId(e.target.value === '' ? '' : Number(e.target.value))}
        >
          <option value="">Çiçekçi seçin</option>
          {aktifler.map((c) => (
            <option key={c.id} value={c.id}>
              {c.adSoyad} · {c.telefon}
            </option>
          ))}
        </Secim>
      </FieldWrapper>

      <FieldWrapper etiket="Talimat notu" id="ck-not" ipucu="Çiçeğin türü, kart metni vb.">
        <Textarea id="ck-not" value={not} onChange={(e) => setNot(e.target.value)} />
      </FieldWrapper>
    </Pencere>
  );
}

function SmsPenceresi({
  acik, kapat, etkinlikId, baslik,
}: {
  acik: boolean; kapat: () => void; etkinlikId: number; baslik: string;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const birimler = useUnits();
  const [secili, setSecili] = useState<number[]>([]);
  const [mesaj, setMesaj] = useState(
    // Varsayılan metin artık yer tutucu KULLANIYOR: özelliğin var olduğunu
    // gösteren en iyi yer, kullanıcının karşısına çıkan ilk metin.
    'Sayın {alici}, {tarih} {saat} tarihinde {konum} adresinde yapılacak ' +
      '"{baslik}" etkinliğine katılımınız beklenmektedir. {gonderici}',
  );
  const mesajRef = useRef<HTMLTextAreaElement>(null);

  /**
   * Yer tutucuyu İMLEÇ KONUMUNA ekler ve odağı metne geri verir.
   *
   * Sona eklemek işe yaramıyor: yer tutucu cümlenin içine giriyor
   * ("Sayın {alici},"). Odağı geri vermezsek kullanıcı her eklemeden sonra
   * alana yeniden tıklamak zorunda kalıyor.
   */
  const yerTutucuKoy = (ad: string) => {
    const alan = mesajRef.current;
    const { yeni, imlec } = insertPlaceholder(alan, mesaj, ad);
    setMesaj(yeni);
    requestAnimationFrame(() => {
      alan?.focus();
      alan?.setSelectionRange(imlec, imlec);
    });
  };

  // Önizleme örnek değerlerle: gerçek alıcı listesi gönderim anında belli
  // oluyor, ama kullanıcının görmek istediği şey metnin ŞEKLİ.
  const { data: yerTutucular } = usePlaceholders();
  const onizleme = useMemo(() => {
    const ornek: Record<string, string> = {
      alici: 'Ahmet Yılmaz',
      gonderici: 'Özel Kalem Müdürlüğü',
      baslik,
      tarih: '20.08.2026',
      saat: '14:30',
      gun: 'Perşembe',
      konum: 'Başkanlık Makamı',
      birim: 'Belediye Başkanlığı',
    };
    return (yerTutucular ?? []).reduce(
      (m, y) => m.replaceAll(`{${y.ad}}`, ornek[y.ad] ?? ''),
      mesaj,
    );
  }, [mesaj, yerTutucular, baslik]);

  const gonder = useMutation({
    mutationFn: () =>
      api.post<SmsResult>('/etkinlik/sms', {
        ajandaId: etkinlikId,
        birimIds: secili,
        message: mesaj,
      }),
    /*
      SONUÇ SAYIYLA gösterilir. Önce her durumda "SMS kuyruğa alındı"
      yazıyordu — hiç mesaj yazılmamış olsa bile. Telefon numarası olmayan
      kullanıcı sunucuda sessizce atlanıyor ve gönderen kişi "gönderdim ama
      gitmedi" diyordu; sebebi görmenin tek yolu veritabanına bakmaktı.
    */
    onSuccess: (s) => {
      qc.invalidateQueries({ queryKey: queryKeys.event.all() });

      const eksikler = s?.telefonsuzKisiler ?? [];
      const bos = s?.bosBirimler ?? [];
      const ayrinti = [
        eksikler.length > 0 ? `Telefonu olmayan: ${eksikler.join(', ')}` : null,
        bos.length > 0 ? `Kullanıcısı olmayan birim: ${bos.join(', ')}` : null,
      ]
        .filter(Boolean)
        .join(' · ');

      if ((s?.gonderilen ?? 0) === 0) {
        bildir('uyari', 'Kimseye SMS gönderilemedi', ayrinti || undefined);
      } else {
        bildir('basari', s!.ozet ?? 'SMS kuyruğa alındı', ayrinti || undefined);
      }
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'SMS gönderilemedi', h.message),
  });

  return (
    <Pencere
      acik={acik}
      kapat={kapat}
      baslik="Birime SMS gönder"
      aciklama="Seçilen birimlerdeki kullanıcılara SMS iletilir."
      onayEtiketi="Gönder"
      onayla={() => gonder.mutate()}
      gecerli={secili.length > 0 && mesaj.trim().length > 0}
      beklemede={gonder.isPending}
    >
      <div>
        <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-text-3">
          Birimler <span className="text-(--st-no)">*</span>
        </p>
        <ul className="max-h-[190px] space-y-1 overflow-y-auto rounded-control border border-border p-1.5">
          {birimler.liste.map((b) => {
            const isaretli = secili.includes(b.id!);
            return (
              <li key={b.id}>
                <label className="flex cursor-pointer items-center gap-2.5 rounded-sm px-2 py-1.5 hover:bg-surface-2">
                  <input
                    type="checkbox"
                    checked={isaretli}
                    onChange={() =>
                      setSecili((s) => (isaretli ? s.filter((x) => x !== b.id) : [...s, b.id!]))
                    }
                    className="h-[16px] w-[16px] accent-(--brand)"
                  />
                  <span className="truncate text-sm">{unitLabel(b)}</span>
                </label>
              </li>
            );
          })}
        </ul>
      </div>

      <div>
        <div className="mb-1.5 flex items-center justify-between gap-2">
          <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
            Mesaj <span className="text-(--st-no)">*</span>
          </p>
          <PlaceholderPicker ekle={yerTutucuKoy} />
        </div>

        <Textarea
          ref={mesajRef}
          id="sms-mesaj"
          value={mesaj}
          onChange={(e) => setMesaj(e.target.value)}
          maxLength={480}
          rows={4}
        />

        <p className="mt-1 text-xs text-text-3">
          {mesaj.length} karakter · her 160 karakter bir SMS sayılır
        </p>

        {/*
          ÖNİZLEME: yer tutucular gönderim anında doluyor, yani kullanıcı
          yazarken sonucu göremiyordu. Örnek değerlerle gösterilen bu satır,
          "{tarıh}" gibi bir yazım hatasını daha gönderilmeden belli ediyor.
        */}
        <div className="mt-2 rounded-control border border-border bg-surface-2 p-2.5">
          <p className="mb-1 text-2xs uppercase tracking-wider text-text-3">
            Önizleme
          </p>
          <p className="text-sm leading-normal text-text-2">{onizleme}</p>
        </div>
      </div>
    </Pencere>
  );
}

/**
 * Silme onayı.
 *
 * <p>
 * Seri bir etkinlikte <b>kapsam sorulur</b>: "yalnızca bu" ile "tüm seri"
 * arasındaki fark geri alınamaz. Tek seferlik etkinlikte soru gereksiz.
 * </p>
 */
function SilOnayi({
  acik, kapat, seriMi, sil,
}: {
  acik: boolean; kapat: () => void; seriMi: boolean; sil: (kapsam: number) => void;
}) {
  const [kapsam, setKapsam] = useState<number>(SCOPE.This);

  if (!seriMi) {
    return (
      <ConfirmDialog
        acik={acik}
        kapat={kapat}
        baslik="Etkinlik silinsin mi?"
        aciklama="Kayıt silinmiş olarak işaretlenir; Silinmiş sekmesinden görülebilir."
        onayEtiketi="Sil"
        yikici
        onayla={() => sil(SCOPE.This)}
      />
    );
  }

  return (
    <Pencere
      acik={acik}
      kapat={kapat}
      baslik="Tekrar eden etkinliği sil"
      aciklama="Bu etkinlik bir serinin parçası."
      onayEtiketi="Sil"
      onayla={() => sil(kapsam)}
      gecerli
      beklemede={false}
    >
      <div className="space-y-2">
        {[
          { d: SCOPE.This, e: 'Yalnızca bu etkinlik', a: 'Serinin diğer tekrarları kalır.' },
          { d: SCOPE.ThisAndFollowing, e: 'Bu ve sonraki etkinlikler', a: 'Geçmiş tekrarlar korunur.' },
          { d: SCOPE.All, e: 'Tüm seri', a: 'Serinin bütün tekrarları silinir.' },
        ].map((s) => (
          <label
            key={s.d}
            className={cn(
              'flex cursor-pointer items-start gap-2.5 rounded-control border p-2.5 transition-colors',
              kapsam === s.d ? 'border-(--st-no) bg-(--st-no-bg)' : 'border-border hover:bg-surface-2',
            )}
          >
            <input
              type="radio"
              name="sil-kapsam"
              checked={kapsam === s.d}
              onChange={() => setKapsam(s.d)}
              className="mt-0.5 h-[16px] w-[16px] accent-(--st-no)"
            />
            <span>
              <span className="block text-sm font-medium">{s.e}</span>
              <span className="block text-xs text-text-3">{s.a}</span>
            </span>
          </label>
        ))}
      </div>
    </Pencere>
  );
}
