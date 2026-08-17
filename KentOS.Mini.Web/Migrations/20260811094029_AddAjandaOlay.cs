using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAjandaOlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ajanda_olaylar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: false),
                    tip = table.Column<int>(type: "integer", nullable: false),
                    kullanici = table.Column<string>(type: "varchar(150)", nullable: false),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    aciklama = table.Column<string>(type: "varchar(500)", nullable: false),
                    degisiklikler_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_olaylar", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajanda_olaylar_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_olaylar_ajanda_id",
                table: "ajanda_olaylar",
                column: "ajanda_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ajanda_olaylar");
        }
    }
}
