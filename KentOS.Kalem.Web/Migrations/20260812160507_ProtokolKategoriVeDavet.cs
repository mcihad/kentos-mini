using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProtokolKategoriVeDavet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_protokoller_sira_no_kategori",
                table: "protokoller");

            // `kategori` METİN sütunu burada DÜŞÜRÜLMEZ: önce kategori tablosu
            // kurulup mevcut değerler oraya taşınacak. EF'in ürettiği sıra
            // sütunu hemen siliyordu — üretimdeki protokol kayıtlarının
            // kategorisi kaybolurdu.
            migrationBuilder.AddColumn<long>(
                name: "kategori_id",
                table: "protokoller",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "davetler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    baslik = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    yer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanici_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_davetler", x => x.id);
                    table.ForeignKey(
                        name: "fk_davetler_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_davetler_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "protokol_kategorileri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    aktif = table.Column<bool>(type: "boolean", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protokol_kategorileri", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "davet_kisileri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    davet_id = table.Column<long>(type: "bigint", nullable: false),
                    protokol_id = table.Column<long>(type: "bigint", nullable: false),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    arandi = table.Column<bool>(type: "boolean", nullable: false),
                    arandi_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    mesaj_gonderildi = table.Column<bool>(type: "boolean", nullable: false),
                    mesaj_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    not = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_davet_kisileri", x => x.id);
                    table.ForeignKey(
                        name: "fk_davet_kisileri_davetler_davet_id",
                        column: x => x.davet_id,
                        principalTable: "davetler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_davet_kisileri_protokoller_protokol_id",
                        column: x => x.protokol_id,
                        principalTable: "protokoller",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            /*
             * VERİ TAŞIMA — kategori metninden kategori tablosuna.
             *
             * Üretimde protokol kayıtlarının kategorisi serbest metindi.
             * Sütunu silip yenisini eklemek bu bilgiyi yok ederdi; burada
             * önce benzersiz kategoriler tabloya yazılıyor, sonra her kayıt
             * kendi kategorisine bağlanıyor.
             *
             * `TRIM` + büyük/küçük harf duyarsız eşleştirme: aynı kategorinin
             * farklı yazımları TEK kayıtta birleşiyor — tabloya almanın
             * sebebi zaten buydu.
             */
            migrationBuilder.Sql("""
                INSERT INTO protokol_kategorileri (ad, sira_no, aktif, olusturma_tarihi)
                SELECT DISTINCT ON (LOWER(TRIM(kategori)))
                       TRIM(kategori),
                       ROW_NUMBER() OVER (ORDER BY LOWER(TRIM(kategori)))::int,
                       TRUE,
                       NOW()
                FROM protokoller
                WHERE kategori IS NOT NULL AND TRIM(kategori) <> ''
                ORDER BY LOWER(TRIM(kategori)), TRIM(kategori);
                """);

            migrationBuilder.Sql("""
                UPDATE protokoller p
                SET kategori_id = k.id
                FROM protokol_kategorileri k
                WHERE LOWER(TRIM(p.kategori)) = LOWER(k.ad);
                """);

            // Kategorisi hiç olmayan kayıtlar için bir toplama kategorisi.
            migrationBuilder.Sql("""
                INSERT INTO protokol_kategorileri (ad, sira_no, aktif, olusturma_tarihi)
                SELECT 'Diğer',
                       COALESCE((SELECT MAX(sira_no) FROM protokol_kategorileri), 0) + 1,
                       TRUE,
                       NOW()
                WHERE EXISTS (SELECT 1 FROM protokoller WHERE kategori_id = 0)
                  AND NOT EXISTS (SELECT 1 FROM protokol_kategorileri WHERE LOWER(ad) = 'diğer');
                """);

            migrationBuilder.Sql("""
                UPDATE protokoller
                SET kategori_id = (SELECT id FROM protokol_kategorileri WHERE LOWER(ad) = 'diğer')
                WHERE kategori_id = 0;
                """);

            migrationBuilder.DropColumn(
                name: "kategori",
                table: "protokoller");

            migrationBuilder.CreateIndex(
                name: "ix_protokoller_kategori_id",
                table: "protokoller",
                column: "kategori_id");

            migrationBuilder.CreateIndex(
                name: "ix_protokoller_sira_no_kategori_id",
                table: "protokoller",
                columns: new[] { "sira_no", "kategori_id" });

            migrationBuilder.CreateIndex(
                name: "ix_davet_kisileri_davet_id_protokol_id",
                table: "davet_kisileri",
                columns: new[] { "davet_id", "protokol_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_davet_kisileri_protokol_id",
                table: "davet_kisileri",
                column: "protokol_id");

            migrationBuilder.CreateIndex(
                name: "ix_davetler_ajanda_id",
                table: "davetler",
                column: "ajanda_id");

            migrationBuilder.CreateIndex(
                name: "ix_davetler_birim_id_tarih",
                table: "davetler",
                columns: new[] { "birim_id", "tarih" });

            migrationBuilder.CreateIndex(
                name: "ix_protokol_kategorileri_ad",
                table: "protokol_kategorileri",
                column: "ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_protokol_kategorileri_sira_no",
                table: "protokol_kategorileri",
                column: "sira_no");

            migrationBuilder.AddForeignKey(
                name: "fk_protokoller_protokol_kategorileri_kategori_id",
                table: "protokoller",
                column: "kategori_id",
                principalTable: "protokol_kategorileri",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_protokoller_protokol_kategorileri_kategori_id",
                table: "protokoller");

            migrationBuilder.DropTable(
                name: "davet_kisileri");

            migrationBuilder.DropTable(
                name: "protokol_kategorileri");

            migrationBuilder.DropTable(
                name: "davetler");

            migrationBuilder.DropIndex(
                name: "ix_protokoller_kategori_id",
                table: "protokoller");

            migrationBuilder.DropIndex(
                name: "ix_protokoller_sira_no_kategori_id",
                table: "protokoller");

            migrationBuilder.DropColumn(
                name: "kategori_id",
                table: "protokoller");

            migrationBuilder.AddColumn<string>(
                name: "kategori",
                table: "protokoller",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_protokoller_sira_no_kategori",
                table: "protokoller",
                columns: new[] { "sira_no", "kategori" });
        }
    }
}
