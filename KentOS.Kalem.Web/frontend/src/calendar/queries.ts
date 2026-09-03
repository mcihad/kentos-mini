import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../data/queryKeys';
import { api } from '../data/client';
import { localToServer } from '../data/time';
import { windowKey } from './viewWindow';
import { RECURRENCE_SCOPE, type CalendarEvent, type RecurrenceScope } from './types';

export function useRange(pencere: { bas: Date; bit: Date }) {
  const [bas, bit] = windowKey(pencere);
  return useQuery({
    queryKey: queryKeys.event.window(bas, bit),
    queryFn: () => api.post<CalendarEvent[]>('/takvim/aralik', { baslangic: bas, bitis: bit }),
  });
}

export function useDayCounts(yil: number) {
  return useQuery({
    queryKey: ['etkinlik', 'sayac', yil] as const,
    queryFn: () => api.get<{ gun: string; adet: number }[]>(`/takvim/sayac?yil=${yil}`),
  });
}

export type TimeChange = {
  id: number;
  baslangic: Date;
  bitis: Date;
  kapsam: RecurrenceScope;
};

/**
 * Sürükleme / yeniden boyutlandırma sonucunu kaydeder.
 *
 * DİKKAT: Bu istek YALNIZCA zaman taşır — tekrar kuralı (rrule) ASLA
 * gönderilmez. İstemciler kuralı formun başlangıç tarihinden türettiği için,
 * bir tekrarı başka güne sürüklemek sunucuda "kural değişti" diye okunuyor,
 * seri bölünüyor ve etkinlik kaybolmuş görünüyordu. Ayrı uç nokta bunu
 * yapısal olarak imkânsız kılar.
 */
export function useClock(pencere: { bas: Date; bit: Date }) {
  const qc = useQueryClient();
  const [bas, bit] = windowKey(pencere);
  const anahtar = queryKeys.event.window(bas, bit);

  return useMutation({
    mutationFn: (d: TimeChange) =>
      api.patch<void>(`/etkinlik/${d.id}/zaman`, {
        baslangic: localToServer(d.baslangic),
        bitis: localToServer(d.bitis),
        kapsam: d.kapsam,
      }),

    // İyimser güncelleme: bırakma anında kart yeni yerinde görünsün.
    onMutate: async (d) => {
      await qc.cancelQueries({ queryKey: anahtar });
      const onceki = qc.getQueryData<CalendarEvent[]>(anahtar);

      qc.setQueryData<CalendarEvent[]>(anahtar, (eski) =>
        (eski ?? []).map((e) =>
          e.id === d.id
            ? {
                ...e,
                baslangic: localToServer(d.baslangic),
                bitis: localToServer(d.bitis),
              }
            : e,
        ),
      );

      return { onceki };
    },

    onError: (_hata, _d, baglam) => {
      if (baglam?.onceki) qc.setQueryData(anahtar, baglam.onceki);
    },

    onSettled: (_veri, _hata, d) => {
      if (d.kapsam === RECURRENCE_SCOPE.yalnizca) {
        // Tek kayıt değişti; ayrıca sunucu `seriAyrik` bayrağını açmış olabilir.
        qc.invalidateQueries({ queryKey: ['etkinlik', 'pencere'] });
        qc.invalidateQueries({ queryKey: queryKeys.event.detail(d.id) });
      } else {
        // Seri bölünmüş ya da yeniden üretilmiş olabilir: satırların kimlikleri
        // bile değişmiş olabileceği için hedefli yama güvenli değil.
        qc.invalidateQueries({ queryKey: queryKeys.event.all() });
      }
    },
  });
}
