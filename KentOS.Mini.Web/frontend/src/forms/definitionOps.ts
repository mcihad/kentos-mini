import type { FormDefinition, FormField, FormGroup, FormStep } from '../data/types';
import { FIELD_TYPE, fieldTypeInfo, isBlock } from './fieldTypes';

/**
 * TANIM AĞACI ÜZERİNDEKİ İŞLEMLER — saf fonksiyonlar.
 *
 * <p>
 * Tasarımcı bileşeni yalnızca "ne oldu"yu biliyor, "nasıl olacağını" bu
 * dosya. Ayrılmasının sebebi test edilebilirlik: ağaç işlemleri (taşı,
 * kopyala, sil, yeniden sırala) React'siz doğrulanabiliyor ve bunlar
 * modülün en kolay bozulan yeri.
 * </p>
 *
 * <p>
 * <b>Hepsi YENİ NESNE döndürür.</b> Yerinde değiştirme, React'in
 * değişikliği görmemesine ve ekranın sessizce eski kalmasına yol açardı.
 * </p>
 */

let sayac = 0;

/**
 * Alan kimliği — KALICI.
 *
 * <p>
 * Etiket değişince kimlik değişmez; değişseydi yayınlanmış bir formda
 * eski yanıtlar sahipsiz kalırdı. Rastgele parça + sayaç: aynı milisaniyede
 * iki alan eklemek çakışma üretmemeli.
 * </p>
 */
export function yeniKimlik(onek = 'a'): string {
  sayac += 1;
  const r = Math.random().toString(36).slice(2, 8);
  return `${onek}_${r}${sayac.toString(36)}`;
}

export function bosTanim(): FormDefinition {
  return {
    semaSurumu: 1,
    ayarlar: { kolonSayisi: 1, ilerlemeCubugu: true, numaralandir: false, kaydetDevamEt: false },
    adimlar: [bosAdim('Form')],
  };
}

export function bosAdim(baslik?: string): FormStep {
  return { kimlik: yeniKimlik('s'), baslik: baslik ?? null, gruplar: [bosGrup()] };
}

export function bosGrup(): FormGroup {
  return { kimlik: yeniKimlik('g'), baslik: null, kolonSayisi: null, alanlar: [] };
}

/** Palette tıklanınca eklenen varsayılan alan. */
export function yeniAlan(tip: number, kolonSayisi: number): FormField {
  // Enum daraltması: katalogdaki sayı zaten geçerli bir üye.
  const t = tip as NonNullable<FormField['tip']>;
  const bilgi = fieldTypeInfo(tip);

  const alan: FormField = {
    kimlik: yeniKimlik(),
    tip: t,
    etiket: bilgi.blok ? bilgi.ad : `${bilgi.ad} sorusu`,
    zorunlu: false,
    // Varsayılan genişlik grubun kolon sayısından: 2 kolonlu bir grupta
    // yeni alan yarım satır kaplasın, kullanıcı her seferinde ayarlamasın.
    genislik: Math.max(1, Math.round(12 / Math.max(1, kolonSayisi))),
  };

  if (bilgi.secenekli) {
    alan.secenekler = [
      { kimlik: yeniKimlik('o'), etiket: 'Seçenek 1' },
      { kimlik: yeniKimlik('o'), etiket: 'Seçenek 2' },
    ];
  }

  if (bilgi.matris) {
    alan.satirlar = [
      { kimlik: yeniKimlik('r'), etiket: 'Satır 1' },
      { kimlik: yeniKimlik('r'), etiket: 'Satır 2' },
    ];
    alan.sutunlar = [
      { kimlik: yeniKimlik('c'), etiket: 'İyi' },
      { kimlik: yeniKimlik('c'), etiket: 'Orta' },
      { kimlik: yeniKimlik('c'), etiket: 'Kötü' },
    ];
  }

  if (tip === FIELD_TYPE.olcek) alan.ayarlar = { enAz: 1, enCok: 5 };
  if (tip === FIELD_TYPE.nps) alan.ayarlar = { enAz: 0, enCok: 10 };
  if (tip === FIELD_TYPE.yildiz) alan.ayarlar = { enAz: 1, enCok: 5 };
  if (tip === FIELD_TYPE.uzunMetin) alan.ayarlar = { satir: 4 };

  return alan;
}

/** Alanın bulunduğu adım ve grup. */
export function alanKonumu(tanim: FormDefinition, kimlik: string) {
  const adimlar = tanim.adimlar ?? [];

  for (let a = 0; a < adimlar.length; a++) {
    const gruplar = adimlar[a].gruplar ?? [];

    for (let g = 0; g < gruplar.length; g++) {
      const i = (gruplar[g].alanlar ?? []).findIndex((x) => x.kimlik === kimlik);
      if (i >= 0) return { adim: a, grup: g, indeks: i };
    }
  }

  return null;
}

export function alanBul(tanim: FormDefinition, kimlik: string): FormField | null {
  const k = alanKonumu(tanim, kimlik);
  if (!k) return null;
  return tanim.adimlar![k.adim].gruplar![k.grup].alanlar![k.indeks];
}

/** Tanımın derin kopyası — değiştirmeden önce. */
const kopya = (t: FormDefinition): FormDefinition => JSON.parse(JSON.stringify(t));

export function alanEkle(
  tanim: FormDefinition, adim: number, grup: number, alan: FormField, indeks?: number,
): FormDefinition {
  const y = kopya(tanim);
  const liste = (y.adimlar![adim].gruplar![grup].alanlar ??= []);
  liste.splice(indeks ?? liste.length, 0, alan);
  return y;
}

export function alanGuncelle(
  tanim: FormDefinition, kimlik: string, kismi: Partial<FormField>,
): FormDefinition {
  const k = alanKonumu(tanim, kimlik);
  if (!k) return tanim;

  const y = kopya(tanim);
  const mevcut = y.adimlar![k.adim].gruplar![k.grup].alanlar![k.indeks];
  y.adimlar![k.adim].gruplar![k.grup].alanlar![k.indeks] = { ...mevcut, ...kismi };
  return y;
}

export function alanSil(tanim: FormDefinition, kimlik: string): FormDefinition {
  const k = alanKonumu(tanim, kimlik);
  if (!k) return tanim;

  const y = kopya(tanim);
  y.adimlar![k.adim].gruplar![k.grup].alanlar!.splice(k.indeks, 1);

  /*
    SİLİNEN ALANA BAKAN KOŞULLAR DA TEMİZLENİR.

    Bırakılsaydı sunucu "koşul var olmayan bir alanı gösteriyor" diye
    kaydetmeyi reddederdi ve kullanıcı, sildiği alanla kaydedemediği form
    arasındaki bağı kuramazdı.
  */
  return kosullariTemizle(y, kimlik);
}

export function alanKopyala(tanim: FormDefinition, kimlik: string): FormDefinition {
  const k = alanKonumu(tanim, kimlik);
  if (!k) return tanim;

  const kaynak = tanim.adimlar![k.adim].gruplar![k.grup].alanlar![k.indeks];

  // Kimlikler YENİDEN üretilir: kopya, kaynağın yanıtlarını paylaşamaz.
  const yeni: FormField = {
    ...JSON.parse(JSON.stringify(kaynak)),
    kimlik: yeniKimlik(),
    etiket: `${kaynak.etiket} (kopya)`,
    secenekler: kaynak.secenekler?.map((s) => ({ ...s, kimlik: yeniKimlik('o') })),
    satirlar: kaynak.satirlar?.map((s) => ({ ...s, kimlik: yeniKimlik('r') })),
    sutunlar: kaynak.sutunlar?.map((s) => ({ ...s, kimlik: yeniKimlik('c') })),

    // KOŞUL KOPYALANMAZ: kopya kaynağın hemen ardında duruyor ve koşulu
    // geriye referans kuralını bozmasa bile büyük olasılıkla yanlış olur.
    kosul: undefined,
  };

  return alanEkle(tanim, k.adim, k.grup, yeni, k.indeks + 1);
}

/** Alanı başka bir gruba / sıraya taşır. */
export function alanTasi(
  tanim: FormDefinition, kimlik: string,
  hedef: { adim: number; grup: number; indeks: number },
): FormDefinition {
  const k = alanKonumu(tanim, kimlik);
  if (!k) return tanim;

  const y = kopya(tanim);
  const [alan] = y.adimlar![k.adim].gruplar![k.grup].alanlar!.splice(k.indeks, 1);

  const hedefListe = (y.adimlar![hedef.adim].gruplar![hedef.grup].alanlar ??= []);
  const duzeltilmis = Math.min(Math.max(0, hedef.indeks), hedefListe.length);
  hedefListe.splice(duzeltilmis, 0, alan);

  return y;
}

/**
 * Koşulu GERİYE bakmayan alanların koşulunu düşürür.
 *
 * <p>
 * Sunucu geriye referansı zorluyor; taşıma sonrası bir koşul ileriye
 * bakar hâle gelebiliyor. Kaydetme anında hata vermek yerine taşıma
 * anında düzeltmek, kullanıcının neyi neden kaybettiğini görmesini
 * sağlıyor (arayüz bunu bir bildirimle söylüyor).
 * </p>
 */
export function ileriKosullariDusur(tanim: FormDefinition): {
  tanim: FormDefinition; dusen: string[];
} {
  const y = kopya(tanim);
  const sira = new Map(tumAlanlarSirali(y).map((a, i) => [a.kimlik ?? '', i]));
  const dusen: string[] = [];

  tumAlanlarSirali(y).forEach((alan, i) => {
    const kurallar = alan.kosul?.kurallar ?? [];
    if (kurallar.length === 0) return;

    const bozuk = kurallar.some((k) => (sira.get(k.alanKimligi ?? '') ?? Infinity) >= i);
    if (bozuk) {
      alan.kosul = undefined;
      dusen.push(alan.etiket ?? '');
    }
  });

  return { tanim: y, dusen };
}

export function kosullariTemizle(tanim: FormDefinition, silinenKimlik: string): FormDefinition {
  const y = kopya(tanim);

  const temizle = (k: { kurallar?: { alanKimligi?: string | null }[] | null } | null | undefined) => {
    if (!k?.kurallar) return k ?? null;
    const kalan = k.kurallar.filter((x) => x.alanKimligi !== silinenKimlik);
    return kalan.length > 0 ? { ...k, kurallar: kalan } : undefined;
  };

  for (const adim of y.adimlar ?? []) {
    for (const grup of adim.gruplar ?? []) {
      grup.kosul = temizle(grup.kosul) as FormGroup['kosul'];
      for (const alan of grup.alanlar ?? []) {
        alan.kosul = temizle(alan.kosul) as FormField['kosul'];
      }
    }
  }

  return y;
}

export function tumAlanlarSirali(tanim: FormDefinition): FormField[] {
  return (tanim.adimlar ?? []).flatMap((a) => (a.gruplar ?? []).flatMap((g) => g.alanlar ?? []));
}

/** Koşulda seçilebilecek alanlar: SADECE daha önce gelenler. */
export function kosulAdaylari(tanim: FormDefinition, kimlik: string): FormField[] {
  const hepsi = tumAlanlarSirali(tanim);
  const i = hepsi.findIndex((a) => a.kimlik === kimlik);
  if (i < 0) return [];

  return hepsi.slice(0, i).filter((a) => !isBlock(a.tip));
}

export function grupEkle(tanim: FormDefinition, adim: number): FormDefinition {
  const y = kopya(tanim);
  (y.adimlar![adim].gruplar ??= []).push(bosGrup());
  return y;
}

export function grupGuncelle(
  tanim: FormDefinition, adim: number, grup: number, kismi: Partial<FormGroup>,
): FormDefinition {
  const y = kopya(tanim);
  y.adimlar![adim].gruplar![grup] = { ...y.adimlar![adim].gruplar![grup], ...kismi };
  return y;
}

export function grupSil(tanim: FormDefinition, adim: number, grup: number): FormDefinition {
  const y = kopya(tanim);
  const silinen = y.adimlar![adim].gruplar![grup];
  y.adimlar![adim].gruplar!.splice(grup, 1);

  // Grubun alanlarına bakan koşullar da temizlenir.
  return (silinen.alanlar ?? []).reduce(
    (t, a) => kosullariTemizle(t, a.kimlik ?? ''), y);
}

export function adimEkle(tanim: FormDefinition): FormDefinition {
  const y = kopya(tanim);
  (y.adimlar ??= []).push(bosAdim(`Adım ${(y.adimlar?.length ?? 0) + 1}`));
  return y;
}

export function adimSil(tanim: FormDefinition, adim: number): FormDefinition {
  const y = kopya(tanim);
  const silinen = y.adimlar![adim];
  y.adimlar!.splice(adim, 1);

  const alanlar = (silinen.gruplar ?? []).flatMap((g) => g.alanlar ?? []);
  return alanlar.reduce((t, a) => kosullariTemizle(t, a.kimlik ?? ''), y);
}

export function adimGuncelle(
  tanim: FormDefinition, adim: number, kismi: Partial<FormStep>,
): FormDefinition {
  const y = kopya(tanim);
  y.adimlar![adim] = { ...y.adimlar![adim], ...kismi };
  return y;
}
