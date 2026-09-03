import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { queryKeys } from './queryKeys';
import { api, queryString, type PagedResult, type PageParams } from './client';
import type {
  NameRecord, Unit, Florist, StatusCount, EventStatus, EventType,
  EventStatistics, Participant, Definition, RequestStatus, RequestStatistics, RequestSummary, TopicStatistics,
  PublicDaySummary,
  PublicDayDetail,
  PublicDayApplication,
  PublicDayOverview,
  PersonFile,
  PersonHistory,
  ResumeSummary,
  ResumeDetail
} from './types';

/**
 * Referans veriler — nadiren değişir, uzun süre taze sayılır.
 *
 * `staleTime` yüksek: durum/tip listeleri gün içinde değişmez, her ekran
 * geçişinde yeniden çekmek boşuna istek üretir.
 */
const REFERENCE_OPTIONS = { staleTime: 10 * 60_000, gcTime: 30 * 60_000 };

/**
 * Açılır liste boyutu.
 *
 * Sunucudaki üst sınır 200. Durum/tip/birim listeleri bunun çok altında;
 * mahalle ve meslek aşabilir, o yüzden onlar aramayla süzülür.
 */
const LIST_SIZE = 200;

/** Sayfalı yanıttan yalnızca satırları alan yardımcı. */
function rows<T>(s: PagedResult<T> | undefined): T[] {
  return s?.veriler ?? [];
}

// ═════════════════════════════════════════════════════════ referans

export function useEventStatuses() {
  const s = useQuery({
    queryKey: queryKeys.settings.statuses(),
    queryFn: () =>
      api.get<PagedResult<EventStatus>>(`/ayar/etkinlik-durumlari?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}

export function useEventTypes() {
  const s = useQuery({
    queryKey: queryKeys.settings.types(),
    queryFn: () => api.get<PagedResult<EventType>>(`/ayar/tipler?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}

export function useRequestStatuses() {
  const s = useQuery({
    queryKey: ['ayar', 'talep-durumlari'] as const,
    queryFn: () =>
      api.get<PagedResult<RequestStatus>>(`/ayar/talep-durumlari?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}

export function useUnits() {
  const s = useQuery({
    queryKey: queryKeys.settings.units(),
    queryFn: () => api.get<PagedResult<Unit>>(`/ayar/birimler?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}

export function useFlorists() {
  const s = useQuery({
    queryKey: ['ayar', 'cicekciler'] as const,
    queryFn: () => api.get<PagedResult<Florist>>(`/ayar/cicekciler?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}

/**
 * Gizli etkinliği GÖREBİLECEK kişiler — yalnızca kendi birimin.
 *
 * Katılımcı birimle karıştırılmamalı: bu liste "kim görebilir" sorusunun
 * cevabı, öteki ("katilimci-birimler") "kim katılacak" sorusununki. Kendisi
 * listede yer almaz; ekleyen zaten her zaman görür.
 */
export function useUnitUsers() {
  const s = useQuery({
    queryKey: queryKeys.settings.unitUsers(),
    queryFn: () =>
      api.get<PagedResult<Participant>>(`/ayar/birim-kullanicilari?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}

/**
 * Kullanıcıya GELMİŞ dosya var mı?
 *
 * Menüyü süzmek için: gönderme yetkisi olmayan ve kendisine hiç dosya
 * gelmemiş kullanıcıya "Dosya Gönderimi" menüsü boş bir ekrandan ibaret.
 *
 * Tek kayıt istenir (`boyut=1`) — burada merak edilen "kaç tane" değil,
 * "hiç var mı".
 */
export function useHasIncomingFile() {
  const s = useQuery({
    queryKey: ['gonderim', 'gelen-var-mi'] as const,
    queryFn: () =>
      api.get<PagedResult<unknown>>('/gonderim?kutu=gelen&boyut=1'),
    staleTime: 60_000,
  });
  return (s.data?.toplam ?? 0) > 0;
}

/**
 * Etkinliğe KATILIMCI olarak eklenebilecek birimler.
 *
 * Katılan taraf kişi değil birim: "Başkan Yardımcısı", "Fen İşleri
 * Müdürlüğü" gibi. O birimden kimin geleceği toplantı gününe kadar
 * değişebiliyor; kişi tutmak her değişiklikte listeyi elden geçirmeyi
 * gerektiriyordu.
 *
 * Sunucu yalnızca kullanıcının KENDİ SEVİYESİNDEKİ ve ALTINDAKİ birimleri
 * döndürür — bir müdürlük başkan yardımcısını kendi toplantısına çağıramaz.
 */
/**
 * Etkinliğe KATILACAK birimler — kendi seviyen ve altı.
 *
 * Bir müdürlük başkan yardımcısını kendi toplantısına çağıramaz; o davet
 * yukarıdan gelir. Süzme sunucuda, sunucu ayrıca zorluyor.
 */
export function useParticipantUnits() {
  const s = useQuery({
    queryKey: queryKeys.settings.participantUnits(),
    queryFn: () =>
      api.get<PagedResult<Unit>>(`/ayar/katilimci-birimler?boyut=${LIST_SIZE}`),
    ...REFERENCE_OPTIONS,
  });
  return { ...s, liste: rows(s.data) };
}


/**
 * Mahalle araması.
 *
 * Tümünü çekmez — binlerce kayıt olabilir. En az iki karakter yazılınca
 * sunucuda süzülür; boşken ilk sayfa gösterilir.
 */
export function useNeighborhoodSearch(search: string) {
  const s = useQuery({
    queryKey: ['ayar', 'mahalleler', search] as const,
    queryFn: () =>
      api.get<PagedResult<NameRecord>>(`/ayar/mahalleler${queryString({ ara: search, boyut: 50 })}`),
    ...REFERENCE_OPTIONS,
    placeholderData: keepPreviousData,
  });
  return { ...s, liste: rows(s.data) };
}

export function useOccupationSearch(search: string) {
  const s = useQuery({
    queryKey: ['ayar', 'meslekler', search] as const,
    queryFn: () =>
      api.get<PagedResult<NameRecord>>(`/ayar/meslekler${queryString({ ara: search, boyut: 50 })}`),
    ...REFERENCE_OPTIONS,
    placeholderData: keepPreviousData,
  });
  return { ...s, liste: rows(s.data) };
}

// ═════════════════════════════════════════════════════════════ talep

export type RequestFilter = PageParams & {
  altBirimlerDahil?: boolean;
  arsiv?: boolean;
  durumId?: number | null;
  tipId?: number | null;
  mahalleId?: number | null;
  ajandayaEklendi?: boolean | null;
  baslangic?: string;
  bitis?: string;
};

/**
 * Talep listesi — sunucu tarafı sayfalama, arama ve süzme.
 *
 * `placeholderData: keepPreviousData` ZORUNLU: sayfa değiştirirken liste
 * boşalıp yeniden dolarsa ekran zıplar ve kullanıcı yerini kaybeder.
 */
export function useRequests(filter: RequestFilter) {
  return useQuery({
    queryKey: queryKeys.request.list(JSON.stringify(filter)),
    queryFn: () => api.get<PagedResult<RequestSummary>>(`/talep${queryString(filter)}`),
    placeholderData: keepPreviousData,
  });
}

/**
 * Durum başına talep sayıları — filtre çipleri ve ana sayfa kartları.
 *
 * `ajandayaEklendi` verilirse sayaçlar YALNIZCA o alt kümeyi sayar. "Ajandaya
 * eklenmemiş" süzgeci açıkken çipler tüm kümeyi saymaya devam ederse rakamlar
 * listeyle çelişiyor ve kullanıcı hangisine güveneceğini bilemiyor.
 */
export function useRequestStatusCounts(
  includeSubUnits = false,
  addedToAgenda?: boolean | null,
) {
  return useQuery({
    queryKey: ['talep', 'durum-sayaclari', includeSubUnits, addedToAgenda ?? null] as const,
    queryFn: () =>
      api.get<StatusCount[]>(
        `/talep/durum-sayaclari${queryString({ altBirimlerDahil: includeSubUnits, ajandayaEklendi: addedToAgenda })}`,
      ),
    staleTime: 30_000,
  });
}

// ════════════════════════════════════════════════════════ istatistik

/**
 * Konu panosu — merkezdeki her kartın arkasındaki uç.
 *
 * Konu adı ROTA PARÇASIYLA aynı (`/istatistikler/form` → `/istatistik/form`);
 * ikisi ayrışırsa kart doğru sayfayı açar ama sayfa boş gelir.
 */
export function useTopicStatistics(
  konu: string, start?: string, end?: string,
) {
  return useQuery({
    queryKey: ['istatistik-konu', konu, start ?? '', end ?? ''] as const,
    queryFn: () => api.get<TopicStatistics>(
      `/istatistik/${konu}${queryString({ baslangic: start, bitis: end })}`),
    staleTime: 60_000,
  });
}

export function useEventStatistics(start?: string, end?: string, enabled = true) {
  return useQuery({
    queryKey: ['istatistik', start ?? '', end ?? ''] as const,
    queryFn: () => api.get<EventStatistics>(`/istatistik${queryString({ baslangic: start, bitis: end })}`),
    // Talep panosundayken bu AĞIR sorgu hiç çalışmasın.
    enabled,
    // Ağır bir sorgu; sekme değiştirince yeniden hesaplatmayalım.
    staleTime: 60_000,
  });
}

/**
 * Talep panosu — mahalle, meslek, tip, durum ve zaman dağılımları.
 *
 * Etkinlik panosundan AYRI bir uç: iki pano farklı soruları cevaplıyor ve tek
 * yanıtta birleştirmek, her iki ekranın da kullanmadığı alanları indirmesi
 * demekti.
 */
export function useRequestStatistics(start?: string, end?: string) {
  return useQuery({
    queryKey: ['istatistik', 'talep', start ?? '', end ?? ''] as const,
    queryFn: () =>
      api.get<RequestStatistics>(`/istatistik/talep${queryString({ baslangic: start, bitis: end })}`),
    staleTime: 60_000,
  });
}

// ═════════════════════════════════════════════════════════ halk günü

export function usePublicDays(filter: Record<string, unknown>) {
  return useQuery({
    queryKey: ['halkgunu', 'liste', JSON.stringify(filter)] as const,
    queryFn: () =>
      api.get<PagedResult<PublicDaySummary>>(`/halk-gunu${queryString(filter)}`),
    placeholderData: keepPreviousData,
  });
}

export function usePublicDay(id: number) {
  return useQuery({
    queryKey: ['halkgunu', 'detay', id] as const,
    queryFn: () => api.get<PublicDayDetail>(`/halk-gunu/${id}`),
    enabled: Number.isFinite(id) && id > 0,
  });
}

export function usePublicDayOverview(id: number) {
  return useQuery({
    queryKey: ['halkgunu', 'ozet', id] as const,
    queryFn: () => api.get<PublicDayOverview>(`/halk-gunu/${id}/ozet`),
    enabled: Number.isFinite(id) && id > 0,
  });
}

export function usePublicDayApplications(filter: Record<string, unknown>) {
  return useQuery({
    queryKey: ['halkgunu', 'basvuru', JSON.stringify(filter)] as const,
    queryFn: () =>
      api.get<PagedResult<PublicDayApplication>>(`/halk-gunu/basvuru${queryString(filter)}`),
    placeholderData: keepPreviousData,
  });
}

/**
 * "Bu vatandaşı daha önce gördük mü?"
 *
 * Telefon girilir girilmez çalışır. `enabled`: en az 7 hane ya da 3 harflik
 * bir ad olmadan sorgu ATILMAZ — her tuşta sunucuya gitmek dört tabloyu
 * boşuna taratırdı.
 */
export function usePersonHistory(phone?: string, name?: string, excludeAttendanceId?: number) {
  const yeterli =
    (phone ?? '').replace(/\D/g, '').length >= 7 || (name ?? '').trim().length >= 3;

  return useQuery({
    queryKey: ['halkgunu', 'gecmis', phone ?? '', name ?? '', excludeAttendanceId ?? 0] as const,
    queryFn: () =>
      api.get<PersonHistory>(
        `/halk-gunu/kisi-gecmisi${queryString({ telefon: phone, ad: name, haricKatilim: excludeAttendanceId })}`,
      ),
    enabled: yeterli,
    staleTime: 30_000,
  });
}

/**
 * VATANDAŞ DOSYASI — özet karttan açılan ayrıntılı geçmiş.
 *
 * Özet ucu her tuş vuruşunda çağrılıyor ve hafif kalmalı; bu uç yalnızca
 * dosya açıldığında çağrılır ve talep/halk günü/etkinlik listelerinin
 * tamamını getirir. Aramanın kapsamı KURUM GENELİ: vatandaş geçen sefer
 * başka bir müdürlüğe gitmiş olabilir.
 */
export function usePersonFile(phone?: string, name?: string, excludeAttendanceId?: number) {
  const yeterli =
    (phone ?? '').replace(/\D/g, '').length >= 7 || (name ?? '').trim().length >= 3;

  return useQuery({
    queryKey: ['halkgunu', 'dosya', phone ?? '', name ?? '', excludeAttendanceId ?? 0] as const,
    queryFn: () =>
      api.get<PersonFile>(
        `/halk-gunu/kisi-gecmisi/dosya${queryString({ telefon: phone, ad: name, haricKatilim: excludeAttendanceId })}`,
      ),
    enabled: yeterli,
    staleTime: 30_000,
  });
}

// ═════════════════════════════════════════════════════════ tanımlar

/** Yönetilebilir tanım türleri — v2 rota parçalarıyla birebir. */
export type DefinitionType = 'etkinlik-tipleri' | 'etkinlik-durumlari' | 'talep-durumlari';

export function useDefinitions(kind: DefinitionType, params: PageParams) {
  return useQuery({
    queryKey: ['tanim', kind, JSON.stringify(params)] as const,
    queryFn: () =>
      api.get<PagedResult<Definition>>(`/tanim/${kind}${queryString(params)}`),
    placeholderData: keepPreviousData,
  });
}

export type NameRecordType = 'mahalleler' | 'meslekler';

export function useNameRecords(kind: NameRecordType, params: PageParams) {
  return useQuery({
    queryKey: ['tanim', kind, JSON.stringify(params)] as const,
    queryFn: () =>
      api.get<PagedResult<NameRecord>>(`/tanim/${kind}${queryString(params)}`),
    placeholderData: keepPreviousData,
  });
}

// ═══════════════════════════════════════════════════════════ yardımcı

/**
 * Bir kimlik→kayıt sözlüğü kurar.
 *
 * Listelerde `durumId` yerine durum ADINI göstermek gerektiğinde her satırda
 * `find` çağırmak O(n·m) olurdu.
 *
 * <p>
 * Not: liste ve takvim uçları artık ad/renk bilgisini KAYITLA BİRLİKTE
 * gönderiyor; bu yardımcı yalnızca açılır listeler ve formlar için gerekli.
 * </p>
 */
export function toMap<T, K extends string | number>(
  list: T[] | undefined,
  key: (o: T) => K,
): Map<K, T> {
  const m = new Map<K, T>();
  for (const o of list ?? []) m.set(key(o), o);
  return m;
}

// ═════════════════════════════════════════════════ özgeçmiş havuzu

/**
 * Havuz listesi.
 *
 * Kayıtlar TEK tablodan gelir: doğrudan havuza eklenenler ve talepten
 * yansıyanlar aynı sorguda. `kaynak` süzgeci ikisini ayırır.
 */
export function useResumes(filter: Record<string, unknown>) {
  return useQuery({
    queryKey: ['ozgecmis', 'liste', JSON.stringify(filter)] as const,
    queryFn: () =>
      api.get<PagedResult<ResumeSummary>>(`/ozgecmis${queryString(filter)}`),
    placeholderData: keepPreviousData,
  });
}

export function useResume(id: number) {
  return useQuery({
    queryKey: ['ozgecmis', 'detay', id] as const,
    queryFn: () => api.get<ResumeDetail>(`/ozgecmis/${id}`),
    enabled: Number.isFinite(id) && id > 0,
  });
}
