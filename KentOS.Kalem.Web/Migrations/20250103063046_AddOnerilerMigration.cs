using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOnerilerMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oneriler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    baslik = table.Column<string>(type: "text", nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    tip = table.Column<int>(type: "integer", nullable: false),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanici_adi = table.Column<string>(type: "text", nullable: true),
                    cevap = table.Column<string>(type: "text", nullable: true),
                    cevap_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oneriler", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oneriler");
        }
    }
}
