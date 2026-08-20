Kullanıcılar, birimler, roller ve oturum kayıtları.

## Kullanıcılar

- **Yeni kullanıcı** düğmesi hesap açar: ad, soyad, kullanıcı adı, birim ve
  rol.
- Satırdaki kalem düğmesi bilgileri düzenler.
- **Şifre sıfırla** yeni bir şifre belirler; kullanıcıya siz iletirsiniz.
- Kullanıcıyı **pasife almak**, hesabı silmeden girişini kapatır. Geçmiş
  kayıtlarındaki adı yerinde kalır.

## Birimler

Kurumun birimleri burada tanımlanır. Birim adının yanında **yetkilisi**
yazar — aynı adı taşıyan birden çok birim olabildiği için
listelerde hangisi olduğu ancak böyle anlaşılıyor.

Birime tıklayınca o birimdeki kullanıcılar ve sayılar görünür.

## Roller

Rol, bir kullanıcının hangi ekranı açabileceğini belirler. **Rol
ayrıntısında**
izinler tek tek açılıp kapatılır; değişiklik o roldeki herkesi etkiler.

- Bir kullanıcının **son rolü** çıkarılamaz: rolsüz kullanıcı giriş yapar ama
  hiçbir ekranı açamaz.
- `Sistem` ve `BaskanOzel` rollerine atama yalnızca sistem yetkisiyle yapılır.

## Oturum kayıtları

Kim, ne zaman, hangi adresten girdi. Başarısız denemeler de görünür; şüpheli
bir durumda ilk bakılacak yer burasıdır.

> Yetki değişiklikleri en geç **beş dakika** içinde etkili olur; kullanıcının
> çıkıp girmesi gerekmez.
