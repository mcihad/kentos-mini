import { Building2, Palette, RotateCcw, Save } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Button } from '../components/Button';
import { FieldWrapper, Input, Textarea } from '../components/Field';
import { FormSection } from '../components/FormSection';
import { useToast } from '../components/Toast';
import { api } from '../data/client';
import {
  applyDocumentIdentity, loadInstitution, refreshInstitution, type Institution,
} from '../institution/institution';
import { KURUM_TEMA_OLAYI, markaPaletiniUygula } from '../theme/palettes';

/**
 * KURUM BİLGİLERİ — Sistem → Kurum Bilgileri.
 *
 * <p>
 * Bu ekranın varlık sebebi: uygulama başka belediyelere verilecek ve kurum
 * adını, amblemi ya da kurumsal rengi değiştirmek için sunucuya girip dosya
 * düzenlemek, uygulamayı yeniden başlatmak gerekmesin. Kayıt veritabanında
 * tek satır; buradan düzenlenir, değişiklik bütün istemcilere anında yayılır.
 * </p>
 *
 * <p>
 * <b>Burada OLMAYANLAR:</b> veritabanı bağlantısı, JWT anahtarı, SMS parolası
 * ve nesne deposu anahtarları. Onlar <code>.env</code> dosyasında — bu kaydı
 * okumak için zaten veritabanına bağlanmak gerekiyor, dolayısıyla oraya
 * konulamazlar. Sırların yedeklere düşmemesi de tercih sebebi.
 * </p>
 *
 * <p>
 * Renk alanları hem renk seçici hem metin kutusu olarak veriliyor: seçici
 * dokunmatikte hızlı, metin kutusu ise kurumsal kimlik kılavuzundaki hex
 * kodunu yapıştırmak için gerekli.
 * </p>
 */

/** Sunucunun beklediği güncelleme gövdesi. */
type Form = {
  ad: string;
  kisaAd: string;
  gorunenAd: string;
  birim: string;
  kunye: string;
  webSitesi: string;
  adres: string;
  telefon: string;
  eposta: string;
  uygulamaAdi: string;
  uygulamaKisaAdi: string;
  uygulamaAciklamasi: string;
  markaBirincil: string;
  markaVurgu: string;
  markaNotr: string;
  markaBirincilKoyu: string;
  amblem: string;
  favicon: string;
  uygulamaIkonu: string;
  ciktiAmblemi: string;
};

const BOS: Form = {
  ad: '', kisaAd: '', gorunenAd: '', birim: '', kunye: '',
  webSitesi: '', adres: '', telefon: '', eposta: '',
  uygulamaAdi: '', uygulamaKisaAdi: '', uygulamaAciklamasi: '',
  markaBirincil: '', markaVurgu: '', markaNotr: '', markaBirincilKoyu: '',
  amblem: '', favicon: '', uygulamaIkonu: '', ciktiAmblemi: '',
};

function kurumdanForm(k: Institution): Form {
  return {
    ad: k.ad ?? '',
    kisaAd: k.kisaAd ?? '',
    gorunenAd: k.gorunenAd ?? '',
    birim: k.birim ?? '',
    kunye: k.kunye ?? '',
    webSitesi: k.webSitesi ?? '',
    adres: k.adres ?? '',
    telefon: k.telefon ?? '',
    eposta: k.eposta ?? '',
    uygulamaAdi: k.uygulamaAdi ?? '',
    uygulamaKisaAdi: k.uygulamaKisaAdi ?? '',
    uygulamaAciklamasi: k.uygulamaAciklamasi ?? '',
    markaBirincil: k.marka.birincil ?? '',
    markaVurgu: k.marka.vurgu ?? '',
    markaNotr: k.marka.notr ?? '',
    markaBirincilKoyu: k.marka.birincilKoyu ?? '',
    amblem: k.marka.amblem ?? '',
    favicon: k.marka.favicon ?? '',
    uygulamaIkonu: k.marka.uygulamaIkonu ?? '',
    ciktiAmblemi: '',
  };
}

export default function InstitutionSettings() {
  const { bildir } = useToast();
  const [form, setForm] = useState<Form>(BOS);
  const [baslangic, setBaslangic] = useState<Form>(BOS);
  const [yukleniyor, setYukleniyor] = useState(true);
  const [kaydediliyor, setKaydediliyor] = useState(false);

  useEffect(() => {
    let iptal = false;
    void loadInstitution().then((k) => {
      if (iptal) return;
      const f = kurumdanForm(k);
      setForm(f);
      setBaslangic(f);
      setYukleniyor(false);
    });
    return () => {
      iptal = true;
    };
  }, []);

  const degisti = useMemo(
    () => JSON.stringify(form) !== JSON.stringify(baslangic),
    [form, baslangic],
  );

  const yaz = <K extends keyof Form>(ad: K) => (deger: string) =>
    setForm((s) => ({ ...s, [ad]: deger }));

  const kaydet = async (olay: React.FormEvent) => {
    olay.preventDefault();

    if (!form.ad.trim()) {
      bildir('hata', 'Kurum adı zorunlu', 'Kurum adı boş bırakılamaz.');
      return;
    }

    setKaydediliyor(true);
    try {
      await api.put<Institution>('/institution', form);

      // Kaydettikten sonra sunucudan TAZE okunur: türetilen alanlar
      // (görünen ad boşsa kurum adına düşer gibi) sunucuda hesaplanıyor.
      const taze = await refreshInstitution();
      const f = kurumdanForm(taze);
      setForm(f);
      setBaslangic(f);

      // Marka ve başlık ANINDA uygulanır — kaydedip hiçbir şeyin
      // değişmediğini görmek "kaydedilmedi" izlenimi veriyor.
      markaPaletiniUygula(taze.marka);
      applyDocumentIdentity(taze);
      window.dispatchEvent(new Event(KURUM_TEMA_OLAYI));

      bildir('basari', 'Kurum bilgileri kaydedildi', 'Değişiklik tüm kullanıcılara yansıyacak.');
    } catch (h) {
      bildir('hata', 'Kaydedilemedi', (h as Error).message);
    } finally {
      setKaydediliyor(false);
    }
  };

  if (yukleniyor) {
    return <p className="p-4 text-sm text-ink-3">Kurum bilgileri yükleniyor…</p>;
  }

  return (
    <form onSubmit={kaydet} className="space-y-4 pb-24 md:pb-4">
      <FormSection
        baslik="KURUM KİMLİĞİ"
        aciklama="Giriş ekranında, menüde ve çıktıların tepesinde görünür."
      >
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <MetinAlani id="ad" etiket="Kurum adı" zorunlu deger={form.ad} yaz={yaz('ad')} />
          <MetinAlani
            id="kisaAd" etiket="Kısa ad" deger={form.kisaAd} yaz={yaz('kisaAd')}
            ipucu="Dar alanlarda kullanılır. Boşsa kurum adı yazılır."
          />
          <MetinAlani
            id="gorunenAd" etiket="Çıktılarda görünen ad" deger={form.gorunenAd}
            yaz={yaz('gorunenAd')} ipucu="Boşsa kurum adı kullanılır."
          />
          <MetinAlani
            id="birim" etiket="Birim" deger={form.birim} yaz={yaz('birim')}
            ipucu="Uygulamayı işleten birim, örn. Başkanlık Makamı."
          />
          <MetinAlani
            id="kunye" etiket="Künye satırı" deger={form.kunye} yaz={yaz('kunye')}
            className="md:col-span-2"
            ipucu="Giriş ekranının ve çıktıların dibinde görünür."
          />
        </div>
      </FormSection>

      <FormSection baslik="İLETİŞİM">
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <MetinAlani id="webSitesi" etiket="Ağ sitesi" deger={form.webSitesi} yaz={yaz('webSitesi')} />
          <MetinAlani id="eposta" etiket="E-posta" deger={form.eposta} yaz={yaz('eposta')} />
          <MetinAlani id="telefon" etiket="Telefon" deger={form.telefon} yaz={yaz('telefon')} />
          <FieldWrapper etiket="Adres" id="adres" className="md:col-span-2">
            <Textarea
              id="adres" rows={2} value={form.adres}
              onChange={(e) => yaz('adres')(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </FormSection>

      <FormSection
        baslik="UYGULAMA"
        aciklama="Sekme başlığında, ana ekran kısayolunda ve giriş ekranında görünür."
      >
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <MetinAlani id="uygulamaAdi" etiket="Uygulama adı" deger={form.uygulamaAdi} yaz={yaz('uygulamaAdi')} />
          <MetinAlani
            id="uygulamaKisaAdi" etiket="Kısa ad" deger={form.uygulamaKisaAdi}
            yaz={yaz('uygulamaKisaAdi')} ipucu="Ana ekran kısayolunda görünür (12 karaktere kadar iyi durur)."
          />
          <FieldWrapper etiket="Açıklama" id="uygulamaAciklamasi" className="md:col-span-2">
            <Textarea
              id="uygulamaAciklamasi" rows={2} value={form.uygulamaAciklamasi}
              onChange={(e) => yaz('uygulamaAciklamasi')(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </FormSection>

      <FormSection
        baslik="KURUMSAL RENKLER"
        aciklama="Tema motoru geri kalan bütün tonları bu renklerden türetir."
      >
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <RenkAlani
            id="markaBirincil" etiket="Birincil renk" deger={form.markaBirincil}
            yaz={yaz('markaBirincil')}
          />
          <RenkAlani
            id="markaBirincilKoyu" etiket="Birincil (koyu tema)" deger={form.markaBirincilKoyu}
            yaz={yaz('markaBirincilKoyu')}
            ipucu="Koyu zeminde okunabilir karşılığı. Boşsa fabrika değeri kullanılır."
          />
          <RenkAlani id="markaVurgu" etiket="Vurgu rengi" deger={form.markaVurgu} yaz={yaz('markaVurgu')} />
          <RenkAlani id="markaNotr" etiket="Nötr zemin" deger={form.markaNotr} yaz={yaz('markaNotr')} />
        </div>
      </FormSection>

      <FormSection
        baslik="GÖRSELLER"
        aciklama="Sunucudaki dosya yolları. Dosyaları wwwroot altına koyup yolu buraya yazın."
      >
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <GorselAlani id="amblem" etiket="Amblem" deger={form.amblem} yaz={yaz('amblem')} />
          <GorselAlani id="favicon" etiket="Sekme simgesi" deger={form.favicon} yaz={yaz('favicon')} />
          <GorselAlani id="uygulamaIkonu" etiket="Uygulama ikonu" deger={form.uygulamaIkonu} yaz={yaz('uygulamaIkonu')} />
          <GorselAlani
            id="ciktiAmblemi" etiket="Çıktı amblemi" deger={form.ciktiAmblemi}
            yaz={yaz('ciktiAmblemi')} ipucu="PDF ve isim kartlarında. Boşsa amblem kullanılır."
          />
        </div>
      </FormSection>

      {/*
        Kaydet çubuğu MOBİLDE SABİT: form uzun ve alta inmeden kaydedememek,
        kullanıcıyı her değişiklikte sayfanın dibine yolluyordu.
      */}
      <div
        className="fixed inset-x-0 bottom-[calc(var(--h-tab)+env(safe-area-inset-bottom))] z-30
          flex items-center gap-2 border-t border-border bg-surface px-4 py-3 shadow-2
          md:static md:inset-auto md:justify-end md:border-0 md:bg-transparent md:p-0 md:shadow-none"
      >
        <span className="flex-1 text-2xs text-ink-3 md:hidden">
          {degisti ? 'Kaydedilmemiş değişiklik var' : 'Tüm değişiklikler kaydedildi'}
        </span>
        {/* Mobilde 44px dokunma hedefi (design.md §4); masaüstünde eski ritim. */}
        <Button
          type="button" varyant="sade" className="h-11 md:h-9"
          disabled={!degisti || kaydediliyor}
          onClick={() => setForm(baslangic)}
        >
          <RotateCcw size={16} />
          Geri al
        </Button>
        <Button type="submit" className="h-11 md:h-9" disabled={!degisti || kaydediliyor}>
          <Save size={16} />
          {kaydediliyor ? 'Kaydediliyor…' : 'Kaydet'}
        </Button>
      </div>
    </form>
  );
}

// ── Alan yardımcıları ────────────────────────────────────────────────

function MetinAlani({
  id, etiket, deger, yaz, zorunlu, ipucu, className,
}: {
  id: string; etiket: string; deger: string; yaz: (d: string) => void;
  zorunlu?: boolean; ipucu?: string; className?: string;
}) {
  return (
    <FieldWrapper etiket={etiket} id={id} zorunlu={zorunlu} ipucu={ipucu} className={className}>
      <Input id={id} value={deger} onChange={(e) => yaz(e.target.value)} />
    </FieldWrapper>
  );
}

/**
 * Renk alanı — seçici + hex kutusu yan yana.
 *
 * Yalnızca seçici olsaydı kurumsal kimlik kılavuzundaki kodu yapıştırmak
 * mümkün olmazdı; yalnızca metin kutusu olsaydı dokunmatikte renk denemek
 * zahmetli olurdu.
 */
function RenkAlani({
  id, etiket, deger, yaz, ipucu,
}: {
  id: string; etiket: string; deger: string; yaz: (d: string) => void; ipucu?: string;
}) {
  const gecerli = /^#[0-9a-fA-F]{6}$/.test(deger.trim());

  return (
    <FieldWrapper
      etiket={etiket} id={id} ipucu={ipucu}
      hata={deger && !gecerli ? '#RRGGBB biçiminde olmalı' : undefined}
    >
      <div className="flex items-center gap-2">
        <input
          type="color"
          aria-label={`${etiket} seçici`}
          value={gecerli ? deger : '#000000'}
          onChange={(e) => yaz(e.target.value.toUpperCase())}
          className="h-11 w-12 shrink-0 cursor-pointer rounded-control border border-border bg-surface-2 p-1 md:h-10"
        />
        <Input
          id={id} value={deger} placeholder="#002E6D" hatali={Boolean(deger) && !gecerli}
          onChange={(e) => yaz(e.target.value)}
        />
      </div>
    </FieldWrapper>
  );
}

/** Görsel yolu + küçük önizleme; yol yanlışsa önizleme boş kalır. */
function GorselAlani({
  id, etiket, deger, yaz, ipucu,
}: {
  id: string; etiket: string; deger: string; yaz: (d: string) => void; ipucu?: string;
}) {
  return (
    <FieldWrapper etiket={etiket} id={id} ipucu={ipucu}>
      <div className="flex items-center gap-2">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-control border border-border bg-surface-2">
          {deger ? (
            <img src={deger} alt="" className="max-h-8 max-w-8 object-contain" />
          ) : (
            <Building2 size={16} className="text-ink-3" />
          )}
        </span>
        <Input id={id} value={deger} placeholder="/amblem.png" onChange={(e) => yaz(e.target.value)} />
      </div>
    </FieldWrapper>
  );
}

/** Menüde kullanılan ikon — tek yerden okunsun diye burada duruyor. */
export const InstitutionSettingsIcon = Palette;
