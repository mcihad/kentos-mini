import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const kok = join(__dirname, '..');
const viteYapilandirmasi = readFileSync(join(kok, 'vite.config.ts'), 'utf8');

/**
 * DERLEME ÇIKTISI TEMİZLİĞİ — güvenlik kapısı yerinde mi?
 *
 * <p>
 * `emptyOutDir` kapalı olmak zorunda: çıktı doğrudan `wwwroot`a yazılıyor ve
 * orada `uploads` altında GERÇEK kullanıcı belgeleri var (özgeçmiş, talep
 * eki, etkinlik fotoğrafı). Bedeli, her derlemenin yeni karma adlı dosyalar
 * bırakması ve eskilerin hiç temizlenmemesiydi — ölçüldü: 176 dosya, 293 MB.
 * </p>
 *
 * <p>
 * Temizlik eklendi ama <b>yanlış bir yola uygulanırsa kullanıcı belgelerini
 * siler</b>. Bu yüzden silme, yolun beklenen sonla bitmesine bağlı. Test o
 * kapının kaynakta durduğunu doğruluyor: davranışı çalışma anında sınamak
 * gerçekten dosya silmeyi gerektirirdi.
 * </p>
 */
describe('derleme çıktısı', () => {
  it('outDir boşaltılmaz — uploads orada', () => {
    expect(viteYapilandirmasi).toMatch(/emptyOutDir:\s*false/);
  });

  it('yalnızca varlık klasörü siliniyor ve yol denetleniyor', () => {
    expect(viteYapilandirmasi).toContain('eskiVarliklariTemizle');
    // Silme çağrısı, beklenen yol kontrolünün ARKASINDA olmalı.
    const kapi = viteYapilandirmasi.indexOf('endsWith(`wwwroot/${VARLIK_DIZINI}`)');
    const silme = viteYapilandirmasi.indexOf('rmSync(');
    expect(kapi).toBeGreaterThan(-1);
    expect(silme).toBeGreaterThan(kapi);
  });

  it('varlık klasörü ile assetsDir aynı', () => {
    const sabit = viteYapilandirmasi.match(/const VARLIK_DIZINI = '([^']+)'/)?.[1];
    const ayar = viteYapilandirmasi.match(/assetsDir:\s*'([^']+)'/)?.[1];
    expect(sabit).toBe(ayar);
  });
});
