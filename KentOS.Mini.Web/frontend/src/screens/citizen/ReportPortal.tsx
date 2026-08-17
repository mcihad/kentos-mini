import {
  Camera, Check, CheckCircle2, Loader2, MapPin, MessageSquare, Phone, X,
} from 'lucide-react';
import { useRef, useState } from 'react';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { FieldWrapper, Input, Textarea } from '../../components/Field';
import { useToast } from '../../components/Toast';
import { portal } from '../../data/citizen';
import { LocationPicker } from '../map/LocationPicker';

type Adim = 'telefon' | 'kod' | 'form' | 'bitti';

/**
 * VATANDAŞ PORTALI — kurum dışına açık tek ekran.
 *
 * <p>
 * <b>Doğrulama formdan ÖNCE.</b> Sonda olsaydı vatandaş bütün formu
 * doldurup kodu bekler, kod gelmezse yazdığı her şeyi kaybederdi.
 * </p>
 *
 * <p>
 * <b>Tek sütun, büyük dokunma hedefleri, az alan.</b> Portalı kullanan kişi
 * çoğunlukla sokakta, telefonda ve acelede. Zorunlu alan beş tane: ad,
 * telefon, konu, açıklama ve (varsa) konum.
 * </p>
 *
 * <p>
 * <b>Sonuçta yalnızca takip numarası var.</b> Sunucu da başka bir şey
 * döndürmüyor — birim, personel ve görev kimliği kurumun iç bilgisi.
 * </p>
 */
export default function ReportPortal() {
  const { bildir } = useToast();

  const [adim, setAdim] = useState<Adim>('telefon');
  const [telefon, setTelefon] = useState('');
  const [kod, setKod] = useState('');
  const [bilet, setBilet] = useState('');
  const [bekliyor, setBekliyor] = useState(false);

  const [adSoyad, setAdSoyad] = useState('');
  const [konu, setKonu] = useState('');
  const [aciklama, setAciklama] = useState('');
  const [adres, setAdres] = useState('');
  const [konum, setKonum] = useState<{ enlem: number; boylam: number } | null>(null);
  const [fotograflar, setFotograflar] = useState<File[]>([]);

  const [takipNo, setTakipNo] = useState('');
  const dosyaAlani = useRef<HTMLInputElement>(null);

  // Yalnızca rakam: "0532 123 45 67" da "+90..." da aynı numara. Sunucu da
  // aynı sadeleştirmeyi yapıyor, buradaki yalnızca sayaç için.
  const rakamlar = telefon.replace(/\D/g, '');
  const telefonGecerli = rakamlar.length >= 10;

  async function kodIste() {
    setBekliyor(true);
    try {
      await portal.kodIste(telefon);
      setAdim('kod');
      bildir('basari', 'Doğrulama kodu gönderildi');
    } catch (h) {
      bildir('hata', 'Kod gönderilemedi', (h as Error).message);
    } finally {
      setBekliyor(false);
    }
  }

  async function dogrula() {
    setBekliyor(true);
    try {
      const s = await portal.dogrula(telefon, kod);
      setBilet(s.bilet ?? '');
      setAdim('form');
    } catch (h) {
      bildir('hata', 'Doğrulanamadı', (h as Error).message);
    } finally {
      setBekliyor(false);
    }
  }

  async function gonder() {
    setBekliyor(true);
    try {
      const sonuc = await portal.bildir({
        adSoyad: adSoyad.trim(),
        telefon,
        bilet,
        konu: konu.trim(),
        aciklama: aciklama.trim(),
        adres: adres.trim() || null,
        enlem: konum?.enlem ?? null,
        boylam: konum?.boylam ?? null,
      });

      /*
        FOTOĞRAFLAR KAYITTAN SONRA ve TEK TEK.

        Biri yüklenemezse bildirimin kendisi kaybolmuyor — kayıt zaten
        alındı. Hepsini tek isteğe koysaydık kopan bir bağlantı bütün
        bildirimi geri aldırırdı.
      */
      let eksik = 0;
      for (const d of fotograflar) {
        try {
          await portal.fotograf(sonuc.yuklemeAnahtari ?? '', d);
        } catch {
          eksik++;
        }
      }

      if (eksik > 0) {
        bildir('uyari', `${eksik} fotoğraf yüklenemedi`,
          'Bildiriminiz yine de kaydedildi.');
      }

      setTakipNo(sonuc.takipNo ?? '');
      setAdim('bitti');
    } catch (h) {
      bildir('hata', 'Bildirim gönderilemedi', (h as Error).message);
    } finally {
      setBekliyor(false);
    }
  }

  // ── bitti ──────────────────────────────────────────────────────────

  if (adim === 'bitti') {
    return (
      <Card className="p-5 text-center">
        <CheckCircle2 size={44} className="mx-auto text-(--st-ok)" />
        <h1 className="mt-3 font-display text-lg font-bold text-ink">Bildiriminiz alındı</h1>
        <p className="mt-1 text-sm text-text-2">
          Takip numaranız SMS ile de gönderildi.
        </p>

        <p className="mt-4 rounded-control border border-line bg-sunken px-4 py-3 font-mono text-lg font-semibold tabular-nums text-ink">
          {takipNo}
        </p>

        <p className="mt-4 text-xs text-text-3">
          Bildiriminiz ilgili birime yönlendirildiğinde bilgilendirileceksiniz.
        </p>

        <Button
          varyant="ikincil"
          className="mt-5 w-full"
          onClick={() => {
            // Yeni bildirim için baştan: bilet duruyor ama form temizleniyor.
            setKonu('');
            setAciklama('');
            setAdres('');
            setKonum(null);
            setFotograflar([]);
            setAdim('form');
          }}
        >
          Yeni bildirim gönder
        </Button>
      </Card>
    );
  }

  return (
    <div className="space-y-3.5">
      <div>
        <h1 className="font-display text-xl font-bold text-ink">Bildirim gönder</h1>
        <p className="mt-1 text-sm text-text-2">
          Gördüğünüz sorunu bize iletin; ilgili birime yönlendirelim.
        </p>
      </div>

      {/* ── Adım göstergesi ── */}
      <ol className="flex items-center gap-1.5" aria-label="Adımlar">
        {(['telefon', 'kod', 'form'] as const).map((a, i) => (
          <li key={a} className="flex flex-1 items-center gap-1.5">
            <span
              className={`grid h-6 w-6 flex-none place-items-center rounded-full text-2xs font-medium ${
                adim === a
                  ? 'bg-brand text-on-brand'
                  : ['telefon', 'kod', 'form'].indexOf(adim) > i
                    ? 'bg-(--st-ok-bg) text-(--st-ok)'
                    : 'bg-sunken text-ink-3'
              }`}
            >
              {['telefon', 'kod', 'form'].indexOf(adim) > i ? <Check size={13} /> : i + 1}
            </span>
            <span className="h-px flex-1 bg-line" aria-hidden />
          </li>
        ))}
      </ol>

      {/* ── 1. Telefon ── */}
      {adim === 'telefon' && (
        <Card className="p-4">
          <FieldWrapper
            etiket="Telefon numaranız"
            id="portal-telefon"
            zorunlu
            ipucu="Doğrulama kodu bu numaraya gönderilecek."
          >
            <Input
              id="portal-telefon"
              type="tel"
              inputMode="tel"
              autoComplete="tel"
              value={telefon}
              onChange={(e) => setTelefon(e.target.value)}
              placeholder="0532 123 45 67"
              className="h-12 text-base"
            />
          </FieldWrapper>

          <Button
            className="mt-3 h-12 w-full text-base"
            disabled={!telefonGecerli || bekliyor}
            onClick={kodIste}
          >
            {bekliyor ? <Loader2 size={17} className="animate-spin" /> : <Phone size={17} />}
            Doğrulama kodu gönder
          </Button>
        </Card>
      )}

      {/* ── 2. Kod ── */}
      {adim === 'kod' && (
        <Card className="p-4">
          <FieldWrapper
            etiket="Doğrulama kodu"
            id="portal-kod"
            zorunlu
            ipucu={`${telefon} numarasına gönderildi.`}
          >
            <Input
              id="portal-kod"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              value={kod}
              onChange={(e) => setKod(e.target.value.replace(/\D/g, ''))}
              placeholder="000000"
              className="h-12 text-center font-mono text-xl tracking-[0.4em]"
            />
          </FieldWrapper>

          <Button
            className="mt-3 h-12 w-full text-base"
            disabled={kod.length !== 6 || bekliyor}
            onClick={dogrula}
          >
            {bekliyor ? <Loader2 size={17} className="animate-spin" /> : <Check size={17} />}
            Doğrula
          </Button>

          <button
            type="button"
            className="mt-3 w-full text-center text-xs text-ink-3 underline"
            onClick={() => {
              setKod('');
              setAdim('telefon');
            }}
          >
            Numarayı değiştir
          </button>
        </Card>
      )}

      {/* ── 3. Form ── */}
      {adim === 'form' && (
        <>
          <Card className="p-4">
            <FieldWrapper etiket="Ad soyad" id="portal-ad" zorunlu>
              <Input
                id="portal-ad"
                autoComplete="name"
                value={adSoyad}
                onChange={(e) => setAdSoyad(e.target.value)}
                className="h-12 text-base"
                maxLength={150}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Konu" id="portal-konu" zorunlu>
              <Input
                id="portal-konu"
                value={konu}
                onChange={(e) => setKonu(e.target.value)}
                placeholder="Sokak lambası yanmıyor"
                className="h-12 text-base"
                maxLength={300}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Açıklama" id="portal-aciklama" zorunlu>
              <Textarea
                id="portal-aciklama"
                rows={4}
                value={aciklama}
                onChange={(e) => setAciklama(e.target.value)}
                placeholder="Sorunu ve yerini olabildiğince açık anlatın."
                className="text-base"
                maxLength={4000}
              />
            </FieldWrapper>

            <FieldWrapper etiket="Adres tarifi" id="portal-adres">
              <Input
                id="portal-adres"
                value={adres}
                onChange={(e) => setAdres(e.target.value)}
                placeholder="Mahalle, sokak, bina"
                className="h-12 text-base"
                maxLength={500}
              />
            </FieldWrapper>
          </Card>

          {/* ── Konum ── */}
          <Card className="p-4">
            <p className="mb-2 flex items-center gap-1.5 text-sm font-medium text-ink">
              <MapPin size={16} className="text-text-3" />
              Konum
            </p>
            <p className="mb-2.5 text-xs text-text-2">
              Sorunun yerini haritada işaretleyin. Konum, ekibin doğru yeri
              bulmasını sağlıyor.
            </p>
            <LocationPicker deger={konum} degistir={setKonum} yukseklik={220} />
          </Card>

          {/* ── Fotoğraf ── */}
          <Card className="p-4">
            <p className="mb-2 flex items-center gap-1.5 text-sm font-medium text-ink">
              <Camera size={16} className="text-text-3" />
              Fotoğraf
            </p>

            <input
              ref={dosyaAlani}
              type="file"
              accept="image/*"
              capture="environment"
              multiple
              className="hidden"
              onChange={(e) => {
                const yeni = [...(e.target.files ?? [])];
                // En fazla beş: sunucu da aynı sınırı uyguluyor ve
                // reddedileceğini bildiğimiz dosyayı yollamak gereksiz.
                setFotograflar((o) => [...o, ...yeni].slice(0, 5));
                if (dosyaAlani.current) dosyaAlani.current.value = '';
              }}
            />

            {fotograflar.length > 0 && (
              <ul className="mb-2.5 space-y-1.5">
                {fotograflar.map((d, i) => (
                  <li
                    key={`${d.name}-${i}`}
                    className="flex items-center gap-2 rounded-control border border-line px-2.5 py-2"
                  >
                    <Camera size={14} className="flex-none text-ink-3" />
                    <span className="min-w-0 flex-1 truncate text-xs text-ink">{d.name}</span>
                    <button
                      type="button"
                      aria-label={`${d.name} kaldır`}
                      onClick={() => setFotograflar((o) => o.filter((_, x) => x !== i))}
                      className="grid h-9 w-9 flex-none place-items-center text-ink-3"
                    >
                      <X size={16} />
                    </button>
                  </li>
                ))}
              </ul>
            )}

            <Button
              varyant="ikincil"
              className="h-12 w-full text-base"
              disabled={fotograflar.length >= 5}
              onClick={() => dosyaAlani.current?.click()}
            >
              <Camera size={17} />
              {fotograflar.length === 0 ? 'Fotoğraf ekle' : 'Bir tane daha'}
            </Button>
          </Card>

          <Button
            className="h-12 w-full text-base"
            disabled={
              bekliyor ||
              adSoyad.trim().length === 0 ||
              konu.trim().length === 0 ||
              aciklama.trim().length === 0
            }
            onClick={gonder}
          >
            {bekliyor ? (
              <Loader2 size={17} className="animate-spin" />
            ) : (
              <MessageSquare size={17} />
            )}
            Gönder
          </Button>

          <p className="pb-2 text-center text-2xs text-text-3">
            Gönderdiğiniz bilgiler yalnızca bildiriminizin çözümü için kullanılır.
          </p>
        </>
      )}
    </div>
  );
}
