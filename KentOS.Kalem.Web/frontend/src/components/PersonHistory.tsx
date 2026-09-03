import {
  CalendarDays,
  FolderOpen,
  ClipboardList,
  Landmark,
  Loader2,
  UserCheck,
  UserSearch,
} from 'lucide-react';
import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { usePersonHistory } from '../data/hooks';
import { shortDate } from '../data/format';
import { cn } from './utils';

/**
 * "BU VATANDAŞI DAHA ÖNCE GÖRDÜK MÜ?"
 *
 * Halk gününe kişi eklerken önce telefon sorulur; numara girilir girilmez bu
 * kart belirir ve "3 talep · 2 etkinlik · 1 halk günü" der. Kullanıcı
 * ayrıntıya bakabilir ya da doğrudan kaydetmeye devam edebilir — kart hiçbir
 * şeyi engellemez, yalnızca bilgilendirir.
 *
 * Aynı bileşen üç yerde: havuz formunda, katılım satırında ve salon modunda.
 * Üç kopya, birinin eksik kalması demekti.
 *
 * Arama SUNUCUDA normalleştirilmiş telefonla yapılıyor: kayıtlardaki
 * numaralar `0541 298 34 50` / `05412983450` / `+90 541…` karışık ve ham
 * karşılaştırma numarayı bitişik yazınca bulmuyordu.
 */
export function PersonHistory({
  telefon,
  ad,
  haricKatilim,
  className,
}: {
  telefon?: string;
  ad?: string;
  /**
   * EKRANDA AÇIK olan görüşme geçmişe SAYILMAZ.
   *
   * Sayaç kendi kaydını da topluyordu: ilk kez gelen vatandaşta bile
   * "1 halk günü", görüşme kaydedilir kaydedilmez "1 kez görüşülmüş"
   * yazıyordu — oysa buradaki soru "DAHA ÖNCE geldi mi?".
   */
  haricKatilim?: number;
  className?: string;
}) {
  const konum = useLocation();
  const [acik, setAcik] = useState(false);
  const { data, isFetching } = usePersonHistory(telefon, ad, haricKatilim);

  // Yeterli girdi yokken hiçbir şey çizilmez: boş bir kutu, kullanıcıya
  // "kayıt yok" diye yanlış bir şey söylerdi.
  if (!data && !isFetching) return null;

  if (isFetching && !data) {
    return (
      <div
        className={cn(
          'flex items-center gap-2 rounded-control border border-border bg-surface-2 px-3 py-2 text-sm text-text-3',
          className,
        )}
      >
        <Loader2 size={13} className="animate-spin" />
        Geçmiş aranıyor…
      </div>
    );
  }

  if (!data) return null;

  if (!data.kayitVar) {
    return (
      <div
        className={cn(
          'flex items-center gap-2 rounded-control border border-border bg-surface-2 px-3 py-2 text-sm text-text-3',
          className,
        )}
      >
        <UserSearch size={13} />
        Bu numarayla ilk kayıt.
      </div>
    );
  }

  const rozetler = [
    { ikon: ClipboardList, sayi: data.talepSayisi ?? 0, etiket: 'talep' },
    { ikon: CalendarDays, sayi: data.etkinlikSayisi ?? 0, etiket: 'etkinlik' },
    { ikon: UserCheck, sayi: data.halkGunuSayisi ?? 0, etiket: 'halk günü' },
  ].filter((r) => r.sayi > 0);

  return (
    <div
      className={cn(
        'rounded-control border border-(--gold) bg-gold-tint',
        className,
      )}
    >
      <button
        type="button"
        onClick={() => setAcik((a) => !a)}
        className="flex w-full items-center gap-2 px-3 py-2 text-left"
      >
        <UserSearch size={14} className="shrink-0 text-(--gold-2)" />

        <span className="flex min-w-0 flex-1 flex-wrap items-center gap-x-2.5 gap-y-1">
          {rozetler.map((r) => (
            <span key={r.etiket} className="flex items-center gap-1 text-sm">
              <r.ikon size={12} className="text-text-3" />
              <b className="tabular-nums">{r.sayi}</b> {r.etiket}
            </span>
          ))}

          {(data.gorusulenSayisi ?? 0) > 0 && (
            <span className="text-xs text-text-2">
              · {data.gorusulenSayisi} kez görüşülmüş
            </span>
          )}

          {data.protokolAd && (
            <span className="flex items-center gap-1 text-xs text-text-2">
              <Landmark size={12} className="text-text-3" />
              protokolde: {data.protokolAd}
            </span>
          )}
        </span>

        <span className="shrink-0 text-xs font-medium text-(--gold-2)">
          {acik ? 'Gizle' : 'Ayrıntı'}
        </span>
      </button>

      {/*
       * DOSYA — özet soruyu açıyor, cevabı burada.
       *
       * Kartın kendi "Ayrıntı" listesi son 15 satırı tek satırlık özetlerle
       * gösteriyor; vatandaşın karşısında "ne istemişti, ne oldu, hangi birim
       * baktı" sorularının cevabı için yetmiyordu. Bağlantı geldiği sayfayı
       * `donus` ile taşıyor: dosyadan geri dönen kişi görüşmesini kaybetmesin.
       */}
      <div className="border-t border-(--gold) px-3 py-1.5">
        <Link
          to={`/halk-gunu/kisi?${new URLSearchParams({
            ...(telefon ? { telefon } : {}),
            ...(ad ? { ad } : {}),
            ...(haricKatilim ? { haric: String(haricKatilim) } : {}),
            donus: konum.pathname + konum.search,
          }).toString()}`}
          className="flex items-center justify-center gap-1.5 text-sm font-semibold text-(--gold-2) hover:underline"
        >
          <FolderOpen size={13} />
          Vatandaş dosyasını aç
        </Link>
      </div>

      {acik && (
        <ul className="border-t border-(--gold) px-3 py-2">
          {(data.son ?? []).map((s) => (
            <li
              key={`${s.tur}-${s.id}`}
              className="flex items-baseline gap-2 border-b border-border/60 py-1.5 last:border-0"
            >
              <span className="w-[68px] shrink-0 text-2xs uppercase tracking-[0.04em] text-text-3">
                {ETIKET[s.tur ?? ''] ?? s.tur}
              </span>
              <span className="w-[74px] shrink-0 text-xs tabular-nums text-text-3">
                {shortDate(s.tarih)}
              </span>
              <span className="min-w-0 flex-1 text-sm">
                <span className="line-clamp-1">{s.baslik}</span>
                {s.not && (
                  <span className="line-clamp-1 text-xs text-text-3">{s.not}</span>
                )}
              </span>
              {s.durumAd && (
                <span className="shrink-0 text-xs text-text-2">{s.durumAd}</span>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

const ETIKET: Record<string, string> = {
  talep: 'Talep',
  etkinlik: 'Etkinlik',
  halkgunu: 'Halk günü',
};
