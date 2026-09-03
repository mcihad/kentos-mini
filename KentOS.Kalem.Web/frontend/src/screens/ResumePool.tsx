import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Download, FileUser, Filter, Pencil, Plus, Search, Share2, Trash2, X,
} from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { SearchInput } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { SkeletonRows } from '../components/Skeleton';
import { DataList, type Column } from '../components/DataList';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { RowActions, type RowAction } from '../components/RowActions';
import { Pagination } from '../components/Pagination';
import { SegmentedSelect } from '../components/Filters';
import { useIsDesktop } from '../components/screenSize';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { Fab } from '../shell/mobile/Fab';
import { useSession } from '../auth/SessionProvider';
import { initials, fileSize as fileSizeText, shortDate } from '../data/format';
import { parseDay } from '../components/DatePicker';
import { download } from '../data/download';
import { ExportButtons } from '../components/ExportButtons';
import { api } from '../data/client';
import { useResumes } from '../data/hooks';
import type { ResumeSummary } from '../data/types';
import { InitialsChip, SourceBadge } from './resume/SourceBadge';
import { ResumeForm } from './resume/ResumeForm';
import {
  ResumeFilter, type ResumeSource, type ResumeFilterValues,
} from './resume/ResumeFilter';
import { ResumeSheet } from './resume/ResumeSheet';
import { ShareDialog } from './resume/ShareDialog';

const BOS_SUZGEC: ResumeFilterValues = {
  kaynak: 'tumu',
  meslekId: null,
  meslekAdi: null,
  mahalleId: null,
  mahalleAdi: null,
  baslangic: '',
  bitis: '',
  banaPaylasilan: false,
};

/**
 * ÖZGEÇMİŞ HAVUZU.
 *
 * <p>
 * "Elimizde kaynakçı var mı?" sorusunun cevabı. Özgeçmişler iki yoldan
 * geliyor — doğrudan havuza yüklenenler ve <b>iş taleplerine</b> eklenenler —
 * ama tek listede aranıyor.
 * </p>
 *
 * <p>
 * <b>Havuz birimden bağımsız.</b> Sistemin geri kalanında kayıt kendi
 * biriminin içinde kalır; burada kalmaz, çünkü modülün varlık sebebi tam
 * tersi: bir müdürlüğün elindeki özgeçmişi işe alacak olan başka müdürlük de
 * görebilmeli.
 * </p>
 *
 * <h3>Satır neden yeniden yazıldı</h3>
 *
 * <p>
 * Mobil liste <c>Liste</c>'nin genel dalını kullanıyordu ve o dal, başlık ve
 * açıklamanın YANINDA bütün sütunları da alt alta basıyor: ad iki kez,
 * telefon iki kez, kaynak rozeti iki kez çiziliyordu; dört ayrı kenarlıklı
 * ikon düğmesi de metnin altında havada duruyordu. Kullanıcının "her şey
 * birbirine geçmiş" dediği şey buydu.
 * </p>
 *
 * <p>
 * Satır artık tek amaçlı: <b>kim, ne iş, ne yazıyor</b>. Dokununca kişinin
 * kendi tabakası açılıyor (bkz. {@link OzgecmisTabakasi}); satırda tek eylem
 * kalıyor, o da havuzun asıl karşılığı — dosyayı indirmek.
 * </p>
 */
export default function ResumePool() {
  const { hasPermission } = useSession();
  const { bildir } = useToast();
  const qc = useQueryClient();
  const masaustu = useIsDesktop();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [sayfa, setSayfa] = useState(1);
  const [s, setS] = useState<ResumeFilterValues>(BOS_SUZGEC);

  const [suzgecAcik, setSuzgecAcik] = useState(false);
  const [acilan, setAcilan] = useState<ResumeSummary | null>(null);
  const [form, setForm] = useState<ResumeSummary | 'yeni' | null>(null);
  const [paylasilacak, setPaylasilacak] = useState<ResumeSummary | null>(null);
  const [silinecek, setSilinecek] = useState<ResumeSummary | null>(null);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const suzgecDegistir = (yeni: Partial<ResumeFilterValues>) => {
    setS((o) => ({ ...o, ...yeni }));
    setSayfa(1);
  };

  const suzgec = useMemo(
    () => ({
      sayfa,
      boyut: 25,
      ara: arama || undefined,
      kaynak: s.kaynak === 'tumu' ? undefined : s.kaynak,
      meslekId: s.meslekId ?? undefined,
      mahalleId: s.mahalleId ?? undefined,
      baslangic: parseDay(s.baslangic)?.toISOString().slice(0, 10) || undefined,
      bitis: parseDay(s.bitis)?.toISOString().slice(0, 10) || undefined,
      banaPaylasilan: s.banaPaylasilan || undefined,
    }),
    [sayfa, arama, s],
  );

  const liste = useResumes(suzgec);

  /** Kaç süzgeç açık — düğmedeki sayı. */
  const acikSuzgec =
    (s.meslekId ? 1 : 0) + (s.mahalleId ? 1 : 0) + (s.baslangic ? 1 : 0) +
    (s.bitis ? 1 : 0) + (s.banaPaylasilan ? 1 : 0);

  const temizle = () => {
    setS(BOS_SUZGEC);
    setSayfa(1);
  };

  const sil = useMutation({
    mutationFn: (id: number) => api.delete(`/ozgecmis/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['ozgecmis'] });
      bildir('basari', 'Özgeçmiş kaldırıldı');
      setSilinecek(null);
      setAcilan(null);
    },
    onError: (h: Error) => bildir('hata', 'Silinemedi', h.message),
  });

  async function dosyayiIndir(o: ResumeSummary) {
    try {
      await download(`/ozgecmis/${o.id}/dosya`);
    } catch (h) {
      bildir('hata', 'Dosya indirilemedi', (h as Error).message);
    }
  }

  /** Satır eylemleri TEK yerde kurulur; iki görünüm de bunu okur. */
  const eylemler = (o: ResumeSummary): RowAction[] => [
    {
      etiket: `${o.adSoyad} özgeçmişini indir`,
      ikon: Download,
      onClick: () => void dosyayiIndir(o),
      ton: 'marka',
      pasif: !o.dosyaAdi,
    },
    ...(hasPermission(PERMISSION.ozgecmisPaylas)
      ? [{ etiket: `${o.adSoyad} kaydını yönlendir`, ikon: Share2, onClick: () => setPaylasilacak(o) }]
      : []),
    ...(hasPermission(PERMISSION.ozgecmisDuzenle)
      ? [{ etiket: `${o.adSoyad} kaydını düzenle`, ikon: Pencil, onClick: () => setForm(o) }]
      : []),
    ...(hasPermission(PERMISSION.ozgecmisSil)
      ? [{
          etiket: `${o.adSoyad} kaydını sil`,
          ikon: Trash2,
          onClick: () => setSilinecek(o),
          ton: 'tehlike' as const,
        }]
      : []),
  ];

  const sutunlar: Column<ResumeSummary>[] = [
    {
      anahtar: 'adSoyad',
      baslik: 'Kişi',
      hucre: (o) => (
        <button
          type="button"
          onClick={() => setAcilan(o)}
          className="flex w-full min-w-0 items-center gap-2.5 text-left"
        >
          <InitialsChip
            harfler={initials(...(o.adSoyad ?? '').split(' '))}
            talepten={Boolean(o.talepId)}
          />
          <span className="min-w-0">
            <span className="block truncate font-medium hover:text-brand-2">{o.adSoyad}</span>
            <span className="block truncate text-xs text-text-3">
              {[o.telefon, o.eposta].filter(Boolean).join(' · ') || '—'}
            </span>
          </span>
        </button>
      ),
    },
    {
      anahtar: 'meslekAd',
      baslik: 'Meslek',
      hucre: (o) => (
        <span className="block min-w-0">
          <span className="block truncate">{o.meslekAd || '—'}</span>
          {o.mahalleAd && (
            <span className="block truncate text-xs text-text-3">{o.mahalleAd}</span>
          )}
        </span>
      ),
    },
    {
      anahtar: 'aciklama',
      baslik: 'Açıklama',
      hucre: (o) => (
        <span className="line-clamp-2 text-sm text-text-2 wrap-anywhere">{o.aciklama || '—'}</span>
      ),
    },
    {
      anahtar: 'kaynakAd',
      baslik: 'Kaynak',
      hucre: (o) => <SourceBadge kayit={o} />,
    },
    {
      anahtar: 'olusturmaTarihi',
      baslik: 'Eklenme',
      hucre: (o) => (
        <span className="whitespace-nowrap text-sm text-text-3">
          {shortDate(o.olusturmaTarihi)}
        </span>
      ),
    },
    {
      anahtar: 'id',
      baslik: '',
      hucre: (o) => (
        <div className="flex justify-end">
          <RowActions boyut="kucuk" eylemler={eylemler(o)} />
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      {/* ── Araç çubuğu ── */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Ad, telefon, meslek veya açıklama"
          aria-label="Özgeçmişlerde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[320px] md:flex-1"
        />

        {hasPermission(PERMISSION.ozgecmisCiktiAl) && (
          <ExportButtons
            className="hidden md:inline-flex"
            /* Sayfa/boyut ÇIKARILIYOR: çıktı sayfalanmaz, ekrandaki
               süzgeçlerin tamamını kapsar. */
            excel={() => download('/ozgecmis/excel', { ...suzgec, sayfa: undefined, boyut: undefined })}
          />
        )}

        <SegmentedSelect<ResumeSource>
          deger={s.kaynak}
          degistir={(d) => suzgecDegistir({ kaynak: d })}
          etiket="Kaynak"
          className="hidden md:inline-flex"
          secenekler={[
            { deger: 'tumu', etiket: 'Tümü' },
            { deger: 'havuz', etiket: 'Havuz' },
            { deger: 'talep', etiket: 'Talepten' },
          ]}
        />

        {/*
          MOBİLDE ÜSTTE YALNIZCA ARAMA.

          Şerit dört parçalıydı: arama · kaynak seçimi · "Süzgeç" düğmesi ·
          "Özgeçmiş ekle". Kaynak seçimi süzgeç tabakasına girdi; ekleme ve
          süzgeç FAB'a taşındı.
        */}
        {!masaustu && (
          <Fab
            etiket="Özgeçmiş eylemleri"
            eylemler={[
              ...(hasPermission(PERMISSION.ozgecmisEkle)
                ? [{
                    etiket: 'Özgeçmiş ekle',
                    ikon: <Plus size={21} strokeWidth={2.2} />,
                    onClick: () => setForm('yeni'),
                  }]
                : []),
              {
                etiket: acikSuzgec > 0 ? `Süzgeç (${acikSuzgec})` : 'Ara ve süz',
                ikon: <Filter size={19} strokeWidth={2} />,
                onClick: () => setSuzgecAcik(true),
              },
            ]}
          />
        )}

        <div className="hidden items-center gap-1.5 md:ml-auto md:flex">
          <Button
            varyant={acikSuzgec > 0 ? 'birincil' : 'ikincil'}
            onClick={() => setSuzgecAcik(true)}
          >
            <Filter size={14} />
            Süzgeç
            {acikSuzgec > 0 && (
              <span className="ml-0.5 rounded-full bg-[rgba(255,255,255,.22)] px-1.5 text-2xs tabular-nums">
                {acikSuzgec}
              </span>
            )}
          </Button>

          {hasPermission(PERMISSION.ozgecmisEkle) && (
            <Button onClick={() => setForm('yeni')}>
              <Plus size={14} />
              Özgeçmiş ekle
            </Button>
          )}
        </div>
      </div>

      {/* Açık süzgeçler görünür kalsın: kapalı bir tabakadaki süzgeç,
          "kayıtlarım kayboldu" diye gelen sorunun bir numaralı sebebi. */}
      {acikSuzgec > 0 && (
        <div className="flex flex-wrap items-center gap-1.5">
          {s.meslekAdi && (
            <Cip etiket={s.meslekAdi} kaldir={() => suzgecDegistir({ meslekId: null, meslekAdi: null })} />
          )}
          {s.mahalleAdi && (
            <Cip etiket={s.mahalleAdi} kaldir={() => suzgecDegistir({ mahalleId: null, mahalleAdi: null })} />
          )}
          {s.baslangic && (
            <Cip etiket={`${s.baslangic} sonrası`} kaldir={() => suzgecDegistir({ baslangic: '' })} />
          )}
          {s.bitis && <Cip etiket={`${s.bitis} öncesi`} kaldir={() => suzgecDegistir({ bitis: '' })} />}
          {s.banaPaylasilan && (
            <Cip etiket="Bana yönlendirilenler" kaldir={() => suzgecDegistir({ banaPaylasilan: false })} />
          )}
          <button
            type="button"
            onClick={temizle}
            className="text-xs text-text-3 underline-offset-2 hover:underline"
          >
            Hepsini temizle
          </button>
        </div>
      )}

      {/* ── Liste ── */}
      {liste.isLoading ? (
        <SkeletonRows adet={5} />
      ) : (liste.data?.veriler.length ?? 0) === 0 ? (
        <EmptyState
          ikon={FileUser}
          baslik={arama || acikSuzgec > 0 ? 'Eşleşen kayıt yok' : 'Özgeçmiş yok'}
          aciklama={
            arama || acikSuzgec > 0
              ? 'Aramayı kısaltmayı ya da süzgeçleri temizlemeyi deneyin.'
              : 'Havuza doğrudan özgeçmiş ekleyebilir ya da iş taleplerine yüklenenlerin burada birikmesini bekleyebilirsiniz.'
          }
          eylem={
            arama || acikSuzgec > 0 ? (
              <Button varyant="ikincil" onClick={temizle}>
                Süzgeçleri temizle
              </Button>
            ) : hasPermission(PERMISSION.ozgecmisEkle) ? (
              <Button onClick={() => setForm('yeni')}>
                <Plus size={14} />
                Özgeçmiş ekle
              </Button>
            ) : undefined
          }
        />
      ) : (
        <>
          {masaustu ? (
            <DataList
              satirlar={liste.data!.veriler}
              sutunlar={sutunlar}
              anahtar={(o) => o.id!}
              mobilBaslik={(o) => o.adSoyad}
            />
          ) : (
            <div className="overflow-hidden rounded-card border border-line bg-surface">
              <ul className="divide-y divide-line">
                {liste.data!.veriler.map((o) => (
                  <OzgecmisSatiri
                    key={o.id}
                    kayit={o}
                    ac={() => setAcilan(o)}
                    indirEylemi={{
                      etiket: `${o.adSoyad} özgeçmişini indir`,
                      ikon: Download,
                      onClick: () => void dosyayiIndir(o),
                      ton: 'marka',
                      pasif: !o.dosyaAdi,
                    }}
                  />
                ))}
              </ul>
            </div>
          )}
          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="özgeçmiş" />
        </>
      )}

      <ResumeFilter
        acik={suzgecAcik}
        kapat={() => setSuzgecAcik(false)}
        deger={s}
        degistir={suzgecDegistir}
        temizle={temizle}
      />

      {acilan && (
        <ResumeSheet
          kayit={acilan}
          kapat={() => setAcilan(null)}
          duzenle={() => {
            setForm(acilan);
            setAcilan(null);
          }}
          paylas={() => {
            setPaylasilacak(acilan);
            setAcilan(null);
          }}
          sil={() => setSilinecek(acilan)}
        />
      )}

      {form && (
        <ResumeForm kayit={form === 'yeni' ? null : form} kapat={() => setForm(null)} />
      )}

      {paylasilacak && (
        <ShareDialog kayit={paylasilacak} kapat={() => setPaylasilacak(null)} />
      )}

      <ConfirmDialog
        acik={silinecek !== null}
        kapat={() => setSilinecek(null)}
        baslik="Özgeçmiş kaldırılsın mı?"
        aciklama={`${silinecek?.adSoyad ?? ''} kaydı havuzdan kaldırılacak. Talebe bağlı bir kayıtsa talebin kendi dosyası yerinde kalır.`}
        onayEtiketi="Kaldır"
        yikici
        onayla={() => silinecek?.id && sil.mutate(silinecek.id)}
      />
    </div>
  );
}

/**
 * MOBİL SATIR.
 *
 * <p>
 * Tek amaç: <b>kim, ne iş, ne yazıyor</b>. Solda baş harf çipi (havuz bir
 * kişi listesi; yüz olması taramayı hızlandırıyor), ortada üç satırlık metin
 * yığını, sağda kaynağı belliyse rozeti ve TEK eylem.
 * </p>
 *
 * <p>
 * <b>"Havuz" rozeti çizilmez.</b> Havuzdaki kaydın havuzdan gelmesi haber
 * değil; her satıra basılınca gözün süzdüğü bir gürültü katmanı oluyordu.
 * Rozet yalnızca <b>istisna</b> için: arkasında bir talep olan kayıt.
 * </p>
 *
 * <p>
 * Metin bloğu bir düğme, indirme ayrı bir düğme — <b>iç içe değil kardeş</b>.
 * Düğme içinde düğme geçersiz HTML ve tarayıcı davranışı tanımsız; aynı aile
 * bağlantı içinde bağlantı tuzağıyla (bkz. `CLAUDE.md`).
 * </p>
 */
function OzgecmisSatiri({
  kayit,
  ac,
  indirEylemi,
}: {
  kayit: ResumeSummary;
  ac: () => void;
  indirEylemi: RowAction;
}) {
  const talepten = Boolean(kayit.talepId);
  const nitelik = [kayit.meslekAd, kayit.mahalleAd].filter(Boolean).join(' · ');
  const meta = [
    shortDate(kayit.olusturmaTarihi),
    kayit.boyut ? fileSizeText(kayit.boyut) : null,
    kayit.paylasimSayisi ? `${kayit.paylasimSayisi} yönlendirme` : null,
  ].filter(Boolean);

  return (
    <li className="flex items-start gap-2 p-3">
      <button
        type="button"
        onClick={ac}
        className="flex min-w-0 flex-1 items-start gap-2.5 text-left transition-opacity active:opacity-70"
      >
        <InitialsChip
          harfler={initials(...(kayit.adSoyad ?? '').split(' '))}
          talepten={talepten}
        />
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-semibold">{kayit.adSoyad}</span>
          {/* Boş alan için "belirtilmemiş" yazılmaz: satıra bilgi katmayan
              bir satır ekliyor ve göz onu da taramak zorunda kalıyor. */}
          {nitelik && (
            <span className="mt-0.5 block truncate text-xs text-ink-2">{nitelik}</span>
          )}
          {/* Havuzun asıl içeriği: "8 yıl deneyim, ehliyet var." */}
          {kayit.aciklama && (
            <span className="mt-1 block text-xs leading-[1.45] text-ink-3 satir-2 wrap-anywhere">
              {kayit.aciklama}
            </span>
          )}
          {/*
            KAYNAK ROZETİ META SATIRINDA.

            Sağ üst köşedeyken satırdan satıra indirme düğmesini aşağı
            itiyordu: rozeti olan satırda düğme ortada, olmayanda tepede
            duruyor ve liste "titrek" görünüyordu. Rozet zaten bir üstveri,
            yeri tarihin yanı.
          */}
          <span className="mt-1 flex flex-wrap items-center gap-x-1.5 gap-y-1 text-2xs tabular-nums text-ink-3">
            {meta.join(' · ')}
            {talepten && <SourceBadge kayit={kayit} />}
          </span>
        </span>
      </button>

      <RowActions boyut="kucuk" eylemler={[indirEylemi]} />
    </li>
  );
}

function Cip({ etiket, kaldir }: { etiket: string; kaldir: () => void }) {
  return (
    <span className="inline-flex items-center gap-1 rounded-full border border-brand-2 bg-brand-tint px-2.5 py-1 text-xs">
      {etiket}
      <button type="button" onClick={kaldir} aria-label={`${etiket} süzgecini kaldır`}>
        <X size={12} />
      </button>
    </span>
  );
}
