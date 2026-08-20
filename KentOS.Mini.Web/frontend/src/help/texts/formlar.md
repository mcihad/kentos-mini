Vatandaştan ve personelden **yapılandırılmış geri bildirim** toplamanın yolu:
anket, başvuru formu, memnuniyet ölçümü, talep formu.

Formu siz tasarlarsınız; program size paylaşılabilir bir **internet adresi**
verir. Vatandaş o adresi açıp doldurur, siz yanıtları burada görürsünüz.

## Form oluşturma

1. **Yeni form** deyin ve bir başlık yazın.
2. Soldaki (telefonda alttaki **Alan ekle**) listeden soru türünü seçin.
   Seçtiğiniz soru bölümün sonuna eklenir.
3. Soruya tıklayın; sağdaki panelden (telefonda **Ayarlar**) metnini,
   zorunluluğunu ve seçeneklerini düzenleyin.
4. **Kaydet** deyin.

> **Kaydetmek yayınlamak değildir.** Kaydedilen form taslakta durur ve
> kimse göremez. Vatandaşa açmak için **Yayınla** demeniz gerekir.

## Soru türleri

| Grup | İçindekiler |
|---|---|
| Metin | Kısa metin, uzun metin, e-posta, telefon, T.C. kimlik no |
| Seçim | Tek seçim, çok seçim, açılır liste, evet/hayır |
| Sayı ve tarih | Sayı, tarih, saat |
| Ölçek | 1–5 gibi ölçekler, yıldız |
| Gelişmiş | Matris (satır × sütun), dosya yükleme |
| İçerik | Başlık, açıklama, ayırıcı — bunlar soru değildir, yanıt üretmez |

**T.C. kimlik no** ve **telefon** alanları biçimi kendiliğinden denetler;
hatalı bir numara girildiğinde form gönderilemez.

**"Diğer" seçeneği:** seçim sorularında bir seçeneği "Diğer" olarak
işaretleyebilirsiniz. Vatandaş onu seçtiğinde altında serbest bir yazı
kutusu açılır ve yazdığı metin yanıtla birlikte kaydedilir.

## Bölümler ve kolonlar

Sorular **bölümlere** ayrılır. Her bölümün kendi başlığı ve kendi **kolon
sayısı** olabilir: "Kimlik bilgileri" iki kolon, altındaki "Şikâyetiniz" tek
kolon olabilir.

Her sorunun genişliğini ayrıca seçebilirsiniz (tam satır, yarım, çeyrek).

> **Telefonda her soru tam genişliktedir.** Dar ekranda iki kolon, soruların
> okunmasını zorlaştırıyor; bu yüzden kolon ayarı yalnızca bilgisayarda
> uygulanır.

## Adımlar (çok sayfalı form)

Uzun formları **adımlara** bölebilirsiniz. Vatandaş her adımı doldurup
"İleri" der; üstte bir ilerleme çubuğu görür.

Bir adım tamamlanmadan diğerine geçilmez: eksik zorunlu alanlar o adımda
işaretlenir.

## Koşullu sorular

Bir soruyu yalnızca belirli bir cevap verildiğinde gösterebilirsiniz.
Örnek: *"Şikâyetiniz var mı?"* sorusuna **Evet** dendiğinde *"Detay"*
sorusu açılır.

Ayarlar panelindeki **Koşullu görünürlük** bölümünden kural ekleyin. Birden
çok kural eklerseniz "tümü" ya da "herhangi biri" seçebilirsiniz.

> **Koşul yalnızca daha önce gelen bir soruya bağlanabilir.** Listede
> yalnızca o sorular çıkar. Bir soruyu sürükleyip yerini değiştirdiğinizde
> koşul geçersiz kalırsa program sizi uyarır ve koşulu kaldırır.

## Yayınlama ve paylaşma

**Yayınla** dediğinizde form vatandaşa açılır ve listede bir **bağlantı
simgesi** belirir. Ona tıklayıp adresi kopyalayın; SMS, web sitesi ya da QR
kod ile paylaşabilirsiniz.

Yayınlanmış bir formu düzenlemeye devam edebilirsiniz. Değişiklikleriniz
**yayınla deyene kadar** vatandaşa gitmez; bu sırada "Yayınlanmamış
değişiklikler var" uyarısı görürsünüz.

## Form ne zaman yanıt almaz?

Aşağıdakilerden biri bile geçerliyse form kapalıdır ve vatandaş bunun
sebebini ekranda okur:

- Form **taslak** ya da **kapalı** durumda
- **Başlangıç tarihi** henüz gelmedi
- **Bitiş tarihi** geçti
- **Yanıt sınırına** ulaşıldı

Ayarlar sekmesinden tarih aralığı ve yanıt sınırı verebilirsiniz.

## Kimler doldurabilir?

| Seçenek | Anlamı |
|---|---|
| Herkese açık | Bağlantıyı bilen herkes; kimlik sorulmaz |
| Telefon ister | Telefon numarası zorunlu |
| Yalnızca personel | Programa giriş yapmış kullanıcılar |

**Kişi başına tek yanıt** ayarı yalnızca telefon ya da personel seçeneğiyle
anlamlıdır: herkese açık bir formda "aynı kişi" diye güvenilir bir ölçüt
yoktur.

> Erişim seçeneği **yanıt geldikten sonra değiştirilemez**. Anonim toplanmış
> yanıtların üstüne sonradan kimlik eklemek mümkün değil.

## Yanıtlar

Formun satırındaki grafik simgesi yanıtlara götürür. İki sekme vardır:

- **Yanıtlar** — kim ne yazdı. Bir satıra dokunduğunuzda tüm cevapları
  görürsünüz. Takip numarası, ad ya da telefonla arayabilirsiniz.
- **Özet** — genel eğilim. Seçim sorularında yüzde dağılımı, ölçeklerde
  ortalama, metin sorularında son birkaç cevap.

**Excel** düğmesi bütün yanıtları indirir; sütunlar formunuzun sorularından
oluşur.

> Yinelenen ya da kötüye kullanım amaçlı bir yanıtı **geçersiz
> sayabilirsiniz**. Kayıt silinmez, yalnızca sayımdan düşer — "kaç kişi
> yanıtladı" ile "kaçı sayıldı" ayrı bilgidir.

## Sonuç sayfası

Vatandaş formu gönderdiğinde bir **takip numarası** alır. Ayarlardan
teşekkür metnini yazabilir ve isterseniz verdiği cevapların özetini de
gösterebilirsiniz.

## Vatandaşın deneyimi

- Form **giriş gerektirmez**; menü ve program ekranları görünmez.
- Yarım bırakılan form tarayıcıda saklanır; geri döndüğünde kaldığı yerden
  devam etmek isteyip istemediği sorulur.
- Zorunlu bir alan boşsa gönderilmez ve ekran ilk eksik soruya kayar.

> Formu yayınlamadan önce **Önizleme** sekmesinden vatandaşın göreceği hâli
> deneyin. Önizleme gerçek formun aynısıdır.
