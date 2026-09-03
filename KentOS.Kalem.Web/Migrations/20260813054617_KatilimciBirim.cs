using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class KatilimciBirim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ajanda_katilimcilar_ajanda_id_kullanici_id",
                table: "ajanda_katilimcilar");

            migrationBuilder.AlterColumn<long>(
                name: "kullanici_id",
                table: "ajanda_katilimcilar",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "birim_id",
                table: "ajanda_katilimcilar",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_katilimcilar_ajanda_id_birim_id",
                table: "ajanda_katilimcilar",
                columns: new[] { "ajanda_id", "birim_id" },
                unique: true,
                filter: "birim_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_katilimcilar_birim_id",
                table: "ajanda_katilimcilar",
                column: "birim_id");

            migrationBuilder.AddForeignKey(
                name: "fk_ajanda_katilimcilar_birimler_birim_id",
                table: "ajanda_katilimcilar",
                column: "birim_id",
                principalTable: "birimler",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ajanda_katilimcilar_birimler_birim_id",
                table: "ajanda_katilimcilar");

            migrationBuilder.DropIndex(
                name: "ix_ajanda_katilimcilar_ajanda_id_birim_id",
                table: "ajanda_katilimcilar");

            migrationBuilder.DropIndex(
                name: "ix_ajanda_katilimcilar_birim_id",
                table: "ajanda_katilimcilar");

            migrationBuilder.DropColumn(
                name: "birim_id",
                table: "ajanda_katilimcilar");

            migrationBuilder.AlterColumn<long>(
                name: "kullanici_id",
                table: "ajanda_katilimcilar",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_katilimcilar_ajanda_id_kullanici_id",
                table: "ajanda_katilimcilar",
                columns: new[] { "ajanda_id", "kullanici_id" },
                unique: true);
        }
    }
}
