import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import {
  SLOT_HEIGHT, overlapping, minuteOffset, minutesToPixels, snapToSlot,
  eventRange, layoutDay, pixelsToMinutes,
} from '../src/calendar/layout';
import { RECURRENCE_SCOPE, DEFAULT_DURATION_MINUTES, type CalendarEvent } from '../src/calendar/types';

let siradaki = 1;

function etkinlik(bas: string, bit: string | null, ek: Partial<CalendarEvent> = {}): CalendarEvent {
  return {
    id: siradaki++,
    baslik: `Etkinlik ${bas}`,
    baslangic: bas,
    bitis: bit,
    tumGun: false,
    konum: null,
    tipId: 1,
    durumId: 1,
    statu: 0,
    gizli: false,
    seriId: null,
    seriAyrik: false,
    resimVar: false,
    basinKatilsin: false,
    ...ek,
  };
}

const GUN = new Date(2026, 7, 12);

/** Sürükleme/boyutlandırma kuralları KAYNAK TARANARAK kilitleniyor. */
const izgaraKaynagi = readFileSync(
  join(__dirname, '..', 'src', 'calendar', 'TimeGrid.tsx'), 'utf8');

describe('ızgara ölçeği', () => {
  it('30 dakika bir dilim yüksekliğine denk gelir', () => {
    expect(minutesToPixels(30)).toBe(SLOT_HEIGHT);
    expect(minutesToPixels(60)).toBe(SLOT_HEIGHT * 2);
    expect(pixelsToMinutes(SLOT_HEIGHT)).toBe(30);
  });

  it('sürükleme deltası 30 dakikaya oturur', () => {
    // Yarım dilimden az → yerinde kalır.
    expect(snapToSlot(SLOT_HEIGHT * 0.4)).toBe(0);
    // Yarımdan fazla → bir dilim.
    expect(snapToSlot(SLOT_HEIGHT * 0.6)).toBe(SLOT_HEIGHT);
    // Negatif yönde de simetrik.
    expect(snapToSlot(-SLOT_HEIGHT * 0.6)).toBe(-SLOT_HEIGHT);
  });

  it('dakika ofseti gece yarısından sayılır', () => {
    expect(minuteOffset(new Date(2026, 7, 12, 0, 0))).toBe(0);
    expect(minuteOffset(new Date(2026, 7, 12, 14, 30))).toBe(870);
  });
});

describe('etkinlik aralığı', () => {
  it('bitişi olmayan etkinliğe 30 dakika verir', () => {
    const { bas, bit } = eventRange(etkinlik('2026-08-12T14:00:00', null));
    expect((bit.getTime() - bas.getTime()) / 60_000).toBe(DEFAULT_DURATION_MINUTES);
  });
});

describe('çakışan etkinlikler', () => {
  it('çakışmayanlar tek sütun kullanır', () => {
    const y = layoutDay(
      [
        etkinlik('2026-08-12T09:00:00', '2026-08-12T10:00:00'),
        etkinlik('2026-08-12T11:00:00', '2026-08-12T12:00:00'),
      ],
      GUN,
    );

    expect(y).toHaveLength(2);
    expect(y.every((k) => k.sutunSayisi === 1)).toBe(true);
  });

  it('çakışanları yan yana koyar — GİZLEMEZ', () => {
    const y = layoutDay(
      [
        etkinlik('2026-08-12T09:00:00', '2026-08-12T11:00:00'),
        etkinlik('2026-08-12T10:00:00', '2026-08-12T12:00:00'),
      ],
      GUN,
    );

    // Çakışma ENGELLENMEZ; ikisi de görünür kalmalı.
    expect(y).toHaveLength(2);
    expect(y.every((k) => k.sutunSayisi === 2)).toBe(true);
    expect(new Set(y.map((k) => k.sutun)).size).toBe(2);
  });

  it('üçlü çakışmada üç sütun açar', () => {
    const y = layoutDay(
      [
        etkinlik('2026-08-12T09:00:00', '2026-08-12T12:00:00'),
        etkinlik('2026-08-12T09:30:00', '2026-08-12T11:00:00'),
        etkinlik('2026-08-12T10:00:00', '2026-08-12T10:30:00'),
      ],
      GUN,
    );

    expect(y).toHaveLength(3);
    expect(Math.max(...y.map((k) => k.sutunSayisi))) .toBe(3);
  });

  it('biten sütunu yeniden kullanır', () => {
    // 09–10 ve 10–11 çakışmaz; ikinci, birincinin sütununa girmeli.
    const y = layoutDay(
      [
        etkinlik('2026-08-12T09:00:00', '2026-08-12T10:00:00'),
        etkinlik('2026-08-12T10:00:00', '2026-08-12T11:00:00'),
      ],
      GUN,
    );
    expect(y.every((k) => k.sutun === 0)).toBe(true);
  });

  it('tüm gün etkinlikleri ızgaraya girmez', () => {
    const y = layoutDay([etkinlik('2026-08-12T00:00:00', null, { tumGun: true })], GUN);
    expect(y).toHaveLength(0);
  });

  it('başka güne ait etkinlik listelenmez', () => {
    const y = layoutDay([etkinlik('2026-08-13T09:00:00', '2026-08-13T10:00:00')], GUN);
    expect(y).toHaveLength(0);
  });

  it('gece yarısını aşan etkinlik gün sınırına kırpılır', () => {
    const y = layoutDay([etkinlik('2026-08-12T22:00:00', '2026-08-13T02:00:00')], GUN);

    expect(y).toHaveLength(1);
    // 22:00 → 24:00 = 2 saat = 4 dilim
    expect(y[0].yukseklikPx).toBe(SLOT_HEIGHT * 4);
  });

  it('konum piksele doğru çevrilir', () => {
    const y = layoutDay([etkinlik('2026-08-12T09:00:00', '2026-08-12T10:30:00')], GUN);

    // 09:00 = 540 dk = 18 dilim
    expect(y[0].ustPx).toBe(SLOT_HEIGHT * 18);
    // 1,5 saat = 3 dilim
    expect(y[0].yukseklikPx).toBe(SLOT_HEIGHT * 3);
  });
});

describe('çakışma uyarısı', () => {
  const aralikOf = (e: CalendarEvent) => eventRange(e);

  it('çakışanları bildirir ama engellemez', () => {
    const a = etkinlik('2026-08-12T09:00:00', '2026-08-12T11:00:00');
    const b = etkinlik('2026-08-12T10:00:00', '2026-08-12T12:00:00');
    const { bas, bit } = aralikOf(a);

    const c = overlapping([a, b], bas, bit, a.id);
    expect(c.map((e) => e.id)).toEqual([b.id]);
  });

  it('kendisini çakışma saymaz', () => {
    const a = etkinlik('2026-08-12T09:00:00', '2026-08-12T11:00:00');
    const { bas, bit } = aralikOf(a);
    expect(overlapping([a], bas, bit, a.id)).toHaveLength(0);
  });

  it('bitişik etkinlikler çakışmaz', () => {
    // 10:00'da biten ile 10:00'da başlayan çakışmaz — sınır dahil değil.
    const a = etkinlik('2026-08-12T09:00:00', '2026-08-12T10:00:00');
    const b = etkinlik('2026-08-12T10:00:00', '2026-08-12T11:00:00');
    const { bas, bit } = aralikOf(a);
    expect(overlapping([a, b], bas, bit, a.id)).toHaveLength(0);
  });

  it('tüm gün etkinliği çakışma saymaz', () => {
    // Tüm gün etkinlikleri ayrı şeritte; saatli etkinlikle çakışmaz.
    const a = etkinlik('2026-08-12T09:00:00', '2026-08-12T10:00:00');
    const tg = etkinlik('2026-08-12T00:00:00', null, { tumGun: true });
    const { bas, bit } = aralikOf(a);
    expect(overlapping([a, tg], bas, bit, a.id)).toHaveLength(0);
  });
});

describe('tekrar kapsamı sözleşmesi', () => {
  it('sunucudaki TekrarKapsam ile aynı sayısal değerler', () => {
    // Sıra değişirse "yalnızca bu" isteği sessizce "tüm seri"ye dönüşür.
    expect(RECURRENCE_SCOPE.yalnizca).toBe(0);
    expect(RECURRENCE_SCOPE.bundanSonrakiler).toBe(1);
    expect(RECURRENCE_SCOPE.tumu).toBe(2);
  });
});

/**
 * TAKVİM IZGARASI — sürükleme ve boyutlandırma sözleşmesi.
 *
 * <p>
 * Buradaki kuralların üçü de <b>sessizce</b> bozulabiliyor: ekran açılır,
 * bloklar çizilir, yalnızca jest bozuk hisseder. Davranışı jsdom'da sınamak
 * gerçek işaretçi olayları ve düzen ölçümü gerektirdiği için kural
 * <b>izgaraKaynagi taranarak</b> kilitleniyor — depodaki diğer izgaraKaynagi tarayan
 * testlerle aynı gerekçe.
 * </p>
 */
describe('takvim zaman ızgarası', () => {
  /**
   * Tek `PointerSensor` fare ile parmağa aynı kısıtı uyguluyordu
   * (`distance: 4`). Parmakta dokunuş sırasında birkaç piksel kayma olağan;
   * etkinliği AÇMAK isteyen kullanıcı farkında olmadan sürükleme başlatıyor,
   * dokunuş yutuluyor ve blok bir dilim kayıyordu.
   */
  it('fare ve parmak AYRI sensör kullanır', () => {
    // KULLANIM aranıyor, metin değil: bu dosyanın yorumları eski sensörün
    // neden bırakıldığını anlatıyor ve düz bir metin taraması kendi
    // açıklamasına takılırdı. (Aynı yanlış pozitifi `tokens.test.ts` de
    // üretmişti — o bekçi yorum satırlarını da tarıyor.)
    const kullanilan = [...izgaraKaynagi.matchAll(/useSensor\(\s*(\w+)/g)].map((m) => m[1]);

    expect(kullanilan).toContain('MouseSensor');
    expect(kullanilan).toContain('TouchSensor');
    expect(kullanilan).not.toContain('PointerSensor');
  });

  it('parmakta ölçüt SÜRE, farede MESAFE', () => {
    expect(izgaraKaynagi).toMatch(/MouseSensor,\s*\{\s*activationConstraint:\s*\{\s*distance:/);
    expect(izgaraKaynagi).toMatch(/TouchSensor,\s*\{\s*activationConstraint:\s*\{\s*delay:/);
  });

  /**
   * Tutamak geometrisi CANLI önizleme yüksekliğine bağlıyken, blok
   * boyutlandırma sırasında eşiği geçince tutamak yeniden konumlanıyordu.
   * Ölçüldü: 56×12px / soldan 68px → 122×8px / soldan 2px, tam kullanıcı
   * onu tutarken.
   */
  it('tutamak geometrisi KAYDEDİLMİŞ yüksekliğe bakar', () => {
    expect(izgaraKaynagi).toContain('tutamakKisa');
    expect(izgaraKaynagi).toMatch(/const tutamakKisa = yukseklikPx </);
    // İçerik düzeni ise canlı yüksekliğe bakmalı: blok uzarken etiket
    // iki satıra geçsin.
    expect(izgaraKaynagi).toMatch(/const icerikKisa = yuk </);
  });

  /**
   * Tutamak bir dönem kısa blokta sağ alt köşeye çekiliyordu ve 124px'lik
   * bloğun içinde soldan 68px içeride, hiçbir kenara yaslanmayan bir çubuk
   * olarak görünüyordu.
   */
  it('tutamak her durumda TAM GENİŞLİK', () => {
    expect(izgaraKaynagi).not.toMatch(/right-0 h-\[12px\] w-14/);
    // Konumlandırma sınıfı tutamağın ortak kısmında olmalı.
    expect(izgaraKaynagi).toMatch(/'inset-x-0',/);
  });
});
