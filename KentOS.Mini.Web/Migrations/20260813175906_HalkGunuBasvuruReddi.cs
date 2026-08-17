using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class HalkGunuBasvuruReddi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "red_nedeni",
                table: "halk_gunu_basvurulari",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "red_tarihi",
                table: "halk_gunu_basvurulari",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "red_nedeni",
                table: "halk_gunu_basvurulari");

            migrationBuilder.DropColumn(
                name: "red_tarihi",
                table: "halk_gunu_basvurulari");
        }
    }
}
