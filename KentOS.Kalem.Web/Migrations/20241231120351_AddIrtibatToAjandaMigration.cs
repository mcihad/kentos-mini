using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIrtibatToAjandaMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "irtibat_kisi",
                table: "ajandalar",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "irtibat_telefon",
                table: "ajandalar",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "irtibat_kisi",
                table: "ajandalar");

            migrationBuilder.DropColumn(
                name: "irtibat_telefon",
                table: "ajandalar");
        }
    }
}
