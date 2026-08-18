using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.IsTakip;

/// <summary>
/// SEÇİLEBİLİR PERSONEL — "bu işi kime veririm?"
/// </summary>
/// <remarks>
/// <para>
/// Görev ataması, ekip üyeliği ve proje ekibi aynı soruyu soruyor ve aynı
/// cevabı hak ediyor. Üç ekran da <c>/ayar/birim-kullanicilari</c> ucunu
/// kullanıyordu; o uç <b>gizli etkinlik katılımcı seçicisi</b> için yazılmış
/// ve iki kuralı var: yalnızca kullanıcının <b>tam olarak kendi</b> birimi ve
/// <b>oturum sahibi listede yok</b>. Etkinlik davetinde ikisi de doğru —
/// davet eden zaten katılımcı. İş takibinde ikisi de yıkıcı:
/// </para>
/// <list type="bullet">
///   <item>Kişi <b>kendini</b> göreve atayamıyor, kendi kurduğu ekibe
///         giremiyor, yönettiği projeye üye olamıyordu — üstelik sunucu
///         proje yöneticisinin üye olmasını <b>şart koşuyor</b>.</item>
///   <item>Alt birimlerdeki personel hiç görünmüyordu; müdür, müdürlüğünün
///         altındaki şefliğe iş veremiyordu.</item>
/// </list>
///
/// <para>
/// Ölçüldü: 13 kullanıcılı geliştirme veritabanında <c>admin</c> hesabının
/// ekip üyesi seçim kutusunda <b>tek bir kişi</b> çıkıyordu. Kullanıcının
/// "ekibe kişi ekleme yok" demesi bu yüzden.
/// </para>
///
/// <para>
/// İletişim bilgisi (e-posta, telefon, cihaz jetonu) <b>taşımaz</b>: liste
/// birimdeki herkese açık, ad/unvan/birim dışında bir şey sızmamalı —
/// <c>KatilimciDto</c> ile aynı gerekçe.
/// </para>
/// </remarks>
public class PersonelSecimDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("ad")] public string Ad { get; set; } = string.Empty;
    [JsonPropertyName("unvan")] public string? Unvan { get; set; }
    [JsonPropertyName("birimId")] public long? BirimId { get; set; }
    [JsonPropertyName("birimAd")] public string? BirimAd { get; set; }

    /// <summary>Oturumu açan kişinin kendisi mi? Arayüz "(siz)" diye işaretler.</summary>
    [JsonPropertyName("kendisi")] public bool Kendisi { get; set; }

    /// <summary>Etkin birimin DIŞINDA, alt birimlerden mi geliyor?</summary>
    [JsonPropertyName("altBirimden")] public bool AltBirimden { get; set; }
}
