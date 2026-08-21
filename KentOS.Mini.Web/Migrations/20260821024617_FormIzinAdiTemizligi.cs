using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class FormIzinAdiTemizligi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
              YENİDEN ADLANDIRILAN İZNİN KALINTISI SİLİNİR.

              `form.yanitGor` geliştirme sırasında `form.yanitGoruntule`
              oldu (komşuları `ajanda.goruntule`, `halkgunu.goruntule`
              diyor). `IzinTohumu` katalogu her açılışta TAZELİYOR ama
              yalnızca ekliyor/güncelliyor — kodda artık olmayan bir adı
              SİLMİYOR.

              Sonucu sessiz: eski ad hiçbir yetki vermiyor (çözümleme kod
              katalogıyla kesişiyor, 7 satırdan 6'sı çözülüyor) ama rol
              yönetimi ekranında açıklanamayan bir onay kutusu olarak
              duruyor ve işaretlense bile hiçbir şey yapmıyor.

              Rol bağları ÖNCE siliniyor: yabancı anahtar sırası.
            */
            migrationBuilder.Sql(
                "DELETE FROM rol_izinleri WHERE izin_ad = 'form.yanitGor';");
            migrationBuilder.Sql(
                "DELETE FROM izinler WHERE ad = 'form.yanitGor';");


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
