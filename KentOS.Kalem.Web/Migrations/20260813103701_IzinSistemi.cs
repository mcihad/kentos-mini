using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class IzinSistemi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "izinler",
                columns: table => new
                {
                    ad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    grup = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    baslik = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    aciklama = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    kullanimda = table.Column<bool>(type: "boolean", nullable: false),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_izinler", x => x.ad);
                });

            migrationBuilder.CreateTable(
                name: "rol_izinleri",
                columns: table => new
                {
                    rol_id = table.Column<long>(type: "bigint", nullable: false),
                    izin_ad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol_izinleri", x => new { x.rol_id, x.izin_ad });
                    table.ForeignKey(
                        name: "fk_rol_izinleri_asp_net_roles_rol_id",
                        column: x => x.rol_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rol_izinleri_izinler_izin_ad",
                        column: x => x.izin_ad,
                        principalTable: "izinler",
                        principalColumn: "ad",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_izinler_grup_sira_no",
                table: "izinler",
                columns: new[] { "grup", "sira_no" });

            migrationBuilder.CreateIndex(
                name: "ix_rol_izinleri_izin_ad",
                table: "rol_izinleri",
                column: "izin_ad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rol_izinleri");

            migrationBuilder.DropTable(
                name: "izinler");
        }
    }
}
