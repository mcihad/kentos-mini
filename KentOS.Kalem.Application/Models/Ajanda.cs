using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models
{
    [Table("ajandalar")]
    public class Ajanda
    {
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// Etkinlik başlığı — <b>uzunluk sınırı YOK</b>.
        /// </summary>
        /// <remarks>
        /// Kolon <c>varchar(100)</c> idi ve sahada yetmiyordu: uzun başlıklar
        /// ya kesiliyor ya da kullanıcı formda yazamıyordu. Üretimde
        /// <c>ALTER</c> ile <c>text</c>e çevrildi; model, DTO ve iki istemci
        /// de bu değişikliğe uyduruldu. Sınır tek katmanda kalsaydı (ör.
        /// yalnızca formda) kullanıcı yine 100 karakterde durdurulurdu.
        /// </remarks>
        [Display(Name = "Başlık")]
        [Required(ErrorMessage = "Başlık alanı gereklidir.")]
        [Column("baslik", TypeName = "text")]
        public string Baslik { get; set; }
        [Column("aciklama", TypeName = "text")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Display(Name = "Konum")]
        [Column("konum", TypeName = "varchar(100)")]
        public string? Konum { get; set; }

        [Display(Name = "Koordinat")]
        [Column("koordinat", TypeName = "varchar(100)")]
        public string? Koordinat { get; set; }

        [Display(Name = "Başlangıç Tarihi")]
        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        [Column("baslangic_tarihi")]
        public DateTime BaslangicTarihi { get; set; } = DateTime.Now;

        [Display(Name = "Bitiş Tarihi")]
        [Column("bitis_tarihi")]
        public DateTime? BitisTarihi { get; set; }

        [Display(Name = "Tüm Gün")]
        [Column("tum_gun")]
        public bool TumGun { get; set; }
        [Display(Name = "İrtibat Kişi")]
        [Column("irtibat_kisi")]

        public string?  IrtibatKisi { get; set; }
        [Display(Name = "İrtibat Telefon")]
        [Column("irtibat_telefon")]
        public string? IrtibatTelefon { get; set; }
        [Display(Name = "Basın Katılsın?")]
        [Column("basin_katilsin")]
        public bool BasinKatilsin { get; set; }
        [Display(Name = "Konuşma Metni")]
        [Column("konusma_metni_durum")]
        public bool KonusmaMetniDurum { get; set; }
        [Display(Name = "Bilgi Notu")]
        [Column("bilgi_notu_durum")]
        public bool BilgiNotuDurum { get; set; }
        [Display(Name = "Resim Var")]
        [Column("resim_var")]
        public bool ResimVar { get; set; }

        [Display(Name = "Tekrar Eden")]
        [Column("tekrar_eden")]
        public bool TekrarEden { get; set; }
        [Display(Name = "Bilgi Notu")]
        [Column("bilgi_notu")]
        public string? BilgiNotu { get; set; }
        [Display(Name = "Konuşma Metni")]
        [Column("konusma_metni")]
        public string? KonusmaMetni { get; set; }

        [Display(Name = "Oluşturma Tarihi")]
        [Column("olusturma_tarihi")]
        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

        [Display(Name = "Güncelleme Tarihi")]
        [Column("guncelleme_tarihi")]
        public DateTime? GuncellemeTarihi { get; set; }

        [Display(Name = "Kullanıcı Id")]
        [Column("kullanici_id")]
        public string? KullaniciId { get; set; }

        [Display(Name = "Birim Id")]
        [Column("birim_id")]
        public long? BirimId { get; set; }
        [Display(Name = "Birim")]
        public Birim? Birim { get; set; }
        [Column("randevu_id")]
        public long RandevuId { get; set; } = 0;

        [Display(Name = "Tekrar Detayları")]
        public AjandaTekrar? TekrarDetaylari { get; set; }
        [Display(Name = "Randevu Tipi")]
        public RandevuTip? RandevuTip { get; set; }
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
        [Required(ErrorMessage = "Bu alan zorunludur")]
        [Column("randevu_tip_id")]

        public long? RandevuTipId { get; set; }
        [Display(Name = "Durum")]
        public AjandaDurum? Durum { get; set; }
        [Required(ErrorMessage="Bu alan zorunludur")]
        [Column("durum_id")]
        public long? DurumId { get; set; }
        [Column("status")]
        public AjandaStatus Status { get; set; } = AjandaStatus.Pending;
        public virtual ICollection<AjandaPhoto> Photos { get; set; } = new List<AjandaPhoto>();
        public virtual ICollection<AjandaNot> AjandaNotlar { get; set; } = new List<AjandaNot>();

        // ---------------------------------------------------------------- gizlilik
        /// <summary>
        /// GİZLİ etkinlik: yalnızca ekleyen kişi ve <see cref="Katilimcilar"/>
        /// görebilir; bildirimler de yalnızca onlara gider. Varsayılan false
        /// olduğu için mevcut kayıtların davranışı değişmez.
        /// </summary>
        [Display(Name = "Gizli")]
        [Column("gizli")]
        public bool Gizli { get; set; }

        /// <summary>Gizli etkinliği görebilecek kişiler (gizli değilse boştur).</summary>
        public virtual ICollection<AjandaKatilimci> Katilimcilar { get; set; } = new List<AjandaKatilimci>();

        // ------------------------------------------------------------------ tekrar
        /// <summary>
        /// Bu satır bir tekrar serisinden üretildiyse serinin kimliği.
        /// Null ise tek seferlik etkinlik.
        /// </summary>
        [Column("seri_id")]
        public long? SeriId { get; set; }
        public AjandaSeri? Seri { get; set; }

        /// <summary>
        /// RECURRENCE-ID: tekrarın kural gereği hesaplanan ÖZGÜN başlangıcı.
        /// Kullanıcı saati değiştirse bile bu değer sabit kalır; seri
        /// güncellemelerinde/genişletmelerinde eşleştirme anahtarıdır.
        /// </summary>
        [Column("seri_orijinal_baslangic")]
        public DateTime? SeriOrijinalBaslangic { get; set; }

        /// <summary>
        /// Bu tekrar bireysel olarak düzenlendi (istisna). Seri genelinde yapılan
        /// "tümü" kapsamlı güncellemeler bu satırı ATLAR.
        /// </summary>
        [Column("seri_ayrik")]
        public bool SeriAyrik { get; set; }
        //has one cicek
        public virtual Cicek? Cicek { get; set; }
        [Column("cicek_id")]
        public long? CicekId { get; set; }

        [NotMapped]
        public bool HasCicek => CicekId != null;
        [NotMapped]
        public bool HasNoCicek => CicekId == null && Status != AjandaStatus.Canceled;
        [NotMapped]
        public bool HasPhotos => Photos.Count > 0;
        [NotMapped]
        public bool HasNoPhotos => Photos.Count == 0;
        [NotMapped]
        public bool IsCurrent => BaslangicTarihi <= DateTime.Now && BitisTarihi >= DateTime.Now;

    }
}