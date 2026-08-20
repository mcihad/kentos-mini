using KentOS.Mini.Web.Options;

namespace KentOS.Mini.Web.Services;

/// <summary>
/// Dışarıya verilen MUTLAK adresleri üretir — SMS bağlantısı, dönüş adresi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Adres artık isteğin KENDİSİNDEN geliyor, ayardan değil.</b> Önceden
/// <c>App:BaseUrl</c> okunuyordu ve o tek bir alan adı: uygulama başka bir
/// adresten yayınlandığında (aynı kurumda ikinci bir alan adı, taşınma, test
/// ortamı) çiçekçiye giden SMS <b>yanlış adrese</b> götürüyordu. Ölçülen
/// durum: uygulama <c>akillisehir…</c> altında çalışıyor, SMS
/// <c>randevu…</c> yazıyordu.
/// </para>
/// <para>
/// <b>Şema ters vekilden okunuyor.</b> IIS/nginx TLS'i sonlandırıp içeriye
/// düz HTTP konuşuyor; <c>Request.Scheme</c> tek başına <c>http</c> derdi ve
/// SMS'teki bağlantı güvensiz görünürdü. <c>UseForwardedHeaders</c>
/// (<c>X-Forwarded-Proto</c>) bunu düzeltiyor ve <c>Program.cs</c>'te
/// <b>ara katman sırasının başında</b> duruyor.
/// </para>
/// <para>
/// <b>İstek dışındayken ayara düşülür.</b> Arka plan servisleri
/// (<c>FirebaseWorker</c>, <c>TekrarUfkuWorker</c>) ve testler bir HTTP
/// isteğinin içinde değil; orada tahmin edilecek bir alan adı yok.
/// </para>
/// <para>
/// <b>GÜVENLİK — <c>Host</c> başlığı istemci denetimindedir.</b> Dinamik
/// adres, "host header injection" yüzeyini açar: kimliği doğrulanmış bir
/// kullanıcı sahte bir <c>Host</c> ile istek atıp SMS'e kendi adresini
/// yazdırabilir. Savunma <c>AllowedHosts</c> ayarıdır (bkz.
/// <c>.env.example</c>); <c>*</c> bırakılırsa çerçevenin ana bilgisayar
/// süzgeci devre dışı kalır. Kurulum belgesi bu yüzden alan adını yazmayı
/// <b>şart koşuyor</b>.
/// </para>
/// </remarks>
public interface IAdresCozucu
{
    /// <summary>Şema + ana bilgisayar, sonda eğik çizgi YOK.</summary>
    string Taban();

    /// <summary>Verilen göreli yolun mutlak hâli.</summary>
    string Mutlak(string goreliYol);
}

/// <inheritdoc cref="IAdresCozucu"/>
public sealed class AdresCozucu(
    IHttpContextAccessor _baglamErisimi,
    ApplicationOptions _uygulamaAyari) : IAdresCozucu
{
    public string Taban()
    {
        var istek = _baglamErisimi.HttpContext?.Request;

        if (istek is not null && istek.Host.HasValue)
        {
            // `Host.Value` bağlantı noktasını da taşıyor (`localhost:5099`),
            // yani geliştirmede de doğru adres çıkıyor.
            return $"{istek.Scheme}://{istek.Host.Value}";
        }

        return (_uygulamaAyari.BaseUrl ?? string.Empty).TrimEnd('/');
    }

    public string Mutlak(string goreliYol) =>
        $"{Taban()}/{goreliYol.TrimStart('/')}";
}
