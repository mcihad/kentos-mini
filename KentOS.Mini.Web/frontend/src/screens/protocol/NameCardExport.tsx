import { Printer, Scissors } from 'lucide-react';
import { useState } from 'react';
import { FieldWrapper } from '../../components/Field';
import { Switch } from '../../components/Switch';
import { Button } from '../../components/Button';
import { FormModal } from '../../components/FormModal';
import { Segment } from '../../components/FilterSheet';
import { cn } from '../../components/utils';
import { download } from '../../data/download';
import { queryString } from '../../data/client';

type Tur = 'kesme' | 'masa';

/**
 * Kesme kartı ızgaraları.
 *
 * <p>
 * Serbest sayı girdirmek yerine hazır ölçü: kullanıcı "kaç kolon kaç satır"
 * diye düşünmüyor, "koltuk arkasına küçük etiket" ya da "geniş isimlik" diye
 * düşünüyor. Her ölçünün yanında karta düşen yaklaşık boy yazıyor, çünkü asıl
 * merak edilen o.
 * </p>
 */
const IZGARALAR = [
  { sutun: 1, satir: 5, ad: 'Çok geniş', not: '19 × 5 cm — 5 kart' },
  { sutun: 1, satir: 10, ad: 'Geniş şerit', not: '19 × 2,5 cm — 10 kart' },
  { sutun: 2, satir: 5, ad: 'Büyük', not: '9,5 × 5 cm — 10 kart' },
  { sutun: 2, satir: 8, ad: 'Orta', not: '9,5 × 3,1 cm — 16 kart' },
  { sutun: 2, satir: 10, ad: 'Standart', not: '9,5 × 2,5 cm — 20 kart' },
  { sutun: 3, satir: 8, ad: 'Küçük', not: '6,3 × 3,1 cm — 24 kart' },
  { sutun: 3, satir: 12, ad: 'Sıkı', not: '6,3 × 2,1 cm — 36 kart' },
  { sutun: 4, satir: 12, ad: 'En küçük', not: '4,7 × 2,1 cm — 48 kart' },
] as const;

/**
 * Tasarım katalogu — sunucudaki <c>KartTasarimlari</c> ile birebir.
 *
 * <p>
 * Uçtan da okunabiliyor (`GET davet/kart-tasarimlari`) ama listeyi burada
 * tutmak bir istek tasarruf ediyor ve <b>önizleme</b> için gereken görsel
 * bilgiyi (font ailesi, süsleme biçimi) sunucudan çekmeye gerek bırakmıyor.
 * Anahtarlar aynı; ad değişirse iki yerde değişir ve bu kabul edilebilir bir
 * tekrar, çünkü listenin kendisi yılda bir değişiyor.
 * </p>
 */
const TASARIMLAR = [
  { anahtar: 'sade', ad: 'Sade', font: 'ui-sans-serif', sus: 'yok' },
  { anahtar: 'altcizgi', ad: 'Alt çizgili', font: 'ui-sans-serif', sus: 'alt' },
  { anahtar: 'kurumsal', ad: 'Kurumsal şerit', font: 'ui-sans-serif', sus: 'sol' },
  { anahtar: 'ustserit', ad: 'Üst şerit', font: 'ui-sans-serif', sus: 'ust' },
  { anahtar: 'modern', ad: 'Modern', font: 'ui-sans-serif', sus: 'buyuk' },
  { anahtar: 'dar', ad: 'Dar (uzun ad)', font: 'ui-sans-serif', sus: 'dar' },
  { anahtar: 'klasik', ad: 'Klasik', font: 'ui-serif', sus: 'cift' },
  { anahtar: 'zarif', ad: 'Zarif', font: 'ui-serif', sus: 'cerceve' },
  { anahtar: 'resmi', ad: 'Resmî', font: 'ui-serif', sus: 'cift' },
  { anahtar: 'kose', ad: 'Köşe işaretli', font: 'ui-serif', sus: 'kose' },
] as const;

/**
 * İSİM KARTLARI — kesme etiketi ve masa kartı.
 *
 * <p>
 * Törenlerde isimlikler sandalyelere yapıştırılıyor ya da masaya konuyor ve
 * bu iş listeyi Word'e kopyalayıp elle tablo kurarak yapılıyordu: her
 * seferinde yeniden, her seferinde başka ölçüde. Kart artık davetle aynı
 * kaynaktan basılıyor.
 * </p>
 * <p>
 * <b>Kaynak protokol defteri DEĞİL, bu davet.</b> Masaya konacak isimlik
 * kurumun bütün protokol listesi değil, o törene çağrılanlar.
 * </p>
 */
export function NameCardExport({
  acik,
  kapat,
  davetId,
  davetBasligi,
}: {
  acik: boolean;
  kapat: () => void;
  davetId: number;
  davetBasligi?: string | null;
}) {
  const [tur, setTur] = useState<Tur>('kesme');
  const [izgara, setIzgara] = useState<(typeof IZGARALAR)[number]>(IZGARALAR[4]);
  const [sayfaBasi, setSayfaBasi] = useState<1 | 2>(2);
  const [tasarim, setTasarim] = useState<string>('sade');
  const [unvan, setUnvan] = useState(true);
  const [kurum, setKurum] = useState(false);
  const [kesmeCizgisi, setKesmeCizgisi] = useState(true);
  const [ciftYuz, setCiftYuz] = useState(true);
  const [logo, setLogo] = useState(false);
  /** 1 üst · 2 sol · 3 sağ. Geniş kartta yan, dar kartta üst iyi oturuyor. */
  const [logoYeri, setLogoYeri] = useState<'1' | '2' | '3'>('1');
  /** 0 küçük · 1 orta · 2 büyük — ad puntosuna oranlı. */
  const [logoBoyu, setLogoBoyu] = useState<'0' | '1' | '2'>('1');
  const [antet, setAntet] = useState(false);
  /** Yalnızca "katılacak" diyenler mi, davetin tamamı mı. */
  const [yalnizKatilacak, setYalnizKatilacak] = useState(true);

  function bas() {
    const ortak = {
      // 1 = Katılacak. Boş bırakılırsa davetin tamamı basılır.
      durum: yalnizKatilacak ? 1 : undefined,
      tasarim,
      unvan,
      kurum: kurum || undefined,
      logo: logo || undefined,
      logoYeri: logo ? Number(logoYeri) : undefined,
      logoBoyu: logo ? Number(logoBoyu) : undefined,
      antet: antet || undefined,
    };

    download(
      tur === 'kesme'
        ? `/davet/${davetId}/kesme-kartlari/pdf${queryString({
            ...ortak,
            sutun: izgara.sutun,
            satir: izgara.satir,
            kesmeCizgisi,
          })}`
        : `/davet/${davetId}/masa-kartlari/pdf${queryString({
            ...ortak,
            sayfaBasi,
            ciftYuz,
          })}`,
    );
    kapat();
  }

  return (
    <FormModal
      acik={acik}
      kapat={kapat}
      baslik="İsim kartları"
      aciklama={`${davetBasligi ?? 'Bu davet'} · A4 çıktı`}
      ikon={<Scissors size={15} />}
      genislik="orta"
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button onClick={bas}>
            <Printer size={14} />
            PDF oluştur
          </Button>
        </>
      }
    >
      {/*
        İlk soru KART TÜRÜ: kesme etiketi ile masa kartının ölçüsü de,
        katlaması da başka. Aynı pencerede iki ayrı yol yerine tek segment.
      */}
      <Segment
        deger={tur}
        degistir={setTur}
        secenekler={[
          { deger: 'kesme' as Tur, etiket: 'Kesme kartı' },
          { deger: 'masa' as Tur, etiket: 'Masa kartı' },
        ]}
      />

      {tur === 'kesme' ? (
        <FieldWrapper etiket="Kart boyu" id="kart-izgara">
          <div className="grid grid-cols-2 gap-1.5 sm:grid-cols-4">
            {IZGARALAR.map((o) => {
              const secili = o.sutun === izgara.sutun && o.satir === izgara.satir;
              return (
                <button
                  key={`${o.sutun}x${o.satir}`}
                  type="button"
                  onClick={() => setIzgara(o)}
                  className={cn(
                    'rounded-sm border p-2 text-left transition-colors active:scale-[0.98]',
                    secili ? 'border-brand bg-brand-soft' : 'border-line bg-surface',
                  )}
                  style={{ transitionTimingFunction: 'var(--ease-spring)' }}
                >
                  <span className="flex items-center justify-between gap-1">
                    <span className={cn('text-2xs font-semibold', secili && 'text-brand')}>
                      {o.ad}
                    </span>
                    <span className="shrink-0 text-2xs tabular-nums text-ink-3">
                      {o.sutun}×{o.satir}
                    </span>
                  </span>
                  <span className="mt-0.5 block text-2xs text-ink-3">{o.not}</span>

                  {/* Küçük ızgara önizlemesi: sayı okumaktan daha hızlı anlaşılıyor. */}
                  <span
                    aria-hidden
                    className="mt-1.5 grid gap-px"
                    style={{ gridTemplateColumns: `repeat(${o.sutun}, 1fr)` }}
                  >
                    {Array.from({ length: o.sutun * Math.min(o.satir, 6) }).map((_, i) => (
                      <span
                        key={i}
                        className={cn('h-[3px] rounded-[1px]', secili ? 'bg-brand/45' : 'bg-line-2')}
                      />
                    ))}
                  </span>
                </button>
              );
            })}
          </div>
        </FieldWrapper>
      ) : (
        <FieldWrapper etiket="Sayfaya kaç kart" id="kart-adet">
          <Segment
            deger={String(sayfaBasi) as '1' | '2'}
            degistir={(d) => setSayfaBasi(d === '1' ? 1 : 2)}
            secenekler={[
              { deger: '1', etiket: 'Tek (büyük)' },
              { deger: '2', etiket: 'İki (orta)' },
            ]}
          />
          <p className="mt-1.5 text-2xs text-ink-3">
            Card sayfanın ortasından katlanır; üst yüz ters basılır ki
            katlandığında konuğa düz baksın.
          </p>
        </FieldWrapper>
      )}

      <FieldWrapper etiket="Tasarım" id="kart-tasarim">
        <div className="grid grid-cols-2 gap-1.5 sm:grid-cols-5">
          {TASARIMLAR.map((t) => {
            const secili = t.anahtar === tasarim;
            return (
              <button
                key={t.anahtar}
                type="button"
                onClick={() => setTasarim(t.anahtar)}
                className={cn(
                  'rounded-sm border p-2 transition-colors active:scale-[0.98]',
                  secili ? 'border-brand bg-brand-soft' : 'border-line bg-surface',
                )}
                style={{ transitionTimingFunction: 'var(--ease-spring)' }}
              >
                {/* Tasarım önizlemesi: ad, süsleme ve font ailesi kabaca
                    kartın kendisi gibi çiziliyor — isim listesinden seçmek
                    "klasik mi zarif mi" sorusunu cevaplamıyordu. */}
                <span
                  aria-hidden
                  className={cn(
                    'grid h-[34px] place-items-center overflow-hidden rounded-xs bg-surface px-1',
                    t.sus === 'sol' && 'border-l-[3px] border-brand',
                    t.sus === 'ust' && 'border-t-[3px] border-brand',
                    t.sus === 'cerceve' && 'border border-(--gold)',
                    t.sus === 'kose' && 'border border-dashed border-(--gold)',
                  )}
                >
                  <span className="w-full text-center">
                    {t.sus === 'cift' && (
                      <span className="mx-auto mb-0.5 block h-px w-8 bg-(--gold)" />
                    )}
                    <span
                      className={cn(
                        'block truncate text-2xs font-bold text-brand',
                        t.sus === 'buyuk' && 'uppercase tracking-[0.12em]',
                        t.sus === 'dar' && 'uppercase tracking-[0.06em]',
                      )}
                      style={{ fontFamily: t.font }}
                    >
                      Ad Soyad
                    </span>
                    {t.sus === 'alt' && (
                      <span className="mx-auto my-0.5 block h-px w-6 bg-(--gold)" />
                    )}
                    <span className="block truncate text-3xs text-ink-3" style={{ fontFamily: t.font }}>
                      Unvan
                    </span>
                    {t.sus === 'cift' && (
                      <span className="mx-auto mt-0.5 block h-px w-8 bg-(--gold)" />
                    )}
                  </span>
                </span>
                <span
                  className={cn(
                    'mt-1 block truncate text-3xs font-semibold',
                    secili ? 'text-brand' : 'text-ink-3',
                  )}
                >
                  {t.ad}
                </span>
              </button>
            );
          })}
        </div>
      </FieldWrapper>

      <div className="space-y-2.5">
        {/* İlk soru "kimler basılacak" — masaya isimlik koyarken aranan
            neredeyse her zaman katılacak diyenler. */}
        <Switch
          isaretli={yalnizKatilacak}
          degistir={setYalnizKatilacak}
          etiket="Yalnızca katılacak diyenler"
        />
        <Switch isaretli={unvan} degistir={setUnvan} etiket="Unvan yazılsın" />
        <Switch isaretli={kurum} degistir={setKurum} etiket="Kurum yazılsın" />
        <Switch isaretli={logo} degistir={setLogo} etiket="Kurum amblemi kartta" />

        {/* Amblem açıkken yeri ve boyu seçilebiliyor: ilk sürümde tek yerleşim
            vardı (adın üstünde) ve amblem üç milimetreydi — "logo var"
            demekten öteye geçmiyordu. */}
        {logo && (
          <div className="ml-1 space-y-2 border-l border-line pl-3">
            <FieldWrapper etiket="Amblem yeri" id="logo-yeri">
              <Segment
                deger={logoYeri}
                degistir={setLogoYeri}
                secenekler={[
                  { deger: '2' as const, etiket: 'Sol' },
                  { deger: '1' as const, etiket: 'Üst' },
                  { deger: '3' as const, etiket: 'Sağ' },
                ]}
              />
            </FieldWrapper>
            <FieldWrapper etiket="Amblem boyu" id="logo-boyu">
              <Segment
                deger={logoBoyu}
                degistir={setLogoBoyu}
                secenekler={[
                  { deger: '0' as const, etiket: 'Küçük' },
                  { deger: '1' as const, etiket: 'Orta' },
                  { deger: '2' as const, etiket: 'Büyük' },
                ]}
              />
            </FieldWrapper>
          </div>
        )}
        <Switch isaretli={antet} degistir={setAntet} etiket="Kurum anteti basılsın" />

        {tur === 'kesme' ? (
          /*
            Kesme çizgisi kapatılabiliyor: etiket kâğıdına (hazır kesikli
            forma) basanlar için çizgi gereksiz ve baskıda görünüyor.
          */
          <Switch
            isaretli={kesmeCizgisi}
            degistir={setKesmeCizgisi}
            etiket="Kesme çizgileri basılsın"
          />
        ) : (
          <Switch
            isaretli={ciftYuz}
            degistir={setCiftYuz}
            etiket="Arka yüze de ad basılsın"
          />
        )}
      </div>
    </FormModal>
  );
}
