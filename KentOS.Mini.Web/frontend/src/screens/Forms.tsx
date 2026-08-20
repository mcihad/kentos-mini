import {
  BarChart3, Copy, ExternalLink, FileText, Link2, Plus, Trash2,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../components/Button';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { EmptyState } from '../components/EmptyState';
import { SearchInput } from '../components/Field';
import { RowActions } from '../components/RowActions';
import { Pagination } from '../components/Pagination';
import { Skeleton } from '../components/Skeleton';
import { ColoredBadge } from '../components/Color';
import { Tabs } from '../components/Tabs';
import { useToast } from '../components/Toast';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { useForms, useFormMutations } from '../data/forms';
import { shortDate } from '../data/format';
import { FORM_STATUS } from '../forms/fieldTypes';

/**
 * FORM VE ANKET LİSTESİ.
 *
 * <p>
 * Sekmeler durum süzgeci: yayındakiler günlük işin merkezinde, taslaklar
 * ayrı. "Tümü" varsayılan olsaydı yayınlanmış bir formu bulmak için her
 * seferinde taslakların arasından ayıklamak gerekirdi.
 * </p>
 */
export default function Forms() {
  const { hasPermission } = useSession();
  const { bildir } = useToast();
  const gezin = useNavigate();

  const [sekme, setSekme] = useState<string>(String(FORM_STATUS.yayinda));
  const [arama, setArama] = useState('');
  const [sayfa, setSayfa] = useState(1);
  const [silinecek, setSilinecek] = useState<{ id: number; ad: string } | null>(null);

  const liste = useForms({
    arama: arama || undefined,
    durum: sekme === 'hepsi' ? undefined : Number(sekme),
    sayfa,
    boyut: 25,
  });

  const m = useFormMutations();
  const yonetebilir = hasPermission(PERMISSION.formYonet);

  async function baglantiKopyala(adres: string | null | undefined) {
    if (!adres) return;
    await navigator.clipboard.writeText(adres);
    bildir('basari', 'Bağlantı kopyalandı', 'Vatandaşla paylaşabilirsiniz.');
  }

  return (
    <div className="space-y-3.5">
      <div className="flex flex-wrap items-center gap-2">
        <SearchInput
          className="min-w-0 flex-1"
          value={arama}
          onChange={(e) => { setArama(e.target.value); setSayfa(1); }}
          placeholder="Form adı ara"
        />
        {yonetebilir && (
          <Link to="/formlar/yeni">
            <Button><Plus size={15} />Yeni form</Button>
          </Link>
        )}
      </div>

      <Tabs<string>
        deger={sekme}
        degistir={(d) => { setSekme(d); setSayfa(1); }}
        sekmeler={[
          { deger: String(FORM_STATUS.yayinda), etiket: 'Yayında' },
          { deger: String(FORM_STATUS.taslak), etiket: 'Taslak' },
          { deger: String(FORM_STATUS.kapali), etiket: 'Kapalı' },
          { deger: 'hepsi', etiket: 'Tümü' },
        ]}
      />

      {liste.isLoading && <Skeleton className="h-40" />}

      {liste.data && liste.data.veriler.length === 0 && (
        <EmptyState
          ikon={FileText}
          baslik="Form yok"
          aciklama="Vatandaştan geri bildirim almak için bir form ya da anket oluşturun."
          eylem={yonetebilir ? (
            <Link to="/formlar/yeni"><Button><Plus size={14} />Yeni form</Button></Link>
          ) : undefined}
        />
      )}

      {liste.data && liste.data.veriler.length > 0 && (
        <>
          {/*
            SATIR KENDİ YAZILDI, `ListRow` DEĞİL.

            `ListRow` tıklanabilir olduğunda satırın TAMAMINI bir düğmeye
            çeviriyor; eylem düğmeleri onun içine düşünce `button > button`
            oluyor ve bu geçersiz HTML — tarayıcı davranışı tanımsız.
            Depodaki zengin satır grameri: açıcı düğme ile eylem grubu
            KARDEŞ.
          */}
          <ul className="overflow-hidden rounded-lg border border-line bg-surface">
            {liste.data.veriler.map((f) => (
              <li
                key={f.id}
                className="flex items-start gap-2 border-b border-line px-3 py-2.5 last:border-0"
              >
                <button
                  type="button"
                  onClick={() => gezin(`/formlar/${f.id}`)}
                  className="min-w-0 flex-1 text-left"
                >
                  <span className="flex flex-wrap items-center gap-1.5 text-xs">
                    <ColoredBadge
                      etiket={f.durumAd ?? ''}
                      renk={
                        f.durum === FORM_STATUS.yayinda ? '#1E874B'
                          : f.durum === FORM_STATUS.taslak ? '#B7791F' : '#8A8F98'
                      }
                    />
                    <span className="text-ink-3">{f.erisimAd}</span>
                    {!f.yanitAliyor && f.durum === FORM_STATUS.yayinda && (
                      <span className="text-(--st-wait)">· {f.kapaliSebebi}</span>
                    )}
                  </span>

                  <span className="mt-0.5 block truncate text-base font-semibold">
                    {f.baslik}
                  </span>

                  <span className="block text-sm tabular-nums text-ink-3">
                    {f.yanitSayisi} yanıt
                    {f.yanitSiniri ? ` / ${f.yanitSiniri}` : ''}
                    {f.bitisTarihi ? ` · ${shortDate(f.bitisTarihi)} tarihine kadar` : ''}
                  </span>
                </button>

                <RowActions
                  boyut="kucuk"
                  eylemler={[
                    ...(f.paylasimAdresi ? [
                      { etiket: 'Bağlantıyı kopyala', ikon: Link2,
                        onClick: () => baglantiKopyala(f.paylasimAdresi) },
                      { etiket: 'Vatandaş gibi aç', ikon: ExternalLink,
                        onClick: () => window.open(f.paylasimAdresi!, '_blank', 'noopener') },
                    ] : []),
                    { etiket: 'Yanıtlar', ikon: BarChart3,
                      onClick: () => gezin(`/formlar/${f.id}/yanitlar`) },
                    ...(yonetebilir ? [
                      { etiket: 'Kopyala', ikon: Copy, onClick: async () => {
                        const k = await m.kopyala.mutateAsync(f.id!);
                        bildir('basari', 'Kopyalandı');
                        gezin(`/formlar/${k.id}`);
                      } },
                      { etiket: 'Sil', ikon: Trash2, ton: 'tehlike' as const,
                        onClick: () => setSilinecek({ id: f.id!, ad: f.baslik ?? '' }) },
                    ] : []),
                  ]}
                />
              </li>
            ))}
          </ul>

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="form" />
        </>
      )}

      <ConfirmDialog
        acik={!!silinecek}
        kapat={() => setSilinecek(null)}
        baslik="Form silinsin mi?"
        aciklama={`"${silinecek?.ad}" arşive alınır. GELEN YANITLAR SİLİNMEZ — form arşivde durmaya devam eder.`}
        onayEtiketi="Arşive al"
        yikici
        onayla={async () => {
          try {
            await m.sil.mutateAsync(silinecek!.id);
            bildir('basari', 'Arşive alındı');
          } catch (h) {
            bildir('hata', 'Silinemedi', (h as Error).message);
          }
        }}
      />
    </div>
  );
}
