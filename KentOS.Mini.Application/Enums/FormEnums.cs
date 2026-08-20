namespace KentOS.Mini.Application.Enums;

/// <summary>
/// Formun yayın durumu.
/// </summary>
/// <remarks>
/// <para>
/// <b>Yayın durumu ile açık/kapalı AYRI şeyler.</b> Bir form yayında olup
/// süresi dolduğu için yanıt almayabilir; taslak bir form ise hiç var
/// olmamış gibi davranır. İkisini tek alana sıkıştırmak, "yayından
/// kaldırdım mı yoksa süresi mi doldu" sorusunu cevapsız bırakırdı.
/// </para>
/// </remarks>
public enum FormDurumu
{
    /// <summary>Hazırlanıyor; vatandaş adresinden erişilemez.</summary>
    Taslak = 0,

    /// <summary>Yayında. Yanıt kabulü ayrıca zaman ve sayı sınırına bakar.</summary>
    Yayinda = 1,

    /// <summary>Yayından kaldırıldı — bağlantı çalışır ama yanıt alınmaz.</summary>
    Kapali = 2,

    /// <summary>Arşiv: listelerde görünmez, yanıtları durur.</summary>
    Arsiv = 3,
}

/// <summary>
/// Form alanı (soru) tipi.
/// </summary>
/// <remarks>
/// <para>
/// Değerler <b>kalıcı sözleşmedir</b>: yayınlanmış bir formun tanımı JSONB
/// içinde bu sayılarla saklanıyor ve eski yanıtlar onlara göre okunuyor.
/// Araya değer eklenmez, sona eklenir; hiçbir değer yeniden numaralanmaz.
/// </para>
/// </remarks>
public enum FormAlanTipi
{
    // ── metin ──
    KisaMetin = 0,
    UzunMetin = 1,
    Eposta = 2,
    Telefon = 3,
    TcKimlik = 4,
    Url = 5,

    // ── sayı ve tarih ──
    Sayi = 10,
    Tarih = 11,
    Saat = 12,
    TarihSaat = 13,
    TarihAraligi = 14,

    // ── seçim ──
    TekSecim = 20,
    CokSecim = 21,
    AcilirListe = 22,
    CokluAcilirListe = 23,
    EvetHayir = 24,

    // ── ölçek ──
    Olcek = 30,
    Nps = 31,
    Yildiz = 32,

    // ── karmaşık ──
    MatrisTekSecim = 40,
    MatrisCokSecim = 41,
    Siralama = 42,

    // ── ek ──
    Dosya = 50,
    Konum = 51,
    Imza = 52,

    /// <summary>
    /// İçerik blokları — soru DEĞİL, yanıt üretmezler.
    /// </summary>
    /// <remarks>
    /// Ayrı bir "blok" kavramı yerine alan tipi olmaları bilinçli: tasarımcıda
    /// aynı listede sürüklenip aynı şekilde sıralanıyorlar. Ayrı tutulsaydı
    /// "başlığı iki sorunun arasına taşı" işlemi iki farklı koleksiyonu
    /// birlikte yönetmek demekti.
    /// </remarks>
    Baslik = 60,
    Aciklama = 61,
    Ayirici = 62,
    Gorsel = 63,
}

/// <summary>Form yanıtının durumu.</summary>
public enum FormYanitDurumu
{
    /// <summary>Yarım kalmış — vatandaş devam edebilir.</summary>
    Taslak = 0,

    /// <summary>Gönderildi.</summary>
    Gonderildi = 1,

    /// <summary>Yetkili tarafından geçersiz sayıldı (yinelenen, spam).</summary>
    Gecersiz = 2,
}

/// <summary>
/// Formun kimlere açık olduğu.
/// </summary>
/// <remarks>
/// <b>Anonim ile kimlikli aynı formda karışmaz.</b> Yanıtın kime ait
/// olduğunu sonradan sormak mümkün değil; formu kuran kişi bunu baştan
/// seçmek zorunda ve vatandaş da hangi kipte olduğunu görüyor.
/// </remarks>
public enum FormErisimi
{
    /// <summary>Bağlantıyı bilen herkes; kimlik sorulmaz.</summary>
    Anonim = 0,

    /// <summary>Telefon doğrulaması ister (tek kişi tek yanıt kuralı buna dayanır).</summary>
    TelefonDogrulamali = 1,

    /// <summary>Yalnızca giriş yapmış personel.</summary>
    Personel = 2,
}
