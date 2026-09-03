import {
  Briefcase, Check, Download, Mail, MapPin, Pencil, Phone, Share2, Trash2,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button, IconButton } from '../../components/Button';
import { FormSection } from '../../components/FormSection';
import { FormModal } from '../../components/FormModal';
import { SkeletonRows } from '../../components/Skeleton';
import { PERMISSION } from '../../components/permissions';
import { useToast } from '../../components/Toast';
import { useSession } from '../../auth/SessionProvider';
import { fileSize as fileSizeText, initials, shortDate, dateTime } from '../../data/format';
import { download } from '../../data/download';
import { useResume } from '../../data/hooks';
import type { ResumeSummary } from '../../data/types';
import { SourceBadge } from './SourceBadge';

/**
 * ÖZGEÇMİŞ TABAKASI — bir kişinin havuzdaki dosyası.
 *
 * <p>
 * Havuzun asıl sorusu "elimizde kaynakçı var mı?" ve cevabı <b>açıklama
 * metninde</b> yazıyor: "8 yıl deneyim, ehliyet var". O metin listede iki
 * satıra kırpılıyor ve tamamını okumanın hiçbir yolu yoktu; satırdaki dört
 * ayrı ikon düğmesi ise 390px'lik ekranda ismin yerini yiyordu.
 * </p>
 *
 * <p>
 * Şimdi satır sade: dokununca <b>kişinin kendi tabakası</b> açılıyor —
 * iletişim, açıklamanın tamamı, dosya, geldiği talep ve <b>kime
 * yönlendirildiği</b>. Sonuncusu listede hiç yoktu: aynı özgeçmişi ikinci kez
 * aynı müdürlüğe göndermenin önüne geçen tek bilgi o.
 * </p>
 */
export function ResumeSheet({
  kayit,
  kapat,
  duzenle,
  paylas,
  sil,
}: {
  /** Listedeki özet — tabaka açılır açılmaz doldurulmuş görünsün diye. */
  kayit: ResumeSummary;
  kapat: () => void;
  duzenle: () => void;
  paylas: () => void;
  sil: () => void;
}) {
  const { hasPermission } = useSession();
  const { bildir } = useToast();
  const detay = useResume(kayit.id!);

  // Ayrıntı gelene kadar özetteki alanlar gösterilir: tabaka boş açılmıyor.
  const o = detay.data ?? kayit;
  const talepten = Boolean(o.talepId);
  // `adres` yalnızca ayrıntı yanıtında var.
  const adres = detay.data?.adres;

  async function dosyayiIndir() {
    try {
      await download(`/ozgecmis/${o.id}/dosya`);
    } catch (h) {
      bildir('hata', 'Dosya indirilemedi', (h as Error).message);
    }
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik={o.adSoyad ?? 'Özgeçmiş'}
      aciklama={[o.meslekAd, o.mahalleAd].filter(Boolean).join(' · ') || 'Havuz kaydı'}
      ikon={
        <span className="font-display text-2xs font-bold">
          {initials(...(o.adSoyad ?? '').split(' '))}
        </span>
      }
      genislik="dar"
      altBilgi={o.paylasimSayisi ? `${o.paylasimSayisi} kişiye yönlendirildi` : undefined}
      eylemler={
        <>
          {hasPermission(PERMISSION.ozgecmisPaylas) && (
            <IconButton etiket="Yönlendir" onClick={paylas}>
              <Share2 size={15} />
            </IconButton>
          )}
          {hasPermission(PERMISSION.ozgecmisSil) && (
            <IconButton etiket="Sil" onClick={sil} className="text-danger">
              <Trash2 size={15} />
            </IconButton>
          )}
          {hasPermission(PERMISSION.ozgecmisDuzenle) && (
            <Button varyant="ikincil" onClick={duzenle}>
              <Pencil size={14} />
              Düzenle
            </Button>
          )}
        </>
      }
    >
      {/*
        DOSYA EN ÜSTTE.

        Bu ekranda yapılan tek şey neredeyse her zaman aynı: özgeçmişi açmak.
        Diğer bilgiler onu açmaya karar vermek için var, o yüzden karar
        düğmesi bilgilerin ALTINDA değil üstünde.
      */}
      <div className="flex items-center gap-3 rounded-card border border-border bg-surface-2 p-3">
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-medium wrap-anywhere">
            {o.dosyaAdi || 'Dosya yok'}
          </span>
          <span className="mt-0.5 block text-2xs text-text-3">
            {o.boyut ? fileSizeText(o.boyut) : '—'}
          </span>
        </span>
        <Button onClick={() => void dosyayiIndir()} disabled={!o.dosyaAdi}>
          <Download size={14} />
          İndir
        </Button>
      </div>

      {(o.telefon || o.eposta || o.mahalleAd || adres) && (
        <FormSection baslik="İletişim">
          <ul className="divide-y divide-line">
            {o.telefon && (
              <IletisimSatiri
                ikon={<Phone size={14} />}
                deger={o.telefon}
                yol={`tel:${o.telefon}`}
                eylem="Ara"
              />
            )}
            {o.eposta && (
              <IletisimSatiri
                ikon={<Mail size={14} />}
                deger={o.eposta}
                yol={`mailto:${o.eposta}`}
                eylem="Yaz"
              />
            )}
            {(o.mahalleAd || adres) && (
              <IletisimSatiri
                ikon={<MapPin size={14} />}
                deger={[o.mahalleAd, adres].filter(Boolean).join(' · ')}
              />
            )}
          </ul>
        </FormSection>
      )}

      {o.aciklama && (
        <FormSection baslik="Açıklama">
          <p className="whitespace-pre-line text-sm leading-[1.6] text-text-2 wrap-anywhere metin-guzel">
            {o.aciklama}
          </p>
        </FormSection>
      )}

      <FormSection baslik="Kaynak">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <SourceBadge kayit={o} />
            {talepten && o.talepId && (
              <Link
                to={`/talepler/${o.talepId}`}
                onClick={kapat}
                className="inline-flex min-w-0 items-center gap-1.5 text-sm text-brand hover:underline"
              >
                <Briefcase size={13} className="shrink-0" />
                <span className="truncate">{o.talepKonusu || `Talep #${o.talepId}`}</span>
              </Link>
            )}
          </div>
          <p className="text-2xs leading-[1.5] text-text-3">
            {[o.olusturan, o.birimAd].filter(Boolean).join(' · ')}
            {o.olusturmaTarihi ? ` · ${shortDate(o.olusturmaTarihi)}` : ''}
          </p>
        </div>
      </FormSection>

      {/*
        YÖNLENDİRME GEÇMİŞİ.

        "Bu özgeçmişi zaten göndermiş miyiz?" sorusunun cevabı. Yoksa aynı
        kayıt aynı müdürlüğe ikinci kez gidiyor ve alıcı bunu "yine mi" diye
        okuyor. Görüntülenme damgası da burada: gönderildi ile okundu ayrı
        şeyler.
      */}
      {detay.isLoading ? (
        <SkeletonRows adet={2} />
      ) : (
        (detay.data?.paylasimlar?.length ?? 0) > 0 && (
          <FormSection baslik={`Yönlendirildi (${detay.data!.paylasimlar!.length})`}>
            <ul className="divide-y divide-line">
              {detay.data!.paylasimlar!.map((p) => (
                <li key={p.id} className="flex items-start gap-2 py-2 first:pt-0 last:pb-0">
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm">{p.aliciAd}</span>
                    <span className="block text-2xs text-text-3">
                      {p.paylasanAd} · {dateTime(p.tarih)}
                    </span>
                    {p.not && (
                      <span className="mt-0.5 block text-2xs leading-[1.45] text-text-2 wrap-anywhere">
                        {p.not}
                      </span>
                    )}
                  </span>
                  {p.goruntulemeTarihi ? (
                    <span
                      className="mt-0.5 inline-flex flex-none items-center gap-1 text-2xs text-(--st-ok)"
                      title={`Görüntülendi: ${dateTime(p.goruntulemeTarihi)}`}
                    >
                      <Check size={12} strokeWidth={2.4} />
                      Görüldü
                    </span>
                  ) : (
                    <span className="mt-0.5 flex-none text-2xs text-text-3">Bekliyor</span>
                  )}
                </li>
              ))}
            </ul>
          </FormSection>
        )
      )}
    </FormModal>
  );
}

/** Telefon/e-posta satırı — değer solda, eylem sağda tek dokunuş. */
function IletisimSatiri({
  ikon,
  deger,
  yol,
  eylem,
}: {
  ikon: React.ReactNode;
  deger: string;
  yol?: string;
  eylem?: string;
}) {
  return (
    <li className="flex items-center gap-2.5 py-2 first:pt-0 last:pb-0">
      <span className="flex-none text-text-3">{ikon}</span>
      <span className="min-w-0 flex-1 truncate text-sm tabular-nums text-text-2">{deger}</span>
      {yol && eylem && (
        <a
          href={yol}
          className="flex-none rounded-control border border-border bg-surface px-2.5 py-1 text-2xs font-medium text-brand transition-colors hover:bg-surface-2"
        >
          {eylem}
        </a>
      )}
    </li>
  );
}
