using KentOS.Mini.Application.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("messages")]
    public class Message
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("user_id")]
        public long UserId { get; set; }
        [Column("token")]
        public string Token { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("content")]
        public string Content { get; set; }
        [Column("data")]
        public string? Data { get; set; }
        [Column("message_type")]
        public SendMessageType MessageType { get; set; } = SendMessageType.PushNotification;
        [Column("is_success")]
        public bool IsSuccess { get; set; } = false;
        [Column("retry_count")]
        public int RetryCount { get; set; } = 0;
        [Column("fail_message")]
        public string? FailMessage { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Kullanıcı bu bildirimi uygulama içinde gördü mü.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>IsSuccess</c> ile KARIŞTIRILMAMALI: o, bildirimin cihaza
        /// GÖNDERİLDİĞİNİ söyler; bu ise kullanıcının OKUDUĞUNU. Tarayıcı
        /// bildirimleri kullanıcı silene kadar bildirim merkezinde birikiyordu;
        /// uygulama içi bildirim merkezi bu alanla temizlenebiliyor.
        /// </para>
        /// </remarks>
        [Column("okundu")]
        public bool Okundu { get; set; } = false;

        [Column("okunma_tarihi")]
        public DateTime? OkunmaTarihi { get; set; }
    }
}
