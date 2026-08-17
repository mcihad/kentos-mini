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
        ILogger<FirebaseWorker> _logger) : BackgroundService
    {
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
            var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
            foreach (var message in messages)
            {
                if (message.MessageType == SendMessageType.PushNotification)
                {
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
                        response = await messaging.SendAsync(fcmMessage);
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
