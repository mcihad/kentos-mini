import { ChevronLeft, ClipboardList, LayoutGrid, LogOut, Map, PlusCircle } from 'lucide-react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useSession } from '../auth/SessionProvider';
import { PERMISSION } from '../components/permissions';
import { cn } from '../components/utils';
import { useInstitution } from '../institution/institution';

/**
 * SAHA KABUĞU — telefonda yerli uygulama gibi duran, kendi başına bir yüzey.
 *
 * <p>
 * <b>Neden <code>AppShell</code> değil.</b> Panel, masaüstünde yirmi yedi
 * menü öğesi taşıyan bir kurumsal kabuk. Saha personeli ise telefonu tek
 * elle, çoğu zaman güneş altında ve eldivenle kullanıyor; günde onlarca kez
 * açıyor ve her açışında yaptığı şey üç işten biri. Aynı kabuğu küçültmek,
 * o üç işi yirmi yedi öğenin arasına gömmek olurdu.
 * </p>
 *
 * <p>
 * <b>Neden portal kabuğu da değil.</b> O, vatandaşın tek seferlik ziyareti
 * için: gezinmesi hiç yok. Sahada gezinme sürekli — iş listesiyle harita
 * arasında gidip gelinir — ve yalnızca geri düğmesiyle gezinilen bir
 * uygulama telefonda her zaman "web sitesi" gibi durur.
 * </p>
 *
 * <h3>Gramer</h3>
 * <ul>
 *   <li><b>Renkli üst şerit.</b> Kurum birincil rengi; ton kurum ayarından
 *       geliyor, koda yazılı değil. Durum çubuğunun altındaki güvenli alan
 *       şeridin kendi zeminiyle doluyor — beyaz bir bant, uygulamayı
 *       tarayıcıya çeviriyordu.</li>
 *   <li><b>Alt sekme çubuğu.</b> Üç hedef, hepsi başparmak menzilinde. Üstte
 *       gezinme olsaydı tek elle kullanan biri telefonu her seferinde
 *       kaydırmak zorunda kalırdı.</li>
 *   <li><b>Sekmeler ROLE GÖRE.</b> Yalnızca tespit girmesi beklenen personel
 *       "İşlerim"i, yalnızca iş yapan personel "Tespit"i görmez. Çalışmayan
 *       bir sekme göstermek, sahada denemekle geçen zaman demek.</li>
 * </ul>
 */

/** Alt çubuktaki bir hedef. */
type Sekme = {
  yol: string;
  etiket: string;
  ikon: typeof ClipboardList;
  /** Bu izinlerden EN AZ BİRİ gerekiyor. */
  izin: string[];
};

const SEKMELER: Sekme[] = [
  { yol: '/saha', etiket: 'İşlerim', ikon: ClipboardList, izin: [PERMISSION.gorevGoruntule] },
  {
    yol: '/saha/tespit',
    etiket: 'Tespit',
    ikon: PlusCircle,
    izin: [PERMISSION.sahaTespit, PERMISSION.gorevEkle],
  },
  // Harita, panelin `/harita` ekranıyla AYNI bileşen: sahada ayrı bir harita
  // yazmak, iki yerde düzeltilecek bir küme mantığı demekti.
  { yol: '/saha/harita', etiket: 'Harita', ikon: Map, izin: [PERMISSION.gorevGoruntule] },
];

export function SahaLayout() {
  const kurum = useInstitution();
  const { me, hasPermission, signOut } = useSession();
  const konum = useLocation();
  const gezin = useNavigate();

  const sekmeler = SEKMELER.filter((s) => hasPermission(s.izin));

  /*
    ALT SEKMEDE OLMAYAN BİR EKRAN (görev detayı) AÇIKSA GERİ DÜĞMESİ ÇIKIYOR.

    Sekme çubuğu "nerede olduğunu" söylüyor ama derinleşilen bir ekranda
    hiçbir sekme etkin değil; geri dönmenin görünür bir yolu olmadan
    kullanıcı tarayıcının geri tuşuna mahkûm kalırdı — ana ekrana eklenmiş
    bir uygulamada o tuş yok.
  */
  const derinde = !SEKMELER.some((s) => s.yol === konum.pathname);

  /*
    "PANELE GEÇ" YALNIZCA PANELE GİREBİLENDE.

    Saha işaretli olmak bir kilit değil; ama gerçekten yalnızca saha izni
    olan birine çalışmayan bir çıkış göstermek, kilit olmadığını değil
    uygulamanın bozuk olduğunu düşündürürdü.
    `gorevGoruntule` saha için zaten gerekli, bu yüzden ölçüt panele ait bir
    izin: ajanda modülü.
  */
  const paneleGecebilir = hasPermission(PERMISSION.ajandaGoruntule);

  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      {/*
        Şeridin zemini `--nav-bg`: kurum birincil renginden türeyen, kenar
        çubuğuyla aynı ton. Ayrı bir renk seçmek, aynı kurumun iki yüzünü
        birbirine yabancı yapardı.
      */}
      <header
        /*
          ALT KENAR ÇİZGİ DEĞİL, İÇERİ GÖLGE: lacivert şeridin altına gri bir
          kenarlık koymak onu "kutu" gibi gösteriyordu. İçeriden beyaz bir
          kıl payı, ışığın şeridin tepesine düştüğü izlenimini veriyor —
          telefonlardaki sistem çubuklarının yaptığı şey.
        */
        className="sticky top-0 z-30 shrink-0 bg-nav-bg text-nav-strong shadow-[inset_0_-1px_0_rgba(255,255,255,0.10)]"
        style={{ paddingTop: 'env(safe-area-inset-top, 0px)' }}
      >
        <div className="mx-auto flex h-16 w-full max-w-2xl items-center gap-2.5 px-3">
          {derinde ? (
            <SeritDugmesi etiket="Geri" tikla={() => gezin(-1)}>
              <ChevronLeft size={22} />
            </SeritDugmesi>
          ) : (
            /*
              AMBLEM SAYDAM BİR DAİRENİN İÇİNDE.

              Lacivert zeminde çıplak duran amblem, zeminden kopuk bir çıkartma
              gibi görünüyordu; hafif aydınlatılmış daire onu şeridin bir
              parçası yapıyor ve sağdaki düğmelerle aynı geometriye sokuyor.
            */
            <span className="grid h-10 w-10 flex-none place-items-center rounded-full bg-white/10">
              <img
                src={kurum.marka.amblem ?? '/amblem.png'}
                alt=""
                className="h-7 w-7 object-contain"
              />
            </span>
          )}

          <div className="min-w-0 flex-1">
            <p className="truncate font-display text-[17px] font-bold leading-tight">
              {baslik(konum.pathname)}
            </p>
            {/* Kimin adına iş yapıldığı sahada da görünmeli: aynı telefonu
                iki kişi kullanabiliyor. */}
            <p className="truncate text-2xs leading-tight text-nav-fg/75">
              {me?.tamAd}
              {me?.birimAd ? ` · ${me.birimAd}` : ''}
            </p>
          </div>

          {paneleGecebilir && (
            <SeritDugmesi etiket="Panele geç" tikla={() => gezin('/')}>
              <LayoutGrid size={19} />
            </SeritDugmesi>
          )}

          <SeritDugmesi etiket="Çıkış" tikla={() => void signOut()}>
            <LogOut size={19} />
          </SeritDugmesi>
        </div>
      </header>

      {/*
        `pb-4` yeterli: alt çubuk akışın İÇİNDE (sabit değil), dolayısıyla
        son satırı örtmüyor. Sabit olsaydı klavye açıldığında iOS'ta
        içeriğin üstüne biner ve bir alanı görünmez yapardı.
      */}
      <main className="mx-auto w-full max-w-2xl flex-1 px-3 pb-4 pt-3">
        <Outlet />
      </main>

      {sekmeler.length > 1 && (
        <nav
          aria-label="Saha"
          className="sticky bottom-0 z-30 shrink-0 border-t border-line bg-surface"
          style={{ paddingBottom: 'env(safe-area-inset-bottom, 0px)' }}
        >
          <ul className="mx-auto flex max-w-2xl">
            {sekmeler.map((s) => {
              const Ikon = s.ikon;
              return (
                <li key={s.yol} className="flex-1">
                  <NavLink
                    to={s.yol}
                    end
                    className={({ isActive }) =>
                      cn(
                        'flex h-[62px] flex-col items-center justify-center gap-1',
                        isActive ? 'text-brand' : 'text-ink-3',
                      )
                    }
                  >
                    {({ isActive }) => (
                      <>
                        {/*
                          ETKİN SEKMENİN İKONU HAPIN İÇİNDE.

                          Önce etkinliği yalnızca renk söylüyordu ve güneş
                          altında, ekran parlarken iki gri ile bir mavi
                          arasındaki fark kayboluyordu. Dolgulu zemin renge
                          bağlı olmayan ikinci bir işaret veriyor.
                        */}
                        <span
                          className={cn(
                            'grid h-7 w-16 place-items-center rounded-pill transition-colors',
                            isActive ? 'bg-brand-soft' : 'bg-transparent',
                          )}
                        >
                          <Ikon size={21} strokeWidth={isActive ? 2.4 : 1.9} />
                        </span>
                        <span
                          className={cn('text-2xs', isActive ? 'font-bold' : 'font-medium')}
                        >
                          {s.etiket}
                        </span>
                      </>
                    )}
                  </NavLink>
                </li>
              );
            })}
          </ul>
        </nav>
      )}
    </div>
  );
}

/**
 * Şeritteki ikon düğmesi.
 *
 * Zemin SAYDAM BEYAZ, kenarlık değil: lacivertin üstünde kenarlıklı bir kutu
 * ağır duruyor, çıplak ikon ise düğme olduğunu söylemiyordu. Hafif
 * aydınlatılmış daire ikisinin arasını buluyor ve amblemle aynı geometriyi
 * paylaşıyor.
 */
function SeritDugmesi({
  etiket, tikla, children,
}: {
  etiket: string; tikla: () => void; children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={tikla}
      aria-label={etiket}
      title={etiket}
      // 40px görünür daire, 44px dokunma alanı (`after`): eldivenli parmak
      // daha küçüğünü ıskalıyor ama 44px'lik bir daire şeridi dolduruyordu.
      className="relative grid h-10 w-10 flex-none place-items-center rounded-full
        bg-white/10 text-nav-strong transition-colors active:bg-white/20
        after:absolute after:inset-[-2px] after:content-['']"
    >
      {children}
    </button>
  );
}

/**
 * Şerit başlığı.
 *
 * Rota tablosundan türetmek yerine burada duruyor: saha üç ekran ve bir
 * eşleme tablosu, gezinme ağacının tamamını buraya taşımaktan basit.
 */
function baslik(yol: string): string {
  if (yol.startsWith('/saha/tespit')) return 'Tespit gir';
  if (yol.startsWith('/saha/harita')) return 'Harita';
  if (yol.startsWith('/saha/gorev')) return 'Görev';
  return 'Saha';
}
