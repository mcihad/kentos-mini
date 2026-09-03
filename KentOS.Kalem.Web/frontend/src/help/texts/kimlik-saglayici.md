Personelin **kurum hesabıyla** giriş yapmasını sağlar. Açıldığında giriş
ekranında ikinci bir düğme çıkar; kullanıcı adı ve şifreyle giriş kapanmaz.

## Ne işe yarar

Kurumun kendi hesap sistemi varsa (kurum içi kimlik sunucusu, e-posta
hesapları, merkezi kullanıcı dizini) personel ayrı bir şifre taşımak zorunda
kalmaz. Şifreyi kurum sistemi doğrular, bu program yalnızca kimin girdiğini
öğrenir.

Kimin neyi görebileceği **yine bu programdaki yetkilerden** gelir; kurum
hesabı yalnızca kapıyı açar.

## Kurulum sırası

1. **Sağlayıcı adresi**, **istemci kimliği** ve **istemci sırrı** alanlarını
   doldurun. Bu üçünü size sistem yöneticiniz ya da kurumun bilgi işlem
   birimi verir.
2. Sayfanın alt bölümündeki **dönüş adresini kopyalayın** ve kurum
   sisteminde bu uygulamanın tanımına ekleyin. Birebir aynı olmalı — tek bir
   harf farkı girişi çalışmaz hâle getirir.
3. **Bağlantıyı sına** deyin. Yeşil bir onay göreceksiniz; görmüyorsanız
   adres yanlış ya da sunucuya ulaşılamıyordur.
4. En üstteki anahtarı açın ve **Kaydet** deyin.

> Üç alan dolmadan giriş açılamaz. Yarım bir ayarla açmak, giriş ekranına
> çalışmayan bir düğme koymak olurdu.

## Alanlar

| Alan | Ne yazılır |
|---|---|
| Sağlayıcı adresi | Kurum kimlik sisteminin adresi |
| İstemci kimliği | Bu uygulama için tanımlanan ad |
| İstemci sırrı | Kurum sisteminin verdiği gizli anahtar |
| Düğme metni | Giriş ekranında "… ile giriş yap" diye yazılır |
| Kullanıcı adı alanı | Kurum sisteminden gelen hangi bilginin buradaki kullanıcı adıyla eşleşeceği |
| Kapsamlar | Kurum sisteminden istenecek bilgiler; çoğu kurumda varsayılan yeterlidir |

**İstemci sırrı bir daha gösterilmez.** Kaydedildikten sonra alan boş görünür
ve "Tanımlı" yazar; boş bırakıp kaydederseniz eskisi korunur. Değiştirmek
için yenisini yazmanız yeterli.

## Tanımsız kullanıcıyı otomatik oluştur

Kapalı tutmanız önerilir. Açıkken, kurum sisteminde hesabı olan **herkes**
bu programa girebilir — kurumsal dizinde binlerce hesap varken programı
kullanması gereken kişi sayısı genelde onlarcadır.

Kapalıyken kullanıcı önce **Yönetim** ekranından tanımlanır; kurum hesabı
yalnızca şifreyi doğrular.

## Giriş düğmesi neden görünmüyor?

Düğme iki şart birden sağlandığında çıkar: ayar **açık** olmalı **ve**
sağlayıcıya **ulaşılabiliyor** olmalı. Kurum sistemi kapalıysa düğme
kendiliğinden kaybolur ve kullanıcılar şifreyle girmeye devam eder —
program erişilemeyen bir sisteme yönlendirmez.

Ayarı değiştirdiyseniz düğmenin görünmesi birkaç dakika sürebilir.

> Bu ekrana yalnızca yetkisi olan kullanıcılar girebilir. Ayar yanlış
> girildiğinde giriş ekranı etkilenir; yetki bu yüzden dar tutulmuştur.
