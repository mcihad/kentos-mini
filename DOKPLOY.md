# Dokploy ile yayına alma

KentOS.Kalem bir Dockerfile taşıyor; Dokploy'da **Application → Docker** tipiyle
doğrudan dağıtılır. Ayrı bir derleme hattı gerekmiyor: imaj ön yüzü de
kendisi derliyor.

---

## 1. Veritabanı

Dokploy'da önce bir **PostgreSQL** hizmeti açın. Uygulama ile aynı projede
olsun; iç ağ üzerinden hizmet adıyla erişilir.

> **Saat dilimini `Europe/Istanbul` yapın.** Damgalar
> `timestamp without time zone` — saat dilimi taşımıyorlar. Veritabanı ile
> uygulama farklı dilimdeyse kaydedilen saat ile okunan saat birbirini
> tutmaz ve bu, hata olarak değil "ajanda yanlış" olarak görünür.

### PostGIS

Harita ve konum sorguları için gerekli. Dokploy'un Postgres servisinde
**süper kullanıcı** ile bir kez:

```sql
CREATE EXTENSION IF NOT EXISTS postgis;
```

Uygulama imajı `postgis/postgis` tabanlı bir veritabanı bekler; düz
`postgres` imajında uzantı **yüklü değildir** ve komut `undefined_file`
hatası verir.

Atlarsanız uygulama yine açılır — yalnızca harita ekranı boş kalır. Açılış
günlüğünde uyarı görürsünüz.

## 2. Uygulama

**Application** oluşturun, kaynak olarak bu depoyu ve `Dockerfile`'ı seçin.

- **Port:** `8080`
- **Build path:** `/` (Dockerfile depo kökünde)

İlk derleme birkaç dakika sürer (npm + NuGet). Sonrakiler katman
önbelleğinden hızlanır: `package-lock.json` ve `.csproj` değişmediği sürece
bağımlılıklar yeniden indirilmez.

## 3. Ortam değişkenleri

Dokploy'un **Environment** sekmesine yazın. `.env` dosyası imaja GİRMEZ —
aynı imajın başka bir kuruma verilebilmesi için ayarlar çalışma anında
gelir.

En az şunlar gerekli:

```dotenv
ConnectionStrings__DefaultConnection=Host=<postgres-hizmet-adi>;Port=5432;Database=workcollab;Username=workcollab;Password=<parola>;Maximum Pool Size=25

Jwt__Secret=<en az 32 karakter — openssl rand -base64 48>
Jwt__ValidIssuer=https://<alan-adiniz>
Jwt__ValidAudience=https://<alan-adiniz>

App__BaseUrl=https://<alan-adiniz>
```

Tam liste ve her ayarın ne işe yaradığı: [`.env.example`](.env.example).

> `Jwt__Secret` kurulumdan sonra **değiştirilmez**: değişirse sahadaki bütün
> oturumlar (mobil dahil) düşer.

## 4. Kalıcı depolama — atlanırsa veri kaybı

Uygulama yüklenen dosyaları `/uygulama/wwwroot/uploads` altına yazıyor:
etkinlik fotoğrafları, talep ekleri, özgeçmişler ve kullanıcıdan kullanıcıya
gönderilen belgeler.

**Bu klasöre bir birim (volume) bağlamazsanız her yeniden dağıtımda hepsi
silinir.** Dokploy → **Volumes**:

| Tür | Mount path |
|---|---|
| Volume | `/uygulama/wwwroot/uploads` |

Birden çok örnek (replica) çalıştıracaksanız birim yetmez — bir örneğe
yüklenen dosyayı diğeri göremez. O durumda nesne deposuna geçin:

```dotenv
Storage__Provider=S3
Storage__S3__Endpoint=<minio-hizmet-adi>:9000
Storage__S3__AccessKey=...
Storage__S3__SecretKey=...
Storage__S3__Bucket=workcollab
Storage__S3__UseSsl=false
```

Var olan dosyaları taşımak için (nesne adı, veritabanındaki yolun aynısı —
tek bir kayıt değişmez):

```bash
mc mirror ./wwwroot/uploads           kova/workcollab/uploads
mc mirror ./wwwroot/uploads/gonderim  kova/workcollab/gonderim
```

## 5. Bildirim (isteğe bağlı)

Firebase hizmet hesabı JSON'u imaja girmez. İki yol var:

- Dosyayı bir birime koyup yolunu verin:
  `Firebase__CredentialsPath=/uygulama/gizli/firebase-service-account.json`
- Ya da bildirimi hiç kurmayın — dosya yoksa uygulama **yine açılır**, sadece
  bildirim göndermez.

## 6. Alan adı ve HTTPS

Dokploy'un **Domains** bölümünden alan adını bağlayın; sertifikayı Traefik
üretir. Uygulama ters vekil arkasında çalışacak şekilde yapılandırılmış
(`UseForwardedHeaders`), yani `X-Forwarded-Proto` doğru okunuyor ve
yönlendirme döngüsü oluşmuyor.

`App__BaseUrl` ile `Jwt__ValidIssuer`/`ValidAudience` değerlerinin bu alan
adıyla **aynı** olduğundan emin olun.

## 7. Sağlık denetimi

İmaj kendi `HEALTHCHECK`'ini taşıyor: `GET /api/v2/institution`. Bu uç anonim
ve veritabanına dokunuyor, yani "süreç ayakta" değil "gerçekten hizmet
veriyor" ölçüyor. Dokploy bunu kendiliğinden kullanır.

## 8. Şema göçü

Varsayılan olarak açılışta bekleyen migration'lar uygulanır
(`Database__AutoMigrate=true`). **Birden çok örnek** çalıştıracaksanız bunu
`false` yapın ve göçü tek seferde elle çalıştırın; aksi hâlde iki örnek aynı
anda şema değiştirmeye kalkar.

## 9. İlk açılış

1. `admin` / `Admin123.` ile girin ve **parolayı hemen değiştirin**.
2. **Sistem → Kurum Bilgileri**: kurum adı, iletişim, uygulama adı, kurumsal
   renkler ve amblem.
3. **Tanımlar**: etkinlik tipleri, durumlar, mahalleler, meslekler.
4. **Yönetim**: birim ağacı ve kullanıcılar.

---

## Ölçülen değerler

Yerelde doğrulandı (Docker Desktop, arm64):

| | |
|---|---|
| İmaj boyutu | 565 MB |
| Açılış | ~8 sn (migration kapalıyken) |
| Sağlık durumu | `healthy` |
| Saat dilimi | `+03` |
| Kullanıcı | `uygulama` (kök değil) |
| PDF / Excel üretimi | çalışıyor — ek yerel kütüphane gerekmedi |

> İmaj bir ara 752 MB'tı: `COPY` sonrası `chown -R` yapmak 131 MB'lık uygulama
> katmanının tamamını ikinci kez yazıyordu. `COPY --chown` ile tek katmana
> indi.
