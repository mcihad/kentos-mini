# İş Takip ve Randevu Sistemi — Tasarım Sistemi Şartnamesi

**Sürüm 2.0 · Token tabanlı, çok kiracılı (multi-tenant), mobile-first**

Bu dosya tek başına yeterlidir: buradaki değerlerle uygulama birebir yeniden üretilebilir.
Belirsizlik bırakılmamıştır — her ölçü bir token'a, her token bir kurala bağlıdır.

> **Bu klasördeki `.dc.html` dosyaları Claude tasarım tuvali önizlemeleridir.**
> Inline stil kullanırlar; içlerinde Tailwind/Radix YOKTUR. Görsel
> referanstır — kaynak kod olarak kopyalanmaz.
>
> İçlerindeki örnek veriler (kişi adları, kurum adları, adresler)
> **uydurmadır**. Şartname ilk yazıldığında canlı sistemden alınmış gerçek
> kayıtlar taşıyordu; depo açık kaynak olduğu için hepsi değiştirildi.

| Dosya | İçerik |
|---|---|
| `Is Takip ve Randevu Sistemi v2.dc.html` | Token motoru (CSS), uygulama kabuğu, masaüstü + mobil çerçeveler, tema durumu |
| `TemaPaneli.dc.html` | Tema Tasarımcısı paneli (sağ üstteki paletten açılır) |
| `EkranV2.dc.html` | 7 iş ekranı + canlı bileşen kütüphanesi |
| `assets/amblem.png` | Amblem (kurumdan gelir) |
| `assets/icons/*.svg` | Lucide 1.31 ikon seti |

Hedef teknoloji: **React 18 + TypeScript + Vite + Tailwind CSS + TanStack (Query/Table/Virtual) + Radix UI + Lucide React**

---

## 0. Değişmez ilkeler

Bu on kural sistemin anayasasıdır. İhlal edilirse tasarım bozulur.

1. **Hiçbir bileşende ham renk, ham px, ham gölge yazılmaz.** Yalnızca `var(--token)` kullanılır. Tek istisna: 1px hairline yerine `var(--bw)`, ve `999px` pill yarıçapı yerine `var(--r-pill)` — yani istisna yok.
2. **Token katmanları tek yönlüdür:** çekirdek → türetilmiş → anlamsal → bileşen. Bileşen asla çekirdek token'a dokunmaz (`--brand` değil `--brand-ui`).
3. **Renk = anlam.** `--brand-ui` etkileşim, `--accent-ui` vurgu/dikkat, durum renkleri yalnızca durum. Dekoratif renk yok.
4. **Ölçü = boşluk birimi.** Tüm boşluk, yükseklik ve ikon kutusu `--sp` katıdır. Serbest px değeri yok.
5. **Yarıçap tek knob'dan türer.** `--r` değişince tüm köşeler oranlı değişir; bileşen kendi yarıçapını uydurmaz.
6. **Tipografi tek temelden türer.** `--fs` ve `--fs-d` değişince tüm ölçek kayar; bileşen sabit punto yazmaz.
7. **Gündüz/gece aynı bileşen, farklı anlamsal katman.** Bileşende `if (dark)` yoktur.
8. **Mobil ayrı bir ürün değil, ayrı bir yerleşim gramerdir:** liste satırı, alt sheet, push geçiş, alt aksiyon çubuğu. Masaüstü tablosu mobile küçültülerek taşınmaz.
9. **Hareket süresi tek knob'dan gelir** (`--dur`); `prefers-reduced-motion` her zaman kazanır.
10. **Dokunma hedefi ≥ 44px**, metin ≥ `--fs-3xs` (temel 14px'te 9.5px yalnızca UPPERCASE etiketlerde; okunur gövde ≥ `--fs-sm`).

---

## 1. Kurumsal kimlik bağlantısı

Kurumsal Kimlik Kılavuzu'ndan gelen bağlayıcı değerler (varsayılan tema = "Kurumsal Gündüz"):

| Öğe | Değer | Token |
|---|---|---|
| Amblem rengi 1 | PANTONE 294C · CMYK 100/69/7/30 · RGB 0/46/109 · `#002E6D` | `--brand` |
| Amblem rengi 2 | PANTONE 871C · CMYK 0/20/60/40 · RGB 167/137/82 · `#A78952` | `--accent` |
| Amblem rengi 3 | %85 Gri · RGB 77/77/79 · `#4D4D4F` | nötr skala referansı |
| Kurumsal font | **Gotham TR** (Thin→Black) | `--font-d` |
| Resmî yazışma | Times New Roman | *arayüzde kullanılmaz* |

Kılavuz kuralları: amblem deforme edilmez, tanımlı renkler dışında kullanılmaz, dişi kullanım **beyaz**, üzerine efekt uygulanmaz. Koyu zeminde amblem beyaz daire içinde durur.

**Font ikamesi:** Gotham TR'nin web lisansı yoksa `Montserrat` kullanılır (aynı geometrik sans karakteri, Türkçe diakritikleri tam). Lisans alındığında tek değişiklik: `--font-d: 'Gotham TR'`. Başka hiçbir değer değişmez.

**Beyaz etiket (white-label) sözleşmesi:** Buradaki renkler *varsayılan preset*'tir, sistemin tanımı değildir. Gerçek değerler kurum kaydından gelir (`kurum_bilgileri` tablosu, `GET /api/v2/institution`). Başka bir belediye için değişen tek şey `--brand`, `--accent`, `--neutral` ve `--font-*` çekirdek token'larıdır; anlamsal katman, bileşenler ve yerleşim aynı kalır. Kurumsal renk zorunluluğu olan kurumlar için preset'e ek olarak kendi hex değerleri girilir.

---

## 2. Token motoru

Üç katman. Her katman yalnızca kendinden öncekini okur.

```
KATMAN 1  ÇEKİRDEK (knob)      → Tema Tasarımcısı bunları yazar. 14 değer.
KATMAN 2  TÜRETİLMİŞ ÖLÇEK     → calc() ile knob'lardan üretilir. Elle yazılmaz.
KATMAN 3  ANLAMSAL             → mod'a (gündüz/gece) göre color-mix ile üretilir.
```

### 2.1 Katman 1 — Çekirdek token'lar (tek gerçek kaynak)

```css
:root{
  --brand:#002E6D;        /* marka rengi (gündüz UI'ında doğrudan kullanılır) */
  --brand-dk:#5B93E8;     /* markanın gece modu karşılığı (elle ayarlı, kontrast garantili) */
  --accent:#A78952;       /* vurgu */
  --accent-dk:#D8BB80;    /* vurgunun gece karşılığı */
  --neutral:#F5F4F0;      /* nötr taban (sıcaklık) */
  --sp:4px;               /* boşluk birimi — yoğunluk */
  --r:10px;               /* köşe ovalliği */
  --fs:14px;              /* temel yazı boyutu */
  --fs-d:1;               /* başlık ölçeği çarpanı */
  --track:0em;            /* harf aralığı */
  --bw:1px;               /* kenarlık kalınlığı */
  --sh-a:.07;             /* gölge yoğunluğu (alfa) */
  --dur:220ms;            /* hareket süresi */
  --font-d:'Montserrat';  /* başlık ailesi */
  --font-t:'IBM Plex Sans'; /* gövde ailesi */
}
```

**Neden marka için iki hex?** Tek hex'ten hem gündüz hem gece için kontrastı garanti eden bir varyant üretmek `color-mix` ile mümkün değildir (mix beyazla doygunluğu düşürür, siyahla okunmaz hale gelir). Bu yüzden her marka rengi *çift* tanımlanır ve gece modunda `--brand-ui: var(--brand-dk)` devreye girer. Aynı kural vurgu rengi için de geçerlidir.

### 2.2 Katman 2 — Türetilmiş ölçekler

```css
:root{
  /* boşluk — tüm padding/gap/margin buradan */
  --sp-05:calc(var(--sp)*.5);  --sp-1:var(--sp);            --sp-15:calc(var(--sp)*1.5);
  --sp-2:calc(var(--sp)*2);    --sp-25:calc(var(--sp)*2.5); --sp-3:calc(var(--sp)*3);
  --sp-35:calc(var(--sp)*3.5); --sp-4:calc(var(--sp)*4);    --sp-45:calc(var(--sp)*4.5);
  --sp-5:calc(var(--sp)*5);    --sp-6:calc(var(--sp)*6);    --sp-7:calc(var(--sp)*7);
  --sp-8:calc(var(--sp)*8);    --sp-10:calc(var(--sp)*10);  --sp-12:calc(var(--sp)*12);
  --sp-16:calc(var(--sp)*16);
  /* yarıçap — oranlar sabit, temel knob değişken */
  --r-xs:calc(var(--r)*.35);  --r-sm:calc(var(--r)*.62); --r-md:var(--r);
  --r-lg:calc(var(--r)*1.4);  --r-xl:calc(var(--r)*2);   --r-2xl:calc(var(--r)*3.2);
  --r-pill:999px;
  /* tipografi — çarpanlar sabit; başlık kademeleri --fs-d ile ayrıca ölçeklenir */
  --fs-3xs:calc(var(--fs)*.68); --fs-2xs:calc(var(--fs)*.75); --fs-xs:calc(var(--fs)*.83);
  --fs-sm:calc(var(--fs)*.91);  --fs-md:var(--fs);            --fs-lg:calc(var(--fs)*1.08);
  --fs-xl:calc(var(--fs)*1.26*var(--fs-d));
  --fs-2xl:calc(var(--fs)*1.55*var(--fs-d));
  --fs-3xl:calc(var(--fs)*2*var(--fs-d));
  --fs-4xl:calc(var(--fs)*2.55*var(--fs-d));
  --track-d:calc(var(--track) - .014em);   /* başlıklar gövdeden daha sıkı */
  /* bileşen ölçüleri — hepsi --sp katı */
  --h-ctrl:calc(var(--sp)*9);      /* 36px  · buton, input, çip */
  --h-ctrl-lg:calc(var(--sp)*11);  /* 44px  · sidebar öğesi, sekme */
  --h-ctrl-xl:calc(var(--sp)*12);  /* 48px  · form input, birincil buton (masaüstü giriş) */
  --h-appbar:calc(var(--sp)*16);   /* 64px  · masaüstü appbar + sidebar başlığı */
  --h-bar-m:calc(var(--sp)*13);    /* 52px  · mobil appbar */
  --h-row:calc(var(--sp)*13);      /* 52px  · tablo satırı */
  --h-row-m:calc(var(--sp)*15);    /* 60px  · mobil liste satırı */
  --h-tab:calc(var(--sp)*14);      /* 56px  · tabbar öğesi */
  --w-side:calc(var(--sp)*64);     /* 256px · sidebar */
  --w-side-sm:calc(var(--sp)*19);  /* 76px  · daraltılmış sidebar */
  /* gölge — tek alfa knob'undan üç kademe */
  --sh-1:0 1px 2px rgba(var(--sh-rgb),calc(var(--sh-a)*.75));
  --sh-2:0 var(--sp-1) calc(var(--sp)*3.5) rgba(var(--sh-rgb),var(--sh-a)),
         0 1px 2px rgba(var(--sh-rgb),calc(var(--sh-a)*.7));
  --sh-3:0 var(--sp-6) var(--sp-16) rgba(var(--sh-rgb),calc(var(--sh-a)*2.2)),
         0 2px 6px rgba(var(--sh-rgb),var(--sh-a));
  /* hareket */
  --ease:cubic-bezier(.32,.72,0,1);        /* giriş/çıkış — iOS benzeri */
  --ease-spring:cubic-bezier(.34,1.4,.64,1); /* basma geri sıçraması */
}
```

### 2.3 Katman 3 — Anlamsal renkler

Nötrler markayla **hafifçe boyanır** (`color-mix`), böylece hangi marka rengi seçilirse seçilsin tüm arayüz o renkle akraba olur. Bu, beyaz etiket kurulumunun "yapıştırılmış logo" gibi görünmesini engeller.

**Gündüz (`:root`, `[data-mod="acik"]`)**

```css
--canvas:color-mix(in oklab,var(--brand) 6%,var(--neutral));   /* uygulama dışı masa */
--bg:color-mix(in oklab,var(--brand) 3%,var(--neutral));        /* içerik zemini */
--surface:color-mix(in oklab,var(--brand) 1%,#fff);             /* kart, appbar */
--surface-2:color-mix(in oklab,var(--brand) 4%,#fff);           /* input, tablo başlığı, hover */
--sunken:color-mix(in oklab,var(--brand) 9%,var(--neutral));    /* segment yatağı, ikon çipi */
--line:color-mix(in oklab,var(--brand) 11%,#E4E2DC);
--line-2:color-mix(in oklab,var(--brand) 20%,#CCC9C1);
--ink:color-mix(in oklab,var(--brand) 14%,#0F141D);
--ink-2:color-mix(in oklab,var(--brand) 12%,#5A6274);
--ink-3:color-mix(in oklab,var(--brand) 10%,#8C93A1);
--brand-ui:var(--brand);
--brand-hover:color-mix(in oklab,var(--brand) 86%,#000);
--brand-soft:color-mix(in oklab,var(--brand) 9%,var(--surface));
--brand-line:color-mix(in oklab,var(--brand) 24%,var(--surface));
--on-brand:#fff;
--accent-ui:var(--accent);
--accent-soft:color-mix(in oklab,var(--accent) 15%,var(--surface));
--accent-ink:color-mix(in oklab,var(--accent) 72%,#241A06);   /* vurgu dolgusu üstünde metin */
--nav-bg:var(--brand);  --nav-panel:var(--brand);
--nav-ink:color-mix(in oklab,#fff 72%,var(--brand));
--nav-ink-strong:#fff;
--nav-line:color-mix(in oklab,#fff 15%,var(--brand));
--nav-hover:color-mix(in oklab,#fff 8%,var(--brand));
--nav-active:color-mix(in oklab,#fff 15%,var(--brand));
--chrome:color-mix(in oklab,var(--brand) 2%,#fff);
--sh-rgb:14,22,38;
--warn:#8A6209; --ok:#1A6A45; --info:#1D5CA3; --danger:#A33127; --mute:#6B675F; --slate:#3B4C6E;
--st-mix:13%;
```

**Gece (`[data-mod="koyu"]`)** — aynı isimler, farklı formüller:

```css
--canvas:color-mix(in oklab,var(--brand) 7%,#05080D);
--bg:color-mix(in oklab,var(--brand) 9%,#080C13);
--surface:color-mix(in oklab,var(--brand) 12%,#0E141E);
--surface-2:color-mix(in oklab,var(--brand) 15%,#131A26);
--sunken:color-mix(in oklab,var(--brand) 8%,#0A0E16);
--line:color-mix(in oklab,var(--brand) 18%,#1B2432);
--line-2:color-mix(in oklab,var(--brand) 26%,#293344);
--ink:color-mix(in oklab,var(--brand) 4%,#E8EDF5);
--ink-2:color-mix(in oklab,var(--brand) 8%,#9AA6B9);
--ink-3:color-mix(in oklab,var(--brand) 10%,#6B788D);
--brand-ui:var(--brand-dk);
--brand-hover:color-mix(in oklab,var(--brand-dk) 80%,#fff);
--brand-soft:color-mix(in oklab,var(--brand-dk) 14%,var(--surface));
--brand-line:color-mix(in oklab,var(--brand-dk) 28%,var(--surface));
--on-brand:color-mix(in oklab,var(--brand-dk) 20%,#03060B);   /* açık butonda koyu metin */
--accent-ui:var(--accent-dk); --accent-ink:var(--accent-dk);
--accent-soft:color-mix(in oklab,var(--accent-dk) 13%,var(--surface));
--nav-bg:color-mix(in oklab,var(--brand) 10%,#080D15);
--nav-panel:color-mix(in oklab,var(--brand) 55%,#040810);
--nav-active:color-mix(in oklab,var(--brand-dk) 14%,var(--nav-bg));
--chrome:color-mix(in oklab,var(--brand) 10%,#0B1017);
--sh-rgb:0,0,0; --sh-a:.42;
--warn:#E0B457; --ok:#5FBE90; --info:#78ADF0; --danger:#E88178; --mute:#A7A29A; --slate:#9BB2D8;
--st-mix:15%;
```

**Durum dolguları (moddan bağımsız, tek formül):**

```css
--warn-soft:color-mix(in oklab,var(--warn) var(--st-mix),var(--surface));
--ok-soft:   /* … aynı kalıp: ok, info, danger, mute, slate */
--on-ok:color-mix(in oklab,var(--ok) 22%,#fff);              /* gündüz: beyaz metin */
[data-mod="koyu"] --on-ok:color-mix(in oklab,var(--ok) 18%,#03060B); /* gece: koyu metin */
```

### 2.4 Token sözlüğü — hangi token nerede kullanılır

| Token | Kullanım | Kullanılmaz |
|---|---|---|
| `--brand` | Sidebar zemini, giriş paneli, preset tanımı | Buton zemini (onun yerine `--brand-ui`) |
| `--brand-ui` | Birincil buton, aktif sekme, bağlantı, seçili çip, FAB | Geniş dekoratif alan |
| `--brand-soft` | Seçili satır zemini, ikon çipi, makam etiketi | Metin rengi |
| `--accent-ui` | Aktif nav çubuğu, "bugün" halkası, sekme altı çizgi, canlı noktası, kart üst şeridi | Buton zemini, geniş yüzey |
| `--accent-soft` + `--accent-ink` | "Bugün", "Canlı", "Tören" çipleri | Gövde metni |
| `--ink` / `--ink-2` / `--ink-3` | Başlık ve gövde / ikincil / meta ve etiket | `--ink-3` uzun gövde metninde |
| `--surface` / `--surface-2` / `--sunken` | Kart / input-hover-tablo başlığı / segment yatağı | — |
| `--line` / `--line-2` | Tüm kenarlıklar / vurgulu kenarlık, kaydırıcı topu | Metin |
| Durum renkleri | Yalnızca talep durumu ve etkinlik türü | Dekorasyon |

---

## 3. Tema Tasarımcısı (sözleşme)

**Erişim:** sağ üstteki palet ikonu — hem sayfa çubuğunda hem uygulama appbar'ında. Panel sağdan `panelIn` animasyonuyla girer, genişlik `min(420px, 100vw)`, kendi içinde kaydırılır, overlay tıklaması ve kapat butonu ile kapanır.

**Çalışma biçimi:** panel yalnızca **çekirdek token'ları** yazar. Yazma yolu:

```js
document.documentElement.setAttribute('data-mod', mod);        // 'acik' | 'koyu'
document.documentElement.style.setProperty('--r', r + 'px');   // ve diğer 13 knob
```

Anlamsal katman ve bileşenler hiçbir şey bilmez; `color-mix` ve `calc` zinciri sonucu tek karede günceller. Bu yüzden tema değişimi yeniden render gerektirmez.

### 3.1 Kontroller

| # | Kontrol | Token | Aralık / seçenek | Varsayılan |
|---|---|---|---|---|
| 1 | Hazır temalar | tümü | 7 preset (§3.2) | Kurumsal Gündüz |
| 2 | Mod | `data-mod` | Gündüz / Gece | Gündüz |
| 3 | Marka rengi | `--brand`, `--brand-dk` | 6 küratörlü palet | Lacivert |
| 4 | Vurgu rengi | `--accent`, `--accent-dk` | 6 küratörlü palet | Altın |
| 5 | Nötr taban | `--neutral` | Sıcak kâğıt / Nötr gri / Soğuk mavi | Sıcak kâğıt |
| 6 | Köşe ovalliği | `--r` | 0 – 20px, adım 1 | 10px |
| 7 | Gölge yoğunluğu | `--sh-a` | 0 – 0.55, adım 0.01 | 0.07 (gece 0.42) |
| 8 | Boşluk birimi | `--sp` | 3.2 – 5.2px, adım 0.1 | 4px |
| 9 | Temel yazı boyutu | `--fs` | 12 – 17px, adım 0.5 | 14px |
| 10 | Başlık ölçeği | `--fs-d` | 0.9 – 1.3, adım 0.05 | 1 |
| 11 | Harf aralığı | `--track` | −0.02 – 0.04em, adım 0.005 | 0 |
| 12 | Hareket süresi | `--dur` | 0 – 400ms, adım 10 | 220ms |
| 13 | Kenarlık kalınlığı | `--bw` | 1 / 1.5 / 2px | 1px |
| 14 | Yazı tipi çifti | `--font-d`, `--font-t` | 3 çift (§3.3) | Kurumsal |

Panel ayrıca **canlı önizleme** (kart + iki buton + durum çipi) ve **token çıktısı** (`globals.css`'e yapıştırılabilir `:root{}` bloğu) gösterir. Bir knob elle değiştirildiğinde preset adı "Özel tema"ya döner; **Sıfırla** varsayılana getirir.

Renk seçimi serbest picker değil, **küratörlü palet**tir: her seçenek gündüz+gece çifti olarak elle dengelenmiştir, dolayısıyla hiçbir seçim kontrast kuralını bozamaz.

### 3.2 Hazır temalar

| Preset | Mod | Marka | Vurgu | `--r` | `--sp` | `--bw` | Font | Kullanım |
|---|---|---|---|---|---|---|---|---|
| Kurumsal Gündüz | Gündüz | Lacivert | Altın | 10 | 4 | 1 | Kurumsal | Varsayılan, kurumsal kimlik |
| Kurumsal Gece | Gece | Lacivert | Altın | 10 | 4 | 1 | Kurumsal | Akşam nöbeti, düşük ışık |
| Zümrüt Belediye | Gündüz | Zümrüt | Altın | 15 | 4.5 | 1 | Modern | Yeşil kimlikli kurumlar |
| Bordo Belediye | Gündüz | Bordo | Bakır | 5 | 4 | 1.5 | Editoryal | Klasik/ciddi kimlikler |
| Petrol Mavisi | Gündüz | Petrol | Turkuaz | 17 | 4.5 | 1 | Modern | Yumuşak, çağdaş kurulum |
| Antrasit Gece | Gece | Antrasit | Gri | 8 | 4 | 1 | Modern | Renk kısıtı olan kurumlar |
| Yüksek Kontrast | Gündüz | Mor | Kiraz | 3 | 4 | 2 | Editoryal | Erişilebilirlik modu |

**Renk paletleri** (gündüz / gece çiftleri):

Marka — Lacivert `#002E6D`/`#5B93E8` · Zümrüt `#0B5D45`/`#4FBE94` · Bordo `#7A1F2B`/`#E58189` · Petrol `#0E4C5C`/`#4FB6C8` · Mor `#4B2E83`/`#A98BE8` · Antrasit `#333A45`/`#A6B3C6`

Vurgu — Altın `#A78952`/`#D8BB80` · Bakır `#A65A2E`/`#E29A6B` · Turkuaz `#157F7F`/`#5CC9C4` · Kiraz `#A8324A`/`#EE8397` · Yeşil `#4A7A2B`/`#97CE6E` · Gri `#7C8592`/`#B3BDCB`

Nötr — Sıcak kâğıt `#F5F4F0` · Nötr gri `#F4F4F5` · Soğuk mavi `#F1F4F9`

### 3.3 Yazı tipi çiftleri

| Çift | Başlık (`--font-d`) | Gövde (`--font-t`) | Karakter |
|---|---|---|---|
| Kurumsal | Montserrat 600/700 | IBM Plex Sans 400/500/600 | Gotham TR ikamesi, resmî |
| Modern | Figtree 500/600/700 | Source Sans 3 400/500/600 | Yumuşak, çağdaş |
| Editoryal | Archivo 600/700 | Karla 400/500/600 | Sıkı, gazete tonu |

Üç çiftin tamamı Türkçe diakritiklerini (İ ı Ğ ğ Ş ş Ö ö Ç ç Ü ü) tam destekler. Tüm gövdede `font-variant-numeric: tabular-nums` zorunludur (tablo ve saat hizası).

---

## 4. Tipografi kuralları

| Rol | Token | Ağırlık | Aile | Not |
|---|---|---|---|---|
| Giriş ekranı H1 | `--fs-4xl` | 700 | `--font-d` | `--track-d`, `line-height:1.16` |
| Mobil büyük başlık | `--fs-3xl` | 700 | `--font-d` | Ekran başlığı, kaydırınca appbar'a devreder |
| Ekran H1 / metrik | `--fs-2xl` | 700 | `--font-d` | `line-height:1.3` |
| Appbar başlığı | `--fs-xl` | 700 | `--font-d` | Tek satır, ellipsis |
| Vurgulu gövde | `--fs-lg` | 500 | `--font-t` | Etkinlik adı, form değeri |
| Gövde | `--fs-md` | 400 | `--font-t` | `line-height:1.45–1.7` |
| Tablo / liste | `--fs-sm` | 400 | `--font-t` | — |
| Meta, buton | `--fs-xs` | 500 | `--font-t` | — |
| Alan etiketi, çip | `--fs-2xs` | 600 | `--font-t` | `letter-spacing:.08em`, UPPERCASE |
| Kolon başlığı, tabbar | `--fs-3xs` | 600 | `--font-t` | `letter-spacing:.09–.14em`, UPPERCASE |

Kurallar: başlıklarda `--track-d` (gövdeden 0.014em daha sıkı) · uzun Türkçe başlıklarda `text-wrap:pretty` · liste satırı başlıklarında en fazla 2 satır (`-webkit-line-clamp:2`) · gövde metni asla `--ink-3` değil.

---

## 5. Yerleşim

### 5.1 Masaüstü (referans 1440×912)

```
┌────────────────────────────────────────────────────────────┐
│ sidebar          │ appbar  --h-appbar (64)                 │
│ --w-side (256)   ├─────────────────────────────────────────┤
│ --nav-bg         │ içerik: overflow-y:auto                 │
│                  │ padding --sp-6 (24) · gap --sp-45 (18)  │
└────────────────────────────────────────────────────────────┘
```

**Sidebar** — marka bloğu (`--h-appbar`, alt kenarlık) → 3 gruplu navigasyon → kullanıcı çipi (üst kenarlık).
Grup etiketleri: `--fs-3xs` 600 `.14em` UPPERCASE, `--nav-ink` %60 opaklık.
Öğe: `--h-ctrl-lg` yükseklik, `--r-sm` yarıçap, ikon 19px + etiket `--fs-sm`.
**Aktif öğe** = `--nav-active` zemin + `--nav-ink-strong` metin + solda 3×20px `--accent-ui` çubuk (`--r` 0 3 3 0).
Daraltma: `--w-side-sm` (76px), etiketler `display:none`, `title` ipucu korunur, geçiş `width var(--dur) var(--ease)`.
Sayaç badge: `--accent-ui` zemin, koyu metin, `--r-pill`.

**Appbar** (soldan sağa) — daralt butonu 34px · başlık + alt satır · esnek boşluk · arama (`flex:0 1 280px`, `⌘K`) · tarih çipi · bildirim (kırmızı nokta) · **Tema Tasarımcısı** (palet, `--brand-soft` zemin) · dikey ayraç · kullanıcı çipi.

**İçerik** — yatay taşma yasaktır. Tablo dar kolonları `width:1%` + `white-space:nowrap`; yalnızca "Konu" kolonu esner (`min-width:260px`).

### 5.2 Mobil (referans 390×844) — native gramer

```
┌──────────────┐
│ durum çubuğu │ 47   saat · sinyal · wifi · pil
│ appbar       │ --h-bar-m (52)
├──────────────┤
│ büyük başlık │ --fs-3xl (yalnızca kök ekranlarda)
│ içerik       │ padding --sp-4 (16) · gap --sp-3 (12)
├──────────────┤
│ tabbar       │ --h-tab (56) + --sp-5 güvenli alan + home indicator
└──────────────┘
```

**Kök ekran (Ana Sayfa, Talepler, Ajanda, Takvim)**
- Appbar: amblem + (kaydırılınca görünen) küçük başlık + bildirim + tema. Kök ekranda başlık **büyük** olarak içerikte durur.
- Kaydırma > 34px → appbar başlığı `opacity 0→1` + `translateY(4px→0)`, süre `--dur`. Büyük başlık doğal olarak yukarı kayar. (iOS "large title" davranışı.)
- Tabbar 5 sekme: Ana Sayfa · Talepler · Ajanda · Takvim · Menü. Aktif: `--brand-ui` renk, etiket 600, ikon `scale(1.06)` yay geçişiyle.
- FAB: Talepler/Ajanda/Takvim ekranlarında sağ altta 56px `--r-xl` daire, `--brand-ui`, `--sh-3`; basmada `scale(.92)`.
- Home indicator: 134×5px, `--ink` %22 opaklık.

**Detay ekranı (Talep Detayı, Etkinlik Detayı) — push navigasyon**
- Tabbar **gizlenir** (native push davranışı).
- Appbar: solda `‹ Talepler` geri butonu (`--brand-ui`), başlık **ortalanmış** ve her zaman görünür.
- Giriş animasyonu `itA/itB` (sağdan 26px + opaklık), geri dönüş `geA/geB` (soldan 20px). Aynı animasyonun A/B ikizi olması, her ekran değişiminde animasyonun yeniden tetiklenmesini sağlar.
- Alt aksiyon çubuğu: `position:sticky; bottom:0`, zeminden yumuşak geçiş; Reddet (flex 1) / Onayla (flex 1.8), yükseklik 52px.

**Liste satırı (mobil temel bileşen)**
- Kart grubu: `--surface` + `--bw` kenarlık + `--r-lg`, `overflow:hidden`, `--sh-1`.
- Satır: `padding: --sp-3 --sp-35`, sol ikon çipi 30px `--r-sm` (durum renginde), başlık 2 satır, meta satırı, sağda 17px chevron (%60 opaklık).
- Ayırıcı: son satır hariç `inset 0 -1px 0 var(--line)` (satır içi gölge — grup köşelerini bozmaz).
- Basma: `--sunken` zemin flaşı; giriş animasyonu `rowIn` + `28ms × index` gecikmeli kademe.
- Grup sonunda tam genişlik bağlantı satırı ("Tüm günü gör", "Tüm talepler (114)").

**Alt sheet (Menü)** — `--r-2xl` üst köşeler, 44×5px tutamak, `sheetUp` animasyonu (`--dur × 1.4`), overlay `rgba(4,8,14,.5)` + 2px blur, satırlar 56px.

**Ajanda gün şeridi** — 7 gün yatay kaydırma, 50px kartlar, seçili gün `--brand-ui` dolgu; etkinliği olan günde 4px nokta.

### 5.3 Kırılım ve eşleme

Tasarım dosyasında iki çerçeve aynı ekran bileşenini kullanır; farkı CSS değişkenleri kurar. React tarafında karşılığı `md:` (768px) kırılımıdır.

| Değişken | Masaüstü | Mobil | Tailwind |
|---|---|---|---|
| `--pad` | `--sp-6` | `--sp-4` | `p-4 md:p-6` |
| `--gap` | `--sp-45` | `--sp-3` | `gap-3 md:gap-[18px]` |
| `--cardpad` | `--sp-45` | `--sp-35` | `p-3.5 md:p-[18px]` |
| `--stat-cols` | `repeat(auto-fit,minmax(158px,1fr))` | `repeat(2,1fr)` | `grid-cols-2 md:grid-cols-[…]` |
| `--main-cols` | `2.05fr 1fr` | `1fr` | `md:grid-cols-[2.05fr_1fr]` |
| `--detay-cols` | `1.85fr 1fr` | `1fr` | `md:grid-cols-[1.85fr_1fr]` |
| `--d` / `--m` | `block` / `none` | `none` / `block` | `hidden md:block` / `md:hidden` |
| `--tbl` | `table` | `none` | `hidden md:table` |
| `--cellh` | 104px | 54px | `min-h-[54px] md:min-h-[104px]` |
| `--h1` | `--fs-2xl` | `--fs-xl` | `text-xl md:text-2xl` |

768px altında: sidebar → tabbar, tablo → liste satırı, sağ kolon → dikey akış, hover → `:active`.

---

## 6. Hareket

| Ad | Kullanım | Tanım |
|---|---|---|
| `itA/itB` | Detaya push | `translate3d(26px,0,0)` + opaklık, `--dur`, `--ease` |
| `geA/geB` | Geri pop | `translate3d(-20px,0,0)` + opaklık |
| `pgA/pgB` | Sekme değişimi | `translate3d(0,--sp-2,0)` + opaklık |
| `rowIn` | Liste kademesi | `translate3d(0,10px,0)`, gecikme `index × 26–28ms` |
| `sheetUp` | Alt sheet | `translate3d(0,100%,0)`, `--dur × 1.4` |
| `panelIn` | Tema paneli | `translate3d(100%,0,0)`, `--dur × 1.3` |
| `fadeIn` | Overlay | opaklık |
| `nabiz` | Canlı noktası, iskelet | 1.4–2s sonsuz, opaklık 1 → .4 |

Etkileşim geri bildirimi: masaüstü `:hover` yalnızca zemin/kenarlık değiştirir; mobil `:active` `scale(.92–.98)` + `--ease-spring`. Transform dışında layout etkileyen animasyon yasaktır.

`@media (prefers-reduced-motion: reduce)` → tüm animasyon ve geçişler `0.01ms`.

---

## 7. Bileşen kataloğu

Canlı hâli uygulamadaki **Bileşen Kütüphanesi** ekranındadır (Yönetim grubu): renk token'ları, tipografi ölçeği, yarıçap/gölge, boşluk ölçeği, buton varyantları, durum çipleri, form alanları, liste satırı, boş durum, iskelet. Tema değiştikçe hepsi canlı güncellenir — tasarım denetimi bu ekrandan yapılır.

### 7.1 Buton

| Varyant | Zemin | Metin | Kenarlık | Not |
|---|---|---|---|---|
| Birincil | `--brand-ui` | `--on-brand` | yok | `--font-d` 600, `--sh-1`, hover `--brand-hover` |
| İkincil | `--surface` | `--ink-2` | `--bw --line` | hover `--surface-2` + `--ink` |
| Onay | `--ok` | `--on-ok` | yok | `--font-d` 600 |
| Yıkıcı (yumuşak) | `--danger-soft` | `--danger` | `--bw --danger-soft` | 600 |
| Sade | şeffaf | `--brand-ui` | yok | hover `--brand-soft` |
| Pasif | `--sunken` | `--ink-3` | `--bw --line` | `cursor:not-allowed`, `opacity:.7` |
| İkon | `--surface-2` | `--ink-2` | `--bw --line` | kare `--h-ctrl` |

Yükseklik `--h-ctrl` (masaüstü) / 48–54px (mobil). Yatay dolgu `--sp-35`. Yarıçap `--r-sm`. İkon 15px (34–36px buton), 17–19px (38px+ ve mobil), stroke 1.8–2.2. İkon–metin arası `--sp-15`.

### 7.2 Form alanı

Etiket: `--fs-2xs` 600 `.08em` UPPERCASE `--ink-3`, altında `--sp-15` boşluk.
Kutu: `--h-ctrl-lg`/`--h-ctrl-xl` (mobil 52px), `--surface-2` zemin, `--bw --line` kenarlık, `--r-sm`, iç dolgu `--sp-35`, sol ikon `--ink-3`.
Odak: `focus-visible` → 2px `--brand-ui` outline, `offset:2px`.
Hata: kenarlık `--danger`, zemin `--danger-soft`, metin `--danger`, sağda uyarı ikonu.

### 7.3 Durum çipi

```
inline-flex · height 24px · padding 0 --sp-25 · --r-pill
zemin <durum>-soft · metin <durum> · --fs-3xs 600
içinde 5×5px currentColor nokta
```

| Anahtar | Etiket | Renk | İkon |
|---|---|---|---|
| `beklemede` | Beklemede | `--warn` | hourglass |
| `onaylandi` | Onaylandı | `--ok` | circle-check |
| `devam` | Devam Ediyor | `--info` | calendar-clock |
| `reddedildi` | Reddedildi | `--danger` | circle-x |
| `iptal` | İptal Edildi | `--mute` | ban |
| `tamamlandi` | Tamamlandı | `--slate` | check-check |

### 7.4 Durum kartı (StatTile)

`--surface` + `--bw --line` + `--r-md` + `--sh-1`, üst dolgu `--sp-3`, yan `--sp-35`, alt 0.
Sıra: etiket (`--fs-3xs` UPPERCASE, esner) + sağda 28px durum ikonu çipi → metrik (`--fs-3xl` 700) → tam genişlik 3px durum rengi şerit (`margin: 0 calc(var(--sp-35) * -1)`).
Tıklama `/talepler?durum=…`. Hover `--line-2`, basma `scale(.98)`.

### 7.5 Veri tablosu (masaüstü)

TanStack Table v8: `getCoreRowModel`, `getSortedRowModel`, `getFilteredRowModel`, `getPaginationRowModel`.
Başlık: `--surface-2`, `sticky top-0`, hücre `--fs-3xs` 600 `.09em` UPPERCASE `--ink-3`, dolgu `--sp-3 --sp-2`.
Satır: alt kenarlık `--line`, hover `--surface-2`, tıklanabilir, sonda chevron.
Kolon sırası: **Durum · Konu · Ad Soyad · Telefon · Makam · Tarih · ›**
Alt çubuk: `--surface-2`, "N kayıt gösteriliyor · toplam 114" + iki 32px ok.
768px altında tablo yerine liste satırı grubu (§5.2).

### 7.6 Ajanda etkinlik kartı

```
[saat 62px, sağa dayalı]   ┌4px tür┬─────────────────────────┐
  11:00 / 11:30            │       │ başlık --fs-md 500      │
                           │       │ 📍 yer · TÜR çipi   📷  │
                           └───────┴─────────────────────────┘
```
Mobilde saat kolonu kartın içine, başlığın üstüne taşınır (`11:00 · 11:30`).
Fotoğrafı olan etkinlikte sağ üstte 28px kamera çipi.
**"Şu an" çizgisi:** `14:12 ŞU AN` (`--font-d` 700 `--fs-2xs` `--accent-ink`) + 1px `--accent-ui` çizgi + 7px nokta.

### 7.7 Takvim hücresi

`min-height:--cellh`, dolgu `--sp-15 --sp-2`, sağ/alt kenarlık `--line`, `overflow:hidden`.
Ay dışı: `--surface-2` zemin, `--ink-3` gün numarası.
Bugün: `--brand-soft` zemin, `--brand-ui` numara, `inset 0 0 0 1.5px var(--accent-ui)` halka.
Seçili (mobil): `--line-2` halka.
Etkinlik çipi (masaüstü): `--surface-2` zemin + 2px sol tür rengi + saat `--fs-3xs` 600 + başlık 2 satır kırpma. Fazlası `+N daha` (`--brand-ui`).
Mobil: çip yok; gün numarası yanında en fazla 3 adet 5px tür noktası, grid altında seçili gün listesi.
Tür lejantı: 9px kare + `--fs-2xs` etiket, 5 tür.

### 7.8 Diğer bileşenler

| Bileşen | Kural |
|---|---|
| Segmented | `--sunken` yatak + `--bw --line` + `--r-sm` + 3px iç dolgu; aktif `--surface` + `--sh-1` + `--ink`; yükseklik 30px |
| Filtre çipi | `--h-ctrl`, `--r-pill`; aktif `--brand-ui`/`--on-brand`; sayaç `opacity:.65` |
| Sekme | `--h-ctrl-lg`; aktif altta 2px `--accent-ui`; pasif `--ink-3` |
| Durum akışı | 22px yuvarlak (durum dolgusu) + 8px iç nokta + 1px `--line` dikey çizgi |
| Not öğesi | 30px baş harf çipi + ad (`--fs-xs` 600) + zaman (`--fs-3xs` `--ink-3`) + metin (`--fs-sm`/1.6) |
| Bilgi ızgarası | Masaüstü `repeat(auto-fit,minmax(170px,1fr))`; mobilde etiket-değer satırlarına dönüşür |
| Boş durum | 44–52px `--r-lg` `--sunken` ikon kutusu + `--font-d` başlık + `--fs-2xs` açıklama (maks 300px) + birincil buton |
| İskelet | `--sunken` bloklar, `nabiz` 1.4s, 0.15s kademeli gecikme |
| Görsel yer tutucu | `repeating-linear-gradient(45deg,var(--sunken) 0 8px,transparent 8px 16px)` + monospace etiket |
| Cihaz/tarayıcı çerçevesi | Yalnızca sunum; ürün kodunda yer almaz |

---

## 8. Ekranlar

| Rota | Ekran | Mobil davranış |
|---|---|---|
| `/giris` | Giriş | Kabuk yok; dikey merkezli form |
| `/` | Ana Sayfa | Büyük başlık + 2 kolon metrik + 2 inset liste |
| `/talepler` | Talepler | Arama + yatay filtre çipleri + liste + FAB |
| `/talepler/:id` | Talep Detayı | Push; alt aksiyon çubuğu |
| `/ajanda` | Ajanda | Gün şeridi + etkinlik kartları + FAB |
| `/ajanda/:id` | Etkinlik Detayı | Push |
| `/takvim` | Takvim | Nokta ızgarası + seçili gün listesi + FAB |
| `/bilesenler` | Bileşen Kütüphanesi | Dikey akış |
| `/istatistikler`, `/ayarlar` | (tasarlanacak) | — |

Alt rota üst nav öğesini aktif tutar.

**8.1 Giriş** — Masaüstü: %46 `--nav-panel` panel (amblem + kurum + 46×3px altın çizgi + `--fs-4xl` iki satır başlık + açıklama + telif; sağ altta 440/290px 1px çemberler) + sağda 376px form (Giriş Yap → kullanıcı adı → şifre/göz → beni hatırla + şifremi unuttum → `--h-ctrl-xl` birincil buton → Bilgi İşlem satırı). Mobil: 74px amblem ortalı, `--fs-2xl` başlık, 38×3px altın çizgi, 52px inputlar, 54px buton, altta iki satır telif.

**8.2 Ana Sayfa** — 6 durum kartı (Beklemede 6 · Onaylandı 29 · Devam Ediyor 66 · Reddedildi 0 · İptal 0 · Tamamlandı 13) → Son Talepler tablosu (2.05fr) + Bugünün Programı (1fr, "Canlı" nabız çipi, altta "Ajandayı aç"). Mobilde iki inset liste grubu.

**8.3 Talepler** — filtre çipleri (Tümü 114 · Beklemede 6 · Onaylandı 29 · Devam 66 · Tamamlandı 13) + Filtrele / Dışa Aktar / Yeni Talep; tablo 10 satır + sayfalama.

**8.4 Talep Detayı** — geri + Düzenle/Reddet/**Onayla ve Randevu Ver**; başlık kartı (çip + `TLP-2026-0114` + kanal + H1); sol: Talep Sahibi (6 alan) → Talep Metni → Notlar (2 + ekleme satırı); sağ: Durum Akışı (4 adım) → Önerilen Randevu (52px tarih bloğu + çakışma notu).

**8.5 Ajanda** — ‹ Bugün › + segmented (Gün/Hafta/Liste) + Dışa Aktar + Yeni Etkinlik; gün başlığı kartı (56px `11/AĞU` + "Salı" + BUGÜN çipi + 3 özet sayaç); 6 etkinlik, 14:00 üstünde "şu an" çizgisi.

**8.6 Takvim** — ‹ Ağustos 2026 › + segmented (Hafta/Ay/Liste) + Yeni Etkinlik; 7×6 ızgara (27 Temmuz – 6 Eylül), gün adları Pzt…Paz, altta tür lejantı.

**8.7 Etkinlik Detayı** — geri + Not Ekle/Düzenle/Sil; başlık kartı üstte 3px `--accent-ui` şerit, tür + makam dışı çipleri + `ETK-2026-8421` + H1 + saat/konum; sol: Etkinlik Detayları (6 alan) → sekmeli kart (Notlar boş durumu / Fotoğraflar 4:3 yer tutucular); sağ: Konum (150px harita) → Kayıt Bilgileri (4 satır) → İlgili Talep bağlantı kartı.

**8.8 Bileşen Kütüphanesi** — §7'nin canlı hâli. Yeni bileşen eklendiğinde **bu ekrana da eklenir**; aksi hâlde bileşen sisteme dahil sayılmaz.

---

## 9. Veri modeli

```ts
export type Durum = 'beklemede'|'onaylandi'|'devam'|'reddedildi'|'iptal'|'tamamlandi';
export type EtkinlikTuru = 'kabul'|'toren'|'basin'|'toplanti'|'nikah'|'inceleme';

export interface Talep {
  id: string;                 // TLP-2026-0114
  durum: Durum;
  konu: string;
  adSoyad: string;
  telefon?: string;
  kurum?: string;
  makam: string;              // 'Örnek Belediye Başkanı'
  tur: string;                // 'Görüşme / Ziyaret'
  kisiSayisi?: number;
  metin: string;
  kanal: 'web'|'telefon'|'evrak'|'mobil';
  olusturmaTarihi: string;    // ISO
  notlar: Not[];
  akis: AkisAdimi[];
  onerilenRandevu?: { baslangic: string; bitis: string; yer: string };
  etkinlikId?: string;
}

export interface Etkinlik {
  id: string;                 // ETK-2026-8421
  baslik: string;
  tur: EtkinlikTuru;
  makamDisi: boolean;
  baslangic: string; bitis: string;
  yer: string;
  irtibatKisi?: string; irtibatTelefon?: string;
  katilim?: string;
  tekrar: 'yok'|'gunluk'|'haftalik'|'aylik';
  fotograflar: string[];
  notlar: Not[];
  talepId?: string;
  olusturan: string; olusturmaTarihi: string; guncellemeTarihi: string;
}

export interface Not { id: string; kisi: string; zaman: string; metin: string }
export interface AkisAdimi { ad: string; zaman: string; durum: 'tamam'|'aktif'|'bekleyen' }

export interface TemaAyari {           // Tema Tasarımcısı çıktısı — kiracı başına saklanır
  preset: string;                       // 'kurumsal-acik' | … | 'ozel'
  mod: 'acik'|'koyu';
  marka: number; vurgu: number; notr: number; font: number;   // palet indeksleri
  r: number; sp: number; fs: number; fsd: number;
  track: number; bw: number; sha: number; dur: number;
}
```

Tarih biçimleri: tablo `dd.MM.yyyy HH:mm` · mobil liste `dd.MM` · başlık `d MMMM yyyy` · gün kısaltmaları `Pzt Sal Çar Per Cum Cmt Paz` · ay `AĞU`. `date-fns` + `tr` locale, hafta Pazartesi başlar.

---

## 10. Uygulama (React)

### 10.1 Paketler

```
react react-dom react-router-dom
@tanstack/react-query @tanstack/react-table @tanstack/react-virtual
@radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-tabs
@radix-ui/react-toggle-group @radix-ui/react-checkbox @radix-ui/react-select
@radix-ui/react-slider @radix-ui/react-tooltip @radix-ui/react-popover @radix-ui/react-toast
lucide-react date-fns clsx tailwind-merge
tailwindcss postcss autoprefixer
```

### 10.2 `tailwind.config.ts`

```ts
export default {
  darkMode: ['class', ':root[data-mod="koyu"]'],
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        canvas:'var(--canvas)', bg:'var(--bg)', surface:'var(--surface)',
        'surface-2':'var(--surface-2)', sunken:'var(--sunken)',
        line:'var(--line)', 'line-2':'var(--line-2)',
        ink:'var(--ink)', 'ink-2':'var(--ink-2)', 'ink-3':'var(--ink-3)',
        brand:'var(--brand-ui)', 'brand-hover':'var(--brand-hover)',
        'brand-soft':'var(--brand-soft)', 'brand-line':'var(--brand-line)',
        'on-brand':'var(--on-brand)',
        accent:'var(--accent-ui)', 'accent-soft':'var(--accent-soft)', 'accent-ink':'var(--accent-ink)',
        warn:'var(--warn)', 'warn-soft':'var(--warn-soft)',
        ok:'var(--ok)', 'ok-soft':'var(--ok-soft)', 'on-ok':'var(--on-ok)',
        info:'var(--info)', 'info-soft':'var(--info-soft)',
        danger:'var(--danger)', 'danger-soft':'var(--danger-soft)',
        mute:'var(--mute)', 'mute-soft':'var(--mute-soft)',
        slate:'var(--slate)', 'slate-soft':'var(--slate-soft)',
        nav:{ bg:'var(--nav-bg)', panel:'var(--nav-panel)', ink:'var(--nav-ink)',
              strong:'var(--nav-ink-strong)', line:'var(--nav-line)',
              hover:'var(--nav-hover)', active:'var(--nav-active)' },
      },
      fontFamily: {
        display:['var(--font-d)','system-ui','sans-serif'],
        sans:['var(--font-t)','system-ui','sans-serif'],
      },
      fontSize: {
        '3xs':'var(--fs-3xs)','2xs':'var(--fs-2xs)',xs:'var(--fs-xs)',sm:'var(--fs-sm)',
        base:'var(--fs-md)',lg:'var(--fs-lg)',xl:'var(--fs-xl)','2xl':'var(--fs-2xl)',
        '3xl':'var(--fs-3xl)','4xl':'var(--fs-4xl)',
      },
      spacing: {
        1:'var(--sp-1)',1.5:'var(--sp-15)',2:'var(--sp-2)',2.5:'var(--sp-25)',
        3:'var(--sp-3)',3.5:'var(--sp-35)',4:'var(--sp-4)',4.5:'var(--sp-45)',
        5:'var(--sp-5)',6:'var(--sp-6)',7:'var(--sp-7)',8:'var(--sp-8)',
        10:'var(--sp-10)',12:'var(--sp-12)',16:'var(--sp-16)',
      },
      borderRadius: {
        xs:'var(--r-xs)',sm:'var(--r-sm)',DEFAULT:'var(--r-md)',md:'var(--r-md)',
        lg:'var(--r-lg)',xl:'var(--r-xl)','2xl':'var(--r-2xl)',full:'var(--r-pill)',
      },
      borderWidth: { DEFAULT:'var(--bw)' },
      boxShadow: { 1:'var(--sh-1)', 2:'var(--sh-2)', 3:'var(--sh-3)' },
      transitionDuration: { DEFAULT:'var(--dur)' },
      transitionTimingFunction: { DEFAULT:'var(--ease)', spring:'var(--ease-spring)' },
      height: {
        ctrl:'var(--h-ctrl)','ctrl-lg':'var(--h-ctrl-lg)','ctrl-xl':'var(--h-ctrl-xl)',
        appbar:'var(--h-appbar)','bar-m':'var(--h-bar-m)',tab:'var(--h-tab)',
      },
      width: { side:'var(--w-side)','side-sm':'var(--w-side-sm)' },
    },
  },
};
```

`globals.css` sırası: **(1)** §2.1 çekirdek · **(2)** §2.2 türetilmiş · **(3)** §2.3 anlamsal (gündüz + gece) · **(4)** gövde sıfırlamaları · **(5)** `@keyframes` (§6).

```css
body{ @apply bg-canvas text-ink font-sans;
  font-size:var(--fs-md); letter-spacing:var(--track);
  font-variant-numeric:tabular-nums; -webkit-font-smoothing:antialiased }
a{ color:var(--brand-ui) } a:hover{ color:var(--accent-ui) }
::selection{ background:var(--accent-soft); color:var(--ink) }
:focus-visible{ outline:2px solid var(--brand-ui); outline-offset:2px }
::-webkit-scrollbar{ width:9px;height:9px }
::-webkit-scrollbar-thumb{ background:var(--line-2); border-radius:var(--r-pill);
  border:3px solid transparent; background-clip:content-box }
@media (prefers-reduced-motion:reduce){ *{ animation-duration:.01ms!important;
  transition-duration:.01ms!important } }
```

### 10.3 Tema sağlayıcı

```tsx
const VARSAYILAN: TemaAyari = { preset:'kurumsal-acik', mod:'acik', marka:0, vurgu:0, notr:0,
  font:0, r:10, sp:4, fs:14, fsd:1, track:0, bw:1, sha:7, dur:220 };

export function TemaSaglayici({ children }: { children: React.ReactNode }) {
  const [t, setT] = useState<TemaAyari>(() => {
    const kayit = localStorage.getItem('sv-tema');
    if (kayit) return { ...VARSAYILAN, ...JSON.parse(kayit) };
    const gece = matchMedia('(prefers-color-scheme: dark)').matches;
    return gece ? { ...VARSAYILAN, preset:'kurumsal-koyu', mod:'koyu', sha:42 } : VARSAYILAN;
  });

  useEffect(() => {
    const r = document.documentElement, m = MARKA[t.marka], v = VURGU[t.vurgu],
          n = NOTR[t.notr], f = FONT[t.font];
    r.setAttribute('data-mod', t.mod);
    r.setAttribute('data-tema', t.preset);
    Object.entries({
      '--brand':m.a, '--brand-dk':m.k, '--accent':v.a, '--accent-dk':v.k, '--neutral':n.v,
      '--r':`${t.r}px`, '--sp':`${t.sp}px`, '--fs':`${t.fs}px`, '--fs-d':`${t.fsd}`,
      '--track':`${t.track}em`, '--bw':`${t.bw}px`, '--sh-a':`${t.sha/100}`,
      '--dur':`${t.dur}ms`, '--font-d':`'${f.d}'`, '--font-t':`'${f.t}'`,
    }).forEach(([k, val]) => r.style.setProperty(k, val));
    localStorage.setItem('sv-tema', JSON.stringify(t));
  }, [t]);

  return <TemaCtx.Provider value={{ t, ayarla:(k,v)=>setT(s=>({ ...s, [k]:v, preset:'ozel' })),
    presetSec:(p)=>setT({ ...PRESET[p], preset:p }) }}>{children}</TemaCtx.Provider>;
}
```

Çok kiracılı kurulumda `TemaAyari` kiracı kaydından (`GET /api/kiracı/tema`) gelir; `localStorage` yalnızca kullanıcının kişisel mod tercihini (gündüz/gece) saklar.

### 10.4 Radix eşlemesi

| Arayüz | Primitif |
|---|---|
| Kullanıcı menüsü, Dışa Aktar | `DropdownMenu` |
| Yeni Talep / Yeni Etkinlik | `Dialog` |
| Mobil Menü sheet · Tema paneli | `Dialog` (alttan / sağdan, `data-state` animasyonu) |
| Notlar / Fotoğraflar | `Tabs` |
| Segmented, filtre çipleri | `ToggleGroup type="single"` |
| Tema kaydıraçları | `Slider` |
| Beni hatırla | `Checkbox` |
| Tür / makam seçimi | `Select` |
| Daraltılmış sidebar ipuçları | `Tooltip` |
| Tarih seçici | `Popover` + `react-day-picker` (tr) |
| Onay / hata bildirimi | `Toast` |

### 10.5 Lucide ikon eşlemesi

`layout-dashboard` Ana Sayfa · `inbox` Talepler · `calendar-clock` Ajanda · `calendar-days` Takvim · `layers` Bileşen Kütüphanesi · `chart-column` İstatistikler · `grid-2x2` Modüller · `settings` Ayarlar · `palette` Tema Tasarımcısı · `panel-left-close/open` sidebar · `menu` mobil menü · `search` arama · `bell` bildirim · `sun`/`moon` mod · `log-in`/`log-out` oturum · `user` kişi · `lock` şifre · `eye` göster · `plus` yeni/FAB · `sliders-horizontal` filtrele · `download` dışa aktar · `chevron-left/right` gezinme · `arrow-left/right` geri/ileri · `clock` saat · `map-pin` konum · `camera` fotoğraf · `pencil` düzenle · `trash-2` sil · `sticky-note` not · `file-text` metin · `check` onay · `circle-check` onaylandı · `circle-x` reddedildi · `hourglass` beklemede · `ban` iptal · `check-check` tamamlandı · `circle-alert` hata · `info` bilgi.

Varsayılan `strokeWidth={1.8}` (2.2 küçük boylarda); boyutlar 12 / 15 / 17 / 19 / 21 / 23px.

### 10.6 Dosya düzeni

```
src/
  main.tsx  App.tsx  routes.tsx
  tema/       TemaSaglayici.tsx  paletler.ts  presetler.ts  TemaPaneli.tsx
  kabuk/      AppShell.tsx  Sidebar.tsx  AppBar.tsx  MobilAppBar.tsx
              TabBar.tsx  MobilMenu.tsx  SayfaGecisi.tsx
  bilesenler/ Buton.tsx  Input.tsx  Kart.tsx  DurumCipi.tsx  TurEtiketi.tsx
              StatTile.tsx  DataTable.tsx  ListeSatiri.tsx  InsetGrup.tsx
              BosDurum.tsx  Iskelet.tsx  Segmented.tsx  FiltreCipleri.tsx
              ZamanCizgisi.tsx  BilgiIzgarasi.tsx  NotListesi.tsx  Fab.tsx
  ekranlar/   Giris.tsx  AnaSayfa.tsx  Talepler.tsx  TalepDetay.tsx
              Ajanda.tsx  Takvim.tsx  EtkinlikDetay.tsx  BilesenKutuphanesi.tsx
  veri/       api.ts  sorgular.ts  tipler.ts  bicimlendir.ts
  stiller/    globals.css
public/ amblem.png
```

---

## 11. Erişilebilirlik ve kalite eşiği

- Kontrast: `--ink`/`--surface` ≥ 12:1 · `--ink-2` ≥ 5.5:1 · `--ink-3` ≥ 4.5:1 (yalnızca meta) · `--on-brand`/`--brand-ui` ≥ 4.5:1 her modda · `--on-ok`/`--ok` ≥ 4.5:1.
- Küratörlü paletler bu eşikleri her mod için karşılar; serbest renk girişi eklenirse **kontrast doğrulaması zorunludur**.
- Her ikon butonunda `aria-label` **ve** `title`. Sayaç badge'i `aria-label="6 bekleyen talep"`.
- Tablo satırı klavyeden erişilebilir (`Enter` detayı açar). Sheet ve panel `Esc` ile kapanır, odak tuzağı Radix'ten gelir.
- Mobil dokunma hedefi ≥ 44px; FAB 56px; tabbar öğesi 56px.
- Yükleme: iskelet (§7.8). Hata: `circle-alert` + "Tekrar dene". Boş: §7.8 boş durum.
- `--dur: 0` veya `prefers-reduced-motion` → hareketsiz çalışır, hiçbir işlev kaybolmaz.

---

## 12. Eski arayüzden ayrılan noktalar

| Eski | Yeni | Gerekçe |
|---|---|---|
| 6 doygun renkli blok kart | Beyaz kart + 3px durum şeridi | Renk = anlam; sayı tipografiyle öne çıkar |
| Bootstrap mavisi `#0d6efd` | `--brand` (kurumsal lacivert) + altın vurgu | Kurumsal kimlik |
| Sabit renkler, tema yok | 3 katmanlı token motoru + Tema Tasarımcısı | Çok kiracılı satış |
| Üstte tek yatay menü | Sidebar (masaüstü) / tabbar (mobil) | Derinleşen modül sayısı |
| Ajandada tam renkli bloklar | Nötr kart + 4px tür çubuğu + "şu an" çizgisi | Okunabilirlik |
| Takvimde taşan etiketler | Sabit yükseklikli hücre + 2 satır kırpma + `+N daha` | Öngörülebilir ızgara |
| Mobilde küçültülmüş tablo | Native liste satırı + push navigasyon + FAB | Mobil öncelik |
| Rastgele buton yerleşimi | Sol bağlam/geri · sağ aksiyonlar, birincil en sağda | Öngörülebilirlik |
| Tema seçeneği yok | Gündüz/gece + 7 preset + sistem tercihi | Kullanım koşulları |

---

## 13. Yeniden üretim kontrol listesi

1. `globals.css`'i §10.2 sırasına göre kur; §2.1–2.3 bloklarını **birebir** yapıştır.
2. Fontları yükle (3 çiftin 6 ailesi) ve `tabular-nums`'ı gövdeye uygula.
3. §10.2 Tailwind temasını gir. **Denetim:** projede `#` ile başlayan hiçbir renk, `px` ile biten hiçbir boşluk/yarıçap değeri kalmayacak (`palet` ve `preset` dosyaları hariç).
4. `TemaSaglayici` + `TemaPaneli`'ni kur; 14 knob'un tamamı çalışsın, `localStorage`'a yazsın.
5. `AppShell`: masaüstü 256/76px sidebar + 64px appbar; mobil 52px appbar + 56px tabbar + güvenli alan.
6. Mobil push navigasyonu kur: detay ekranında tabbar gizli, geri butonu ve ortalanmış başlık, `itA/itB` ↔ `geA/geB` animasyonları.
7. Büyük başlık → appbar devri (34px kaydırma eşiği) çalışsın.
8. §7 bileşenlerini yaz ve **her birini Bileşen Kütüphanesi ekranına ekle**.
9. 8 ekranı §8'e göre kur; her ekranın boş / yükleniyor / hata durumu olsun.
10. 7 preset × 2 mod = 14 kombinasyonu Bileşen Kütüphanesi ekranında gözle denetle; `--r=0`, `--sp=3.2`, `--fs=17`, `--bw=2`, `--sh-a=0` uç değerlerinde kırılma olmadığını doğrula.
11. Amblem denetimi: deforme yok, dişi kullanım beyaz, koyu zeminde beyaz daire.
12. Kontrast, klavye, 44px dokunma hedefi ve `prefers-reduced-motion` testleri.
