/** Takvim görünümleri — design.md §7.8 segment kontrolü. */
export type CalendarView = 'gun' | 'hafta' | 'ay' | 'yil' | 'ajanda';

export const VIEW_LABELS: Record<CalendarView, string> = {
  gun: 'Gün',
  hafta: 'Hafta',
  ay: 'Ay',
  yil: 'Yıl',
  ajanda: 'Ajanda',
};

/** Sunucudan gelen hafif etkinlik kaydı (EtkinlikOzetDto karşılığı). */
export type CalendarEvent = {
  id: number;
  baslik: string;
  baslangic: string;
  bitis: string | null;
  tumGun: boolean;
  konum: string | null;
  tipId: number | null;
  durumId: number | null;
  statu: number;

  /**
   * Tanım adları ve RENKLERİ kayıtla birlikte gelir.
   *
   * Eski arayüz de takvimi `AjandaDurum.Renk` ile boyuyordu. Rengin sunucudan
   * gelmesi, istemcinin durum listesini ayrıca çekip eşleştirmesini gereksiz
   * kılar ve tanımı SİLİNMİŞ bir etkinliğin de doğru renkte kalmasını sağlar.
   */
  tipAd: string | null;
  tipRenk: string | null;
  durumAd: string | null;
  durumRenk: string | null;
  gizli: boolean;
  seriId: number | null;
  seriAyrik: boolean;
  resimVar: boolean;
  basinKatilsin: boolean;
};

/** Tekrar kapsamı — sunucudaki `TekrarKapsam` ile AYNI sayısal karşılıklar. */
export const RECURRENCE_SCOPE = {
  yalnizca: 0,
  bundanSonrakiler: 1,
  tumu: 2,
} as const;

export type RecurrenceScope = (typeof RECURRENCE_SCOPE)[keyof typeof RECURRENCE_SCOPE];

export const RECURRENCE_SCOPE_OPTIONS: { deger: RecurrenceScope; etiket: string; aciklama: string }[] = [
  {
    deger: RECURRENCE_SCOPE.yalnizca,
    etiket: 'Yalnızca bu etkinlik',
    aciklama: 'Seri değişmez; bu tekrar seriden ayrılır.',
  },
  {
    deger: RECURRENCE_SCOPE.bundanSonrakiler,
    etiket: 'Bu ve sonraki etkinlikler',
    aciklama: 'Seri bu tarihten itibaren ikiye bölünür.',
  },
  {
    deger: RECURRENCE_SCOPE.tumu,
    etiket: 'Tüm seri',
    aciklama: 'Bireysel düzenlenmiş tekrarlar korunur.',
  },
];

/** 30 dakikalık ızgara — varsayılan etkinlik süresi de bu. */
export const SLOT_MINUTES = 30;
export const DEFAULT_DURATION_MINUTES = 30;
