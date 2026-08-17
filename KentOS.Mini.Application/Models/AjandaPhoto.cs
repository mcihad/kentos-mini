using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Mini.Application.Models
{
    [Table("ajanda_photos")]
    public class AjandaPhoto
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("filename")]
        public string Filename { get; set; }
        [Column("content_type")]
        public string ContentType { get; set; }
        [Column("ajanda_id")]
        public long AjandaId { get; set; }

        public Ajanda Ajanda { get; set; }
    }
}
