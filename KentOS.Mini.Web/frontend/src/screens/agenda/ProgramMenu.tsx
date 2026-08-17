import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import {
  ChevronDown, FileSpreadsheet, FileText, LayoutGrid, LayoutList, NotebookPen,
  Printer, Rows3,
} from 'lucide-react';
import { useState } from 'react';
import { Button } from '../../components/Button';
import { useIsDesktop } from '../../components/screenSize';
import { BottomSheet, SheetDivider, SheetHeading, SheetRow } from '../../shell/mobile/BottomSheet';
import { useToast } from '../../components/Toast';
import { download } from '../../data/download';
import { tokenStore, queryString } from '../../data/client';

/**
 * Günlük program çıktı tasarımları.
 *
 * <p>
 * Sayısal değerler sunucudaki <c>ProgramTasarimi</c> ile birebir; sıra
 * değişirse "standart" istenip "boş not sayfası" basılır.
 * </p>
 */
const TASARIMLAR = [
  { deger: 1, ad: 'Standart', aciklama: 'Saat · Konu · Yer tablosu', ikon: LayoutGrid },
  { deger: 2, ad: 'Kompakt', aciklama: 'Büyük punto, uzaktan okunur', ikon: Rows3 },
  { deger: 3, ad: 'Detaylı', aciklama: 'İrtibat ve hazırlık bilgileriyle', ikon: LayoutList },
  { deger: 5, ad: 'Saat şeridi', aciklama: 'Dikey zaman çizgisi', ikon: LayoutList },
  { deger: 6, ad: 'Pano', aciklama: 'Duvara asılabilir, çok büyük punto', ikon: LayoutGrid },
  { deger: 4, ad: 'Boş not sayfası', aciklama: 'Yanında el yazısı alanı', ikon: NotebookPen },
] as const;

/**
 * Çıktı menüsü — günlük program tasarımları + liste dışa aktarımı.
 *
 * <p>
 * İki çıkış yolu var: <b>Yazdır</b> tarayıcıda HTML önizleme açar (kâğıda
 * basmadan önce görülür, yazıcı ayarları kullanıcının), <b>PDF</b> dosyayı
 * indirir (e-postayla göndermek için). Eskisinde yalnızca HTML vardı ve
 * paylaşmak için ekran görüntüsü alınıyordu.
 * </p>
 *
 * <p>
 * Liste dışa aktarımı (Excel/PDF) de <b>buraya taşındı</b>. Önce araç
 * çubuğunda ayrı bir satırda üç düğme daha duruyordu; ajanda ekranı
 * sekmeler, arama, gezinme, çıktılar ve tip çipleriyle <b>dört sıra</b>
 * denetimle açılıyor, asıl liste ekranın altına itiliyordu. Hepsi tek bir
 * yazıcı düğmesinin altında: çıktı almak günde bir yapılan bir iş, her an
 * görünür durmasına gerek yok.
 * </p>
 */
export function ProgramMenu({
  tarih,
  excel,
  pdf,
  tetikleyici,
}: {
  tarih: string;
  /** Liste dışa aktarımı — verilmezse o bölüm hiç çizilmez. */
  excel?: () => void;
  pdf?: () => void;
  /**
   * Düğmenin kendisini çağıran verir.
   *
   * Mobilde yazıcı, arama kutusuyla TEK bir kontrolün içinde duruyor:
   * ortak kenarlık, aralarında saç teli. Kendi kenarlıklı düğmesi orada
   * "yan yana iki kutu" görüntüsü veriyordu.
   */
  tetikleyici?: React.ReactNode;
}) {
  const { bildir } = useToast();
  const masaustu = useIsDesktop();
  const [tabaka, setTabaka] = useState(false);
  const [tabakaTasarim, setTabakaTasarim] = useState<number | null>(null);

  /**
   * HTML önizlemeyi yeni sekmede açar.
   *
   * Jeton `Authorization` başlığıyla gitmek zorunda; `window.open` başlık
   * gönderemez. Bu yüzden içerik `fetch` ile alınıp `blob:` URL olarak
   * açılıyor — jeton adres çubuğuna hiç düşmüyor.
   */
  async function yazdir(tasarim: number) {
    try {
      const jeton = tokenStore.read();
      const yanit = await fetch(
        `/api/v2/disa-aktar/gunluk-program/html${queryString({ tarih, tasarim })}`,
        { headers: jeton ? { Authorization: `Bearer ${jeton.jeton}` } : {} },
      );

      if (!yanit.ok) throw new Error(`Sunucu ${yanit.status} döndü.`);

      const html = await yanit.text();
      const pencere = window.open('', '_blank');
      if (!pencere) {
        bildir('uyari', 'Açılır pencere engellendi', 'Tarayıcı ayarlarından izin verin.');
        return;
      }
      pencere.document.write(html);
      pencere.document.close();
    } catch (h) {
      bildir('hata', 'Program açılamadı', (h as Error).message);
    }
  }

  async function pdfIndir(tasarim: number) {
    try {
      await download('/disa-aktar/gunluk-program', { tarih, tasarim });
    } catch (h) {
      bildir('hata', 'PDF indirilemedi', (h as Error).message);
    }
  }

  const varsayilanTetikleyici = (
    <Button varyant="ikincil" className="h-9 shrink-0 px-2.5" title="Çıktılar">
      <Printer size={15} />
      <span className="hidden text-sm sm:inline">Çıktılar</span>
      <ChevronDown size={12} className="text-text-3" />
    </Button>
  );

  /*
    MOBİLDE AÇILIR MENÜ DEĞİL, ALT TABAKA.

    Menü iki kademeliydi: tasarım seç → yanında açılan alt menüden "Yazdır"
    ya da "PDF". Alt menü telefonda EKRANIN DIŞINA taşıyordu ve düzeltilemez:
    Radix iç içe menüyü yalnızca sağdan sola çeviriyor, yatayda görünür alana
    kaydırmıyor. Ana menü 290px genişlikte ve x=83'ten başlıyor; 180px'lik
    alt menü sola çevrilince -96'ya düşüyor.

    Tabakada kademe zaten gerekmiyor: önce tasarım satırları, seçilince aynı
    tabaka "Yazdır / PDF" ikilisine dönüşüyor. Hedefler 56px ve hiçbir şey
    ekran dışına çıkamıyor.
  */
  if (!masaustu) {
    const secili = TASARIMLAR.find((x) => x.deger === tabakaTasarim);

    return (
      <>
        {/*
          `contents`: sarmalayıcı DÜZENE GİRMEZ.

          Düz bir `<span>` esnek kabın öğesi oluyor ve içindeki düğme
          kabın yüksekliğine gerilemiyordu — yazıcı, arama alanıyla aynı
          kutunun içinde olmasına rağmen kısa kalıyor ve hizası bozuluyordu.
          `display: contents` ile düğme doğrudan kabın öğesi oluyor; tık
          zaten çocuktan kabarıyor.
        */}
        <span className="contents" onClick={() => setTabaka(true)}>
          {tetikleyici ?? varsayilanTetikleyici}
        </span>

        <BottomSheet
          acik={tabaka}
          kapat={() => {
            setTabaka(false);
            setTabakaTasarim(null);
          }}
          baslik={secili ? secili.ad : 'Çıktılar'}
          aciklama={secili ? secili.aciklama : 'Günlük program ve liste çıktıları'}
        >
          {secili ? (
            <>
              <SheetRow
                ikon={<Printer size={17} />}
                okYok
                onClick={() => {
                  setTabaka(false);
                  setTabakaTasarim(null);
                  void yazdir(secili.deger);
                }}
              >
                Yazdır / önizle
              </SheetRow>
              <SheetRow
                ikon={<FileText size={17} />}
                okYok
                onClick={() => {
                  setTabaka(false);
                  setTabakaTasarim(null);
                  void pdfIndir(secili.deger);
                }}
              >
                PDF download
              </SheetRow>
              <SheetDivider />
              <SheetRow ikon={<ChevronDown size={17} />} okYok onClick={() => setTabakaTasarim(null)}>
                Başka tasarım seç
              </SheetRow>
            </>
          ) : (
            <>
              <SheetHeading>Günlük program</SheetHeading>
              {TASARIMLAR.map((d) => (
                <SheetRow
                  key={d.deger}
                  ikon={<d.ikon size={17} />}
                  onClick={() => setTabakaTasarim(d.deger)}
                >
                  {d.ad}
                </SheetRow>
              ))}

              {(excel || pdf) && (
                <>
                  <SheetDivider />
                  <SheetHeading>Liste</SheetHeading>
                  {excel && (
                    <SheetRow
                      ikon={<FileSpreadsheet size={17} />}
                      okYok
                      onClick={() => {
                        setTabaka(false);
                        excel();
                      }}
                    >
                      Excel olarak download
                    </SheetRow>
                  )}
                  {pdf && (
                    <SheetRow
                      ikon={<FileText size={17} />}
                      okYok
                      onClick={() => {
                        setTabaka(false);
                        pdf();
                      }}
                    >
                      PDF olarak download
                    </SheetRow>
                  )}
                </>
              )}
            </>
          )}
        </BottomSheet>
      </>
    );
  }

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        {tetikleyici ?? varsayilanTetikleyici}
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          /*
            `align="end"` + çarpışma payı: düğme mobilde ekranın SAĞ ucunda ve
            menü 290px. Başlangıca hizalanınca ekran dışına taşıyordu. Sona
            hizalanıp 8px pay bırakınca Radix menüyü görünür alana sıkıştırıyor.
          */
          align="end"
          sideOffset={6}
          collisionPadding={8}
          className="katman anim-katman z-400 w-[min(290px,calc(100vw-16px))] overflow-hidden rounded-card border border-border bg-surface p-1 shadow-3"
        >
          <DropdownMenu.Label className="px-2.5 py-1.5 text-2xs font-semibold uppercase tracking-[0.06em] text-text-3">
            Günlük program
          </DropdownMenu.Label>

          {TASARIMLAR.map((t) => (
            <DropdownMenu.Sub key={t.deger}>
              <DropdownMenu.SubTrigger className="flex w-full cursor-default items-center gap-2.5 rounded-sm px-2.5 py-2 text-left outline-hidden data-highlighted:bg-surface-2">
                <t.ikon size={15} className="shrink-0 text-text-3" strokeWidth={1.8} />
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-medium">{t.ad}</span>
                  <span className="block truncate text-xs text-text-3">{t.aciklama}</span>
                </span>
                <ChevronDown size={13} className="-rotate-90 text-text-3" />
              </DropdownMenu.SubTrigger>

              <DropdownMenu.Portal>
                <DropdownMenu.SubContent
                  sideOffset={4}
                  /*
                    ALT MENÜ EKRANIN DIŞINA TAŞIYORDU.

                    Ana menü sağa hizalı ve telefonda neredeyse tam genişlik;
                    alt menü varsayılan olarak onun SAĞINA açılıyor, yer
                    kalmayınca Radix sola çeviriyor ve 180px'lik kutu ekranın
                    sol kenarından dışarı çıkıyordu. `collisionPadding` ile
                    görünür alana sıkıştırılıyor, `avoidCollisions` zaten açık;
                    genişlik de ekranla sınırlandı.
                  */
                  collisionPadding={8}
                  className="katman anim-katman z-400 w-[min(180px,calc(100vw-16px))] overflow-hidden rounded-card border border-border bg-surface p-1 shadow-3"
                >
                  <DropdownMenu.Item
                    onSelect={() => void yazdir(t.deger)}
                    className="flex cursor-default items-center gap-2 rounded-sm px-2.5 py-2 text-sm outline-hidden data-highlighted:bg-surface-2"
                  >
                    <Printer size={14} className="text-text-3" />
                    Yazdır / önizle
                  </DropdownMenu.Item>
                  <DropdownMenu.Item
                    onSelect={() => void pdfIndir(t.deger)}
                    className="flex cursor-default items-center gap-2 rounded-sm px-2.5 py-2 text-sm outline-hidden data-highlighted:bg-surface-2"
                  >
                    <FileText size={14} className="text-text-3" />
                    PDF download
                  </DropdownMenu.Item>
                </DropdownMenu.SubContent>
              </DropdownMenu.Portal>
            </DropdownMenu.Sub>
          ))}

          {(excel || pdf) && (
            <>
              <DropdownMenu.Separator className="my-1 h-px bg-border" />
              <DropdownMenu.Label className="px-2.5 py-1.5 text-2xs font-semibold uppercase tracking-[0.06em] text-text-3">
                Görünen liste
              </DropdownMenu.Label>
              {excel && (
                <DropdownMenu.Item
                  onSelect={excel}
                  className="flex cursor-default items-center gap-2.5 rounded-sm px-2.5 py-2 text-sm outline-hidden data-highlighted:bg-surface-2"
                >
                  <FileSpreadsheet size={15} className="text-text-3" strokeWidth={1.8} />
                  Excel olarak download
                </DropdownMenu.Item>
              )}
              {pdf && (
                <DropdownMenu.Item
                  onSelect={pdf}
                  className="flex cursor-default items-center gap-2.5 rounded-sm px-2.5 py-2 text-sm outline-hidden data-highlighted:bg-surface-2"
                >
                  <FileText size={15} className="text-text-3" strokeWidth={1.8} />
                  PDF olarak download
                </DropdownMenu.Item>
              )}
            </>
          )}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
