using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class OpenIdAyarlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "openid_ayarlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    etkin = table.Column<bool>(type: "boolean", nullable: false),
                    gorunen_ad = table.Column<string>(type: "text", nullable: true),
                    yetkili = table.Column<string>(type: "text", nullable: true),
                    istemci_id = table.Column<string>(type: "text", nullable: true),
                    istemci_sirri = table.Column<string>(type: "text", nullable: true),
                    kapsamlar = table.Column<string>(type: "text", nullable: true),
                    kullanici_adi_talebi = table.Column<string>(type: "text", nullable: true),
                    otomatik_kullanici_olustur = table.Column<bool>(type: "boolean", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_openid_ayarlari", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "openid_ayarlari");
        }
    }
}
