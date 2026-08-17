import * as Dialog from '@radix-ui/react-dialog';
import { Building2, Check, Search, User, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import { SearchInput } from '../../components/Field';
import { Button, IconButton } from '../../components/Button';
import { cn } from '../../components/utils';

/** Hem birimi hem kişiyi karşılayan satır. */
export type SelectableItem = {
  id?: number;
  /** Birim adı ya da kişinin tam adı. */
  ad?: string | null;
  soyad?: string | null;
  /** Birimde yetkilinin adı; kişide boş. */
  yetkili?: string | null;
  unvan?: string | null;
};

const KIPLER = {
  birim: {
    baslik: 'Katılımcı birimler',
    aciklama: 'Etkinliğe katılacak birimleri seçin.',
    yerTutucu: 'Birim adı veya yetkili ara',
    bos: 'Eşleşen birim yok.',
    sayac: (n: number) => `${n} birim seçildi`,
    ikon: Building2,
  },
  kisi: {
    baslik: 'Görebilecek kişiler',
    aciklama: 'Bu gizli etkinliği görebilecek kişileri seçin.',
    yerTutucu: 'Ad, soyad veya unvan ara',
    bos: 'Biriminizde başka kullanıcı yok.',
    sayac: (n: number) => `${n} kişi seçildi`,
    ikon: User,
  },
} as const;

/**
 * Katılımcı seçme diyaloğu — <b>iki kip</b>.
 *
 * <p>
 * <b>`kip="birim"` — katılımcı birimler.</b> Etkinliğe katılacak
 * departmanlar: "Başkan Yardımcısı", "Fen İşleri Müdürlüğü". Kullanıcının
 * kendi seviyesindeki ve altındaki birimlerden seçilir; bir müdürlük başkan
 * yardımcısını kendi toplantısına çağıramaz, o davet yukarıdan gelir.
 * </p>
 *
 * <p>
 * <b>`kip="kisi"` — gizli etkinliği görebilecekler.</b> Etkinliği kimin
 * görebileceğini belirler ve <b>kendi birimindeki</b> kullanıcılardan
 * seçilir.
 * </p>
 *
 * <p>
 * İkisi <b>birbirinin yerine geçmez</b>: birim eklemek gizli etkinliği o
 * birime açmaz, kişi eklemek de onu toplantıya davet etmez. Bir dönem tek bir
 * seçici vardı ve gizli bir toplantıya bir müdürlüğü davet etmek o
 * müdürlükteki herkesi içeriğe ortak ediyordu.
 * </p>
 *
 * <p>
 * Seçim <b>geçici tutulur</b>; "Tamam" denene kadar forma yazılmaz. Böylece
 * yanlışlıkla açılan diyalog vazgeçilince mevcut seçimi bozmaz.
 * </p>
 */
export function ParticipantPicker({
  acik,
  kapat,
  kip = 'birim',
  ogeler,
  secili,
  degistir,
}: {
  acik: boolean;
  kapat: () => void;
  kip?: 'birim' | 'kisi';
  ogeler: SelectableItem[];
  secili: number[];
  degistir: (idler: number[]) => void;
}) {
  const metin = KIPLER[kip];
  const Ikon = metin.ikon;
  const [taslak, setTaslak] = useState<number[]>(secili);
  const [arama, setArama] = useState('');

  // Diyalog her açılışta mevcut seçimden başlar.
  const [sonAcik, setSonAcik] = useState(acik);
  if (acik !== sonAcik) {
    setSonAcik(acik);
    if (acik) {
      setTaslak(secili);
      setArama('');
    }
  }

  const suzulmus = useMemo(() => {
    const a = arama.trim().toLocaleLowerCase('tr-TR');
    if (!a) return ogeler;
    return ogeler.filter((b) =>
      [b.ad, b.soyad, b.yetkili, b.unvan]
        .filter(Boolean)
        .some((m) => m!.toLocaleLowerCase('tr-TR').includes(a)),
    );
  }, [ogeler, arama]);

  function degistirBiri(id: number) {
    setTaslak((s) => (s.includes(id) ? s.filter((x) => x !== id) : [...s, id]));
  }

  return (
    <Dialog.Root open={acik} onOpenChange={(a) => !a && kapat()}>
      <Dialog.Portal>
        <Dialog.Overlay className="anim-perde fixed inset-0 z-50 bg-perde" />
        <Dialog.Content
          className="katman anim-tabaka fixed inset-x-0 bottom-0 z-50 flex max-h-[88dvh] flex-col rounded-t-win bg-surface shadow-3
            md:inset-x-auto md:bottom-auto md:left-1/2 md:top-1/2 md:max-h-[80dvh] md:w-[min(520px,calc(100vw-48px))]
            md:-translate-x-1/2 md:-translate-y-1/2 md:rounded-win"
        >
          <div className="flex items-center gap-2.5 border-b border-border px-4 py-3">
            <span className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-brand-tint text-brand-2">
              <Ikon size={15} />
            </span>
            <Dialog.Title className="flex-1 font-display text-lg font-bold">
              {metin.baslik}
            </Dialog.Title>
            <Dialog.Close asChild>
              <IconButton etiket="Kapat">
                <X size={16} />
              </IconButton>
            </Dialog.Close>
          </div>

          <Dialog.Description className="sr-only">{metin.aciklama}</Dialog.Description>

          <div className="border-b border-border p-3">
            <SearchInput
              value={arama}
              onChange={(e) => setArama(e.target.value)}
              placeholder={metin.yerTutucu}
              aria-label={metin.baslik}
              ikon={<Search size={15} />}
            />
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto p-2">
            {suzulmus.length === 0 ? (
              <p className="px-2 py-6 text-center text-sm text-text-3">
                {arama ? 'Eşleşen kayıt yok.' : metin.bos}
              </p>
            ) : (
              <ul className="space-y-1">
                {suzulmus.map((b) => {
                  const isaretli = taslak.includes(b.id!);
                  return (
                    <li key={b.id}>
                      <button
                        type="button"
                        onClick={() => degistirBiri(b.id!)}
                        aria-pressed={isaretli}
                        className={cn(
                          'flex w-full items-center gap-2.5 rounded-control border px-2.5 py-2 text-left transition-colors',
                          isaretli
                            ? 'border-brand bg-brand-tint'
                            : 'border-transparent hover:bg-surface-2',
                        )}
                      >
                        <span
                          className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-sunken text-text-3"
                          aria-hidden
                        >
                          <Ikon size={16} />
                        </span>
                        <span className="min-w-0 flex-1">
                          <span className="block truncate text-sm font-medium">
                            {[b.ad, b.soyad].filter(Boolean).join(' ')}
                          </span>
                          {/*
                            Birimde yetkilinin adı ZORUNLU ayırt edici: kurumda
                            altı ayrı "Başkan Yardımcısı" birimi var ve yalnızca
                            birim adıyla hangisinin seçildiği anlaşılmıyor.
                          */}
                          <span className="block truncate text-xs text-text-3">
                            {[b.yetkili, b.unvan].filter(Boolean).join(' · ')}
                          </span>
                        </span>
                        <span
                          className={cn(
                            'grid h-5 w-5 shrink-0 place-items-center rounded-sm border transition-colors',
                            isaretli
                              ? 'border-brand bg-brand text-on-brand'
                              : 'border-border-2',
                          )}
                          aria-hidden
                        >
                          {isaretli && <Check size={13} strokeWidth={3} />}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <div className="flex shrink-0 items-center gap-2 border-t border-border px-4 py-3">
            <span className="flex-1 text-sm text-text-3">
              {metin.sayac(taslak.length)}
            </span>
            <Button type="button" varyant="ikincil" onClick={kapat}>
              Vazgeç
            </Button>
            <Button
              type="button"
              onClick={() => {
                degistir(taslak);
                kapat();
              }}
            >
              Tamam
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
