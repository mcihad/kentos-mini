import { afterEach, describe, expect, it, vi } from 'vitest';

/**
 * PWA KURULUM DURUM MAKİNESİ.
 *
 * <p>
 * Buradaki testlerin hepsi tek bir üretim hatasının etrafında: kullanıcı
 * uygulamayı telefonuna kuruyor, sonra kaldırıyor ve <b>kurulum düğmesi bir
 * daha hiç görünmüyordu</b>. Sebebi tek satırlık kalıcı bir işaretti —
 * yazılıyor ama hiçbir koşulda silinmiyordu.
 * </p>
 *
 * <p>
 * Modül durumu MODÜL DÜZEYİNDE tutuluyor (bekleyen istem, dinleyiciler), bu
 * yüzden her senaryo `resetModules` ile taze bir kopya yüklüyor.
 * </p>
 */

type Kurulum = typeof import('../src/pwa/install');

async function moduluYukle(): Promise<Kurulum> {
  vi.resetModules();
  const m = await import('../src/pwa/install');
  m.startInstallListener();
  return m;
}

/** Tarayıcının gönderdiği kurulum istemini taklit eder. */
function istemGonder(sonuc: 'accepted' | 'dismissed' = 'accepted') {
  const olay = new Event('beforeinstallprompt') as Event & {
    prompt: () => Promise<void>;
    userChoice: Promise<{ outcome: string }>;
  };
  olay.prompt = vi.fn(async () => undefined);
  olay.userChoice = Promise.resolve({ outcome: sonuc });
  window.dispatchEvent(olay);
  return olay;
}

afterEach(() => {
  vi.useRealTimers();
});

describe('kurulum durumu', () => {
  it('istem gelmeden de kurulum kapısı açık kalır', async () => {
    const k = await moduluYukle();
    const d = k.installState();

    expect(d.kurulu).toBe(false);
    expect(d.istemVar).toBe(false);
    // iOS'ta istem HİÇ gelmiyor, Chrome'da istem tek kullanımlık. İkisinde de
    // kurulum mümkün; kapıyı isteme bağlamak kurulum yolunu yok ediyordu.
    expect(d.kurulabilir).toBe(true);
  });

  it('istem gelince tek dokunuşluk kuruluma döner', async () => {
    const k = await moduluYukle();
    istemGonder();

    expect(k.installState().istemVar).toBe(true);
  });

  it('kurulum sonrası işaret kalıcı: sekmede de kurulu sayılır', async () => {
    const k = await moduluYukle();
    window.dispatchEvent(new Event('appinstalled'));

    const d = k.installState();
    expect(d.kurulu).toBe(true);
    expect(d.kurulabilir).toBe(false);
    // Pencere kipi hâlâ sekme: bilgi yalnızca işaretten geliyor.
    expect(d.isaretten).toBe(true);
  });

  it('KALDIRILINCA kurulum yeniden görünür (asıl hata)', async () => {
    const k = await moduluYukle();

    window.dispatchEvent(new Event('appinstalled'));
    expect(k.installState().kurulabilir).toBe(false);

    // Kullanıcı uygulamayı telefondan sildi ve sayfayı yeniledi: tarayıcı
    // istemi YALNIZCA kurulu değilken gönderir, yani bu olay "kaldırıldı"
    // demenin tek güvenilir yolu.
    istemGonder();

    const d = k.installState();
    expect(d.kurulu).toBe(false);
    expect(d.kurulabilir).toBe(true);
    expect(d.istemVar).toBe(true);
    expect(localStorage.getItem('sv-pwa-kurulu')).toBeNull();
  });

  it('kullanıcı "kaldırdım" dediğinde de kapı açılır', async () => {
    const k = await moduluYukle();
    window.dispatchEvent(new Event('appinstalled'));

    // Tarayıcı istemi geciktirebiliyor; elde çalışan bir kapı olmalı.
    k.clearInstalledFlag();

    expect(k.installState().kurulu).toBe(false);
    expect(k.installState().kurulabilir).toBe(true);
  });

  it('erteleme SÜRELİ; kalıcı gizleme yok', async () => {
    const k = await moduluYukle();

    k.snoozeInstall();
    expect(k.installState().ertelendi).toBe(true);

    // 15 gün sonra kart kendiliğinden geri gelir.
    const damga = Number(localStorage.getItem('sv-pwa-ertelendi'));
    vi.spyOn(Date, 'now').mockReturnValue(damga + 15 * 24 * 60 * 60 * 1000);
    // Durum önbelleğe alınıyor; yeniden hesaplansın diye bir olay tetikle.
    window.dispatchEvent(new Event('focus'));

    expect(k.installState().ertelendi).toBe(false);
    vi.restoreAllMocks();
  });

  it('kaldırılma ertelemeyi de siler', async () => {
    const k = await moduluYukle();

    window.dispatchEvent(new Event('appinstalled'));
    k.snoozeInstall();
    istemGonder();

    // "Sonra" kararı KURULUYKEN verilmişti; kaldırdıktan sonra geçersiz.
    expect(k.installState().ertelendi).toBe(false);
    expect(localStorage.getItem('sv-pwa-ertelendi')).toBeNull();
  });

  it('eski KALICI gizleme anahtarı göç eder', async () => {
    localStorage.setItem('sv-kurulum-gizlendi', '1');
    await moduluYukle();

    // Kartın bir daha asla dönmemesinin ikinci sebebi buydu.
    expect(localStorage.getItem('sv-kurulum-gizlendi')).toBeNull();
  });

  it('istem tek kullanımlık: ikinci çağrı yeniden prompt açmaz', async () => {
    const k = await moduluYukle();
    const olay = istemGonder('accepted');

    expect(await k.promptInstall()).toBe('kuruldu');
    expect(olay.prompt).toHaveBeenCalledTimes(1);
    expect(await k.promptInstall()).toBe('yok');
    expect(olay.prompt).toHaveBeenCalledTimes(1);
  });

  it('vazgeçilen kurulum işaret yazmaz', async () => {
    const k = await moduluYukle();
    istemGonder('dismissed');

    expect(await k.promptInstall()).toBe('vazgecildi');
    expect(k.installState().kurulu).toBe(false);
    // İstem tükendi ama kurulum yolu (elle talimat) hâlâ açık.
    expect(k.installState().kurulabilir).toBe(true);
  });

  it('abonelere yalnızca gerçek değişimde haber verilir', async () => {
    const k = await moduluYukle();
    const dinleyici = vi.fn();
    k.onInstallStateChange(dinleyici);

    window.dispatchEvent(new Event('focus'));
    expect(dinleyici).not.toHaveBeenCalled();

    istemGonder();
    expect(dinleyici).toHaveBeenCalledTimes(1);
  });
});
