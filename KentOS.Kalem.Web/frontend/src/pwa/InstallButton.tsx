import { Download } from 'lucide-react';
import { useState } from 'react';
import { IconButton } from '../components/Button';
import { InstallSheet } from './InstallSheet';
import { useInstall } from './install';

/**
 * Appbar'daki kurulum simgesi.
 *
 * <p>
 * <b>Yalnızca kurulu değilken çizilir</b> ve kurulduğu anda kendiliğinden
 * kaybolur — kalıcı bir düğme değil, bir davet. Kurulum kartı Ayarlar
 * ekranındaydı; oraya girmeyen kullanıcı uygulamanın kurulabildiğini hiç
 * öğrenmiyordu.
 * </p>
 *
 * <p>
 * Diğer şerit düğmeleri nötr; bu <b>marka renginde</b>. Sebebi süs değil:
 * altı simgelik bir şeritte hepsi aynı griyse yeni gelen simge fark
 * edilmiyor. Kurulum tek seferlik bir eylem, bir kez görülmesi yeterli.
 * </p>
 */
export function InstallButton() {
  const durum = useInstall();
  const [acik, setAcik] = useState(false);

  if (!durum.kurulabilir) return null;

  return (
    <>
      <IconButton
        varyant="sade"
        etiket="Uygulamayı kur"
        onClick={() => setAcik(true)}
        className="border-brand-soft bg-brand-soft text-brand hover:text-brand"
      >
        <Download size={17} strokeWidth={1.9} />
      </IconButton>

      <InstallSheet acik={acik} kapat={() => setAcik(false)} />
    </>
  );
}
