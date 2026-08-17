import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, request, tokenStore, onSessionExpired } from '../src/data/client';

/**
 * 401'in İKİ anlamı var ve karıştırılırsa kullanıcı yanlış şeyi düzeltmeye
 * çalışır: yanlış şifre yazan kişiye "oturum süresi doldu" demek, şifresini
 * gözden geçirmesi gereken kişiye sayfayı yenilemesini söylemektir.
 */
describe('istek(): 401 ayrımı', () => {
  afterEach(() => {
    onSessionExpired(() => undefined);
    vi.unstubAllGlobals();
    localStorage.clear();
  });

  function yanitVer(durum: number, govde: unknown) {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        new Response(govde === null ? '' : JSON.stringify(govde), {
          status: durum,
          headers: { 'Content-Type': 'application/json' },
        }),
      ),
    );
  }

  it('giriş reddedildiğinde sunucunun gerekçesini gösterir, oturumu düşürmez', async () => {
    const kanca = vi.fn();
    onSessionExpired(kanca);
    yanitVer(401, {
      type: 'https://ornek.test/hatalar/kimlik',
      title: 'Giriş yapılamadı',
      status: 401,
      detail: 'Kullanıcı adı veya şifre hatalı',
    });

    const hata = await request('/oturum/giris', {
      yontem: 'POST',
      govde: { kullaniciAdi: 'admin', parola: 'yanlis' },
    }).catch((h) => h);

    expect(hata).toBeInstanceOf(ApiError);
    expect(hata.message).toBe('Kullanıcı adı veya şifre hatalı');
    expect(kanca).not.toHaveBeenCalled();
  });

  it('kilitlenen hesabın gerekçesi de kullanıcıya ulaşır', async () => {
    yanitVer(401, {
      title: 'Giriş yapılamadı',
      status: 401,
      detail: 'Çok fazla hatalı deneme nedeniyle hesabınız geçici olarak kilitlendi.',
    });

    const hata = await request('/oturum/giris', { yontem: 'POST', govde: {} }).catch((h) => h);

    expect(hata.message).toContain('kilitlendi');
  });

  it('jetonlu istekte 401 oturumu düşürür', async () => {
    const kanca = vi.fn();
    onSessionExpired(kanca);
    tokenStore.write({ jeton: 'eski', gecerlilikSonu: '2030-01-01T00:00:00' });
    // Kimlik doğrulama katmanının meydan okuması: gövde BOŞ gelir.
    yanitVer(401, null);

    const hata = await request('/etkinlik').catch((h) => h);

    expect(hata.message).toBe('Oturum süresi doldu.');
    expect(kanca).toHaveBeenCalledTimes(1);
    expect(tokenStore.read()).toBeNull();
  });
});
