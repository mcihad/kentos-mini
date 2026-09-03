using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class CicekDogrulamaDenemesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dogrulama_denemesi",
                table: "cicekler",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dogrulama_denemesi",
                table: "cicekler");
        }
    }
}
