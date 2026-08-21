import { ChevronRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import { EmptyState } from '../../components/EmptyState';
import { BarChart3 } from 'lucide-react';
import { useSession } from '../../auth/SessionProvider';
import { STAT_GROUPS, type StatTopic } from './catalog';

/**
 * İSTATİSTİK MERKEZİ — gruplu kart ızgarası.
 *
 * <p>
 * Bütün panolar bir dönem TEK sayfada, iki segment düğmesinin arkasındaydı.
 * Konu sayısı ikiden dokuza çıkınca o düzen taşıdı: segment şeridi
 * mobilde satır kaydırmaya başlıyor ve kullanıcı hangi panoların var
 * olduğunu ancak şeridi kaydırarak görebiliyordu. Izgara, VAR OLAN her
 * konuyu tek bakışta gösteriyor.
 * </p>
 *
 * <p>
 * <b>Kart bir bağlantı, içinde düğme YOK.</b> Bütün kart tıklanabilir
 * olduğu için içine ikinci bir etkileşimli öğe konsaydı iç içe bağlantı
 * olurdu — depoda ölçülmüş ve düzeltilmiş bir hata (bkz. frontend/CLAUDE.md
 * "İÇ İÇE BAĞLANTI OLMAZ").
 * </p>
 */
export default function StatisticsHub() {
  const { me, hasPermission } = useSession();

  /*
    İZİN LİSTESİ YOKSA HEPSİ GÖSTERİLİR.

    Menü süzgeciyle aynı geri düşüş: izin listesi göndermeyen bir sunucuya
    bağlanıldığında (eski sürüm) kartları gizlemek, çalışan bir ekranı
    boşaltırdı. Asıl kapı zaten sunucuda — kart açılsa bile uç 403 döner.
  */
  const izinListesiVar = (me?.izinler?.length ?? 0) > 0;
  const gorunur = (k: StatTopic) => !izinListesiVar || hasPermission(k.izin);

  const gruplar = STAT_GROUPS
    .map((g) => ({ ...g, konular: g.konular.filter(gorunur) }))
    .filter((g) => g.konular.length > 0);

  if (gruplar.length === 0) {
    return (
      <EmptyState
        ikon={BarChart3}
        baslik="Görüntüleyebileceğiniz istatistik yok"
        aciklama="İstatistikler modül izinlerine bağlı; yetkiniz genişletildiğinde burada görünürler."
      />
    );
  }

  return (
    <div className="space-y-6 md:space-y-7">
      <header>
        <h1 className="font-display text-2xl font-extrabold tracking-[-0.02em]">
          İstatistikler ve Raporlar
        </h1>
        <p className="mt-1 text-sm font-medium text-text-3">
          Bir başlık seçin; o konunun sayıları ve çıktıları açılır.
        </p>
      </header>

      {gruplar.map((grup) => (
        <section key={grup.baslik} className="space-y-2.5">
          {/* Grup başlığı ince bir çizgiyle uzuyor: kartların arasındaki
              boşluk tek başına gruplamayı taşımıyordu. */}
          <div className="flex items-center gap-3">
            <h2 className="shrink-0 text-2xs font-bold uppercase tracking-[0.08em] text-text-3">
              {grup.baslik}
            </h2>
            <span className="h-px flex-1 bg-border" aria-hidden />
          </div>

          <ul className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
            {grup.konular.map((k) => (
              <li key={k.konu}>
                <KonuKarti konu={k} />
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function KonuKarti({ konu }: { konu: StatTopic }) {
  const Ikon = konu.ikon;

  return (
    <Link
      to={konu.disRota ?? `/istatistikler/${konu.konu}`}
      className="group flex h-full min-w-0 items-start gap-3 rounded-card border border-border
                 bg-surface p-3.5 shadow-1 transition-[border-color,box-shadow,transform]
                 duration-150 hover:border-brand hover:shadow-2 active:scale-[0.98]"
    >
      <span
        className="grid size-11 shrink-0 place-items-center rounded-md bg-brand-soft text-brand"
        aria-hidden
      >
        <Ikon size={20} />
      </span>

      <span className="min-w-0 flex-1">
        <span className="block font-display text-[15px] font-bold leading-tight">
          {konu.baslik}
        </span>
        {/* Açıklama SARILIR, kırpılmaz: kartın işi neyi bulacağını
            söylemek ve yarım cümle bunu yapmıyor. */}
        <span className="mt-1 block text-xs font-medium leading-snug text-text-3">
          {konu.aciklama}
        </span>
      </span>

      <ChevronRight
        size={16}
        className="mt-0.5 shrink-0 text-text-3 transition-transform duration-150
                   group-hover:translate-x-0.5 group-hover:text-brand"
        aria-hidden
      />
    </Link>
  );
}
