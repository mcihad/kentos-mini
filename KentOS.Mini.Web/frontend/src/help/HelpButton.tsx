import { CircleHelp } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { IconButton } from '../components/Button';
import { findHelp } from './catalog';
import { HelpPanel } from './HelpPanel';

/**
 * BULUNDUĞUNUZ EKRANIN YARDIMI.
 *
 * Düğme her sayfada AYNI yerde (üst çubukta) duruyor ama açtığı metin o anki
 * ekrana ait. Her ekranın içine ayrı bir düğme koymak, kullanıcının yardımı
 * her sayfada yeniden aramasına yol açardı; sabit yer öğrenilir, sayfaya özel
 * içerik ise aranan şeydir.
 *
 * Yardımı olmayan bir sayfada düğme HİÇ çizilmez — boş bir panel açan düğme
 * "yardım yok" demenin en kötü yolu.
 */
export function HelpButton() {
  const konum = useLocation();
  const [acik, setAcik] = useState(false);
  const kayit = findHelp(konum.pathname);

  // Sayfa değişince panel kapanır: açık kalan yardım artık başka bir ekranı
  // anlatıyor olurdu.
  useEffect(() => setAcik(false), [konum.pathname]);

  if (!kayit) return null;

  return (
    <>
      {/*
        ŞERİTTEKİ BÜTÜN DÜĞMELER AYNI KALIPTA.

        Bu düğme bir dönem kendi ölçüsünü uyduruyordu: 34px yükseklik,
        kenarlıksız, geniş ekranda yanında "Yardım" yazısı. Yanındaki beş
        düğme ise 38×38 kenarlıklı `IkonButon`du — şerit tek bir düğme
        yüzünden hizasız görünüyor, yazı da masaüstünde 46px yiyordu.
        Ekranın adı zaten `title`da ve `aria-label`da yazılı.
      */}
      <IconButton
        etiket={`${kayit.baslik} — bu ekran nasıl kullanılır?`}
        onClick={() => setAcik(true)}
      >
        <CircleHelp size={17} strokeWidth={1.8} />
      </IconButton>

      <HelpPanel
        acik={acik}
        kapat={() => setAcik(false)}
        baslik={kayit.baslik}
        ozet={kayit.ozet}
        metin={kayit.metin}
      />
    </>
  );
}
