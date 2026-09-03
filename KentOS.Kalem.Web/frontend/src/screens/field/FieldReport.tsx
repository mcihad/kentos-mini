import { Camera, Loader2, Send, X } from 'lucide-react';
import { useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../../components/Button';
import { Card } from '../../components/Card';
import { FieldWrapper, Input, Secim, Textarea } from '../../components/Field';
import { Switch } from '../../components/Switch';
import { useToast } from '../../components/Toast';
import { useFieldMutations } from '../../data/citizen';
import { uploadTaskFile, useUsableTaskTypes } from '../../data/tasks';
import { TASK_PRIORITY_LABELS } from '../../data/types';
import { LocationPicker, type Konum } from '../map/LocationPicker';

/**
 * SAHA TESPİTİ — yerinde görülen sorunu kaydetmek.
 *
 * <p>
 * <b>Karşılama adımı yok.</b> Tespiti yapan zaten kurumun personeli ve
 * hangi birimin işi olduğunu biliyor; kayıt doğrudan kendi biriminin görevi
 * oluyor. Vatandaş bildiriminden asıl farkı bu.
 * </p>
 *
 * <p>
 * <b>Konum açılışta isteniyor.</b> Personel zaten olayın yerinde; haritayı
 * elle sürüklemesi gereksiz iş. Portalda ise izin kendiliğinden İSTENMİYOR —
 * kurumla ilk teması bir izin kutusuyla başlatmak doğru değil.
 * </p>
 *
 * <p>
 * <b>Fotoğraf görev açıldıktan SONRA yükleniyor.</b> Biri yüklenemezse
 * tespit kaybolmuyor; kayıt zaten alındı.
 * </p>
 */
export default function FieldReport() {
  const gezin = useNavigate();
  const { bildir } = useToast();
  const m = useFieldMutations();
  const tipler = useUsableTaskTypes();

  const [baslik, setBaslik] = useState('');
  const [aciklama, setAciklama] = useState('');
  const [tipId, setTipId] = useState<number | null>(null);
  const [oncelik, setOncelik] = useState(1);
  const [adres, setAdres] = useState('');
  const [konum, setKonum] = useState<Konum | null>(null);
  const [kendimeAta, setKendimeAta] = useState(true);
  const [fotograflar, setFotograflar] = useState<File[]>([]);
  const [bekliyor, setBekliyor] = useState(false);

  const dosyaAlani = useRef<HTMLInputElement>(null);

  const secilenTip = tipler.liste.find((t) => t.id === tipId);
  const konumZorunlu = !!secilenTip?.konumZorunlu;
  const gecerli = baslik.trim().length > 0 && (!konumZorunlu || !!konum);

  async function gonder() {
    setBekliyor(true);
    try {
      const gorev = await m.tespit.mutateAsync({
        baslik: baslik.trim(),
        aciklama: aciklama.trim() || null,
        gorevTipiId: tipId,
        oncelik: oncelik as never,
        adres: adres.trim() || null,
        enlem: konum?.enlem ?? null,
        boylam: konum?.boylam ?? null,
        kendimeAta,
      });

      let eksik = 0;
      for (const d of fotograflar) {
        try {
          await uploadTaskFile(gorev.id!, d);
        } catch {
          eksik++;
        }
      }

      if (eksik > 0) {
        bildir('uyari', `${eksik} fotoğraf yüklenemedi`, 'Tespit yine de kaydedildi.');
      }

      bildir('basari', `Tespit kaydedildi — ${gorev.takipNo}`);
      gezin(`/saha/gorev/${gorev.id}`);
    } catch (h) {
      bildir('hata', 'Tespit kaydedilemedi', (h as Error).message);
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <div className="space-y-3.5">
      <h1 className="font-display text-xl font-bold text-ink">Saha tespiti</h1>

      <Card className="p-4">
        <FieldWrapper etiket="Ne gördünüz?" id="tespit-baslik" zorunlu>
          <Input
            id="tespit-baslik"
            value={baslik}
            onChange={(e) => setBaslik(e.target.value)}
            placeholder="Kaldırım taşı sökülmüş"
            className="h-12 text-base"
            maxLength={300}
          />
        </FieldWrapper>

        <FieldWrapper etiket="Ayrıntı" id="tespit-aciklama">
          <Textarea
            id="tespit-aciklama"
            rows={3}
            value={aciklama}
            onChange={(e) => setAciklama(e.target.value)}
            className="text-base"
            maxLength={4000}
          />
        </FieldWrapper>

        <FieldWrapper etiket="Görev tipi" id="tespit-tip">
          <Secim
            id="tespit-tip"
            value={tipId ?? ''}
            onChange={(e) => setTipId(e.target.value ? Number(e.target.value) : null)}
            className="h-12 text-base"
          >
            <option value="">Tipsiz</option>
            {tipler.liste.map((t) => (
              <option key={t.id} value={t.id!}>
                {t.ad}
              </option>
            ))}
          </Secim>
        </FieldWrapper>

        <FieldWrapper etiket="Öncelik" id="tespit-oncelik">
          <Secim
            id="tespit-oncelik"
            value={oncelik}
            onChange={(e) => setOncelik(Number(e.target.value))}
            className="h-12 text-base"
          >
            {Object.entries(TASK_PRIORITY_LABELS).map(([d, e]) => (
              <option key={d} value={d}>
                {e}
              </option>
            ))}
          </Secim>
        </FieldWrapper>

        <FieldWrapper etiket="Adres tarifi" id="tespit-adres">
          <Input
            id="tespit-adres"
            value={adres}
            onChange={(e) => setAdres(e.target.value)}
            className="h-12 text-base"
            maxLength={500}
          />
        </FieldWrapper>
      </Card>

      <Card className="p-4">
        <p className="mb-2 text-sm font-medium text-ink">
          Konum
          {konumZorunlu && <span className="ml-1.5 text-2xs text-(--st-no)">zorunlu</span>}
        </p>
        <LocationPicker deger={konum} degistir={setKonum} yukseklik={240} otomatikKonum />
      </Card>

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
            setFotograflar((o) => [...o, ...(e.target.files ?? [])].slice(0, 5));
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
          {fotograflar.length === 0 ? 'Fotoğraf çek' : 'Bir tane daha'}
        </Button>
      </Card>

      <Card className="p-4">
        <Switch
          isaretli={kendimeAta}
          degistir={setKendimeAta}
          etiket="Bu işi ben yapacağım"
          aciklama="Kapatırsanız görev atanmamış olarak açılır ve birim yöneticisi dağıtır."
        />
      </Card>

      <Button
        className="h-12 w-full text-base"
        disabled={!gecerli || bekliyor}
        onClick={gonder}
      >
        {bekliyor ? <Loader2 size={17} className="animate-spin" /> : <Send size={17} />}
        Tespiti kaydet
      </Button>
    </div>
  );
}
