# Sistem Özellik Ağacı

Bu dosya **ne yapabildiğimizin tek listesi**. Her modül burada; her satır
yeteneği, sunucu ucunu, ekranı, izni ve bekçi testini gösterir.

> **Yeni bir modül bittiğinde buraya işlenir** — `CLAUDE.md` → *Modül üretim
> sırası* adım 5. İşlenmemiş modül bitmiş sayılmaz. Amaç bir envanter değil:
> "bu işi sistem yapıyor mu?" sorusuna bakılacak tek yer olması ve yeni bir
> geliştiricinin sistemi bir sayfada görebilmesi.

**Gösterim:** ✅ tam · 🟡 kısmi (notu var) · ⬜ planlı

---

## 1. Kimlik ve yetki

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Giriş (JWT) | `POST oturum/giris` | `/giris` | — | ✅ |
| Kim olduğum | `GET oturum/ben` | kabuk | — | ✅ |
| Şifre değiştirme | `PUT oturum/parola` | `/ayarlar` | — | ✅ |
| Oturum kayıtları | `GET oturum/*` | `/yonetim?bolum=oturumlar` | `yonetim.oturumKaydi` | ✅ |
| Kullanıcı yönetimi | `api/v2/yonetim` | `/yonetim` | `yonetim.kullanici` | ✅ |
| Birim ağacı | `api/v2/yonetim/birimler` | `/yonetim?bolum=birimler` | `yonetim.birim` | ✅ |
| Rol ve izin dağıtımı | `api/v2/yonetim/roller` | `/yonetim/roller/:ad` | `yonetim.rol` | ✅ |

**Değişmezler:** yetki **rolden değil izinden** gelir; üç katman (menü · rota ·
düğme) aynı izne bakar. Birim izolasyonu her sorguda `GorunurOlanlar()`.
**Bekçi:** `IzinSistemiTests`, `IzinKataloguSenkronTests`, `IzinUcKapsamiTests`,
`BirimIzolasyonuTests`, `frontend/test/izin.test.tsx`.

## 2. Ajanda ve takvim

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Etkinlik CRUD | `api/v2/etkinlik` | `/ajanda`, `/ajanda/:id` | `ajanda.duzenle` | ✅ |
| Tekrar eden etkinlik (RRULE) | `etkinlik` + `kapsam` | etkinlik formu | `ajanda.duzenle` | ✅ |
| Gizli etkinlik | sunucu süzer | kilit rozeti | `ajanda.gizli` | ✅ |
| Katılımcı birimler | `etkinlik/{id}/katilimci` | etkinlik detayı | `ajanda.duzenle` | ✅ |
| Beş görünümlü takvim | `POST takvim/aralik` | `/takvim` | `ajanda.goruntule` | ✅ |
| Sürükle-bırak taşıma/boyutlandırma | `PUT etkinlik/{id}` | `/takvim` | `ajanda.duzenle` | ✅ |
| Notlar · fotoğraflar · geçmiş | `etkinlik/{id}/*` | etkinlik detayı | `ajanda.goruntule` | ✅ |
| Hazırlık (bilgi notu, konuşma metni) | `etkinlik/{id}` | üç durumlu rozet | `ajanda.duzenle` | ✅ |
| Basın ajandası | ayrı izin | `/ajanda` | `ajanda.basinGoruntule` | ✅ |
| Günlük program çıktısı | `disa-aktar/*` | Çıktılar menüsü | `ajanda.ciktiAl` | ✅ |

**Bekçi:** `RRuleTests`, `GizliEtkinlikTests`, `GizliEtkinlikYetkisiTests`,
`KatilimciBirimTests`, `BasinAjandasiTests`, `SilinmisSiralamaTests`,
`frontend/test/tekrar.test.ts`, `takvim.test.ts`.

## 3. Talepler

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Talep CRUD | `api/v2/talep` | `/talepler`, `/talepler/:id` | `talep.duzenle` | ✅ |
| Havale (birime gönder) | `talep/{id}/havale` | detay | `talep.havale` | ✅ |
| Üst birime gönder | `talep/{id}/ust-birim` | detay | `talep.havale` | ✅ |
| Durum akışı ve notlar | `talep/{id}/*` | detay | `talep.duzenle` | ✅ |
| Ajandaya ekleme | `talep/{id}/ajanda` | `AjandayaEkleModal` | `ajanda.duzenle` | ✅ |
| Dosya ekleri | `talep/{id}/dosya` | detay | `talep.goruntule` | ✅ |
| Harita ucu (mobil) | `talep/harita` | — (web'de yok) | `talep.goruntule` | ✅ |

> `ust-birim` ucu **200 + `false`** dönebiliyor (üst birim yoksa). İstemci
> yalnızca HTTP durumuna bakarsa "gönderildi" der; her iki istemci de gövdeyi
> denetliyor.

## 4. Halk günü

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Vatandaş havuzu (başvuru) | `halk-gunu/basvuru` | `/halk-gunu/basvurular` | `halkgunu.basvuru` | ✅ |
| Gün ve zaman dilimleri | `halk-gunu`, `/dilim` | `/halk-gunu/:id` | `halkgunu.yonet` | ✅ |
| Atama ve sıralama | `/katilim`, `/siralama` | gün ayrıntısı | `halkgunu.atama` | ✅ |
| Salon modu | `/katilim/{id}/gorusme` | `/halk-gunu/:id/salon` | `halkgunu.gorusme` | ✅ |
| Kişi geçmişi (telefon normalleştirmeli) | `halk-gunu/kisi-gecmisi` | `KisiGecmisi` | `halkgunu.goruntule` | ✅ |
| Toplu SMS (yer tutuculu) | `halk-gunu/{id}/sms` | gün ayrıntısı | `halkgunu.sms` | ✅ |
| Talebe dönüştürme | `/katilim/{id}/talep` | gün ayrıntısı | `halkgunu.talepOlustur` | ✅ |
| Çıktılar (3 kâğıt × 2 biçim) | `/excel`, `/pdf` | `CiktiMenusu` | `halkgunu.ciktiAl` | ✅ |

**Bekçi:** `HalkGunuTests`, `HalkGunuCiktiSutunTests`, `SmsYerTutucuTests`.

## 5. Protokol, davet ve çiçek

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Protokol kişi listesi | `api/v2/protokol` | `/protokol`, `/protokol/:id` | `protokol.goruntule` | ✅ |
| Kişinin davet geçmişi | `protokol/{id}/davetler` | kişi detayı | `protokol.goruntule` | ✅ |
| Davet listesi ve katılım | `api/v2/davet` | `/davetler/:id` | `davet.goruntule` | ✅ |
| Takip: arandı/mesaj + cevap | `davet/{id}/kisi` | kişi tabakası | `davet.duzenle` | ✅ |
| Dört çıktı (takip, telefon, imza, protokol) | `davet/{id}/*/pdf` | çıktı menüsü | `davet.ciktiAl` | ✅ |
| İsimlik kartları (10 kesme + 10 masa) | `davet/{id}/kartlar/pdf` | `KartCiktisi` | `davet.ciktiAl` | ✅ |
| Çiçek siparişi | `api/v2/cicek` | `/cicek` | `cicek.goruntule` | ✅ |
| Çiçekçi dosyası + dönem çıktısı | `cicek/{id}`, `/excel`, `/pdf` | `/cicek/:id` | `cicek.goruntule` | ✅ |

**Bekçi:** `DavetTests`.

## 6. Özgeçmiş havuzu

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Havuz listesi + süzgeç | `GET ozgecmis` | `/ozgecmisler` | `ozgecmis.goruntule` | ✅ |
| Ekleme/düzenleme (tek istek, multipart) | `POST/PUT ozgecmis` | form tabakası | `ozgecmis.ekle` | ✅ |
| Kayıt tabakası (iletişim, dosya, geçmiş) | `GET ozgecmis/{id}` | satıra dokunma | `ozgecmis.goruntule` | ✅ |
| Kişilere yönlendirme + bildirim | `ozgecmis/{id}/paylas` | paylaşım penceresi | `ozgecmis.paylas` | ✅ |
| Talepten otomatik düşme | talep dosyası | rozet | — | ✅ |

**Bekçi:** `OzgecmisHavuzuTests`.

## 7. Dosya gönderimi

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Kişiye belge gönderme | `POST gonderim` | `/gonderim` | `gonderim.gonder` | ✅ |
| Gelen/giden kutusu | `GET gonderim` | `/gonderim` | — (alma yetki istemez) | ✅ |
| Belge üzerinde yazışma | `gonderim/{id}/not` | detay | — | ✅ |
| Erişim koruması (yalnızca taraflar) | indirme ucu | — | — | ✅ |

**Bekçi:** `DosyaGonderimiTests`, `GonderimDosyaKorumasiTests`.

## 8. Bildirim

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Bildirim merkezi + okunmamış sayısı | `api/v2/bildirim` | zil · `/bildirimler` | — | ✅ |
| Web push (FCM) | `bildirim/web-jeton` | izin kartı | — | ✅ |
| Mobil push | v1 sözleşmesi | — | — | ✅ |
| Arka plan tıklaması → kayda gitme | service worker | — | — | ✅ |
| Ön plan şeridi → tıklayınca gitme | — | toast | — | ✅ |
| SMS (birim + vatandaş) | `ayar/sms-*` | SMS pencereleri | ilgili modül | ✅ |

**Yönlendirme sözleşmesi tek:** `data.fcmData` → `{ entity, id, action }`.
Yeni varlık **üç yerde** tanımlanır: `bildirim/BildirimMerkezi.tsx`,
`bildirim/fcm.ts`, `public/firebase-messaging-sw.js`.
**Bekçi:** `WebPushJetonTests`, `frontend/test/toast.test.tsx`.

## 9. PWA

| Yetenek | Nerede | Durum |
|---|---|---|
| Ana ekrana kurulum (Android/iOS/masaüstü) | `pwa/kurulum.ts` | ✅ |
| Kaldırma algılama + kaçış kapısı | aynı | ✅ |
| Appbar kurulum simgesi | `pwa/KurulumDugmesi.tsx` | ✅ |
| Platforma özel talimat | `pwa/talimat.tsx` | ✅ |
| Çevrimdışı kabuk | `firebase-messaging-sw.js` | ✅ |
| Açılış perdesi (tema uyumlu) | `index.html` | ✅ |

**Bekçi:** `frontend/test/pwa.test.ts`.

## 10. Raporlama ve çıktı

| Yetenek | Uç | Ekran | Durum |
|---|---|---|---|
| İstatistik dağılımları | `api/v2/istatistik` | `/istatistikler` | ✅ |
| Excel üretimi (ClosedXML) | `DisaAktarmaServisi` | çıktı düğmeleri | ✅ |
| PDF üretimi (QuestPDF) | modül servisleri | çıktı düğmeleri | ✅ |

> QuestPDF lisansı **her PDF sınıfının kendi statik kurucusunda** ayarlanır.

## 11. Form ve anket

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Form listesi | `GET api/v2/form` | `/formlar` | `form.goruntule` | ✅ |
| **Form tasarımcısı** | `POST/PUT api/v2/form` | `/formlar/:id` | `form.yonet` | ✅ |
| Yayınlama / kapatma | `POST form/{id}/yayinla` · `/durum` | tasarımcı | `form.yayinla` | ✅ |
| Kopyalama | `POST form/{id}/kopyala` | liste | `form.yonet` | ✅ |
| **Vatandaş formu** | `GET/POST api/v2/form-portal/{anahtar}` (anonim) | `/form/:anahtar` | — | ✅ |
| Yarım yanıtı sürdürme | `POST form-portal/{a}/taslak` | vatandaş formu | — | ✅ |
| Yanıt listesi | `GET form/{id}/yanit` | `/formlar/:id/yanitlar` | `form.yanitGoruntule` | ✅ |
| Yanıt detayı | `GET form/{id}/yanit/{yid}` | yanıt tabakası | `form.yanitGoruntule` | ✅ |
| Özet dağılımlar | `GET form/{id}/ozet` | Özet sekmesi | `form.yanitGoruntule` | ✅ |
| Yanıt geçersiz sayma | `DELETE form/{id}/yanit/{yid}` | yanıt listesi | `form.yanitSil` | ✅ |
| Dosya yükleme (vatandaş) | `POST form-portal/{a}/dosya` (anonim) | vatandaş formu | — | ✅ |
| Dosya indirme (yetkili) | `GET form/{id}/yanit/{yid}/dosya/{did}` | yanıt detayı | `form.yanitGoruntule` | ✅ |
| Excel (dinamik sütun) | `GET form/{id}/excel` | yanıt ekranı | `form.ciktiAl` | ✅ |
| Portal anahtarı | `PUT api/v2/institution` | `/kurum` | `sistem.kurum` | ✅ |

**Alan tipleri:** 29 tanımlı, **19'u paletten** çıkıyor — metin, e-posta,
telefon, T.C. kimlik, sayı, tarih, saat, tek/çok seçim, açılır liste,
evet/hayır, ölçek, yıldız, matris, dosya, başlık, açıklama, ayırıcı.

**Yetenekler:** adımlı (stepper) kip · koşullu görünürlük (VE/VEYA, yalnızca
geriye referans) · grup ve form bazında kolon düzeni · sürümleme · üç erişim
kipi · açık/kapalı/tarih/kota · tek yanıt kuralı · sonuç sayfası ·
`localStorage` taslağı.

**Bekçi:** `FormDogrulamaTests`, `FormSemaTests`, `AnonimUcTests`,
`frontend/test/forms.test.ts`.

Tanım ve cevaplar **jsonb** (bu depoda ilk); `cevaplar` üzerinde GIN
indeksi, tek yanıt kuralında **kısmi benzersiz indeks**. Vatandaş yüzeyi
kurum ayarındaki `form_portali_acik` bayrağına bağlı ve kapalıyken uçlar
404 döner.

---

## 12. Sistem yönetimi

| Yetenek | Uç | Ekran | İzin | Durum |
|---|---|---|---|---|
| Tanımlar (referans veriler) | `api/v2/tanim` | `/tanimlar` | `tanim.yonet` | ✅ |
| Sistem hata kayıtları | `api/v2/hata` | `/hatalar` | `sistem.hata` | ✅ |
| Yapay zekâ hata raporu | `hata/{id}` | detay | `sistem.hata` | ✅ |
| Yardım merkezi | — (istemci) | `/yardim` | — | ✅ |
| Tema tasarımcısı | — (istemci) | palet simgesi | — | ✅ |
| **Kurum bilgileri** | `GET/PUT api/v2/institution` | `/kurum` | `sistem.kurum` | ✅ |
| **PWA manifesti (kuruma göre)** | `GET /manifest.webmanifest` | — | — | ✅ |
| **Kimlik sağlayıcı ayarı** | `GET/PUT api/v2/openid` · `POST openid/sina` | `/kimlik-saglayici` | `sistem.openid` | ✅ |
| **Kurum hesabıyla giriş** | `GET openid/giris-durumu · baslat · geri-donus` (anonim) | giriş ekranı | — | ✅ |

**Bekçi:** `HataKaydiTests`, `YapilandirmaTests`, `OpenIdTests`,
`AnonimUcTests`, `frontend/test/yardim.test.tsx`, `tokenlar.test.ts`.

Kurum kaydı **tek satırlık** `kurum_bilgileri` tablosunda; `GET` anonim
(giriş ekranı da okur), `PUT` izne bağlı. Tablo boşken ilk satır `.env`
değerlerinden tohumlanır — sıfırdan kurulum hâlâ "sadece .env doldur".

Kimlik sağlayıcı ayarı da **tek satırlık** (`openid_ayarlari`) ve aynı
sebeple veritabanında: sağlayıcı değiştiğinde sunucuya girip dosya
düzenlemek gerekmesin. Giriş ekranındaki düğme **iki şartla** çıkar — ayar
açık **ve** sağlayıcıya ulaşılabiliyor; erişilemeyen bir sağlayıcıya
yönlendirmek kullanıcıyı geri dönemeyeceği bir sayfada bırakıyor.

**Güvenlik yüzeyi** (`YuklemeGuvenligiTests`, `AnonimUcTests`): yüklenen
dosyalar tarayıcıda belge olarak açılmıyor, girişte IP başına hız sınırı
var, JWT anahtar gücü açılışta zorlanıyor, Swagger üretimde kapalı.

---

## Yatay konular

| Konu | Durum | Not |
|---|---|---|
| Sayfalama tek zarfta (`SayfaliSonuc<T>`) | 🟡 | Yeni uçlarda zorunlu; bazı eski uçlarda yok — dokunulursa eklenir |
| Kod dili İngilizce | 🟡 | **Ön yüz TAMAM** (dizin, dosya, tanımlayıcı). Sunucu **toplu çevrilmeyecek**: yeni kod İngilizce, eski kod dokundukça. Sözleşme sabitlendi (40 `[Table]` · 451 `[Column]` · 1190 `[JsonPropertyName]`) — dokundukça çevirmek güvenli |
| Kuruma özel bilgi koddan çıktı | ✅ | Sırlar/altyapı `.env`'de (`Bolum__Alt` biçimi, `DotNetEnv`), kurum kimliği veritabanında ve `/kurum` ekranından düzenlenir. Bekçi: `YapilandirmaTests` — ayar varsayılanlarında kurum bilgisi olmadığını ve `.env.example`'ın eksiksizliğini denetler |
| Ürün adı ve ad alanı | ✅ | `KentOS.Mini.*`; kaynak ağacında kurum adı geçmiyor. Depo `kentos-mini`; Flutter uygulaması ayrı depoda ve dahil değil |
| Dosya depolama sağlayıcısı | ✅ | `STORAGE__PROVIDER=Local\|S3`. Nesne adı veritabanındaki yolun aynısı, geçişte kayıt değişmiyor; S3 kipinde eski `/uploads/...` adresleri köprü ara katmanıyla ayakta (v1 mobil sözleşmesi). Bekçi: `DepolamaTests` |
| E2E test paketi | 🟡 | Görsel tur (154 ekran) + yetki matrisi (7 rol) var; işlem uçtan uca akışları eklenecek |
| Çevrimdışı yazma (kuyruk) | ⬜ | Şu an yalnızca okuma kabuğu çevrimdışı |

## Planlanan modüller

| Modül | Kapsam | Not |
|---|---|---|
| **Proje yönetimi** | Proje · aşama · görev · atama · ilerleme | Halk günü modülünün kalıbı örnek alınacak |
| **İş takibi** | Görev listesi, sorumlu, süre, durum akışı | Talep akışıyla arasındaki sınır tanımlanmalı |
| **Doküman yönetimi** | Sürümleme, onay akışı | Dosya gönderimi bunun ön adımı |

> Yeni modül eklerken sıra `CLAUDE.md` → *Modül üretim sırası*: entity →
> service → validation → DTO → mapping → controller → test → istemci sınıfları
> → ön yüz → döküman → bu ağaç.
