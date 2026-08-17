using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class HalkGunuModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "halk_gunleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    baslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    konum = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanici_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_halk_gunleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_halk_gunleri_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "halk_gunu_basvurulari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    soyad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    telefon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    telefon_sade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    adres = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    mahalle_id = table.Column<long>(type: "bigint", nullable: true),
                    meslek = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    konu = table.Column<string>(type: "text", nullable: true),
                    not = table.Column<string>(type: "text", nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    randevu_id = table.Column<long>(type: "bigint", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    olusturan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_halk_gunu_basvurulari", x => x.id);
                    table.ForeignKey(
                        name: "fk_halk_gunu_basvurulari_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_halk_gunu_basvurulari_mahalleler_mahalle_id",
                        column: x => x.mahalle_id,
                        principalTable: "mahalleler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_halk_gunu_basvurulari_randevular_randevu_id",
                        column: x => x.randevu_id,
                        principalTable: "randevular",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "halk_gunu_dilimleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    halk_gunu_id = table.Column<long>(type: "bigint", nullable: false),
                    baslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    bitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    baslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kapasite = table.Column<int>(type: "integer", nullable: true),
                    sira_no = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_halk_gunu_dilimleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_halk_gunu_dilimleri_halk_gunleri_halk_gunu_id",
                        column: x => x.halk_gunu_id,
                        principalTable: "halk_gunleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "halk_gunu_katilimlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    halk_gunu_id = table.Column<long>(type: "bigint", nullable: false),
                    dilim_id = table.Column<long>(type: "bigint", nullable: true),
                    basvuru_id = table.Column<long>(type: "bigint", nullable: false),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    gorusme_notu = table.Column<string>(type: "text", nullable: true),
                    gorusme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    degerlendirmeye_esas = table.Column<bool>(type: "boolean", nullable: false),
                    degerlendirme_notu = table.Column<string>(type: "text", nullable: true),
                    olusan_randevu_id = table.Column<long>(type: "bigint", nullable: true),
                    sms_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_halk_gunu_katilimlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_halk_gunu_katilimlari_halk_gunleri_halk_gunu_id",
                        column: x => x.halk_gunu_id,
                        principalTable: "halk_gunleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_halk_gunu_katilimlari_halk_gunu_basvurulari_basvuru_id",
                        column: x => x.basvuru_id,
                        principalTable: "halk_gunu_basvurulari",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_halk_gunu_katilimlari_halk_gunu_dilimleri_dilim_id",
                        column: x => x.dilim_id,
                        principalTable: "halk_gunu_dilimleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunleri_birim_id_tarih",
                table: "halk_gunleri",
                columns: new[] { "birim_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_basvurulari_birim_id_durum",
                table: "halk_gunu_basvurulari",
                columns: new[] { "birim_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_basvurulari_mahalle_id",
                table: "halk_gunu_basvurulari",
                column: "mahalle_id");

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_basvurulari_randevu_id",
                table: "halk_gunu_basvurulari",
                column: "randevu_id");

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_basvurulari_telefon_sade",
                table: "halk_gunu_basvurulari",
                column: "telefon_sade");

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_dilimleri_halk_gunu_id_sira_no",
                table: "halk_gunu_dilimleri",
                columns: new[] { "halk_gunu_id", "sira_no" });

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_katilimlari_basvuru_id",
                table: "halk_gunu_katilimlari",
                column: "basvuru_id");

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_katilimlari_dilim_id",
                table: "halk_gunu_katilimlari",
                column: "dilim_id");

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_katilimlari_halk_gunu_id_basvuru_id",
                table: "halk_gunu_katilimlari",
                columns: new[] { "halk_gunu_id", "basvuru_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_halk_gunu_katilimlari_halk_gunu_id_dilim_id_sira_no",
                table: "halk_gunu_katilimlari",
                columns: new[] { "halk_gunu_id", "dilim_id", "sira_no" });

            // ── Kişi geçmişi için NORMALLEŞTİRİLMİŞ TELEFON indeksleri ──
            //
            // Vatandaşı bulmanın tek doğal anahtarı telefon, ama kayıtlardaki
            // numaralar karışık biçimde: `0541 298 34 50`, `05412983450`,
            // `+90 541…`. Bu yüzden karşılaştırma
            // `regexp_replace(telefon,'\D','','g')` ile yapılıyor — böyle bir
            // ifade B-tree indeksini kullanamaz, o yüzden İFADE İNDEKSİ
            // gerekiyor. EF bunu modelden üretemiyor, elle yazılıyor.
            //
            // `IF NOT EXISTS`: üretim veritabanında indeks elle oluşturulmuş
            // olabilir; migration ikinci kez çalıştığında düşmesin.
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_randevular_telefon_sade
                    ON randevular ((regexp_replace(coalesce(telefon,''), '\D', '', 'g')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_ajandalar_irtibat_telefon_sade
                    ON ajandalar ((regexp_replace(coalesce(irtibat_telefon,''), '\D', '', 'g')));
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_protokoller_telefon_sade
                    ON protokoller ((regexp_replace(coalesce(telefon,''), '\D', '', 'g')),
                                    (regexp_replace(coalesce(cep_telefon,''), '\D', '', 'g')));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_randevular_telefon_sade;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_ajandalar_irtibat_telefon_sade;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_protokoller_telefon_sade;");

            migrationBuilder.DropTable(
                name: "halk_gunu_katilimlari");

            migrationBuilder.DropTable(
                name: "halk_gunu_basvurulari");

            migrationBuilder.DropTable(
                name: "halk_gunu_dilimleri");

            migrationBuilder.DropTable(
                name: "halk_gunleri");
        }
    }
}
