# KentOS.Kalem — Tasarım Sistemi Şartnamesi

**Sürüm 3.0 — mobil tasarım dili.** Bu belge bağlayıcıdır: renk, ölçü,
yarıçap, gölge, tipografi ve bileşen anatomisi buradan gelir. Buradaki
değerlerin dışına çıkılmaz; yeni bir değer gerekiyorsa önce bu belgeye
eklenir, sonra koda girer.

Sürüm 3, kurumsal **mobil arayüz tasarım sisteminin** (Kurumsal Kimlik
Kılavuzu'ndan türetilmiş, telefon-öncelikli tasarım sayfaları) web
uygulamasına uyarlanmış hâlidir. Kaynak tasarım React Native içindi; bu
şartname aynı dili token motoru üzerinden web'e taşır ve **çok kiracılı**
kalır: hiçbir kurum adı, amblemi ya da rengi koda yazılmaz.

| Kaynak kavram | Web karşılığı |
|---|---|
| dp | px (1:1) |
| Gotham TR (kurumsal font) | **Plus Jakarta Sans** (lisans ikamesi; iki aile de geometrik sans, Türkçe seti tam) |
| Material Symbols Rounded | **lucide-react** (kontur ikon; yeni ikon kütüphanesi eklemek kullanıcı onayına bağlı) |
| PANTONE 294C lacivert / 871C altın | **varsayılan preset** — gerçek değerler kurum kaydından (`GET /api/v2/institution`) |
| `AsyncStorage` tema kaydı | `localStorage` (`sv-tema*` anahtarları) |

## 0. Değişmez ilkeler

1. **Kurum kimliği pazarlık konusu değil; ama koda da yazılmaz.** Marka
   rengi uygulamanın hâkim rengidir, vurgu (altın ailesi) yalnızca vurgudur:
   aktif göstergeler, "bugün" halkası, sekme çizgisi, saç teli başlıklar.
   Vurgu **geniş dolgu olarak asla** kullanılmaz. Amblem ikon, dolgu ya da
   arka plan deseni yapılmaz.
2. **Hiçbir bileşen renk değeri hard-code etmez.** Yalnızca token okur
   (`--brand-ui`, `--surface`, `--st-ok`…). Ham hex yalnızca
   `styles/tokens.css` çekirdeğinde ve palet listelerinde yaşar.
3. **Renk tek başına bilgi taşımaz.** Her durum rengi bir ikon, nokta ya da
   metin etiketiyle birlikte gelir (§7.3).
4. **Boşluk 4px'in katıdır** (`--sp` knob'u), yarıçap merdiveni §2.2'deki
   oranlardan gelir; keyfi ara değer yok.
5. **Tek karar, tek ekran.** Ekran birincil bir işe odaklanır; ikincil
   işlemler alt tabakaya (mobil) ya da diyaloğa (masaüstü) iner.
6. **Yıkıcı eylem çerçevelidir.** Dolu kırmızı buton tek bir yerde var:
   onay diyaloğunun son butonu. Tam kaydırmayla tetiklenen yıkıcı işlem ve
   onay istemeden veri silen akış yasak.
7. **Emoji arayüzde kullanılmaz.** Metni büyük harfe çevirmek gerekiyorsa
   `toLocaleUpperCase('tr-TR')` (JS) ya da `lang="tr"` altında CSS
   `uppercase` — `toUpperCase()` yasak (`iş` → `IS` hatası).

## 1. Kurumsal kimlik bağlantısı

Marka ve vurgu renkleri, amblem, kurum adı **veritabanındaki kurum
kaydından** gelir (`kurum_bilgileri`, tek satır; `GET /api/v2/institution`
anonim). `markaPaletiniUygula()` bu değerleri palet listelerinin 0.
sırasına yazar; "Kurumsal" preset'i o sırayı okur.

Şartnamedeki somut hex değerleri **fabrika varsayılanlarıdır** — kurum
kaydı boşken ve tasarım dosyalarında görülen preset budur:

| Rol | Gündüz | Gece | Not |
|---|---|---|---|
| Marka (`--brand`) | `#002E6D` | `#4E85C4` (`--brand-dk`) | gece karşılığı elle dengeli; tek hex'ten türetilmez |
| Vurgu (`--accent`) | `#A78952` | `#C9A96A` (`--accent-dk`) | beyaz zeminde metin olarak KULLANILMAZ (2.9:1) |
| Zemin tabanı (`--neutral`) | `#F4F8FC` | — | soğuk kâğıt; koyu ton reddedilir (`zeminOlabilirMi`) |

## 2. Token motoru

Üç katman, tek yön: **çekirdek → türetilmiş → anlamsal**. Bileşen çekirdeğe
dokunmaz (`--brand` değil `--brand-ui`). Kaynak: `styles/tokens.css`.
En altta bir de **v1 uyum katmanı** var: eski token adlarını (`--text`,
`--border`, `--st-ok`…) anlamsal katmana bağlar.

### 2.1 Katman 1 — Çekirdek token'lar (tek gerçek kaynak)

14 knob + 1 sabit; Tema Tasarımcısı yalnızca bunları yazar:

```css
--brand: #002E6D;      --brand-dk: #4E85C4;
--accent: #A78952;     --accent-dk: #C9A96A;
--neutral: #F4F8FC;
--sp: 4px;             /* boşluk birimi */
--r: 12px;             /* yarıçap tabanı = radius/md */
--fs: 15px;            /* yazı tabanı = body */
--fs-d: 1;             /* başlık çarpanı */
--track: 0em;          --bw: 1px;
--sh-a: 0.10;          /* gölge alfası = elev/1 */
--dur: 240ms;          /* hareket süresi = sayfa geçişi */
--font-d: 'Plus Jakarta Sans';  --font-t: 'Plus Jakarta Sans';
--font-m: 'JetBrains Mono';     /* SABİT — knob değil (§4) */
```

### 2.2 Katman 2 — Türetilmiş ölçekler

**Yarıçap** (şartname merdiveni; varsayılan knob ile birebir):

| Token | Oran | 12px tabanda | Kullanım |
|---|---|---|---|
| `--r-xs` | 0.5 | 6 | mini rozet, onay kutusu |
| `--r-sm` | 0.667 | 8 | durum çipi, küçük denetim |
| `--r-md` | 1 | 12 | girdi, buton, çip kutusu |
| `--r-lg` | 1.333 | 16 | kart (`rounded-card`) |
| `--r-xl` | 1.667 | 20 | diyalog (`rounded-win`) |
| `--r-2xl` | 2 | 24 | alt tabaka üst köşesi (`--radius-tabaka`, 28px'te kırpılır) |

**Tipografi** (15 tabanına oranlı; satır yüksekliği ve harf aralığı
Tailwind `--text-*--line-height/--letter-spacing` eşleriyle utility'nin
kendisine gömülü):

| Utility | Boyut/satır | Ağırlık | Aralık | Şartname adı · kullanım |
|---|---|---|---|---|
| `text-3xs` | 10/14 | 600 | — | rozet sayısı |
| `text-2xs` | 11/15 | 600 | +0.04em | `caption` — durum etiketi, sayaç, meta |
| `text-xs` | 12/16 | 600 | +0.01em | `label` — form etiketi, çip, sekme yazısı |
| `text-sm` | 13/19 | 500 | 0 | `bodySm` — ikincil satır, tablo hücresi |
| `text-base` | 15/22 | 500 | 0 | `body` — birincil satır, form değeri |
| `text-lg` | 16/22 | 700 | 0 | `title3` — kart/bölüm başlığı |
| `text-xl` | 19/25 | 700 | −0.01em | `title2` — appbar, tabaka başlığı |
| `text-2xl` | 24/30 | 800 | −0.015em | `title1` — sayfa başlığı |
| `text-3xl` | 30/34 | 800 | −0.02em | `display` — karşılama, büyük metrik |

Gövde varsayılan ağırlığı **500** (`body{font-weight:500}`) — hiyerarşi
aileden değil ağırlıktan gelir: 800 başlık · 700 alt başlık · 600 etiket ·
500 gövde. 400'ün altı ağırlık kullanılmaz.

**Bileşen ölçüleri** (`--sp` katları):

| Token | Değer | Şartname karşılığı |
|---|---|---|
| `--h-ctrl` | 40 | buton `sm` — masaüstü yoğunluğu |
| `--h-ctrl-lg` | 48 | buton `md` / `touch.min` — mobil denetim |
| `--h-ctrl-xl` | 56 | buton `lg` / `touch.field` / FAB |
| `--h-field` | 50 | form girdisi (§7.2) |
| `--h-bar-m` | 56 | mobil appbar (`layout.appBar`) |
| `--h-appbar` | 64 | masaüstü appbar (web eki) |
| `--h-row` | 52 | masaüstü tablo satırı (web eki) |
| `--h-row-m` | 64 | mobil liste satırı (`layout.rowMin`) |
| `--h-tabbar` | 64 | alt gezinme çubuğu (`layout.tabBar`) |

Dokunma hedefi mobilde en az **48×48** (görsel daha küçükse `after:` ile
telafi edilir — ikon butonu 40 görsel + 48 hedef).

**Gölge** — üç kademe, markayla boyalı; alfalar tek knob'dan
(`elev/1`=α, `elev/2`=1.2α, `elev/3`=2α → varsayılanla .10/.12/.20):

| Token | Geometri | Kullanım |
|---|---|---|
| `--sh-1` | 0 1 3 | kart, çip |
| `--sh-2` | 0 4 12 (+0 1 2) | kaydırılmış appbar, açılır menü |
| `--sh-3` | 0 12 32 (+0 2 6) | alt tabaka, diyalog, FAB |

**Koyu temada gölge yoktur** (`--sh-scale: 0`); ayrım kenarlıkla kurulur.
Knob satır içi yazıldığı için kapatma alfadan değil bu çarpandan gelir.

### 2.3 Katman 3 — Anlamsal renkler

Nötrler markayla hafifçe boyanır (`color-mix`): hangi marka seçilirse
seçilsin arayüz onunla akraba olur. Varsayılan knob'larla hedeflenen
değerler (kaynak tasarımın tema tablosu):

| Token | Gündüz (~) | Gece (~) |
|---|---|---|
| `--bg` | `#F4F8FC` | `#0B1524` |
| `--surface` | `#FFFFFF` | `#101F33` |
| `--surface-2` / `--sunken` | `#EDF1F6` ailesi | `#16283F` ailesi |
| `--line` | `#D6DBE2` | `#23364F` |
| `--ink` | `#14181F` | `#E8EDF4` |
| `--ink-2` | `#4D4D4F` ailesi | `#9AAABF` |
| `--brand-ui` | `--brand` | `--brand-dk` |
| `--perde` | `rgba(8,14,26,.45)` | `rgba(2,8,16,.66)` |

**Durum renkleri** (gündüz metin değerleri şartname ile birebir; dolgular
`--st-mix` %13 karışımdan):

| Token | Gündüz fg | Dolgu (~) | Anlam |
|---|---|---|---|
| `--ok` | `#1F7A4D` | `#E4F1EA` | onaylandı, tamamlandı |
| `--warn` | `#9C6B12` | `#FBF0DC` | beklemede, süresi yaklaşan |
| `--danger` | `#A32E24` | `#F9E8E6` | reddedildi, gecikmiş, hata |
| `--info` | `#1B5AA8` | `#E8F0F9` | işlemde, bilgi |
| `--mute` / `--slate` | gri / arduvaz | — | iptal / kapandı (web eki) |

Bildirim şeridi renkleri ayrı: `--toast-bg` (marka mürekkebiyle koyu,
**iki temada da**), `--toast-ink`, `--toast-line` (§7.8).

### 2.4 Token sözlüğü — hangi token nerede

- Buton zemininde `--brand` DEĞİL `--brand-ui` kullanılır (gecede
  `--brand-dk`ya döner). `--brand` yalnızca giriş paneli ve kenar çubuğu
  gibi "her modda kurumsal lacivert" yüzeylerde.
- Metin: `--ink` birincil, `--ink-2` ikincil, `--ink-3` meta. Vurgu metni
  `--accent-ink` (beyaz zeminde okunur karışım) — ham `--accent` metin
  olarak kullanılmaz.
- Odak halkası `--focus-ring` (%12 marka, 3px) + kenarlık markaya döner.
- "Şu an" çizgisi `--simdi` (tehlikeden türetilir) — vurgu rengi DEĞİL:
  altın "bugün"ün işareti, ikisi karışıyordu.

## 3. Tema Tasarımcısı (sözleşme)

Panel yalnızca çekirdek knob'ları yazar (satır içi, `documentElement`);
anlamsal katman ve bileşenler hiçbir şey bilmez. Çözülmüş değerler
`sv-tema-cekirdek` altına da yazılır — `index.html` ilk kareden önce geri
oynatır (tema zıplaması olmasın).

### 3.1 Kontroller

| Knob | Aralık | Not |
|---|---|---|
| `r` | 0–20 | yarıçap tabanı |
| `sp` | 3.2–5.2 | yoğunluk |
| `fs` | 12–17 | yazı tabanı |
| `fsd` | 0.9–1.3 | başlık çarpanı |
| `track` | −0.02–0.04 | harf aralığı |
| `bw` | — | kenarlık kalınlığı |
| `sha` | 0–55 | gölge (gecede etkisiz — §2.2) |
| `dur` | 0–400 | hareket süresi |

### 3.2 Hazır temalar

`kurumsal-acik` / `kurumsal-koyu` fabrika ayarıdır ve **şartname
değerleriyle** gelir: `r 12 · sp 4 · fs 15 · sha 10 · dur 240 · font 0`.
Diğerleri (Zümrüt, Bordo, Petrol, Antrasit, Yüksek Kontrast) kasıtlı
karakter varyasyonlarıdır; kontrast preset'i gölgesiz + 2px kenarlıklı
çalışır.

### 3.3 Yazı tipi çiftleri

| Ad | Başlık | Gövde |
|---|---|---|
| **Kurumsal** (fabrika) | Plus Jakarta Sans | Plus Jakarta Sans |
| Modern | Figtree | Source Sans 3 |
| Editoryal | Archivo | Karla |

Kurumsal çift tek ailedir — kaynak tasarım hiyerarşiyi aileden değil
ağırlıktan kurar. Sayısal aile (`--font-m`, JetBrains Mono) knob değildir.

## 4. Tipografi kuralları

1. **Sayısal veri** (tutar, saat, kod, sicil, metrik) `font-mono`
   (JetBrains Mono 500) + `tabular-nums` ile dizilir ve tutar sütunları
   sağa hizalanır. Gövde geneli zaten `tabular-nums`.
2. Tarih `GG.AA.YYYY`, saat `SS:dd`, hafta **pazartesi** başlar. Para
   `₺ 1.234.567,89`; yüzde `%41,8`.
3. Form etiketi `label` kademesi (12/600), **cümle düzeni** — büyük harfe
   çevrilmez, yer tutucuya gömülmez (§7.2).
4. Başlıklar `numberOfLines` karşılığı olarak `truncate` ya da
   `line-clamp-2`; uzun başlıkta `metin-guzel` (`text-wrap: pretty`).
5. Buton metni büyük harfe çevrilmez.

## 5. Yerleşim

### 5.1 Masaüstü (referans 1440×912)

Kenar çubuğu (256px / daraltılmış 76px, marka zemin) + 64px appbar +
içerik. Aktif menü öğesi: 3×20px altın çubuk + açık zemin. Kenar çubuğu
altında kullanıcı bloğu + çıkış.

### 5.2 Mobil (referans 390×844) — native gramer

- **Appbar 56px**: amblem + başlık (`title3` 16/700 — şerit 6 eylem
  taşıdığı için `title2` yerine bir kademe altta; masaüstünde 19) +
  ikon eylemleri (40 görsel / 48 hedef).
- **Alt çubuk 64px** + güvenli alan: 4 sekme + Menü. Aktif sekme üç
  işaret birden taşır: üstte kayan 2.5px **altın gösterge**, `surfaceAlt`
  zemin, marka renkli ikon+etiket (11/600). Rozet 99 üstünde `99+`.
- **FAB 56px**, alt çubuğun 16px üstünde, `--r-xl`, `--sh-3`; yığın
  açılınca perde + ikon çarpıya döner ve vurgu rengine geçer. Bir ekranda
  en fazla bir FAB + bir mini yardımcı.
- **Liste satırı en az 64px** (`rowMin`), tek yüzeyde saç teli ayırıcılar
  (`InsetGroup`), solda 30px durum ikonu kutusu, iki satır başlık, sağda
  chevron.
- **Alt tabaka**: üst köşeler 24px, tutamak, başlık `title2`, gövde kayar,
  eylem çubuğu sabit. Snap görevleri: menü `auto` · süzgeç/seçim `~60%` ·
  form `92dvh`. Tabaka içinde tabaka açılmaz.

### 5.3 Kırılım ve eşleme

`md = 768px`. Tek bileşen iki görünüm; fark CSS değişkenlerinden gelir.
Mobil/masaüstü ağaçları `useIsDesktop()` ile ayrılır (`md:hidden` ile
ikisi birden çizilmez). Tablolar mobilde kart/satır dizisine döner.

## 6. Hareket

| Hareket | Süre / eğri |
|---|---|
| Basış geri bildirimi (`bas-yay`) | 90ms, `scale(.97)`, ease-out; bırakış yayla |
| Tabaka açılış / kapanış | 280ms `cubic-bezier(.22,1,.36,1)` / 200ms `cubic-bezier(.4,0,1,1)` |
| Sayfa/sekme geçişi | `--dur` (240ms), `--ease` |
| Açılır menü | 170/130ms |
| İskelet nabzı | 1200ms döngü |

Yalnızca `transform` ve `opacity` canlandırılır. `prefers-reduced-motion`
altında tüm geçişler kapanır (katman yine ANINDA görünür). Animasyonlu
katmanlar bileşik katmana alınır (`.katman`, `.anim-*` — globals.css).

## 7. Bileşen kataloğu

### 7.1 Buton

| Varyant | Zemin | Metin | Kenarlık |
|---|---|---|---|
| `birincil` | `--brand-ui` | `--on-brand` | — |
| `ikincil` | `--surface` | marka | 1.5px marka |
| `ucuncul` | `--brand-soft` | marka | — |
| `onay` | `--st-ok` | `--on-ok` | — (web eki) |
| `yikici` | `--surface` | `--st-no` | 1.5px `--st-no` |
| `sade` | şeffaf | marka | — |

Boylar: `normal` 40 (masaüstü) · `mobil` 48; büyük birincil 56
(`--h-ctrl-xl`). Yarıçap `--r-md`. Metin 700. Basış `bas-yay`.
Bir ekranda tek `birincil`. Dolu kırmızı yalnızca onay diyaloğunun son
butonu (§7.8). İkon butonu: 40 görsel / 48 hedef, `etiket` zorunlu.

### 7.2 Form alanı

- Etiket HER ZAMAN görünür (12/600, cümle düzeni); odakta markaya döner.
  Yer tutucu etiketin yerine geçmez.
- Girdi: mobil 50px / masaüstü 40px, `--r-md`, 1px `--line`, zemin
  `--surface-2` (yüzeyden bir ton çukur). Odak: kenarlık marka + 3px
  `--focus-ring`. Hata: kenarlık `--danger` + altta hata metni.
- **Yardım/hata satırı sabit yer tutar** (`min-h-4`): hata belirince form
  zıplamaz.
- Doğrulama `blur`da; maske ve sayaç canlı. Mobilde `autoFocus` yok
  sayılır (klavye tabaka animasyonunu bozuyor).
- Yerel `<select>`/tarih bileşeni kullanılmaz: `SelectMenu`,
  `SearchSelect`, `DatePicker` (hafta pazartesi, gün hücresi 44px,
  hızlı aralık çipleri), `TimeRangePicker` (süre koruyarak kayar).

### 7.3 Durum çipi

`--r-sm` köşe, `caption` (11/600/+0.04em), `fg`+`bg` ikilisi, solda 5px
nokta — renk tek başına anlam taşımaz. Altı durum sabit: beklemede ·
onaylandı · devam · reddedildi · iptal · tamamlandı (`DURUMLAR`).
Süzgeç çipi ondan ayrıdır: 36px, tam yuvarlak, seçili = marka dolgusu +
`×` (§7.8).

### 7.4 Durum kartı (StatTile)

Etiket (12) + sayı (`title1` 24, **`font-mono` 500**, `tabular-nums`) +
isteğe bağlı ikon kutusu ve alt metin. Sayı rengi yalnızca token.

### 7.5 Veri tablosu (masaüstü)

52px satır, `bodySm` hücre, sayılar sağa hizalı ve `tabular-nums`.
Mobilde tablo çizilmez — ekranın kendi satırı ya da `Liste`nin sıkı
görünümü (satır grameri `frontend/CLAUDE.md`).

### 7.6 Ajanda etkinlik kartı

Renk şeridi + saat bloğu + başlık + hazırlık rozetleri. Mobilde kart
görünümü yalnızca ajandaya özgüdür (`mobilGorunum="kart"`).

### 7.7 Takvim hücresi

"Bugün" halkası vurgu renginde; "şu an" çizgisi `--simdi`. Gün/hafta tek
bileşen; sütun en az 104px, boş yarım saat hücresinde `+` belirir.

### 7.8 Diğer bileşenler

- **Alt tabaka / diyalog kabı** (`OverlayShell`): §5.2; masaüstünde
  ortalanmış pencere `--r-xl`, `--sh-3`. Dışarı tıklayınca form kapanmaz.
- **Onay diyaloğu** (`ConfirmDialog`): 44px durum ikonu kutusu + `title3`
  başlık + sonucu sayıyla anlatan açıklama + EŞİT genişlikte iki buton;
  yıkıcı onay dolu `--st-no` (tek istisna). Bilgilendirme için diyalog
  açılmaz.
- **Bildirim şeridi** (`Toast`): `--toast-bg` koyu zemin (iki temada da),
  `--r-lg`, 13/19 metin, 22px durum ikonu; süre 3s, eylemliyse 5s ve tüm
  şerit tek düğme. Kaydırarak kapatma gezinme sayılmaz (10px eşiği).
- **Anahtar** (`Switch`): 46×28, tüm satır tıklanabilir; form listesi
  işaretlemesi onay kutusunda kalır.
- **Akordiyon**: başlık satırı en az 50px, sağda sayaç + ok; eylem düğmesi
  tetikleyicinin dışında.
- **Zaman çizelgesi** (`Timeline`): 25px durum dairesi + 2px çizgi; çizgi
  son öğede kesilir.
- **İskelet**: spinner değil iskelet; 1.2s nabız. 400ms altı yüklemede
  gösterge çıkmaz (liste `keepPreviousData` ile boşalmaz).
- **Boş durum** (`EmptyState`): 52px ikon kutusu (mobil 44) + `title3`
  başlık + ne yapılacağını söyleyen açıklama + eylem. "Kayıt bulunamadı"
  tek başına yasak; süzgeçliyken metin farklı ("Süzgeçleri temizle").
- **Rozet**: `--st-no` zemin + beyaz sayı; appbar'da zemin renginde 1.5px
  kenarlık. Baş harf çipi: `--brand-soft` zemin + marka metin.
- **Süzgeç grameri**: arama üstte kalır; süzgeçler alt tabakada; açık
  süzgeç listenin üstünde çip olarak görünür; süzgeç değişince sayfa 1'e
  döner (ayrıntı `frontend/CLAUDE.md`).

## 8. Ekranlar

Ekran kataloğu ve rota tablosu `frontend/CLAUDE.md` + `App.tsx`'te yaşar;
bu şartname yalnızca **gramerleri** bağlar: kabuk → ekran → FAB → tabaka
(§5), arama-süzme grameri (§7.8), satır grameri (§7.5). Yeni ekran bu
gramerin dışına çıkacaksa önce bu belgeye gerekçesi yazılır.

## 9. Veri modeli

Sunucu sözleşmesi `data/types.generated.ts` (üretilmiş) + `data/types.ts`
(takma adlar). `PagedResult<T>` alan adları Türkçe kalır (sözleşme
sunucunun). Zaman `timestamp without time zone`; dönüşüm yalnızca
`data/time.ts` (`toISOString()` yasak).

## 10. Uygulama (React)

### 10.1 Paketler

React 18 + Vite + Tailwind 4 (CSS-first) · Radix UI (headless) ·
lucide-react · TanStack Query/Table/Virtual · date-fns (`tr`) · dnd-kit ·
vaul (alt tabaka) · firebase (push) · maplibre-gl · echarts.
**Yeni UI kütüphanesi eklemek kullanıcı onayına bağlıdır.**

Fontlar: Plus Jakarta Sans (400–800) + JetBrains Mono (400, 500) —
`index.html`'de Google Fonts; kapalı ağda @fontsource'a alınır.

### 10.2 Tailwind eşlemesi

Yapılandırma dosyası YOK; eşleme `styles/globals.css` → `@theme inline`.
Her `--color-*`/`--text-*`/`--radius-*` eşlemesinin kaynağı
`tokens.css`'te TANIMLI olmalı — bekçi: `test/tokens.test.ts`.
`bg-(--x)` ham değişken okur; Tailwind rengi istiyorsan yalın sınıf
(`ring-brand-2`).

### 10.3 Tema sağlayıcı

`ThemeProvider` knob'ları satır içi yazar; mod `data-tema` + `data-mod`.
`color-scheme` hem CSS'te hem satır içi (ilk boyama). Sistem çubuğu
`theme-color` hesaplanmış tokendan güncellenir.

### 10.4 Radix eşlemesi

Dialog/AlertDialog/DropdownMenu/Popover/Select/Switch/Tabs/Toast/
ToggleGroup/Tooltip/Accordion — hepsi tokenlı kabuklarla sarılıdır
(`components/`). Ham Radix ekrana çıkmaz.

### 10.5 Lucide ikon eşlemesi

24 ızgara, 1.8 kontur (aktif/vurgulu 2.1–2.2). Yalnızca ikonlu düğmede
`aria-label` + `title` zorunlu.

### 10.6 Dosya düzeni

`src/components · screens · shell · calendar · theme · notifications ·
help · styles · data · auth · institution · pwa` — adlar İngilizce,
kullanıcı yüzeyi Türkçe (`frontend/CLAUDE.md` dil sözleşmesi).

## 11. Erişilebilirlik ve kalite eşiği

1. Gövde metni kontrastı ≥ 4.5:1; büyük metin ve grafik öğe ≥ 3:1.
   Vurgu (altın) beyaz zeminde metin olamaz; yalnızca ≥3px grafik öğe.
2. Dokunma hedefi ≥ 48×48 (mobil); masaüstü fare denetimleri ≥ 40.
3. `:focus-visible` halkası her etkileşimli öğede; odak sırası görsel
   sıraya eşit; tabaka açılınca odak tabakaya taşınır, kapanınca geri.
4. Yazı %200 ölçeklenmede taşmaz; sabit yükseklikli metin kabı yasak
   (`--fs` knob'u zaten tüm merdiveni büyütür).
5. Durum yalnızca renkle anlatılmaz (§0.3). Canlı bölgeler: bildirim,
   seçim sayacı, eşitleme durumu.
6. `prefers-reduced-motion` uygulanır (§6).
7. Görsel tur (`test/gorsel/tur.mjs`) her ekranı iki boyut × iki temada
   gezer; yatay taşma ve JS hatası çıkış kodu 1.

## 12. Eski arayüzden ayrılan noktalar

Sürüm 2 → 3 geçişinde bilinçli değişenler (ölçümleriyle):

| Ne | v2 | v3 |
|---|---|---|
| Font | Montserrat + IBM Plex Sans | Plus Jakarta Sans (tek aile) + JetBrains Mono (sayısal) |
| Yazı tabanı | 14 (+mobil medya düzeltmesi) | **15**, tek merdiven (11/12/13/15/16/19/24/30) |
| Gövde ağırlığı | 400 | **500** |
| Yarıçap | 10 taban (6.2/14/20/32) | **12** taban (8/12/16/20/24) |
| Girdi boyu | 44 mobil / 40 masaüstü | **50 / 40** |
| Buton | 36 / 48 | **40 / 48**, ikincil+yıkıcı 1.5px çerçeveli |
| Durum çipi | tam yuvarlak hap | `--r-sm` köşe + nokta |
| Gölge | nötr gri, gecede 0.42 alfa | marka mürekkebi .10/.12/.20; **gecede yok** |
| Toast | yüzey rengi kart | koyu şerit (`--toast-bg`), 3s/5s |
| Basış | `scale(.94)` tam süre | `scale(.97)` 90ms |
| Zemin | sıcak kâğıt `#F5F4F0` | soğuk kâğıt `#F4F8FC` |
| Koyu tema | `#080c13` tabanlı, gölgeli | `#0B1524` ailesi, kenarlıkla ayrım |

## 13. Yeniden üretim kontrol listesi

1. `npx tsc --noEmit` · `npm test` · `npm run build` ·
   `node test/gorsel/tur.mjs` (sunucu ayakta).
2. Token eklerken: `tokens.css` tanımı → `globals.css` eşlemesi →
   `test/tokens.test.ts` yeşil.
3. Yeni bileşen: ham renk yok, `bas-yay` basışı, 48 dokunma hedefi,
   `aria-label`, iki temada ekran görüntüsü.
4. Yeni ekran: menü + rota + yardım metni + izin kapısı birlikte
   (`frontend/CLAUDE.md` sırası).
