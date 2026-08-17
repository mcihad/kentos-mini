import { Bell, CircleCheck, Download, Maximize2, Smartphone } from 'lucide-react';
import { Button } from '../components/Button';
import { OverlayShell } from '../components/OverlayShell';
import { useToast } from '../components/Toast';
import { promptInstall, useInstall } from './install';
import { findInstructions } from './instructions';

const KAZANIMLAR = [
  { ikon: Smartphone, metin: 'Ana ekrandan tek dokunuşla açılır' },
  { ikon: Maximize2, metin: 'Adres çubuğu olmadan, tam ekran çalışır' },
  { ikon: Bell, metin: 'Bildirimler doğrudan telefonunuza düşer' },
];

/**
 * KURULUM TABAKASI — "uygulamayı kur" penceresi.
 *
 * <p>
 * İki hâli var ve ayrım tarayıcının bize kurulum istemi verip vermemesinde:
 * </p>
 *
 * <ul>
 *   <li><b>İstem varsa</b> tek bir düğme. Anlatacak bir şey yok, dokunulur ve
 *       biter.</li>
 *   <li><b>İstem yoksa</b> (iOS'un tamamı, macOS Safari, kurulum istemini
 *       henüz göndermemiş Chrome) tarayıcıya özel adımlar. Burada tek satırlık
 *       genel bir cümle yazmak işe yaramıyor: menünün adı da yeri de her
 *       tarayıcıda başka.</li>
 * </ul>
 *
 * <p>
 * Pencere <b>bildirimlerden önce</b> gelmesi gereken adımı da anlatıyor:
 * iOS'ta web push yalnızca ana ekrana eklenmiş uygulamalarda çalışıyor, yani
 * kurulum orada bir kolaylık değil ön şart.
 * </p>
 */
export function InstallSheet({ acik, kapat }: { acik: boolean; kapat: () => void }) {
  const durum = useInstall();
  const { bildir } = useToast();
  const talimat = findInstructions(durum);

  async function kur() {
    const sonuc = await promptInstall();
    if (sonuc === 'kuruldu') {
      bildir('basari', 'Uygulama kuruldu', 'Artık ana ekranınızdan açabilirsiniz.');
      kapat();
    } else if (sonuc === 'yok') {
      // İstem bir başka sekmede kullanılmış ya da tarayıcı geri almış olabilir.
      bildir('uyari', 'Kurulum penceresi açılamadı', 'Sayfayı yenileyip tekrar deneyin.');
    }
  }

  return (
    <OverlayShell
      acik={acik}
      kapat={kapat}
      baslik="Uygulamayı kur"
      aciklama={durum.istemVar ? 'Tek dokunuşla ana ekranınıza eklenir.' : talimat.ozet}
      ikon={<Download size={17} strokeWidth={1.9} />}
      genislik="dar"
    >
      <div className="min-h-0 flex-1 overflow-y-auto p-3.5">
        <ul className="space-y-2">
          {KAZANIMLAR.map((k) => {
            const Ikon = k.ikon;
            return (
              <li key={k.metin} className="flex items-center gap-2.5 text-sm text-text-2">
                <span className="grid h-7 w-7 shrink-0 place-items-center rounded-sm bg-brand-soft text-brand">
                  <Ikon size={14} strokeWidth={1.9} />
                </span>
                <span className="min-w-0 metin-guzel">{k.metin}</span>
              </li>
            );
          })}
        </ul>

        {durum.istemVar ? (
          <Button boyut="mobil" className="mt-4 w-full" onClick={() => void kur()}>
            <Download size={16} />
            Kur
          </Button>
        ) : (
          <>
            {/*
              Kazanımlar ile adımlar arasına AYRAÇ.

              İkisi arka arkaya gelince tek bir liste gibi okunuyor ve
              kullanıcı "ana ekrandan tek dokunuşla açılır" satırını da
              yapılacak bir adım sanıyordu. Ayraç uygulamanın kendi
              gramerinden: iki yanından altın saç teli (bkz. `FormBolumu`).
            */}
            <p
              className="mt-4 flex items-center gap-3 font-display text-2xs font-bold uppercase tracking-[0.12em] text-accent-ink
                before:h-px before:flex-1 before:bg-gold before:opacity-50 before:content-['']
                after:h-px after:flex-1 after:bg-gold after:opacity-50 after:content-['']"
            >
              Nasıl eklenir
            </p>

            {/*
              Adımlar NUMARALI: burada sıra gerçekten bilgi taşıyor — paylaş
              menüsü açılmadan "Ana Ekrana Ekle" satırı ortada yok.
            */}
            <ol className="mt-3 space-y-2.5">
              {talimat.adimlar.map((a, i) => {
                const Ikon = a.ikon;
                return (
                  <li key={i} className="flex items-start gap-2.5">
                    <span className="mt-px grid h-7 w-7 shrink-0 place-items-center rounded-full bg-surface-2 font-display text-2xs font-bold tabular-nums text-text-2">
                      {i + 1}
                    </span>
                    <span className="flex min-w-0 flex-1 items-start gap-2 pt-1 text-sm leading-[1.5] text-text-2 metin-guzel">
                      <Ikon size={15} strokeWidth={1.9} className="mt-px shrink-0 text-text-3" />
                      <span className="min-w-0">{a.metin}</span>
                    </span>
                  </li>
                );
              })}
            </ol>

            {talimat.not && (
              <p className="mt-3 rounded-card bg-surface-2 px-3 py-2 text-2xs leading-[1.5] text-text-3 metin-guzel">
                {talimat.not}
              </p>
            )}
          </>
        )}

        <p className="mt-4 flex items-start gap-2 text-2xs leading-[1.5] text-text-3 metin-guzel">
          <CircleCheck size={13} strokeWidth={1.9} className="mt-0.5 shrink-0 text-(--st-ok)" />
          Kurulum indirme değildir: aynı hesap, aynı veri. Uygulama yalnızca ana
          ekranınızda kendi simgesiyle durur.
        </p>
      </div>
    </OverlayShell>
  );
}
