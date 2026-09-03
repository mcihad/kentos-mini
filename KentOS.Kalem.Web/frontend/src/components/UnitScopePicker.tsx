import { Building2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { activeUnitStore } from '../data/client';
import { useUnitScope } from '../data/tasks';
import { useSession } from '../auth/SessionProvider';
import { PERMISSION } from './permissions';
import { Secim } from './Field';
import { cn } from './utils';

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
 * hangi verinin kimin olduğunu ayırt edemez.
 * </p>
 *
 * <h4>Ölçü ORTAK KONTROLDEN gelir</h4>
 *
 * <p>
 * Bileşen kendi <code>&lt;select&gt;</code>'ini kuruyor ve girdi
 * sınıflarını elle kopyalıyordu; sonuç, <b>mobilde 40px</b> yüksekliğinde
 * bir kutunun yanında <b>50px</b>'lik arama alanı ve düğmelerdi — araç
 * çubuğu her ekranda hizasız görünüyordu. Artık <see cref="Secim"/>
 * kullanıyor: ölçü, köşe, odak halkası ve zemin tek yerden geliyor.
 * </p>
 *
 * <p>
 * Dosya da <code>screens/task/</code> altından <code>components/</code>'e
 * taşındı — altı ekran kullanıyor, hiçbiri göreve özel değil.
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
    <div className={cn('relative min-w-0', className)}>
      {/*
        İkon kutunun İÇİNDE, yanında değil. Dışarıdayken bileşenin toplam
        genişliği ikonun kendisi kadar artıyor ve dar ekranda araç çubuğu
        bir satır daha kırıyordu.
      */}
      <Building2
        size={15}
        aria-hidden
        className="pointer-events-none absolute left-3 top-1/2 z-10 -translate-y-1/2 text-ink-3"
      />
      <Secim
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
        className="w-full pl-9 md:max-w-[240px]"
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
      </Secim>
    </div>
  );
}
