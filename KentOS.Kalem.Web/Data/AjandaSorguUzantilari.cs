using System.Linq;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Data
{
    /// <summary>
    /// Etkinlik sorgularında GİZLİLİK kapısı.
    ///
    /// NEDEN UZANTI, NEDEN GLOBAL FİLTRE DEĞİL: EF global query filter statik bir
    /// ifadedir; <c>AppDbContext</c> yalnızca <c>DbContextOptions</c> alıyor, yani
    /// oturum bilgisine (kullanıcı kimliği) erişemez. Ayrıca kod tabanında
    /// <c>IgnoreQueryFilters()</c> kullanan yerler var (silinmiş kayıt listesi,
    /// istatistik) — global filtreye konsa gizlilik oralarda SESSİZCE devre dışı
    /// kalırdı. Açık <c>Where</c> ise <c>IgnoreQueryFilters()</c> ile atlanamaz.
    /// </summary>
    public static class AjandaSorguUzantilari
    {
        /// <summary>
        /// Yalnızca çağıran kullanıcının görmeye yetkili olduğu etkinlikler.
        ///
        /// Kural: gizli olmayan her etkinlik görünür; gizli etkinliği ise
        /// <b>ekleyen kişi</b> ve <b>katılımcılar</b> görür. Rol ayrıcalığı YOKTUR —
        /// Admin/Başkan dahil kimse başkasının gizli etkinliğini görmez.
        /// </summary>
        /// <param name="kullaniciId">AspNetUsers.Id — katılımcı eşleşmesi için.</param>
        /// <param name="kullaniciAdi">
        /// Kullanıcı adı — <c>Ajanda.KullaniciId</c> alanı sayısal kimlik değil,
        /// kullanıcı ADI tutuyor (bkz. AjandaService.CreateAsync).
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>Katılımcı BİRİM burada geçmez.</b> Katılımcı birim "etkinliğe
        /// katılacak departman" demek; gizli etkinliği görme yetkisi ise ayrı
        /// bir liste (<c>AjandaKatilimci.KullaniciId</c>) ve ekleyenin kendi
        /// biriminden seçiliyor. Bir dönem ikisi birbirine bağlanmıştı: gizli
        /// bir toplantıya bir müdürlüğü davet etmek, o müdürlükteki HERKESİ
        /// toplantının içeriğine ortak ediyordu.
        /// </para>
        /// </remarks>
        /// <param name="yalnizcaBasin">
        /// <c>true</c> ise yalnızca <b>basın katılacak</b> etkinlikler döner
        /// (<c>ajanda.basinGoruntule</c> izni). Kapı
        /// <see cref="Application.Services.ICurrentUserService.YalnizcaBasinMiAsync"/>
        /// ile açılır.
        ///
        /// <para>
        /// Parametrenin <b>varsayılanı yok</b>: her çağrı yeri kendi kararını
        /// açıkça yazmak zorunda. Varsayılan <c>false</c> koymak, yeni bir
        /// okuma sorgusu eklendiğinde daraltmanın sessizce atlanması demekti
        /// — basın kullanıcısına makamın bütün gününü gösteren tek bir sorgu
        /// yeter.
        /// </para>
        /// </param>
        public static IQueryable<Ajanda> GorunurOlanlar(
            this IQueryable<Ajanda> sorgu,
            long? kullaniciId,
            string? kullaniciAdi,
            bool yalnizcaBasin)
        {
            if (yalnizcaBasin)
            {
                // Gizli etkinlik basın kapsamına GİRMEZ. Gizli bir kaydın
                // `BasinKatilsin` işaretli olması zaten reddediliyor ama kural
                // burada da yazılı: daraltma, gizlilik kuralının önüne
                // geçemez.
                sorgu = sorgu.Where(a => a.BasinKatilsin && !a.Gizli);
            }

            return sorgu.Where(a =>
                !a.Gizli
                || (kullaniciAdi != null && a.KullaniciId == kullaniciAdi)
                || (kullaniciId != null && a.Katilimcilar.Any(k => k.KullaniciId == kullaniciId)));
        }

        /// <summary>
        /// v2 OKUMA sorgularının tek giriş kapısı: <b>birim izolasyonu</b> +
        /// <b>gizlilik</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// GERÇEK HATA: v2 yalnızca <see cref="GorunurOlanlar"/> çağırıyordu,
        /// birim süzgecini atlıyordu. Sonuç: yeni web arayüzü <b>başka
        /// birimlerin etkinliklerini</b> listeliyordu — eski arayüzde ve
        /// mobilde görünmeyen kayıtlar takvimde çıkıyordu. İki kural ayrı ayrı
        /// çağrılabildiği sürece biri unutulmaya devam eder; bu yüzden tek
        /// metotta birleştirildi ve v2'nin her etkinlik okuması buradan geçer.
        /// </para>
        /// <para>
        /// İki koşul <b>VE</b> ile birleşir, tıpkı v1'de olduğu gibi
        /// (<c>AjandaService.GetAllAsync</c>, <c>GetByIdAsync</c>): kullanıcı
        /// kendi biriminin etkinliklerini görür, gizli olanlardan ise yalnızca
        /// oluşturduğu ya da katılımcısı olduklarını.
        /// </para>
        /// <para>
        /// <b>ÇAĞRILAN BİRİM DE GÖRÜR</b> — ama yalnızca birim süzgecinden.
        /// Fen İşleri'ni başkanlık toplantısına çağırıp Fen İşleri'nin o
        /// toplantıyı görememesi, davetin hiçbir işe yaramaması demekti.
        /// </para>
        /// <para>
        /// <b>GİZLİLİK BUNUN ÜSTÜNDE.</b> İki koşul VE ile bağlı: davet edilen
        /// birim gizli bir etkinliği yine göremez, çünkü gizli etkinliğin
        /// görünürlüğü ayrı bir kişi listesinden geliyor. Davet etmek ile
        /// "içeriği görebilsin" demek aynı şey değil.
        /// </para>
        /// </remarks>
        /// <param name="birimId">Oturum sahibinin birimi. 0 ise hiçbir kayıt dönmez.</param>
        /// <summary>
        /// Kullanıcının biriminin KAPSAMI: kendi birimi <b>veya</b> etkinliğe
        /// katılımcı olarak çağrıldığı etkinlikler.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Çağrılan birim etkinliği KENDİ ajandasında görmeli — davet edilip
        /// toplantıyı görememek, davetin hiçbir işe yaramaması demekti. Liste
        /// sorguları bir dönem yalnızca <c>a.BirimId == birimId</c> diyordu ve
        /// takvim (<c>ErisilebilirOlanlar</c>) çağrılan birimi gösterirken
        /// ajanda listesi göstermiyordu: aynı kullanıcı iki ekranda iki farklı
        /// küme görüyordu.
        /// </para>
        /// <para>
        /// <b>Bu yalnızca GÖRME kapısıdır.</b> Yazma yolları (düzenle, sil,
        /// havale) etkinliğin SAHİBİ birime bağlı kalır: davet edilen birim
        /// başkasının etkinliğini değiştiremez.
        /// </para>
        /// <para>
        /// Gizlilik bunun üstünde: iki kural VE ile bağlanır, çağrılan birim
        /// gizli etkinliği yine göremez.
        /// </para>
        /// </remarks>
        public static IQueryable<Ajanda> BirimKapsami(
            this IQueryable<Ajanda> sorgu, long birimId) =>
            sorgu.Where(a => a.BirimId == birimId
                          || a.Katilimcilar.Any(k => k.BirimId == birimId));

        public static IQueryable<Ajanda> ErisilebilirOlanlar(
            this IQueryable<Ajanda> sorgu,
            long? kullaniciId,
            string? kullaniciAdi,
            long birimId,
            bool yalnizcaBasin)
        {
            return sorgu
                .Where(a => a.BirimId == birimId
                         || a.Katilimcilar.Any(k => k.BirimId == birimId))
                .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin);
        }

        /// <summary>
        /// Gizli etkinliğin bildirim alıcıları: <b>görebilecek kişiler ∪ ekleyen</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>KATILIMCI BİRİMLERE BİLDİRİM GİTMEZ.</b> Bir dönem katılımcı
        /// birimlerin TÜM kullanıcılarına da gönderiliyordu; gizli bir toplantıya
        /// bir müdürlük davet edilince o müdürlükteki herkesin telefonuna
        /// toplantının BAŞLIĞI düşüyor, sonra uygulamada açamıyorlardı.
        /// Görünürlük listesinde olmayan kimseye bildirim gitmemeli — bildirim
        /// metni de bir sızıntı yüzeyi.
        /// </para>
        /// <para>
        /// Bu liste görünürlük kuralının (<see cref="GorunurOlanlar"/>) birebir
        /// karşılığıdır; ikisi ayrışırsa ya bildirim sızar ya da kayıt
        /// görebilen birine hiç haber gitmez.
        /// </para>
        /// </remarks>
        public static async Task<List<long>> GizliAliciIdleriAsync(
            this AppDbContext context,
            long ajandaId,
            string? olusturanKullaniciAdi)
        {
            var alicilar = await context.AjandaKatilimcilar
                .Where(k => k.AjandaId == ajandaId && k.KullaniciId != null)
                .Select(k => k.KullaniciId!.Value)
                .ToListAsync();

            if (!string.IsNullOrEmpty(olusturanKullaniciAdi))
            {
                var olusturanId = await context.Users
                    .Where(u => u.UserName == olusturanKullaniciAdi)
                    .Select(u => (long?)u.Id)
                    .FirstOrDefaultAsync();

                if (olusturanId.HasValue && !alicilar.Contains(olusturanId.Value))
                {
                    alicilar.Add(olusturanId.Value);
                }
            }

            return alicilar;
        }

        /// <summary>
        /// Etkinlik bildiriminin TEK dağıtım noktası — <c>AjandaService</c> ve
        /// <c>AjandaSeriService</c> aynı kuralı kullanır ki gizli etkinlik
        /// bildirimi iki yerde ayrışmasın.
        ///
        /// Gizli etkinlikte: alıcılar katılımcılar ∪ ekleyen; başlık gizlilik
        /// işaretiyle gönderilir. Gizli değilse bugüne kadarki birim davranışı.
        /// </summary>
        public static async Task EtkinlikBildirAsync(
            AppDbContext context,
            IMessageService messageService,
            Ajanda ajanda,
            string baslik,
            string icerik,
            SendMessageType tip,
            NotifikasyonTip notifikasyonTip,
            string? data,
            long? birimId = null)
        {
            if (ajanda.Gizli)
            {
                // Bildirimde de GİZLİ olduğu belli olsun. Emoji yalnızca push'ta:
                // SMS'te emoji mesajı UCS-2'ye çevirip karakter sınırını yarıya düşürür.
                var isaret = tip == SendMessageType.SMS ? "[GİZLİ] " : "🔒 Gizli · ";
                var alicilar = await context.GizliAliciIdleriAsync(ajanda.Id, ajanda.KullaniciId);

                await messageService.CreateForUsersAsync(
                    alicilar,
                    isaret + baslik,
                    icerik + " (Gizli etkinlik — yalnızca görmesine izin verilenler görebilir.)",
                    tip, notifikasyonTip, data);
                return;
            }

            var hedefBirim = birimId ?? ajanda.BirimId ?? 0;
            await messageService.CreateForAllPersonAsync(hedefBirim, baslik, icerik, tip, notifikasyonTip, data);

            // ÇAĞRILAN BİRİMLERE DE HABER GİDER.
            //
            // Toplantıya davet edilen müdürlük etkinliği kendi ajandasında
            // görüyor ama haberi olmuyordu: davet, karşı taraf takvimi açıp
            // fark edene kadar hiçbir şey ifade etmiyordu.
            //
            // GİZLİ ETKİNLİKTE BURAYA HİÇ GELİNMEZ (yukarıdaki dal döner):
            // göremeyecek birine etkinliğin BAŞLIĞINI göndermek, gizliliği
            // bildirim üzerinden delmek demek.
            //
            // Hedef birim ayrıca çağrılmışsa (havalede olduğu gibi) iki kez
            // bildirim gitmesin diye o birim listeden düşülür.
            var katilimciBirimler = await context.AjandaKatilimcilar
                .Where(k => k.AjandaId == ajanda.Id
                            && k.BirimId != null
                            && k.BirimId != hedefBirim)
                .Select(k => k.BirimId!.Value)
                .Distinct()
                .ToListAsync();

            foreach (var kb in katilimciBirimler)
            {
                await messageService.CreateForAllPersonAsync(
                    kb, baslik, icerik, tip, notifikasyonTip, data);
            }
        }

        /// <summary>Tek etkinliğin katılımcı listesi (ad/unvan ile, iletişim bilgisi olmadan).</summary>
        public static async Task<List<KatilimciDto>> KatilimcilariGetirAsync(this AppDbContext context, long ajandaId)
        {
            var harita = await context.KatilimcilariGetirAsync(new[] { ajandaId });
            return harita.TryGetValue(ajandaId, out var liste) ? liste : [];
        }

        /// <summary>
        /// Katılımcı listesini eşitler — <b>tek uygulama noktası</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Aynı iş daha önce <c>AjandaService</c>, <c>AjandaSeriService</c> (iki
        /// yerde) ve <c>AjandaController</c> içinde ayrı ayrı yazılıydı. Dört
        /// kopya, kural değiştiğinde birinin unutulması demek: katılımcılar
        /// birime çevrilirken tam da bu oldu ve derleyici uyarmasa sessiz
        /// kalırdı.
        /// </para>
        /// <para>
        /// <paramref name="birimIdler"/> ve <paramref name="kullaniciIdler"/>
        /// <c>null</c> ise ilgili liste DOKUNULMAZ.
        /// </para>
        /// <para>
        /// <b>KAYNAK DENETİMİ SUNUCUDA.</b> Katılımcı birim yalnızca kullanıcının
        /// kendi seviyesindeki ve altındaki birimlerden, görebilecek kişi ise
        /// yalnızca <b>kendi biriminden</b> seçilebilir. Denetim arayüzde
        /// yapılıyordu; elle kurulmuş bir istek gizli bir etkinliği kurumdaki
        /// herhangi birine açabilirdi.
        /// </para>
        /// <para>
        /// Denetim <b>yalnızca YENİ eklenenlere</b> uygulanır. Kayıtta zaten
        /// duran satırlar geçer: kullanıcının birimi ya da bir birimin
        /// hiyerarşideki yeri sonradan değişince eski bir etkinliği açıp
        /// kaydetmek imkânsız hâle gelirdi.
        /// </para>
        /// </remarks>
        /// <param name="kullanicininBirimId">
        /// Oturum sahibinin birimi — iki listenin de kaynağını sınırlar.
        /// </param>
        public static async Task KatilimcilariEsitleAsync(
            this AppDbContext context,
            long ajandaId,
            bool gizli,
            IEnumerable<long>? birimIdler,
            IEnumerable<long>? kullaniciIdler,
            long kullanicininBirimId)
        {
            var mevcutlar = await context.AjandaKatilimcilar
                .Where(k => k.AjandaId == ajandaId)
                .ToListAsync();

            // ── Birim katılımcılar: gizlilikten BAĞIMSIZ ──
            if (birimIdler != null)
            {
                var istenen = birimIdler.Distinct().ToList();
                await KatilimciBirimleriDogrulaAsync(
                    context, istenen, mevcutlar, kullanicininBirimId);

                foreach (var fazla in mevcutlar
                    .Where(k => k.BirimId != null && !istenen.Contains(k.BirimId.Value)))
                {
                    context.AjandaKatilimcilar.Remove(fazla);
                }

                var varOlan = mevcutlar
                    .Where(k => k.BirimId != null)
                    .Select(k => k.BirimId!.Value)
                    .ToHashSet();

                foreach (var yeni in istenen.Where(id => !varOlan.Contains(id)))
                {
                    context.AjandaKatilimcilar.Add(new AjandaKatilimci
                    {
                        AjandaId = ajandaId,
                        BirimId = yeni,
                        OlusturmaTarihi = DateTime.Now,
                    });
                }
            }

            // ── Eski kişi katılımcılar: gizliliğe bağlı ──
            if (!gizli)
            {
                var kisiler = mevcutlar.Where(k => k.KullaniciId != null).ToList();
                if (kisiler.Count > 0)
                {
                    context.AjandaKatilimcilar.RemoveRange(kisiler);
                }
                return;
            }

            if (kullaniciIdler == null)
            {
                return;
            }

            var istenenKisiler = kullaniciIdler.Distinct().ToList();
            await GorebileceklerDogrulaAsync(
                context, istenenKisiler, mevcutlar, kullanicininBirimId);

            foreach (var fazla in mevcutlar
                .Where(k => k.KullaniciId != null && !istenenKisiler.Contains(k.KullaniciId.Value)))
            {
                context.AjandaKatilimcilar.Remove(fazla);
            }

            var varOlanKisiler = mevcutlar
                .Where(k => k.KullaniciId != null)
                .Select(k => k.KullaniciId!.Value)
                .ToHashSet();

            foreach (var yeni in istenenKisiler.Where(id => !varOlanKisiler.Contains(id)))
            {
                context.AjandaKatilimcilar.Add(new AjandaKatilimci
                {
                    AjandaId = ajandaId,
                    KullaniciId = yeni,
                    OlusturmaTarihi = DateTime.Now,
                });
            }
        }

        /// <summary>
        /// Katılımcı birimler kullanıcının seviyesinde ya da altında mı.
        /// </summary>
        /// <remarks>
        /// Bir müdürlük başkan yardımcısını kendi toplantısına çağıramaz; o
        /// davet yukarıdan gelir. Kendi birimi de listede olamaz — o zaten
        /// toplantının sahibi.
        /// </remarks>
        private static async Task KatilimciBirimleriDogrulaAsync(
            AppDbContext context,
            List<long> istenen,
            List<AjandaKatilimci> mevcutlar,
            long kullanicininBirimId)
        {
            var zatenVar = mevcutlar
                .Where(k => k.BirimId != null)
                .Select(k => k.BirimId!.Value)
                .ToHashSet();

            var yeniler = istenen.Where(id => !zatenVar.Contains(id)).ToList();
            if (yeniler.Count == 0) return;

            var seviye = await context.Birimler
                .Where(b => b.Id == kullanicininBirimId)
                .Select(b => (int?)b.Level)
                .FirstOrDefaultAsync();

            if (seviye is null)
            {
                throw new BusinessRuleException(
                    "Biriminiz çözülemediği için katılımcı birim eklenemez.");
            }

            var izinliler = await context.Birimler
                .Where(b => yeniler.Contains(b.Id)
                         && b.Level >= seviye.Value
                         && b.Id != kullanicininBirimId)
                .Select(b => b.Id)
                .ToListAsync();

            if (izinliler.Count != yeniler.Count)
            {
                throw new BusinessRuleException(
                    "Katılımcı olarak yalnızca kendi seviyenizdeki ve alt birimler seçilebilir.");
            }
        }

        /// <summary>
        /// Gizli etkinliği görebilecek kişiler ekleyenin KENDİ biriminden mi.
        /// </summary>
        private static async Task GorebileceklerDogrulaAsync(
            AppDbContext context,
            List<long> istenen,
            List<AjandaKatilimci> mevcutlar,
            long kullanicininBirimId)
        {
            var zatenVar = mevcutlar
                .Where(k => k.KullaniciId != null)
                .Select(k => k.KullaniciId!.Value)
                .ToHashSet();

            var yeniler = istenen.Where(id => !zatenVar.Contains(id)).ToList();
            if (yeniler.Count == 0) return;

            var izinliler = await context.Users
                .Where(u => yeniler.Contains(u.Id) && u.BirimId == kullanicininBirimId)
                .Select(u => u.Id)
                .ToListAsync();

            if (izinliler.Count != yeniler.Count)
            {
                throw new BusinessRuleException(
                    "Gizli etkinliği yalnızca kendi biriminizdeki kişiler görebilir.");
            }
        }

        /// <summary>
        /// TOPLU katılımcı okuma — liste uçlarında etkinlik başına sorgu açmamak
        /// için tek sorguda getirir (N+1 önlenir).
        /// </summary>
        public static async Task<Dictionary<long, List<KatilimciDto>>> KatilimcilariGetirAsync(
            this AppDbContext context, IEnumerable<long> ajandaIdler)
        {
            var idler = ajandaIdler.Distinct().ToList();
            if (idler.Count == 0)
            {
                return [];
            }

            var satirlar = await (
                from k in context.AjandaKatilimcilar
                join b in context.Birimler on k.BirimId equals b.Id
                where idler.Contains(k.AjandaId)
                orderby b.Ad
                select new
                {
                    k.AjandaId,
                    Id = b.Id,
                    Ad = (string?)null,
                    Soyad = (string?)null,
                    Unvan = b.Unvan,
                    BirimAd = (string?)b.Ad,
                    BirimId = (long?)b.Id,
                }).ToListAsync();

            // Eski kişi katılımcılar da okunur; aksi hâlde canlıdaki gizli
            // etkinliklerin katılımcı listesi bir anda boş görünürdü.
            var eskiler = await (
                from k in context.AjandaKatilimcilar
                join u in context.Users on k.KullaniciId equals u.Id
                where idler.Contains(k.AjandaId)
                orderby u.Ad, u.Soyad
                select new
                {
                    k.AjandaId,
                    Id = u.Id,
                    Ad = (string?)u.Ad,
                    Soyad = (string?)u.Soyad,
                    u.Unvan,
                    BirimAd = u.Birim != null ? u.Birim.Ad : null,
                    BirimId = (long?)null,
                }).ToListAsync();

            satirlar = satirlar.Concat(eskiler).ToList();

            return satirlar
                .GroupBy(x => x.AjandaId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new KatilimciDto
                    {
                        Id = x.Id,
                        Ad = x.Ad,
                        Soyad = x.Soyad,
                        Unvan = x.Unvan,
                        BirimAd = x.BirimAd,
                        BirimId = x.BirimId,
                    }).ToList());
        }
    }
}
