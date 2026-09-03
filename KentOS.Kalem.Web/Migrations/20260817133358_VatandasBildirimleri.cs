using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class VatandasBildirimleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telefon_dogrulamalari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    telefon_sade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    kod_karmasi = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    gecerlilik = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    deneme = table.Column<int>(type: "integer", nullable: false),
                    dogrulandi = table.Column<bool>(type: "boolean", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telefon_dogrulamalari", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vatandas_bildirimleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    takip_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ad_soyad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    telefon_sade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    konu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: false),
                    enlem = table.Column<double>(type: "double precision", nullable: true),
                    boylam = table.Column<double>(type: "double precision", nullable: true),
                    adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mahalle_id = table.Column<long>(type: "bigint", nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    gorev_id = table.Column<long>(type: "bigint", nullable: true),
                    islem_notu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    isleyen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    islem_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vatandas_bildirimleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_vatandas_bildirimleri_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_vatandas_bildirimleri_mahalleler_mahalle_id",
                        column: x => x.mahalle_id,
                        principalTable: "mahalleler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_telefon_dogrulamalari_telefon_sade_olusturma_tarihi",
                table: "telefon_dogrulamalari",
                columns: new[] { "telefon_sade", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_vatandas_bildirimleri_birim_id",
                table: "vatandas_bildirimleri",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_vatandas_bildirimleri_durum_olusturma_tarihi",
                table: "vatandas_bildirimleri",
                columns: new[] { "durum", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_vatandas_bildirimleri_mahalle_id",
                table: "vatandas_bildirimleri",
                column: "mahalle_id");

            migrationBuilder.CreateIndex(
                name: "ix_vatandas_bildirimleri_takip_no",
                table: "vatandas_bildirimleri",
                column: "takip_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vatandas_bildirimleri_telefon_sade_olusturma_tarihi",
                table: "vatandas_bildirimleri",
                columns: new[] { "telefon_sade", "olusturma_tarihi" });

            // KONUM — `gorevler` ve `projeler` ile aynı düzen. Uzantı yoksa
            // kolon eklenmiyor ve göç yine başarılı sayılıyor.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'postgis') THEN
                        ALTER TABLE vatandas_bildirimleri
                        ADD COLUMN konum geometry(Point, 4326)
                        GENERATED ALWAYS AS (
                            CASE
                                WHEN enlem IS NULL OR boylam IS NULL THEN NULL
                                ELSE ST_SetSRID(ST_MakePoint(boylam, enlem), 4326)
                            END
                        ) STORED;

                        CREATE INDEX ix_vatandas_bildirimleri_konum
                            ON vatandas_bildirimleri USING GIST (konum);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_vatandas_bildirimleri_konum;");
            migrationBuilder.Sql("ALTER TABLE vatandas_bildirimleri DROP COLUMN IF EXISTS konum;");

            migrationBuilder.DropTable(
                name: "telefon_dogrulamalari");

            migrationBuilder.DropTable(
                name: "vatandas_bildirimleri");
        }
    }
}
