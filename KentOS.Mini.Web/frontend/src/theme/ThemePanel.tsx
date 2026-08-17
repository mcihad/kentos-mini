import * as Dialog from '@radix-ui/react-dialog';
import { Check, Copy, RotateCcw, X } from 'lucide-react';
import { useState } from 'react';
import type { ReactNode } from 'react';
import { cn } from '../components/utils';
import { FONTS, BRAND_COLORS, NEUTRAL_COLORS, ACCENT_COLORS } from './palettes';
import { RANGES, PRESETS, type PresetKey } from './presets';
import { useTheme } from './ThemeProvider';

/**
 * TEMA TASARIMCISI — design_new/design.md §3.
 *
 * Panel YALNIZCA 14 çekirdek token'ı yazar. Anlamsal katman ve bileşenler
 * hiçbir şey bilmez; `color-mix`/`calc` zinciri tek karede günceller. Bu
 * yüzden kaydırıcıyı sürüklerken bütün arayüz akıcı biçimde dönüyor —
 * hiçbir React bileşeni yeniden render olmuyor.
 *
 * Renk seçimi serbest picker DEĞİL, küratörlü palet: her seçenek gündüz+gece
 * çifti olarak elle dengelendi, dolayısıyla hiçbir seçim kontrast kuralını
 * bozamıyor. Serbest hex'e izin vermek, kullanıcıya okunmayan bir arayüz
 * üretme imkânı vermek olurdu.
 */
export function ThemePanel({ acik, kapat }: { acik: boolean; kapat: () => void }) {
  const t = useTheme();
  const [kopyalandi, setKopyalandi] = useState(false);

  const kopyala = async () => {
    try {
      await navigator.clipboard.writeText(t.tokenCiktisi());
      setKopyalandi(true);
      setTimeout(() => setKopyalandi(false), 1600);
    } catch {
      /* pano izni yoksa sessizce geç — metin zaten ekranda seçilebilir */
    }
  };

  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-[90] bg-perde-hafif" />
        <Dialog.Content
          aria-describedby={undefined}
          className="anim-panel fixed inset-y-0 right-0 z-[90] flex w-[min(420px,100vw)] flex-col border-l border-line bg-surface shadow-3"
        >
          <header className="flex h-appbar flex-none items-center gap-2 border-b border-line px-4">
            <div className="min-w-0 flex-1">
              <Dialog.Title className="font-display text-base font-bold tracking-[var(--track-d)]">
                Tema Tasarımcısı
              </Dialog.Title>
              <p className="truncate text-2xs text-ink-3">
                {t.preset === 'ozel' ? 'Özel tema' : PRESETS[t.preset].ad}
              </p>
            </div>
            <button
              onClick={t.sifirla}
              className="grid h-9 w-9 place-items-center rounded-sm text-ink-2 transition-colors hover:bg-surface-2"
              aria-label="Varsayılana sıfırla"
              title="Varsayılana sıfırla"
            >
              <RotateCcw size={16} strokeWidth={1.8} />
            </button>
            <Dialog.Close
              className="grid h-9 w-9 place-items-center rounded-sm text-ink-2 transition-colors hover:bg-surface-2"
              aria-label="Kapat"
            >
              <X size={17} strokeWidth={1.9} />
            </Dialog.Close>
          </header>

          <div className="min-h-0 flex-1 space-y-5 overflow-y-auto p-4">
            <Bolum baslik="Hazır temalar">
              <div className="grid grid-cols-2 gap-2">
                {(Object.keys(PRESETS) as Exclude<PresetKey, 'ozel'>[]).map((p) => (
                  <PresetKarti
                    key={p}
                    anahtar={p}
                    secili={t.preset === p}
                    sec={() => t.presetSec(p)}
                  />
                ))}
              </div>
            </Bolum>

            <Bolum baslik="Mod">
              <Segment
                secenekler={[
                  { deger: 'acik', etiket: 'Gündüz' },
                  { deger: 'koyu', etiket: 'Gece' },
                ]}
                deger={t.knob.mod}
                degistir={(d) => t.knobAyarla('mod', d as 'acik' | 'koyu')}
              />
            </Bolum>

            <Bolum baslik="Marka rengi" not={BRAND_COLORS[t.knob.marka]?.ad}>
              <ColorStrip liste={BRAND_COLORS} secili={t.knob.marka} sec={(i) => t.knobAyarla('marka', i)} />
            </Bolum>

            <Bolum baslik="Vurgu rengi" not={ACCENT_COLORS[t.knob.vurgu]?.ad}>
              <ColorStrip liste={ACCENT_COLORS} secili={t.knob.vurgu} sec={(i) => t.knobAyarla('vurgu', i)} />
            </Bolum>

            <Bolum baslik="Nötr taban">
              <Segment
                secenekler={NEUTRAL_COLORS.map((n, i) => ({ deger: String(i), etiket: n.ad }))}
                deger={String(t.knob.notr)}
                degistir={(d) => t.knobAyarla('notr', Number(d))}
              />
            </Bolum>

            <Bolum baslik="Yazı tipi çifti">
              <Segment
                secenekler={FONTS.map((f, i) => ({ deger: String(i), etiket: f.ad }))}
                deger={String(t.knob.font)}
                degistir={(d) => t.knobAyarla('font', Number(d))}
              />
            </Bolum>

            <Bolum baslik="Ölçüler">
              <Kaydirici etiket="Köşe ovalliği" alan="r" />
              <Kaydirici etiket="Gölge yoğunluğu" alan="sha" bicim={(v) => (v / 100).toFixed(2)} />
              <Kaydirici etiket="Boşluk birimi" alan="sp" />
              <Kaydirici etiket="Temel yazı boyutu" alan="fs" />
              <Kaydirici etiket="Başlık ölçeği" alan="fsd" />
              <Kaydirici etiket="Harf aralığı" alan="track" bicim={(v) => v.toFixed(3)} />
              <Kaydirici etiket="Hareket süresi" alan="dur" />
              <div className="pt-1">
                <Etiket>Kenarlık kalınlığı</Etiket>
                <Segment
                  secenekler={[
                    { deger: '1', etiket: '1px' },
                    { deger: '1.5', etiket: '1.5px' },
                    { deger: '2', etiket: '2px' },
                  ]}
                  deger={String(t.knob.bw)}
                  degistir={(d) => t.knobAyarla('bw', Number(d))}
                />
              </div>
            </Bolum>

            <Bolum baslik="Canlı önizleme">
              <Onizleme />
            </Bolum>

            <Bolum baslik="Token çıktısı" not="globals.css'e yapıştırılabilir">
              <div className="relative">
                <pre className="max-h-56 overflow-auto rounded-sm border border-line bg-sunken p-3 text-3xs leading-[1.7] text-ink-2">
                  {t.tokenCiktisi()}
                </pre>
                <button
                  onClick={kopyala}
                  className="absolute right-2 top-2 flex items-center gap-1.5 rounded-sm border border-line bg-surface px-2 py-1 text-3xs font-semibold text-ink-2 transition-colors hover:bg-surface-2"
                >
                  {kopyalandi ? <Check size={12} className="text-ok" /> : <Copy size={12} />}
                  {kopyalandi ? 'Kopyalandı' : 'Kopyala'}
                </button>
              </div>
            </Bolum>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

/* ══════════════════════════════════════════════════════ parçalar */

function Bolum({ baslik, not, children }: { baslik: string; not?: string; children: ReactNode }) {
  return (
    <section className="space-y-2">
      <div className="flex items-baseline justify-between">
        <h3 className="text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">{baslik}</h3>
        {not && <span className="text-3xs text-ink-3">{not}</span>}
      </div>
      {children}
    </section>
  );
}

function Etiket({ children }: { children: ReactNode }) {
  return (
    <span className="mb-1.5 block text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">
      {children}
    </span>
  );
}

/**
 * Preset kartı — kendi renklerini GÖSTERİR.
 *
 * Ad listesi yeterli değildi: "Petrol Mavisi" ile "Antrasit Gece" arasındaki
 * fark okunarak değil görülerek seçiliyor.
 */
function PresetKarti({
  anahtar,
  secili,
  sec,
}: {
  anahtar: Exclude<PresetKey, 'ozel'>;
  secili: boolean;
  sec: () => void;
}) {
  const p = PRESETS[anahtar];
  const m = BRAND_COLORS[p.marka];
  const v = ACCENT_COLORS[p.vurgu];
  const gece = p.mod === 'koyu';

  return (
    <button
      onClick={sec}
      aria-pressed={secili}
      className={cn(
        'flex items-center gap-2 rounded-sm border p-2 text-left transition-colors',
        secili ? 'border-brand bg-brand-soft' : 'border-line bg-surface hover:bg-surface-2',
      )}
    >
      <span
        className="grid h-8 w-8 flex-none place-items-center rounded-xs"
        style={{ background: gece ? m.koyu : m.acik }}
      >
        <span
          className="h-2.5 w-2.5 rounded-full"
          style={{ background: gece ? v.koyu : v.acik }}
        />
      </span>
      <span className="min-w-0">
        <span className="block truncate text-2xs font-semibold text-ink">{p.ad}</span>
        <span className="block text-3xs text-ink-3">{gece ? 'Gece' : 'Gündüz'}</span>
      </span>
    </button>
  );
}

function ColorStrip({
  liste,
  secili,
  sec,
}: {
  liste: typeof BRAND_COLORS;
  secili: number;
  sec: (i: number) => void;
}) {
  const { knob } = useTheme();
  const gece = knob.mod === 'koyu';

  return (
    <div className="flex flex-wrap gap-2">
      {liste.map((r, i) => (
        <button
          key={r.ad}
          onClick={() => sec(i)}
          aria-label={r.ad}
          title={r.ad}
          aria-pressed={secili === i}
          className={cn(
            'grid h-9 w-9 place-items-center rounded-sm border-2 transition-transform active:scale-90',
            secili === i ? 'border-ink' : 'border-transparent',
          )}
          style={{ background: gece ? r.koyu : r.acik }}
        >
          {secili === i && (
            <Check size={15} strokeWidth={3} style={{ color: gece ? '#0b0f16' : '#fff' }} />
          )}
        </button>
      ))}
    </div>
  );
}

function Segment({
  secenekler,
  deger,
  degistir,
}: {
  secenekler: { deger: string; etiket: string }[];
  deger: string;
  degistir: (d: string) => void;
}) {
  return (
    <div className="flex gap-[3px] rounded-sm border border-line bg-sunken p-[3px]">
      {secenekler.map((s) => (
        <button
          key={s.deger}
          onClick={() => degistir(s.deger)}
          aria-pressed={deger === s.deger}
          className={cn(
            'h-[30px] flex-1 rounded-xs text-2xs font-semibold transition-colors',
            deger === s.deger ? 'bg-surface text-ink shadow-1' : 'text-ink-3 hover:text-ink-2',
          )}
        >
          {s.etiket}
        </button>
      ))}
    </div>
  );
}

function Kaydirici({
  etiket,
  alan,
  bicim,
}: {
  etiket: string;
  alan: keyof typeof RANGES;
  bicim?: (v: number) => string;
}) {
  const t = useTheme();
  const a = RANGES[alan];
  const deger = t.knob[alan] as number;

  return (
    <label className="block pt-1">
      <span className="mb-1.5 flex items-baseline justify-between">
        <Etiket>{etiket}</Etiket>
        <span className="text-2xs tabular-nums text-ink-2">
          {bicim ? bicim(deger) : deger}
          {a.birim}
        </span>
      </span>
      <input
        type="range"
        min={a.min}
        max={a.max}
        step={a.adim}
        value={deger}
        onChange={(e) => t.knobAyarla(alan, Number(e.target.value) as never)}
        className="w-full accent-brand"
      />
    </label>
  );
}

/** Kart + iki buton + durum çipi — knob'un etkisi burada anında görünür. */
function Onizleme() {
  return (
    <div className="rounded-md border border-line bg-surface p-3.5 shadow-1">
      <p className="font-display text-base font-bold tracking-[var(--track-d)] text-ink">
        Muhtarlar Toplantısı
      </p>
      <p className="mt-1 text-sm text-ink-2">Belediye Meclis Salonu · 14:00</p>
      <div className="mt-3 flex items-center gap-2">
        <span className="inline-flex h-6 items-center gap-1.5 rounded-full bg-ok-soft px-2.5 text-3xs font-semibold text-ok">
          <span className="h-[5px] w-[5px] rounded-full bg-current" />
          Onaylandı
        </span>
        <span className="inline-flex h-6 items-center gap-1.5 rounded-full bg-warn-soft px-2.5 text-3xs font-semibold text-warn">
          <span className="h-[5px] w-[5px] rounded-full bg-current" />
          Beklemede
        </span>
      </div>
      <div className="mt-3 flex gap-2">
        <button className="h-ctrl flex-1 rounded-sm bg-brand px-3.5 font-display text-xs font-semibold text-on-brand shadow-1 transition-colors hover:bg-brand-hover">
          Onayla
        </button>
        <button className="h-ctrl flex-1 rounded-sm border border-line bg-surface px-3.5 text-xs font-medium text-ink-2 transition-colors hover:bg-surface-2 hover:text-ink">
          Vazgeç
        </button>
      </div>
    </div>
  );
}
