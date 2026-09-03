import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, queryString, request, type PagedResult, type PageParams } from './client';
import type {
  Person, ScopeUnit, TaskAssignRequest, TaskDetail, TaskSave, TaskStageRequest,
  TaskStatusRequest, TaskSummary, TaskType, TaskTypeSave, Team, TeamSave,
  WorkAttachment, WorkComment, WorkEvent,
} from './types';

/**
 * İŞ TAKİP veri katmanı.
 *
 * <p>
 * `hooks.ts` içinde DEĞİL: o dosya makam modüllerini (ajanda, talep, halk
 * günü) taşıyor ve 400 satırı geçmiş durumda. İş takip kendi uçlarına,
 * kendi önbellek ön ekine ve kendi geçersizleştirme kurallarına sahip;
 * ayrı dosya, bir modülü değiştirirken ötekini okumak zorunda bırakmıyor.
 * </p>
 *
 * <p>
 * <b>Geçersizleştirme kaba tutuluyor.</b> Bir görevde herhangi bir değişiklik
 * `['gorev']` ön ekinin tamamını tazeliyor. İnce ayar (yalnızca o kaydın
 * anahtarı) daha az istek üretirdi ama bir aşama tamamlandığında listedeki
 * ilerleme çubuğu, gecikme rengi ve sayaçlar da değişiyor — hepsini tek tek
 * saymak, biri unutulduğunda kullanıcıya eski veriyi göstermek demek.
 * </p>
 */

/** Önbellek anahtarları — ön ek tek yerden üretilir. */
export const taskKeys = {
  all: () => ['gorev'] as const,
  list: (filtre: string) => ['gorev', 'liste', filtre] as const,
  detail: (id: number) => ['gorev', 'detay', id] as const,
  events: (id: number) => ['gorev', 'olaylar', id] as const,
  attachments: (id: number) => ['gorev', 'ekler', id] as const,
  comments: (id: number) => ['gorev', 'yorumlar', id] as const,

  types: (filtre: string) => ['gorev-tipi', 'liste', filtre] as const,
  typesAll: () => ['gorev-tipi'] as const,
  usableTypes: () => ['gorev-tipi', 'kullanilabilir'] as const,
  typeDetail: (id: number) => ['gorev-tipi', 'detay', id] as const,

  teams: (filtre: string) => ['ekip', 'liste', filtre] as const,
  teamsAll: () => ['ekip'] as const,
  teamDetail: (id: number) => ['ekip', 'detay', id] as const,

  scope: () => ['birim-kapsam'] as const,
  people: (filtre: string) => ['personel', filtre] as const,
  peopleAll: () => ['personel'] as const,
} as const;

/** Açılır liste boyutu — sunucudaki üst sınır 200. */
const LISTE_BOYUTU = 200;

/** Referans veriler günde bir değişir; her ekran geçişinde çekmek boşuna. */
const REFERANS = { staleTime: 10 * 60_000, gcTime: 30 * 60_000 };

// ═══════════════════════════════════════════════════════════════ görev

export type TaskFilter = PageParams & {
  durumlar?: number[];
  oncelikler?: number[];
  kaynaklar?: number[];
  gorevTipiId?: number | null;
  projeId?: number | null;
  kullaniciId?: number | null;
  ekipId?: number | null;
  yalnizKok?: boolean;
  yalnizGeciken?: boolean;
  altBirimlerDahil?: boolean;
  baslangic?: string | null;
  bitis?: string | null;
};

export function useTasks(filtre: TaskFilter) {
  return useQuery({
    queryKey: taskKeys.list(JSON.stringify(filtre)),
    queryFn: () => api.get<PagedResult<TaskSummary>>(`/gorev${queryString(filtre)}`),
    placeholderData: keepPreviousData,
  });
}

export function useTask(id: number | undefined) {
  return useQuery({
    queryKey: taskKeys.detail(id ?? 0),
    queryFn: () => api.get<TaskDetail>(`/gorev/${id}`),
    enabled: !!id,
  });
}

export function useTaskEvents(id: number | undefined) {
  return useQuery({
    queryKey: taskKeys.events(id ?? 0),
    queryFn: () => api.get<WorkEvent[]>(`/gorev/${id}/olaylar`),
    enabled: !!id,
  });
}

export function useTaskAttachments(id: number | undefined) {
  return useQuery({
    queryKey: taskKeys.attachments(id ?? 0),
    queryFn: () => api.get<WorkAttachment[]>(`/gorev/${id}/ek`),
    enabled: !!id,
  });
}

export function useTaskComments(id: number | undefined) {
  return useQuery({
    queryKey: taskKeys.comments(id ?? 0),
    queryFn: () => api.get<WorkComment[]>(`/gorev/${id}/yorum`),
    enabled: !!id,
  });
}

/**
 * Bir görevin bütün önbelleğini tazeler.
 *
 * Aşama, durum, atama ve yorum birbirini etkiliyor: aşama tamamlanınca durum
 * "devam ediyor"a geçiyor ve çizelgeye satır düşüyor. Tek tek saymak, birini
 * unutulduğunda ekranda çelişki bırakırdı.
 */
function tazele(qc: ReturnType<typeof useQueryClient>, id?: number) {
  qc.invalidateQueries({ queryKey: taskKeys.all() });

  /*
    PROJE VE PANO DA DÜŞÜYOR.

    Projenin yüzdesi artık bağlı görevlerin ilerleme ORTALAMASI; bir aşama
    kapandığında proje kartındaki çubuk, kilometre taşı oranı ve gantt
    doluluğu da değişiyor. Yalnızca `['gorev']` düşürüldüğünde açık duran
    proje ekranı eski yüzdede kalıyordu — kullanıcının "aşamaları
    tamamlasak bile progress ilerlemiyor" demesinin ikinci sebebi tam olarak
    buydu: sunucu doğru sayıyı verse bile ekran onu istemiyordu.
  */
  qc.invalidateQueries({ queryKey: ['proje'] });
  qc.invalidateQueries({ queryKey: ['is-istatistik'] });

  if (id) {
    qc.invalidateQueries({ queryKey: taskKeys.events(id) });
    qc.invalidateQueries({ queryKey: taskKeys.attachments(id) });
    qc.invalidateQueries({ queryKey: taskKeys.comments(id) });
  }
}

export function useTaskMutations(id?: number) {
  const qc = useQueryClient();
  const bitince = () => tazele(qc, id);

  return {
    olustur: useMutation({
      mutationFn: (govde: TaskSave) => api.post<TaskDetail>('/gorev', govde),
      onSuccess: bitince,
    }),
    guncelle: useMutation({
      mutationFn: (govde: TaskSave) => api.put<TaskDetail>(`/gorev/${id}`, govde),
      onSuccess: bitince,
    }),
    sil: useMutation({
      mutationFn: (gorevId: number) => api.delete<void>(`/gorev/${gorevId}`),
      onSuccess: () => qc.invalidateQueries({ queryKey: taskKeys.all() }),
    }),
    ata: useMutation({
      mutationFn: (atamalar: TaskAssignRequest[]) =>
        api.put<TaskDetail>(`/gorev/${id}/atama`, atamalar),
      onSuccess: bitince,
    }),
    durum: useMutation({
      mutationFn: (govde: TaskStatusRequest) =>
        api.put<TaskDetail>(`/gorev/${id}/durum`, govde),
      onSuccess: bitince,
    }),
    /** Personelin "bitirdim" beyanı — görevi TAMAMLAMAZ, onaya gönderir. */
    tamamlanmayaGonder: useMutation({
      mutationFn: () => api.post<TaskDetail>(`/gorev/${id}/tamamla`),
      onSuccess: bitince,
    }),
    /** Yöneticinin onay/iade kapısı. Ayrı uç, ayrı izin. */
    onay: useMutation({
      mutationFn: (govde: TaskStatusRequest) =>
        api.post<TaskDetail>(`/gorev/${id}/onay`, govde),
      onSuccess: bitince,
    }),
    asama: useMutation({
      mutationFn: (girdi: { asamaId: number; govde: TaskStageRequest }) =>
        api.post<TaskDetail>(`/gorev/${id}/asama/${girdi.asamaId}`, girdi.govde),
      onSuccess: bitince,
    }),
    yorumEkle: useMutation({
      mutationFn: (govde: { metin: string; ustYorumId?: number | null }) =>
        api.post<WorkComment>(`/gorev/${id}/yorum`, govde),
      onSuccess: bitince,
    }),
    yorumSil: useMutation({
      mutationFn: (yorumId: number) => api.delete<void>(`/gorev/yorum/${yorumId}`),
      onSuccess: bitince,
    }),
    ekSil: useMutation({
      mutationFn: (ekId: number) => api.delete<void>(`/gorev/ek/${ekId}`),
      onSuccess: bitince,
    }),
  };
}

/**
 * Dosya yükler.
 *
 * `api` yardımcısı JSON gönderiyor; yükleme <code>multipart/form-data</code>
 * ve gövdeyi tarayıcının kendisi kurmalı — `Content-Type` elle yazılırsa
 * sınır (boundary) eksik kalır ve sunucu gövdeyi ayrıştıramaz.
 */
export async function uploadTaskFile(
  gorevId: number,
  dosya: File,
  asamaId?: number,
): Promise<WorkAttachment> {
  const govde = new FormData();
  govde.append('dosya', dosya);

  const yol = asamaId
    ? `/gorev/${gorevId}/asama/${asamaId}/ek`
    : `/gorev/${gorevId}/ek`;

  return request<WorkAttachment>(yol, { method: 'POST', body: govde });
}

// ═══════════════════════════════════════════════════════════ görev tipi

export function useTaskTypes(filtre: PageParams & { yalnizKullanimda?: boolean }) {
  return useQuery({
    queryKey: taskKeys.types(JSON.stringify(filtre)),
    queryFn: () => api.get<PagedResult<TaskType>>(`/gorev-tipi${queryString(filtre)}`),
    placeholderData: keepPreviousData,
  });
}

/** Etkin birimin KULLANABİLECEĞİ tipler — görev açma formu için. */
export function useUsableTaskTypes() {
  const s = useQuery({
    queryKey: taskKeys.usableTypes(),
    queryFn: () => api.get<TaskType[]>('/gorev-tipi/kullanilabilir'),
    ...REFERANS,
  });
  return { ...s, liste: s.data ?? [] };
}

export function useTaskType(id: number | undefined) {
  return useQuery({
    queryKey: taskKeys.typeDetail(id ?? 0),
    queryFn: () => api.get<TaskType>(`/gorev-tipi/${id}`),
    enabled: !!id,
  });
}

export function useTaskTypeMutations() {
  const qc = useQueryClient();

  // Tip değişince GÖREV önbelleği de tazeleniyor: liste satırları tip adını
  // gösteriyor ve "kullanılabilir tipler" açılır listesi ona bağlı.
  const bitince = () => {
    qc.invalidateQueries({ queryKey: taskKeys.typesAll() });
    qc.invalidateQueries({ queryKey: taskKeys.all() });
  };

  return {
    olustur: useMutation({
      mutationFn: (govde: TaskTypeSave) => api.post<TaskType>('/gorev-tipi', govde),
      onSuccess: bitince,
    }),
    guncelle: useMutation({
      mutationFn: (girdi: { id: number; govde: TaskTypeSave }) =>
        api.put<TaskType>(`/gorev-tipi/${girdi.id}`, girdi.govde),
      onSuccess: bitince,
    }),
    sil: useMutation({
      mutationFn: (id: number) => api.delete<void>(`/gorev-tipi/${id}`),
      onSuccess: bitince,
    }),
  };
}

// ═══════════════════════════════════════════════════════════════ ekip

export function useTeams(
  filtre: PageParams & { altBirimlerDahil?: boolean; yalnizKullanimda?: boolean },
) {
  return useQuery({
    queryKey: taskKeys.teams(JSON.stringify(filtre)),
    queryFn: () => api.get<PagedResult<Team>>(`/ekip${queryString(filtre)}`),
    placeholderData: keepPreviousData,
  });
}

/** Atama açılır listesi için ekipler — kullanımda olanlar, tek sayfa. */
export function useAssignableTeams() {
  const s = useQuery({
    queryKey: taskKeys.teams('atama'),
    queryFn: () =>
      api.get<PagedResult<Team>>(
        `/ekip${queryString({ boyut: LISTE_BOYUTU, yalnizKullanimda: true })}`,
      ),
    ...REFERANS,
  });
  return { ...s, liste: s.data?.veriler ?? [] };
}

export function useTeam(id: number | undefined) {
  return useQuery({
    queryKey: taskKeys.teamDetail(id ?? 0),
    queryFn: () => api.get<Team>(`/ekip/${id}`),
    enabled: !!id,
  });
}

export function useTeamMutations() {
  const qc = useQueryClient();
  const bitince = () => {
    qc.invalidateQueries({ queryKey: taskKeys.teamsAll() });
    qc.invalidateQueries({ queryKey: taskKeys.all() });
  };

  return {
    olustur: useMutation({
      mutationFn: (govde: TeamSave) => api.post<Team>('/ekip', govde),
      onSuccess: bitince,
    }),
    guncelle: useMutation({
      mutationFn: (girdi: { id: number; govde: TeamSave }) =>
        api.put<Team>(`/ekip/${girdi.id}`, girdi.govde),
      onSuccess: bitince,
    }),
    sil: useMutation({
      mutationFn: (id: number) => api.delete<void>(`/ekip/${id}`),
      onSuccess: bitince,
    }),
  };
}

// ═══════════════════════════════════════════════════════════════ personel

/**
 * SEÇİLEBİLİR PERSONEL — görev ataması, ekip üyeliği, proje ekibi.
 *
 * <p>
 * Üç ekran da <code>useUnitUsers()</code> kullanıyordu. O uç
 * (<code>/ayar/birim-kullanicilari</code>) gizli etkinlik davetlisi seçmek
 * için yazılmış ve iki kuralı var: <b>oturum sahibini listeden çıkarır</b> ve
 * yalnızca <b>tam olarak kendi</b> birimini tarar. Sonuç ölçüldü: 13
 * kullanıcılı veritabanında yönetici hesabının ekip üyesi kutusunda TEK kişi
 * çıkıyordu — kişi kendini ne göreve atayabiliyor, ne kurduğu ekibe
 * ekleyebiliyor, ne de yönettiği projeye üye olabiliyordu. Üstelik sunucu
 * proje yöneticisinin üye olmasını ŞART koşuyor.
 * </p>
 *
 * <p>
 * Bu uç etkin birimin ALT AĞACINI tarıyor ve kişinin kendisini içeriyor.
 * </p>
 */
export function usePeople(ara: string, altBirimlerDahil = true) {
  const filtre = JSON.stringify({ ara, altBirimlerDahil });

  const s = useQuery({
    queryKey: taskKeys.people(filtre),
    queryFn: () => api.get<Person[]>(`/personel${queryString({ ara, altBirimlerDahil })}`),
    placeholderData: keepPreviousData,

    // Personel listesi gün içinde değişmiyor; her tuş vuruşunda ağa çıkmak
    // yerine arama sonucu önbellekte tutuluyor.
    staleTime: 5 * 60_000,
  });

  return { ...s, liste: s.data ?? [] };
}

// ═══════════════════════════════════════════════════════════ birim kapsamı

/**
 * Kullanıcının adına çalışabileceği birimler.
 *
 * Liste bir YETKİ BELGESİ DEĞİL: asıl kapı her istekte sunucuda,
 * `X-Etkin-Birim` başlığı yeniden doğrulanıyor. Burası yalnızca arayüzün ne
 * göstereceğini söyler.
 */
export function useUnitScope(etkin: boolean) {
  const s = useQuery({
    queryKey: taskKeys.scope(),
    queryFn: () => api.get<ScopeUnit[]>('/birim-kapsam'),
    enabled: etkin,
    ...REFERANS,
  });
  return { ...s, liste: s.data ?? [] };
}

/**
 * LİSTE SATIRINDAN çalışan hızlı eylemler.
 *
 * <p>
 * <c>useTaskMutations(id)</c> tek bir görevin uçlarına bağlı; liste satırında
 * kullanılamaz çünkü kanca her satır için ayrı çağrılamaz. Bu kanca aynı
 * uçları <b>görev kimliğini parametre alarak</b> sunar, böylece tek bir
 * çağrıyla bütün liste satırları kaydırma eylemlerini paylaşır.
 * </p>
 */
export function useTaskQuickActions() {
  const qc = useQueryClient();
  const tazeleHepsi = () => qc.invalidateQueries({ queryKey: taskKeys.all() });

  return {
    /** Personelin "bitirdim" beyanı — görevi TAMAMLAMAZ, onaya gönderir. */
    tamamlanmayaGonder: useMutation({
      mutationFn: (gorevId: number) => api.post<TaskDetail>(`/gorev/${gorevId}/tamamla`),
      onSuccess: tazeleHepsi,
    }),
    sil: useMutation({
      mutationFn: (gorevId: number) => api.delete<void>(`/gorev/${gorevId}`),
      onSuccess: tazeleHepsi,
    }),
  };
}
