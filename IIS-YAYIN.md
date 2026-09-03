# IIS'e yayın

`dotnet publish` çıktısı IIS'e kopyalanır. **Ön yüz (React) derlemesi publish
sırasında otomatik çalışır** — ayrı bir `npm run build` adımı gerekmez.

## Kısaca

```bash
dotnet publish KentOS.Kalem.Web/KentOS.Kalem.Web.csproj \
  -c Release -o C:\yayin\workcollab
```

Bu tek komut şunları yapar:

1. `npm ci` (yalnızca `frontend/node_modules` yoksa)
2. `npm run build` → `wwwroot/yeni/`
3. .NET derlemesi
4. `wwwroot/yeni/**` çıktısını yayın klasörüne ekler
5. `web.config`'i dönüştürür (aspNetCore işleyicisi eklenir)

## Publish yapan makinede **node** olmalı

Ön yüz derlemesi `npm` ister. Publish'i geliştirme makinesinde ya da derleme
sunucusunda yapıp **çıktı klasörünü** IIS'e kopyalamak en kolayı; o zaman IIS
sunucusunda node aranmaz.

`npm` PATH'te değilse (nvm kullanıcılarında sık):

```bash
dotnet publish ... -p:NpmKomutu=/tam/yol/npm
```

npm bulunamazsa derleme **anlaşılır bir hatayla** durur, sessizce eksik çıktı
üretmez.

> `-p:SkipFrontend=true` **yayında kullanılmaz**. Derlemeyi hızlandırmak için
> geliştirme sırasında vardır; yayında kullanılırsa `/yeni` adresi 404 döner.

## IIS tarafı

| Ayar | Değer |
|---|---|
| .NET Hosting Bundle | **kurulu olmalı** (ASP.NET Core Module V2) |
| Uygulama havuzu · .NET CLR sürümü | **Yönetilen kod yok** (No Managed Code) |
| Uygulama havuzu · Pipeline | Integrated |
| Barındırma modeli | `inprocess` (web.config'de hazır) |

### Ortam değişkeni

`ASPNETCORE_ENVIRONMENT` **Production** olmalı (varsayılan bu). Development'a
alınırsa geliştirme tohumu (`GelistirmeTohumu`) çalışır ve üretim veritabanına
sahte kayıtlar yazar.

Uygulama havuzu için ayarlamak: IIS Yöneticisi → Uygulama Havuzları → havuz →
Gelişmiş Ayarlar → Ortam Değişkenleri.

### Yazma izni — **ek ayar gerekmez**

Yeni bir klasör izni AÇMANIZ GEREKMİYOR. Yüklenen her şey `wwwroot\uploads`
altına yazılıyor; oraya yazma izni bu sistemde zaten var (etkinlik fotoğrafları
ve talep dosyaları iki yıldır oraya yazılıyor).

| Klasör | İçerik |
|---|---|
| `wwwroot\uploads\ajanda`, `...\randevu`, `...\ozgecmis` | mevcut yüklemeler |
| `wwwroot\uploads\gonderim` | kullanıcıdan kullanıcıya gönderilen belgeler |

Uygulama **açılışta bu klasörlere yazmayı dener**; başaramazsa günlüğe büyük
harfle `DİZİNE YAZILAMIYOR` yazar ve ne yapılacağını söyler. Uygulama yine de
başlar — okuma işlevleri çalışmaya devam etsin diye.

> **Gönderim belgeleri `wwwroot` altında ama HTTP'den İNDİRİLEMEZ.**
> `GonderimDosyaKorumasi` ara katmanı `/uploads/gonderim` altına gelen her
> isteği 404'ler; dosyanın tek girişi kimlik denetimli
> `GET /api/v2/gonderim/{id}/dosya` ucu ve orada da yalnızca gönderen ile alıcı
> geçebiliyor. Kural ve ara katman sırası `GonderimDosyaKorumasiTests` ile
> kilitli.

İsteğe bağlı: belgeleri sürümden sürüme taşımak istemiyorsanız
`appsettings.json` içindeki `Depolama:GonderimDizini` alanına yayın klasörünün
dışında bir yol yazın (o klasöre yazma izni vermeniz gerekir):

```json
"Depolama": { "GonderimDizini": "D:\\workcollab-veri\\gonderim" }
```

### Yükleme boyutu

`web.config` içindeki `maxAllowedContentLength` **50 MB**'a çekilmiştir.
IIS'in varsayılanı 30.000.000 bayt (~28,6 MB) ve bu sınır isteği ASP.NET
Core'a **ulaşmadan** reddeder — kullanıcı uygulamanın anlaşılır hatası yerine
IIS'in `404.13` sayfasını görürdü. Uygulamanın kendi sınırları: talep dosyası
20 MB, gönderim 25 MB.

## Veritabanı

Açılışta bekleyen migration'lar **otomatik uygulanır**
(`Database:AutoMigrate`, varsayılan `true`). 3/6/9/12 saniye aralıklarla 5
deneme yapılır, sonra uygulama **başlatılmaz** (fail-fast). Ayrıntı:
`OTOMATIK-MIGRATION.md`.

Bağlantı dizesi `appsettings.json` içindedir ve yayın çıktısına aynen gider —
sunucuda düzenlemeniz gerekmez. Farklı bir sunucuya alırken tek değiştirilecek
yer orası.

## Yayın sonrası denetim listesi

```
https://<adres>/            → eski MVC arayüzü açılır
https://<adres>/yeni        → yeni SPA açılır
https://<adres>/yeni/takvim → sayfa YENİLENEREK açılır (derin bağlantı)
https://<adres>/api/v2/oturum/giris → 200 döner (mobil ve web girişi)
```

- `/yeni` boş beyaz sayfa geliyorsa: publish sırasında ön yüz derlenmemiştir
  (`wwwroot/yeni/index.html` var mı bakın).
- `/yeni/takvim` yenilendiğinde 404 geliyorsa: istekler ASP.NET Core'a
  gitmiyordur — `web.config` handler'ı ya da Hosting Bundle eksiktir.
- PWA kurulumu çıkmıyorsa: site **HTTPS** olmalı ve
  `/yeni/manifest.webmanifest` 200 dönmeli.

## Ayarlar

Tümü `appsettings.json` içinde ve yayın çıktısına aynen kopyalanır — veritabanı
bağlantısı, SMS, JWT ve `Depolama`. Yayın için ayrıca bir dosya düzenlemeniz
gerekmiyor.

Depo özel olduğu için sırlar `appsettings.json` içinde tutuluyor. Depo bir gün
paylaşılırsa bunların ortam değişkenlerine taşınması gerekir
(`ConnectionStrings__DefaultConnection`, `JWT__Secret`, `SMS__Password`).
