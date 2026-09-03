using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class BaslikMetin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "baslik",
                table: "ajandalar",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "baslik",
                table: "ajandalar",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
