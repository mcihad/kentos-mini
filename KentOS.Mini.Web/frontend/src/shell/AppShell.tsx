import { Loader2, LogOut, Menu, Moon, Palette, PanelLeftClose, PanelLeftOpen, Sun } from 'lucide-react';
import { PERMISSION } from '../components/permissions';
import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { IconButton } from '../components/Button';
import { cn } from '../components/utils';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { initials } from '../data/format';
import { useSession } from '../auth/SessionProvider';
import { useTheme } from '../theme/ThemeProvider';
import { useHasIncomingFile } from '../data/hooks';
import { isActive, NAVIGATION, pageTitle, type NavigationItem } from './navigation';
import { MobileMenu } from './MobileMenu';
import { LargeTitleProvider } from './mobile/LargeTitle';
import { ThemePanel } from '../theme/ThemePanel';
import { NotificationBridge } from '../notifications/NotificationBridge';
import { NotificationPermissionCard } from '../notifications/PermissionCard';
import { NotificationCenter } from '../notifications/NotificationCenter';
import { HelpButton } from '../help/HelpButton';
import { InstallButton } from '../pwa/InstallButton';
import { useInstitution } from '../institution/institution';

/**
 * Uygulama kabuğu — design.md §5.
 *
 * TEK bileşen, İKİ görünüm: 768px altında tabbar (54px appbar + 60px tabbar),
 * üstünde sidebar (258/76px + 64px appbar). Fark yalnızca duyarlı sınıflardan
 * gelir; ayrı mobil/masaüstü ağacı yok.
 */
function AppShellIc() {
  const [daraltilmis, setDaraltilmis] = useState(false);
  const [menuAcik, setMenuAcik] = useState(false);
  const { etkinTema, temaDegistir } = useTheme();
  const { me, hasPolicy, hasPermission, signOut } = useSession();
  const kurum = useInstitution();
  const [cikisSorusu, setCikisSorusu] = useState(false);
  /**
   * Çıkış SÜRÜYOR.
   *
   * Çıkış iki ağ çağrısı yapıyor (web push jetonunu sil + denetim kaydı) ve
   * bu süre boyunca ekranda hiçbir şey değişmiyordu: kullanıcı düğmeye
   * basıyor, bir şey olmuyor, tekrar basıyordu. Perde hem beklemeyi görünür
   * kılıyor hem de ikinci tıklamayı engelliyor.
   */
  const [cikisSuruyor, setCikisSuruyor] = useState(false);

  // Rol tabanlı öğeler: `Sistem` rolü `Admin`in gördüğü her yeri görür.
  const rolVar = (rol?: string) =>
    !rol || (me?.roller ?? []).some((r) => r === rol || r === 'Sistem');

  // Veriye bağlı koşullar. Gönderme yetkisi olan menüyü her zaman görür;
  // olmayan yalnızca kendisine dosya geldiyse.
  const gelenDosya = useHasIncomingFile();
  const kosulTutuyor = (kosul?: NavigationItem['kosul']) =>
    kosul !== 'gelenDosya' || hasPermission(PERMISSION.gonderimGonder) || gelenDosya;
  const konum = useLocation();
  const [temaPaneli, setTemaPaneli] = useState(false);

  /**
   * Menü süzgeci — İZİN önce gelir.
   *
   * Yetki dağılımı artık veritabanında; menüyü role/politikaya bağlamak,
   * yönetim ekranından oluşturulan yeni bir rolün hiçbir menüyü görememesi
   * demekti. Bir öğe izin ilan ediyorsa <b>yalnızca ona</b> bakılır.
   *
   * Eski rol/politika alanları, izin listesi HİÇ gelmeyen bir sunucuya
   * bağlanıldığında geri düşülecek yol olarak duruyor.
   */
  const izinListesiVar = (me?.izinler?.length ?? 0) > 0;

  const gorunurMu = (o: NavigationItem) => {
    if (!kosulTutuyor(o.kosul)) return false;
    if (o.izin && izinListesiVar) return hasPermission(o.izin);
    return (!o.politika || hasPolicy(o.politika)) && rolVar(o.rol);
  };

  const gorunurGruplar = NAVIGATION
    .map((g) => ({ ...g, ogeler: g.ogeler.filter(gorunurMu) }))
    .filter((g) => g.ogeler.length > 0);

  // Alt çubuk sırası MENÜDEN bağımsız: `tabbar` alanı hem "görünsün mü"yü
  // hem sırayı taşıyor. Ana Sayfa · Ajanda · Talepler · Halk Günü — gün
  // içinde en sık açılan ekranlar başta.
  const tabbarOgeleri = gorunurGruplar
    .flatMap((g) => g.ogeler)
    .filter((o) => o.tabbar)
    .sort((a, b) => (a.tabbar ?? 99) - (b.tabbar ?? 99))
    .slice(0, 4);

  /**
   * Kayan gösterge için aktif sekmenin sırası ve sekme genişliği.
   *
   * "Menü" düğmesi de bir sekme yeri kaplıyor, bu yüzden bölen +1.
   * Aktif öğe yoksa (menü altındaki bir sayfadaysak) gösterge çizilmez —
   * yanlış bir sekmenin altında durması, kullanıcıya nerede olduğunu
   * yanlış söylerdi.
   */
  const sekmeSayisi = tabbarOgeleri.length + 1;
  const sekmeGenisligi = 100 / sekmeSayisi;
  const aktifSekmeIndeksi = tabbarOgeleri.findIndex((o) => isActive(o, konum.pathname));

  const sayfaBasligi = pageTitle(gorunurGruplar, konum.pathname) ?? kurum.uygulamaAdi;

  return (
    <div className="flex min-h-dvh bg-bg">
      {/* --------- Kenar çubuğu: yalnızca md ve üstü --------- */}
      {/*
        SABİT kenar çubuğu.

        `sticky top-0 h-dvh`: içerik kaydırılırken menü yerinde kalır. Uzun
        listelerde kullanıcı sayfanın altındayken menüye ulaşmak için başa
        dönmek zorunda kalıyordu. `fixed` yerine `sticky` seçildi çünkü
        `fixed` öğe akıştan çıkar ve ana sütuna elle sol boşluk vermek
        gerekir — daraltma animasyonunda o boşluk kayıyor.
      */}
      <aside
        className={cn(
          'sticky top-0 hidden h-dvh shrink-0 flex-col bg-nav-bg transition-[width] duration-180 md:flex',
          daraltilmis ? 'w-[76px]' : 'w-[258px]',
        )}
      >
        {/*
          Marka bloğu appbar ile AYNI yükseklikte (64px) ve aynı yerde alt
          kenarlığı var; ikisi tek bir yatay çizgi oluşturur.
        */}
        <div
          className={cn(
            'flex h-16 shrink-0 items-center gap-2.5 border-b border-(--nav-border) px-4',
            daraltilmis && 'justify-center px-0',
          )}
        >
          <img src={kurum.marka.amblem ?? '/amblem.png'} alt="" className="h-7 w-7 shrink-0" />
          {!daraltilmis && (
            <span className="min-w-0">
              {/* Kurum adı KODA YAZILMAZ; sunucudan gelir (institution.ts). */}
              <span className="block truncate font-display text-sm font-bold leading-tight text-nav-strong">
                {kurum.gorunenAd || kurum.ad}
              </span>
              {/*
                GİREN KİŞİNİN BİRİMİ. Burada sabit "Başkanlık Makamı" yazıyordu:
                uygulamayı bütün müdürlükler kullanıyor ve listeler kullanıcının
                birimine göre süzülüyor — ekranın tepesinde başka bir birim adı
                görmek, hangi birimin verisine bakıldığını yanlış söylüyordu.
              */}
              <span
                className="block truncate text-2xs text-nav-label"
                title={me?.birimAd ?? undefined}
              >
                {me?.birimAd || 'Başkanlık Makamı'}
              </span>
            </span>
          )}
        </div>

        <nav className="min-h-0 flex-1 overflow-y-auto px-2.5 py-3">
          {gorunurGruplar.map((grup) => (
            <div key={grup.baslik} className="mb-4">
              {!daraltilmis && (
                <p className="mb-1.5 px-2.5 text-3xs font-semibold uppercase tracking-[0.14em] text-nav-label">
                  {grup.baslik}
                </p>
              )}
              <ul className="space-y-0.5">
                {grup.ogeler.map((oge) => {
                  const Ikon = oge.ikon;
                  const aktif = isActive(oge, konum.pathname);
                  return (
                    <li key={oge.yol}>
                      <NavLink
                        to={oge.yol}
                        title={daraltilmis ? oge.etiket : undefined}
                        className={cn(
                          'relative flex h-[42px] items-center gap-2.5 rounded-sm px-2.5 text-sm font-medium transition-colors',
                          aktif
                            ? 'bg-nav-active text-nav-activeFg'
                            : 'text-nav-fg hover:bg-(--nav-hover)',
                          daraltilmis && 'justify-center px-0',
                        )}
                      >
                        {/* Aktif göstergesi: 3×20px altın çubuk (design.md §5.1) */}
                        {aktif && (
                          <span
                            aria-hidden
                            className="absolute left-0 h-5 w-[3px] rounded-r-xs bg-gold"
                          />
                        )}
                        <Ikon size={17} strokeWidth={1.8} className="shrink-0" />
                        {!daraltilmis && <span>{oge.etiket}</span>}
                      </NavLink>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </nav>

        {/*
          ── Kullanıcı bloğu ──

          Kenar çubuğunun ALTINDA sabit. Çıkış yalnızca Ayarlar ekranının
          dibindeydi: kullanıcı çıkmak için önce menüden ayarları bulmak
          zorundaydı. Kim olarak giriş yapıldığı da appbar'da küçük puntoyla
          yazıyordu; ortak bilgisayarda yanlış hesapla çalışıldığını fark
          etmek zordu.
        */}
        <div className="shrink-0 border-t border-(--nav-border) p-2.5">
          <div
            className={cn(
              'flex items-center gap-2.5 rounded-sm px-2 py-2',
              daraltilmis && 'justify-center px-0',
            )}
          >
            <span
              className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-(--nav-active-bg) font-display text-2xs font-bold text-nav-strong"
              aria-hidden
            >
              {initials(me?.ad, me?.soyad) || initials(me?.kullaniciAdi)}
            </span>
            {!daraltilmis && (
              <>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-semibold text-nav-strong">
                    {me?.tamAd || me?.kullaniciAdi}
                  </span>
                  <span className="block truncate text-2xs text-nav-label">
                    {me?.birimAd}
                  </span>
                </span>
                <button
                  onClick={() => setCikisSorusu(true)}
                  aria-label="Çıkış yap"
                  title="Çıkış yap"
                  className="grid h-8 w-8 shrink-0 place-items-center rounded-sm text-nav-fg transition-colors hover:bg-(--nav-hover) hover:text-nav-strong"
                >
                  <LogOut size={16} />
                </button>
              </>
            )}
          </div>

          {/* Daraltılmışken ad sığmıyor; çıkış kendi satırında kalıyor. */}
          {daraltilmis && (
            <button
              onClick={() => setCikisSorusu(true)}
              aria-label="Çıkış yap"
              title="Çıkış yap"
              className="mt-1 grid h-9 w-full place-items-center rounded-sm text-nav-fg transition-colors hover:bg-(--nav-hover) hover:text-nav-strong"
            >
              <LogOut size={16} />
            </button>
          )}
        </div>
      </aside>

      {/* --------- Ana sütun --------- */}
      <div className="flex min-w-0 flex-1 flex-col">
        {/* Appbar da sabit: bildirim ve tema her zaman erişilebilir kalsın. */}
        {/*
          Mobilde BEŞ eylem düğmesi var (yardım · bildirim · tema tasarımcısı ·
          gece/gündüz · çıkış). 390px'te sığmalarının yolu aralığı daraltmak:
          `gap-2` (8px) dört boşlukta 32px yiyordu, `gap-1` ile 16px'e iniyor
          ve başlığa o kadar yer açılıyor. Masaüstünde eski ritim duruyor.
        */}
        {/*
          KENARLIK YERİNE GÖLGE.

          Şerit `sticky` ve içerik altından geçiyor; düz bir `border-b` bunu
          anlatmıyordu — sayfa kaydırılınca appbar içeriğe yapışık, sayfanın
          bir parçasıymış gibi duruyordu. Gölge, şeridin içeriğin ÜSTÜNDE
          olduğunu söylüyor.

          Gölge TEMAYA GÖRE değişiyor: aydınlıkta hafif bir düşüm, gecede
          üstte ince bir açık çizgi — karanlıkta yükseklik gölgeyle değil
          ışıkla anlatılıyor. İkisi de `--appbar-shadow` altında tanımlı.
        */}
        <header className="sticky top-0 z-30 flex h-bar-m shrink-0 items-center gap-1 appbar-golge bg-surface px-3 md:h-appbar md:gap-2 md:px-[26px]">
          <button
            onClick={() => setDaraltilmis((d) => !d)}
            aria-label={daraltilmis ? 'Menüyü genişlet' : 'Menüyü daralt'}
            title={daraltilmis ? 'Menüyü genişlet' : 'Menüyü daralt'}
            className="hidden h-[34px] w-[34px] place-items-center rounded-sm text-text-2 hover:bg-surface-2 hover:text-text md:grid"
          >
            {daraltilmis ? <PanelLeftOpen size={17} /> : <PanelLeftClose size={17} />}
          </button>

          {/*
            Başlık HER ZAMAN görünür. Bir dönem iOS'un "large title" devri
            uygulanmıştı: appbar başlığı 34px kaydırılana kadar gizliydi.
            Ekranın adı, kullanıcının "neredeyim" sorusunun tek cevabı ve onu
            kaydırma şartına bağlamak, uygulamayı ilk açan kişiyi başlıksız
            bir ekranla baş başa bırakıyordu.
          */}
          <div className="min-w-0 flex-1">
            {/*
              MOBİLDE BAŞLIK BÜYÜK VE TEK BAŞINA.

              Altında "Admin · Belediye Başkanlığı" künyesi vardı; her ekranda
              tekrar eden, o an hiçbir işe yaramayan bir satır. Künye zaten
              Menü tabakasının tepesinde, avatarıyla birlikte duruyor.
              Kaldırılınca başlığa iki punto yer açıldı ve şerit native
              uygulamalardaki gibi tek satırlık, okunur bir başlığa döndü.
            */}
            <h1 className="truncate font-display text-lg font-bold tracking-[var(--track-d)] md:text-xl">
              {sayfaBasligi}
            </h1>
            {me && (
              <p className="hidden truncate text-xs text-text-3 md:block">
                {me.tamAd} · {me.birimAd}
              </p>
            )}
          </div>

          {/*
            KURULUM SİMGESİ — yalnızca uygulama kurulu değilken.

            Grubun BAŞINDA duruyor: kurulduğunda kaybolacak geçici bir düğme
            ve şeridin sağı sabit kalsın istiyoruz. Sona konsaydı kurulumdan
            sonra çıkış, tema, bildirim — hepsi bir düğme boyu kayardı ve
            kullanıcının kas hafızası bozulurdu.
          */}
          <InstallButton />

          {/* Bulunduğun ekranın yardımı — her sayfada AYNI yerde. */}
          <HelpButton />

          {/* Bildirim mobilde de üstte: rozeti "yeni bir şey var" demenin en
              hızlı yolu ve menüye gömülünce görünmez oluyordu. */}
          <NotificationCenter />

          {/*
            TEMA TASARIMCISI — §3: erişim sağ üstteki palet ikonundan.
            Mod düğmesinin yanında duruyor çünkü ikisi de "görünüm" işi;
            ayrı yerlere koymak kullanıcıyı ikisini ayrı ayrı aramaya
            zorluyordu.
          */}
          <IconButton
            etiket="Tema Tasarımcısı"
            varyant="sade"
            onClick={() => setTemaPaneli(true)}
            className="text-brand"
          >
            <Palette size={17} strokeWidth={1.8} />
          </IconButton>

          {/*
            GECE/GÜNDÜZ ÜSTTE KALIYOR.

            Bir denemede tema araçlarının tümü Menü tabakasına indirilmişti;
            gündüz-gece geçişi günde birkaç kez yapılan bir şey ve iki
            dokunuş arkasına gömülmesi hissedilir bir yavaşlama oldu. Şeritte
            artık iki eylem var (tema + bildirim) — beş değil; başlık hâlâ
            rahat sığıyor. Tema Tasarımcısı ve Yardım menüde kaldı: ikisi de
            ara sıra açılan ekranlar.
          */}
          <IconButton
            etiket={etkinTema === 'acik' ? 'Koyu temaya geç' : 'Açık temaya geç'}
            varyant="sade"
            onClick={temaDegistir}
          >
            {etkinTema === 'acik' ? <Moon size={17} strokeWidth={1.8} /> : <Sun size={17} strokeWidth={1.8} />}
          </IconButton>

          {/* Mobilde kenar çubuğu yok; çıkış oradaki kullanıcı bloğuyla
              birlikte erişilemez kalıyordu. */}
          <IconButton
            etiket="Çıkış yap"
            varyant="sade"
            onClick={() => setCikisSorusu(true)}
            className="md:hidden"
          >
            <LogOut size={17} strokeWidth={1.8} />
          </IconButton>
        </header>

        {/*
          `overflow-y-auto` KALDIRILDI: iç kaydırma kabı, appbar'ın
          `sticky` davranışını iptal ediyordu. Kaydırma artık belge
          düzeyinde; hem appbar hem kenar çubuğu yerinde kalıyor.
        */}
        {/*
          Alt boşluk = tabbar (62px) + güvenli alan + nefes payı. Yetersiz
          kalırsa son kart sabit çubuğun altında kalıyor ve liste "kesilmiş"
          görünüyor.
        */}
        <main className="flex-1 p-4 pb-[calc(var(--h-tab)+env(safe-area-inset-bottom,0px)+var(--sp-8))] md:p-[26px] md:pb-[26px]">
          {/* Giriş sonrası bildirim izni — tarayıcının izin kutusunu doğrudan
              açmaz, önce ne için istendiğini anlatır (bkz. bileşen notu). */}
          <NotificationPermissionCard />
          <Outlet />
        </main>
      </div>

      {/* --------- Tabbar: yalnızca md altı --------- */}
      {/*
        ── Tabbar (md altı) ──────────────────────────────────────────────

        Mobil uygulamanın alt çubuğuyla aynı dil: 62px yükseklik, üstte saç
        teli + yumuşak gölge, sekmeler arasında KAYAN altın gösterge, seçili
        ikon hafifçe büyüyüp yükseliyor.

        Gösterge her sekmede ayrı bir öğe olarak belirip kaybolmuyor; TEK öğe
        `translate3d` ile kayıyor. Böylece hem hareket sürekli (kullanıcı
        nereden nereye gittiğini görüyor) hem de animasyon ekran kartında
        çalışıyor — `left` canlandırmak her karede yeniden yerleşim demekti.
      */}
      <nav
        aria-label="Ana gezinme"
        className="fixed inset-x-0 bottom-0 z-20 border-t border-border bg-surface guvenli-alt md:hidden
          shadow-[0_-2px_12px_rgba(16,26,45,.05)] dark:shadow-none"
      >
        {/*
          Yükseklik `--h-tab` (56px) — demoda ölçülen native değer. Sabit 62px
          yazmak yoğunluk knob'unu (`--sp`) es geçiyordu: kullanıcı arayüzü
          sıkılaştırınca alt çubuk tek başına eski boyunda kalıyordu.
        */}
        <div className="relative flex h-[64px] items-stretch">
          {/* Kayan gösterge */}
          {aktifSekmeIndeksi >= 0 && (
            <span
              aria-hidden
              className="pointer-events-none absolute top-0 h-[2.5px] rounded-b-xs bg-accent transition-transform duration-260 ease-[cubic-bezier(.22,1,.36,1)] motion-reduce:transition-none"
              style={{
                width: 24,
                left: 0,
                transform: `translate3d(calc(${aktifSekmeIndeksi} * ${sekmeGenisligi}vw + ${sekmeGenisligi / 2}vw - 12px), 0, 0)`,
              }}
            />
          )}

          {tabbarOgeleri.map((oge) => {
            const Ikon = oge.ikon;
            const aktif = isActive(oge, konum.pathname);
            return (
              <NavLink
                key={oge.yol}
                to={oge.yol}
                aria-current={aktif ? 'page' : undefined}
                className="group flex flex-1 flex-col items-center justify-center gap-1.5 transition-opacity active:opacity-60 katman"
              >
                {/*
                  İkon YAYLA büyür (`--ease-spring`), etiket yalnızca ağırlık
                  değiştirir. Demodaki his buradan geliyor: seçili sekme
                  "yerleşiyor" gibi duruyor, renk değişimiyle yetinmiyor.
                */}
                <span
                  className={cn(
                    'grid place-items-center transition-transform',
                    aktif ? 'scale-[1.06]' : 'scale-100',
                  )}
                  style={{ transitionTimingFunction: 'var(--ease-spring)' }}
                >
                  <Ikon
                    size={23}
                    strokeWidth={aktif ? 2.1 : 1.8}
                    className={cn(
                      'transition-colors motion-reduce:transition-none',
                      aktif ? 'text-brand' : 'text-ink-3',
                    )}
                  />
                </span>
                <span
                  className={cn(
                    'text-2xs leading-none tracking-[0.01em] transition-colors',
                    aktif ? 'font-semibold text-brand' : 'font-medium text-ink-3',
                  )}
                >
                  {oge.etiket}
                </span>
              </NavLink>
            );
          })}

          <button
            onClick={() => setMenuAcik(true)}
            className="group flex flex-1 flex-col items-center justify-center gap-1 text-ink-3 transition-opacity active:opacity-60 katman"
            aria-label="Menüyü aç"
          >
            <span className="grid place-items-center">
              <Menu size={23} strokeWidth={1.8} />
            </span>
            <span className="text-2xs font-medium leading-none tracking-[0.01em]">Menü</span>
          </button>
        </div>

      </nav>

      <MobileMenu
        acik={menuAcik}
        kapat={() => setMenuAcik(false)}
        gruplar={gorunurGruplar}
        etkinTema={etkinTema}
        temaDegistir={temaDegistir}
        temaPaneliAc={() => setTemaPaneli(true)}
      />

      <ThemePanel acik={temaPaneli} kapat={() => setTemaPaneli(false)} />

      <ConfirmDialog
        acik={cikisSorusu}
        kapat={() => setCikisSorusu(false)}
        baslik="Çıkış yapılsın mı?"
        aciklama="Bu cihazdaki bildirim kaydı da silinir."
        onayEtiketi="Çıkış yap"
        yikici
        onayla={() => {
          setCikisSuruyor(true);
          void signOut().finally(() => setCikisSuruyor(false));
        }}
      />

      {/* Çıkış perdesi — yalnızca opaklık canlanır (bileşik katman). */}
      {cikisSuruyor && (
        <div
          role="status"
          aria-live="polite"
          className="anim-perde fixed inset-0 z-600 grid place-items-center bg-perde backdrop-blur-[2px]"
        >
          <div className="flex items-center gap-3 rounded-card border border-border bg-surface px-5 py-4 shadow-2">
            <Loader2 size={18} className="animate-spin text-brand-2" />
            <span className="text-sm font-medium">Çıkış yapılıyor…</span>
          </div>
        </div>
      )}

      {/* Görsel çıktısı yok: push bildirimlerini toast'a ve gezinmeye bağlar. */}
      <NotificationBridge />
    </div>
  );
}

/**
 * Kabuk, büyük başlık bağlamıyla sarılı dışa aktarılır.
 *
 * Sağlayıcı DIŞARIDA olmak zorunda: appbar başlığı ile içerikteki büyük
 * başlık aynı ağaçta ama farklı dallarda; devir bilgisini ikisinin ortak
 * atası taşımalı.
 */
export function AppShell() {
  return (
    <LargeTitleProvider>
      <AppShellIc />
    </LargeTitleProvider>
  );
}
