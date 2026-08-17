using System.Text.Json.Serialization;

namespace KentOS.Mini.Web.Models
{
    public class SmsRequest
    {
        [JsonPropertyName("type")]
        public int Type { get; set; } = 1;
        [JsonPropertyName("sendingType")]
        public int SendingType { get; set; } = 0;
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; }
        [JsonPropertyName("number")]
        public string Number { get; set; }
        [JsonPropertyName("encoding")]
        public int Encoding { get; set; } = 0;
        [JsonPropertyName("sender")]
        public string Sender { get; set; }
        [JsonPropertyName("sendingDate")]
        public string? SendingDate { get; set; }
        [JsonPropertyName("validity")]
        public int Validity { get; set; } = 60;
        [JsonPropertyName("commercial")]
        public bool Commercial { get; set; } = false;
        [JsonPropertyName("skipAhsQuery")]
        public bool SkipAhsQuery { get; set; } = false;
        [JsonPropertyName("recipientType")]
        public int RecipientType { get; set; } = 0;
        [JsonPropertyName("customID")]
        public string CustomID { get; set; } = Guid.NewGuid().ToString();
        [JsonPropertyName("pushSettings")]
        public PushSettings? PushSettings { get; set; }
    }

    public class PushSettings
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class SmsResponse
    {
        [JsonPropertyName("data")]
        public Data? Data { get; set; }
        [JsonPropertyName("err")]
        public Error? Err { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("pkgID")]
        public int PkgID { get; set; }
    }

    public class Error
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

}
