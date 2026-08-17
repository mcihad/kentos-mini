using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KentOS.Mini.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ajanda_durumlar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "varchar(100)", nullable: false),
                    renk = table.Column<string>(type: "varchar(20)", nullable: false),
                    aciklama = table.Column<string>(type: "varchar(500)", nullable: true),
                    icon = table.Column<string>(type: "varchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_durumlar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    description = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "birimler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "varchar(100)", nullable: false),
                    yetkili = table.Column<string>(type: "varchar(100)", nullable: false),
                    unvan = table.Column<string>(type: "varchar(50)", nullable: true),
                    telefon = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    adres = table.Column<string>(type: "text", nullable: true),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    ust_birim_id = table.Column<long>(type: "bigint", nullable: true),
                    left_id = table.Column<int>(type: "integer", nullable: false),
                    right_id = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_birimler", x => x.id);
                    table.ForeignKey(
                        name: "fk_birimler_birimler_ust_birim_id",
                        column: x => x.ust_birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cicekciler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad_soyad = table.Column<string>(type: "text", nullable: false),
                    telefon = table.Column<string>(type: "text", nullable: false),
                    adres = table.Column<string>(type: "text", nullable: false),
                    aktif = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cicekciler", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "icmesuyu_analizleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    tarih = table.Column<DateOnly>(type: "date", nullable: false),
                    analiz_no = table.Column<Guid>(type: "uuid", nullable: false),
                    analiz_nokta = table.Column<int>(type: "integer", nullable: false),
                    sicaklik = table.Column<float>(type: "real", nullable: true),
                    ph = table.Column<float>(type: "real", nullable: true),
                    phs = table.Column<float>(type: "real", nullable: true),
                    bulaniklik = table.Column<float>(type: "real", nullable: true),
                    renk = table.Column<float>(type: "real", nullable: true),
                    serbest_klor = table.Column<float>(type: "real", nullable: true),
                    alkalinite = table.Column<float>(type: "real", nullable: true),
                    tsertlik = table.Column<float>(type: "real", nullable: true),
                    casertlik = table.Column<float>(type: "real", nullable: true),
                    mgsertlik = table.Column<float>(type: "real", nullable: true),
                    tds = table.Column<float>(type: "real", nullable: true),
                    iletkenlik = table.Column<float>(type: "real", nullable: true),
                    demir = table.Column<float>(type: "real", nullable: true),
                    mangan = table.Column<float>(type: "real", nullable: true),
                    nitrat = table.Column<float>(type: "real", nullable: true),
                    nitrit = table.Column<float>(type: "real", nullable: true),
                    amonyak = table.Column<float>(type: "real", nullable: true),
                    permanganat = table.Column<float>(type: "real", nullable: true),
                    oksijen = table.Column<float>(type: "real", nullable: true),
                    bakteriyolojik = table.Column<float>(type: "real", nullable: true),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_icmesuyu_analizleri", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mahalleler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mahalleler", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meslekler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meslekler", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "text", nullable: true),
                    message_type = table.Column<int>(type: "integer", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    fail_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "randevu_durumlar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    durum_ad = table.Column<string>(type: "varchar(50)", nullable: false),
                    renk = table.Column<string>(type: "varchar(50)", nullable: false),
                    simge = table.Column<string>(type: "varchar(50)", nullable: true),
                    aciklama = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_randevu_durumlar", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "randevu_tipleri",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "varchar(50)", nullable: false),
                    renk = table.Column<string>(type: "varchar(50)", nullable: true),
                    aciklama = table.Column<string>(type: "varchar(255)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_randevu_tipleri", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "text", nullable: true),
                    soyad = table.Column<string>(type: "text", nullable: true),
                    unvan = table.Column<string>(type: "text", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_users_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ajandalar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    baslik = table.Column<string>(type: "varchar(100)", nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    konum = table.Column<string>(type: "varchar(100)", nullable: true),
                    koordinat = table.Column<string>(type: "varchar(100)", nullable: true),
                    baslangic_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    bitis_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tum_gun = table.Column<bool>(type: "boolean", nullable: false),
                    basin_katilsin = table.Column<bool>(type: "boolean", nullable: false),
                    konusma_metni_durum = table.Column<bool>(type: "boolean", nullable: false),
                    bilgi_notu_durum = table.Column<bool>(type: "boolean", nullable: false),
                    resim_var = table.Column<bool>(type: "boolean", nullable: false),
                    tekrar_eden = table.Column<bool>(type: "boolean", nullable: false),
                    bilgi_notu = table.Column<string>(type: "text", nullable: true),
                    konusma_metni = table.Column<string>(type: "text", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    kullanici_id = table.Column<string>(type: "text", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    randevu_id = table.Column<long>(type: "bigint", nullable: false),
                    randevu_tip_id = table.Column<long>(type: "bigint", nullable: true),
                    durum_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    cicek_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajandalar", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajandalar_ajanda_durumlar_durum_id",
                        column: x => x.durum_id,
                        principalTable: "ajanda_durumlar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ajandalar_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ajandalar_randevu_tipleri_randevu_tip_id",
                        column: x => x.randevu_tip_id,
                        principalTable: "randevu_tipleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "randevular",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    konu = table.Column<string>(type: "varchar(100)", nullable: false),
                    ad = table.Column<string>(type: "varchar(100)", nullable: false),
                    soyad = table.Column<string>(type: "varchar(100)", nullable: false),
                    meslek = table.Column<string>(type: "varchar(100)", nullable: true),
                    telefon = table.Column<string>(type: "varchar(50)", nullable: true),
                    email = table.Column<string>(type: "varchar(100)", nullable: true),
                    adres = table.Column<string>(type: "varchar(255)", nullable: true),
                    yer = table.Column<string>(type: "varchar(100)", nullable: true),
                    koordinat = table.Column<string>(type: "varchar(100)", nullable: true),
                    baslangic_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    bitis_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    ozgecmis_durum = table.Column<bool>(type: "boolean", nullable: false),
                    ozgecmis_dosya = table.Column<string>(type: "varchar(255)", nullable: true),
                    birim_id = table.Column<long>(type: "bigint", nullable: true),
                    randevu_tip_id = table.Column<long>(type: "bigint", nullable: true),
                    mahalle_id = table.Column<long>(type: "bigint", nullable: false),
                    randevu_durum_id = table.Column<long>(type: "bigint", nullable: false),
                    olusturma_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    guncelleme_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturan = table.Column<string>(type: "varchar(100)", nullable: true),
                    tamamlanma_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    guncelleyen = table.Column<string>(type: "varchar(100)", nullable: true),
                    ajanda_durum = table.Column<bool>(type: "boolean", nullable: false),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_randevular", x => x.id);
                    table.ForeignKey(
                        name: "fk_randevular_birimler_birim_id",
                        column: x => x.birim_id,
                        principalTable: "birimler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_randevular_mahalleler_mahalle_id",
                        column: x => x.mahalle_id,
                        principalTable: "mahalleler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_randevular_randevu_durumlar_randevu_durum_id",
                        column: x => x.randevu_durum_id,
                        principalTable: "randevu_durumlar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_randevular_randevu_tipleri_randevu_tip_id",
                        column: x => x.randevu_tip_id,
                        principalTable: "randevu_tipleri",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ajanda_hareketler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false),
                    kullanici = table.Column<string>(type: "varchar(100)", nullable: false),
                    eski_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    eski_birim = table.Column<string>(type: "varchar(100)", nullable: false),
                    yeni_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    yeni_birim = table.Column<string>(type: "varchar(100)", nullable: false),
                    asagi_hareket = table.Column<bool>(type: "boolean", nullable: false),
                    tarih = table.Column<string>(type: "varchar(100)", nullable: false),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_hareketler", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajanda_hareketler_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ajanda_notlar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    not = table.Column<string>(type: "text", nullable: false),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: true),
                    olusturan = table.Column<string>(type: "text", nullable: true),
                    olusturulma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_notlar", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajanda_notlar_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ajanda_photos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    filename = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajanda_photos_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ajanda_tekrarlar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    tekrar_sikligi = table.Column<int>(type: "integer", nullable: false),
                    baslangic_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    bitis_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    tekrar_sayisi = table.Column<int>(type: "integer", nullable: true),
                    haftanin_gunleri = table.Column<int>(type: "integer", nullable: true),
                    ayin_gunleri = table.Column<string>(type: "text", nullable: true),
                    olusturma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ajanda_tekrarlar", x => x.id);
                    table.ForeignKey(
                        name: "fk_ajanda_tekrarlar_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ajanda_tekrarlar_ajandalar_id",
                        column: x => x.id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cicekler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    cicekci_id = table.Column<long>(type: "bigint", nullable: false),
                    guid = table.Column<string>(type: "text", nullable: true),
                    ajanda_id = table.Column<long>(type: "bigint", nullable: true),
                    ad = table.Column<string>(type: "text", nullable: true),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    adres = table.Column<string>(type: "text", nullable: true),
                    resim = table.Column<string>(type: "text", nullable: true),
                    dogrulama_kodu = table.Column<int>(type: "integer", nullable: false),
                    olusturan = table.Column<string>(type: "text", nullable: true),
                    gonderildi = table.Column<bool>(type: "boolean", nullable: false),
                    gonderilme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    olusturulma_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    guncelleme_tarihi = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cicekler", x => x.id);
                    table.ForeignKey(
                        name: "fk_cicekler_ajandalar_ajanda_id",
                        column: x => x.ajanda_id,
                        principalTable: "ajandalar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dosyalar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ad = table.Column<string>(type: "text", nullable: false),
                    aciklama = table.Column<string>(type: "text", nullable: true),
                    path = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "text", nullable: true),
                    size = table.Column<long>(type: "bigint", nullable: true),
                    olusturma_tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    randevu_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dosyalar", x => x.id);
                    table.ForeignKey(
                        name: "fk_dosyalar_randevular_randevu_id",
                        column: x => x.randevu_id,
                        principalTable: "randevular",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notlar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    tip = table.Column<string>(type: "text", nullable: true),
                    not = table.Column<string>(type: "text", nullable: false),
                    randevu_id = table.Column<long>(type: "bigint", nullable: true),
                    tarih = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    olusturan = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notlar", x => x.id);
                    table.ForeignKey(
                        name: "fk_notlar_randevular_randevu_id",
                        column: x => x.randevu_id,
                        principalTable: "randevular",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "randevu_hareketler",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false),
                    kullanici = table.Column<string>(type: "varchar(100)", nullable: false),
                    eski_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    eski_birim = table.Column<string>(type: "varchar(100)", nullable: false),
                    yeni_birim_id = table.Column<long>(type: "bigint", nullable: false),
                    yeni_birim = table.Column<string>(type: "varchar(100)", nullable: false),
                    asagi_hareket = table.Column<bool>(type: "boolean", nullable: false),
                    tarih = table.Column<string>(type: "varchar(100)", nullable: false),
                    randevu_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_randevu_hareketler", x => x.id);
                    table.ForeignKey(
                        name: "fk_randevu_hareketler_randevular_randevu_id",
                        column: x => x.randevu_id,
                        principalTable: "randevular",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_hareketler_ajanda_id",
                table: "ajanda_hareketler",
                column: "ajanda_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_notlar_ajanda_id",
                table: "ajanda_notlar",
                column: "ajanda_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_photos_ajanda_id",
                table: "ajanda_photos",
                column: "ajanda_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajanda_tekrarlar_ajanda_id",
                table: "ajanda_tekrarlar",
                column: "ajanda_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajandalar_birim_id",
                table: "ajandalar",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajandalar_durum_id",
                table: "ajandalar",
                column: "durum_id");

            migrationBuilder.CreateIndex(
                name: "ix_ajandalar_randevu_tip_id",
                table: "ajandalar",
                column: "randevu_tip_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_birim_id",
                table: "AspNetUsers",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_birimler_ust_birim_id",
                table: "birimler",
                column: "ust_birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_cicekler_ajanda_id",
                table: "cicekler",
                column: "ajanda_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cicekler_cicekci_id",
                table: "cicekler",
                column: "cicekci_id");

            migrationBuilder.CreateIndex(
                name: "ix_dosyalar_randevu_id",
                table: "dosyalar",
                column: "randevu_id");

            migrationBuilder.CreateIndex(
                name: "ix_notlar_randevu_id",
                table: "notlar",
                column: "randevu_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevu_hareketler_eski_birim_id",
                table: "randevu_hareketler",
                column: "eski_birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevu_hareketler_kullanici_id",
                table: "randevu_hareketler",
                column: "kullanici_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevu_hareketler_randevu_id",
                table: "randevu_hareketler",
                column: "randevu_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevu_hareketler_yeni_birim_id",
                table: "randevu_hareketler",
                column: "yeni_birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevular_ad_soyad_meslek_email_telefon",
                table: "randevular",
                columns: new[] { "ad", "soyad", "meslek", "email", "telefon" });

            migrationBuilder.CreateIndex(
                name: "ix_randevular_baslangic_tarih",
                table: "randevular",
                column: "baslangic_tarih");

            migrationBuilder.CreateIndex(
                name: "ix_randevular_birim_id",
                table: "randevular",
                column: "birim_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevular_bitis_tarih",
                table: "randevular",
                column: "bitis_tarih");

            migrationBuilder.CreateIndex(
                name: "ix_randevular_mahalle_id",
                table: "randevular",
                column: "mahalle_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevular_randevu_durum_id",
                table: "randevular",
                column: "randevu_durum_id");

            migrationBuilder.CreateIndex(
                name: "ix_randevular_randevu_tip_id",
                table: "randevular",
                column: "randevu_tip_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ajanda_hareketler");

            migrationBuilder.DropTable(
                name: "ajanda_notlar");

            migrationBuilder.DropTable(
                name: "ajanda_photos");

            migrationBuilder.DropTable(
                name: "ajanda_tekrarlar");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "cicekciler");

            migrationBuilder.DropTable(
                name: "cicekler");

            migrationBuilder.DropTable(
                name: "dosyalar");

            migrationBuilder.DropTable(
                name: "icmesuyu_analizleri");

            migrationBuilder.DropTable(
                name: "meslekler");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "notlar");

            migrationBuilder.DropTable(
                name: "randevu_hareketler");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ajandalar");

            migrationBuilder.DropTable(
                name: "randevular");

            migrationBuilder.DropTable(
                name: "ajanda_durumlar");

            migrationBuilder.DropTable(
                name: "birimler");

            migrationBuilder.DropTable(
                name: "mahalleler");

            migrationBuilder.DropTable(
                name: "randevu_durumlar");

            migrationBuilder.DropTable(
                name: "randevu_tipleri");
        }
    }
}
