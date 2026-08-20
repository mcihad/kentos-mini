using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class FormModulu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "form_portali_acik",
                table: "kurum_bilgileri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "form_surumleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    form_id = table.Column<long>(type: "bigint", nullable: false),
                    surum_no = table.Column<int>(type: "integer", nullable: false),
                    tanim = table.Column<string>(type: "jsonb", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    olusturan_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_surumleri", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "formlar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    erisim_anahtari = table.Column<string>(type: "text", nullable: false),
                    anonim_tuzu = table.Column<string>(type: "text", nullable: false),
                    baslik = table.Column<string>(type: "text", nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    erisim = table.Column<int>(type: "integer", nullable: false),
                    yayin_surum_id = table.Column<long>(type: "bigint", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    baslangic_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    bitis_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    yanit_siniri = table.Column<int>(type: "integer", nullable: true),
                    yanit_sayisi = table.Column<int>(type: "integer", nullable: false),
                    tek_yanit = table.Column<bool>(type: "boolean", nullable: false),
                    tesekkur_metni = table.Column<string>(type: "text", nullable: true),
                    tesekkur_adresi = table.Column<string>(type: "text", nullable: true),
                    yanit_ozeti_gorunur = table.Column<bool>(type: "boolean", nullable: false),
                    sonuclar_herkese_acik = table.Column<bool>(type: "boolean", nullable: false),
                    olusturan_id = table.Column<long>(type: "bigint", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    yayin_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    silindi = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formlar", x => x.id);
                    table.ForeignKey(
                        name: "fk_formlar_form_surumleri_yayin_surum_id",
                        column: x => x.yayin_surum_id,
                        principalTable: "form_surumleri",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "form_yanitlari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    form_id = table.Column<long>(type: "bigint", nullable: false),
                    surum_id = table.Column<long>(type: "bigint", nullable: false),
                    takip_no = table.Column<string>(type: "text", nullable: false),
                    surdurme_anahtari = table.Column<string>(type: "text", nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    cevaplar = table.Column<string>(type: "jsonb", nullable: false),
                    ad_soyad = table.Column<string>(type: "text", nullable: true),
                    telefon = table.Column<string>(type: "text", nullable: true),
                    telefon_sade = table.Column<string>(type: "text", nullable: true),
                    kimlik_karmasi = table.Column<string>(type: "text", nullable: true),
                    eposta = table.Column<string>(type: "text", nullable: true),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: true),
                    ip_ozeti = table.Column<string>(type: "text", nullable: true),
                    tarayici = table.Column<string>(type: "text", nullable: true),
                    baslama_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    gonderim_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_yanitlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_yanitlari_form_surumleri_surum_id",
                        column: x => x.surum_id,
                        principalTable: "form_surumleri",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_form_yanitlari_formlar_form_id",
                        column: x => x.form_id,
                        principalTable: "formlar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_yanit_dosyalari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    yanit_id = table.Column<long>(type: "bigint", nullable: false),
                    alan_kimligi = table.Column<string>(type: "text", nullable: false),
                    ad = table.Column<string>(type: "text", nullable: false),
                    anahtar = table.Column<string>(type: "text", nullable: false),
                    icerik_tipi = table.Column<string>(type: "text", nullable: true),
                    boyut = table.Column<long>(type: "bigint", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_form_yanit_dosyalari", x => x.id);
                    table.ForeignKey(
                        name: "fk_form_yanit_dosyalari_form_yanitlari_yanit_id",
                        column: x => x.yanit_id,
                        principalTable: "form_yanitlari",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_form_surumleri_form_id_surum_no",
                table: "form_surumleri",
                columns: new[] { "form_id", "surum_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_yanit_dosyalari_yanit_id",
                table: "form_yanit_dosyalari",
                column: "yanit_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_yanitlari_cevaplar",
                table: "form_yanitlari",
                column: "cevaplar")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_form_yanitlari_form_id_durum",
                table: "form_yanitlari",
                columns: new[] { "form_id", "durum" });

            migrationBuilder.CreateIndex(
                name: "ix_form_yanitlari_form_id_kimlik_karmasi",
                table: "form_yanitlari",
                columns: new[] { "form_id", "kimlik_karmasi" },
                unique: true,
                filter: "kimlik_karmasi IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_form_yanitlari_form_id_telefon_sade",
                table: "form_yanitlari",
                columns: new[] { "form_id", "telefon_sade" });

            migrationBuilder.CreateIndex(
                name: "ix_form_yanitlari_surum_id",
                table: "form_yanitlari",
                column: "surum_id");

            migrationBuilder.CreateIndex(
                name: "ix_form_yanitlari_takip_no",
                table: "form_yanitlari",
                column: "takip_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formlar_erisim_anahtari",
                table: "formlar",
                column: "erisim_anahtari",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formlar_yayin_surum_id",
                table: "formlar",
                column: "yayin_surum_id");

            migrationBuilder.AddForeignKey(
                name: "fk_form_surumleri_formlar_form_id",
                table: "form_surumleri",
                column: "form_id",
                principalTable: "formlar",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_form_surumleri_formlar_form_id",
                table: "form_surumleri");

            migrationBuilder.DropTable(
                name: "form_yanit_dosyalari");

            migrationBuilder.DropTable(
                name: "form_yanitlari");

            migrationBuilder.DropTable(
                name: "formlar");

            migrationBuilder.DropTable(
                name: "form_surumleri");

            migrationBuilder.DropColumn(
                name: "form_portali_acik",
                table: "kurum_bilgileri");
        }
    }
}
