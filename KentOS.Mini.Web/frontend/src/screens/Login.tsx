import { CircleAlert, Eye, EyeOff, LogIn } from 'lucide-react';
import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Button } from '../components/Button';
import { useSession } from '../auth/SessionProvider';
import { inisYolu } from '../auth/inisEkrani';
import { ApiError } from '../data/client';
import { buyukHarf, useInstitution } from '../institution/institution';

/**
 * Giriş ekranı — design.md §8.1.
 *
 * Masaüstünde %45 lacivert marka paneli + ortalanmış 372px form; mobilde
 * panel gizlenir, amblem ortalanır ve dokunma hedefleri büyür (50px input,
 * 52px buton). Kabuk YOK — sidebar/tabbar burada gösterilmez.
 *
 * <p>
 * Belgedeki "Beni hatırla" ve "Şifremi unuttum" bağlantıları KONMADI:
 * sunucuda parola sıfırlama akışı yok (yönetici sıfırlıyor) ve oturum zaten
 * kalıcı saklanıyor. Hiçbir şey yapmayan bir onay kutusu, olmayan kutudan
 * daha kötü.
 * </p>
 *
 * <p>
 * Destek satırı da KALDIRILDI: yanlış müdürlüğü işaret ediyordu ve giriş
 * yapamayan kullanıcıyı yanlış kapıya gönderen bir numara, hiç numara
 * olmamasından kötü.
 * </p>
 */
export default function Login() {
  // Kurum kimliği KODA YAZILMAZ; sunucudan gelir (bkz. institution.ts).
  const kurum = useInstitution();
  const amblem = kurum.marka.amblem ?? '/amblem.png';
  const kurumAdi = buyukHarf(kurum.gorunenAd || kurum.ad);
  const kunye = [kurum.gorunenAd || kurum.ad, kurum.kunye].filter(Boolean).join(' · ');

  const { me, signIn } = useSession();
  const gezin = useNavigate();
  const konum = useLocation();
  const [hata, setHata] = useState<string | null>(null);
  const [alanHatalari, setAlanHatalari] = useState<Record<string, string[]>>({});
  const [gonderiliyor, setGonderiliyor] = useState(false);

  if (me) {
    // Zaten açık bir oturumla giriş ekranına gelindi (geri tuşu, yer imi).
    return <Navigate to={inisYolu(me, new URLSearchParams(konum.search).get('donus'))} replace />;
  }

  async function gonder(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setHata(null);
    setAlanHatalari({});
    setGonderiliyor(true);

    const f = new FormData(e.currentTarget);
    try {
      const kullanici = await signIn(
        String(f.get('kullaniciAdi') ?? ''), String(f.get('parola') ?? ''));

      // `me` durumu ancak bir sonraki render'da doluyor; hedefi `signIn`in
      // döndürdüğü kayıttan çözüyoruz.
      gezin(inisYolu(kullanici, new URLSearchParams(konum.search).get('donus')), { replace: true });
    } catch (h) {
      if (h instanceof ApiError) {
        setAlanHatalari(h.fieldErrors);
        setHata(Object.keys(h.fieldErrors).length ? null : h.message);
      } else {
        setHata('Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.');
      }
    } finally {
      setGonderiliyor(false);
    }
  }

  return (
    <div className="flex min-h-dvh bg-bg">
      {/* ── Marka paneli: %45, yalnızca md ve üstü ── */}
      <aside
        className="relative hidden w-[45%] shrink-0 overflow-hidden md:flex md:flex-col"
        style={{ background: 'var(--login-panel)' }}
      >
        {/* Dekor: taşan kısım kırpılır (design.md §8.1) */}
        <span
          aria-hidden
          className="pointer-events-none absolute bottom-[-150px] right-[-130px] h-[420px] w-[420px] rounded-full border"
          style={{ borderColor: 'rgba(201,162,39,.28)' }}
        />
        <span
          aria-hidden
          className="pointer-events-none absolute bottom-[-70px] right-[90px] h-[280px] w-[280px] rounded-full border"
          style={{ borderColor: 'rgba(255,255,255,.08)' }}
        />

        {/* Üst: kurum kimliği */}
        <div className="relative flex items-center gap-3 px-14 pt-12">
          <img src={amblem} alt="" className="h-14 w-14" />
          <div>
            <p className="font-display text-lg font-bold tracking-[0.02em] text-white">
              {kurumAdi}
            </p>
            {kurum.birim && <p className="text-sm text-white/70">{kurum.birim}</p>}
          </div>
        </div>

        {/* Orta: konum bildirimi */}
        <div className="relative flex flex-1 flex-col justify-center px-14">
          <span className="mb-5 block h-[3px] w-11 bg-gold" aria-hidden />
          <h1 className="font-display text-3xl font-bold leading-[1.15] tracking-[-0.02em] text-white metin-guzel">
            {kurum.uygulamaAdi}
          </h1>
          {kurum.uygulamaAciklamasi && (
            <p className="mt-4 max-w-[360px] text-base leading-[1.6] text-white/70 metin-guzel">
              {kurum.uygulamaAciklamasi}
            </p>
          )}
        </div>

        {/* Alt: telif */}
        <p className="relative px-14 pb-10 text-xs text-white/50">
          © {new Date().getFullYear()} {kunye}
        </p>
      </aside>

      {/* ── Form ── */}
      <div className="flex flex-1 flex-col items-center justify-center px-5 py-10">
        <form onSubmit={gonder} className="w-full max-w-[372px]">
          {/* Mobil kimlik başlığı — masaüstünde panel bunu zaten söylüyor */}
          <div className="mb-8 flex flex-col items-center text-center md:hidden">
            <img src={amblem} alt="" className="h-[70px] w-[70px]" />
            <p className="mt-3 font-display text-sm font-bold tracking-[0.06em] text-text-2">
              {kurumAdi}
            </p>
            {kurum.birim && <p className="text-xs text-text-3">{kurum.birim}</p>}
            <span className="mt-4 block h-[3px] w-9 bg-gold" aria-hidden />
            <h1 className="mt-4 font-display text-2xl font-bold tracking-[-0.02em]">
              {kurum.uygulamaAdi}
            </h1>
          </div>

          <h2 className="hidden font-display text-2xl font-bold tracking-[-0.02em] md:block">
            Giriş Yap
          </h2>
          <p className="mb-7 hidden text-sm text-text-2 md:mt-1 md:block">
            Kurumsal hesabınızla oturum açın.
          </p>

          {hata && (
            <div
              role="alert"
              className="mb-4 flex items-start gap-2 rounded-control px-3 py-2.5 text-sm"
              style={{ background: 'var(--st-no-bg)', color: 'var(--st-no)' }}
            >
              <CircleAlert size={16} strokeWidth={2} className="mt-0.5 shrink-0" />
              <span>{hata}</span>
            </div>
          )}

          <Alan
            ad="kullaniciAdi"
            etiket="Kullanıcı Adı"
            otomatikTamamla="username"
            hatalar={alanHatalari.kullaniciAdi}
          />
          <ParolaAlani hatalar={alanHatalari.parola} />

          <Button
            type="submit"
            className="mt-2 h-[52px] w-full rounded-md text-base shadow-2 md:h-12"
            disabled={gonderiliyor}
          >
            <LogIn size={17} strokeWidth={2} />
            {gonderiliyor ? 'Giriş yapılıyor…' : 'Giriş yap'}
          </Button>

          <p className="mt-8 text-center text-xs leading-normal text-text-3 md:hidden">
            © {new Date().getFullYear()} {kurum.gorunenAd || kurum.ad}
            {kurum.kunye && (
              <>
                <br />
                {kurum.kunye}
              </>
            )}
          </p>
        </form>
      </div>
    </div>
  );
}

/**
 * Giriş alanı görünümü.
 *
 * Hata kenarlığı SINIFLA veriliyor, satır içi `style` ile değil: satır içi
 * stil her zaman sınıflardan üstün olduğu için `focus:border-brand-2` hiçbir
 * zaman uygulanmıyordu ve odaklanan alan sadece halkayla belli oluyordu.
 */
const alanSinifi =
  'h-[50px] w-full rounded-control border border-border bg-surface-2 px-3.5 text-base text-text ' +
  'outline-hidden transition-[border-color,box-shadow] duration-150 ' +
  'hover:border-border-2 ' +
  'focus:border-brand-2 focus:ring-[3px] focus:ring-(--focus-ring) md:h-12';

function Alan({
  ad,
  etiket,
  otomatikTamamla,
  hatalar,
}: {
  ad: string;
  etiket: string;
  otomatikTamamla?: string;
  hatalar?: string[];
}) {
  const hataliMi = (hatalar?.length ?? 0) > 0;
  return (
    <label className="mb-4 block">
      <span className="mb-1.5 block text-2xs font-semibold uppercase tracking-[0.09em] text-text-3">
        {etiket}
      </span>
      <input
        name={ad}
        type="text"
        autoComplete={otomatikTamamla}
        aria-invalid={hataliMi}
        className={`${alanSinifi} ${hataliMi ? 'border-(--st-no)' : ''}`}
      />
      {hataliMi && (
        <span className="mt-1 block text-xs" style={{ color: 'var(--st-no)' }}>
          {hatalar!.join(' ')}
        </span>
      )}
    </label>
  );
}

/**
 * Parola alanı — göz düğmesiyle.
 *
 * Göz düğmesi `type="button"`: varsayılan `submit` olsaydı parolayı görmek
 * için basmak formu gönderirdi.
 */
function ParolaAlani({ hatalar }: { hatalar?: string[] }) {
  const [acik, setAcik] = useState(false);
  const hataliMi = (hatalar?.length ?? 0) > 0;

  return (
    <div className="mb-4">
      <label htmlFor="parola" className="mb-1.5 block text-2xs font-semibold uppercase tracking-[0.09em] text-text-3">
        Şifre
      </label>
      <div className="relative">
        <input
          id="parola"
          name="parola"
          type={acik ? 'text' : 'password'}
          autoComplete="current-password"
          aria-invalid={hataliMi}
          className={`${alanSinifi} pr-12 ${hataliMi ? 'border-(--st-no)' : ''}`}
        />
        <button
          type="button"
          onClick={() => setAcik((a) => !a)}
          aria-label={acik ? 'Şifreyi gizle' : 'Şifreyi göster'}
          className="absolute right-1 top-1/2 grid h-11 w-11 -translate-y-1/2 place-items-center rounded-control text-text-3 hover:text-text"
        >
          {acik ? <EyeOff size={17} /> : <Eye size={17} />}
        </button>
      </div>
      {hataliMi && (
        <span className="mt-1 block text-xs" style={{ color: 'var(--st-no)' }}>
          {hatalar!.join(' ')}
        </span>
      )}
    </div>
  );
}
