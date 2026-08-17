using System.Text.Json.Serialization;

namespace KentOS.Mini.Application.Dto.V2.Talep;

/// <summary>
/// Talep ajandaya eklendiğinde oluşan etkinliğin kimliği.
/// </summary>
/// <remarks>
/// Uç bir dönem yalnızca <c>true</c> dönüyordu; istemci "eklendi" mesajını
/// gösteriyor ama kullanıcı oluşan etkinliğe gidemiyor, takvimde elle
/// arıyordu. Tek alanlı da olsa bir nesne dönülüyor — ileride "çakışan
/// etkinlik var" gibi bilgiler eklenince şekil bozulmasın.
/// </remarks>
public class AjandayaEklendiDto
{
    [JsonPropertyName("etkinlikId")]
    public long EtkinlikId { get; set; }
}
