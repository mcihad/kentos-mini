/**
 * Sunucu tiplerinin okunabilir takma adları.
 *
 * Kaynak `types.generated.ts`; o dosya `npm run tipler:uret` ile
 * `/swagger/v2/swagger.json`'dan üretilir ve ELLE DÜZENLENMEZ. Buradaki
 * takma adlar sayesinde ekranlar `components['schemas']['…']` yazmak zorunda
 * kalmaz ve sözleşme değişince derleme kırılır — sessizce kaymaz.
 */
import type { components } from './types.generated';

type S = components['schemas'];

// ─────────────────────────────────────────────────────────── etkinlik
export type EventSummary = S['EtkinlikOzetDto'];
export type Event = S['AjandaDto'];
export type EventNote = S['AjandaNotDto'];
export type EventActivity = S['AjandaOlayDto'];
export type FieldChange = S['AjandaAlanDegisikligiDto'];
export type EventPhoto = S['AjandaPhotoDto'];
export type Participant = S['KatilimciDto'];

// ─────────────────────────────────────────────────────────── talep
export type RequestSummary = S['TalepOzetDto'];
export type Request = S['RandevuDto'];
export type RequestNote = S['RandevuNotDto'];
export type RequestActivity = S['RandevuHareketDto'];
export type RequestFile = S['RandevuDosyaDto'];

// ─────────────────────────────────────────────────────────── referans
export type EventStatus = S['AjandaDurumDto'];
export type EventType = S['RandevuTipDto'];
export type RequestStatus = S['RandevuDurumDto'];
export type Unit = S['BirimDto'];
export type Florist = S['CicekciDto'];
export type FloristDetail = S['CicekciDetayDto'];
export type FloristInstruction = S['CicekciTalimatDto'];
export type Flower = S['CicekDto'];

// ─────────────────────────────────────────────────────────── istatistik
export type EventStatistics = S['AjandaIstatistikDto'];

/** Talep panosu — mahalle, meslek, tip, durum ve zaman dağılımları. */
export type RequestStatistics = S['TalepIstatistikDto'];

// ── Halk Günü ──
export type PublicDaySummary = S['HalkGunuOzetDto'];
export type PublicDayDetail = S['HalkGunuDetayDto'];
export type PublicDaySlot = S['HalkGunuDilimDto'];
export type PublicDayAttendance = S['HalkGunuKatilimDto'];
export type PublicDayApplication = S['BasvuruDto'];
export type PublicDayOverview = S['HalkGunuOzetiDto'];
export type PersonHistory = S['KisiGecmisiDto'];

/** Vatandaş dosyası — kişinin kurumla bütün geçmişi. */
export type PersonFile = S['KisiDosyasiDto'];
export type PersonRequest = S['KisiTalepDto'];
export type PersonPublicDay = S['KisiHalkGunuDto'];
export type PersonEvent = S['KisiEtkinlikDto'];

export type ResumeSummary = S['OzgecmisOzetDto'];
export type ResumeDetail = S['OzgecmisDetayDto'];
export type ResumeShare = S['OzgecmisPaylasimDto'];

export type SmsResult = S['SmsGonderimSonucuDto'];

/** Katılım durumu — sunucudaki `KatilimDurumu` ile aynı sayılar. */
export const ATTENDANCE = {
  waiting: 0,
  arrived: 1,
  noShow: 2,
  met: 3,
  cancelled: 4,
} as const;

/** Halk günü durumu — sunucudaki `HalkGunuDurumu` ile aynı sayılar. */
export const PUBLIC_DAY_STATUS = {
  planning: 0,
  live: 1,
  completed: 2,
  cancelled: 3,
} as const;
export type StatisticsSummary = S['IstatistikOzetDto'];
export type StatBucket = S['IstatistikDilimDto'];
export type SeriesPoint = S['IstatistikSeriNoktasiDto'];

// ─────────────────────────────────────────────────────────── öneri
export type Suggestion = S['OneriDto'];

// ─────────────────────────────────────────────────────────── tanımlar
/** Renkli tanım kaydı (etkinlik tipi / etkinlik durumu / talep durumu). */
export type Definition = S['TanimDto'];
export type DefinitionInput = S['TanimIstegi'];
/** Yalnızca adı olan kayıt (mahalle, meslek). */
export type NameRecord = S['AdKaydiDto'];
export type BulkImportResult = S['TopluIceAktarmaSonucu'];

// ─────────────────────────────────────────────────────────── bildirim
export type AppNotification = S['BildirimDto'];
export type SessionRecord = S['OturumKaydiDto'];

// ─────────────────────────────────────────────────────────── harita
export type MapPoint = S['HaritaNoktasiDto'];
export type PublicDay = S['HalkGunuDto'];
export type StatusCount = S['DurumSayaciDto'];

// ─────────────────────────────────────────────────────────── yönetim
export type UserSummary = S['KullaniciOzetDto'];
export type UnitNode = S['BirimDugumDto'];
export type UnitDetail = S['BirimDetayDto'];
export type Role = S['RolDto'];

/** İzin kataloğu kaydı — rol yetkisi ekranındaki seçim listesi. */
export type PermissionRecord = S['IzinDto'];
export type ErrorSummary = S['HataOzetDto'];
export type ErrorDetail = S['HataDetayDto'];

/**
 * Etkinlik statüsü. Sunucudaki `AjandaStatus` sayısal; sıra DEĞİŞMEMELİ —
 * değişirse "beklemede" sessizce "iptal" olarak okunur.
 */
export const STATUS = {
  Pending: 0,
  Completed: 1,
  Cancelled: 2,
} as const;

/** Tekrar kapsamı — sunucudaki `TekrarKapsam` ile birebir. */
export const SCOPE = {
  This: 0,
  ThisAndFollowing: 1,
  All: 2,
} as const;

/**
 * Olay tipleri (`AjandaOlayTip`). Sunucu sayısal gönderiyor; zaman
 * çizelgesinde okunabilir etiket gerekiyor.
 */
export const EVENT_ACTIVITY_LABELS: Record<number, string> = {
  0: 'Oluşturuldu',
  1: 'Güncellendi',
  2: 'Silindi',
  3: 'Geri alındı',
  4: 'Ertelendi',
  5: 'Havale edildi',
  6: 'Üst birime gönderildi',
  7: 'Durum değişti',
  8: 'Tip değişti',
  9: 'Statü değişti',
  10: 'Not eklendi',
  11: 'Fotoğraf eklendi',
  12: 'Fotoğraf silindi',
  13: 'Çiçek talimatı verildi',
  14: 'Çiçek talimatı iptal edildi',
  15: 'SMS gönderildi',
  16: 'Katılımcı değişti',
  17: 'Seri oluşturuldu',
  18: 'Seri güncellendi',
};
