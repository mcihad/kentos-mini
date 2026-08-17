import { Filter } from 'lucide-react';
import { useState } from 'react';
import { FieldWrapper } from '../../components/Field';
import { Switch } from '../../components/Switch';
import { SearchSelect } from '../../components/SearchSelect';
import { Button } from '../../components/Button';
import { FormModal } from '../../components/FormModal';
import { Segment } from '../../components/FilterSheet';
import { DatePicker } from '../../components/DatePicker';
import { useNeighborhoodSearch, useOccupationSearch } from '../../data/hooks';

export type ResumeSource = 'tumu' | 'havuz' | 'talep';

export type ResumeFilterValues = {
  kaynak: ResumeSource;
  meslekId: number | null;
  meslekAdi: string | null;
  mahalleId: number | null;
  mahalleAdi: string | null;
  baslangic: string;
  bitis: string;
  banaPaylasilan: boolean;
};

/**
 * Süzgeç tabakası.
 *
 * <p>
 * Mobilde alttan açılan bir tabaka, masaüstünde ortalanmış pencere —
 * <c>FormModal</c> ikisini tek bileşende taşıyor. Süzgeçleri araç çubuğuna
 * dizmek 390px'te üçüncü satırı doğuruyor ve liste ilk ekrandan tamamen
 * çıkıyordu.
 * </p>
 *
 * <p>
 * <b>Kaynak seçimi mobilde burada</b>, masaüstünde araç çubuğundaki bölümlü
 * seçimde: telefonda şeritte yer yok, masaüstünde ise en sık kullanılan
 * süzgeci bir tabakanın arkasına gömmenin anlamı yok.
 * </p>
 */
export function ResumeFilter({
  acik,
  kapat,
  deger,
  degistir,
  temizle,
}: {
  acik: boolean;
  kapat: () => void;
  deger: ResumeFilterValues;
  degistir: (yeni: Partial<ResumeFilterValues>) => void;
  temizle: () => void;
}) {
  const [meslekArama, setMeslekArama] = useState('');
  const [mahalleArama, setMahalleArama] = useState('');
  const meslekler = useOccupationSearch(meslekArama);
  const mahalleler = useNeighborhoodSearch(mahalleArama);

  return (
    <FormModal
      acik={acik}
      kapat={kapat}
      baslik="Süzgeçler"
      aciklama="Havuzu meslek, mahalle ve tarihe göre daraltın."
      ikon={<Filter size={15} />}
      genislik="dar"
      eylemler={
        <>
          <Button varyant="ikincil" onClick={temizle}>
            Temizle
          </Button>
          <Button onClick={kapat}>Uygula</Button>
        </>
      }
    >
      {/* Kaynak, masaüstünde şeritteki bölümlü seçimde; mobilde burada. */}
      <div className="md:hidden">
        <FieldWrapper etiket="Kaynak" id="oz-kaynak">
          <Segment
            deger={deger.kaynak}
            degistir={(d) => degistir({ kaynak: d })}
            secenekler={[
              { deger: 'tumu' as ResumeSource, etiket: 'Tümü' },
              { deger: 'havuz' as ResumeSource, etiket: 'Havuz' },
              { deger: 'talep' as ResumeSource, etiket: 'Talepten' },
            ]}
          />
        </FieldWrapper>
      </div>

      <FieldWrapper etiket="Meslek" id="oz-meslek">
        <SearchSelect
          id="oz-meslek"
          deger={deger.meslekId}
          seciliAd={deger.meslekAdi}
          degistir={(id, ad) => degistir({ meslekId: id, meslekAdi: ad })}
          ogeler={meslekler.liste.map((m) => ({ id: m.id!, ad: m.ad! }))}
          ara={meslekArama}
          araDegistir={setMeslekArama}
          yukleniyor={meslekler.isFetching}
          yerTutucu="Tüm meslekler"
        />
      </FieldWrapper>

      <FieldWrapper etiket="Mahalle" id="oz-mahalle">
        <SearchSelect
          id="oz-mahalle"
          deger={deger.mahalleId}
          seciliAd={deger.mahalleAdi}
          degistir={(id, ad) => degistir({ mahalleId: id, mahalleAdi: ad })}
          ogeler={mahalleler.liste.map((m) => ({ id: m.id!, ad: m.ad! }))}
          ara={mahalleArama}
          araDegistir={setMahalleArama}
          yukleniyor={mahalleler.isFetching}
          yerTutucu="Tüm mahalleler"
        />
      </FieldWrapper>

      <div className="grid gap-3 sm:grid-cols-2">
        <FieldWrapper etiket="Başlangıç" id="oz-bas">
          <DatePicker
            id="oz-bas"
            deger={deger.baslangic}
            degistir={(d) => degistir({ baslangic: d })}
          />
        </FieldWrapper>
        <FieldWrapper etiket="Bitiş" id="oz-bit">
          <DatePicker id="oz-bit" deger={deger.bitis} degistir={(d) => degistir({ bitis: d })} />
        </FieldWrapper>
      </div>

      {/*
        Tek bir şeyi açıp kapatıyoruz: ANAHTAR. Onay kutusu listeden
        işaretlemek için — yerleşik kutunun görünümü tarayıcıya göre
        değişiyor ve dokunma hedefi 16px'te kalıyordu.
      */}
      <Switch
        isaretli={deger.banaPaylasilan}
        degistir={(d) => degistir({ banaPaylasilan: d })}
        etiket="Bana yönlendirilenler"
        aciklama="Yalnızca bir meslektaşınızın size gönderdiği özgeçmişler."
      />
    </FormModal>
  );
}
