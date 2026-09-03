import { Check, Crown, Search, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { SearchInput } from './Field';
import { Skeleton } from './Skeleton';
import { usePeople } from '../data/tasks';
import type { Person } from '../data/types';
import { cn } from './utils';

/**
 * KİŞİ SEÇİCİ — göreve atama, ekip üyeliği ve proje ekibi için ortak.
 *
 * <p>
 * <b>Neden ortak bir bileşen:</b> üç ekran da aynı soruyu soruyor ve üçü de
 * onay kutulu düz bir liste çiziyordu. Aramasız bir onay kutusu listesi,
 * kırk kişilik bir müdürlükte kullanılamaz: aranan kişiyi bulmak için
 * kaydırmak gerekiyordu ve kimin seçili olduğu ancak listenin tamamı
 * taranarak görülebiliyordu.
 * </p>
 *
 * <p>
 * <b>Seçilenler yukarıda, çip olarak.</b> "Kim seçili?" sorusunun cevabı
 * listenin içinde dağınık durmaz; çip şeridi hem cevabı tek bakışta verir hem
 * de çıkarmayı tek dokunuşa indirir.
 * </p>
 *
 * <p>
 * Arama SUNUCUDA: liste etkin birimin alt ağacını kapsıyor ve büyük bir
 * kurumda yüzlerce satır olabilir.
 * </p>
 */
export function PersonPicker({
  secili,
  degistir,
  liderId,
  liderDegistir,
  liderEtiketi = 'Lider',
  tekli,
  id,
}: {
  secili: number[];
  degistir: (idler: number[]) => void;
  /** Verilirse her seçili satırda "lider yap" düğmesi çıkar. */
  liderId?: number | null;
  liderDegistir?: (id: number | null) => void;
  liderEtiketi?: string;
  /** Tek kişi seçilir (atama kutusu) — seçim listeyi değiştirmez, değiştirir. */
  tekli?: boolean;
  id?: string;
}) {
  const [girdi, setGirdi] = useState('');
  const [ara, setAra] = useState('');

  useEffect(() => {
    const z = setTimeout(() => setAra(girdi.trim()), 250);
    return () => clearTimeout(z);
  }, [girdi]);

  const { liste, isLoading } = usePeople(ara);

  /*
    SEÇİLİ KİŞİLER ARAMADAN ETKİLENMEZ.

    Kullanıcı "Ahmet" yazdığında liste daralıyor; seçili olan Ayşe artık
    yanıtta yok. Çipleri yalnızca gelen listeden çizseydik Ayşe ekrandan
    kaybolur ve kullanıcı onu çıkardığını sanardı. Bir kez görülen her kişi
    burada saklanıyor.
  */
  const [bilinen, setBilinen] = useState<Map<number, Person>>(new Map());

  useEffect(() => {
    if (liste.length === 0) return;
    setBilinen((eski) => {
      const yeni = new Map(eski);
      for (const k of liste) if (k.id != null) yeni.set(k.id, k);
      return yeni;
    });
  }, [liste]);

  const seciliKume = useMemo(() => new Set(secili), [secili]);

  const seciliKisiler = secili
    .map((k) => bilinen.get(k))
    .filter((k): k is Person => !!k);

  function degistirBir(kisiId: number) {
    if (tekli) {
      degistir(seciliKume.has(kisiId) ? [] : [kisiId]);
      return;
    }

    const yeni = new Set(seciliKume);
    if (yeni.has(kisiId)) yeni.delete(kisiId);
    else yeni.add(kisiId);
    degistir([...yeni]);
  }

  return (
    <div className="space-y-2" id={id}>
      <SearchInput
        value={girdi}
        onChange={(e) => setGirdi(e.target.value)}
        placeholder="Ad, soyad ya da unvanla ara"
        aria-label="Personel ara"
        ikon={<Search size={15} />}
      />

      {/* ── Seçilenler ── */}
      {!tekli && seciliKisiler.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {seciliKisiler.map((k) => (
            <span
              key={k.id}
              className="inline-flex items-center gap-1 rounded-full bg-brand-soft py-0.5 pl-2 pr-1 text-2xs text-ink"
            >
              {k.ad}
              {liderId === k.id && <Crown size={11} className="text-brand" />}
              <button
                type="button"
                onClick={() => degistirBir(k.id!)}
                aria-label={`${k.ad} kişisini çıkar`}
                className="grid h-4 w-4 place-items-center rounded-full text-ink-3 hover:bg-black/10 hover:text-ink"
              >
                <X size={11} />
              </button>
            </span>
          ))}
        </div>
      )}

      <div className="max-h-64 divide-y divide-line overflow-y-auto overscroll-contain rounded-control border border-line">
        {isLoading ? (
          <div className="space-y-2 p-3">
            <Skeleton className="h-5 w-2/3" />
            <Skeleton className="h-5 w-1/2" />
            <Skeleton className="h-5 w-3/5" />
          </div>
        ) : liste.length === 0 ? (
          <p className="px-3 py-6 text-center text-xs text-ink-3">
            {ara ? `"${ara}" ile eşleşen kişi yok.` : 'Biriminizde kayıtlı personel yok.'}
          </p>
        ) : (
          liste.map((k) => {
            const isaretli = seciliKume.has(k.id!);
            return (
              <button
                key={k.id}
                type="button"
                onClick={() => degistirBir(k.id!)}
                aria-pressed={isaretli}
                className={cn(
                  'flex min-h-11 w-full items-center gap-2.5 px-3 py-2 text-left',
                  isaretli ? 'bg-brand-soft/60' : 'hover:bg-sunken',
                )}
              >
                <Avatar ad={k.ad ?? ''} secili={isaretli} />

                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm text-ink">
                    {k.ad}
                    {k.kendisi && <span className="ml-1 text-2xs text-ink-3">(siz)</span>}
                  </span>
                  <span className="block truncate text-2xs text-ink-3">
                    {k.unvan}
                    {/*
                      ALT BİRİM ADI YALNIZCA ALT BİRİMDEN GELENDE.
                      Herkesin yanına kendi biriminin adını yazmak, listenin
                      tamamında tekrar eden ve hiçbir şey ayırt etmeyen bir
                      sütun olurdu; buradaki bilgi "bu kişi BENİM birimimden
                      değil" uyarısı.
                    */}
                    {k.altBirimden && (k.unvan ? ' · ' : '') + (k.birimAd ?? '')}
                  </span>
                </span>

                {isaretli && liderDegistir && (
                  <span
                    role="button"
                    tabIndex={0}
                    onClick={(e) => {
                      e.stopPropagation();
                      liderDegistir(liderId === k.id ? null : k.id!);
                    }}
                    onKeyDown={(e) => {
                      if (e.key !== 'Enter' && e.key !== ' ') return;
                      e.preventDefault();
                      e.stopPropagation();
                      liderDegistir(liderId === k.id ? null : k.id!);
                    }}
                    className={cn(
                      'shrink-0 rounded-full px-2 py-0.5 text-2xs',
                      liderId === k.id
                        ? 'bg-brand text-white'
                        : 'bg-sunken text-ink-3 hover:text-ink-2',
                    )}
                  >
                    {liderId === k.id ? liderEtiketi : `${liderEtiketi} yap`}
                  </span>
                )}

                {isaretli && (
                  <Check size={16} className="shrink-0 text-brand" strokeWidth={2.6} />
                )}
              </button>
            );
          })
        )}
      </div>
    </div>
  );
}

/**
 * BAŞ HARF ROZETİ.
 *
 * Fotoğraf alanı yok; ad baş harfleri kişiyi listede taramaya yetiyor ve
 * boş bir kullanıcı ikonunu her satırda tekrar etmekten daha çok şey
 * söylüyor.
 */
export function Avatar({
  ad,
  secili,
  boyut = 'orta',
}: {
  ad: string;
  secili?: boolean;
  boyut?: 'kucuk' | 'orta';
}) {
  const harfler = ad
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toLocaleUpperCase('tr-TR'))
    .join('');

  return (
    <span
      aria-hidden
      className={cn(
        'grid flex-none place-items-center rounded-full font-medium',
        boyut === 'kucuk' ? 'h-6 w-6 text-3xs' : 'h-8 w-8 text-2xs',
        secili ? 'bg-brand text-white' : 'bg-sunken text-ink-2',
      )}
    >
      {harfler || '·'}
    </span>
  );
}
