using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;

namespace KentOS.Kalem.Application.Services
{
    public interface IMessageService
    {
        /// <param name="hassas">
        /// İçerik gönderildikten sonra SİLİNSİN mi. Parola SMS'i gibi tek
        /// kullanımlık sırlar için; varsayılan <c>false</c>.
        /// </param>
        Task CreateAsync(long userId, string token, string title, string content, SendMessageType type,NotifikasyonTip tip, string? data, bool hassas = false);
        Message BuildMessage(long userId, string token, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data);
        Task CreateForAllPersonAsync(long departmentId, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data);

        /// <summary>
        /// Bildirimi yalnızca VERİLEN kullanıcılara üretir (gizli etkinlikler).
        /// Birim bazlı gönderimin (<see cref="CreateForAllPersonAsync"/>) alıcı
        /// kümesi değiştirilmiş hâlidir; tercih/kanal mantığı aynıdır.
        /// </summary>
        Task CreateForUsersAsync(IEnumerable<long> userIds, string title, string content, SendMessageType type, NotifikasyonTip tip, string? data);
        Task<Message> GetAsync(long id);
        Task DeleteAsync(long id);

        Task UpdateAsync(Message message);
        Task<IEnumerable<Message>> GetWaitingMessagesAsync();
    }
}
