import { FileUp, Star } from 'lucide-react';
import { useState } from 'react';
import { FieldWrapper, Input, Secim, Textarea } from '../components/Field';
import { Switch } from '../components/Switch';
import { DatePicker } from '../components/DatePicker';
import { cn } from '../components/utils';
import type { FormField } from '../data/types';
import { FIELD_TYPE } from './fieldTypes';
import { deger, sarmala, type Answer } from './formEngine';

/**
 * TEK BİR FORM ALANININ GİRDİSİ.
 *
 * <p>
 * Hem oynatıcı (vatandaş) hem tasarımcı önizlemesi bunu kullanıyor. İki
 * ayrı çizim olsaydı tasarımcıda gördüğün ile vatandaşın gördüğü zamanla
 * ayrışırdı — anket aracında en pahalı hata sınıfı budur.
 * </p>
 *
 * <p>
 * <b>Ölçüler ortak bileşenlerden.</b> Kendi <c>&lt;input&gt;</c>'unu kuran
 * bir alan, mobilde 40px kalıp yanındaki 50px'lik alanlarla hizasız
 * görünürdü — bu depoda ölçülmüş bir hata.
 * </p>
 */
export function FormFieldInput({
  alan,
  cevap,
  degistir,
  hata,
  pasif,
  no,
  yukle,
}: {
  alan: FormField;
  cevap: Answer | undefined;
  degistir: (c: Answer) => void;
  hata?: string;
  pasif?: boolean;
  /** Soru numarası; form "numaralandır" ayarı açıkken dolu. */
  no?: number | null;
  /**
   * Dosya yükleyici — yalnızca vatandaş sayfasında verilir.
   *
   * Tasarımcı önizlemesinde YOK: orada dosya yüklemek, kurulmakta olan
   * bir forma gerçek bir taslak yanıt açmak demekti.
   */
  yukle?: (alanKimligi: string, dosya: File) => Promise<{ dosyaId: number; ad: string }>;
}) {
  const kimlik = `alan-${alan.kimlik}`;
  const d = deger(cevap);
  const yaz = (v: unknown, metin?: string) => degistir(sarmala(v, metin));

  const sar = (icerik: React.ReactNode) => (
    <FieldWrapper
      /*
        NUMARA ETİKETİN İÇİNDE, ayrı bir sütunda değil.

        Ayrı sütundayken numara girdinin dikey ortasına düşüyor ve
        etiketle hizalanmıyordu — üstelik hizayı tutturmak için `mt-[30px]`
        gibi bir sihirli sayı gerekiyordu, o da alan tipine göre değişen
        yükseklikte tutmuyordu.
      */
      etiket={no != null ? `${no}. ${alan.etiket ?? ''}` : (alan.etiket ?? '')}
      id={kimlik}
      zorunlu={alan.zorunlu ?? false}
      hata={hata}
      ipucu={hata ? undefined : (alan.aciklama ?? undefined)}
    >
      {icerik}
    </FieldWrapper>
  );

  switch (alan.tip) {
    // ── içerik blokları: yanıt üretmez ──
    case FIELD_TYPE.baslik:
      return (
        <h3 className="mt-2 font-display text-lg font-bold tracking-[-0.01em]">
          {alan.etiket}
        </h3>
      );

    case FIELD_TYPE.aciklama:
      return (
        <p className="text-sm leading-[1.6] text-ink-2 metin-guzel">{alan.etiket}</p>
      );

    case FIELD_TYPE.ayirici:
      return <hr className="my-1 border-line" />;

    // ── metin ──
    case FIELD_TYPE.uzunMetin:
      return sar(
        <Textarea
          id={kimlik}
          rows={alan.ayarlar?.satir ?? 4}
          disabled={pasif}
          value={String(d ?? '')}
          placeholder={alan.yerTutucu ?? ''}
          onChange={(e) => yaz(e.target.value)}
        />,
      );

    case FIELD_TYPE.eposta:
    case FIELD_TYPE.telefon:
    case FIELD_TYPE.tcKimlik:
    case FIELD_TYPE.url:
    case FIELD_TYPE.kisaMetin:
      return sar(
        <Input
          id={kimlik}
          disabled={pasif}
          value={String(d ?? '')}
          placeholder={alan.yerTutucu ?? ''}
          type={alan.tip === FIELD_TYPE.eposta ? 'email' : 'text'}
          inputMode={
            alan.tip === FIELD_TYPE.telefon || alan.tip === FIELD_TYPE.tcKimlik
              ? 'numeric' : undefined
          }
          autoComplete={
            alan.tip === FIELD_TYPE.eposta ? 'email'
              : alan.tip === FIELD_TYPE.telefon ? 'tel' : undefined
          }
          onChange={(e) => yaz(e.target.value)}
          hatali={!!hata}
        />,
      );

    // ── sayı ve tarih ──
    case FIELD_TYPE.sayi:
      return sar(
        <Input
          id={kimlik}
          type="number"
          inputMode="decimal"
          disabled={pasif}
          value={String(d ?? '')}
          placeholder={alan.yerTutucu ?? ''}
          min={alan.dogrulama?.enAzDeger ?? undefined}
          max={alan.dogrulama?.enCokDeger ?? undefined}
          onChange={(e) => yaz(e.target.value === '' ? '' : Number(e.target.value))}
          hatali={!!hata}
        />,
      );

    case FIELD_TYPE.tarih:
      return sar(
        <DatePicker id={kimlik} deger={String(d ?? '')} degistir={(v) => yaz(v)} />,
      );

    case FIELD_TYPE.saat:
      return sar(
        <Input
          id={kimlik} type="time" disabled={pasif}
          value={String(d ?? '')} onChange={(e) => yaz(e.target.value)} hatali={!!hata}
        />,
      );

    // ── seçim ──
    case FIELD_TYPE.evetHayir:
      return sar(
        <div className="flex h-field items-center md:h-ctrl">
          <Switch
            isaretli={d === true || d === 'true'}
            degistir={(v) => yaz(v)}
            etiket={d === true || d === 'true' ? 'Evet' : 'Hayır'}
            pasif={pasif}
          />
        </div>,
      );

    case FIELD_TYPE.acilirListe:
      return sar(
        <Secim
          id={kimlik}
          disabled={pasif}
          value={String(d ?? '')}
          onChange={(e) => yaz(e.target.value)}
        >
          <option value="">Seçin</option>
          {(alan.secenekler ?? []).map((s) => (
            <option key={s.kimlik} value={s.kimlik ?? ''}>{s.etiket}</option>
          ))}
        </Secim>,
      );

    case FIELD_TYPE.tekSecim:
      return sar(<TekSecim alan={alan} cevap={cevap} yaz={yaz} pasif={pasif} />);

    case FIELD_TYPE.cokSecim:
      return sar(<CokSecim alan={alan} cevap={cevap} yaz={yaz} pasif={pasif} />);

    // ── ölçek ──
    case FIELD_TYPE.olcek:
    case FIELD_TYPE.nps:
      return sar(<Olcek alan={alan} d={d} yaz={yaz} pasif={pasif} />);

    case FIELD_TYPE.yildiz:
      return sar(<Yildiz alan={alan} d={d} yaz={yaz} pasif={pasif} />);

    // ── matris ──
    case FIELD_TYPE.matrisTekSecim:
    case FIELD_TYPE.matrisCokSecim:
      return sar(<Matris alan={alan} d={d} yaz={yaz} pasif={pasif} />);

    // ── dosya ──
    case FIELD_TYPE.dosya:
      return sar(
        <DosyaAlani
          kimlik={kimlik}
          alan={alan}
          cevap={cevap}
          degistir={degistir}
          pasif={pasif}
          yukle={yukle}
        />,
      );

    default:
      return sar(
        <Input
          id={kimlik} disabled={pasif} value={String(d ?? '')}
          onChange={(e) => yaz(e.target.value)} hatali={!!hata}
        />,
      );
  }
}

/* ─────────────────────────────────────────────────────── seçim bileşenleri */

/**
 * Radyo grubu.
 *
 * <p>
 * Yerleşik <c>&lt;input type="radio"&gt;</c> görünümü tarayıcıya göre
 * değişiyor ve dokunma hedefi 16px kalıyor. Etiketin tamamı tıklanabilir,
 * satır yüksekliği 44px'in altına inmiyor.
 * </p>
 */
function TekSecim({ alan, cevap, yaz, pasif }: {
  alan: FormField; cevap: Answer | undefined;
  yaz: (v: unknown, m?: string) => void; pasif?: boolean;
}) {
  const secili = String(deger(cevap) ?? '');

  return (
    <div className="space-y-1.5">
      {(alan.secenekler ?? []).map((s) => {
        const bu = secili === s.kimlik;

        return (
          <div key={s.kimlik}>
            <label
              className={cn(
                'flex min-h-11 cursor-pointer items-center gap-2.5 rounded-md border px-3 py-2 transition-colors',
                bu ? 'border-brand bg-brand-soft' : 'border-line bg-surface-2 hover:border-line-2',
                pasif && 'cursor-default opacity-60',
              )}
            >
              <input
                type="radio"
                name={`a-${alan.kimlik}`}
                className="sr-only"
                disabled={pasif}
                checked={bu}
                onChange={() => yaz(s.kimlik, bu ? cevap?.metin : undefined)}
              />
              <span
                aria-hidden
                className={cn(
                  'grid size-[18px] shrink-0 place-items-center rounded-full border-2',
                  bu ? 'border-brand' : 'border-line-2',
                )}
              >
                {bu && <span className="size-2.5 rounded-full bg-brand" />}
              </span>
              <span className="min-w-0 flex-1 text-sm">{s.etiket}</span>
            </label>

            {/* "Diğer" seçiliyse serbest metin kutusu ALTINDA açılır. */}
            {bu && s.digerMi && (
              <Input
                className="mt-1.5"
                placeholder="Lütfen belirtin"
                value={cevap?.metin ?? ''}
                disabled={pasif}
                onChange={(e) => yaz(s.kimlik, e.target.value)}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}

function CokSecim({ alan, cevap, yaz, pasif }: {
  alan: FormField; cevap: Answer | undefined;
  yaz: (v: unknown, m?: string) => void; pasif?: boolean;
}) {
  const secili = Array.isArray(deger(cevap)) ? (deger(cevap) as string[]) : [];

  const cevir = (k: string) =>
    secili.includes(k) ? secili.filter((x) => x !== k) : [...secili, k];

  return (
    <div className="space-y-1.5">
      {(alan.secenekler ?? []).map((s) => {
        const bu = secili.includes(s.kimlik ?? '');

        return (
          <div key={s.kimlik}>
            <label
              className={cn(
                'flex min-h-11 cursor-pointer items-center gap-2.5 rounded-md border px-3 py-2 transition-colors',
                bu ? 'border-brand bg-brand-soft' : 'border-line bg-surface-2 hover:border-line-2',
                pasif && 'cursor-default opacity-60',
              )}
            >
              <input
                type="checkbox" className="sr-only" disabled={pasif} checked={bu}
                onChange={() => yaz(cevir(s.kimlik ?? ''), cevap?.metin)}
              />
              <span
                aria-hidden
                className={cn(
                  'grid size-[18px] shrink-0 place-items-center rounded-sm border-2',
                  bu ? 'border-brand bg-brand text-on-brand' : 'border-line-2',
                )}
              >
                {bu && (
                  <svg viewBox="0 0 12 12" className="size-3" fill="none" stroke="currentColor" strokeWidth="2.5">
                    <path d="M2 6.5 4.5 9 10 3.5" strokeLinecap="round" strokeLinejoin="round" />
                  </svg>
                )}
              </span>
              <span className="min-w-0 flex-1 text-sm">{s.etiket}</span>
            </label>

            {bu && s.digerMi && (
              <Input
                className="mt-1.5" placeholder="Lütfen belirtin"
                value={cevap?.metin ?? ''} disabled={pasif}
                onChange={(e) => yaz(secili, e.target.value)}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}

/* ─────────────────────────────────────────────────────── ölçek */

function Olcek({ alan, d, yaz, pasif }: {
  alan: FormField; d: unknown; yaz: (v: unknown) => void; pasif?: boolean;
}) {
  const az = alan.ayarlar?.enAz ?? (alan.tip === FIELD_TYPE.nps ? 0 : 1);
  const cok = alan.ayarlar?.enCok ?? (alan.tip === FIELD_TYPE.nps ? 10 : 5);
  const secili = typeof d === 'number' ? d : Number(d);

  const adimlar = Array.from({ length: cok - az + 1 }, (_, i) => az + i);

  return (
    <div className="space-y-1.5">
      {/*
        Butonlar SARILIR (`flex-wrap`): 0–10 arası bir ölçek 390px'te tek
        satıra sığmıyor ve yatay kaydırma, bütün seçenekleri görmeyi
        kullanıcının keşfetmesine bırakırdı.
      */}
      <div className="flex flex-wrap gap-1.5">
        {adimlar.map((n) => (
          <button
            key={n}
            type="button"
            disabled={pasif}
            onClick={() => yaz(n)}
            aria-pressed={secili === n}
            className={cn(
              'h-11 min-w-11 flex-1 rounded-md border text-sm font-semibold tabular-nums transition-colors',
              secili === n
                ? 'border-brand bg-brand text-on-brand'
                : 'border-line bg-surface-2 hover:border-brand-2',
            )}
          >
            {n}
          </button>
        ))}
      </div>

      {(alan.ayarlar?.altEtiket || alan.ayarlar?.ustEtiket) && (
        <div className="flex justify-between text-2xs text-ink-3">
          <span>{alan.ayarlar?.altEtiket}</span>
          <span>{alan.ayarlar?.ustEtiket}</span>
        </div>
      )}
    </div>
  );
}

function Yildiz({ alan, d, yaz, pasif }: {
  alan: FormField; d: unknown; yaz: (v: unknown) => void; pasif?: boolean;
}) {
  const cok = alan.ayarlar?.enCok ?? 5;
  const secili = typeof d === 'number' ? d : Number(d) || 0;

  return (
    <div className="flex h-field items-center gap-1 md:h-ctrl">
      {Array.from({ length: cok }, (_, i) => i + 1).map((n) => (
        <button
          key={n}
          type="button"
          disabled={pasif}
          onClick={() => yaz(n === secili ? 0 : n)}
          aria-label={`${n} yıldız`}
          className="grid size-11 place-items-center rounded-md transition-colors hover:bg-surface-2"
        >
          <Star
            size={24}
            className={n <= secili ? 'text-gold' : 'text-line-2'}
            fill={n <= secili ? 'currentColor' : 'none'}
          />
        </button>
      ))}
    </div>
  );
}

/* ─────────────────────────────────────────────────────── matris */

/**
 * MATRİS — masaüstünde tablo, MOBİLDE satır satır kart.
 *
 * <p>
 * 390px'te altı sütunlu bir tablo okunamıyor: her sütun 55px'e düşüyor ve
 * başlıklar dikey yazıya dönüyor. Yatay kaydırma da çözüm değil — kullanıcı
 * satır etiketini kaybediyor. Mobilde her satır kendi başlığıyla bir kart
 * ve seçenekler sarılan düğmeler.
 * </p>
 */
function Matris({ alan, d, yaz, pasif }: {
  alan: FormField; d: unknown; yaz: (v: unknown) => void; pasif?: boolean;
}) {
  const coklu = alan.tip === FIELD_TYPE.matrisCokSecim;
  const deger = (d ?? {}) as Record<string, unknown>;
  const satirlar = alan.satirlar ?? [];
  const sutunlar = alan.sutunlar ?? [];

  const isaretli = (s: string, c: string) => {
    const v = deger[s];
    return coklu ? Array.isArray(v) && v.includes(c) : v === c;
  };

  const cevir = (s: string, c: string) => {
    if (!coklu) return yaz({ ...deger, [s]: deger[s] === c ? undefined : c });

    const mevcut = Array.isArray(deger[s]) ? (deger[s] as string[]) : [];
    const yeni = mevcut.includes(c) ? mevcut.filter((x) => x !== c) : [...mevcut, c];
    return yaz({ ...deger, [s]: yeni });
  };

  return (
    <>
      {/* ── masaüstü: tablo ── */}
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr>
              <th className="w-1/3 border-b border-line px-2 py-2 text-left font-semibold text-ink-2" />
              {sutunlar.map((c) => (
                <th key={c.kimlik} className="border-b border-line px-2 py-2 text-center text-xs font-semibold text-ink-2">
                  {c.etiket}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {satirlar.map((s) => (
              <tr key={s.kimlik} className="border-b border-line last:border-0">
                <td className="px-2 py-2 text-sm">{s.etiket}</td>
                {sutunlar.map((c) => (
                  <td key={c.kimlik} className="px-2 py-2 text-center">
                    <input
                      type={coklu ? 'checkbox' : 'radio'}
                      name={`m-${alan.kimlik}-${s.kimlik}`}
                      disabled={pasif}
                      checked={isaretli(s.kimlik ?? '', c.kimlik ?? '')}
                      onChange={() => cevir(s.kimlik ?? '', c.kimlik ?? '')}
                      aria-label={`${s.etiket} — ${c.etiket}`}
                      className="size-4 accent-[var(--brand)]"
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* ── mobil: satır satır kart ── */}
      <div className="space-y-2.5 md:hidden">
        {satirlar.map((s) => (
          <div key={s.kimlik} className="rounded-md border border-line bg-surface-2 p-2.5">
            <p className="mb-1.5 text-sm font-semibold">{s.etiket}</p>
            <div className="flex flex-wrap gap-1.5">
              {sutunlar.map((c) => {
                const bu = isaretli(s.kimlik ?? '', c.kimlik ?? '');

                return (
                  <button
                    key={c.kimlik}
                    type="button"
                    disabled={pasif}
                    onClick={() => cevir(s.kimlik ?? '', c.kimlik ?? '')}
                    aria-pressed={bu}
                    className={cn(
                      'min-h-9 rounded-sm border px-2.5 text-xs font-medium transition-colors',
                      bu ? 'border-brand bg-brand text-on-brand' : 'border-line bg-surface',
                    )}
                  >
                    {c.etiket}
                  </button>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </>
  );
}


/* ─────────────────────────────────────────────────────── dosya */

/**
 * DOSYA ALANI — yükleme GÖNDERİMDEN ÖNCE.
 *
 * <p>
 * Dosya seçilir seçilmez sunucuya gidiyor ve geriye bir kimlik dönüyor;
 * cevapta o kimlik duruyor. Gönderimle birlikte yollamak, 12 MB'lık bir
 * gövdenin doğrulamada düşmesi hâlinde her şeyi yeniden yükletirdi.
 * </p>
 *
 * <p>
 * <b>Yükleyici yoksa alan salt okunur çizilir</b> (tasarımcı önizlemesi):
 * kurulmakta olan bir forma gerçek bir taslak yanıt açmak istemiyoruz.
 * </p>
 */
function DosyaAlani({
  kimlik, alan, cevap, degistir, pasif, yukle,
}: {
  kimlik: string;
  alan: FormField;
  cevap: Answer | undefined;
  degistir: (c: Answer) => void;
  pasif?: boolean;
  yukle?: (alanKimligi: string, dosya: File) => Promise<{ dosyaId: number; ad: string }>;
}) {
  const [yukleniyor, setYukleniyor] = useState(false);
  const [hata, setHata] = useState<string | null>(null);

  const mevcut = Array.isArray(deger(cevap)) ? (deger(cevap) as unknown[]) : [];
  const adlar = (cevap?.metin ?? '').split('|').filter(Boolean);

  return (
    <div className="space-y-1.5">
      <label
        className={cn(
          'flex min-h-field cursor-pointer items-center gap-3 rounded-md border border-dashed',
          'border-line bg-surface-2 px-3 py-2.5 transition-colors hover:border-brand-2',
          (pasif || !yukle) && 'cursor-default opacity-70',
        )}
      >
        <span className="grid size-9 shrink-0 place-items-center rounded-md bg-brand-soft text-brand" aria-hidden>
          <FileUp size={17} />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block text-sm font-semibold">
            {yukleniyor ? 'Yükleniyor…' : adlar.length > 0 ? `${adlar.length} dosya seçildi` : 'Dosya seç'}
          </span>
          <span className="block truncate text-xs text-ink-3">
            {adlar.length > 0 ? adlar.join(', ')
              : (alan.dogrulama?.dosyaUzantilari?.join(', ') ?? 'PDF, resim ya da belge')}
          </span>
        </span>
        <input
          id={kimlik}
          type="file"
          className="sr-only"
          disabled={pasif || !yukle || yukleniyor}
          accept={alan.dogrulama?.dosyaUzantilari?.join(',') ?? undefined}
          onChange={async (e) => {
            const d = e.target.files?.[0];
            if (!d || !yukle) return;

            setYukleniyor(true);
            setHata(null);

            try {
              const s = await yukle(alan.kimlik ?? '', d);
              degistir({
                deger: [...mevcut, s.dosyaId],
                metin: [...adlar, s.ad].join('|'),
              });
            } catch (h) {
              setHata((h as Error).message);
            } finally {
              setYukleniyor(false);
              e.target.value = '';
            }
          }}
        />
      </label>

      {hata && <p className="text-xs text-(--st-no)">{hata}</p>}
    </div>
  );
}
