# KentOS.Mini

Belediye **başkanlık makamı** için ajanda, talep ve iş takip sistemi.

Makamın günü üç şeyin etrafında dönüyor: kimin ne zaman geleceği (ajanda),
vatandaşın ne istediği (talepler) ve bunların hangi birime düştüğü. KentOS.Mini
bu üçünü tek yerde tutar; üzerine halk günü, protokol/davet, özgeçmiş havuzu ve
kurum içi dosya gönderimi modülleri gelir.

İki yıldır bir belediyede canlıda çalışıyor.

---

## Ne yapar

| Modül | Kapsam |
|---|---|
| **Ajanda ve takvim** | Gün/hafta/ay/yıl görünümleri, tekrar eden etkinlikler (RRULE), gizli etkinlik, katılımcı birimler, hazırlık takibi |
| **Talepler** | Vatandaş talebi kaydı, havale, not/dosya, arşiv, talepten etkinlik oluşturma |
| **Halk günü** | Vatandaş havuzu, gün ve zaman dilimleri, atama ve sıralama, toplu SMS, salon modu, talebe dönüştürme |
| **Protokol ve davet** | İl protokolü, davet listeleri, arama/cevap takibi, kesme kartı çıktıları |
| **Özgeçmiş havuzu** | İş başvurularını toplama, arama, paylaşma |
| **Dosya gönderimi** | Kurum içi belge gönderimi ve üzerinde yazışma |
| **Bildirim** | Web push (PWA) ve mobil push |
| **Çıktı** | Excel ve PDF; günlük program için altı ayrı yerleşim |

Yetki **rolden değil izinden** gelir; listeler kullanıcının birimine göre
süzülür. Ayrıntı: [`OZELLIK-AGACI.md`](OZELLIK-AGACI.md).

## Yığın

- **Sunucu:** .NET 10 · ASP.NET Core · EF Core · PostgreSQL
- **Web:** React + TypeScript + Vite + Tailwind CSS 4 + Radix UI · PWA
- **Mobil:** Flutter — **ayrı depoda**, aynı v1 API sözleşmesini kullanır
- **Depolama:** yerel disk ya da S3 uyumlu nesne deposu (MinIO, AWS S3…)

## Kurulum

```bash
cp .env.example .env      # doldurun: veritabanı, JWT anahtarı, adres
dotnet run --project KentOS.Mini.Web
```

Ön yüz derlemesi `dotnet build/run/publish` ile otomatik tetiklenir.
Adım adım: [`kurulum.md`](kurulum.md).

### Kapsayıcı ile

```bash
cp .env.example .env
docker compose up --build        # uygulama + PostgreSQL
```

Yayına alma (Dokploy): [`DOKPLOY.md`](DOKPLOY.md). İmaj ön yüzü de kendisi
derler; ayrı bir derleme hattı gerekmez.

## Başka bir kurumda çalıştırmak

Uygulama **tek bir kuruma bağlı değildir**. Kaynak ağacında hiçbir kurumun adı,
alan adı, rengi ya da amblemi geçmez. Kuruma özel her şey iki yerden gelir:

| Ne | Nerede |
|---|---|
| Veritabanı, imza anahtarı, SMS, depolama, Firebase | **`.env`** |
| Kurum adı, iletişim, uygulama adı, marka renkleri, amblem | **Veritabanı** — uygulama içinden düzenlenir (Sistem → Kurum Bilgileri) |

Ayrım keyfi değil: kurum kaydını okumak için zaten veritabanına bağlanmak
gerekiyor, dolayısıyla bağlantı bilgisi orada tutulamaz. `.env` yine de
kurulumun tek adımı olarak kalır — kurum tablosu boşken ilk satır oradan
tohumlanır.

Sonuç: yeni bir belediye için **`.env`'i doldur, çalıştır, arayüzden kurum
bilgilerini gir**. Yeniden derleme yok, kaynak değişikliği yok.

## Geliştirme

```bash
dotnet build                              # ön yüzü de derler
dotnet test                               # sunucu testleri

cd KentOS.Mini.Web/frontend
npm run dev                               # Vite; /api → :5097 proxy
npx tsc --noEmit && npm test && npm run build
node test/gorsel/tur.mjs                  # gerçek Chrome ile görsel tur
```

Mimari kararlar, değişmez kurallar ve tuzaklar [`CLAUDE.md`](CLAUDE.md) ile
[`KentOS.Mini.Web/frontend/CLAUDE.md`](KentOS.Mini.Web/frontend/CLAUDE.md)
içinde. İkisi de **bağlayıcıdır**; kod yazmadan önce okunur.

### Dil sözleşmesi

Kod İngilizce, kullanıcı yüzeyi ve veritabanı Türkçe. Sınır kesindir:
sınıf/dosya/değişken adları İngilizce; ekranda görünen her metin, tablo ve
kolon adları Türkçe.

## Sürüm notu

`/api/XxxApi` (v1) **mobil uygulamanın sözleşmesidir** ve değiştirilmez —
rota, alan adı, HTTP fiili, dönüş şekli. Yeni işler `/api/v2` altına yazılır.
Bu kural `ContractFreezeTests` ile bekçilenir.

## Lisans

Henüz belirlenmedi.
