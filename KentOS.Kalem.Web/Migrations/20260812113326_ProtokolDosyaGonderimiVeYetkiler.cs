using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Kalem.Web.Migrations
{
    /// <inheritdoc />
    public partial class ProtokolDosyaGonderimiVeYetkiler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "dosya_gonderebilir",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "gizli_etkinlik_ekleyebilir",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "dosya_gonderimleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gonderen_id = table.Column<long>(type: "bigint", nullable: false),
                    alici_id = table.Column<long>(type: "bigint", nullable: false),
                    konu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dosya_adi = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    dosya_yolu = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    icerik_turu = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    boyut = table.Column<long>(type: "bigint", nullable: false),
                    okunma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dosya_gonderimleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_dosya_gonderimleri_users_alici_id",
                        column: x => x.alici_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_dosya_gonderimleri_users_gonderen_id",
                        column: x => x.gonderen_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "protokoller",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    kategori = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    kurum = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ad_soyad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    unvan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    telefon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    cep_telefon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    eposta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    adres = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    aktif = table.Column<bool>(type: "boolean", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_protokoller", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dosya_gonderimi_notlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gonderim_id = table.Column<long>(type: "bigint", nullable: false),
                    yazan_id = table.Column<long>(type: "bigint", nullable: false),
                    metin = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dosya_gonderimi_notlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_dosya_gonderimi_notlari_dosya_gonderimleri_gonderim_id",
                        column: x => x.gonderim_id,
                        principalTable: "dosya_gonderimleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dosya_gonderimi_notlari_users_yazan_id",
                        column: x => x.yazan_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            /*
             * MEVCUT kullanıcılar için gizli etkinlik yetkisini AÇIK bırak.
             *
             * Sistem iki yıldır canlıda ve bugüne kadar bu kısıt yoktu; sütunu
             * `false` ile eklemek, bir sonraki dağıtımda gizli etkinlik
             * oluşturan herkesin (mobil dahil) sessizce engellenmesi demekti.
             * Var olan davranış korunuyor, YENİ kullanıcılar kısıtlı
             * başlıyor (sütun varsayılanı `false`). Yöneticiler yetkiyi
             * kullanıcı ekranından geri alabilir.
             *
             * Dosya gönderme yetkisi YENİ bir özellik; kimsenin elinden bir
             * şey alınmıyor, o yüzden herkeste `false` başlıyor.
             */
            // Tablo adı `AspNetUsers` — Identity varsayılanı korunmuş, snake_case
            // adlandırma sözleşmesi ona uygulanmamış. Tırnaksız yazmak
            // Postgres'te küçük harfe indirilir ve tablo bulunamaz.
            migrationBuilder.Sql(
                @"UPDATE ""AspNetUsers"" SET gizli_etkinlik_ekleyebilir = TRUE;");

            migrationBuilder.CreateIndex(
                name: "ix_dosya_gonderimi_notlari_gonderim_id_olusturma_tarihi",
                table: "dosya_gonderimi_notlari",
                columns: new[] { "gonderim_id", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_dosya_gonderimi_notlari_yazan_id",
                table: "dosya_gonderimi_notlari",
                column: "yazan_id");

            migrationBuilder.CreateIndex(
                name: "ix_dosya_gonderimleri_alici_id_olusturma_tarihi",
                table: "dosya_gonderimleri",
                columns: new[] { "alici_id", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_dosya_gonderimleri_gonderen_id_olusturma_tarihi",
                table: "dosya_gonderimleri",
                columns: new[] { "gonderen_id", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_protokoller_sira_no_kategori",
                table: "protokoller",
                columns: new[] { "sira_no", "kategori" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dosya_gonderimi_notlari");

            migrationBuilder.DropTable(
                name: "protokoller");

            migrationBuilder.DropTable(
                name: "dosya_gonderimleri");

            migrationBuilder.DropColumn(
                name: "dosya_gonderebilir",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "gizli_etkinlik_ekleyebilir",
                table: "AspNetUsers");
        }
    }
}
