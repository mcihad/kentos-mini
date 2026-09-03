import { tokenStore } from './client';

/**
 * Etkinlik fotoğrafı yükler.
 *
 * `api` sarmalayıcısı JSON gönderiyor; `FormData` için doğrudan `fetch`
 * kullanılıyor. **`Content-Type` ELLE VERİLMEZ** — tarayıcının `boundary`
 * eklemesi gerekiyor, elle yazılırsa sunucu gövdeyi ayrıştıramaz.
 *
 * Aynı kod hem detay ekranında hem de OLUŞTURMA formunda gerekiyordu; iki
 * kopya, `boundary` gibi bir ayrıntının birinde unutulması demekti.
 */
export async function uploadEventPhoto(
  eventId: number,
  files: File[] | FileList,
): Promise<void> {
  const list = Array.from(files);
  if (list.length === 0) return;

  const body = new FormData();
  for (const d of list) body.append('dosyalar', d);

  const token = tokenStore.read();
  const response = await fetch(`/api/v2/etkinlik/${eventId}/fotograflar`, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token.jeton}` } : {},
    body,
  });

  if (!response.ok) {
    const h = await response.json().catch(() => null);
    throw new Error(h?.detail ?? `Yükleme başarısız (${response.status})`);
  }
}

/** Sunucunun kabul ettiği türler — form da aynı sınırı uygulamalı. */
export const PHOTO_TYPES = 'image/jpeg,image/png,image/webp';

/** Sunucu sınırı: dosya başına 5 MB. */
export const PHOTO_MAX_BYTES = 5 * 1024 * 1024;
