import { Download, MessageSquare, Paperclip, Reply, Trash2, Upload } from 'lucide-react';
import { useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Button, IconButton } from '../../components/Button';
import { Card, CardHeader } from '../../components/Card';
import { EmptyState } from '../../components/EmptyState';
import { NoteComposer } from '../../components/NoteComposer';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { dateTime } from '../../data/format';
import { tokenStore } from '../../data/client';
import {
  taskKeys, uploadTaskFile, useTaskAttachments, useTaskComments, useTaskMutations,
} from '../../data/tasks';
import type { WorkComment } from '../../data/types';

/**
 * DOSYALAR ve YORUMLAR — görevin tartışma alanı.
 *
 * <p>
 * İkisi de <b>ortak</b> bileşenlerden geliyor (<code>is_ekleri</code>,
 * <code>is_yorumlari</code>): görev, aşama, proje ve vatandaş bildirimi aynı
 * tabloları kullanıyor. Her varlık için ayrı dosya ve yorum tablosu açmak on
 * tablo ve on ayrı servis yolu demekti.
 * </p>
 */
export function TaskDiscussion({ gorevId, kapali }: { gorevId: number; kapali: boolean }) {
  return (
    <div className="space-y-3.5">
      <Ekler gorevId={gorevId} kapali={kapali} />
      <Yorumlar gorevId={gorevId} />
    </div>
  );
}

// ── dosyalar ─────────────────────────────────────────────────────────

function Ekler({ gorevId, kapali }: { gorevId: number; kapali: boolean }) {
  const { bildir } = useToast();
  const qc = useQueryClient();
  const { hasPermission } = useSession();
  const m = useTaskMutations(gorevId);

  const { data: ekler } = useTaskAttachments(gorevId);
  const [yukleniyor, setYukleniyor] = useState(false);
  const alan = useRef<HTMLInputElement>(null);

  const yazabilir =
    !kapali &&
    (hasPermission(PERMISSION.gorevDuzenle) || hasPermission(PERMISSION.gorevAsama));

  async function yukle(dosya: File) {
    setYukleniyor(true);
    try {
      await uploadTaskFile(gorevId, dosya);
      qc.invalidateQueries({ queryKey: taskKeys.attachments(gorevId) });
      bildir('basari', 'Dosya yüklendi');
    } catch (h) {
      bildir('hata', 'Dosya yüklenemedi', (h as Error).message);
    } finally {
      setYukleniyor(false);
      if (alan.current) alan.current.value = '';
    }
  }

  return (
    <Card>
      <CardHeader
        baslik="Dosyalar"
        aciklama={ekler?.length ? `${ekler.length} dosya` : undefined}
        eylem={
          yazabilir ? (
            <>
              <input
                ref={alan}
                type="file"
                className="hidden"
                onChange={(e) => {
                  const d = e.target.files?.[0];
                  if (d) yukle(d);
                }}
              />
              <Button varyant="sade" onClick={() => alan.current?.click()} disabled={yukleniyor}>
                <Upload size={14} />
                Yükle
              </Button>
            </>
          ) : undefined
        }
      />

      {!ekler || ekler.length === 0 ? (
        <div className="px-3.5 pb-4">
          <EmptyState ikon={Paperclip} baslik="Dosya yok" />
        </div>
      ) : (
        <ul className="divide-y divide-line">
          {ekler.map((e) => (
            <li key={e.id} className="flex items-center gap-2.5 px-3.5 py-2.5">
              <Paperclip size={15} className="flex-none text-ink-3" />
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm text-ink">{e.ad}</span>
                <span className="text-2xs text-ink-3">
                  {boyutMetni(e.boyut ?? 0)}
                  {e.yukleyen && ` · ${e.yukleyen}`}
                  {e.tarih && ` · ${dateTime(e.tarih)}`}
                </span>
              </span>
              <IconButton etiket="İndir" onClick={() => indir(e.id!, e.ad ?? 'dosya')}>
                <Download size={16} />
              </IconButton>
              {yazabilir && (
                <IconButton
                  etiket="Sil"
                  onClick={async () => {
                    try {
                      await m.ekSil.mutateAsync(e.id!);
                      qc.invalidateQueries({ queryKey: taskKeys.attachments(gorevId) });
                      bildir('basari', 'Dosya silindi');
                    } catch (h) {
                      bildir('hata', 'Dosya silinemedi', (h as Error).message);
                    }
                  }}
                >
                  <Trash2 size={16} />
                </IconButton>
              )}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

/**
 * Dosyayı indirir.
 *
 * Basit bir <code>&lt;a href&gt;</code> yetmiyor: indirme ucu kimlik denetimli
 * ve tarayıcı gezinme isteğine <code>Authorization</code> başlığı eklemiyor.
 * Bu yüzden içerik <code>fetch</code> ile alınıp geçici bir bağlantıya
 * bağlanıyor.
 */
async function indir(ekId: number, ad: string) {
  const jeton = tokenStore.read();
  const yanit = await fetch(`/api/v2/gorev/ek/${ekId}`, {
    headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : undefined,
  });
  if (!yanit.ok) return;

  const veri = await yanit.blob();
  const url = URL.createObjectURL(veri);
  const bag = document.createElement('a');
  bag.href = url;
  bag.download = ad;
  bag.click();
  URL.revokeObjectURL(url);
}

function boyutMetni(bayt: number): string {
  if (bayt < 1024) return `${bayt} B`;
  if (bayt < 1024 * 1024) return `${Math.round(bayt / 1024)} KB`;
  return `${(bayt / 1024 / 1024).toFixed(1)} MB`;
}

// ── yorumlar ─────────────────────────────────────────────────────────

function Yorumlar({ gorevId }: { gorevId: number }) {
  const { bildir } = useToast();
  const m = useTaskMutations(gorevId);
  const { data: yorumlar } = useTaskComments(gorevId);
  const [yanitlanan, setYanitlanan] = useState<number | null>(null);

  async function gonder(metin: string) {
    await m.yorumEkle.mutateAsync({ metin, ustYorumId: yanitlanan });
    setYanitlanan(null);
  }

  return (
    <Card>
      <CardHeader baslik="Yorumlar" aciklama={yorumlar?.length ? undefined : 'Henüz yorum yok'} />

      <div className="px-3.5 pb-3.5">
        {yanitlanan !== null && (
          <p className="mb-2 flex items-center gap-2 text-2xs text-ink-3">
            <Reply size={12} />
            Bir yoruma yanıt yazıyorsunuz.
            <button
              type="button"
              className="underline"
              onClick={() => setYanitlanan(null)}
            >
              vazgeç
            </button>
          </p>
        )}

        <NoteComposer
          yerTutucu="Yorum yazın"
          alanId={`gorev-${gorevId}-yorum`}
          bekliyor={m.yorumEkle.isPending}
          gonder={async (metin) => {
            try {
              await gonder(metin);
            } catch (h) {
              bildir('hata', 'Yorum eklenemedi', (h as Error).message);
              throw h;
            }
          }}
        />
      </div>

      {yorumlar && yorumlar.length > 0 && (
        <ul className="divide-y divide-line">
          {yorumlar.map((y) => (
            <YorumSatiri
              key={y.id}
              yorum={y}
              derinlik={0}
              yanitla={setYanitlanan}
              sil={async (id) => {
                try {
                  await m.yorumSil.mutateAsync(id);
                  bildir('basari', 'Yorum silindi');
                } catch (h) {
                  bildir('hata', 'Yorum silinemedi', (h as Error).message);
                }
              }}
            />
          ))}
        </ul>
      )}

      {(!yorumlar || yorumlar.length === 0) && (
        <div className="px-3.5 pb-4">
          <EmptyState ikon={MessageSquare} baslik="Yorum yok" />
        </div>
      )}
    </Card>
  );
}

/**
 * Tek yorum ve altındaki yanıtlar.
 *
 * Girinti <b>üç seviyede duruyor</b>: daha derini dar ekranda metni okunmaz
 * bir sütuna sıkıştırıyor. Ağacın kendisi sunucudan geliyor ve derinlik
 * sınırsız — kısıtlanan yalnızca çizim.
 */
function YorumSatiri({
  yorum,
  derinlik,
  yanitla,
  sil,
}: {
  yorum: WorkComment;
  derinlik: number;
  yanitla: (id: number) => void;
  sil: (id: number) => void;
}) {
  const girinti = Math.min(derinlik, 3) * 16;

  return (
    <li>
      <div className="px-3.5 py-2.5" style={{ paddingLeft: 14 + girinti }}>
        <div className="flex items-baseline gap-2">
          <span className="text-xs font-medium text-ink">{yorum.yazan || 'Kullanıcı'}</span>
          <span className="text-3xs text-ink-3">{dateTime(yorum.tarih)}</span>
          {yorum.silindi && <span className="text-3xs text-ink-3">· silindi</span>}
        </div>

        <p className="mt-1 whitespace-pre-wrap text-sm text-text-2">
          {yorum.silindi ? <em className="text-ink-3">Bu yorum silindi.</em> : yorum.metin}
        </p>

        {!yorum.silindi && (
          <div className="mt-1.5 flex items-center gap-3">
            <button
              type="button"
              className="inline-flex items-center gap-1 text-3xs text-ink-3 hover:text-ink-2"
              onClick={() => yanitla(yorum.id!)}
            >
              <Reply size={11} />
              Yanıtla
            </button>
            {yorum.benimMi && (
              <button
                type="button"
                className="inline-flex items-center gap-1 text-3xs text-ink-3 hover:text-(--st-no)"
                onClick={() => sil(yorum.id!)}
              >
                <Trash2 size={11} />
                Sil
              </button>
            )}
          </div>
        )}
      </div>

      {(yorum.yanitlar ?? []).length > 0 && (
        <ul>
          {yorum.yanitlar!.map((y) => (
            <YorumSatiri
              key={y.id}
              yorum={y}
              derinlik={derinlik + 1}
              yanitla={yanitla}
              sil={sil}
            />
          ))}
        </ul>
      )}
    </li>
  );
}
