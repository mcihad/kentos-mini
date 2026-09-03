import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { FileSpreadsheet, FileText } from 'lucide-react';
import { cn } from './utils';

/**
 * Excel / PDF çıktı düğmeleri — birleşik ikili.
 *
 * <p>
 * Önceden iki ayrı "sade" düğmeydi ve araç çubuğunun ikinci bir satırında
 * duruyorlardı; o satır arşiv, harita ve halk günleri düğmeleriyle birlikte
 * ekranın en kalabalık yeriydi. İkisi tek bir kabukta birleştirilince hem
 * yer açıldı hem de <b>aynı işin iki biçimi</b> oldukları görünür oldu.
 * </p>
 *
 * <p>
 * Yükseklik bölümlü seçimle (36px) aynı: yan yana durduklarında hizasız iki
 * kutu, araç çubuğunu dağınık gösteriyordu.
 * </p>
 *
 * <p>
 * <b>Etiketler her boyutta yazılı.</b> Dar ekranda yalnızca ikon
 * bırakılmıştı ama iki ikon da "belge" silüeti; yan yana durduklarında
 * hangisinin Excel hangisinin PDF olduğu anlaşılmıyordu. Yer açmak için
 * onun yerine ikon dar ekranda gizleniyor: yazı ayırt edici, ikon değil.
 * </p>
 */
export function ExportButtons({
  excel,
  pdf,
  className,
  tetikleyici,
}: {
  excel: () => void;
  /**
   * PDF çıktısı — YOKSA tek düğme çizilir.
   *
   * <p>
   * Görev, proje, özgeçmiş, protokol ve vatandaş havuzu listelerinin PDF'i
   * yok ve bilinçli: onlar süzülmek ve sayılmak için dışa aktarılıyor,
   * elden ele dolaşmak için değil. Boş bir "PDF" düğmesi çizmek, basıp
   * hiçbir şey alamayan bir kullanıcı bırakırdı.
   * </p>
   */
  pdf?: () => void;
  className?: string;
  /**
   * Verilirse ikili düğme yerine TEK düğme + açılır menü çizilir.
   *
   * Mobilde çıktı, arama kutusuyla aynı kabuğun içinde tek bir yazıcı
   * simgesi olarak duruyor: yan yana "Excel | PDF" iki etiket orada
   * genişliğin yarısını yiyordu ve arama alanı iki kelimeye düşüyordu.
   * Masaüstünde ikisi de görünür kalıyor — orada tek dokunuşluk olması
   * daha değerli.
   */
  tetikleyici?: React.ReactNode;
}) {
  if (tetikleyici) {
    return (
      <DropdownMenu.Root>
        <DropdownMenu.Trigger asChild>{tetikleyici}</DropdownMenu.Trigger>
        <DropdownMenu.Portal>
          <DropdownMenu.Content
            align="end"
            sideOffset={6}
            collisionPadding={8}
            className="katman anim-katman z-400 w-[min(220px,calc(100vw-16px))] overflow-hidden rounded-card border border-border bg-surface p-1 shadow-3"
          >
            <DropdownMenu.Label className="px-2.5 py-1.5 text-2xs font-semibold uppercase tracking-[0.06em] text-text-3">
              Dışa aktar
            </DropdownMenu.Label>
            <MenuSatiri ikon={<FileSpreadsheet size={15} />} etiket="Excel" tikla={excel} />
            {pdf && <MenuSatiri ikon={<FileText size={15} />} etiket="PDF" tikla={pdf} />}
          </DropdownMenu.Content>
        </DropdownMenu.Portal>
      </DropdownMenu.Root>
    );
  }

  return (
    <div
      role="group"
      aria-label="Dışa aktar"
      className={cn(
        'inline-flex h-ctrl shrink-0 items-stretch overflow-hidden rounded-control border border-border bg-surface shadow-1',
        className,
      )}
    >
      <Parca ikon={<FileSpreadsheet size={14} />} etiket="Excel" tikla={excel} />
      {pdf && (
        <>
          <span className="w-px shrink-0 bg-border" aria-hidden />
          <Parca ikon={<FileText size={14} />} etiket="PDF" tikla={pdf} />
        </>
      )}
    </div>
  );
}

function MenuSatiri({
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
      className="flex cursor-default items-center gap-2.5 rounded-sm px-2.5 py-2.5 text-sm outline-hidden data-highlighted:bg-surface-2"
    >
      <span className="text-text-3">{ikon}</span>
      {etiket} olarak indir
    </DropdownMenu.Item>
  );
}

function Parca({
  ikon,
  etiket,
  tikla,
}: {
  ikon: React.ReactNode;
  etiket: string;
  tikla: () => void;
}) {
  return (
    <button
      type="button"
      onClick={tikla}
      title={`${etiket} olarak indir`}
      aria-label={`${etiket} olarak indir`}
      className="inline-flex items-center gap-1.5 px-2.5 text-sm font-medium text-text-2 sm:px-3
        transition-colors hover:bg-surface-2 hover:text-text
        focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-(--focus-ring)"
    >
      <span className="hidden sm:inline">{ikon}</span>
      {etiket}
    </button>
  );
}
