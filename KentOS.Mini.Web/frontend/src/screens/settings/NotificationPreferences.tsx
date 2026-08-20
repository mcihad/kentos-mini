import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Bell, Briefcase, CalendarDays, ChevronDown, ClipboardList, FolderInput, Users,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { Switch } from '../../components/Switch';
import { Skeleton } from '../../components/Skeleton';
import { useToast } from '../../components/Toast';
import { cn } from '../../components/utils';
import { api } from '../../data/client';
import { haptic } from '../../data/haptics';
import type { components } from '../../data/types.generated';

type Tercihler = components['schemas']['UserSettingDto'];
/** Yalnızca bildirim bayrakları — `hideOldAgendas` bir görünüm ayarı. */
type BildirimAnahtari = Exclude<keyof Tercihler, 'hideOldAgendas'>;

type Satir = { anahtar: BildirimAnahtari; etiket: string };
type Grup = { ad: string; ikon: LucideIcon; aciklama: string; satirlar: Satir[] };

/**
 * BİLDİRİM GRUPLARI — modüle göre, menüyle aynı zihin haritası.
 *
 * <p>
 * Otuz ayrı anahtarı düz liste hâlinde göstermek kullanıcıyı okumadan
 * kapatmaya iter. Gruplar hem listeyi kısaltıyor (kapalı başlarlar) hem de
 * "iş takiple ilgili her şeyi kapat" gibi tek hamlelik karara izin veriyor.
 * </p>
 *
 * <p>
 * Etiketler <b>olayın kendisini</b> söyler, alan adını değil: "Bana görev
 * atandığında" — "taskOnAssigned" değil. Kullanıcı hangi durumda telefonunun
 * öteceğini okumak istiyor.
 * </p>
 */
const GRUPLAR: Grup[] = [
  {
    ad: 'Ajanda ve etkinlikler',
    ikon: CalendarDays,
    aciklama: 'Programa eklenen ve değişen kayıtlar',
    satirlar: [
      { anahtar: 'agendaOnCreated', etiket: 'Yeni etkinlik eklendiğinde' },
      { anahtar: 'agendaOnUpdated', etiket: 'Etkinlik güncellendiğinde' },
      { anahtar: 'agendaOnPostponed', etiket: 'Etkinlik ertelendiğinde' },
      { anahtar: 'agendaOnStatusChange', etiket: 'Etkinliğin durumu değiştiğinde' },
      { anahtar: 'agendaOnDeleted', etiket: 'Etkinlik silindiğinde' },
      { anahtar: 'agendaOnOrganized', etiket: 'Etkinliğe havale yapıldığında' },
      { anahtar: 'agendaOnNoteAdded', etiket: 'Etkinliğe not eklendiğinde' },
      { anahtar: 'agendaOnImageUpload', etiket: 'Etkinliğe fotoğraf eklendiğinde' },
      { anahtar: 'agendaOnFlowerSent', etiket: 'Çiçek talimatı verildiğinde' },
      { anahtar: 'agendaOnFlowerDeleted', etiket: 'Çiçek talimatı iptal edildiğinde' },
    ],
  },
  {
    ad: 'Talepler',
    ikon: ClipboardList,
    aciklama: 'Vatandaş ve kurum içi istekler',
    satirlar: [
      { anahtar: 'requestOnCreated', etiket: 'Yeni talep açıldığında' },
      { anahtar: 'requestOnUpdated', etiket: 'Talep güncellendiğinde' },
      { anahtar: 'requestOnStatusChange', etiket: 'Talebin durumu değiştiğinde' },
      { anahtar: 'requestOnRemittance', etiket: 'Talep birimime havale edildiğinde' },
      { anahtar: 'requestOnAddedToAgenda', etiket: 'Talep ajandaya eklendiğinde' },
      { anahtar: 'requestOnFileAttached', etiket: 'Talebe dosya eklendiğinde' },
      { anahtar: 'requestOnNoteAdded', etiket: 'Talebe not eklendiğinde' },
      { anahtar: 'requestOnOrganized', etiket: 'Talep düzenlendiğinde' },
      { anahtar: 'requestOnDeleted', etiket: 'Talep silindiğinde' },
    ],
  },
  {
    ad: 'İş ve görev takibi',
    ikon: Briefcase,
    aciklama: 'Size düşen işler, onaylar ve süre aşımları',
    satirlar: [
      { anahtar: 'taskOnAssigned', etiket: 'Bana görev atandığında' },
      { anahtar: 'taskOnStatusChange', etiket: 'Görevin durumu değiştiğinde' },
      { anahtar: 'taskOnApprovalNeeded', etiket: 'Onayımı bekleyen görev olduğunda' },
      { anahtar: 'taskOnOverdue', etiket: 'Görevin süresi aşıldığında' },
      { anahtar: 'projectOnTeamChange', etiket: 'Proje ekibine eklendiğimde' },
    ],
  },
  {
    ad: 'Halk günü',
    ikon: Users,
    aciklama: 'Görüşme günleri ve sonuçları',
    satirlar: [
      { anahtar: 'publicDayOnAssigned', etiket: 'Halk gününde görevlendirildiğimde' },
      { anahtar: 'publicDayOnResult', etiket: 'Görüşme sonucu kaydedildiğinde' },
    ],
  },
  {
    ad: 'Davetler',
    ikon: Bell,
    aciklama: 'Tören ve protokol davet listeleri',
    satirlar: [
      { anahtar: 'invitationOnAssigned', etiket: 'Davet listesi bana atandığında' },
      { anahtar: 'invitationOnResponse', etiket: 'Davette cevap değiştiğinde' },
    ],
  },
  {
    ad: 'Gelen belgeler',
    ikon: FolderInput,
    aciklama: 'Size gönderilen dosya, özgeçmiş ve kutu kayıtları',
    satirlar: [
      { anahtar: 'fileOnReceived', etiket: 'Bana dosya gönderildiğinde' },
      { anahtar: 'resumeOnShared', etiket: 'Bana özgeçmiş paylaşıldığında' },
      { anahtar: 'inboxOnReceived', etiket: 'Gelen kutuma kayıt düştüğünde' },
      { anahtar: 'citizenReportOnUpdate', etiket: 'Vatandaş bildirimimde gelişme olduğunda' },
    ],
  },
];

/**
 * HANGİ OLAYLARDA BİLDİRİM ALINACAĞI — kullanıcının kendi tercihi.
 *
 * <h4>Neden vardı ama görünmüyordu</h4>
 * <p>
 * Sunucu bu tercihleri tutuyordu (<c>oturum/tercihler</c>) ve bildirim
 * gönderilirken gerçekten okuyordu; ama <b>arayüzde hiçbir ekran onları
 * göstermiyordu</b>. Kullanıcının elinde yalnızca "bu tarayıcıda bildirimleri
 * aç/kapat" düğmesi vardı: ya hepsi ya hiçbiri.
 * </p>
 *
 * <h4>Grup mu, tek tek mi</h4>
 * <p>
 * İkisi de. Grup satırındaki anahtar o gruptaki her şeyi birden çevirir —
 * "iş takiple ilgili hiçbir şey gelmesin" tek dokunuş. Grup açıldığında
 * satırlar tek tek de kapatılabilir; başlık altındaki sayı ("3 / 5 açık")
 * grubu açmadan durumu söyler.
 * </p>
 *
 * <h4>Kaydetme</h4>
 * <p>
 * Kaydet düğmesi yok: her değişiklik anında gönderilir. Ayar ekranında
 * "kaydettim mi?" sorusu kullanıcıyı ekranda tutuyor; anahtarın kendisi zaten
 * sonucu gösteriyor. İstek düşerse anahtar eski hâline döner ve bildirim
 * şeridi sebebi yazar.
 * </p>
 */
export function NotificationPreferences() {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [acikGrup, setAcikGrup] = useState<string | null>(null);

  const tercihler = useQuery({
    queryKey: ['oturum', 'tercihler'] as const,
    queryFn: () => api.get<Tercihler>('/oturum/tercihler'),
  });

  /** Ekranda gösterilen taslak — istek uçarken anahtar hemen dönsün diye. */
  const [taslak, setTaslak] = useState<Tercihler | null>(null);
  useEffect(() => {
    if (tercihler.data) setTaslak(tercihler.data);
  }, [tercihler.data]);

  const kaydet = useMutation({
    mutationFn: (govde: Tercihler) => api.put<Tercihler>('/oturum/tercihler', govde),
    onSuccess: (d) => {
      qc.setQueryData(['oturum', 'tercihler'], d);
      haptic('basari');
    },
    onError: (h: Error) => {
      // Sunucu kabul etmediyse ekran gerçeği yansıtmalı: taslağı geri al.
      setTaslak(tercihler.data ?? null);
      haptic('hata');
      bildir('hata', 'Tercih kaydedilemedi', h.message);
    },
  });

  function uygula(degisiklik: Partial<Tercihler>) {
    if (!taslak) return;
    const yeni = { ...taslak, ...degisiklik };
    setTaslak(yeni);
    kaydet.mutate(yeni);
  }

  if (tercihler.isLoading || !taslak) {
    return (
      <div className="space-y-2 p-4">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    );
  }

  if (tercihler.isError) {
    return (
      <p className="p-4 text-sm text-text-3">
        Bildirim tercihleri okunamadı. Sayfayı yenileyip tekrar deneyin.
      </p>
    );
  }

  return (
    <div className="divide-y divide-line">
      {GRUPLAR.map((grup) => {
        const acikSayisi = grup.satirlar.filter((s) => taslak[s.anahtar] === true).length;
        const hepsiAcik = acikSayisi === grup.satirlar.length;
        const hicbiri = acikSayisi === 0;
        const genisletildi = acikGrup === grup.ad;
        const Ikon = grup.ikon;

        return (
          <div key={grup.ad}>
            <div className="flex items-center gap-3 px-4 py-3">
              <button
                type="button"
                onClick={() => {
                  haptic('secim');
                  setAcikGrup(genisletildi ? null : grup.ad);
                }}
                aria-expanded={genisletildi}
                className="flex min-w-0 flex-1 items-center gap-3 text-left"
              >
                <span
                  className={cn(
                    'grid size-9 shrink-0 place-items-center rounded-md',
                    hicbiri ? 'bg-sunken text-ink-3' : 'bg-brand-soft text-brand',
                  )}
                  aria-hidden
                >
                  <Ikon size={17} strokeWidth={1.9} />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-base font-semibold text-ink">
                    {grup.ad}
                  </span>
                  {/* Sayı açıklamadan çok işe yarıyor: kullanıcı grubu açmadan
                      neyin kapalı olduğunu görüyor. */}
                  <span className="mt-0.5 block truncate text-sm text-ink-3">
                    {hepsiAcik
                      ? grup.aciklama
                      : hicbiri
                        ? 'Kapalı'
                        : `${acikSayisi} / ${grup.satirlar.length} açık`}
                  </span>
                </span>
                <ChevronDown
                  size={17}
                  aria-hidden
                  className={cn(
                    'shrink-0 text-ink-3 transition-transform motion-reduce:transition-none',
                    genisletildi && 'rotate-180',
                  )}
                />
              </button>

              {/*
                GRUP ANAHTARI — tek dokunuşla hepsini çevirir.

                Kısmi durumda "aç" tarafına çalışır: yarısı açık bir grupta
                anahtara basan kullanıcının beklediği şey hepsini açmaktır,
                kalanı kapatmak değil.
              */}
              <Switch
                isaretli={hepsiAcik}
                degistir={() => {
                  haptic('secim');
                  const hedef = !hepsiAcik;
                  uygula(
                    Object.fromEntries(
                      grup.satirlar.map((s) => [s.anahtar, hedef]),
                    ) as Partial<Tercihler>,
                  );
                }}
                id={`grup-${grup.ad}`}
              />
            </div>

            {genisletildi && (
              <ul className="space-y-0.5 bg-sunken/40 px-4 pb-3">
                {grup.satirlar.map((s) => (
                  <li key={s.anahtar}>
                    <Switch
                      isaretli={taslak[s.anahtar] === true}
                      degistir={(a) => {
                        haptic('secim');
                        uygula({ [s.anahtar]: a } as Partial<Tercihler>);
                      }}
                      etiket={s.etiket}
                    />
                  </li>
                ))}
              </ul>
            )}
          </div>
        );
      })}
    </div>
  );
}
