import { ArrowLeft, Layers, Plus, Save, Send, Settings2, Trash2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Button, IconButton } from '../../components/Button';
import { Card } from '../../components/Card';
import { FieldWrapper, Input, Secim, Textarea } from '../../components/Field';
import { FormModal } from '../../components/FormModal';
import { Skeleton } from '../../components/Skeleton';
import { Switch } from '../../components/Switch';
import { Tabs } from '../../components/Tabs';
import { useToast } from '../../components/Toast';
import { useIsDesktop } from '../../components/screenSize';
import { cn } from '../../components/utils';
import { useForm, useFormMutations } from '../../data/forms';
import type { FormDefinition, FormSave } from '../../data/types';
import { FieldPalette } from '../../forms/FieldPalette';
import { FieldSettings } from '../../forms/FieldSettings';
import { FormCanvas } from '../../forms/FormCanvas';
import { FormRenderer } from '../../forms/FormRenderer';
import { FORM_ACCESS_LABELS } from '../../forms/fieldTypes';
import {
  adimEkle, adimSil, alanBul, alanEkle, alanGuncelle,
  alanKopyala, alanSil, alanTasi, bosTanim, grupEkle, grupGuncelle,
  ileriKosullariDusur, yeniAlan,
} from '../../forms/definitionOps';
import type { Answers } from '../../forms/formEngine';

type Sekme = 'tasarim' | 'onizleme' | 'ayarlar';

/**
 * FORM TASARIMCISI.
 *
 * <h4>Üç sütun mu, üç sekme mi</h4>
 * <p>
 * Masaüstünde <b>palet | tuval | ayarlar</b> yan yana; telefonda üçü de
 * yan yana sığmıyor ve "küçültülmüş masaüstü" okunmaz oluyor. Mobilde
 * palet ve ayarlar birer <b>alt tabaka</b>, tuval tam genişlik: telefonda
 * asıl iş formun şeklini görmek.
 * </p>
 *
 * <h4>Kaydetmek yayınlamak değildir</h4>
 * <p>
 * Tasarım her zaman TASLAK sürüme yazılıyor; vatandaşın gördüğü, en son
 * <b>yayınlanan</b> sürüm. Böylece yayındaki bir formu düzenlerken
 * yarım kalan değişiklikler vatandaşa gitmiyor.
 * </p>
 */
export default function FormDesigner() {
  const { id } = useParams<{ id: string }>();
  const yeniMi = id === 'yeni';
  const formId = yeniMi ? undefined : Number(id);

  const gezin = useNavigate();
  const { bildir } = useToast();
  const masaustu = useIsDesktop();

  const kayit = useForm(formId);
  const m = useFormMutations(formId);

  const [sekme, setSekme] = useState<Sekme>('tasarim');
  const [tanim, setTanim] = useState<FormDefinition>(bosTanim);
  const [adim, setAdim] = useState(0);
  const [secili, setSecili] = useState<string | null>(null);
  const [paletAcik, setPaletAcik] = useState(false);
  const [ayarAcik, setAyarAcik] = useState(false);
  const [onizlemeCevaplari, setOnizlemeCevaplari] = useState<Answers>({});

  const [kunye, setKunye] = useState<FormSave>({
    baslik: '', aciklama: null, erisim: 0, tekYanit: false,
    yanitOzetiGorunur: true, sonuclarHerkeseAcik: false,
    tesekkurMetni: null, tesekkurAdresi: null,
    baslangicTarihi: null, bitisTarihi: null, yanitSiniri: null,
    tanim: bosTanim(),
  });

  useEffect(() => {
    if (!kayit.data) return;
    const d = kayit.data;

    setTanim(d.tanim ?? bosTanim());
    setKunye({
      baslik: d.baslik ?? '', aciklama: d.aciklama ?? null,
      erisim: d.erisim ?? 0, tekYanit: d.tekYanit ?? false,
      yanitOzetiGorunur: d.yanitOzetiGorunur ?? true,
      sonuclarHerkeseAcik: d.sonuclarHerkeseAcik ?? false,
      tesekkurMetni: d.tesekkurMetni ?? null, tesekkurAdresi: d.tesekkurAdresi ?? null,
      baslangicTarihi: d.baslangicTarihi ?? null, bitisTarihi: d.bitisTarihi ?? null,
      yanitSiniri: d.yanitSiniri ?? null,
      tanim: d.tanim ?? bosTanim(),
    });
  }, [kayit.data]);

  const seciliAlan = secili ? alanBul(tanim, secili) : null;
  const gruplar = (tanim.adimlar ?? [])[adim]?.gruplar ?? [];

  function alanEkleTikla(tip: number) {
    const grup = gruplar.length > 0 ? 0 : 0;
    const kolon = gruplar[grup]?.kolonSayisi ?? tanim.ayarlar?.kolonSayisi ?? 1;
    const yeni = yeniAlan(tip, kolon);

    setTanim((t) => alanEkle(t, adim, grup, yeni));
    setSecili(yeni.kimlik ?? null);
    setPaletAcik(false);
    if (!masaustu) setAyarAcik(true);
  }

  async function kaydet(sonra?: 'yayinla') {
    if (!kunye.baslik.trim()) {
      bildir('hata', 'Başlık zorunlu', 'Formun bir adı olmalı.');
      return;
    }

    try {
      const govde: FormSave = { ...kunye, tanim };

      const sonuc = yeniMi
        ? await m.olustur.mutateAsync(govde)
        : await m.guncelle.mutateAsync({ id: formId!, govde });

      if (sonra === 'yayinla') {
        await m.yayinla.mutateAsync(sonuc.id!);
        bildir('basari', 'Yayınlandı', 'Form artık vatandaş adresinden doldurulabilir.');
      } else {
        bildir('basari', 'Kaydedildi');
      }

      if (yeniMi) gezin(`/formlar/${sonuc.id}`, { replace: true });
    } catch (h) {
      bildir('hata', sonra === 'yayinla' ? 'Yayınlanamadı' : 'Kaydedilemedi', (h as Error).message);
    }
  }

  if (!yeniMi && kayit.isLoading) {
    return <div className="space-y-3 p-4"><Skeleton className="h-10 w-1/2" /><Skeleton className="h-64" /></div>;
  }

  return (
    <div className="space-y-3.5 pb-24 lg:pb-4">
      {/* ── başlık şeridi ── */}
      <div className="flex items-start gap-2">
        <Link to="/formlar" className="mt-0.5">
          <IconButton etiket="Formlara dön"><ArrowLeft size={18} /></IconButton>
        </Link>

        <div className="min-w-0 flex-1">
          <Input
            className="h-10 border-0 bg-transparent px-0 font-display text-xl font-bold shadow-none focus:ring-0"
            placeholder="Form başlığı"
            value={kunye.baslik}
            onChange={(e) => setKunye({ ...kunye, baslik: e.target.value })}
          />
          {kayit.data?.yayinlanmamisDegisiklik && (
            <p className="text-2xs text-(--st-wait)">Yayınlanmamış değişiklikler var</p>
          )}
        </div>

        <div className="flex shrink-0 gap-1.5">
          <Button varyant="ikincil" onClick={() => kaydet()} disabled={m.olustur.isPending || m.guncelle.isPending}>
            <Save size={15} />
            <span className="hidden sm:inline">Kaydet</span>
          </Button>
          <Button onClick={() => kaydet('yayinla')} disabled={m.yayinla.isPending}>
            <Send size={15} />
            <span className="hidden sm:inline">Yayınla</span>
          </Button>
        </div>
      </div>

      <Tabs<Sekme>
        deger={sekme}
        degistir={setSekme}
        sekmeler={[
          { deger: 'tasarim', etiket: 'Tasarım' },
          { deger: 'onizleme', etiket: 'Önizleme' },
          { deger: 'ayarlar', etiket: 'Ayarlar' },
        ]}
      />

      {sekme === 'tasarim' && (
        <>
          {/* adım şeridi */}
          <div className="flex flex-wrap items-center gap-1.5">
            {(tanim.adimlar ?? []).map((a, i) => (
              <button
                key={a.kimlik}
                type="button"
                onClick={() => { setAdim(i); setSecili(null); }}
                className={cn(
                  'h-9 rounded-md border px-3 text-xs font-semibold transition-colors',
                  i === adim ? 'border-brand bg-brand text-on-brand' : 'border-line bg-surface',
                )}
              >
                {a.baslik || `Adım ${i + 1}`}
              </button>
            ))}

            <IconButton etiket="Adım ekle" onClick={() => { setTanim(adimEkle); setAdim((tanim.adimlar ?? []).length); }}>
              <Plus size={15} />
            </IconButton>

            {(tanim.adimlar ?? []).length > 1 && (
              <IconButton etiket="Adımı sil" onClick={() => {
                setTanim((t) => adimSil(t, adim));
                setAdim((a) => Math.max(0, a - 1));
              }}>
                <Trash2 size={15} className="text-(--st-no)" />
              </IconButton>
            )}
          </div>

          <div className={cn('gap-3.5', masaustu && 'grid grid-cols-[220px_minmax(0,1fr)_300px]')}>
            {masaustu && (
              <aside className="min-w-0">
                <Card className="p-3">
                  <FieldPalette ekle={alanEkleTikla} />
                </Card>
              </aside>
            )}

            <div className="min-w-0">
              <FormCanvas
                tanim={tanim}
                adim={adim}
                secili={secili}
                sec={(k) => { setSecili(k); if (!masaustu) setAyarAcik(true); }}
                siralaDegisti={(grup, kaynak, hedefIndeks) => {
                  const tasinmis = alanTasi(tanim, kaynak, { adim, grup, indeks: hedefIndeks });
                  const { tanim: temiz, dusen } = ileriKosullariDusur(tasinmis);

                  // Taşıma bir koşulu ileriye baktırdıysa koşul düşürülüyor
                  // ve BU SÖYLENİYOR: sessizce düşürmek, kullanıcının
                  // kaydettiği kuralın kaybolduğunu fark etmemesi demek.
                  if (dusen.length > 0) {
                    bildir('uyari', 'Koşul kaldırıldı',
                      `${dusen.join(', ')} alanının koşulu kendisinden sonraki bir soruya bakıyordu.`);
                  }

                  setTanim(temiz);
                }}
                kopyala={(k) => setTanim((t) => alanKopyala(t, k))}
                sil={(k) => { setTanim((t) => alanSil(t, k)); if (secili === k) setSecili(null); }}
                grupEkle={() => setTanim((t) => grupEkle(t, adim))}
                grupYaz={(gi, kismi) => setTanim((t) => grupGuncelle(t, adim, gi, kismi))}
              />
            </div>

            {masaustu && (
              <aside className="min-w-0">
                <Card className="p-3.5">
                  {seciliAlan ? (
                    <FieldSettings
                      tanim={tanim}
                      alan={seciliAlan}
                      guncelle={(k) => setTanim((t) => alanGuncelle(t, secili!, k))}
                      sil={() => { setTanim((t) => alanSil(t, secili!)); setSecili(null); }}
                    />
                  ) : (
                    <p className="py-6 text-center text-xs text-ink-3">
                      Ayarlarını görmek için bir alan seçin.
                    </p>
                  )}
                </Card>
              </aside>
            )}
          </div>
        </>
      )}

      {sekme === 'onizleme' && (
        <Card className="p-4 md:p-5">
          {/* Önizleme GERÇEK oynatıcı: tasarımcıda gördüğün ile vatandaşın
              göreceği aynı bileşen. */}
          <FormRenderer
            tanim={tanim}
            cevaplar={onizlemeCevaplari}
            degistir={(k, c) => setOnizlemeCevaplari((o) => ({ ...o, [k]: c }))}
          />
        </Card>
      )}

      {sekme === 'ayarlar' && (
        <FormAyarlari kunye={kunye} yaz={setKunye} tanim={tanim} tanimYaz={setTanim} />
      )}

      {/* ── mobil: palet ve ayarlar tabakada ── */}
      {!masaustu && sekme === 'tasarim' && (
        <>
          <div className="fixed inset-x-0 bottom-0 z-20 flex gap-2 border-t border-line bg-surface px-4 pt-3"
            style={{ paddingBottom: 'calc(var(--h-tab) + env(safe-area-inset-bottom, 0px) + 12px)' }}>
            <Button varyant="ikincil" className="flex-1" onClick={() => setPaletAcik(true)}>
              <Plus size={15} />
              Alan ekle
            </Button>
            <Button
              varyant="ikincil"
              className="flex-1"
              disabled={!seciliAlan}
              onClick={() => setAyarAcik(true)}
            >
              <Settings2 size={15} />
              Ayarlar
            </Button>
          </div>

          <FormModal
            acik={paletAcik}
            kapat={() => setPaletAcik(false)}
            baslik="Alan ekle"
            aciklama="Seçtiğiniz alan bölümün sonuna eklenir."
            ikon={<Layers size={17} />}
            eylemler={<Button varyant="ikincil" onClick={() => setPaletAcik(false)}>Kapat</Button>}
          >
            <div className="p-3.5">
              <FieldPalette ekle={alanEkleTikla} />
            </div>
          </FormModal>

          <FormModal
            acik={ayarAcik && !!seciliAlan}
            kapat={() => setAyarAcik(false)}
            baslik="Alan ayarları"
            ikon={<Settings2 size={17} />}
            eylemler={<Button onClick={() => setAyarAcik(false)}>Tamam</Button>}
          >
            <div className="p-3.5">
              {seciliAlan && (
                <FieldSettings
                  tanim={tanim}
                  alan={seciliAlan}
                  guncelle={(k) => setTanim((t) => alanGuncelle(t, secili!, k))}
                  sil={() => { setTanim((t) => alanSil(t, secili!)); setSecili(null); setAyarAcik(false); }}
                />
              )}
            </div>
          </FormModal>
        </>
      )}
    </div>
  );
}

/* ────────────────────────────────────────────────── form ayarları */

function FormAyarlari({
  kunye, yaz, tanim, tanimYaz,
}: {
  kunye: FormSave;
  yaz: (k: FormSave) => void;
  tanim: FormDefinition;
  tanimYaz: (t: FormDefinition) => void;
}) {
  return (
    <div className="grid gap-3.5 lg:grid-cols-2">
      <Card className="space-y-3 p-3.5">
        <p className="text-xs font-semibold text-ink-3">FORM</p>

        <FieldWrapper etiket="Açıklama" id="f-aciklama"
          ipucu="Formun başında görünür.">
          <Textarea id="f-aciklama" rows={3} value={kunye.aciklama ?? ''}
            onChange={(e) => yaz({ ...kunye, aciklama: e.target.value })} />
        </FieldWrapper>

        <FieldWrapper etiket="Kimler doldurabilir?" id="f-erisim"
          ipucu={FORM_ACCESS_LABELS.find((x) => x.deger === kunye.erisim)?.ipucu}>
          <Secim id="f-erisim" value={String(kunye.erisim ?? 0)}
            onChange={(e) => yaz({ ...kunye, erisim: Number(e.target.value) as 0 })}>
            {FORM_ACCESS_LABELS.map((x) => (
              <option key={x.deger} value={x.deger}>{x.etiket}</option>
            ))}
          </Secim>
        </FieldWrapper>

        <Switch
          etiket="Kişi başına tek yanıt"
          aciklama="Yalnızca telefon ya da personel kipinde anlamlı; anonim formda 'aynı kişi' güvenilir değil."
          isaretli={kunye.tekYanit ?? false}
          degistir={(v) => yaz({ ...kunye, tekYanit: v })}
        />

        <div className="grid grid-cols-2 gap-2">
          <FieldWrapper etiket="Yanıt sınırı" id="f-sinir" ipucu="Boş = sınırsız">
            <Input id="f-sinir" type="number" value={String(kunye.yanitSiniri ?? '')}
              onChange={(e) => yaz({ ...kunye, yanitSiniri: e.target.value ? Number(e.target.value) : null })} />
          </FieldWrapper>
        </div>
      </Card>

      <Card className="space-y-3 p-3.5">
        <p className="text-xs font-semibold text-ink-3">GÖRÜNÜM VE SONUÇ</p>

        <FieldWrapper etiket="Kolon sayısı" id="f-kolon"
          ipucu="Telefonda her alan tam genişlik olur.">
          <Secim id="f-kolon" value={String(tanim.ayarlar?.kolonSayisi ?? 1)}
            onChange={(e) => tanimYaz({ ...tanim, ayarlar: {
              ...tanim.ayarlar, kolonSayisi: Number(e.target.value) } })}>
            <option value="1">Tek kolon</option>
            <option value="2">İki kolon</option>
            <option value="3">Üç kolon</option>
          </Secim>
        </FieldWrapper>

        <Switch
          etiket="Soruları numaralandır"
          isaretli={tanim.ayarlar?.numaralandir ?? false}
          degistir={(v) => tanimYaz({ ...tanim, ayarlar: { ...tanim.ayarlar, numaralandir: v } })}
        />

        <Switch
          etiket="İlerleme çubuğu"
          aciklama="Çok adımlı formlarda görünür."
          isaretli={tanim.ayarlar?.ilerlemeCubugu ?? true}
          degistir={(v) => tanimYaz({ ...tanim, ayarlar: { ...tanim.ayarlar, ilerlemeCubugu: v } })}
        />

        <FieldWrapper etiket="Teşekkür metni" id="f-tesekkur"
          ipucu="Gönderimden sonra gösterilir.">
          <Textarea id="f-tesekkur" rows={3} value={kunye.tesekkurMetni ?? ''}
            onChange={(e) => yaz({ ...kunye, tesekkurMetni: e.target.value })} />
        </FieldWrapper>

        <Switch
          etiket="Vatandaş kendi yanıtını görsün"
          aciklama="Sonuç sayfasında verdiği cevapların özeti çıkar."
          isaretli={kunye.yanitOzetiGorunur ?? false}
          degistir={(v) => yaz({ ...kunye, yanitOzetiGorunur: v })}
        />
      </Card>
    </div>
  );
}
