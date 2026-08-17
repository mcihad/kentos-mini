using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Models.Sibeski;
using KentOS.Mini.Application.Services;

namespace KentOS.Mini.Web.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, long>
    {
        public DbSet<RandevuDurum> RandevuDurumlar { get; set; }
        public DbSet<RandevuTip> RandevuTipleri { get; set; }
        public DbSet<Birim> Birimler { get; set; }
        //public DbSet<User> Users { get; set; }
        public DbSet<Meslek> Meslekler { get; set; }
        public DbSet<Randevu> Randevular { get; set; }
        public DbSet<RandevuNot> Notlar { get; set; }
        public DbSet<RandevuDosya> Dosyalar { get; set; }
        public DbSet<Mahalle> Mahalleler { get; set; }
        public DbSet<RandevuHareket> RandevuHareketler { get; set; }
        public DbSet<Ajanda> Ajandalar { get; set; }
        public DbSet<AjandaTekrar> AjandaTekrarlar { get; set; }
        /// <summary>
        /// Tekrar serileri (RRULE). Tekrarların kendisi <see cref="Ajandalar"/>
        /// içinde gerçek satır olarak durur; burada yalnızca kural saklanır.
        /// </summary>
        public DbSet<AjandaSeri> AjandaSeriler { get; set; }
        /// <summary>Gizli etkinliklerin katılımcıları (gizli olmayanlarda boş).</summary>
        public DbSet<AjandaKatilimci> AjandaKatilimcilar { get; set; }
        public DbSet<SistemHatasi> SistemHatalari { get; set; }
        public DbSet<AjandaDurum> AjandaDurumlar { get; set; }
        public DbSet<AjandaPhoto> AjandaPhotos { get; set; }
        public DbSet<AjandaNot> AjandaNotlar { get; set; }
        public DbSet<Cicek> Cicekler { get; set; }
        public DbSet<Cicekci> Cicekciler { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<KullaniciOturumKaydi> OturumKayitlari { get; set; }
        public DbSet<Protokol> Protokoller { get; set; }
        public DbSet<ProtokolKategori> ProtokolKategorileri { get; set; }
        public DbSet<Davet> Davetler { get; set; }

        /// <summary>
        /// Kurum bilgileri — TEK SATIR. Kurum adı, marka renkleri ve amblem
        /// koda değil buraya yazılır; ilk satır <c>.env</c> değerlerinden
        /// tohumlanır (<c>KurumTohumu</c>).
        /// </summary>
        public DbSet<Institution> KurumBilgileri { get; set; }

        // ── Halk Günü ──
        public DbSet<HalkGunu> HalkGunleri { get; set; }
        public DbSet<HalkGunuDilim> HalkGunuDilimleri { get; set; }
        public DbSet<HalkGunuBasvuru> HalkGunuBasvurulari { get; set; }
        public DbSet<HalkGunuKatilim> HalkGunuKatilimlari { get; set; }
        public DbSet<Ozgecmis> Ozgecmisler { get; set; }
        public DbSet<OzgecmisPaylasim> OzgecmisPaylasimlari { get; set; }
        public DbSet<DavetKisi> DavetKisileri { get; set; }
        public DbSet<DosyaGonderimi> DosyaGonderimleri { get; set; }
        public DbSet<DosyaGonderimiNotu> DosyaGonderimiNotlari { get; set; }
        public DbSet<AjandaHareket> AjandaHareketler { get; set; }
        /// Etkinlik zaman çizelgesi (append-only). Yeni tablo; mevcut hiçbir
        /// tabloyla ilişkisi AjandaId dışında değişmez.
        public DbSet<AjandaOlay> AjandaOlaylar { get; set; }
        public DbSet<Oneri> Oneriler { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }

        /// <summary>İzin kataloğu — açılışta koddan tohumlanır.</summary>
        public DbSet<Izin> Izinler { get; set; }

        /// <summary>Rol ↔ izin bağları — yönetim ekranından düzenlenir.</summary>
        public DbSet<RolIzin> RolIzinleri { get; set; }
        //sibeski
        public DbSet<IcmesuyuAnaliz> IcmesuyuAnalizleri { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            //execute raw sql
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            //builder.HasPostgresExtension("uuid-ossp");
            builder.UseSerialColumns();

            // ── İzin kataloğu ve rol bağları ──
            builder.Entity<Izin>(entity =>
            {
                // Anahtar AD: kod sabitiyle aynı metin. Sayısal kimlik
                // kullanmak, katalog yeniden tohumlandığında kimliklerin
                // kayması ve rol bağlarının yanlış izne düşmesi riskini
                // taşırdı.
                entity.HasKey(e => e.Ad);
                entity.Property(e => e.Ad).HasMaxLength(80);
                entity.Property(e => e.Grup).HasMaxLength(60).IsRequired();
                entity.Property(e => e.Baslik).HasMaxLength(120).IsRequired();
                entity.Property(e => e.Aciklama).HasMaxLength(600).IsRequired();
                entity.HasIndex(e => new { e.Grup, e.SiraNo });
            });

            builder.Entity<RolIzin>(entity =>
            {
                entity.HasKey(e => new { e.RolId, e.IzinAd });
                entity.Property(e => e.IzinAd).HasMaxLength(80);

                entity.HasOne(e => e.Izin)
                    .WithMany()
                    .HasForeignKey(e => e.IzinAd)
                    .OnDelete(DeleteBehavior.Cascade);

                // Rol silinince bağları da gitsin; yetimsiz kalan satır
                // yönetim ekranında "bilinmeyen rol" olarak görünürdü.
                entity.HasOne<AppRole>()
                    .WithMany()
                    .HasForeignKey(e => e.RolId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProtokolKategori>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ad).HasMaxLength(100).IsRequired();

                // Aynı ad iki kez açılamaz — kategoriyi tabloya almanın sebebi
                // zaten yazım çeşitlenmesini durdurmaktı.
                entity.HasIndex(e => e.Ad).IsUnique();
                entity.HasIndex(e => e.SiraNo);
            });

            builder.Entity<Davet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Baslik).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Yer).HasMaxLength(200);
                entity.Property(e => e.Aciklama).HasMaxLength(1000);
                entity.Property(e => e.KullaniciId).HasMaxLength(100);

                entity.HasOne(e => e.Ajanda).WithMany()
                    .HasForeignKey(e => e.AjandaId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Birim).WithMany()
                    .HasForeignKey(e => e.BirimId).OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => new { e.BirimId, e.Tarih });
            });

            builder.Entity<DavetKisi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Not).HasMaxLength(500);

                entity.HasOne(e => e.Davet).WithMany(d => d.Kisiler)
                    .HasForeignKey(e => e.DavetId).OnDelete(DeleteBehavior.Cascade);

                // Protokol kaydı silinirse davet satırı da gider: kişisi
                // olmayan bir davet satırı listede "boş" görünürdü.
                entity.HasOne(e => e.Protokol).WithMany()
                    .HasForeignKey(e => e.ProtokolId).OnDelete(DeleteBehavior.Cascade);

                // Aynı kişi bir davete iki kez eklenemez.
                entity.HasIndex(e => new { e.DavetId, e.ProtokolId }).IsUnique();
            });

            // ══════════════════════════════════════════ Kurum bilgileri
            //
            // TEK SATIRLIK tablo: kimlik `Institution.TekilId` (1) olarak
            // ELLE verilir. Otomatik artan bırakılsaydı ikinci bir satır açmak
            // mümkün olur ve "hangisi geçerli?" sorusu doğardı.
            builder.Entity<Institution>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            // ══════════════════════════════════════════════ Halk Günü
            builder.Entity<HalkGunu>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Baslik).HasMaxLength(200);
                entity.Property(e => e.Konum).HasMaxLength(200);
                entity.Property(e => e.KullaniciId).HasMaxLength(100);

                entity.HasOne(e => e.Birim).WithMany()
                    .HasForeignKey(e => e.BirimId).OnDelete(DeleteBehavior.SetNull);

                // Liste ekranının tek sorgusu: birim + tarih aralığı.
                entity.HasIndex(e => new { e.BirimId, e.Tarih });
            });

            builder.Entity<HalkGunuDilim>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Baslik).HasMaxLength(200);

                entity.HasOne(e => e.HalkGunu).WithMany(h => h.Dilimler)
                    .HasForeignKey(e => e.HalkGunuId).OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.HalkGunuId, e.SiraNo });
            });

            builder.Entity<HalkGunuBasvuru>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Ad).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Soyad).HasMaxLength(100);
                entity.Property(e => e.Telefon).HasMaxLength(50);
                entity.Property(e => e.TelefonSade).HasMaxLength(20);
                entity.Property(e => e.Adres).HasMaxLength(255);
                entity.Property(e => e.Meslek).HasMaxLength(100);
                entity.Property(e => e.Olusturan).HasMaxLength(100);

                entity.HasOne(e => e.Birim).WithMany()
                    .HasForeignKey(e => e.BirimId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Mahalle).WithMany()
                    .HasForeignKey(e => e.MahalleId).OnDelete(DeleteBehavior.Restrict);

                // Talep silinse bile halk günü geçmişi kaybolmamalı.
                entity.HasOne(e => e.Randevu).WithMany()
                    .HasForeignKey(e => e.RandevuId).OnDelete(DeleteBehavior.Restrict);

                // Kişiyi telefondan bulmanın anahtarı — geçmiş sorgusu bunu
                // kullanıyor ve ham `telefon` sütunu karışık biçimde.
                entity.HasIndex(e => e.TelefonSade);
                entity.HasIndex(e => new { e.BirimId, e.Durum });
            });

            builder.Entity<HalkGunuKatilim>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.HalkGunu).WithMany(h => h.Katilimlar)
                    .HasForeignKey(e => e.HalkGunuId).OnDelete(DeleteBehavior.Cascade);

                // Dilim silinince katılım GÜNDE KALIR, yalnızca dilimi boşalır:
                // yanlışlıkla silinen bir saat aralığı yüzünden atanmış on
                // kişinin kaydı yok olmamalı.
                entity.HasOne(e => e.Dilim).WithMany(d => d.Katilimlar)
                    .HasForeignKey(e => e.DilimId).OnDelete(DeleteBehavior.SetNull);

                // Başvuru silinemez: görüşme geçmişi ona bağlı.
                entity.HasOne(e => e.Basvuru).WithMany(b => b.Katilimlar)
                    .HasForeignKey(e => e.BasvuruId).OnDelete(DeleteBehavior.Restrict);

                // Aynı kişi aynı güne iki kez atanamaz.
                entity.HasIndex(e => new { e.HalkGunuId, e.BasvuruId }).IsUnique();
                entity.HasIndex(e => new { e.HalkGunuId, e.DilimId, e.SiraNo });
            });

            // ══════════════════════════════════════════ Özgeçmiş havuzu
            builder.Entity<Ozgecmis>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AdSoyad).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Telefon).HasMaxLength(50);
                entity.Property(e => e.TelefonSade).HasMaxLength(20);
                entity.Property(e => e.Eposta).HasMaxLength(150);
                entity.Property(e => e.MeslekAd).HasMaxLength(150);
                entity.Property(e => e.Adres).HasMaxLength(255);
                entity.Property(e => e.DosyaAdi).HasMaxLength(255).IsRequired();
                entity.Property(e => e.DosyaYolu).HasMaxLength(255).IsRequired();
                entity.Property(e => e.IcerikTuru).HasMaxLength(120);
                entity.Property(e => e.Olusturan).HasMaxLength(100);
                entity.Property(e => e.Guncelleyen).HasMaxLength(100);

                entity.HasOne(e => e.Meslek).WithMany()
                    .HasForeignKey(e => e.MeslekId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.Mahalle).WithMany()
                    .HasForeignKey(e => e.MahalleId).OnDelete(DeleteBehavior.SetNull);

                // Talep silinse bile özgeçmiş havuzda kalır: kişi hâlâ iş
                // arıyor olabilir ve dosya kurumun elindeki tek kopya.
                entity.HasOne(e => e.Randevu).WithMany()
                    .HasForeignKey(e => e.RandevuId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne<Birim>().WithMany()
                    .HasForeignKey(e => e.BirimId).OnDelete(DeleteBehavior.SetNull);

                // Aramanın üç ekseni: kişi (telefon), meslek ve kaynak.
                entity.HasIndex(e => e.TelefonSade);
                entity.HasIndex(e => e.MeslekId);
                entity.HasIndex(e => e.RandevuId);
                entity.HasIndex(e => new { e.IsDeleted, e.OlusturmaTarihi });
            });

            builder.Entity<OzgecmisPaylasim>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PaylasanAd).HasMaxLength(150);
                entity.Property(e => e.AliciAd).HasMaxLength(150);
                entity.Property(e => e.Not).HasMaxLength(500);

                entity.HasOne(e => e.Ozgecmis).WithMany(o => o.Paylasimlar)
                    .HasForeignKey(e => e.OzgecmisId).OnDelete(DeleteBehavior.Cascade);

                // "Bana paylaşılanlar" ekranının tek sorgusu.
                entity.HasIndex(e => new { e.AliciId, e.Tarih });
            });

            builder.Entity<Protokol>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Kategori).WithMany(k => k.Protokoller)
                    .HasForeignKey(e => e.KategoriId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Kurum).HasMaxLength(150);
                entity.Property(e => e.AdSoyad).HasMaxLength(120).IsRequired();
                entity.Property(e => e.Unvan).HasMaxLength(150);
                entity.Property(e => e.Telefon).HasMaxLength(30);
                entity.Property(e => e.CepTelefon).HasMaxLength(30);
                entity.Property(e => e.Eposta).HasMaxLength(150);
                entity.Property(e => e.Adres).HasMaxLength(400);
                entity.Property(e => e.Aciklama).HasMaxLength(500);

                // Liste her zaman sıra + kategori ile okunuyor.
                entity.HasIndex(e => new { e.SiraNo, e.KategoriId });
            });

            builder.Entity<DosyaGonderimi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Konu).HasMaxLength(200).IsRequired();
                entity.Property(e => e.DosyaAdi).HasMaxLength(260).IsRequired();
                entity.Property(e => e.DosyaYolu).HasMaxLength(400).IsRequired();
                entity.Property(e => e.IcerikTuru).HasMaxLength(150);

                entity.HasOne(e => e.Gonderen)
                    .WithMany()
                    .HasForeignKey(e => e.GonderenId)
                    // Kullanıcı silinse bile gönderim kaydı SİLİNMEZ; karşı
                    // taraf gönderilen dosyaya erişmeye devam edebilmeli.
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Alici)
                    .WithMany()
                    .HasForeignKey(e => e.AliciId)
                    .OnDelete(DeleteBehavior.Restrict);

                // "Bana gelenler" ve "gönderdiklerim" listelerinin sorguları.
                entity.HasIndex(e => new { e.AliciId, e.OlusturmaTarihi });
                entity.HasIndex(e => new { e.GonderenId, e.OlusturmaTarihi });
            });

            builder.Entity<DosyaGonderimiNotu>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Metin).HasMaxLength(2000).IsRequired();

                entity.HasOne(e => e.Gonderim)
                    .WithMany(g => g.Notlar)
                    .HasForeignKey(e => e.GonderimId)
                    // Gönderim silinince notları da gider — nota tek başına
                    // erişilecek bir yol yok.
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Yazan)
                    .WithMany()
                    .HasForeignKey(e => e.YazanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.GonderimId, e.OlusturmaTarihi });
            });

            builder.Entity<KullaniciOturumKaydi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.KullaniciAdi).HasMaxLength(64).IsRequired();
                entity.Property(e => e.Aciklama).HasMaxLength(256);
                entity.Property(e => e.IpAdresi).HasMaxLength(64);
                entity.Property(e => e.Istemci).HasMaxLength(256);

                // Denetim listesi her zaman "en yeni önce" okunuyor; indeks
                // olmadan tablo büyüdükçe her açılış tam tarama olurdu.
                entity.HasIndex(e => e.Tarih);
                entity.HasIndex(e => new { e.KullaniciId, e.Tarih });

                // Yabancı anahtar YOK: kullanıcı silinse bile denetim kaydı
                // KALMALI. Denetim izinin silinebilir olması onu değersiz kılar.
            });

            // Bildirim merkezi "okunmamışları" sık sorar.
            builder.Entity<Message>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.Okundu, e.CreatedAt });
            });

            builder.Entity<Birim>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity
                    .HasOne(e => e.UstBirim)
                    .WithMany(e => e.AltBirimler)
                    .HasForeignKey(e => e.UstBirimId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Cicek>(entity =>
            {
                entity.HasIndex(e => e.CicekciId);
            });

            builder.Entity<RandevuHareket>(entity =>
            {
                entity.HasIndex(e => e.RandevuId);
                entity.HasIndex(e => e.EskiBirimId);
                entity.HasIndex(e => e.YeniBirimId);
                entity.HasIndex(e => e.KullaniciId);
            });

            builder.Entity<RandevuNot>(entity =>
            {
                entity.HasIndex(e => e.RandevuId);
            });

            builder.Entity<RandevuDosya>(entity =>
            {
                entity.HasIndex(e => e.RandevuId);
            });



            builder.Entity<Randevu>(entity =>
            {
                entity.HasKey(e => e.Id);
                //add default filter
                entity.HasQueryFilter(e => !e.Arsivlendi);
                entity
                    .HasOne(e => e.Birim)
                    .WithMany(b => b.Randevular)
                    .HasForeignKey(e => e.BirimId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RandevuTip)
                    .WithMany(rt => rt.Randevular)
                    .HasForeignKey(e => e.RandevuTipId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RandevuDurum)
                    .WithMany(rd => rd.Randevular)
                    .HasForeignKey(e => e.RandevuDurumId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Notlar)
                    .WithOne(n => n.Randevu)
                    .HasForeignKey(n => n.RandevuId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Notlar)
                    .WithOne(n => n.Randevu)
                    .HasForeignKey(n => n.RandevuId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Mahalle)
                    .WithMany(m => m.Randevular)
                    .HasForeignKey(e => e.MahalleId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.Dosyalar)
                    .WithOne(d => d.Randevu)
                    .HasForeignKey(d => d.RandevuId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                //index with FirstName LastName Meslek Email Telefon
                entity.HasIndex(e => new { e.Ad, e.Soyad, e.Meslek, e.Email, e.Telefon });
                entity.HasIndex(e => e.BaslangicTarih);
                entity.HasIndex(e => e.BitisTarih);

            });

            builder.Entity<Ajanda>(entity =>
            {
                //hasDefaultQueryFilter
                entity.HasQueryFilter(e => !e.IsDeleted);
                entity.HasMany(e => e.Photos)
                    .WithOne(ap => ap.Ajanda)
                    .HasForeignKey(ap => ap.AjandaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Birim)
                    .WithMany(b => b.Ajandalar)
                    .HasForeignKey(e => e.BirimId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TekrarDetaylari)
                    .WithOne()
                    .HasForeignKey<AjandaTekrar>()
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.BirimId);

                entity.HasOne(e => e.RandevuTip)
                    .WithMany(rt => rt.Ajandalar)
                    .HasForeignKey(e => e.RandevuTipId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Durum)
                    .WithMany(d => d.Ajandalar)
                    .HasForeignKey(e => e.DurumId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.AjandaNotlar)
                    .WithOne(an => an.Ajanda)
                    .HasForeignKey(an => an.AjandaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Cicek)
                    .WithOne(c => c.Ajanda)
                    .HasForeignKey<Cicek>(c => c.AjandaId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Cascade);

                // --- gizli etkinlik katılımcıları
                entity.HasMany(e => e.Katilimcilar)
                    .WithOne(k => k.Ajanda)
                    .HasForeignKey(k => k.AjandaId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // --- tekrar serisi: seri silinirse tekrarlar SATIRDA KALIR (SetNull),
                //     çünkü tekrarlar not/fotoğraf gibi kullanıcı verisi taşıyabilir.
                entity.HasOne(e => e.Seri)
                    .WithMany(s => s.Tekrarlar)
                    .HasForeignKey(e => e.SeriId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.SeriId);
                // Liste/takvim/genişletme sorguları hep (birim + başlangıç) üzerinden gider.
                entity.HasIndex(e => new { e.BirimId, e.BaslangicTarihi });
            });

            builder.Entity<AjandaKatilimci>(entity =>
            {
                // Aynı birim bir etkinliğe iki kez eklenemez.
                //
                // Benzersizlik KISMİ: yalnızca birim taşıyan satırlar için.
                // Eski kişi satırlarında `birim_id` null ve Postgres'te null'lar
                // birbirine eşit sayılmadığından koşulsuz bir indeks de işe
                // yarardı, ama kısmi indeks niyeti açıkça yazıyor.
                entity.HasIndex(e => new { e.AjandaId, e.BirimId })
                    .IsUnique()
                    .HasFilter("birim_id IS NOT NULL");
                entity.HasIndex(e => e.BirimId);

                entity.HasOne(e => e.Birim)
                    .WithMany()
                    .HasForeignKey(e => e.BirimId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ESKİ kişi katılımcıları: yalnızca geçmiş kayıtlar için duruyor.
                // Gezinmesiz FK: AppUser Web derlemesinde, AjandaKatilimci ise
                // Application derlemesinde olduğu için ilişki burada kurulur.
                entity.HasIndex(e => e.KullaniciId);
                entity.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(e => e.KullaniciId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<SistemHatasi>(entity =>
            {
                // Aynı hatayı bulmanın anahtarı; tekilleştirme buradan geçiyor.
                entity.HasIndex(e => e.Parmakizi).IsUnique();
                // Liste "önce en yeni" ve "çözülmemişler" diye süzülüyor.
                entity.HasIndex(e => new { e.Cozuldu, e.SonGorulme });
            });

            builder.Entity<AjandaSeri>(entity =>
            {
                entity.HasIndex(e => e.BirimId);
                // Ufuk uzatma görevi "iptal olmayan + imi geçmiş" serileri tarar.
                entity.HasIndex(e => new { e.Iptal, e.UretilenSonTarih });
            });

            builder.Entity<AjandaPhoto>()
                .HasIndex(e => e.AjandaId);

            // ajanda_tekrarlar.ajanda_id GÖLGE (shadow) özelliktir: AjandaTekrar
            // sınıfında karşılığı olan bir C# property YOK — yalnızca `Ajanda`
            // gezinme özelliği var — bu yüzden [Column] ile sabitlenemiyor.
            //
            // Adı gezinme özelliğinden türetiliyor: `Ajanda` → `AjandaId` →
            // `ajanda_id`. Sınıf ya da gezinme özelliği İngilizce'ye çevrilince
            // ad sessizce `agenda_id` olur ve EF canlı tabloda BULUNMAYAN bir
            // kolonu arar. Diğer bütün kolonlar entity üzerinde [Column] ile
            // donduruldu; attribute'un ulaşamadığı tek yer burası.
            builder.Entity<AjandaTekrar>()
                .Property<long>("AjandaId")
                .HasColumnName("ajanda_id");
                

        }
    }
}
