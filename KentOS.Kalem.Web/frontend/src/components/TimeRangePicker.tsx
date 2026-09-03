import * as Popover from '@radix-ui/react-popover';
import { Clock } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { cn } from './utils';
import { useSwipeGestures } from './swipeGestures';

/** Izgara adımı — takvimle aynı olmak zorunda. */
const ADIM_DK = 30;

const ik = (n: number) => String(n).padStart(2, '0');

/** `HH:mm` → gece yarısından itibaren dakika. */
export function clockToMinutes(metin: string): number | null {
  const e = /^(\d{1,2}):(\d{2})$/.exec(metin);
  if (!e) return null;
  const s = Number(e[1]);
  const d = Number(e[2]);
  if (s < 0 || s > 23 || d < 0 || d > 59) return null;
  return s * 60 + d;
}

/** Dakika → `HH:mm`. 24 saati aşan değerler ertesi güne taşar. */
export function minutesToClock(dk: number): string {
  const t = ((dk % 1440) + 1440) % 1440;
  return `${ik(Math.floor(t / 60))}:${ik(t % 60)}`;
}

/** `1 sa 30 dk` */
export function durationText(dakika: number): string {
  if (dakika <= 0) return '—';
  const sa = Math.floor(dakika / 60);
  const dk = dakika % 60;
  if (sa === 0) return `${dk} dk`;
  if (dk === 0) return `${sa} sa`;
  return `${sa} sa ${dk} dk`;
}

/** Gün boyu 30 dakikalık dilimler. */
const DILIMLER = Array.from({ length: (24 * 60) / ADIM_DK }, (_, i) => minutesToClock(i * ADIM_DK));

/**
 * Zaman aralığı seçici — başlangıç ve bitiş birlikte.
 *
 * <p>
 * İki ayrı saat kutusu yerine TEK bileşen olmasının sebebi: kullanıcı
 * "14:00–15:00" diye düşünüyor, iki bağımsız alan olarak değil. Bitiş
 * başlangıçtan önce olamaz ve başlangıç değişince bitiş <b>süreyi
 * koruyarak</b> kayar — 30 dakikalık bir etkinliği bir saat ileri almak
 * için iki alanı da düzeltmek gerekmiyor.
 * </p>
 *
 * <p>
 * Değerler saat dilimi TAŞIMAZ (`HH:mm`); tarih ayrı bileşende. Sunucudaki
 * damgalar da kayan yerel saat olduğu için bu ayrım doğal.
 * </p>
 */
export function TimeRangePicker({
  baslangic,
  bitis,
  degistir,
  tumGun,
  id,
}: {
  /** `HH:mm` */
  baslangic: string;
  /** `HH:mm` — boş olabilir. */
  bitis: string;
  degistir: (bas: string, bit: string) => void;
  tumGun?: boolean;
  id?: string;
}) {
  const basDk = clockToMinutes(baslangic) ?? 0;
  const bitDk = clockToMinutes(bitis);
  const sure = bitDk === null ? 0 : (bitDk >= basDk ? bitDk - basDk : bitDk + 1440 - basDk);

  /** Başlangıç kayarsa bitiş SÜREYİ KORUYARAK kayar. */
  function baslangicDegistir(yeni: string) {
    const yeniDk = clockToMinutes(yeni);
    if (yeniDk === null) return;

    if (bitDk === null || sure <= 0) {
      degistir(yeni, minutesToClock(yeniDk + ADIM_DK));
      return;
    }
    degistir(yeni, minutesToClock(yeniDk + sure));
  }

  /** Bitiş başlangıçtan önce seçilemez; seçilirse ertesi güne taşındığı varsayılmaz. */
  function bitisDegistir(yeni: string) {
    const yeniDk = clockToMinutes(yeni);
    if (yeniDk === null) return;
    if (yeniDk <= basDk) {
      // Geçersiz seçim: bir adım sonrasına çek. Sessizce yok saymak,
      // kullanıcının tıkladığı değerin neden tutmadığını anlaşılmaz kılar.
      degistir(baslangic, minutesToClock(basDk + ADIM_DK));
      return;
    }
    degistir(baslangic, yeni);
  }

  if (tumGun) {
    return (
      <div
        className="flex h-10 items-center gap-2.5 rounded-control border border-border bg-surface-2 px-3 text-sm text-text-3"
        id={id}
      >
        <Clock size={15} className="shrink-0" />
        Tüm gün
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2" id={id}>
      <SaatKutusu deger={baslangic} degistir={baslangicDegistir} etiket="Başlangıç saati" />
      <span className="shrink-0 text-sm text-text-3" aria-hidden>
        –
      </span>
      <SaatKutusu
        deger={bitis}
        degistir={bitisDegistir}
        etiket="Bitiş saati"
        enAz={minutesToClock(basDk + ADIM_DK)}
      />
      <span className="shrink-0 whitespace-nowrap rounded-full bg-sunken px-2.5 py-1 text-xs tabular-nums text-text-2">
        {durationText(sure)}
      </span>
    </div>
  );
}

/**
 * Tek saat kutusu.
 *
 * Açılır liste seçili saate KAYDIRILIR; 48 dilimlik bir listede kullanıcının
 * 14:00'ı elle araması gereksiz.
 */
function SaatKutusu({
  deger,
  degistir,
  etiket,
  enAz,
}: {
  deger: string;
  degistir: (d: string) => void;
  etiket: string;
  enAz?: string;
}) {
  const seciliRef = useRef<HTMLButtonElement>(null);
  const [acik, setAcik] = useState(false);

  // Tekerlek ve parmak hareketleri AÇIKÇA bağlanır; `overflow-y-auto` tek
  // başına yetmiyordu (Radix panel içindeki kaydırmayı yutuyor).
  const { kap: listeRef, baglar: kaydirmaBaglari } =
    useSwipeGestures<HTMLDivElement>();

  /**
   * Seçili saate KAYDIR — yalnızca panel AÇILDIĞINDA.
   *
   * GERÇEK HATA: bu etki bağımlılık dizisi olmadan yazılmıştı, yani her
   * yeniden çizimde çalışıyor ve listeyi sürekli seçili öğeye geri
   * çekiyordu. Kullanıcı fare tekerleğiyle kaydırdığında liste anında geri
   * zıplıyor, "kaydırma çalışmıyor" gibi görünüyordu.
   */
  useEffect(() => {
    if (!acik) return;
    // Panelin yerleşmesini bekle; açılış karesinde ölçüler henüz yok.
    const z = requestAnimationFrame(() =>
      seciliRef.current?.scrollIntoView({ block: 'center' }),
    );
    return () => cancelAnimationFrame(z);
  }, [acik]);

  return (
    <Popover.Root open={acik} onOpenChange={setAcik}>
      <Popover.Trigger asChild>
        <button
          type="button"
          aria-label={etiket}
          className="flex h-10 min-w-[86px] items-center gap-2 rounded-control border border-border bg-surface px-2.5 text-sm tabular-nums
            transition-colors hover:bg-surface-2 focus:border-brand focus:outline-hidden focus:ring-2 focus:ring-(--focus-ring)"
        >
          <Clock size={14} className="shrink-0 text-text-3" />
          <span className={cn(!deger && 'text-text-3')}>{deger || '--:--'}</span>
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={6}
          className="katman anim-katman z-400 w-[104px] rounded-card border border-border bg-surface p-1 shadow-3"
        >
          {/*
            `overscroll-contain`: liste sonuna gelince kaydırma arkadaki
            sayfaya geçmesin. `touch-pan-y` dokunmatikte dikey kaydırmayı
            açıkça serbest bırakır — Radix'in dokunma yakalaması bazı
            tarayıcılarda listeyi kilitliyordu.
          */}
          <div
            ref={listeRef}
            {...kaydirmaBaglari}
            className="max-h-[260px] touch-pan-y overflow-y-auto overscroll-contain"
          >
            {DILIMLER.map((s) => {
              const secili = s === deger;
              const kapali = enAz !== undefined && s < enAz;
              return (
                <Popover.Close asChild key={s}>
                  <button
                    ref={secili ? seciliRef : undefined}
                    type="button"
                    disabled={kapali}
                    onClick={() => degistir(s)}
                    className={cn(
                      'block w-full rounded-sm px-2.5 py-1.5 text-left text-sm tabular-nums transition-colors',
                      secili ? 'bg-brand font-semibold text-on-brand' : 'hover:bg-surface-2',
                      kapali && 'cursor-not-allowed opacity-35',
                    )}
                  >
                    {s}
                  </button>
                </Popover.Close>
              );
            })}
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
