using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class IsTakipProjeler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projeler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    kod = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    birim_id = table.Column<long>(type: "bigint", nullable: false),
                    yonetici_id = table.Column<long>(type: "bigint", nullable: true),
                    baslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    bitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tamamlanma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    butce = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    enlem = table.Column<double>(type: "double precision", nullable: true),
                    boylam = table.Column<double>(type: "double precision", nullable: true),
                    adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    guncelleyen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projeler", x => x.id);
                    table.ForeignKey(
                        name: "fk_projeler_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kilometre_taslari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    proje_id = table.Column<long>(type: "bigint", nullable: false),
                    ad = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    hedef_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tamamlandi = table.Column<bool>(type: "boolean", nullable: false),
                    tamamlanma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sira_no = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kilometre_taslari", x => x.id);
                    table.ForeignKey(
                        name: "fk_kilometre_taslari_projeler_proje_id",
                        column: x => x.proje_id,
                        principalTable: "projeler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pano_sutunlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    proje_id = table.Column<long>(type: "bigint", nullable: false),
                    ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    gorev_durumu = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pano_sutunlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_pano_sutunlari_projeler_proje_id",
                        column: x => x.proje_id,
                        principalTable: "projeler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proje_uyeleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    proje_id = table.Column<long>(type: "bigint", nullable: false),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false),
                    rol = table.Column<int>(type: "integer", nullable: false),
                    eklenme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proje_uyeleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_proje_uyeleri_projeler_proje_id",
                        column: x => x.proje_id,
                        principalTable: "projeler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_kilometre_taslari_proje_id_sira_no",
                table: "kilometre_taslari",
                columns: new[] { "proje_id", "sira_no" });

            migrationBuilder.CreateIndex(
                name: "ix_pano_sutunlari_proje_id_sira_no",
                table: "pano_sutunlari",
                columns: new[] { "proje_id", "sira_no" });

            migrationBuilder.CreateIndex(
                name: "ix_proje_uyeleri_proje_id_kullanici_id",
                table: "proje_uyeleri",
                columns: new[] { "proje_id", "kullanici_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projeler_birim_id_durum",
                table: "projeler",
                columns: new[] { "birim_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "ix_projeler_kod",
                table: "projeler",
                column: "kod");

            // PROJE KONUMU — `gorevler` ile aynı düzen.
            //
            // Uzantı yoksa kolon HİÇ eklenmiyor ve göç yine başarılı sayılıyor:
            // PostGIS'siz bir kurulumda uygulamanın açılmaması, harita
            // dışındaki her şey çalışabilecekken kabul edilemez.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'postgis') THEN
                        ALTER TABLE projeler
                        ADD COLUMN konum geometry(Point, 4326)
                        GENERATED ALWAYS AS (
                            CASE
                                WHEN enlem IS NULL OR boylam IS NULL THEN NULL
                                ELSE ST_SetSRID(ST_MakePoint(boylam, enlem), 4326)
                            END
                        ) STORED;

                        CREATE INDEX ix_projeler_konum ON projeler USING GIST (konum);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_projeler_konum;");
            migrationBuilder.Sql("ALTER TABLE projeler DROP COLUMN IF EXISTS konum;");

            migrationBuilder.DropTable(
                name: "kilometre_taslari");

            migrationBuilder.DropTable(
                name: "pano_sutunlari");

            migrationBuilder.DropTable(
                name: "proje_uyeleri");

            migrationBuilder.DropTable(
                name: "projeler");
        }
    }
}
