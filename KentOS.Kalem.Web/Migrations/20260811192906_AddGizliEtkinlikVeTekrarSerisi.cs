using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGizliEtkinlikVeTekrarSerisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "gizli",
                table: "ajandalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "seri_ayrik",
                table: "ajandalar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "seri_id",
                table: "ajandalar",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "seri_orijinal_baslangic",
                table: "ajandalar",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ajanda_katilimcilar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: false),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_katilimcilar", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajanda_katilimcilar_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ajanda_katilimcilar_asp_net_users_kullanici_id",
                        column: x => x.kullanici_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ajanda_seriler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    rrule = table.Column<string>(type: "varchar(500)", nullable: false),
                    dtstart = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    sure_dakika = table.Column<int>(type: "integer", nullable: false),
                    bitis_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tekrar_sayisi = table.Column<int>(type: "integer", nullable: true),
                    uretilen_son_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanici_id = table.Column<string>(type: "varchar(150)", nullable: true),
                    iptal = table.Column<bool>(type: "boolean", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_seriler", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ajandalar_birim_id_baslangic_tarihi",
                table: "ajandalar",
                columns: new[] { "birim_id", "baslangic_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_ajandalar_seri_id",
                table: "ajandalar",
                column: "seri_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_katilimcilar_ajanda_id_kullanici_id",
                table: "ajanda_katilimcilar",
                columns: new[] { "ajanda_id", "kullanici_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_katilimcilar_kullanici_id",
                table: "ajanda_katilimcilar",
                column: "kullanici_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_seriler_birim_id",
                table: "ajanda_seriler",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_seriler_iptal_uretilen_son_tarih",
                table: "ajanda_seriler",
                columns: new[] { "iptal", "uretilen_son_tarih" });

            migrationBuilder.AddForeignKey(
                name: "fk_ajandalar_ajanda_seriler_seri_id",
                table: "ajandalar",
                column: "seri_id",
                principalTable: "ajanda_seriler",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ajandalar_ajanda_seriler_seri_id",
                table: "ajandalar");

            migrationBuilder.DropTable(
                name: "ajanda_katilimcilar");

            migrationBuilder.DropTable(
                name: "ajanda_seriler");

            migrationBuilder.DropIndex(
                name: "ix_ajandalar_birim_id_baslangic_tarihi",
                table: "ajandalar");

            migrationBuilder.DropIndex(
                name: "ix_ajandalar_seri_id",
                table: "ajandalar");

            migrationBuilder.DropColumn(
                name: "gizli",
                table: "ajandalar");

            migrationBuilder.DropColumn(
                name: "seri_ayrik",
                table: "ajandalar");

            migrationBuilder.DropColumn(
                name: "seri_id",
                table: "ajandalar");

            migrationBuilder.DropColumn(
                name: "seri_orijinal_baslangic",
                table: "ajandalar");
        }
    }
}
