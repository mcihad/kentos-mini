namespace KentOS.Mini.Application.Identity;

/// <summary>
/// Sistemdeki TÜM izinler — <b>tek doğruluk kaynağı burasıdır</b>.
///
/// <para>
/// İzin ADLARI kodda sabittir, veritabanında yalnızca <b>kimde hangi izin var</b>
/// tutulur. Sebebi: <c>[Izin(Izinler.EtkinlikSil)]</c> yazarken yanlış yazım
/// derlemede yakalanır. Ad serbest metin olsaydı bir harf hatası ucun sessizce
/// <b>hiç kimseye kapalı</b> ya da doğrulanamayan hâle gelmesi demekti.
/// </para>
///
/// <para>
/// Katalog açılışta veritabanına <b>tohumlanır</b> (<c>DataSeeder</c>): burada
/// yeni bir izin tanımlayıp uygulamayı başlatmak yeterli, elle SQL yazılmaz.
/// Koddan kaldırılan izin veritabanında <b>silinmez</b> — bir rolün o izne
/// bağlı olması ve sessizce kaybolması, yetkinin fark edilmeden genişlemesi
/// ya da daralması demekti; onun yerine "artık kullanılmıyor" diye işaretlenir.
/// </para>
///
/// <para>
/// <b>Adlandırma:</b> <c>alan.eylem</c>. Alan bir ekranı/modülü, eylem o
/// ekranda yapılan işi gösterir. Nokta bir gruplama değil, okunabilirlik
/// içindir — yönetim ekranı grupları <see cref="Katalog"/> içindeki
/// <c>Grup</c> alanından gelir.
/// </para>
/// </summary>
public static class Izinler
{
    // ─────────────────────────────────────────────────────────── ajanda
    public const string AjandaGoruntule = "ajanda.goruntule";

    /// <summary>
    /// Yalnızca <b>basın katılacak</b> etkinlikleri görüntüleme.
    /// </summary>
    /// <remarks>
    /// Katalogdaki tek <b>daraltan</b> izin. Ötekiler bir kapı açar; bu,
    /// açılan kapının ardında ne görüleceğini kısar: sahibi ajandayı görür
    /// ama listede yalnızca <c>BasinKatilsin</c> işaretli, gizli olmayan
    /// kayıtlar döner.
    ///
    /// Basın biriminin işi programın basına açık kısmını takip etmek; makamın
    /// bütün gününü görmesi gerekmiyor. Ayrı bir "basın ajandası" ekranı
    /// yazmak yerine aynı ekranın veri kapısını daraltmak, iki ekranın
    /// zamanla ayrışması riskini ortadan kaldırıyor.
    ///
    /// <see cref="AjandaGoruntule"/> ile birlikte verilirse GENİŞ olan kazanır
    /// — daraltma yalnızca tek başına anlamlıdır.
    /// </remarks>
    public const string AjandaBasinGoruntule = "ajanda.basinGoruntule";

    public const string AjandaEkle = "ajanda.ekle";
    public const string AjandaDuzenle = "ajanda.duzenle";
    public const string AjandaSil = "ajanda.sil";
    public const string AjandaKatilimciEkle = "ajanda.katilimciEkle";
    public const string AjandaGizliEtkinlik = "ajanda.gizliEtkinlik";
    public const string AjandaHavale = "ajanda.havale";
    public const string AjandaStatuDegistir = "ajanda.statuDegistir";
    public const string AjandaNotEkle = "ajanda.notEkle";
    public const string AjandaFotografEkle = "ajanda.fotografEkle";
    public const string AjandaSmsGonder = "ajanda.smsGonder";
    public const string AjandaCiktiAl = "ajanda.ciktiAl";

    // ──────────────────────────────────────────────────────────── talep
    public const string TalepGoruntule = "talep.goruntule";
    public const string TalepEkle = "talep.ekle";
    public const string TalepDuzenle = "talep.duzenle";
    public const string TalepSil = "talep.sil";
    public const string TalepDurumDegistir = "talep.durumDegistir";
    public const string TalepHavale = "talep.havale";
    public const string TalepAjandayaEkle = "talep.ajandayaEkle";
    public const string TalepArsivle = "talep.arsivle";
    public const string TalepNotEkle = "talep.notEkle";
    public const string TalepDosyaYukle = "talep.dosyaYukle";
    public const string TalepCiktiAl = "talep.ciktiAl";

    // ────────────────────────────────────────────────────────── protokol
    public const string ProtokolGoruntule = "protokol.goruntule";
    public const string ProtokolYonet = "protokol.yonet";

    // ──────────────────────────────────────────────────────────── davet
    public const string DavetGoruntule = "davet.goruntule";
    public const string DavetYonet = "davet.yonet";
    public const string DavetCiktiAl = "davet.ciktiAl";

    // ────────────────────────────────────────────────────────── halk günü
    //
    // Alan adı BİTİŞİK ve küçük harf (`halkgunu`), camelCase değil: izin
    // kataloğu senkron testi adları `[a-z]+\.[a-zA-Z]+` deseniyle tarıyor ve
    // `halkGunu.goruntule` o desene takılmıyor — izin katalogda görünür ama
    // istemci aynalarında bulunamaz sanılırdı.
    public const string HalkgunuGoruntule = "halkgunu.goruntule";
    public const string HalkgunuYonet = "halkgunu.yonet";
    public const string HalkgunuBasvuru = "halkgunu.basvuru";
    public const string HalkgunuAtama = "halkgunu.atama";
    public const string HalkgunuGorusme = "halkgunu.gorusme";
    public const string HalkgunuSms = "halkgunu.sms";
    public const string HalkgunuCiktiAl = "halkgunu.ciktiAl";
    public const string HalkgunuTalepOlustur = "halkgunu.talepOlustur";

    // ── Özgeçmiş havuzu ────────────────────────────────────────────────
    public const string OzgecmisGoruntule = "ozgecmis.goruntule";
    public const string OzgecmisEkle = "ozgecmis.ekle";
    public const string OzgecmisDuzenle = "ozgecmis.duzenle";
    public const string OzgecmisSil = "ozgecmis.sil";
    public const string OzgecmisPaylas = "ozgecmis.paylas";

    // ──────────────────────────────────────────────────────────── çiçek
    public const string CicekGoruntule = "cicek.goruntule";
    public const string CicekYonet = "cicek.yonet";

    // ──────────────────────────────────────────────────── dosya gönderimi
    public const string GonderimGoruntule = "gonderim.goruntule";
    public const string GonderimGonder = "gonderim.gonder";

    // ───────────────────────────────────────────────────────────── öneri
    public const string OneriGoruntule = "oneri.goruntule";
    public const string OneriYanitla = "oneri.yanitla";

    // ────────────────────────────────────────────────────────── istatistik
    public const string IstatistikGoruntule = "istatistik.goruntule";

    // ─────────────────────────────────────────────────────────── yönetim
    public const string YonetimKullanici = "yonetim.kullanici";
    public const string YonetimBirim = "yonetim.birim";
    public const string YonetimRol = "yonetim.rol";
    public const string YonetimOturumKaydi = "yonetim.oturumKaydi";
    public const string TanimYonet = "tanim.yonet";

    // ────────────────────────────────────────────────────────── iş takip
    //
    // Alan adı BİTİŞİK küçük harf (`gorev`, `ekip`): senkron testi adları
    // `[a-z]+\.[a-zA-Z]+` deseniyle tarıyor ve `isTakip.goruntule` o desene
    // takılmıyor.
    public const string GorevBirimKapsam = "gorev.birimKapsam";

    // ─────────────────────────────────────────────────────────── sistem
    public const string SistemHata = "sistem.hata";
    public const string SistemKurum = "sistem.kurum";

    /// <summary>Bir iznin katalog kaydı.</summary>
    /// <param name="Ad">Kod sabiti — veritabanındaki anahtar.</param>
    /// <param name="Grup">Yönetim ekranındaki başlık.</param>
    /// <param name="Baslik">Kısa ad.</param>
    /// <param name="Aciklama">
    /// Ne işe yaradığı. <b>Zorunlu</b>: rol kuran kişi izin adına bakarak karar
    /// veremiyor — "ajanda.havale" adı, havalenin gizli etkinlikte
    /// çalışmadığını söylemiyor.
    /// </param>
    public record Kayit(string Ad, string Grup, string Baslik, string Aciklama);

    /// <summary>
    /// Tüm izinler, yönetim ekranındaki sırayla.
    /// </summary>
    public static readonly IReadOnlyList<Kayit> Katalog =
    [
        new(AjandaGoruntule, "Ajanda", "Görüntüleme",
            "Etkinlikleri, takvimi ve program görünümünü açar. Bu izin yoksa ajanda menüsü hiç görünmez."),
        new(AjandaBasinGoruntule, "Ajanda", "Yalnızca basın etkinlikleri",
            "Ajandayı açar ama SADECE \"basın katılacak\" işaretli etkinlikleri gösterir. "
            + "Basın birimi için: makamın bütün günü değil, basına açık kısım. "
            + "Tam görüntüleme izniyle birlikte verilirse bu daraltma etkisiz kalır."),
        new(AjandaEkle, "Ajanda", "Etkinlik ekleme",
            "Yeni etkinlik oluşturur. Tekrar eden seri kurmayı da kapsar."),
        new(AjandaDuzenle, "Ajanda", "Etkinlik düzenleme",
            "Var olan etkinliği değiştirir; tarih/saat taşımayı da kapsar."),
        new(AjandaSil, "Ajanda", "Etkinlik silme",
            "Etkinliği siler. Silinen kayıt geri alınabilir ama listelerden düşer."),
        new(AjandaKatilimciEkle, "Ajanda", "Katılımcı birim ekleme",
            "Etkinliğe katılacak birimleri seçer. Yalnızca kendi seviyesindeki ve alt birimler seçilebilir."),
        new(AjandaGizliEtkinlik, "Ajanda", "Gizli etkinlik oluşturma",
            "Gizli etkinlik açar ve kimlerin görebileceğini seçer. Başkalarının gizli etkinliklerini GÖRMEZ — görünürlük ayrı bir kural."),
        new(AjandaHavale, "Ajanda", "Havale etme",
            "Etkinliği başka bir birime havale eder. Gizli etkinlik havale edilemez."),
        new(AjandaStatuDegistir, "Ajanda", "Statü değiştirme",
            "Etkinliği tamamlandı/iptal gibi bir statüye taşır."),
        new(AjandaNotEkle, "Ajanda", "Not ve metin ekleme",
            "Etkinliğe not, konuşma metni ve bilgi notu yazar."),
        new(AjandaFotografEkle, "Ajanda", "Fotoğraf ekleme",
            "Etkinliğe fotoğraf yükler ve siler."),
        new(AjandaSmsGonder, "Ajanda", "Birim SMS'i gönderme",
            "Etkinliği seçilen birimlere SMS ile duyurur. Gizli etkinlikte kullanılamaz."),
        new(AjandaCiktiAl, "Ajanda", "Çıktı alma",
            "Günlük program ve etkinlik listesini yazdırır ya da indirir."),

        new(TalepGoruntule, "Talep", "Görüntüleme",
            "Talep listesini ve detaylarını açar. Bu izin yoksa talepler menüsü görünmez."),
        new(TalepEkle, "Talep", "Talep ekleme",
            "Vatandaş/kurum talebi kaydeder."),
        new(TalepDuzenle, "Talep", "Talep düzenleme",
            "Var olan talebin bilgilerini değiştirir."),
        new(TalepSil, "Talep", "Talep silme", "Talebi kalıcı olarak siler."),
        new(TalepDurumDegistir, "Talep", "Durum değiştirme",
            "Talebi onaylar, reddeder ya da başka bir duruma taşır. Onaylamak ile ajandaya eklemek AYRI izinler."),
        new(TalepHavale, "Talep", "Havale etme",
            "Talebi başka bir birime ya da üst birime gönderir."),
        new(TalepAjandayaEkle, "Talep", "Ajandaya ekleme",
            "Onaylanan talebi takvime alır ve etkinliğe dönüştürür. Onaylama izniyle birlikte verilmek ZORUNDA değil — çoğu kurumda onaylayan ile ekleyen farklı kişidir."),
        new(TalepArsivle, "Talep", "Arşivleme",
            "Talebi arşive taşır ya da arşivden çıkarır."),
        new(TalepNotEkle, "Talep", "Not ekleme", "Talebe not düşer."),
        new(TalepDosyaYukle, "Talep", "Dosya yükleme",
            "Talebe dosya ve özgeçmiş yükler, siler."),
        new(TalepCiktiAl, "Talep", "Çıktı alma",
            "Talep listesini Excel ya da PDF olarak indirir."),

        new(ProtokolGoruntule, "Protokol", "Görüntüleme",
            "İl protokol listesini ve kişi detaylarını açar. Liste kişisel iletişim bilgisi taşır."),
        new(ProtokolYonet, "Protokol", "Yönetme",
            "Protokol kaydı ekler, düzenler, siler ve sırasını değiştirir."),

        new(DavetGoruntule, "Davet", "Görüntüleme", "Davet listelerini açar."),
        new(DavetYonet, "Davet", "Yönetme",
            "Davet listesi oluşturur, kişi ekler/çıkarır ve cevapları işler."),
        new(DavetCiktiAl, "Davet", "Çıktı alma",
            "Takip, telefon ve imza listelerini PDF olarak indirir."),

        new(HalkgunuGoruntule, "Halk Günü", "Görüntüleme",
            "Halk günlerini, bekleyenler havuzunu ve görüşme kayıtlarını açar. Bu izin yoksa menüde hiç görünmez."),
        new(HalkgunuYonet, "Halk Günü", "Gün ve saat yönetimi",
            "Halk günü oluşturur, siler ve zaman dilimlerini tanımlar (14:00–15:00 gibi ya da 10 dakikalık aralıklar)."),
        new(HalkgunuBasvuru, "Halk Günü", "Vatandaş kaydı",
            "Görüşmek isteyen vatandaşı bekleyenler havuzuna ekler ve bilgilerini düzenler."),
        new(HalkgunuAtama, "Halk Günü", "Listeye atama ve sıralama",
            "Bekleyenleri zaman dilimlerine atar, dilim içindeki görüşme sırasını değiştirir."),
        new(HalkgunuGorusme, "Halk Günü", "Görüşme kaydı",
            "Salonda geldi/gelmedi işaretler, görüşme notu yazar ve \"ilgilenilecek\" olarak işaretler."),
        new(HalkgunuSms, "Halk Günü", "Toplu SMS",
            "Listedeki herkese randevu saatini içeren bilgilendirme SMS'i gönderir."),
        new(OzgecmisGoruntule, "Özgeçmiş", "Havuzu görüntüleme",
            "Kurumdaki bütün özgeçmişleri arar, açar ve indirir. Havuz BİRİMDEN BAĞIMSIZDIR: izin verilen kişi başka birimin kaydını da görür."),
        new(OzgecmisEkle, "Özgeçmiş", "Ekleme",
            "Havuza yeni özgeçmiş yükler; ad, telefon, meslek ve açıklama girer."),
        new(OzgecmisDuzenle, "Özgeçmiş", "Düzenleme",
            "Var olan kaydın bilgilerini ve dosyasını değiştirir."),
        new(OzgecmisSil, "Özgeçmiş", "Silme",
            "Özgeçmişi havuzdan kaldırır. Talepten gelen kayıt silinse de talebin kendi dosyası yerinde kalır."),
        new(OzgecmisPaylas, "Özgeçmiş", "Paylaşma",
            "Özgeçmişi başka bir kullanıcıya yönlendirir; alıcı bildirim alır ve paylaşım kaydı kalır."),

        new(HalkgunuCiktiAl, "Halk Günü", "Excel çıktısı",
            "Günün listesini ya da tek bir saat aralığını Excel olarak indirir."),
        new(HalkgunuTalepOlustur, "Halk Günü", "Talebe dönüştürme",
            "İlgilenilecek diye işaretlenmiş bir görüşmeyi talebe çevirip ilgili birime havale eder."),

        new(CicekGoruntule, "Çiçek", "Görüntüleme", "Çiçekçi listesini ve talimatları açar."),
        new(CicekYonet, "Çiçek", "Yönetme",
            "Çiçekçi ekler/düzenler ve çiçek talimatı gönderir."),

        new(GonderimGoruntule, "Dosya Gönderimi", "Görüntüleme",
            "Gelen ve giden dosyaları açar. Dosya ALMAK için ayrıca izin gerekmez."),
        new(GonderimGonder, "Dosya Gönderimi", "Dosya gönderme",
            "Başka bir kullanıcıya dosya gönderir."),

        new(OneriGoruntule, "Öneri", "Görüntüleme", "Öneri ve şikâyetleri açar."),
        new(OneriYanitla, "Öneri", "Yanıtlama", "Öneriye cevap yazar."),

        new(IstatistikGoruntule, "İstatistik", "Görüntüleme",
            "Birim geneli sayıları ve grafikleri açar. Kişi bazlı üretkenlik verisi içerir."),

        new(YonetimKullanici, "Yönetim", "Kullanıcı yönetimi",
            "Kullanıcı ekler, düzenler, siler ve parola sıfırlar."),
        new(YonetimBirim, "Yönetim", "Birim yönetimi",
            "Birim ağacını düzenler."),
        new(YonetimRol, "Yönetim", "Rol ve yetki yönetimi",
            "Rol oluşturur, rollere izin bağlar ve kullanıcılara rol atar. Bu izin, sahibine SİSTEMİN TAMAMINI açabilme gücü verir."),
        new(YonetimOturumKaydi, "Yönetim", "Giriş kayıtları",
            "Kimin ne zaman, hangi IP'den giriş yaptığını görüntüler."),
        new(TanimYonet, "Yönetim", "Tanım yönetimi",
            "Etkinlik tipi, durum, mahalle, meslek gibi referans listelerini düzenler."),

        new(GorevBirimKapsam, "İş Takip", "Alt birim kapsamı",
            "Kendi biriminin ALTINDAKİ bir birimi seçip o birimin işlerini görür ve yönetir. " +
            "Başkan yardımcısının bağlı müdürlükleri takip etmesi için. Yalnızca kendi alt ağacı — " +
            "üst ya da yan birimler açılmaz."),

        new(SistemHata, "Sistem", "Hata kayıtları",
            "Sunucu hatalarını görüntüler. Kayıtlar istek gövdeleri, IP adresleri ve yığın izleri içerir."),
        new(SistemKurum, "Sistem", "Kurum bilgileri",
            "Kurum adını, iletişim bilgilerini, uygulama adını, amblemi ve kurumsal renkleri düzenler. " +
            "Değişiklik BÜTÜN kullanıcıların gördüğü arayüzü etkiler."),
    ];

    /// <summary>Katalogdaki tüm izin adları.</summary>
    public static IReadOnlyList<string> Adlar { get; } =
        [.. Katalog.Select(k => k.Ad)];

    /// <summary>Ad geçerli mi — elle kurulmuş isteklere karşı.</summary>
    public static bool Gecerli(string ad) => Adlar.Contains(ad);
}
