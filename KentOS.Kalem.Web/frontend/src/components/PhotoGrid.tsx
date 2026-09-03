import { ImageOff, Paperclip } from 'lucide-react';
import { useKorumaliAdres, useKorumaliAdresler } from '../data/korumaliMedya';
import { ImageViewer, useImageViewer, type Resim } from './ImageViewer';
import { cn } from './utils';

export type Foto = {
  /** Görselin kimlik denetimli uç adresi. */
  yol: string;
  baslik?: string | null;
  altBilgi?: string | null;
};

/**
 * FOTOĞRAF IZGARASI — eklerin kendisini gösterir, adını değil.
 *
 * <p>
 * İş takip modülünde fotoğraf her yerde <b>kanıt</b>: aşamalarda zorunlu
 * tutuluyor, vatandaş şikayetiyle birlikte geliyor, sahadan yükleniyor. Buna
 * rağmen arayüz hiçbirini göstermiyordu — ataş simgesi, dosya adı ve bir
 * indirme düğmesi vardı. Fotoğrafı görmek için indirip işletim sisteminin
 * görüntüleyicisinde açmak gerekiyordu.
 * </p>
 *
 * <p>
 * <b>Küçük görseller kare kırpılıyor</b> (<code>object-cover</code>): sahadan
 * gelen fotoğraflar dikey, yatay ve kare karışık ve oranlarına saygı göstermek
 * ızgarayı tırtıklı bir duvara çeviriyordu. Tam kare, tam oran — büyütülmüş
 * hâlde.
 * </p>
 */
export function PhotoGrid({
  fotograflar,
  boyut = 'normal',
  className,
}: {
  fotograflar: Foto[];
  /** `kucuk` — aşama satırı içinde; `normal` — kendi bölümünde. */
  boyut?: 'kucuk' | 'normal';
  className?: string;
}) {
  const goruntuleyici = useImageViewer();
  const adresler = useKorumaliAdresler(fotograflar.map((f) => f.yol));

  if (fotograflar.length === 0) return null;

  /*
    GÖRÜNTÜLEYİCİYE YALNIZCA ÇÖZÜLMÜŞ RESİMLER GİDİYOR.

    `ImageViewer` düz bir `src` bekliyor; korumalı uç adresini oraya vermek
    401 dönen bir kare gösterirdi. Henüz inmemiş resimler listeden çıkarılıyor
    ve tıklanan resmin indeksi ona göre hesaplanıyor — aksi hâlde ikinci
    resme tıklayınca üçüncüsü açılırdı.
  */
  const hazir: Resim[] = [];
  const indeksHaritasi = new Map<number, number>();

  adresler.forEach((adres, i) => {
    if (!adres) return;
    indeksHaritasi.set(i, hazir.length);
    hazir.push({
      yol: adres,
      baslik: fotograflar[i].baslik,
      altBilgi: fotograflar[i].altBilgi,
    });
  });

  const kare = boyut === 'kucuk' ? 'h-16 w-16' : 'h-24 w-24 md:h-28 md:w-28';

  return (
    <>
      <div className={cn('flex flex-wrap gap-2', className)}>
        {fotograflar.map((f, i) => (
          <Kucuk
            key={f.yol}
            foto={f}
            kare={kare}
            ac={() => {
              const sira = indeksHaritasi.get(i);
              if (sira !== undefined) goruntuleyici.ac(sira);
            }}
          />
        ))}
      </div>

      <ImageViewer
        resimler={hazir}
        acikIndeks={goruntuleyici.acikIndeks}
        kapat={goruntuleyici.kapat}
        indeksDegistir={goruntuleyici.indeksDegistir}
      />
    </>
  );
}

/** Tek bir küçük görsel — kendi yüklenme ve hata durumunu taşır. */
function Kucuk({ foto, kare, ac }: { foto: Foto; kare: string; ac: () => void }) {
  const { adres, yukleniyor, hata } = useKorumaliAdres(foto.yol);

  const ortak = cn(
    'relative shrink-0 overflow-hidden rounded-control border border-line bg-sunken',
    kare,
  );

  if (yukleniyor) {
    // İskelet: resmin yerini şimdiden tutuyor, yoksa fotoğraflar indikçe
    // ızgara zıplıyor ve kullanıcı yanlış kareye basıyor.
    return <span className={cn(ortak, 'animate-pulse')} aria-hidden />;
  }

  if (hata || !adres) {
    return (
      <span
        className={cn(ortak, 'grid place-items-center text-ink-3')}
        title={`${foto.baslik ?? 'Görsel'} açılamadı`}
      >
        <ImageOff size={18} />
      </span>
    );
  }

  return (
    <button
      type="button"
      onClick={ac}
      className={cn(ortak, 'group transition-transform active:scale-[0.97]')}
      aria-label={foto.baslik ? `${foto.baslik} — büyüt` : 'Fotoğrafı büyüt'}
    >
      <img
        src={adres}
        alt={foto.baslik ?? ''}
        loading="lazy"
        className="h-full w-full object-cover transition-transform group-hover:scale-[1.04]"
      />
    </button>
  );
}

/**
 * Resim OLMAYAN ekler — belge, PDF, tablo.
 *
 * Izgaradan ayrı duruyorlar: bir PDF'in küçük görselini çizmek mümkün değil ve
 * boş bir kare ızgarayı bozardı.
 */
export function DosyaSatiri({
  ad,
  altBilgi,
  eylem,
}: {
  ad: string;
  altBilgi?: string;
  eylem?: React.ReactNode;
}) {
  return (
    <div className="flex items-center gap-2.5">
      <Paperclip size={15} className="flex-none text-ink-3" />
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm text-ink">{ad}</span>
        {altBilgi && <span className="text-2xs text-ink-3">{altBilgi}</span>}
      </span>
      {eylem}
    </div>
  );
}
