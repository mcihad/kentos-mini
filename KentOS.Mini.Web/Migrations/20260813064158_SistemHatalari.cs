using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class SistemHatalari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sistem_hatalari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    parmakizi = table.Column<string>(type: "text", nullable: false),
                    ilk_gorulme = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    son_gorulme = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    adet = table.Column<int>(type: "integer", nullable: false),
                    tur = table.Column<string>(type: "text", nullable: false),
                    mesaj = table.Column<string>(type: "text", nullable: false),
                    ic_mesaj = table.Column<string>(type: "text", nullable: true),
                    yigin_izi = table.Column<string>(type: "text", nullable: true),
                    dosya = table.Column<string>(type: "text", nullable: true),
                    satir = table.Column<int>(type: "integer", nullable: true),
                    durum_kodu = table.Column<int>(type: "integer", nullable: false),
                    yol = table.Column<string>(type: "text", nullable: true),
                    yontem = table.Column<string>(type: "text", nullable: true),
                    sorgu_dizesi = table.Column<string>(type: "text", nullable: true),
                    govde = table.Column<string>(type: "text", nullable: true),
                    basliklar = table.Column<string>(type: "text", nullable: true),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanici_adi = table.Column<string>(type: "text", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    ip_adresi = table.Column<string>(type: "text", nullable: true),
                    istemci = table.Column<string>(type: "text", nullable: true),
                    iz_kimligi = table.Column<string>(type: "text", nullable: true),
                    cozuldu = table.Column<bool>(type: "boolean", nullable: false),
                    cozulme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cozen_kullanici = table.Column<string>(type: "text", nullable: true),
                    notlar = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sistem_hatalari", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sistem_hatalari_cozuldu_son_gorulme",
                table: "sistem_hatalari",
                columns: new[] { "cozuldu", "son_gorulme" });

            migrationBuilder.CreateIndex(
                name: "ix_sistem_hatalari_parmakizi",
                table: "sistem_hatalari",
                column: "parmakizi",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sistem_hatalari");
        }
    }
}
