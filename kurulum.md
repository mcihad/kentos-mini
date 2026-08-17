# Kurulum Rehberi

**KentOS.Mini** — belediye başkanlık makamı için ajanda, talep ve iş takip
sistemi. Bu rehber sıfırdan bir kuruluma başlayan sistem yöneticisi ve
geliştirici için yazıldı.

> **Kaynak ağacında hiçbir kurumun adı geçmez.** Kurum adı, amblem, renkler,
> alan adı, SMS hesabı — hepsi yapılandırmadan gelir. Bir öncekinden kalma
> "yer tutucuları bul ve değiştir" adımı **yok**; kod zaten kurumdan bağımsız.
> Ayrıntı: `CLAUDE.md` → *KURUM BİLGİSİ KODA YAZILMAZ*.

Flutter mobil uygulaması **bu deponun parçası değildir**; ayrı bir depoda
yaşar ve aynı v1 API sözleşmesini kullanır.

---

## 0. Hızlı bakış

Kurulum iki şeyi doldurmaktan ibaret:

| Ne | Nerede | Kimden alınır |
|---|---|---|
| Veritabanı, imza anahtarı, SMS, depolama, Firebase | **`.env`** (kök dizin) | Sistem yöneticisi + sağlayıcılarınız |
| Kurum adı, iletişim, amblem, kurumsal renkler | **Uygulama içi**: Sistem → Kurum Bilgileri | Kurumsal kimlik biriminiz |

`.env` tablo boşken kurum kaydını da **tohumlar**, yani ilk açılışta arayüze
girmeden de doğru kurum bilgisiyle çalışır. Sonrasında kurum bilgisi
arayüzden düzenlenir.

---

## 1. Gereksinimler

| Bileşen | Sürüm | Not |
|---|---|---|
| .NET SDK | 10.0 | |
| PostgreSQL | 14+ | |
| Node.js | 20+ | web arayüzü derlemesi için |
| MinIO / S3 | — | **isteğe bağlı**, yalnızca çok sunuculu kurulumda |

---

## 2. Veritabanı

Uygulamanın kendi rolü ve veritabanı olsun; süper kullanıcı kullanmayın.

```sql
CREATE ROLE workcollab LOGIN PASSWORD 'guclu-bir-parola';
CREATE DATABASE workcollab OWNER workcollab;
```

Şema **açılışta otomatik** kurulur (`Database__AutoMigrate=true`). Birden çok
uygulama sunucusu aynı veritabanına bağlanacaksa bunu `false` yapın ve
migration'ı yayın hattında tek seferde çalıştırın; aksi hâlde iki örnek aynı
anda şema değiştirmeye kalkar.

---

## 3. `.env`

```bash
cp .env.example .env
```

Şablonun içinde her ayarın ne işe yaradığı ve neden öyle olduğu yazılı.
**En az doldurulması gerekenler:**

```dotenv
ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=workcollab;Username=workcollab;Password=...;Maximum Pool Size=25"

# En az 32 karakter.  openssl rand -base64 48
Jwt__Secret="..."
Jwt__ValidIssuer="https://randevu.kurumunuz.gov.tr"
Jwt__ValidAudience="https://randevu.kurumunuz.gov.tr"

App__BaseUrl="https://randevu.kurumunuz.gov.tr"
```

### Biçim

.NET'in kendi kuralı: **iki alt çizgi bölüm ayırıcıdır.**

```
Bolum__Alt=deger            →  "Bolum:Alt"
Bolum__Alt__DahaAlt=deger   →  "Bolum:Alt:DahaAlt"
```

Aynı ayarı ortam değişkeni olarak da verebilirsiniz (IIS, Docker, systemd) ve
**gerçek ortam değişkeni `.env`'i ezer**. Yayın makinesinde "dosyada ne
yazıyorsa o çalışır" sürprizi olmaz.

> ⚠ `Jwt__Secret` kurulumdan sonra **değiştirilmez**: değişirse sahadaki
> bütün oturumlar (mobil dahil) düşer.

> `.env` depoya girmez (`.gitignore`). Sırları depoya koymayın.

---

## 4. Dosya depolama

Varsayılan `Local`: dosyalar `wwwroot/uploads` altına yazılır. Tek sunuculu
kurulumda başka bir şey yapmanız gerekmez — uygulama havuzu kimliğinin o
klasöre yazma izni olsun yeter (açılışta denetlenir ve günlüğe yazılır).

Nesne deposu (MinIO / AWS S3 / Ceph / Wasabi) **şu iki durumda gerekli**:

- Uygulama birden çok sunucuda çalışıyor — bir sunucuya yüklenen dosyayı
  diğeri göremez.
- Kapsayıcıyla dağıtılıyor ve yayın klasörü kalıcı değil.

```dotenv
Storage__Provider=S3
Storage__S3__Endpoint="127.0.0.1:9000"     # şemasız
Storage__S3__AccessKey="..."
Storage__S3__SecretKey="..."
Storage__S3__Bucket="workcollab"
Storage__S3__UseSsl=false
```

**Var olan bir kurulumu S3'e taşırken** dosyaları kovaya *aynı yolla*
kopyalayın; veritabanında hiçbir kayıt değişmez:

```bash
mc mirror ./wwwroot/uploads            yerel/workcollab/uploads
mc mirror ./wwwroot/uploads/gonderim   yerel/workcollab/gonderim
```

Eski `/uploads/...` adresleri S3 kipinde de çalışmaya devam eder — mobil
uygulama o adresleri kullanıyor.

> Ayarlar eksikken `Storage__Provider=S3` verilirse **uygulama açılmaz**.
> Sessizce yerele düşmek daha kötü olurdu: yükleme çalışır, dosyalar beklenen
> yere gitmez, kimse fark etmez.

---

## 5. Bildirim (isteğe bağlı)

Firebase Cloud Messaging kullanılıyor. Bildirim istemiyorsanız bu bölümü
atlayın — uygulama bildirimsiz çalışır.

1. Firebase konsolunda bir proje açın.
2. **Hizmet hesabı** JSON dosyasını indirin, sunucuya koyun ve yolunu yazın:
   ```dotenv
   Firebase__CredentialsPath="firebase-service-account.json"
   ```
3. Bir **Web uygulaması** ekleyin; konsolun verdiği değerleri `.env`'e yazın
   (`Firebase__ApiKey`, `Firebase__AuthDomain`, `Firebase__ProjectId`,
   `Firebase__StorageBucket`, `Firebase__MessagingSenderId`, `Firebase__AppId`).
4. **Cloud Messaging → Web Push certificates**'tan **ortak** anahtarı alın:
   ```dotenv
   Firebase__VapidPublicKey="..."
   ```

> Bu değerler tarayıcıya nasılsa iniyor, gizli değiller — ama **kuruma
> özeller**. Bu yüzden ön yüz derlemesine gömülmez; `GET /api/v2/institution`
> ile çalışma anında okunur. Kaynakta hiçbir Firebase anahtarı yoktur.

---

## 6. Çalıştırma

```bash
dotnet run --project KentOS.Mini.Web
```

Ön yüz derlemesi `dotnet build/run/publish` ile otomatik tetiklenir.
Atlamak için `-p:SkipFrontend=true`.

Yayın için:

```bash
dotnet publish KentOS.Mini.Web -c Release -o /yayin/klasoru
```

`.env` dosyasını yayın klasörüne kopyalayın (ya da ayarları ortam değişkeni
olarak verin). IIS ayrıntıları: `IIS-YAYIN.md`.

### İlk giriş

Açılışta bir `admin` kullanıcısı ve temel roller oluşturulur. **İlk işiniz
parolayı değiştirmek olsun.**

---

## 7. Kurum kimliği

Uygulamaya `admin` ile girin → **Sistem → Kurum Bilgileri** (`/kurum`).

| Bölüm | Ne yazılır |
|---|---|
| Kurum kimliği | Kurum adı, kısa ad, birim, künye satırı |
| İletişim | Ağ sitesi, e-posta, telefon, adres |
| Uygulama | Uygulamanın adı, kısa adı, açıklaması |
| Kurumsal renkler | Birincil, vurgu, nötr + koyu tema karşılığı |
| Görseller | Amblem, sekme simgesi, uygulama ikonu, çıktı amblemi |

Kaydettiğiniz an giriş ekranı, menü, sekme başlığı, PWA manifesti ve
PDF/Excel çıktıları o değerleri kullanır.

**Görselleri** sunucuda `KentOS.Mini.Web/wwwroot/` altına koyun ve yolunu
alana yazın (örn. `/amblem.png`). Ekrandaki küçük önizleme boş kalıyorsa yol
yanlıştır. Gereken dosyalar:

| Dosya | Ölçü |
|---|---|
| `wwwroot/amblem.png` | 512×512, şeffaf zemin |
| `wwwroot/ikon/ikon-{48…512}.png` | PWA ikonları |
| `wwwroot/ikon/maskable-{192,512}.png` | Android maskeli ikon |
| `wwwroot/ikon/favicon-{16,32}.png` | sekme simgesi |
| `wwwroot/ikon/apple-touch-icon.png` | 180×180 |

> Telefonda **kurulu** uygulamanın adı ve simgesi işletim sisteminde
> önbelleklenir; onların güncellenmesi için uygulamayı kaldırıp yeniden
> kurmak gerekir.

Bu ekrana girmek için **`sistem.kurum`** izni gerekir. İzinler
**Yönetim → Roller** ekranından dağıtılır.

---

## 8. Referans veriler

**Tanımlar** (`/tanimlar`) ekranından doldurun: etkinlik tipleri, etkinlik ve
talep durumları, mahalleler, meslekler. Bunlar formlardaki açılır listeleri
besliyor; boş kalırsa kayıt açılamaz.

Ardından **Yönetim** (`/yonetim`) ekranından birim ağacını ve kullanıcıları
kurun. Her kullanıcı bir birime bağlıdır ve **listeler birime göre süzülür**;
birim ağacı yanlışsa kullanıcı kendi işini göremez.

---

## 9. Doğrulama

```bash
dotnet build                       # ön yüzü de derler
dotnet test                        # sunucu testleri
cd KentOS.Mini.Web/frontend
npx tsc --noEmit && npm test && npm run build
node test/gorsel/tur.mjs           # sunucu ayakta olmalı
```

Kurulumun ayakta olduğunu gösteren en hızlı iki kontrol:

```bash
curl -s http://localhost:5097/api/v2/institution   # kurum bilgisi dönmeli
curl -s http://localhost:5097/manifest.webmanifest # kurum adını taşımalı
```

---

## 10. Güvenlik listesi

- [ ] `Jwt__Secret` rastgele ve en az 32 karakter
- [ ] Veritabanı rolü **süper kullanıcı değil**, yalnızca kendi veritabanının sahibi
- [ ] `.env` dosya izinleri kısıtlı (`chmod 600`), depoda değil
- [ ] Firebase hizmet hesabı JSON'u depoda değil
- [ ] HTTPS ve HSTS açık; ters vekil arkasındaysa `X-Forwarded-*` iletiliyor
- [ ] `admin` parolası değiştirildi
- [ ] Yedekleme: veritabanı **ve** yükleme klasörü (ya da nesne deposu kovası)

---

## Sorun giderme

| Belirti | Sebep |
|---|---|
| Açılışta `JWT imza anahtarı tanımsız` | `.env` bulunamadı ya da `Jwt__Secret` boş. Uygulama bilerek durur. |
| `.env bulunamadı` bilgisi | Dosya çalışma dizininde ya da üst dizinlerinde yok. Yayın klasörüne kopyalayın. |
| Ayar değiştirdim, etkisi yok | Aynı adda **gerçek bir ortam değişkeni** var; o `.env`'i ezer. |
| Dosya yüklenmiyor | Yerel kipte klasör yazılabilir değil (açılış günlüğüne bakın); S3 kipinde kova/anahtar hatalı. |
| Bildirim gitmiyor | `Firebase__CredentialsPath` yanlış — açılışta uyarı yazılır, uygulama yine açılır. |
| Kurum adı eski görünüyor | Tarayıcı önbelleği; sayfayı yenileyin. Kurulu PWA'nın adı için yeniden kurulum gerekir. |
