import { describe, expect, it } from 'vitest';
import { cn } from '../src/components/utils';

/**
 * SINIF BİRLEŞTİRME — tema tabanlı ölçüler de ayıklanmalı.
 *
 * <p>
 * `h-ctrl`, `h-field` gibi sınıflar Tailwind 4'ün `@theme` bloğundan
 * üretiliyor ve `tailwind-merge` onları kendiliğinden tanımıyor. Tanımadığı
 * sürece çakışma ayıklanmıyor: iki sınıf da DOM'a düşüyor ve CSS'te özel
 * olan kazanıyor — yani ÇAĞIRANIN verdiği boy sessizce yok sayılıyor.
 * </p>
 *
 * <p>
 * Ölçülmüş bedeli: giriş ekranının birincil düğmesi `h-[52px]` yazdığı
 * hâlde 40px çiziliyordu ve şartnamedeki 48px dokunma hedefinin altında
 * kalıyordu.
 * </p>
 */
describe('sınıf birleştirme', () => {
  it('tema tabanlı boyu çağıranın boyu ezer', () => {
    expect(cn('h-ctrl', 'h-[52px]')).toBe('h-[52px]');
    expect(cn('h-ctrl', 'h-ctrl-lg')).toBe('h-ctrl-lg');
    expect(cn('h-field', 'h-12')).toBe('h-12');
  });

  it('en küçük boy da ayıklanır', () => {
    expect(cn('min-h-ctrl', 'min-h-[92px]')).toBe('min-h-[92px]');
  });

  // Duyarlı varyantlar AYRI gruptur: `h-ctrl md:h-12` ikisini de korumalı,
  // yoksa masaüstü boyu mobil boyu siler.
  it('duyarlı varyant ayıklanmaz', () => {
    expect(cn('h-ctrl', 'md:h-12')).toBe('h-ctrl md:h-12');
  });

  it('çekirdek Tailwind ayıklaması bozulmaz', () => {
    expect(cn('px-4', 'px-6')).toBe('px-6');
    expect(cn('text-sm', 'text-base')).toBe('text-base');
  });
});
