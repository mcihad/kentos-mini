import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import {
  CalendarDays, Check, CircleAlert, Flower2, MapPin, StickyNote, User,
} from 'lucide-react';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { FieldWrapper, Input } from '../../components/Field';
import { Skeleton } from '../../components/Skeleton';
import { api } from '../../data/client';
import { dateTime } from '../../data/format';
import { haptic } from '../../data/haptics';
import { useInstitution } from '../../institution/institution';

type TeslimKarti = {
  etkinlikBasligi: string;
  etkinlikTarihi: string;
  etkinlikKonumu: string | null;
  alici: string | null;
  adres: string | null;
  not: string | null;
  kurumAdi: string | null;
  teslimEdildi: boolean;
  teslimTarihi: string | null;
};

/**
 * ÇİÇEK TESLİM EKRANI — çiçekçinin SMS bağlantısından açtığı sayfa.
 *
 * <h4>Neden giriş yok</h4>
 * <p>
 * Çiçekçi kurumun kullanıcısı değil: hesabı, rolü, parolası yok. Bağlantı ona
 * SMS ile gidiyor ve tek yetki belirteci bağlantıdaki <b>tahmin edilemez
 * kimlik</b>. Sayfa bu yüzden <c>ProtectedRoute</c> dışında ve kurumsal
 * kabuğu (menü, sekmeler) hiç çizmiyor — o menüyü görecek biri burada yok.
 * </p>
 *
 * <h4>Neyi düzeltiyor</h4>
 * <p>
 * SMS'teki bağlantı <c>/Cicekci/CicekKart/{'{'}kimlik{'}'}</c> adresine
 * gidiyordu ve o adresin karşılığı olan sayfa <b>yoktu</b>: çiçekçi bağlantıya
 * dokununca 404 görüyordu. Kurum içi uçlar da giriş ve yetki istediği için
 * teslim işaretlemesi hiçbir şekilde yapılamıyordu — akış baştan sona kırıktı.
 * </p>
 *
 * <h4>Doğrulama kodu</h4>
 * <p>
 * Kod bu sayfada <b>gösterilmez</b>; yalnızca talimat SMS'inde geçer. Çiçeği
 * gerçekten teslim edenin elinde olduğunu kanıtlayan tek şey o. Beş hatalı
 * denemeden sonra kart kilitlenir.
 * </p>
 */
export function FlowerDelivery() {
  const { kimlik } = useParams<{ kimlik: string }>();
  const kurum = useInstitution();
  const [kod, setKod] = useState('');
  const [hata, setHata] = useState<string | null>(null);

  const kart = useQuery({
    queryKey: ['cicek', 'teslim-karti', kimlik] as const,
    queryFn: () => api.get<TeslimKarti>(`/cicek/teslim-karti/${kimlik}`),
    enabled: !!kimlik,
    retry: false,
  });

  const teslimEt = useMutation({
    mutationFn: () =>
      api.post<boolean>(`/cicek/teslim-karti/${kimlik}/teslim`, {
        dogrulamaKodu: Number(kod),
      }),
    onSuccess: () => {
      haptic('basari');
      setHata(null);
      void kart.refetch();
    },
    onError: (h: Error) => {
      haptic('hata');
      // Sunucu kalan deneme hakkını mesajda yazıyor; olduğu gibi gösterilir.
      setHata(h.message);
    },
  });

  const gecerliKod = /^\d{5}$/.test(kod.trim());

  return (
    <div className="mx-auto flex min-h-dvh max-w-[560px] flex-col gap-4 p-4">
      <header className="flex items-center gap-3 pt-2">
        <span className="grid size-11 shrink-0 place-items-center rounded-lg bg-brand-soft text-brand">
          <Flower2 size={22} strokeWidth={1.9} />
        </span>
        <div className="min-w-0">
          <h1 className="font-display text-xl font-bold">Çiçek teslim fişi</h1>
          <p className="text-sm text-text-3">
            {kart.data?.kurumAdi || kurum.gorunenAd || kurum.ad || 'Kurum'}
          </p>
        </div>
      </header>

      {kart.isLoading && (
        <Card className="space-y-3 p-4">
          <Skeleton className="h-6 w-2/3" />
          <Skeleton className="h-20 w-full" />
        </Card>
      )}

      {kart.isError && (
        <Card className="p-5 text-center">
          <span className="mx-auto mb-3 grid size-11 place-items-center rounded-lg bg-(--st-no-bg) text-(--st-no)">
            <CircleAlert size={22} />
          </span>
          <p className="font-display text-lg font-bold">Kart bulunamadı</p>
          <p className="mt-1 text-sm text-text-2">
            Bağlantı hatalı olabilir ya da talimat iptal edilmiştir. Size SMS
            gönderen personelle görüşün.
          </p>
        </Card>
      )}

      {kart.data && (
        <>
          <Card className="divide-y divide-line">
            <div className="p-4">
              <p className="text-xs font-semibold text-text-3">ETKİNLİK</p>
              <p className="mt-1 font-display text-lg font-bold metin-guzel">
                {kart.data.etkinlikBasligi}
              </p>
              <p className="mt-1.5 flex items-center gap-1.5 text-sm text-text-2">
                <CalendarDays size={14} aria-hidden />
                {dateTime(kart.data.etkinlikTarihi)}
              </p>
              {kart.data.etkinlikKonumu && (
                <p className="mt-1 flex items-center gap-1.5 text-sm text-text-2">
                  <MapPin size={14} aria-hidden />
                  {kart.data.etkinlikKonumu}
                </p>
              )}
            </div>

            {(kart.data.alici || kart.data.adres) && (
              <div className="p-4">
                <p className="text-xs font-semibold text-text-3">TESLİM</p>
                {kart.data.alici && (
                  <p className="mt-1 flex items-center gap-1.5 text-base font-medium">
                    <User size={14} aria-hidden className="text-text-3" />
                    {kart.data.alici}
                  </p>
                )}
                {kart.data.adres && (
                  <p className="mt-1 flex items-start gap-1.5 text-sm text-text-2">
                    <MapPin size={14} aria-hidden className="mt-0.5 shrink-0 text-text-3" />
                    <span className="wrap-anywhere">{kart.data.adres}</span>
                  </p>
                )}
              </div>
            )}

            {kart.data.not && (
              <div className="p-4">
                <p className="text-xs font-semibold text-text-3">KART NOTU</p>
                <p className="mt-1 flex items-start gap-1.5 text-base metin-guzel">
                  <StickyNote size={14} aria-hidden className="mt-1 shrink-0 text-text-3" />
                  {kart.data.not}
                </p>
              </div>
            )}
          </Card>

          {kart.data.teslimEdildi ? (
            /*
              TESLİM EDİLDİ — form yerine sonuç.

              Kart bir kez işaretlendikten sonra kod alanını göstermek, işi
              bitmiş çiçekçiye "bir şey daha yap" demek olurdu.
            */
            <Card className="flex items-start gap-3 p-4">
              <span className="grid size-9 shrink-0 place-items-center rounded-md bg-(--st-ok-bg) text-(--st-ok)">
                <Check size={18} strokeWidth={2.6} />
              </span>
              <div className="min-w-0">
                <p className="font-display text-base font-bold text-(--st-ok)">
                  Teslim edildi
                </p>
                {kart.data.teslimTarihi && (
                  <p className="mt-0.5 text-sm text-text-2">
                    {dateTime(kart.data.teslimTarihi)}
                  </p>
                )}
              </div>
            </Card>
          ) : (
            <Card className="p-4">
              <p className="font-display text-base font-bold">Teslim ettiniz mi?</p>
              <p className="mt-1 text-sm text-text-2">
                Çiçeği teslim ettikten sonra size SMS ile gelen beş haneli
                doğrulama kodunu girin.
              </p>

              <form
                className="mt-3"
                onSubmit={(e) => {
                  e.preventDefault();
                  if (gecerliKod && !teslimEt.isPending) teslimEt.mutate();
                }}
              >
                <FieldWrapper
                  etiket="Doğrulama kodu"
                  id="cicek-kod"
                  hata={hata ?? undefined}
                  ipucu={hata ? undefined : 'SMS ile gönderilen beş haneli kod'}
                >
                  <Input
                    id="cicek-kod"
                    value={kod}
                    onChange={(e) => {
                      setKod(e.target.value.replace(/\D/g, '').slice(0, 5));
                      setHata(null);
                    }}
                    inputMode="numeric"
                    autoComplete="one-time-code"
                    placeholder="12345"
                    className="font-mono tracking-[0.3em]"
                    hatali={!!hata}
                  />
                </FieldWrapper>

                <Button
                  type="submit"
                  boyut="mobil"
                  className="mt-1 w-full"
                  disabled={!gecerliKod || teslimEt.isPending}
                >
                  <Check size={16} />
                  {teslimEt.isPending ? 'Gönderiliyor…' : 'Teslim ettim'}
                </Button>
              </form>
            </Card>
          )}
        </>
      )}

      <p className="mt-auto pb-2 text-center text-2xs text-text-3">
        Bu sayfa yalnızca çiçek teslimi içindir; giriş yapmanız gerekmez.
      </p>
    </div>
  );
}
