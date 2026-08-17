using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class EtkinlikSahibiOnarimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── SAHİPSİZ KALAN ETKİNLİKLERİ ONAR ───────────────────────────
            //
            // `AjandaService.UpdateAsync` içindeki `_mapper.Map(dto, entity)`
            // gövdede olmayan alanları da yazıyordu ve istemciler
            // `kullaniciId` göndermediği için HER GÜNCELLEME etkinliğin
            // sahibini NULL'a çekiyordu.
            //
            // Sonucu ölümcül: gizli etkinliğin görünürlüğü "oluşturan"
            // eşleşmesine bakıyor; sahip silinmiş bir kayıt gizliye
            // çevrildiğinde OLUŞTURANIN DA gözünden kayboluyor, detay 404
            // veriyordu. Ortada istisna olmadığı için sistem hatası da
            // düşmüyordu.
            //
            // Sahip, olay günlüğünden geri getirilir: `ajanda_olaylar` "oluşturuldu"
            // kaydında kullanıcının TAM ADI duruyor; kullanıcı adına o ad
            // üzerinden dönülür.
            migrationBuilder.Sql(@"
                UPDATE ajandalar a
                SET kullanici_id = k.user_name
                FROM (
                    SELECT DISTINCT ON (o.ajanda_id) o.ajanda_id, u.user_name
                    FROM ajanda_olaylar o
                    JOIN ""AspNetUsers"" u
                      ON btrim(coalesce(u.ad, '') || ' ' || coalesce(u.soyad, '')) = btrim(o.kullanici)
                    WHERE o.tip = 0
                    ORDER BY o.ajanda_id, o.tarih
                ) k
                WHERE k.ajanda_id = a.id
                  AND (a.kullanici_id IS NULL OR btrim(a.kullanici_id) = '');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
