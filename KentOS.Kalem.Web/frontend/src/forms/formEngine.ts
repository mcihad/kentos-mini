import type { FormDefinition, FormField, FormGroup } from '../data/types';
import { CONDITION_OP, FIELD_TYPE, isBlock } from './fieldTypes';

/**
 * FORM MOTORU — koşul, görünürlük ve istemci doğrulaması.
 *
 * <p>
 * <b>Sunucudaki <c>FormDogrulayici</c>'nin aynası.</b> İkisi aynı tanımı
 * okuyor ve aynı kuralları uyguluyor; fark, buradakinin bir KOLAYLIK
 * olması. Karar sunucuda veriliyor — bu dosya yalnızca kullanıcıya
 * göndermeden önce söylüyor.
 * </p>
 *
 * <p>
 * <b>Cevaplar SARMALAYICI ile taşınır:</b> <c>{ deger, metin }</c>.
 * "Diğer" seçeneğinin yanındaki serbest metin ancak böyle sığıyor ve
 * sunucu da bu şekli bekliyor.
 * </p>
 */

export type Answer = { deger?: unknown; metin?: string };
export type Answers = Record<string, Answer>;

export const deger = (a: Answer | undefined) => a?.deger;
export const sarmala = (d: unknown, metin?: string): Answer =>
  metin ? { deger: d, metin } : { deger: d };

/** Tanımdaki bütün alanlar, sırayla. */
export function tumAlanlar(tanim: FormDefinition | undefined): FormField[] {
  return (tanim?.adimlar ?? []).flatMap((a) => (a.gruplar ?? []).flatMap((g) => g.alanlar ?? []));
}

const bos = (d: unknown): boolean =>
  d === null || d === undefined
  || (typeof d === 'string' && d.trim() === '')
  || (Array.isArray(d) && d.length === 0)
  || (typeof d === 'object' && !Array.isArray(d) && Object.keys(d as object).length === 0);

const metin = (d: unknown): string => {
  if (d === null || d === undefined) return '';
  if (typeof d === 'boolean') return d ? 'true' : 'false';
  return String(d);
};

const sayi = (d: unknown): number | null => {
  const n = typeof d === 'number' ? d : Number(String(d ?? '').replace(',', '.'));
  return Number.isFinite(n) ? n : null;
};

/** Tek kuralın değerlendirilmesi. */
function kuralSaglandi(
  kural: { alanKimligi?: string | null; operator?: number; deger?: string | null },
  cevaplar: Answers,
): boolean {
  const d = deger(cevaplar[kural.alanKimligi ?? '']);
  const h = kural.deger ?? '';

  switch (kural.operator) {
    case CONDITION_OP.dolu: return !bos(d);
    case CONDITION_OP.bos: return bos(d);
    case CONDITION_OP.esit: return metin(d) === h;
    case CONDITION_OP.esitDegil: return metin(d) !== h;
    case CONDITION_OP.icerir:
      return Array.isArray(d) ? d.includes(h) : metin(d).toLowerCase().includes(h.toLowerCase());
    case CONDITION_OP.icermez:
      return !(Array.isArray(d) ? d.includes(h) : metin(d).toLowerCase().includes(h.toLowerCase()));
    case CONDITION_OP.buyuk: {
      const a = sayi(d); const b = sayi(h);
      return a !== null && b !== null && a > b;
    }
    case CONDITION_OP.kucuk: {
      const a = sayi(d); const b = sayi(h);
      return a !== null && b !== null && a < b;
    }
    default: return true;
  }
}

/**
 * Bağlaçlı koşul.
 *
 * Boş kural listesi <b>koşulsuz</b> demektir: tasarımcıda "koşul ekle"
 * deyip hiçbir kural yazmayan kullanıcı alanı kaybetmemeli.
 */
export function kosulSaglandi(
  kosul: { baglac?: number; kurallar?: { alanKimligi?: string | null; operator?: number; deger?: string | null }[] | null } | null | undefined,
  cevaplar: Answers,
): boolean {
  const kurallar = kosul?.kurallar ?? [];
  if (kurallar.length === 0) return true;

  return kosul?.baglac === 1
    ? kurallar.some((k) => kuralSaglandi(k, cevaplar))
    : kurallar.every((k) => kuralSaglandi(k, cevaplar));
}

export const grupGorunur = (grup: FormGroup, cevaplar: Answers) =>
  kosulSaglandi(grup.kosul, cevaplar);

export const alanGorunur = (alan: FormField, grup: FormGroup, cevaplar: Answers) =>
  grupGorunur(grup, cevaplar) && kosulSaglandi(alan.kosul, cevaplar);

/**
 * Tek alanın doğrulaması — hata metni ya da <c>null</c>.
 *
 * <p>
 * Sunucudaki kurallarla aynı, ama <b>karar sunucunun</b>. Burada eksik
 * kalan bir kural gönderimde yakalanır; burada fazladan bir kural ise
 * kullanıcıyı gönderemez hâle getirir — bu yüzden gevşek tarafta durmak
 * bilinçli.
 * </p>
 */
export function alaniDogrula(alan: FormField, cevap: Answer | undefined): string | null {
  const d = deger(cevap);
  const k = alan.dogrulama;

  if (bos(d)) return alan.zorunlu ? 'Bu alan zorunlu.' : null;

  switch (alan.tip) {
    case FIELD_TYPE.kisaMetin:
    case FIELD_TYPE.uzunMetin: {
      const m = metin(d);
      if (k?.enAzUzunluk && m.length < k.enAzUzunluk) return `En az ${k.enAzUzunluk} karakter girin.`;
      if (k?.enCokUzunluk && m.length > k.enCokUzunluk) return `En çok ${k.enCokUzunluk} karakter girebilirsiniz.`;
      if (k?.desen) {
        try {
          if (!new RegExp(k.desen).test(m)) return k.desenMesaji ?? 'Girilen değer beklenen biçimde değil.';
        } catch { /* bozuk deseni sunucu yakalar */ }
      }
      return null;
    }

    case FIELD_TYPE.eposta: {
      const m = metin(d);
      return m.includes('@') && !m.startsWith('@') && !m.endsWith('@') && !m.includes(' ')
        ? null : 'Geçerli bir e-posta adresi girin.';
    }

    case FIELD_TYPE.telefon: {
      const r = metin(d).replace(/\D/g, '');
      return r.length >= 10 && r.length <= 13 ? null : 'Geçerli bir telefon numarası girin.';
    }

    case FIELD_TYPE.tcKimlik:
      return tcGecerli(metin(d).replace(/\D/g, '')) ? null : 'Geçerli bir T.C. kimlik numarası girin.';

    case FIELD_TYPE.sayi:
    case FIELD_TYPE.olcek:
    case FIELD_TYPE.nps:
    case FIELD_TYPE.yildiz: {
      const s = sayi(d);
      if (s === null) return 'Sayı girin.';
      if (k?.enAzDeger != null && s < k.enAzDeger) return `En az ${k.enAzDeger} olmalı.`;
      if (k?.enCokDeger != null && s > k.enCokDeger) return `En çok ${k.enCokDeger} olmalı.`;
      return null;
    }

    case FIELD_TYPE.cokSecim:
    case FIELD_TYPE.cokluAcilirListe: {
      const l = Array.isArray(d) ? d : [];
      if (k?.enAzSecim && l.length < k.enAzSecim) return `En az ${k.enAzSecim} seçim yapın.`;
      if (k?.enCokSecim && l.length > k.enCokSecim) return `En çok ${k.enCokSecim} seçim yapabilirsiniz.`;
      return null;
    }

    case FIELD_TYPE.matrisTekSecim:
    case FIELD_TYPE.matrisCokSecim: {
      if (!alan.zorunlu) return null;
      const m = (d ?? {}) as Record<string, unknown>;
      const eksik = (alan.satirlar ?? []).some((s) => bos(m[s.kimlik ?? '']));
      return eksik ? 'Tüm satırları işaretleyin.' : null;
    }

    default:
      return null;
  }
}

/** T.C. kimlik algoritması — sunucudakiyle aynı. */
export function tcGecerli(tc: string): boolean {
  if (tc.length !== 11 || tc[0] === '0') return false;
  const r = [...tc].map(Number);
  const tek = r[0] + r[2] + r[4] + r[6] + r[8];
  const cift = r[1] + r[3] + r[5] + r[7];
  let onuncu = (tek * 7 - cift) % 10;
  if (onuncu < 0) onuncu += 10;
  const onbir = r.slice(0, 10).reduce((a, b) => a + b, 0) % 10;
  return r[9] === onuncu && r[10] === onbir;
}

/** Bir adımın görünür ve doldurulması gereken alanları. */
export function adimHatalari(
  tanim: FormDefinition, adimIndeksi: number, cevaplar: Answers,
): Record<string, string> {
  const hatalar: Record<string, string> = {};
  const adim = (tanim.adimlar ?? [])[adimIndeksi];
  if (!adim) return hatalar;

  for (const grup of adim.gruplar ?? []) {
    if (!grupGorunur(grup, cevaplar)) continue;

    for (const alan of grup.alanlar ?? []) {
      if (isBlock(alan.tip)) continue;
      if (!alanGorunur(alan, grup, cevaplar)) continue;

      const h = alaniDogrula(alan, cevaplar[alan.kimlik ?? '']);
      if (h) hatalar[alan.kimlik ?? ''] = h;
    }
  }

  return hatalar;
}

/** Formun tamamındaki hatalar. */
export function tumHatalar(tanim: FormDefinition, cevaplar: Answers): Record<string, string> {
  const hepsi: Record<string, string> = {};

  (tanim.adimlar ?? []).forEach((_, i) => {
    Object.assign(hepsi, adimHatalari(tanim, i, cevaplar));
  });

  return hepsi;
}

/**
 * Gönderilecek gövde — GÖRÜNMEYEN alanlar ÇIKARILIR.
 *
 * <p>
 * Koşulu sağlanmayan bir alan doldurulmuş olabilir: kullanıcı önce "Evet"
 * deyip detayı yazmış, sonra "Hayır"a dönmüş olabilir. O metni göndermek,
 * kaydın kendi mantığıyla çelişmesi demek.
 * </p>
 */
export function gonderilecek(tanim: FormDefinition, cevaplar: Answers): Answers {
  const govde: Answers = {};

  for (const grup of (tanim.adimlar ?? []).flatMap((a) => a.gruplar ?? [])) {
    if (!grupGorunur(grup, cevaplar)) continue;

    for (const alan of grup.alanlar ?? []) {
      if (isBlock(alan.tip)) continue;
      if (!alanGorunur(alan, grup, cevaplar)) continue;

      const c = cevaplar[alan.kimlik ?? ''];
      if (c && !bos(c.deger)) govde[alan.kimlik ?? ''] = c;
    }
  }

  return govde;
}

/**
 * Ham cevabı kullanıcının gördüğü metne çevirir.
 *
 * <p>
 * JSONB'de seçenek KİMLİĞİ duruyor (`o_3`, `r_1`); yanıt detayı bir dönem
 * onu olduğu gibi basıyor ve matris cevabı `r_1: c_2` diye okunuyordu.
 * Sunucudaki karşılığı `FormDegerMetni`; ikisi aynı kuralı uygular —
 * ayrışırlarsa aynı cevap ekranda ve Excel'de farklı görünür.
 * </p>
 */
export function etiketliDeger(alan: FormField, cevap: Answer | undefined): string {
  const d = deger(cevap);
  const serbest = cevap?.metin;
  if (d === null || d === undefined || d === '') return serbest || '—';

  // Seçenek ve sütun kimlikleri tek sözlükte: matriste iç değerler sütun
  // kimliği, seçim alanlarında seçenek kimliği — çözücü ikisini de bilmeli.
  const sozluk = new Map<string, string>();
  for (const s of [...(alan.secenekler ?? []), ...(alan.sutunlar ?? [])]) {
    if (s.kimlik) sozluk.set(s.kimlik, s.etiket ?? s.kimlik);
  }
  const cevir = (k: string) => sozluk.get(k) ?? k;

  const temel = (() => {
    if (typeof d === 'boolean') return d ? 'Evet' : 'Hayır';
    if (Array.isArray(d)) return d.map((x) => cevir(String(x))).join(', ');

    if (typeof d === 'object') {
      return Object.entries(d as Record<string, unknown>)
        .map(([satirKimligi, ic]) => {
          const satir = (alan.satirlar ?? []).find((s) => s.kimlik === satirKimligi);
          const icMetin = Array.isArray(ic)
            ? ic.map((x) => cevir(String(x))).join(', ')
            : cevir(String(ic ?? ''));
          return `${satir?.etiket ?? satirKimligi}: ${icMetin}`;
        })
        .join(' · ');
    }

    return cevir(String(d));
  })();

  // "Diğer"in serbest metni parantez içinde: ne seçtiği de ne yazdığı da
  // tek satırda görünmeli.
  return serbest ? `${temel} (${serbest})` : temel;
}
