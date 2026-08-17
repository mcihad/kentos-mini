# Kurum Bilgileri

Kurumun adı, iletişim bilgileri, uygulama adı, amblemi ve kurumsal renkleri
buradan düzenlenir. Kaydettiğiniz an **bütün kullanıcılara** yansır — giriş
ekranı, menü, sekme başlığı, telefon ana ekranındaki kısayol ve PDF/Excel
çıktılarının tepesi hep buradan besleniyor.

Bu ekrana girebilmek için **Kurum bilgileri** yetkisi gerekir.

## Bölümler

### Kurum kimliği

| Alan | Nerede görünür |
|---|---|
| **Kurum adı** | Giriş ekranı, menü başlığı, çıktı alt bilgisi |
| **Kısa ad** | Dar alanlar. Boş bırakılırsa kurum adı yazılır |
| **Çıktılarda görünen ad** | PDF ve Excel tepesi. Boşsa kurum adı kullanılır |
| **Birim** | Giriş ekranında kurum adının altında |
| **Künye satırı** | Giriş ekranının ve çıktıların dibi |

### İletişim

Ağ sitesi, e-posta, telefon ve adres. Bu alanlar API künyesinde ve
çıktılarda kullanılır; boş bırakılabilir.

### Uygulama

Uygulamanın adı ve açıklaması. **Kısa ad** telefonun ana ekranındaki simgenin
altında yazar; 12 karakteri aşarsa telefon kırpar.

### Kurumsal renkler

Üç renk yeterli: **birincil**, **vurgu** ve **nötr zemin**. Arayüzdeki bütün
tonlar bunlardan türetilir.

**Birincil (koyu tema)** ayrı bir alan çünkü koyu bir lacivert, siyah zeminde
düğme olarak okunmuyor. Boş bırakırsanız hazır bir açık karşılık kullanılır.

Renkleri hem seçiciden hem de `#RRGGBB` yazarak verebilirsiniz — kurumsal
kimlik kılavuzundaki kodu doğrudan yapıştırmak için ikincisi gerekli.

> Kullanıcılar Tema Tasarımcısı'ndan kendi renklerini seçmişse burada
> yaptığınız değişikliği görmezler. Buradaki renkler **Kurumsal** ön ayarını
> tanımlar.

### Görseller

Sunucudaki dosya yolları yazılır (örn. `/amblem.png`). Dosyaları sunucuda
`wwwroot` klasörüne koyup yolunu buraya yazın; yanındaki küçük kutu
önizlemeyi gösterir, boş kalıyorsa yol yanlıştır.

**Çıktı amblemi** boş bırakılırsa amblem kullanılır — çoğu kurumda ikisi
aynıdır.

## Sık sorulanlar

**Değişiklik neden hemen görünmedi?**
Sekme başlığı ve renkler anında değişir. Telefondaki kurulu uygulamanın adı
ve simgesi ise işletim sisteminde önbelleklenir; onun güncellenmesi için
uygulamayı kaldırıp yeniden kurmak gerekir.

**Veritabanı, SMS ve bildirim ayarları nerede?**
Onlar burada değil, sunucudaki `.env` dosyasında. Sebebi basit: bu ekranı
açabilmek için önce veritabanına bağlanmak gerekiyor, dolayısıyla bağlantı
bilgisi veritabanında tutulamaz. Sırların yedeklere düşmemesi de tercih
sebebi. O ayarlar için sistem yöneticinize başvurun.

**Yanlış bir şey kaydettim.**
Kaydetmeden önce **Geri al** düğmesi son kayıtlı hâle döner. Kaydettikten
sonra alanları elle düzeltmeniz gerekir; sürüm geçmişi tutulmuyor.
