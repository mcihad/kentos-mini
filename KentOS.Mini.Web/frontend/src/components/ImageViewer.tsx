import * as Dialog from '@radix-ui/react-dialog';
import {
  ChevronLeft, ChevronRight, Download, Maximize2, Minimize2, X, ZoomIn, ZoomOut,
} from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { cn } from './utils';

export type Resim = { yol: string; baslik?: string | null; altBilgi?: string | null };

/**
 * Resim görüntüleyici.
 *
 * <p>
 * Resimler eskiden yeni sekmede açılıyordu: kullanıcı uygulamadan çıkıyor,
 * tarayıcının çıplak resim görünümünde kalıyor ve geri gelmek için sekme
 * kapatmak zorunda kalıyordu. Galeride gezmek de mümkün değildi.
 * </p>
 *
 * <p>
 * Yakınlaştırma <c>transform: scale</c> ile yapılır — genişlik/yükseklik
 * canlandırmak her karede yeniden yerleşim demek; <c>scale</c> ekran
 * kartında çalışır ve büyük fotoğrafta bile akıcı kalır.
 * </p>
 *
 * <p>
 * Klavye: <b>←/→</b> gezinme, <b>+/−</b> yakınlaştırma, <b>0</b> sıfırlama,
 * <b>F</b> tam ekran, <b>Esc</b> kapatma.
 * </p>
 */
export function ImageViewer({
  resimler,
  acikIndeks,
  kapat,
  indeksDegistir,
}: {
  resimler: Resim[];
  /** `null` = kapalı. */
  acikIndeks: number | null;
  kapat: () => void;
  indeksDegistir: (i: number) => void;
}) {
  const acik = acikIndeks !== null;
  const kap = useRef<HTMLDivElement>(null);

  const [olcek, setOlcek] = useState(1);
  const [kaydirma, setKaydirma] = useState({ x: 0, y: 0 });
  const [tamEkran, setTamEkran] = useState(false);
  const surukleme = useRef<{ x: number; y: number } | null>(null);

  const resim = acikIndeks !== null ? resimler[acikIndeks] : undefined;

  /** Resim değişince yakınlaştırma sıfırlanır — önceki resmin oranı yenisine yapışmasın. */
  useEffect(() => {
    setOlcek(1);
    setKaydirma({ x: 0, y: 0 });
  }, [acikIndeks]);

  const git = useCallback(
    (yon: -1 | 1) => {
      if (acikIndeks === null || resimler.length < 2) return;
      // Baştan sona / sondan başa döner: uzun galeride uca gelince
      // düğmenin ölmesi kullanıcıyı geri saymaya zorluyordu.
      indeksDegistir((acikIndeks + yon + resimler.length) % resimler.length);
    },
    [acikIndeks, resimler.length, indeksDegistir],
  );

  const tamEkranDegistir = useCallback(async () => {
    const hedef = kap.current;
    if (!hedef) return;

    try {
      if (document.fullscreenElement) {
        await document.exitFullscreen();
      } else {
        await hedef.requestFullscreen();
      }
    } catch {
      // Tarayıcı izin vermeyebilir (iOS Safari); sessizce yut, diyalog açık kalır.
    }
  }, []);

  // Tarayıcının kendi tam ekran çıkışını (Esc / sistem tuşu) izle.
  useEffect(() => {
    const dinle = () => setTamEkran(document.fullscreenElement !== null);
    document.addEventListener('fullscreenchange', dinle);
    return () => document.removeEventListener('fullscreenchange', dinle);
  }, []);

  useEffect(() => {
    if (!acik) return;

    const tus = (e: KeyboardEvent) => {
      if (e.key === 'ArrowRight') git(1);
      else if (e.key === 'ArrowLeft') git(-1);
      else if (e.key === '+' || e.key === '=') setOlcek((o) => Math.min(5, o + 0.25));
      else if (e.key === '-') setOlcek((o) => Math.max(1, o - 0.25));
      else if (e.key === '0') {
        setOlcek(1);
        setKaydirma({ x: 0, y: 0 });
      } else if (e.key.toLowerCase() === 'f') void tamEkranDegistir();
    };

    window.addEventListener('keydown', tus);
    return () => window.removeEventListener('keydown', tus);
  }, [acik, git, tamEkranDegistir]);

  if (!resim) return null;

  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde backdrop-blur-[3px]" />
        <Dialog.Content
          ref={kap}
          className="katman anim-katman fixed inset-0 z-400 flex flex-col bg-transparent outline-hidden"
          // Odak resme değil kabuğa gitsin; klavye kısayolları oradan işliyor.
          onOpenAutoFocus={(e) => e.preventDefault()}
        >
          <Dialog.Title className="sr-only">
            {resim.baslik ?? 'Fotoğraf'}
          </Dialog.Title>
          <Dialog.Description className="sr-only">
            Ok tuşlarıyla gezinin, + ve − ile yakınlaştırın, F ile tam ekran, Esc ile kapatın.
          </Dialog.Description>

          {/* ── Üst çubuk ── */}
          <div className="flex shrink-0 items-center gap-2 px-3 py-2.5 text-white">
            <div className="min-w-0 flex-1">
              {resim.baslik && (
                <p className="truncate text-sm font-semibold">{resim.baslik}</p>
              )}
              {resim.altBilgi && (
                <p className="truncate text-xs text-white/65">{resim.altBilgi}</p>
              )}
            </div>

            {resimler.length > 1 && (
              <span className="shrink-0 rounded-full bg-white/12 px-2.5 py-1 text-xs tabular-nums">
                {(acikIndeks ?? 0) + 1} / {resimler.length}
              </span>
            )}

            <CubukDugmesi
              etiket="Uzaklaştır"
              tikla={() => setOlcek((o) => Math.max(1, o - 0.25))}
              pasif={olcek <= 1}
            >
              <ZoomOut size={16} />
            </CubukDugmesi>
            <CubukDugmesi
              etiket="Yakınlaştır"
              tikla={() => setOlcek((o) => Math.min(5, o + 0.25))}
              pasif={olcek >= 5}
            >
              <ZoomIn size={16} />
            </CubukDugmesi>
            <CubukDugmesi etiket={tamEkran ? 'Tam ekrandan çık' : 'Tam ekran'} tikla={tamEkranDegistir}>
              {tamEkran ? <Minimize2 size={16} /> : <Maximize2 size={16} />}
            </CubukDugmesi>
            <CubukDugmesi
              etiket="İndir"
              tikla={() => window.open(resim.yol, '_blank', 'noopener')}
            >
              <Download size={16} />
            </CubukDugmesi>
            {/*
              KAPATMA `kapat()`I DOĞRUDAN ÇAĞIRIR.

              Önce `<Dialog.Close asChild>` sarmalıyordu: Radix kapatma
              işleyicisini `onClick` olarak ÇOCUĞA geçiriyor, ama
              `CubukDugmesi` gelen propları yaymıyor (yalnızca kendi
              adlandırılmış proplarını okuyor). İşleyici sessizce düşüyor ve
              `tikla={() => {}}` hiçbir şey yapmıyordu — düğme görünüyor,
              basılıyor, pencere KAPANMIYORDU. Masaüstünde fark edilmiyordu
              çünkü `Esc` çalışıyor; telefonda `Esc` yok ve görüntüleyici
              kilitleniyordu.
            */}
            <CubukDugmesi etiket="Kapat" tikla={kapat}>
              <X size={17} />
            </CubukDugmesi>
          </div>

          {/* ── Resim ── */}
          <div
            className="relative min-h-0 flex-1 overflow-hidden"
            onWheel={(e) => {
              // Ctrl/⌘ ile tekerlek yakınlaştırır; düz tekerlek galeride gezer.
              if (e.ctrlKey || e.metaKey) {
                setOlcek((o) => Math.min(5, Math.max(1, o - e.deltaY * 0.003)));
              }
            }}
            onPointerDown={(e) => {
              if (olcek <= 1) return;
              surukleme.current = { x: e.clientX - kaydirma.x, y: e.clientY - kaydirma.y };
              (e.target as HTMLElement).setPointerCapture?.(e.pointerId);
            }}
            onPointerMove={(e) => {
              if (!surukleme.current) return;
              setKaydirma({
                x: e.clientX - surukleme.current.x,
                y: e.clientY - surukleme.current.y,
              });
            }}
            onPointerUp={() => {
              surukleme.current = null;
            }}
          >
            <img
              src={resim.yol}
              alt={resim.baslik ?? 'Fotoğraf'}
              draggable={false}
              className={cn(
                'absolute inset-0 m-auto max-h-full max-w-full select-none object-contain',
                'transition-transform duration-200 ease-[cubic-bezier(.22,1,.36,1)] motion-reduce:transition-none',
                olcek > 1 ? 'cursor-grab active:cursor-grabbing' : 'cursor-zoom-in',
              )}
              style={{
                transform: `translate3d(${kaydirma.x}px, ${kaydirma.y}px, 0) scale(${olcek})`,
              }}
              onClick={() => olcek === 1 && setOlcek(2)}
            />

            {resimler.length > 1 && (
              <>
                <GezinmeDugmesi yon="sol" tikla={() => git(-1)} />
                <GezinmeDugmesi yon="sag" tikla={() => git(1)} />
              </>
            )}
          </div>

          {/* ── Küçük resim şeridi ── */}
          {resimler.length > 1 && (
            <div className="shrink-0 overflow-x-auto px-3 py-2.5 kaydirma-gizle">
              <div className="flex gap-1.5">
                {resimler.map((r, i) => (
                  <button
                    key={r.yol + i}
                    type="button"
                    onClick={() => indeksDegistir(i)}
                    aria-label={`${i + 1}. fotoğraf`}
                    aria-current={i === acikIndeks}
                    className={cn(
                      'h-12 w-16 shrink-0 overflow-hidden rounded-sm border-2 transition-colors',
                      i === acikIndeks ? 'border-white' : 'border-transparent opacity-55 hover:opacity-100',
                    )}
                  >
                    <img src={r.yol} alt="" className="h-full w-full object-cover" />
                  </button>
                ))}
              </div>
            </div>
          )}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

function CubukDugmesi({
  etiket,
  tikla,
  pasif,
  children,
}: {
  etiket: string;
  tikla: () => void;
  pasif?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={tikla}
      disabled={pasif}
      aria-label={etiket}
      title={etiket}
      className="grid h-9 w-9 shrink-0 place-items-center rounded-md text-white/85 transition-colors hover:bg-white/12 hover:text-white disabled:opacity-35"
    >
      {children}
    </button>
  );
}

function GezinmeDugmesi({ yon, tikla }: { yon: 'sol' | 'sag'; tikla: () => void }) {
  return (
    <button
      type="button"
      onClick={tikla}
      aria-label={yon === 'sol' ? 'Önceki fotoğraf' : 'Sonraki fotoğraf'}
      className={cn(
        'absolute top-1/2 grid h-11 w-11 -translate-y-1/2 place-items-center rounded-full',
        'bg-black/35 text-white/90 backdrop-blur-sm transition-colors hover:bg-black/55 hover:text-white',
        yon === 'sol' ? 'left-3' : 'right-3',
      )}
    >
      {yon === 'sol' ? <ChevronLeft size={20} /> : <ChevronRight size={20} />}
    </button>
  );
}

/** Görüntüleyiciyi açmayı kolaylaştıran kanca. */
export function useImageViewer() {
  const [acikIndeks, setAcikIndeks] = useState<number | null>(null);
  return {
    acikIndeks,
    ac: (i: number) => setAcikIndeks(i),
    kapat: () => setAcikIndeks(null),
    indeksDegistir: setAcikIndeks,
  };
}
