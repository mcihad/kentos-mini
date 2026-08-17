using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Web.Data;
using FirebaseAdmin;
using KentOS.Mini.Application.Services;
namespace KentOS.Mini.Web.Services
{
    public class FirebaseWorker(
        IServiceProvider _services,
        ISMSService _smsService,
        Options.SmsOptions _smsAyari,
        ILogger<FirebaseWorker> _logger) : BackgroundService
    {
        /// <summary>
        /// SMS ayarı eksik uyarısı verildi mi.
        /// </summary>
        /// <remarks>
        /// İşçi on saniyede bir dönüyor; uyarıyı her turda yazmak günlüğü
        /// dakikada altı satırla dolduruyor ve asıl hatayı gömüyordu.
        /// </remarks>
        private bool _smsUyarisiVerildi;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendNotificationAsync();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Notifikasyon gönderme işlemi iptal edildi.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notifikasyon gönderirken Bir hata oluştu.");
                }
                await Task.Delay(10000, stoppingToken);
            }
        }

        private async Task SendNotificationAsync()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _logger.LogInformation("Notifikasyon gönderme işlemi başladı.");
            // Parti sınırı: web jetonu eklenmesiyle bir bildirim artık kullanıcı başına
            // iki satır üretebiliyor. Sorgu sınırsızdı; birikmiş bir kuyrukta tek
            // turda binlerce satır işlemeye kalkardı.
            var messages = await dbContext.Messages
                .Where(x => x.IsSuccess == false && x.RetryCount < 3)
                .OrderBy(x => x.Id)
                .Take(200)
                .ToListAsync();

            /*
              SMS, FIREBASE'E BAĞLI DEĞİL.

              Burada `FirebaseMessaging.DefaultInstance` KOŞULSUZ okunuyordu.
              Kimlik dosyası yoksa (açılışta yalnızca uyarı veriliyor,
              uygulama ayakta kalıyor) bu satır istisna fırlatıyor, istisnayı
              dışarıdaki `catch` yutuyor ve TURUN TAMAMI atlanıyordu — sırada
              bekleyen SMS'ler dâhil. Yani "Firebase kurulmamış" bir
              kurulumda SMS hiç gitmiyordu ve tek belirtisi, kimsenin
              bakmadığı bir günlük satırıydı.

              Erişim artık yalnızca push dalında ve orada da null denetimiyle.
            */
            var firebaseKurulu = FirebaseApp.DefaultInstance is not null;

            if (!firebaseKurulu && messages.Any(m => m.MessageType == SendMessageType.PushNotification))
            {
                _logger.LogWarning(
                    "Firebase kurulu değil; {Sayi} push bildirimi atlanıyor. SMS gönderimi etkilenmiyor.",
                    messages.Count(m => m.MessageType == SendMessageType.PushNotification));
            }

            foreach (var message in messages)
            {
                if (message.MessageType == SendMessageType.PushNotification)
                {
                    /*
                      Kurulu değilse mesaja DOKUNULMUYOR: `RetryCount`
                      artırmak, kimlik dosyası sonradan konulduğunda üç turda
                      tükenmiş ve kalıcı olarak ölmüş bir kuyruk bırakırdı.
                      Yapılandırma hatası, mesajın hatası değil.
                    */
                    if (!firebaseKurulu) continue;

                    var fcmMessage = new FirebaseAdmin.Messaging.Message()
                    {
                        Notification = new FirebaseAdmin.Messaging.Notification
                        {
                            Title = message.Title,
                            Body = message.Content
                        },
                        Token = message.Token,

                    };

                    if (!string.IsNullOrEmpty(message.Data))
                    {
                        fcmMessage.Data = new Dictionary<string, string>
                        {
                            { "fcmData", message.Data }
                        };
                    }
                    var response = string.Empty;
                    try
                    {
                        _logger.LogInformation("Notifikasyon gönderiliyor.");
                        // `DefaultInstance` yalnızca BURADA okunuyor: yukarıda
                        // koşulsuz okunması, Firebase'siz kurulumlarda SMS
                        // kuyruğunu da durduruyordu.
                        response = await FirebaseAdmin.Messaging.FirebaseMessaging
                            .DefaultInstance.SendAsync(fcmMessage);
                        _logger.LogInformation("Notifikasyon gönderildi.");

                        if (!string.IsNullOrEmpty(response))
                        {
                            message.IsSuccess = true;
                            message.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            message.RetryCount++;
                            message.FailMessage = "Bir hata oluştu.";
                        }
                    }
                    catch (FirebaseAdmin.Messaging.FirebaseMessagingException fmex) when (
                        fmex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.Unregistered ||
                        fmex.MessagingErrorCode == FirebaseAdmin.Messaging.MessagingErrorCode.SenderIdMismatch)
                    {
                        // Jeton artık geçersiz (uygulama silinmiş, tarayıcı verisi
                        // temizlenmiş, jeton yenilenmiş). Yeniden denemek anlamsız;
                        // jetonu kayıtlardan da düşür ki bir daha kuyruğa girmesin.
                        //
                        // DİKKAT: `InvalidArgument` BİLEREK dışarıda. O, bozuk bir
                        // MESAJ gövdesinde de üretiliyor; ona göre budamak, tek bir
                        // payload hatasının sahadaki bütün cihazların kaydını
                        // silmesine yol açardı.
                        message.RetryCount = 3;
                        message.FailMessage = "Jeton geçersiz — kayıttan düşürüldü.";

                        // Hangi sütun olduğu bilinmiyor; değere göre ikisi de denenir.
                        await dbContext.Users
                            .Where(u => u.FcmToken == message.Token)
                            .ExecuteUpdateAsync(su => su.SetProperty(u => u.FcmToken, (string?)null));
                        await dbContext.Users
                            .Where(u => u.WebFcmToken == message.Token)
                            .ExecuteUpdateAsync(su => su.SetProperty(u => u.WebFcmToken, (string?)null));

                        _logger.LogWarning("Geçersiz FCM jetonu temizlendi. Mesaj: {MesajId}", message.Id);
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.FailMessage = "Bir hata oluştu." + ex.Message;
                    }
                    await dbContext.SaveChangesAsync();
                }
                else if (message.MessageType == SendMessageType.SMS)
                {
                    /*
                      AYARLAR EKSİKSE MESAJ HARCANMIYOR.

                      `SMSService` eksik ayarda istisna fırlatıyor; buradaki
                      `catch` onu `RetryCount++` olarak sayıyordu ve üçüncü
                      turda mesaj kalıcı olarak ölüyordu. Sonra ayar
                      düzeltilse bile o SMS'ler bir daha hiç denenmiyordu —
                      kuyrukta "başarısız" görünüp duruyorlardı.

                      Yapılandırma hatası mesajın hatası değil: sayaç
                      artırılmıyor, tur atlanıyor ve sebep bir kez
                      loglanıyor.
                    */
                    if (!_smsAyari.IsConfigured)
                    {
                        if (!_smsUyarisiVerildi)
                        {
                            _smsUyarisiVerildi = true;
                            _logger.LogError(
                                "SMS ayarları eksik (Sms__Url / Sms__Username / Sms__Password). " +
                                "Kuyruktaki SMS'ler ayar gelene kadar BEKLETİLİYOR, düşürülmüyor.");
                        }
                        continue;
                    }

                    try
                    {
                        _logger.LogInformation("SMS gönderiliyor.");
                        var result = await _smsService.SendAsync(message.Token, message.Title, message.Content, message.Data);
                        if (result)
                        {
                            message.IsSuccess = true;
                            message.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            message.RetryCount++;
                            message.FailMessage = "Bir hata oluştu.";
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        /*
                          YAPILANDIRMA HATASI, MESAJIN HATASI DEĞİL.

                          `SMSService` eksik ayarda bu türü fırlatıyor.
                          Yukarıdaki bekçi çoğu durumu zaten kesiyor ama
                          ayarın servise nasıl ulaştığı tek bir yol değil
                          (ortam değişkeni, .env, appsettings) ve bekçiyle
                          servisin gördüğü değer ayrışabiliyor. Sayaç burada
                          da artmıyor: aksi hâlde üç turda kuyruk kalıcı
                          olarak ölüyor ve ayar düzeltilse bile o SMS'ler bir
                          daha denenmiyordu.
                        */
                        if (!_smsUyarisiVerildi)
                        {
                            _smsUyarisiVerildi = true;
                            _logger.LogError(ex,
                                "SMS gönderilemiyor: sağlayıcı ayarları eksik. " +
                                "Kuyruk BEKLETİLİYOR, mesajlar düşürülmüyor.");
                        }
                        continue;
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.FailMessage = "Bir hata oluştu." + ex.Message;
                    }
                    await dbContext.SaveChangesAsync();

                }
            }
        }
    }
}
