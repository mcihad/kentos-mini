import { describe, expect, it, vi, beforeEach } from 'vitest';
import { markaPaletiniUygula, NEUTRAL_COLORS, BRAND_COLORS, ACCENT_COLORS } from '../src/theme/palettes';

/**
 * KURUM RENKLERİNİN PALETE YAZILMASI.
 *
 * `--neutral` bir "kurumsal gri" değil, sayfanın ZEMİN TABANI: `--bg`,
 * `--canvas` ve `--sunken` ondan türüyor. Kurum kaydına %85 gri (#4D4D4F)
 * yazıldığında bütün uygulama koyu griye döndü ve ikincil metinler zeminle
 * aynı tona düşüp okunmaz oldu — üstelik hiçbir yerde hata vermeden, çünkü
 * geçerli bir hex geçerli bir CSS değeri.
 */
describe('kurum marka paleti', () => {
  // v3 tasarım dili: fabrika zemini soğuk kâğıt (şartname açık tema `bg`).
  const FABRIKA_ZEMIN = '#F4F8FC';

  beforeEach(() => {
    markaPaletiniUygula({});
  });

  it('açık bir zemin tonu kabul edilir', () => {
    markaPaletiniUygula({ notr: '#F1F4F9' });
    expect(NEUTRAL_COLORS[0].deger).toBe('#F1F4F9');
  });

  it('KOYU zemin tonu REDDEDİLİR — arayüz okunmaz hâle gelmemeli', () => {
    const uyari = vi.spyOn(console, 'warn').mockImplementation(() => {});

    markaPaletiniUygula({ notr: '#4D4D4F' });   // kurumsal gri

    expect(NEUTRAL_COLORS[0].deger).toBe(FABRIKA_ZEMIN);
    expect(uyari).toHaveBeenCalledOnce();
    uyari.mockRestore();
  });

  it('bozuk hex sessizce fabrika değerine döner', () => {
    const uyari = vi.spyOn(console, 'warn').mockImplementation(() => {});
    markaPaletiniUygula({ notr: 'lacivert' });
    expect(NEUTRAL_COLORS[0].deger).toBe(FABRIKA_ZEMIN);
    // Bozuk değer için uyarı YOK: kullanıcı renk seçmemiş olabilir.
    expect(uyari).not.toHaveBeenCalled();
    uyari.mockRestore();
  });

  it('marka ve vurgu KOYU olabilir — onlar zemin değil, dolgu', () => {
    markaPaletiniUygula({ birincil: '#002E6D', vurgu: '#A78952' });
    expect(BRAND_COLORS[0].acik).toBe('#002E6D');
    expect(ACCENT_COLORS[0].acik).toBe('#A78952');
  });

  it('koyu tema karşılığı verilmezse fabrika değeri korunur', () => {
    markaPaletiniUygula({ birincil: '#7A1F2B' });
    expect(BRAND_COLORS[0].acik).toBe('#7A1F2B');
    expect(BRAND_COLORS[0].koyu).toBe('#4E85C4');   // fabrika — şartname koyu tema `primary`
  });
});
