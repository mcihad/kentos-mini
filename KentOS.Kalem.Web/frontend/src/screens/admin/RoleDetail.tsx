import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Lock, Plus, ShieldCheck, UserMinus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { EmptyState } from '../../components/EmptyState';
import { Button, IconButton } from '../../components/Button';
import { SkeletonRows } from '../../components/Skeleton';
import { Card, CardHeader } from '../../components/Card';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { SearchSelect } from '../../components/SearchSelect';
import { useToast } from '../../components/Toast';
import { useSession } from '../../auth/SessionProvider';
import { initials, number } from '../../data/format';
import { api, queryString, type PagedResult } from '../../data/client';
import type { UserSummary, Role } from '../../data/types';

/**
 * Rol detayı — role bağlı kullanıcılar.
 *
 * <p>
 * Eski sistemde bir rolün kimlerde olduğunu görmenin tek yolu kullanıcıları
 * tek tek açmaktı. "Sekreter kim?" sorusu 40 kullanıcı açmayı gerektiriyordu.
 * </p>
 *
 * <p>
 * Kural sunucuda: korumalı roller (<c>Sistem</c>, <c>BaskanOzel</c>) yalnızca
 * <c>Sistem</c> rolündeki bir kullanıcı tarafından atanabilir ve bir
 * kullanıcının <b>son rolü</b> alınamaz — rolsüz kullanıcı giriş yapıyor ama
 * hiçbir ekranı göremiyordu.
 * </p>
 */
export default function RoleDetail() {
  const { ad = '' } = useParams<{ ad: string }>();
  const git = useNavigate();
  const qc = useQueryClient();
  const { bildir } = useToast();
  const { me } = useSession();

  const [eklenecek, setEklenecek] = useState<number | null>(null);
  const [eklenecekAd, setEklenecekAd] = useState<string | null>(null);
  const [arama, setArama] = useState('');
  const [cikarilacak, setCikarilacak] = useState<UserSummary | null>(null);

  const roller = useQuery({
    queryKey: ['yonetim', 'roller'] as const,
    queryFn: () => api.get<Role[]>('/yonetim/roller'),
  });

  const uyeler = useQuery({
    queryKey: ['yonetim', 'rol', ad, 'kullanicilar'] as const,
    queryFn: () => api.get<UserSummary[]>(`/yonetim/roller/${encodeURIComponent(ad)}/kullanicilar`),
    enabled: ad.length > 0,
  });

  const rol = (roller.data ?? []).find((r) => r.ad === ad);
  const uyeIdleri = useMemo(
    () => new Set((uyeler.data ?? []).map((k) => k.id)),
    [uyeler.data],
  );

  // Aday listesi: rolde OLMAYAN kullanıcılar. Zaten rolde olanı listelemek,
  // eklendiğinde hiçbir şey değişmediği için hata gibi görünüyordu.
  const adaylar = useQuery({
    queryKey: ['yonetim', 'kullanicilar', 'aday', arama] as const,
    queryFn: () =>
      api.get<PagedResult<UserSummary>>(
        `/yonetim/kullanicilar${queryString({ sayfa: 1, boyut: 25, ara: arama })}`,
      ),
    enabled: atayabilirMi(rol, me?.roller),
  });

  const adayOgeleri = (adaylar.data?.veriler ?? [])
    .filter((k) => !uyeIdleri.has(k.id))
    .map((k) => ({
      id: k.id!,
      ad: `${k.ad ?? ''} ${k.soyad ?? ''}`.trim() || k.kullaniciAdi!,
      aciklama: `@${k.kullaniciAdi}${k.birimAdi ? ` · ${k.birimAdi}` : ''}`,
    }));

  const atayabilir = atayabilirMi(rol, me?.roller);

  const ekle = useMutation({
    mutationFn: (kullaniciId: number) =>
      api.post<void>(`/yonetim/roller/${encodeURIComponent(ad)}/kullanicilar/${kullaniciId}`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim'] });
      setEklenecek(null);
      setEklenecekAd(null);
      bildir('basari', 'Kullanıcı role eklendi');
    },
    onError: (h: Error) => bildir('hata', 'Eklenemedi', h.message),
  });

  const cikar = useMutation({
    mutationFn: (kullaniciId: number) =>
      api.delete<void>(`/yonetim/roller/${encodeURIComponent(ad)}/kullanicilar/${kullaniciId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['yonetim'] });
      setCikarilacak(null);
      bildir('basari', 'Kullanıcı rolden çıkarıldı');
    },
    // "Son rolü alınamaz" gibi anlaşılır sunucu mesajları olduğu gibi gösterilir.
    onError: (h: Error) => bildir('hata', 'Çıkarılamadı', h.message),
  });

  return (
    <div className="space-y-3.5">
      <div className="flex items-start gap-2.5">
        <IconButton etiket="Geri" onClick={() => git('/yonetim')}>
          <ArrowLeft size={17} />
        </IconButton>

        <div className="min-w-0 flex-1">
          <h1 className="flex items-center gap-2 truncate font-baslik text-xl font-semibold leading-tight">
            {ad}
            {rol?.korumali && (
              <span
                className="inline-flex items-center gap-1 rounded-full bg-(--gold-tint) px-2 py-0.5 text-2xs font-semibold text-(--gold-strong)"
                title="Yalnızca Sistem rolündeki kullanıcılar atayabilir"
              >
                <Lock size={10} />
                korumalı
              </span>
            )}
          </h1>
          <p className="text-sm text-text-3">
            {number((uyeler.data ?? []).length)} kullanıcı bu role sahip
          </p>
        </div>
      </div>

      {atayabilir ? (
        <Card>
          <CardHeader
            baslik="Role kullanıcı ekle"
            aciklama="Kullanıcı adına göre arayın; zaten rolde olanlar listelenmez."
          />
          <div className="flex flex-col gap-2.5 p-4 sm:flex-row sm:items-end">
            <div className="min-w-0 flex-1">
              <SearchSelect
                id="rol-kullanici"
                deger={eklenecek}
                seciliAd={eklenecekAd}
                degistir={(id, ad) => {
                  setEklenecek(id);
                  setEklenecekAd(ad);
                }}
                ogeler={adayOgeleri}
                ara={arama}
                araDegistir={setArama}
                yukleniyor={adaylar.isFetching}
                yerTutucu="Kullanıcı ara"
                bosMetin="Eşleşen kullanıcı yok."
              />
            </div>
            <Button
              disabled={eklenecek === null || ekle.isPending}
              onClick={() => eklenecek !== null && ekle.mutate(eklenecek)}
            >
              <Plus size={14} />
              Ekle
            </Button>
          </div>
        </Card>
      ) : (
        <Card className="flex items-start gap-2.5 p-3.5">
          <Lock size={15} className="mt-0.5 shrink-0 text-text-3" />
          <p className="text-sm text-text-2">
            <b>{ad}</b> korumalı bir roldür; yalnızca <b>Sistem</b> rolündeki kullanıcılar
            atama yapabilir. Bu kural sunucuda da uygulanır.
          </p>
        </Card>
      )}

      {uyeler.isLoading ? (
        <SkeletonRows adet={6} />
      ) : (uyeler.data ?? []).length === 0 ? (
        <EmptyState
          ikon={ShieldCheck}
          baslik="Bu rolde kullanıcı yok"
          aciklama="Yukarıdan kullanıcı arayarak role ekleyebilirsiniz."
        />
      ) : (
        <Card>
          <CardHeader baslik="Roldeki kullanıcılar" />
          <ul className="divide-y divide-border">
            {(uyeler.data ?? []).map((k) => (
              <li key={k.id} className="flex items-center gap-3 px-4 py-2.5">
                <span
                  className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-brand-tint font-baslik text-xs font-semibold text-brand-2"
                  aria-hidden
                >
                  {initials(`${k.ad ?? ''} ${k.soyad ?? ''}`.trim() || k.kullaniciAdi)}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">
                    {`${k.ad ?? ''} ${k.soyad ?? ''}`.trim() || k.kullaniciAdi}
                    <span className="ml-2 font-normal text-text-3">@{k.kullaniciAdi}</span>
                  </p>
                  <p className="truncate text-xs text-text-3">
                    {k.birimAdi ?? 'Birimsiz'}
                    {(k.roller ?? []).length > 1
                      ? ` · ${(k.roller ?? []).filter((r) => r !== ad).join(', ')}`
                      : ''}
                  </p>
                </div>
                {atayabilir && (
                  <IconButton
                    etiket="Rolden çıkar"
                    onClick={() => setCikarilacak(k)}
                    className="hover:text-(--st-no)"
                  >
                    <UserMinus size={15} />
                  </IconButton>
                )}
              </li>
            ))}
          </ul>
        </Card>
      )}

      <ConfirmDialog
        acik={cikarilacak !== null}
        kapat={() => setCikarilacak(null)}
        baslik="Rolden çıkarılsın mı?"
        aciklama={`${`${cikarilacak?.ad ?? ''} ${cikarilacak?.soyad ?? ''}`.trim() || cikarilacak?.kullaniciAdi} kullanıcısı "${ad}" rolünden çıkarılacak.`}
        onayEtiketi="Çıkar"
        yikici
        onayla={() => cikarilacak?.id && cikar.mutate(cikarilacak.id)}
      />
    </div>
  );
}

/**
 * Korumalı role atama yetkisi.
 *
 * Kural sunucuda zorlanıyor; burada yalnızca düğmeyi gizliyoruz — kullanıcıya
 * basılabilir görünüp 403 dönen bir düğme göstermek dürüst değil.
 */
function atayabilirMi(rol: Role | undefined, roller: string[] | undefined): boolean {
  return !rol?.korumali || (roller ?? []).includes('Sistem');
}
