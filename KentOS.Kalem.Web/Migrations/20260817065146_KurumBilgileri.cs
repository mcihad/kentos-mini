using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class KurumBilgileri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kurum_bilgileri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kisa_ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gorunen_ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    birim = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kunye = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    web_sitesi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    telefon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    eposta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    uygulama_adi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    uygulama_kisa_adi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uygulama_aciklamasi = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    marka_birincil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    marka_vurgu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    marka_notr = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    marka_birincil_koyu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    amblem = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    favicon = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    uygulama_ikonu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cikti_amblemi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    guncelleyen = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kurum_bilgileri", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kurum_bilgileri");
        }
    }
}
