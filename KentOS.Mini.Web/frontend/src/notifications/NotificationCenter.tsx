import * as Popover from '@radix-ui/react-popover';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, BellOff, CheckCheck, Lock, Trash2, X } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, IconButton } from '../components/Button';
import { useIsDesktop } from '../components/screenSize';
import { Skeleton } from '../components/Skeleton';
import { InsetGroup, ListRow } from '../components/ListRow';
import { BottomSheet } from '../shell/mobile/BottomSheet';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { relativeTime, dateTime } from '../data/format';
import { api, queryString, type PagedResult } from '../data/client';
import type { AppNotification } from '../data/types';

/**
 * Bildirimin uygulama içi hedefi.
 *
 * <p>
 * Sunucu `fcmData` sözleşmesini çözüp `varlik` / `varlikId` / `eylem` olarak
 * gönderiyor; burada yalnızca yola çevriliyor. Mobildeki `routeFromTokenData`
 * ile AYNI eşleme — aynı bildirim iki platformda farklı yere götürürse
 * kullanıcı hangisine güveneceğini bilemez.
 * </p>
 */
export function resolveNotificationPath(b: AppNotification): string | null {
  if (!b.varlik || !b.varlikId || b.eylem === 'None') return null;

  switch (b.varlik.toLowerCase()) {
    case 'ajanda':
      return `/ajanda/${b.varlikId}`;
    case 'talep':
      return `/talepler/${b.varlikId}`;
    case 'oneri':
      return `/oneriler/${b.varlikId}`;
    case 'dosya':
      return `/gonderim/${b.varlikId}`;
    default:
      return null;
  }
}

/** Okunmamış sayısı — appbar rozetinin kaynağı. */
export function useUnreadCount() {
  return useQuery({
    queryKey: ['bildirim', 'okunmamis-sayi'] as const,
    queryFn: () => api.get<number>('/bildirim/okunmamis-sayi'),
    // Push geldiğinde köprü bu sorguyu geçersiz kılıyor; yine de ağ
    // kesintisinden sonra kendi kendine toparlansın diye dakikada bir.
    refetchInterval: 60_000,
    staleTime: 30_000,
  });
}

/**
 * Appbar bildirim merkezi.
 *
 * <p>
 * Tarayıcı bildirimleri işletim sisteminin merkezinde kullanıcı tek tek
 * silene kadar birikiyordu. Uygulama içi merkez bunu çözer: buradan tümü
 * bir kerede okundu işaretlenebiliyor, okunanlar toplu silinebiliyor.
 * </p>
 */
export function NotificationCenter() {
  const masaustu = useIsDesktop();
  const [tabaka, setTabaka] = useState(false);
  const qc = useQueryClient();
  const gezin = useNavigate();
  const { bildir } = useToast();

  const sayi = useUnreadCount();

  const liste = useQuery({
    queryKey: ['bildirim', 'liste'] as const,
    queryFn: () =>
      api.get<PagedResult<AppNotification>>(`/bildirim${queryString({ boyut: 25 })}`),
    placeholderData: keepPreviousData,
  });

  const tazele = () => qc.invalidateQueries({ queryKey: ['bildirim'] });

  const okundu = useMutation({
    mutationFn: (id: number) => api.post<void>(`/bildirim/${id}/okundu`),
    onSuccess: tazele,
  });

  const tumuOkundu = useMutation({
    mutationFn: () => api.post<number>('/bildirim/tumu-okundu'),
    onSuccess: (adet) => {
      tazele();
      bildir('basari', `${adet} bildirim okundu işaretlendi`);
    },
    onError: (h: Error) => bildir('hata', 'İşlem başarısız', h.message),
  });

  const okunanlariSil = useMutation({
    mutationFn: () => api.delete<number>('/bildirim/okunanlar'),
    onSuccess: (adet) => {
      tazele();
      bildir('basari', `${adet} okunmuş bildirim silindi`);
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  const sil = useMutation({
    mutationFn: (id: number) => api.delete<void>(`/bildirim/${id}`),
    onSuccess: tazele,
  });

  const okunmamis = sayi.data ?? 0;
  const kayitlar = liste.data?.veriler ?? [];

  function ac(b: AppNotification) {
    if (!b.okundu && b.id) okundu.mutate(b.id);

    const yol = resolveNotificationPath(b);
    if (yol) gezin(yol);
  }

  const zil = (
    <button
      type="button"
      aria-label={okunmamis > 0 ? `Bildirimler — ${okunmamis} okunmamış` : 'Bildirimler'}
      title="Bildirimler"
      className="relative grid h-[38px] w-[38px] place-items-center rounded-control border border-border bg-surface-2 text-text-2 transition-colors hover:text-text"
    >
      <Bell size={17} strokeWidth={1.8} />
      {okunmamis > 0 && (
        <span
          className="absolute -right-1 -top-1 grid h-[17px] min-w-[17px] place-items-center rounded-full px-1 text-3xs font-bold tabular-nums text-white"
          style={{ background: 'var(--st-no)' }}
        >
          {okunmamis > 99 ? '99+' : okunmamis}
        </span>
      )}
    </button>
  );

  /*
    MOBİLDE ALT TABAKA, MASAÜSTÜNDE POPOVER.

    Merkez, zile bağlı yüzen bir kart olarak açılıyordu. Telefonda bu kart
    ekranın neredeyse tamamını kaplıyor ama ekranın KENDİSİ değil: köşeleri
    havada duruyor, kapatmak için dışarı dokunmak gerekiyor ve liste satırları
    fareye göre ölçülmüş (36px, silme düğmesi yalnızca hover'da). Bildirim
    listesi mobilde bir ekran; kabı da öyle olmalı.
  */
  if (!masaustu) {
    return (
      <>
        <span className="contents" onClick={() => { setTabaka(true); tazele(); }}>
          {zil}
        </span>

        <BottomSheet
          acik={tabaka}
          kapat={() => setTabaka(false)}
          baslik="Bildirimler"
          aciklama={okunmamis > 0 ? `${okunmamis} okunmamış` : 'Hepsi okundu'}
        >
          {kayitlar.length > 0 && (
            <div className="mb-2 flex gap-2">
              <button
                type="button"
                onClick={() => tumuOkundu.mutate()}
                disabled={okunmamis === 0 || tumuOkundu.isPending}
                className="h-ctrl flex-1 rounded-sm border border-line bg-surface text-xs font-semibold text-ink-2 disabled:opacity-45 active:scale-[0.98]"
              >
                Tümünü okundu işaretle
              </button>
              <button
                type="button"
                onClick={() => okunanlariSil.mutate()}
                disabled={okunanlariSil.isPending}
                className="h-ctrl flex-1 rounded-sm border border-line bg-surface text-xs font-semibold text-ink-2 disabled:opacity-45 active:scale-[0.98]"
              >
                Okunanları temizle
              </button>
            </div>
          )}

          {liste.isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} className="h-16 w-full" />
              ))}
            </div>
          ) : kayitlar.length === 0 ? (
            <div className="grid place-items-center px-6 py-12 text-center">
              <span className="grid h-14 w-14 place-items-center rounded-full bg-sunken">
                <BellOff size={22} className="text-ink-3" strokeWidth={1.8} />
              </span>
              <p className="mt-3 text-base font-semibold">Bildirim yok</p>
              <p className="mt-1 text-sm text-ink-3">
                Yeni bir gelişme olduğunda burada görürsünüz.
              </p>
            </div>
          ) : (
            <InsetGroup>
              {kayitlar.map((b, i) => (
                <ListRow
                  key={b.id}
                  sira={i}
                  sonuncu={i === kayitlar.length - 1}
                  ikon={
                    b.gizli ? <Lock size={15} strokeWidth={1.9} /> : <Bell size={15} strokeWidth={1.9} />
                  }
                  // Okunmamış kayıt marka renginde, okunmuş sönük: rozet
                  // yerine ÇİPİN RENGİ taşıyor — satırda fazladan bir nokta
                  // olmadan aynı bilgi.
                  ikonRengi={b.okundu ? 'var(--ink-3)' : 'var(--brand-ui)'}
                  ust={<span className="tabular-nums">{relativeTime(b.tarih)}</span>}
                  baslik={
                    <span className={b.okundu ? 'text-ink-2' : 'font-semibold'}>{b.baslik}</span>
                  }
                  alt={<span className="satir-2 leading-[1.45]">{b.icerik}</span>}
                  onClick={
                    resolveNotificationPath(b) !== null
                      ? () => {
                          setTabaka(false);
                          ac(b);
                        }
                      : undefined
                  }
                />
              ))}
            </InsetGroup>
          )}

          {(liste.data?.toplam ?? 0) > kayitlar.length && (
            <p className="mt-3 text-center text-2xs text-ink-3">
              Son {kayitlar.length} bildirim · toplam {liste.data?.toplam}
            </p>
          )}
        </BottomSheet>
      </>
    );
  }

  return (
    <Popover.Root
      onOpenChange={(acikMi) => {
        // Panel her açılışta taze veri ister; arka planda gelen bir bildirim
        // 30 saniye boyunca görünmezse merkez işe yaramaz.
        if (acikMi) tazele();
      }}
    >
      <Popover.Trigger asChild>{zil}</Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="end"
          sideOffset={8}
          className="katman anim-katman z-400 w-[min(400px,calc(100vw-24px))] overflow-hidden rounded-card border border-border bg-surface shadow-3"
        >
          <div className="flex items-center justify-between gap-2 border-b border-border px-3.5 py-2.5">
            <p className="font-display text-sm font-bold">
              NotificationsScreen
              {okunmamis > 0 && (
                <span className="ml-2 font-normal text-text-3">{okunmamis} yeni</span>
              )}
            </p>
            <Popover.Close asChild>
              <IconButton etiket="Kapat" className="h-8 w-8 border-0 bg-transparent">
                <X size={15} />
              </IconButton>
            </Popover.Close>
          </div>

          {kayitlar.length > 0 && (
            <div className="flex gap-1.5 border-b border-border px-3 py-2">
              <Button
                varyant="sade"
                className="h-7 px-2 text-xs"
                onClick={() => tumuOkundu.mutate()}
                disabled={okunmamis === 0 || tumuOkundu.isPending}
              >
                <CheckCheck size={12} />
                Tümünü okundu işaretle
              </Button>
              <Button
                varyant="sade"
                className="h-7 px-2 text-xs"
                onClick={() => okunanlariSil.mutate()}
                disabled={okunanlariSil.isPending}
              >
                <Trash2 size={12} />
                Okunanları temizle
              </Button>
            </div>
          )}

          <div className="max-h-[min(60vh,440px)] overflow-y-auto">
            {liste.isLoading ? (
              <div className="space-y-2 p-3">
                {Array.from({ length: 4 }).map((_, i) => (
                  <Skeleton key={i} className="h-14 w-full" />
                ))}
              </div>
            ) : kayitlar.length === 0 ? (
              <div className="grid place-items-center px-6 py-10 text-center">
                <span className="grid h-11 w-11 place-items-center rounded-lg bg-sunken">
                  <BellOff size={18} className="text-text-3" strokeWidth={1.8} />
                </span>
                <p className="mt-2.5 text-sm font-semibold">Bildirim yok</p>
                <p className="mt-0.5 text-xs text-text-3">
                  Yeni bir gelişme olduğunda burada görürsünüz.
                </p>
              </div>
            ) : (
              <ul className="divide-y divide-border">
                {kayitlar.map((b) => {
                  const tiklanabilir = resolveNotificationPath(b) !== null;
                  return (
                    <li key={b.id} className="group relative">
                      <Popover.Close asChild>
                        <button
                          type="button"
                          onClick={() => ac(b)}
                          disabled={!tiklanabilir}
                          className={cn(
                            'flex w-full gap-2.5 px-3.5 py-2.5 text-left transition-colors',
                            tiklanabilir ? 'hover:bg-surface-2' : 'cursor-default',
                            !b.okundu && 'bg-brand-tint/40',
                          )}
                        >
                          {/* Okunmamış işareti — renk değil konum taşıyor,
                              renk körlüğünde de ayırt edilir. */}
                          <span
                            className={cn(
                              'mt-1.5 h-[7px] w-[7px] shrink-0 rounded-full',
                              b.okundu ? 'bg-transparent' : 'bg-brand',
                            )}
                            aria-hidden
                          />

                          <span className="min-w-0 flex-1">
                            <span className="flex items-center gap-1.5">
                              {b.gizli && (
                                <Lock size={11} className="shrink-0 text-text-3" aria-label="Gizli" />
                              )}
                              <span
                                className={cn(
                                  'truncate text-sm',
                                  b.okundu ? 'font-medium text-text-2' : 'font-semibold',
                                )}
                              >
                                {b.baslik}
                              </span>
                            </span>
                            <span className="mt-0.5 block text-xs leading-[1.45] text-text-2 satir-2">
                              {b.icerik}
                            </span>
                            <time
                              className="mt-1 block text-2xs text-text-3"
                              title={dateTime(b.tarih)}
                            >
                              {relativeTime(b.tarih)}
                            </time>
                          </span>
                        </button>
                      </Popover.Close>

                      {/* Tek tek silme — listeyi kalabalıklaştırmasın diye
                          yalnızca imleç üzerindeyken görünür. */}
                      <button
                        type="button"
                        aria-label="Bildirimi sil"
                        title="Sil"
                        onClick={() => b.id && sil.mutate(b.id)}
                        className="absolute right-2 top-2 hidden h-7 w-7 place-items-center rounded-sm text-text-3 hover:bg-sunken hover:text-(--st-no) group-hover:grid"
                      >
                        <Trash2 size={13} />
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          {(liste.data?.toplam ?? 0) > kayitlar.length && (
            <p className="border-t border-border px-3.5 py-2 text-center text-xs text-text-3">
              Son {kayitlar.length} bildirim gösteriliyor · toplam {liste.data?.toplam}
            </p>
          )}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
