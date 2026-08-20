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

// ─────────────────────────────────────────────────────────── iş takip
export type TaskSummary = S['GorevOzetDto'];
export type TaskDetail = S['GorevDetayDto'];
export type TaskStage = S['GorevAsamaDto'];
export type TaskAssignment = S['GorevAtamaDto'];
export type TaskStatusOption = S['GorevDurumSecenegiDto'];
export type TaskSave = S['GorevKayitDto'];
export type TaskAssignRequest = S['GorevAtamaIstegiDto'];
export type TaskStatusRequest = S['GorevDurumIstegiDto'];
export type TaskStageRequest = S['GorevAsamaIstegiDto'];

export type TaskType = S['GorevTipiDto'];
export type TaskTypeSave = S['GorevTipiKayitDto'];
export type TaskTypeStage = S['GorevTipiAsamaDto'];
export type TaskTypeHandoff = S['GorevTipiDevirDto'];

export type Team = S['EkipDto'];
export type TeamSave = S['EkipKayitDto'];
export type TeamMember = S['EkipUyeDto'];

// ── proje ──
export type ProjectSummary = S['ProjeOzetDto'];
export type ProjectDetail = S['ProjeDetayDto'];
export type ProjectSave = S['ProjeKayitDto'];
export type ProjectMember = S['ProjeUyeDto'];
export type ProjectMemberRequest = S['ProjeUyeIstegiDto'];
export type ProjectTeamRequest = S['ProjeEkibiIstegiDto'];
export type Milestone = S['KilometreTasiDto'];
export type BoardColumn = S['PanoSutunuDto'];
export type Board = S['PanoDto'];
export type BoardColumnCards = S['PanoSutunKartlariDto'];
export type CardMove = S['KartTasimaDto'];
export type GanttRow = S['GanttSatiriDto'];

/** Proje durumları — `ProjeDurumu` ile birebir. */
export const PROJECT_STATUS = {
  planlaniyor: 0,
  devam: 1,
  durduruldu: 2,
  tamamlandi: 3,
  iptal: 4,
} as const;

/**
 * Proje durum etiketleri — YALNIZCA süzgeç için.
 *
 * Kayıt satırları etiketi sunucudan alıyor (`durumAd`, `durumRenk`); süzgeç
 * ise henüz seçilmemiş durumları da göstermek zorunda ve o adlar hiçbir
 * satırda bulunmuyor. Görev durumlarındaki gerekçenin aynısı.
 */
export const PROJECT_STATUS_LABELS: Record<number, string> = {
  0: 'Planlanıyor',
  1: 'Devam ediyor',
  2: 'Durduruldu',
  3: 'Tamamlandı',
  4: 'İptal edildi',
};

/** Proje üye rolleri — `ProjeUyeRolu`. */
export const PROJECT_MEMBER_ROLE_LABELS: Record<number, string> = {
  0: 'Yönetici',
  1: 'Üye',
  2: 'İzleyici',
};

// ── vatandaş ve saha ──
export type CitizenReport = S['VatandasBildirimiDto'];
export type CitizenReportRequest = S['VatandasBildirimiIstegiDto'];
export type CitizenReportResult = S['VatandasBildirimiSonucuDto'];
export type VerificationResult = S['DogrulamaSonucuDto'];
export type ReportRouteRequest = S['BildirimYonlendirmeDto'];
export type FieldReportRequest = S['SahaTespitiDto'];
export type WorkMapPoint = S['IsHaritaNoktasiDto'];

/** Vatandaş bildirimi durumları — `VatandasBildirimDurumu`. */
export const REPORT_STATUS = {
  yeni: 0,
  yonlendirildi: 1,
  reddedildi: 2,
} as const;

export const REPORT_STATUS_LABELS: Record<number, string> = {
  0: 'Bekliyor',
  1: 'Yönlendirildi',
  2: 'İşleme alınmadı',
};

// ── gelen kutusu ve pano ──
export type InboxItem = S['GelenKutusuDto'];
export type InboxAccept = S['GelenKutusuKabulDto'];
export type UnitScorecard = S['BirimKarnesiDto'];
export type WorkStatistics = S['IsIstatistikDto'];

/** Gelen kutusu durumları — `GelenKutusuDurumu`. */
export const INBOX_STATUS = {
  bekliyor: 0,
  kabul: 1,
  ret: 2,
  okundu: 3,
} as const;

export const INBOX_STATUS_LABELS: Record<number, string> = {
  0: 'Bekliyor',
  1: 'Kabul edildi',
  2: 'Reddedildi',
  3: 'Okundu',
};

export type WorkEvent = S['IsOlayDto'];
export type WorkEventChange = S['IsOlayDegisiklikDto'];
export type WorkAttachment = S['IsEkDto'];
export type WorkComment = S['IsYorumDto'];

/** Kullanıcının adına çalışabileceği birimler — `X-Etkin-Birim` seçimi. */
export type ScopeUnit = S['KapsamBirimiDto'];

/**
 * Göreve atanabilecek / ekibe eklenebilecek kişi.
 *
 * Etkin birimin ALT AĞACINI kapsar ve kullanıcının KENDİSİNİ içerir — ikisi
 * de eski `/ayar/birim-kullanicilari` ucunda yoktu, gerekçesi
 * `usePeople` üzerinde yazılı.
 */
export type Person = S['PersonelSecimDto'];

/**
 * GÖREV DURUMLARI — sunucudaki `GorevDurumu` ile BİREBİR.
 *
 * Ad ve renk sunucudan geliyor (`durumAd`, `durumRenk`); buradaki sayılar
 * yalnızca süzgeç ve istek gövdesi kurmak için. Sunucudaki sıra değişirse
 * burası da değişmeli — ama enum'a değer SONA ekleniyor, araya değil.
 */
export const TASK_STATUS = {
  yeni: 0,
  atandi: 1,
  basladi: 2,
  devamEdiyor: 3,
  beklemede: 4,
  onayBekliyor: 5,
  tamamlandi: 6,
  iadeEdildi: 7,
  reddedildi: 8,
  iptal: 9,
} as const;

/**
 * Durum etiketleri — YALNIZCA süzgeç çipleri için.
 *
 * Liste ve detay satırları etiketi SUNUCUDAN alıyor (`durumAd`, `durumRenk`);
 * burası hiçbir kaydın etiketini üretmiyor. Süzgeç çipleri henüz seçilmemiş
 * durumları da göstermek zorunda ve o durumların adı hiçbir satırda
 * bulunmuyor — tek kaynaktan okunamayacak yer burası.
 */
export const TASK_STATUS_LABELS: Record<number, string> = {
  0: 'Yeni',
  1: 'Atandı',
  2: 'Başladı',
  3: 'Devam ediyor',
  4: 'Beklemede',
  5: 'Onay bekliyor',
  6: 'Tamamlandı',
  7: 'İade edildi',
  8: 'Reddedildi',
  9: 'İptal edildi',
};

/** Öncelikler — `GorevOnceligi`. */
export const TASK_PRIORITY = {
  dusuk: 0,
  normal: 1,
  yuksek: 2,
  acil: 3,
} as const;

export const TASK_PRIORITY_LABELS: Record<number, string> = {
  0: 'Düşük',
  1: 'Normal',
  2: 'Yüksek',
  3: 'Acil',
};

/** Aşama durumları — `GorevAsamaDurumu`. */
export const TASK_STAGE_STATUS = {
  bekliyor: 0,
  tamamlandi: 1,
  atlandi: 2,
} as const;

/** Atama rolleri — `GorevAtamaRolu`. */
export const TASK_ASSIGNMENT_ROLE = {
  sorumlu: 0,
  yardimci: 1,
  izleyici: 2,
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

// ─────────────────────────────────────────────────── form ve anket
export type FormSummary = S['FormOzetDto'];
export type FormDetail = S['FormDetayDto'];
export type FormSave = S['FormKayitDto'];
export type FormDefinition = S['FormTanimiDto'];
export type FormStep = S['FormAdimiDto'];
export type FormGroup = S['FormGrubuDto'];
export type FormField = S['FormAlaniDto'];
export type FormOption = S['FormSecenegiDto'];
export type FormCondition = S['FormKosuluDto'];
export type FormConditionRule = S['FormKosulKuraliDto'];
export type FormFieldValidation = S['FormDogrulamaDto'];
export type FormFieldSettings = S['FormAlanAyarlariDto'];

export type FormResponseSummary = S['FormYanitOzetDto'];
export type FormResponseDetail = S['FormYanitDetayDto'];
export type FormReport = S['FormOzetRaporuDto'];
export type FormFieldReport = S['FormAlanOzetiDto'];

export type FormPublic = S['FormPortalDto'];
export type FormAnswerRequest = S['FormYanitIstegiDto'];
export type FormAnswerResult = S['FormYanitSonucuDto'];
