import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, Check, Copy, PlugZap } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Button } from '../components/Button';
import { FieldWrapper, Input } from '../components/Field';
import { FormSection } from '../components/FormSection';
import { Switch } from '../components/Switch';
import { useToast } from '../components/Toast';
import { api } from '../data/client';

/**
 * KURUMSAL KİMLİK SAĞLAYICI AYARLARI.
 *
 * Kurum kendi hesap altyapısını (Keycloak, Azure AD, kurum içi kimlik
 * sunucusu) kullanıyorsa personel ayrı bir şifre taşımak zorunda kalmaz.
 * Ayar veritabanında: sağlayıcı değiştiğinde sunucuya girip dosya düzenlemek
 * ve uygulamayı yeniden başlatmak gerekmiyor.
 *
 * Ekranın en kritik iki parçası, ikisi de yapılandırmanın en sık yanlış
 * giren yerleri olduğu için var:
 *
 *  1. **Kopyalanabilir dönüş adresi** — sağlayıcı tarafında birebir
 *     eşleşmeli. Elle yazıldığında bir harf kayması yetiyor ve alınan hata
 *     sağlayıcının sayfasında çıkıyor, yani kullanıcı neyi düzelteceğini bu
 *     ekranda göremiyor.
 *  2. **Bağlantıyı sına** — kaydetmeden önce denenebiliyor. Yanlış adresle
 *     kaydedip girişi açmak, giriş ekranına çalışmayan bir düğme koymak
 *     demek.
 */

type Ayar = {
  etkin: boolean;
  gorunenAd: string | null;
  yetkili: string | null;
  istemciId: string | null;
  sirTanimli: boolean;
  kapsamlar: string | null;
  kullaniciAdiTalebi: string | null;
  otomatikKullaniciOlustur: boolean;
  donusAdresi: string | null;
};

type Sinama = { basarili: boolean; mesaj: string; yetkilendirmeAdresi: string | null };

export default function OpenIdSettings() {
  const qc = useQueryClient();
  const { bildir } = useToast();
  const [taslak, setTaslak] = useState<Ayar | null>(null);
  const [yeniSir, setYeniSir] = useState('');
  const [sinama, setSinama] = useState<Sinama | null>(null);
  const [kopyalandi, setKopyalandi] = useState(false);

  const ayar = useQuery({
    queryKey: ['openid'] as const,
    queryFn: () => api.get<Ayar>('/openid'),
  });

  useEffect(() => {
    if (ayar.data) setTaslak(ayar.data);
  }, [ayar.data]);

  const kaydet = useMutation({
    mutationFn: (govde: Ayar & { istemciSirri?: string }) => api.put<Ayar>('/openid', govde),
    onSuccess: (d) => {
      qc.setQueryData(['openid'], d);
      setTaslak(d);
      setYeniSir('');
      bildir('basari', 'Kaydedildi', 'Kimlik sağlayıcı ayarları güncellendi.');
    },
    onError: (h: Error) => bildir('hata', 'Kaydedilemedi', h.message),
  });

  const sina = useMutation({
    mutationFn: () => api.post<Sinama>('/openid/sina', {}),
    onSuccess: setSinama,
    onError: (h: Error) =>
      setSinama({ basarili: false, mesaj: h.message, yetkilendirmeAdresi: null }),
  });

  if (ayar.isLoading || !taslak) {
    return <p className="p-4 text-sm text-ink-3">Ayarlar yükleniyor…</p>;
  }

  const t = taslak;
  const yaz = (kismi: Partial<Ayar>) => setTaslak({ ...t, ...kismi });

  async function adresiKopyala() {
    if (!t.donusAdresi) return;
    await navigator.clipboard.writeText(t.donusAdresi);
    setKopyalandi(true);
    setTimeout(() => setKopyalandi(false), 2000);
  }

  return (
    <div className="space-y-4 pb-24 md:pb-4">
      <FormSection
        baslik="KURUM HESABIYLA GİRİŞ"
        aciklama="Açıkken giriş ekranında sağlayıcı düğmesi çıkar. Kullanıcı adı ve şifreyle giriş kapanmaz."
      >
        <div className="p-3.5">
          <Switch
            etiket="Kurum hesabıyla girişi aç"
            isaretli={t.etkin}
            degistir={(v) => yaz({ etkin: v })}
          />
        </div>
      </FormSection>

      <FormSection
        baslik="SAĞLAYICI"
        aciklama="Bu üç alan dolmadan giriş açılamaz."
      >
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <FieldWrapper
            etiket="Sağlayıcı adresi"
            id="oid-yetkili"
            ipucu="Ayarlar bu adresin altındaki keşif belgesinden okunur."
            className="md:col-span-2"
          >
            <Input
              id="oid-yetkili"
              value={t.yetkili ?? ''}
              onChange={(e) => yaz({ yetkili: e.target.value })}
              placeholder="https://kimlik.kurum.gov.tr/realms/kurum"
            />
          </FieldWrapper>

          <FieldWrapper etiket="İstemci kimliği" id="oid-istemci">
            <Input
              id="oid-istemci"
              value={t.istemciId ?? ''}
              onChange={(e) => yaz({ istemciId: e.target.value })}
              placeholder="kentos-kalem"
            />
          </FieldWrapper>

          {/* Sır okuma ucundan HİÇ dönmüyor; forma doldurulamaz.
              Boş bırakmak "değiştirme" demek — aksi hâlde ayarı açıp
              kaydeden herkes girişi bozardı. */}
          <FieldWrapper
            etiket="İstemci sırrı"
            id="oid-sir"
            ipucu={
              t.sirTanimli
                ? 'Tanımlı. Değiştirmeyecekseniz boş bırakın.'
                : 'Henüz tanımlanmadı.'
            }
          >
            <Input
              id="oid-sir"
              type="password"
              autoComplete="new-password"
              value={yeniSir}
              onChange={(e) => setYeniSir(e.target.value)}
              placeholder={t.sirTanimli ? '•••••• (değiştirmek için yazın)' : ''}
            />
          </FieldWrapper>
        </div>
      </FormSection>

      <FormSection baslik="EŞLEŞME">
        <div className="grid gap-3 p-3.5 md:grid-cols-2">
          <FieldWrapper
            etiket="Düğme metni"
            id="oid-ad"
            ipucu='Giriş ekranında "… ile giriş yap" diye yazılır.'
          >
            <Input
              id="oid-ad"
              value={t.gorunenAd ?? ''}
              onChange={(e) => yaz({ gorunenAd: e.target.value })}
              placeholder="Kurum Hesabı"
            />
          </FieldWrapper>

          <FieldWrapper
            etiket="Kullanıcı adı alanı"
            id="oid-talep"
            ipucu="Sağlayıcıdan gelen hangi bilgi buradaki kullanıcı adıyla eşleşecek."
          >
            <Input
              id="oid-talep"
              value={t.kullaniciAdiTalebi ?? ''}
              onChange={(e) => yaz({ kullaniciAdiTalebi: e.target.value })}
              placeholder="preferred_username"
            />
          </FieldWrapper>

          <FieldWrapper
            etiket="Kapsamlar"
            id="oid-kapsam"
            ipucu="Boşlukla ayrılır. Çoğu kurumda varsayılan yeterlidir."
            className="md:col-span-2"
          >
            <Input
              id="oid-kapsam"
              value={t.kapsamlar ?? ''}
              onChange={(e) => yaz({ kapsamlar: e.target.value })}
              placeholder="openid profile email"
            />
          </FieldWrapper>

          <div className="md:col-span-2">
            <Switch
              etiket="Tanımsız kullanıcıyı otomatik oluştur"
              aciklama="Kapalı tutmanız önerilir: açıkken sağlayıcıda hesabı olan herkes uygulamaya girebilir."
              ton={t.otomatikKullaniciOlustur ? 'uyari' : undefined}
              isaretli={t.otomatikKullaniciOlustur}
              degistir={(v) => yaz({ otomatikKullaniciOlustur: v })}
            />
          </div>
        </div>
      </FormSection>

      <FormSection
        baslik="SAĞLAYICIYA KAYDEDİLECEK ADRES"
        aciklama="Sağlayıcıdaki uygulama tanımına izin verilen dönüş adresi olarak ekleyin. Birebir aynı olmalı."
      >
        <div className="flex items-center gap-2 p-3.5">
          <code className="min-w-0 flex-1 wrap-anywhere rounded-md bg-sunken px-3 py-2.5 font-mono text-xs leading-[1.5]">
            {t.donusAdresi}
          </code>
          <Button type="button" varyant="ikincil" onClick={adresiKopyala} className="shrink-0">
            {kopyalandi ? <Check size={15} /> : <Copy size={15} />}
            {kopyalandi ? 'Kopyalandı' : 'Kopyala'}
          </Button>
        </div>
      </FormSection>

      <FormSection
        baslik="BAĞLANTI SINAMASI"
        aciklama="Kaydedilmiş ayarla sağlayıcıya ulaşılıp ulaşılmadığını dener."
      >
        <div className="p-3.5">
          <Button
            type="button"
            varyant="ikincil"
            onClick={() => sina.mutate()}
            disabled={sina.isPending}
          >
            <PlugZap size={15} />
            {sina.isPending ? 'Deneniyor…' : 'Bağlantıyı sına'}
          </Button>

          {sinama && (
            <div
              className={`mt-3 flex items-start gap-2 rounded-md px-3 py-2.5 text-sm leading-[1.5] ${
                sinama.basarili
                  ? 'bg-(--st-ok-bg) text-(--st-ok)'
                  : 'bg-(--st-no-bg) text-(--st-no)'
              }`}
            >
              {sinama.basarili ? (
                <Check size={16} className="mt-0.5 shrink-0" />
              ) : (
                <AlertTriangle size={16} className="mt-0.5 shrink-0" />
              )}
              <span className="min-w-0 wrap-anywhere">{sinama.mesaj}</span>
            </div>
          )}
        </div>
      </FormSection>

      {/* Mobilde kaydet çubuğu SABİT: form uzun ve alta inmeden kaydedememek
          kullanıcıyı her değişiklikte sayfanın dibine yolluyor. */}
      <div
        className="fixed inset-x-0 bottom-0 z-20 border-t border-line bg-surface px-4 pt-3 md:static md:border-0 md:bg-transparent md:p-0"
        style={{ paddingBottom: 'calc(var(--h-tab) + env(safe-area-inset-bottom, 0px) + 12px)' }}
      >
        <Button
          onClick={() => kaydet.mutate({ ...t, istemciSirri: yeniSir || undefined })}
          disabled={kaydet.isPending}
          className="w-full md:w-auto"
        >
          {kaydet.isPending ? 'Kaydediliyor…' : 'Kaydet'}
        </Button>
      </div>
    </div>
  );
}
