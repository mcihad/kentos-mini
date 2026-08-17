# Sistem Hataları

Uygulamada beklenmeyen bir durum oluştuğunda kaydı buraya düşer.

## Listede ne var

Her satır bir hata türüdür: **hangi uçta** oluştu (`POST /api/v2/etkinlik`),
mesajı ne, kaç kez tekrarlandı ve en son ne kadar önce görüldü. Aynı hata yeni
satır açmaz, sayacı artar.

Uç en üstte yazar çünkü kaydı ayıran şey odur: aynı istisna dört ayrı uçtan
geldiğinde mesajlar birbirinin aynısı olur.

- Üstteki **Yalnızca çözülmemiş** anahtarı, üzerinde çalışılacak kayıtları
  bırakır.
- **Temizle** düğmesi yalnızca **çözüldü** işaretli kayıtları siler;
  çözülmemişlere dokunmaz.

## Ayrıntı

Satıra tıklayınca hangi ekranda, hangi kullanıcıda ve hangi istekle oluştuğu
görünür.

- **Çözüldü** işareti, hata giderildiğinde konur. Aynı hata yeniden
  görülürse işaret otomatik kalkar.
- **Yapay zekâ raporu** düğmesi, hatayı bir yazılımcıya ya da yapay zekâ
  aracına doğrudan verilebilecek bir metin üretir; kopyalayıp gönderebilir­siniz.

> Bu ekran yalnızca **Sistem** yetkisine açıktır: kayıtlarda istek içerikleri
> ve IP adresleri bulunuyor.
