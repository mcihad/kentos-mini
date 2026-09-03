using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KentOS.Kalem.Application.Dto.Analiz
{
    /// <summary>Tek bir etiket/değer çifti — pasta, çubuk ve liste grafiklerinde kullanılır.</summary>
    public class IstatistikDilimDto
    {
        [JsonPropertyName("etiket")]
        public string Etiket { get; set; } = string.Empty;
        [JsonPropertyName("deger")]
        public int Deger { get; set; }
        /// <summary>Toplam içindeki yüzde payı (0-100, bir ondalık).</summary>
        [JsonPropertyName("yuzde")]
        public double Yuzde { get; set; }
        /// <summary>Varsa kaynak kaydın rengi (#RRGGBB). Yoksa mobil taraf kendi paletini kullanır.</summary>
        [JsonPropertyName("renk")]
        public string? Renk { get; set; }
    }

    /// <summary>Zaman serisi noktası (ay, gün, yıl vb.).</summary>
    public class IstatistikSeriNoktasiDto
    {
        /// <summary>Grafikte gösterilecek kısa etiket, ör. "Oca 26".</summary>
        [JsonPropertyName("etiket")]
        public string Etiket { get; set; } = string.Empty;
        /// <summary>Sıralama ve gruplama için ISO tarih (yyyy-MM-dd).</summary>
        [JsonPropertyName("tarih")]
        public string Tarih { get; set; } = string.Empty;
        [JsonPropertyName("deger")]
        public int Deger { get; set; }
    }

    /// <summary>Üst kısımdaki tek sayı kutucukları.</summary>
    public class IstatistikOzetDto
    {
        [JsonPropertyName("toplamEtkinlik")]
        public int ToplamEtkinlik { get; set; }
        [JsonPropertyName("aktifEtkinlik")]
        public int AktifEtkinlik { get; set; }
        [JsonPropertyName("silinmisEtkinlik")]
        public int SilinmisEtkinlik { get; set; }
        [JsonPropertyName("tamamlananEtkinlik")]
        public int TamamlananEtkinlik { get; set; }
        [JsonPropertyName("iptalEdilenEtkinlik")]
        public int IptalEdilenEtkinlik { get; set; }
        [JsonPropertyName("bekleyenEtkinlik")]
        public int BekleyenEtkinlik { get; set; }
        [JsonPropertyName("gecmisEtkinlik")]
        public int GecmisEtkinlik { get; set; }
        [JsonPropertyName("gelecekEtkinlik")]
        public int GelecekEtkinlik { get; set; }
        [JsonPropertyName("bugunkuEtkinlik")]
        public int BugunkuEtkinlik { get; set; }
        [JsonPropertyName("buHaftaEtkinlik")]
        public int BuHaftaEtkinlik { get; set; }
        [JsonPropertyName("buAyEtkinlik")]
        public int BuAyEtkinlik { get; set; }
        /// <summary>Ortalama etkinlik süresi (dakika). Bitiş tarihi olmayanlar hariç.</summary>
        [JsonPropertyName("ortalamaSureDakika")]
        public double OrtalamaSureDakika { get; set; }
        /// <summary>Etkinlik başına ortalama not sayısı.</summary>
        [JsonPropertyName("ortalamaNotSayisi")]
        public double OrtalamaNotSayisi { get; set; }
        /// <summary>Etkinlik başına ortalama fotoğraf sayısı.</summary>
        [JsonPropertyName("ortalamaFotografSayisi")]
        public double OrtalamaFotografSayisi { get; set; }
        [JsonPropertyName("toplamNot")]
        public int ToplamNot { get; set; }
        [JsonPropertyName("toplamFotograf")]
        public int ToplamFotograf { get; set; }
        /// <summary>Tamamlanma oranı (%), tamamlanan / (tamamlanan + iptal + bekleyen).</summary>
        [JsonPropertyName("tamamlanmaOrani")]
        public double TamamlanmaOrani { get; set; }
    }

    /// <summary>
    /// Bir birimin etkinlik (Ajanda) istatistiklerinin tamamı.
    /// SALT OKUNUR — bu DTO'yu üreten uç hiçbir kaydı değiştirmez.
    /// </summary>
    public class AjandaIstatistikDto
    {
        [JsonPropertyName("birimId")]
        public long BirimId { get; set; }
        [JsonPropertyName("birimAdi")]
        public string BirimAdi { get; set; } = string.Empty;
        /// <summary>Hesaplamanın kapsadığı aralık (dahil).</summary>
        [JsonPropertyName("baslangicTarihi")]
        public string BaslangicTarihi { get; set; } = string.Empty;
        [JsonPropertyName("bitisTarihi")]
        public string BitisTarihi { get; set; } = string.Empty;
        [JsonPropertyName("uretilmeZamani")]
        public string UretilmeZamani { get; set; } = string.Empty;

        // 1
        [JsonPropertyName("ozet")]
        public IstatistikOzetDto Ozet { get; set; } = new();
        // 2  Aylara göre dağılım (aralık boyunca, boş aylar 0 ile dolu)
        [JsonPropertyName("aylaraGore")]
        public List<IstatistikSeriNoktasiDto> AylaraGore { get; set; } = new();
        // 3  Yıllara göre dağılım
        [JsonPropertyName("yillaraGore")]
        public List<IstatistikDilimDto> YillaraGore { get; set; } = new();
        // 4  Etkinlik tipine göre (RandevuTip)
        [JsonPropertyName("tipeGore")]
        public List<IstatistikDilimDto> TipeGore { get; set; } = new();
        // 5  Duruma göre (AjandaDurum — kullanıcı tanımlı durumlar)
        [JsonPropertyName("durumaGore")]
        public List<IstatistikDilimDto> DurumaGore { get; set; } = new();
        // 6  Statüye göre (Beklemede / Tamamlandı / İptal)
        [JsonPropertyName("statuyeGore")]
        public List<IstatistikDilimDto> StatuyeGore { get; set; } = new();
        // 7  Haftanın gününe göre
        [JsonPropertyName("haftaGunineGore")]
        public List<IstatistikDilimDto> HaftaGunineGore { get; set; } = new();
        // 8  Saat aralığına göre (2'şer saatlik 12 kova)
        [JsonPropertyName("saatAraliginaGore")]
        public List<IstatistikDilimDto> SaatAraliginaGore { get; set; } = new();
        // 9  Günün bölümüne göre (Sabah/Öğle/Akşam/Gece)
        [JsonPropertyName("gunBolumuneGore")]
        public List<IstatistikDilimDto> GunBolumuneGore { get; set; } = new();
        // 10 En yoğun konumlar (ilk 10)
        [JsonPropertyName("konumaGore")]
        public List<IstatistikDilimDto> KonumaGore { get; set; } = new();
        // 11 En çok etkinlik oluşturan kullanıcılar (ilk 10)
        [JsonPropertyName("olusturanaGore")]
        public List<IstatistikDilimDto> OlusturanaGore { get; set; } = new();
        // 12 Basın katılımı
        [JsonPropertyName("basinKatilimi")]
        public List<IstatistikDilimDto> BasinKatilimi { get; set; } = new();
        // 13 Fotoğraf durumu (fotoğraflı / fotoğrafsız)
        [JsonPropertyName("fotografDurumu")]
        public List<IstatistikDilimDto> FotografDurumu { get; set; } = new();
        // 14 Çiçek gönderimi
        [JsonPropertyName("cicekDurumu")]
        public List<IstatistikDilimDto> CicekDurumu { get; set; } = new();
        // 15 Tüm gün / saatli etkinlik
        [JsonPropertyName("tumGunDurumu")]
        public List<IstatistikDilimDto> TumGunDurumu { get; set; } = new();
        // 16 Tekrar eden / tek seferlik
        [JsonPropertyName("tekrarDurumu")]
        public List<IstatistikDilimDto> TekrarDurumu { get; set; } = new();
        // 17 Bilgi notu & konuşma metni hazırlık durumu
        [JsonPropertyName("hazirlikDurumu")]
        public List<IstatistikDilimDto> HazirlikDurumu { get; set; } = new();
        // 18 Günlük yoğunluk (son 90 gün — ısı haritası / sparkline)
        [JsonPropertyName("gunlukYogunluk")]
        public List<IstatistikSeriNoktasiDto> GunlukYogunluk { get; set; } = new();
        // 19 Etkinlik süresi dağılımı (dakika kovaları)
        [JsonPropertyName("sureDagilimi")]
        public List<IstatistikDilimDto> SureDagilimi { get; set; } = new();
        // 20 En çok not alan etkinlikler (ilk 10)
        [JsonPropertyName("enCokNotAlanEtkinlikler")]
        public List<IstatistikDilimDto> EnCokNotAlanEtkinlikler { get; set; } = new();
        // 21 Ay bazında tamamlanma oranı (%)
        [JsonPropertyName("aylikTamamlanmaOrani")]
        public List<IstatistikSeriNoktasiDto> AylikTamamlanmaOrani { get; set; } = new();
    }
}
