import { startOfDay, localToServer } from '../../data/time';

/**
 * Panoların ORTAK zaman aralığı.
 *
 * <p>
 * Hesap bir dönem `Statistics.tsx` içinde yaşıyordu; merkez ayrı ekranlara
 * bölününce iki kopya çıkacaktı. İki kopya, "bu yıl"ın bir panoda 1 Ocak'tan
 * bir başkasında 365 gün öncesinden başlaması demekti.
 * </p>
 */
export type Aralik = 'buYil' | 'son12Ay' | 'tumZamanlar';

export const ARALIK_ETIKETLERI: Record<Aralik, string> = {
  buYil: 'Bu yıl',
  son12Ay: 'Son 12 ay',
  tumZamanlar: 'Tümü',
};

export function aralikHesapla(a: Aralik): [string | undefined, string | undefined] {
  const bugun = startOfDay(new Date());

  if (a === 'tumZamanlar') return [undefined, undefined];

  if (a === 'buYil') {
    return [localToServer(new Date(bugun.getFullYear(), 0, 1)), localToServer(bugun)];
  }

  /*
    "Son 12 ay" = SON 12 TAKVİM AYI, 365 gün değil.

    Eskiden bugünden 12 ay geriye gidiliyordu; aylık grafik o zaman iki
    YARIM ay (başta ve sonda) görüyor ve 12 yerine 13 sütun çiziyordu.
    Ölçüldü: "Son 12 ay" seçiliyken eksende Ağu 25 … Ağu 26 = 13 sütun.
    Ayın 1'inden başlamak hem grafiği hem "geçen ay ne olmuş" karşılaştırmasını
    doğru kılıyor.
  */
  const bas = new Date(bugun.getFullYear(), bugun.getMonth() - 11, 1);
  return [localToServer(bas), localToServer(bugun)];
}
