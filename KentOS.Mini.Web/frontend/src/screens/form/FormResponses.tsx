import { ArrowLeft, BarChart3, Download, Inbox, List, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, IconButton } from '../../components/Button';
import { Card, CardHeader } from '../../components/Card';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import { EmptyState } from '../../components/EmptyState';
import { SearchInput } from '../../components/Field';
import { FormModal } from '../../components/FormModal';
import { RowActions } from '../../components/RowActions';
import { Pagination } from '../../components/Pagination';
import { Skeleton } from '../../components/Skeleton';
import { Tabs } from '../../components/Tabs';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import {
  useForm, useFormMutations, useFormReport, useFormResponse, useFormResponses,
} from '../../data/forms';
import { dateTime } from '../../data/format';
import { tokenStore } from '../../data/client';
import { isBlock } from '../../forms/fieldTypes';
import { etiketliDeger, type Answers } from '../../forms/formEngine';

type Sekme = 'liste' | 'ozet';

/**
 * FORM YANITLARI — liste, detay ve dağılım.
 *
 * <p>
 * İki sekme iki ayrı soruyu cevaplıyor: <b>liste</b> "kim ne yazdı",
 * <b>özet</b> "genel eğilim ne". Tek ekranda birleştirmek, 500 yanıtlı bir
 * ankette dağılımı görmek için sayfalarca satır kaydırmak demekti.
 * </p>
 */
export default function FormResponses() {
  const { id } = useParams<{ id: string }>();
  const formId = Number(id);

  const { hasPermission } = useSession();
  const { bildir } = useToast();

  const [sekme, setSekme] = useState<Sekme>('liste');
  const [arama, setArama] = useState('');
  const [sayfa, setSayfa] = useState(1);
  const [acikYanit, setAcikYanit] = useState<number | null>(null);
  const [silinecek, setSilinecek] = useState<number | null>(null);

  const form = useForm(formId);
  const yanitlar = useFormResponses(formId, { arama: arama || undefined, sayfa, boyut: 25 });
  const ozet = useFormReport(sekme === 'ozet' ? formId : undefined);
  const detay = useFormResponse(formId, acikYanit ?? undefined);
  const m = useFormMutations(formId);

  async function excelIndir() {
    // Jeton `Authorization` başlığıyla gitmek zorunda; düz bir bağlantı
    // (`<a href>`) başlık taşımıyor ve uç 401 dönerdi.
    const j = tokenStore.read();
    const y = await fetch(`/api/v2/form/${formId}/excel`, {
      headers: j ? { authorization: `Bearer ${j.jeton}` } : {},
    });

    if (!y.ok) { bildir('hata', 'İndirilemedi'); return; }

    const b = await y.blob();
    const a = document.createElement('a');
    a.href = URL.createObjectURL(b);
    a.download = `${form.data?.baslik ?? 'form'}-yanitlar.xlsx`;
    a.click();
    URL.revokeObjectURL(a.href);
  }

  return (
    <div className="space-y-3.5">
      <div className="flex items-start gap-2">
        <Link to="/formlar" className="mt-0.5">
          <IconButton etiket="Formlara dön"><ArrowLeft size={18} /></IconButton>
        </Link>
        <div className="min-w-0 flex-1">
          <h1 className="truncate font-display text-xl font-bold tracking-[-0.01em]">
            {form.data?.baslik ?? 'Yanıtlar'}
          </h1>
          <p className="text-xs text-ink-3 tabular-nums">
            {form.data?.yanitSayisi ?? 0} yanıt
          </p>
        </div>
        {hasPermission(PERMISSION.formCiktiAl) && (
          <Button varyant="ikincil" onClick={excelIndir}>
            <Download size={15} />
            <span className="hidden sm:inline">Excel</span>
          </Button>
        )}
      </div>

      <Tabs<Sekme>
        deger={sekme}
        degistir={setSekme}
        sekmeler={[
          { deger: 'liste', etiket: 'Yanıtlar', ikon: <List size={14} /> },
          { deger: 'ozet', etiket: 'Özet', ikon: <BarChart3 size={14} /> },
        ]}
      />

      {sekme === 'liste' && (
        <>
          <SearchInput
            value={arama}
            onChange={(e) => { setArama(e.target.value); setSayfa(1); }}
            placeholder="Takip no, ad ya da telefon ara"
          />

          {yanitlar.isLoading && <Skeleton className="h-40" />}

          {yanitlar.data?.veriler.length === 0 && (
            <EmptyState
              ikon={Inbox}
              baslik="Henüz yanıt yok"
              aciklama="Form bağlantısını paylaştıktan sonra gelen yanıtlar burada listelenir."
            />
          )}

          {(yanitlar.data?.veriler.length ?? 0) > 0 && (
            <>
              {/* Açıcı düğme ile eylem KARDEŞ: `ListRow` tıklanabilir
                  olduğunda satırı bir düğmeye çeviriyor ve içindeki
                  düğmeler `button > button` üretiyor. */}
              <ul className="overflow-hidden rounded-lg border border-line bg-surface">
                {yanitlar.data!.veriler.map((y) => (
                  <li
                    key={y.id}
                    className="flex items-start gap-2 border-b border-line px-3 py-2.5 last:border-0"
                  >
                    <button
                      type="button"
                      onClick={() => setAcikYanit(y.id!)}
                      className="min-w-0 flex-1 text-left"
                    >
                      <span className="block font-mono text-2xs tracking-wide text-ink-3">
                        {y.takipNo}
                        {y.gonderimTarihi && (
                          <span className="ml-2 font-sans">{dateTime(y.gonderimTarihi)}</span>
                        )}
                      </span>
                      <span className="mt-0.5 block truncate text-base font-semibold">
                        {y.adSoyad || 'İsimsiz yanıt'}
                      </span>
                      {y.onizleme && (
                        <span className="block truncate text-sm text-ink-3">{y.onizleme}</span>
                      )}
                    </button>

                    {hasPermission(PERMISSION.formYanitSil) && (
                      <RowActions
                        boyut="kucuk"
                        eylemler={[{
                          etiket: 'Geçersiz say', ikon: Trash2, ton: 'tehlike',
                          onClick: () => setSilinecek(y.id!),
                        }]}
                      />
                    )}
                  </li>
                ))}
              </ul>

              <Pagination sonuc={yanitlar.data} sayfaDegistir={setSayfa} birim="yanıt" />
            </>
          )}
        </>
      )}

      {sekme === 'ozet' && (
        <div className="space-y-3">
          {ozet.isLoading && <Skeleton className="h-40" />}

          {(ozet.data?.alanlar ?? []).map((a) => (
            <Card key={a.alanKimligi} className="overflow-hidden">
              <CardHeader baslik={a.etiket ?? ''} aciklama={`${a.yanitSayisi} yanıt`} />

              <div className="px-3.5 pb-3.5">
                {a.dagilim && a.dagilim.length > 0 && (
                  <ul className="space-y-1.5">
                    {a.dagilim.map((d, i) => (
                      <li key={i}>
                        {/* MATRİS: satır adı yalnızca değiştiğinde yazılır.
                            Her çubuğa tekrarlansaydı "Temizlik → İyi",
                            "Temizlik → Orta" diye okunur ve asıl sayı
                            satır adının arkasında kaybolurdu. */}
                        {d.satir && d.satir !== a.dagilim![i - 1]?.satir && (
                          <p className={`text-xs font-semibold text-ink-2 ${i > 0 ? 'mt-3' : ''}`}>
                            {d.satir}
                          </p>
                        )}
                        <div className="flex items-baseline justify-between gap-2 text-sm">
                          <span className="min-w-0 truncate">{d.etiket}</span>
                          <span className="shrink-0 tabular-nums text-ink-3">
                            {d.adet} · %{d.yuzde}
                          </span>
                        </div>
                        {/* Dağılım çubuğu: grafik kütüphanesi açmadan da
                            oran okunuyor ve mobilde yer kaplamıyor. */}
                        <div className="mt-1 h-1.5 overflow-hidden rounded-full bg-sunken">
                          <div className="h-full rounded-full bg-brand"
                            style={{ width: `${d.yuzde}%` }} />
                        </div>
                      </li>
                    ))}
                  </ul>
                )}

                {a.ortalama != null && (
                  <p className="text-sm">
                    Ortalama: <b className="tabular-nums">{a.ortalama}</b>
                  </p>
                )}

                {a.ornekler && a.ornekler.length > 0 && (
                  <ul className="space-y-1">
                    {a.ornekler.map((o, i) => (
                      <li key={i} className="rounded-sm bg-sunken px-2.5 py-1.5 text-sm wrap-anywhere">
                        {o}
                      </li>
                    ))}
                  </ul>
                )}

                {!a.dagilim?.length && a.ortalama == null && !a.ornekler?.length && (
                  <p className="text-sm text-ink-3">Bu alana yanıt gelmemiş.</p>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* ── yanıt detayı ── */}
      <FormModal
        acik={acikYanit !== null}
        kapat={() => setAcikYanit(null)}
        baslik={detay.data?.takipNo ?? 'Yanıt'}
        aciklama={detay.data?.gonderimTarihi ? dateTime(detay.data.gonderimTarihi) : undefined}
        eylemler={<Button varyant="ikincil" onClick={() => setAcikYanit(null)}>Kapat</Button>}
      >
        <div className="p-3.5">
          {detay.isLoading && <Skeleton className="h-40" />}

          {detay.data && (
            <dl className="divide-y divide-line">
              {(detay.data.tanim?.adimlar ?? [])
                .flatMap((a) => a.gruplar ?? [])
                .flatMap((g) => g.alanlar ?? [])
                .filter((alan) => !isBlock(alan.tip))
                .map((alan) => {
                  const c = (detay.data!.cevaplar as Answers)[alan.kimlik ?? ''];

                  return (
                    <div key={alan.kimlik} className="py-2.5">
                      <dt className="text-xs text-ink-3">{alan.etiket}</dt>
                      <dd className="mt-0.5 text-sm wrap-anywhere">
                        {etiketliDeger(alan, c)}
                      </dd>
                    </div>
                  );
                })}
            </dl>
          )}
        </div>
      </FormModal>

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Yanıt geçersiz sayılsın mı?"
        aciklama="Kayıt SİLİNMEZ, yalnızca sayımdan düşer. Yinelenen ya da kötüye kullanım amaçlı yanıtlar için."
        onayEtiketi="Geçersiz say"
        yikici
        onayla={async () => {
          try {
            await m.yanitSil.mutateAsync({ formId, yanitId: silinecek! });
            bildir('basari', 'Geçersiz sayıldı');
          } catch (h) {
            bildir('hata', 'İşlem yapılamadı', (h as Error).message);
          }
        }}
      />
    </div>
  );
}
