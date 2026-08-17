/**
 * TanStack Query anahtar fabrikası.
 *
 * Tek yerden üretilmesi, geçersiz kılma (invalidate) sırasında ön eki
 * kaçırmamak için: takvimde bir etkinlik değiştiğinde AÇIK pencerelerin
 * tamamı tazelenmeli, yoksa kullanıcı eski konumu görmeye devam eder.
 *
 * NOT: Dizi değerlerinin İÇERİĞİ (ör. `'oturum'`, `'etkinlik'`, `'talep'`)
 * kasıtlı olarak Türkçe bırakıldı. Bu değerler sunucuya gitmiyor; sadece
 * TanStack Query önbelleğinin dahili anahtarları. Ama bazı ekranlar aynı
 * önekleri (`['etkinlik']`, `['talep', 'notlar', id]` gibi) bu fabrikayı
 * kullanmadan elle de yazıyor — buradaki dize değiştirilirse o ekranlardaki
 * `invalidateQueries` çağrıları eşleşmeyi kaybeder ve önbellek bayat kalır.
 */
export const queryKeys = {
  session: {
    me: () => ['oturum', 'ben'] as const,
  },
  event: {
    all: () => ['etkinlik'] as const,
    window: (start: string, end: string, filter?: string) =>
      ['etkinlik', 'pencere', start, end, filter ?? ''] as const,
    detail: (id: number) => ['etkinlik', 'detay', id] as const,
    notes: (id: number) => ['etkinlik', 'notlar', id] as const,
    activity: (id: number) => ['etkinlik', 'olaylar', id] as const,
    series: (id: number) => ['etkinlik', 'seri', id] as const,
  },
  request: {
    all: () => ['talep'] as const,
    list: (filter?: string) => ['talep', 'liste', filter ?? ''] as const,
    detail: (id: number) => ['talep', 'detay', id] as const,
  },
  settings: {
    statuses: () => ['ayar', 'durumlar'] as const,
    types: () => ['ayar', 'tipler'] as const,
    units: () => ['ayar', 'birimler'] as const,
    unitUsers: () => ['ayar', 'birim-kullanicilari'] as const,
    participantUnits: () => ['ayar', 'katilimci-birimler'] as const,
  },
} as const;
