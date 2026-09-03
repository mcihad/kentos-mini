/**
 * DOKUNSAL GERİ BİLDİRİM — native hissin en ucuz, en çok hissedilen parçası.
 *
 * <p>
 * Bir düğmeye basınca telefonun hafifçe titremesi, dokunuşun "kaydedildiğini"
 * ekrana bakmadan söyler. Web'de tek yol <c>navigator.vibrate</c>: Android
 * Chrome/Firefox destekliyor, <b>iOS Safari desteklemiyor</b> ve muhtemelen
 * desteklemeyecek. Bu yüzden titreşim bir <b>ek</b>dir, tek başına bilgi
 * taşımaz — her titreşimin yanında görsel bir karşılık (renk değişimi,
 * bildirim şeridi, satırın kayması) da olmalı.
 * </p>
 *
 * <p>
 * Desenler kısa tutuldu. 10ms'nin altı çoğu cihazda hissedilmiyor, 50ms'nin
 * üstü "telefon çalıyor" gibi duruyor; aradaki bant dokunuşu onaylamaya
 * yetiyor.
 * </p>
 */

/** Desen adları — çağıran yer milisaniye düşünmez, NİYET söyler. */
export type HapticPattern =
  /** Seçim değişti: sekme, çip, liste satırı. En hafifi. */
  | 'secim'
  /** Eylem başarıyla bitti: kaydedildi, aşama kapandı. */
  | 'basari'
  /** Uyarı ya da geri alınamaz bir eşiğe gelindi. */
  | 'uyari'
  /** Hata: istek düştü, doğrulama tutmadı. */
  | 'hata'
  /** Kaydırma eşiği aşıldı — eylem "kilitlendi". */
  | 'esik';

const DESENLER: Record<HapticPattern, number | number[]> = {
  secim: 8,
  basari: 18,
  uyari: [14, 40, 14],
  hata: [22, 45, 22],
  esik: 12,
};

/**
 * Titreşim isteği gönderir.
 *
 * <p>
 * Sessizce başarısız olur: destek yoksa, kullanıcı sekmeden ayrıldıysa ya da
 * tarayıcı kullanıcı etkileşimi görmediği için isteği reddediyorsa hiçbir şey
 * olmaz. Titreşim asla bir akışın ön koşulu değildir.
 * </p>
 *
 * <p>
 * <b>Hareket azaltma tercihine uyar:</b> <c>prefers-reduced-motion</c> açan
 * kullanıcı çoğu zaman duyusal uyarıyı da azaltmak istiyor.
 * </p>
 */
export function haptic(desen: HapticPattern = 'secim'): void {
  try {
    if (typeof navigator === 'undefined' || typeof navigator.vibrate !== 'function') return;
    if (typeof window !== 'undefined'
      && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return;

    navigator.vibrate(DESENLER[desen]);
  } catch {
    /* Titreşim yüzünden hiçbir akış bozulmaz. */
  }
}
