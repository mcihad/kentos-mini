import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, queryString, type PagedResult, type PageParams } from './client';
import { taskKeys } from './tasks';
import type {
  Board, CardMove, GanttRow, Milestone, ProjectDetail, ProjectSave,
  ProjectSummary, ProjectTeamRequest,
} from './types';

/**
 * PROJE veri katmanı.
 *
 * <p>
 * <b>Proje değişikliği GÖREV önbelleğini de düşürüyor</b> ve tersi de doğru.
 * İkisi aynı veriye iki pencereden bakıyor: bir görev tamamlanınca projenin
 * ilerleme sayısı, gantt çubuğunun doluluğu ve panodaki sütunu değişiyor.
 * Yalnızca kendi ön ekini tazelemek, açık duran proje ekranında eski
 * rakamları bırakırdı.
 * </p>
 */

export const projectKeys = {
  all: () => ['proje'] as const,
  list: (filtre: string) => ['proje', 'liste', filtre] as const,
  detail: (id: number) => ['proje', 'detay', id] as const,
  board: (id: number) => ['proje', 'pano', id] as const,
  gantt: (id: number) => ['proje', 'gantt', id] as const,
} as const;

export type ProjectFilter = PageParams & {
  durumlar?: number[];
  yoneticiId?: number | null;
  altBirimlerDahil?: boolean;
  yalnizAcik?: boolean;
};

export function useProjects(filtre: ProjectFilter) {
  return useQuery({
    queryKey: projectKeys.list(JSON.stringify(filtre)),
    queryFn: () => api.get<PagedResult<ProjectSummary>>(`/proje${queryString(filtre)}`),
    placeholderData: keepPreviousData,
  });
}

export function useProject(id: number | undefined) {
  return useQuery({
    queryKey: projectKeys.detail(id ?? 0),
    queryFn: () => api.get<ProjectDetail>(`/proje/${id}`),
    enabled: !!id,
  });
}

export function useBoard(id: number | undefined, etkin = true) {
  return useQuery({
    queryKey: projectKeys.board(id ?? 0),
    queryFn: () => api.get<Board>(`/proje/${id}/pano`),
    enabled: !!id && etkin,
  });
}

export function useGantt(id: number | undefined, etkin = true) {
  return useQuery({
    queryKey: projectKeys.gantt(id ?? 0),
    queryFn: () => api.get<GanttRow[]>(`/proje/${id}/gantt`),
    enabled: !!id && etkin,
  });
}

/** Proje ve görev önbelleklerini birlikte tazeler — ikisi aynı veriye bakıyor. */
function tazele(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: projectKeys.all() });
  qc.invalidateQueries({ queryKey: taskKeys.all() });
}

export function useProjectMutations(id?: number) {
  const qc = useQueryClient();
  const bitince = () => tazele(qc);

  return {
    olustur: useMutation({
      mutationFn: (govde: ProjectSave) => api.post<ProjectDetail>('/proje', govde),
      onSuccess: bitince,
    }),
    guncelle: useMutation({
      mutationFn: (girdi: { id: number; govde: ProjectSave }) =>
        api.put<ProjectDetail>(`/proje/${girdi.id}`, girdi.govde),
      onSuccess: bitince,
    }),
    sil: useMutation({
      mutationFn: (projeId: number) => api.delete<void>(`/proje/${projeId}`),
      onSuccess: bitince,
    }),

    /** Ekip AYRI uçtan: ayrı izin (`proje.uyeYonet`). */
    ekip: useMutation({
      mutationFn: (govde: ProjectTeamRequest) =>
        api.put<ProjectDetail>(`/proje/${id}/uye`, govde),
      onSuccess: bitince,
    }),

    /**
     * Kartı taşır — yani görevin DURUMUNU değiştirir.
     *
     * İyimser güncelleme YAPILMIYOR: sunucu geçişi reddedebiliyor (onay
     * kapısı, atanmamış görev). Kartı önce taşıyıp sonra geri almak,
     * kullanıcıya bir an "oldu" dedirtip sonra sebepsizce geri alırdı.
     */
    kartTasi: useMutation({
      mutationFn: (govde: CardMove) => api.post<Board>(`/proje/${id}/pano/tasi`, govde),
      onSuccess: bitince,
    }),

    kilometreTasi: useMutation({
      mutationFn: (girdi: { tasId: number; tamamlandi: boolean }) =>
        api.post<Milestone>(
          `/proje/${id}/kilometre-tasi/${girdi.tasId}?tamamlandi=${girdi.tamamlandi}`,
        ),
      onSuccess: bitince,
    }),

    /**
     * Tek bir ara hedef ekler — projenin TAMAMINI kaydetmeden.
     *
     * Önce bunun tek yolu düzenleme formuydu: bütçe, tarih, ekip ve pano
     * sütunlarıyla açılan bir form, tek satırlık bir iş için fazla. Üstelik
     * o formu kaydetmek projenin geri kalanını da yeniden yazıyordu.
     */
    kilometreTasiEkle: useMutation({
      mutationFn: (govde: { ad: string; aciklama?: string; hedefTarih?: string | null }) =>
        api.post<Milestone>(`/proje/${id}/kilometre-tasi`, govde),
      onSuccess: bitince,
    }),

    kilometreTasiSil: useMutation({
      mutationFn: (tasId: number) =>
        api.delete<void>(`/proje/${id}/kilometre-tasi/${tasId}`),
      onSuccess: bitince,
    }),
  };
}
