using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class OzgecmisHavuzu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ozgecmisler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad_soyad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    telefon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    telefon_sade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    eposta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    meslek_id = table.Column<long>(type: "bigint", nullable: true),
                    meslek_ad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    mahalle_id = table.Column<long>(type: "bigint", nullable: true),
                    adres = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    dosya_adi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    dosya_yolu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    boyut = table.Column<long>(type: "bigint", nullable: false),
                    icerik_turu = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    randevu_id = table.Column<long>(type: "bigint", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    olusturan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleyen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ozgecmisler", x => x.id);
                    table.ForeignKey(
                        name: "fk_ozgecmisler_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ozgecmisler_mahalleler_mahalle_id",
                        column: x => x.mahalle_id,
                        principalTable: "mahalleler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ozgecmisler_meslekler_meslek_id",
                        column: x => x.meslek_id,
                        principalTable: "meslekler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_ozgecmisler_randevular_randevu_id",
                        column: x => x.randevu_id,
                        principalTable: "randevular",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ozgecmis_paylasimlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ozgecmis_id = table.Column<long>(type: "bigint", nullable: false),
                    paylasan_id = table.Column<long>(type: "bigint", nullable: false),
                    paylasan_ad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    alici_id = table.Column<long>(type: "bigint", nullable: false),
                    alici_ad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    not = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    goruntuleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ozgecmis_paylasimlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_ozgecmis_paylasimlari_ozgecmisler_ozgecmis_id",
                        column: x => x.ozgecmis_id,
                        principalTable: "ozgecmisler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmis_paylasimlari_alici_id_tarih",
                table: "ozgecmis_paylasimlari",
                columns: new[] { "alici_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmis_paylasimlari_ozgecmis_id",
                table: "ozgecmis_paylasimlari",
                column: "ozgecmis_id");

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmisler_birim_id",
                table: "ozgecmisler",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmisler_is_deleted_olusturma_tarihi",
                table: "ozgecmisler",
                columns: new[] { "is_deleted", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmisler_mahalle_id",
                table: "ozgecmisler",
                column: "mahalle_id");

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmisler_meslek_id",
                table: "ozgecmisler",
                column: "meslek_id");

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmisler_randevu_id",
                table: "ozgecmisler",
                column: "randevu_id");

            migrationBuilder.CreateIndex(
                name: "ix_ozgecmisler_telefon_sade",
                table: "ozgecmisler",
                column: "telefon_sade");

            // ── Var olan talep özgeçmişlerini havuza taşı ──────────────
            //
            // Canlıda taleplere yüklenmiş özgeçmişler `randevular.ozgecmis_dosya`
            // içinde duruyor. Havuz onları GÖSTERMEZSE modül boş açılır ve
            // "elimizde kim var?" sorusunun cevabı yine talepleri tek tek
            // açmak olur. Dosya kopyalanmaz — aynı dosyayı gösteren bir satır
            // açılır ve `randevu_id` ile kaynağı yazılır.
            //
            // `regexp_replace` ile sade telefon da doldurulur: arama bu sütunda
            // yapılıyor ve ham sütunda numara üç ayrı biçimde duruyor.
            migrationBuilder.Sql(@"
                INSERT INTO ozgecmisler (
                    ad_soyad, telefon, telefon_sade, eposta, meslek_ad,
                    mahalle_id, adres, aciklama,
                    dosya_adi, dosya_yolu, boyut, icerik_turu,
                    randevu_id, birim_id, olusturan, olusturma_tarihi, is_deleted)
                SELECT
                    btrim(coalesce(r.ad, '') || ' ' || coalesce(r.soyad, '')),
                    r.telefon,
                    nullif(regexp_replace(coalesce(r.telefon, ''), '\D', '', 'g'), ''),
                    r.email,
                    r.meslek,
                    r.mahalle_id,
                    r.adres,
                    r.konu,
                    r.ozgecmis_dosya,
                    r.ozgecmis_dosya,
                    0,
                    NULL,
                    r.id,
                    r.birim_id,
                    r.olusturan,
                    coalesce(r.olusturma_tarih, now()::timestamp),
                    false
                FROM randevular r
                WHERE r.ozgecmis_dosya IS NOT NULL
                  AND btrim(r.ozgecmis_dosya) <> ''
                  AND NOT EXISTS (
                      SELECT 1 FROM ozgecmisler o WHERE o.randevu_id = r.id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ozgecmis_paylasimlari");

            migrationBuilder.DropTable(
                name: "ozgecmisler");
        }
    }
}
