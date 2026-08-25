# KentOS.Mini — Sunucu (.NET 10)

Belediye başkanlık makamı için ajanda / talep / iş takip sistemi. **İki yıldır
canlıda**, binlerce kayıt ve aktif bir Flutter mobil uygulaması var. Buradaki
her değişiklik üretimdeki bir sistemi etkiler.

> **Ürün adı `KentOS.Mini`, depo adı `kentos-mini`.** Ad alanları ve derleme
> adları `KentOS.Mini.*` önekini taşır. Uygulama açık kaynak olacak ve başka
> belediyelere verilecek; bu yüzden **kaynak ağacında hiçbir kurumun adı
> geçmez** — ne sınıf adında, ne varsayılanda, ne bir metinde. Yorum satırında
> geçebilir. Kural ve bekçisi: *KURUM BİLGİSİ KODA YAZILMAZ*.
>
> Flutter mobil uygulaması **ayrı bir depodur** (`workcollab/`) ve bu depoya
> dahil değildir.

## Çözüm yerleşimi

| Proje | Ne barındırır |
|---|---|
| `KentOS.Mini.Web` | MVC View'ları, `Controllers/Api/*` (mobil API), `Controllers/V2/*` (yeni API), `AppDbContext`, Migrations, **servis implementasyonları**, 2 `BackgroundService` |
| `KentOS.Mini.Application` | Entity'ler (`Models/`), DTO'lar (`Dto/`), enum'lar, **servis arayüzleri**, RRULE motoru. **NuGet bağımlılığı yok, proje referansı yok** — bu kasıtlı, böyle kalsın. |
| `KentOS.Mini.Tests` | xUnit |

Ayrı Domain/Infrastructure projesi yok; iki katmanlı bölünme bilinçli.

SPA'nın kendi kılavuzu ayrı ve **bağlayıcı**:
`KentOS.Mini.Web/frontend/CLAUDE.md`.

## HER İYİLEŞTİRMEDEN SONRA DÖKÜMAN GÜNCELLENİR

Bir değişiklik, dökümanı güncellenmeden **bitmiş sayılmaz**. Üç yerde iz kalır:

| Nerede | Ne yazılır |
|---|---|
| **Kod içi XML yorumu** | O kararın NEDEN'i; hangi üretim hatası yüzünden |
| **`CLAUDE.md`** (kök ya da `frontend/`) | Mimari karar, değişmez kural, tuzak |
| **`frontend/src/yardim/metinler/*.md`** | Kullanıcının gördüğü davranış değiştiyse |

**Kararın yanına ölçümü de yaz.** "Havuz 40 → 25 bağlantıda sabitlendi",
"628px yatay taşma", "1913 → 1849px". Sayısı olmayan bir karar, bir sonraki
kişiye yeniden ölçtürüyor.

Bu kural özellikle **davranış değiştiren** işler için geçerli: süzgeçler araç
çubuğundan alt tabakaya taşındığında yardım metinleri aylarca "sağ üstteki
düğmeye basın" demeye devam etti — yardım, kullanıcıyı olmayan bir düğmeye
yönlendirdi. Yanlış döküman, dökümansızlıktan kötüdür.

## DİL SÖZLEŞMESİ — nerede İngilizce, nerede Türkçe

Üç ayrı dil alanı var ve **sınırları kesindir**. Bir alandaki ad diğerine
sızmaz.

| Alan | Dil | Kapsam |
|---|---|---|
| **Kod** | **İngilizce** | Sınıf, arayüz, metot, property, değişken, dosya, klasör, namespace, controller, servis, DTO, enum üyesi, test adı, git dalı — **hem sunucu hem SPA** |
| **Veritabanı** | **Türkçe** | Tablo ve kolon adları, indeks/kısıt adları. `randevular.baslangic_tarih` AYNEN kalır |
| **Kullanıcı yüzeyi** | **Türkçe** | Arayüz metinleri, hata mesajları, bildirimler, yardım metinleri, PDF/Excel başlıkları, SMS şablonları |

> **Bu üç alanı birbirine bağlayan şey MAP'TİR, ad benzerliği değil.** Bugün
> `Randevu.Konu` property'si `konu` kolonuna, `"konu"` JSON alanına ve
> "Konu" etiketine aynı anda karşılık geliyor — üçü de aynı yazımdan
> türediği için. Sınıf adı `Appointment.Subject` olduğunda bu tesadüfi
> hizalanma biter ve **her biri açıkça yazılmalıdır**.

### Yeniden adlandırmanın üç ölümcül tuzağı

**1. Kolon adı property'den türetiliyor.** `UseSnakeCaseNamingConvention()`
kolon adını C# property adından üretir; kodda tek bir açık `[Column]` yok.
`Konu` → `Subject` yeniden adlandırması, canlı tabloda **bulunmayan**
`subject` kolonunu arar ve uygulama açılışta değil, o sorgu ilk çalıştığında
patlar.

> Kural: **yeniden adlandırılan HER property'ye açık kolon adı yazılır.**
> `AppDbContext`in Fluent yapılandırmasında `HasColumnName("konu")` ya da
> entity üzerinde `[Column("konu")]`. Migration üretilmemeli — üretiliyorsa
> bir yeri kaçırmışsındır, migration'ı silip mapping'i düzelt.

**2. JSON alan adı property'den türetiliyor.** Serileştirici camelCase
uyguluyor: `Konu` → `"konu"`. **v1 API canlı Flutter uygulamasının
sözleşmesidir**; `Subject` → `"subject"` mobil uygulamayı sessizce kırar
(alan `null` gelir, ekran boş açılır — 500 bile almazsın).

> Kural: **v1 yüzeyindeki yeniden adlandırılan her property'ye
> `[JsonPropertyName("konu")]`.** v2'de de aynısı geçerli: SPA tiplerini
> yeniden üretiyor ama **mobil bazı v2 uçlarını da kullanıyor**.

**3. Migration geçmişi dokunulmazdır.** `Migrations/` altındaki 45 dosya
uygulanmış geçmiştir. İçindeki tip/property adları **değiştirilmez**;
gerekiyorsa `using` takma adıyla eski ada bakan bir köprü yazılır. Uygulanmış
bir migration'ı düzenlemek, `__EFMigrationsHistory` ile kodu ayrıştırır.

### Sunucu TOPLU ÇEVRİLMEZ — dokundukça çevrilir

Ön yüz baştan sona İngilizceye çevrildi. **Sunucuda aynı şey yapılmayacak.**
Kural şu:

- **Yeni yazılan her şey İngilizce**: yeni entity, servis, DTO, controller,
  metot, değişken, dosya.
- **Eski kod, DOKUNULDUĞUNDA çevrilir.** Bir servisi değiştiriyorsan o
  dosyayı İngilizceye al; yanındaki dosyaya dokunma.
- **Kimse "çeviri turu" başlatmaz.** 285 dosyalık toplu yeniden adlandırma
  planlanmıyor.

Sebep, taşınan riskin kazanılan okunabilirlikten büyük olması:

| Risk | Neden |
|---|---|
| `Migrations/AppDbContextModelSnapshot.cs` bayatlar | Entity sınıf adları orada dize olarak yazılı. Çalışma anını etkilemez ama bir sonraki `migrations add` **sahte bir fark** üretir — var olan tabloları düşürüp yeniden yaratmayı önerir. |
| Canlı mobil uygulama | v1 sözleşmesi; her yeniden adlandırma bir `[JsonPropertyName]`e bağımlı. Öznitelik unutulursa hata 500 vermez, alan `null` gelir. |
| Getiri düşük | Sunucu kodunu yalnızca biz okuyoruz; ön yüzdeki gibi dış ekosistem baskısı yok. |

> Sözleşme zaten **sabitlendi** (40 `[Table]` · 451 `[Column]` ·
> 1190 `[JsonPropertyName]`), yani dokundukça çevirmek güvenli. Öznitelik
> property'ye yapışık olduğu için ad değişince Türkçe kolon/JSON adı onunla
> birlikte taşınıyor. Bekçi testleri her derlemede bunu doğruluyor.

### Bekçi: sözleşme dondurma testleri

Yeniden adlandırmaya başlamadan önce **mevcut sözleşme dondurulur**:

| Test | Neyi kilitler |
|---|---|
| `DatabaseContractTests` | Her entity'nin her property'sinin **kolon adı**; tablo adları |
| `JsonContractTests` | v1 (ve mobilin kullandığı v2) DTO'larının **JSON alan adları** |

Anlık görüntü depoda duruyor. Bir yeniden adlandırma bu adlardan birini
değiştirirse test kırmızıya döner — yani hata, mobil kullanıcı fark etmeden
**derleme hattında** yakalanır. Anlık görüntü **yalnızca kasıtlı bir sözleşme
değişikliğinde** ve gerekçesi yazılarak güncellenir.

> **Bekçinin ateş ettiği ölçüldü.** `Randevu.Konu` üzerindeki
> `[Column("konu")]` bilerek `[Column("subject")]` yapıldı; test
> `KAYBOLAN − konu / EKLENEN + subject` diyerek düştü, geri alınınca yeşile
> döndü. Hiç ateş etmeyen bir bekçi, olmayan bir bekçidir — yeni bir
> sözleşme testi yazarken **kırılabildiğini de kanıtla**.

### Sözleşme SABİTLENDİ (yeniden adlandırma artık güvenli)

Çeviriden önce mevcut adlar koda açıkça yazıldı — hiçbir şey yeniden
adlandırılmadan, yalnızca öznitelik eklenerek:

| Ne | Nerede | Adet |
|---|---|---|
| `[Table("...")]` | 40 entity | 40 |
| `[Column("...")]` | entity skaler property'leri | 451 |
| `[JsonPropertyName("...")]` | `Application/Dto/**` + `Web/Services/V2/**` DTO'ları | 1190 |

**Öznitelik property'ye yapışıktır**: `Konu` → `Subject` olduğunda
`[Column("konu")]` onunla birlikte taşınır ve kolon adı değişmez. Sıra bu
yüzden şuydu ve bir daha yapılacaksa yine böyle olmalı: **önce sabitle, sonra
adlandır.**

- `ColumnAttribute` tek kullanımlıktır: `[Column(TypeName = ...)]` zaten
  taşıyan property'de ad **aynı özniteliğe birleştirildi**, ikinci bir
  `[Column]` eklenmedi.
- `ajanda_tekrarlar.ajanda_id` bir EF **gölge property**'sidir — C# tarafında
  karşılığı yok, öznitelik takılacak bir yer yok. `AppDbContext` içinde Fluent
  API ile sabitlendi.
- Ölçüm: koddaki **485 kolonun tamamı** canlı veritabanında karşılığını
  buluyor; `randevular` 27 kolonuyla birebir. Migration ÜRETİLMEDİ (üretilmesi
  gerekseydi bir yeri kaçırmış olurduk).

## MODÜL ÜRETİM SIRASI — atlanamaz

Yeni bir modül istendiğinde sıra **budur**. Bir adım tamamlanmadan sonrakine
geçilmez; "sonra bakarız" diye bırakılan adım hiç yapılmıyor.

### 1. Sunucu — modül tamamlanır ve TEST EDİLİR

```
Entity  →  Service  →  Validation  →  DTO  →  Mapping  →  Controller  →  Test
```

| Adım | Nerede | Kural |
|---|---|---|
| **Entity** | `Application/Models/` | İngilizce sınıf/property; **Türkçe tablo ve kolon adı açıkça yazılır** |
| **Service arayüzü** | `Application/Services/` | NuGet/proje bağımlılığı YOK |
| **Service** | `Web/Services/V2/` | İş kuralının tek sahibi; controller'da mantık olmaz |
| **Validation** | FluentValidation | v2 DTO'larına DataAnnotations KONMAZ |
| **DTO** | servis dosyasında ya da `Application/Dto/V2/` | İstek/yanıt ayrı; entity sızmaz |
| **Mapping** | Mapster | Elle kopyalama yok |
| **Controller** | `Web/Controllers/V2/` | **Yalnızca v2.** İnce; yetkilendirme + servis çağrısı |
| **Test** | `Tests/` | Birim izolasyonu, izin kapısı, sayfalama, hata yolları |

Modül **testleri geçmeden** bitmiş sayılmaz. Postgres'e dokunan her test
sınıfı `[Collection(SunucuKoleksiyonu.Ad)]` taşır.

### 2. İstemci sınıfları — AYNI sözleşmeden üretilir

Sunucu bittikten sonra istemci tarafı **aynı servis arayüzleri ve DTO'lardan**
türetilir:

- **SPA**: `npm run tipler:uret` → `src/veri/tipler.uretilen.ts` (elle
  düzenlenmez). Okunabilir takma adlar `src/veri/tipler.ts` içinde.
- **Mobil**: `lib/models/*.dart` + `lib/clients/*_client.dart`, savunmacı
  `fromJson` ile.

> **Sözleşme değişirse istemci AYNI iş içinde güncellenir.** Sunucuyu
> değiştirip istemciyi "sonra" bırakmak, kırık bir sistemi commit etmektir.
> SPA'da bu otomatik: üretilmiş tipler değişince **derleme kırılır**. Mobilde
> otomatik değil — bu yüzden mobil istemci güncellemesi modülün tanımına
> dahildir.

### 3. Her liste SAYFALANIR — tek zarf

**Bundan sonraki her liste ucu** `SayfaliSonuc<T>` döner. Çıplak dizi dönen
uç yazılmaz.

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Veriler,   // JSON: veriler
    int Sayfa, int Boyut, int Toplam, int ToplamSayfa,
    bool OncekiVar, bool SonrakiVar);
```

- Zarfın **şekli her uçta aynıdır** — istemci tek bir sarmalayıcı yazar.
- Varsayılan `boyut` 25, üst sınır 100. Sınırsız liste yok.
- Süzgeç nesnesi `[FromQuery]` tek sınıf olarak alınır.
- **Mevcut uçlar değişmez.** Sayfalama eklenmemiş eski bir uca dokunulacaksa
  mevcut kalıba (`SayfalamaUzantilari`) uyulur.

### 4. Ön yüz — bileşen önce, mobil önce

Sunucu bittikten sonra arayüz **komple** tasarlanır.

- **Tekrar kullanılabilecek her parça bileşendir.** İkinci kez ihtiyaç
  duyulacağını düşündüğün şeyi ekranın içine gömme; `bilesenler/` altına
  çıkar. Ölçüt: "başka bir ekranda da lazım olur mu?"
- **Mobil önce tasarlanır**, masaüstü ondan türetilir — tersi değil. Ama
  masaüstü "küçültülmüş mobil" de değildir: orada yer bol, fare hassas,
  klavye var; tablo, çoklu sütun ve kısayollar masaüstünün hakkıdır.
- Etkileşim grameri `frontend/CLAUDE.md` → *Etkileşim mimarisi*'nden gelir:
  kabuk → ekran → FAB → tabaka. Yeni bir gramer icat edilmez.
- Ekran, **izin kapılarıyla** birlikte biter (menü + rota + düğme).

### 5. Döküman ve özellik ağacı

Arayüz bittikten sonra, aynı iş içinde:

1. `CLAUDE.md` (kök ve/veya `frontend/`) — mimari karar + **ölçüm**.
2. `frontend/src/yardim/metinler/<ekran>.md` — kullanıcı yardımı;
   `yardim/katalog.ts`'e kaydı (grup alanı zorunlu).
3. **`OZELLIK-AGACI.md`** — modül sistem özellik ağacına işlenir: hangi
   yetenek, hangi uç, hangi ekran, hangi izin, hangi test.

## KURUM BİLGİSİ KODA YAZILMAZ

Uygulama **tek bir kuruma ait değildir**. Hedef net: başka bir belediyeye
verildiğinde kurulum, `.env` dosyasını doldurup çalıştırmaktan ibaret olmalı.

Kod içinde asla geçmez: kurum adı, kısa ad, alan adı, e‑posta alanı, logo
yolu, kurumsal renk, adres, telefon, imza/antet metni, SMS başlığı.

### İKİ AYRI YER — hangisi nereye

Bilgi türüne göre ikiye ayrılır ve ayrım keyfi değil, **zorunlu**:

| Ne | Nerede | Neden orada |
|---|---|---|
| Veritabanı bağlantısı, JWT imza anahtarı, SMS hesabı, nesne deposu anahtarları, Firebase kimliği | **`.env`** | Bunları okumak için zaten veritabanına bağlanmak gerekiyor — veritabanında tutulamazlar. Sırların yedeklere düşmemesi de tercih sebebi. |
| Kurum adı, iletişim, uygulama adı, marka renkleri, amblem | **Veritabanı** (`kurum_bilgileri`, tek satır) | Değiştirmek için sunucuya girip dosya düzenlemek ve uygulamayı yeniden başlatmak gerekmesin. Yetkili kullanıcı arayüzden düzenler (`/kurum`, `sistem.kurum` izni). |

**`.env` yine de kurulumun tek adımı olarak kalır:** `kurum_bilgileri` tablosu
BOŞKEN ilk satır oradaki `Institution__*` / `Brand__*` değerlerinden
tohumlanır (`InstitutionService.TohumlaAsync`). Yani sıfırdan bir kurulum
hâlâ "sadece .env doldur, çalıştır". Tablo dolduktan sonra o satırları
değiştirmek hiçbir şeyi değiştirmez.

### `.env` biçimi — .NET'in kendi kuralı

```
Bolum__Alt=deger            →  yapılandırma anahtarı "Bolum:Alt"
Bolum__Alt__DahaAlt=deger   →  "Bolum:Alt:DahaAlt"
```

Uygulama açılırken dosyayı **ortam değişkenlerine** yükler
(`Configuration/EnvironmentFile.cs`, `DotNetEnv`); anahtarları eşleyen ekstra
bir kod **yoktur**, .NET'in `AddEnvironmentVariables()` sağlayıcısı `__`
işaretini `:` yapıp kendisi bulur. İki sonucu var:

- Aynı ayar `.env` ile de, IIS/Docker/systemd ortam değişkeniyle de verilebilir.
- **Gerçek ortam değişkeni her zaman kazanır**; `.env` onu ezmez. Yayın
  makinesinde "dosyada ne yazıyorsa o çalışır" sürprizi olmaz.

> **SIRA KRİTİK:** yükleme `WebApplication.CreateBuilder`dan ÖNCE olmak
> zorunda — builder ortam değişkenlerini o anda okuyor. Sonra çağrılsaydı
> dosya sessizce etkisiz kalırdı.

### Ayar sınıfları

Hepsi `Options/` altında, `AddApplicationOptions()` ile bağlanır. `IOptions<T>`
sarmalı yerine doğrudan `T` de çözülebilir: ayarlar çalışma anında değişmiyor.

| Sınıf | Bölüm |
|---|---|
| `InstitutionOptions` | `Institution:*` — yalnızca ilk tohum |
| `BrandOptions` | `Brand:*` — yalnızca ilk tohum |
| `ApplicationOptions` | `App:*` (`BaseUrl`, `Name`, `Description`…) |
| `StorageOptions` + `S3StorageOptions` | `Storage:*`, `Storage:S3:*` |
| `SmsOptions` · `JwtOptions` · `FirebaseOptions` | `Sms:*` · `Jwt:*` · `Firebase:*` |
| `DatabaseOptions` · `RequestOptions` | `Database:*` · `Requests:*` |

**Eski anahtar geri düşüşleri.** Yayında çalışan `appsettings.json` dosyaları
bu sürümle güncellenmeyecek; `OptionsRegistration.LegacyKeys` eski anahtarı
yeni karşılığı boşken okur. Yeni anahtar varsa o kazanır.

| Eski | Yeni |
|---|---|
| `URL` | `App:BaseUrl` |
| `Depolama:GonderimDizini` | `Storage:SendDirectory` |
| `Randevu:HalkGunuTipId` | `Requests:PublicDayTypeId` |

### İstemciler kurumu nereden okur

`GET /api/v2/institution` — **anonim**, çünkü giriş ekranı da amblemi, kurum
adını ve marka rengini göstermek zorunda. Yanıtta gizli bir şey yok; hepsi
zaten sayfanın görünen yüzü. Yazma ucu (`PUT`) `sistem.kurum` ister.

> **Derleme anı değil ÇALIŞMA ANI.** `VITE_*` değişkeni derlemeye gömülür;
> o zaman her kurum için ayrı bir SPA derlemesi gerekir ve "sadece .env"
> hedefi ölür. SPA markayı bu uçtan okur, son yanıtı `localStorage`'da
> saklar (çevrimdışı açılışta amblem kaybolmasın) ve ilk kareyi önbellekten
> boyar.

Firebase'in **istemci** alanları da bu yanıtta taşınır. Gizli değiller
(tarayıcıya nasılsa iniyorlar) ama kuruma özeller.

**PWA manifesti sunucuda üretilir** (`Controllers/ManifestController.cs`,
`GET /manifest.webmanifest`): adı, açıklaması ve tema rengi kurum kaydından
gelir. Statik bir dosya olsaydı kurum değişince ön yüzü yeniden derlemek
gerekirdi.

### Bekçiler

| Test | Neyi kilitler |
|---|---|
| `YapilandirmaTests` | Ayar varsayılanlarına kurum bilgisi sızmamış; `.env.example` **her ayarı** içeriyor; şablonda gerçek sır yok; eski anahtar geri düşüşü çalışıyor |
| `DepolamaTests` | Anahtar temizliği, dizin dışına çıkma reddi, eksik S3 ayarında açılışın durması |

`.env.example` depoda durur ve **her yeni ayar oraya da yazılır** — bunu test
denetliyor, unutmak mümkün değil. Gerçek `.env` depoya girmez.

## DOSYA DEPOLAMA — disk ya da nesne deposu

`STORAGE__PROVIDER` tek satırla seçer:

| Değer | Ne yapar |
|---|---|
| `Local` (varsayılan) | `wwwroot/uploads` altına yazar — iki yıldır yayında olan davranış |
| `S3` | S3 uyumlu nesne deposu (MinIO, AWS S3, Ceph, Wasabi…) |

Nesne deposu şu iki durumda **gerekli**: (1) uygulama birden çok sunucuda
çalışıyor — bir sunucuya yüklenen dosyayı diğeri göremez; (2) kapsayıcıyla
dağıtılıyor ve yayın klasörü kalıcı değil.

`IFileStorage` arayüzü iki alan tanır: `Public` (`/uploads/...`) ve `Private`
(kullanıcıdan kullanıcıya gönderilen belgeler, hiçbir zaman statik sunulmaz).

**Nesne adı = veritabanındaki yolun aynısı** (`uploads/ajanda/….jpg`).
Bu sayede yerelden nesne deposuna geçiş dosyaları kopyalamaktan ibaret;
tek bir veritabanı kaydı değişmiyor:

```bash
mc mirror ./wwwroot/uploads            yerel/workcollab/uploads
mc mirror ./wwwroot/uploads/gonderim   yerel/workcollab/gonderim
```

> **`/uploads/...` ADRESLERİ S3 KİPİNDE DE ÇALIŞIR.** Yayındaki v1 mobil
> istemcileri fotoğrafları ve talep eklerini doğrudan o adresten indiriyor —
> dokunulmayacağına söz verilen sözleşmenin parçası. `Middleware/
> UzakDepoKopruSu.cs` isteği nesne deposundan karşılıyor. Yerel kipte hiç
> devreye girmez, yani bugünkü kurulumda ek maliyet yok.

> **Eksik S3 ayarında uygulama AÇILMAZ.** Sessizce yerele düşmek çok daha
> kötü olurdu: yükleme çalışmaya devam eder, dosyalar beklenen yere gitmez ve
> kimse fark etmez.

**Ölçüldü** (MinIO ile, hem `Local` hem `S3` kipinde): yükleme → uçtan
indirme → statik `/uploads` yolu → gizli alanın 404'ü → silme. İkisinde de
içerik birebir aynı; S3 kipinde dosya yerel diske hiç yazılmıyor.

> **Bir yanlış teşhis, kayda değer:** MinIO'nun .NET istemcisi bir süre
> "uyumsuz" sanıldı — istekler 404 ve XML ayrıştırma hatasıyla dönüyordu.
> Sebep istemci değildi: test için seçilen 9100 portunu **başka bir süreç**
> (bir Dart geliştirme sunucusu) tutuyordu ve istekler MinIO'ya hiç
> ulaşmıyordu. Boş bir portta aynı istemci ilk denemede çalıştı. Ders:
> "kütüphane bozuk" sonucuna varmadan önce `lsof -nP -iTCP:<port>` ile
> isteğin gerçekten hedefe gittiğini doğrula.

## Değiştirilemez kurallar

### 0. Eski MVC arayüzünde iş yapılmaz

`Views/**/*.cshtml` **artık geliştirilmiyor.** Eski uygulama aşama aşama
yayından kaldırılacak; her yeni işlev **sunucu + `/api/v2` + SPA (`frontend/`) +
mobil** üçlüsünde yapılır. `.cshtml` dosyalarına yalnızca canlıyı ayakta tutan
düzeltmeler girer, yeni özellik girmez.

### 1. v1 API'ye dokunma

`Controllers/Api/*` ve onların DTO'ları **canlı mobil uygulamanın sözleşmesidir**. Rota, alan adı, JSON adlandırması, HTTP fiili, dönüş şekli — hiçbiri değişmez. Yeni işlevsellik `Controllers/V2/` altına yazılır.

> Rota tuzağı: `[Route("api/[controller]")]` + `AjandaApiController` → `/api/AjandaApi`. "Api" soneki rotada kalır. v2'de `[controller]` belirteci **kullanılmaz**, rotalar birebir yazılır (`[Route("api/v2/ajanda")]`).

### 2. İş mantığı servislerde kalır

v2 controller'ları mevcut servisleri (`IAjandaService`, `IAjandaSeriService`, `IRandevuService`, …) çağırır. Mantık kopyalanmaz. Servis yetmiyorsa arayüze **yeni metot eklenir**, mevcut imza değiştirilmez.

### 3. Gizli etkinlik kuralları

Tek doğruluk kaynağı: `Web/Data/AjandaSorguUzantilari.cs` → `GorunurOlanlar(kullaniciId, kullaniciAdi)`.

```csharp
!a.Gizli
|| (kullaniciAdi != null && a.KullaniciId == kullaniciAdi)      // oluşturan
|| (kullaniciId  != null && a.Katilimcilar.Any(k => k.KullaniciId == kullaniciId))
```

- **Rol bypass'ı yoktur.** Admin/Başkan başkasının gizli etkinliğini göremez. Böyle kalacak.
- Global query filter **değil**, açık `Where` — çünkü silinmiş liste ve istatistikler `IgnoreQueryFilters()` kullanıyor ve global filtre orada sessizce devre dışı kalırdı.
- Değişmezler: `Gizli && BasinKatilsin` → `BusinessRuleException`. Gizli etkinlik havale edilemez, çiçek talimatı üretmez, birim SMS'i göndermez, medya listesine girmez. Bildirimi yalnızca katılımcılar ∪ oluşturan alır, başlık `🔒 Gizli · ` (push) / `[GİZLİ] ` (SMS) ile öneklenir.

> **Asimetri — dikkat:** `Ajanda.KullaniciId` bir **kullanıcı adı metnidir**. `AjandaKatilimci.KullaniciId` ise **sayısal `AspNetUsers.Id`**'dir. İkisi aynı şey değil.

### Güncelleme KÜNYEYE dokunmaz (ölümcül hata)

`_mapper.Map(dto, entity)` güncellemede var olan satırın üstüne yazıyor:
**gövdede olmayan her alan varsayılana düşüyor.** İstemciler `kullaniciId`
göndermediği için her düzenleme etkinliğin SAHİBİNİ `NULL`'a çekiyordu.

Sonucu ölümcül: gizli etkinliğin görünürlüğü "oluşturan" eşleşmesine bakar
(`a.KullaniciId == kullaniciAdi`); sahipsiz kalmış bir kayıt gizliye
çevrildiğinde **oluşturanın da gözünden kayboluyor**, detay 404 veriyordu.
İstisna atılmadığı için `sistem_hatalari`'na da bir şey düşmüyordu —
kullanıcı yalnızca "kaydettim, etkinlik kayboldu" diyebiliyordu.

- `KullaniciId`, `OlusturmaTarihi`, `GuncellemeTarihi` artık
  `MapsterConfig`'te DTO→entity yönünde **yok sayılıyor**; künyeyi servis
  yönetir.
- `UpdateAsync` sahibi ayrıca koruyor; sahip boşsa (eski kayıtlar) düzenleyen
  kişi sahiplenir — kaydı görebilen biri, kimsenin göremediği bir kayıttan
  iyidir.
- Hasarlı satırlar migration'da **olay günlüğünden** onarıldı
  (`ajanda_olaylar` "oluşturuldu" kaydındaki tam ad → kullanıcı adı).
- Bekçi: `GizliEtkinlikTests.Guncelleme_etkinligin_sahibini_silmez` ve
  `Gizliye_cevirme_etkinligi_olusturandan_gizlemez`.

### Katılımcılar HER etkinlikte okunur

`AjandaService.ZenginlestirAsync` katılımcı listesini yalnızca **gizli**
etkinlikler için yüklüyordu — katılımcı bir zamanlar "bu gizli kaydı kim
görebilir" demekti. Katılımcı BİRİM eklendiğinde anlam genişledi (açık bir
toplantıya müdürlük davet etmek de katılımcıdır) ama o satır olduğu gibi
kaldı: birimler kaydediliyor, **hiçbir ekranda görünmüyordu**. Düzenlemeye
girildiğinde liste boş geliyor, kaydedince de seçim siliniyordu. Bekçi:
`GizliEtkinlikTests.Katilimci_birimler_acik_etkinlikte_de_okunur`.

### Katılımcı BİRİM ile GÖREBİLECEK KİŞİ — İKİ AYRI KAVRAM

`AjandaKatilimci` tablosu iki farklı soruyu cevaplıyor ve **birbirinin yerine
geçmezler**:

| Sütun | Ne demek | Kaynak |
|---|---|---|
| `BirimId` | **Katılımcı birim** — etkinliğe katılacak departman | kendi seviyen ve **altı** |
| `KullaniciId` | **Görebilecek kişi** — gizli etkinliği kim görebilir | **kendi birimin** |

- Katılımcı birim eklemek gizli bir etkinliği o birime **AÇMAZ**.
- Görebilecek kişi eklemek onu toplantıya **davet etmez**.
- Bir dönem `KullaniciId` "eski alan" sanılıp görünürlük katılımcı birimlere
  bağlanmıştı: gizli bir toplantıya bir müdürlüğü davet etmek, o müdürlükteki
  **herkesi** toplantının içeriğine ortak ediyordu. Üstelik eski MVC formu
  kullanıcı seçtirip değerleri `birim_id` sütununa yazacaktı.

**Kaynak denetimi sunucuda** (`KatilimcilariEsitleAsync`): birim yalnızca
`Level >= seviye && Id != kendiBirim`, kişi yalnızca `BirimId == kendiBirim`.
Denetim **yalnızca yeni eklenenlere** uygulanır — kullanıcının birimi ya da
bir birimin hiyerarşideki yeri değişince eski bir kaydı açıp kaydetmek
imkânsız hâle gelirdi.

DTO: yazma `katilimciBirimIdler` / `katilimciIdler`; okuma tek liste
(`katilimcilar`) ve ayrım `birimId` doluluğuyla yapılır. `null` = dokunma,
boş liste = temizle. Gizlilik kapanınca görebilecekler listesi temizlenir,
davet listesi kalır.

Seçim uçları: `GET ayar/katilimci-birimler` (davet) ·
`GET ayar/birim-kullanicilari` (görünürlük).

> **Gizli etkinlik bildirimi yalnızca görebilecekler ∪ ekleyene gider.**
> Katılımcı birimlerin kullanıcılarına **gitmez**: göremeyecek birine
> etkinliğin BAŞLIĞINI göndermek, gizliliği bildirim üzerinden delmek demek.
> `GizliAliciIdleriAsync` görünürlük kuralının birebir karşılığıdır; ikisi
> ayrışırsa ya bildirim sızar ya da kayıt görebilen birine haber gitmez.

> **`ErisilebilirOlanlar` genişledi ama gizlilik üstte.** Davet edilen birim
> AÇIK etkinliği görür (`a.Katilimcilar.Any(k => k.BirimId == birimId)`);
> gizli olanı göremez, çünkü iki koşul VE ile bağlı. Davet etmek ile "içeriği
> görebilsin" demek aynı şey değil.

Eşitleme **tek yerde**: `AjandaSorguUzantilari.KatilimcilariEsitleAsync`.
Önce dört kopyası vardı ve kural değişince biri unutuluyordu.

### 4. Tekrar eden etkinlik kuralları

Kural `AjandaSeri`'de (`Rrule`, `Dtstart`, `SureDakika`, `UretilenSonTarih`, `Iptal`). **Tekrarlar gerçek `Ajanda` satırı olarak üretilir** — sorgu anında genişletilmez.

| Alan | Anlamı |
|---|---|
| `Ajanda.SeriId` | null → tek seferlik |
| `Ajanda.SeriOrijinalBaslangic` | RECURRENCE-ID; kuralın hesapladığı özgün başlangıç, eşleştirme anahtarı |
| `Ajanda.SeriAyrik` | bu tekrar bireysel düzenlendi → seri güncellemeleri atlar, ufuk genişletmesi yeniden üretmez |

`TekrarKapsam { Yalnizca=0, BundanSonrakiler=1, Tumu=2 }`.

- RRULE ayrıştırıcısı **elle yazılmış** (`Application/Services/RRuleKural.cs`, `RRuleGenisletici.cs`). iCal kütüphanesi **eklenmeyecek**: tüm zaman damgaları `timestamp without time zone` (kayan yerel saat), kütüphane DST kaydırır.
- Desteklenen alt küme: `FREQ, INTERVAL, COUNT, UNTIL, BYDAY (sıra ekli), BYMONTHDAY, BYMONTH, WKST`. Dışındaki her şey `FormatException` → 400.
- Ufuk 18 ay / 200 tekrar; `TekrarUfkuWorker` 24 saatte bir kaydırır. **Bu servisi kapatma** — kapatılırsa ileri tarihli tekrarlar takvimde kaybolur.
- Yönlendirme önceliği `Web/Services/AjandaService.cs` `UpdateAsync` içinde: (1) `TekrarKaldir` → kaldır, (2) yeni kural + `SeriId==null` → seriye çevir, (3) kural değişti **ve** kapsam ≠ Yalnizca → seriyi böl, (4) kapsam ≠ Yalnizca → seri güncelle, (5) aksi hâlde tek satır güncelle + `SeriAyrik = true`.
- **Tek tekrar düzenlemesi kuralı asla değiştirmez.** İstemciler RRULE'u formun başlangıç tarihinden türetiyor; dokunulursa seri kayar. (Düzeltilmiş gerçek bir hata.)

### 5. Mapster döngüsü

`Ajanda ⇄ AjandaNot / Cicek` döngüsü bir kez `StackOverflowException` ile **tüm API sürecini düşürdü**. `Web/Mapping/MapsterConfig.cs` `Katilimcilar`'ı iki yönde, `SeriId/SeriOrijinalBaslangic/SeriAyrik`'i DTO→entity yönünde yok sayar. `Tests/MapsterCycleTests.cs` bunu bekçiler. **Yeni bir profil eklerken aynı testi yaz.**


## İzin sistemi — yetki ROLDEN değil İZİNDEN gelir

`Application/Identity/Izinler.cs` tek doğruluk kaynağı: 42 sabit ad
(`ajanda.ekle`, `talep.havale`, …) + başlık ve açıklama. Roller veritabanında,
izinler kodda; bağ `rol_izinleri` tablosunda ve yönetim ekranından
değiştirilebilir.

- Uçlar `[Izin(...)]` ile korunur. **Çoklu izin VEYA ile** değerlendirilir;
  web ve mobil de aynı şekilde yorumlamak zorunda, yoksa arayüzün açtığı ekran
  403 verir.
- `IIzinServisi` izinleri kullanıcı başına **5 dakika** önbellekler; rol
  değişiminde önbellek düşürülür. JWT'ye izin YAZILMAZ — jeton 15 saat geçerli
  ve iptal listesi yok.
- Tohum (`IzinTohumu`) her açılışta katalogu tazeler. Dağıtım iki yerde:
  **hiç izni olmayan role** ilk dağılım, **koda YENİ eklenen izne** ise o
  iznin ilk tanımlandığı açılışta. İkincisi olmadan yeni bir izin var olan
  rollere hiç ulaşmıyordu — katalogda görünüyor, kimsede olmuyordu.
- İlk dağılım eski `PolicyRegistrar` eşlemesinin birebir kopyasıdır; geçiş
  günü kimsenin yetkisi değişmez. **Genişletmek de bir değişikliktir**: Başkan
  bir ara `istatistik.goruntule` alıyordu, oysa o uç `Admin,Sistem`e açıktı.

### Kullanıcıya özel bayraklar emekliye ayrıldı

`AppUser.GizliEtkinlikEkleyebilir` ve `DosyaGonderebilir` sütunları
veritabanında **duruyor ama okunmuyor**. Karar `ajanda.gizliEtkinlik` ve
`gonderim.gonder` izinlerinde. Aynı yetkinin iki kaynağı olması, rol
ekranından kısılan bir iznin kullanıcı kaydından açık kalması demekti ve
hangisinin geçerli olduğu ekrandan anlaşılmıyordu.

`oturum/ben` yanıtındaki alanlar KALDI ama artık **izinden türetiliyor**:
sahadaki eski uygulama sürümleri onlara bakıyor, kaldırmak güncelleme almamış
telefonlarda gizli etkinlik anahtarını sessizce kapatırdı.

### Basın: daraltan izin

`ajanda.basinGoruntule` katalogdaki **tek daraltan** izin. Ötekiler bir kapı
açar, bu açılan kapının ardında ne görüleceğini kısar: sahibi ajandayı görür
ama listede yalnızca `BasinKatilsin` işaretli, **gizli olmayan** kayıtlar
döner.

- Kapı `ICurrentUserService.YalnizcaBasinMiAsync()`. Ajandayı okuyan yedi
  servisin hepsinde bu arayüz zaten var; ayrı bir bağımlılık eklemek her
  birine tek tek dokunmayı ve birini unutmayı davet ederdi.
- `GorunurOlanlar` ve `ErisilebilirOlanlar` parametresinin **varsayılanı
  yok** — derleyici her çağrı yerini karar vermeye zorlar. Unutulan tek
  sorgu, basın kullanıcısına makamın bütün gününü gösterirdi.
  (`BasinAjandasiTests` sabit `false` geçen yeni bir dosya çıkarsa kırmızıya
  döner; izinli tek istisna eski MVC.)
- Tam görüntüleme izniyle birlikte verilirse **geniş olan kazanır**.
- Okuma uçları iki izni de kabul eder
  (`[Izin(AjandaGoruntule, AjandaBasinGoruntule)]`). Yalnızca tam görüntüleme
  istendiğinde arayüz ekranı açıyor ama her istek 403 dönüyordu.

> **Test yalıtımı:** Postgres'e dokunan her test sınıfı
> `[Collection(SunucuKoleksiyonu.Ad)]` taşımalı. Fixture kurucusu şemayı
> sıfırlıyor; koleksiyon dışında kalan tek bir sınıf, komşularını rastgele
> `relation "birimler" does not exist` ile düşürüyor.

## Veritabanı

PostgreSQL (Npgsql 10) + `EFCore.NamingConventions` → **snake_case**. `builder.UseSerialColumns()`. Legacy timestamp davranışı açık.

Global query filter yalnızca iki tane: `Ajanda → !IsDeleted`, `Randevu → !Arsivlendi`.

### Bağlantı havuzu SINIRLIDIR — `Data/BaglantiAyari.cs`

Bağlantı dizesi ham hâliyle kullanılmaz; `BaglantiAyari.Tamamla()` eksik havuz
ayarlarını doldurur: **`Maximum Pool Size=25`**, `Minimum Pool Size=1`,
`Connection Idle Lifetime=60`, `Application Name=workcollab-web`.

Sebebi bir üretim arızası: dört ayrı uç aynı saniyede 500 döndü ve dördünün de
iç hatası `53300: remaining connection slots are reserved for roles with the
SUPERUSER attribute`'tı. Npgsql'in varsayılan havuz sınırı **100**, sunucunun
`max_connections` değeri de 100 (3'ü süper kullanıcıya ayrılmış). Yani tek bir
uygulama örneği, yükseldiğinde sunucunun bütün yuvalarını kaplayabiliyordu —
ve **bu PostgreSQL örneği paylaşımlı** (`kentos`, `turbopos`, `turbohesap`),
yani yuvaları tüketmek yalnızca bizi değil onları da düşürüyor.

Ölçüm: 60 eşzamanlı istemciyle 200 istek → hepsi 200, `pg_stat_activity`
boyunca **tam 25 bağlantıda sabit** (düzeltmeden önce 40 ve artıyordu).

- Değerler **ancak dizede yoksa** yazılır; `appsettings.json` istediğini ezer.
- `EnableRetryOnFailure` **açılmadı**: EF'in yeniden deneme stratejisi
  kullanıcı tarafından başlatılan işlemleri (`BeginTransaction`) çalışma anında
  reddediyor. Üstelik 53300 geçici bir arıza değil bütçe hatası — doğru çözüm
  beklemek değil, hiç taşmamak.

> Tuzak: `NpgsqlConnectionStringBuilder.ContainsKey`, anahtarın **tanınıp
> tanınmadığını** söyler, yazılıp yazılmadığını değil — her zaman `true`
> döner. "Kullanıcı yazmadıysa varsayılanı koy" mantığı bu yüzden bir süre
> sessizce çalışmadı; ölçüm 25 yerine 40 bağlantı gösterdi. Dize elle
> ayrıştırılıyor (`YaziliAnahtarlar`).

### Yerel geliştirme

Postgres, `/Users/cihad/Projects/database/docker-compose.yml` içindeki `postgis_db` konteynerinde çalışır (port 5432, süper kullanıcı `postgres/postgres`).
**Bu konteyner başka projelerle paylaşılıyor** (`kentos`, `turbopos`, `turbohesap`) — onlara hiçbir komut gönderme.

```bash
docker exec postgis_db psql -U postgres -c "CREATE ROLE workcollab LOGIN PASSWORD 'workcollab';"
docker exec postgis_db psql -U postgres -c "CREATE DATABASE workcollab OWNER workcollab;"
docker exec postgis_db psql -U postgres -c "CREATE DATABASE workcollab_test OWNER workcollab;"
```

### Migration

`Database:AutoMigrate` (varsayılan `true`) → uygulama açılışta bekleyen migration'ları uygular; 3/6/9/12 sn ile 5 deneme, sonra **fail-fast**. Ayrıntı: `OTOMATIK-MIGRATION.md`.

```bash
dotnet ef migrations add AdiBuraya -p KentOS.Mini.Web -s KentOS.Mini.Web
```

> `.config/dotnet-tools.json` `dotnet-ef` **9.0.0**'a sabitli ama EF paketleri 10.0.0 — sürüm uyuşmazlığı var, araç güncellenmesi gerekebilir.

> `DataSeeder.EnsureInitialData` **her açılışta koşulsuz** çalışır (üretim dahil). Buraya geliştirme verisi ekleme; geliştirme tohumu `IsDevelopment()` koşuluna bağlanır.

## Kimlik doğrulama

- Varsayılan challenge şeması **Cookie** (`/Account/Login`) — eski MVC arayüzü için.
- **Tüm API controller'ları** `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` ilan etmek **zorunda**. Etmezse tarayıcıdan gelen yetkisiz istek 401 yerine giriş sayfasına 302 döner ve istemci bunu anlamaz.
- JWT: `POST /api/AccountApi/Login` → `{ Token, Expiration }`. HMAC-SHA256, 900 dakika. **Refresh token yok, iptal listesi yok** — 401 = yeniden giriş.
- Claim'ler: `ClaimTypes.Name`, `ClaimTypes.Role`, `UserId`, `BirimId` (`Application/Identity/WorkClaimTypes.cs`).
- 11 rol (`UserRoles.cs`), 5 politika (`AuthPolicies/`): `Ajanda`, `OzelKalem`, `Medya`, `Cicek`, `Sibeski`.

## Bildirim

Giden kutusu deseni: `MessageService` `Messages` tablosuna satır yazar → `FirebaseWorker` 10 saniyede bir okur ve gönderir (FCM push veya SMS), `IsSuccess`/`RetryCount`/`FailMessage` günceller, 3 denemede bırakır.

FCM gövdesi: `Notification { Title, Body }` + `Data["fcmData"] = <json>`.
`fcmData` sözleşmesi: `{ "entity": "Ajanda|Talep|Oneri", "id": "<int>", "action": "OpenDetails|OpenNotes|OpenImages|None" }`.

Token'lar `AspNetUsers` üzerinde: `FcmToken` **yalnızca mobil**, `WebFcmToken` **yalnızca web**. Bir kullanıcının dolu her token'ı için ayrı bir `Messages` satırı üretilir.

## Test

```bash
dotnet test                                    # tümü
dotnet test --filter FullyQualifiedName~RRule  # tek grup
```

`IntegrationTests` canlı PostgreSQL ister (`PostgresTestFixture`) — `workcollab_test` veritabanını kullanır. Diğerleri EF InMemory.

**Mobil sözleşmesinin bozulmadığını kanıtlamanın yolu**, mobil deposundaki uçtan uca koşumdur:
```bash
cd ../workcollab && flutter test test/e2e/yerel_sunucu_test.dart \
  --dart-define=E2E=1 --dart-define=API_KOK=http://127.0.0.1:5097
```
Yerel sunucu `http://127.0.0.1:5097` üzerinde ayakta olmalı. v1'e dokunan her değişiklikten sonra çalıştır.

> **`API_KOK` tuzağı:** testin kendi `_taban` sabiti 127.0.0.1'e bakar ama
> **istemciler** `lib/consts/api_consts.dart` içindeki `gelistirmeKok`
> değerini kullanır; o da geliştirme makinesinin LAN adresine (`192.168.1.111`)
> sabitlenmiş. Makinenin IP'si değiştiğinde giriş çalışır, geri kalan 11 test
> `connection error` ile düşer ve bu **sözleşme bozulmuş gibi görünür**.
> `API_KOK`'u geçmeyi unutma.

## `/api/v2` yüzeyi

217 uç nokta, 18 controller (`Controllers/V2/`). Hepsi `V2ControllerBase`'den türer:
`[ApiController]` + JWT + `V2HataFiltresi` (RFC 7807) + `V2DogrulamaFiltresi`
(FluentValidation). Filtreler `MvcOptions.Filters`'a **eklenmez** — v1 etkilenmez.

| Grup | Uç | Not |
|---|---|---|
| `oturum` | 2 | `giris`, `ben` |
| `ayar` | 11 | referans listeleri |
| `takvim` | 2 | `aralik` (POST), `sayac` |
| `etkinlik` | 17 | CRUD + not/foto/olay/ertele/havale/statü/tip/durum/çiçek/SMS |
| `talep` | 17 | CRUD + not/dosya/hareket/havale/arşiv/ajandaya-ekle |
| `istatistik` | 1 | 20+ dağılımı tek çağrıda döner |
| `oneri` | 6 | `benim` ucu kimliği **oturumdan** alır (v1 rotadan alıyordu) |
| `cicek` | 6 | çiçekçi yönetimi + kart teslimi |
| `yonetim` | 10 | birim/kullanıcı/rol + birim detayı + rol üyeliği |
| `bildirim` | 1 | web push jetonu (POST/DELETE) |
| `protokol` | 7 | il protokol listesi; okuma Ajanda politikası, yazma Admin |
| `gonderim` | 7 | kullanıcıdan kullanıcıya dosya; **görünürlük servis katmanında** |
| `tanim` | 12 | referans veri yönetimi |
| `davet` | 9 | davet listeleri + 4 türde PDF çıktı |
| `halk-gunu` | 21 | vatandaş havuzu, gün/dilim/atama, salon, SMS, Excel, talebe dönüştürme |
| `ozgecmis` | 7 | özgeçmiş havuzu: arama, yükleme, indirme, paylaşma |
| `institution` | 2 | kurum bilgisi; **`GET` anonim** (giriş ekranı da okur), `PUT` `sistem.kurum` ister |

> `ManifestController` bu tablonun dışında: `Controllers/` altında, rotası
> kökte (`GET /manifest.webmanifest`) ve `/api/v2` yüzeyine ait değil.

### Protokol kategorileri ve davet listeleri

Kategori artık AYRI TABLO (`ProtokolKategori`). Serbest metin olduğu sürece
aynı kategori "Mülki İdare"/"Mülki idare"/"MÜLKİ İDARE" diye üç grup üretiyor
ve liste bölünüyordu. Migration mevcut metinleri **koruyarak** taşır
(`20260812160507_ProtokolKategoriVeDavet` içindeki `migrationBuilder.Sql`
blokları); kategorisiz kalanlar "Diğer"e düşer. Ad benzersizdir, büyük/küçük
harf duyarsız.

**Davet listesi** protokolden seçilen kişilerin takibi (`Davet` +
`DavetKisi`). Kişi bilgisi protokol kaydından OKUNUR, kopyalanmaz — unvan
değişince davet listesi de güncel kalır. Takip iki eksende:
**eylem** (`Arandi`, `MesajGonderildi`) ve **cevap** (`DavetDurumu`). Tek
enum'a sıkıştırılsaydı "arandı ama cevap yok" ile "hiç aranmadı" ayırt
edilemezdi. Davetler birime aittir; `DavetServisi.GorunurOlanlar` kapısı.

`GET protokol/{id}/davetler` — kişinin **davet geçmişi**: hangi törene
çağrıldı, ne cevap verdi, ne not düşüldü. Telefonu elinde tutan kişi aramadan
önce geçen seferi bilmek istiyor. Liste birim süzgecinden geçer (protokol
kaydı kurum geneli, davet listeleri birime ait) ve durum etiketi **sunucuda**
üretilir — iki istemcide ayrı kurmak, enum'a yeni değer eklendiğinde birinin
"bilinmeyen" göstermesi demekti.

PDF dört türde (`DavetCiktiTuru`): takip · telefon listesi · boş katılım
(imza sütunlu) · boş protokol. Her biri ayrı bir işte kullanılıyor; tek "her
şey" çıktısı hiçbirinde işe yaramıyordu.

> QuestPDF lisansı **her PDF üreten sınıfın statik kurucusunda** ayarlanmalı —
> statik kurucu yalnızca kendi sınıfı için çalışır, `DisaAktarmaServisi`'ndeki
> ayar `DavetCiktiServisi`'ni kapsamaz (ilk denemede 500 verdi).

### Çiçekçi dosyası ve protokol kesme kartları

İki uç, iki yeni ekranın karşılığı:

- `GET cicek/cicekciler/{id}/detay?baslangic=&bitis=` — çiçekçinin
  talimatları **bağlı oldukları programla birlikte**. Eski
  `…/talimatlar` ucu yalnızca çiçeğin kendi alanlarını döndürüyordu;
  "hangi program içindi" bilgisi yoktu ve ay sonu hesaplaşmasında her satır
  için ayrıca etkinliğe bakmak gerekiyordu. Aynı süzgeçle
  `…/excel` ve `…/pdf`.
  > Süzgeç talimatın **oluşturulma** tarihine göre, gönderilmeye göre değil:
  > henüz gönderilmemiş talimatlar da dönemin içinde sayılmalı. Bitiş günü
  > **dahil** (`< bitis + 1 gün`).
  > **Birim kapısı yok ve bu bilinçli**: çiçek talimatı zaten yalnızca gizli
  > olmayan etkinliklerde üretiliyor, ama çiçekçi hesabı kurum geneli bir iş —
  > talimatı veren birim ile ödemeyi yapan birim aynı olmayabiliyor.

- `GET davet/{id}/kesme-kartlari/pdf` ve `GET davet/{id}/masa-kartlari/pdf` —
  tören isimlikleri (`Services/V2/IsimKartiServisi.cs`). Kesme kartı A4'e
  ızgara hâlinde dizilip makasla kesiliyor; masa kartı ortadan katlanan çadır
  kart. Katalog `GET davet/kart-tasarimlari` ile okunur: **10 kesme + 10 masa
  tasarımı** (`Services/V2/KartTasarimlari.cs`).
  > **Kaynak PROTOKOL DEFTERİ DEĞİL, DAVET.** İlk sürüm defterin tamamını
  > basıyordu; masaya konacak isimlik kurumun bütün protokol listesi değil, o
  > törene çağrılanlar. Varsayılan süzgeç `durum=Katilacak` — bütün davetliyi
  > basmak boş sandalyelere isimlik koymak demek. Protokol ekranındaki kart
  > penceresi bu yüzden kaldırıldı.
  > **Kesme payı kart İÇİNDE değil ARASINDA**: hücrelerin dışına çizgi çizip
  > araya boşluk koymak makası iki çizgi arasında yürütüyor ve kartlar farklı
  > boyda çıkıyordu. Izgara bitişik, çizgiler ortak.
  > **Masa kartında üst yarı 180° dönük basılır**: kâğıt katlandığında o yüz
  > düz durup konuğa bakıyor. Dönük basılmasaydı katlanan yüz baş aşağı
  > çıkardı.
  > **Yerleşim SABİT ÖLÇÜLÜ, `Extend()` değil.** İlk sürüm yarımları
  > `Extend()` ile büyütüp üst yüzü `Rotate(180)` ile çeviriyordu; ikisi de
  > QuestPDF'te *serbest* öğe, çocuğa sınır vermiyorlar. İçerik sayfaya
  > sığmayıp bir sonrakine taşıyordu — 6 kişilik davet 8 sayfa basıyordu
  > (beklenen 6 ve 3). Artık her yarım santimetre cinsinden ölçülü ve dönüş
  > `RotateLeft()` çiftiyle yapılıyor (düzeni koruyor). Dönüşün İÇİNE ayrıca
  > yükseklik vermek "çakışan ölçü" istisnası üretiyor — dış kutu zaten
  > sınırlı.

> **TÜRKÇE BÜYÜK HARF**: `ToUpperInvariant()` kartlarda **yanlış** —
> "Vali Yardımcısı" → "VALI YARDıMCıSı" (noktasız `ı` hiç büyümüyor, `i`
> `İ` yerine `I` oluyor). `KartTasarimlari.BuyukHarfe` `tr-TR` kültürünü
> kullanıyor: "VALİ YARDIMCISI". Kartın en büyük yazısı buydu ve baskıda
> hemen göze çarpıyordu.
  > Punto hücre yüksekliğinden **kendiliğinden** ölçekleniyor; sunucu ızgarayı
  > `1–4 × 1–12` aralığına kırpıyor.

> **KART FONTLARI DEPODA** (`Web/Fontlar/*.ttf`, SIL OFL): Fira Sans, Fira
> Sans Condensed, PT Sans, PT Serif, Crimson Text. Hepsinin Türkçe kapsamı
> (ğĞ şŞ ıİ çÇ öÖ üÜ) `cmap` üzerinden ölçüldü. QuestPDF yalnızca Lato ile
> geliyor ve sistem fontuna **adıyla** güvenmek riskli: geliştirme makinesinde
> duran bir aile yayın sunucusunda olmayınca PDF sessizce başka bir fonta
> düşüyor. Kayıt açılışta bir kez (`KartTasarimlari.FontlariKaydet`), kayıt
> başarısız olsa bile çıktı üretilir — font eksikliği yüzünden 500 vermek,
> kartın biraz farklı görünmesinden kötü.

### Çiçek teslim akışı — çiçekçinin HESABI YOK

Akış baştan sona kırıktı ve üç ayrı yerden kırıktı:

| Kırık | Belirti |
|---|---|
| SMS'teki adres koda yazılıydı ve karşılığı olan MVC sayfası kaldırılmıştı | Çiçekçinin eline **ölü bağlantı** gidiyordu |
| Uçlar `[Izin(CicekGoruntule)]` + JWT arkasındaydı | Bağlantı açılsa bile **401** |
| Etkinlik detayı `Cicek`i Include etmiyordu | Rozet, çiçek teslim edilse bile hep **"bekliyor"** |

**Çiçekçi kurumun kullanıcısı değil**: hesabı, rolü, jetonu yok. Bu yüzden
teslim yüzeyi anonim (`GET/POST api/v2/cicek/teslim-karti/{guid}`) ve yetki
belirteci **bağlantıdaki GUID**. Yanıt ayrı bir DTO ile daraltıldı
(`CicekTeslimKartiDto`): etkinlik, adres, alıcı ve not var — **doğrulama kodu
yok**. Kod kartta gösterilseydi teslim kapısı hiçbir şeyi korumazdı; kod
yalnızca SMS'te geçiyor. Kaba kuvvete karşı **beş deneme**, sayaç
veritabanında (`cicekler.dogrulama_denemesi`) — bellekte olsaydı istek başka
bir sunucu örneğine gönderilerek aşılabilirdi. Kod artık
`RandomNumberGenerator` ile üretiliyor.

> Gizli etkinlikler çiçek talimatı üretmiyor, dolayısıyla bu anonim uçtan
> gizli bir kaydın bilgisi sızamaz.

#### `[AllowAnonymous]` ARTIK GERÇEKTEN ANONİM

`IzinAttribute` elle yazılmış bir `IAsyncAuthorizationFilter` ve
`[AllowAnonymous]`'u **hiç okumuyordu**. Çerçeve bunu `[Authorize]` için
kendisi yapıyor; kendi filtren kendi kapısını kuruyorsa istisnayı da kendin
yazmak zorundasın. Sınıf düzeyinde `[Izin(...)]` taşıyan bir controller'da
metoda anonim demek işe yaramıyordu.

Düzeltme sistem geneli, o yüzden bedeli de sistem genelinde ölçüldü: değişiklik
**yalnızca** `[AllowAnonymous]` VE `[Izin]` birlikte taşıyan uçları etkiliyor —
kod tabanında bunlar sadece iki yeni çiçek ucu. `oturum/giris`,
`institution`, `manifest.webmanifest` ve vatandaş portalı sınıf düzeyinde
`[Izin]` taşımıyor, yani zaten anonimdiler.

Bekçi `AnonimUcTests`: anonim yüzeyin tamamı **ad ad** kilitli (7 uç,
her biri gerekçesiyle). Yeni bir `[AllowAnonymous]` testi düşürür — artık o
işaret gerçekten kapıyı açtığı için bilinçli bir karar olmak zorunda. Test
ayrıca filtrenin **erken çıktığını** kanıtlıyor: servis sağlayıcı bilerek boş,
filtre kapıya uğrarsa istisna ile düşer.
`IzinUcKapsamiTests` ("her yazma ucu kendi iznini ilan eder") anonim uçları
bu listeye devrediyor — boşluk kalmıyor, kapı değişiyor.

#### Etkinlik detayında üç durum

`GetAsync(long id)` artık `Cicek`i de Include ediyor. Liste ucu ediyordu,
detay etmiyordu: yanıtta `cicekId` dolu, `cicek` **null**. Sessiz bir hata —
eksik `Include` istisna atmaz, sorgu çalışır, alan boş kalır. İstemcinin
elinde yalnızca "talimat verilmiş mi" bilgisi vardı.

| Rozet | Anlamı |
|---|---|
| Sarı üçgen | Talimat verildi, teslim edilmedi |
| Yeşil onay | Teslim edildi |
| Yok | Talimat verilmemiş |

Bekçi `CicekAkisTests` (kaynak taraması — hata davranışta sessiz olduğu için).
**Ateş ettiği ölçüldü:** `Include(a => a.Cicek)` silindiğinde test düştü,
geri konunca yeşile döndü.

> Kurum içi `CicekDto` doğrulama kodunu taşımaya **devam ediyor** (v1
> sözleşmesi ve personel teslimi elle işaretleyebilmeli). Anonim DTO'nun
> ondan ayrı tutulmasının tek sebebi bu; `AnonimUcTests` kod taşımadığını
> kilitliyor.

**Ölçüm (uçtan uca, jetonsuz):** anonim kart 200 · yanıtta kod yok · yanlış
kod 400 ("kalan deneme") · doğru kod 200 · teslim ve tarih işaretlendi ·
etkinlik detayı `gonderildi: true` · görsel tur 218 görüntü, taşma 0, JS
hatası 0 · 579 sunucu testi yeşil.

Sözleşme anlık görüntüleri +1 kolon (`dogrulama_denemesi`) ve +10 JSON alanı
aldı; **hiçbir ad silinmedi ya da değişmedi** — v1 mobil sözleşmesi bozulmadı.

### Halk Günü

Vatandaşın makamda sırayla dinlendiği günler. Akış: **havuz → gün → dilim →
atama → salon → talep**. Dört tablo, `Davet`/`DavetKisi` deseninin aynısı —
liste ayrı, kişi ayrı, katılım ayrı:

| Tablo | Ne tutar |
|---|---|
| `HalkGunuBasvuru` | bekleyenler havuzu; kişi bilgisi + konu + **`TelefonSade`** |
| `HalkGunu` | gün (tarih, konum, durum, birim) |
| `HalkGunuDilim` | zaman dilimi (`Baslangic`–`Bitis`, `Kapasite?`) |
| `HalkGunuKatilim` | atama + görüşme sonucu (`Durum`, not, `DegerlendirmeyeEsas`, `OlusanRandevuId?`) |

- **Kişinin tek güvenilir anahtarı telefon.** Vatandaş tablosu yok; ad yazımı
  her kayıtta değişiyor. `TelefonSade` yazarken `Telefon.Duzelt` ile
  üretilir. Var olan `randevular`/`ajandalar` tablolarına kolon EKLENMEDİ
  (canlı ve kalabalık); onun yerine migration'da **ifade indeksi** var:
  `ON randevular ((regexp_replace(coalesce(telefon,''), '\D', '', 'g')))`.
  Geçmiş sorgusu o tablolarda `FromSqlInterpolated` ile aynı ifadeyi kullanır
  — EF `regexp_replace` çeviremez. Eşleşme **son 8 hane** üzerinden: aynı
  numara `0541 298 34 50`, `05412983450`, `+90 541 298 34 50` ve
  `541 298 34 50` diye dört türlü yazılmış hâlde veritabanında duruyor.
- **Kapasite sunucuda zorlanır.** Dilime kapasitesinden fazla kişi atanınca
  400 döner; istemcide saymak "3 / 1 kişi" yazan bir ekran üretiyordu.
- **Sıralama tek çağrıda, dizinin tamamı yeniden yazılır** (`POST /{id}/siralama`).
  Tek tek `SiraNo` güncellemek, iki kullanıcı aynı anda sıralarken çakışan
  numaralar bırakıyordu.
- **"İlgilenilecek" işareti otomatik talep AÇMAZ.** `DegerlendirmeyeEsas`
  yalnızca işaret; dönüşüm `POST /katilim/{id}/talep` ile ve **bir kez**
  (ikinci çağrı `BusinessRuleException`). Talep açarken `mahalle_id` NOT NULL:
  seçilmemişse varsayılan mahalle/tip/durum kullanılır, hiçbiri yoksa anlaşılır
  bir iş kuralı hatası döner — 500 yerine.
- **Toplu SMS** `SmsYerTutucu.HalkGunuKatalog` ile: `{ad}`, `{soyad}`,
  `{adSoyad}`, `{tarih}`, `{saat}`, `{sira}`, `{konum}`. Telefonu boş kayıt
  atlanır, gönderilen katılıma `SmsTarihi` damgalanır (ikinci gönderimde kimin
  aldığı belli olsun).
- **İzinler sekiz tane** ve hepsi `halkgunu.*`: `goruntule`, `yonet`,
  `basvuru`, `atama`, `gorusme`, `sms`, `ciktiAl`, `talepOlustur`. Alan adı
  **tek kelime ve küçük harf** olmak zorunda — katalog senkron testinin
  regex'i `[a-z]+\.[a-zA-Z]+`; `halkGunu.*` testi kırardı.
- Controller sınıf düzeyinde `[Izin(HalkgunuGoruntule)]`, yazma uçları kendi
  iznini ayrıca ister. Görünürlük birim kapısından geçer
  (`GorunurGunler`/`GorunurBasvurular`) — sistemin geri kalanıyla aynı.

**Çıktılar üç tür, ikisi de biçim** (`GET {id}/excel` · `GET {id}/pdf`,
`tur=` ve isteğe bağlı `dilimId=`):

| `tur` | Ne için |
|---|---|
| `0` Program | Gün başlamadan elden ele dolaşır: Sıra · Saat · Telefon · Ad · Konu |
| `2` Katılım çizelgesi | Salonda elle işaretlenir; son iki sütun **boş kutu** |
| `1` Sonuç raporu | Gün bitince Özel Kalem'de: durum, görüşme notu, takip işareti |

- Liste **gruplanır** ve her grubun sıra numarası 1'den başlar: salonda
  çağrılan şey "günün 14. kişisi" değil, "ikinci grubun 4. kişisi".
- `dilimId` verilirse yalnızca o grup basılır — kapıdaki görevlinin kâğıdı
  bütün günü göstermiyor.
- Telefon **tek biçime getirilerek** yazılır (`0532 111 22 33`); aynı numara
  veritabanında üç ayrı yazımla duruyor ve basılı listede alt alta üç biçim
  okunmuyordu.
- Excel'de otomatik süzgeç **yok**: sayfada birden çok başlık satırı var,
  süzgeç ilkini tablo sanıp grup başlıklarını gizliyordu. Bu tablo süzülmek
  için değil basılmak için.
- Sütun başlıkları **her grupta tekrarlanır** — liste ikinci sayfaya taşınca
  hangi sütunun ne olduğu kayboluyordu.

**Akış: havuz önce, gün sonra.** Vatandaş geldiğinde kaydı açılır (ad, telefon,
konu, not) ve havuzda bekler; halk günü geldiğinde bu havuzdan **bazıları**
bir zaman dilimine atanır. Bu yüzden iki karar ayrı ayrı kayıtlı:

- **Ret** (`POST /basvuru/{id}/reddet`) — görüşme uygun görülmedi.
  `BasvuruDurumu.Reddedildi` + gerekçe + tarih. Kayıt SİLİNMEZ: "kaç kişi geri
  çevrildi" ve "bunu neden çevirmiştik?" sorularının cevabı ancak öyle
  duruyor. Kişi bir güne ATANMIŞSA o atama kaldırılır — ret kararı liste
  kurulduktan sonra geldiğinde vatandaş salonda çağrılmaya devam ediyordu;
  **görüşülmüş** kayda dokunulmaz. `POST /basvuru/{id}/geri-al` kararı geri
  alır. `Iptal`den ayrı tutuldu: iptalde vazgeçen vatandaş, retde karar
  makamın.
- **Taşıma** — `POST /{id}/siralama` yalnızca sırayı değil `dilimId`'yi de
  yazar; kişiyi başka saate almanın yolu bu. **Kapasite burada da denetlenir**
  (`SiralamaGuncelleAsync`): denetim yalnızca atama ucunda dururken taşıma
  sınırı sessizce aşıyordu.
- "Bekleyenler" listesi (`atanmamis=true`) reddedilenleri **göstermez**;
  yoksa geri çevrilmiş bir kayıt yanlışlıkla yeniden atanırdı.

`GET /halk-gunu/kisi-gecmisi?telefon=` dört kaynağı tek yanıtta toplar:
talepler · etkinlik irtibatları · önceki halk günü katılımları · protokol
kaydı. Salon operatörü "bu vatandaşı daha önce gördük mü?" sorusunu
konuşmanın ortasında ayrı bir ekranda aramıyor.

### Birimlere SMS: sonuç SAYIYLA döner

`POST /api/v2/etkinlik/sms` artık `SmsGonderimSonucuDto` döndürüyor:
`gonderilen`, `telefonsuzKisiler`, `bosBirimler`, `ozet`.

Uç eskiden yalnızca `true` dönüyordu ve arayüz her durumda "SMS gönderildi"
yazıyordu — **hiç mesaj yazılmamış olsa bile**. Telefon numarası olmayan
kullanıcı `SendSmsToBirimAsync` içinde sessizce atlanıyor, seçilen birimin hiç
kullanıcısı olmayabiliyor; ikisi de "gönderdim ama gelmedi" şikâyetinin
sebebi ve ikisi de ekranda görünmüyordu. Üçü de düzeltilebilir şeyler —
görünmedikleri sürece düzeltilmiyorlardı.

> v1 imzası (`Task<bool> SendSmsToBirimAsync`) **değişmedi**; yeni metot
> `SendSmsToBirimDetayliAsync` eklendi ve v1 onu çağırıp `bool`a indiriyor.
> Mobil model eski `true` yanıtını da tanıyor (sahadaki eski sunucu).

### Özgeçmiş havuzu

"Elimizde kaynakçı var mı?" sorusunun cevabı. İş talebiyle gelen özgeçmiş
`Randevu.OzgecmisDosya` alanında duruyordu: talebe bağlı, tek dosya, aranabilir
bir bilgi taşımıyor — cevap için talepleri tek tek açmak gerekiyordu.

`ozgecmisler` tablosu o kaydın YERİNE geçmez, **üstüne** gelir:

- Talebe özgeçmiş yüklendiğinde (`RandevuService.UploadOzgecmisAsync`) havuzda
  da bir satır açılır ve `RandevuId` ile hangi talepten geldiği yazılır.
  **Dosya kopyalanmaz**, ikisi aynı dosyayı gösterir. Aynı talebe ikinci kez
  yükleme yeni satır AÇMAZ, var olanı günceller.
- Migration canlıdaki talep özgeçmişlerini **geriye dönük doldurur**; havuz
  boş açılsaydı modül ilk gün işe yaramazdı.
- İki kaynak **tek tabloda**: sorgu anında birleştirmek (UNION) sayfalamayı ve
  sıralamayı bozuyordu. Süzgeç `kaynak=havuz|talep`.

> **Görünürlük birime bağlı DEĞİL** — sistemin geri kalanının tersi. Havuzun
> varlık sebebi kaydın birimler arasında dolaşabilmesi: bir müdürlüğün
> elindeki özgeçmişi, işe alacak olan başka müdürlük de görmeli. Kapı tek:
> `ozgecmis.goruntule`. Birim yine de kaydediliyor ve **süzgeç** olarak
> sunuluyor. `OzgecmisHavuzuTests.Havuz_birim_suzgecinden_gecmez` bu kasıtlı
> istisnayı kilitler — "tutarlılık" adına eklenecek bir birim süzgeci modülü
> işlevsiz bırakır.

- **Telefon `TelefonSade` sütununda aranır** ve ARANAN metin de aynı şekilde
  sadeleştirilir: `+90 532…` yazınca ham rakamlar `905326042211` oluyor ama
  sütunda `05326042211` var; ülke kodu eşleşmeyi düşürüyordu.
- **Paylaşım** (`POST {id}/paylas`) `ozgecmis_paylasimlari` tablosuna kayıt
  düşer ve alıcıya bildirim gider. Bildirim `NotificationAction.None` ile:
  `NotificationEntity.Ozgecmis` değerini tanımayan eski mobil sürümler varlığı
  sessizce `talep`e düşürüyor ve var olmayan bir talebi açmaya çalışırdı.
  Alıcı kaydı açtığında `GoruntulemeTarihi` damgalanır — "gönderdim ama
  bakmadı" ile "hiç göndermedim" ayrı bilgi.
- **Silme yumuşak** ve dosya diskte kalır: aynı dosya bir talebe de bağlı
  olabilir ve yanlışlıkla silinen bir özgeçmişi geri getirmenin başka yolu yok.
- Dosya `GET {id}/dosya` ile, **kimlik denetimli** iner. Statik
  `/uploads/ozgecmis` yolu v1 ve eski MVC için açık duruyor ama özgeçmiş
  kişisel bir belge; yeni istemciler bu ucu kullanır.
- İzinler: `ozgecmis.goruntule` · `ekle` · `duzenle` · `sil` · `paylas`.

### Yönetim: birim detayı ve rol üyeliği

`GET yonetim/birimler/{id}` birim detayını (sayaçlar + birimdeki kullanıcılar),
`GET/POST/DELETE yonetim/roller/{ad}/kullanicilar[/{kullaniciId}]` rol
üyeliğini yönetir. İki kural sunucuda:

- Korumalı role (`Sistem`, `BaskanOzel`) atama yalnızca `Sistem` yetkisiyle —
  `UnauthorizedAccessException` → **403**. (İlk sürümde `BusinessRuleException`
  atıyordu ve 400 dönüyordu; istemci bunu "girdini düzelt" diye okuyup formu
  yeniden gönderiyordu.)
- Kullanıcının **son rolü** çıkarılamaz: rolsüz kullanıcı giriş yapıyor ama
  hiçbir politikadan geçemiyor, boş bir kabuk görüyordu.

`OturumKaydiSuzgeci` artık `KullaniciId`, `IpAdresi` (ILike **ön ek** —
`192.168.` bir ağ bloğunu süzer), `Baslangic`, `Bitis` ve `Basarili` alıyor;
hepsi VE'lenir (`OturumKaydiTests`).

### v1 → v2 geçişi — BİTTİ

**Mobil uygulama tamamen v2'de.** Ölçüldü: `workcollab/lib` altında
`/api/XxxApi` biçiminde **tek bir çağrı yok**; mobil deposundaki
`test/clients/v2_yuzeyi_test.dart` bunu kaynak tarayarak kilitliyor.

`../workcollab/test/e2e/v2_sozlesme_test.dart` her iki yüzeyi **çapraz**
koşturmaya devam ediyor (bir yüzeyde yaz, diğerinde oku) — v1 sözleşmesinin
bozulmadığını kanıtlayan ölçüm bu.

> **v1 KALDIRILMIYOR.** Sahadaki eski uygulama sürümleri hâlâ ona bağlı ve
> güncelleme almamış bir telefon, uç kapatıldığı gün çalışmayı bırakır.
> `Controllers/Api/*` dokunulmadan duruyor; kural değişmedi.

**İyi haber:** v2 çekirdek uçlarda **v1'in DTO'larını yeniden kullanıyor** —
`AjandaDto`, `RandevuDto`, `OneriDto`, `UserSettingDto`, `AjandaSeriDto`,
`AjandaIstatistikDto`. Mobilin modelleri değişmeden v2'ye gidiyor; geçiş
"DTO yeniden yazımı" değil, adres değişikliği. Şekil farkı yalnızca **liste**
uçlarında (`SayfaliSonuc<T>` zarfı) ve takvimde (`EtkinlikOzetDto`).

Eşdeğerlik çalışması sırasında kapatılan v2 boşlukları:

| v1 ucu | v2 karşılığı |
|---|---|
| `GET/POST AccountApi/Settings` | `GET/PUT oturum/tercihler` (aynı `UserSettingDto`) |
| `GET SettingsApi/UpdateFcmToken` | `POST/DELETE bildirim/mobil-jeton` (gövdeyle + jeton geri çalma) |
| `GET AjandaApi/{id}/Seri` | `GET etkinlik/{id}/seri` |
| `GET RandevuApi/CountByTip` | `GET talep/tip-sayaclari?arsiv=true` |
| `GET AjandaApi/CountByDay/{ay}/{yil}` | `GET takvim/sayac?yil=&ay=` |
| *(yok)* | `GET talep/dosya/{dosyaId}` — kimlik denetimli indirme |
| `POST RandevuApi/{id}/UploadOzgecmis` | `POST talep/{id}/ozgecmis` |
| `GET AjandaApi` | `GET etkinlik` |
| `POST AjandaApi/GetByDate` | `POST etkinlik/gune-gore` |

> `takvim/aralik`, `etkinlik/gune-gore`'nin yerine geçemez: o, takvim çizimi
> için **özet** model (`EtkinlikOzetDto`) döndürür; mobilin gün listesi tam
> `AjandaDto` alanlarına (irtibat, bilgi notu, çiçek durumu) bakıyor.

> **Bu paragraf bir dönem YANLIŞTI ve yanlış yönlendirdi.** Önce "mobil hâlâ
> v1'de", sonra "geçiş anahtarlarla yapılıyor, v1 yolu derlemede duruyor
> (`v2_gecisi.dart`)" yazıyordu. İkisi de artık doğru değil: anahtarlar
> sökülmüş, `v2_gecisi.dart` diye bir dosya YOK ve yerine kaynak tarayan bir
> bekçi konmuş. Bayat kaldığı süre boyunca "mobil hangi yüzeyde" sorusuna
> yanlış cevap verdi — **yanlış döküman, dökümansızlıktan kötüdür.**

> **Talep dosyaları statik yoldan açık.** `wwwroot/uploads` altındaki her şey
> kimlik doğrulanmadan servis ediliyor ve v1 ile eski MVC arayüzü buna bağlı;
> bu yüzden kapatılmadı. Mobil eskiden adresi elle kurup işletim sistemine
> devrediyordu — istek uygulamanın dışına çıktığı için jeton taşımıyordu ve
> özgeçmiş gibi kişisel belgeler de aynı yoldan iniyordu. Yeni istemciler
> `talep/dosya/{id}` ucunu kullanır; orası **birim süzgecinden** geçer
> (`TalepDosyaIndirmeTests`).

**Dikkat edilecek davranış farkları:**

- **Talep sayaçları arşivi sayar.** v1'in `CountByDurum`/`CountByTip` uçları
  YALNIZCA `Arsivlendi` kayıtları sayıyor (`RandevuService.cs`) — mobil bunları
  Arşiv sekmesinde kullandığı için kasıtlı. v2'nin varsayılanı aktif
  taleplerdir; v1 ile birebir eşleşme için `arsiv=true` gerekir.
- **RRULE normalleştirilir.** Sunucu `INTERVAL=1`'i düşürür; istemci
  gönderdiği metnin aynen geri geleceğini varsayamaz.
- **Hata gövdesi RFC 7807 ve alan adları İNGİLİZCE** (`type`/`title`/`status`/
  `detail`/`instance`), metinler Türkçe. Üstelik **iki farklı 400 şekli** var:
  DataAnnotations → `errors{}` (ASP.NET ModelState), FluentValidation →
  `detail`. `ApiError.mesaj` geçişte ikisini de tanımalı.
- **v1'in `Me` yanıtı kullanıcı adını hiç taşımıyor**; v2'nin `oturum/ben`
  taşıyor ve ayrıca **çözülmüş yetki listesi** (`yetkiler`) veriyor.
- `oneri` v1'de `Ajanda` politikası ister, v2'de istemez (v2 doğrusu).

### Talepten etkinliğe

Akış: memur talebi girer → yetkili onaylar → **ekleyen personel** takvime
koyar. Onaylayan ile ekleyen aynı kişi değil, arayı bildirimler bağlıyor.

- `POST talep/ajandaya-ekle` → `{ etkinlikId }`. Kimlik dönüyor ki istemci
  oluşan etkinliğe gidebilsin; eskiden düz `true` dönüyor, kullanıcı etkinliği
  takvimde elle arıyordu. `baslangicTarih` serbest — görüşme çoğu zaman ileri
  bir tarihe veriliyor.
- **Durum değişimi bildirimi TALEBE** götürür (kullanıcı onu ajandaya
  ekleyecek), **ajandaya eklendi bildirimi ETKİNLİĞE**. İkisi karışırsa
  kullanıcı geldiği yere geri gönderiliyor.
- `IRandevuService.TalebiEtkinligeCevirAsync` — v1'in `RandevuToAjandaAsync`
  imzası korunuyor (sözleşme) ama o `catch { return false; }` ile hatayı
  yutuyordu; yeni metot fırlatıyor ve kayıt sistem hatalarına düşüyor.

> **`randevu.AjandaDurum` bayrağı HİÇ yazılmıyordu.** Etkinlik oluşuyor ama
> talep bunu bilmiyor: listede "Ajandada: Hayır" görünüyor ve "ajandaya
> eklenmemiş" süzgeci her şeyi döndürüyordu. Aynı yerde `Ajanda.KullaniciId`
> (kullanıcı ADI) da boş kalıyordu — etkinliğin sahibi olmuyordu.

`durum-sayaclari` ucu `ajandayaEklendi` alıyor: süzgeç açıkken çipler tüm
kümeyi saymaya devam ederse rakamlar listeyle çelişir.

### Silinmiş kayıtlar İKİ uçtan okunuyor

SPA `GET etkinlik/silinmis`, mobil ise `POST etkinlik/ara` +
`SilinmisFiltre.Silinmis`. **İkisi aynı kümeyi aynı sırada vermeli**; ayrışınca
kullanıcı aynı hesapta bir yerde 8, diğerinde 80 kayıt görüyor ve hangisinin
doğru olduğunu bilemiyor. Ayrışmanın iki sebebi vardı, ikisi de
`SilinmisSiralamaTests` ile kilitlendi:

- **Sıralama anahtarı.** `SearchAsync` etkinlik tarihine göre sıralıyordu;
  silinmiş listesi artık **silinme tarihine** (`GuncellemeTarihi`) göre
  sıralanır — çöp kutusunda merak edilen "ne zaman yapılacaktı" değil "ne
  zaman silindi". Aktif liste etkinlik tarihinde kalır.
- **Dönem sınırı.** `gun` parametresi duruyor ama SPA artık varsayılan olarak
  sınırsız istiyor (`gun=0`). 30 günlük varsayılan kayıt gizliyordu.

### Silinmiş etkinlikler listesi

`GET etkinlik/silinmis` **silinme tarihine göre** sınırlıdır (`gun`,
varsayılan 30; `gun=0` sınırı kaldırır). Uç bir dönem tüm silme geçmişini
döndürüyordu ve ekranda yıllar öncesinin kayıtları bugünkülerle karışıyordu —
liste "geri alma" işine yaramıyor, alakasız görünüyordu. Sınır silinme
tarihine göre çünkü bu bir çöp kutusu: merak edilen "ne zaman yapılacaktı"
değil, "ne zaman silindi".

Silinme anı ayrı bir sütunda değil, soft-delete'in yazdığı
`GuncellemeTarihi`nde; ayrı sütun eklemek canlıdaki kayıtları geriye dönük
dolduramazdı.

### Veritabanı kısıt ihlalleri 400'e çevrilir

`V2HataFiltresi` Postgres `SQLSTATE` kodlarını iş kuralı hatasına çeviriyor:
`23503` (yabancı anahtar) · `23502` (NOT NULL) · `23505` (benzersizlik).
Gövdede var olmayan bir kimlik geldiğinde (silinmiş bir etkinlik tipi) istek
**500** dönüyor, kullanıcı "beklenmeyen bir hata" görüyor ve
`sistem_hatalari`'na kayıt düşüyordu; gerçekte söylenmesi gereken tek şey
"geçersiz seçim"di. Kod iç istisnadan **tip adıyla** okunuyor — filtre
Npgsql'e derleme zamanı bağımlılık kurmuyor. Mesaj kullanıcıya ham verilmez:
kısıt adı tablo/kolon yapısını ele veriyor.

### Sistem hataları

Sunucuda beklenmeyen bir hata oluştuğunda `sistem_hatalari` tablosuna kayıt
düşer: tarih, kullanıcı, birim, IP, istemci, uç, sorgu dizesi, **istek
gövdesi**, başlıklar, dosya+satır, HTTP kodu, tam mesaj ve yığın izi.

Konsol günlüğü sunucu yeniden başlayınca kayboluyordu; kullanıcı "hata aldım"
dediğinde geriye bakacak hiçbir şey yoktu.

- **Erişim yalnızca `Sistem` rolü** — Admin bile göremez. Kayıtlarda istek
  gövdeleri, IP adresleri ve yığın izleri var: hem kişisel veri hem saldırı
  yüzeyini tarif eden bilgi.
- **Aynı hata yeni satır açmaz**, `adet` artar. Parmakizi tür + dosya + satır
  + ilk kendi karemizden üretilir; **mesaj bilerek dışlanır** çünkü içinde
  değişken değerler oluyor ("42 kimlikli kayıt bulunamadı") ve her biri ayrı
  satır açardı.
- **Çözüldü işareti, hata yeniden görülünce otomatik kalkar.**
- `parola`, `jeton`, `Authorization`, `Cookie` **maskelenir**; çok parçalı
  istek gövdeleri hiç saklanmaz.
- `HataDetayDto.AiRaporu` — kopyalanıp doğrudan bir yapay zekâ ajanına
  verilebilen Markdown rapor. Ham döküm değil: teknoloji bağlamı, ne olduğu,
  hangi istekle tetiklendiği, gövde, yığın izi ve **ne beklendiği** sırayla.
  Sunucuda üretilir — web ve mobilde iki kez kurmak, birinin eksik kalması
  demekti.

> **Kayıt AYRI DbContext ile yazılır.** Hata çoğu zaman `SaveChangesAsync`
> sırasında oluşuyor ve o an istek kapsamındaki bağlam bozuk bir varlığı
> izliyor; aynı bağlamda kaydetmek aynı hatalı INSERT'ü yeniden denemek
> demekti — hata kaydının kendisi hatayla düşüyordu.

> İstek gövdesini okuyabilmek için `/api` altındaki isteklerde
> `EnableBuffering()` açık (`Program.cs`). Tamponsuz akış bir kez okununca
> tükeniyor ve "hangi veriyle patladı" bilgisi kayboluyordu.

### Yazıcı çıktılarında tip ve durum YOK

Günlük program HTML/PDF çıktıları ile etkinlik/talep **PDF** listelerinden
etkinlik tipi ve durum alanları çıkarıldı. İkisi de iç takip alanı: basılı
program makam odasında elden ele dolaşıyor ve "Beklemede" damgalı bir satır
orada yanlış bir şey söylüyordu.

**Excel çıktısında ikisi de DURUYOR** — tablo süzülmek ve sayılmak için var,
elden ele dolaşmak için değil. Renk şeridi de duruyor; o satırları ayırmaya
yarıyor, bir durum ilan etmiyor.

### Telefon numarası

`Application/Dto/V2/Ortak/Telefon.Duzelt` — istek DTO'larının setter'ında
çağrılır. Doğrulayıcı bitişik `0XXXXXXXXXX` istiyordu ama veritabanındaki
numaralar boşlukluydu (`0541 298 34 50`, eski MVC formundan): bir kullanıcıyı
açıp hiçbir şeye dokunmadan kaydetmek **400** veriyordu. Artık boşluk, tire,
parantez, `+90` ve baştaki `0`sız yazım tek biçime indiriliyor.

### Dosya gönderimi

Gönderilen dosyalar `wwwroot/uploads/gonderim` altında durur — **kurulum
kararı**: uygulama havuzunun oraya yazma izni zaten var, ayrı bir klasör her
yayında elle izin vermeyi gerektirir ve unutulduğunda özellik sessizce çalışmaz.

`wwwroot` altındaki her şey statik dosya ara katmanıyla **kimlik doğrulanmadan**
servis edildiği için `Middleware/GonderimDosyaKorumasi.cs` bu klasörü kapatır:
`/uploads/gonderim` altına gelen her istek 404. **`UseStaticFiles`'tan ÖNCE
eklenmelidir** — sıra bozulursa kural sessizce etkisiz kalır, bu yüzden
`GonderimDosyaKorumasiTests` sırayı da denetler.

Dosyanın tek girişi `GET /api/v2/gonderim/{id}/dosya`; o da gönderen/alıcı
süzgecinden geçer (`DosyaGonderimiTests`).

Gönderme yetkisi rol değil, kullanıcıya özel bayrak: `AppUser.DosyaGonderebilir`.
**Almak yetki istemez.** Yetki her istekte veritabanından okunur, JWT'den değil —
yetki geri alındığında jeton 15 saat daha geçerli olurdu.

### Gizli etkinlik OLUŞTURMA yetkisi

`AppUser.GizliEtkinlikEkleyebilir`, görünürlükten **ayrı** bir kural: kimsenin
başkasının gizli etkinliğini görmesini sağlamaz. Denetim
`AjandaService.GizliEkleyebilirMiKontrolAsync` içinde, yani **servis
katmanında** — controller'a konsaydı v1 (mobil) kuralı atlardı. Hem oluşturma
hem "açık etkinliği gizliye çevirme" yolu kapalı (`GizliEtkinlikYetkisiTests`).

**v2'de bilinçli olarak düzeltilenler** (v1 aynen bırakıldı):
- `PATCH /etkinlik/{id}/zaman` — sürükleme için ayrı uç; gövdesi tekrar kuralı **taşımaz**.
- `GET /oneri/benim` — kimlik oturumdan; boş sonuç `200 []` (v1 404 veriyordu,
  `IOneriService.KullaniciOnerileriAsync` bunun için **eklendi**, eski imza duruyor).
- `POST /bildirim/web-jeton` — jeton gövdede (v1 sorgu dizesinde, günlüklere düşüyor).
- Yönetimde `Sistem`/`BaskanOzel` rol kısıtı **sunucuda** zorlanır (v1'de yalnızca görünümdeydi).
- Yönetim yanıtları FCM jetonunu **dışarı vermez**, yalnızca "bağlı mı" bilgisini.
- Giriş artık **birim kaydı aramaz**: birimi olmayan (ya da birimi silinmiş)
  kullanıcı, parolası doğru olsa bile "Kayıt bulunamadı" ile giriş yapamıyordu.
  `BirimId` talebi doğrudan kullanıcıdan yazılır (`OturumServisiTests`).
- `RandevuDosyaDto`'ya `Id` **eklendi**: silme ucu kimlik istiyor ama liste
  yanıtı taşımıyordu, arayüz listelediği dosyayı silemiyordu. Alan eklemek v1
  sözleşmesini bozmaz.

## Yayın (IIS)

`dotnet publish` **ön yüzü de derler** (`npm ci` + `npm run build`), çıktıyı
`wwwroot/yeni/` altına koyar ve yayın listesine ekler. Publish yapan makinede
**node gerekir**; `-p:SkipFrontend=true` yayında KULLANILMAZ (o durumda `/yeni`
404 döner). Adımlar, IIS ayarları ve yazma izni gereken klasörler:
`IIS-YAYIN.md`.

Yayın için **ek klasör izni gerekmez**: gönderilen belgeler
`wwwroot/uploads/gonderim` altına yazılır, orası zaten yazılabilir.
`Depolama:GonderimDizini` ile başka bir yola alınabilir. Uygulama açılışta bu
klasörlere yazmayı dener, başaramazsa günlüğe yazar ama başlar.

## Yeni web uygulaması

`KentOS.Mini.Web/frontend/` (Vite + React) → doğrudan **`wwwroot/`**
altına derlenir ve **kökten** (`/`) yayınlanır. Eski MVC arayüzü **aynen**
çalışmaya devam eder; onun rotaları (`/Randevu`, `/Ajanda`, `/Modules`…)
dokunulmadan geçer.

- SPA derin bağlantıları: `app.MapFallbackToFile("{*yol:nonfile}", "/index.html")`.
  **`:nonfile` kısıtı şart** — onsuz `/uygulama/index-abc.js` ve `/uploads/x.pdf`
  gibi gerçek dosya istekleri de `index.html` alırdı.
- Uygulama bir dönem `/yeni` altındaydı. Eski yer imleri ve **kurulu PWA'lar**
  bir süre daha `/yeni/...` isteyecek; `app.MapGet("/yeni/{*yol}")` onları
  **302** ile köke yönlendiriyor. 301 değil: kalıcı yönlendirme tarayıcıda
  süresiz önbelleğe alınıyor ve geri dönmek gerekirse kullanıcıların
  tarayıcısını temizletmek gerekirdi.
- **Service worker `/firebase-messaging-sw.js`, kapsam `/`.** Hem web push hem
  çevrimdışı kabuk aynı worker'da: bir kapsamı yalnızca TEK worker
  denetleyebilir, ikincisi birincinin yerini alır ve push sessizce çalışmayı
  bırakır. Kapsamın kökte olması ayrıca kurulabilirliğin şartı — `start_url`i
  karşılayamayan worker'la tarayıcı uygulamayı kurulabilir saymıyor.

Ayrıntı ve bütün arayüz kuralları: `KentOS.Mini.Web/frontend/CLAUDE.md`.

## DİNAMİK FORM VE ANKET

Kurumun kendi tasarladığı formu vatandaşa benzersiz bir adresle sunması ve
yanıtları toplaması. Google Forms sınıfı bir tasarımcı + anonim bir portal.

### Veri modeli MELEZ — tanım ve yanıt JSONB, iskelet ilişkisel

| Tablo | Ne tutar |
|---|---|
| `formlar` | Başlık, durum, erişim kipi, tarih/kota, **erişim anahtarı** |
| `form_surumleri` | **Donmuş tanım** (`tanim jsonb`) — her yayın bir sürüm |
| `form_yanitlari` | Cevaplar (`cevaplar jsonb`) + takip no + kimlik |
| `form_yanit_dosyalari` | Yüklenen belgeler (gizli alanda) |

- **Tanım JSONB** çünkü bir ağaç: adım → grup → alan, üstüne koşul, kolon
  düzeni ve tipe göre değişen ayarlar. İlişkisel modellenseydi altı tablo,
  her sıralamada toplu güncelleme ve tip başına ayar tablosu gerekirdi.
  Tanım **bir bütün** okunup **bir bütün** yazılıyor.
- **Yanıt JSONB** çünkü soru başına satır her okumada pivot demek.
  PostgreSQL 18 + GIN indeksi `"şu soruya X diyenler"` sorgusunu doğrudan
  karşılıyor. **Ölçüldü:** `puan=4` → 2 yanıt, `puan=1` → 0.
- **Sürümleme şart.** Yayınlanmış form düzenlenebiliyor; tanım form
  kaydında dursaydı düzenleme eski yanıtların şemasını da değiştirir ve
  "3. soruya ne cevap verilmişti" sessizce bozulurdu.

> Bu depoda ilk `jsonb` kullanımı. Kolon tipi `AppDbContext`'te açıkça
> `jsonb` (metin değil): asıl kazanç GIN indeksi ve yol sorgusu.
> C# tarafı `string` çünkü `Application` katmanının NuGet bağımlılığı yok
> ve serileştirme tek yerde, denetimli kalmalı.

### Cevap belgesi SARMALAYICI

```json
{ "a_7f3a": { "deger": "diger", "metin": "Belediye afişi" } }
```

Düz değer "Diğer" seçeneğinin yanındaki serbest metni taşıyamıyor; yan bir
`a_7f3a__diger` anahtarı ise **"tanımda olmayan anahtar reddedilir"**
kuralıyla çatışırdı. Containment sorgusu bozulmuyor:
`cevaplar @> '{"a_7f3a":{"deger":"evet"}}'`.

### ERİŞİM ANAHTARINDA BAŞLATICI YOK

```csharp
public string ErisimAnahtari { get; set; } = string.Empty;   // ✓
// = Guid.NewGuid().ToString("N")                            // ✗ ÖLÜMCÜL
```

Başlatıcı yazılsaydı `_mapper.Map(dto, entity)` her güncellemede alanı
varsayılana çeker ve **vatandaşın adresi sessizce dönerdi**: dağıtılmış QR
kodları, SMS bağlantıları ve afişler ölür, hiçbir istisna atılmaz. Depoda
adı konmuş `Ajanda.KullaniciId` hatasının birebir aynısı. Değer servis
katmanında, kayıt YARATILIRKEN bir kez üretiliyor.

### Koşullar YALNIZCA GERİYE bakar

Bir alanın koşulu, kendisinden **önce** gelen bir alana bağlanmak zorunda
(`FormServisi.TanimiDogrula` zorluyor, tasarımcı da yalnızca onları
listeliyor). İki kazancı var:

- **Döngü tespiti hiç yazılmıyor.** Mapster döngüsü bu depoda bir kez
  `StackOverflowException` ile bütün API sürecini düşürdü; aynı sınıf
  hatayı yapı gereği imkânsız kılmak testle yakalamaktan ucuz.
- **Tek geçişte oynatma.** Sunucunun "bu soru zorunlu muydu" kararını
  yeniden üretebilmesi gerekiyor; sıra garantisi olmadan tanımsız.

Koşul **bağlaçlı liste** (VE/VEYA, en çok 8 kural). Tek koşul "işyeri VEYA
inşaat ise" gibi sıradan bir isteği karşılamıyordu. İç içe ifade ağacı
elendi: `A ve (B veya C)` zaten **grup koşulu ∧ alan koşulu** ile
yazılabiliyor.

### Kolon düzeni: 12'lik ızgara

Masaüstünde ızgara **daima 12 kolon**; `kolonSayisi` DOM'a hiç inmiyor,
yalnızca tasarımcıda yeni alanın varsayılan genişliğini belirliyor
(`12 / kolonSayisi`). Böylece 3 kolonlu bir grupta bir alan "iki kolon
kapla" diyebiliyor — 1–4 modelinde bu ifade edilemiyordu.

**Mobilde her alan tam genişlik.** 390px'te iki kolon, alan başına 171px
demek; etiket sığmıyor ve 44px dokunma kuralı genişliği kurtarmıyor.
`minmax(0,1fr)` pazarlıksız: `1fr` aslında `minmax(auto,1fr)` ve uzun bir
seçenek metni ızgarayı taşırıyor (bu depoda ölçülmüş 628px'lik hata).

### Matris MOBİLDE TABLO DEĞİL

390px'te altı sütunlu bir tablo okunmuyor: sütun 55px'e düşüyor, başlıklar
dikey yazıya dönüyor. Yatay kaydırma da çözüm değil — kullanıcı satır
etiketini kaybediyor. Mobilde her satır **kendi başlığıyla bir kart**,
seçenekler sarılan düğmeler.

### Güvenlik — vatandaş portalı kalıbı

Uygulamanın **ikinci anonim yazma yüzeyi**. Kararlar bildirim portalından
devralındı:

| Savunma | Nasıl |
|---|---|
| Kapalı portal | `kurum_bilgileri.form_portali_acik` + `FormPortaliFiltresi`, **`Order = -2001`** (ölçülmüş sayı: `[ApiController]`'ın kendi filtresi `-2000`'de) → **404**, 403 değil |
| Ayrı yüzey | `/api/v2/form-portal`, ayrı controller; `V2ControllerBase`'den türemiyor (o JWT zorunlu kılıyor) |
| Hız sınırı | Okuma 60/dk, yazma 10/dk; bölüm anahtarı **`ip\|erisimAnahtari`** — aynı NAT'tan 300 kişilik bir okul aynı ankete girebiliyor |
| Bot | **Honeypot**: dolu gelen gizli alan sessizce başarılı görünüp atılıyor. Hata dönseydi bot tuzağı öğrenirdi |
| Doğrulama | Tanımda olmayan alan **reddedilir**; seçenek listesinde olmayan değer, uydurma matris satırı/sütunu da |
| Desen | Kullanıcı regex'i **50 ms zaman aşımıyla**; zaman aşımı alanı geçerli SAYMAZ |
| IP | Ham değil **tuzlanmış özet** (tuz: JWT anahtarı) |
| Tek yanıt | **Kısmi benzersiz indeks** (`WHERE kimlik_karmasi IS NOT NULL`) — `CountAsync`+`Insert` bir TOCTOU'ydu. Anahtar `HMAC(form tuzu, telefon)`: telefon anonim formda hiç saklanmıyor ve **tuz form başına**, yani iki anketteki aynı kişi eşleştirilemiyor |
| Excel | Formül enjeksiyonu kapalı: `=`, `+`, `-`, `@` ile başlayan cevap metne kaçırılıyor |

### Dosya alanı — AYRI UÇ, gizli alan

Dosya seçilir seçilmez sunucuya gidiyor ve geriye bir kimlik dönüyor;
cevapta o kimlik duruyor. Gönderimle birlikte yollamak, 12 MB'lık bir
gövdenin doğrulamada düşmesi hâlinde her şeyi yeniden yükletirdi.

> **Çiçek teslim akışındaki "tek çağrı" kararı burada geçerli değil** ve bu
> yazıya geçmeli, çünkü ilk bakışta çelişki gibi duruyor: orada ayrı uç,
> fotoğrafın **kodsuz** yüklenebilmesi demekti; burada kapı zaten adresteki
> erişim anahtarı ve ayrı uç yeni bir kapı açmıyor.

- İlk dosya bir **taslak yanıt satırı** açıyor (dosyanın bağlanacağı bir
  kayıt gerekiyor). Nullable yabancı anahtar + ikinci bir durum makinesi
  yerine zaten var olan `Taslak` durumu kullanılıyor — sürdürme özelliğini
  de bedavaya veriyor. Taslak **yanıt sayacını artırmaz**: yoksa yüz dosya
  yükleyen biri formu kotasından kapatırdı.
- Dönen `surdurmeAnahtari` gönderimde geri gelmezse yeni bir yanıt açılır ve
  dosya sahipsiz kalır — kullanıcının gördüğü "dosyayı ekledim ama kayıtta
  yok".
- Dosya **gizli alanda** (`StorageArea.Private` → `uploads/gonderim`);
  `GonderimDosyaKorumasi` o klasörü tamamen kapatıyor. Tek giriş
  `GET form/{id}/yanit/{yid}/dosya/{did}`, kimlik denetimli.

> **Ölçüldü:** `.html` → 400 (*"İzin verilenler: .pdf, .png"*) · `.png` →
> 200 + sürdürme anahtarı · gönderim dosyayı yanıta bağladı · indirme
> jetonlu **200 image/png**, jetonsuz **401** · statik yol
> `/uploads/gonderim/form/2/….png` → **404**.

### Vatandaş sayfasında AMBLEM solda

Vatandaş bu sayfayı bir SMS ya da QR koddan açıyor; karşısına çıkan ilk şey
formun hangi kuruma ait olduğu olmalı. Kurum adını yazıya bırakmak
yetmiyor — amblem tanınırlığı metinden hızlı taşıyor ve sayfanın "resmî"
olduğunu tek bakışta söylüyor.

Amblem `useInstitution()`'dan geliyor, **form yanıtından değil**: SPA marka
bilgisini açılışta bir kez yüklüyor ve son yanıtı `localStorage`'da
tutuyor. Forma özel bir amblem alanı eklemek, aynı kurumun her formunda
aynı dosyayı yeniden yönetmek demekti.

> Ölçüm: mobil 48px, masaüstü 56px, sol kenardan 16px, yatay taşma 0.
> `object-contain` — kurum amblemleri kare olmak zorunda değil ve kırpmak
> logoyu bozar.

### Portal kapalıyken SEBEBİ YAZILI

Bayrak kapalıyken vatandaş ucu 404 dönüyor. Yönetim ekranı bunu
bilmiyordu: yayınlanmış bir formun paylaşım adresi veriliyor,
kopyalanıyor, açılıyor ve *"Form bulunamadı"* çıkıyordu — sebebi hiçbir
yerde yazmıyordu.

```
ÖNCE   yayında form · adres verildi · açınca 404 · ekranda hiçbir açıklama
SONRA  liste üstünde uyarı + Kurum Bilgileri bağlantısı,
       satırda "Form portalı kapalı. Kurum Bilgileri ekranından açın."
```

`YanitDurumu` artık portal bayrağını da tartıyor, yani sebep tek yerden
üretilip listeye, detaya ve vatandaş sayfasına birden gidiyor. Bayrak
istek başına **bir kez** okunuyor: 25 satırlık liste satır başına
sorsaydı 25 sorgu olurdu.

**Adres kapalıyken de veriliyor** — gizlemek "adres yok" gibi okunurdu;
`kapaliSebebi` neden çalışmadığını zaten söylüyor.

> Vatandaş ucunda bayrak İKİNCİ KEZ sorulmuyor: isteğin oraya gelebilmiş
> olması `FormPortaliFiltresi`den geçtiği anlamına geliyor.

### CEVAP EKRANDA ETİKETLE OKUNUR

JSONB'de seçenek **kimliği** duruyor (`o_afis`, `r_temiz`). Çeviri üç ayrı
yerde kopyalanmıştı ve üçü ayrışmıştı:

| Nerede | Ne yazıyordu |
|---|---|
| Vatandaşın sonuç sayfası, Excel | doğru — etiket |
| **Özet raporu** | `satir_1: sutun_2` — kopyası etiketlere hiç bakmıyordu |
| **Yanıt detayı (ön yüz)** | `o_afis`, `r_temiz: c_iyi` — çeviri hiç yoktu |

Tek yer artık `Services/V2/FormDegerMetni.cs`; ön yüzdeki karşılığı
`formEngine.etiketliDeger`. İkisi aynı kuralı uygular — ayrışırlarsa aynı
cevap ekranda ve Excel'de farklı görünür.

- **Matris özeti SATIR SATIR dağılım.** Eskiden "örnek cevap" bölümüne
  düşüyordu: hem anahtar hem sayılmayacak bir şey. Merak edilen "kim ne
  yazdı" değil, "Temizlik'e kaç kişi İyi dedi". Yüzde **satırın kendi
  toplamına** göre — o satırı boş bırakanlar diğer satırların yüzdesini
  bozmamalı. `FormDagilimDto.Satir` bunun için eklendi; satır adı etikete
  gömülseydi ("Temizlik → İyi") istemci gruplayamazdı.
- **Kimlik → etiket çözümü `GroupBy`, `ToDictionary` DEĞİL.** Tasarımcıda
  alan tipi seçimden matrise çevrildiğinde eski `Secenekler` listesi
  tanımda kalıyor ve kimlikleri `Sutunlar` ile çakışabiliyor;
  `ToDictionary` orada `ArgumentException` atıp yanıt ekranını düşürürdü.
- **Çözülemeyen kimlik ham hâliyle kalır.** Seçenek sonradan silinmişse
  boş göstermek "bu soruya cevap verilmemiş" demek olurdu.

> Ölçüm: `Diğer (Belediye afişi)` · `Spor, Kültür` · `Evet` ·
> `Temizlik: İyi · Ulaşım: Orta`; Excel'de 23 hücrenin hiçbirinde ham
> anahtar yok. Bekçi `FormEtiketTests` (7) + `forms.test.ts` (7); çevirici
> devre dışı bırakılınca 5 ve 4 test düştü, geri alınınca yeşile döndü.

### AYNI KİŞİ FORMU İKİ KEZ GÖNDEREMEZ

İki **ayrı** kapı; karıştırılırsa biri ötekinin işini yapıyor sanılıyor:

| Kapı | Neyi karşılar | Anahtar |
|---|---|---|
| **İdempotans** | Aynı gönderimin iki kez ulaşması — ağ yeniden denemesi, "geri" tuşu, sekme geri yüklemesi | `surdurmeAnahtari` |
| **Tek yanıt** | Kişinin bilerek ikinci kez doldurması | `kimlik_karmasi` |

**Tek yanıt ayarı yayında hiç çalışmıyordu.** Kapı
`form.TekYanit && telefonSade is not null` diyordu: telefon sormayan bir
formda ayar açık görünüyor ama **hiçbir şey yapmıyordu**. Vatandaş
gönderiyor, sayfayı yeniliyor, yeniden dolduruyordu.

`kimlik_karmasi` kolonu ve kısmi benzersiz indeksi **baştan beri vardı ama
hiçbir yer yazmıyordu** — belgelenmiş TOCTOU koruması ölü koddu.

- Kimlik sırası **telefon > cihaz anahtarı**. Telefon gerçek bir kimlik
  (`Telefon.Duzelt` sayesinde `0541 298 34 50` ile `+90 541 298 34 50` aynı
  kişi). Cihaz anahtarı **yumuşak** bir kapı: tarayıcı verisi temizlenince
  ya da başka cihazdan girilince aşılır — anonim bir formda bundan fazlası
  mümkün değil, kimliği olmayan birini "aynı kişi" diye tanımanın yolu yok.
  Telefon önce geliyor, yoksa aynı kişi numarasını yazıp bir de tarayıcı
  temizleyerek iki kez gönderebilirdi.
- Ham telefon/cihaz anahtarı **saklanmaz**: `HMAC(form.AnonimTuzu, kimlik)`.
  Tuz **form başına**, yani iki ayrı anketi dolduran aynı kişi
  eşleştirilemiyor.
- `AnyAsync` bir TOCTOU; asıl kapı benzersiz indeks. `AnyAsync` yalnızca
  anlaşılır mesaj için duruyor, ihlal `DbUpdateException` → aynı cümle.

> **KAPI SIRASI ÖLÇÜLDÜ.** İdempotans denetimi "tek yanıt"tan ÖNCE olmak
> zorunda. Ters sırada, kaydedilmiş bir gönderimin ağ yeniden denemesi
> *"Bu forma zaten yanıt verdiniz."* alıyordu — vatandaş cevabı gitmiş
> olmasına rağmen gitmemiş sanardı. İlk ölçümde tam bu çıktı (2. istek
> 400); sıra düzeltilince aynı takip numarası döndü.

> `surdurmeAnahtari` gönderimde `null`'a çekiliyordu; artık saklanıyor.
> Sayaç da bir kez artıyor — iki kez artsaydı yanıt sınırı olan bir form
> ağ yeniden denemeleriyle vaktinden önce kapanırdı.

```
ölçüm (uçtan uca, jetonsuz)
  1. gönderim                      200 · takip no
  aynı cihaz, ikinci gönderim      400 "Bu forma zaten yanıt verdiniz."
  başka cihaz                      200
  aynı anahtar iki kez             200 · AYNI takip no · sayaç 1
  aynı numara başka yazım+cihaz    400
tarayıcı (375px)  gönder → sonuç sayfası · yenile → doldur → hata görünür
```

### Bekçiler

| Test | Neyi kilitler |
|---|---|
| `FormDogrulamaTests` | Bilinmeyen alan, geçersiz seçim, matris bütünlüğü, koşullu zorunluluk, T.C. algoritması, desen zaman aşımı, `JsonElement` çözümü |
| `FormSemaTests` | **Alan tipi sayıları** (29 üye tek tek) — bir üyeyi taşımak canlıdaki bütün soruları başka tiplere çevirirdi; ayrıca veri taşıyan her tipin doğrulayıcısı var mı |
| `AnonimUcTests` | Dört yeni anonim uç ad ad kilitli |
| `FormEtiketTests` | Seçenek/matris kimliğinin etikete çevrilmesi; yinelenen kimlikte 500 vermemesi |
| `FormTekYanitTests` | Telefonsuz formda ikinci gönderimin reddi, telefonun cihazı ezmesi, idempotans, sayacın bir kez artması |
| `frontend/test/forms.test.ts` | Kimlik benzersizliği, silinen alanın koşullarının temizlenmesi, kopyada kimliklerin yenilenmesi, koşul adaylarının yalnızca geriye bakması, taşımada ileri koşulun düşürülmesi |

> **`FormSemaTests` yayına çıkmadan GERÇEK bir açık yakaladı:** matris
> alanına düz metin göndermek sessizce geçiyordu (`Sozluk()` boş sözlük
> döndürünce döngü hiç çalışmıyor). Düzeltildi.

### Ölçümler

```
uçtan uca (jetonsuz)  form aç → yayınla → vatandaş doldur → sonuç
  koşullu zorunlu boş → 400 "detay: Bu alan zorunlu"
  listede olmayan seçim → 400 "kanal: Geçersiz seçim"
  honeypot dolu → 200, KAYIT AÇILMADI
  geçerli → takip no + 7 satırlık sonuç özeti
tarayıcı (390px)      zorunlu uyarısı ✓ · koşullu alan açıldı ✓ ·
                      adım 2/2 ✓ · gönderim ✓ · sonuç sayfası 5 satır ✓
ekranlar              taşma 0 · iç içe etkileşim 0 · JS hatası 0
testler               672 sunucu + 228 ön yüz · görsel tur 218 görüntü
```

> **Tasarımcı tuvali kolonları GÖSTERMEZ**, önizleme gösterir. Tuval dikey
> bir sıralama listesi (sürükle-bırak orada çalışıyor); "form neye
> benziyor" sorusunun cevabı Önizleme sekmesinde ve o **gerçek oynatıcı** —
> tasarımcıda görülenle vatandaşın gördüğü aynı bileşen.

## İSTATİSTİK MERKEZİ — dokuz konu, gruplu ızgara

İstatistikler tek bir sayfada, iki segment düğmesinin arkasında duruyordu
(**etkinlik** ve **talep**). Konu sayısı ikiden dokuza çıkınca o düzen
taşıyor: segment şeridi mobilde satır kaydırmaya başlıyor ve kullanıcı
hangi panoların VAR OLDUĞUNU ancak şeridi kaydırarak görebiliyor.

`/istatistikler` artık bir **merkez**: gruplanmış kart ızgarası. Her kart bir
konuya gidiyor (`/istatistikler/<konu>`).

| Grup | Kartlar |
|---|---|
| Makam | Etkinlikler · Talepler |
| İş Takip | Gecikme Panosu (mevcut `/is-panosu`'na gider) |
| Vatandaş | Halk Günü · Form ve Anket |
| Program | Protokol ve Davet · Çiçek Gönderi |
| Kurum | Özgeçmiş Havuzu · Sistem Sağlığı |

Gruplar **menüdeki gruplamayı izler**: kullanıcı "Halk Günü"nü menüde nerede
arıyorsa merkezde de orada bulmalı. Ayrı bir sınıflandırma icat etmek, aynı
kurumu iki farklı haritayla gezdirmek olurdu.

### GENEL ŞEKİL — altı konu, tek çizici

Yeni konuların hepsi aynı DTO'yu döndürüyor
(`Application/Dto/Analiz/KonuIstatistigiDto.cs`): karolar + bölümler +
aylık seyir. İstemcide karşılığı **tek** bir ekran
(`screens/statistics/TopicDashboard.tsx`).

Konu başına ayrı DTO ve ayrı ekran yazılsaydı altı neredeyse birebir kopya
olurdu ve zamanla ayrışırlardı — bu depoda aynı hata etiket çevirisinde üç
kopya olarak yaşandı.

> **Etkinlik ve talep panoları TAŞINMADI.** İkisi çok daha zengin (ortalama
> süre, tamamlanma oranı seyri, katılımcı kırılımı) ve çalışan iki ekranı
> genel şekle sığdırmak için yeniden yazmanın karşılığı yok. Merkeze kart
> olarak girdiler, şekilleri kendilerinde kaldı.

- **Karo değeri METİN.** Sayı dönseydi "%73", "4,2 gün" ve "1.240"
  biçimlerinin her biri için istemciye ayrı bir kural göndermek gerekirdi.
  Biçim **açıkça `tr-TR`** — süreç kültürüne bırakılsaydı yayın makinesinde
  "1.348" ekranda "1,348" diye okunabilirdi.
- **Ton RENK KODU değil AD** (`iyi` · `uyari` · `kotu`). Sunucudan
  `#RRGGBB` yollamak beyaz etiket sözleşmesini bozardı; karşılık ön yüzde
  durum token'larına bağlanıyor (`--st-ok` · `--st-warn` · `--st-no`).
- **Paydası sıfır olan oran yüzde YAZMAZ.** Hiç kayıt yokken "%0" yanlış bir
  şey söylüyor: "hiçbiri teslim edilmedi" değil, "ölçecek bir şey yok".
- **Boş aylar da doldurulur.** Yalnızca kaydı olan aylar dönseydi grafik boş
  bir ayı atlar ve çizgi "kesintisiz devam ediyor" gibi okunurdu.

### GÖRÜNÜRLÜK KAPILARI İSTATİSTİKTE DE GEÇERLİ

**Sayı da bir bilgidir.** Listede göremediğin kaydı sayan bir uç, gizliliği
sayı üzerinden deliyor ("bu birimde kaç gizli görüşme var"). Her metot kendi
modülünün kapısını birebir tekrarlar:

| Konu | Kapı | Neden |
|---|---|---|
| Halk günü, protokol daveti | `BirimId == etkin birim` | `HalkGunuServisi.GorunurGunler` / `DavetServisi.GorunurOlanlar` ile aynı; ayrışırlarsa listede 8, istatistikte 80 kayıt görünür |
| Protokol **defteri** | kapı YOK | Defter kurum geneli; birime süzmek aynı vali yardımcısını her birimin ayrı sayması demekti |
| Çiçek | kapı YOK | Çiçekçi hesabı kurum geneli; talimatı veren birim ile ödemeyi yapan birim aynı olmayabiliyor. Gizli etkinlik zaten çiçek talimatı üretmiyor |
| Özgeçmiş | kapı YOK — **kasıtlı istisna** | Havuzun varlık sebebi kaydın birimler arasında dolaşabilmesi (`OzgecmisHavuzuTests.Havuz_birim_suzgecinden_gecmez`). Birim yalnızca **dağılım** olarak gösteriliyor |
| Sistem | `sistem.hata` | Hata ekranıyla aynı kapı: Admin bile göremez. Pano yığın izi ve istek gövdesi DÖNDÜRMEZ ama "hangi uç patlıyor" da saldırı yüzeyini tarif ediyor |

**İzin İKİ KATLI:** sınıf düzeyindeki `istatistik.goruntule` MERKEZİ, metot
düzeyindeki modül izni o KARTI açıyor. Yalnızca modül iznine bakılsaydı
istatistik yetkisi olmayan biri kartların bir kısmını görürdü; yalnızca
merkez iznine bakılsaydı halk gününü hiç görmeyen biri halk günü sayılarını
okurdu.

> **Merkez ekranının kendisi izin İSTEMEZ** ve bu bilinçli: kartlar zaten tek
> tek süzülüyor. `istatistik.goruntule` konsaydı, halk günü sayılarını
> görmeye yetkili ama makam istatistiğine yetkisiz bir kullanıcı kapıda
> kalırdı. Menü öğesi de bu yüzden **çoklu izin (VEYA)** taşıyor — yeni bir
> konu eklenirken izni `navigation.ts`'e de yazılmalı.

### ÇIKTI: pano, liste DEĞİL

`GET /api/v2/istatistik/<konu>/excel` — özet bir sayfada, her dağılım ayrı
sayfada, aylık seyir sonda. Altı konu **tek üreticiden** geçiyor
(`IstatistikCiktiServisi`), çünkü hepsi aynı şekli döndürüyor.

- Dosyanın başına **dönem yazılır**: sayı tek başına anlamsız ve iki farklı
  dönemin sayıları rapora yapıştırıldığında karışıyor.
- Her dağılım **ayrı sayfa**: hepsi alt alta konsaydı süzgeç ve grafik
  kurulamazdı, oysa Excel'e aktarmanın amacı orada işlem yapmak.
- Yüzde **sayı olarak** yazılır (0–1 + `0.0%` biçimi); metin olsaydı sütunda
  toplam alınamazdı.
- Sayfa adı 31 karakter, `[]:*?/\` yasak ve **benzersiz** olmak zorunda —
  aynı adı ikinci kez eklemek `ArgumentException` atıp çıktıyı 500'e
  düşürürdü ve iki bölümün aynı başlığı taşıması olağan.

> Kayıt listesi isteyen modülün kendi ucunu kullanır (`disa-aktar/*`, çiçekçi
> dosyası, halk günü çizelgesi). Pano çıktısı onların yerine geçmez.

### "SON 12 AY" 12 TAKVİM AYIDIR

İlk ölçümde grafik **13 sütun** çiziyordu: hem sunucu varsayılanı hem
istemcinin aralık hesabı bugünden 12 ay geriye gidiyor, aylık gruplama da
baştaki ve sondaki YARIM ayları ayrı kova sayıyordu. İkisi de ayın 1'inden
başlayacak şekilde düzeltildi.

> Ölçüm: `Ağu 25 … Ağu 26` (13) → `Eyl 25 … Ağu 26` (12).

### Ölçümler

```
uçlar          6 konu × (pano + excel) = 12 uç, hepsi 200
excel          8,7–10,9 KB · sayfalar: Özet + dağılımlar + Aylık seyir
merkez         9 kart · 5 grup · masaüstü 3 kolon (315px) · mobil 1 kolon
               yatay taşma 0 · iç içe etkileşim 0
testler        684 sunucu + 235 ön yüz · görsel tur 226 görüntü, TEMİZ
sözleşme       +3 DTO, sıfır kayıp — v1 mobil sözleşmesi bozulmadı
```

> **`tokens.test.ts` ateş etti.** Karo tonu ilk yazımda depoda BULUNMAYAN bir
> token adına bağlanmıştı; renk sessizce hiç uygulanmıyordu. Bekçi kaynağı
> ham tarıyor — **yorum satırları dahil**, yani belgede örnek olarak yazılan
> tanımsız bir token adı da testi düşürüyor.

## LİSTE ÇIKTISI — beş modül daha

Görev, proje, özgeçmiş, protokol ve vatandaş havuzu listeleri artık Excel'e
aktarılıyor (`GET <modül>/excel`). Ajanda ve talepte bu zaten vardı; yeni
modüller eklenirken atlanmıştı ve kullanıcı "ötekinde var burada yok" diyordu.

### SÜZGEÇ VE KAPI LİSTEYLE AYNI YERDEN

Her modülde sorgu kurulumu bir metoda çıkarıldı (`SorguKurAsync` /
`SorguKur`); liste de çıktı da onu çağırıyor.

Kopyalansaydı belirti **sessiz** olurdu: bir süzgeç eklendiğinde biri
unutulur ve Excel ekrandakinden farklı bir küme döndürürdü — iki listeyi yan
yana koymadan anlaşılmıyor. Aynı gerekçe istatistik merkezinde de yazılı:
listede göremediğin kaydı sayan ya da dışa aktaran bir uç, kapıyı deliyor.

> **Ölçüldü:** liste `toplam` alanı ile Excel satır sayısı altı süzgeç
> bileşiminde birebir eşleşti (görev 20/20 · görev+arama 0/0 · özgeçmiş 2/2 ·
> protokol 8/8 · havuz+atanmamış 2/2).

| Modül | Kapı |
|---|---|
| Görev · Proje | `kapsam.Contains(BirimId)` — `IEtkinBirim.KapsamAsync` |
| Vatandaş havuzu | `BirimId == etkin birim` |
| Özgeçmiş | kapı YOK — kasıtlı istisna (havuz birimler arası dolaşır) |
| Protokol | kapı YOK — defter kurum geneli |

### PDF YOK ve bilinçli

Bu beş liste **süzülmek ve sayılmak** için dışa aktarılıyor, elden ele
dolaşmak için değil. `ExportButtons`'ın `pdf` alanı isteğe bağlı yapıldı;
verilmezse tek düğme çiziliyor. Boş bir "PDF" düğmesi, basıp hiçbir şey
alamayan bir kullanıcı bırakırdı.

### SESSİZ KIRPMA YOK

`ListeCikti.UstSinir` = **20.000 satır**. Aşılırsa kırpılmıyor, iş kuralı
hatası dönüyor ve kullanıcıdan süzgeç daraltması isteniyor: kırpmak "her şeyi
indirdim" sanan bir kullanıcı bırakırdı ve eksik raporun yanlış olduğu ancak
başka bir yerden sayılınca anlaşılırdı. Sorgu `UstSinir + 1` çekiyor, yani
aşım tek bir satır fazlasından anlaşılıyor.

### İzin: görüntülemekten AYRI

Dört yeni izin (`gorev.ciktiAl` · `proje.ciktiAl` · `ozgecmis.ciktiAl` ·
`protokol.ciktiAl`); havuz mevcut `halkgunu.ciktiAl`ı kullanıyor. Gerekçe
`form.ciktiAl` ile aynı: dosya kurum dışına taşınabiliyor, 25 satır
sayfalamak ile tüm veriyi indirmek aynı şey değil.

`IzinTohumu` yeni izinleri `IlkDagilim`daki rollere kendiliğinden dağıtıyor —
`Yonetici` ilk açılışta alıyor, elle SQL gerekmiyor.

> **Bekçi ateş etti:** `IzinKataloguSenkronTests` ön yüzdeki
> `permissions.ts`in geride kaldığını söyledi.

### Telefon biçimi TEK YERE alındı

`Telefon.Bicimle` — `0532 111 22 33`. Kural bir dönem
`HalkGunuCiktiServisi` içinde özel bir metottu ve yalnızca halk günü
çıktılarında uygulanıyordu; liste çıktıları eklenirken kopyalanmak yerine
`Application/Dto/V2/Ortak/Telefon.cs`'e çıkarıldı. Aynı numara veritabanında
dört ayrı yazımla duruyor ve süzülen bir sütunda bu, aynı kişiyi dört ayrı
değer gibi gösteriyordu.

### Yanında bulunan bir hata: EZİLEN ARAMA KUTUSU

Görev araç çubuğu 1280px'te kapasitesini aşıyordu ve `flex-1` olan arama
kutusu bütün taşmayı emiyordu.

```
ÖNCE   arama kutusu 52px — içine tek harf sığmıyor
SONRA  180px (alt sınır) · denetim grubu sığmayınca SARILIYOR
```

> **Çıktı düğmesi bunun sebebi DEĞİL**: düğme gizlenip yeniden ölçüldü, arama
> yine 52px çıktı. Kapasiteyi aşan bir satırı tek satırda tutmak, en çok
> kullanılan denetimi kullanılamaz kılmak demek.

## DIŞARIYA VERİLEN ADRES İSTEKTEN GELİR

`App:BaseUrl` **tek bir alan adı**. Uygulama başka bir adresten
yayınlandığında (aynı kurumda ikinci alan adı, taşınma, test ortamı)
dışarıya giden her bağlantı yanlış yeri gösteriyordu.

> **Ölçülen durum:** uygulama `akillisehir…` altında çalışıyor, çiçekçiye
> giden SMS `randevu…` yazıyor ve bağlantı **hiç açılmıyordu**.

`Services/AdresCozucu.cs` isteğin kendi şeması ve ana bilgisayarını
kullanıyor. İki yerde bağlı:

| Nerede | Ne olurdu |
|---|---|
| Çiçekçiye giden SMS bağlantısı | Çiçekçi ölü bir adrese gidiyordu |
| Kimlik sağlayıcı dönüş adresi | Sağlayıcı `redirect_uri mismatch` ile isteği reddederdi |

- **Şema ters vekilden okunuyor.** IIS/nginx TLS'i sonlandırıp içeriye düz
  HTTP konuşuyor; `Request.Scheme` tek başına `http` derdi. `UseForwardedHeaders`
  (`X-Forwarded-Proto`) bunu düzeltiyor. Ölçüldü: `X-Forwarded-Proto: https`
  ile üretilen adres `https://akillisehir…`.
- **İstek dışındayken ayara düşülür.** Arka plan servisleri bir HTTP
  isteğinin içinde değil; orada tahmin edilecek alan adı yok.

> **GÜVENLİK — `Host` başlığı istemci denetimindedir.** Dinamik adres, host
> header injection yüzeyini açar: kimliği doğrulanmış bir kullanıcı sahte
> bir `Host` ile SMS'e kendi adresini yazdırabilir. Savunma **`AllowedHosts`**
> ayarıdır; `*` bırakılırsa çerçevenin ana bilgisayar süzgeci devre dışı
> kalır. `.env.example` bunu gerekçesiyle yazıyor.
>
> OpenID dönüş adresinde bu riski **sağlayıcının kendisi kapatıyor**:
> gönderilen `redirect_uri` sağlayıcıdaki kayıtlı adreslerden biri değilse
> istek zaten reddediliyor.

Bekçi `AdresCozucuTests`: hem davranışı (istekten türetme, geri düşüş) hem
de **kaynağı** tarıyor — biri kolaylık olsun diye `BaseUrl.TrimEnd`'e geri
dönerse hata çalışma anında sessizdir, bağlantı üretilir yalnızca yanlış
yere gider.

## ÇİÇEK TESLİM FOTOĞRAFI

Çiçekçi teslim ederken fotoğraf ekleyebiliyor. Makam "çiçek gitti mi, nasıl
gitti" sorusunu çiçekçiyi aramadan görüyor.

- **Tek çağrı.** Kod ve fotoğraf aynı çok parçalı istekte gidiyor. İki ayrı
  uç olsaydı çiçekçi kodu iki kez girecek ya da fotoğraf **kodsuz** bir
  uçtan yüklenecekti — ikincisi, bağlantıyı bilen herkesin teslim
  fotoğrafını değiştirebilmesi demek.
- **Fotoğraf da doğrulama kodu ister**, kart teslim edilmiş olsa bile.
  Teslim işaretlemesi devretmeli (aynı kartı ikinci kez işaretlemek hata
  değil) ama fotoğraf öyle değil. Bekçi `AnonimUcTests.Teslim_fotografi_kodsuz_yuklenemez`
  kod denetiminin fotoğraf kaydından ÖNCE geldiğini kaynakta doğruluyor.
- **Yalnızca resim**, 12 MB üst sınır. Uç anonim: sınırsız yükleme diski
  doldurabilirdi. İkinci savunma `Middleware/YuklemeGuvenligi` ama burada
  anlaşılır bir hata vermek daha iyi.
- **Dosya adı GUID'den türetilir**, kullanıcının adından değil: gelen ad yol
  ayracı taşıyabiliyor ve aynı kart yeniden yüklendiğinde eskisinin üzerine
  yazılması isteniyor — her denemede yeni dosya bırakmak çöp biriktirirdi.
- Telefonda `capture="environment"` arka kamerayı doğrudan açıyor; çiçekçi
  teslim yerinde ve galeriden dosya aramak fazladan iki adım. Masaüstünde
  yok sayılıyor.

Fotoğraf iki yerde görünüyor: çiçekçinin kendi fişinde ve **etkinlik
detayında** (rozetin altında, dokununca tam ekran).

> **Ölçüldü (uçtan uca, jetonsuz):** yanlış kod → 400 ve fotoğraf
> **kaydedilmedi**; doğru kod → teslim işaretlendi + fotoğraf
> `/uploads/cicek/{guid}.png` olarak indi (`200 image/png`); `.html`
> yükleme → 400 *"Yalnızca fotoğraf yükleyebilirsiniz"*.

## KURUMSAL KİMLİK SAĞLAYICI (OpenID Connect)

Personel kurum hesabıyla girebiliyor; şifreyle giriş **kapanmıyor**.

### Ayar VERİTABANINDA, `.env`'de değil

Kurulum kuralının ikinci yarısı: okumak için zaten veritabanına bağlanmak
gereken şeyler `.env`'de, **yetkilinin arayüzden değiştirmesi gerekenler**
veritabanında. Kimlik sağlayıcı ikincisi — kurum sağlayıcı değiştirdiğinde
ya da istemci sırrı döndüğünde sunucuya girip dosya düzenlemek ve
uygulamayı yeniden başlatmak gerekmemeli.

`openid_ayarlari` tek satır, `Institution` deseninin aynısı. Ekran
`/kimlik-saglayici`, izin **`sistem.openid`** — kurum bilgilerinden ayrı,
çünkü yanlış girildiğinde giriş ekranı etkileniyor.

### Handler değil, elle akış

`AddOpenIdConnect` açılışta yapılandırılıyor; buradaki ayar çalışma anında
değişiyor. Handler'ı çalışırken yeniden yapılandırmak, ayarı `.env`'e
taşımaktan daha kırılgan olurdu. Akış üç HTTP çağrısı (keşif →
yetkilendirme → jeton); kütüphane getirisi düşük.

| Karar | Neden |
|---|---|
| **PKCE (S256) zorunlu** | Gizli istemci olsak da tek satırlık maliyeti var; Azure AD ve yeni Keycloak zaten şart koşuyor |
| **`state` sunucuda, 10 dk ömürlü** | CSRF koruması; süresi geçmiş ya da uydurulmuş `state` reddediliyor |
| **Keşif belgesi 10 dk önbellekli** | Her girişte indirmek, sağlayıcı yavaşladığında giriş ekranını da yavaşlatıyor. "Sına" düğmesi önbelleği ATLAR — o kişi ŞU ANKİ durumu soruyor |
| **`id_token` imzası doğrulanmıyor** | Jeton, sağlayıcının jeton ucundan **doğrudan bize**, TLS üzerinden ve istemci sırrıyla kimlik kanıtlayarak geldi. İmza, jeton güvenilmeyen bir taraftan (ör. tarayıcıdan) gelseydi şart olurdu |
| **Jeton uygulamanın KENDİ jetonu** | Sağlayıcının kimlik jetonu yalnızca kimliği kanıtlıyor, sonra atılıyor: yetkiler bu sistemde |

### Jeton üretimi TEK YERDE

`OturumServisi.JetonUretAsync` — parola girişi de sağlayıcı girişi de onu
çağırıyor. Talep listesi (rol, birim, kullanıcı kimliği, `jti`) ve oturum
kaydı kopyalansaydı, sisteme yeni bir talep eklendiğinde yalnızca bir yola
eklenir ve **sağlayıcıyla giren kullanıcı sessizce eksik yetkiyle**
dolaşırdı.

Kilitli hesap sağlayıcıyla da giremiyor: kilit bu sistemin kararı, sağlayıcı
onu bilmiyor. Denetlenmeseydi parolayla kilitlenen hesap sağlayıcı
düğmesiyle girmeye devam eder, yani kilit hiçbir şey ifade etmezdi.

### Düğme İKİ şartla çıkar

Kullanıcının istediği buydu: *"eğer openid ayarları yapılmış **ve
erişilebilir** ise"*.

```
ayar açık? ─── hayır ──▶ düğme yok
   │ evet
   ▼
sağlayıcıya ulaşılıyor? ─── hayır ──▶ düğme yok
   │ evet
   ▼
"<Düğme metni> ile giriş yap"
```

> **Ölçüldü:** sahte bir OIDC sağlayıcı ayağa kaldırıldı. Sağlayıcı
> ayaktayken `giris-durumu` → `{kullanilabilir:true}` ve düğme hem 390px
> hem 1280px'te çıktı; sağlayıcı kapatılıp sunucu yeniden başlatılınca
> `{kullanilabilir:false}` ve düğme **kayboldu**. Giriş ekranının kendisi
> etkilenmedi — şifreyle giriş sağlayıcıya bağımlı değil.

`GET openid/giris-durumu` **anonim** (çağıran henüz giriş yapmamış) ve
yalnızca iki alan döner: `kullanilabilir` + `gorunenAd`. Yetkili adres,
istemci kimliği ve kapsamlar oradan sızmaz — `OpenIdTests` bunu alan alan
kilitliyor.

### Sızdırmayan üç yer

| Ne | Nasıl |
|---|---|
| **İstemci sırrı** | Okuma ucu döndürmüyor; DTO'da alan bile yok, yalnızca `sirTanimli` bayrağı. Yazmada **boş = değiştirme** — aksi hâlde ayarı açıp kaydeden herkes girişi bozardı |
| **Jeton** | Dönüşte adres **parçasında** (`#`) taşınıyor, sorgu dizesinde değil: sorgu dizesi sunucu günlüklerine, ters vekil kayıtlarına ve `Referer` başlığına düşüyor. SPA okuyup hemen adres çubuğundan siliyor |
| **Sağlayıcı hatası** | Jeton ucunun gövdesi kullanıcıya verilmiyor (istemci kimliği ve yapılandırma ayrıntısı içerebiliyor); günlüğe tam hâli, kullanıcıya anlaşılır bir cümle |

### AÇIK YÖNLENDİRME kapalı

Dönüş yolu sorgu dizesinden geliyor. Süzülmeseydi
`?donus=https://saldirgan.example` ile kullanıcı giriş yaptıktan **hemen
sonra** saldırganın sayfasına gönderilirdi — üstelik jeton adres parçasında,
yani saldırganın eline geçerek. Yalnızca `/` ile başlayan ve `//`
içermeyen yollar kabul ediliyor (`OpenIdTests`, 8 vaka).

### Eksik yapılandırmayla AÇILAMAZ

Sağlayıcı adresi, istemci kimliği ve sır dolu değilken `etkin=true`
kaydedilemiyor (400). Yarım yapılandırmayla açmak, giriş ekranına çalışmayan
bir düğme koymak demek: kullanıcı basıyor, sağlayıcıya gidiyor ve geri
dönemeyeceği bir sayfada kalıyor.

### Ekranın iki kritik parçası

Yapılandırmanın en sık yanlış giren yerleri:

1. **Kopyalanabilir dönüş adresi** — sağlayıcı tarafında birebir eşleşmeli.
   Elle yazıldığında bir harf kayması yetiyor ve alınan hata **sağlayıcının
   sayfasında** çıkıyor, yani kullanıcı neyi düzelteceğini bu ekranda
   göremiyor.
2. **"Bağlantıyı sına"** — kaydetmeden önce denenebiliyor.

**Otomatik kullanıcı oluşturma varsayılan KAPALI** ve açıldığında uyarı
tonuyla çiziliyor: açıkken sağlayıcıda hesabı olan **herkes** girebilir.
Kurumsal dizinde binlerce hesap var, uygulamayı kullanması gereken kişi
sayısı onlarca.

## GİRİŞ EKRANI

- **Büyük harf kilidi uyarısı.** Şifre alanı yazılanı göstermiyor; kilit
  açıkken kullanıcı doğru şifreyi yazdığını sanıp üst üste reddediliyor ve
  **hesap kilidine (10 deneme) kadar** gidebiliyor. `getModifierState`
  tuşa basıldığı anda okunuyor.
- **Sağlayıcı düğmesi şifreli girişin ALTINDA**, "ya da" ayracıyla. Şifreyle
  giriş üstte ve dolgulu kalıyor: sağlayıcı kapandığında ekranın düzeni
  değişmesin, kas hafızası bozulmasın.
- Sağlayıcı sorgusu **düşerse hiçbir şey olmuyor** — giriş ekranının kendisi
  sağlayıcıya bağımlı değil.

## GÜVENLİK — kapatılan açıklar

### Yüklenen dosya tarayıcıda ÇALIŞIYORDU (depolanmış XSS)

En ciddi bulgu ve **ölçülerek** bulundu, tahminle değil:

```
talebe zararli.html eklendi
  → /uploads/randevu/{guid}.html
  → JETONSUZ istek: HTTP 200 · Content-Type: text/html
  → script çalıştı
```

Sayfa uygulamayla **aynı kaynakta** olduğu için
`localStorage['sv-jetonu']`'na erişebiliyordu. Jeton 15 saat geçerli ve
**iptal listesi yok** — yani tam hesap devri, dosya eklemeye yetkili
herhangi bir kullanıcıdan başlayarak.

Kök neden iki şeyin birleşimi: yükleme uçları uzantıyı **kullanıcıdan**
alıp diske olduğu gibi yazıyordu (`Guid + Path.GetExtension(gelenAd)`) ve
`wwwroot/uploads` altındaki her şey **kimlik doğrulanmadan** servis
ediliyor.

**Çözüm servis anında, yükleme anında değil.** `Middleware/
YuklemeGuvenligi.cs`: `/uploads` altındaki çalışabilir uzantılar
`application/octet-stream` + `Content-Disposition: attachment` ile
dönüyor, yani tarayıcı belge olarak açmıyor, indiriyor.

| Neden servis anında | |
|---|---|
| Yükleme yolu tek değil | Etkinlik fotoğrafı, talep dosyası, özgeçmiş, görev eki, portal fotoğrafı — her birine ayrı denetim koymak birini unutmayı davet eder |
| Diskte zaten duran dosyalar | Yükleme denetimi onları kurtarmaz |
| Dosya silinmiyor | 404 vermek meşru bir `.xml` ekini de kaybettirirdi; indirilebilir kalıyor, yalnızca çalışmıyor |

Liste "zararlı dosyalar" değil, **tarayıcının script çalıştırdığı
bağlamlar**: `.svg` de içinde (`<img>`de zararsız, adrese doğrudan
gidilince çalışır), `.xml` de (XSLT). Sunucuda işlenebilecekler
(`.cshtml`, `.php`, `.aspx`) da var — bugün işlenmiyorlar ama uygulama
başka kurumlarda başka bir yapılandırmayla yayınlanacak.

Yükleme tarafına da denetim eklendi ama o **kullanıcıya anlaşılır hata**
vermek için; güvenlik sınırı ara katman.

> **Ölçüm — düzeltmeden sonra:** aynı dosya, aynı adres, jetonsuz →
> `200 · application/octet-stream · attachment · nosniff`. Gerçek
> yüklemelerde regresyon yok: `.png` hâlâ `200 · image/png · inline`.

> **Sıra tuzağı:** ara katman `UseStaticFiles`'tan ÖNCE bağlanmalı; sonra
> bağlanırsa statik dosya ara katmanı yanıtı çoktan yazmıştır ve kural
> **sessizce** etkisiz kalır. `YuklemeGuvenligiTests` sırayı da denetliyor
> ve ateş ettiği ölçüldü.

### Girişte hız sınırı — hesap kilidi YETMİYOR

Hesap kilidi (10 hatalı deneme / 5 dk) tek bir hesabı koruyor ama **kimlik
doldurmayı** (credential stuffing) durdurmuyor: saldırgan her kullanıcı
adını bir kez dener, hiçbir hesap kilitlenmez, binlerce deneme tek bir
IP'den sorunsuz geçer.

`POST oturum/giris` artık IP başına **30 deneme/dakika**. Sınır bilerek
cömert: kurumun tamamı tek bir NAT adresinin arkasında olabiliyor ve sabah
mesai başında herkes aynı anda giriyor. Jeton 15 saat geçerli olduğu için
bir kişi günde bir kez giriyor — 30/dk meşru kullanımın çok üstünde, ama
otomatik bir denemeyi ilk dakikada kesiyor.

### JWT anahtar gücü AÇILIŞTA zorlanıyor

`JwtOptions` belgesi "en az 32 karakter" diyordu ama **hiçbir yer
denetlemiyordu**: 4 karakterlik bir anahtarla uygulama sorunsuz açılıyor,
jetonlar üretiliyor ve her şey çalışıyor görünüyordu. HMAC-SHA256'nın
güvenliği doğrudan anahtarın entropisine bağlı; kısa ya da tahmin
edilebilir anahtar, saldırganın **kendi jetonunu imzalayıp istediği
kullanıcı olması** demek. Sessiz kalınacak en kötü ayar bu.

Açılış artık iki durumda duruyor: 32 karakterden kısa anahtar, ve
`.env.example`'daki şablon değerini taşıyan anahtar (şablon depoda duruyor
— değiştirilmemiş bir anahtarı herkes biliyor; uzunluk denetimi bunu
yakalamaz).

> Ölçüldü: `Jwt__Secret="kisa"` → *"JWT imza anahtarı çok kısa (4
> karakter)"* ile açılış durdu. Şablon değeriyle de durdu.

### Swagger üretimde KAPALI

217 v2 ucunun tamamını, parametrelerini ve DTO şemalarını kimlik
doğrulamadan yayınlıyordu. Uçların kendisi yetkili; sızan şey verinin değil
**yapının** kendisi ve onu gizlemek ücretsiz. `App:SwaggerAcik=true` ile
bilerek açılabilir.

> **Ölçüm tuzağı:** ilk denemede üretim kipinde swagger 200 döndü ve
> "kapı çalışmıyor" sanıldı. Sebep koddaki kapı değil, ölçümün kendisiydi:
> `dotnet run` `launchSettings.json`'daki `ASPNETCORE_ENVIRONMENT=Development`
> değerini uyguluyor ve uygulama Production sanılırken Development'ta
> koşuyordu. `--no-launch-profile` ile gerçek ölçüm: swagger JSON **404**,
> uygulama 200. (`/swagger` yolu 200 dönüyor ama o SPA'nın kendi sayfası,
> Swagger UI değil.)

### Temiz çıkanlar

Aranıp **sorun bulunmayan** yerler de kayda değer; bir sonraki inceleme
aynı yolu yeniden yürümesin:

| Alan | Durum |
|---|---|
| Sırlar depoda | `appsettings*.json` temiz; her şey `.env`'den |
| SQL enjeksiyonu | Tek ham SQL var, o da `FromSqlInterpolated` (parametreli) |
| XSS (ön yüz) | `dangerouslySetInnerHTML` hiç kullanılmıyor; markdown çizici de kullanmıyor |
| JWT doğrulama | Issuer/audience/lifetime/signing key hepsi açık, 2 dk sapma toleransı |
| Güvenlik başlıkları | `nosniff`, `SAMEORIGIN`, `strict-origin-when-cross-origin`, üretimde HSTS |
| CORS | Yapılandırma yok — aynı kaynaktan servis ediliyor, gerekmiyor |

## Bilinen pürüzler

> **Bu liste bayatlamıştı ve iki maddesi yanlıştı** — düzeltildi. "Swagger
> üretimde açık" ve "üretim sırları depoya işlenmiş" maddeleri, aynı dosyanın
> *GÜVENLİK* bölümünde çözüldüğü yazılı olduğu hâlde burada duruyordu.
> Ölçüldü: `appsettings*.json` temiz, `appsettings.json`a dokunan tek commit
> var ve içinde sır yok, depo geçmişinde Firebase servis hesabı hiç
> bulunmuyor (o madde eski `workcollab` deposundan kalmış). Swagger
> `App:SwaggerAcik` kapısının arkasında. **Yanlış döküman,
> dökümansızlıktan kötüdür**: bu listeyi okuyan bir sonraki kişi çözülmüş
> iki işi yeniden çözmeye kalkardı.

- `AjandaTekrar` tablosu ölü — hiçbir kod okumuyor/yazmıyor, `AjandaSeri` onun yerini aldı. Dokunma, kaldırma.
- `AjandaHareketler` kullanılmıyor; zaman çizelgesi `AjandaOlaylar`'da.
- `GET /api/SettingsApi/UpdateFcmToken` durum değiştiren bir GET — v1 sözleşmesi olduğu için düzeltilemez.
- `Views/Randevu/HalkGunleri.cshtml` ve `GET /api/v2/talep/halk-gunleri` **ölü
  ama duruyor**: ikisi de yalnızca `RandevuTipId == 1` süzgeciydi ve hiçbir
  istemci çağırmıyor. Yeni `/api/v2/halk-gunu` modülü ayrı tablolara oturuyor;
  emekliye ayırma ayrı bir karar.
