import { AlertTriangle, ClipboardCheck, Clock, MapPin, PlusCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useSession } from '../../auth/SessionProvider';
import { Button } from '../../components/Button';
import { ColoredBadge } from '../../components/Color';
import { EmptyState } from '../../components/EmptyState';
import { PERMISSION } from '../../components/permissions';
import { SkeletonRows } from '../../components/Skeleton';
import { cn } from '../../components/utils';
import { useMyFieldWork } from '../../data/citizen';
import type { TaskSummary } from '../../data/types';
import { StageProgress } from '../task/TaskBits';

/**
 * SAHA — "İŞLERİM".
 *
 * <p>
 * Sahadaki kişinin sorduğu tek soru: <b>şimdi hangisini yapacağım?</b> Ekran
 * o soruya cevap veriyor — geciken işler ayrı bir öbekte en üstte, geri
 * kalanı en az vakti kalan önce.
 * </p>
 *
 * <p>
 * <b>Sıralama tarihe göre DEĞİL.</b> Açılış tarihi sahadaki birine hiçbir şey
 * söylemiyor; kalan süre söylüyor.
 * </p>
 *
 * <p>
 * <b>ROLE GÖRE İKİ AYRI EKRAN, tek dosyada.</b> Yalnızca tespit girmesi
 * beklenen personelin (görev yürütmeyen) üzerinde hiçbir zaman iş olmuyor;
 * ona boş bir liste ve "açık işiniz yok" göstermek, uygulamanın bozuk
 * olduğunu düşündürüyordu. Onun gördüğü şey doğrudan tespit girişi.
 * </p>
 */
export default function FieldHome() {
  const { data: isler, isLoading } = useMyFieldWork();
  const { hasPermission } = useSession();

  const tespitGirebilir = hasPermission([PERMISSION.sahaTespit, PERMISSION.gorevEkle]);

  /*
    "İŞ YÜRÜTEN PERSONEL" ÖLÇÜTÜ: aşama ilerletme izni.

    Görev görüntüleme herkeste var (saha kabuğuna girebilmek için gerekiyor);
    ayırt edici olan, bir aşamayı tamamlayıp kanıt yükleyebilmek.
  */
  const isYurutur = hasPermission(PERMISSION.gorevAsama);

  const liste = [...(isler ?? [])];
  const gecikenler = liste.filter((g) => g.gecikti);
  const otekiler = liste.filter((g) => !g.gecikti);

  // Sadece TESPİT girenler için: liste yerine doğrudan iş.
  if (!isYurutur && tespitGirebilir) {
    return (
      <div className="flex min-h-[60dvh] flex-col items-center justify-center gap-5 text-center">
        <span className="grid h-20 w-20 place-items-center rounded-full bg-brand-soft text-brand">
          <MapPin size={34} />
        </span>
        <div>
          <h1 className="font-display text-xl font-bold text-ink">Sahada bir sorun mu var?</h1>
          <p className="mt-1 text-sm text-text-2">
            Konumu ve fotoğrafıyla bildirin; ilgili birime görev olarak düşsün.
          </p>
        </div>
        <Link to="/saha/tespit" className="w-full max-w-xs">
          {/* 56px: tek elle, bakmadan basılan düğme. */}
          <Button className="h-14 w-full text-base">
            <PlusCircle size={20} />
            Tespit gir
          </Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {isLoading ? (
        <SkeletonRows adet={4} />
      ) : liste.length === 0 ? (
        <EmptyState
          ikon={ClipboardCheck}
          baslik="Açık işiniz yok"
          aciklama="Size ya da ekibinize atanan bir görev bulunmuyor."
          eylem={
            tespitGirebilir ? (
              <Link to="/saha/tespit">
                <Button varyant="ikincil" className="h-12">
                  <PlusCircle size={18} />
                  Tespit gir
                </Button>
              </Link>
            ) : undefined
          }
        />
      ) : (
        <>
          {gecikenler.length > 0 && (
            <section>
              {/*
                GECİKENLER AYRI BİR ÖBEKTE, sadece kırmızı bir rozetle değil.
                Rozet, uzun bir listede sıradaki satırlardan biri olarak
                kayboluyordu; ayrı başlık "önce bunlar" diyor.
              */}
              <h2 className="mb-2 flex items-center gap-1.5 px-1 text-2xs font-semibold uppercase tracking-[0.1em] text-danger">
                <AlertTriangle size={13} />
                Süresi geçti · {gecikenler.length}
              </h2>
              <ul className="space-y-2">
                {gecikenler.map((g) => (
                  <IsKarti key={g.id} is={g} gecikti />
                ))}
              </ul>
            </section>
          )}

          {otekiler.length > 0 && (
            <section>
              <h2 className="mb-2 px-1 text-2xs font-semibold uppercase tracking-[0.1em] text-ink-3">
                Üzerimdeki işler · {otekiler.length}
              </h2>
              <ul className="space-y-2">
                {otekiler.map((g) => (
                  <IsKarti key={g.id} is={g} />
                ))}
              </ul>
            </section>
          )}
        </>
      )}
    </div>
  );
}

/**
 * Tek iş satırı.
 *
 * <p>
 * Kart değil <b>düğme gibi</b> davranıyor: bütün yüzey dokunulabilir ve
 * basılınca zemin koyulaşıyor. Sahada küçük bir hedefe nişan almak, yanlış
 * işi açmak demek.
 * </p>
 *
 * <p>
 * <b>Yön oku YOK.</b> Kartın tamamı zaten dokunulabilir; sağ kenardaki ok
 * hem yer kaplıyor hem de "asıl düğme burası" diye yanlış bir hedef
 * gösteriyordu.
 * </p>
 */
function IsKarti({ is: g, gecikti }: { is: TaskSummary; gecikti?: boolean }) {
  const asamali = (g.asamaToplam ?? 0) > 0;

  return (
    <li>
      <Link
        to={`/saha/gorev/${g.id}`}
        className={cn(
          'block rounded-card border bg-surface p-4 transition-colors active:bg-sunken',
          gecikti ? 'border-danger/35' : 'border-line',
        )}
      >
        {/*
          BAŞLIK EN ÜSTTE.

          Önce durum rozeti kendi satırında en üstteydi ve süre verisi
          olmayan işlerde (SLA'sı olmayan tip) satırın sağı tamamen boş
          kalıyordu — kartın en değerli yeri tek bir pula harcanıyordu.
          Üstelik sahadaki kişi listeyi BAŞLIKLARA bakarak tarıyor; durum,
          işi bulduktan sonra bakılan ikinci bilgi.
        */}
        <div className="flex items-start gap-3">
          {/* 17px: hareket hâlinde, kolun ucundaki telefondan okunuyor. */}
          <p className="min-w-0 flex-1 line-clamp-2 text-[17px] font-semibold leading-snug text-ink">
            {g.baslik}
          </p>

          {gecikti ? (
            <span className="flex flex-none items-center gap-1 rounded-pill bg-danger-soft px-2 py-1 text-2xs font-bold text-danger">
              <AlertTriangle size={12} />
              Gecikti
            </span>
          ) : (
            g.kalanSaat != null && (
              <span className="flex flex-none items-center gap-1 py-1 text-2xs font-semibold tabular-nums text-text-2">
                <Clock size={12} className="text-ink-3" />
                {g.kalanSaat} sa
              </span>
            )
          )}
        </div>

        {/*
          KÜNYE SATIRI: durum, numara ve adres YAN YANA.

          Üçü de "işi tanımlayan ama aramayan" bilgiler; ayrı satırlara
          bölmek kartı iki katına çıkarıyor ve hiçbirini daha okunur
          yapmıyordu.
        */}
        <div className="mt-2 flex min-w-0 items-center gap-2 text-xs text-text-3">
          <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />
          <span className="font-mono tabular-nums">{g.takipNo}</span>
          {g.adres && (
            <>
              <MapPin size={12} className="shrink-0" />
              <span className="truncate">{g.adres}</span>
            </>
          )}
        </div>

        {/*
          Aşama ilerlemesi TAM GENİŞLİK: rozetin yanında sıkışmış bir
          parçayken "1/3" okunmuyordu. İşin ne kadarının bittiğini söyleyen
          tek şey bu.
        */}
        {asamali && (
          <div className="mt-3 border-t border-line pt-3">
            <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} ilerleme={g.ilerleme} />
          </div>
        )}
      </Link>
    </li>
  );
}
