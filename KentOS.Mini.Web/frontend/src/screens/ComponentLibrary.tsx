import {
  Ban,
  Calendar,
  CalendarClock,
  CheckCheck,
  CircleAlert,
  CircleCheck,
  CircleX,
  Hourglass,
  Inbox,
  Palette,
  Plus,
  User,
} from 'lucide-react';
import type { ReactNode } from 'react';
import { InsetGroup, ListRow } from '../components/ListRow';
import { useTheme } from '../theme/ThemeProvider';
import { PRESETS } from '../theme/presets';

/**
 * BİLEŞEN KÜTÜPHANESİ — tasarım denetiminin yapıldığı ekran (design_new §7).
 *
 * NEDEN VAR: token motoru 14 knob'la çalışıyor ve `--r=0`, `--sp=3.2`,
 * `--fs=17`, `--bw=2`, `--sh-a=0` gibi uç değerlerde bir şeyin kırılıp
 * kırılmadığı ancak her bileşen YAN YANA görüldüğünde anlaşılıyor. 40 ekranı
 * tek tek gezerek denetlemek pratikte yapılmıyor; burası tek sayfada
 * hepsini gösteriyor ve tema değiştikçe canlı güncelleniyor.
 *
 * Ekranın kendisi de bir kural: buraya eklenmemiş bileşen "bitmiş" sayılmaz.
 */
export default function ComponentLibrary() {
  const t = useTheme();

  return (
    <div className="space-y-6">
      <div>
        <h2 className="font-display text-2xl font-bold tracking-[var(--track-d)]">
          Bileşen Kütüphanesi
        </h2>
        <p className="mt-0.5 text-sm text-ink-2">
          Tüm bileşenler tek sayfada; tema değiştikçe canlı güncellenir.
        </p>
      </div>

      {/* Etkin tema künyesi — hangi ayarla bakıyoruz? */}
      <div className="flex flex-wrap items-center gap-2 rounded-md border border-brand-line bg-brand-soft p-3">
        <Palette size={16} className="text-brand" />
        <span className="text-sm font-semibold text-ink">
          {t.preset === 'ozel' ? 'Özel tema' : PRESETS[t.preset].ad}
        </span>
        <span className="text-2xs text-ink-2">
          {t.knob.mod === 'koyu' ? 'Gece' : 'Gündüz'} · r={t.knob.r}px · sp={t.knob.sp}px ·
          fs={t.knob.fs}px · bw={t.knob.bw}px · gölge={(t.knob.sha / 100).toFixed(2)} ·
          {t.knob.dur}ms
        </span>
      </div>

      <Bolum baslik="Renk token'ları" not="Anlamsal katman — bileşenler yalnızca bunları görür">
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-5">
          {[
            ['canvas', 'Masa'], ['bg', 'Zemin'], ['surface', 'Yüzey'],
            ['surface-2', 'Yüzey 2'], ['sunken', 'Çukur'],
            ['line', 'Çizgi'], ['line-2', 'Çizgi 2'],
            ['ink', 'Mürekkep'], ['ink-2', 'Mürekkep 2'], ['ink-3', 'Mürekkep 3'],
            ['brand-ui', 'Marka'], ['brand-soft', 'Marka yumuşak'],
            ['accent-ui', 'Vurgu'], ['accent-soft', 'Vurgu yumuşak'],
            ['warn', 'Uyarı'], ['ok', 'Onay'], ['info', 'Bilgi'],
            ['danger', 'Tehlike'], ['mute', 'Sessiz'], ['slate', 'Arduvaz'],
          ].map(([token, ad]) => (
            <div key={token} className="overflow-hidden rounded-sm border border-line">
              <div className="h-11" style={{ background: `var(--${token})` }} />
              <div className="bg-surface px-2 py-1.5">
                <div className="text-3xs font-semibold text-ink">{ad}</div>
                <code className="text-3xs text-ink-3">--{token}</code>
              </div>
            </div>
          ))}
        </div>
      </Bolum>

      <Bolum baslik="Tipografi ölçeği" not="--fs ve --fs-d knob'larından türer">
        <div className="space-y-2 rounded-md border border-line bg-surface p-4">
          {([
            ['4xl', 'Giriş başlığı', 'font-display font-bold'],
            ['3xl', 'Mobil büyük başlık', 'font-display font-bold'],
            ['2xl', 'Ekran başlığı / metrik', 'font-display font-bold'],
            ['xl', 'Appbar başlığı', 'font-display font-bold'],
            ['lg', 'Vurgulu gövde', 'font-medium'],
            ['base', 'Gövde', ''],
            ['sm', 'Tablo / liste', ''],
            ['xs', 'Meta, buton', 'font-medium'],
            ['2xs', 'Alan etiketi', 'font-semibold uppercase tracking-[0.08em]'],
            ['3xs', 'Kolon başlığı', 'font-semibold uppercase tracking-[0.12em]'],
          ] as const).map(([boyut, ad, ek]) => (
            <div key={boyut} className="flex items-baseline gap-3 border-b border-line pb-2 last:border-0">
              <code className="w-12 flex-none text-3xs text-ink-3">{boyut}</code>
              <span className={`text-${boyut} ${ek} truncate text-ink`}>{ad}</span>
            </div>
          ))}
        </div>
      </Bolum>

      <Bolum baslik="Buton varyantları" not="§7.1">
        <div className="flex flex-wrap gap-2 rounded-md border border-line bg-surface p-4">
          <button className="h-ctrl rounded-sm bg-brand px-3.5 font-display text-xs font-semibold text-on-brand shadow-1 transition-colors hover:bg-brand-hover">
            Birincil
          </button>
          <button className="h-ctrl rounded-sm border border-line bg-surface px-3.5 text-xs font-medium text-ink-2 transition-colors hover:bg-surface-2 hover:text-ink">
            İkincil
          </button>
          <button className="h-ctrl rounded-sm bg-ok px-3.5 font-display text-xs font-semibold text-on-ok">
            Onay
          </button>
          <button className="h-ctrl rounded-sm border border-danger-soft bg-danger-soft px-3.5 text-xs font-semibold text-danger">
            Yıkıcı
          </button>
          <button className="h-ctrl rounded-sm px-3.5 text-xs font-medium text-brand transition-colors hover:bg-brand-soft">
            Sade
          </button>
          <button disabled className="h-ctrl cursor-not-allowed rounded-sm border border-line bg-sunken px-3.5 text-xs font-medium text-ink-3 opacity-70">
            Pasif
          </button>
          <button className="grid h-ctrl w-ctrl place-items-center rounded-sm border border-line bg-surface-2 text-ink-2 transition-colors hover:bg-surface">
            <Plus size={15} strokeWidth={2} />
          </button>
        </div>
      </Bolum>

      <Bolum baslik="Durum çipleri" not="§7.3 — renk yalnızca durum anlatır">
        <div className="flex flex-wrap gap-2 rounded-md border border-line bg-surface p-4">
          <Cip renk="warn" ikon={<Hourglass size={11} />}>Beklemede</Cip>
          <Cip renk="ok" ikon={<CircleCheck size={11} />}>Onaylandı</Cip>
          <Cip renk="info" ikon={<CalendarClock size={11} />}>Devam Ediyor</Cip>
          <Cip renk="danger" ikon={<CircleX size={11} />}>Reddedildi</Cip>
          <Cip renk="mute" ikon={<Ban size={11} />}>İptal Edildi</Cip>
          <Cip renk="slate" ikon={<CheckCheck size={11} />}>Tamamlandı</Cip>
        </div>
      </Bolum>

      <Bolum baslik="Form alanı" not="§7.2">
        <div className="grid gap-3 rounded-md border border-line bg-surface p-4 sm:grid-cols-2">
          <label className="block">
            <span className="mb-1.5 block text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">
              Ad Soyad
            </span>
            <input
              defaultValue="Ayşe Demir"
              className="h-field w-full rounded-md border border-line bg-surface-2 px-3.5 text-base text-ink outline-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand"
            />
          </label>
          <label className="block">
            <span className="mb-1.5 block text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">
              Telefon
            </span>
            <div className="relative">
              <input
                defaultValue="053"
                className="h-field w-full rounded-md border border-danger bg-danger-soft px-3.5 pr-9 text-base text-danger outline-none"
              />
              <CircleAlert
                size={16}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-danger"
              />
            </div>
            <span className="mt-1 block text-2xs text-danger">Telefon 11 hane olmalı.</span>
          </label>
        </div>
      </Bolum>

      <Bolum baslik="Liste satırı (mobil temel)" not="§5.2 — kademeli giriş, saç teli ayırıcı">
        <InsetGroup>
          {[
            { d: 'Beklemede', r: 'var(--warn)', k: 'Kaldırım onarımı talebi', a: 'Mustafa Taş' },
            { d: 'Onaylandı', r: 'var(--ok)', k: 'Muhtarlık işbirliği toplantısı', a: 'Fatma Erdem' },
            { d: 'Reddedildi', r: 'var(--danger)', k: 'İş başvurusu', a: 'Mehmet Kaya' },
          ].map((s, i, dizi) => (
            <ListRow
              key={s.k}
              sira={i}
              sonuncu={i === dizi.length - 1}
              ikon={<Inbox size={15} />}
              ikonRengi={s.r}
              ust={
                <>
                  <span className="font-bold uppercase tracking-[0.05em]" style={{ color: s.r }}>
                    {s.d}
                  </span>
                  <span className="h-[3px] w-[3px] rounded-full bg-line-2" />
                  <span>14 Ağu</span>
                </>
              }
              baslik={s.k}
              alt={<><User size={12} /> {s.a}</>}
              onClick={() => {}}
            />
          ))}
        </InsetGroup>
      </Bolum>

      <Bolum baslik="Boş durum ve iskelet" not="§7.8">
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="grid place-items-center rounded-md border border-line bg-surface p-6 text-center">
            <div className="grid h-12 w-12 place-items-center rounded-lg bg-sunken text-ink-3">
              <Calendar size={22} strokeWidth={1.8} />
            </div>
            <p className="mt-3 font-display text-base font-bold text-ink">Kayıt yok</p>
            <p className="mt-1 max-w-[300px] text-2xs text-ink-3">
              Bu aralıkta etkinlik bulunamadı. Tarih aralığını genişletmeyi deneyin.
            </p>
            <button className="mt-3 h-ctrl rounded-sm bg-brand px-3.5 font-display text-xs font-semibold text-on-brand">
              Yeni etkinlik
            </button>
          </div>
          <div className="space-y-2 rounded-md border border-line bg-surface p-4">
            {[0, 1, 2].map((i) => (
              <div key={i} className="flex items-center gap-3">
                <div className="anim-nabiz h-8 w-8 rounded-sm bg-sunken" style={{ animationDelay: `${i * 0.15}s` }} />
                <div className="flex-1 space-y-1.5">
                  <div className="anim-nabiz h-3 w-3/4 rounded-xs bg-sunken" style={{ animationDelay: `${i * 0.15}s` }} />
                  <div className="anim-nabiz h-2.5 w-1/2 rounded-xs bg-sunken" style={{ animationDelay: `${i * 0.15 + 0.05}s` }} />
                </div>
              </div>
            ))}
          </div>
        </div>
      </Bolum>

      <Bolum baslik="Yarıçap, gölge, boşluk" not="Knob'ların doğrudan çıktısı">
        <div className="grid gap-3 sm:grid-cols-3">
          <div className="rounded-md border border-line bg-surface p-4">
            <p className="mb-2 text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">Yarıçap</p>
            <div className="flex flex-wrap items-end gap-2">
              {(['xs', 'sm', 'md', 'lg', 'xl', '2xl'] as const).map((r) => (
                <div key={r} className="text-center">
                  <div className={`h-10 w-10 border border-line-2 bg-sunken rounded-${r}`} />
                  <code className="mt-1 block text-3xs text-ink-3">{r}</code>
                </div>
              ))}
            </div>
          </div>
          <div className="rounded-md border border-line bg-surface p-4">
            <p className="mb-2 text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">Gölge</p>
            <div className="flex items-end gap-3">
              {([1, 2, 3] as const).map((g) => (
                <div key={g} className="text-center">
                  <div className={`h-10 w-10 rounded-md bg-surface shadow-${g}`} />
                  <code className="mt-1 block text-3xs text-ink-3">sh-{g}</code>
                </div>
              ))}
            </div>
          </div>
          <div className="rounded-md border border-line bg-surface p-4">
            <p className="mb-2 text-2xs font-semibold uppercase tracking-[0.08em] text-ink-3">Boşluk</p>
            <div className="space-y-1">
              {(['1', '2', '3', '4', '6', '8'] as const).map((sp) => (
                <div key={sp} className="flex items-center gap-2">
                  <code className="w-6 text-3xs text-ink-3">{sp}</code>
                  <div className="h-2 rounded-xs bg-brand-soft" style={{ width: `calc(var(--sp) * ${sp} * 3)` }} />
                </div>
              ))}
            </div>
          </div>
        </div>
      </Bolum>
    </div>
  );
}

function Bolum({ baslik, not, children }: { baslik: string; not?: string; children: ReactNode }) {
  return (
    <section className="space-y-2">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h3 className="font-display text-base font-bold tracking-[var(--track-d)] text-ink">
          {baslik}
        </h3>
        {not && <span className="text-2xs text-ink-3">{not}</span>}
      </div>
      {children}
    </section>
  );
}

function Cip({
  renk,
  ikon,
  children,
}: {
  renk: 'warn' | 'ok' | 'info' | 'danger' | 'mute' | 'slate';
  ikon: ReactNode;
  children: ReactNode;
}) {
  return (
    <span
      className="inline-flex h-6 items-center gap-1.5 rounded-sm px-2 text-2xs font-semibold"
      style={{ background: `var(--${renk}-soft)`, color: `var(--${renk})` }}
    >
      <span className="h-[5px] w-[5px] rounded-full bg-current" />
      {ikon}
      {children}
    </span>
  );
}
