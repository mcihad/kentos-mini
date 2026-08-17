import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, Share2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { FieldWrapper, SearchInput, Textarea } from '../../components/Field';
import { Button } from '../../components/Button';
import { FormModal } from '../../components/FormModal';
import { useToast } from '../../components/Toast';
import { initials } from '../../data/format';
import { api } from '../../data/client';
import { useUnitUsers } from '../../data/hooks';
import type { ResumeSummary } from '../../data/types';

/**
 * Yönlendirme penceresi.
 *
 * <p>
 * "Bu iş için bizde uygun kişi var" demenin yolu: kaydı ilgili kişiye
 * yönlendirmek. Dosyayı e-postayla göndermek kaydı havuzun dışına çıkarıyor
 * ve kimin kime ne gönderdiği kayboluyordu; burada paylaşım kaydı kalıyor,
 * alıcı bildirim alıyor ve <b>geçmiş özgeçmişin tabakasında görünüyor</b>.
 * </p>
 *
 * <p>
 * Seçim ONAY KUTUSUYLA: bu bir aç/kapa değil, listeden işaretleme. Anahtar
 * (switch) tek bir şeyi açıp kapatmak için.
 * </p>
 */
export function ShareDialog({
  kayit,
  kapat,
}: {
  kayit: ResumeSummary;
  kapat: () => void;
}) {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const kullanicilar = useUnitUsers();

  const [secili, setSecili] = useState<number[]>([]);
  const [not, setNot] = useState('');
  const [ara, setAra] = useState('');

  const suzulmus = useMemo(() => {
    const k = ara.trim().toLocaleLowerCase('tr');
    const hepsi = kullanicilar.liste;
    if (!k) return hepsi;
    return hepsi.filter((x) =>
      [x.tamAd, x.unvan, x.birimAd].some((a) => (a ?? '').toLocaleLowerCase('tr').includes(k)),
    );
  }, [ara, kullanicilar.liste]);

  const paylas = useMutation({
    mutationFn: () =>
      api.post<{ adet: number }>(`/ozgecmis/${kayit.id}/paylas`, {
        aliciIdler: secili,
        not: not || null,
      }),
    onSuccess: (s) => {
      qc.invalidateQueries({ queryKey: ['ozgecmis'] });
      bildir('basari', `${s?.adet ?? secili.length} kişiye yönlendirildi`);
      kapat();
    },
    onError: (h: Error) => bildir('hata', 'Paylaşılamadı', h.message),
  });

  return (
    <FormModal
      acik
      kapat={kapat}
      baslik="Özgeçmişi yönlendir"
      aciklama={`${kayit.adSoyad} — seçtiğiniz kişiler bildirim alır ve kaydı havuzda açabilir.`}
      ikon={<Share2 size={15} />}
      genislik="dar"
      altBilgi={secili.length > 0 ? `${secili.length} kişi seçildi` : 'Kişi seçin'}
      eylemler={
        <>
          <Button varyant="ikincil" onClick={kapat}>
            Vazgeç
          </Button>
          <Button
            onClick={() => secili.length > 0 && paylas.mutate()}
            disabled={secili.length === 0 || paylas.isPending}
          >
            {paylas.isPending ? 'Gönderiliyor…' : 'Yönlendir'}
          </Button>
        </>
      }
    >
      <SearchInput
        value={ara}
        onChange={(e) => setAra(e.target.value)}
        placeholder="Kişi, unvan veya birim"
        aria-label="Kişi ara"
        ikon={<Search size={15} />}
      />

      {/*
        Satırlar 48px: dokunma hedefi. Önce 36px'ti ve fareyle tasarlanmıştı;
        telefonda yanlış kişiyi işaretlemek sık oluyordu.
      */}
      <ul className="max-h-[320px] divide-y divide-line overflow-y-auto overscroll-contain rounded-control border border-border">
        {suzulmus.length === 0 && (
          <li className="px-3 py-6 text-center text-sm text-text-3">Eşleşen kişi yok</li>
        )}
        {suzulmus.map((k) => {
          const isaretli = secili.includes(k.id!);
          return (
            <li key={k.id}>
              <label
                className={`flex min-h-12 cursor-pointer items-center gap-2.5 px-3 py-2 transition-colors
                  ${isaretli ? 'bg-brand-soft' : 'active:bg-sunken md:hover:bg-surface-2'}`}
              >
                <input
                  type="checkbox"
                  checked={isaretli}
                  onChange={(e) =>
                    setSecili((s) =>
                      e.target.checked ? [...s, k.id!] : s.filter((x) => x !== k.id),
                    )
                  }
                  className="h-[18px] w-[18px] shrink-0"
                />
                <span
                  aria-hidden
                  className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-surface-2 font-display text-3xs font-bold text-text-2"
                >
                  {initials(...(k.tamAd ?? '').split(' '))}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm">{k.tamAd}</span>
                  <span className="block truncate text-2xs text-text-3">
                    {[k.unvan, k.birimAd].filter(Boolean).join(' · ')}
                  </span>
                </span>
              </label>
            </li>
          );
        })}
      </ul>

      <FieldWrapper etiket="Not" id="oz-paylas-not" ipucu="Neden yönlendirdiğinizi yazın">
        <Textarea
          id="oz-paylas-not"
          value={not}
          onChange={(e) => setNot(e.target.value)}
          rows={2}
          placeholder="Fen İşleri'ndeki kaynakçı ilanı için uygun olabilir."
        />
      </FieldWrapper>
    </FormModal>
  );
}
