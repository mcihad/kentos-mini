using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Mini.Application.Models;

/// <summary>
/// İl protokol listesi kaydı.
///
/// <para>
/// Resmî törenlerde oturma düzeni ve karşılama sırası protokol kurallarına
/// göre belirlenir. Bu liste o sıralamayı ve ilgili kişilerin iletişim
/// bilgilerini tutar.
/// </para>
///
/// <para>
/// Kategori artık AYRI TABLO (<see cref="ProtokolKategori"/>). Serbest metin
/// olduğu sürece aynı kategori "Mülki İdare", "Mülki idare", "MÜLKİ İDARE"
/// diye üç ayrı grup üretiyor ve liste bölünüyordu. Kurum metin olarak
/// kaldı: her kurum için kayıt açmak, yılda birkaç kez güncellenen bir
/// listede gereksiz yük.
/// </para>
/// </summary>
[Table("protokoller")]
public class Protokol
{
    [Column("id")]
    public long Id { get; set; }

    /// <summary>Protokol kategorisi (<see cref="ProtokolKategori"/>).</summary>
    [Column("kategori_id")]
    public long KategoriId { get; set; }

    public ProtokolKategori? Kategori { get; set; }

    /// <summary>Kurum adı — "İl Valiliği", "… Üniversitesi" gibi.</summary>
    [Column("kurum")]
    public string? Kurum { get; set; }

    [Column("ad_soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    [Column("unvan")]
    public string? Unvan { get; set; }

    /// <summary>
    /// Protokol sırası — küçük olan önce gelir.
    /// </summary>
    /// <remarks>
    /// Aynı numaranın birden fazla kayıtta olması SERBEST: protokolde eş
    /// sıralı makamlar vardır (örneğin aynı seviyedeki müdürlükler).
    /// Eşitlikte kategori ve ad alfabetik olarak ayırır.
    /// </remarks>
    [Column("sira_no")]
    public int SiraNo { get; set; }

    [Column("telefon")]
    public string? Telefon { get; set; }
    [Column("cep_telefon")]
    public string? CepTelefon { get; set; }
    [Column("eposta")]
    public string? Eposta { get; set; }
    [Column("adres")]
    public string? Adres { get; set; }
    [Column("aciklama")]
    public string? Aciklama { get; set; }

    /// <summary>Görevden ayrılanlar silinmez, pasife çekilir — geçmiş kayıtlar korunur.</summary>
    [Column("aktif")]
    public bool Aktif { get; set; } = true;

    [Column("olusturma_tarihi")]
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    [Column("guncelleme_tarihi")]
    public DateTime? GuncellemeTarihi { get; set; }
}
