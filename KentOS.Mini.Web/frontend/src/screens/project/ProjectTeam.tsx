import { Crown, Pencil, UserPlus, Users } from 'lucide-react';
import { useState } from 'react';
import { Button } from '../../components/Button';
import { Card, CardHeader } from '../../components/Card';
import { EmptyState } from '../../components/EmptyState';
import { FieldWrapper } from '../../components/Field';
import { FormModal } from '../../components/FormModal';
import { Avatar, PersonPicker } from '../../components/PersonPicker';
import { useToast } from '../../components/Toast';
import { PERMISSION } from '../../components/permissions';
import { useSession } from '../../auth/SessionProvider';
import { useProjectMutations } from '../../data/projects';
import { usePeople } from '../../data/tasks';
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
 * kılardı. Sunucu da bunu reddediyor. Bu kural bir süre <b>uygulanamaz</b>
 * durumdaydı: seçim listesi oturum sahibini dışarıda bırakıyordu, yani kişi
 * yönettiği projeye kendini ekleyemiyordu. Kaynağı ve ölçümü
 * <code>usePeople</code> üzerinde yazılı.
 * </p>
 */
export function ProjectTeam({ proje }: { proje: ProjectDetail }) {
  const { hasPermission } = useSession();
  const [acik, setAcik] = useState(false);

  const uyeler = proje.uyeler ?? [];
  const yetkili =
    hasPermission(PERMISSION.projeUyeYonet) || hasPermission(PERMISSION.projeYonet);

  return (
    <Card serit>
      <CardHeader
        baslik="Proje ekibi"
        aciklama={uyeler.length ? `${uyeler.length} kişi` : undefined}
        eylem={
          yetkili && uyeler.length > 0 ? (
            <Button varyant="sade" onClick={() => setAcik(true)}>
              <Pencil size={14} />
              Düzenle
            </Button>
          ) : undefined
        }
      />

      {uyeler.length === 0 ? (
        <div className="px-3.5 pb-4">
          {/*
            BOŞ DURUM KENDİSİNİ DOLDURAN EYLEMİ TAŞIR. Önceki hâli yalnızca
            "Ekip kurulmamış" yazıyordu ve üye eklemenin tek yolu, boş durumda
            hiç çizilmeyen başlık düğmesiydi: ekranda ekip kurmaya davet eden
            hiçbir şey yoktu.
          */}
          <EmptyState
            ikon={Users}
            baslik="Ekip kurulmamış"
            aciklama="Projeyi kimlerin yürüteceğini seçin; yönetici de ekibin üyesi olmalı."
            eylem={
              yetkili ? (
                <Button onClick={() => setAcik(true)}>
                  <UserPlus size={14} />
                  Ekip kur
                </Button>
              ) : undefined
            }
          />
        </div>
      ) : (
        <ul className="divide-y divide-line">
          {uyeler.map((u) => (
            <li key={u.kullaniciId} className="flex items-center gap-2.5 px-3.5 py-2.5">
              <Avatar ad={u.ad ?? ''} boyut="kucuk" />
              <span className="min-w-0 flex-1 truncate text-sm text-ink">{u.ad}</span>
              {u.yoneticiMi && (
                <Crown size={13} className="shrink-0 text-brand" strokeWidth={2.4} />
              )}
              <span className="shrink-0 text-2xs text-ink-3">{u.rolAd}</span>
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

  const [uyeler, setUyeler] = useState<ProjectMemberRequest[]>(
    (proje.uyeler ?? []).map((u) => ({ kullaniciId: u.kullaniciId!, rol: u.rol })),
  );
  const [yoneticiId, setYoneticiId] = useState<number | null>(proje.yoneticiId ?? null);

  /*
    ROL SATIRLARI İÇİN AD LAZIM ve seçici arama yaptıkça listesi daralıyor.
    Aynı sorgu React Query'de önbellekli, ikinci bir istek atmıyor; buradaki
    boş arama, seçicinin ilk yüklemesiyle aynı anahtarı paylaşıyor.
  */
  const { liste: personel } = usePeople('');
  const adBul = (id: number) =>
    personel.find((k) => k.id === id)?.ad
    ?? (proje.uyeler ?? []).find((u) => u.kullaniciId === id)?.ad
    ?? `#${id}`;

  const secili = uyeler.map((u) => u.kullaniciId!).filter((x): x is number => x != null);
  const yoneticiUye = !yoneticiId || secili.includes(yoneticiId);

  function uyeleriYaz(idler: number[]) {
    // Var olan roller KORUNUYOR: seçiciden geçen bir kişi "İzleyici" ise
    // listeyi yeniden kurmak onu sessizce "Üye"ye çevirirdi.
    setUyeler(
      idler.map(
        (id) =>
          uyeler.find((u) => u.kullaniciId === id) ?? { kullaniciId: id, rol: 1 as never },
      ),
    );

    if (yoneticiId && !idler.includes(yoneticiId)) setYoneticiId(null);
  }

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Proje ekibi"
      aciklama="Proje yöneticisi ekibin üyesi olmalı."
      altBilgi={
        !yoneticiUye ? 'Yönetici, üyeler arasında olmalı.' : `${secili.length} kişi`
      }
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            disabled={!yoneticiUye || m.ekip.isPending}
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
      <FieldWrapper etiket="Üyeler" id="proje-uyeler">
        <PersonPicker
          id="proje-uyeler"
          secili={secili}
          degistir={uyeleriYaz}
          liderId={yoneticiId}
          liderDegistir={setYoneticiId}
          liderEtiketi="Yönetici"
        />
      </FieldWrapper>

      {/*
        ROLLER AYRI BİR BÖLÜMDE, SEÇİCİNİN İÇİNDE DEĞİL.

        Rol açılır kutusunu satır içine koymak, aramayla daralan bir listede
        rolü görünmez yapıyordu: kullanıcı "Ahmet" yazınca Ayşe'nin rolünü
        artık göremiyordu. Roller yalnızca SEÇİLİ kişiler için sorulur ve
        seçim ne olursa olsun burada durur.
      */}
      {secili.length > 0 && (
        <FieldWrapper etiket="Roller" id="proje-roller">
          <ul id="proje-roller" className="divide-y divide-line rounded-control border border-line">
            {uyeler.map((u) => (
              <li key={u.kullaniciId} className="flex min-h-11 items-center gap-2.5 px-3 py-2">
                <Avatar ad={adBul(u.kullaniciId!)} boyut="kucuk" />
                <span className="min-w-0 flex-1 truncate text-sm text-ink">
                  {adBul(u.kullaniciId!)}
                </span>
                {yoneticiId === u.kullaniciId && (
                  <Crown size={13} className="shrink-0 text-brand" strokeWidth={2.4} />
                )}
                <select
                  value={u.rol ?? 1}
                  onChange={(e) =>
                    setUyeler(
                      uyeler.map((x) =>
                        x.kullaniciId === u.kullaniciId
                          ? { ...x, rol: Number(e.target.value) as never }
                          : x,
                      ),
                    )
                  }
                  aria-label={`${adBul(u.kullaniciId!)} rolü`}
                  className="h-8 shrink-0 rounded-control border border-line bg-surface px-1.5 text-2xs text-ink outline-hidden"
                >
                  {Object.entries(PROJECT_MEMBER_ROLE_LABELS).map(([d, e]) => (
                    <option key={d} value={d}>
                      {e}
                    </option>
                  ))}
                </select>
              </li>
            ))}
          </ul>
        </FieldWrapper>
      )}
    </FormModal>
  );
}
