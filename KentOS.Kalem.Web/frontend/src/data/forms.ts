import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, queryString, type PagedResult } from './client';
import type {
  FormAnswerRequest, FormAnswerResult, FormDetail, FormPublic, FormReport,
  FormResponseDetail, FormResponseSummary, FormSave, FormSummary,
} from './types';

/**
 * FORM VE ANKET veri katmanı.
 *
 * <p>
 * İki ayrı yüzey: yetkili (<c>/form</c>, jetonlu) ve vatandaş
 * (<c>/form-portal</c>, anonim). Vatandaş uçları <c>api</c> sarmalayıcısını
 * KULLANMAZ — o sarmalayıcı 401'de oturumu düşürüp giriş ekranına
 * yönlendiriyor, oysa vatandaşın oturumu hiç yok.
 * </p>
 */

export const formKeys = {
  liste: (s: unknown) => ['form', 'liste', s] as const,
  detay: (id: number) => ['form', id] as const,
  yanitlar: (id: number, s: unknown) => ['form', id, 'yanit', s] as const,
  yanit: (id: number, yid: number) => ['form', id, 'yanit', yid] as const,
  ozet: (id: number) => ['form', id, 'ozet'] as const,
};

export type FormFilter = {
  arama?: string;
  durum?: number;
  sayfa?: number;
  boyut?: number;
};

export function useForms(filtre: FormFilter) {
  return useQuery({
    queryKey: formKeys.liste(filtre),
    queryFn: () => api.get<PagedResult<FormSummary>>(`/form${queryString(filtre)}`),
    placeholderData: (o) => o,
  });
}

export function useForm(id: number | undefined) {
  return useQuery({
    queryKey: formKeys.detay(id!),
    queryFn: () => api.get<FormDetail>(`/form/${id}`),
    enabled: !!id,
  });
}

export function useFormMutations(id?: number) {
  const qc = useQueryClient();
  const bitince = () => qc.invalidateQueries({ queryKey: ['form'] });

  return {
    olustur: useMutation({
      mutationFn: (govde: FormSave) => api.post<FormDetail>('/form', govde),
      onSuccess: bitince,
    }),
    guncelle: useMutation({
      mutationFn: (g: { id: number; govde: FormSave }) =>
        api.put<FormDetail>(`/form/${g.id}`, g.govde),
      onSuccess: bitince,
    }),
    yayinla: useMutation({
      mutationFn: (fid: number) => api.post<FormDetail>(`/form/${fid}/yayinla`, {}),
      onSuccess: bitince,
    }),
    durum: useMutation({
      mutationFn: (g: { id: number; durum: number }) =>
        api.post<FormDetail>(`/form/${g.id}/durum?durum=${g.durum}`, {}),
      onSuccess: bitince,
    }),
    kopyala: useMutation({
      mutationFn: (fid: number) => api.post<FormDetail>(`/form/${fid}/kopyala`, {}),
      onSuccess: bitince,
    }),
    sil: useMutation({
      mutationFn: (fid: number) => api.delete<void>(`/form/${fid}`),
      onSuccess: bitince,
    }),
    yanitSil: useMutation({
      mutationFn: (g: { formId: number; yanitId: number }) =>
        api.delete<void>(`/form/${g.formId}/yanit/${g.yanitId}`),
      onSuccess: bitince,
    }),
    _id: id,
  };
}

export type FormResponseFilter = {
  arama?: string;
  durum?: number;
  baslangic?: string;
  bitis?: string;
  alanKimligi?: string;
  alanDegeri?: string;
  sayfa?: number;
  boyut?: number;
};

export function useFormResponses(formId: number | undefined, filtre: FormResponseFilter) {
  return useQuery({
    queryKey: formKeys.yanitlar(formId!, filtre),
    queryFn: () => api.get<PagedResult<FormResponseSummary>>(`/form/${formId}/yanit${queryString(filtre)}`),
    enabled: !!formId,
    placeholderData: (o) => o,
  });
}

export function useFormResponse(formId: number | undefined, yanitId: number | undefined) {
  return useQuery({
    queryKey: formKeys.yanit(formId!, yanitId!),
    queryFn: () => api.get<FormResponseDetail>(`/form/${formId}/yanit/${yanitId}`),
    enabled: !!formId && !!yanitId,
  });
}

export function useFormReport(formId: number | undefined) {
  return useQuery({
    queryKey: formKeys.ozet(formId!),
    queryFn: () => api.get<FormReport>(`/form/${formId}/ozet`),
    enabled: !!formId,
  });
}

// ═══════════════════════════════════════════════ vatandaş yüzeyi (anonim)

/**
 * Anonim uçlar için ham <c>fetch</c>.
 *
 * <p>
 * <b><c>api</c> sarmalayıcısı KULLANILMAZ.</b> O sarmalayıcı 401 gelince
 * jetonu temizleyip giriş ekranına yönlendiriyor; vatandaşın oturumu hiç
 * yok ve kapalı bir formda giriş ekranına atılmak, "form kapalı" demekten
 * çok daha kafa karıştırıcı olurdu.
 * </p>
 */
async function portal<T>(yol: string, secenek?: RequestInit): Promise<T> {
  const y = await fetch(`/api/v2/form-portal${yol}`, {
    ...secenek,
    headers: { 'content-type': 'application/json', ...(secenek?.headers ?? {}) },
  });

  const metin = await y.text();

  if (!y.ok) {
    let mesaj = 'Bir şeyler ters gitti.';
    try {
      mesaj = JSON.parse(metin).detail ?? JSON.parse(metin).title ?? mesaj;
    } catch { /* düz metin */ }
    throw new Error(mesaj);
  }

  return metin ? (JSON.parse(metin) as T) : (undefined as T);
}

export function usePublicForm(anahtar: string | undefined) {
  return useQuery({
    queryKey: ['form-portal', anahtar] as const,
    queryFn: () => portal<FormPublic>(`/${anahtar}`),
    enabled: !!anahtar,
    retry: false,
  });
}

export function usePublicFormSubmit(anahtar: string | undefined) {
  return useMutation({
    mutationFn: (govde: FormAnswerRequest) =>
      portal<FormAnswerResult>(`/${anahtar}`, { method: 'POST', body: JSON.stringify(govde) }),
  });
}

export function usePublicFormDraft(anahtar: string | undefined) {
  return useMutation({
    mutationFn: (govde: FormAnswerRequest) =>
      portal<{ surdurmeAnahtari: string }>(`/${anahtar}/taslak`, {
        method: 'POST', body: JSON.stringify(govde),
      }),
  });
}
