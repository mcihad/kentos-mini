import { tokenStore, queryString } from './client';

/**
 * Kimlik doğrulamalı dosya indirme.
 *
 * <p>
 * Doğrudan `window.open('/api/v2/...')` ÇALIŞMAZ: tarayıcı o isteğe
 * `Authorization` başlığını eklemez ve sunucu 401 döner. Bu yüzden dosya
 * `fetch` ile çekilip geçici bir `blob:` bağlantısı üzerinden indirilir.
 * </p>
 *
 * <p>
 * Jetonu sorgu dizesine koymak da bir seçenekti; yapılmadı — jeton erişim
 * günlüklerine, vekil sunucu günlüklerine ve tarayıcı geçmişine düşerdi.
 * </p>
 */
export async function download(
  path: string,
  params: Record<string, unknown> = {},
): Promise<void> {
  const token = tokenStore.read();

  const response = await fetch(`/api/v2${path}${queryString(params)}`, {
    headers: token ? { Authorization: `Bearer ${token.jeton}` } : {},
  });

  if (!response.ok) {
    throw new Error(`Dosya indirilemedi (${response.status}).`);
  }

  // Dosya adını sunucunun Content-Disposition başlığından al; yoksa uca göre üret.
  const filenameFromHeader = parseFilename(response.headers.get('Content-Disposition'));
  const blob = await response.blob();

  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filenameFromHeader ?? path.split('/').pop() ?? 'indirilen';
  document.body.appendChild(link);
  link.click();
  link.remove();

  // Nesne URL'i serbest bırakılmazsa sekme kapanana kadar bellekte kalır.
  URL.revokeObjectURL(url);
}

/**
 * `Content-Disposition` başlığından dosya adını ayıklar.
 *
 * `filename*=UTF-8''...` biçimi önceliklidir — Türkçe karakterli adlar
 * yalnızca o biçimde doğru gelir.
 */
function parseFilename(header: string | null): string | null {
  if (!header) return null;

  const genisletilmis = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (genisletilmis) {
    try {
      return decodeURIComponent(genisletilmis[1]);
    } catch {
      // Bozuk kodlama — sade biçime düş.
    }
  }

  const sade = /filename="?([^";]+)"?/i.exec(header);
  return sade ? sade[1] : null;
}
