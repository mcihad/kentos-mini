import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Secim } from './Field';
import { IconButton } from './Button';
import { cn } from './utils';
import type { PagedResult } from '../data/client';

const BOYUTLAR = [25, 50, 100, 200];

/**
 * Sayfa gezinme çubuğu.
 *
 * <p>
 * "1 2 3 … 47" biçiminde numaralı sayfalar YOK: bu listelerde kullanıcı 31.
 * sayfaya atlamıyor, arama yapıyor. Numaralar mobilde de sığmıyor ve
 * dokunma hedefi 44px'in altına iniyor. Bunun yerine konum bilgisi + ileri/geri.
 * </p>
 */
export function Pagination<T>({
  sonuc,
  sayfaDegistir,
  boyutDegistir,
  birim = 'kayıt',
  className,
}: {
  sonuc: PagedResult<T> | undefined;
  sayfaDegistir: (sayfa: number) => void;
  boyutDegistir?: (boyut: number) => void;
  /** "12 talep", "40 kullanıcı" gibi. */
  birim?: string;
  className?: string;
}) {
  if (!sonuc || sonuc.toplam === 0) return null;

  const ilk = (sonuc.sayfa - 1) * sonuc.boyut + 1;
  const son = Math.min(sonuc.sayfa * sonuc.boyut, sonuc.toplam);
  // Tek sayfaya sığıyorsa gezinme düğmeleri gürültüdür; yalnızca sayı kalır.
  const tekSayfa = sonuc.toplamSayfa <= 1;

  return (
    <div
      className={cn(
        'flex flex-wrap items-center justify-between gap-3 pt-1',
        className,
      )}
    >
      <p className="text-xs tabular-nums text-text-3">
        {tekSayfa ? (
          <>
            {sonuc.toplam} {birim}
          </>
        ) : (
          <>
            <span className="font-medium text-text-2">
              {ilk}–{son}
            </span>{' '}
            / {sonuc.toplam} {birim}
          </>
        )}
      </p>

      <div className="flex items-center gap-2">
        {boyutDegistir && !tekSayfa && (
          <>
            <label htmlFor="sayfa-boyutu" className="sr-only">
              Sayfa başına kayıt
            </label>
            <Secim
              id="sayfa-boyutu"
              value={sonuc.boyut}
              onChange={(e) => boyutDegistir(Number(e.target.value))}
              className="h-8 w-[86px] text-xs"
            >
              {BOYUTLAR.map((b) => (
                <option key={b} value={b}>
                  {b} / sayfa
                </option>
              ))}
            </Secim>
          </>
        )}

        {!tekSayfa && (
          <>
            <span className="text-xs tabular-nums text-text-3">
              {sonuc.sayfa} / {sonuc.toplamSayfa}
            </span>
            <IconButton
              etiket="Önceki sayfa"
              disabled={!sonuc.oncekiVar}
              onClick={() => sayfaDegistir(sonuc.sayfa - 1)}
              className="disabled:cursor-not-allowed disabled:opacity-40"
            >
              <ChevronLeft size={16} />
            </IconButton>
            <IconButton
              etiket="Sonraki sayfa"
              disabled={!sonuc.sonrakiVar}
              onClick={() => sayfaDegistir(sonuc.sayfa + 1)}
              className="disabled:cursor-not-allowed disabled:opacity-40"
            >
              <ChevronRight size={16} />
            </IconButton>
          </>
        )}
      </div>
    </div>
  );
}
