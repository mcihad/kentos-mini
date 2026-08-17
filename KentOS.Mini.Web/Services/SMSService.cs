using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KentOS.Mini.Web.Services
{
    public class SMSService(
        ILogger<SMSService> _logger,
        Options.SmsOptions _ayar,
        IHttpClientFactory _httpClientFactory) : ISMSService
    {
        public async Task<bool> SendAsync(string token, string title, string body, string data)
        {
            if (!_ayar.IsConfigured)
            {
                // Eskiden eksik ayar "null adrese istek" hatasıyla ortaya
                // çıkıyordu; sebebini anlamak için günlüğü kazmak gerekiyordu.
                _logger.LogError(
                    "SMS ayarları eksik (SMS__URL / SMS__USERNAME / SMS__PASSWORD). Mesaj gönderilmedi.");
                throw new InvalidOperationException("SMS sağlayıcı ayarları tanımsız.");
            }

            // IHttpClientFactory: singleton serviste her çağrıda `new HttpClient()`
            // yaratmak soket/port tükenmesine (SocketException) yol açıyordu.
            var client = _httpClientFactory.CreateClient("sms");
            var sender = _ayar.Sender;
            var base64 = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_ayar.Username}:{_ayar.Password}"));

            var request = new HttpRequestMessage(HttpMethod.Post, _ayar.Url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
            var num = token.Length == 11 ? "9" + token : "90" + token;
            var smsRequestData = new SmsRequest
            {
                Number = num,
                Sender = sender,
                Title = title,
                Content = body,
            };

            request.Content = new StringContent(JsonSerializer.Serialize(smsRequestData), Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(responseString);
            if (response.IsSuccessStatusCode)
            {
                var smsResponse = JsonSerializer.Deserialize<SmsResponse>(responseString);
                if (smsResponse?.Data?.PkgID > 0)
                {
                    return true;
                }
                else
                {
                    _logger.LogError($"SMS sending failed: {responseString}");
                    throw new Exception("SMS sending failed");
                }
            } else {
                _logger.LogError($"SMS sending failed: {responseString}");
                throw new Exception("SMS sending failed."+ responseString);
            }
        }
    }
}
