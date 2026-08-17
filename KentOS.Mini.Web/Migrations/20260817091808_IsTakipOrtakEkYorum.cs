using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class IsTakipOrtakEkYorum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "is_ekleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    varlik_turu = table.Column<int>(type: "integer", nullable: false),
                    varlik_id = table.Column<long>(type: "bigint", nullable: false),
                    ad = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    dosya_yolu = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    icerik_turu = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    boyut = table.Column<long>(type: "bigint", nullable: false),
                    resim_mi = table.Column<bool>(type: "boolean", nullable: false),
                    aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    yukleyen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_is_ekleri", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "is_yorumlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    varlik_turu = table.Column<int>(type: "integer", nullable: false),
                    varlik_id = table.Column<long>(type: "bigint", nullable: false),
                    ust_yorum_id = table.Column<long>(type: "bigint", nullable: true),
                    metin = table.Column<string>(type: "text", nullable: false),
                    yazan_id = table.Column<long>(type: "bigint", nullable: true),
                    yazan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    silindi = table.Column<bool>(type: "boolean", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_is_yorumlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_is_yorumlari_is_yorumlari_ust_yorum_id",
                        column: x => x.ust_yorum_id,
                        principalTable: "is_yorumlari",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_is_ekleri_varlik_turu_varlik_id",
                table: "is_ekleri",
                columns: new[] { "varlik_turu", "varlik_id" });

            migrationBuilder.CreateIndex(
                name: "ix_is_yorumlari_ust_yorum_id",
                table: "is_yorumlari",
                column: "ust_yorum_id");

            migrationBuilder.CreateIndex(
                name: "ix_is_yorumlari_varlik_turu_varlik_id",
                table: "is_yorumlari",
                columns: new[] { "varlik_turu", "varlik_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "is_ekleri");

            migrationBuilder.DropTable(
                name: "is_yorumlari");
        }
    }
}
