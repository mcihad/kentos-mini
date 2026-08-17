using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class IsTakipGorevler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ekipler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: false),
                    lider_id = table.Column<long>(type: "bigint", nullable: true),
                    kullanimda = table.Column<bool>(type: "boolean", nullable: false),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ekipler", x => x.id);
                    table.ForeignKey(
                        name: "fk_ekipler_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gorev_tipleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    renk = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hizmet_standardi_gun = table.Column<int>(type: "integer", nullable: true),
                    sla_saat = table.Column<int>(type: "integer", nullable: true),
                    varsayilan_oncelik = table.Column<int>(type: "integer", nullable: false),
                    konum_zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    kullanimda = table.Column<bool>(type: "boolean", nullable: false),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorev_tipleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorev_tipleri_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "is_olaylari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    varlik_turu = table.Column<int>(type: "integer", nullable: false),
                    varlik_id = table.Column<long>(type: "bigint", nullable: false),
                    tip = table.Column<int>(type: "integer", nullable: false),
                    aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    degisiklikler_json = table.Column<string>(type: "text", nullable: true),
                    kullanici = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_is_olaylari", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ekip_uyeleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ekip_id = table.Column<long>(type: "bigint", nullable: false),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false),
                    eklenme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ekip_uyeleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_ekip_uyeleri_ekipler_ekip_id",
                        column: x => x.ekip_id,
                        principalTable: "ekipler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gorev_tipi_asamalari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gorev_tipi_id = table.Column<long>(type: "bigint", nullable: false),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    aciklama_zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    fotograf_zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    tahmini_saat = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorev_tipi_asamalari", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorev_tipi_asamalari_gorev_tipleri_gorev_tipi_id",
                        column: x => x.gorev_tipi_id,
                        principalTable: "gorev_tipleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gorev_tipi_birimleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gorev_tipi_id = table.Column<long>(type: "bigint", nullable: false),
                    birim_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorev_tipi_birimleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorev_tipi_birimleri_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gorev_tipi_birimleri_gorev_tipleri_gorev_tipi_id",
                        column: x => x.gorev_tipi_id,
                        principalTable: "gorev_tipleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gorev_tipi_devirleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gorev_tipi_id = table.Column<long>(type: "bigint", nullable: false),
                    hedef_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    is_talebi = table.Column<bool>(type: "boolean", nullable: false),
                    not = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hedef_gorev_tipi_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorev_tipi_devirleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorev_tipi_devirleri_birimler_hedef_birim_id",
                        column: x => x.hedef_birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_gorev_tipi_devirleri_gorev_tipleri_gorev_tipi_id",
                        column: x => x.gorev_tipi_id,
                        principalTable: "gorev_tipleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gorevler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    takip_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    baslik = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    gorev_tipi_id = table.Column<long>(type: "bigint", nullable: true),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    oncelik = table.Column<int>(type: "integer", nullable: false),
                    kaynak = table.Column<int>(type: "integer", nullable: false),
                    kaynak_id = table.Column<long>(type: "bigint", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: false),
                    ust_gorev_id = table.Column<long>(type: "bigint", nullable: true),
                    proje_id = table.Column<long>(type: "bigint", nullable: true),
                    kilometre_tasi_id = table.Column<long>(type: "bigint", nullable: true),
                    enlem = table.Column<double>(type: "double precision", nullable: true),
                    boylam = table.Column<double>(type: "double precision", nullable: true),
                    adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mahalle_id = table.Column<long>(type: "bigint", nullable: true),
                    planlanan_baslangic = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    planlanan_bitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    baslama_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tamamlanma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    sla_bitis = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    bekleme_dakika = table.Column<int>(type: "integer", nullable: false),
                    gerekce = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    onaylayan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    guncelleyen = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    olusturan_birim_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorevler", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorevler_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gorevler_gorev_tipleri_gorev_tipi_id",
                        column: x => x.gorev_tipi_id,
                        principalTable: "gorev_tipleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gorevler_gorevler_ust_gorev_id",
                        column: x => x.ust_gorev_id,
                        principalTable: "gorevler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gorevler_mahalleler_mahalle_id",
                        column: x => x.mahalle_id,
                        principalTable: "mahalleler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gorev_asamalari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gorev_id = table.Column<long>(type: "bigint", nullable: false),
                    gorev_tipi_asama_id = table.Column<long>(type: "bigint", nullable: true),
                    sira_no = table.Column<int>(type: "integer", nullable: false),
                    ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    durum = table.Column<int>(type: "integer", nullable: false),
                    zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    aciklama_zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    fotograf_zorunlu = table.Column<bool>(type: "boolean", nullable: false),
                    not = table.Column<string>(type: "text", maxLength: 2000, nullable: true),
                    tamamlanma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tamamlayan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    tamamlayan_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorev_asamalari", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorev_asamalari_gorevler_gorev_id",
                        column: x => x.gorev_id,
                        principalTable: "gorevler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gorev_atamalari",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    gorev_id = table.Column<long>(type: "bigint", nullable: false),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: true),
                    ekip_id = table.Column<long>(type: "bigint", nullable: true),
                    rol = table.Column<int>(type: "integer", nullable: false),
                    atayan = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    atama_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gorev_atamalari", x => x.id);
                    table.ForeignKey(
                        name: "fk_gorev_atamalari_ekipler_ekip_id",
                        column: x => x.ekip_id,
                        principalTable: "ekipler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_gorev_atamalari_gorevler_gorev_id",
                        column: x => x.gorev_id,
                        principalTable: "gorevler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ekip_uyeleri_ekip_id_kullanici_id",
                table: "ekip_uyeleri",
                columns: new[] { "ekip_id", "kullanici_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ekipler_birim_id_kullanimda",
                table: "ekipler",
                columns: new[] { "birim_id", "kullanimda" });

            migrationBuilder.CreateIndex(
                name: "ix_gorev_asamalari_gorev_id_sira_no",
                table: "gorev_asamalari",
                columns: new[] { "gorev_id", "sira_no" });

            migrationBuilder.CreateIndex(
                name: "ix_gorev_atamalari_ekip_id",
                table: "gorev_atamalari",
                column: "ekip_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorev_atamalari_gorev_id_kullanici_id",
                table: "gorev_atamalari",
                columns: new[] { "gorev_id", "kullanici_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gorev_atamalari_kullanici_id",
                table: "gorev_atamalari",
                column: "kullanici_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorev_tipi_asamalari_gorev_tipi_id_sira_no",
                table: "gorev_tipi_asamalari",
                columns: new[] { "gorev_tipi_id", "sira_no" });

            migrationBuilder.CreateIndex(
                name: "ix_gorev_tipi_birimleri_birim_id",
                table: "gorev_tipi_birimleri",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorev_tipi_birimleri_gorev_tipi_id_birim_id",
                table: "gorev_tipi_birimleri",
                columns: new[] { "gorev_tipi_id", "birim_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gorev_tipi_devirleri_gorev_tipi_id",
                table: "gorev_tipi_devirleri",
                column: "gorev_tipi_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorev_tipi_devirleri_hedef_birim_id",
                table: "gorev_tipi_devirleri",
                column: "hedef_birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorev_tipleri_birim_id_kullanimda",
                table: "gorev_tipleri",
                columns: new[] { "birim_id", "kullanimda" });

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_birim_id_durum_olusturma_tarihi",
                table: "gorevler",
                columns: new[] { "birim_id", "durum", "olusturma_tarihi" });

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_birim_id_sla_bitis",
                table: "gorevler",
                columns: new[] { "birim_id", "sla_bitis" });

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_gorev_tipi_id",
                table: "gorevler",
                column: "gorev_tipi_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_kaynak_kaynak_id",
                table: "gorevler",
                columns: new[] { "kaynak", "kaynak_id" });

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_mahalle_id",
                table: "gorevler",
                column: "mahalle_id");

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_takip_no",
                table: "gorevler",
                column: "takip_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gorevler_ust_gorev_id",
                table: "gorevler",
                column: "ust_gorev_id");

            migrationBuilder.CreateIndex(
                name: "ix_is_olaylari_varlik_turu_varlik_id_tarih",
                table: "is_olaylari",
                columns: new[] { "varlik_turu", "varlik_id", "tarih" });

            // ── KONUM: enlem/boylamdan TÜRETİLEN geometri ──────────────────
            //
            // Kolon `GENERATED ALWAYS ... STORED`: enlem ve boylam tek
            // doğruluk kaynağı, geometri onlardan üretiliyor. İkisini ayrı
            // yazsaydık birbirini tutmayan bir kayıt mümkün olurdu ve
            // haritadaki nokta ile formdaki sayı ayrışırdı.
            //
            // Neden C# tarafında `Point` yok: `KentOS.Mini.Application`
            // projesinin HİÇ NuGet bağımlılığı yok (değişmez kural) ve
            // NetTopologySuite oraya giremiyor. Uzamsal sorgular (yarıçap,
            // alan içi, kümeleme, ısı haritası) SQL'de yazılıyor; API
            // enlem/boylam döndürüyor, WKT değil.
            //
            // ST_MakePoint SIRASI: (boylam, enlem) — x, y. Ters yazmak
            // Türkiye'deki noktaları Somali açıklarına taşırdı.
            //
            // ── PostGIS ZORUNLU DEĞİL ──────────────────────────────────────
            //
            // Uzantıyı kurmak SÜPER KULLANICI ister; uygulamanın rolü öyle
            // olmak zorunda değil ve açık kaynak bir üründe olmayacağını
            // varsaymak gerekiyor. Bu yüzden migration üç adımda ilerliyor:
            //
            //   1. Uzantıyı kurmayı DENE, yetki yoksa sessizce geç.
            //   2. Uzantı VARSA geometri kolonunu ve GIST indeksini ekle.
            //   3. Yoksa hiçbir şey yapma — tablolar yine kurulur, enlem ve
            //      boylam yine saklanır, yalnızca uzamsal sorgular kapalı
            //      kalır. Açılışta bunu söyleyen bir uyarı yazılır
            //      (`Program.cs` → PostGIS denetimi).
            //
            // Alternatifi migration'ın patlaması olurdu: PostGIS'siz bir
            // kurulumda uygulama HİÇ açılmazdı — harita dışındaki her şey
            // çalışabilecekken.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    CREATE EXTENSION IF NOT EXISTS postgis;
                EXCEPTION
                    WHEN insufficient_privilege THEN
                        RAISE NOTICE 'PostGIS kurulamadı (yetki yok); konum kolonu atlanıyor.';
                    WHEN undefined_file THEN
                        RAISE NOTICE 'PostGIS sunucuda yüklü değil; konum kolonu atlanıyor.';
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'postgis') THEN
                        ALTER TABLE gorevler
                        ADD COLUMN konum geometry(Point, 4326)
                        GENERATED ALWAYS AS (
                            CASE
                                WHEN enlem IS NULL OR boylam IS NULL THEN NULL
                                ELSE ST_SetSRID(ST_MakePoint(boylam, enlem), 4326)
                            END
                        ) STORED;

                        -- GIST: yarıçap ve alan sorgularının çalıştığı indeks.
                        -- B-tree geometriye fayda etmiyor.
                        CREATE INDEX ix_gorevler_konum ON gorevler USING GIST (konum);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_gorevler_konum;");
            migrationBuilder.Sql("ALTER TABLE gorevler DROP COLUMN IF EXISTS konum;");

            migrationBuilder.DropTable(
                name: "ekip_uyeleri");

            migrationBuilder.DropTable(
                name: "gorev_asamalari");

            migrationBuilder.DropTable(
                name: "gorev_atamalari");

            migrationBuilder.DropTable(
                name: "gorev_tipi_asamalari");

            migrationBuilder.DropTable(
                name: "gorev_tipi_birimleri");

            migrationBuilder.DropTable(
                name: "gorev_tipi_devirleri");

            migrationBuilder.DropTable(
                name: "is_olaylari");

            migrationBuilder.DropTable(
                name: "ekipler");

            migrationBuilder.DropTable(
                name: "gorevler");

            migrationBuilder.DropTable(
                name: "gorev_tipleri");
        }
    }
}
