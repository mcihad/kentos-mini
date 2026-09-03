using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace KentOS.Kalem.Application.Models
{
    public class AppRole: IdentityRole<long>
    {
        [Column("description")]
        public string? Description { get; set; }
    }
}
