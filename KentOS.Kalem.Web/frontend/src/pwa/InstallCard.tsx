import { Bell, CheckCircle2, Download, ListChecks, RotateCcw, X } from 'lucide-react';
import { useState } from 'react';
import { Button } from '../components/Button';
import { Card } from '../components/Card';
import { useToast } from '../components/Toast';
import { InstallSheet } from './InstallSheet';
import { clearInstalledFlag, promptInstall, snoozeInstall, useInstall } from './install';

/**
 * "Ana ekrana ekle" kartı — Ayarlar ekranının tepesinde.
 *
 * <p>
 * Sadece bir kolaylık değil: <b>iOS'ta web push yalnızca kurulu PWA'da
 * çalışıyor</b>. Kurulmadan bildirim izni istemek sessizce başarısız oluyor
 * ve kullanıcı "açtım ama gelmiyor" diyor. Bu yüzden kart, bildirimden
 * ÖNCE gösterilmesi gereken adımı anlatıyor.
 * </p>
 *
 * <p>
 * <b>Kapatmak ERTELEMEKTİR, gizlemek değil.</b> Kart eskiden "bir daha
 * gösterme" diye kalıcı bir işaret yazıyordu ve aynı işareti <i>kurulum
 * başarılı olduğunda da</i> yazıyordu: kullanıcı uygulamayı kurup sonra
 * kaldırınca kart bir daha asla dönmüyordu. Artık iki hafta susuyor ve
 * uygulamanın kaldırıldığı anlaşıldığında erteleme de siliniyor.
 * </p>
 *
 * <p>
 * Kurulu görünen ama <b>yalnızca kalıcı işarete dayanan</b> durumda kart
 * kaybolmaz: ince bir satır olarak kalır ve "kaldırdıysanız yeniden kurun"
 * kapısını açık tutar. Tarayıcı kaldırma olayını hiçbir zaman bildirmiyor;
 * kullanıcının elinde çalışan bir kapı olmalı.
 * </p>
 */
export function InstallCard() {
  const durum = useInstall();
  const { bildir } = useToast();
  const [tabaka, setTabaka] = useState(false);

  async function kur() {
    const sonuc = await promptInstall();
    if (sonuc === 'kuruldu') {
      bildir('basari', 'Uygulama kuruldu', 'Artık ana ekranınızdan açabilirsiniz.');
    } else if (sonuc === 'yok') {
      setTabaka(true);
    }
  }

  // Kurulu ve bundan eminiz (pencere kipi ya da işletim sistemi doğruladı).
  if (durum.kurulu && !durum.isaretten) return null;

  if (durum.kurulu) {
    return (
      <Card className="flex flex-wrap items-center gap-x-3 gap-y-2 p-3">
        <span className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-(--st-ok-bg) text-(--st-ok)">
          <CheckCircle2 size={16} strokeWidth={1.9} />
        </span>
        <p className="min-w-0 flex-1 text-sm text-text-2 metin-guzel">
          Uygulama bu cihaza <b>kurulu</b> görünüyor.
        </p>
        <Button
          varyant="ikincil"
          onClick={() => {
            clearInstalledFlag();
            bildir('bilgi', 'Kurulum kaydı sıfırlandı', 'Kurulum seçenekleri yeniden görünecek.');
          }}
        >
          <RotateCcw size={14} />
          Kaldırdım
        </Button>
      </Card>
    );
  }

  if (!durum.kurulabilir || durum.ertelendi) return null;

  return (
    <>
      <Card className="relative overflow-hidden p-4">
        <span
          aria-hidden
          className="absolute inset-y-0 left-0 w-[3px]"
          style={{ background: 'var(--gold)' }}
        />

        <button
          type="button"
          onClick={snoozeInstall}
          aria-label="Şimdilik kapat"
          title="Şimdilik kapat"
          className="absolute right-2 top-2 grid h-7 w-7 place-items-center rounded-sm text-text-3 hover:bg-sunken hover:text-text"
        >
          <X size={14} />
        </button>

        <div className="flex items-start gap-3 pr-6">
          <span className="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-brand text-on-brand">
            <Download size={18} strokeWidth={1.9} />
          </span>

          <div className="min-w-0">
            <p className="font-display text-base font-bold">Uygulamayı telefonuna kur</p>
            <p className="mt-1 text-sm leading-[1.55] text-text-2 metin-guzel">
              Ana ekrandan tek dokunuşla açılır, tam ekran çalışır ve{' '}
              <b>bildirimler telefonuna gelir</b>.
            </p>

            {durum.platform === 'ios' && (
              <p className="mt-1.5 flex items-start gap-1.5 text-2xs leading-[1.5] text-text-3 metin-guzel">
                <Bell size={12} strokeWidth={1.9} className="mt-0.5 shrink-0" />
                {/*
                  METİN TEK BİR `span` İÇİNDE.

                  Esnek kapta her metin parçası ve her satır içi etiket AYRI
                  bir esnek öğe oluyor: "iPhone ve iPad'de bildirimler",
                  "<b>yalnızca kurulu uygulamada</b>" ve "çalışır." üç ayrı
                  kutuya bölünüp aralarına `gap` giriyordu — cümle üç sütuna
                  dağılmış gibi okunuyordu.
                */}
                <span className="min-w-0">
                  iPhone ve iPad’de bildirimler <b>yalnızca kurulu uygulamada</b> çalışır.
                </span>
              </p>
            )}

            <div className="mt-3 flex flex-wrap gap-2">
              {durum.istemVar ? (
                <Button onClick={() => void kur()}>
                  <Download size={14} />
                  Kur
                </Button>
              ) : (
                <Button onClick={() => setTabaka(true)}>
                  <ListChecks size={14} />
                  Nasıl kurulur?
                </Button>
              )}
              <Button varyant="ikincil" onClick={snoozeInstall}>
                Sonra
              </Button>
            </div>
          </div>
        </div>
      </Card>

      <InstallSheet acik={tabaka} kapat={() => setTabaka(false)} />
    </>
  );
}
