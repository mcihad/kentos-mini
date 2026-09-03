import { MessageSquarePlus, Send } from 'lucide-react';
import { useState } from 'react';
import { Textarea } from './Field';
import { Button } from './Button';
import { FormModal } from './FormModal';
import { Card } from './Card';
import { useIsDesktop } from './screenSize';

/**
 * NOT EKLEME — mobilde alt tabaka, masaüstünde satır içi kart.
 *
 * <h4>Neden mobilde tabaka</h4>
 * <p>
 * Not kutusu, notlar sekmesinin EN ÜSTÜNDE duran 140px'lik bir karttı. Ekran
 * her açıldığında yer kaplıyor, oysa kullanıcıların çoğu o sekmeye
 * <b>okumak</b> için giriyor. Üstelik telefonda alana dokunulunca klavye
 * açılıyor, görünür alan yarıya iniyor ve yazarken altındaki notlar tamamen
 * kayboluyordu.
 * </p>
 * <p>
 * Artık sekmenin başında tam genişlik bir düğme var; dokununca alt tabaka
 * açılıyor. Klavye tabakanın kendi alanını itiyor, arkadaki liste yerinde
 * kalıyor ve kaydettikten sonra kullanıcı okuduğu yere geri dönüyor.
 * </p>
 * <p>
 * Masaüstünde kart aynen duruyor: orada yer bol ve iki tıkla yazmak yerine
 * doğrudan yazmaya başlamak daha hızlı.
 * </p>
 */
export function NoteComposer({
  yerTutucu,
  gonder,
  bekliyor,
  alanId,
}: {
  yerTutucu: string;
  /** Metni gönderir. Başarılıysa alan temizlenir — çağıran `Promise` döndürmeli. */
  gonder: (metin: string) => Promise<unknown>;
  bekliyor: boolean;
  alanId: string;
}) {
  const masaustu = useIsDesktop();
  const [metin, setMetin] = useState('');
  const [acik, setAcik] = useState(false);

  const gecerli = metin.trim().length > 0;

  async function kaydet() {
    if (!gecerli) return;
    await gonder(metin.trim());
    setMetin('');
    setAcik(false);
  }

  if (masaustu) {
    return (
      <Card className="p-3.5">
        <label htmlFor={alanId} className="sr-only">
          Yeni not
        </label>
        <Textarea
          id={alanId}
          value={metin}
          onChange={(e) => setMetin(e.target.value)}
          placeholder={yerTutucu}
          className="min-h-[76px]"
        />
        <div className="mt-2.5 flex justify-end">
          <Button onClick={kaydet} disabled={!gecerli || bekliyor}>
            <Send size={13} />
            Not ekle
          </Button>
        </div>
      </Card>
    );
  }

  return (
    <>
      <button
        type="button"
        onClick={() => setAcik(true)}
        className="flex h-ctrl-lg w-full items-center justify-center gap-2 rounded-control border border-dashed border-line-2 bg-surface text-sm font-semibold text-ink-2 active:scale-[0.99]"
        style={{ transitionTimingFunction: 'var(--ease-spring)' }}
      >
        <MessageSquarePlus size={16} strokeWidth={2} />
        Not ekle
      </button>

      <FormModal
        acik={acik}
        kapat={() => setAcik(false)}
        baslik="Not ekle"
        ikon={<MessageSquarePlus size={15} />}
        altBilgi={metin.trim().length > 0 ? `${metin.trim().length} karakter` : undefined}
        eylemler={
          <>
            <Button varyant="ikincil" onClick={() => setAcik(false)}>
              Vazgeç
            </Button>
            <Button onClick={kaydet} disabled={!gecerli || bekliyor}>
              <Send size={13} />
              {bekliyor ? 'Ekleniyor…' : 'Ekle'}
            </Button>
          </>
        }
      >
        <label htmlFor={alanId} className="sr-only">
          Yeni not
        </label>
        <Textarea
          id={alanId}
          value={metin}
          onChange={(e) => setMetin(e.target.value)}
          placeholder={yerTutucu}
          className="min-h-[140px]"
        />
      </FormModal>
    </>
  );
}
