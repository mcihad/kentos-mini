import { GripVertical, Plus, Trash2 } from 'lucide-react';
import { Button, IconButton } from '../components/Button';
import { FieldWrapper, Input, Secim } from '../components/Field';
import { Switch } from '../components/Switch';
import type { FormDefinition, FormField, FormOption } from '../data/types';
import {
  CONDITION_OP, CONDITION_OP_LABELS, FIELD_TYPE, fieldTypeInfo, isBlock,
} from './fieldTypes';
import { kosulAdaylari, yeniKimlik } from './definitionOps';

/**
 * SEÇİLİ ALANIN AYARLARI.
 *
 * <p>
 * Google Forms'ta ayarlar kartın içinde; burada <b>ayrı bir panelde</b> ve
 * bu bilinçli: tipe göre değişen 10+ ayar kartın içine sığdığında kart
 * dev bir forma dönüşüyor ve tuvalde formun ŞEKLİNİ görmek imkânsızlaşıyor.
 * Tuval "form neye benziyor", panel "bu soru nasıl çalışıyor" sorusunu
 * cevaplıyor.
 * </p>
 */
export function FieldSettings({
  tanim, alan, guncelle, sil,
}: {
  tanim: FormDefinition;
  alan: FormField;
  guncelle: (kismi: Partial<FormField>) => void;
  sil: () => void;
}) {
  const bilgi = fieldTypeInfo(alan.tip);
  const blok = isBlock(alan.tip);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 border-b border-line pb-3">
        <span className="grid size-8 shrink-0 place-items-center rounded-md bg-brand-soft text-brand">
          <bilgi.ikon size={16} />
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold">{bilgi.ad}</p>
          <p className="truncate text-2xs text-ink-3">{bilgi.ipucu}</p>
        </div>
        <IconButton etiket="Alanı sil" varyant="sade" onClick={sil}>
          <Trash2 size={16} className="text-(--st-no)" />
        </IconButton>
      </div>

      <FieldWrapper etiket={blok ? 'Metin' : 'Soru'} id="ay-etiket" zorunlu>
        <Input id="ay-etiket" value={alan.etiket ?? ''}
          onChange={(e) => guncelle({ etiket: e.target.value })} />
      </FieldWrapper>

      {!blok && (
        <>
          <FieldWrapper etiket="Yardım metni" id="ay-aciklama"
            ipucu="Sorunun altında küçük punto ile görünür.">
            <Input id="ay-aciklama" value={alan.aciklama ?? ''}
              onChange={(e) => guncelle({ aciklama: e.target.value })} />
          </FieldWrapper>

          <Switch
            etiket="Zorunlu alan"
            aciklama="Boş bırakılırsa form gönderilemez."
            isaretli={alan.zorunlu ?? false}
            degistir={(v) => guncelle({ zorunlu: v })}
          />

          <FieldWrapper etiket="Genişlik" id="ay-genislik"
            ipucu="12'lik ızgarada kaç birim. Telefonda her alan tam genişlik.">
            <Secim id="ay-genislik" value={String(alan.genislik ?? 12)}
              onChange={(e) => guncelle({ genislik: Number(e.target.value) })}>
              <option value="12">Tam satır</option>
              <option value="6">Yarım</option>
              <option value="4">Üçte bir</option>
              <option value="3">Çeyrek</option>
              <option value="8">Üçte iki</option>
            </Secim>
          </FieldWrapper>
        </>
      )}

      {bilgi.secenekli && (
        <SecenekListesi
          baslik="Seçenekler"
          liste={alan.secenekler ?? []}
          yaz={(l) => guncelle({ secenekler: l })}
          digerDestekli
        />
      )}

      {bilgi.matris && (
        <>
          <SecenekListesi baslik="Satırlar" liste={alan.satirlar ?? []}
            yaz={(l) => guncelle({ satirlar: l })} onek="r" />
          <SecenekListesi baslik="Sütunlar" liste={alan.sutunlar ?? []}
            yaz={(l) => guncelle({ sutunlar: l })} onek="c" />
        </>
      )}

      {(alan.tip === FIELD_TYPE.olcek || alan.tip === FIELD_TYPE.nps
        || alan.tip === FIELD_TYPE.yildiz) && (
        <div className="grid grid-cols-2 gap-2">
          <FieldWrapper etiket="En az" id="ay-az">
            <Input id="ay-az" type="number" value={String(alan.ayarlar?.enAz ?? 1)}
              onChange={(e) => guncelle({ ayarlar: { ...alan.ayarlar, enAz: Number(e.target.value) } })} />
          </FieldWrapper>
          <FieldWrapper etiket="En çok" id="ay-cok">
            <Input id="ay-cok" type="number" value={String(alan.ayarlar?.enCok ?? 5)}
              onChange={(e) => guncelle({ ayarlar: { ...alan.ayarlar, enCok: Number(e.target.value) } })} />
          </FieldWrapper>
          <FieldWrapper etiket="Alt uç etiketi" id="ay-alt" className="col-span-2">
            <Input id="ay-alt" value={alan.ayarlar?.altEtiket ?? ''}
              placeholder="Hiç memnun değilim"
              onChange={(e) => guncelle({ ayarlar: { ...alan.ayarlar, altEtiket: e.target.value } })} />
          </FieldWrapper>
          <FieldWrapper etiket="Üst uç etiketi" id="ay-ust" className="col-span-2">
            <Input id="ay-ust" value={alan.ayarlar?.ustEtiket ?? ''}
              placeholder="Çok memnunum"
              onChange={(e) => guncelle({ ayarlar: { ...alan.ayarlar, ustEtiket: e.target.value } })} />
          </FieldWrapper>
        </div>
      )}

      {(alan.tip === FIELD_TYPE.kisaMetin || alan.tip === FIELD_TYPE.uzunMetin) && (
        <div className="grid grid-cols-2 gap-2">
          <FieldWrapper etiket="En az karakter" id="ay-eau">
            <Input id="ay-eau" type="number" value={String(alan.dogrulama?.enAzUzunluk ?? '')}
              onChange={(e) => guncelle({ dogrulama: {
                ...alan.dogrulama, enAzUzunluk: e.target.value ? Number(e.target.value) : null } })} />
          </FieldWrapper>
          <FieldWrapper etiket="En çok karakter" id="ay-ecu">
            <Input id="ay-ecu" type="number" value={String(alan.dogrulama?.enCokUzunluk ?? '')}
              onChange={(e) => guncelle({ dogrulama: {
                ...alan.dogrulama, enCokUzunluk: e.target.value ? Number(e.target.value) : null } })} />
          </FieldWrapper>
        </div>
      )}

      {alan.tip === FIELD_TYPE.cokSecim && (
        <div className="grid grid-cols-2 gap-2">
          <FieldWrapper etiket="En az seçim" id="ay-eas">
            <Input id="ay-eas" type="number" value={String(alan.dogrulama?.enAzSecim ?? '')}
              onChange={(e) => guncelle({ dogrulama: {
                ...alan.dogrulama, enAzSecim: e.target.value ? Number(e.target.value) : null } })} />
          </FieldWrapper>
          <FieldWrapper etiket="En çok seçim" id="ay-ecs">
            <Input id="ay-ecs" type="number" value={String(alan.dogrulama?.enCokSecim ?? '')}
              onChange={(e) => guncelle({ dogrulama: {
                ...alan.dogrulama, enCokSecim: e.target.value ? Number(e.target.value) : null } })} />
          </FieldWrapper>
        </div>
      )}

      {!blok && <KosulKurucu tanim={tanim} alan={alan} guncelle={guncelle} />}
    </div>
  );
}

/* ────────────────────────────────────────────────── seçenek listesi */

function SecenekListesi({
  baslik, liste, yaz, onek = 'o', digerDestekli,
}: {
  baslik: string;
  liste: FormOption[];
  yaz: (l: FormOption[]) => void;
  onek?: string;
  digerDestekli?: boolean;
}) {
  return (
    <div>
      <p className="mb-1.5 text-xs font-semibold text-ink-2">{baslik}</p>

      <div className="space-y-1.5">
        {liste.map((s, i) => (
          <div key={s.kimlik} className="flex items-center gap-1.5">
            <GripVertical size={14} className="shrink-0 text-line-2" aria-hidden />
            <Input
              className="h-9 flex-1 text-sm"
              value={s.etiket ?? ''}
              placeholder={`${baslik} ${i + 1}`}
              onChange={(e) => yaz(liste.map((x, j) =>
                j === i ? { ...x, etiket: e.target.value } : x))}
            />
            {digerDestekli && (
              <button
                type="button"
                title='"Diğer" seçeneği: işaretlenince serbest metin ister'
                onClick={() => yaz(liste.map((x, j) => j === i ? { ...x, digerMi: !x.digerMi } : x))}
                className={`h-9 shrink-0 rounded-sm border px-2 text-2xs font-semibold ${
                  s.digerMi ? 'border-brand bg-brand-soft text-brand' : 'border-line text-ink-3'}`}
              >
                Diğer
              </button>
            )}
            <IconButton
              etiket="Seçeneği kaldır"
              varyant="sade"
              onClick={() => yaz(liste.filter((_, j) => j !== i))}
            >
              <Trash2 size={14} />
            </IconButton>
          </div>
        ))}
      </div>

      <Button
        varyant="ikincil"
        className="mt-2 h-9 w-full text-xs"
        onClick={() => yaz([...liste, { kimlik: yeniKimlik(onek), etiket: '' }])}
      >
        <Plus size={13} />
        {baslik.slice(0, -3)} ekle
      </Button>
    </div>
  );
}

/* ────────────────────────────────────────────────── koşul kurucu */

/**
 * KOŞULLU GÖRÜNÜRLÜK KURUCUSU.
 *
 * <p>
 * <b>Aday listesi YALNIZCA daha önce gelen soruları içerir.</b> Sunucu
 * geriye referansı zorluyor; ileriye bakan bir koşul kaydetmeyi
 * reddediyor. İleri soruları listede gösterip sonra reddetmek, kullanıcıya
 * neyi yanlış yaptığını hiç söylememek olurdu.
 * </p>
 */
function KosulKurucu({
  tanim, alan, guncelle,
}: {
  tanim: FormDefinition;
  alan: FormField;
  guncelle: (kismi: Partial<FormField>) => void;
}) {
  const adaylar = kosulAdaylari(tanim, alan.kimlik ?? '');
  const kurallar = alan.kosul?.kurallar ?? [];

  if (adaylar.length === 0) {
    return (
      <div className="rounded-md bg-sunken px-3 py-2.5 text-2xs leading-[1.5] text-ink-3">
        Koşullu görünürlük için bu sorudan <b>önce</b> gelen bir soru gerekiyor.
      </div>
    );
  }

  const yaz = (yeni: typeof kurallar) =>
    guncelle({ kosul: yeni.length > 0
      ? { baglac: alan.kosul?.baglac ?? 0, kurallar: yeni }
      : undefined });

  return (
    <div className="rounded-md border border-line p-2.5">
      <div className="mb-2 flex items-center justify-between">
        <p className="text-xs font-semibold text-ink-2">Koşullu görünürlük</p>
        {kurallar.length > 1 && (
          <Secim
            className="h-8 w-24 text-xs"
            value={String(alan.kosul?.baglac ?? 0)}
            onChange={(e) => guncelle({ kosul: {
              baglac: Number(e.target.value) as 0 | 1, kurallar } })}
          >
            <option value="0">Tümü</option>
            <option value="1">Herhangi biri</option>
          </Secim>
        )}
      </div>

      <div className="space-y-1.5">
        {kurallar.map((k, i) => {
          const degersiz = CONDITION_OP_LABELS
            .find((o) => o.deger === k.operator)?.degersiz;

          return (
            <div key={i} className="space-y-1.5 rounded-sm bg-sunken p-2">
              <Secim
                className="h-9 text-xs"
                value={k.alanKimligi ?? ''}
                onChange={(e) => yaz(kurallar.map((x, j) =>
                  j === i ? { ...x, alanKimligi: e.target.value } : x))}
              >
                {adaylar.map((a) => (
                  <option key={a.kimlik} value={a.kimlik ?? ''}>{a.etiket}</option>
                ))}
              </Secim>

              <div className="flex gap-1.5">
                <Secim
                  className="h-9 flex-1 text-xs"
                  value={String(k.operator ?? CONDITION_OP.esit)}
                  onChange={(e) => yaz(kurallar.map((x, j) =>
                    j === i ? { ...x, operator: Number(e.target.value) as 0 } : x))}
                >
                  {CONDITION_OP_LABELS.map((o) => (
                    <option key={o.deger} value={o.deger}>{o.etiket}</option>
                  ))}
                </Secim>

                {!degersiz && (
                  <Input
                    className="h-9 flex-1 text-xs"
                    placeholder="değer"
                    value={k.deger ?? ''}
                    onChange={(e) => yaz(kurallar.map((x, j) =>
                      j === i ? { ...x, deger: e.target.value } : x))}
                  />
                )}

                <IconButton etiket="Kuralı kaldır" varyant="sade"
                  onClick={() => yaz(kurallar.filter((_, j) => j !== i))}>
                  <Trash2 size={14} />
                </IconButton>
              </div>
            </div>
          );
        })}
      </div>

      {kurallar.length < 8 && (
        <Button
          varyant="ikincil"
          className="mt-2 h-9 w-full text-xs"
          onClick={() => yaz([...kurallar, {
            alanKimligi: adaylar[0].kimlik ?? '', operator: CONDITION_OP.esit, deger: '' }])}
        >
          <Plus size={13} />
          Kural ekle
        </Button>
      )}
    </div>
  );
}
