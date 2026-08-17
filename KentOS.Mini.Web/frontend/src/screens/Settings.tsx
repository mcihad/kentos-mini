import {
  Bell, BellOff, Building2, LogOut, Monitor, Moon, ShieldCheck, Smartphone, Sun,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button } from '../components/Button';
import { Card, CardHeader } from '../components/Card';
import { SegmentedSelect } from '../components/Filters';
import { useToast } from '../components/Toast';
import { cn } from '../components/utils';
import {
  bildirimDurumu, webJetonuKaydet, webJetonuSil, type NotificationState,
} from '../notifications/fcm';
import { useSession } from '../auth/SessionProvider';
import { useTheme, type ThemeMode } from '../theme/ThemeProvider';
import { InstallCard } from '../pwa/InstallCard';
import { initials } from '../data/format';

/** Ayarlar — profil, görünüm, bildirimler, oturum. */
export default function Settings() {
  const { me, signOut } = useSession();
  const { tema, temaAyarla, sistemTercihi } = useTheme();
  const { bildir } = useToast();

  const [push, setPush] = useState<NotificationState>('bilinmiyor');
  const [islemde, setIslemde] = useState(false);

  useEffect(() => {
    bildirimDurumu().then(setPush);
  }, []);

  const pushAc = async () => {
    setIslemde(true);
    try {
      await webJetonuKaydet();
      setPush(await bildirimDurumu());
      bildir('basari', 'Bildirimler açıldı', 'Bu tarayıcıya bildirim gönderilecek.');
    } catch (h) {
      bildir('hata', 'Bildirim açılamadı', (h as Error).message);
      setPush(await bildirimDurumu());
    } finally {
      setIslemde(false);
    }
  };

  const pushKapat = async () => {
    setIslemde(true);
    try {
      await webJetonuSil();
      setPush(await bildirimDurumu());
      bildir('bilgi', 'Bildirimler kapatıldı');
    } catch (h) {
      bildir('hata', 'İşlem başarısız', (h as Error).message);
    } finally {
      setIslemde(false);
    }
  };

  return (
    <div className="space-y-4">
      {/* Kurulum kartı — bildirimlerden ÖNCE, çünkü iOS'ta ön koşul. */}
      <InstallCard />

      {/* ── Profil ── */}
      <Card>
        <CardHeader baslik="Profil" />
        <div className="flex items-center gap-3.5 p-4">
          <span
            className="grid h-14 w-14 shrink-0 place-items-center rounded-full bg-brand font-display text-xl font-bold text-on-brand"
            aria-hidden
          >
            {initials(me?.ad, me?.soyad) || initials(me?.kullaniciAdi)}
          </span>
          <div className="min-w-0">
            <p className="truncate font-display text-lg font-bold">
              {me?.tamAd || me?.kullaniciAdi}
            </p>
            {me?.unvan && <p className="truncate text-sm text-text-2">{me.unvan}</p>}
            {me?.birimAd && (
              <p className="mt-0.5 inline-flex items-center gap-1.5 text-xs text-text-3">
                <Building2 size={12} />
                {me.birimAd}
              </p>
            )}
          </div>
        </div>

        <dl className="grid gap-3 border-t border-border p-4 sm:grid-cols-2">
          <div>
            <dt className="text-2xs uppercase tracking-[0.06em] text-text-3">Kullanıcı adı</dt>
            <dd className="text-sm text-text-2">{me?.kullaniciAdi}</dd>
          </div>
          <div>
            <dt className="text-2xs uppercase tracking-[0.06em] text-text-3">E-posta</dt>
            <dd className="truncate text-sm text-text-2">{me?.eposta || '—'}</dd>
          </div>
        </dl>

        {(me?.roller ?? []).length > 0 && (
          <div className="border-t border-border p-4">
            <p className="mb-2 inline-flex items-center gap-1.5 text-2xs uppercase tracking-[0.06em] text-text-3">
              <ShieldCheck size={12} />
              Roller
            </p>
            <ul className="flex flex-wrap gap-1.5">
              {(me?.roller ?? []).map((r) => (
                <li
                  key={r}
                  className="rounded-full bg-sunken px-2.5 py-1 text-xs font-medium text-text-2"
                >
                  {r}
                </li>
              ))}
            </ul>
          </div>
        )}
      </Card>

      {/* ── Görünüm ── */}
      <Card>
        <CardHeader
          baslik="Görünüm"
          aciklama={
            tema === 'sistem'
              ? `Sistem tercihinize uyuluyor (şu an ${sistemTercihi === 'koyu' ? 'koyu' : 'açık'})`
              : undefined
          }
        />
        <div className="p-4">
          <SegmentedSelect<ThemeMode>
            deger={tema}
            degistir={temaAyarla}
            etiket="Tema"
            secenekler={[
              { deger: 'acik', etiket: 'Açık', ikon: <Sun size={13} /> },
              { deger: 'koyu', etiket: 'Koyu', ikon: <Moon size={13} /> },
              { deger: 'sistem', etiket: 'Sistem', ikon: <Monitor size={13} /> },
            ]}
          />
        </div>
      </Card>

      {/* ── Bildirimler ── */}
      <Card>
        <CardHeader
          baslik="Bildirimler"
          aciklama="Bu tarayıcıya masaüstü bildirimi gönderilir."
        />
        <div className="p-4">
          <BildirimDurumKarti durum={push} />

          <div className="mt-3.5 flex flex-wrap gap-2">
            {push === 'kurulumGerekli' ? null : push === 'acik' ? (
              <Button varyant="ikincil" onClick={pushKapat} disabled={islemde}>
                <BellOff size={14} />
                Bu tarayıcıda kapat
              </Button>
            ) : (
              <Button
                onClick={pushAc}
                disabled={islemde || push === 'engellendi' || push === 'desteklenmiyor'}
              >
                <Bell size={14} />
                Bildirimleri aç
              </Button>
            )}
          </div>

          <p className="mt-3 flex items-start gap-2 text-xs text-text-3">
            <Smartphone size={13} className="mt-0.5 shrink-0" />
            Mobil uygulamadaki bildirimler bundan bağımsızdır; burada yapılan
            değişiklik telefonunuzu etkilemez.
          </p>
        </div>
      </Card>

      {/* ── Oturum ── */}
      <Card>
        <CardHeader baslik="Oturum" />
        <div className="flex items-center justify-between gap-3 p-4">
          <p className="text-sm text-text-2">
            Çıkış yaptığınızda bu tarayıcıdaki bildirim kaydı da silinir.
          </p>
          <Button varyant="yikici" onClick={() => signOut()}>
            <LogOut size={14} />
            Çıkış yap
          </Button>
        </div>
      </Card>
    </div>
  );
}

/**
 * Bildirim izninin okunabilir hâli.
 *
 * "engellendi" için ayrı bir metin var: tarayıcı izni bir kez reddedildikten
 * sonra site JavaScript'ten yeniden soramaz; kullanıcının adres çubuğundaki
 * kilit simgesinden düzeltmesi gerekir. Bunu söylemezsek buton işe yaramıyor
 * gibi görünür.
 */
function BildirimDurumKarti({ durum }: { durum: NotificationState }) {
  const g: Record<NotificationState, { metin: string; aciklama: string; renk: string }> = {
    acik: {
      metin: 'Açık',
      aciklama: 'Bu tarayıcı bildirim alıyor.',
      renk: '--st-ok',
    },
    kapali: {
      metin: 'Kapalı',
      aciklama: 'Bildirimleri açmak için izin vermeniz gerekiyor.',
      renk: '--st-wait',
    },
    engellendi: {
      metin: 'Engellendi',
      aciklama:
        'Tarayıcı bildirimleri engelliyor. Adres çubuğundaki kilit simgesinden site izinlerini açın.',
      renk: '--st-no',
    },
    desteklenmiyor: {
      metin: 'Desteklenmiyor',
      aciklama: 'Bu tarayıcı web bildirimlerini desteklemiyor.',
      renk: '--st-cancel',
    },
    kurulumGerekli: {
      metin: 'Önce uygulamayı kurun',
      aciklama:
        'iPhone ve iPad’de bildirim yalnızca ana ekrana eklenmiş uygulamada çalışır. ' +
        'Safari’de Paylaş → Ana Ekrana Ekle adımlarını izleyin, sonra uygulamayı ' +
        'ana ekrandan açıp bildirimlere izin verin.',
      renk: '--st-wait',
    },
    bilinmiyor: {
      metin: 'Kontrol ediliyor…',
      aciklama: '',
      renk: '--text-3',
    },
  };

  const d = g[durum];

  return (
    <div className="flex items-start gap-2.5 rounded-md bg-sunken p-3">
      <span
        className={cn('mt-0.5 h-[9px] w-[9px] shrink-0 rounded-full')}
        style={{ background: `var(${d.renk})` }}
        aria-hidden
      />
      <div>
        <p className="text-sm font-semibold">{d.metin}</p>
        {d.aciklama && <p className="mt-0.5 text-sm text-text-2">{d.aciklama}</p>}
      </div>
    </div>
  );
}
