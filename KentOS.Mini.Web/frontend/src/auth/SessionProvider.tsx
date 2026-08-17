import { useQueryClient } from '@tanstack/react-query';
import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { webJetonuSil } from '../notifications/fcm';
import { api, tokenStore, onSessionExpired } from '../data/client';

export type Me = {
  id: number;
  kullaniciAdi: string;
  ad?: string | null;
  soyad?: string | null;
  tamAd: string;
  unvan?: string | null;
  eposta?: string | null;
  birimId?: number | null;
  birimAd?: string | null;
  roller: string[];
  /**
   * Gizli etkinlik OLUŞTURABİLİR mi.
   *
   * Görünürlük DEĞİL: başkalarının gizli etkinliklerini açmaz, yalnızca
   * formdaki "gizli" kutusunun görünüp görünmeyeceğini belirler. Sunucu aynı
   * kuralı ayrıca zorlar.
   */
  gizliEtkinlikEkleyebilir: boolean;
  /** Dosya gönderimi başlatabilir mi (almak yetki istemez). */
  dosyaGonderebilir: boolean;
  /** Sunucudaki politikaların istemci karşılığı — menüleri şekillendirir. */
  yetkiler: string[];
  /**
   * Rollerden çözülmüş İZİNLER — `ajanda.sil` gibi tek tek eylemler.
   *
   * Eski sunucu sürümünden gelen yanıtta olmayabilir; o yüzden isteğe bağlı.
   * `hasPermission` yokluğunda `false` döner: izni olmayan kullanıcıya çalışmayan
   * düğme göstermektense göstermemek doğru.
   */
  izinler?: string[];
};

type SessionContextValue = {
  me: Me | null;
  ready: boolean;
  signIn: (kullaniciAdi: string, parola: string) => Promise<void>;
  signOut: () => Promise<void>;
  hasPolicy: (politika: string) => boolean;

  /**
   * İnce taneli izin denetimi — `hasPermission('ajanda.sil')`.
   *
   * `hasPolicy` beş politikadan ibaret ve "ajanda modülüne girebilir mi"
   * diyor; bu ise tek tek eylemleri söylüyor. Düğmeleri buna göre gizleriz.
   *
   * Yetkinin kaynağı yine SUNUCU: her uç kendi iznini ayrıca denetliyor.
   */
  hasPermission: (izin: string | string[]) => boolean;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [me, setMe] = useState<Me | null>(null);
  const [ready, setReady] = useState(false);
  const queryClient = useQueryClient();

  const clearLocal = useCallback(() => {
    tokenStore.clear();
    setMe(null);
  }, []);

  /**
   * Önbellek, korumalı ekranlar ÇÖZÜLDÜKTEN sonra temizlenir.
   *
   * Ortak bilgisayarda temizlik zorunlu: önbellekte önceki kullanıcının gizli
   * etkinlikleri kalırsa bir sonraki kullanıcı onları görür.
   *
   * Ama `queryClient.clear()` doğrudan `clearLocal` içinde çağrılınca ekran
   * HÂLÂ MONTELİ oluyor: React `setMe(null)`'ı bu geri çağrı bittikten sonra
   * işliyor, `clear()` ise hemen çalışıp o an bağlı sorguları YENİDEN
   * ÇAĞIRIYOR. Jeton çoktan silindiği için istek 401 dönüyor ve kullanıcı
   * çıkış yaparken "Etkinlikler yüklenemedi" görüyordu.
   *
   * Etki (effect) commit sonrası çalışır — yani `ProtectedRoute` kullanıcıyı
   * giriş ekranına aldıktan sonra. O noktada bağlı sorgu kalmıyor.
   */
  useEffect(() => {
    if (me !== null) return;
    // Uçuştaki istekleri de iptal et: yanıtları geldiğinde artık kimse
    // beklemiyor ama 401 işleyicisi gereksiz yere tetikleniyordu.
    queryClient.cancelQueries();
    queryClient.clear();
  }, [me, queryClient]);

  useEffect(() => {
    onSessionExpired(clearLocal);
  }, [clearLocal]);

  // Açılışta saklı jeton varsa kimliği doğrula.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      const token = tokenStore.read();
      if (tokenStore.isExpired(token)) {
        tokenStore.clear();
        if (!cancelled) setReady(true);
        return;
      }
      try {
        const user = await api.get<Me>('/oturum/ben');
        if (!cancelled) setMe(user);
      } catch {
        tokenStore.clear();
      } finally {
        if (!cancelled) setReady(true);
      }
    })();
    return () => { cancelled = true; };
  }, []);

  const signIn = useCallback(async (username: string, password: string) => {
    const tokenResponse = await api.post<{ jeton: string; gecerlilikSonu: string }>(
      '/oturum/giris',
      { kullaniciAdi: username, parola: password },
    );
    tokenStore.write(tokenResponse);
    const user = await api.get<Me>('/oturum/ben');
    setMe(user);
  }, []);

  const signOut = useCallback(async () => {
    // Web push jetonunu sunucudan da sil: ortak bilgisayarda bir sonraki
    // kullanıcı önceki kullanıcının bildirimlerini almasın.
    try {
      await webJetonuSil();
    } catch {
      // Bildirim hiç kurulmamış olabilir; çıkışı engellemez.
    }

    // Denetim kaydı: "kim ne zaman çıktı" sorusunun cevabı. JWT'nin iptal
    // listesi yok, dolayısıyla bu çağrı jetonu geçersiz KILMAZ — yalnızca
    // kaydı bırakır. Başarısız olursa çıkış yine de tamamlanır.
    try {
      await api.post('/oturum/cikis');
    } catch {
      // Ağ yoksa bile kullanıcı çıkabilmeli.
    }
    clearLocal();
  }, [clearLocal]);

  const hasPolicy = useCallback(
    (politika: string) => me?.yetkiler.includes(politika) ?? false,
    [me],
  );

  /**
   * İzin denetimi.
   *
   * `yetkiler`den farklı olarak burada BOŞ LİSTE "hiçbir izni yok" demektir,
   * "sunucu söylemedi" değil: izin listesi v2'nin `oturum/ben` ucundan her
   * zaman geliyor. İyimser davranmak, izni olmayan kullanıcıya çalışmayan
   * düğmeler göstermek olurdu.
   */
  const hasPermission = useCallback(
    (izin: string | string[]) => {
      // ÇOKLU İZİN = VEYA. Sunucudaki `[Izin(...)]` süzgeci de böyle çalışır;
      // iki tarafın farklı yorumlaması, arayüzün gösterdiği ekranın 403
      // vermesi (ya da tersi) demek. Örnek: ajanda ekranı hem tam görüntüleme
      // hem de yalnızca-basın izniyle açılır.
      const list = me?.izinler;
      if (!list) return false;
      return Array.isArray(izin)
        ? izin.some((i) => list.includes(i))
        : list.includes(izin);
    },
    [me],
  );

  return (
    <SessionContext.Provider value={{ me, ready, signIn, signOut, hasPolicy, hasPermission }}>
      {children}
    </SessionContext.Provider>
  );
}

export function useSession() {
  const value = useContext(SessionContext);
  if (!value) throw new Error('useSession, SessionProvider içinde kullanılmalı.');
  return value;
}
