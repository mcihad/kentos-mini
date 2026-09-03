import { Outlet } from 'react-router-dom';
import { useInstitution } from '../institution/institution';

/**
 * VATANDAŞ PORTALI KABUĞU — kurumun dışarıya bakan tek yüzü.
 *
 * <p>
 * Bu sayfayı görecek kişi büyük ihtimalle kurumun uygulamasını hiç
 * kullanmamış, sokakta, telefonda ve sinirli. Sayfanın işi iki şeyi bir
 * bakışta söylemek: <b>doğru yerdesin</b> ve <b>burada ne yapacağın belli</b>.
 * </p>
 *
 * <p>
 * <b>Renkli tepe + üstüne binen beyaz yaprak.</b> Tepe kurum birincil
 * rengiyle boyanıyor (ton kurum ayarından geliyor, koda yazılı değil) ve
 * içerik yaprağı onun üzerine biraz taşıyor. Düz beyaz bir sayfa da
 * çalışırdı ama telefonda bir form değil bir <i>uygulama</i> gibi durması,
 * bırakılma oranını doğrudan etkiliyor.
 * </p>
 *
 * <p>
 * <b>Gezinme YOK.</b> Menü, geri düğmesi, dil seçici, hiçbiri. Portalda tek
 * bir iş var; ondan uzaklaştıracak her bağlantı yarım kalmış bir bildirim
 * demek.
 * </p>
 *
 * <p>
 * Dipte kurum künyesi duruyor: vatandaşın hangi kuruma yazdığını sayfanın
 * sonunda da görebilmesi, resmî bir kanalda güven meselesi.
 * </p>
 */
export function PortalLayout() {
  const kurum = useInstitution();

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <header
        className="shrink-0 bg-nav-bg text-nav-strong"
        style={{ paddingTop: 'env(safe-area-inset-top, 0px)' }}
      >
        {/* `pb-10` yaprağın binmesi için: yaprak `-mt-6` ile yukarı çıkıyor. */}
        <div className="mx-auto w-full max-w-2xl px-4 pb-10 pt-6">
          <div className="flex items-center gap-3">
            <img
              src={kurum.marka.amblem ?? '/amblem.png'}
              alt=""
              className="h-12 w-12 shrink-0 object-contain"
            />
            <div className="min-w-0">
              <p className="truncate font-display text-lg font-bold leading-tight">
                {kurum.gorunenAd || kurum.ad}
              </p>
              {kurum.birim && (
                <p className="truncate text-xs leading-tight text-nav-fg opacity-80">
                  {kurum.birim}
                </p>
              )}
            </div>
          </div>
        </div>
      </header>

      <main className="mx-auto -mt-6 w-full max-w-2xl flex-1 px-3 pb-10">
        <Outlet />
      </main>

      <footer
        className="shrink-0 px-4 pb-4 pt-2 text-center"
        style={{ paddingBottom: 'calc(env(safe-area-inset-bottom, 0px) + 1rem)' }}
      >
        <p className="text-2xs text-ink-3">
          {kurum.kunye || kurum.gorunenAd || kurum.ad}
        </p>
        {kurum.telefon && (
          <p className="mt-0.5 text-2xs text-ink-3">
            Acil durumlar için: <span className="tabular-nums">{kurum.telefon}</span>
          </p>
        )}
      </footer>
    </div>
  );
}
