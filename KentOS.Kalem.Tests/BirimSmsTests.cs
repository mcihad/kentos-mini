using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Services;
using Xunit;

namespace KentOS.Kalem.Tests;

/// <summary>
/// BİRİME SMS — telefonsuz kullanıcı gönderimi düşürmemeli.
///
/// <para>
/// Gerçek hata: <c>messages.token</c> NOT NULL. Telefon numarası olmayan bir
/// kullanıcı için satır eklenince <c>SaveChangesAsync</c> 23502 ile düşüyor,
/// <c>MessageService</c> içindeki <c>catch</c> bunu yutuyor ama BOZUK VARLIK
/// bağlamda izlenir hâlde kalıyor ve <b>çağıranın</b> kaydetmesi patlıyordu.
/// Sonuç: birimdeki tek bir telefonsuz kullanıcı yüzünden istek 500 dönüyordu
/// (mobil e2e koşumunda yakalandı).
/// </para>
///
/// <para>
/// İki katman da sınanıyor: çağrı yerindeki eleme (<c>AjandaService</c>) ve
/// darboğazdaki koruma (<c>MessageService</c>).
/// </para>
/// </summary>
[Collection("SeriPostgres")]
public class BirimSmsTests : IClassFixture<SunucuTestOrtami>
{
    private readonly SunucuTestOrtami _ortam;

    public BirimSmsTests(SunucuTestOrtami ortam) => _ortam = ortam;

    private void PostgresYoksaAtla()
    {
        if (!_ortam.BaglanabildiMi)
        {
            throw Xunit.Sdk.SkipException.ForSkip(_ortam.AtlamaNedeni ?? "Postgres kullanılamıyor");
        }
    }

    /// <summary>Birim 1'deki kullanıcılardan yalnızca ilkine telefon verir.</summary>
    private async Task<(long telefonlu, long telefonsuz)> TelefonlariAyarlaAsync()
    {
        using var b = _ortam.Baglam();
        var kullanicilar = await b.Users.Where(u => u.BirimId == 1).OrderBy(u => u.Id).ToListAsync();

        for (var i = 0; i < kullanicilar.Count; i++)
        {
            kullanicilar[i].PhoneNumber = i == 0 ? "05551112233" : null;
        }
        await b.SaveChangesAsync();

        return (kullanicilar[0].Id, kullanicilar[1].Id);
    }

    [Fact]
    public async Task Birime_sms_telefonsuz_kullaniciyi_atlar_ve_dusmez()
    {
        PostgresYoksaAtla();

        using (var hazirlik = _ortam.Baglam())
        {
            await hazirlik.Database.ExecuteSqlRawAsync(
                "TRUNCATE messages, ajanda_katilimcilar, ajanda_notlar, ajanda_olaylar, ajandalar RESTART IDENTITY CASCADE;");
        }
        await _ortam.TemelVerileriKurAsync();
        var (telefonlu, telefonsuz) = await TelefonlariAyarlaAsync();

        using var baglam = _ortam.Baglam();
        var kullanici = new SahteKullaniciServisi(1, "ekleyen", 1);
        var (ajandaServisi, _, mesaj) = TestServisFabrikasi.Kur(baglam, kullanici, _ortam.Mapper);

        var etkinlik = await ajandaServisi.CreateAsync(new AjandaDto
        {
            Baslik = "SMS denemesi",
            BaslangicTarihi = new DateTime(2026, 11, 3, 9, 0, 0),
            BitisTarihi = new DateTime(2026, 11, 3, 10, 0, 0),
            RandevuTipId = 1,
            DurumId = 1,
        });

        mesaj.TekKisiyeGidenler.Clear();

        // Fırlatmamalı.
        var sonuc = await ajandaServisi.SendSmsToBirimAsync(new SendSmsToBirimDto
        {
            AjandaId = etkinlik.Id,
            BirimIds = [1],
            Message = "Toplantı hatırlatması",
        });

        Assert.True(sonuc);
        Assert.Contains(telefonlu, mesaj.TekKisiyeGidenler);
        Assert.DoesNotContain(telefonsuz, mesaj.TekKisiyeGidenler);
    }

    /// <summary>
    /// Darboğaz koruması: boş jetonla SMS satırı hiç yazılmamalı.
    /// </summary>
    /// <remarks>
    /// Eski MVC ekranları da telefonu doğrudan geçiyor; korumanın
    /// <c>MessageService</c> içinde olması hepsini birden kapsıyor.
    /// </remarks>
    [Fact]
    public async Task MessageService_bos_telefonla_satir_yazmaz()
    {
        PostgresYoksaAtla();

        using (var hazirlik = _ortam.Baglam())
        {
            await hazirlik.Database.ExecuteSqlRawAsync("TRUNCATE messages RESTART IDENTITY CASCADE;");
        }
        await _ortam.TemelVerileriKurAsync();

        using var baglam = _ortam.Baglam();
        var servis = new MessageService(
            baglam, new HerZamanBildirimAlan(), NullLogger<IMessageService>.Instance);

        await servis.CreateAsync(
            1, null!, "Başlık", "İçerik", SendMessageType.SMS, NotifikasyonTip.Always, null);
        await servis.CreateAsync(
            1, "   ", "Başlık", "İçerik", SendMessageType.SMS, NotifikasyonTip.Always, null);

        using var kontrol = _ortam.Baglam();
        Assert.Empty(await kontrol.Messages.AsNoTracking().ToListAsync());

        // Numara varsa satır yazılır — koruma fazla geniş olmamalı.
        await servis.CreateAsync(
            1, "05551112233", "Başlık", "İçerik", SendMessageType.SMS, NotifikasyonTip.Always, null);

        using var kontrol2 = _ortam.Baglam();
        var yazilan = Assert.Single(await kontrol2.Messages.AsNoTracking().ToListAsync());
        Assert.Equal("05551112233", yazilan.Token);
    }

    /// <summary>Bildirim tercihi sorgusunu kısa devre yapan yerine geçen.</summary>
    private sealed class HerZamanBildirimAlan : IUserService
    {
        public Task<bool> HasReceiveNotification(long userId, NotifikasyonTip tip) =>
            Task.FromResult(true);

        public Task<UserDto> Get() => throw new NotSupportedException();
        public Task<UserSettingDto> GetSetting() => throw new NotSupportedException();
        public Task<UserSettingDto> UpdateSetting(UserSettingDto s) => throw new NotSupportedException();
        public Task<LoginResponseDto> LoginAsync(LoginDto d) => throw new NotSupportedException();
        public Task<PasswordChangeResponseDto> PasswordChange(PasswordChangeDto d) =>
            throw new NotSupportedException();
        public void LogoutAsync() => throw new NotSupportedException();
    }
}
