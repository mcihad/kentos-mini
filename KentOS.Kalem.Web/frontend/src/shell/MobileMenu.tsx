import { CircleHelp, LogOut, Moon, Palette, Sun } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useSession } from '../auth/SessionProvider';
import { BottomSheet, SheetDivider, SheetHeading, SheetRow } from './mobile/BottomSheet';
import type { NavigationGroup } from './navigation';

/**
 * Mobil "Menü" sekmesinin açtığı alt sayfa — design_new §5.2.
 *
 * Alt çubukta yalnızca dört sekme var; geri kalan bütün modüller buradan
 * açılıyor. Bu yüzden sheet bir "menü listesi" değil, ekranın kendisi kadar
 * önemli bir gezinme yüzeyi: satırlar 56px, ikonlar kendi çipinde ve her
 * satırda chevron var — dokunulabilir olduğu bakışta anlaşılsın.
 *
 * Kullanıcı künyesi en üstte duruyor: "hangi hesapla girdim" sorusu masaüstünde
 * kenar çubuğunun dibinde cevaplanıyor, mobilde kenar çubuğu yok.
 */
export function MobileMenu({
  acik,
  kapat,
  gruplar,
  etkinTema,
  temaDegistir,
  temaPaneliAc,
}: {
  acik: boolean;
  kapat: () => void;
  gruplar: NavigationGroup[];
  etkinTema: 'acik' | 'koyu';
  temaDegistir: () => void;
  temaPaneliAc: () => void;
}) {
  const { me, signOut } = useSession();
  const git = useNavigate();

  return (
    <BottomSheet acik={acik} kapat={kapat} baslik="Menü" aciklama="Tüm modüller ve hesap işlemleri"
        baslikGizli>
      {me && (
        <div className="mb-3 flex items-center gap-3 px-1">
          <span className="grid h-11 w-11 flex-none place-items-center rounded-full bg-brand-soft font-display text-lg font-bold text-brand">
            {(me.tamAd ?? '?').slice(0, 1).toUpperCase()}
          </span>
          <span className="min-w-0">
            <span className="block truncate font-display text-base font-bold text-ink">
              {me.tamAd}
            </span>
            <span className="block truncate text-2xs text-ink-3">
              {[me.unvan, me.birimAd].filter(Boolean).join(' · ')}
            </span>
          </span>
        </div>
      )}

      {gruplar.map((grup) => (
        <div key={grup.baslik} className="mb-2">
          <SheetHeading>{grup.baslik}</SheetHeading>
          <div className="flex flex-col gap-0.5">
            {grup.ogeler.map((oge) => {
              const Ikon = oge.ikon;
              return (
                <SheetRow
                  key={oge.yol}
                  ikon={<Ikon size={19} strokeWidth={1.8} />}
                  onClick={() => {
                    kapat();
                    git(oge.yol);
                  }}
                >
                  {oge.etiket}
                </SheetRow>
              );
            })}
          </div>
        </div>
      ))}

      <SheetDivider />

      {/*
        GÖRÜNÜM VE YARDIM BURAYA TAŞINDI.

        Mobil appbar'da BEŞ ikon düğmesi vardı: yardım, bildirim, tema
        tasarımcısı, gece/gündüz ve çıkış. 390px'lik bir ekranda 52px'lik
        şeridin yarısını denetimler yiyor, ekranın adına yer kalmıyordu.
        Native uygulamalarda üst çubuk başlık + en çok bir-iki eylem taşır;
        geri kalanı menüdedir. Bildirim üstte kaldı (rozetiyle birlikte
        anlamlı), ötekiler buraya indi.
      */}
      <SheetHeading>Görünüm</SheetHeading>
      <div className="flex flex-col gap-0.5">
        <SheetRow
          ikon={etkinTema === 'acik' ? <Moon size={19} strokeWidth={1.8} /> : <Sun size={19} strokeWidth={1.8} />}
          okYok
          onClick={temaDegistir}
        >
          {etkinTema === 'acik' ? 'Koyu tema' : 'Açık tema'}
        </SheetRow>
        <SheetRow
          ikon={<Palette size={19} strokeWidth={1.8} />}
          onClick={() => {
            kapat();
            temaPaneliAc();
          }}
        >
          Tema Tasarımcısı
        </SheetRow>
        <SheetRow
          ikon={<CircleHelp size={19} strokeWidth={1.8} />}
          onClick={() => {
            kapat();
            git('/yardim');
          }}
        >
          Yardım
        </SheetRow>
      </div>

      <SheetDivider />

      <SheetRow
        ikon={<LogOut size={19} strokeWidth={1.9} />}
        ton="tehlike"
        okYok
        onClick={() => {
          kapat();
          void signOut();
        }}
      >
        Çıkış Yap
      </SheetRow>
    </BottomSheet>
  );
}
