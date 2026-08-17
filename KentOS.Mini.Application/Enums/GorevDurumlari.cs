namespace KentOS.Mini.Application.Enums;

/// <summary>
/// Görevin yaşam döngüsü.
/// </summary>
/// <remarks>
/// <para>
/// <b>YENİ DEĞER EKLERKEN SONA EKLEYİN</b> — sayısal karşılıklar veritabanında
/// saklanıyor.
/// </para>
/// <para>
/// Durum bir TANIM TABLOSU değil, kodda sabit. Sebebi akışın kendisi: hangi
/// durumdan hangisine geçilebileceği, kimin geçirebileceği ve her geçişin ne
/// tetiklediği (bildirim, SLA damgası, onay kapısı) kodda yazılı. Durumları
/// veritabanından okusaydık, yönetici bir satır eklediğinde akışın o durumu
/// nasıl ele alacağı tanımsız kalırdı.
/// </para>
/// <para>
/// Etiket ve renk yine de <b>sunucuda</b> üretilir (<c>durumAd</c>,
/// <c>durumRenk</c>) — iki istemcinin aynı duruma farklı ad vermesi
/// imkânsız olsun diye.
/// </para>
/// </remarks>
public enum GorevDurumu
{
    /// <summary>Açıldı ama kimseye atanmadı.</summary>
    Yeni = 0,

    /// <summary>Kişiye ya da ekibe atandı; henüz başlanmadı.</summary>
    Atandi = 1,

    /// <summary>Atanan kişi işe başladığını bildirdi — SLA sayacı burada anlam kazanır.</summary>
    Basladi = 2,

    /// <summary>En az bir aşama tamamlandı.</summary>
    DevamEdiyor = 3,

    /// <summary>
    /// Dış bir sebeple duruyor (malzeme yok, hava, başka birim bekleniyor).
    /// </summary>
    /// <remarks>
    /// SLA sayacı bu durumda DURUR: bekleyen bir işi geciktirdi diye personele
    /// yazmak, ölçümü anlamsız kılar.
    /// </remarks>
    Beklemede = 4,

    /// <summary>
    /// Personel "bitirdim" dedi, yönetici onayı bekliyor.
    /// </summary>
    /// <remarks>
    /// Ara durum ŞART: personelin beyanı ile kurumun kabulü aynı şey değil.
    /// Tek adımda tamamlansaydı "yapıldı" denen ama yapılmayan iş, kimse
    /// bakmadan kapanırdı.
    /// </remarks>
    TamamlanmaBekliyor = 5,

    /// <summary>Yönetici onayladı — iş bitti.</summary>
    Tamamlandi = 6,

    /// <summary>Yönetici iade etti; gerekçe zorunlu, görev atanana geri döner.</summary>
    IadeEdildi = 7,

    /// <summary>Atanan kişi/birim işi kabul etmedi (yanlış birim, yetki dışı).</summary>
    Reddedildi = 8,

    /// <summary>Görev geçersiz kaldı (mükerrer kayıt, vatandaş vazgeçti).</summary>
    Iptal = 9,
}

/// <summary>Görevin aciliyeti.</summary>
/// <remarks>
/// Öncelik SLA süresini DEĞİŞTİRMEZ — süre görev tipinden gelir. Öncelik
/// yalnızca sıralama ve görsel vurgu içindir. İkisini birbirine bağlamak,
/// "acil" işaretleyerek hizmet standardını delmeyi mümkün kılardı.
/// </remarks>
public enum GorevOnceligi
{
    Dusuk = 0,
    Normal = 1,
    Yuksek = 2,
    Acil = 3,
}

/// <summary>
/// Görevin nereden geldiği.
/// </summary>
/// <remarks>
/// <para>
/// Üç ayrı iş akışı (vatandaş şikayeti, sahada tespit, birimin kendi planı)
/// AYNI <c>gorevler</c> tablosunda buluşuyor; fark yalnızca bu alanda.
/// Ayrı tablolar kursaydık aynı SLA, aynı aşama ve aynı bildirim mantığını
/// üç kez yazmak gerekirdi.
/// </para>
/// <para>
/// <b>Genişlemeye açık:</b> ileride talepten ve ajandadan görev
/// oluşturulacak. O zaman yeni bir oluşturma yolu değil, bu listeye yeni bir
/// değer eklenir — <c>IGorevServisi.OlusturAsync</c> tek giriş noktası
/// olarak kalır.
/// </para>
/// </remarks>
public enum GorevKaynagi
{
    /// <summary>Birim çalışanı elle açtı.</summary>
    Manuel = 0,

    /// <summary>Sahadaki personel tespit edip gönderdi.</summary>
    Saha = 1,

    /// <summary>Vatandaş bildirdi; talep karşılama ekranından birime verildi.</summary>
    Vatandas = 2,

    /// <summary>Var olan bir talepten (randevu) türetildi.</summary>
    Talep = 3,

    /// <summary>Bir etkinlikten türetildi.</summary>
    Etkinlik = 4,

    /// <summary>Projenin iş kırılımından geldi.</summary>
    Proje = 5,

    /// <summary>Başka bir birimin devir kaydından (gelen kutusu) doğdu.</summary>
    BirimDevri = 6,
}

/// <summary>Görevin tek bir aşamasının durumu.</summary>
public enum GorevAsamaDurumu
{
    Bekliyor = 0,
    Tamamlandi = 1,

    /// <summary>Zorunlu olmayan aşama atlandı; gerekçe not alanında.</summary>
    Atlandi = 2,
}

/// <summary>Bir göreve atanan kişinin rolü.</summary>
public enum GorevAtamaRolu
{
    /// <summary>İşi yapan. SLA ve bildirimler öncelikle buna.</summary>
    Sorumlu = 0,

    /// <summary>Yardımcı — işi birlikte yapıyor.</summary>
    Yardimci = 1,

    /// <summary>Yalnızca haberdar olur; iş beklenmiyor.</summary>
    Izleyici = 2,
}

/// <summary>
/// Görev zaman çizelgesindeki olay türü.
/// </summary>
/// <remarks>
/// <b>YENİ DEĞER SONA EKLENİR</b> — sayısal karşılıklar veritabanında.
/// <c>AjandaOlayTip</c> ile aynı kural.
/// </remarks>
public enum GorevOlayTipi
{
    Olusturuldu = 0,
    Guncellendi = 1,
    DurumDegisti = 2,
    Atandi = 3,
    AtamaKaldirildi = 4,
    AsamaTamamlandi = 5,
    AsamaGeriAlindi = 6,
    TamamlanmayaGonderildi = 7,
    Onaylandi = 8,
    IadeEdildi = 9,
    Reddedildi = 10,
    IptalEdildi = 11,
    YorumEklendi = 12,
    EkEklendi = 13,
    AltGorevAcildi = 14,

    /// <summary>Başka bir birim adına işlem yapıldı — vekâlet kaydı.</summary>
    BirimAdinaIslem = 15,

    /// <summary>SLA aşımı bildirildi. Aynı eşik iki kez bildirilmesin diye.</summary>
    SlaUyarisi = 16,
}
