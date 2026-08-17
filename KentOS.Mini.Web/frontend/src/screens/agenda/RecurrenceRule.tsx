import { Switch } from '../../components/Switch';
import { FieldWrapper, Input, Secim } from '../../components/Field';
import { cn } from '../../components/utils';

/** Haftanın günleri — RRULE `BYDAY` kısaltmalarıyla. */
const GUNLER = [
  { kod: 'MO', kisa: 'Pzt' },
  { kod: 'TU', kisa: 'Sal' },
  { kod: 'WE', kisa: 'Çar' },
  { kod: 'TH', kisa: 'Per' },
  { kod: 'FR', kisa: 'Cum' },
  { kod: 'SA', kisa: 'Cmt' },
  { kod: 'SU', kisa: 'Paz' },
] as const;

/**
 * Aylık tekrarın deseni — mobildeki <c>AylikTur</c> ile birebir.
 *
 * `ayinGunu`      → BYMONTHDAY=15  (her ayın 15'i)
 * `haftaninGunu`  → BYDAY=2TH      (ayın 2. perşembesi)
 * `sonGun`        → BYMONTHDAY=-1  (ayın son günü)
 */
export type MonthlyMode = 'ayinGunu' | 'haftaninGunu' | 'sonGun';

export type RecurrenceState = {
  acik: boolean;
  siklik: 'DAILY' | 'WEEKLY' | 'MONTHLY' | 'YEARLY';
  aralik: number;
  gunler: string[];
  bitisTuru: 'yok' | 'tarih' | 'sayi';
  bitisTarihi: string;
  adet: number;

  /** Yalnızca MONTHLY'de anlamlı. */
  aylikTur: MonthlyMode;
  /** `ayinGunu` için ayın günü (1–31). */
  ayinGunu: number;
  /** `haftaninGunu` için kaçıncı hafta (1–4) ve gün kodu. */
  haftaSirasi: number;
  haftaGunu: string;

  /**
   * Sunucudan gelen HAM kural.
   *
   * <p>
   * Kullanıcı tekrar bölümüne dokunmadıysa kural <b>aynen</b> geri gönderilir.
   * Bu olmadan, formun anlamadığı bir parça (örneğin mobilin ürettiği
   * <c>BYMONTH</c>) sessizce düşüyor ve kaydet'e basmak kuralı değiştirmiş
   * sayılıp seriyi bölüyordu.
   * </p>
   */
  ham: string | null;
  /** Kullanıcı tekrar ayarlarına dokundu mu? */
  dokunuldu: boolean;
};

export const EMPTY_RECURRENCE: RecurrenceState = {
  acik: false,
  siklik: 'WEEKLY',
  aralik: 1,
  gunler: [],
  aylikTur: 'ayinGunu',
  ayinGunu: 1,
  haftaSirasi: 1,
  haftaGunu: 'MO',
  ham: null,
  dokunuldu: false,
  bitisTuru: 'yok',
  bitisTarihi: '',
  adet: 10,
};

/**
 * Kullanıcı seçimlerinden RRULE üretir.
 *
 * <p>
 * <b>Kritik:</b> `BYDAY` yalnızca kullanıcı AÇIKÇA gün seçtiyse yazılır.
 * Eski istemciler kuralı formun başlangıç tarihinden türetiyordu; bir
 * tekrarı çarşambadan perşembeye taşımak `BYDAY=TH` göndermeye yol açıyor,
 * sunucu bunu "kural değişti" diye okuyup seriyi bölüyordu — kullanıcı
 * açısından etkinlik kaybolmuş görünüyordu. Tarihten kural türetmek YASAK.
 * </p>
 */
export function buildRrule(t: RecurrenceState): string | null {
  if (!t.acik) return null;

  // Kullanıcı dokunmadıysa geldiği gibi geri gönder: formun anlamadığı
  // parçalar (mobilin ürettiği BYMONTH gibi) korunur ve kural "değişmiş"
  // sayılıp seri bölünmez.
  if (!t.dokunuldu && t.ham) return t.ham;

  const parcalar = [`FREQ=${t.siklik}`];

  if (t.aralik > 1) parcalar.push(`INTERVAL=${t.aralik}`);

  if (t.siklik === 'WEEKLY' && t.gunler.length > 0) {
    parcalar.push(`BYDAY=${t.gunler.join(',')}`);
  }

  // Aylık desenler — mobildeki `AylikTur` ile birebir aynı çıktı.
  if (t.siklik === 'MONTHLY') {
    if (t.aylikTur === 'sonGun') {
      parcalar.push('BYMONTHDAY=-1');
    } else if (t.aylikTur === 'haftaninGunu') {
      parcalar.push(`BYDAY=${t.haftaSirasi}${t.haftaGunu}`);
    } else {
      parcalar.push(`BYMONTHDAY=${t.ayinGunu}`);
    }
  }

  if (t.bitisTuru === 'sayi' && t.adet > 0) {
    parcalar.push(`COUNT=${t.adet}`);
  } else if (t.bitisTuru === 'tarih' && t.bitisTarihi) {
    // UNTIL saat dilimsiz yazılır; sunucudaki damgalar da kayan yerel saat.
    parcalar.push(`UNTIL=${t.bitisTarihi.replace(/-/g, '')}T235959`);
  }

  return parcalar.join(';');
}

/** Var olan bir RRULE'u forma geri çevirir (düzenleme ekranı için). */
export function parseRrule(rrule: string | null | undefined): RecurrenceState {
  if (!rrule) return EMPTY_RECURRENCE;

  const harita = new Map(
    rrule.split(';').map((p) => {
      const [a, d] = p.split('=');
      return [a.toUpperCase(), d ?? ''];
    }),
  );

  const siklik = (harita.get('FREQ') ?? 'WEEKLY') as RecurrenceState['siklik'];
  const byday = (harita.get('BYDAY') ?? '').split(',').filter(Boolean);
  const bymonthday = harita.get('BYMONTHDAY') ?? '';
  const sayi = Number(harita.get('COUNT') ?? 0);
  const until = harita.get('UNTIL') ?? '';

  // Aylık desen: `BYDAY=2TH` (ayın 2. perşembesi) sıra ekli, haftalıktaki
  // düz `BYDAY=TH` ile karıştırılmamalı.
  const siraliGun = /^(-?\d)([A-Z]{2})$/.exec(byday[0] ?? '');

  let aylikTur: MonthlyMode = 'ayinGunu';
  if (siklik === 'MONTHLY') {
    if (bymonthday === '-1') aylikTur = 'sonGun';
    else if (siraliGun) aylikTur = 'haftaninGunu';
  }

  return {
    acik: true,
    siklik,
    aralik: Number(harita.get('INTERVAL') ?? 1) || 1,
    // Sıra ekli BYDAY haftalık gün seçimi DEĞİLDİR; oraya taşınmamalı.
    gunler: siraliGun ? [] : byday,
    aylikTur,
    ayinGunu: Number(bymonthday) > 0 ? Number(bymonthday) : 1,
    haftaSirasi: siraliGun ? Math.abs(Number(siraliGun[1])) : 1,
    haftaGunu: siraliGun ? siraliGun[2] : 'MO',
    ham: rrule,
    dokunuldu: false,
    bitisTuru: sayi > 0 ? 'sayi' : until ? 'tarih' : 'yok',
    // `20261231T235959` → `2026-12-31`
    bitisTarihi: until ? `${until.slice(0, 4)}-${until.slice(4, 6)}-${until.slice(6, 8)}` : '',
    adet: sayi > 0 ? sayi : 10,
  };
}

/** İnsan diliyle özet — kullanıcı ne kurduğunu görsün. */
export function recurrenceSummary(t: RecurrenceState): string {
  if (!t.acik) return 'Tekrar etmiyor';

  const sik = {
    DAILY: t.aralik > 1 ? `${t.aralik} günde bir` : 'Her gün',
    WEEKLY: t.aralik > 1 ? `${t.aralik} haftada bir` : 'Her hafta',
    MONTHLY: t.aralik > 1 ? `${t.aralik} ayda bir` : 'Her ay',
    YEARLY: t.aralik > 1 ? `${t.aralik} yılda bir` : 'Her yıl',
  }[t.siklik];

  const gun =
    t.siklik === 'WEEKLY' && t.gunler.length > 0
      ? ' · ' + t.gunler.map((k) => GUNLER.find((g) => g.kod === k)?.kisa ?? k).join(', ')
      : '';

  const bitis =
    t.bitisTuru === 'sayi'
      ? ` · ${t.adet} kez`
      : t.bitisTuru === 'tarih' && t.bitisTarihi
        ? ` · ${t.bitisTarihi} tarihine kadar`
        : '';

  return sik + gun + bitis;
}

/**
 * Tekrar kuralı düzenleyicisi.
 *
 * <p>
 * Sunucudaki ayrıştırıcı `FREQ, INTERVAL, COUNT, UNTIL, BYDAY, BYMONTHDAY,
 * BYMONTH, WKST` alt kümesini destekliyor; bu form yalnızca güvenle
 * üretilebilen bölümü sunuyor. Desteklenmeyen bir alan gönderilirse sunucu
 * 400 döner.
 * </p>
 */
export function RecurrenceRule({
  deger,
  degistir,
  kilitli,
}: {
  deger: RecurrenceState;
  degistir: (d: RecurrenceState) => void;
  /** Var olan bir seride kural değişikliği kapsam sorusu gerektirir. */
  kilitli?: boolean;
}) {
  /**
   * Her değişiklik `dokunuldu` bayrağını kaldırır.
   *
   * Bu olmadan `rruleUret` ham kuralı geri gönderir ve kullanıcının
   * düzenlemesi sessizce yok sayılırdı — kayıpsız gidiş-dönüşün bedeli.
   */
  const guncelle = (yama: Partial<RecurrenceState>) =>
    degistir({ ...deger, ...yama, dokunuldu: true });

  return (
    <div className="rounded-card border border-border bg-surface-2 p-3.5">
      <div className="flex items-center gap-2.5">
        <Switch
          isaretli={deger.acik}
          degistir={(a) => guncelle({ acik: a })}
          pasif={kilitli}
          etiket="Tekrar eden etkinlik"
        />
        {deger.acik && (
          <span className="ml-auto text-xs text-text-3">{recurrenceSummary(deger)}</span>
        )}
      </div>

      {deger.acik && (
        <div className="mt-3.5 space-y-3.5 border-t border-border pt-3.5">
          <div className="grid gap-3 sm:grid-cols-2">
            <FieldWrapper etiket="Sıklık" id="t-siklik">
              <Secim
                id="t-siklik"
                value={deger.siklik}
                onChange={(e) => guncelle({ siklik: e.target.value as RecurrenceState['siklik'] })}
              >
                <option value="DAILY">Günlük</option>
                <option value="WEEKLY">Haftalık</option>
                <option value="MONTHLY">Aylık</option>
                <option value="YEARLY">Yıllık</option>
              </Secim>
            </FieldWrapper>

            <FieldWrapper etiket="Aralık" id="t-aralik" ipucu="Kaç birimde bir tekrarlansın.">
              <Input
                id="t-aralik"
                type="number"
                min={1}
                max={52}
                value={deger.aralik}
                onChange={(e) => guncelle({ aralik: Math.max(1, Number(e.target.value) || 1) })}
              />
            </FieldWrapper>
          </div>

          {/*
            Aylık desen — mobildeki `AylikTur` ile birebir. Bu seçenekler
            olmadan web, mobilde kurulmuş "ayın 2. perşembesi" gibi bir kuralı
            gösteremiyor ve kaydettiğinde deseni düşürüyordu.
          */}
          {deger.siklik === 'MONTHLY' && (
            <div className="space-y-2.5">
              <p className="text-xs font-semibold uppercase tracking-wider text-text-3">
                Aylık desen
              </p>

              {([
                { d: 'ayinGunu' as const, e: 'Ayın belirli bir günü' },
                { d: 'haftaninGunu' as const, e: 'Ayın kaçıncı haftasındaki gün' },
                { d: 'sonGun' as const, e: 'Ayın son günü' },
              ]).map((se) => (
                <label
                  key={se.d}
                  className={cn(
                    'flex cursor-pointer items-center gap-2.5 rounded-control border p-2.5 transition-colors',
                    deger.aylikTur === se.d
                      ? 'border-brand bg-brand-tint'
                      : 'border-border hover:bg-surface',
                  )}
                >
                  <input
                    type="radio"
                    name="aylik-tur"
                    checked={deger.aylikTur === se.d}
                    onChange={() => guncelle({ aylikTur: se.d })}
                    className="h-[16px] w-[16px] accent-(--brand)"
                  />
                  <span className="flex-1 text-sm">{se.e}</span>

                  {se.d === 'ayinGunu' && deger.aylikTur === 'ayinGunu' && (
                    <input
                      type="number"
                      min={1}
                      max={31}
                      aria-label="Ayın günü"
                      value={deger.ayinGunu}
                      onChange={(e) =>
                        guncelle({
                          ayinGunu: Math.min(31, Math.max(1, Number(e.target.value) || 1)),
                        })
                      }
                      className="h-8 w-16 rounded-control border border-border bg-surface px-2 text-sm tabular-nums"
                    />
                  )}

                  {se.d === 'haftaninGunu' && deger.aylikTur === 'haftaninGunu' && (
                    <span className="flex gap-1.5">
                      <select
                        aria-label="Kaçıncı hafta"
                        value={deger.haftaSirasi}
                        onChange={(e) => guncelle({ haftaSirasi: Number(e.target.value) })}
                        className="h-8 rounded-control border border-border bg-surface px-1.5 text-sm"
                      >
                        {[1, 2, 3, 4].map((n) => (
                          <option key={n} value={n}>
                            {n}.
                          </option>
                        ))}
                      </select>
                      <select
                        aria-label="Haftanın günü"
                        value={deger.haftaGunu}
                        onChange={(e) => guncelle({ haftaGunu: e.target.value })}
                        className="h-8 rounded-control border border-border bg-surface px-1.5 text-sm"
                      >
                        {GUNLER.map((g) => (
                          <option key={g.kod} value={g.kod}>
                            {g.kisa}
                          </option>
                        ))}
                      </select>
                    </span>
                  )}
                </label>
              ))}
            </div>
          )}

          {deger.siklik === 'WEEKLY' && (
            <div>
              <p className="mb-1.5 text-xs font-semibold uppercase tracking-wider text-text-3">
                Günler
              </p>
              <div className="flex flex-wrap gap-1.5">
                {GUNLER.map((g) => {
                  const secili = deger.gunler.includes(g.kod);
                  return (
                    <button
                      key={g.kod}
                      type="button"
                      onClick={() =>
                        guncelle({
                          gunler: secili
                            ? deger.gunler.filter((k) => k !== g.kod)
                            : [...deger.gunler, g.kod],
                        })
                      }
                      className={cn(
                        'h-9 w-11 rounded-control border text-xs font-medium transition-colors',
                        secili
                          ? 'border-brand bg-brand text-on-brand'
                          : 'border-border bg-surface text-text-2 hover:bg-surface-2',
                      )}
                    >
                      {g.kisa}
                    </button>
                  );
                })}
              </div>
              <p className="mt-1.5 text-xs text-text-3">
                Seçim yapılmazsa etkinliğin başladığı gün kullanılır.
              </p>
            </div>
          )}

          <div className="grid gap-3 sm:grid-cols-2">
            <FieldWrapper etiket="Bitiş" id="t-bitis">
              <Secim
                id="t-bitis"
                value={deger.bitisTuru}
                onChange={(e) =>
                  guncelle({ bitisTuru: e.target.value as RecurrenceState['bitisTuru'] })
                }
              >
                <option value="yok">Süresiz</option>
                <option value="tarih">Belirli tarihte</option>
                <option value="sayi">Belirli sayıda</option>
              </Secim>
            </FieldWrapper>

            {deger.bitisTuru === 'tarih' && (
              <FieldWrapper etiket="Bitiş tarihi" id="t-btarih">
                <Input
                  id="t-btarih"
                  type="date"
                  value={deger.bitisTarihi}
                  onChange={(e) => guncelle({ bitisTarihi: e.target.value })}
                />
              </FieldWrapper>
            )}

            {deger.bitisTuru === 'sayi' && (
              <FieldWrapper etiket="Tekrar sayısı" id="t-adet">
                <Input
                  id="t-adet"
                  type="number"
                  min={1}
                  max={200}
                  value={deger.adet}
                  onChange={(e) => guncelle({ adet: Math.max(1, Number(e.target.value) || 1) })}
                />
              </FieldWrapper>
            )}
          </div>

          <p className="rounded-sm bg-sunken px-3 py-2 text-xs leading-normal text-text-2">
            Tekrarlar sunucuda gerçek kayıt olarak üretilir; 18 aylık ufka kadar
            oluşturulur ve otomatik olarak ileriye taşınır.
          </p>
        </div>
      )}
    </div>
  );
}
