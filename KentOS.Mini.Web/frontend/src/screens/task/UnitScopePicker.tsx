import { Building2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { activeUnitStore } from '../../data/client';
import { useUnitScope } from '../../data/tasks';
import { useSession } from '../../auth/SessionProvider';
import { PERMISSION } from '../../components/permissions';

/**
 * ETKİN BİRİM SEÇİCİ — "hangi birim adına çalışıyorum?"
 *
 * <p>
 * Başkan yardımcısı bağlı bir müdürlüğü seçtiğinde iş takip ekranları o
 * müdürlüğün kayıtlarını gösterir. Seçim <code>localStorage</code>'a yazılır
 * ve <code>X-Etkin-Birim</code> başlığıyla her isteğe eklenir.
 * </p>
 *
 * <p>
 * <b>Seçim değişince BÜTÜN önbellek düşürülür.</b> Aksi hâlde kullanıcı
 * müdürlüğe geçtiğinde bir an kendi biriminin listesini görmeye devam eder ve
 * hangi verinin kimin olduğunu ayırt edemez — yanlış birimin listesine bakıp
 * "burada iş yok" demek, hata mesajı görmekten kötüdür.
 * </p>
 *
 * <p>
 * İzni olmayanda ya da alt birimi olmayanda HİÇ ÇİZİLMEZ: tek seçenekli bir
 * açılır liste, seçim varmış izlenimi veren boş bir kontroldür.
 * </p>
 */
export function UnitScopePicker({ className }: { className?: string }) {
  const { hasPermission } = useSession();
  const qc = useQueryClient();

  const yetkili = hasPermission(PERMISSION.gorevBirimKapsam);
  const { liste } = useUnitScope(yetkili);

  if (!yetkili || liste.length < 2) return null;

  const kendi = liste.find((b) => b.kendiBirimi);
  const secili = activeUnitStore.read() ?? kendi?.id ?? null;

  return (
    <label className={`inline-flex h-ctrl items-center gap-1.5 ${className ?? ''}`}>
      <span className="sr-only">Etkin birim</span>
      <Building2 size={15} className="flex-none text-ink-3" aria-hidden />
      <select
        value={secili ?? ''}
        onChange={(e) => {
          const id = Number(e.target.value);

          // Kendi birimi seçildiğinde başlık HİÇ gönderilmiyor: sunucu
          // başlıksız isteği zaten kullanıcının kendi birimi sayıyor ve
          // gereksiz bir vekâlet izi bırakmamak daha doğru.
          activeUnitStore.write(id === kendi?.id ? null : id);
          qc.clear();
        }}
        aria-label="Etkin birim"
        className="h-ctrl min-w-0 max-w-[210px] rounded-control border border-line bg-surface px-2 text-sm text-ink outline-hidden focus-visible:border-brand"
      >
        {liste.map((b) => (
          <option key={b.id} value={b.id}>
            {/* Girinti ağaçtaki derinliği gösteriyor: "Park Müdürlüğü"nün
                kime bağlı olduğu düz bir listede kayboluyordu. */}
            {'  '.repeat(b.derinlik ?? 0)}
            {b.ad}
            {b.kendiBirimi ? ' (birimim)' : ''}
          </option>
        ))}
      </select>
    </label>
  );
}
