import { Ban } from 'lucide-react';
import { Navigate, useLocation } from 'react-router-dom';
import { EmptyState } from '../components/EmptyState';
import { useSession } from './SessionProvider';

/**
 * Oturum ve yetki kontrolü — <b>İZİN önce gelir</b>.
 *
 * <p>
 * Yetkinin KAYNAĞI sunucudur; buradaki kontrol yalnızca kullanıcıyı 403
 * duvarına çarpmadan önce durdurur ve anlaşılır bir mesaj gösterir.
 * </p>
 *
 * <p>
 * Rota politikaya bağlıyken yeni bir sorun çıkmıştı: yönetim ekranından
 * oluşturulan bir rol hiçbir politikada olmadığı için, <b>izni verilmiş bir
 * ekran bile</b> "erişim yetkiniz yok" diyordu. Menü öğeyi gösteriyor, rota
 * kapıdan çeviriyordu.
 * </p>
 *
 * <p>
 * <c>politika</c> ve <c>rol</c> yalnızca izin listesi HİÇ gelmeyen bir
 * sunucuya bağlanıldığında geri düşülecek yol.
 * </p>
 */
export function ProtectedRoute({
  permission,
  policy,
  role,
  children,
}: {
  permission?: string | string[];
  policy?: string;
  role?: string;
  children: React.ReactNode;
}) {
  const { me, ready, hasPolicy, hasPermission } = useSession();
  const location = useLocation();

  if (!ready) {
    return (
      <div className="grid min-h-dvh place-items-center">
        <p className="text-sm text-text-2">Yükleniyor…</p>
      </div>
    );
  }

  if (!me) {
    const returnTo = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/giris?donus=${returnTo}`} replace />;
  }

  const hasPermissionList = (me.izinler?.length ?? 0) > 0;

  let blocked: boolean;
  if (permission && hasPermissionList) {
    blocked = !hasPermission(permission);
  } else {
    const policyMissing = policy && !hasPolicy(policy);
    // `Sistem` rolü `Admin`in yapabildiği her şeyi yapabilir (sunucudaki
    // yetkilendirme de öyle); rol kontrolü bunu yansıtmalı.
    const roleMissing = role && !(me.roller ?? []).some((r) => r === role || r === 'Sistem');
    blocked = Boolean(policyMissing || roleMissing);
  }

  if (blocked) {
    return (
      <EmptyState
        ikon={Ban}
        baslik="Bu bölüme erişim yetkiniz yok"
        aciklama="Yetki gerekiyorsa birim yöneticinizle görüşün."
      />
    );
  }

  return <>{children}</>;
}
