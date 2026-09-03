import { describe, expect, it } from 'vitest';
import {
  EMPTY_RECURRENCE,
  parseRrule,
  buildRrule,
  type RecurrenceState,
} from '../src/screens/agenda/RecurrenceRule';

/**
 * TEKRAR KURALI — mobil ile aynı RRULE alt kümesi.
 *
 * Mobil (`rrule_yardimcisi.dart`) ve sunucu (`RRuleKural`) referanstır. Web
 * bir kuralı okuyup geri yazdığında **aynı** kuralı üretmeli; aksi hâlde
 * kaydet'e basmak "kural değişti" sayılır ve sunucu seriyi böler —
 * kullanıcı açısından etkinlikler kaybolmuş görünür.
 */
describe('rruleUret', () => {
  const temel = (yama: Partial<RecurrenceState> = {}): RecurrenceState => ({
    ...EMPTY_RECURRENCE,
    acik: true,
    dokunuldu: true,
    ...yama,
  });

  it('haftalık kuralı gün seçimiyle yazar', () => {
    expect(buildRrule(temel({ siklik: 'WEEKLY', gunler: ['MO', 'WE'] })))
      .toBe('FREQ=WEEKLY;BYDAY=MO,WE');
  });

  /**
   * BİLİNEN ÜRETİM HATASI: istemciler kuralı formun başlangıç tarihinden
   * türetiyordu. Bir tekrarı çarşambadan perşembeye taşımak `BYDAY=TH`
   * göndermeye yol açıyor, sunucu bunu "kural değişti" diye okuyup seriyi
   * bölüyordu.
   */
  it('gün seçilmediyse BYDAY YAZMAZ', () => {
    expect(buildRrule(temel({ siklik: 'WEEKLY', gunler: [] }))).toBe('FREQ=WEEKLY');
  });

  it('aralık 1 ise INTERVAL yazmaz', () => {
    expect(buildRrule(temel({ siklik: 'DAILY', aralik: 1 }))).toBe('FREQ=DAILY');
    expect(buildRrule(temel({ siklik: 'DAILY', aralik: 3 }))).toBe('FREQ=DAILY;INTERVAL=3');
  });

  it('aylık desenleri mobil ile aynı yazar', () => {
    expect(buildRrule(temel({ siklik: 'MONTHLY', aylikTur: 'ayinGunu', ayinGunu: 15 })))
      .toBe('FREQ=MONTHLY;BYMONTHDAY=15');

    expect(buildRrule(temel({ siklik: 'MONTHLY', aylikTur: 'sonGun' })))
      .toBe('FREQ=MONTHLY;BYMONTHDAY=-1');

    expect(
      buildRrule(temel({
        siklik: 'MONTHLY',
        aylikTur: 'haftaninGunu',
        haftaSirasi: 2,
        haftaGunu: 'TH',
      })),
    ).toBe('FREQ=MONTHLY;BYDAY=2TH');
  });

  it('bitiş türünü COUNT ya da UNTIL olarak yazar', () => {
    expect(buildRrule(temel({ siklik: 'DAILY', bitisTuru: 'sayi', adet: 5 })))
      .toBe('FREQ=DAILY;COUNT=5');

    expect(buildRrule(temel({ siklik: 'DAILY', bitisTuru: 'tarih', bitisTarihi: '2026-12-31' })))
      .toBe('FREQ=DAILY;UNTIL=20261231T235959');
  });

  it('kapalıysa null döner', () => {
    expect(buildRrule({ ...EMPTY_RECURRENCE, acik: false })).toBeNull();
  });
});

describe('rruleCoz', () => {
  it('haftalık günleri okur', () => {
    const t = parseRrule('FREQ=WEEKLY;BYDAY=MO,WE;INTERVAL=2');
    expect(t.siklik).toBe('WEEKLY');
    expect(t.gunler).toEqual(['MO', 'WE']);
    expect(t.aralik).toBe(2);
  });

  /**
   * `BYDAY=2TH` sıra eklidir ve AYLIK deseni anlatır; haftalık gün seçimine
   * taşınırsa kural "her perşembe"ye dönüşür.
   */
  it('sıra ekli BYDAY haftalık gün listesine taşınmaz', () => {
    const t = parseRrule('FREQ=MONTHLY;BYDAY=2TH');
    expect(t.gunler).toEqual([]);
    expect(t.aylikTur).toBe('haftaninGunu');
    expect(t.haftaSirasi).toBe(2);
    expect(t.haftaGunu).toBe('TH');
  });

  it('ayın son günü desenini tanır', () => {
    const t = parseRrule('FREQ=MONTHLY;BYMONTHDAY=-1');
    expect(t.aylikTur).toBe('sonGun');
  });

  it('ayın belirli gününü tanır', () => {
    const t = parseRrule('FREQ=MONTHLY;BYMONTHDAY=15');
    expect(t.aylikTur).toBe('ayinGunu');
    expect(t.ayinGunu).toBe(15);
  });

  it('UNTIL tarihini forma çevirir', () => {
    expect(parseRrule('FREQ=DAILY;UNTIL=20261231T235959').bitisTarihi).toBe('2026-12-31');
  });
});

/**
 * GİDİŞ-DÖNÜŞ: mobilde kurulmuş bir kural, webde açılıp dokunulmadan
 * kaydedilirse AYNEN geri gitmeli.
 */
describe('kayıpsız gidiş-dönüş', () => {
  const kurallar = [
    'FREQ=WEEKLY;BYDAY=MO,WE',
    'FREQ=MONTHLY;BYMONTHDAY=15',
    'FREQ=MONTHLY;BYMONTHDAY=-1',
    'FREQ=MONTHLY;BYDAY=2TH',
    'FREQ=DAILY;INTERVAL=3;COUNT=10',
    'FREQ=WEEKLY;BYDAY=FR;UNTIL=20261231T235959',
    // Formun ANLAMADIĞI parça — yine de korunmalı.
    'FREQ=YEARLY;BYMONTH=6;BYMONTHDAY=15',
  ];

  it.each(kurallar)('dokunulmayan kural aynen döner: %s', (kural) => {
    expect(buildRrule(parseRrule(kural))).toBe(kural);
  });

  it('kullanıcı dokununca kural yeniden üretilir', () => {
    const cozulen = parseRrule('FREQ=WEEKLY;BYDAY=MO');
    const degistirilmis = { ...cozulen, gunler: ['TU'], dokunuldu: true };

    expect(buildRrule(degistirilmis)).toBe('FREQ=WEEKLY;BYDAY=TU');
  });

  /**
   * Aynı kuralın gidiş-dönüşü DEĞİŞMEMELİ: değişirse etkinlik formu kuralı
   * "değişti" sanıp sunucuya gönderir ve seri bölünür.
   */
  it.each(kurallar)('gidiş-dönüş kuralı değiştirmez: %s', (kural) => {
    const cozulen = parseRrule(kural);
    expect(cozulen.dokunuldu).toBe(false);
    expect(buildRrule(cozulen)).toBe(kural);
  });
});
