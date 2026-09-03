using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.Analiz
{
    /// <summary>
    /// Talep panosunun üst kısmındaki tek sayı kutucukları.
    /// </summary>
    /// <remarks>
    /// Ad <c>TalepIstatistikOzetDto</c>: <c>Dto.V2.Talep</c> altında zaten bir
    /// <c>TalepOzetDto</c> var (liste satırı) ve Swagger şema kimliklerini KISA
    /// tip adından ürettiği için ikisi çakışıp <c>/swagger/v2/swagger.json</c>
    /// tamamen 500 dönüyordu — yani tek bir isim, bütün tip üretimini durdurdu.
    /// </remarks>
    public class TalepIstatistikOzetDto
    {
        [JsonPropertyName("toplamTalep")]
        public int ToplamTalep { get; set; }
        [JsonPropertyName("aktifTalep")]
        public int AktifTalep { get; set; }
        [JsonPropertyName("arsivlenmisTalep")]
        public int ArsivlenmisTalep { get; set; }

        /// <summary>Ajandaya eklenmiş talepler.</summary>
        [JsonPropertyName("ajandayaEklenen")]
        public int AjandayaEklenen { get; set; }

        /// <summary>
        /// Onaylandığı hâlde ajandaya HENÜZ eklenmemiş talepler.
        /// </summary>
        /// <remarks>
        /// Panodaki en işe yarar sayı: sırada bekleyen iş. "Başkan onaylar,
        /// personel ekler" ayrımı yüzünden bu kümenin büyümesi bir tıkanmaya
        /// işaret ediyor.
        /// </remarks>
        [JsonPropertyName("onayliAmaEklenmemis")]
        public int OnayliAmaEklenmemis { get; set; }

        [JsonPropertyName("ozgecmisYuklu")]
        public int OzgecmisYuklu { get; set; }
        [JsonPropertyName("dosyaEkliTalep")]
        public int DosyaEkliTalep { get; set; }
        [JsonPropertyName("bugunGelen")]
        public int BugunGelen { get; set; }
        [JsonPropertyName("buHaftaGelen")]
        public int BuHaftaGelen { get; set; }
        [JsonPropertyName("buAyGelen")]
        public int BuAyGelen { get; set; }

        /// <summary>Talebin girilmesinden ajandaya eklenmesine kadar geçen ortalama gün.</summary>
        [JsonPropertyName("ortalamaAjandaGunu")]
        public double OrtalamaAjandaGunu { get; set; }
    }

    /// <summary>
    /// Talep (randevu) istatistik panosu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Etkinlik panosundan AYRI: ikisi farklı soruları cevaplıyor. Etkinlik
    /// panosu "makamın günü nasıl geçiyor" der; bu ise "vatandaş neyi, nereden,
    /// kim aracılığıyla istiyor" der. Tek DTO'ya sıkıştırmak, iki ekranın da
    /// birbirinin alanlarını taşıması demekti.
    /// </para>
    /// <para>
    /// <b>MAHALLE ve MESLEK</b> bu panonun asıl sebebi: talebin nereden ve
    /// kimden geldiğini gösteren tek iki alan bunlar ve hiçbir yerde
    /// toplanmıyorlardı.
    /// </para>
    /// </remarks>
    public class TalepIstatistikDto
    {
        [JsonPropertyName("birimAdi")]
        public string BirimAdi { get; set; } = string.Empty;
        [JsonPropertyName("baslangicTarihi")]
        public string BaslangicTarihi { get; set; } = string.Empty;
        [JsonPropertyName("bitisTarihi")]
        public string BitisTarihi { get; set; } = string.Empty;
        [JsonPropertyName("uretilmeZamani")]
        public string UretilmeZamani { get; set; } = string.Empty;

        [JsonPropertyName("ozet")]
        public TalepIstatistikOzetDto Ozet { get; set; } = new();

        /// <summary>Aylara göre talep sayısı (zaman serisi).</summary>
        [JsonPropertyName("aylaraGore")]
        public List<IstatistikSeriNoktasiDto> AylaraGore { get; set; } = new();

        /// <summary>Günlük yoğunluk (ısı haritası / sütun serisi).</summary>
        [JsonPropertyName("gunlukYogunluk")]
        public List<IstatistikSeriNoktasiDto> GunlukYogunluk { get; set; } = new();

        /// <summary>
        /// <b>Mahalleye göre</b> — talep nereden geliyor.
        /// </summary>
        [JsonPropertyName("mahalleyeGore")]
        public List<IstatistikDilimDto> MahalleyeGore { get; set; } = new();

        /// <summary>
        /// <b>Mesleğe göre</b> — talep kimden geliyor.
        /// </summary>
        /// <remarks>
        /// Meslek serbest metin bir sütun (referans tabloya bağlı değil);
        /// büyük/küçük harf ve baş/son boşluk farkları tek başlık altında
        /// toplanır, yoksa "Çiftçi" ile "çiftçi " iki ayrı dilim olurdu.
        /// </remarks>
        [JsonPropertyName("meslegeGore")]
        public List<IstatistikDilimDto> MeslegeGore { get; set; } = new();

        [JsonPropertyName("tipeGore")]
        public List<IstatistikDilimDto> TipeGore { get; set; } = new();
        [JsonPropertyName("durumaGore")]
        public List<IstatistikDilimDto> DurumaGore { get; set; } = new();

        /// <summary>Talebi giren birim.</summary>
        [JsonPropertyName("birimeGore")]
        public List<IstatistikDilimDto> BirimeGore { get; set; } = new();

        /// <summary>Talebi kaydeden kullanıcı.</summary>
        [JsonPropertyName("olusturanaGore")]
        public List<IstatistikDilimDto> OlusturanaGore { get; set; } = new();

        /// <summary>Haftanın günü — hangi gün daha yoğun.</summary>
        [JsonPropertyName("haftaGunineGore")]
        public List<IstatistikDilimDto> HaftaGunineGore { get; set; } = new();

        /// <summary>Ajandaya eklendi / eklenmedi.</summary>
        [JsonPropertyName("ajandaDurumu")]
        public List<IstatistikDilimDto> AjandaDurumu { get; set; } = new();

        /// <summary>Özgeçmiş yüklü / yüklü değil.</summary>
        [JsonPropertyName("ozgecmisDurumu")]
        public List<IstatistikDilimDto> OzgecmisDurumu { get; set; } = new();

        /// <summary>Aktif / arşivlenmiş.</summary>
        [JsonPropertyName("arsivDurumu")]
        public List<IstatistikDilimDto> ArsivDurumu { get; set; } = new();
    }
}
