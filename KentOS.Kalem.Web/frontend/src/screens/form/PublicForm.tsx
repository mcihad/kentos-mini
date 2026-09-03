import { AlertTriangle, ArrowLeft, ArrowRight, Check, Send } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { FieldWrapper, Input } from '../../components/Field';
import { Skeleton } from '../../components/Skeleton';
import { cn } from '../../components/utils';
import { usePublicForm, usePublicFormSubmit } from '../../data/forms';
import { useInstitution } from '../../institution/institution';
import type { FormAnswerResult } from '../../data/types';
import { FormRenderer } from '../../forms/FormRenderer';
import { FORM_ACCESS } from '../../forms/fieldTypes';
import { adimHatalari, gonderilecek, tumHatalar, type Answers } from '../../forms/formEngine';

/**
 * VATANDAŞIN GÖRDÜĞÜ FORM — giriş gerektirmez.
 *
 * <p>
 * <b>Uygulama kabuğunun DIŞINDA</b> (menü, sekme çubuğu, bildirim yok):
 * bağlantıyı açan kişi kurumun personeli değil ve ona kullanamayacağı bir
 * gezinme göstermek, sayfayı "yanlış yere geldim" hissi veriyor.
 * </p>
 *
 * <p>
 * <b>Taslak <c>localStorage</c>'da.</b> Sunucuya yazmak anonim bir uçta
 * sınırsız yazma demek. Üç kural: şema sürümü değiştiyse taslak ATILIR
 * (eski cevaplar yeni sorulara oturmaz), dosya alanı yazılmaz ve geri
 * yükleme SESSİZ DEĞİL — kullanıcıya soruluyor, çünkü ortak bir tablette
 * başkasının yarım formunu görmek en hafifinden şaşırtıcı.
 * </p>
 */
/**
 * Kalıcı rastgele anahtar — yoksa üretir, varsa aynısını döner.
 *
 * <p>
 * Gizli bir değer değil, bir ayırt edici: sunucu bunu ham saklamıyor,
 * form başına tuzlanmış özetini yazıyor.
 * </p>
 */
function kalici(depoAnahtari: string): string {
  try {
    const varOlan = localStorage.getItem(depoAnahtari);
    if (varOlan) return varOlan;

    const yeni = crypto.randomUUID();
    localStorage.setItem(depoAnahtari, yeni);
    return yeni;
  } catch {
    // Gizli sekmede depo kapalı olabilir; anahtar yalnızca o sekme boyunca
    // yaşar. Tekrar gönderimi engellemez ama formu da kırmaz.
    return crypto.randomUUID();
  }
}

export default function PublicForm() {
  const { anahtar } = useParams<{ anahtar: string }>();
  const form = usePublicForm(anahtar);
  const gonder = usePublicFormSubmit(anahtar);

  const [cevaplar, setCevaplar] = useState<Answers>({});
  const [adim, setAdim] = useState(0);
  const [hatalar, setHatalar] = useState<Record<string, string>>({});
  const [sonuc, setSonuc] = useState<FormAnswerResult | null>(null);
  const [kimlik, setKimlik] = useState({ adSoyad: '', telefon: '', eposta: '' });
  const [taslakSoruldu, setTaslakSoruldu] = useState(false);

  /*
    SÜRDÜRME ANAHTARI — hem dosya bağı hem İDEMPOTANS anahtarı.

    Sunucu ilk dosyada bir TASLAK yanıt açıyor ve anahtarını dönüyor;
    gönderimde bu anahtar geri gitmezse yeni bir yanıt açılır ve yüklenen
    dosya sahipsiz kalır — kullanıcının gördüğü "dosyayı ekledim ama
    kayıtta yok".

    Artık dosya beklenmeden ÜRETİLİYOR ve saklanıyor: aynı gövde ikinci kez
    ulaşırsa (ağ koptuğunda tarayıcının yeniden denemesi, "geri" tuşu,
    mobilde sekme geri yüklemesi) sunucu ikinci bir yanıt açmak yerine
    ilkinin sonucunu döndürüyor. Başarıdan sonra silinir — yeni bir doldurma
    yeni bir anahtar alır.
  */
  const [surdurmeAnahtari, setSurdurmeAnahtari] = useState<string | null>(
    () => kalici(`sv-form-gonderim-${anahtar}`),
  );

  /*
    CİHAZ ANAHTARI — "tek yanıt" ayarının telefonsuz formlardaki karşılığı.

    Başarıdan sonra SİLİNMEZ; silinseydi vatandaş sayfayı yenileyip formu
    yeniden doldurabilirdi — şikâyet edilen davranış tam olarak buydu.
    Yumuşak bir kapı (tarayıcı verisi temizlenirse aşılır) ama anonim bir
    formda kimliğin başka kaynağı yok.
  */
  const cihazAnahtari = useState(() => kalici(`sv-form-cihaz-${anahtar}`))[0];

  const tanim = form.data?.tanim;
  const adimlar = tanim?.adimlar ?? [];
  const cokAdimli = adimlar.length > 1;
  const depoAnahtari = `sv-form-taslak-${anahtar}`;

  // ── yarım kalan taslağı sor ──
  useEffect(() => {
    if (!form.data || taslakSoruldu) return;
    setTaslakSoruldu(true);

    try {
      const ham = localStorage.getItem(depoAnahtari);
      if (!ham) return;

      const kayit = JSON.parse(ham) as { surum: number; cevaplar: Answers };

      // ŞEMA SÜRÜMÜ DEĞİŞTİYSE ATILIR: eski cevaplar yeni sorulara oturmaz
      // ve yanlış alana yazılmış bir değer, boş bir formdan kötüdür.
      if (kayit.surum !== form.data.surumNo) {
        localStorage.removeItem(depoAnahtari);
        return;
      }

      if (Object.keys(kayit.cevaplar ?? {}).length === 0) return;

      // Sessiz geri yükleme YOK: ortak bir tablette başkasının yarım
      // formunu görmek şaşırtıcı olurdu.
      if (window.confirm('Bu formu daha önce doldurmaya başlamışsınız. Kaldığınız yerden devam edilsin mi?')) {
        setCevaplar(kayit.cevaplar);
      } else {
        localStorage.removeItem(depoAnahtari);
      }
    } catch {
      localStorage.removeItem(depoAnahtari);
    }
  }, [form.data, depoAnahtari, taslakSoruldu]);

  // ── her değişiklikte yerel taslak ──
  useEffect(() => {
    if (!form.data || sonuc) return;
    if (Object.keys(cevaplar).length === 0) return;

    try {
      localStorage.setItem(depoAnahtari, JSON.stringify({
        surum: form.data.surumNo,
        cevaplar,
      }));
    } catch { /* kotası dolu olabilir; taslak bir kolaylık */ }
  }, [cevaplar, form.data, depoAnahtari, sonuc]);

  const degistir = (kimlikAlan: string, cevap: { deger?: unknown; metin?: string }) => {
    setCevaplar((o) => ({ ...o, [kimlikAlan]: cevap }));
    setHatalar((h) => {
      if (!h[kimlikAlan]) return h;
      const y = { ...h };
      delete y[kimlikAlan];
      return y;
    });
  };

  const ilerleme = useMemo(
    () => (cokAdimli ? Math.round(((adim + 1) / adimlar.length) * 100) : 0),
    [adim, adimlar.length, cokAdimli],
  );

  // ── durumlar ──
  if (form.isLoading) {
    return (
      <Kabuk>
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="h-40 w-full" />
      </Kabuk>
    );
  }

  if (form.isError || !form.data) {
    return (
      <Kabuk>
        <Card className="flex items-start gap-3 p-5">
          <span className="grid size-10 shrink-0 place-items-center rounded-md bg-(--st-no-bg) text-(--st-no)">
            <AlertTriangle size={20} />
          </span>
          <div>
            <p className="font-display text-base font-bold">Form bulunamadı</p>
            <p className="mt-1 text-sm text-ink-2">
              Bağlantı hatalı olabilir ya da form kaldırılmış olabilir.
            </p>
          </div>
        </Card>
      </Kabuk>
    );
  }

  const f = form.data;

  // ── sonuç sayfası ──
  if (sonuc) {
    return (
      <Kabuk baslik={f.baslik ?? ''} kurum={f.kurumAdi}>
        <Card className="p-5">
          <div className="flex items-start gap-3">
            <span className="grid size-10 shrink-0 place-items-center rounded-md bg-(--st-ok-bg) text-(--st-ok)">
              <Check size={20} strokeWidth={2.6} />
            </span>
            <div className="min-w-0">
              <p className="font-display text-lg font-bold text-(--st-ok)">Yanıtınız alındı</p>
              <p className="mt-1 text-sm leading-[1.6] text-ink-2 metin-guzel">
                {sonuc.tesekkurMetni || 'Katkınız için teşekkür ederiz.'}
              </p>
            </div>
          </div>

          <div className="mt-4 rounded-md bg-sunken px-3 py-2.5">
            <p className="text-xs text-ink-3">Takip numaranız</p>
            <p className="font-mono text-lg font-bold tracking-[0.15em]">{sonuc.takipNo}</p>
          </div>
        </Card>

        {(sonuc.ozet ?? []).length > 0 && (
          <Card className="overflow-hidden">
            <p className="border-b border-line px-4 py-2.5 text-xs font-semibold text-ink-3">
              VERDİĞİNİZ YANITLAR
            </p>
            <dl className="divide-y divide-line">
              {(sonuc.ozet ?? []).map((o, i) => (
                <div key={i} className="px-4 py-2.5">
                  <dt className="text-xs text-ink-3">{o.etiket}</dt>
                  <dd className="mt-0.5 text-sm wrap-anywhere">{o.deger}</dd>
                </div>
              ))}
            </dl>
          </Card>
        )}
      </Kabuk>
    );
  }

  // ── kapalı form ──
  if (!f.yanitAliyor) {
    return (
      <Kabuk baslik={f.baslik ?? ''} kurum={f.kurumAdi}>
        <Card className="flex items-start gap-3 p-5">
          <span className="grid size-10 shrink-0 place-items-center rounded-md bg-(--st-wait-bg) text-(--st-wait)">
            <AlertTriangle size={20} />
          </span>
          <div>
            <p className="font-display text-base font-bold">Bu form şu anda yanıt almıyor</p>
            <p className="mt-1 text-sm text-ink-2">{f.kapaliSebebi}</p>
          </div>
        </Card>
      </Kabuk>
    );
  }

  const sonAdim = !cokAdimli || adim === adimlar.length - 1;

  function ileri() {
    const h = adimHatalari(f.tanim!, adim, cevaplar);
    setHatalar(h);

    if (Object.keys(h).length > 0) {
      // İlk hatalı alana kaydır: uzun bir adımda hatanın nerede olduğunu
      // aramak, formu terk etmenin bir numaralı sebebi.
      document.querySelector(`#alan-${Object.keys(h)[0]}`)
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }

    setAdim((a) => a + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function gonderimYap() {
    const h = tumHatalar(f.tanim!, cevaplar);
    setHatalar(h);

    if (Object.keys(h).length > 0) {
      document.querySelector(`#alan-${Object.keys(h)[0]}`)
        ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return;
    }

    try {
      const s = await gonder.mutateAsync({
        surdurmeAnahtari: surdurmeAnahtari ?? undefined,
        cihazAnahtari,
        cevaplar: gonderilecek(f.tanim!, cevaplar) as Record<string, unknown>,
        adSoyad: kimlik.adSoyad || undefined,
        telefon: kimlik.telefon || undefined,
        eposta: kimlik.eposta || undefined,
      });

      localStorage.removeItem(depoAnahtari);
      // Gönderim anahtarı düşer, CİHAZ anahtarı kalır: biri "bu doldurmayı
      // bir kez kaydet", öteki "bu kişi bu formu yanıtladı" demek.
      localStorage.removeItem(`sv-form-gonderim-${anahtar}`);
      setSonuc(s);
      window.scrollTo({ top: 0 });
    } catch {
      /* hata mesajı aşağıda gösteriliyor */
    }
  }

  return (
    <Kabuk baslik={f.baslik ?? ''} kurum={f.kurumAdi} aciklama={f.aciklama}>
      {cokAdimli && (
        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-xs text-ink-3">
            <span>{adimlar[adim]?.baslik || `Adım ${adim + 1}`}</span>
            <span className="tabular-nums">{adim + 1} / {adimlar.length}</span>
          </div>
          {(f.tanim?.ayarlar?.ilerlemeCubugu ?? true) && (
            <div className="h-1.5 overflow-hidden rounded-full bg-sunken">
              <div
                className="h-full rounded-full bg-brand transition-[width] duration-300"
                style={{ width: `${ilerleme}%` }}
              />
            </div>
          )}
        </div>
      )}

      <Card className="p-4 md:p-5">
        <FormRenderer
          tanim={f.tanim!}
          cevaplar={cevaplar}
          degistir={degistir}
          hatalar={hatalar}
          adim={cokAdimli ? adim : undefined}
          yukle={async (alanKimligi, dosya) => {
            const govde = new FormData();
            govde.append('alanKimligi', alanKimligi);
            govde.append('dosya', dosya);
            if (surdurmeAnahtari) govde.append('surdurmeAnahtari', surdurmeAnahtari);

            const y = await fetch(`/api/v2/form-portal/${anahtar}/dosya`, {
              method: 'POST', body: govde,
            });

            const metin = await y.text();
            if (!y.ok) {
              let mesaj = 'Dosya yüklenemedi.';
              try { mesaj = JSON.parse(metin).detail ?? mesaj; } catch { /* düz metin */ }
              throw new Error(mesaj);
            }

            const s = JSON.parse(metin) as {
              dosyaId: number; ad: string; surdurmeAnahtari: string;
            };

            setSurdurmeAnahtari(s.surdurmeAnahtari);
            return { dosyaId: s.dosyaId, ad: s.ad };
          }}
        />

        {/* Kimlik alanları SON ADIMDA: formun başında ad-telefon sormak,
            anonim olduğunu sanan kullanıcıyı baştan caydırıyor. */}
        {sonAdim && f.erisim !== FORM_ACCESS.anonim && (
          <div className="mt-5 space-y-3 border-t border-line pt-4">
            <p className="text-xs font-semibold text-ink-3">İLETİŞİM BİLGİLERİNİZ</p>
            <FieldWrapper etiket="Ad soyad" id="k-ad">
              <Input id="k-ad" value={kimlik.adSoyad}
                onChange={(e) => setKimlik({ ...kimlik, adSoyad: e.target.value })} />
            </FieldWrapper>
            <FieldWrapper
              etiket="Telefon" id="k-tel"
              zorunlu={f.erisim === FORM_ACCESS.telefonDogrulamali}
            >
              <Input id="k-tel" inputMode="tel" value={kimlik.telefon}
                onChange={(e) => setKimlik({ ...kimlik, telefon: e.target.value })} />
            </FieldWrapper>
          </div>
        )}
      </Card>

      {gonder.isError && (
        <div
          role="alert"
          className="flex items-start gap-2 rounded-md bg-(--st-no-bg) px-3 py-2.5 text-sm text-(--st-no)"
        >
          <AlertTriangle size={16} className="mt-0.5 shrink-0" />
          <span className="min-w-0 wrap-anywhere">{(gonder.error as Error).message}</span>
        </div>
      )}

      <div className="flex gap-2">
        {cokAdimli && adim > 0 && (
          <Button varyant="ikincil" boyut="mobil" onClick={() => {
            setAdim((a) => a - 1);
            window.scrollTo({ top: 0, behavior: 'smooth' });
          }}>
            <ArrowLeft size={16} />
            Geri
          </Button>
        )}

        {sonAdim ? (
          <Button
            boyut="mobil"
            className="flex-1"
            disabled={gonder.isPending}
            onClick={gonderimYap}
          >
            <Send size={16} />
            {gonder.isPending ? 'Gönderiliyor…' : 'Gönder'}
          </Button>
        ) : (
          <Button boyut="mobil" className="flex-1" onClick={ileri}>
            İleri
            <ArrowRight size={16} />
          </Button>
        )}
      </div>

      {/*
        BOT TUZAĞI — insan bunu görmüyor, otomatik doldurucu dolduruyor.
        `display:none` yerine ekran dışına alınıyor: bazı botlar gizli
        alanları atlıyor. `tabIndex={-1}` ve `aria-hidden` gerçek
        kullanıcıya hiç değdirmiyor.
      */}
      <input
        type="text" name="website" tabIndex={-1} aria-hidden autoComplete="off"
        className="absolute left-[-9999px] size-px opacity-0"
        onChange={(e) => { (window as unknown as Record<string, unknown>).__hp = e.target.value; }}
      />
    </Kabuk>
  );
}

/**
 * Formun kabuğu — uygulama gezinmesi YOK.
 *
 * <p>
 * <b>Amblem solda.</b> Vatandaş bu sayfayı bir SMS ya da QR koddan
 * açıyor; karşısına çıkan ilk şey formun hangi kuruma ait olduğu olmalı.
 * Kurum adını yazıya bırakmak yetmiyor — amblem tanınırlığı metinden
 * hızlı taşıyor ve sayfanın "resmî" olduğunu tek bakışta söylüyor.
 * </p>
 *
 * <p>
 * Amblem <c>useInstitution()</c>'dan geliyor, form yanıtından değil:
 * SPA marka bilgisini açılışta bir kez yüklüyor ve son yanıtı
 * <c>localStorage</c>'da tutuyor, yani çevrimdışı açılışta bile
 * kayboluyor değil. Forma özel bir amblem alanı eklemek, aynı kurumun
 * her formunda aynı dosyayı yeniden yönetmek demekti.
 * </p>
 */
function Kabuk({
  children, baslik, kurum, aciklama,
}: {
  children: React.ReactNode;
  baslik?: string;
  kurum?: string | null;
  aciklama?: string | null;
}) {
  const marka = useInstitution();
  const amblem = marka.marka.amblem ?? '/amblem.png';

  return (
    <div className={cn(
      'mx-auto flex min-h-dvh max-w-[720px] flex-col gap-4 p-4 pb-10',
    )}>
      {baslik && (
        <header className="flex items-start gap-3 pt-2">
          {/*
            Amblem `shrink-0`: uzun bir form başlığı onu ezmemeli.
            `object-contain` — kurum amblemleri kare olmak zorunda değil ve
            kırpmak logoyu bozar.
          */}
          <img
            src={amblem}
            alt=""
            className="size-12 shrink-0 rounded-lg object-contain md:size-14"
          />

          <div className="min-w-0 flex-1">
            {kurum && <p className="text-xs font-semibold text-ink-3">{kurum}</p>}
            <h1 className="mt-0.5 font-display text-2xl font-bold tracking-[-0.02em] wrap-anywhere">
              {baslik}
            </h1>
            {aciklama && (
              <p className="mt-1.5 text-sm leading-[1.6] text-ink-2 metin-guzel">{aciklama}</p>
            )}
          </div>
        </header>
      )}

      {children}

      <p className="mt-auto pt-4 text-center text-2xs text-ink-3">
        Bu form kurum tarafından oluşturulmuştur; giriş yapmanız gerekmez.
      </p>
    </div>
  );
}
