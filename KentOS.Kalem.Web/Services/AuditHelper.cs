using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using KentOS.Kalem.Application.Dto;

namespace KentOS.Kalem.Web.Services
{
    /// <summary>
    /// EF Core change-tracking üzerinden, bir varlıkta değişen kullanıcı-görünür
    /// alanları "Etiket: eski → yeni" biçiminde Türkçe olarak üretir.
    /// Değişiklik geçmişi (audit) notları için kullanılır. Bu yardımcı yalnızca
    /// metin üretir; DB'ye yazmaz ve BİLDİRİM GÖNDERMEZ.
    /// </summary>
    public static class AuditHelper
    {
        /// Audit'te gösterilmeyecek teknik/metadata alanları.
        private static readonly HashSet<string> GizliAlanlar = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "GuncellemeTarih", "GuncellemeTarihi", "Guncelleyen",
            "OlusturmaTarih", "OlusturmaTarihi", "OlusturulmaTarihi",
            "BirimId", "KullaniciId", "Koordinat"
        };

        /// <summary>
        /// Verilen varlık girişindeki değişen (modified) alanları
        /// "Etiket: eski → yeni" listesi olarak döndürür. Değişiklik yoksa boş liste.
        /// SaveChanges'ten ÖNCE, alan eşlemesinden (map) SONRA çağrılmalıdır.
        /// </summary>
        public static List<string> DegisenAlanlar(EntityEntry entry)
        {
            var sonuc = new List<string>();
            if (entry.State != EntityState.Modified)
            {
                return sonuc;
            }

            foreach (var prop in entry.Properties)
            {
                if (!prop.IsModified) continue;

                var ad = prop.Metadata.Name;
                if (GizliAlanlar.Contains(ad)) continue;

                var eski = prop.OriginalValue;
                var yeni = prop.CurrentValue;
                if (Equals(eski, yeni)) continue;

                sonuc.Add($"{Etiket(entry, ad)}: {Bicimle(eski)} → {Bicimle(yeni)}");
            }

            return sonuc;
        }


        /// <summary>
        /// <see cref="DegisenAlanlar"/> ile aynı bilgiyi, metin yerine YAPISAL
        /// olarak döndürür. Zaman çizelgesi bunu kullanır: istemci "Başlık: A → B"
        /// biçimini ayrıştırmak zorunda kalmadan kendi düzenini kurabilir.
        /// </summary>
        public static List<AjandaAlanDegisikligiDto> DegisenAlanlarYapisal(EntityEntry entry)
        {
            var sonuc = new List<AjandaAlanDegisikligiDto>();
            if (entry.State != EntityState.Modified) return sonuc;

            foreach (var prop in entry.Properties)
            {
                if (!prop.IsModified) continue;

                var ad = prop.Metadata.Name;
                if (GizliAlanlar.Contains(ad)) continue;

                var eski = prop.OriginalValue;
                var yeni = prop.CurrentValue;
                if (Equals(eski, yeni)) continue;

                sonuc.Add(new AjandaAlanDegisikligiDto(
                    Etiket(entry, ad), Bicimle(eski), Bicimle(yeni)));
            }

            return sonuc;
        }

        /// [Display(Name=...)] varsa onu, yoksa özellik adını döndürür.
        private static string Etiket(EntityEntry entry, string propName)
        {
            var prop = entry.Entity.GetType().GetProperty(propName);
            var display = prop?.GetCustomAttribute<DisplayAttribute>();
            return display?.Name ?? propName;
        }

        private static string Bicimle(object? deger)
        {
            if (deger is null) return "(boş)";
            if (deger is DateTime dt) return dt.ToString("dd.MM.yyyy HH:mm");
            if (deger is bool b) return b ? "Evet" : "Hayır";
            var s = deger.ToString() ?? "";
            return string.IsNullOrWhiteSpace(s) ? "(boş)" : s;
        }
    }
}
