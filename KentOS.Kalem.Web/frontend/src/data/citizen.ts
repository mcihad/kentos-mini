import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, queryString, request, type PagedResult, type PageParams } from './client';
import { taskKeys } from './tasks';
import type {
  CitizenReport, CitizenReportRequest, CitizenReportResult, FieldReportRequest,
  InboxAccept, InboxItem, ReportRouteRequest, TaskDetail, TaskSummary,
  VerificationResult, WorkAttachment, WorkMapPoint, WorkStatistics,
} from './types';

/**
 * VATANDAŞ PORTALI ve SAHA veri katmanı.
 *
 * <p>
 * Portal çağrıları <b>jetonsuz</b>: <code>request</code> jeton yoksa
 * <code>Authorization</code> başlığını hiç eklemiyor, dolayısıyla ayrı bir
 * istemci gerekmiyor. Ama portal ekranı oturum açmış bir personelde de
 * açılabilir; o durumda jeton gitse bile sunucu anonim uçta onu yok sayıyor.
 * </p>
 */

export const citizenKeys = {
  all: () => ['vatandas-bildirimi'] as const,
  list: (filtre: string) => ['vatandas-bildirimi', 'liste', filtre] as const,
  detail: (id: number) => ['vatandas-bildirimi', 'detay', id] as const,
  attachments: (id: number) => ['vatandas-bildirimi', 'ekler', id] as const,
  map: (filtre: string) => ['saha', 'harita', filtre] as const,
  myWork: () => ['saha', 'islerim'] as const,

  inbox: (filtre: string) => ['gelen-kutusu', 'liste', filtre] as const,
  inboxAll: () => ['gelen-kutusu'] as const,
  inboxPending: () => ['gelen-kutusu', 'bekleyen'] as const,

  stats: (filtre: string) => ['is-istatistik', filtre] as const,
} as const;

// ═══════════════════════════════════════════════════════ portal (anonim)

/**
 * Portal adımları.
 *
 * <p>
 * Üç ayrı çağrı, tek bir "gönder" değil: telefon doğrulaması formu
 * doldurmadan ÖNCE bitmeli. Sonda olsaydı vatandaş bütün formu doldurup
 * kodu bekler, kod gelmezse yazdığı her şeyi kaybederdi.
 * </p>
 */
export const portal = {
  kodIste: (telefon: string) =>
    api.post<void>('/bildir/kod', { telefon }),

  dogrula: (telefon: string, kod: string) =>
    api.post<VerificationResult>('/bildir/dogrula', { telefon, kod }),

  bildir: (govde: CitizenReportRequest) =>
    api.post<CitizenReportResult>('/bildir', govde),

  /** Fotoğraf çok parçalı gövdeyle; anahtar imzalı ve kısa ömürlü. */
  async fotograf(anahtar: string, dosya: File) {
    const govde = new FormData();
    govde.append('anahtar', anahtar);
    govde.append('dosya', dosya);
    return request<void>('/bildir/fotograf', { method: 'POST', body: govde });
  },
};

// ═══════════════════════════════════════════════════ karşılama (personel)

export function useCitizenReports(filtre: PageParams & { durum?: number | null }) {
  return useQuery({
    queryKey: citizenKeys.list(JSON.stringify(filtre)),
    queryFn: () =>
      api.get<PagedResult<CitizenReport>>(`/vatandas-bildirimi${queryString(filtre)}`),
  });
}

export function useCitizenReport(id: number | undefined) {
  return useQuery({
    queryKey: citizenKeys.detail(id ?? 0),
    queryFn: () => api.get<CitizenReport>(`/vatandas-bildirimi/${id}`),
    enabled: !!id,
  });
}

export function useCitizenReportAttachments(id: number | undefined) {
  return useQuery({
    queryKey: citizenKeys.attachments(id ?? 0),
    queryFn: () => api.get<WorkAttachment[]>(`/vatandas-bildirimi/${id}/ek`),
    enabled: !!id,
  });
}

export function useCitizenReportMutations(id?: number) {
  const qc = useQueryClient();

  // Yönlendirme GÖREV açıyor: görev önbelleği de tazelenmezse yeni iş
  // listede görünmezdi.
  const bitince = () => {
    qc.invalidateQueries({ queryKey: citizenKeys.all() });
    qc.invalidateQueries({ queryKey: taskKeys.all() });
  };

  return {
    yonlendir: useMutation({
      mutationFn: (govde: ReportRouteRequest) =>
        api.post<CitizenReport>(`/vatandas-bildirimi/${id}/yonlendir`, govde),
      onSuccess: bitince,
    }),
    reddet: useMutation({
      mutationFn: (not: string) =>
        api.post<CitizenReport>(`/vatandas-bildirimi/${id}/reddet`, { not }),
      onSuccess: bitince,
    }),
  };
}

// ═══════════════════════════════════════════════════════════════ saha

export function useMyFieldWork() {
  return useQuery({
    queryKey: citizenKeys.myWork(),
    queryFn: () => api.get<TaskSummary[]>('/saha/islerim'),
  });
}

export type MapFilter = {
  altBirimlerDahil?: boolean;
  bildirimlerDahil?: boolean;
  yalnizAcik?: boolean;
};

export function useMapPoints(filtre: MapFilter) {
  return useQuery({
    queryKey: citizenKeys.map(JSON.stringify(filtre)),
    queryFn: () => api.get<WorkMapPoint[]>(`/saha/harita${queryString(filtre)}`),
    // Harita noktaları sık değişmiyor ve her açılışta bin satır çekmek
    // gereksiz; bir dakika taze sayılıyor.
    staleTime: 60_000,
  });
}

export function useFieldMutations() {
  const qc = useQueryClient();

  return {
    tespit: useMutation({
      mutationFn: (govde: FieldReportRequest) =>
        api.post<TaskDetail>('/saha/tespit', govde),
      onSuccess: () => {
        qc.invalidateQueries({ queryKey: taskKeys.all() });
        qc.invalidateQueries({ queryKey: citizenKeys.myWork() });
        qc.invalidateQueries({ queryKey: ['saha', 'harita'] });
      },
    }),
  };
}

// ═══════════════════════════════════════════════════ gelen kutusu

export function useInbox(filtre: PageParams & { durum?: number | null; altBirimlerDahil?: boolean }) {
  return useQuery({
    queryKey: citizenKeys.inbox(JSON.stringify(filtre)),
    queryFn: () => api.get<PagedResult<InboxItem>>(`/gelen-kutusu${queryString(filtre)}`),
  });
}

/**
 * Bekleyen kayıt sayısı — menüdeki rozet.
 *
 * <p>
 * Bir dakika taze sayılıyor: rozet için her ekran geçişinde sorgu atmak
 * gereksiz, ama saatlerce bayat kalması da "kimse bakmıyor" hissi verirdi.
 * </p>
 */
export function useInboxPending(etkin: boolean) {
  return useQuery({
    queryKey: citizenKeys.inboxPending(),
    queryFn: () => api.get<number>('/gelen-kutusu/bekleyen'),
    enabled: etkin,
    staleTime: 60_000,
  });
}

export function useInboxMutations(id?: number) {
  const qc = useQueryClient();

  // Karar GÖREV açıyor: görev önbelleği de tazelenmezse yeni iş listede
  // görünmezdi.
  const bitince = () => {
    qc.invalidateQueries({ queryKey: citizenKeys.inboxAll() });
    qc.invalidateQueries({ queryKey: taskKeys.all() });
  };

  return {
    kabul: useMutation({
      mutationFn: (govde: InboxAccept) =>
        api.post<InboxItem>(`/gelen-kutusu/${id}/kabul`, govde),
      onSuccess: bitince,
    }),
    reddet: useMutation({
      mutationFn: (gerekce: string) =>
        api.post<InboxItem>(`/gelen-kutusu/${id}/reddet`, { gerekce }),
      onSuccess: bitince,
    }),
    okundu: useMutation({
      mutationFn: () => api.post<InboxItem>(`/gelen-kutusu/${id}/okundu`),
      onSuccess: bitince,
    }),
  };
}

// ═══════════════════════════════════════════════════ gecikme panosu

export function useWorkStatistics(altBirimlerDahil: boolean) {
  return useQuery({
    queryKey: citizenKeys.stats(String(altBirimlerDahil)),
    queryFn: () =>
      api.get<WorkStatistics>(`/is-istatistik${queryString({ altBirimlerDahil })}`),
    staleTime: 60_000,
  });
}
