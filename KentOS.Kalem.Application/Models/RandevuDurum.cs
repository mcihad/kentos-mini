using KentOS.Kalem.Application.Dto;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models;

[Table("randevu_durumlar")]
public class RandevuDurum
{
    [Column("id")]
    public long Id { get; set; }

    [Column("durum_ad", TypeName = "varchar(50)")]
    [Display(Name = "Durum Adı")]
    [Required(ErrorMessage = "Durum adı boş bırakılamaz")]
    public string DurumAd { get; set; }

    [Column("renk", TypeName = "varchar(50)")]
    [Display(Name = "Renk")]
    [Required(ErrorMessage = "Renk alanı boş bırakılamaz")]
    public string Renk { get; set; }
    [Column("simge", TypeName = "varchar(50)")]
    [Display(Name = "Simge")]
    public string? Simge { get; set; }
    [Column("aciklama", TypeName = "varchar(255)")]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    public ICollection<Randevu>? Randevular { get; set; }

}