import { useEffect, useState } from 'react';
import { tokenStore } from './client';

/**
 * KORUMALI GÖRSELLER — kimlik denetimli uçtan gelen resimleri gösterir.
 *
 * <p>
 * İş takip ve vatandaş bildirimi ekleri <code>StorageArea.Private</code>
 * altında duruyor ve yalnızca <code>Authorization</code> başlığı taşıyan bir
 * istekle okunabiliyor. Tarayıcı ise <code>&lt;img src&gt;</code> isteğine
 * başlık eklemiyor: bir vatandaşın gönderdiği fotoğrafı doğrudan
 * <code>src</code> ile göstermek 401 döndürür.
 * </p>
 *
 * <p>
 * Bu yüzden içerik <code>fetch</code> ile alınıp <code>blob:</code> adresine
 * çevriliyor. <b>Alternatifi imzalı adres üretmekti</b> — süreli bir jeton
 * içeren URL. Onu seçmedik: bir kez üretilen imzalı adres kopyalanabiliyor ve
 * süresi dolana kadar kimlik denetimi olmadan çalışıyor; vatandaşın adresini
 * ve fotoğrafını taşıyan bir kayıt için bu, korumayı gevşetmek olurdu.
 * </p>
 *
 * <p>
 * <b>Önbellek modül düzeyinde.</b> Aynı resim hem küçük görselde hem
 * büyütülmüş hâlde çiziliyor; iki kez indirmek gereksiz. Anahtar uç adresi,
 * değer ise çözülmüş <code>blob:</code> adresi.
 * </p>
 */

/** En fazla kaç blob adresi bellekte tutulur. */
const TAVAN = 120;

const onbellek = new Map<string, Promise<string>>();

/**
 * Korumalı bir adresi <code>blob:</code> adresine çevirir.
 *
 * <p>
 * Aynı adres için ikinci çağrı ağa çıkmaz; ilk çağrının <b>sözü</b> paylaşılır
 * — böylece yan yana çizilen on küçük görsel tek istek atıyor.
 * </p>
 */
export function korumaliAdres(yol: string): Promise<string> {
  const varsa = onbellek.get(yol);
  if (varsa) return varsa;

  const soz = (async () => {
    const jeton = tokenStore.read();
    const yanit = await fetch(yol, {
      headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : undefined,
    });

    if (!yanit.ok) throw new Error(`Görsel alınamadı (${yanit.status})`);
    return URL.createObjectURL(await yanit.blob());
  })();

  /*
    BAŞARISIZ SÖZ ÖNBELLEKTE KALMAZ.

    Kalsaydı geçici bir ağ hatası o resmi oturum boyunca kırık bırakırdı:
    kullanıcı sayfayı yenilemeden bir daha göremezdi.
  */
  soz.catch(() => onbellek.delete(yol));

  onbellek.set(yol, soz);
  budala();
  return soz;
}

/** Önbellek tavanı aşıldığında en eskiyi bırakır ve adresini serbest bırakır. */
function budala() {
  while (onbellek.size > TAVAN) {
    const enEski = onbellek.keys().next().value;
    if (enEski === undefined) return;

    const soz = onbellek.get(enEski);
    onbellek.delete(enEski);

    // `revokeObjectURL` çağrılmazsa blob bellekte kalır; uzun bir oturumda
    // yüzlerce fotoğraf sekmeyi şişirir.
    soz?.then((adres) => URL.revokeObjectURL(adres)).catch(() => {});
  }
}

/** Oturum kapanınca bütün blob adresleri bırakılır. */
export function korumaliOnbellegiBosalt() {
  for (const soz of onbellek.values()) {
    soz.then((adres) => URL.revokeObjectURL(adres)).catch(() => {});
  }
  onbellek.clear();
}

export type KorumaliDurum = { adres: string | null; yukleniyor: boolean; hata: boolean };

/** Tek bir korumalı görsel. */
export function useKorumaliAdres(yol?: string | null): KorumaliDurum {
  const [durum, setDurum] = useState<KorumaliDurum>({
    adres: null,
    yukleniyor: !!yol,
    hata: false,
  });

  useEffect(() => {
    if (!yol) {
      setDurum({ adres: null, yukleniyor: false, hata: false });
      return;
    }

    let gecerli = true;
    setDurum({ adres: null, yukleniyor: true, hata: false });

    korumaliAdres(yol)
      .then((adres) => gecerli && setDurum({ adres, yukleniyor: false, hata: false }))
      .catch(() => gecerli && setDurum({ adres: null, yukleniyor: false, hata: true }));

    return () => {
      gecerli = false;
    };
  }, [yol]);

  return durum;
}

/**
 * Birden çok korumalı görsel — sırası korunur.
 *
 * <p>
 * Görüntüleyiciye verilecek listeyi hazırlamak için: hepsi çözülene kadar
 * <code>null</code> taşıyan bir dizi döner, böylece çağıran hangi resmin
 * hazır olduğunu bilir ve boş bir kareyi büyütmez.
 * </p>
 */
export function useKorumaliAdresler(yollar: string[]): (string | null)[] {
  // Dizi her renderda yeni bir referans olur; etki bağımlılığı İÇERİĞE
  // bakmalı, yoksa sonsuz döngü kurulur.
  const anahtar = yollar.join('|');
  const [adresler, setAdresler] = useState<(string | null)[]>(() => yollar.map(() => null));

  useEffect(() => {
    let gecerli = true;
    const liste = anahtar ? anahtar.split('|') : [];
    setAdresler(liste.map(() => null));

    liste.forEach((yol, i) => {
      korumaliAdres(yol)
        .then((adres) => {
          if (!gecerli) return;
          setAdresler((eski) => {
            const yeni = [...eski];
            yeni[i] = adres;
            return yeni;
          });
        })
        .catch(() => {});
    });

    return () => {
      gecerli = false;
    };
  }, [anahtar]);

  return adresler;
}
