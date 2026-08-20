import { describe, expect, it } from 'vitest';
import {
  alanEkle, alanKopyala, alanSil, alanTasi, bosTanim, ileriKosullariDusur,
  kosulAdaylari, tumAlanlarSirali, yeniAlan, yeniKimlik,
} from '../src/forms/definitionOps';
import { FIELD_TYPE } from '../src/forms/fieldTypes';
import {
  adimHatalari, alaniDogrula, gonderilecek, kosulSaglandi, tcGecerli, type Answers,
} from '../src/forms/formEngine';
import type { FormDefinition } from '../src/data/types';

/**
 * FORM TASARIMCISI VE MOTORU.
 *
 * Ağaç işlemleri React'siz test edilebilsin diye ayrı bir dosyaya çıkarıldı;
 * bu modülün en kolay bozulan yeri sıralama ve koşul bağları.
 */

function ornek(): FormDefinition {
  let t = bosTanim();
  t = alanEkle(t, 0, 0, { ...yeniAlan(FIELD_TYPE.evetHayir, 1), kimlik: 'q1', etiket: 'Şikâyet var mı?' });
  t = alanEkle(t, 0, 0, { ...yeniAlan(FIELD_TYPE.uzunMetin, 1), kimlik: 'q2', etiket: 'Detay' });
  t = alanEkle(t, 0, 0, { ...yeniAlan(FIELD_TYPE.tekSecim, 1), kimlik: 'q3', etiket: 'Kanal' });
  return t;
}

describe('tanım ağacı işlemleri', () => {
  it('yeni kimlikler benzersiz', () => {
    const k = new Set(Array.from({ length: 500 }, () => yeniKimlik()));
    expect(k.size).toBe(500);
  });

  it('alan silinince ona bakan koşullar da temizlenir', () => {
    let t = ornek();
    t = { ...t, adimlar: t.adimlar!.map((a) => ({
      ...a,
      gruplar: a.gruplar!.map((g) => ({
        ...g,
        alanlar: g.alanlar!.map((x) => x.kimlik === 'q2'
          ? { ...x, kosul: { baglac: 0 as const, kurallar: [{ alanKimligi: 'q1', operator: 0 as const, deger: 'true' }] } }
          : x),
      })),
    })) };

    // Koşulun hedefi silinince koşul da düşmeli; kalsaydı sunucu
    // "var olmayan alanı gösteriyor" diye kaydetmeyi reddederdi.
    const sonra = alanSil(t, 'q1');
    const q2 = tumAlanlarSirali(sonra).find((a) => a.kimlik === 'q2');

    expect(q2?.kosul ?? null).toBeNull();
  });

  it('kopyalanan alanın kimlikleri YENİDEN üretilir', () => {
    const t = alanKopyala(ornek(), 'q3');
    const alanlar = tumAlanlarSirali(t);
    const kimlikler = alanlar.map((a) => a.kimlik);

    expect(new Set(kimlikler).size).toBe(kimlikler.length);

    // Seçenek kimlikleri de: paylaşılsaydı iki soru aynı seçeneği
    // gösterir ve raporda birbirine karışırdı.
    const secenekler = alanlar.flatMap((a) => (a.secenekler ?? []).map((s) => s.kimlik));
    expect(new Set(secenekler).size).toBe(secenekler.length);
  });

  it('koşul adayları YALNIZCA daha önce gelen alanlar', () => {
    const t = ornek();

    expect(kosulAdaylari(t, 'q1').map((a) => a.kimlik)).toEqual([]);
    expect(kosulAdaylari(t, 'q2').map((a) => a.kimlik)).toEqual(['q1']);
    expect(kosulAdaylari(t, 'q3').map((a) => a.kimlik)).toEqual(['q1', 'q2']);
  });

  /**
   * Taşıma bir koşulu İLERİYE baktırırsa koşul düşürülür.
   *
   * Sunucu geriye referansı zorluyor. Kaydetme anında hata vermek yerine
   * taşıma anında düzeltmek, kullanıcının neyi neden kaybettiğini
   * görmesini sağlıyor.
   */
  it('taşıma sonrası ileriye bakan koşul düşürülür', () => {
    let t = ornek();
    t = { ...t, adimlar: t.adimlar!.map((a) => ({
      ...a,
      gruplar: a.gruplar!.map((g) => ({
        ...g,
        alanlar: g.alanlar!.map((x) => x.kimlik === 'q2'
          ? { ...x, kosul: { baglac: 0 as const, kurallar: [{ alanKimligi: 'q1', operator: 0 as const, deger: 'true' }] } }
          : x),
      })),
    })) };

    // q2'yi q1'in ÖNÜNE al: koşul artık ileriye bakıyor.
    const tasinmis = alanTasi(t, 'q2', { adim: 0, grup: 0, indeks: 0 });
    const { tanim, dusen } = ileriKosullariDusur(tasinmis);

    expect(dusen).toContain('Detay');
    expect(tumAlanlarSirali(tanim).find((a) => a.kimlik === 'q2')?.kosul ?? null).toBeNull();
  });
});

describe('form motoru', () => {
  it('bağlaç: VE tümünü, VEYA birini ister', () => {
    const c: Answers = { a: { deger: '1' }, b: { deger: '2' } };

    const ve = { baglac: 0, kurallar: [
      { alanKimligi: 'a', operator: 0, deger: '1' },
      { alanKimligi: 'b', operator: 0, deger: 'yok' }] };
    const veya = { ...ve, baglac: 1 };

    expect(kosulSaglandi(ve, c)).toBe(false);
    expect(kosulSaglandi(veya, c)).toBe(true);
  });

  it('boş kural listesi KOŞULSUZ demektir', () => {
    // Tasarımcıda "koşul ekle" deyip kural yazmayan kullanıcı alanı
    // kaybetmemeli.
    expect(kosulSaglandi({ baglac: 0, kurallar: [] }, {})).toBe(true);
    expect(kosulSaglandi(null, {})).toBe(true);
  });

  it('görünmeyen zorunlu alan hata vermez', () => {
    let t = bosTanim();
    t = alanEkle(t, 0, 0, { ...yeniAlan(FIELD_TYPE.evetHayir, 1), kimlik: 'q1', etiket: 'Var mı?' });
    t = alanEkle(t, 0, 0, {
      ...yeniAlan(FIELD_TYPE.uzunMetin, 1), kimlik: 'q2', etiket: 'Detay', zorunlu: true,
      kosul: { baglac: 0, kurallar: [{ alanKimligi: 'q1', operator: 0, deger: 'true' }] },
    });

    expect(adimHatalari(t, 0, { q1: { deger: false } })).toEqual({});
    expect(adimHatalari(t, 0, { q1: { deger: true } })).toHaveProperty('q2');
  });

  /**
   * Görünmeyen alanın cevabı GÖNDERİLMEZ.
   *
   * Kullanıcı önce "Evet" deyip detayı yazmış, sonra "Hayır"a dönmüş
   * olabilir. O metni göndermek, kaydın kendi mantığıyla çelişmesi demek.
   */
  it('görünmeyen alanın cevabı gövdeye girmez', () => {
    let t = bosTanim();
    t = alanEkle(t, 0, 0, { ...yeniAlan(FIELD_TYPE.evetHayir, 1), kimlik: 'q1', etiket: 'Var mı?' });
    t = alanEkle(t, 0, 0, {
      ...yeniAlan(FIELD_TYPE.uzunMetin, 1), kimlik: 'q2', etiket: 'Detay',
      kosul: { baglac: 0, kurallar: [{ alanKimligi: 'q1', operator: 0, deger: 'true' }] },
    });

    const c: Answers = { q1: { deger: false }, q2: { deger: 'vazgeçtim' } };
    expect(Object.keys(gonderilecek(t, c))).toEqual(['q1']);
  });

  it('T.C. algoritması sunucudakiyle aynı', () => {
    expect(tcGecerli('10000000146')).toBe(true);
    expect(tcGecerli('11111111111')).toBe(false);
    expect(tcGecerli('01234567890')).toBe(false);
  });

  it('çok seçimde seçim sayısı sınırlanır', () => {
    const alan = {
      ...yeniAlan(FIELD_TYPE.cokSecim, 1),
      dogrulama: { enAzSecim: 2, enCokSecim: 2 },
    };

    expect(alaniDogrula(alan, { deger: ['a'] })).toBeTruthy();
    expect(alaniDogrula(alan, { deger: ['a', 'b'] })).toBeNull();
    expect(alaniDogrula(alan, { deger: ['a', 'b', 'c'] })).toBeTruthy();
  });
});
