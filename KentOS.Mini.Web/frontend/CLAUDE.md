# KentOS.Mini — Web Uygulaması

Ajanda/talep sisteminin tek sayfa arayüzü. **Eski MVC arayüzü aynen çalışmaya
devam eder**; bu uygulama artık **kökten** (`/`) yayınlanır ve onun yerini
kademeli olarak alır.

> Uygulama bir dönem `/yeni` altındaydı. Taşındı: `vite.config.ts` → `base: '/'`,
> router `basename="/"`, çıktı doğrudan `../wwwroot`. Eski derin bağlantılar ve
> **kurulu PWA'lar** bir süre daha `/yeni/...` isteyebiliyor; sunucu onları
> 302 ile köke yönlendiriyor (`Program.cs`). Kalıcı 301 kullanılmadı —
> tarayıcıda süresiz önbelleğe alınıyor ve geri dönmek gerekirse
> kullanıcıların tarayıcısını temizletmek gerekirdi.

Sunucu: `../` — bkz. `../CLAUDE.md`.

## HER İYİLEŞTİRMEDEN SONRA DÖKÜMAN GÜNCELLENİR

Bu, isteğe bağlı bir alışkanlık değil, işin **bitmiş sayılma şartı**. Bir
değişiklik üç yerde iz bırakır ve üçü de aynı commit'te güncellenir:

| Nerede | Ne yazılır |
|---|---|
| **Kod içi yorum** | O satırın NEDEN böyle olduğu; hangi hata yüzünden |
| **`CLAUDE.md`** (bu dosya / kök) | Mimari karar, kural, tuzak, ölçüm sonucu |
| **`src/yardim/metinler/*.md`** | Kullanıcının gördüğü davranış değiştiyse |

Kural şu: **davranış değiştiyse yardım metni de değişir.** Süzgeçler araç
çubuğundan alt tabakaya taşındığında yardım metinleri aylarca "sağ üstteki
düğmeye basın" demeye devam etti — yani yardım, kullanıcıyı olmayan bir
düğmeye yönlendirdi. Yanlış döküman, dökümansızlıktan kötüdür.

Karar yazarken **ölçümü de yaz**: "390px'te 1913 → 1849px", "40 → 25 bağlantı",
"628px yatay taşma". Sayı olmadan bir sonraki kişi aynı yolu yeniden ölçmek
zorunda kalıyor.

Yeni bir ekran ya da bileşen eklerken sırasıyla:

1. Bileşeni yaz, **neden** böyle olduğunu yorumla.
2. `src/yardim/metinler/` altına metnini ekle, `yardim/katalog.ts`'e kaydet
   (grup alanı zorunlu). `test/yardim.test.tsx` menüdeki her ekranın yardımı
   olmasını kilitliyor.
3. Rotayı `App.tsx`'e, menü öğesini `kabuk/gezinme.ts`'e ekle — **ikisi
   birlikte**, aksi hâlde kullanıcı menüde görüp tıkladığı yerde duvara
   çarpar.
4. `CLAUDE.md`'ye kararı ve ölçümü yaz.
5. `npx tsc --noEmit` · `npm test` · `npm run build` · `node test/gorsel/tur.mjs`.

## Yığın

React 18 + TypeScript + Vite · TanStack Query/Table/Virtual · **Radix UI** (headless) · **lucide-react** · Tailwind CSS · date-fns (`tr` yerelleştirmesi) · dnd-kit (takvim) · firebase (web push).

Kütüphane seçimi `design/design.md` §10.1'den gelir. **Yeni bir UI
kütüphanesi eklemek kullanıcının onayına bağlıdır** — kural "asla ekleme"
değil, "kendi başına ekleme".

Onayla eklenen: **`vaul`** (alt sheet). Radix Dialog + elle yazılmış CSS
animasyonu + kendi sürükleme kancamız aynı `transform`u üç ayrı yerden
yazıyordu; parmak tabakayı indiriyor, CSS kapanışı `transform: none`dan
başladığı için tabaka yukarı zıplayıp bir daha iniyordu. `vaul` yalnızca bu
işi yapıyor ve Radix Dialog'un üstüne kurulu, yani `Esc`/odak tuzağı/
erişilebilirlik sözleşmesi aynen duruyor.

> Alt sheet **mobile özgüdür**: `FormModal` 768px altında `vaul`, üstünde
> ortalanmış Radix penceresi çiziyor (`useMasaustuMu`). Masaüstünde
> sürükleyerek kapatma beklenen bir hareket değil.

## Tasarım sistemi — bağlayıcı

Tek kaynak: **`design/design.md`** (depo kökünde, sürüm 2.0 — token tabanlı,
çok kiracılı). Renk, tipografi, ölçü, bileşen sınıfları ve ekran düzenleri
orada birebir yazılı. Uydurma yok.

> Bu dosya bir dönem depo DIŞINDA, mutlak bir yolla işaret ediliyordu
> (`/Users/cihad/Projects/workcollab/design/design.md`) — ve o yoldaki dosya
> **eski v1 sürümüydü**, içinde kurum adı geçiyordu. Yeni depoyu klonlayan
> hiç kimse tasarım sistemine ulaşamıyordu. Şartname artık depoda ve beyaz
> etiket sözleşmesine uygun: renkler kurum kaydından gelir, şartname yalnızca
> varsayılan preset'i tarif eder.

**En önemli kural: hiçbir bileşen renk değeri hard-code etmez.** Yalnızca token kullanılır.

- Tokenlar `src/stiller/tokenlar.css` içinde `:root` ve `:root[data-tema="koyu"]` bloklarında; ikisi de `design.md` §2'den birebir kopyalanır.
- Tailwind **4**, CSS-first: yapılandırma dosyası YOK. Token'lar
  `src/stiller/globals.css` içindeki `@theme inline` bloğunda utility'lere
  eşlenir (`--color-perde: var(--perde)` gibi).
  > **`@theme` eşlemesi token'ın VARLIĞINI denetlemez.** `--color-perde`
  > eşlemesi aylarca duruyordu ama `--perde` hiç tanımlanmamıştı: `bg-perde`
  > çözümsüz kalıp **tamamen saydam** boyanıyordu, yani uygulamadaki her
  > diyalog ve alt tabakanın perdesi yoktu. Arkadaki ekran tam parlaklıkta
  > durduğu için katmanlar "üstte" okunmuyordu. Yeni bir `--color-*` eşlemesi
  > yazarken kaynağının `tokenlar.css`'te GERÇEKTEN tanımlı olduğunu doğrula.
  > Bekçi: `test/tokenlar.test.ts` — hem `@theme` eşlemelerini, hem
  > bileşenlerdeki `bg-(--x)` / `var(--x)` okumalarını tanımlarla karşılaştırır
  > ve kendine referans veren token'ı yakalar.
- **`bg-(--x)` HAM değişkeni okur**, Tailwind rengini değil: `--color-brand-2`
  eşlemesi varken `ring-(--brand-2)` yine de çözümsüzdü ve üç odak halkası ile
  okunmamış bildirim noktası görünmüyordu. Tailwind rengi istiyorsan sınıf adı
  yalın olmalı (`ring-brand-2`); ham değişken istiyorsan o değişken
  `tokenlar.css`'te tanımlı olmalı.
- Kurumsal renkler: lacivert `#002E6D`, altın `#A78952`, %85 gri `#4D4D4F`.
- **Altın yalnızca vurgudur**: aktif göstergeler, "bugün" halkası, sekme altı çizgisi, kart üst şeridi. Geniş dolgu olarak asla.
- Koyu temada `--brand` `#1E5FBF`'e açılır (lacivert koyu zeminde buton olarak okunmuyor). Gerçek kurumsal lacivert koyu temada yalnızca `--login-panel` ve kenar çubuğunda yaşar.
- Yazı tipleri: **Montserrat** (500/600/700) başlık/marka/metrik/saat · **IBM Plex Sans** (400/500/600) gövde/tablo/form. `font-variant-numeric: tabular-nums`.

> `design/*.dc.html` dosyaları Claude tasarım tuvali önizlemeleridir. Inline stil kullanırlar, içlerinde Tailwind/Radix **yoktur**. Görsel referanstır — kaynak kod olarak kopyalanmaz.

## Mobil öncelikli

`md = 768px`. Altında **tabbar** (60px + safe area, 5 sekme), üstünde **kenar çubuğu** (258px / daraltılmış 76px).
Tek bileşen, iki görünüm; fark yalnızca CSS değişkenlerinden gelir (`design.md` §5.3'teki eşleme tablosu).
Tablolar 768px altında kart listesine dönüşür. **Dokunma hedefi asla 44px'in altına inmez.**

## ETKİLEŞİM MİMARİSİ — dört katman

Bu bölüm uygulamanın **tamamında geçerli** olan yapıyı bir kerede anlatıyor.
Aşağıdaki bölümler tek tek ekranların hikâyesi; burası sözleşme. Yeni bir ekran
yazarken bu dört katmanın dışına çıkma — çıkıyorsan önce buraya bir satır yaz.

```
┌─ 1. KABUK ───────────────────────────────────────────────┐
│  masaüstü: kenar çubuğu + appbar                          │
│  mobil:    appbar + alt sekme çubuğu                      │
│                                                           │
│  ┌─ 2. EKRAN ──────────────────────────────────────────┐  │
│  │  araç çubuğu (arama · segment · çıktı)              │  │
│  │  liste / ızgara / detay                              │  │
│  │  masaüstü: <Liste> tablo · mobil: kendi satırı       │  │
│  └──────────────────────────────────────────────────────┘ │
│                                                           │
│  3. FAB (yalnızca mobil, sağ alt)  → 4. TABAKA            │
└───────────────────────────────────────────────────────────┘
```

### 1. Kabuk — `kabuk/AppShell.tsx`

Tek bileşen, iki görünüm. Appbar'daki eylem düğmeleri **hepsi `IkonButon`**
(38×38, kenarlıklı, `rounded-control`): kurulum · yardım · bildirim · tema
tasarımcısı · gece-gündüz · çıkış. Kendi ölçüsünü uyduran bir düğme şeridi
hizasız gösteriyor — yardım düğmesi bir dönem 34px ve kenarlıksızdı.

Sıra da kural: **geçici düğmeler grubun BAŞINDA**. Kurulum simgesi kurulunca
kaybolacak; sona konsaydı kaybolduğunda çıkış/tema/bildirim bir düğme boyu
kayar ve kas hafızası bozulurdu.

Başlık menüden okunur (`gezinme.ts` → `sayfaAdi`). Menüde karşılığı olmayan
ekranlar `EK_BASLIKLAR` tablosuna yazılır; yoksa "Randevu Takip Sistemi"
kalıyor ve kullanıcı nerede olduğunu göremiyor.

### 2. Ekran — mobil ve masaüstü ağaçları AYRI

`useMasaustuMu()` (768px) **bileşen ağacını** böler, `md:hidden` ile ikisini
birden çizmez. İkisi de DOM'daysa aynı `aria-label`lı iki arama alanı oluşur,
ekran okuyucu ikisini de duyurur ve testler "birden çok eşleşme" der.

> **Boş durumu iki dala da ver.** Bir kez unutuldu; mobilde boş liste bomboş
> bir sayfa gösterdi. Boş durum ayrıca **süzgeçliyken farklı** olmalı:
> "Eşleşen kayıt yok + Süzgeçleri temizle", "Kayıt yok + Ekle" değil.

Araç çubuğu **karta konmaz**; denetimler doğrudan sayfa zemininde durur.
Hatalar ekranında kart içine alınmıştı: 390px'te 370px yükseklik, yarısı boş,
liste ilk ekrandan tamamen çıkıyordu. Kartsız hâli 88px.

### 3. FAB — `kabuk/mobil/Fab.tsx`

Mobilde ekranın işlemleri sağ alttaki yuvarlak düğmede toplanır. `z-40`,
`bottom: calc(var(--h-tab) + var(--sp-4) + env(safe-area-inset-bottom))`.

| Ekran türü | FAB'ın taşıdığı |
|---|---|
| Liste ekranı | **Yeni kayıt** · **Ara ve süz** |
| Detay ekranı | Düzenle · havale · yazdır · sil (`EylemTabakasi`) |

Detay ekranlarında **eylem listesi TEK yerde kurulur** ve iki dal da onu okur.
İzin koşulları iki kez yazılsaydı biri unutulduğunda kullanıcı çalışmayacak bir
eylem görürdü.

### 4. Tabaka — `bilesenler/TabakaKabi.tsx`

Üstte açılan **her** pencerenin kabuğu: mobilde `vaul` alt tabakası, masaüstünde
ortalanmış Radix Dialog. Perde, köşe yarıçapı, tutamak, başlık şeridi ve
kapatma düğmesi tek yerde.

Üstüne kurulanlar:

| Bileşen | İş |
|---|---|
| `FormModal` | kayan gövde + **sabit alt eylem çubuğu** — bütün formlar, süzgeçler ve detay tabakaları |
| `SuzgecTabakasi` | arama/süzme grameri: `SuzgecBolumu` · `Segment` · `SuzgecSecenekleri` |
| `EylemTabakasi` | detay sayfasının FAB menüsü (56px satırlar) |
| `AltSheet` | menü/liste tabakası (`SheetSatiri`, `SheetAyirici`) |
| `NotEkleme` | mobilde tabaka, masaüstünde satır içi kart |

### Arama ve süzme grameri — her listede AYNI

Bu, kullanıcının en çok öğrendiği ve en çok tekrar eden şey; ekrandan ekrana
değişmemeli.

1. **Arama kutusu üstte kalır** — tabakanın içine girmez. Yazdıkça süzer
   (300ms geciktirmeli), `keepPreviousData` ile liste boşalmaz: dönem
   değiştirmek listeyi iskelete çevirip kaydırmayı başa atıyordu. İskelet
   yalnızca elde hiç veri yokken.
2. **Süzgeç tabakasının sırası sabit**: sekmeler (segment) → dönem → durum/tip
   çipleri → sıralama → `Temizle` / `Uygula`.
3. **Masaüstünde en sık kullanılan süzgeç şeritte kalır** (ör. özgeçmişte
   `Kaynak` bölümlü seçimi), gerisi tabakada. Telefonda hepsi tabakada.
4. **Açık süzgeç listenin üstünde ÇİP olarak görünür** ve tek tek
   kaldırılabilir; yanında `Hepsini temizle`. Kapalı bir tabakadaki unutulmuş
   süzgeç, "kayıtlarım kayboldu" diye gelen sorunun bir numaralı sebebi.
5. **Süzgeç düğmesi açıkken renklenir** ve yanında sayı taşır.
6. Süzgeç değişince **sayfa 1'e döner**. Yoksa 3. sayfadayken daraltılan liste
   boş görünüyor ve kullanıcı "kayıt yok" sanıyor.

> Süzgeç durumu **tek bir nesnede** tutulur (`OzgecmisSuzgecDegerleri` kalıbı)
> ve `degistir(kismi)` ile güncellenir. Sekiz ayrı `useState` varken her yeni
> süzgeçte "sayfayı 1'e al" satırını da tekrar yazmak gerekiyordu; biri
> unutulduğunda hata sessizdi.

### Satır grameri

| Görünüm | Ne kullanılır |
|---|---|
| Masaüstü | `Liste` → tablo (`Sutun<T>` tanımı) |
| Mobil, tek tip kayıt | `Liste` → sıkı liste (`mobilGorunum="liste"`) |
| Mobil, zengin kayıt | Ekranın **kendi satırı** (`InsetGrup` + kendi düzeni) |
| Mobil, kart anlamlı | `mobilGorunum="kart"` — yalnızca ajanda |

> **`Liste`'nin mobil dalı, `mobilBaslik`in YANINDA bütün sütunları da basar.**
> Sütunlara `mobil: false` verilmezse ad, telefon ve rozet iki kez çizilir;
> eylem sütunu da metnin altında havada duran ikon dizisine dönüşür. Özgeçmiş
> havuzunda kullanıcının "her şey birbirine geçmiş" dediği şey buydu. Zengin
> satırda `Liste`'yi masaüstüne bırak, mobil satırı kendin yaz.

Mobil satırın iskeleti — üç bileşen, **kardeş**, iç içe değil:

```tsx
<li className="flex items-start gap-2 p-3">
  <button onClick={ac} className="flex min-w-0 flex-1 …">  {/* kaydı açar */}
    <BasHarfCipi … />
    <span className="min-w-0 flex-1">{/* ad · nitelik · özet · üstveri */}</span>
  </button>
  <SatirEylemleri boyut="kucuk" eylemler={[…]} />          {/* tek grup */}
</li>
```

- **Düğme içinde düğme, bağlantı içinde bağlantı OLMAZ** — geçersiz HTML,
  tarayıcı davranışı tanımsız. Tarama yolu: `document.querySelectorAll('a a, button button')`.
- Düğmenin içindeki her şey `<span className="block">`; `<p>`/`<div>` düğme
  içeriği değil.
- **Dört ayrı ikon düğmesi değil, `SatirEylemleri` grubu.** 390px'te dört
  düğme 150–180px yiyor ve isim "Ahm…" diye kırpılıyor.
- **Rozet üstveri satırında durur**, sağ üst köşede değil: köşedeyken rozeti
  olan satırda eylem düğmesi aşağı kayıyor, olmayanda tepede duruyor ve liste
  titrek görünüyordu.
- **İstisna olmayan rozet çizilmez.** Havuzdaki kaydın "Havuz" demesi haber
  değil; rozet yalnızca ayırt eden durum için.

### Kayıt tabakası — detay sayfası olmayan kayıtlar için

Bir kaydın kendi rotası yoksa (özgeçmiş, davetteki kişi, halk günü katılımı)
satıra dokunmak **`FormModal` tabanlı bir kayıt tabakası** açar: en üstte asıl
eylem (indir/ara), sonra `FormBolumu` başlıklarıyla bölümler, altta sabit
eylem çubuğu. Listede kesilen uzun metnin tamamı orada okunur.

Tabaka **özetle açılır, ayrıntıyla dolar**: `detay.data ?? kayit`. Boş açılıp
sonra dolmak, dokunuşun karşılığını geciktiriyor.

## Klasör düzeni

```
src/
  kabuk/       AppShell, sidebar, appbar, tabbar, alt sayfa (sheet)
  bilesenler/  Buton, Input, StatusBadge, StatTile, DataTable, Toast, Takvim…
  ekranlar/    Giris, AnaSayfa, Talepler, TalepDetay, Ajanda, EtkinlikDetay, Takvim, Istatistikler, Ayarlar, Cicek, Yonetim
  data/        query istemcisi, API sarmalayıcı, üretilmiş tipler (İngilizce)
  auth/        SessionProvider, ProtectedRoute (İngilizce)
  stiller/     tokenlar.css, globals.css
```

### DİL SÖZLEŞMESİ

**Kod İngilizce, kullanıcı yüzeyi Türkçe.** Sınır kesindir:

| Ne | Dil | Örnek |
|---|---|---|
| Klasör, dosya, bileşen, kanca, değişken, tip, test adı | **İngilizce** | `components/Button.tsx`, `useInstallState()` |
| Kullanıcının GÖRDÜĞÜ her şey | **Türkçe** | `<Buton>Kaydet</Buton>`, `aria-label="Kapat"`, yardım metinleri |
| Sunucudan gelen alan adları | **Türkçe kalır** | `veriler`, `toplamSayfa` — sözleşme sunucunundur |

> Bu kural **tersine çevrildi**. Önceki sürüm klasör, dosya ve bileşen
> adlarının Türkçe olmasını söylüyordu (`design.md` §10.4). Sebebi
> okunabilirlikti; bedeli, kod tabanına dışarıdan katılan hiç kimsenin
> `SuzgecTabakasi`nin ne olduğunu anlayamaması ve araç ekosisteminin
> (lint kuralları, kod üreticiler, kütüphane kalıpları) hep İngilizce
> varsayması oldu. Ürün büyüdükçe (proje yönetimi, iş takibi modülleri)
> bu maliyet artıyor.
>
> **Ön yüzde geçiş TAMAMLANDI.** Katman katman yapıldı, dosya dosya değil:
> yarım bırakılmış bir katman iki dilden de kötüdür — `data/client.ts`
> İngilizce ama `veri/bicim.ts` Türkçe kalsaydı hangi adın nerede olduğunu
> kimse hatırlamazdı.

### Çevrilen katmanlar

| Katman | Durum |
|---|---|
| `src/data/` (eski `veri/`) · `src/auth/` (eski `kimlik/`) | ✅ |
| Dizin adları: `components · screens · shell · calendar · theme · notifications · help · styles` | ✅ |
| Dosya adları (129 dosya, 9 alt dizin) | ✅ |
| Tanımlayıcılar: `components` · `shell` · `theme` · `help` · `notifications` · `pwa` · `calendar` · `screens` | ✅ |
| **Ön yüz TAMAM** — kalan Türkçe dışa aktarım yok | ✅ |
| Sunucu: sınıf/metot/property adları | **toplu çevrilmeyecek** — bkz. aşağısı |

### Çeviri nasıl yapılır — maskeleyici

Naif kelime değiştirme YASAK. Ölçüm: `Liste` 10, `Renk` 7, `Kart` 2 kez
**kullanıcı metninin içinde** geçiyordu; toplu değiştirme "Liste" sekmesini
"DataList" yapardı. Araç (`scratchpad/cevir.py` kalıbı) şunları maskeler:

| Maskelenen | Neden |
|---|---|
| `//` ve `/* */` yorumları | Türkçe kalır |
| `'...'` `"..."` dizeleri | kullanıcı metni ve API yolları |
| `` `...` `` şablon METNİ | ama `${...}` KOD sayılır, çevrilir |
| JSX metin düğümleri | `>` ile `<` arası, `{}();=` içermeyen harfli bölge |
| Regex değişmezleri | `/Liste/` bir sekme adını eşliyordu |

**Aracın iki bilinen boşluğu var; ikisi de doğrulamayla yakalandı:**

1. **Regex** ilk sürümde maskelenmiyordu → `/Liste/` `/DataList/` oldu.
   Testler yakaladı (erişilebilir ad eşleşmedi).
2. **Şablon `${...}` derinliği** iç içe şablonda kayıyor; kapanış backtick'i
   ifadenin parçası sanılıp sonrasındaki gerçek kod metin olarak maskeleniyor.
   `tsc` yakaladı — import çevrilmiş, kullanım kalmıştı.

> İkinci boşluk **sessiz değil**: yutulan bölgedeki kullanım, dışarıdaki
> import yeni adı aradığı için derlemeyi kırar. Yine de çeviriden sonra
> yalnızca `tsc`'ye bakma — aşağıdaki üç tarama da koşulur.

### Çeviriden sonra ZORUNLU doğrulama

```bash
npx tsc --noEmit && npm test && npm run build && node test/gorsel/tur.mjs
# SUNUCU TESTLERİ DE: bir C# testi bu depoya SABİT YOLLA uzanıyor
(cd ../.. && dotnet test -p:SkipFrontend=true)
# JSX metnine sızıntı
grep -rnoE ">[^<>{}]*\b(YeniAd1|YeniAd2)\b[^<>{}]*<" --include="*.tsx" src/
# Türkçe proplara sızıntı
grep -rnE "(etiket|baslik|aciklama|placeholder|aria-label|title)=\"[^\"]*\b(YeniAd1)\b" --include="*.tsx" src/
# regex değişmezine sızıntı
grep -rnoE "/[^/\n ][^/\n]*\b(YeniAd1)\b[^/\n]*/[gimsuy]*" --include="*.ts*" src test
```

> **ÖN YÜZ TAŞIMASI SUNUCU TESTİNİ KIRABİLİR.**
> `IzinKataloguSenkronTests` (C#) izin kataloğunu doğrulamak için
> `frontend/src/components/permissions.ts` dosyasını **sabit yolla** okuyor.
> `bilesenler/izin.ts` taşınınca o test düştü ve buradaki hiçbir doğrulama —
> `tsc`, `vitest`, görsel tur — bunu göremezdi. SPA'da dosya/dizin
> taşıdıktan sonra `dotnet test` de koşulur.

> **`tsc` tek başına YETMEZ.** Dizin/dosya taşımasından sonra iki sınıf hata
> yalnızca testlerden çıktı: kaynağı yol DİZESİYLE okuyan testler
> (`'screens/Requests.tsx'`) ve yolu parça parça veren testler
> (`join(kok,'styles','tokens.css')`). Bunlar derlemeye görünmez.

Çevrilen adlardan sık kullanılanlar:

| Eski | Yeni |
|---|---|
| `useOturum()` → `.ben` `.iznimVar` `.yetkisiVar` `.cikisYap` | `useSession()` → `.me` `.hasPermission` `.hasPolicy` `.signOut` |
| `KorumaliRota izin= politika= rol=` | `ProtectedRoute permission= policy= role=` |
| `SayfaliSonuc<T>` · `ApiHatasi` · `jetonDepo` · `sorguDizesi` | `PagedResult<T>` · `ApiError` · `tokenStore` · `queryString` |
| `sunucudanYerele` · `yerelDenSunucuya` | `serverToLocal` · `localToServer` |
| `tarihKisa` · `tarihSaat` · `basHarfler` · `bagil` · `boyut` | `shortDate` · `dateTime` · `initials` · `relativeTime` · `fileSize` |
| `Etkinlik` · `Talep` · `KullaniciOzet` | `Event` · `Request` · `UserSummary` |

> **`PagedResult<T>`in ALANLARI Türkçe kaldı** (`veriler`, `toplam`, `sayfa`,
> `boyut`, `toplamSayfa`, `oncekiVar`, `sonrakiVar`) — sözleşme sunucunun.
> Aynı sebeple `queryKeys` içindeki önbellek anahtarı METİNLERİ de Türkçe:
> henüz çevrilmemiş ekranlar `['ozgecmis']` gibi ham anahtarları elle kurup
> `invalidateQueries` çağırıyor; anahtarı değiştirmek o çağrıları sessizce
> etkisiz kılardı.

> **Çeviri sırasında yakalanan tuzak:** toplu değiştirme, JSX METİN
> düğümlerine de dokunuyor — "Bildirim yok" bir ara "AppNotification yok"
> oldu. Aynı aile: bileşenin kendi fonksiyon adı, takma adlı bir tip
> importuyla aynı yazımdaysa o da yeniden adlandırılıyor. Bu yüzden çeviriden
> sonra **kullanıcıya görünen metinlerde İngilizce kelime taraması** yapılır:
> ```bash
> grep -rnoE ">[^<>{}]*\b(Event|Request|Notification|User|Save|Delete)\b[^<>{}]*<" --include="*.tsx" src/
> grep -rnE "(etiket|baslik|aria-label|title)=\"[A-Za-z ]*\b(Event|Request|Save)\b" --include="*.tsx" src/
> ```

**Kullanıcı metni asla tanımlayıcıdan türetilmez.** `etiket={alan.name}` gibi
bir şey yazma; Türkçe metin ayrı yazılır. İkisini birbirine bağlamak, kodu
çevirdiğinde arayüzü İngilizceye çevirir.

## KURUM BİLGİSİ DERLEMEYE GÖMÜLMEZ

Uygulama başka belediyelere verilecek. Kurum adı, amblem ve kurumsal renkler
kaynağa yazılsaydı her kurum için ayrı bir ön yüz derlemesi gerekirdi — ve
"kurumu değiştirmek tek bir ayar" hedefi ölürdü.

Tek kaynak: **`GET /api/v2/institution`** (anonim). Sunucu bunu veritabanındaki
tek satırlık `kurum_bilgileri` tablosundan üretiyor; yetkili kullanıcı
`/kurum` ekranından düzenliyor.

`src/institution/institution.ts`:

| Dışa aktarım | İş |
|---|---|
| `currentInstitution()` | Bellekteki/önbellekteki değer — **senkron** |
| `loadInstitution()` | Sunucudan okur; eşzamanlı çağrılarda tek istek |
| `refreshInstitution()` | Önbelleği atlar — kayıttan sonra gerekli |
| `useInstitution()` | Bileşenler için kanca |
| `applyDocumentIdentity()` | Belge başlığı, `meta description`, favicon |
| `buyukHarf()` | Türkçe büyük harf (`toLocaleUpperCase('tr-TR')`) |

**İKİ AŞAMALI uygulama** (`main.tsx`): önce `localStorage`'daki son yanıt
senkron uygulanır — ilk kare doğru renkle boyansın, ağ beklenmesin — sonra
sunucudan tazelenir ve `KURUM_TEMA_OLAYI` ile tema motoruna duyurulur.

> Önbellek olmadan uygulama çevrimdışı açıldığında amblem ve renkler
> kayboluyordu; markanın bir açılışta var, diğerinde yok olması "bozuldu"
> izlenimi veriyor.

**Renkler palete yazılır, ayrı bir yola değil.** `markaPaletiniUygula()`
(`theme/palettes.ts`) kurumun renklerini `BRAND_COLORS[0]` / `ACCENT_COLORS[0]`
/ `NEUTRAL_COLORS[0]` üzerine yazar. "Kurumsal" ön ayarı zaten 0. indeksi
kullanıyor; böylece hem ön ayar hem tema panelindeki seçenek otomatik olarak
o kurumun rengini gösterir, diğer paletler (Zümrüt, Bordo…) tasarlandığı gibi
kalır.

> Ön ayar adları `sivas-acik`/`sivas-koyu` idi → **`kurumsal-acik`/
> `kurumsal-koyu`**, görünen adlar "Kurumsal Gündüz"/"Kurumsal Gece".

**Firebase istemci yapılandırması da bu uçtan gelir** — gizli değil ama kuruma
özel. `fcm.ts` içindeki `baslat()` bu yüzden **asenkron**; `onForegroundMessage`
yine de senkron döner (React temizleyicisi söz kabul etmiyor) ve abonelik hazır
olunca kuruluyor.

**Manifest sunucuda üretiliyor** (`GET /manifest.webmanifest`). `public/`
altındaki statik dosya kaldırıldı: kurum değişince manifest de değişmeli ve
bunun için ön yüzü yeniden derlemek gerekmemeli.

**Açılış perdesi** (`index.html`) kurum adını `localStorage`'daki son kayıttan
okur; ilk açılışta boş kalır. Yanlış bir kurum adı göstermektense boş bırakmak
doğru.

### Ölçüldü — uçtan uca

Kurum kaydı değiştirilip sayfa yenilendiğinde:

```
--brand      #002E6D → #7A1F2B
belge adı    "Randevu Takip Sistemi · Sivas Belediyesi"
             → "KentOS.Mini · Deneme Belediyesi"
menü · manifest · giriş ekranı  hepsi kayda uydu
```

### `/kurum` ekranı

`screens/InstitutionSettings.tsx` — beş bölüm (kimlik · iletişim · uygulama ·
renkler · görseller), `sistem.kurum` izniyle kapalı.

- **Renk alanı hem seçici hem hex kutusu.** Yalnızca seçici olsaydı kurumsal
  kimlik kılavuzundaki kodu yapıştırmak mümkün olmazdı; yalnızca kutu olsaydı
  dokunmatikte renk denemek zahmetli olurdu.
- Kaydettikten sonra **sunucudan taze okunur**: türetilen alanlar (görünen ad
  boşsa kurum adına düşer gibi) sunucuda hesaplanıyor.
- Marka ve başlık **anında** uygulanır — kaydedip hiçbir şeyin değişmediğini
  görmek "kaydedilmedi" izlenimi veriyor.
- Mobilde kaydet çubuğu **sabit** (`bottom: calc(var(--h-tab) + safe-area)`);
  form uzun ve alta inmeden kaydedememek kullanıcıyı her değişiklikte sayfanın
  dibine yolluyordu.
- Ölçüm (390px): yatay taşma yok · iç içe etkileşim 0 · 44px altı dokunma
  hedefi yok (renk seçici 40→44, eylem düğmeleri 36→44; masaüstünde eski ölçü).

> Bu ekranı yazarken **iki bekçi de işini yaptı**: token testi
> `var(--tabbar-h)` diye bir token olmadığını söyledi (doğrusu `--h-tab`),
> yardım testi de menüdeki her ekranın yardım metni olması gerektiğini.

## Rotalar

`design.md` §6: `/giris` · `/` · `/talepler` · `/talepler/:id` · `/ajanda` · `/ajanda/:id` · `/takvim` · `/istatistikler` · `/ayarlar` (+ `/cicek`, `/yonetim/*`).

Sonradan eklenenler: `/talepler/yeni`, `/talepler/:id/duzenle`, `/protokol`,
`/protokol/:id`, `/davetler`, `/davetler/:id`, `/gonderim`, `/gonderim/:id`,
`/bildirimler`, `/tanimlar`, `/yonetim/birimler/:id`, `/yonetim/roller/:ad`,
`/halk-gunu`, `/halk-gunu/basvurular`, `/halk-gunu/:id`, `/halk-gunu/:id/salon`,
`/ozgecmisler`, `/cicek/:id`, `/hatalar`, `/hatalar/:id`, **`/yardim`**,
**`/kurum`**.

> **Menüde ya da bir tabakada gezinilen HER yolun `App.tsx`'te rotası
> olmalı.** `/yardim` bağlantısı aylarca "Sayfa bulunamadı" gösterdi: ölü
> bağlantı geçerli bir dizedir, derleme de test de görmez. Bekçi:
> `test/yardim.test.tsx` → "gezinme hedefleri" — kaynak taranıp menü ve kabuk
> hedefleri rota tablosuyla karşılaştırılıyor.

**Harita ekranı KALDIRILDI**; sunucudaki `talep/harita` ucu duruyor (mobil
kullanıyor). Taleplerdeki arşiv düğmesi de kaldırıldı; arşivli kayıtlara
web'den erişim yok, mobildeki Arşiv sekmesi yerinde.

> Eski `talep/halk-gunleri` ucu ve onun ekranı da kalkmıştı — o yalnızca
> `RandevuTipId == 1` süzgeciydi. **`/halk-gunu` bununla ilgisiz**, ayrı
> tablolara oturan yeni bir modül; eski uç ölü ama duruyor.

Ajanda sekmesi de URL'de: `/ajanda?sekme=liste|silinmis` (varsayılan
`program`).

Yönetim ekranının **etkin sekmesi de URL'de**: `?bolum=birimler|roller|oturumlar`
(varsayılan `kullanicilar`), birim formu `?bolum=birimler&birim=yeni|<id>`,
kullanıcı formu `?bolum=kullanicilar&kullanici=yeni|<id>`.
Birim detayından "Düzenle" ile geri dönen kullanıcı ağacı açık buluyor ve
görsel tur her sekmeyi — formlar dahil — tek başına gezebiliyor.

> Kullanıcı formunun durumu önce bileşen içindeydi ve görsel tur o ekranı hiç
> açamıyordu: birim açılır listesinin yetkiliyi göstermediği hata tam da bu
> yüzden gözden kaçtı. **Bir formu yalnızca tıklayarak açılabilir bırakmak,
> onu doğrulamanın dışında bırakmak demek.** Düzenlenen kullanıcı listeden
> değil `kullanicilar/{id}` ucundan okunur — derin bağlantı ikinci sayfadaki
> bir kullanıcıya gelebiliyor ve listede bulunamayınca form boş açılıyordu.

> Rota sırası tuzağı: `yeni` ve `:id/duzenle` rotaları **detaydan ÖNCE**
> yazılır; aksi hâlde `yeni` bir kimlik sanılır ve detay ekranı 404 gösterir.

Uygulama **kökten** çalışır (Vite `base: '/'`, router `basename="/"`).
**Modül seçme ekranı yoktur** — giriş sonrası doğrudan Ana Sayfa'ya düşülür.

## API

Yalnızca **`/api/v2`** kullanılır. `/api/XxxApi` (v1) mobil uygulamanın sözleşmesidir, buradan çağrılmaz.

TypeScript tipleri `openapi-typescript` ile v2 Swagger dökümanından üretilir ve depoya işlenir:
```bash
npm run tipler:uret   # /swagger/v2/swagger.json → src/data/types.generated.ts
```
`types.generated.ts` **elle düzenlenmez**. Ekranlar `src/data/types.ts` içindeki
okunabilir takma adları kullanır (`Etkinlik`, `TalepOzet`, `KullaniciOzet`…).
Sunucu sözleşmesi değişince derleme kırılır — sessizce kaymaz.

Uçları üretmek için sunucunun ayakta olması gerekir:
```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../ --urls http://localhost:5097
```

## Kimlik

Mobil ile **aynı JWT**, ama uç v2: `POST /api/v2/oturum/giris` → `{ jeton, gecerlilikSonu }`.
Jeton `localStorage`'da (`sv-jetonu`); `data/client.ts` sarmalayıcısı
`Authorization: Bearer` ekler.
**Refresh token yok** → 401 gelince token temizlenir ve `/yeni/giris`'e yönlendirilir.

> **Her 401 "oturum düştü" değil.** Giriş isteği jetonsuz gider; kimlik
> hatalıysa sunucu yine 401 döner ve gövdesinde gerekçe yazılıdır
> (`detail`: "Kullanıcı adı veya şifre hatalı", hesap kilitlendiyse ne kadar
> beklenmesi gerektiği). Sarmalayıcı bir dönem gövdeyi atıp yerine "Oturum
> süresi doldu." yazıyordu: yanlış şifre yazan kişi bunu anlamıyor, kilitlenen
> hesap sessiz kalıyor ve kullanıcı denedikçe kilit uzuyordu. Ayrım **jeton
> gönderilip gönderilmediğine** bakar — jetonsuz istekte 401 düşmüş bir
> oturumdan gelemez, reddedilen şey isteğin kendisidir; orada oturum da
> temizlenmez. Bekçi: `test/istemci.test.ts`.
Çıkışta web push jetonu da sunucudan silinir **ve `queryClient.clear()` çağrılır** —
ortak bilgisayarda önceki kullanıcının gizli etkinlikleri önbellekte kalmasın.

## İş kuralları istemcide yeniden yorumlanmaz

**Tekrar eden etkinlikler:** kuralın sahibi sunucudur, tekrarlar gerçek satır olarak gelir — istemcide genişletme yapılmaz. Bir tekrar düzenlenirken/taşınırken **kapsam** sorulur (`Yalnızca bu / Bu ve sonrakiler / Tümü`) ve `kapsam` alanıyla gönderilir.

**Gizli etkinlikler:** görünürlüğü sunucu süzer. İstemci yalnızca kilit rozetini ve katılımcı listesini gösterir. `Gizli && BasinKatilsin` formda engellenir (sunucu da reddeder).

Ayrıntılı değişmezler için `../CLAUDE.md`.

## Takvim

Beş görünüm: **gün · hafta · ay · yıl · ajanda**. Sıfırdan yazılmıştır (hazır
takvim kütüphanesi yok), sürükleme dnd-kit ile.
Varsayılan etkinlik süresi **30 dakika**, ızgara ve yuvarlama **30 dakikalık** adımlarla.
Çakışma **engellenmez, uyarılır**.

- Gün ve hafta **tek bileşendir** (`takvim/ZamanIzgarasi.tsx`): hafta,
  "yedi sütunlu gün"dür. İki kopya olsaydı sürükleme hatası yalnızca birinde
  düzeltilirdi. Haftada sürükleme **günü de değiştirir** (hedef sütun dnd-kit
  `useDroppable` ile bulunur).
- Izgara tek bir CSS grid: başlık · tüm gün · saatler satırları hep hizalı ve
  dar ekranda **yatay kayarken** başlıklar sütunuyla birlikte gider. Sütun en az
  104px; 390px'te yedi sütun 46px'e düşüp etkinlik başlığı kırpılıyordu.
- **"Şu an" çizgisi kırmızıdır** (`--simdi`). Altın, "bugün" halkasının ve etkin
  sekmenin rengi; aynı rengi "şu an" için de kullanmak ikisini karıştırıyordu.
- Boş yarım saatlik hücreye gelince **+** belirir, tıklayınca etkinlik diyaloğu
  o saatle açılır. Hücreler `tabIndex={-1}`: haftada 336 hücre var, hepsi sekme
  durağı olsaydı klavye kullanıcısı ızgarayı geçemezdi (klavye yolu araç
  çubuğundaki "Yeni etkinlik").

## Etkinlik formu diyalogda

`ekranlar/etkinlik/EtkinlikModal.tsx` — takvim dilimi, ajandadaki düğme ve
`/ajanda/yeni` rotası **aynı** diyaloğu açar. Takvimden eklerken sayfa
değişmiyor, kullanıcı baktığı haftayı kaybetmiyor.
Alanların kendisi kabuktan bağımsız: `EtkinlikAlanlari.tsx`; kabuk ise
`TabakaKabi` (bkz. *Katman gramerı*) — yani mobilde parmakla kapanan gerçek
bir tabaka.

Rotalar deep-link olarak duruyor ve diyaloğun **arkasına** gidilecek ekranı
çizer (yeni → ajanda, düzenle → etkinlik detayı).

> Talep formu diyalog DEĞİL, tam sayfa: vatandaş karşısında doldurulan uzun bir
> kayıt ve mobilde klavye açıkken diyalogda kaydet düğmesi ekran dışında kalıyor.

## Tekrar eden etkinlik kuralı

`ekranlar/ajanda/TekrarKurali.tsx` — mobil (`rrule_yardimcisi.dart`) ile AYNI
RRULE alt kümesi, aylık desenler dahil (`BYMONTHDAY`, sıra ekli `BYDAY=2TH`).

İki kural pazarlık konusu değil, ikisi de testle bekçileniyor
(`test/tekrar.test.ts`):

1. **`BYDAY` yalnızca kullanıcı açıkça gün seçtiyse yazılır.** Kuralı formun
   başlangıç tarihinden türetmek bilinen üretim hatası: tekrarı çarşambadan
   perşembeye taşımak `BYDAY=TH` gönderiyor, sunucu "kural değişti" diye okuyup
   seriyi bölüyor, etkinlik kaybolmuş görünüyordu.
2. **Kayıpsız gidiş-dönüş.** Kullanıcı tekrar bölümüne dokunmadıysa ham kural
   AYNEN geri gider (`ham` + `dokunuldu`). Bu olmadan formun anlamadığı bir
   parça (mobilin ürettiği `BYMONTH` gibi) sessizce düşüyor ve kaydet'e basmak
   seriyi bölüyordu.

## Tarih ve saat seçicileri

`bilesenler/TarihSecici.tsx` ve `bilesenler/ZamanAraligiSecici.tsx`.
Tarayıcının yerleşik `<input type="date">` / `datetime-local` alanları
**kullanılmaz**: görünümü tarayıcıya göre değişiyor, koyu temaya uymuyor ve
Safari'de gün adları İngilizce çıkıyor. Hafta **pazartesi** başlar.
Zaman aralığında başlangıç kayınca bitiş **süreyi koruyarak** kayar.

Uzun referans listeleri için `bilesenler/AramaSecici.tsx` — mahalle/meslek
sunucuda aranır, `<select>` ile binlerce satır indirilmez.

## Derleme

```bash
npm run dev      # Vite dev sunucusu; /api → http://localhost:5097 proxy'si (CORS gerekmez)
npm run build    # → ../wwwroot/ (uygulama varlıkları ../wwwroot/uygulama/)
```

`dotnet build` / `dotnet run` / `dotnet publish` bu derlemeyi otomatik tetikler (Web csproj'daki MSBuild hedefi).
Atlamak için: `dotnet build -p:SkipFrontend=true`.

`wwwroot/uygulama/` ve `wwwroot/index.html` **üretilmiş çıktıdır** — elle
düzenlenmez.

> `build.emptyOutDir` **KAPALI**: çıktı doğrudan `wwwroot`a yazılıyor ve açık
> bırakmak `wwwroot/uploads` altındaki GERÇEK belgeleri (özgeçmiş, talep eki,
> etkinlik fotoğrafı) her derlemede silerdi. Varlıklar da `assetsDir: 'uygulama'`
> ile kendi klasörüne gidiyor — `wwwroot/assets` eski MVC'nin dosyalarını
> taşıyor, ikisini karıştırmak temizliği imkânsız kılardı.

## Web push

`firebase-messaging-sw.js` `public/` içinde durur ve `wwwroot/` altına
kopyalanır; **`{ scope: '/' }`** ile kaydedilip
`getToken({ serviceWorkerRegistration })`'a verilir.

> Kapsam bir dönem `/yeni/` idi ve uygulama köke taşınınca **kapsam dışında
> kaldı**. Kök kapsam artık zorunlu: hem `start_url` (`/`) worker'ın
> denetiminde olmalı (kurulabilirlik şartı) hem de push kaydı aynı kapsamdan
> geliyor. Bedeli, eski MVC sayfalarının da bu worker'dan geçmesi — bu yüzden
> `fetch` dalı yalnızca **bizim ürettiğimiz** statik varlıkları önbelleğe
> alıyor (bkz. *PWA kurulumu*).

Yapılandırma `.env` içinde (`Firebase__*`) ve istemciye `GET /api/v2/institution` ile iner — kaynakta hiçbir Firebase anahtarı yoktur.
Yönlendirme sözleşmesi mobille aynı: `data.fcmData` → `{ entity, id, action }`.
Varlıklar: `Ajanda → /ajanda/:id` · `Talep → /talepler/:id` · `Oneri → /oneriler/:id`
· `Dosya → /gonderim/:id`. Yeni bir varlık eklenirse **üç yerde** karşılığı
yazılmalı: `bildirim/BildirimMerkezi.tsx` (`bildirimYoluCoz`),
`bildirim/BildirimKoprusu.tsx` (bayatlayan sorgular) ve
`public/firebase-messaging-sw.js` (arka plan tıklaması).

## PWA kurulumu: "kurulu mu?" bir TAHMİNDİR

`src/pwa/kurulum.ts` bir durum makinesi (`useKurulum()` ile React'e bağlanır).
Hiçbir tarayıcı "bu uygulama bu cihazda kurulu mu?" sorusuna cevap vermiyor;
elde dört sinyal var ve her birinin kör noktası ayrı:

| Sinyal | Ne söyler | Kör noktası |
|---|---|---|
| `display-mode: standalone` | KURULU pencerenin içindeyiz | sekmede her zaman `false` |
| `appinstalled` | kurulum ANI | sonrasını biz hatırlamalıyız |
| `beforeinstallprompt` | uygulama kurulu DEĞİL | tarayıcı geciktirebilir |
| `getInstalledRelatedApps()` | sekmeden de kurulu diyebilir | her tarayıcıda yok |

> **Düzeltilen üretim hatası:** kurulum işareti `localStorage`'a yazılıyor ama
> hiç silinmiyordu — üstelik kart kapatıldığında da, kurulum başarılı
> olduğunda da aynı KALICI "bir daha gösterme" anahtarı yazılıyordu.
> Kullanıcı uygulamayı telefonuna kurup sonra kaldırdığında kurulum düğmesi
> **bir daha asla görünmüyordu**. Artık: yeni bir `beforeinstallprompt`
> işareti siler (tarayıcı bu olayı yalnızca kurulu DEĞİLKEN gönderir),
> `getInstalledRelatedApps()` olumlu sonucu doğrular, kapatmak süreli
> **erteleme**dir (14 gün) ve kullanıcının elinde `kuruluIsaretiniSil()`
> kaçış kapısı var ("Kaldırdım" düğmesi, Ayarlar).

- **Kurulabilirlik isteme BAĞLANMAZ.** İstem tek kullanımlık ve iOS'ta hiç
  gelmiyor; kapıyı ona bağlamak kurulum yolunu tamamen yok ediyordu. Kapı
  yalnızca kurulumu gerçekten desteklemeyen tarayıcıda kapanır (masaüstü
  Firefox).
- `src/pwa/talimat.tsx` — elle kurulum adımları **tarayıcıya göre**: iOS
  Safari (Paylaş → Ana Ekrana Ekle), iOS Chrome (`⋯`), Android (`⋮`), macOS
  Safari (Dosya → Dock'a Ekle), masaüstü Chromium (adres çubuğu simgesi).
  Yanlış yeri tarif etmek hiç tarif etmemekten kötü: kullanıcı olmayan bir
  menüyü arıyor.
- `src/pwa/KurulumDugmesi.tsx` — **appbar'daki kurulum simgesi**, yalnızca
  kurulu değilken. Marka renginde: altı simgelik bir şeritte hepsi nötr griyse
  yeni gelen fark edilmiyor. Grubun BAŞINDA duruyor ki kurulunca kaybolduğunda
  şeridin sağı (çıkış, tema, bildirim) yerinden oynamasın.
- Bekçi: `test/pwa.test.ts` — özellikle "kaldırılınca kurulum yeniden görünür".

> **Service worker'ın `fetch` kapısı da kırıktı.** `url.pathname.indexOf('/yeni') !== 0`
> ile başlayan erken çıkış, uygulama kökten yayınlanmaya başlayınca HİÇBİR
> isteği içeri almaz oldu: worker vardı, önbellek boştu, çevrimdışı açılış
> tarayıcının hata sayfasıydı. Yerine **beyaz liste** geldi (`/uygulama/`,
> `/ikon/`, `/amblem.png`, `/manifest.webmanifest`); gezinme istekleri ağa
> gidiyor, yalnızca ağ düşünce kabuk devreye giriyor — kapsam kökte olduğu
> için eski MVC sayfaları önbellekten YANLIŞLIKLA sunulamamalı. Precache de
> `addAll` değil tek tek: hep-ya-hiç davranışı, bir ikonun eksikliğinde
> worker'ı hiç etkinleştirmiyor ve bildirimleri de sessizce kapatıyordu.

## Bildirim şeridi TIKLANABİLİR

`bildir(tur, baslik, aciklama, { eylem, eylemEtiketi })` — dördüncü parametre
verildiğinde şerit baştan başa bir düğmeye dönüşür (`Toast.Action`), sağında
bir `>` belirir ve tıklanınca hem gider hem kapanır.

Sebebi: aynı bildirim iki yerde çıkıyordu ve **davranışı pencereye göre
değişiyordu** — uygulama kapalıyken işletim sisteminin bildirimi kayda
götürüyor, açıkken çıkan şerit hiçbir şey yapmıyordu.

- **Yol değil GERİ ÇAĞRI alınır**: `ToastSaglayici` router'ın DIŞINDA
  (`main.tsx`), yani orada `useNavigate` yok. Yolu bilen zaten çağıran taraf.
- **Otomatik yönlendirme hâlâ YOK.** Kullanıcıyı okuduğu ekrandan habersizce
  koparmak, bildirimin kendisinden rahatsız edici. Karar kullanıcıda; şerit
  yalnızca yolu açıyor.
- **Kaydırarak kapatma gezinme SAYILMAZ**: şeridi ekrandan dışarı atmak da
  `click` üretiyor ve kullanıcı bu hareketi bildirimi REDDETMEK için yapıyor.
  10px eşiği bu iki niyeti ayırıyor (`test/toast.test.tsx`).

## Resimler modalda açılır

`bilesenler/ResimGoruntuleyici.tsx` — etkinlik fotoğrafları ve talep
dosyalarındaki resimler yeni sekmede DEĞİL, tam ekran yapılabilen bir
görüntüleyicide açılır (ok tuşlarıyla gezinme, `+`/`−` yakınlaştırma, `0`
sıfırlama, `F` tam ekran, `Esc` kapatma, küçük resim şeridi).
Yakınlaştırma `transform: scale` ile — genişlik/yükseklik canlandırmak her
karede yeniden yerleşim demek.

Talep dosyalarında yalnızca **resim uzantıları** görüntüleyiciye gider;
PDF/belge tarayıcının kendi görüntüleyicisine gider.

> `.katman` yardımcı sınıfı katman yükseltmesi için `transform: translateZ(0)`
> KULLANMAZ: kendisi de bir yardımcı sınıf olduğu için Tailwind'in
> `-translate-x-1/2` değerini eziyordu ve ortalanmış her diyalog animasyon
> bittiği anda sağ alta kayıyordu. Yükseltmeyi `will-change: transform` yapar.

## Halk Günü üç ekran

Modül tek bir ekrana sığmıyor çünkü **üç ayrı kişi** kullanıyor:

| Ekran | Kim · ne yapar |
|---|---|
| `ekranlar/HalkGunu.tsx` | gün listesi — "hangi gün ne kadar iş çıktı" |
| `ekranlar/halkgunu/Basvurular.tsx` | sekreter · bekleyenler havuzuna kişi yazar |
| `ekranlar/halkgunu/HalkGunuDetay.tsx` | Özel Kalem · dilim tanımlar, atar, sıralar, SMS atar |
| `ekranlar/halkgunu/SalonModu.tsx` | salondaki personel · tabletle sırayla ilerler |

- **Salon modu MOBİLLE AYNI davranır.** Tablet her zaman olmayabiliyor;
  görüşme notu ve katılım işareti bilgisayardan da giriliyor. Bu yüzden
  davranış birebir eşitlendi:
  **Geldi / Gelmedi tek seçim ve sırayı İLERLETMEZ** (vatandaş daha yeni
  oturdu, not yazılacak), sırayı yalnızca **Tamamlandı** ilerletir. Not ve
  "ilgilenilecek" işareti HER kayıtla gönderilir — sunucu kısmi güncelleme
  yaptığı için "Geldi" derken yazılan not kaybolmuyor. Aktif kişi değişince
  not kutusu ONUN kaydından doldurulur; önce yalnızca listeden elle
  seçildiğinde yükleniyordu ve sıra otomatik ilerlediğinde bir öncekinin notu
  kutuda kalıyordu (her kayıtta not da gönderildiği için bu, birinin notunun
  başkasının üstüne yazılması demekti).
- **Salon modu ayrı tutuldu.** Kurma düğmeleri (dilim ekle, atama yap, SMS)
  salonda işe yaramıyor ve kalabalıkları sırası gelen vatandaşı bekletiyordu.
  Orada üç eylem var, hepsi tek dokunuş: Geldi · Gelmedi · Görüşüldü. Not
  alanı **her zaman açık** — ayrı pencere açtırmıyor.
- **Sıralama sürükle-bırak DEĞİL**, yukarı/aşağı düğmeleri. `@dnd-kit/sortable`
  kurulu değil ve tablette uzun listede sürüklemek düğmeye basmaktan zor.
- `bilesenler/KisiGecmisi.tsx` **üç yerde aynı bileşen**: havuz formunda,
  katılım satırında ve salon modunda. "Bu kişiyi daha önce gördük mü?" sorusu
  üçünde de aynı; üç kez kurmak, birinde eksik alan göstermek demekti.
- Takip sayısı (`takipSayisi`) **gün listesinde** duruyor: Özel Kalem'in bir
  sonraki işi tam olarak bu ve günün içine girmeden görülmeli.
- **Havuz üç sekme**: Bekleyenler · Reddedilenler · Tümü. Ret bir pencerede
  gerekçe sorar; gerekçe listede kaydın altında yazılı kalır — aynı kişi bir
  sonraki ay yeniden başvurduğunda okunacak tek şey o. "Havuza geri al" kararı
  geri alır.
- **Kişi satırında "başka saate taşı"**: hedef dilimleri doluluklarıyla
  (`2 / 4 kişi`) listeler, "saati belirlenmemişlere al" da seçenek. Listeyi
  kurarken en sık yapılan düzeltme bu; çıkarıp yeniden atamak sırayı ve notu
  kaybettiriyordu.

## Form bölümleri: mobilde çerçeve değil ayraç

`bilesenler/FormBolumu.tsx` — diyalog içindeki grupları çizer.

Gruplar önce `Kart` idi: çerçeve + 16px iç boşluk. Mobilde form zaten alttan
açılan bir tabakanın içinde ve tabakanın kendi 16px'i var; üstüne kartınki
binince kullanılabilir alan iki yandan ~34px daralıyordu.

Mobilde çerçeve **kalkar** ve başlık iki yanından altın saç teliyle yazılır:
`──── BAŞVURAN ────`. İç boşluk da yalnızca masaüstünde (`md:p-4`) — mobilde
sınırı bu çizgi çiziyor. Masaüstünde kart **aynen** kalır; oradaki diyalog
görünümü beğenildi, değiştirilmiyor.

> Bir denemede aynı iş **dikey** bir altın şeritle yapılmıştı: şerit metnin
> solundan yer yiyor, başlıkla aynı satırda olmadığı için bölümün nerede
> başladığını da vurgulamıyordu.

Altın burada kılavuzdaki işini yapıyor: **saç teli vurgu**, geniş dolgu değil.

> Bölüm başlığı CSS ile büyük harfe çevriliyor ve `innerText` **dönüşmüş**
> hâli veriyor: görsel turda "Başvuran" diye beklemek artık çalışmaz.

## Mobil liste gramerı: üstte arama, altta FAB

Ajanda ve Talepler telefonda aynı düzeni kullanır; ikisi arasında kas hafızası
bozulmasın diye:

| Yer | Ne var |
|---|---|
| Üst şerit | **Tek kontrol**: arama alanı + sağında yazıcı, ortak kenarlık içinde |
| Sağ alt | **FAB** (`kabuk/mobil/Fab.tsx`) → "Yeni kayıt" ve "Ara ve süz" |
| Süzgeç tabakası | `bilesenler/SuzgecTabakasi.tsx` — sekmeler segment olarak, dönem/durum/tip çipleri, sıralama |
| Liste | `InsetGrup` + `ListeSatiri` — tek yüzey, saç teli ayırıcı, solda durum renkli çip |

Sebep: üst taraf denetim yığınına dönmüştü (sekmeler, arama, dönem gezgini,
tip menüsü, çıktı düğmeleri, ekleme düğmesi) ve telefonda ilk ekranın yarısı
kontrol, asıl liste kıvrımın altındaydı.

- **Mobil ve masaüstü ağaçları `useMasaustuMu()` ile AYRILIR**, `md:hidden` ile
  ikisini birden çizmek değil. İkisi de DOM'daysa aynı `aria-label`'lı iki
  arama alanı oluyor, ekran okuyucu ikisini de duyuruyor ve testler "birden
  çok eşleşme" diyor. Ayırınca **boş durumu iki dala da vermeyi unutma** —
  bir kez unutuldu ve mobilde boş liste bomboş bir sayfa gösterdi.
- **Program görünümünde tarih ayracı YAPIŞKAN** (`top: var(--h-bar-m)`).
  Soldaki tarih kutusu masaüstünde kalır; telefonda 58px'i kalıcı olarak
  yiyordu ve kart başlıkları iki satıra kırılıyordu.
- **Liste boşalmaz** (`placeholderData: keepPreviousData`): dönem değiştirmek
  listeyi iskelete çevirip kaydırmayı başa atıyordu. Artık eski liste yerinde
  durur, üstünde ince bir şerit akar. İskelet yalnızca elde hiç veri yokken.
- Kaydırma konumu `kabuk/KaydirmaGeriYukle.tsx` ile korunur — konumu
  **kaydırdıkça** kaydeder, ayrılırken değil: React yeni sayfayı DOM'a
  yazdığı anda belge kısalıyor ve tarayıcı kaydırmayı etkiler çalışmadan
  sıfıra kırpıyor.

## Çiçekçi ve protokol kişi dosyaları

İki liste ekranında kayda tıklamak hiçbir şey açmıyordu; ikisinin de detayı
yazıldı:

- `ekranlar/cicek/CicekciDetay.tsx` (`/cicek/:id`) — talimatlar, bağlı
  oldukları programlar, dönem süzgeci ve Excel/PDF. Dönem **boş başlar**:
  varsayılan "bu ay" olsaydı ekran çoğu zaman boş açılır ve "kayıt yok"
  sanılırdı.
- `ekranlar/protokol/ProtokolDetay.tsx` (`/protokol/:id`) — iletişim bilgileri
  ve **davet edildiği programlar**. Sunucudaki `GET protokol/{id}/davetler`
  bu dökümü mobil için zaten üretiyordu, webde karşılığı yoktu.
- `ekranlar/protokol/KartCiktisi.tsx` — kesme kartları penceresi. Serbest
  sayı yerine **hazır ölçüler**: kullanıcı "kaç kolon kaç satır" diye değil,
  "koltuk etiketi mi masa isimliği mi" diye düşünüyor. Her seçeneğin yanında
  karta düşen yaklaşık boy ve küçük bir ızgara önizlemesi var.

## Yatay taşmanın iki sessiz kaynağı

- **`overflow-wrap: break-word` EN-KÜÇÜK-İÇERİK ÖLÇÜSÜNÜ değiştirmez.** Metin
  görsel olarak bölünür ama esnek/ızgara öğesi hâlâ bölünmemiş genişliği
  talep eder. Sistem hatası detayında yığın izindeki dosya yolu belgeyi
  390px'ten **628px**'e çıkarıyordu — sayfanın tamamı yatay kayıyor,
  kullanıcının "menüler sağa taşıyor, düzen bozuluyor" dediği şey buydu.
  Uzun boşluksuz metinde **`wrap-anywhere`** kullan.
- **Izgara/esnek hücresinin varsayılan en küçük genişliği `auto`'dur.** İçerik
  hücreyi iter ve etki yukarı yayılır; `min-w-0` (ya da `[&>*]:min-w-0`)
  olmadan hiçbir `truncate`/`wrap` kuralı kurtarmaz.

## İÇ İÇE BAĞLANTI OLMAZ

`Liste`'ye `bagla` verildiğinde satırın tamamı `<a>` olur; hücrelerin içinde
ayrıca `<Link>` ya da `tel:` bağlantısı bırakmak `<a>` içinde `<a>` üretir.
Bu geçersiz HTML ve **tarayıcı davranışı tanımsız**: çiçekçi listesinde satıra
dokunmak mobilde hiçbir şey yapmıyordu. Tarama yolu: sayfada
`document.querySelectorAll('a a')`.

> Aynı aile: **`asChild` prop yaymayan bileşeni sarmaz.** Radix
> `<Dialog.Close asChild>` kapatma işleyicisini `onClick` olarak çocuğa
> geçiriyor; çocuk yalnızca kendi adlandırılmış proplarını okuyorsa işleyici
> sessizce düşer. Resim görüntüleyicinin kapat düğmesi bu yüzden çalışmıyordu
> ve mobilde `Esc` olmadığı için pencere kilitleniyordu.

## Katman içindeki kartlar dış yarıçapı izler

İç içe yuvarlatmada doğru oran **iç = dış − aradaki boşluk**. Kart her yerde
`--r-lg`; bir diyaloğun içinde dış köşe `--r-xl` ve arada `--sp-3` var.
Yarıçap knob'u 18'de pencere 36px, kartlar 25px oluyordu — kartlar dış
köşeden daha yuvarlak görünüyor, "balonun içinde balon" gibi duruyordu.
Kural `globals.css` sonunda ve **katmansız** yazılı: Tailwind yardımcıları
`@layer utilities` içinde, katmansız kural onları geçiyor.

## Satır eylemleri: DÖRT düğme yerine İKİ GRUP

Halk günü katılım satırı, protokol satırı ve davet satırı aynı hastalığı
taşıyordu: dört-beş ayrı kenarlıklı ikon düğmesi yan yana. 390px'lik ekranda
150–180px yiyorlar ve kalan yere asıl veri sığmıyor — isim "Ahm..." diye
kırpılıyor, telefon iki satıra bölünüyor, unvan üç satıra iniyordu.

- `SatirEylemleri` artık **dikey** de olabilir (`yon="dikey"`). Yalnızca
  yukarı/aşağı gibi **eksen taşıyan** ikili için: yan yana dururken hangisinin
  yukarı taşıdığını ok söylüyor ama üst üste dizilince düğmenin **yeri** de
  söylüyor — ve satırdan 44px genişlik kazanılıyor.
- Kalan eylemler ikinci bir grupta. İki grup toplam ~90px; kazanılan yer
  doğrudan isme/unvana gidiyor.

**Davet detayında satır tamamen okunur hâle geldi**: "Arandı" hapı, "Mesaj"
hapı, cevap `<select>`'i, not ve çıkarma düğmeleri satırdan çıkıp **kişinin
kendi alt tabakasına** taşındı. Satırda yalnızca ad, unvan·kurum, not, cevap
çipi ve arandı/mesaj için iki küçük simge (yapılmışsa dolu, değilse sönük)
kalıyor. Sayfa 1652px → 950px.

> Ölçüm (390px, mobil): halk günü detayı 1913 → 1849px ve ilk ekranda artık
> vatandaş adları görünüyor · davet detayı 1652 → 950px · protokol 1645 →
> 1169px · vatandaş havuzu dört sıra denetimden tek arama kutusuna indi.

## Mobil appbar ve bildirim merkezi

Şerit **altı eylem** taşır: kurulum · yardım · bildirim · Tema Tasarımcısı ·
gece/gündüz · çıkış. Yer açan şey düğmeleri kaldırmak değil, **künye
satırıydı**: başlığın altında her ekranda tekrar eden "Admin · Belediye
Başkanlığı" duruyordu ve hiçbir işe yaramıyordu (aynı bilgi Menü tabakasının
tepesinde, avatarıyla birlikte). Kaldırılınca başlık iki punto büyüdü ve
tek satırlık, okunur bir başlığa dönüştü.

390px'e sığsınlar diye mobilde aralık `gap-1`, kenar boşluğu `px-3`
(masaüstünde eski ritim). Ölçüm: 390px'te altı düğmeyle başlık 119px, beşle
161px — kırpılma ve taşma yok. Kurulum düğmesi zaten **geçici**: uygulama
kurulunca kaybolur ve şerit beşe döner.

> **Hepsi `IkonButon`** — 38×38, kenarlıklı, `rounded-control`. Yardım düğmesi
> bir dönem kendi ölçüsünü uyduruyordu (34px, kenarlıksız, `lg` üstünde
> yanında "Yardım" yazısı): şerit tek bir düğme yüzünden hizasız görünüyor ve
> yazı masaüstünde 46px yiyordu. Ekranın adı zaten `title`/`aria-label`da.
> Ölçüm: bütün şerit düğmeleri artık 38×38.

> Bir ara denendi ve GERİ ALINDI: tema araçlarını ve çıkışı tamamen Menü
> tabakasına indirmek. Şerit sadeleşiyor ama günde birkaç kez yapılan
> gece/gündüz geçişi iki dokunuş arkasına gidiyor. Ölçüt "kaç düğme" değil,
> **hangi eylem ne sıklıkta**. Aynı seçenekler menüde de duruyor — araç
> çubuğu ile menü arasındaki bu tekrar native uygulamalarda olağan.

**Bildirim merkezi** mobilde `AltSheet`, masaüstünde Popover. Zile bağlı
yüzen kart telefonda ekranın tamamını kaplıyor ama ekranın kendisi olmuyordu:
köşeleri havada, satırları fareye göre ölçülü (36px) ve silme düğmesi yalnızca
`hover`'da — dokunmatikte hiç. Tabakada satırlar `ListeSatiri`, okunmamışlık
ayrı bir nokta yerine **çipin rengiyle** taşınıyor.

## Detay sayfaları: eylemler FAB'da

Etkinlik ve talep detayında "Düzenle / Havale et / Diğer ▾" düğme sırası
mobilde `bilesenler/EylemTabakasi.tsx` ile **sağ alttaki FAB**'a taşındı;
dokununca 56px'lik satırlardan oluşan bir alt tabaka açılıyor. Masaüstünde
düğme sırası aynen duruyor. **Eylem listesi TEK yerde** kurulur ve iki dal da
onu okur — izin koşulları iki kez yazılsaydı biri unutulduğunda kullanıcı
çalışmayacak bir eylem görürdü.

- **Kapalı eylem gizlenmez**, sönük çizilir ve sebebi satırın altında yazar
  ("Gizli etkinlik havale edilemez"). Masaüstünde bu bilgi `title`'daydı;
  dokunmatikte `title` hiç görünmüyor.
- **Not ekleme** `bilesenler/NotEkleme.tsx`: mobilde tabaka, masaüstünde
  satır içi kart. Kutu notlar sekmesinin tepesinde 140px yer kaplıyordu ve
  yazarken açılan klavye altındaki notları tamamen örtüyordu.
- **Talep edeni MOBİLDE ÜSTTE**: yan sütun kartı tek sütuna düşünce dört
  sekmenin bütün içeriğinin ALTINA kayıyordu — talebin asıl verisi ekranın en
  görünmez yerindeydi. Şimdi başlığın altında, baş harf çipi + ad + telefon
  ve **arama düğmesiyle**.
- **İç içe açılır menü mobilde kullanılmaz.** Yazıcı menüsünün alt menüsü
  ekranın 96px dışına taşıyordu ve düzeltilemiyor: Radix alt menüyü yalnızca
  sağdan sola çeviriyor, yatayda görünür alana kaydırmıyor. Mobilde iki
  kademeli bir alt tabaka var.

> **Katman sırası:** appbar `z-30`, alt sekme çubuğu `z-20`, FAB ve menüsü
> `z-40`, alt tabakalar/diyaloglar `z-50`. FAB perdesi bir dönem `z-20`'ydi
> ve menü açıkken başlık ile sekmeler perdenin ÜSTÜNDE kalıyordu.

## Katman gramerı: tek kap

`bilesenler/TabakaKabi.tsx` — üstte açılan HER pencerenin kabuğu. Mobilde
alttan gelen bir tabaka (`vaul`), masaüstünde ortalanmış bir pencere (Radix
Dialog); perde, köşe yarıçapı, tutamak, başlık şeridi ve kapatma düğmesi tek
yerde. `FormModal` ve `EtkinlikModal` ikisi de bunun üstünde.

Ayrılmasının sebebi ayrışmasıydı: etkinlik penceresi ham Radix Dialog + elle
yazılmış CSS animasyonuyla kalmıştı, mobilde tabaka gibi davranmıyor,
parmakla kapanmıyordu.

> **Açılışta odak bir metin alanına DÜŞMEZ.** Radix açılan katmandaki ilk
> odaklanabilir öğeye gider; o bir metin alanıysa telefon klavyeyi kaldırır,
> görünür alan küçülür ve `vaul` tabakayı **giriş animasyonunun ortasında**
> yeniden konumlandırır — kullanıcının gördüğü şey titremedir. Formu olmayan
> tabakalar (halk günü menüsü) bu yüzden hep daha düzgün görünüyordu.
> `Girdi`/`CokSatirli` de mobilde `autoFocus`u yok sayar: klavyenin formun
> yarısını kullanıcı daha bakmadan kapatması kendi başına da yanlış.
> Masaüstünde ilk alan odaklı kalır.

## Form diyalogları

`bilesenler/FormModal.tsx` — `TabakaKabi`'na **form düzenini** ekler (kayan
gövde + sabit eylem çubuğu). **Web'deki bütün formlar** diyalogda açılır:
protokol, davet, talep, kullanıcı, parola sıfırlama, birim, tanım, toplu
ekleme, çiçekçi, dosya gönderme. Liste ekranı arkada durur. Önceden tam sayfaya geçiliyordu
ve kaydettikten sonra kullanıcı listedeki yerini (sayfa, süzgeç) kaybediyordu.

Gövde kayar, **alt çubuk sabit kalır**. Bu şart: talep formu uzun ve mobilde
klavye açıkken kaydet düğmesi ekran dışında kalıyordu — daha önce bu yüzden
talep formu bilinçli olarak tam sayfa yapılmıştı.

**Dışarı tıklayınca kapanmaz** (`onInteractOutside` engelli): yarısı
doldurulmuş formu yanlış bir tıkla kaybetmek diyalogla çalışmanın en sinir
bozucu tarafıydı.

Davet oluşturulunca **doğrudan detayına gidilir** — boş bir davet listede tek
başına işe yaramıyor, bir sonraki adım her zaman kişi eklemek.

## Birim her yerde YETKİLİSİYLE anılır

`data/format.ts` → `unitLabel(birim)` = `"Ad — Yetkili"`. Kurumda **altı ayrı
"Başkan Yardımcısı" birimi** var; yalnızca adla listelendiğinde hangisinin
seçildiği anlaşılmıyordu. Kural birim adının göründüğü **her yerde** geçerli:
kullanıcı formu, talep formu, talep detayı, havale seçimleri, SMS birim
listesi, silme onayı. Ayrı bir "Yetkili" satırı zaten gösteren yerler (birim
detayı, katılımcı seçici) olduğu gibi kalır — orada bilgi ikinci satırda.

## Anahtar (switch) ve akordiyon

`bilesenler/Anahtar.tsx` onay kutusunun yerini aldı: yerleşik
`<input type="checkbox">` görünümü tarayıcıya göre değişiyordu (Safari'de
`accent-color` farklı) ve dokunma hedefi 16px kalıyordu. Etiketin tamamı
tıklanabilir. `ton="uyari"` durum rengini kırmızıya çevirir.

> Çoklu-seçim listeleri (SMS'te birim seçimi, rol atama) onay kutusu olarak
> KALDI — onlar aç/kapa değil, liste işaretlemesi. Ayrım şu: **tek bir şeyi
> açıp kapatıyorsan anahtar, bir listeden işaretliyorsan onay kutusu.**

`bilesenler/Akordiyon.tsx` — yükseklik `height: auto`ya canlandırılamadığı
için Radix'in ölçtüğü `--radix-accordion-content-height` kullanılır. Başlıktaki
eylem düğmesi tetikleyicinin DIŞINDA: içinde olsaydı düğmeye basmak bölümü de
açıp kapatırdı.

## Araç çubukları tek satır

Ajanda dört sıra denetimle açılıyordu (sekmeler · arama+gezinme · çıktı
düğmeleri · tip çipleri) ve program mobilde ilk ekrana hiç girmiyordu.
Talepler'de de ikinci bir "ikincil eylemler" satırı vardı.

- **Çıktılar tek düğmede**: `ekranlar/ajanda/ProgramMenusu.tsx` artık günlük
  program tasarımlarının yanında Excel/PDF dışa aktarımını da taşıyor. Çıktı
  almak günde bir yapılan bir iş; her an görünür durmasına gerek yok.
- **Tip süzgeci çip şeridi değil menü** (`bilesenler/SecimMenusu.tsx`).
  Seçiliyken düğme marka rengine döner — açık kalmış bir süzgeci fark
  etmemek, "kayıtlarım kayboldu" diye gelen sorunun bir numaralı sebebiydi.
- **Gezinmede tarih aralığı ortada yazılı.** Önce sağda, yalnızca `lg`
  üstünde görünen bir metindi; mobilde ileri geri gidip hangi haftada
  olduğunu kimse bilmiyordu. "Bugün" düğmesi de yalnızca bugünden uzaktayken
  çıkıyor.
- Taleplerde çıktı ikilisi (`bilesenler/DisaAktar.tsx`) kapsam seçiminin
  soluna alındı. Etiketler her boyutta yazılı: iki ikon da "belge" silüeti,
  yan yana ayırt edilmiyordu.

## Talepten etkinliğe

`ekranlar/talep/AjandayaEkleModal.tsx` — tarih, saat, etkinlik durumu ve
hazırlık bayraklarıyla. Web eskiden doğrudan `{ randevuId }` gönderiyor, tarih
hiç geçmiyordu: sunucudaki `BaslangicTarih` **0001-01-01** kalıyor ve etkinlik
takvimde ulaşılamayacak bir yere düşüyordu. Varsayılan **yarın 09:00** —
geçmişe etkinlik açmak neredeyse hiç istenmiyor.

Kaydedince oluşan etkinliğe gidilir (uç `etkinlikId` döner).

Talepler listesinde **tarih sıralaması** (tek düğme, yön çevirir) ve
**"Ajandada"** süzgeci var; asıl işe yarayan bileşim "Onaylandı + ajandaya
eklenmemiş", yani sırada bekleyen iş. Süzgeç açıkken durum çipleri de o alt
kümeyi sayar.

## Çiçekçiler listede

Kart ızgarası değil `Liste`. İki çiçekçi kaydı üç sütunluk ızgarada iki küçük
kutu olarak duruyor, ekranın kalanı boş kalıyordu. Kayıtlar birkaç satır ve
hepsi aynı alanları taşıyor (ad, telefon, adres, durum) — bu bir tablo işi.
Mobilde `Liste` zaten kart görünümüne dönüyor.

## Kullanıcı bloğu ve çıkış

Kenar çubuğunun **altında sabit**: kim olarak giriş yapıldığı + çıkış. Çıkış
yalnızca Ayarlar ekranının dibindeydi ve kullanıcı çıkmak için önce menüden
ayarları bulmak zorundaydı. Mobil görünümde kenar çubuğu yok, o yüzden çıkış
**appbar'da** (`md:hidden`).

## Ajanda: Program ve Liste AYNI pencereyi paylaşır

İki sekme iki ayrı aralık kuruyordu — Program 2 hafta, Liste 6 ay — ve arayüzde
bunu söyleyen bir şey yoktu. Sonuç: aynı ekranda "Program: 1 etkinlik / Liste: 2
etkinlik". Sekme kaydın **görünümünü** değiştirir (güne göre gruplu program ya
da tablo), **kümesini** değil.

Aralık ve sayı (`N etkinlik · gg.aa.yyyy – gg.aa.yyyy`) her iki sekmede de
yazılı, gezinme de her ikisinde. Pencereyi gizlemek, sayı farkını
açıklanamaz kılıyordu.

## Çıkışta önbellek SONRA temizlenir

`queryClient.clear()` doğrudan çıkış geri çağrısında çağrılınca ekran hâlâ
monteli oluyor: React `setBen(null)`'ı geri çağrı bittikten sonra işliyor,
`clear()` ise hemen çalışıp o an bağlı sorguları YENİDEN çağırıyor. Jeton
çoktan silindiği için istek 401 dönüyor ve kullanıcı çıkarken "Etkinlikler
yüklenemedi" görüyordu.

Temizlik artık `ben === null` olduğunda çalışan bir etkide (effect) —
yani `ProtectedRoute` kullanıcıyı giriş ekranına aldıktan sonra. Uçuştaki
istekler ayrıca `cancelQueries()` ile iptal edilir.

## Silinmiş etkinlikler

Liste yalnızca etkinliğin KENDİ tarihini gösteriyordu; ekrana bakan
"bunlar silinmiş mi, yoksa geçmiş etkinlikler mi?" diye ayırt edemiyordu.
Artık ilk sütun **silinme tarihi** (`silinmeTarihi`, kırmızı) ve sıralama da
ona göre.

## Etkinlik detayı

- **Tarih ve saat büyük punto.** Bu ekranın tek sorusu "ne zaman"; önceden
  diğer alanlarla aynı boyuttaydı.
- **"Tamamlandı" düğmesi kaldırıldı.** En görünür, en yeşil düğmeydi ve tek
  işi statü değiştirmekti; kullanıcılar düzenlemek isterken ona basıp
  etkinliği yanlışlıkla kapatıyordu. Statü artık "Diğer" menüsünden.
- **Katılımcılar ve kayıt bilgileri kapalı akordiyonda.** Ekranın asıl işi
  etkinliğin kendisi.
- **Katılımcılar BİRİM** ve detaydan eklenebilir (`+` düğmesi → diyalog).
  Liste sunucudan gelir ve yalnızca kullanıcının kendi seviyesindeki ve
  altındaki birimleri içerir.
- **Hazırlık rozetleri üç durumlu.** Önceki davranış yanıltıcıydı: "konuşma
  metni hazırlanacak" işaretliyse rozet, metin yazılmamış olsa bile yeşil
  çıkıyordu — kutucuk "hazırlanacak", rozet "hazır" diyordu. Artık
  istenmemiş (rozet yok) · istenmiş ama boş (**kırmızı**, tıklayınca yazılır)
  · hazır (yeşil, tıklayınca düzenlenir).

## Izgara ve esnek kutuda `min-w-0`

Izgara/esnek kutu öğelerinin varsayılan en küçük genişliği `auto`dur: içeriğin
min-content genişliğinin altına inemezler. İçeride `truncate` (yani
`white-space: nowrap`) bir başlık varsa o min-content **metnin tamamı** kadar
olur. Ana sayfada uzun başlıklı bir kart 814px'e şişip sayfayı yatay
kaydırılır hâle getiriyordu.

Bunun mobilde ikinci bir bedeli var: düzen alanı görüş alanından geniş olunca
**`position: fixed` alt çubuk da** düzen alanına göre konumlanır ve ekranın
altından kayıp gider. "Alt sekme kayboluyor" ile "başlık taşıyor" aynı hatanın
iki yüzüydü.

`Kart` artık `min-w-0` taşıyor. Yeni bir kap bileşeni yazarken aynısını yap;
görsel tur yatay taşmayı yakalıyor ama **yalnızca o veriyle** — kısa başlıklı
tohum veride hata görünmüyordu.

## Odak halkası

`--focus-ring` tokenı (`tokenlar.css`). Sınıflarda `ring-[--focus-ring]` diye
çağrılıyordu ama **token hiç tanımlı değildi**: çözümlenemeyen renk alan halka
çizilmiyor, odaklanan alan hiçbir şeyle belli olmuyordu.

> **Kenarlık rengini satır içi `style` ile verme.** Satır içi stil her zaman
> sınıflardan üstündür; giriş ekranındaki `style={{ borderColor }}`,
> `focus:border-brand-2`'yi sessizce eziyordu. Hata durumu da sınıfla
> veriliyor (`border-[--st-no]`).

> Başsız tarayıcıda `:focus` ölçmek: sayfa "odaklı" sayılmadığı için
> `element.focus()` + `getComputedStyle` halkayı GÖSTERMEZ.
> `Emulation.setFocusEmulationEnabled` + `Input.dispatchMouseEvent` ile
> gerçek tıklama gerekiyor.

## Zaman — en sessiz hata kaynağı

Sunucudaki **her** damga `timestamp without time zone`; saat dilimi taşımaz.
`toISOString()` **YASAK** — Türkiye'de bütün etkinlikleri 3 saat kaydırır ve
sunucu hatası gibi görünür. Dönüşüm yalnızca `data/time.ts` üzerinden:
`serverToLocal()` / `localToServer()`. Ekrana basmak için `data/format.ts`.


## Arayüz izne göre kısıtlanır

`src/bilesenler/izin.ts` sunucudaki katalogun aynası (test kilitler).
`useSession().hasPermission(izin)` — tek ad ya da **dizi (VEYA)**; sunucudaki
`[Izin(...)]` de böyle çalışıyor.

Üç katman da izne bakar ve **rol adına bakmaz**:
- **Menü** (`kabuk/gezinme.ts` + `AppShell`) — öğe `izin` ilan eder.
- **Rota** (`ProtectedRoute permission={...}`) — menüde görünen her ekrana
  girilebilmeli; ikisi ayrışırsa kullanıcı tıklayıp duvara çarpar.
- **Ekran içi eylem düğmeleri** — `hasPermission(IZIN.x) && <Buton…>`.

`politika` ve `rol` alanları **yalnızca geri düşüş**: sunucu izin listesi
göndermezse devreye girer. Kaldırmak, o yolda Yönetim menüsünü herkese
açardı.

> **Boş durum düğmelerini unutma.** Araç çubuğundaki "Yeni etkinlik" izin
> kapısındaydı ama liste BOŞKEN çizilen ikinci düğme kapının dışındaydı —
> yetkisi olmayan kullanıcının gördüğü tek düğme korumasız olandı.
> `test/izin.test.tsx` kaynağı tarayıp korumasız ekleme düğmesi arıyor.

> **Oturumu taklit eden testler `iznimVar`ı da vermeli** ve çoklu izni VEYA
> ile işlemeli. Eksik bir taklit, o alanı hiç okumayan ekran testlerini de
> "düğme bulunamadı" ile düşürüyor ve hangisinin bozuk olduğu anlaşılmıyor.

Kullanıcı formundaki "gizli etkinlik ekleyebilir" / "dosya gönderebilir"
anahtarları **kaldırıldı**; yetki artık rolün izinlerinden geliyor.

Ajanda ekranları iki izinden biriyle açılır
(`[ajandaGoruntule, ajandaBasinGoruntule]`): basın kullanıcısında tam
görüntüleme izni yok, listeyi **sunucu** daraltıyor.

### Yetki matrisi turu

```bash
node test/gorsel/yetki-turu.mjs   # sunucu ayakta olmalı
```
Yedi rolle tek tek giriş yapar, her ekranı masaüstü + 390px gezer, menüyü ve
eylem düğmelerini ÖLÇER (`/tmp/workcollab-yetki/rapor.json`). Gözle
karşılaştırmak on ekran × yedi rol için güvenilir değil.

## Test

```bash
npm test                       # vitest: birim + ekran testleri (jsdom)
node test/gorsel/tur.mjs       # gerçek Chrome ile görsel tur (sunucu ayakta olmalı)
```

Görsel tur her ekranı **masaüstü + 390px**, **açık + koyu** temada gezer;
yatay taşma ve JavaScript hatası bulursa çıkış kodu 1 döner. Ekran görüntüleri
`/tmp/workcollab-gorsel/` altına yazılır. Playwright/Puppeteer **kurulmadı** —
sistemdeki Chrome, CDP ile sürülüyor (`test/gorsel/cdp.mjs`, ~200 satır).

> Kare, `animasyonBitsin()` ile **canlandırma bitince** alınır. Bekleme koşulu
> (metin/seçici) sağlanır sağlanmaz çekilen görüntüde diyaloglar yarı saydam
> çıkıyor ve tasarım "soluk" görünüyordu — ekranda olmayan bir hata aratıyordu.

> Radix menüleri `element.click()` ile açılmaz (bileşen `pointerdown`
> dinliyor); `t.tikla(x, y)` gerçek fare olayı gönderir.

### Test dosyaları ve neyi kilitliyorlar

| Dosya | Kilitlediği |
|---|---|
| `tokenlar.test.ts` | `@theme` eşlemeleri ve `bg-(--x)` okumaları gerçekten tanımlı mı; kendine referans veren token |
| `izin.test.tsx` | Menü/rota/düğme izin kapıları; korumasız ekleme düğmesi taraması |
| `yardim.test.tsx` | Her ekranın yardımı var mı; kalıp sırası; **gezilen her yolun rotası var mı** |
| `tekrar.test.ts` | RRULE gidiş-dönüş kaybı; `BYDAY` yalnızca seçildiyse |
| `istemci.test.ts` | 401 ayrımı — jetonsuz istek "oturum düştü" değildir |
| `pwa.test.ts` | Kurulum durum makinesi; **kaldırılınca düğme geri gelir** |
| `toast.test.tsx` | Şeride dokunma gezindiriyor mu; kaydırma gezinme sayılmıyor mu |
| `ekranlar.test.tsx` | Ekranların gerçek ağaçla çizilmesi (fetch taklidi) |

**Kaynak tarayan testler bilinçli.** Bazı hatalar çalışma anında değil
*yazımda* var: olmayan bir rotaya giden bağlantı, izin kapısı olmayan bir
düğme, tanımsız bir token. Bunları yakalamanın tek ucuz yolu dosyayı okumak.

### Ölçüm önce, iddia sonra

Bu depoda "düzelttim" demenin şartı **ölçüm**. Sırasıyla: hatayı üret, sayıyı
al, düzelt, sayıyı tekrar al, ikisini de `CLAUDE.md`'ye yaz. Yerleşik yollar:

- Yatay taşma: `document.documentElement.scrollWidth > clientWidth`
- İç içe etkileşim: `document.querySelectorAll('a a, button button').length`
- Satır/kutu yüksekliği: `getBoundingClientRect()`
- Dokunmatik davranış: `Emulation.setTouchEmulationEnabled` (yoksa
  `pointer: coarse` kuralları hiç uygulanmaz)
- Odak halkası: `Emulation.setFocusEmulationEnabled` + gerçek tıklama;
  başsız tarayıcıda `element.focus()` halkayı GÖSTERMEZ

## Erişilebilirlik

`design.md` §11 bağlayıcı: her ikon butonda `aria-label` + `title`, tıklanabilir tablo satırları klavyeyle erişilebilir ve `Enter` ile açılır, `:focus-visible` halkası `2px --brand-2`, `prefers-reduced-motion` altında geçişler kapanır, alt sayfa `Esc` ve overlay ile kapanır, yükleme için iskelet.

## Mobil listeler SIKI, ajanda kart

`bilesenler/Liste.tsx` mobilde satırları tek yüzeyde, saç teli çizgilerle
ayrılmış olarak çizer (`mobilGorunum="liste"`, varsayılan). Önceki hâlde her
satır çerçeveli, gölgeli, 14px iç boşluklu bir kutuydu ve etiket/değer
ızgarası taşıyordu: telefonda ekrana üç satır sığıyordu.

**Ajanda bunun DIŞINDA** (`mobilGorunum="kart"`): oradaki satır bir etkinliğin
kartı — renk şeridi, saat bloğu ve hazırlık rozetleriyle birlikte anlam
taşıyor.

> Araç çubukları dar ekranda **sarılmalı**. Taleplerde beş denetim tek satıra
> sığmıyor ve birbirinin üstüne biniyordu; yatay taşma ölçümü bunu YAKALAMIYOR
> çünkü öğeler genişliği itmek yerine üst üste biniyor. Kapsam seçimi mobilde
> kendi satırında.

## Yardım sistemi

`src/yardim/` — kullanıcıya dönük **ekran yardımları**. Üç parça:

| Dosya | İş |
|---|---|
| `metinler/*.md` | Ekran başına bir yardım metni (21 dosya) |
| `katalog.ts` | Rota kalıbı → metin eşlemesi (`?raw` ile derlemeye gömülür) |
| `YardimDugmesi.tsx` | Üst çubuktaki düğme; o anki rotanın metnini açar |
| `YardimPaneli.tsx` | Masaüstünde SAĞDAN, telefonda alttan açılan tabaka |
| `Markdown.tsx` | Küçük çizici — başlık, liste, tablo, alıntı, satır içi biçim |

Kararlar:

- **Düğme her sayfada AYNI yerde**, içerik sayfaya özel. Her ekranın içine
  ayrı düğme koymak, kullanıcının yardımı her sayfada yeniden aramasına yol
  açardı; sabit yer öğrenilir.
- **Yardımı olmayan sayfada düğme çizilmez.** Boş panel açan bir düğme
  "yardım yok" demenin en kötü yolu.
- **Markdown kütüphanesi EKLENMEDİ**: metinler bizim yazdığımız denetimli
  dosyalar, rastgele kullanıcı girdisi değil. Çizici dar bir alt küme
  destekler; tanımadığı söz dizimini düz metin olarak basar, sayfa hiçbir
  durumda boşalmaz.
- Kaynak dosyalarda satırlar 80 sütuna sarılı; çizici **madde devam
  satırlarını** aynı maddeye ekler — yoksa cümle ortadan ikiye bölünüyordu.
- Panel sağa yaslı olduğu için masaüstünde `anim-yan` (sağdan kayma)
  kullanılır; ortadan büyüyen `anim-tabaka` kenara yapışık bir panelde yanlış
  yerden geliyormuş gibi duruyordu.

> Katalogda **sıra önemli**: `/halk-gunu/basvurular` kaydı `/halk-gunu/:id`
> kalıbından ÖNCE gelmeli, yoksa vatandaş havuzunda gün ayrıntısının yardımı
> açılır. `test/yardim.test.tsx` hem bunu hem de menüdeki her ekranın yardımı
> olduğunu kilitler.

### Yardım Merkezi ve ekranı olmayan konular

`/yardim` → `yardim/YardimMerkezi.tsx`. Menüdeki "Yardım" satırı bu adrese
gidiyordu ama **rota hiç tanımlanmamıştı**: düğmeye basan kullanıcı "Sayfa
bulunamadı" görüyordu. Ölü bağlantı derlemede de testte de görünmez — geçerli
bir dizedir.

- Konular **menüyle aynı gruplarda** (`grup` alanı zorunlu): kullanıcı yardımı,
  ekranı menüde aradığı yerde arıyor.
- Arama **metnin içine de bakar** — "kesme kartı nerede?" diye arayan kişi o
  kelimenin geçtiği yardım metnini bulur.
- **`kalip: ''` = ekranı olmayan konu.** Uygulamanın tamamında geçerli
  davranışlar (`Telefonda Kullanım`, `Kurulum ve Bildirimler`) böyle yazılı:
  hiçbir yolla eşleşmez, yalnızca merkezde listelenir. Alternatif, aynı
  paragrafı yirmi bir dosyada güncel tutmaktı.
- Merkez **izin kapısı taşımaz**: yardım bir yetki değil açıklama; kapatmak,
  ekranı göremeyen kişinin "bu ekran ne işe yarıyordu?" sorusunu da cevapsız
  bırakırdı.

> **Bekçi: `test/yardim.test.tsx` → "gezinme hedefleri".** Kaynak taranıp
> `MobilMenu`'deki `git('/...')` çağrıları ve `GEZINME`'deki `yol` alanları
> `App.tsx`'teki rota tablosuyla karşılaştırılıyor. Menüde görünüp rotası
> olmayan bir hedef artık testi düşürüyor.

## Menü grupları işe göre

`GENEL` gün içinde en çok açılan üçlüyle başlar: **Ana Sayfa · Ajanda ·
Takvim**, sonra Talepler ve Dosya Gönderimi. Ajanda bir dönem "Program"
başlığı altında, taleplerin ALTINDA duruyordu — oysa makamın günü ajandayla
başlıyor ve takvim onun başka bir görünümü.

**Halk Günü** ve **Özgeçmişler** kendi gruplarında: ikisi de ayrı birer modül
ve halk gününün iki ekranı ardışık iki iş — vatandaş havuza yazılır, sonra bir
güne atanır. Bu yüzden grupta **Vatandaş Havuzu önce** gelir.

> `haric` alanı: `/halk-gunu/basvurular` hem "Halk Günleri" öğesinin
> `altYollar` kapsamında hem de kendi menü öğesi. Dışlanmasaydı o sayfada iki
> öğe birden vurgulanırdı (`aktifMi`).

## Çıkış perdesi

Çıkış iki ağ çağrısı yapıyor (web push jetonunu sil + denetim kaydı) ve o
süre boyunca ekranda hiçbir şey değişmiyordu: kullanıcı düğmeye basıyor, bir
şey olmuyor, tekrar basıyordu. Artık yumuşak bir perde + "Çıkış yapılıyor…"
göstergesi var; ikinci tıklamayı da engelliyor. Yalnızca opaklık canlanıyor.

## Alt çubuk sırası menüden bağımsız

`GezinmeOgesi.tabbar` bir sayı: hem "görünsün mü" hem SIRA. Menüde Halk Günü,
Talepler'in altında (ikisi de vatandaştan gelen iş) ama alt çubukta sıra
**Ana Sayfa · Ajanda · Talepler · Halk Günü** — gün içinde en sık açılan ekran
önce.

## Üst üste açılan katmanlar

Perdeler `z-40`, içerikler `z-50` idi. İkinci bir diyalog açıldığında onun
**perdesi birincinin içeriğinin altında** kalıyordu: iki pencere iç içe
görünüyor, karartma en altta duruyor, hangisinin etkin olduğu anlaşılmıyordu.
Mobilde alttan açılan tabakalarda daha da belirgindi.

Kural artık tek satır: **diyalog perdesi ve içeriği aynı `z-50`**. İkisi de
aynı portalda ve Radix perdeyi önce çizdiği için içerik kendi perdesinin
üstünde; farklı diyaloglar arasındaki sırayı **DOM sırası** belirler ve
sonra açılan portal her zaman sonra eklenir, yani üstte kalır.

- Menü / açılır seçici / ipucu: `z-[400]` — bir diyaloğun içinden açılsa bile
  üstte olmalı.
- Bildirim şeridi: `z-[500]` — "kaydedildi" mesajının açık bir diyaloğun
  altında kalması, kullanıcıya işlemi tekrar yaptırıyordu.
- Kabuk: appbar `z-30`, alt çubuk `z-20`.

## Animasyonlar bileşik katmanda

Yalnızca `transform` ve `opacity` canlandırılır — ikisi de düzen hesabı
gerektirmez. Buna ek olarak:

- `.anim-katman`, `.anim-alt`, `.anim-tabaka` → `will-change: transform,
  opacity` + `backface-visibility: hidden`.
- Mobilde (`max-width: 767px`) alt tabakalar ayrıca `transform: translateZ(0)`
  ile kendi katmanına çıkar. **Masaüstünde uygulanmaz**: ortalanmış diyalogda
  Tailwind'in `-translate-x-1/2` değerini ezip pencereyi sağ alta kaydırıyor.
- Perde de `will-change: opacity` + `translateZ(0)`: karartma canlanırken
  altındaki sayfa yeniden boyanıyordu.
- Mobil tabaka 300ms açılır / 220ms kapanır (`cubic-bezier(0.16, 1, 0.3, 1)`),
  masaüstü penceresi 200/140ms ile **aynen** kalır.

**Sürükleyerek kapatma React'i uyandırmaz** (`bilesenler/kaydirmaKapat.ts`):
her `pointermove` bir `setState` tetiklediğinde uzun form yeniden çiziliyor ve
sürükleme takılıyordu. Artık `transform` doğrudan öğeye yazılıyor,
`requestAnimationFrame` ile kareye bağlanıyor. Bırakınca eşiğin altındaysa
açılış eğrisiyle **yerine yaylanır** (önce anında zıplıyordu); üstündeyse önce
aşağı kayıp kaybolur, kapanma çağrısı 180ms sonra gelir — yoksa Radix'in
kapanış animasyonu tabakayı bir an yukarı çekiyordu.

## Özgeçmiş havuzu

`ekranlar/Ozgecmisler.tsx` — iki kaynak (doğrudan yüklenenler + **iş
taleplerinden** gelenler) tek listede; hangisinin nereden geldiği satırdaki
rozetten belli (talepten gelen ALTIN çerçeveli).

- **Süzgeçler alt tabakada** (`FormModal`, mobilde bottom sheet): meslek,
  mahalle, tarih aralığı, "bana paylaşılanlar". Araç çubuğuna dizmek 390px'te
  üçüncü satırı doğuruyor ve liste ilk ekrandan çıkıyordu. Seçilenler listenin
  üstünde **çip** olarak kalır — kapalı bir tabakadaki süzgeç, "kayıtlarım
  kayboldu" diye gelen sorunun bir numaralı sebebi.
- **Form `FormData` ile tek istekte gider** (`fetch`, `api` sarmalayıcısı
  değil): alanlar ve dosya birlikte. Önce kaydı açıp sonra dosya yüklemek,
  ikinci adım düşünce havuzda dosyasız kayıt bırakıyordu.
- **Paylaşım** kişilere yönlendirir ve alıcı bildirim alır; liste
  `gonderim/alicilar` ucundan gelir — aynı soru, aynı liste.

Ekran beş dosyaya bölündü (`ekranlar/ozgecmis/`): `OzgecmisTabakasi` (kayıt
tabakası) · `OzgecmisFormu` · `PaylasimPenceresi` · `OzgecmisSuzgeci` ·
`KaynakRozeti`. 845 satırlık tek dosyada süzgeç tabakasının adı da
`SuzgecTabakasi`ydı ve `bilesenler/SuzgecTabakasi`yi gölgeliyordu.

- **Mobil satır `Liste`'nin genel dalını KULLANMAZ.** O dal başlık/açıklamanın
  yanında bütün sütunları da basıyordu: ad iki kez, telefon iki kez, kaynak
  rozeti iki kez ve dört ayrı ikon düğmesi metnin altında havada. Ölçüm:
  satır 180 → 107px, iç içe düğme 0. (Genel kural: *Etkileşim mimarisi →
  Satır grameri*.)
- **Kayıt tabakası** dosyayı en üste alır — bu ekranda yapılan tek şey
  neredeyse her zaman özgeçmişi açmak; diğer bilgiler ona karar vermek için.
  `paylasimlar` listesi de orada: "bu özgeçmişi zaten göndermiş miyiz?"
  sorusunun cevabı listede hiç yoktu.
- **Düzenlerken önce AYRINTI çekilir.** Form listedeki özetten dolduruluyordu
  ve `adres` yalnızca ayrıntı yanıtında var: her düzenlemede adres boş gidiyor,
  sunucu tam güncelleme yaptığı için **sessizce siliniyordu**. Alanlar artık
  ayrıntı gelene kadar hiç çizilmiyor.

## Halk günü çıktıları

`ekranlar/halkgunu/CiktiMenusu.tsx` — üç kâğıt (program · katılım çizelgesi ·
sonuç raporu) × iki biçim (Excel · PDF). Aynı menü **dilim başlığında** da
duruyor ve orada `dilimId` geçirir: kapıdaki görevlinin kâğıdı bütün günü
değil o saatteki grubu gösteriyor.

## Bildirim şeridi (toast) parmakla kapanır

`bilesenler/Toast.tsx` — giriş/çıkış canlandırması `globals.css` içindeki
`.bildirim` keyframe'lerinde. Kaydırma yönü görünüme göre değişir: masaüstünde
**sağa**, mobilde **aşağı**. Şerit masaüstünde sağ altta, mobilde altta
duruyor; her iki durumda da parmağın/farenin şeridi "ekrandan dışarı" attığı
yön bu.

## İstatistiklerde Apache ECharts

`bilesenler/EGrafik.tsx` ince bir sarmalayıcı, `IstatistikGrafikleri.tsx`
dağılımları çizer (mahalle, meslek, tip, durum, saat…).

- **Sıfır boyutlu kapta başlatma çöküyor**: grafik ancak kabın ölçüsü
  oluştuktan sonra kurulur, `ResizeObserver` jsdom'da korumalı.
- **Sıralı listelerde tek marka rengi.** Bir dönem renkler çizim dizisinden
  hesaplanıyordu ve dizi ters çevrilince renkler de terse dönüyordu: en büyük
  dilim en soluk renkte çıkıyordu.
- Sunucu uzun listeleri **kendisi kırpar** ve görünür bir "Diğer (N)" dilimi
  bırakır; istemcide kırpmak toplamı sessizce değiştiriyordu.

## SMS'te yer tutucu

`bilesenler/YerTutucuSecici.tsx` — metin yazarken `{ad}` gibi alanlar
**imlecin bulunduğu yere** eklenir, metnin sonuna değil. Katalog sunucudan
gelir (`ayar/sms-yer-tutucular?baglam=`), yani etkinlik ve halk günü aynı
bileşeni farklı listeyle kullanır.
