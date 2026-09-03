using System;
using System.Text;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.V2.Ortak;

/// <summary>Hata listesi süzgeci.</summary>
public class HataSuzgeci : SayfaIstegi
{
    /// <summary>`true` çözülenler, `false` çözülmeyenler, `null` hepsi.</summary>
    [JsonPropertyName("cozuldu")]
    public bool? Cozuldu { get; set; }
}

/// <summary>Hata listesi öğesi.</summary>
public class HataOzetDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tur")] public string Tur { get; set; } = string.Empty;
    [JsonPropertyName("mesaj")] public string Mesaj { get; set; } = string.Empty;
    [JsonPropertyName("yol")] public string? Yol { get; set; }
    [JsonPropertyName("yontem")] public string? Yontem { get; set; }
    [JsonPropertyName("durumKodu")] public int DurumKodu { get; set; }
    [JsonPropertyName("adet")] public int Adet { get; set; }
    [JsonPropertyName("ilkGorulme")] public DateTime IlkGorulme { get; set; }
    [JsonPropertyName("sonGorulme")] public DateTime SonGorulme { get; set; }
    [JsonPropertyName("kullaniciAdi")] public string? KullaniciAdi { get; set; }
    [JsonPropertyName("cozuldu")] public bool Cozuldu { get; set; }

    /// <summary>Türün yalnızca sınıf adı — tam ad listede taşmıyor.</summary>
    [JsonPropertyName("kisaTur")]
    public string KisaTur => Tur.Contains('.') ? Tur[(Tur.LastIndexOf('.') + 1)..] : Tur;
}

/// <summary>Hata detayı — teşhis için gereken her şey.</summary>
public class HataDetayDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("parmakizi")] public string Parmakizi { get; set; } = string.Empty;

    [JsonPropertyName("tur")] public string Tur { get; set; } = string.Empty;
    [JsonPropertyName("mesaj")] public string Mesaj { get; set; } = string.Empty;
    [JsonPropertyName("icMesaj")] public string? IcMesaj { get; set; }
    [JsonPropertyName("yiginIzi")] public string? YiginIzi { get; set; }
    [JsonPropertyName("dosya")] public string? Dosya { get; set; }
    [JsonPropertyName("satir")] public int? Satir { get; set; }
    [JsonPropertyName("durumKodu")] public int DurumKodu { get; set; }

    [JsonPropertyName("yol")] public string? Yol { get; set; }
    [JsonPropertyName("yontem")] public string? Yontem { get; set; }
    [JsonPropertyName("sorguDizesi")] public string? SorguDizesi { get; set; }
    [JsonPropertyName("govde")] public string? Govde { get; set; }
    [JsonPropertyName("basliklar")] public string? Basliklar { get; set; }

    [JsonPropertyName("kullaniciId")] public long? KullaniciId { get; set; }
    [JsonPropertyName("kullaniciAdi")] public string? KullaniciAdi { get; set; }
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("ipAdresi")] public string? IpAdresi { get; set; }
    [JsonPropertyName("istemci")] public string? Istemci { get; set; }
    [JsonPropertyName("izKimligi")] public string? IzKimligi { get; set; }

    [JsonPropertyName("adet")] public int Adet { get; set; }
    [JsonPropertyName("ilkGorulme")] public DateTime IlkGorulme { get; set; }
    [JsonPropertyName("sonGorulme")] public DateTime SonGorulme { get; set; }

    [JsonPropertyName("cozuldu")] public bool Cozuldu { get; set; }
    [JsonPropertyName("cozulmeTarihi")] public DateTime? CozulmeTarihi { get; set; }
    [JsonPropertyName("cozenKullanici")] public string? CozenKullanici { get; set; }
    [JsonPropertyName("notlar")] public string? Notlar { get; set; }

    /// <summary>
    /// Yapay zekâ ajanına doğrudan yapıştırılabilecek rapor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bu alanın tek amacı kopyalanıp bir ajana verilmek. O yüzden ham veri
    /// dökümü değil: <b>ne olduğu, nerede olduğu, hangi istekle tetiklendiği
    /// ve ne beklendiği</b> sırayla yazılır. Yığın izi sona konur — önce
    /// bağlam okunmalı.
    /// </para>
    /// <para>
    /// Sunucuda üretilir, istemcide değil: aynı metnin web ve mobilde iki kez
    /// kurulması, birinin eksik kalması demekti.
    /// </para>
    /// </remarks>
    [JsonPropertyName("aiRaporu")]
    public string AiRaporu => RaporUret();

    private string RaporUret()
    {
        var y = new StringBuilder();

        y.AppendLine("# Sunucu hatası — çözüm isteği");
        y.AppendLine();
        y.AppendLine("Aşağıdaki hata canlı bir ASP.NET Core (.NET 10) uygulamasında oluştu.");
        // Kurum adı YAZILMAZ: uygulama başka kurumlara verilecek ve bu metin
        // hata ekranından kopyalanıp dışarıya (yapay zekâ aracına) gidiyor.
        y.AppendLine("Uygulama: kurumsal ajanda/talep takip sistemi.");
        y.AppendLine("Katmanlar: `Web` (controller + servis + EF Core/Npgsql), `Application` (DTO/model).");
        y.AppendLine("Veritabanı: PostgreSQL, snake_case adlandırma (`EFCore.NamingConventions`).");
        y.AppendLine();
        y.AppendLine("## Ne oldu");
        y.AppendLine();
        y.AppendLine($"- **Tür:** `{Tur}`");
        y.AppendLine($"- **Mesaj:** {Mesaj}");
        if (!string.IsNullOrWhiteSpace(IcMesaj)) y.AppendLine($"- **İç hata:** {IcMesaj}");
        if (!string.IsNullOrWhiteSpace(Dosya))
            y.AppendLine($"- **Konum:** `{Dosya}`{(Satir is > 0 ? $" satır {Satir}" : "")}");
        y.AppendLine($"- **HTTP durum:** {DurumKodu}");
        y.AppendLine($"- **Kaç kez:** {Adet} (ilk: {IlkGorulme:dd.MM.yyyy HH:mm}, son: {SonGorulme:dd.MM.yyyy HH:mm})");
        y.AppendLine();
        y.AppendLine("## Hangi istekte");
        y.AppendLine();
        y.AppendLine($"- **Uç:** `{Yontem} {Yol}{SorguDizesi}`");
        if (!string.IsNullOrWhiteSpace(KullaniciAdi))
            y.AppendLine($"- **Kullanıcı:** {KullaniciAdi} (id {KullaniciId}, birim {BirimId})");
        if (!string.IsNullOrWhiteSpace(IpAdresi)) y.AppendLine($"- **IP:** {IpAdresi}");
        if (!string.IsNullOrWhiteSpace(Istemci)) y.AppendLine($"- **İstemci:** {Istemci}");
        if (!string.IsNullOrWhiteSpace(IzKimligi)) y.AppendLine($"- **İz kimliği:** `{IzKimligi}`");

        if (!string.IsNullOrWhiteSpace(Govde))
        {
            y.AppendLine();
            y.AppendLine("### İstek gövdesi");
            y.AppendLine();
            y.AppendLine("```json");
            y.AppendLine(Govde.Trim());
            y.AppendLine("```");
        }

        if (!string.IsNullOrWhiteSpace(Basliklar))
        {
            y.AppendLine();
            y.AppendLine("<details><summary>İstek başlıkları</summary>");
            y.AppendLine();
            y.AppendLine("```");
            y.AppendLine(Basliklar.Trim());
            y.AppendLine("```");
            y.AppendLine();
            y.AppendLine("</details>");
        }

        if (!string.IsNullOrWhiteSpace(Notlar))
        {
            y.AppendLine();
            y.AppendLine("## Ekibin notları");
            y.AppendLine();
            y.AppendLine(Notlar.Trim());
        }

        y.AppendLine();
        y.AppendLine("## Yığın izi");
        y.AppendLine();
        y.AppendLine("```");
        y.AppendLine((YiginIzi ?? "(yok)").Trim());
        y.AppendLine("```");

        y.AppendLine();
        y.AppendLine("## Senden beklenen");
        y.AppendLine();
        y.AppendLine("1. Hatanın **kök nedenini** söyle — mesajı tekrar etme, neden oluştuğunu açıkla.");
        y.AppendLine("2. Hangi dosyada ne değişmeli, **kod olarak** yaz.");
        y.AppendLine("3. Bu hatayı yakalayacak bir **test** öner.");
        y.AppendLine("4. Aynı kök nedenin başka nerelerde olabileceğini belirt.");

        return y.ToString();
    }
}

/// <summary>Hata notu / çözüm durumu güncelleme isteği.</summary>
public class HataNotIstegi
{
    [JsonPropertyName("notlar")] public string? Notlar { get; set; }
    [JsonPropertyName("cozuldu")] public bool Cozuldu { get; set; }
}
