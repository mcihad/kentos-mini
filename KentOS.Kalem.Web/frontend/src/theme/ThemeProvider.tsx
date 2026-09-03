import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { FONTS, BRAND_COLORS, NEUTRAL_COLORS, ACCENT_COLORS, KURUM_TEMA_OLAYI } from './palettes';
import { PRESETS, DEFAULT_KNOBS, type ThemeKnobs, type PresetKey } from './presets';

/** Kullanıcının SEÇİMİ. `sistem` = işletim sistemi tercihine uy. */
export type ThemeMode = 'acik' | 'koyu' | 'sistem';

/** Ekranda GERÇEKTEN uygulanan tema. */
export type ActiveTheme = 'acik' | 'koyu';

const ANAHTAR = 'sv-tema';
const KNOB_ANAHTARI = 'sv-tema-knob';
/** Ön-boyama betiğinin okuduğu çözülmüş çekirdek token'lar. */
const CEKIRDEK_ANAHTARI = 'sv-tema-cekirdek';

type TemaBaglami = {
  tema: ThemeMode;
  etkinTema: ActiveTheme;
  sistemTercihi: ActiveTheme;
  temaDegistir: () => void;
  temaAyarla: (t: ThemeMode) => void;

  /* ── Tema Tasarımcısı ── */
  knob: ThemeKnobs;
  preset: PresetKey;
  /** Tek knob değiştirir; preset "Özel tema"ya döner. */
  knobAyarla: <K extends keyof ThemeKnobs>(ad: K, deger: ThemeKnobs[K]) => void;
  presetSec: (p: Exclude<PresetKey, 'ozel'>) => void;
  sifirla: () => void;
  /** `globals.css`'e yapıştırılabilir `:root{}` bloğu. */
  tokenCiktisi: () => string;
};

const Baglam = createContext<TemaBaglami | null>(null);

function kayitliTema(): ThemeMode {
  const k = localStorage.getItem(ANAHTAR);
  return k === 'acik' || k === 'koyu' || k === 'sistem' ? k : 'sistem';
}

function sistemOku(): ActiveTheme {
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'koyu' : 'acik';
}

function kayitliKnob(): { knob: ThemeKnobs; preset: PresetKey } {
  try {
    const ham = localStorage.getItem(KNOB_ANAHTARI);
    if (ham) {
      const o = JSON.parse(ham) as { knob: ThemeKnobs; preset: PresetKey };
      if (o?.knob) return { knob: { ...DEFAULT_KNOBS, ...o.knob }, preset: o.preset ?? 'ozel' };
    }
  } catch {
    /* bozuk kayıt varsayılana düşer — tema yüzünden uygulama açılmamazlık etmemeli */
  }
  return { knob: DEFAULT_KNOBS, preset: 'kurumsal-acik' };
}

/**
 * Temayı `<html>` üzerinden uygular — design_new §3.
 *
 * İKİ AYRI KAVRAM burada birleşiyor:
 *
 *  • **Mod** (gündüz/gece/sistem) kullanıcının kişisel tercihidir ve
 *    `data-tema` + `data-mod` nitelikleriyle uygulanır.
 *  • **Knob'lar** (renk, yoğunluk, yarıçap, punto…) kurumun kimliğidir ve
 *    satır içi `style` ile 14 çekirdek token'a yazılır.
 *
 * Panel yalnızca çekirdek token'ları yazar; anlamsal katman ve bileşenler
 * hiçbir şey bilmez. `color-mix`/`calc` zinciri tek karede günceller —
 * tema değişimi yeniden render GEREKTİRMEZ. Bu yüzden aşağıdaki etki
 * yalnızca `documentElement.style`'a dokunuyor, React ağacına değil.
 */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [tema, setTema] = useState<ThemeMode>(kayitliTema);
  const [sistemTercihi, setSistemTercihi] = useState<ActiveTheme>(sistemOku);
  const [{ knob, preset }, setKnobDurumu] = useState(kayitliKnob);

  /*
    KURUM RENKLERİ SONRADAN GELEBİLİR.

    Palet 0. sırası kurumun kendi rengiyle değiştiriliyor (`palettes.ts`), ama
    kurum bilgisi ağdan geliyor ve ilk boyamadan sonra tazelenebiliyor. Bu
    sayaç yalnızca aşağıdaki token efektini yeniden tetiklemek için var;
    değerin kendisi kullanılmıyor.
  */
  const [kurumSurumu, setKurumSurumu] = useState(0);

  useEffect(() => {
    const tazele = () => setKurumSurumu((s) => s + 1);
    window.addEventListener(KURUM_TEMA_OLAYI, tazele);
    return () => window.removeEventListener(KURUM_TEMA_OLAYI, tazele);
  }, []);

  useEffect(() => {
    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const dinle = () => setSistemTercihi(mq.matches ? 'koyu' : 'acik');
    mq.addEventListener('change', dinle);
    return () => mq.removeEventListener('change', dinle);
  }, []);

  const etkinTema: ActiveTheme = tema === 'sistem' ? sistemTercihi : tema;

  useEffect(() => {
    // İKİ NİTELİK birden: `data-tema` uygulamanın var olan anahtarı (40+ ekran
    // ve testler onu bekliyor), `data-mod` token motorunun şartnamedeki adı.
    document.documentElement.setAttribute('data-tema', etkinTema);
    document.documentElement.setAttribute('data-mod', etkinTema);
    // Tarayıcının kendi arayüzü (kaydırma çubuğu, form denetimleri) de dönsün;
    // PWA olarak kurulduğunda bu fark açıkça görünüyor.
    document.documentElement.style.colorScheme = etkinTema === 'koyu' ? 'dark' : 'light';
    localStorage.setItem(ANAHTAR, tema);
  }, [tema, etkinTema]);

  // ── 14 çekirdek knob → satır içi CSS değişkeni
  useEffect(() => {
    const k = document.documentElement;
    const m = BRAND_COLORS[knob.marka] ?? BRAND_COLORS[0];
    const v = ACCENT_COLORS[knob.vurgu] ?? ACCENT_COLORS[0];
    const n = NEUTRAL_COLORS[knob.notr] ?? NEUTRAL_COLORS[0];
    const f = FONTS[knob.font] ?? FONTS[0];

    const deger: Record<string, string> = {
      '--brand': m.acik,
      '--brand-dk': m.koyu,
      '--accent': v.acik,
      '--accent-dk': v.koyu,
      '--neutral': n.deger,
      '--r': `${knob.r}px`,
      '--sp': `${knob.sp}px`,
      '--fs': `${knob.fs}px`,
      '--fs-d': `${knob.fsd}`,
      '--track': `${knob.track}em`,
      '--bw': `${knob.bw}px`,
      '--sh-a': `${knob.sha / 100}`,
      '--dur': `${knob.dur}ms`,
      '--font-d': `'${f.baslik}'`,
      '--font-t': `'${f.govde}'`,
    };
    Object.entries(deger).forEach(([ad, d]) => k.style.setProperty(ad, d));

    localStorage.setItem(KNOB_ANAHTARI, JSON.stringify({ knob, preset }));

    /*
      ÇÖZÜLMÜŞ ÇEKİRDEK DEĞERLER AYRICA SAKLANIR.

      `index.html` içindeki ön-boyama betiği bunları okuyup ilk kareden önce
      uyguluyor. Aksi hâlde açılış perdesi kurumsal laciverti gösteriyor,
      React bağlanınca kullanıcının seçtiği renge (ve gece moduna) ZIPLIYORDU
      — kurulu bir PWA'da bu sıçrama en çok göze batan yer, çünkü uygulama
      her açılışta oradan başlıyor.

      Paleti HTML'e kopyalamak yerine ÇÖZÜLMÜŞ değerleri saklıyoruz: palet
      dizileri tek yerde (`paletler.ts`) kalıyor ve kullanıcı temayı
      değiştirdiğinde kayıt kendiliğinden tazeleniyor.
    */
    localStorage.setItem(CEKIRDEK_ANAHTARI, JSON.stringify(deger));
  }, [knob, preset, kurumSurumu]);

  /**
   * İŞLETİM SİSTEMİ ÇUBUĞU da temayla dönsün.
   *
   * `<meta name="theme-color">` iki sabit değerle yazılıydı; kullanıcı Bordo
   * ya da Zümrüt seçtiğinde telefonun durum çubuğu lacivert kalıyordu.
   * Kurulu PWA'da bu fark en görünür yerde: uygulamanın kendi rengi ile
   * üstündeki şerit uyuşmuyor ve "web sayfası" hissi oradan sızıyor.
   *
   * Değer hesaplanmış token'dan okunuyor, palet dizisinden değil: gece
   * modunda çubuk `--nav-bg`, gündüzde `--brand` — anlamsal katman zaten
   * hangisinin doğru olduğunu biliyor.
   */
  useEffect(() => {
    const kok = getComputedStyle(document.documentElement);
    const renk = kok.getPropertyValue(etkinTema === 'koyu' ? '--nav-bg' : '--brand').trim();
    if (!renk) return;

    // Medya sorgulu iki etiket duruyor; ikisini de aynı değere çekmek yerine
    // sorgusuz TEK etiket bırakılıyor — mod zaten burada biliniyor.
    document
      .querySelectorAll('meta[name="theme-color"]')
      .forEach((m) => m.parentElement?.removeChild(m));

    const meta = document.createElement('meta');
    meta.name = 'theme-color';
    meta.content = renk;
    document.head.appendChild(meta);
  }, [knob, etkinTema]);

  const temaDegistir = useCallback(() => {
    setTema(etkinTema === 'acik' ? 'koyu' : 'acik');
  }, [etkinTema]);

  const knobAyarla = useCallback(
    <K extends keyof ThemeKnobs>(ad: K, d: ThemeKnobs[K]) => {
      setKnobDurumu((s) => {
        // Mod knob'u kullanıcının kişisel tercihiyle aynı şey; ikisini ayrı
        // tutmak "panelde gece seçtim ama uygulama gündüz" durumunu üretirdi.
        if (ad === 'mod') setTema(d as ActiveTheme);
        return { knob: { ...s.knob, [ad]: d }, preset: 'ozel' };
      });
    },
    [],
  );

  const presetSec = useCallback((p: Exclude<PresetKey, 'ozel'>) => {
    const { ad: _ad, ...k } = PRESETS[p];
    void _ad;
    setTema(k.mod);
    setKnobDurumu({ knob: k, preset: p });
  }, []);

  const sifirla = useCallback(() => presetSec('kurumsal-acik'), [presetSec]);

  const tokenCiktisi = useCallback(() => {
    const m = BRAND_COLORS[knob.marka] ?? BRAND_COLORS[0];
    const v = ACCENT_COLORS[knob.vurgu] ?? ACCENT_COLORS[0];
    const n = NEUTRAL_COLORS[knob.notr] ?? NEUTRAL_COLORS[0];
    const f = FONTS[knob.font] ?? FONTS[0];
    return [
      ':root{',
      `  --brand:${m.acik};        --brand-dk:${m.koyu};`,
      `  --accent:${v.acik};       --accent-dk:${v.koyu};`,
      `  --neutral:${n.deger};`,
      `  --sp:${knob.sp}px;  --r:${knob.r}px;  --fs:${knob.fs}px;  --fs-d:${knob.fsd};`,
      `  --track:${knob.track}em;  --bw:${knob.bw}px;  --sh-a:${knob.sha / 100};  --dur:${knob.dur}ms;`,
      `  --font-d:'${f.baslik}';  --font-t:'${f.govde}';`,
      '}',
    ].join('\n');
  }, [knob]);

  const deger = useMemo(
    () => ({
      tema, etkinTema, sistemTercihi, temaDegistir, temaAyarla: setTema,
      knob, preset, knobAyarla, presetSec, sifirla, tokenCiktisi,
    }),
    [tema, etkinTema, sistemTercihi, temaDegistir, knob, preset, knobAyarla, presetSec, sifirla, tokenCiktisi],
  );

  return <Baglam.Provider value={deger}>{children}</Baglam.Provider>;
}

export function useTheme() {
  const b = useContext(Baglam);
  if (!b) throw new Error('useTema, TemaSaglayici içinde kullanılmalı.');
  return b;
}
