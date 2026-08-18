import { Plus, User, Users, X } from 'lucide-react';
import { useState } from 'react';
import { Button, IconButton } from '../../components/Button';
import { Card, CardHeader } from '../../components/Card';
import { EmptyState } from '../../components/EmptyState';
import { FormModal } from '../../components/FormModal';
import { FieldWrapper, Secim } from '../../components/Field';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { Avatar, PersonPicker } from '../../components/PersonPicker';
import { useAssignableTeams, useTaskMutations } from '../../data/tasks';
import {
  TASK_ASSIGNMENT_ROLE, type TaskAssignRequest, type TaskDetail,
} from '../../data/types';

const ROL_ADLARI: Record<number, string> = {
  0: 'Sorumlu',
  1: 'Yardımcı',
  2: 'İzleyici',
};

/**
 * ATAMALAR — "bu iş kimde?"
 *
 * <p>
 * Kişi ve ekip aynı listede. Ekibe atandığında bildirim <b>önce ekip
 * liderine</b> gidiyor; iş dağıtımını lider yapıyor. Lider yoksa ekibin
 * tamamına — kimsenin bilmediği bir atama, atanmamış görevle aynı şey.
 * </p>
 *
 * <p>
 * <b>Liste tam olarak gönderiliyor.</b> Tek tek ekle/çıkar uçları açmak,
 * yarısı başarısız olmuş bir dizi istek sonrası görevi kimin yürüttüğü
 * belirsiz bir durumda bırakırdı.
 * </p>
 */
export function TaskAssignments({ gorev }: { gorev: TaskDetail }) {
  const { bildir } = useToast();
  const { hasPermission } = useSession();
  const m = useTaskMutations(gorev.id!);

  const [ekleAcik, setEkleAcik] = useState(false);
  const [tur, setTur] = useState<'kisi' | 'ekip'>('kisi');
  const [hedefId, setHedefId] = useState<number | null>(null);
  const [rol, setRol] = useState<number>(TASK_ASSIGNMENT_ROLE.sorumlu);

  const ekipler = useAssignableTeams();

  const atamalar = gorev.atamalar ?? [];
  const yetkili = hasPermission(PERMISSION.gorevAtama);
  const kapali = (gorev.sonrakiDurumlar ?? []).length === 0;

  /** Mevcut atamaları koruyarak yeni satır ekler ya da bir satırı çıkarır. */
  async function yaz(yeni: TaskAssignRequest[]) {
    try {
      await m.ata.mutateAsync(yeni);
      bildir('basari', 'Atamalar güncellendi');
      setEkleAcik(false);
      setHedefId(null);
    } catch (h) {
      bildir('hata', 'Atama yapılamadı', (h as Error).message);
    }
  }

  const mevcut: TaskAssignRequest[] = atamalar.map((a) => ({
    kullaniciId: a.kullaniciId ?? null,
    ekipId: a.ekipId ?? null,
    rol: a.rol,
  }));

  return (
    <Card>
      <CardHeader
        baslik="Atamalar"
        eylem={
          yetkili && !kapali ? (
            <Button varyant="sade" onClick={() => setEkleAcik(true)}>
              <Plus size={14} />
              Ata
            </Button>
          ) : undefined
        }
      />

      {atamalar.length === 0 ? (
        <div className="px-3.5 pb-4">
          {/* Boş durum kendisini dolduran eylemi taşır — "atanmadı" deyip
              atama yolunu göstermemek, kullanıcıyı başlığa geri gönderir. */}
          <EmptyState
            ikon={User}
            baslik="Atanmadı"
            aciklama="Görev başlatılabilmesi için önce bir sorumlu atanmalı."
            eylem={
              yetkili && !kapali ? (
                <Button onClick={() => setEkleAcik(true)}>
                  <Plus size={14} />
                  Sorumlu ata
                </Button>
              ) : undefined
            }
          />
        </div>
      ) : (
        <ul className="divide-y divide-line">
          {atamalar.map((a) => (
            <li key={a.id} className="flex items-center gap-2.5 px-3.5 py-2.5">
              {a.ekipId ? (
                <span className="grid h-7 w-7 flex-none place-items-center rounded-sm bg-sunken text-ink-3">
                  <Users size={14} />
                </span>
              ) : (
                <Avatar ad={a.kullaniciAd ?? ''} boyut="kucuk" />
              )}
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm text-ink">
                  {a.kullaniciAd || a.ekipAd || '—'}
                </span>
                <span className="text-2xs text-ink-3">
                  {ROL_ADLARI[a.rol ?? 0]}
                  {a.atayan && ` · ${a.atayan} atadı`}
                </span>
              </span>
              {yetkili && !kapali && (
                <IconButton
                  etiket="Atamayı kaldır"
                  onClick={() =>
                    yaz(
                      mevcut.filter(
                        (x) =>
                          !(
                            x.kullaniciId === (a.kullaniciId ?? null) &&
                            x.ekipId === (a.ekipId ?? null) &&
                            x.rol === a.rol
                          ),
                      ),
                    )
                  }
                >
                  <X size={16} />
                </IconButton>
              )}
            </li>
          ))}
        </ul>
      )}

      <FormModal
        acik={ekleAcik}
        kapat={() => setEkleAcik(false)}
        baslik="Görevi ata"
        aciklama="Ekibe atandığında bildirim önce ekip liderine gider."
        eylemler={
          <>
            <Button varyant="ikincil" onClick={() => setEkleAcik(false)}>
              Vazgeç
            </Button>
            <Button
              disabled={!hedefId || m.ata.isPending}
              onClick={() =>
                yaz([
                  ...mevcut,
                  tur === 'kisi'
                    ? { kullaniciId: hedefId, rol: rol as never }
                    : { ekipId: hedefId, rol: rol as never },
                ])
              }
            >
              Ata
            </Button>
          </>
        }
      >
        <FieldWrapper etiket="Kime" id="atama-tur">
          <Secim
            id="atama-tur"
            value={tur}
            onChange={(e) => {
              setTur(e.target.value as 'kisi' | 'ekip');
              setHedefId(null);
            }}
          >
            <option value="kisi">Kişi</option>
            <option value="ekip">Ekip</option>
          </Secim>
        </FieldWrapper>

        <FieldWrapper etiket={tur === 'kisi' ? 'Personel' : 'Ekip'} id="atama-hedef" zorunlu>
          {/*
            KİŞİ SEÇİMİ ARANABİLİR, EKİP SEÇİMİ AÇILIR KUTU.

            Personel sayısı yüzlerce olabilir ve <select> içinde aranamaz;
            üstelik eski liste oturum sahibini ve alt birimleri hiç
            içermiyordu — kişi görevi kendine bile atayamıyordu. Ekip sayısı
            ise bir birimde birkaç tane: orada açılır kutu doğru araç.
          */}
          {tur === 'kisi' ? (
            <PersonPicker
              id="atama-hedef"
              tekli
              secili={hedefId ? [hedefId] : []}
              degistir={(idler) => setHedefId(idler[0] ?? null)}
            />
          ) : (
            <Secim
              id="atama-hedef"
              value={hedefId ?? ''}
              onChange={(e) => setHedefId(e.target.value ? Number(e.target.value) : null)}
            >
              <option value="">Seçin</option>
              {ekipler.liste.map((e) => (
                <option key={e.id} value={e.id!}>
                  {e.ad}
                  {e.liderAd ? ` (lider: ${e.liderAd})` : ''}
                </option>
              ))}
            </Secim>
          )}
        </FieldWrapper>

        <FieldWrapper
          etiket="Rol"
          id="atama-rol"
          ipucu="İzleyiciler bildirim alır ama işin sorumlusu sayılmaz."
        >
          <Secim id="atama-rol" value={rol} onChange={(e) => setRol(Number(e.target.value))}>
            {Object.entries(ROL_ADLARI).map(([d, e]) => (
              <option key={d} value={d}>
                {e}
              </option>
            ))}
          </Secim>
        </FieldWrapper>
      </FormModal>
    </Card>
  );
}
