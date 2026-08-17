import { Pencil, User, Users } from 'lucide-react';
import { useState } from 'react';
import { Button } from '../../components/Button';
import { Card, CardHeader } from '../../components/Card';
import { EmptyState } from '../../components/EmptyState';
import { FieldWrapper } from '../../components/Field';
import { FormModal } from '../../components/FormModal';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { useUnitUsers } from '../../data/hooks';
import { useProjectMutations } from '../../data/projects';
import {
  PROJECT_MEMBER_ROLE_LABELS, type ProjectDetail, type ProjectMemberRequest,
} from '../../data/types';

/**
 * PROJE EKİBİ.
 *
 * <p>
 * Ayrı bir uçtan ve ayrı bir izinle (<code>proje.uyeYonet</code>) yazılıyor:
 * ekibi düzenlemek ile projenin tarihini ve bütçesini değiştirmek farklı
 * ağırlıkta işler. Proje yöneticisine ekibini kurma yetkisi verirken bütçeyi
 * de açmak zorunda kalmamak gerekiyor.
 * </p>
 *
 * <p>
 * <b>Proje yöneticisi ekibin üyesi olmalı</b> — projeyi yürüten kişinin
 * ekipte görünmemesi, "bu iş kimde?" sorusunu üye listesinden cevaplanamaz
 * kılardı. Sunucu da bunu reddediyor.
 * </p>
 */
export function ProjectTeam({ proje }: { proje: ProjectDetail }) {
  const { hasPermission } = useSession();
  const [acik, setAcik] = useState(false);

  const uyeler = proje.uyeler ?? [];
  const yetkili =
    hasPermission(PERMISSION.projeUyeYonet) || hasPermission(PERMISSION.projeYonet);

  return (
    <Card>
      <CardHeader
        baslik="Proje ekibi"
        aciklama={uyeler.length ? `${uyeler.length} kişi` : undefined}
        eylem={
          yetkili ? (
            <Button varyant="sade" onClick={() => setAcik(true)}>
              <Pencil size={14} />
              Düzenle
            </Button>
          ) : undefined
        }
      />

      {uyeler.length === 0 ? (
        <div className="px-3.5 pb-4">
          <EmptyState ikon={Users} baslik="Ekip kurulmamış" />
        </div>
      ) : (
        <ul className="divide-y divide-line">
          {uyeler.map((u) => (
            <li key={u.kullaniciId} className="flex items-center gap-2.5 px-3.5 py-2.5">
              <span className="grid h-7 w-7 flex-none place-items-center rounded-sm bg-sunken text-ink-3">
                <User size={14} />
              </span>
              <span className="min-w-0 flex-1 truncate text-sm text-ink">{u.ad}</span>
              <span className="shrink-0 text-2xs text-ink-3">
                {u.rolAd}
                {u.yoneticiMi && ' · proje yöneticisi'}
              </span>
            </li>
          ))}
        </ul>
      )}

      {acik && <EkipFormu proje={proje} kapat={() => setAcik(false)} />}
    </Card>
  );
}

function EkipFormu({ proje, kapat }: { proje: ProjectDetail; kapat: () => void }) {
  const { bildir } = useToast();
  const m = useProjectMutations(proje.id!);
  const kullanicilar = useUnitUsers();

  const [uyeler, setUyeler] = useState<ProjectMemberRequest[]>(
    (proje.uyeler ?? []).map((u) => ({ kullaniciId: u.kullaniciId!, rol: u.rol })),
  );
  const [yoneticiId, setYoneticiId] = useState<number | null>(proje.yoneticiId ?? null);

  const secili = new Map(uyeler.map((u) => [u.kullaniciId, u]));
  const yoneticiUye = !yoneticiId || secili.has(yoneticiId);
  const gecerli = yoneticiUye;

  function degistir(id: number, isaretli: boolean) {
    const yeni = isaretli
      ? [...uyeler, { kullaniciId: id, rol: 1 as never }]
      : uyeler.filter((u) => u.kullaniciId !== id);

    setUyeler(yeni);

    // Üyelikten çıkarılan kişi yönetici kalamaz: sunucu bu kaydı reddederdi
    // ve kullanıcı neden reddedildiğini formda göremezdi.
    if (!isaretli && yoneticiId === id) setYoneticiId(null);
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Proje ekibi"
      aciklama="Proje yöneticisi ekibin üyesi olmalı."
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            disabled={!gecerli || m.ekip.isPending}
            onClick={async () => {
              try {
                await m.ekip.mutateAsync({ yoneticiId, uyeler });
                bildir('basari', 'Proje ekibi güncellendi');
                kapat();
              } catch (h) {
                bildir('hata', 'Ekip kaydedilemedi', (h as Error).message);
              }
            }}
          >
            Kaydet
          </Button>
        </>
      }
    >
      <FieldWrapper
        etiket="Üyeler"
        id="proje-uyeler"
        ipucu="Listede olmayan kişi ekipten çıkarılır."
      >
        <div
          id="proje-uyeler"
          className="max-h-72 divide-y divide-line overflow-y-auto rounded-control border border-line"
        >
          {kullanicilar.liste.map((k) => {
            const u = secili.get(k.id!);
            return (
              <div
                key={k.id}
                className="flex min-h-11 items-center gap-2.5 px-3 py-2 hover:bg-sunken"
              >
                <input
                  type="checkbox"
                  id={`uye-${k.id}`}
                  checked={!!u}
                  onChange={(e) => degistir(k.id!, e.target.checked)}
                  className="h-4 w-4 accent-[var(--brand-ui)]"
                />
                <label htmlFor={`uye-${k.id}`} className="min-w-0 flex-1 cursor-pointer truncate text-sm text-ink">
                  {k.ad}
                </label>

                {u && (
                  <>
                    <select
                      value={u.rol ?? 1}
                      onChange={(e) =>
                        setUyeler(
                          uyeler.map((x) =>
                            x.kullaniciId === k.id
                              ? { ...x, rol: Number(e.target.value) as never }
                              : x,
                          ),
                        )
                      }
                      aria-label={`${k.ad} rolü`}
                      className="h-8 rounded-control border border-line bg-surface px-1.5 text-2xs text-ink outline-hidden"
                    >
                      {Object.entries(PROJECT_MEMBER_ROLE_LABELS).map(([d, e]) => (
                        <option key={d} value={d}>
                          {e}
                        </option>
                      ))}
                    </select>

                    <button
                      type="button"
                      onClick={() => setYoneticiId(yoneticiId === k.id ? null : k.id!)}
                      className={`shrink-0 rounded-full px-2 py-0.5 text-3xs ${
                        yoneticiId === k.id
                          ? 'bg-brand-ui text-white'
                          : 'bg-sunken text-ink-3 hover:text-ink-2'
                      }`}
                    >
                      {yoneticiId === k.id ? 'Yönetici' : 'Yönetici yap'}
                    </button>
                  </>
                )}
              </div>
            );
          })}
        </div>
      </FieldWrapper>
    </FormModal>
  );
}
