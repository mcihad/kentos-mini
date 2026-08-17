import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, BellOff, CheckCheck, Search, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { SearchInput } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { SkeletonRows } from '../components/Skeleton';
import { Pagination } from '../components/Pagination';
import { SegmentedSelect } from '../components/Filters';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import { resolveNotificationPath } from '../notifications/NotificationCenter';
import type { AppNotification } from '../data/types';
import { dateTime } from '../data/format';
import { api, queryString, type PagedResult } from '../data/client';

type RecurrenceScope = 'son' | 'tumu';

/**
 * Tüm bildirimler.
 *
 * <p>
 * Appbar'daki zil yalnızca <b>son 30 günü</b> gösterir: <c>Messages</c> iki
 * yıllık giden kutusu ve yeni arayüze ilk giren kullanıcı on binden fazla
 * geçmiş bildirimle karşılaşıyordu. Bu sayfa tüm geçmişi sayfalayarak açar —
 * hiçbir kayıt silinmedi ya da sessizce okundu işaretlenmedi.
 * </p>
 */
export default function NotificationsScreen() {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const gezin = useNavigate();

  const [kapsam, setKapsam] = useState<RecurrenceScope>('son');
  const [yalnizcaOkunmamis, setYalnizcaOkunmamis] = useState(false);
  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [sayfa, setSayfa] = useState(1);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const liste = useQuery({
    queryKey: ['bildirim', 'sayfa', kapsam, yalnizcaOkunmamis, sayfa, arama] as const,
    queryFn: () =>
      api.get<PagedResult<AppNotification>>(
        `/bildirim${queryString({
          sayfa,
          boyut: 30,
          ara: arama,
          yalnizcaOkunmamis,
          tumGecmis: kapsam === 'tumu',
        })}`,
      ),
    placeholderData: keepPreviousData,
  });

  function tazele() {
    qc.invalidateQueries({ queryKey: ['bildirim'] });
  }

  const tumunuOku = useMutation({
    mutationFn: () => api.post<number>('/bildirim/tumu-okundu'),
    onSuccess: (adet) => {
      tazele();
      bildir('basari', `${adet} bildirim okundu işaretlendi`);
    },
    onError: (h: Error) => bildir('hata', 'İşaretlenemedi', h.message),
  });

  const okunanlariSil = useMutation({
    mutationFn: () => api.delete<number>('/bildirim/okunanlar'),
    onSuccess: (adet) => {
      tazele();
      bildir('basari', `${adet} okunmuş bildirim silindi`);
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  const okunduIsaretle = useMutation({
    mutationFn: (id: number) => api.post<void>(`/bildirim/${id}/okundu`),
    onSuccess: tazele,
  });

  const kayitlar = liste.data?.veriler ?? [];

  function ac(b: AppNotification) {
    if (!b.okundu && b.id) okunduIsaretle.mutate(b.id);
    const yol = resolveNotificationPath(b);
    if (yol) gezin(yol);
  }

  return (
    <div className="space-y-3.5">
      {/* ── Araç çubuğu ── */}
      <div className="flex flex-col gap-2.5 md:flex-row md:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Bildirim başlığı veya içeriğinde ara"
          aria-label="Bildirimlerde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[340px] md:flex-1"
        />

        <SegmentedSelect<RecurrenceScope>
          deger={kapsam}
          degistir={(d) => {
            setKapsam(d);
            setSayfa(1);
          }}
          etiket="Dönem"
          secenekler={[
            { deger: 'son', etiket: 'Son 30 gün' },
            { deger: 'tumu', etiket: 'Tüm geçmiş' },
          ]}
          className="md:ml-auto"
        />
      </div>

      {/* ── İkincil eylemler ── */}
      <div className="flex flex-wrap items-center gap-2">
        <Button
          varyant={yalnizcaOkunmamis ? 'birincil' : 'ikincil'}
          className="h-8 px-2.5 text-xs"
          onClick={() => {
            setYalnizcaOkunmamis((o) => !o);
            setSayfa(1);
          }}
        >
          <BellOff size={13} />
          Yalnızca okunmamış
        </Button>

        <span className="ml-auto flex gap-2">
          <Button
            varyant="sade"
            className="h-8 px-2.5 text-xs"
            onClick={() => tumunuOku.mutate()}
            disabled={tumunuOku.isPending}
          >
            <CheckCheck size={13} />
            Tümünü okundu işaretle
          </Button>
          <Button
            varyant="sade"
            className="h-8 px-2.5 text-xs"
            onClick={() => okunanlariSil.mutate()}
            disabled={okunanlariSil.isPending}
          >
            <Trash2 size={13} />
            Okunanları sil
          </Button>
        </span>
      </div>

      {/* ── Liste ── */}
      {liste.isLoading ? (
        <SkeletonRows adet={8} />
      ) : liste.isError ? (
        <EmptyState
          ikon={Bell}
          baslik="Bildirimler yüklenemedi"
          aciklama={(liste.error as Error)?.message}
        />
      ) : kayitlar.length === 0 ? (
        <EmptyState
          ikon={Bell}
          baslik={
            arama || yalnizcaOkunmamis ? 'Eşleşen bildirim yok' : 'Bildirim yok'
          }
          aciklama={
            kapsam === 'son'
              ? 'Son 30 günde bildirim bulunmuyor. Daha eskisi için “Tüm geçmiş”e geçin.'
              : 'Henüz size gönderilmiş bir bildirim yok.'
          }
        />
      ) : (
        <>
          <ul className="space-y-1.5">
            {kayitlar.map((b) => (
              <li key={b.id}>
                <button
                  onClick={() => ac(b)}
                  className={cn(
                    'flex w-full items-start gap-3 rounded-card border bg-surface p-3 text-left transition-colors hover:bg-surface-2',
                    b.okundu ? 'border-border' : 'border-l-[3px] border-l-brand border-border',
                  )}
                >
                  <span
                    className={cn(
                      'mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-md',
                      b.okundu ? 'bg-sunken text-text-3' : 'bg-brand-tint text-brand-2',
                    )}
                    aria-hidden
                  >
                    <Bell size={14} />
                  </span>

                  <span className="min-w-0 flex-1">
                    <span className="flex items-baseline gap-2">
                      <span
                        className={cn(
                          'min-w-0 flex-1 truncate text-sm',
                          b.okundu ? 'font-medium' : 'font-bold',
                        )}
                      >
                        {b.baslik}
                      </span>
                      <span className="shrink-0 text-xs tabular-nums text-text-3">
                        {dateTime(b.tarih)}
                      </span>
                    </span>
                    <span className="mt-0.5 block text-sm leading-normal text-text-2 metin-guzel">
                      {b.icerik}
                    </span>
                  </span>
                </button>
              </li>
            ))}
          </ul>

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="bildirim" />
        </>
      )}
    </div>
  );
}
