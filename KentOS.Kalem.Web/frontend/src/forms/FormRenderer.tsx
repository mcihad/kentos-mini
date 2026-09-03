import { useMemo } from 'react';
import { cn } from '../components/utils';
import type { FormDefinition, FormGroup } from '../data/types';
import { FormFieldInput } from './FormFieldInput';
import { isBlock } from './fieldTypes';
import { alanGorunur, grupGorunur, type Answers } from './formEngine';

/**
 * FORM OYNATICI — tanımı ekrana çizer.
 *
 * <p>
 * Hem vatandaş sayfası hem tasarımcı önizlemesi bunu kullanıyor: iki ayrı
 * çizim, tasarımcıda görülenle vatandaşın gördüğünün zamanla ayrışması
 * demekti.
 * </p>
 *
 * <h4>Kolon düzeni</h4>
 * <p>
 * Izgara masaüstünde <b>daima 12 kolon</b>; <c>kolonSayisi</c> DOM'a hiç
 * inmiyor, yalnızca tasarımcıda yeni alanın varsayılan genişliğini
 * belirliyor (<c>12 / kolonSayisi</c>). Böylece 3 kolonlu bir grupta bir
 * alan "iki kolon kapla" diyebiliyor.
 * </p>
 * <p>
 * <b>Mobilde her alan tam genişlik.</b> 390px'te iki kolon, alan başına
 * 171px demek; etiket sığmıyor ve 44px dokunma kuralı genişliği
 * kurtarmıyor.
 * </p>
 * <p>
 * <c>minmax(0, 1fr)</c> ve hücrelerde <c>min-w-0</c> <b>pazarlıksız</b>:
 * <c>1fr</c> aslında <c>minmax(auto, 1fr)</c> ve uzun bir seçenek metni
 * ızgarayı görünür alandan taşırıyor — bu depoda ölçülmüş bir hata
 * (628px yatay taşma).
 * </p>
 */
export function FormRenderer({
  tanim,
  cevaplar,
  degistir,
  hatalar,
  adim,
  pasif,
  yukle,
}: {
  tanim: FormDefinition;
  cevaplar: Answers;
  degistir: (kimlik: string, cevap: { deger?: unknown; metin?: string }) => void;
  hatalar?: Record<string, string>;
  /** Verilirse yalnızca o adım çizilir (stepper). */
  adim?: number;
  pasif?: boolean;
  /** Dosya yükleyici — yalnızca vatandaş sayfasında verilir. */
  yukle?: (alanKimligi: string, dosya: File) => Promise<{ dosyaId: number; ad: string }>;
}) {
  const adimlar = useMemo(
    () => (adim === undefined ? (tanim.adimlar ?? []) : [(tanim.adimlar ?? [])[adim]].filter(Boolean)),
    [tanim, adim],
  );

  const formKolon = tanim.ayarlar?.kolonSayisi ?? 1;
  let sira = 0;

  return (
    <div className="space-y-5">
      {adimlar.map((a) => (
        <div key={a.kimlik} className="space-y-5">
          {adim === undefined && a.baslik && (
            <div>
              <h2 className="font-display text-xl font-bold tracking-[-0.01em]">{a.baslik}</h2>
              {a.aciklama && <p className="mt-1 text-sm text-ink-2">{a.aciklama}</p>}
            </div>
          )}

          {(a.gruplar ?? []).map((grup) => {
            if (!grupGorunur(grup, cevaplar)) return null;

            return (
              <Grup
                key={grup.kimlik}
                grup={grup}
                formKolon={formKolon}
                cevaplar={cevaplar}
                degistir={degistir}
                hatalar={hatalar}
                pasif={pasif}
                yukle={yukle}
                numaralandir={tanim.ayarlar?.numaralandir ?? false}
                siraBaslangici={() => ++sira}
              />
            );
          })}
        </div>
      ))}
    </div>
  );
}

/**
 * 12'lik ızgarada kaç birim kaplayacağı.
 *
 * Sınıflar STATİK olmak zorunda: Tailwind kaynak taramasıyla çalışıyor ve
 * `md:col-span-${n}` gibi kurulan bir ad üretilen CSS'te bulunmaz.
 */
const KOLON: Record<number, string> = {
  1: 'md:col-span-1', 2: 'md:col-span-2', 3: 'md:col-span-3', 4: 'md:col-span-4',
  5: 'md:col-span-5', 6: 'md:col-span-6', 7: 'md:col-span-7', 8: 'md:col-span-8',
  9: 'md:col-span-9', 10: 'md:col-span-10', 11: 'md:col-span-11', 12: 'md:col-span-12',
};

/**
 * Alanın genişliği — grubun kolon sayısına GÖRE KIRPILIR.
 *
 * Tasarımcı 2 kolonluk bir grupta alana 12 verdiyse alan tam satır kaplar;
 * ama grup 3 kolona düşürülünce eski genişlikler anlamsız kalabiliyor.
 * Kırpma, tanımı bozmadan çizimi tutarlı tutuyor.
 */
function genislik(deger: number | null | undefined, kolon: number): number {
  const varsayilan = Math.max(1, Math.round(12 / Math.max(1, kolon)));
  const g = deger && deger > 0 ? deger : varsayilan;
  return Math.min(12, Math.max(1, g));
}

function Grup({
  grup, formKolon, cevaplar, degistir, hatalar, pasif, yukle, numaralandir, siraBaslangici,
}: {
  grup: FormGroup;
  formKolon: number;
  cevaplar: Answers;
  degistir: (kimlik: string, cevap: { deger?: unknown; metin?: string }) => void;
  hatalar?: Record<string, string>;
  pasif?: boolean;
  yukle?: (alanKimligi: string, dosya: File) => Promise<{ dosyaId: number; ad: string }>;
  numaralandir: boolean;
  siraBaslangici: () => number;
}) {
  const kolon = grup.kolonSayisi ?? formKolon;

  return (
    <section className="space-y-3">
      {(grup.baslik || grup.aciklama) && (
        <div>
          {grup.baslik && (
            <h3 className="font-display text-base font-bold tracking-[-0.01em]">{grup.baslik}</h3>
          )}
          {grup.aciklama && <p className="mt-0.5 text-sm text-ink-2">{grup.aciklama}</p>}
        </div>
      )}

      {/*
        Izgara MASAÜSTÜNDE DAİMA 12 KOLON, mobilde tek.
        `minmax(0,1fr)` şart: `1fr` aslında `minmax(auto,1fr)` ve uzun bir
        seçenek metni ızgarayı görünür alandan taşırıyor.
      */}
      <div className="grid grid-cols-1 gap-3 md:grid-cols-[repeat(12,minmax(0,1fr))]">
        {(grup.alanlar ?? []).map((alan) => {
          if (!alanGorunur(alan, grup, cevaplar)) return null;

          const soruMu = !isBlock(alan.tip);
          const no = soruMu && numaralandir ? siraBaslangici() : null;

          return (
            <div key={alan.kimlik} className={cn('min-w-0', KOLON[genislik(alan.genislik, kolon)])}>
              <FormFieldInput
                alan={alan}
                cevap={cevaplar[alan.kimlik ?? '']}
                degistir={(c) => degistir(alan.kimlik ?? '', c)}
                hata={hatalar?.[alan.kimlik ?? '']}
                pasif={pasif}
                no={no}
                yukle={yukle}
              />
            </div>
          );
        })}
      </div>

    </section>
  );
}
