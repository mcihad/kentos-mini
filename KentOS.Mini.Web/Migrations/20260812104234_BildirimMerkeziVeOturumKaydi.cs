using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class BildirimMerkeziVeOturumKaydi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "okundu",
                table: "messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "okunma_tarihi",
                table: "messages",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "oturum_kayitlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanici_adi = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    olay = table.Column<int>(type: "integer", nullable: false),
                    basarili = table.Column<bool>(type: "boolean", nullable: false),
                    aciklama = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ip_adresi = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    istemci = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oturum_kayitlari", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_messages_user_id_okundu_created_at",
                table: "messages",
                columns: new[] { "user_id", "okundu", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_oturum_kayitlari_kullanici_id_tarih",
                table: "oturum_kayitlari",
                columns: new[] { "kullanici_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "ix_oturum_kayitlari_tarih",
                table: "oturum_kayitlari",
                column: "tarih");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oturum_kayitlari");

            migrationBuilder.DropIndex(
                name: "ix_messages_user_id_okundu_created_at",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "okundu",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "okunma_tarihi",
                table: "messages");
        }
    }
}
