using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KentOS.Mini.Application.Enums;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

/// <summary>
/// GÖREV ÖZETİ — liste satırı.
/// </summary>
/// <remarks>
/// <para>
/// Durum adı ve rengi SUNUCUDA üretiliyor. İki istemcinin aynı duruma farklı
/// ad ya da renk vermesi bu sayede imkânsız — mevcut <c>HalkGunuServisi</c>
/// kalıbı.
/// </para>
/// <para>
/// <see cref="Gecikti"/> ve <see cref="KalanSaat"/> de burada hesaplanıyor:
/// istemcinin saati yanlışsa gecikme tablosu da yanlış olurdu.
/// </para>
/// </remarks>
public class GorevOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("takipNo")] public string TakipNo { get; set; } = string.Empty;
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;

    [JsonPropertyName("durum")] public GorevDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("durumRenk")] public string DurumRenk { get; set; } = string.Empty;

    [JsonPropertyName("oncelik")] public GorevOnceligi Oncelik { get; set; }
    [JsonPropertyName("oncelikAd")] public string OncelikAd { get; set; } = string.Empty;

    [JsonPropertyName("kaynak")] public GorevKaynagi Kaynak { get; set; }
    [JsonPropertyName("kaynakAd")] public string KaynakAd { get; set; } = string.Empty;

    [JsonPropertyName("gorevTipiId")] public long? GorevTipiId { get; set; }
    [JsonPropertyName("gorevTipiAd")] public string? GorevTipiAd { get; set; }

    [JsonPropertyName("birimId")] public long BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    [JsonPropertyName("ustGorevId")] public long? UstGorevId { get; set; }
    [JsonPropertyName("altGorevSayisi")] public int AltGorevSayisi { get; set; }

    [JsonPropertyName("enlem")] public double? Enlem { get; set; }
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    [JsonPropertyName("planlananBitis")] public DateTime? PlanlananBitis { get; set; }
    [JsonPropertyName("slaBitis")] public DateTime? SlaBitis { get; set; }
    [JsonPropertyName("olusturmaTarihi")] public DateTime OlusturmaTarihi { get; set; }
    [JsonPropertyName("tamamlanmaTarihi")] public DateTime? TamamlanmaTarihi { get; set; }

    /// <summary>SLA aşıldı mı? Kapanmış görevde her zaman <c>false</c>.</summary>
    [JsonPropertyName("gecikti")] public bool Gecikti { get; set; }

    /// <summary>SLA bitişine kalan saat; negatifse aşım. SLA yoksa <c>null</c>.</summary>
    [JsonPropertyName("kalanSaat")] public double? KalanSaat { get; set; }

    /// <summary>Tamamlanan aşama / toplam aşama — liste satırındaki ilerleme çubuğu.</summary>
    [JsonPropertyName("asamaToplam")] public int AsamaToplam { get; set; }
    [JsonPropertyName("asamaBiten")] public int AsamaBiten { get; set; }

    /// <summary>
    /// Görevin ilerlemesi (0–100) — SUNUCUDA hesaplanır.
    /// </summary>
    /// <remarks>
    /// Aşaması olan görevde aşamalardan, olmayanda durumdan okunuyor; kuralın
    /// tamamı ve gerekçesi <c>GorevDurumAkisi.Ilerleme</c> içinde. Proje
    /// yüzdesi, gantt çubuğunun doluluğu ve kilometre taşı oranı hep bu tek
    /// sayıdan besleniyor — iki ekranın aynı işe farklı yüzde vermesi bu
    /// sayede imkânsız.
    ///
    /// <para>
    /// <b>%100 yalnızca onaylanmış görevde.</b> Aşamaları biten ama onay
    /// bekleyen iş %95'te durur.
    /// </para>
    /// </remarks>
    [JsonPropertyName("ilerleme")] public int Ilerleme { get; set; }

    /// <summary>Atanan kişi ve ekip adları — listede "kimde?" sorusunun cevabı.</summary>
    [JsonPropertyName("sorumlular")] public List<string> Sorumlular { get; set; } = [];
}

/// <summary>Görevin tam detayı.</summary>
public class GorevDetayDto : GorevOzetDto
{
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("gerekce")] public string? Gerekce { get; set; }
    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("mahalleAd")] public string? MahalleAd { get; set; }
    [JsonPropertyName("planlananBaslangic")] public DateTime? PlanlananBaslangic { get; set; }
    [JsonPropertyName("baslamaTarihi")] public DateTime? BaslamaTarihi { get; set; }
    [JsonPropertyName("beklemeDakika")] public int BeklemeDakika { get; set; }
    [JsonPropertyName("olusturan")] public string? Olusturan { get; set; }
    [JsonPropertyName("onaylayan")] public string? Onaylayan { get; set; }

    /// <summary>Kayıt hangi birim adına açıldı — vekâlet izi.</summary>
    [JsonPropertyName("olusturanBirimId")] public long? OlusturanBirimId { get; set; }
    [JsonPropertyName("olusturanBirimAd")] public string? OlusturanBirimAd { get; set; }

    [JsonPropertyName("projeId")] public long? ProjeId { get; set; }
    [JsonPropertyName("kilometreTasiId")] public long? KilometreTasiId { get; set; }

    [JsonPropertyName("asamalar")] public List<GorevAsamaDto> Asamalar { get; set; } = [];
    [JsonPropertyName("atamalar")] public List<GorevAtamaDto> Atamalar { get; set; } = [];
    [JsonPropertyName("altGorevler")] public List<GorevOzetDto> AltGorevler { get; set; } = [];

    /// <summary>
    /// Bu durumdan gidilebilecek durumlar — arayüz düğmelerini buradan çizer.
    /// </summary>
    /// <remarks>
    /// Akış sunucuda; istemci hangi düğmenin görüneceğini kendi kurallarıyla
    /// hesaplasaydı, iki istemci farklı düğme gösterir ve biri her zaman
    /// sunucudan geri çevrilirdi.
    /// </remarks>
    [JsonPropertyName("sonrakiDurumlar")] public List<GorevDurumSecenegiDto> SonrakiDurumlar { get; set; } = [];
}

/// <summary>Geçilebilecek bir durum — düğme etiketi ve rengiyle.</summary>
public class GorevDurumSecenegiDto
{
    [JsonPropertyName("durum")] public GorevDurumu Durum { get; set; }

    /// <summary>Durumun ADI — "İptal edildi", "Beklemede".</summary>
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;

    /// <summary>
    /// DÜĞME ETİKETİ — "İptal et", "Beklemeye al".
    /// </summary>
    /// <remarks>
    /// Düğmeler <see cref="Ad"/> ile yazılıyordu ve ekranda geçmiş zamanlı
    /// birer beyan çıkıyordu: üzerinde "İptal edildi" yazan bir düğme,
    /// basılmadan önce işin zaten iptal olduğunu söylüyor. Alan EK: eski
    /// istemciler <c>ad</c>'ı okumaya devam ediyor.
    /// </remarks>
    [JsonPropertyName("eylem")] public string Eylem { get; set; } = string.Empty;

    [JsonPropertyName("renk")] public string Renk { get; set; } = string.Empty;
}

/// <summary>Görevin bir aşaması.</summary>
public class GorevAsamaDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("siraNo")] public int SiraNo { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("durum")] public GorevAsamaDurumu Durum { get; set; }
    [JsonPropertyName("durumAd")] public string DurumAd { get; set; } = string.Empty;
    [JsonPropertyName("zorunlu")] public bool Zorunlu { get; set; }
    [JsonPropertyName("aciklamaZorunlu")] public bool AciklamaZorunlu { get; set; }
    [JsonPropertyName("fotografZorunlu")] public bool FotografZorunlu { get; set; }
    [JsonPropertyName("not")] public string? Not { get; set; }
    [JsonPropertyName("tamamlanmaTarihi")] public DateTime? TamamlanmaTarihi { get; set; }
    [JsonPropertyName("tamamlayan")] public string? Tamamlayan { get; set; }

    /// <summary>Bu aşamaya yüklenmiş fotoğraf/dosya sayısı.</summary>
    [JsonPropertyName("ekSayisi")] public int EkSayisi { get; set; }

    /// <summary>
    /// Aşamaya yüklenen dosyalar — <b>fotoğraf kanıtı burada</b>.
    /// </summary>
    /// <remarks>
    /// Yalnızca <see cref="EkSayisi"/> gönderiliyordu ve arayüz "2 dosya"
    /// yazan gri bir satır çiziyordu: aşama fotoğrafı ZORUNLU tutulan bir
    /// modülde, çekilen fotoğrafı görmenin hiçbir yolu yoktu. Sayı kaldı
    /// (rozet ve "fotoğraf var mı" denetimi onu okuyor), yanına dosyaların
    /// kendisi eklendi.
    /// </remarks>
    [JsonPropertyName("ekler")] public List<IsEkDto> Ekler { get; set; } = [];

    /// <summary>Sıradaki aşama mı? Yalnızca bu tamamlanabilir.</summary>
    [JsonPropertyName("sirada")] public bool Sirada { get; set; }
}

/// <summary>Görevin bir ataması — kişi ya da ekip.</summary>
public class GorevAtamaDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("kullaniciId")] public long? KullaniciId { get; set; }
    [JsonPropertyName("kullaniciAd")] public string? KullaniciAd { get; set; }
    [JsonPropertyName("ekipId")] public long? EkipId { get; set; }
    [JsonPropertyName("ekipAd")] public string? EkipAd { get; set; }
    [JsonPropertyName("rol")] public GorevAtamaRolu Rol { get; set; }
    [JsonPropertyName("rolAd")] public string RolAd { get; set; } = string.Empty;
    [JsonPropertyName("atayan")] public string? Atayan { get; set; }
    [JsonPropertyName("atamaTarihi")] public DateTime AtamaTarihi { get; set; }
}

/// <summary>Zaman çizelgesi satırı.</summary>
public class IsOlayDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tip")] public GorevOlayTipi Tip { get; set; }
    [JsonPropertyName("tipAd")] public string TipAd { get; set; } = string.Empty;
    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("kullanici")] public string? Kullanici { get; set; }
    [JsonPropertyName("tarih")] public DateTime Tarih { get; set; }

    /// <summary>Alan bazlı farklar. Fark yoksa boş liste.</summary>
    [JsonPropertyName("degisiklikler")] public List<IsOlayDegisiklikDto> Degisiklikler { get; set; } = [];
}

/// <summary>Tek bir alanın eski/yeni değeri.</summary>
public class IsOlayDegisiklikDto
{
    [JsonPropertyName("alan")] public string Alan { get; set; } = string.Empty;
    [JsonPropertyName("eski")] public string? Eski { get; set; }
    [JsonPropertyName("yeni")] public string? Yeni { get; set; }
}

/// <summary>
/// Görev oluşturma/güncelleme isteği.
/// </summary>
/// <remarks>
/// <see cref="Kaynak"/> ve <see cref="KaynakId"/> gövdede taşınıyor ki
/// talepten, ajandadan ya da vatandaş bildiriminden görev açmak için AYRI bir
/// oluşturma yolu yazmak gerekmesin. Bugün yalnızca elle ve saha kullanılıyor;
/// ötekiler aynı metoda birer çağrı olarak eklenecek.
/// </remarks>
public class GorevKayitDto
{
    [Required(ErrorMessage = "Başlık zorunlu.")]
    [MaxLength(300)]
    [JsonPropertyName("baslik")] public string Baslik { get; set; } = string.Empty;

    [JsonPropertyName("aciklama")] public string? Aciklama { get; set; }
    [JsonPropertyName("gorevTipiId")] public long? GorevTipiId { get; set; }
    [JsonPropertyName("oncelik")] public GorevOnceligi? Oncelik { get; set; }
    [JsonPropertyName("kaynak")] public GorevKaynagi Kaynak { get; set; } = GorevKaynagi.Manuel;
    [JsonPropertyName("kaynakId")] public long? KaynakId { get; set; }

    /// <summary>Üst görev — alt görev açarken. Üst görevin birimi devralınır.</summary>
    [JsonPropertyName("ustGorevId")] public long? UstGorevId { get; set; }

    [Range(-90, 90, ErrorMessage = "Enlem -90 ile 90 arasında olmalı.")]
    [JsonPropertyName("enlem")] public double? Enlem { get; set; }

    [Range(-180, 180, ErrorMessage = "Boylam -180 ile 180 arasında olmalı.")]
    [JsonPropertyName("boylam")] public double? Boylam { get; set; }

    [MaxLength(500)]
    [JsonPropertyName("adres")] public string? Adres { get; set; }

    [JsonPropertyName("mahalleId")] public long? MahalleId { get; set; }
    [JsonPropertyName("planlananBaslangic")] public DateTime? PlanlananBaslangic { get; set; }
    [JsonPropertyName("planlananBitis")] public DateTime? PlanlananBitis { get; set; }
    [JsonPropertyName("projeId")] public long? ProjeId { get; set; }
    [JsonPropertyName("kilometreTasiId")] public long? KilometreTasiId { get; set; }

    /// <summary>Açılışta atama — boş bırakılabilir, sonra atanır.</summary>
    [JsonPropertyName("atamalar")] public List<GorevAtamaIstegiDto> Atamalar { get; set; } = [];
}

/// <summary>Kişi ya da ekip ataması isteği.</summary>
public class GorevAtamaIstegiDto
{
    [JsonPropertyName("kullaniciId")] public long? KullaniciId { get; set; }
    [JsonPropertyName("ekipId")] public long? EkipId { get; set; }
    [JsonPropertyName("rol")] public GorevAtamaRolu Rol { get; set; } = GorevAtamaRolu.Sorumlu;
}

/// <summary>Durum değiştirme isteği.</summary>
public class GorevDurumIstegiDto
{
    [JsonPropertyName("durum")] public GorevDurumu Durum { get; set; }

    /// <summary>İade, ret ve iptalde ZORUNLU.</summary>
    [MaxLength(1000)]
    [JsonPropertyName("gerekce")] public string? Gerekce { get; set; }
}

/// <summary>Aşama tamamlama isteği.</summary>
public class GorevAsamaIstegiDto
{
    [MaxLength(2000)]
    [JsonPropertyName("not")] public string? Not { get; set; }

    /// <summary>Zorunlu olmayan aşamayı atla.</summary>
    [JsonPropertyName("atla")] public bool Atla { get; set; }
}

/// <summary>Görev listesi süzgeci.</summary>
public class GorevSuzgecDto : Ortak.SayfaIstegi
{
    [JsonPropertyName("durumlar")] public List<GorevDurumu>? Durumlar { get; set; }
    [JsonPropertyName("oncelikler")] public List<GorevOnceligi>? Oncelikler { get; set; }
    [JsonPropertyName("kaynaklar")] public List<GorevKaynagi>? Kaynaklar { get; set; }
    [JsonPropertyName("gorevTipiId")] public long? GorevTipiId { get; set; }
    [JsonPropertyName("projeId")] public long? ProjeId { get; set; }

    /// <summary>Belirli bir kişiye atanmış görevler.</summary>
    [JsonPropertyName("kullaniciId")] public long? KullaniciId { get; set; }
    [JsonPropertyName("ekipId")] public long? EkipId { get; set; }

    /// <summary>Yalnızca kök görevler — ağacın gövdesi listede tekrar etmesin.</summary>
    [JsonPropertyName("yalnizKok")] public bool YalnizKok { get; set; }

    /// <summary>Yalnızca SLA'sı aşılmış görevler.</summary>
    [JsonPropertyName("yalnizGeciken")] public bool YalnizGeciken { get; set; }

    /// <summary>Etkin birimin ALT birimlerini de kapsa.</summary>
    [JsonPropertyName("altBirimlerDahil")] public bool AltBirimlerDahil { get; set; }

    [JsonPropertyName("baslangic")] public DateTime? Baslangic { get; set; }
    [JsonPropertyName("bitis")] public DateTime? Bitis { get; set; }
}
