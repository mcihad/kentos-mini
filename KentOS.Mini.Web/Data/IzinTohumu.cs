using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;

namespace KentOS.Mini.Web.Data;

/// <summary>
/// İzin kataloğunu koddan veritabanına taşır ve <b>ilk kurulumda</b> rollere
/// dağıtır.
/// </summary>
/// <remarks>
/// <para>
/// Her açılışta çalışır ama <b>yalnızca eksikleri</b> ekler: yeni bir izin
/// tanımlayıp uygulamayı başlatmak yeterli, elle SQL yazılmaz. Metin
/// alanları (başlık, açıklama) her açılışta koddan tazelenir — açıklamayı
/// düzeltmek için yayın dışında bir işlem gerekmesin.
/// </para>
/// <para>
/// Koddan kaldırılan izin SİLİNMEZ, <c>Kullanimda = false</c> olur. Silmek o
/// izne bağlı rol kayıtlarını da götürür ve yetkinin fark edilmeden değişmesi
/// demektir.
/// </para>
/// </remarks>
public static class IzinTohumu
{
    /// <summary>
    /// İlk kurulum dağılımı — <b>bugünkü davranışın birebir kopyası</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Geçiş günü hiç kimsenin yetkisi değişmesin diye bu tablo eski
    /// <c>PolicyRegistrar</c> eşlemesinden çıkarıldı: <c>Ajanda</c> politikası
    /// Admin/Sekreter/Yonetici/Baskan'a açıktı, bu roller o modülün tüm
    /// izinlerini alıyor.
    /// </para>
    /// <para>
    /// <b>YALNIZCA rolün hiç izni yoksa uygulanır.</b> Yönetici bir rolün
    /// iznini kıstıktan sonra uygulama yeniden başlayınca eski hâline dönmesi,
    /// yetki yönetimini işe yaramaz kılardı.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string[]> IlkDagilim = new()
    {
        // Admin her şeyi yapar AMA hata kayıtlarını GÖREMEZ: kayıtlarda istek
        // gövdeleri, IP adresleri ve yığın izleri var. Bu, izin sistemi
        // gelmeden önce de böyleydi ve öyle kalmalı.
        [UserRoles.Admin] = [.. Izinler.Adlar.Where(a => a != Izinler.SistemHata)],

        [UserRoles.Sistem] = [.. Izinler.Adlar],

        // `Ajanda` politikasındaki roller (Admin/Sekreter/Yonetici/Baskan).
        [UserRoles.Sekreter] = [.. AjandaModulu],
        [UserRoles.Yonetici] = [.. AjandaModulu],
        // İstatistik ucu eskiden `[Authorize(Roles = "Admin,Sistem")]` idi —
        // Başkan'a vermek geçiş gününde YETKİ GENİŞLETMEK olurdu. Yönetici
        // isterse rol ekranından açar; tohumun işi eski durumu korumak.
        [UserRoles.Baskan] = [.. AjandaModulu, Izinler.CicekGoruntule, Izinler.CicekYonet],

        // `Cicek` ve `Medya` yalnızca `OzelKalem` politikasındaydı; o politika
        // hiçbir v2 ucunu korumuyor, dolayısıyla ellerinde yalnızca herkese
        // açık ekranlar kalıyor.
        [UserRoles.Cicek] = [Izinler.CicekGoruntule, Izinler.CicekYonet,
                             Izinler.GonderimGoruntule],
        [UserRoles.Medya] = [Izinler.GonderimGoruntule],

        // Bu üç rol bugün hiçbir politikada yok: giriş yapıyor ama hiçbir
        // yere giremiyorlar. Dosya gönderimi herkese açık olduğu için o
        // veriliyor — hiç izinsiz bir rol, boş bir uygulama demek.
        [UserRoles.Kullanici] = [Izinler.GonderimGoruntule],

        // BASIN: ajandanın yalnızca basına açık kısmı.
        //
        // Tam görüntüleme izni bilerek VERİLMİYOR — ikisi bir aradayken geniş
        // olan kazanıyor ve daraltma etkisiz kalırdı. Basın biriminin işi
        // programın basına açık kısmını takip etmek; makamın bütün gününü
        // görmesi gerekmiyor.
        [UserRoles.Basin] = [Izinler.AjandaBasinGoruntule, Izinler.AjandaCiktiAl,
                             Izinler.GonderimGoruntule],
        [UserRoles.Sibeski] = [Izinler.GonderimGoruntule],
        [UserRoles.BaskanOzel] = [Izinler.GonderimGoruntule],
    };

    /// <summary>`Ajanda` politikasının kapsadığı modüllerin tüm izinleri.</summary>
    private static string[] AjandaModulu =>
    [
        Izinler.AjandaGoruntule, Izinler.AjandaEkle, Izinler.AjandaDuzenle,
        Izinler.AjandaSil, Izinler.AjandaKatilimciEkle, Izinler.AjandaHavale,
        Izinler.AjandaStatuDegistir, Izinler.AjandaNotEkle,
        Izinler.AjandaFotografEkle, Izinler.AjandaSmsGonder, Izinler.AjandaCiktiAl,

        Izinler.TalepGoruntule, Izinler.TalepEkle, Izinler.TalepDuzenle,
        Izinler.TalepSil, Izinler.TalepDurumDegistir, Izinler.TalepHavale,
        Izinler.TalepAjandayaEkle, Izinler.TalepArsivle, Izinler.TalepNotEkle,
        Izinler.TalepDosyaYukle, Izinler.TalepCiktiAl,

        Izinler.ProtokolGoruntule,
        Izinler.DavetGoruntule, Izinler.DavetYonet, Izinler.DavetCiktiAl,

        // Halk günü: `Ajanda` politikasındaki roller (Sekreter/Yönetici/Başkan)
        // günü kurabilsin, kişi ekleyebilsin ve salonda not tutabilsin diye
        // modülün tamamı buraya konuyor. Daraltmak yöneticinin işi — tohumun
        // işi ilk günü çalışır hâlde teslim etmek.
        Izinler.HalkgunuGoruntule, Izinler.HalkgunuYonet, Izinler.HalkgunuBasvuru,
        Izinler.HalkgunuAtama, Izinler.HalkgunuGorusme, Izinler.HalkgunuSms,
        Izinler.HalkgunuCiktiAl, Izinler.HalkgunuTalepOlustur,
        // Özgeçmiş havuzu: iş talebini zaten bu roller karşılıyor ve
        // havuzun varlık sebebi kaydın birimler arasında dolaşabilmesi.
        Izinler.OzgecmisGoruntule, Izinler.OzgecmisEkle, Izinler.OzgecmisDuzenle,
        Izinler.OzgecmisSil, Izinler.OzgecmisPaylas,

        Izinler.OneriGoruntule, Izinler.OneriYanitla,
        Izinler.GonderimGoruntule,


        // İŞ TAKİP — `gorev.birimKapsam` tohumla DAĞITILMAZ.
        //
        // Başka bir birim adına iş yapmak bir VEKÂLET, varsayılan değil:
        // kimin hangi müdürlüğü takip edeceği kuruma göre değişir ve rol
        // ekranından açıkça verilir.
    ];

    public static async Task UygulaAsync(AppDbContext context)
    {
        var yeniIzinler = await KatalogTazeleAsync(context);
        await IlkDagilimUygulaAsync(context);
        await YeniIzinleriDagitAsync(context, yeniIzinler);
    }

    /// <summary>
    /// Katalogu tazeler ve <b>ilk kez görülen</b> izinlerin adlarını döndürür.
    /// </summary>
    private static async Task<HashSet<string>> KatalogTazeleAsync(AppDbContext context)
    {
        var yeniler = new HashSet<string>();
        var mevcutlar = await context.Izinler.ToDictionaryAsync(i => i.Ad);
        var kodda = new HashSet<string>(Izinler.Adlar);

        for (var i = 0; i < Izinler.Katalog.Count; i++)
        {
            var k = Izinler.Katalog[i];

            if (mevcutlar.TryGetValue(k.Ad, out var kayit))
            {
                // Metinler her açılışta koddan tazelenir; açıklamayı
                // düzeltmek için ayrı bir işlem gerekmesin.
                kayit.Grup = k.Grup;
                kayit.Baslik = k.Baslik;
                kayit.Aciklama = k.Aciklama;
                kayit.SiraNo = i;
                kayit.Kullanimda = true;
                continue;
            }

            yeniler.Add(k.Ad);
            context.Izinler.Add(new Izin
            {
                Ad = k.Ad,
                Grup = k.Grup,
                Baslik = k.Baslik,
                Aciklama = k.Aciklama,
                SiraNo = i,
                Kullanimda = true,
            });
        }

        foreach (var (ad, kayit) in mevcutlar.Where(m => !kodda.Contains(m.Key)))
        {
            _ = ad;
            kayit.Kullanimda = false;
        }

        await context.SaveChangesAsync();
        return yeniler;
    }

    /// <summary>
    /// Koda YENİ eklenen bir izni, varsayılan dağılımdaki rollere verir.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IlkDagilimUygulaAsync"/> yalnızca <b>hiç izni olmayan</b>
    /// role dokunur — yöneticinin kıstığı yetkiyi geri açmamak için. Ama bu,
    /// katalogda yeni tanımlanan bir iznin var olan rollere <b>hiç
    /// ulaşamaması</b> demekti: izin ekleniyor, kodda ve arayüzde görünüyor,
    /// hiç kimsede olmuyor. (Basın izni tam olarak böyle sessiz kaldı.)
    /// </para>
    /// <para>
    /// Kapı iznin <b>ilk kez tanımlandığı</b> ana bağlı: dağıtım yalnızca o
    /// açılışta yapılır. Sonraki açılışlarda izin katalogda zaten var, yani
    /// yönetici kaldırdıysa geri gelmez.
    /// </para>
    /// </remarks>
    private static async Task YeniIzinleriDagitAsync(
        AppDbContext context, HashSet<string> yeniIzinler)
    {
        if (yeniIzinler.Count == 0) return;

        var roller = await context.Roles
            .Where(r => r.Name != null)
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        var mevcut = await context.RolIzinleri
            .Select(x => new { x.RolId, x.IzinAd })
            .ToListAsync();
        var mevcutKume = mevcut.Select(x => (x.RolId, x.IzinAd)).ToHashSet();

        var eklenecek = new List<RolIzin>();

        foreach (var rol in roller)
        {
            if (!IlkDagilim.TryGetValue(rol.Name!, out var varsayilan)) continue;

            foreach (var izin in varsayilan.Where(yeniIzinler.Contains).Distinct())
            {
                if (mevcutKume.Contains((rol.Id, izin))) continue;
                eklenecek.Add(new RolIzin { RolId = rol.Id, IzinAd = izin });
            }
        }

        if (eklenecek.Count == 0) return;

        context.RolIzinleri.AddRange(eklenecek);
        await context.SaveChangesAsync();
    }

    private static async Task IlkDagilimUygulaAsync(AppDbContext context)
    {
        var roller = await context.Roles
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        var izinliRoller = await context.RolIzinleri
            .Select(x => x.RolId)
            .Distinct()
            .ToListAsync();

        var eklenecek = new List<RolIzin>();

        foreach (var rol in roller)
        {
            // Rolün ZATEN izni varsa dokunulmaz: yönetici kıstıktan sonra
            // yeniden başlatmak eski hâline döndürseydi yetki yönetimi işe
            // yaramazdı.
            if (rol.Name is null || izinliRoller.Contains(rol.Id)) continue;
            if (!IlkDagilim.TryGetValue(rol.Name, out var izinler)) continue;

            eklenecek.AddRange(izinler.Distinct().Select(i => new RolIzin
            {
                RolId = rol.Id,
                IzinAd = i,
            }));
        }

        if (eklenecek.Count == 0) return;

        context.RolIzinleri.AddRange(eklenecek);
        await context.SaveChangesAsync();
    }
}
