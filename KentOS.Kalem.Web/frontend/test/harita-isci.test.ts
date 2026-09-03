import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

/**
 * MAPLIBRE İŞÇİSİ — haritanın boş kalmasının bekçisi.
 *
 * <p>
 * MapLibre v6 kendi işçi dosyasının adresini <code>import.meta.url</code>
 * üzerinden, "kendi modülümün yanındaki kardeş dosya" diye türetiyor. Vite
 * kütüphaneyi uygulama yığınına paketleyince o adres
 * <code>/uygulama/maplibre-gl-worker.mjs</code>'e dönüşüyor ve böyle bir
 * dosya çıktıda yok.
 * </p>
 *
 * <p>
 * <b>Arıza tamamen sessiz.</b> Ölçüldü: adres 404 dönüyor, işçi hiç ayağa
 * kalkmıyor, MapLibre'nin GeoJSON kaynağı sonsuza kadar "yükleniyor"da
 * kalıyor. Konsolda hata yok, <code>map.on('error')</code> susuyor,
 * <code>sourcedata</code> olayı hiç gelmiyor. Raster altlık ana iş
 * parçacığında çizildiği için harita <b>çalışıyor gibi</b> görünüyor —
 * sokaklar var, üzerinde tek bir iş yok. Kullanıcının bildirdiği arıza
 * buydu.
 * </p>
 *
 * <p>
 * Bu test iki şeyi kilitliyor ve ikisi de tek başına yetersiz:
 * </p>
 * <ol>
 *   <li><code>setWorkerUrl</code> çağrılıyor — MapLibre'nin kendi tahmini
 *       geçersiz kılınmalı.</li>
 *   <li>Adres <code>?worker&amp;url</code> ile alınıyor — yalnızca
 *       <code>?url</code> dosyayı kopyalar ama <b>bağımlılığını kopyalamaz</b>;
 *       işçi indirilir, ilk <code>import</code>unda 404 alır ve yine hiç
 *       cevap vermez. Bu da ölçüldü.</li>
 * </ol>
 */
describe('maplibre işçisi', () => {
  const KAYNAK = readFileSync(
    join(dirname(fileURLToPath(import.meta.url)), '..', 'src/screens/map/harita.ts'),
    'utf8',
  );

  /** Yorumlar bu kararı ANLATIYOR; aranan şey çalışan kod. */
  const kod = KAYNAK.replace(/\/\*[\s\S]*?\*\//g, '').replace(/^\s*\/\/.*$/gm, '');

  it('setWorkerUrl çağrılıyor', () => {
    expect(kod).toContain('setWorkerUrl');
  });

  it('işçi ?worker&url ile alınıyor — yalnızca ?url yetmez', () => {
    expect(kod).toMatch(/maplibre-gl-worker\.mjs\?worker&url/);

    // `?url` tek başına: işçi iner, kardeş modülü 404 alır, kaynak hiç
    // yüklenmez. Yanlış olan biçim açıkça yasaklanıyor.
    expect(kod).not.toMatch(/maplibre-gl-worker\.mjs\?url/);
  });
});
