using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <summary>
    /// Görevin beklemeye alındığı an.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>bekleme_dakika</c> zaten vardı ama beklemenin NE ZAMAN başladığını
    /// tutan alan yoktu. Süreyi <c>guncelleme_tarihi</c>'nden hesaplamak
    /// denendi ve yanlış çıkıyor: bekleyen bir görevin başlığı
    /// düzenlendiğinde o damga da değişiyor ve bekleme olduğundan kısa
    /// ölçülüyordu. Zaman çizelgesinden okumak da olmaz — çizelge yazımı
    /// istisna yutuyor, yani hiç yazılmamış olabilir.
    /// </para>
    /// <para>
    /// Beklemeden çıkışta değer <c>bekleme_dakika</c>'ya eklenip
    /// <c>NULL</c>'a döner ve SLA bitişi aynı kadar ileri itilir: malzeme
    /// bekleyen işi "geciktirdi" diye personele yazmak ölçümü anlamsız kılar.
    /// </para>
    /// </remarks>
    public partial class GorevBeklemeBaslangic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "bekleme_baslangic",
                table: "gorevler",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bekleme_baslangic",
                table: "gorevler");
        }
    }
}
