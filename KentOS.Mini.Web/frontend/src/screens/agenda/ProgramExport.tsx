import {
  CalendarDays, FileSpreadsheet, FileText, LayoutGrid, LayoutList,
  NotebookPen, Printer, Rows3,
} from 'lucide-react';
import { useState } from 'react';
import { Button } from '../../components/Button';
import { FieldWrapper } from '../../components/Field';
import { Segment } from '../../components/FilterSheet';
import { FormModal } from '../../components/FormModal';
import { DatePicker } from '../../components/DatePicker';
import { useToast } from '../../components/Toast';
import { cn } from '../../components/utils';
import { download } from '../../data/download';
import { tokenStore, queryString } from '../../data/client';
import { dayName, shortDate } from '../../data/format';
import { haptic } from '../../data/haptics';

/**
 * Günlük program çıktı tasarımları.
 *
 * <p>
 * Sayısal değerler sunucudaki <c>ProgramTasarimi</c> ile birebir; sıra
 * değişirse "standart" istenip "boş not sayfası" basılır.
 * </p>
 *
 * <p>
 * Her tasarımın yanında <b>ne işe yaradığı</b> yazıyor, adı değil: kullanıcı
 * "kompakt mı detaylı mı" diye değil, "masaya mı konacak, duvara mı asılacak"
 * diye düşünüyor. Önizleme şeması da bu yüzden var — altı satırlık bir
 * açıklamayı okumaktan hızlı anlaşılıyor.
 * </p>
 */
const TASARIMLAR = [
  {
    deger: 1, ad: 'Standart', aciklama: 'Saat · Konu · Yer tablosu',
    ikon: LayoutGrid, sema: 'tablo',
  },
  {
    deger: 2, ad: 'Kompakt', aciklama: 'Büyük punto, uzaktan okunur',
    ikon: Rows3, sema: 'genis',
  },
  {
    deger: 3, ad: 'Detaylı', aciklama: 'İrtibat ve hazırlık bilgileriyle',
    ikon: LayoutList, sema: 'detay',
  },
  {
    deger: 5, ad: 'Saat şeridi', aciklama: 'Dikey zaman çizgisi',
    ikon: LayoutList, sema: 'serit',
  },
  {
    deger: 6, ad: 'Pano', aciklama: 'Duvara asılır, çok büyük punto',
    ikon: LayoutGrid, sema: 'pano',
  },
  {
    deger: 4, ad: 'Boş not sayfası', aciklama: 'Yanında el yazısı alanı',
    ikon: NotebookPen, sema: 'not',
  },
] as const;

type Sema = (typeof TASARIMLAR)[number]['sema'];

/** Bugünü `yyyy-MM-dd` olarak verir — sunucunun beklediği biçim. */
function bugun(): string {
  const d = new Date();
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

/** `yyyy-MM-dd` üzerine gün ekler; ay/yıl sarmasını Date halleder. */
function gunEkle(gun: string, adet: number): string {
  const [y, a, g] = gun.split('-').map(Number);
  const d = new Date(y, (a ?? 1) - 1, (g ?? 1) + adet);
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

/**
 * PROGRAM ÇIKTI PENCERESİ — tarih ve tasarım birlikte sorulur.
 *
 * <h3>Neden pencere</h3>
 * <p>
 * Çıktı önce bir açılır menüydü ve <b>tarihi hiç sormuyordu</b>: ekranda
 * görünen dönemin ilk günü sessizce kullanılıyordu. Kullanıcı "yarının
 * programını bas" diyemiyor, önce takvimi o güne getirip sonra çıktı almak
 * zorunda kalıyordu — üstelik hangi günün basıldığı çıktı elde edilene kadar
 * görünmüyordu. Davet çıktılarında olduğu gibi tek bir pencerede
 * <b>tarih + tasarım</b> sorulup oradan basılıyor.
 * </p>
 *
 * <h3>Neden önizleme şeması</h3>
 * <p>
 * Altı tasarımın farkı yazıyla anlatılınca ("kompakt", "detaylı") kullanıcı
 * hangisinin kâğıtta ne göstereceğini kestiremiyordu. Her seçeneğin yanında
 * kabaca sayfa düzenini çizen küçük bir şema var; aynı yaklaşım kesme kartı
 * penceresinde (<c>NameCardExport</c>) zaten kullanılıyor ve orada işe
 * yaradığı görüldü.
 * </p>
 *
 * <h3>İki çıkış yolu</h3>
 * <p>
 * <b>Yazdır</b> tarayıcıda HTML önizleme açar (kâğıda basmadan önce görülür,
 * yazıcı ayarları kullanıcının), <b>PDF</b> dosyayı indirir (e-postayla
 * göndermek için).
 * </p>
 */
export function ProgramExport({
  acik,
  kapat,
  /** Pencere açılırken önerilecek gün — ekranda görünen dönemin başı. */
  onerilenTarih,
  /** Liste dışa aktarımı — verilmezse o bölüm hiç çizilmez. */
  excel,
  pdf,
}: {
  acik: boolean;
  kapat: () => void;
  onerilenTarih?: string;
  excel?: () => void;
  pdf?: () => void;
}) {
  const { bildir } = useToast();
  const [tarih, setTarih] = useState(() => onerilenTarih || bugun());
  const [tasarim, setTasarim] = useState<number>(1);
  /*
    TEK KARAR AKIŞI: kapsam → seçenekler → TEK eylem.

    İlk sürümde pencerede beş düğme vardı (Excel · PDF · Vazgeç · PDF indir ·
    Yazdır) ve mobilde üçü alt çubuğa sıkışıyordu: kullanıcı hangisinin asıl
    eylem olduğunu ayırt edemiyordu. Şartname §6.5 alt bara en fazla İKİ
    eylem veriyor — biri birincil, biri vazgeç.

    Çözüm düğme kaldırmak değil, SORUYU BÖLMEK: önce ne basılacak
    (günlük program mı liste mi), sonra nasıl (yazdır/PDF/Excel). Alt çubukta
    tek birincil düğme kalıyor ve etiketi seçime göre değişiyor, yani düğme
    ne yapacağını kendisi söylüyor.
  */
  const [kapsam, setKapsam] = useState<'program' | 'liste'>('program');
  const [bicim, setBicim] = useState<'yazdir' | 'pdf'>('yazdir');
  const [listeBicimi, setListeBicimi] = useState<'excel' | 'pdf'>('excel');
  const [calisiyor, setCalisiyor] = useState<'html' | 'pdf' | null>(null);

  /*
    PENCERE HER AÇILIŞTA ÖNERİLEN GÜNE DÖNER.

    Kullanıcı bir kez 3 gün sonrasını bastıktan sonra pencereyi tekrar
    açtığında o tarihi hatırlamak yanlış: bir sonraki iş neredeyse her zaman
    "şu an baktığım gün". `useEffect` yerine render sırasında karşılaştırma —
    fazladan bir boyama turu olmuyor.
  */
  const [sonAcik, setSonAcik] = useState(acik);
  if (acik !== sonAcik) {
    setSonAcik(acik);
    if (acik) {
      setTarih(onerilenTarih || bugun());
      setTasarim(1);
      setKapsam('program');
      setBicim('yazdir');
      setListeBicimi('excel');
    }
  }

  /**
   * HTML önizlemeyi yeni sekmede açar.
   *
   * Jeton `Authorization` başlığıyla gitmek zorunda; `window.open` başlık
   * gönderemez. Bu yüzden içerik `fetch` ile alınıp yeni pencereye yazılıyor —
   * jeton adres çubuğuna hiç düşmüyor.
   */
  async function yazdir() {
    setCalisiyor('html');
    try {
      const jeton = tokenStore.read();
      const yanit = await fetch(
        `/api/v2/disa-aktar/gunluk-program/html${queryString({ tarih, tasarim })}`,
        { headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : {} },
      );
      if (!yanit.ok) throw new Error(`Sunucu ${yanit.status} döndü.`);

      const html = await yanit.text();
      const pencere = window.open('', '_blank');
      if (!pencere) {
        bildir('uyari', 'Açılır pencere engellendi', 'Tarayıcı ayarlarından izin verin.');
        return;
      }
      pencere.document.write(html);
      pencere.document.close();
      haptic('basari');
      kapat();
    } catch (h) {
      haptic('hata');
      bildir('hata', 'Program açılamadı', (h as Error).message);
    } finally {
      setCalisiyor(null);
    }
  }

  async function pdfIndir() {
    setCalisiyor('pdf');
    try {
      await download('/disa-aktar/gunluk-program', { tarih, tasarim });
      haptic('basari');
      kapat();
    } catch (h) {
      haptic('hata');
      bildir('hata', 'PDF indirilemedi', (h as Error).message);
    } finally {
      setCalisiyor(null);
    }
  }

  const secili = TASARIMLAR.find((t) => t.deger === tasarim) ?? TASARIMLAR[0];

  /*
    TEK GİRİŞ NOKTASI. Dört çıktı yolu (program yazdır/PDF, liste
    Excel/PDF) tek düğmeye bağlı; hangisinin çalışacağını kapsam ve biçim
    seçimi belirliyor. Düğmenin etiketi de buradan türüyor — kullanıcı
    basmadan önce ne olacağını okuyor.
  */
  async function uygula() {
    if (kapsam === 'liste') {
      const calistir = listeBicimi === 'excel' ? excel : pdf;
      calistir?.();
      haptic('basari');
      kapat();
      return;
    }
    if (bicim === 'yazdir') await yazdir();
    else await pdfIndir();
  }

  const eylemEtiketi =
    kapsam === 'liste'
      ? (listeBicimi === 'excel' ? 'Excel indir' : 'PDF indir')
      : (bicim === 'yazdir' ? 'Yazdır' : 'PDF indir');

  const eylemIkonu =
    kapsam === 'liste'
      ? (listeBicimi === 'excel' ? <FileSpreadsheet size={15} /> : <FileText size={15} />)
      : (bicim === 'yazdir' ? <Printer size={15} /> : <FileText size={15} />);

  return (
    <FormModal
      acik={acik}
      kapat={kapat}
      baslik="Program çıktısı"
      aciklama="Ne basılacak, hangi düzende ve nasıl alınacak?"
      ikon={<Printer size={15} />}
      genislik="genis"
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          {/*
            TEK BİRİNCİL EYLEM — etiketi ne yapacağını söylüyor.

            Şartname §6.5: alt bar bir birincil + bir ikincil eylem taşır.
            Önce burada üç düğme vardı (Vazgeç · PDF indir · Yazdır) ve
            mobilde hangisinin asıl eylem olduğu ayırt edilemiyordu.
          */}
          <Button onClick={() => void uygula()} disabled={calisiyor !== null}>
            {eylemIkonu}
            {calisiyor ? 'Hazırlanıyor…' : eylemEtiketi}
          </Button>
        </>
      }
    >
      <div className="space-y-5">
        {/*
          ÖNCE NE BASILACAK. İki ayrı iş tek pencerede toplandı: günlük
          program (tek gün, kâğıda) ve liste dışa aktarımı (ekrandaki
          süzgeçli liste, dosyaya). İkisinin alanları ve çıktı biçimleri
          farklı; aynı anda göstermek pencereyi düğme yığınına çeviriyordu.
        */}
        {(excel || pdf) && (
          <Segment
            deger={kapsam}
            degistir={(d) => {
              setKapsam(d);
              haptic('secim');
            }}
            secenekler={[
              { deger: 'program' as const, etiket: 'Günlük program' },
              { deger: 'liste' as const, etiket: 'Liste' },
            ]}
          />
        )}

        {kapsam === 'program' ? (
          <>
            {/* ── Gün ── */}
            <FieldWrapper etiket="Gün" id="cikti-tarih">
              <DatePicker id="cikti-tarih" deger={tarih} degistir={setTarih} />
              {/*
                HIZLI GÜNLER — takvimi açmadan.

                Basılan program neredeyse her zaman bugün ya da yarın; takvimi
                açıp gün seçmek iki dokunuş fazla. "Dün" de duruyor çünkü geçmiş
                günün programı toplantı tutanağına ek olarak isteniyor.
              */}
              <div className="mt-2 flex flex-wrap gap-1.5">
                {[
                  { etiket: 'Dün', deger: gunEkle(bugun(), -1) },
                  { etiket: 'Bugün', deger: bugun() },
                  { etiket: 'Yarın', deger: gunEkle(bugun(), 1) },
                ].map((k) => (
                  <button
                    key={k.etiket}
                    type="button"
                    onClick={() => {
                      setTarih(k.deger);
                      haptic('secim');
                    }}
                    aria-pressed={tarih === k.deger}
                    className={cn(
                      'bas-yay h-9 rounded-full border px-3.5 text-sm font-semibold transition-colors',
                      tarih === k.deger
                        ? 'border-brand bg-brand text-on-brand'
                        : 'border-border bg-surface text-text-2 hover:bg-surface-2 hover:text-text',
                    )}
                  >
                    {k.etiket}
                  </button>
                ))}
              </div>

              {/*
                SEÇİLEN GÜN YAZIYLA DA YAZILI: "12.09.2026" ile "Cumartesi" ayrı
                şeyler söylüyor ve hafta sonuna program basıldığını fark etmek
                ancak gün adıyla mümkün.
              */}
              <p className="mt-2 flex items-center gap-1.5 text-2xs text-text-3">
                <CalendarDays size={12} aria-hidden />
                {shortDate(tarih)} · {dayName(tarih)}
              </p>
            </FieldWrapper>

            {/* ── Sayfa düzeni ── */}
            <FieldWrapper etiket="Sayfa düzeni" id="cikti-tasarim">
              <div
                id="cikti-tasarim"
                role="radiogroup"
                aria-label="Sayfa düzeni"
                className="grid grid-cols-2 gap-2 lg:grid-cols-3"
              >
                {TASARIMLAR.map((t) => {
                  const aktif = t.deger === tasarim;
                  return (
                    <button
                      key={t.deger}
                      type="button"
                      role="radio"
                      aria-checked={aktif}
                      onClick={() => {
                        setTasarim(t.deger);
                        haptic('secim');
                      }}
                      className={cn(
                        'bas-yay flex items-start gap-2.5 rounded-md border p-2.5 text-left transition-colors',
                        aktif
                          ? 'border-brand bg-brand-soft'
                          : 'border-border bg-surface hover:bg-surface-2',
                      )}
                    >
                      <SayfaSemasi sema={t.sema} aktif={aktif} />
                      <span className="min-w-0 flex-1">
                        <span
                          className={cn(
                            'block truncate text-sm font-bold',
                            aktif ? 'text-brand' : 'text-ink',
                          )}
                        >
                          {t.ad}
                        </span>
                        <span className="mt-0.5 block text-2xs leading-[1.35] text-text-3">
                          {t.aciklama}
                        </span>
                      </span>
                    </button>
                  );
                })}
              </div>
            </FieldWrapper>

            {/* ── Biçim ── */}
            <FieldWrapper etiket="Nasıl alınsın?" id="cikti-bicim">
              <Segment
                deger={bicim}
                degistir={(d) => {
                  setBicim(d);
                  haptic('secim');
                }}
                secenekler={[
                  { deger: 'yazdir' as const, etiket: 'Yazdır' },
                  { deger: 'pdf' as const, etiket: 'PDF indir' },
                ]}
              />
              <p className="mt-2 text-2xs text-text-3">
                {bicim === 'yazdir'
                  ? 'Tarayıcıda önizleme açılır; yazıcı ayarları sizin.'
                  : 'Dosya olarak iner — e-postayla göndermek için.'}
              </p>
            </FieldWrapper>
          </>
        ) : (
          /* ── Liste dışa aktarımı ── */
          <FieldWrapper etiket="Nasıl alınsın?" id="cikti-liste-bicim">
            <Segment
              deger={listeBicimi}
              degistir={(d) => {
                setListeBicimi(d);
                haptic('secim');
              }}
              secenekler={[
                { deger: 'excel' as const, etiket: 'Excel' },
                { deger: 'pdf' as const, etiket: 'PDF' },
              ]}
            />
            <p className="mt-2 text-2xs text-text-3">
              Ekranda görünen listeyi süzgeçleriyle birlikte dışa aktarır —
              günlük programdan bağımsızdır.
            </p>
          </FieldWrapper>
        )}

        {/*
          SEÇİMİN ÖZETİ ALT ÇUBUĞUN HEMEN ÜSTÜNDE. Uzun bir pencerede
          birincil düğmeye basarken hangi günün ve düzenin seçili olduğu
          ekranın yukarısında kalıyordu.
        */}
        <p className="rounded-sm bg-sunken px-3 py-2 text-2xs text-text-2">
          {kapsam === 'program' ? (
            <>
              <span className="font-semibold text-ink">{shortDate(tarih)}</span> günü,{' '}
              <span className="font-semibold text-ink">
                {secili.ad.toLocaleLowerCase('tr-TR')}
              </span>{' '}
              düzeninde {bicim === 'yazdir' ? 'yazdırılacak' : 'PDF olarak inecek'}.
            </>
          ) : (
            <>
              Ekrandaki liste{' '}
              <span className="font-semibold text-ink">
                {listeBicimi === 'excel' ? 'Excel' : 'PDF'}
              </span>{' '}
              olarak inecek.
            </>
          )}
        </p>
      </div>
    </FormModal>
  );
}

/**
 * Sayfa düzeninin küçük şeması.
 *
 * Gerçek bir önizleme değil — kâğıdın nasıl bölündüğünü gösteren birkaç
 * çizgi. Amaç "hangisi büyük puntolu, hangisi tablo" sorusunu okumadan
 * cevaplamak; ayrıntı zaten seçenegin yanında yazılı.
 */
function SayfaSemasi({ sema, aktif }: { sema: Sema; aktif: boolean }) {
  const cizgi = aktif ? 'bg-brand/45' : 'bg-line-2';
  const kutu = cn(
    'grid h-9 w-8 shrink-0 content-start gap-[3px] rounded-xs border p-1',
    aktif ? 'border-brand/40 bg-surface' : 'border-border bg-sunken',
  );

  if (sema === 'tablo') {
    return (
      <span className={kutu} aria-hidden>
        {[0, 1, 2, 3].map((i) => (
          <span key={i} className={cn('h-[3px] w-full rounded-full', cizgi)} />
        ))}
      </span>
    );
  }
  if (sema === 'genis') {
    return (
      <span className={kutu} aria-hidden>
        {[0, 1].map((i) => (
          <span key={i} className={cn('h-[6px] w-full rounded-xs', cizgi)} />
        ))}
      </span>
    );
  }
  if (sema === 'detay') {
    return (
      <span className={kutu} aria-hidden>
        {[0, 1, 2].map((i) => (
          <span key={i} className="flex gap-[2px]">
            <span className={cn('h-[3px] w-1/3 rounded-full', cizgi)} />
            <span className={cn('h-[3px] flex-1 rounded-full opacity-60', cizgi)} />
          </span>
        ))}
      </span>
    );
  }
  if (sema === 'serit') {
    return (
      <span className={cn(kutu, 'grid-cols-[4px_1fr] items-start gap-x-[3px]')} aria-hidden>
        <span className={cn('row-span-4 h-full w-[3px] rounded-full', cizgi)} />
        {[0, 1, 2].map((i) => (
          <span key={i} className={cn('h-[3px] w-full rounded-full', cizgi)} />
        ))}
      </span>
    );
  }
  if (sema === 'pano') {
    return (
      <span className={kutu} aria-hidden>
        <span className={cn('h-[11px] w-full rounded-xs', cizgi)} />
        <span className={cn('h-[7px] w-2/3 rounded-xs', cizgi)} />
      </span>
    );
  }
  // not
  return (
    <span className={cn(kutu, 'grid-cols-2 gap-x-[3px]')} aria-hidden>
      {[0, 1, 2].map((i) => (
        <span key={i} className={cn('h-[3px] w-full rounded-full', cizgi)} />
      ))}
      <span className={cn('col-start-2 row-start-1 row-span-3 h-full w-full rounded-xs border border-dashed', aktif ? 'border-brand/40' : 'border-line-2')} />
    </span>
  );
}
