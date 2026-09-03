import { describe, expect, it } from 'vitest';
import { range, relativeTime, time, date, dateTime } from '../src/data/format';
import { roundToMinutes, serverToLocal, localToServer } from '../src/data/time';

/**
 * Zaman dönüşümleri bu projedeki en sessiz hata kaynağı.
 *
 * <p>
 * Sunucudaki her damga `timestamp without time zone` — saat dilimi
 * TAŞIMAZ. Tek bir `toISOString()` çağrısı Türkiye'de bütün etkinlikleri
 * 3 saat kaydırır ve bu sunucu hatası gibi görünür. Aşağıdaki testler
 * gidiş-dönüşün bit düzeyinde aynı kaldığını kilitler.
 * </p>
 */
describe('sunucu ⇄ yerel zaman', () => {
  it('metni yerel saat olarak okur, UTC olarak DEĞİL', () => {
    const t = serverToLocal('2026-08-12T14:30:00');

    // Saat dilimi ne olursa olsun 14:30 kalmalı.
    expect(t.getHours()).toBe(14);
    expect(t.getMinutes()).toBe(30);
    expect(t.getFullYear()).toBe(2026);
    expect(t.getMonth()).toBe(7); // Ağustos
    expect(t.getDate()).toBe(12);
  });

  it('gidiş-dönüş metni değiştirmez', () => {
    const ozgun = '2026-08-12T14:30:00';
    expect(localToServer(serverToLocal(ozgun))).toBe(ozgun);
  });

  it('sondaki Z ekini yok sayar', () => {
    // Bazı istemciler yanlışlıkla Z ekliyor; yine de yerel okunmalı.
    expect(serverToLocal('2026-08-12T14:30:00Z').getHours()).toBe(14);
    expect(serverToLocal('2026-08-12T14:30:00+03:00').getHours()).toBe(14);
  });

  it('DST sınırında kaymaz', () => {
    // Türkiye 2016'da kalıcı UTC+3'e geçti; yine de tarayıcı başka bir
    // saat diliminde olabilir. Mart sonu klasik DST sınırıdır.
    for (const metin of [
      '2026-03-29T02:30:00',
      '2026-03-29T03:30:00',
      '2026-10-25T02:30:00',
    ]) {
      expect(localToServer(serverToLocal(metin))).toBe(metin);
    }
  });

  it('gece yarısını doğru taşır', () => {
    expect(localToServer(serverToLocal('2026-01-01T00:00:00'))).toBe('2026-01-01T00:00:00');
  });

  it('30 dakikaya yuvarlar', () => {
    expect(time(localToServer(roundToMinutes(new Date(2026, 7, 12, 14, 8), 30)))).toBe('14:00');
    expect(time(localToServer(roundToMinutes(new Date(2026, 7, 12, 14, 22), 30)))).toBe('14:30');
    expect(time(localToServer(roundToMinutes(new Date(2026, 7, 12, 14, 52), 30)))).toBe('15:00');
  });
});

describe('biçimlendirme', () => {
  it('tarihi Türkçe basar', () => {
    expect(date('2026-08-12T14:30:00')).toBe('12 Ağustos 2026');
    expect(time('2026-08-12T09:05:00')).toBe('09:05');
    expect(dateTime('2026-08-12T14:30:00')).toBe('12 Ağustos 2026, 14:30');
  });

  it('boş değerde tire döner, çökmez', () => {
    expect(date(null)).toBe('—');
    expect(time(undefined)).toBe('—');
  });

  it('aynı gün aralığını kısa yazar', () => {
    expect(range('2026-08-12T14:30:00', '2026-08-12T15:30:00')).toBe('14:30 – 15:30');
  });

  it('gün aşan aralıkta tarihi de gösterir', () => {
    expect(range('2026-08-12T22:00:00', '2026-08-13T02:00:00')).toContain('13.08.2026');
  });

  it('tüm gün etkinliğinde saat göstermez', () => {
    expect(range('2026-08-12T00:00:00', '2026-08-12T23:59:00', true)).toBe('Tüm gün');
  });

  it('bağıl zamanı doğru yönde söyler', () => {
    const simdi = new Date(2026, 7, 12, 12, 0, 0);
    expect(relativeTime('2026-08-12T14:00:00', simdi)).toBe('2 saat sonra');
    expect(relativeTime('2026-08-12T10:00:00', simdi)).toBe('2 saat önce');
    expect(relativeTime('2026-08-13T12:00:00', simdi)).toBe('yarın');
    expect(relativeTime('2026-08-11T12:00:00', simdi)).toBe('dün');
  });
});
