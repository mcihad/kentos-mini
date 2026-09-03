using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class BirimGelenKutusu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "birim_gelen_kutusu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    hedef_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    kaynak_gorev_id = table.Column<long>(type: "bigint", nullable: false),
                    kaynak_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    gorev_tipi_devir_id = table.Column<long>(type: "bigint", nullable: true),
                    hedef_gorev_tipi_id = table.Column<long>(type: "bigint", nullable: true),
                    konu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    is_talebi = table.Column<bool>(type: "boolean", nullable: false),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    gorev_id = table.Column<long>(type: "bigint", nullable: true),
                    gerekce = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    isleyen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    islem_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    enlem = table.Column<double>(type: "double precision", nullable: true),
                    boylam = table.Column<double>(type: "double precision", nullable: true),
                    adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_birim_gelen_kutusu", x => x.id);
                    table.ForeignKey(
                        name: "fk_birim_gelen_kutusu_birimler_hedef_birim_id",
                        column: x => x.hedef_birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_birim_gelen_kutusu_hedef_birim_id_durum_olusturma_tarihi",
                table: "birim_gelen_kutusu",
                columns: new[] { "hedef_birim_id", "durum", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_birim_gelen_kutusu_kaynak_gorev_id_hedef_birim_id",
                table: "birim_gelen_kutusu",
                columns: new[] { "kaynak_gorev_id", "hedef_birim_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "birim_gelen_kutusu");
        }
    }
}
