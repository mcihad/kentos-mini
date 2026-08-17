import { ClipboardCheck, Map, PlusCircle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Card } from '../../components/Card';
import { ColoredBadge } from '../../components/Color';
import { EmptyState } from '../../components/EmptyState';
import { SkeletonRows } from '../../components/Skeleton';
import { useMyFieldWork } from '../../data/citizen';
import { SlaBadge, StageProgress } from '../task/TaskBits';

/**
 * SAHA ANA EKRANI — "sıradaki iş ne?"
 *
 * <p>
 * Kabuksuz yerleşimde: kenar çubuğu, sekme çubuğu ve bildirim ikonu yok.
 * Saha personeli telefonu tek elle, güneş altında, eldivenle kullanıyor;
 * ekranın tamamı işe ayrılmalı.
 * </p>
 *
 * <p>
 * <b>Liste EN AZ VAKTİ KALAN önce.</b> Sahadaki kişi "hangisi acil?" diye
 * soruyor; tarih sıralaması ona bir şey söylemiyordu.
 * </p>
 *
 * <p>
 * İki büyük düğme: <b>tespit</b> ve <b>harita</b>. Üçüncü bir seçenek
 * koymak, hareket hâlindeki birine karar verdirtmek olurdu.
 * </p>
 */
export default function FieldHome() {
  const { data: isler, isLoading } = useMyFieldWork();

  return (
    <div className="space-y-3.5">
      <h1 className="font-display text-xl font-bold text-ink">Saha</h1>

      <div className="grid grid-cols-2 gap-2.5">
        <Link to="/saha/tespit">
          <Card className="flex h-24 flex-col items-center justify-center gap-1.5 p-3 active:bg-sunken">
            <PlusCircle size={26} className="text-brand-ui" />
            <span className="text-sm font-medium text-ink">Tespit gir</span>
          </Card>
        </Link>

        <Link to="/harita">
          <Card className="flex h-24 flex-col items-center justify-center gap-1.5 p-3 active:bg-sunken">
            <Map size={26} className="text-brand-ui" />
            <span className="text-sm font-medium text-ink">Harita</span>
          </Card>
        </Link>
      </div>

      <div>
        <h2 className="mb-2 px-1 font-display text-base font-bold text-ink">
          Üzerimdeki işler
          {isler && isler.length > 0 && (
            <span className="ml-2 text-2xs font-normal text-ink-3">{isler.length}</span>
          )}
        </h2>

        {isLoading ? (
          <SkeletonRows adet={4} />
        ) : !isler || isler.length === 0 ? (
          <EmptyState
            ikon={ClipboardCheck}
            baslik="Açık işiniz yok"
            aciklama="Size ya da ekibinize atanan bir görev bulunmuyor."
          />
        ) : (
          <ul className="space-y-2">
            {isler.map((g) => (
              <li key={g.id}>
                <Link to={`/saha/gorev/${g.id}`}>
                  {/* Kart yüksekliği CÖMERT: eldivenli parmak küçük hedefi
                      ıskalıyor ve yanlış işi açmak sahada pahalı. */}
                  <Card className="p-3.5 active:bg-sunken">
                    <div className="flex items-start gap-2">
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-mono text-2xs tabular-nums text-ink-3">
                            {g.takipNo}
                          </span>
                          <SlaBadge gecikti={!!g.gecikti} kalanSaat={g.kalanSaat} />
                        </div>
                        <p className="mt-1 line-clamp-2 text-base leading-snug text-ink">
                          {g.baslik}
                        </p>
                        {g.adres && (
                          <p className="mt-1 truncate text-xs text-text-3">{g.adres}</p>
                        )}
                        <div className="mt-2">
                          <StageProgress biten={g.asamaBiten ?? 0} toplam={g.asamaToplam ?? 0} />
                        </div>
                      </div>

                      <ColoredBadge etiket={g.durumAd} renk={g.durumRenk} />
                    </div>
                  </Card>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
