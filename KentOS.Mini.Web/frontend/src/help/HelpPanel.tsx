import { BookOpen } from 'lucide-react';
import { OverlayShell } from '../components/OverlayShell';
import { Markdown } from './Markdown';

/**
 * YARDIM PANELİ.
 *
 * <p>
 * Masaüstünde SAĞDAN açılan bir panel, telefonda alttan gelen bir tabaka.
 * Ekranın üstünü kapatan ortalanmış bir pencere değil: yardım okunurken
 * arkadaki ekranın görünmesi gerekiyor — kullanıcı anlatılan düğmeyi aynı
 * anda görebilsin.
 * </p>
 *
 * <p>
 * <b>Kabuğu artık kendi kurmuyor.</b> Panel ham Radix Dialog + elle yazılmış
 * CSS ile çiziliyordu ve telefonda <b>parmakla kapanmıyordu</b>: tepesinde
 * tutamak vardı ama tutamağa bağlı hiçbir kod yoktu — görsel olarak
 * "beni aşağı çek" diyen, çekilince hiçbir şey yapmayan bir şerit.
 * <see cref="OverlayShell"/> mobilde <c>vaul</c> kullanıyor; kaydırarak
 * kapatma oradan geliyor ve uygulamadaki bütün tabakalarla aynı davranıyor.
 * </p>
 *
 * İçerik <c>texts/*.md</c> dosyalarından geliyor; yazan kişi React bilmeden
 * güncelleyebilsin diye düz markdown.
 */
export function HelpPanel({
  acik,
  kapat,
  baslik,
  ozet,
  metin,
}: {
  acik: boolean;
  kapat: () => void;
  baslik: string;
  ozet?: string;
  metin: string;
}) {
  return (
    <OverlayShell
      acik={acik}
      kapat={kapat}
      baslik={baslik}
      aciklama={ozet}
      masaustuYerlesim="yan"
      ikon={<BookOpen size={17} />}
    >
      <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain p-4 md:p-5">
        <Markdown metin={metin} />

        {/*
          KURUM ADI YAZILMAZ. Burada bir müdürlük adı gömülüydü; uygulama
          başka kurumlara verildiğinde yardım metni yanlış bir birimi işaret
          ediyordu. Kural yardım metinlerinde uygulanmıştı ama bu satır
          `.tsx` içinde olduğu için taramanın dışında kalmıştı.
        */}
        <p className="mt-8 border-t border-border pt-3 text-xs leading-[1.6] text-text-3">
          Anlatılanı ekranda bulamadıysanız yetkiniz kapalı olabilir; yetki
          tanımlarını sistem yöneticiniz açar.
        </p>
      </div>
    </OverlayShell>
  );
}
