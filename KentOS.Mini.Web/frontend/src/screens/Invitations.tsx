import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  CalendarCheck, MailCheck, MapPin, Plus, Search, Users,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Switch } from '../components/Switch';
import { PERMISSION } from '../components/permissions';
import { useSession } from '../auth/SessionProvider';
import { FieldWrapper, SearchInput, Textarea, Input } from '../components/Field';
import { EmptyState } from '../components/EmptyState';
import { Button } from '../components/Button';
import { FormModal } from '../components/FormModal';
import { SkeletonRows } from '../components/Skeleton';
import { Pagination } from '../components/Pagination';
import { DatePicker } from '../components/DatePicker';
import { useToast } from '../components/Toast';
import { date } from '../data/format';
import { api, queryString, type PagedResult } from '../data/client';

export type InvitationSummaryCard = {
  id: number;
  baslik: string;
  tarih?: string | null;
  yer?: string | null;
  kisiSayisi: number;
  katilacak: number;
  katilmayacak: number;
  beklemede: number;
  arandi: number;
};

/**
 * Davet listeleri.
 *
 * <p>
 * Protokol listesi <b>kimin nerede durduğunu</b>, davet listesi <b>kimin
 * çağrıldığını ve ne cevap verdiğini</b> tutar. Aynı protokol kaydı onlarca
 * davete girer ve her davette farklı cevap verir; bu yüzden ayrı.
 * </p>
 */
export default function Invitations() {
  // Yönetim düğmeleri İZNE göre gizlenir.
  const { hasPermission } = useSession();

  const qc = useQueryClient();
  const gezin = useNavigate();
  const { bildir } = useToast();

  const [aramaGirdisi, setAramaGirdisi] = useState('');
  const [arama, setArama] = useState('');
  const [gecmisDahil, setGecmisDahil] = useState(false);
  const [sayfa, setSayfa] = useState(1);
  const [formAcik, setFormAcik] = useState(false);

  useEffect(() => {
    const z = setTimeout(() => {
      setArama(aramaGirdisi);
      setSayfa(1);
    }, 300);
    return () => clearTimeout(z);
  }, [aramaGirdisi]);

  const liste = useQuery({
    queryKey: ['davet', 'liste', sayfa, arama, gecmisDahil] as const,
    queryFn: () =>
      api.get<PagedResult<InvitationSummaryCard>>(
        `/davet${queryString({ sayfa, boyut: 25, ara: arama, gecmisDahil })}`,
      ),
    placeholderData: keepPreviousData,
  });

  const olustur = useMutation({
    mutationFn: (d: { baslik: string; tarih: string | null; yer: string | null; aciklama: string | null }) =>
      api.post<InvitationSummaryCard>('/davet', d),
    onSuccess: (olusan) => {
      qc.invalidateQueries({ queryKey: ['davet'] });
      setFormAcik(false);
      bildir('basari', 'Davet oluşturuldu', 'Şimdi protokolden kişi ekleyin.');

      // Oluşturduktan sonra DOĞRUDAN detaya: boş bir davet listede tek başına
      // işe yaramıyor, bir sonraki adım her zaman kişi eklemek.
      if (olusan?.id) gezin(`/davetler/${olusan.id}`);
    },
    onError: (h: Error) => bildir('hata', 'Oluşturulamadı', h.message),
  });

  const kayitlar = liste.data?.veriler ?? [];

  return (
    <div className="space-y-3.5">
      {/*
        Form MODAL: liste arkada duruyor ve oluşturulunca doğrudan detaya
        gidiliyor. Önceden tam sayfaya geçiyordu, kaydedince listeye
        dönülüyordu ve kullanıcı yeni daveti listede aramak zorunda kalıyordu.
      */}
      <DavetFormu
        acik={formAcik}
        kaydet={(d) => olustur.mutate(d)}
        beklemede={olustur.isPending}
        vazgec={() => setFormAcik(false)}
      />

      <div className="flex flex-col gap-2.5 md:flex-row md:items-center">
        <SearchInput
          value={aramaGirdisi}
          onChange={(e) => setAramaGirdisi(e.target.value)}
          placeholder="Davet başlığı veya yer ara"
          aria-label="Davetlerde ara"
          ikon={<Search size={15} />}
          className="md:max-w-[320px] md:flex-1"
        />

        <div className="md:ml-auto">
          <Switch
            isaretli={gecmisDahil}
            degistir={(a) => {
              setGecmisDahil(a);
              setSayfa(1);
            }}
            etiket="Geçmiş davetler"
          />
        </div>

{hasPermission(PERMISSION.davetYonet) && (
                <Button className="w-full md:w-auto" onClick={() => setFormAcik(true)}>
          <Plus size={14} />
          Yeni davet
        </Button>
        )}
      </div>

      {liste.isLoading ? (
        <SkeletonRows adet={5} />
      ) : liste.isError ? (
        <EmptyState
          ikon={Users}
          baslik="Davetler yüklenemedi"
          aciklama={(liste.error as Error)?.message}
        />
      ) : kayitlar.length === 0 ? (
        <EmptyState
          ikon={Users}
          baslik={arama ? 'Eşleşen davet yok' : 'Davet yok'}
          aciklama="Bir tören ya da etkinlik için protokolden kişi seçerek davet listesi oluşturun."
          eylem={
            // Boş durumdaki EKLEME düğmesi de izin ister; araç çubuğundaki
            // düğme kapıdan geçiyordu ama liste boşken çizilen bu ikinci
            // düğme kapının dışında kalmıştı.
            hasPermission(PERMISSION.davetYonet) ? (
              <Button onClick={() => setFormAcik(true)}>
                <Plus size={14} />
                Yeni davet
              </Button>
            ) : undefined
          }
        />
      ) : (
        <>
          <ul className="space-y-2">
            {kayitlar.map((d) => (
              <li key={d.id}>
                <Link
                  to={`/davetler/${d.id}`}
                  className="block rounded-card border border-border bg-surface p-3.5 transition-colors hover:bg-surface-2"
                >
                  <div className="flex items-start gap-3">
                    <span
                      className="mt-0.5 grid h-9 w-9 shrink-0 place-items-center rounded-md bg-brand-tint text-brand-2"
                      aria-hidden
                    >
                      <CalendarCheck size={16} />
                    </span>

                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-semibold">{d.baslik}</p>
                      <p className="flex flex-wrap items-center gap-x-3 text-xs text-text-3">
                        {d.tarih && <span>{date(d.tarih)}</span>}
                        {d.yer && (
                          <span className="inline-flex items-center gap-1">
                            <MapPin size={11} />
                            {d.yer}
                          </span>
                        )}
                        <span>{d.kisiSayisi} kişi</span>
                      </p>

                      {/* Takip özeti: listede en çok merak edilen bu. */}
                      <div className="mt-1.5 flex flex-wrap gap-1.5">
                        <Rozet renk="ok" sayi={d.katilacak} etiket="katılacak" />
                        <Rozet renk="no" sayi={d.katilmayacak} etiket="katılmayacak" />
                        <Rozet renk="wait" sayi={d.beklemede} etiket="beklemede" />
                        <Rozet renk="live" sayi={d.arandi} etiket="arandı" />
                      </div>
                    </div>
                  </div>
                </Link>
              </li>
            ))}
          </ul>

          <Pagination sonuc={liste.data} sayfaDegistir={setSayfa} birim="davet" />
        </>
      )}
    </div>
  );
}

function Rozet({
  renk,
  sayi,
  etiket,
}: {
  renk: 'ok' | 'no' | 'wait' | 'live';
  sayi: number;
  etiket: string;
}) {
  if (sayi === 0) return null;
  return (
    <span
      className="rounded-full px-2 py-0.5 text-2xs font-medium"
      style={{
        background: `var(--st-${renk}-bg)`,
        color: `var(--st-${renk})`,
      }}
    >
      {sayi} {etiket}
    </span>
  );
}

function DavetFormu({
  acik,
  kaydet,
  vazgec,
  beklemede,
}: {
  acik: boolean;
  kaydet: (d: { baslik: string; tarih: string | null; yer: string | null; aciklama: string | null }) => void;
  vazgec: () => void;
  beklemede: boolean;
}) {
  const [baslik, setBaslik] = useState('');
  const [gun, setGun] = useState('');
  const [yer, setYer] = useState('');
  const [aciklama, setAciklama] = useState('');

  const gonder = () => {
    if (!baslik.trim()) return;
    kaydet({
      baslik: baslik.trim(),
      // Saat taşımayan alan; sunucu damgası kayan yerel saat.
      tarih: gun ? `${gun}T00:00:00` : null,
      yer: yer || null,
      aciklama: aciklama || null,
    });
  };

  return (
    <FormModal
      acik={acik}
      kapat={vazgec}
      baslik="Yeni davet"
      aciklama="Oluşturduktan sonra protokolden kişi ekleyeceksiniz."
      ikon={<MailCheck size={15} />}
      eylemler={
        <>
          <Button type="button" varyant="ikincil" onClick={vazgec}>
            Vazgeç
          </Button>
          <Button onClick={gonder} disabled={!baslik.trim() || beklemede}>
            {beklemede ? 'Oluşturuluyor…' : 'Oluştur'}
          </Button>
        </>
      }
    >
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          gonder();
        }}
      >
        <div className="space-y-4">
          <FieldWrapper etiket="Başlık" id="d-baslik" zorunlu>
            <Input
              id="d-baslik"
              value={baslik}
              onChange={(e) => setBaslik(e.target.value)}
              placeholder="Örn. Cumhuriyet Bayramı Resepsiyonu"
              maxLength={200}
              autoFocus
            />
          </FieldWrapper>

          <div className="grid gap-4 sm:grid-cols-2">
            <FieldWrapper etiket="Tarih" id="d-tarih">
              <DatePicker id="d-tarih" deger={gun} degistir={setGun} temizlenebilir />
            </FieldWrapper>

            <FieldWrapper etiket="Yer" id="d-yer">
              <Input id="d-yer" value={yer} onChange={(e) => setYer(e.target.value)} />
            </FieldWrapper>
          </div>

          <FieldWrapper etiket="Açıklama" id="d-aciklama">
            <Textarea
              id="d-aciklama"
              value={aciklama}
              onChange={(e) => setAciklama(e.target.value)}
            />
          </FieldWrapper>
        </div>
      </form>
    </FormModal>
  );
}
