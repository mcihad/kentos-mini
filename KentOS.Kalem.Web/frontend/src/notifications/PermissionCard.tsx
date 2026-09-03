import { Bell, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button, IconButton } from '../components/Button';
import { useToast } from '../components/Toast';
import { useSession } from '../auth/SessionProvider';
import { bildirimDurumu, iosNeedsInstall, webJetonuKaydet } from './fcm';

const ERTELEME_ANAHTARI = 'sv-bildirim-erteleme';

/**
 * Giriş sonrası bildirim izni isteği.
 *
 * <p>
 * Tarayıcının izin kutusu <b>doğrudan açılmaz</b>. Sayfa yüklenir yüklenmez
 * çıkan bir izin kutusuna kullanıcı ne istendiğini anlamadan "Engelle"
 * basıyor ve <b>bu karar kalıcı</b> — sonradan uygulama içinden geri almanın
 * yolu yok, tarayıcı ayarlarına girmek gerekiyor.
 * </p>
 *
 * <p>
 * Bunun yerine önce ne için istendiğini anlatan bir kart gösterilir; asıl
 * izin kutusu ancak kullanıcı "İzin ver"e bastığında açılır. Kapatılırsa
 * bir hafta tekrar sorulmaz.
 * </p>
 */
export function NotificationPermissionCard() {
  const { me } = useSession();
  const { bildir } = useToast();

  const [gorunur, setGorunur] = useState(false);
  const [beklemede, setBeklemede] = useState(false);

  useEffect(() => {
    if (!me) {
      setGorunur(false);
      return;
    }

    let iptal = false;
    (async () => {
      // iOS'ta izin yalnızca ana ekrana eklenmiş uygulamada istenebilir;
      // Safari sekmesinde kart göstermek boşuna umut olurdu.
      if (iosNeedsInstall()) return;

      const erteleme = Number(localStorage.getItem(ERTELEME_ANAHTARI) ?? 0);
      if (erteleme > Date.now()) return;

      // Yalnızca HENÜZ SORULMAMIŞSA sor: verilmişse gerek yok, reddedilmişse
      // tarayıcı bir daha sormaz ve kart çalışmayan bir düğmeye dönüşür.
      const durum = await bildirimDurumu();
      if (!iptal && durum === 'kapali') setGorunur(true);
    })();

    return () => {
      iptal = true;
    };
  }, [me]);

  if (!gorunur) return null;

  function ertele() {
    // Bir hafta: her girişte sormak, izin kutusundan farksız bir rahatsızlık.
    localStorage.setItem(ERTELEME_ANAHTARI, String(Date.now() + 7 * 24 * 60 * 60 * 1000));
    setGorunur(false);
  }

  async function izinVer() {
    setBeklemede(true);
    try {
      await webJetonuKaydet();
      bildir('basari', 'Bildirimler açıldı', 'Etkinlik ve talep bildirimleri bu cihaza gelecek.');
      setGorunur(false);
    } catch (h) {
      bildir('hata', 'Bildirimler açılamadı', (h as Error).message);
      // Reddedildiyse kartı da kaldır: tarayıcı bir daha sormaz, kart
      // ekranda kalırsa çalışmayan bir düğme olur.
      if ((await bildirimDurumu()) === 'engellendi') setGorunur(false);
    } finally {
      setBeklemede(false);
    }
  }

  return (
    <div className="mb-3.5 flex items-start gap-3 rounded-card border border-border bg-surface p-3.5 shadow-1 md:items-center">
      <span
        className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-brand-tint text-brand-2"
        aria-hidden
      >
        <Bell size={16} />
      </span>

      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold">Bildirimleri açalım mı?</p>
        <p className="text-sm leading-normal text-text-2 metin-guzel">
          Yeni etkinlik, havale ve size gönderilen dosyalar anında bu cihaza düşsün.
        </p>

        <div className="mt-2.5 flex gap-2">
          <Button className="h-8 px-3 text-sm" onClick={izinVer} disabled={beklemede}>
            {beklemede ? 'İzin isteniyor…' : 'İzin ver'}
          </Button>
          <Button varyant="sade" className="h-8 px-3 text-sm" onClick={ertele}>
            Şimdi değil
          </Button>
        </div>
      </div>

      <IconButton etiket="Kapat" onClick={ertele}>
        <X size={15} />
      </IconButton>
    </div>
  );
}
