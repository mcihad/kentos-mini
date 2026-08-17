using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Models;

namespace KentOS.Mini.Web.Data
{
    /// <summary>
    /// Yerel geliştirme ve uçtan uca test verisi.
    ///
    /// <para>
    /// YALNIZCA <c>Development</c> ortamında ve yalnızca ajanda tablosu BOŞSA
    /// çalışır. <see cref="DataSeeder"/>'dan ayrı tutulmasının sebebi budur:
    /// o, üretim dahil her açılışta koşulsuz çalışıyor ve oraya konulan her
    /// kayıt canlı veritabanına düşerdi.
    /// </para>
    ///
    /// <para>
    /// Tekrar eden etkinlikler <see cref="RRuleGenisletici"/> ile — yani
    /// uygulamanın GERÇEK genişletme motoruyla — üretilir. Elle uydurulmuş
    /// tarihler, testleri gerçekte olamayacak bir veri üzerinde yeşile
    /// boyardı.
    /// </para>
    ///
    /// <para>
    /// Değişmezlere uyulur: <c>Gizli</c> etkinlik asla <c>BasinKatilsin</c>
    /// olmaz ve mutlaka en az bir katılımcısı vardır; seri tekrarları
    /// <c>SeriId</c> + <c>SeriOrijinalBaslangic</c> taşır; bireysel düzenlenmiş
    /// istisna <c>SeriAyrik = true</c> olur.
    /// </para>
    /// </summary>
    public static class GelistirmeTohumu
    {
        /// <summary>Üretilecek etkinlik penceresi: bugünden geriye ve ileriye.</summary>
        private static readonly TimeSpan Gecmis = TimeSpan.FromDays(180);
        private static readonly TimeSpan Gelecek = TimeSpan.FromDays(180);

        /// <summary>Aynı veriyi her koşuda üretmek için sabit tohum.</summary>
        private const int RastgeleTohumu = 58;

        public static async Task UygulaAsync(IServiceProvider saglayici, ILogger logger)
        {
            using var kapsam = saglayici.CreateScope();
            var db = kapsam.ServiceProvider.GetRequiredService<AppDbContext>();

            // Yeniden çalıştırmada veriyi ikiye katlamamak için tek kapı.
            if (await db.Ajandalar.IgnoreQueryFilters().AnyAsync())
            {
                logger.LogInformation("Geliştirme tohumu atlandı — ajanda tablosu dolu.");
                return;
            }

            logger.LogInformation("Geliştirme tohumu başlıyor…");

            var rastgele = new Random(RastgeleTohumu);
            var birimler = await BirimleriUretAsync(db);
            var kullanicilar = await KullanicilariUretAsync(kapsam.ServiceProvider, birimler);
            await ReferansVeriUretAsync(db, rastgele);

            var durumlar = await db.AjandaDurumlar.AsNoTracking().ToListAsync();
            var tipler = await db.RandevuTipleri.AsNoTracking().ToListAsync();

            await TekSeferlikEtkinlikUretAsync(db, rastgele, birimler, kullanicilar, durumlar, tipler);
            await TekrarliEtkinlikUretAsync(db, rastgele, birimler, kullanicilar, durumlar, tipler);
            await GizliEtkinlikUretAsync(db, rastgele, birimler, kullanicilar, durumlar, tipler);
            await TalepUretAsync(db, rastgele, birimler, kullanicilar);

            var toplam = await db.Ajandalar.IgnoreQueryFilters().CountAsync();
            var gizli = await db.Ajandalar.IgnoreQueryFilters().CountAsync(a => a.Gizli);
            var seri = await db.Ajandalar.IgnoreQueryFilters().CountAsync(a => a.SeriId != null);
            var talep = await db.Randevular.IgnoreQueryFilters().CountAsync();
            logger.LogInformation(
                "Geliştirme tohumu bitti — {Toplam} etkinlik ({Seri} tekrar, {Gizli} gizli), {Talep} talep.",
                toplam, seri, gizli, talep);
        }

        // ----------------------------------------------------------- birimler

        private static async Task<List<Birim>> BirimleriUretAsync(AppDbContext db)
        {
            var mevcut = await db.Birimler.ToListAsync();
            var kok = mevcut.FirstOrDefault(b => b.UstBirimId == null);
            if (kok == null)
            {
                return mevcut;
            }

            if (mevcut.Count > 1)
            {
                return mevcut;
            }

            string[] adlar =
            [
                "Özel Kalem Müdürlüğü",
                "Basın Yayın ve Halkla İlişkiler Müdürlüğü",
                "Fen İşleri Müdürlüğü",
                "Park ve Bahçeler Müdürlüğü",
                "Zabıta Müdürlüğü",
                "Kültür ve Sosyal İşler Müdürlüğü",
                "Mali Hizmetler Müdürlüğü",
                "Bilgi İşlem Müdürlüğü",
            ];

            foreach (var ad in adlar)
            {
                db.Birimler.Add(new Birim
                {
                    Ad = ad,
                    Yetkili = $"{ad.Split(' ')[0]} Yetkilisi",
                    Unvan = "Müdür",
                    UstBirimId = kok.Id,
                    Level = 1,
                });
            }

            await db.SaveChangesAsync();
            return await db.Birimler.ToListAsync();
        }

        // -------------------------------------------------------- kullanıcılar

        private static async Task<List<AppUser>> KullanicilariUretAsync(
            IServiceProvider saglayici, List<Birim> birimler)
        {
            var userManager = saglayici.GetRequiredService<UserManager<AppUser>>();

            // (kullanıcı adı, ad, soyad, unvan, rol)
            (string K, string A, string S, string U, string R)[] kisiler =
            [
                ("sekreter",  "Ayşe",    "Yılmaz",   "Özel Kalem Sekreteri", UserRoles.Sekreter),
                ("yonetici",  "Mehmet",  "Demir",    "Müdür",                UserRoles.Yonetici),
                ("baskan",    "Ahmet",   "Kaya",     "Belediye Başkanı",     UserRoles.Baskan),
                ("basin",     "Elif",    "Şahin",    "Basın Danışmanı",      UserRoles.Basin),
                ("medya",     "Burak",   "Çelik",    "Medya Sorumlusu",      UserRoles.Medya),
                ("cicek",     "Zeynep",  "Arslan",   "Protokol Görevlisi",   UserRoles.Cicek),
                ("kullanici", "Hasan",   "Doğan",    "Memur",                UserRoles.Kullanici),
                ("kalem1",    "Fatma",   "Aydın",    "Kalem Görevlisi",      UserRoles.Sekreter),
                ("kalem2",    "Emre",    "Koç",      "Kalem Görevlisi",      UserRoles.Sekreter),
                ("mudur1",    "Selin",   "Öztürk",   "Müdür",                UserRoles.Yonetici),
            ];

            var ozelKalem = birimler.FirstOrDefault(b => b.Ad.StartsWith("Özel Kalem"))
                            ?? birimler[0];

            foreach (var (k, a, s, u, r) in kisiler)
            {
                if (await userManager.FindByNameAsync(k) != null) continue;

                var kullanici = new AppUser
                {
                    UserName = k,
                    Email = $"{k}@ornek.local",
                    EmailConfirmed = true,
                    Ad = a,
                    Soyad = s,
                    Unvan = u,
                    BirimId = ozelKalem.Id,
                };

                var sonuc = await userManager.CreateAsync(kullanici, "Gelistirme123.");
                if (sonuc.Succeeded)
                {
                    await userManager.AddToRoleAsync(kullanici, r);
                }
            }

            return await userManager.Users.ToListAsync();
        }

        // ------------------------------------------------------- referans veri

        private static async Task ReferansVeriUretAsync(AppDbContext db, Random rastgele)
        {
            if (!await db.Mahalleler.AnyAsync())
            {
                string[] mahalleler =
                [
                    "Akdeğirmen", "Bahtiyarbostan", "Çayyurt", "Demircilerardı",
                    "Esentepe", "Ferhatbostan", "Gültepe", "Halilbostan",
                    "İnönü", "Kadıburhanettin", "Mehmet Akif Ersoy", "Yenişehir",
                ];
                db.Mahalleler.AddRange(mahalleler.Select(m => new Mahalle { Ad = m }));
            }

            if (!await db.Meslekler.AnyAsync())
            {
                string[] meslekler =
                [
                    "Öğretmen", "Doktor", "Mühendis", "Esnaf", "Çiftçi",
                    "Emekli", "Öğrenci", "Avukat", "Muhtar", "İşçi",
                ];
                db.Meslekler.AddRange(meslekler.Select(m => new Meslek { Ad = m }));
            }

            if (!await db.Cicekciler.AnyAsync())
            {
                db.Cicekciler.AddRange(
                    new Cicekci
                    {
                        AdSoyad = "Gül Çiçekçilik",
                        Telefon = "3462210101",
                        Adres = "İstasyon Caddesi No:12",
                        Aktif = true,
                    },
                    new Cicekci
                    {
                        AdSoyad = "Lale Çiçek Evi",
                        Telefon = "3462210202",
                        Adres = "Atatürk Bulvarı No:45",
                        Aktif = true,
                    });
            }

            await db.SaveChangesAsync();
        }

        // --------------------------------------------------- tek seferlik etkinlik

        private static async Task TekSeferlikEtkinlikUretAsync(
            AppDbContext db, Random rastgele, List<Birim> birimler,
            List<AppUser> kullanicilar, List<AjandaDurum> durumlar, List<RandevuTip> tipler)
        {
            string[] basliklar =
            [
                "Muhtarlar Toplantısı", "Okul Ziyareti", "Basın Açıklaması",
                "Yatırım İnceleme Gezisi", "Vatandaş Kabulü", "Açılış Töreni",
                "Kurum Ziyareti", "Nikâh Töreni", "Sanayi Sitesi İncelemesi",
                "Şehit Ailesi Ziyareti", "Spor Kulübü Kabulü", "Dernek Heyeti Kabulü",
                "Altyapı Çalışması İncelemesi", "Kültür Merkezi Ziyareti",
                "Huzurevi Ziyareti", "Pazar Yeri Denetimi",
            ];
            string[] konumlar =
            [
                "MAKAM", "Belediye Konferans Salonu", "Kültür Merkezi",
                "Şehir Meydanı", "Sanayi Sitesi", "Yenişehir Mahallesi",
            ];

            var bugun = DateTime.Now.Date;
            var liste = new List<Ajanda>();

            for (var i = 0; i < 240; i++)
            {
                var gunFarki = rastgele.Next(-(int)Gecmis.TotalDays, (int)Gelecek.TotalDays);
                var baslangic = bugun.AddDays(gunFarki)
                                     .AddHours(rastgele.Next(8, 18))
                                     .AddMinutes(rastgele.Next(0, 2) * 30);
                var sureDk = new[] { 30, 30, 60, 60, 90, 120 }[rastgele.Next(6)];
                var gecmisMi = baslangic < DateTime.Now;

                liste.Add(new Ajanda
                {
                    Baslik = $"{basliklar[rastgele.Next(basliklar.Length)]} ({i + 1})",
                    Aciklama = "Geliştirme tohumuyla üretilmiş örnek kayıt.",
                    Konum = konumlar[rastgele.Next(konumlar.Length)],
                    BaslangicTarihi = baslangic,
                    BitisTarihi = baslangic.AddMinutes(sureDk),
                    TumGun = false,
                    BasinKatilsin = rastgele.Next(4) == 0,
                    OlusturmaTarihi = baslangic.AddDays(-rastgele.Next(1, 20)),
                    KullaniciId = kullanicilar[rastgele.Next(kullanicilar.Count)].UserName,
                    BirimId = birimler[rastgele.Next(birimler.Count)].Id,
                    RandevuTipId = tipler[rastgele.Next(tipler.Count)].Id,
                    DurumId = durumlar[rastgele.Next(durumlar.Count)].Id,
                    // Geçmiş etkinliklerin çoğu tamamlanmış, azı iptal.
                    Status = gecmisMi
                        ? (rastgele.Next(10) == 0 ? AjandaStatus.Canceled : AjandaStatus.Completed)
                        : AjandaStatus.Pending,
                });
            }

            db.Ajandalar.AddRange(liste);
            await db.SaveChangesAsync();
        }

        // -------------------------------------------------------- tekrarlı seri

        private static async Task TekrarliEtkinlikUretAsync(
            AppDbContext db, Random rastgele, List<Birim> birimler,
            List<AppUser> kullanicilar, List<AjandaDurum> durumlar, List<RandevuTip> tipler)
        {
            var bugun = DateTime.Now.Date;

            // (başlık, rrule, başlangıç saati, süre dk, kaç gün önce başladı)
            (string Baslik, string Rrule, int Saat, int SureDk, int GunOnce)[] seriler =
            [
                ("Haftalık Müdürler Toplantısı", "FREQ=WEEKLY;BYDAY=MO",            9, 90, 120),
                ("Günlük Program Değerlendirme", "FREQ=DAILY;INTERVAL=1;COUNT=40", 17, 30,  30),
                ("İki Haftada Bir Basın Toplantısı", "FREQ=WEEKLY;INTERVAL=2;BYDAY=TH", 14, 60, 90),
                ("Aylık Meclis Toplantısı",      "FREQ=MONTHLY;BYMONTHDAY=5",      10,120, 150),
                ("Ayın Son Cuması Denetim",      "FREQ=MONTHLY;BYDAY=-1FR",        15, 60, 150),
                ("Yıllık Kuruluş Yıldönümü",     "FREQ=YEARLY",                    11,180, 400),
            ];

            foreach (var (baslik, rrule, saat, sureDk, gunOnce) in seriler)
            {
                var dtstart = bugun.AddDays(-gunOnce).AddHours(saat);
                var ufuk = DateTime.Now.AddMonths(18);

                var seri = new AjandaSeri
                {
                    Rrule = rrule,
                    Dtstart = dtstart,
                    SureDakika = sureDk,
                    KullaniciId = kullanicilar[rastgele.Next(kullanicilar.Count)].UserName,
                    BirimId = birimler[0].Id,
                    OlusturmaTarihi = dtstart.AddDays(-3),
                };
                db.AjandaSeriler.Add(seri);
                await db.SaveChangesAsync();

                // Gerçek motorla genişlet — uydurma tarih yok.
                var tarihler = RRuleGenisletici.Genislet(rrule, dtstart, ufuk);

                var tipId = tipler[rastgele.Next(tipler.Count)].Id;
                var durumId = durumlar[rastgele.Next(durumlar.Count)].Id;
                var tekrarlar = new List<Ajanda>();

                for (var i = 0; i < tarihler.Count; i++)
                {
                    var t = tarihler[i];
                    // Her serinin ortasındaki bir tekrarı bireysel düzenlenmiş
                    // (ayrık) yap: seri güncellemeleri onu atlamalı.
                    var ayrik = i == tarihler.Count / 2;

                    tekrarlar.Add(new Ajanda
                    {
                        Baslik = ayrik ? $"{baslik} — yeri değişti" : baslik,
                        Aciklama = "Tekrar eden seri (geliştirme tohumu).",
                        Konum = ayrik ? "Kültür Merkezi" : "MAKAM",
                        BaslangicTarihi = ayrik ? t.AddHours(1) : t,
                        BitisTarihi = (ayrik ? t.AddHours(1) : t).AddMinutes(sureDk),
                        OlusturmaTarihi = seri.OlusturmaTarihi,
                        KullaniciId = seri.KullaniciId,
                        BirimId = seri.BirimId,
                        RandevuTipId = tipId,
                        DurumId = durumId,
                        TekrarEden = true,
                        SeriId = seri.Id,
                        SeriOrijinalBaslangic = t,
                        SeriAyrik = ayrik,
                        Status = t < DateTime.Now ? AjandaStatus.Completed : AjandaStatus.Pending,
                    });
                }

                db.Ajandalar.AddRange(tekrarlar);
                seri.UretilenSonTarih = tarihler.Count > 0 ? tarihler[^1] : null;
                await db.SaveChangesAsync();
            }
        }

        // --------------------------------------------------------- gizli etkinlik

        private static async Task GizliEtkinlikUretAsync(
            AppDbContext db, Random rastgele, List<Birim> birimler,
            List<AppUser> kullanicilar, List<AjandaDurum> durumlar, List<RandevuTip> tipler)
        {
            string[] basliklar =
            [
                "Özel Görüşme", "Kapalı Oturum Değerlendirme", "Gizli Protokol Görüşmesi",
                "Personel Değerlendirme", "Hukuki İstişare", "Yatırımcı Görüşmesi",
            ];

            var bugun = DateTime.Now.Date;
            var liste = new List<Ajanda>();

            for (var i = 0; i < 30; i++)
            {
                var olusturan = kullanicilar[rastgele.Next(kullanicilar.Count)];
                var baslangic = bugun.AddDays(rastgele.Next(-60, 90))
                                     .AddHours(rastgele.Next(9, 17));

                liste.Add(new Ajanda
                {
                    Baslik = $"{basliklar[rastgele.Next(basliklar.Length)]} ({i + 1})",
                    Aciklama = "Gizli etkinlik — yalnızca ekleyen ve katılımcılar görebilir.",
                    Konum = "MAKAM",
                    BaslangicTarihi = baslangic,
                    BitisTarihi = baslangic.AddMinutes(60),
                    OlusturmaTarihi = baslangic.AddDays(-2),
                    // Ajanda.KullaniciId bir KULLANICI ADI metnidir (sayısal id değil).
                    KullaniciId = olusturan.UserName,
                    BirimId = birimler[0].Id,
                    RandevuTipId = tipler[rastgele.Next(tipler.Count)].Id,
                    DurumId = durumlar[rastgele.Next(durumlar.Count)].Id,
                    Gizli = true,
                    // DEĞİŞMEZ: gizli etkinlikte basın katılamaz.
                    BasinKatilsin = false,
                    Status = baslangic < DateTime.Now ? AjandaStatus.Completed : AjandaStatus.Pending,
                });
            }

            db.Ajandalar.AddRange(liste);
            await db.SaveChangesAsync();

            // Katılımcılar: AjandaKatilimci.KullaniciId SAYISAL AspNetUsers.Id'dir.
            foreach (var etkinlik in liste)
            {
                var olusturanAdi = etkinlik.KullaniciId;
                var adaylar = kullanicilar
                    .Where(k => k.UserName != olusturanAdi)
                    .OrderBy(_ => rastgele.Next())
                    .Take(rastgele.Next(2, 5))
                    .ToList();

                foreach (var k in adaylar)
                {
                    db.AjandaKatilimcilar.Add(new AjandaKatilimci
                    {
                        AjandaId = etkinlik.Id,
                        KullaniciId = k.Id,
                        OlusturmaTarihi = etkinlik.OlusturmaTarihi,
                    });
                }
            }

            await db.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- talep

        private static async Task TalepUretAsync(
            AppDbContext db, Random rastgele, List<Birim> birimler, List<AppUser> kullanicilar)
        {
            var tipler = await db.RandevuTipleri.AsNoTracking().ToListAsync();
            var durumlar = await db.RandevuDurumlar.AsNoTracking().ToListAsync();
            var mahalleler = await db.Mahalleler.AsNoTracking().ToListAsync();
            var meslekler = await db.Meslekler.AsNoTracking().ToListAsync();

            string[] adlar = ["Ali", "Ayşe", "Mustafa", "Fatma", "Hüseyin", "Emine", "Osman", "Hatice", "İbrahim", "Zeynep"];
            string[] soyadlar = ["Yıldız", "Aksoy", "Bulut", "Erdem", "Kurt", "Şimşek", "Polat", "Taş", "Güneş", "Kılıç"];
            string[] konular =
            [
                "Yol asfalt talebi", "Park alanı düzenlemesi", "Su kesintisi şikâyeti",
                "İş başvurusu görüşmesi", "Sosyal yardım talebi", "İmar durumu sorusu",
                "Aydınlatma direği talebi", "Çöp konteyneri talebi", "Gürültü şikâyeti",
                "Muhtarlık işbirliği", "Öğrenci bursu talebi", "Kaldırım onarımı",
            ];

            var bugun = DateTime.Now.Date;
            var liste = new List<Randevu>();

            for (var i = 0; i < 150; i++)
            {
                var baslangic = bugun.AddDays(rastgele.Next(-150, 60))
                                     .AddHours(rastgele.Next(9, 17));
                var arsiv = rastgele.Next(6) == 0;

                liste.Add(new Randevu
                {
                    Konu = konular[rastgele.Next(konular.Length)],
                    Ad = adlar[rastgele.Next(adlar.Length)],
                    Soyad = soyadlar[rastgele.Next(soyadlar.Length)],
                    Meslek = meslekler.Count > 0 ? meslekler[rastgele.Next(meslekler.Count)].Ad : null,
                    Telefon = $"05{rastgele.Next(10, 60)}{rastgele.Next(1000000, 9999999)}",
                    Email = $"vatandas{i}@ornek.com",
                    Adres = "Merkez",
                    Yer = "MAKAM",
                    BaslangicTarih = baslangic,
                    BitisTarih = baslangic.AddMinutes(30),
                    Aciklama = "Geliştirme tohumuyla üretilmiş örnek talep.",
                    BirimId = birimler[rastgele.Next(birimler.Count)].Id,
                    RandevuTipId = tipler.Count > 0 ? tipler[rastgele.Next(tipler.Count)].Id : null,
                    RandevuDurumId = durumlar.Count > 0 ? durumlar[rastgele.Next(durumlar.Count)].Id : null,
                    MahalleId = mahalleler.Count > 0 ? mahalleler[rastgele.Next(mahalleler.Count)].Id : null,
                    OlusturmaTarih = baslangic.AddDays(-rastgele.Next(1, 15)),
                    Olusturan = kullanicilar[rastgele.Next(kullanicilar.Count)].UserName,
                    Arsivlendi = arsiv,
                });
            }

            db.Randevular.AddRange(liste);
            await db.SaveChangesAsync();
        }
    }
}
