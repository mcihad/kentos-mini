using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using System.Diagnostics;

namespace KentOS.Mini.Web.Services
{
    public class MessageService(
        AppDbContext _context,
        IUserService _userService,
        ILogger<IMessageService> _logger) : IMessageService
    {
        public Message BuildMessage(long userId, string token, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data)
        {
            var message = new Message
            {
                UserId = userId,
                Token = token,
                Title = title,
                Content = content,
                MessageType = type,
                Data = data,
                CreatedAt = DateTime.Now,
                IsSuccess = false,
                RetryCount = 0                
            };

            return message;
        }

        /// <summary>
        /// Bir kullanıcının push gönderilecek TÜM jetonları.
        ///
        /// <para>
        /// Kullanıcı hem telefondan hem tarayıcıdan giriş yapmış olabilir; her
        /// ikisine de bildirim gitmeli. Tek jeton varsayımı, web'den giriş
        /// yapıldığında mobil bildirimlerin sessizce kesilmesine yol açardı.
        /// </para>
        /// </summary>
        private static IEnumerable<string> PushHedefleri(AppUser kullanici) =>
            new[] { kullanici.FcmToken, kullanici.WebFcmToken }
                .Where(j => !string.IsNullOrWhiteSpace(j))
                .Distinct()!
                .Cast<string>();

        public async Task CreateAsync(long userId, string token, string title, string content, SendMessageType type, NotifikasyonTip notifyTip, string? data)
        {
            try
            {
                if (!await _userService.HasReceiveNotification(userId, notifyTip))
                {
                    _logger.LogWarning("Kullanıcı bildirim almayı reddetti.");
                    return;
                }
                if (type == SendMessageType.PushNotification)
                {
                    // Push'ta verilen `token` YOKSAYILIR ve kullanıcının tüm
                    // jetonları kullanılır.
                    //
                    // Güvenli, çünkü tüm push çağrı yerleri (OneriService) zaten
                    // tam olarak `user.FcmToken` geçiyor. Ayrıca gizli bir hatayı
                    // da kapatıyor: FcmToken null geçildiğinde Token'ı null olan
                    // bir Message satırı üretiliyordu.
                    //
                    // SMS'te `token` bir TELEFON NUMARASIDIR; olduğu gibi kullanılır.
                    var kullanici = await _context.Users.FindAsync(userId);
                    if (kullanici is null)
                    {
                        _logger.LogWarning("Bildirim için kullanıcı bulunamadı: {UserId}", userId);
                        return;
                    }

                    foreach (var jeton in PushHedefleri(kullanici))
                    {
                        await _context.Messages.AddAsync(
                            BuildMessage(userId, jeton, title, content, type, notifyTip, data));
                    }
                }
                else
                {
                    // SMS'te `token` telefon numarasıdır ve `messages.token`
                    // NOT NULL.
                    //
                    // Telefonu olmayan bir kullanıcı için satır eklemek
                    // `SaveChangesAsync`'i 23502 ile düşürüyordu. Buradaki
                    // `catch` hatayı yutuyor ama BOZUK VARLIK bağlamda izlenir
                    // hâlde kalıyor ve ÇAĞIRANIN `SaveChangesAsync`'i patlıyordu
                    // — "birimlere SMS gönder" isteği, birimdeki tek bir
                    // telefonsuz kullanıcı yüzünden 500 dönüyordu. Satırı hiç
                    // eklememek doğru davranış: numarası olanlara SMS gider.
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        _logger.LogWarning(
                            "SMS atlandı, kullanıcının telefon numarası yok: {UserId}", userId);
                        return;
                    }

                    await _context.Messages.AddAsync(
                        BuildMessage(userId, token, title, content, type, notifyTip, data));
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mesaj oluşturulurken hata oluştu."+ex.Message);
            }
        }

        public async Task CreateForAllPersonAsync(long departmentId, string title, string content, SendMessageType type, NotifikasyonTip notifyTip, string? data)
        {
            _logger.LogError("Birim Id: " + departmentId);
            if (departmentId==0)
            {
                return;
            }
            var users = await _context.Users.Where(x => x.BirimId == departmentId).ToListAsync();
            _logger.LogInformation("Toplam kullanıcı sayısı: " + users.Count);
            foreach (var user in users)
            {
                if (!await _userService.HasReceiveNotification(user.Id, notifyTip))
                {
                    _logger.LogWarning("Kullanıcı bildirim almayı reddetti.");
                    continue;
                }
                if (type == SendMessageType.PushNotification)
                {
                    // Her dolu jeton için AYRI satır: mobil ve web ayrı ayrı
                    // gönderilir, her birinin kendi yeniden deneme durumu olur.
                    foreach (var jeton in PushHedefleri(user))
                    {
                        _logger.LogInformation("Kullanıcıya FCM bildirim gönderiliyor: " + user.Id);
                        var message = BuildMessage(user.Id, jeton, title, content, type, notifyTip, data);
                        await _context.Messages.AddAsync(message);
                    }
                } else if (type == SendMessageType.SMS && user.PhoneNumber!=null)
                {
                    _logger.LogInformation("Kullanıcıya SMS gönderiliyor: " + user.Id);
                    var message = BuildMessage(user.Id, user.PhoneNumber??"", title, content, type,notifyTip, data);
                    await _context.Messages.AddAsync(message);
                }
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// BELİRLİ kullanıcılara bildirim üretir — gizli etkinlikler için.
        ///
        /// Gövde <see cref="CreateForAllPersonAsync"/> ile birebir aynıdır; tek fark
        /// alıcı kümesinin birim değil, verilen kullanıcı kimlikleri olması.
        /// Bildirim tercihi (HasReceiveNotification) ve FCM/SMS seçimi aynen korunur.
        /// </summary>
        public async Task CreateForUsersAsync(IEnumerable<long> userIds, string title, string content, SendMessageType type, NotifikasyonTip notifyTip, string? data)
        {
            var idListe = userIds?.Distinct().ToList() ?? [];
            if (idListe.Count == 0)
            {
                _logger.LogInformation("Gizli etkinlik bildirimi: alıcı listesi boş.");
                return;
            }

            var users = await _context.Users.Where(x => idListe.Contains(x.Id)).ToListAsync();
            _logger.LogInformation("Gizli etkinlik bildirimi — alıcı sayısı: " + users.Count);
            foreach (var user in users)
            {
                if (!await _userService.HasReceiveNotification(user.Id, notifyTip))
                {
                    _logger.LogWarning("Kullanıcı bildirim almayı reddetti.");
                    continue;
                }
                if (type == SendMessageType.PushNotification)
                {
                    // Her dolu jeton için AYRI satır (mobil + web).
                    foreach (var jeton in PushHedefleri(user))
                    {
                        await _context.Messages.AddAsync(
                            BuildMessage(user.Id, jeton, title, content, type, notifyTip, data));
                    }
                }
                else if (type == SendMessageType.SMS && user.PhoneNumber != null)
                {
                    var message = BuildMessage(user.Id, user.PhoneNumber ?? "", title, content, type, notifyTip, data);
                    await _context.Messages.AddAsync(message);
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(x => x.Id == id);
            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Message> GetAsync(long id)
        {
            var message = await _context.Messages.FirstOrDefaultAsync(x => x.Id == id);
            return message;
        }

        public async Task<IEnumerable<Message>> GetWaitingMessagesAsync()
        {
            var messages = await _context.Messages.Where(x => x.IsSuccess == false).ToListAsync();
            return messages;
        }

        public Task UpdateAsync(Message message)
        {
            var entity = _context.Messages.Update(message);
            entity.State = EntityState.Modified;
            return _context.SaveChangesAsync();

        }
    }
}
