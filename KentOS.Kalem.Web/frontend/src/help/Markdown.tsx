import { Fragment, type ReactNode } from 'react';

/**
 * YARDIM METİNLERİ İÇİN KÜÇÜK MARKDOWN ÇİZİCİ.
 *
 * Hazır bir markdown kütüphanesi EKLENMEDİ: `frontend/CLAUDE.md` kendi başına
 * kütüphane eklemeyi yasaklıyor ve burada işlenen metinlerin tamamı bizim
 * yazdığımız, denetimli dosyalar — rastgele kullanıcı girdisi değil. Bu yüzden
 * desteklenen alt küme dar ve okunur tutuldu:
 *
 * `#`–`###` başlık · paragraf · `-` ve `1.` listeler · `>` alıntı · `---` ayraç
 * · `|` tablo · satır içi `**kalın**`, `*eğik*`, `` `kod` ``.
 *
 * Desteklenmeyen bir söz dizimi düz metin olarak çizilir; yardım sayfası hiçbir
 * durumda boş kalmaz.
 */
export function Markdown({ metin }: { metin: string }) {
  return <div className="yardim-metin">{blokCiz(metin)}</div>;
}

function blokCiz(metin: string): ReactNode[] {
  const satirlar = metin.replace(/\r\n/g, '\n').split('\n');
  const cikti: ReactNode[] = [];
  let i = 0;

  while (i < satirlar.length) {
    const s = satirlar[i];

    // boş satır
    if (!s.trim()) {
      i++;
      continue;
    }

    // ayraç
    if (/^---+$/.test(s.trim())) {
      cikti.push(<hr key={i} className="my-5 border-border" />);
      i++;
      continue;
    }

    // başlık
    const baslik = /^(#{1,3})\s+(.*)$/.exec(s);
    if (baslik) {
      const seviye = baslik[1].length;
      const icerik = satirIci(baslik[2]);
      cikti.push(
        seviye === 1 ? (
          <h2
            key={i}
            className="mt-6 font-display text-xl font-bold tracking-[-0.01em] first:mt-0"
          >
            {icerik}
          </h2>
        ) : seviye === 2 ? (
          <h3
            key={i}
            className="mt-6 flex items-center gap-2 font-display text-lg font-semibold first:mt-0"
          >
            <span className="h-[14px] w-[3px] shrink-0 rounded-full bg-gold" aria-hidden />
            {icerik}
          </h3>
        ) : (
          <h4 key={i} className="mt-4 text-sm font-semibold text-text">
            {icerik}
          </h4>
        ),
      );
      i++;
      continue;
    }

    // alıntı — "şunu unutma" kutusu
    if (s.startsWith('> ')) {
      const parcalar: string[] = [];
      while (i < satirlar.length && satirlar[i].startsWith('> ')) {
        parcalar.push(satirlar[i].slice(2));
        i++;
      }
      cikti.push(
        <blockquote
          key={`a${i}`}
          className="my-3 rounded-md border border-(--gold) bg-(--gold-tint) px-3.5 py-2.5 text-sm leading-[1.6] text-text-2"
        >
          {satirIci(parcalar.join(' '))}
        </blockquote>,
      );
      continue;
    }

    // tablo
    if (s.trim().startsWith('|')) {
      const satirBlogu: string[] = [];
      while (i < satirlar.length && satirlar[i].trim().startsWith('|')) {
        satirBlogu.push(satirlar[i]);
        i++;
      }
      cikti.push(<Tablo key={`t${i}`} satirlar={satirBlogu} />);
      continue;
    }

    // numaralı liste
    if (/^\d+\.\s/.test(s)) {
      const ogeler: string[] = [];
      while (i < satirlar.length && /^\d+\.\s/.test(satirlar[i])) {
        ogeler.push(satirlar[i].replace(/^\d+\.\s/, ''));
        i++;
        // DEVAM SATIRLARI aynı maddeye aittir: kaynakta satır sonuna gelen bir
        // madde ikinci satıra taşınca ayrı bir paragraf olarak çiziliyor ve
        // cümle ortadan bölünüyordu.
        i = devamiEkle(satirlar, i, ogeler);
      }
      cikti.push(
        <ol key={`o${i}`} className="my-3 space-y-2">
          {ogeler.map((o, n) => (
            <li key={n} className="flex gap-2.5 text-sm leading-[1.65] text-text-2">
              <span className="mt-px grid h-[19px] w-[19px] shrink-0 place-items-center rounded-full bg-brand-tint text-2xs font-bold text-brand-2">
                {n + 1}
              </span>
              <span className="min-w-0">{satirIci(o)}</span>
            </li>
          ))}
        </ol>,
      );
      continue;
    }

    // madde listesi
    if (/^[-*]\s/.test(s)) {
      const ogeler: string[] = [];
      while (i < satirlar.length && /^[-*]\s/.test(satirlar[i])) {
        ogeler.push(satirlar[i].replace(/^[-*]\s/, ''));
        i++;
        i = devamiEkle(satirlar, i, ogeler);
      }
      cikti.push(
        <ul key={`u${i}`} className="my-3 space-y-1.5">
          {ogeler.map((o, n) => (
            <li key={n} className="flex gap-2.5 text-sm leading-[1.65] text-text-2">
              <span className="mt-[8px] h-[4px] w-[4px] shrink-0 rounded-full bg-gold" aria-hidden />
              <span className="min-w-0">{satirIci(o)}</span>
            </li>
          ))}
        </ul>,
      );
      continue;
    }

    // paragraf — ardışık düz satırlar tek paragraf
    const parca: string[] = [];
    while (
      i < satirlar.length &&
      satirlar[i].trim() &&
      !/^(#{1,3}\s|>\s|[-*]\s|\d+\.\s|\|)/.test(satirlar[i]) &&
      !/^---+$/.test(satirlar[i].trim())
    ) {
      parca.push(satirlar[i]);
      i++;
    }
    cikti.push(
      <p key={`p${i}`} className="my-2.5 text-sm leading-[1.7] text-text-2 metin-guzel">
        {satirIci(parca.join(' '))}
      </p>,
    );
  }

  return cikti;
}

/**
 * Maddenin DEVAM satırlarını son maddeye ekler.
 *
 * Devam satırı: boş olmayan ve yeni bir blok başlatmayan satır. Metin
 * dosyalarında satırlar 80 sütuna sarıldığı için hemen her uzun madde
 * ikinci satıra taşıyor.
 */
function devamiEkle(satirlar: string[], i: number, ogeler: string[]): number {
  while (
    i < satirlar.length &&
    satirlar[i].trim() &&
    !/^(#{1,3}\s|>\s|[-*]\s|\d+\.\s|\|)/.test(satirlar[i]) &&
    !/^---+$/.test(satirlar[i].trim())
  ) {
    ogeler[ogeler.length - 1] += ` ${satirlar[i].trim()}`;
    i++;
  }
  return i;
}

function Tablo({ satirlar }: { satirlar: string[] }) {
  const hucreler = (satir: string) =>
    satir
      .trim()
      .replace(/^\|/, '')
      .replace(/\|$/, '')
      .split('|')
      .map((h) => h.trim());

  const baslik = hucreler(satirlar[0]);
  // İkinci satır hizalama satırıysa (---) atlanır.
  const govde = satirlar
    .slice(/^[\s|:-]+$/.test(satirlar[1] ?? '') ? 2 : 1)
    .map(hucreler);

  return (
    <div className="my-3 overflow-x-auto rounded-md border border-border">
      <table className="w-full border-collapse text-sm">
        <thead>
          <tr className="bg-surface-2">
            {baslik.map((b, i) => (
              <th
                key={i}
                className="border-b border-border px-3 py-2 text-left text-2xs font-semibold uppercase tracking-[0.04em] text-text-3"
              >
                {satirIci(b)}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {govde.map((satir, i) => (
            <tr key={i} className="border-b border-border last:border-0">
              {satir.map((h, n) => (
                <td key={n} className="px-3 py-2 align-top leading-[1.55] text-text-2">
                  {satirIci(h)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

/** `**kalın**`, `*eğik*`, `` `kod` `` — iç içe geçmeyen basit biçimleme. */
function satirIci(metin: string): ReactNode {
  const parcalar: ReactNode[] = [];
  const desen = /(\*\*[^*]+\*\*|\*[^*]+\*|`[^`]+`)/g;
  let son = 0;
  let e: RegExpExecArray | null;

  while ((e = desen.exec(metin)) !== null) {
    if (e.index > son) parcalar.push(metin.slice(son, e.index));
    const p = e[0];

    if (p.startsWith('**')) {
      parcalar.push(
        <b key={e.index} className="font-semibold text-text">
          {p.slice(2, -2)}
        </b>,
      );
    } else if (p.startsWith('`')) {
      parcalar.push(
        <code
          key={e.index}
          className="rounded-sm bg-surface-2 px-1.5 py-px font-mono text-xs text-text"
        >
          {p.slice(1, -1)}
        </code>,
      );
    } else {
      parcalar.push(<i key={e.index}>{p.slice(1, -1)}</i>);
    }
    son = e.index + p.length;
  }

  if (son < metin.length) parcalar.push(metin.slice(son));
  return parcalar.map((p, i) => <Fragment key={i}>{p}</Fragment>);
}
