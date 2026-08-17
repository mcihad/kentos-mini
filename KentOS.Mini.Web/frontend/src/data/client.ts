/**
 * Sunucu istemcisi. Yalnızca `/api/v2` kullanılır — `/api/XxxApi` (v1) mobil
 * uygulamanın sözleşmesidir, buradan çağrılmaz.
 */

const TOKEN_KEY = 'sv-jetonu';

export type StoredToken = { jeton: string; gecerlilikSonu: string };

export const tokenStore = {
  read(): StoredToken | null {
    try {
      const ham = localStorage.getItem(TOKEN_KEY);
      return ham ? (JSON.parse(ham) as StoredToken) : null;
    } catch {
      return null;
    }
  },
  write(j: StoredToken) {
    localStorage.setItem(TOKEN_KEY, JSON.stringify(j));
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY);
  },
  /** Süresi dolmuş mu? 60 sn erken davranır — sınırda çağrı yapmayalım. */
  isExpired(j: StoredToken | null): boolean {
    if (!j) return true;
    const son = new Date(j.gecerlilikSonu).getTime();
    return Number.isNaN(son) || son - Date.now() < 60_000;
  },
};

/** Sunucunun RFC 7807 hata gövdesi. */
export type ErrorResponse = {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  hatalar?: Record<string, string[]>;
  izKimligi?: string;
};

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly body: ErrorResponse | null,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /** Forma dağıtılacak alan hataları. */
  get fieldErrors(): Record<string, string[]> {
    return this.body?.hatalar ?? {};
  }
}

/** 401 alındığında çağrılır — oturumu kapatıp giriş ekranına götürür. */
let sessionExpiredHandler: (() => void) | null = null;
export function onSessionExpired(f: () => void) {
  sessionExpiredHandler = f;
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  signal?: AbortSignal;
};

/**
 * Hata gövdesini okur; okunamazsa `null`.
 *
 * 401'de gövde İKİ türlü gelir: uç kendi cevabını yazdıysa RFC 7807 JSON,
 * kimlik doğrulama katmanı meydan okuduysa bomboş. İkincisinde `JSON.parse`
 * patlar ve gerçek hatanın yerine ayrıştırma hatası geçerdi.
 */
async function parseErrorBody(response: Response): Promise<ErrorResponse | null> {
  try {
    const metin = await response.text();
    return metin ? (JSON.parse(metin) as ErrorResponse) : null;
  } catch {
    return null;
  }
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, signal } = options;
  const token = tokenStore.read();

  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token.jeton}`;

  const response = await fetch(`/api/v2${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    signal,
  });

  if (response.status === 401) {
    const h = await parseErrorBody(response);

    /*
     * Her 401 "oturum düştü" DEĞİLDİR.
     *
     * Giriş isteği jetonsuz gider ve kimlik hatalıysa sunucu yine 401 döner:
     * gövdesinde "Kullanıcı adı veya şifre hatalı" ya da hesap kilitlendiyse
     * onun gerekçesi yazılı. Bu dal gövdeyi atıp yerine "Oturum süresi doldu."
     * yazıyordu — kullanıcı yanlış şifre yazdığını anlamıyor, kilitlenen hesap
     * da sessizce kalıyordu (kullanıcı denemeye devam ettikçe kilit uzuyor).
     *
     * Ayrım şu: JETON GÖNDERMEDİĞİMİZ bir istekte 401, düşmüş bir oturumdan
     * gelemez — reddedilen şey isteğin kendisidir. Oturumu da temizlemiyoruz;
     * temizlenecek bir oturum yok.
     */
    if (!token || path.startsWith('/oturum/giris')) {
      throw new ApiError(401, h, h?.detail || h?.title || 'Giriş yapılamadı.');
    }

    // Sunucudan jeton silmeyi DENEMEYİZ — o çağrı da 401 alırdı.
    tokenStore.clear();
    sessionExpiredHandler?.();
    // Süresi dolan jetonda gövde boş gelir (kimlik doğrulama meydan okuması).
    throw new ApiError(401, h, h?.detail || 'Oturum süresi doldu.');
  }

  if (response.status === 204) return undefined as T;

  const metin = await response.text();
  const data = metin ? JSON.parse(metin) : null;

  if (!response.ok) {
    const h = data as ErrorResponse | null;
    throw new ApiError(
      response.status,
      h,
      h?.detail || h?.title || `İstek başarısız (${response.status})`,
    );
  }

  return data as T;
}

export const api = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal }),
  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),
  patch: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PATCH', body }),
  delete: <T>(path: string, body?: unknown) => request<T>(path, { method: 'DELETE', body }),
};

/**
 * Sunucunun sayfalı yanıt zarfı.
 *
 * v2'de HİÇBİR liste ucu sınırsız veri döndürmez; hepsi bu zarfla gelir.
 */
export type PagedResult<T> = {
  veriler: T[];
  sayfa: number;
  boyut: number;
  toplam: number;
  toplamSayfa: number;
  oncekiVar: boolean;
  sonrakiVar: boolean;
};

/** Sayfalı liste isteğinin ortak parametreleri. */
export type PageParams = {
  sayfa?: number;
  boyut?: number;
  ara?: string;
  sirala?: string;
  azalan?: boolean;
};

/**
 * Sorgu dizesi kurar; `undefined`, `null` ve boş metinleri ATLAR.
 *
 * Boş bir `ara=` göndermek sunucuda "boş metin ara" değil, "arama yok"
 * anlamına geliyor ama sorgu anahtarını değiştirdiği için TanStack Query
 * gereksiz bir yeniden çekim yapardı.
 */
export function queryString(params: Record<string, unknown>): string {
  const p = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    if (Array.isArray(value)) {
      for (const v of value) p.append(key, String(v));
    } else {
      p.set(key, String(value));
    }
  }

  const metin = p.toString();
  return metin ? `?${metin}` : '';
}

/** Boş bir sayfalı sonuç — yükleme sırasında `data` yerine kullanılır. */
export function emptyPage<T>(): PagedResult<T> {
  return {
    veriler: [],
    sayfa: 1,
    boyut: 50,
    toplam: 0,
    toplamSayfa: 0,
    oncekiVar: false,
    sonrakiVar: false,
  };
}
