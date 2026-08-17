import { ArrowLeft } from 'lucide-react';
import { Outlet, useNavigate } from 'react-router-dom';
import { useInstitution } from '../institution/institution';

/**
 * KABUKSUZ YERLEŞİM — saha ve vatandaş ekranları.
 *
 * <p>
 * <code>AppShell</code> kenar çubuğu, alt sekme çubuğu, bildirim ve yardım
 * ikonlarıyla geliyor. Saha personeli telefonu <b>tek elle, güneş altında,
 * eldivenle</b> kullanıyor: ekranın tamamı işe ayrılmalı ve gezinme
 * kalabalığı olmamalı. Vatandaş portalı için ise kurumsal menüyü göstermek
 * anlamsız — orada kimse oturum açmış değil.
 * </p>
 *
 * <p>
 * Yalnızca ince bir üst şerit var: kurum adı ve (varsa) geri düğmesi. Şerit
 * bile atılabilirdi ama vatandaşın hangi kurumun sayfasında olduğunu
 * görmesi gerekiyor.
 * </p>
 */
export function BareLayout({ geriYok }: { geriYok?: boolean }) {
  const kurum = useInstitution();
  const gezin = useNavigate();

  return (
    <div className="min-h-dvh bg-canvas">
      <header className="sticky top-0 z-20 flex h-14 items-center gap-2 border-b border-line bg-surface px-3">
        {!geriYok && (
          <button
            type="button"
            onClick={() => gezin(-1)}
            aria-label="Geri"
            // 44px dokunma hedefi: eldivenli parmak daha küçüğünü ıskalıyor.
            className="grid h-11 w-11 flex-none place-items-center rounded-control text-ink-2 active:bg-sunken"
          >
            <ArrowLeft size={20} />
          </button>
        )}

        <div className="min-w-0 flex-1">
          <p className="truncate font-display text-sm font-bold leading-tight text-ink">
            {kurum?.gorunenAd || kurum?.ad || 'Belediye'}
          </p>
          {kurum?.birim && (
            <p className="truncate text-2xs text-ink-3">{kurum.birim}</p>
          )}
        </div>
      </header>

      {/*
        `pb-24`: sabit alt eylem çubuğu olan ekranlarda içeriğin son satırı
        çubuğun altında kalıyordu.
      */}
      <main className="mx-auto max-w-2xl px-3 pb-24 pt-3">
        <Outlet />
      </main>
    </div>
  );
}
