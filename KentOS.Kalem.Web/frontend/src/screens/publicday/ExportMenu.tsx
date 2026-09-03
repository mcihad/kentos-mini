import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import {
  ChevronDown,
  ClipboardCheck,
  FileSpreadsheet,
  FileText,
  ListOrdered,
  Printer,
  RectangleHorizontal,
  RectangleVertical,
  SquarePen,
} from 'lucide-react';
import { useState } from 'react';
import { Button } from '../../components/Button';
import { cn } from '../../components/utils';
import { useIsDesktop } from '../../components/screenSize';
import { download } from '../../data/download';
import { BottomSheet, SheetHeading, SheetRow } from '../../shell/mobile/BottomSheet';

/**
 * Halk günü çıktıları.
 *
 * <p>
 * Üç ayrı kâğıt, çünkü üç ayrı iş var:
 * </p>
 * <ul>
 *   <li><b>Program</b> — gün başlamadan elden ele dolaşır (kapı, salon,
 *       makam). Yalnızca sıra, saat, telefon, ad ve konu.</li>
 *   <li><b>Katılım çizelgesi</b> — salonda elle işaretlenir; son iki sütun
 *       boş kutu.</li>
 *   <li><b>Sonuç raporu</b> — gün bittikten sonra Özel Kalem'in masasında:
 *       durum, görüşme notu, takip işareti.</li>
 * </ul>
 *
 * <p>
 * Her biri hem <b>Excel</b> hem <b>PDF</b>: Excel süzülüp düzenlenmek,
 * PDF basılıp dağıtılmak için. Tek biçim bırakmak, ikisinden birini her
 * seferinde elle dönüştürmek demekti.
 * </p>
 *
 * <p>
 * <code>dilimId</code> verilirse çıktı YALNIZCA o grubu içerir — kapıdaki
 * görevlinin elindeki kâğıt bütün günü değil, o saatteki grubu gösteriyor.
 * </p>
 */
const TURLER = [
  {
    deger: 0,
    ad: 'Program',
    aciklama: 'Sıra · Saat · Telefon · Ad · Konu',
    ikon: ListOrdered,
  },
  {
    deger: 2,
    ad: 'Katılım çizelgesi',
    aciklama: 'Salonda elle işaretlemek için boş kutulu',
    ikon: ClipboardCheck,
  },
  {
    deger: 1,
    ad: 'Sonuç raporu',
    aciklama: 'Durum, görüşme notu ve takip işaretiyle',
    ikon: SquarePen,
  },
] as const;

export function ExportMenu({
  halkGunuId,
  dilimId,
  etiket = 'Çıktı',
  varyant = 'ikincil',
  acik,
  kapat,
}: {
  halkGunuId: number;
  /** Verilirse çıktı yalnızca bu grubu içerir. */
  dilimId?: number;
  etiket?: string;
  varyant?: 'birincil' | 'ikincil' | 'sade';
  /**
   * DIŞARIDAN AÇILAN MOBİL TABAKA.
   *
   * Verilirse kendi düğmesini çizmez; kap dışarıdan (FAB eylem tabakasından)
   * açılır. Mobilde çıktı almak artık üst şeritteki bir menüden değil,
   * "Halk günü işlemleri" tabakasındaki "Çıktılar" satırından geçiyor.
   */
  acik?: boolean;
  kapat?: () => void;
}) {
  /**
   * KÂĞIT YÖNÜ. Konu ve görüşme notu uzun olduğunda dikey A4'te sütunlar
   * sıkışıp metin alt alta kırpılıyordu; yatay sayfa aynı listeye nefes
   * aldırıyor. Seçim menü açıkken korunur — arka arkaya iki çıktı almak
   * yaygın.
   */
  const [yatay, setYatay] = useState(false);
  const masaustu = useIsDesktop();
  /** Kendi tetikleyicisiyle açılan mobil tabaka (dışarıdan kontrol yoksa). */
  const [kendiTabaka, setKendiTabaka] = useState(false);
  const disaridanKap = acik !== undefined;
  // Mobilde her durumda tabaka: dışarıdan kontrol varsa onunla, yoksa kendi
  // düğmesiyle. Açılır menü telefonda 290px'lik bir ızgaraya dönüşüyordu.
  const tabakaKipi = disaridanKap || !masaustu;

  const sorgu = (tur: number) =>
    `tur=${tur}${dilimId ? `&dilimId=${dilimId}` : ''}${yatay ? '&yatay=true' : ''}`;

  /*
    MOBİLDE AÇILIR MENÜ DEĞİL TABAKA: menü 290px genişlikte ve içinde
    yön seçimi + üç tür × iki biçim var; telefonda 12px'lik etiketlerle
    avlanamayan bir ızgaraya dönüşüyordu.
  */
  if (tabakaKipi) {
    const kapali = () => (disaridanKap ? kapat?.() : setKendiTabaka(false));
    return (
      <>
      {/* Dışarıdan açılmıyorsa kendi düğmesini çizer. */}
      {!disaridanKap && (
        <button
          type="button"
          onClick={() => setKendiTabaka(true)}
          aria-label={etiket}
          title={etiket}
          className="grid h-9 w-9 place-items-center rounded-sm text-ink-2 active:bg-sunken"
        >
          <Printer size={16} />
        </button>
      )}
      <BottomSheet
        acik={disaridanKap ? (acik ?? false) : kendiTabaka}
        kapat={kapali}
        baslik="Çıktılar"
        aciklama={dilimId ? 'Yalnızca bu grup' : 'Günün tamamı'}
      >
        <SheetHeading>Kâğıt yönü</SheetHeading>
        <div className="mb-3 flex gap-1 rounded-sm bg-sunken p-1">
          <button
            type="button"
            onClick={() => setYatay(false)}
            className={cn(
              'h-ctrl flex flex-1 items-center justify-center gap-1.5 rounded-xs text-xs font-semibold',
              !yatay ? 'bg-surface text-ink shadow-1' : 'text-ink-3',
            )}
          >
            <RectangleVertical size={14} /> Dikey
          </button>
          <button
            type="button"
            onClick={() => setYatay(true)}
            className={cn(
              'h-ctrl flex flex-1 items-center justify-center gap-1.5 rounded-xs text-xs font-semibold',
              yatay ? 'bg-surface text-ink shadow-1' : 'text-ink-3',
            )}
          >
            <RectangleHorizontal size={14} /> Yatay
          </button>
        </div>

        {TURLER.map((tur) => (
          <div key={tur.deger} className="mb-3">
            <SheetHeading>{tur.ad}</SheetHeading>
            <p className="px-1 pb-1.5 text-2xs leading-[1.4] text-ink-3">{tur.aciklama}</p>
            <SheetRow
              ikon={<FileSpreadsheet size={17} />}
              okYok
              onClick={() => {
                kapali();
                download(`/halk-gunu/${halkGunuId}/excel?${sorgu(tur.deger)}`);
              }}
            >
              Excel download
            </SheetRow>
            <SheetRow
              ikon={<FileText size={17} />}
              okYok
              onClick={() => {
                kapali();
                download(`/halk-gunu/${halkGunuId}/pdf?${sorgu(tur.deger)}`);
              }}
            >
              PDF download
            </SheetRow>
          </div>
        ))}
      </BottomSheet>
      </>
    );
  }

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <Button varyant={varyant}>
          <Printer size={14} />
          {etiket}
          <ChevronDown size={13} className="opacity-70" />
        </Button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align="end"
          sideOffset={6}
          className="katman anim-menu z-menu w-[290px] rounded-card border border-border bg-surface p-1 shadow-3"
        >
          <p className="px-2.5 py-1.5 text-2xs font-semibold uppercase tracking-wider text-text-3">
            {dilimId ? 'Bu grup için' : 'Günün tamamı için'}
          </p>

          {/* Yön seçimi hem Excel hem PDF için geçerli. */}
          <div
            className="mx-1 mb-1 flex gap-1 rounded-control bg-sunken p-1"
            // Menü, içindeki düğmeye basınca kapanmasın: kullanıcı önce yönü
            // seçip sonra biçime basıyor.
            onKeyDown={(e) => e.stopPropagation()}
          >
            <YonDugmesi
              secili={!yatay}
              ikon={<RectangleVertical size={13} />}
              etiket="Dikey"
              tikla={() => setYatay(false)}
            />
            <YonDugmesi
              secili={yatay}
              ikon={<RectangleHorizontal size={13} />}
              etiket="Yatay"
              tikla={() => setYatay(true)}
            />
          </div>

          {TURLER.map((t) => (
            <div key={t.deger} className="px-1 py-1">
              <p className="px-1.5 text-sm font-medium">{t.ad}</p>
              <p className="px-1.5 pb-1.5 text-xs leading-[1.35] text-text-3">
                {t.aciklama}
              </p>
              <div className="flex gap-1">
                <Bicim
                  ikon={<FileSpreadsheet size={13} />}
                  etiket="Excel"
                  tikla={() =>
                    download(`/halk-gunu/${halkGunuId}/excel?${sorgu(t.deger)}`)
                  }
                />
                <Bicim
                  ikon={<FileText size={13} />}
                  etiket="PDF"
                  tikla={() =>
                    download(`/halk-gunu/${halkGunuId}/pdf?${sorgu(t.deger)}`)
                  }
                />
              </div>
            </div>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}

/** Kâğıt yönü seçimi — menüyü KAPATMAZ (`onSelect` değil `onClick`). */
function YonDugmesi({
  secili,
  ikon,
  etiket,
  tikla,
}: {
  secili: boolean;
  ikon: React.ReactNode;
  etiket: string;
  tikla: () => void;
}) {
  return (
    <button
      type="button"
      onClick={tikla}
      aria-pressed={secili}
      className={cn(
        'flex flex-1 items-center justify-center gap-1.5 rounded-sm px-2 py-1.5 text-xs transition-colors',
        secili
          ? 'bg-surface font-semibold text-text shadow-1'
          : 'text-text-3 hover:text-text',
      )}
    >
      {ikon}
      {etiket}
    </button>
  );
}

function Bicim({
  ikon,
  etiket,
  tikla,
}: {
  ikon: React.ReactNode;
  etiket: string;
  tikla: () => void;
}) {
  return (
    <DropdownMenu.Item
      onSelect={tikla}
      className="flex flex-1 cursor-pointer items-center justify-center gap-1.5 rounded-control border border-border
        px-2 py-1.5 text-sm outline-hidden transition-colors data-highlighted:border-brand-2
        data-highlighted:bg-brand-tint"
    >
      {ikon}
      {etiket}
    </DropdownMenu.Item>
  );
}
