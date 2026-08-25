using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto.V2.Ortak;
using KentOS.Mini.Application.Dto.V2.Yonetim;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;

namespace KentOS.Mini.Web.Services.V2;

public interface IYonetimServisi
{
    Task<SayfaliSonuc<KullaniciOzetDto>> KullanicilarAsync(SayfaIstegi istek);
    Task<KullaniciOzetDto> KullaniciAsync(long id);
    Task<KullaniciOzetDto> KullaniciOlusturAsync(KullaniciOlusturIstegi istek, bool sistemYetkisi);
    Task<KullaniciOzetDto> KullaniciGuncelleAsync(long id, KullaniciGuncelleIstegi istek, bool sistemYetkisi);
    Task ParolaSifirlaAsync(long id, ParolaSifirlaIstegi istek);
    Task KullaniciSilAsync(long id, long isteyenId);

    Task<List<BirimDugumDto>> BirimAgaciAsync();
    Task<BirimDugumDto> BirimOlusturAsync(BirimIstegi istek);
    Task<BirimDugumDto> BirimGuncelleAsync(long id, BirimIstegi istek);
    Task BirimSilAsync(long id);

    Task<List<RolDto>> RollerAsync();
    Task<BirimDetayDto> BirimDetayAsync(long id);
    Task<List<KullaniciOzetDto>> RolKullanicilariAsync(string rolAdi);
    Task RoleKullaniciEkleAsync(string rolAdi, long kullaniciId, long isteyenId);
    Task RoldenKullaniciCikarAsync(string rolAdi, long kullaniciId, long isteyenId);

    Task<RolDto> RolOlusturAsync(RolIstegi istek);
    Task<RolDto> RolGuncelleAsync(long id, RolIstegi istek);
    Task RolSilAsync(long id);

    /// <summary>İzin kataloğu — koddan tohumlanan liste.</summary>
    Task<List<IzinDto>> IzinKatalogAsync();
    Task<List<string>> RolIzinleriAsync(long rolId);
    Task RolIzinleriniYazAsync(long rolId, IReadOnlyList<string> izinler);
}

/// <summary>
/// Birim / kullanıcı / rol yönetimi.
///
/// <para>
/// Bu mantık bugüne kadar MVC controller'larının içinde, view model'lere
/// gömülü hâlde yaşıyordu. Buraya taşınması bir <b>kopyalama değil</b>: eski
/// controller'lar olduğu gibi çalışmaya devam ediyor, v2 ise aynı işi JSON
/// üzerinden ve <b>sunucu tarafında zorlanan</b> kurallarla yapıyor.
/// </para>
/// </summary>
public class YonetimServisi(
    AppDbContext _context,
    UserManager<AppUser> _kullaniciYoneticisi,
    RoleManager<AppRole> _rolYoneticisi,
    IMessageService _mesajServisi,
    ILogger<YonetimServisi> _logger,
    // İsteğe bağlı: mevcut testler bu servisi kurmadan YonetimServisi
    // örnekliyor ve izin önbelleği yalnızca bir başarım ayrıntısı.
    AuthPolicies.IIzinServisi? _izinServisi = null) : IYonetimServisi
{
    /// <summary>
    /// Yalnızca <c>Sistem</c> rolündeki bir kullanıcının atayabileceği roller.
    /// </summary>
    /// <remarks>
    /// v1'de bu kısıt <b>yalnızca görünümde</b> vardı: <c>Create</c>/<c>Edit</c>
    /// eylemleri gelen rol listesini denetlemiyordu, dolayısıyla elle
    /// hazırlanmış bir POST ile herhangi bir Admin kendini <c>Sistem</c>
    /// yapabilirdi. v2 kuralı sunucuda uygular.
    /// </remarks>
    private static readonly string[] KorumaliRoller = [UserRoles.Sistem, UserRoles.BaskanOzel];

    // ------------------------------------------------------------ kullanıcı

    public async Task<SayfaliSonuc<KullaniciOzetDto>> KullanicilarAsync(SayfaIstegi istek)
    {
        var ara = istek.TemizArama;

        var sorgu = _context.Users
            .AsNoTracking()
            .Include(k => k.Birim)
            .Where(k => ara == null
                || EF.Functions.ILike(k.UserName!, $"%{ara}%")
                || (k.Ad != null && EF.Functions.ILike(k.Ad, $"%{ara}%"))
                || (k.Soyad != null && EF.Functions.ILike(k.Soyad, $"%{ara}%"))
                || (k.Unvan != null && EF.Functions.ILike(k.Unvan, $"%{ara}%"))
                || (k.Birim != null && EF.Functions.ILike(k.Birim.Ad, $"%{ara}%")))
            .OrderBy(k => k.UserName);

        var toplam = await sorgu.LongCountAsync();
        var kullanicilar = await sorgu.Skip(istek.Atla).Take(istek.Boyut).ToListAsync();

        // Rolleri tek sorguda topla: kullanıcı başına UserManager.GetRolesAsync
        // çağırmak 200 kullanıcıda 200 sorgu demek olurdu. Yalnızca BU
        // sayfadaki kullanıcılar sorulur.
        var idler = kullanicilar.Select(k => k.Id).ToList();
        var rolEslesmeleri = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where idler.Contains(ur.UserId)
            select new { ur.UserId, r.Name }).ToListAsync();

        var rolHaritasi = rolEslesmeleri
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList());

        return SayfaliSonuc<KullaniciOzetDto>.Olustur(
            kullanicilar.Select(k => Ozet(k, rolHaritasi.GetValueOrDefault(k.Id, []))).ToList(),
            toplam, istek);
    }

    public async Task<KullaniciOzetDto> KullaniciAsync(long id)
    {
        var kullanici = await _context.Users
            .AsNoTracking()
            .Include(k => k.Birim)
            .FirstOrDefaultAsync(k => k.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli kullanıcı bulunamadı.");

        var roller = await RolleriGetirAsync(id);
        return Ozet(kullanici, roller);
    }

    public async Task<KullaniciOzetDto> KullaniciOlusturAsync(KullaniciOlusturIstegi istek, bool sistemYetkisi)
    {
        RolleriDogrula(istek.Roller, sistemYetkisi);

        var kullanici = new AppUser
        {
            UserName = istek.KullaniciAdi,
            Email = istek.Eposta,
            Ad = istek.Ad,
            Soyad = istek.Soyad,
            Unvan = istek.Unvan,
            PhoneNumber = istek.Telefon,
            BirimId = istek.BirimId,
            EmailConfirmed = true,
            GizliEtkinlikEkleyebilir = istek.GizliEtkinlikEkleyebilir,
            DosyaGonderebilir = istek.DosyaGonderebilir,
            SahaPersoneli = istek.SahaPersoneli,
        };

        var sonuc = await _kullaniciYoneticisi.CreateAsync(kullanici, istek.Parola);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(HatalariBirlestir(sonuc));
        }

        if (istek.Roller.Count > 0)
        {
            var rolSonucu = await _kullaniciYoneticisi.AddToRolesAsync(kullanici, istek.Roller);
            if (!rolSonucu.Succeeded)
            {
                // Rolsüz bir kullanıcı hiçbir işe yaramaz ve sessizce sistemde
                // kalırdı; v1 de burada geri alıyor.
                await _kullaniciYoneticisi.DeleteAsync(kullanici);
                throw new BusinessRuleException(HatalariBirlestir(rolSonucu));
            }
        }

        if (istek.SmsGonder && !string.IsNullOrWhiteSpace(istek.Telefon))
        {
            await _mesajServisi.CreateAsync(
                kullanici.Id,
                istek.Telefon,
                "Kullanıcı Oluşturuldu",
                $"Randevu sisteminde kullanıcı kaydınız oluşturuldu. Kullanıcı adı: {kullanici.UserName} Şifre: {istek.Parola}",
                SendMessageType.SMS,
                NotifikasyonTip.Always,
                null,
                // HASSAS: gövde yeni parolayı taşıyor. Gönderildikten (ya da
                // denemeler tükendikten) sonra `messages` satırının içeriği
                // boşaltılıyor; aksi hâlde her sıfırlanan parola
                // veritabanında düz metin olarak süresiz kalırdı.
                hassas: true);
        }

        _logger.LogInformation("Yeni kullanıcı oluşturuldu: {KullaniciAdi} ({Id})",
            kullanici.UserName, kullanici.Id);

        return await KullaniciAsync(kullanici.Id);
    }

    public async Task<KullaniciOzetDto> KullaniciGuncelleAsync(
        long id, KullaniciGuncelleIstegi istek, bool sistemYetkisi)
    {
        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(id.ToString())
            ?? throw new EntityNotFoundException($"{id} kimlikli kullanıcı bulunamadı.");

        var mevcutRoller = (await _kullaniciYoneticisi.GetRolesAsync(kullanici)).ToList();

        // Korumalı rol denetimi iki yönlüdür: yetkisiz biri korumalı bir rolü
        // ne EKLEYEBİLİR ne de KALDIRABİLİR. Yalnızca eklemeyi denetlemek,
        // sistem yöneticisinin rolünü silme kapısını açık bırakırdı.
        RolleriDogrula(istek.Roller.Except(mevcutRoller), sistemYetkisi);
        RolleriDogrula(mevcutRoller.Except(istek.Roller), sistemYetkisi);

        kullanici.UserName = istek.KullaniciAdi;
        kullanici.Email = istek.Eposta;
        kullanici.Ad = istek.Ad;
        kullanici.Soyad = istek.Soyad;
        kullanici.Unvan = istek.Unvan;
        kullanici.PhoneNumber = istek.Telefon;
        kullanici.BirimId = istek.BirimId;
        kullanici.GizliEtkinlikEkleyebilir = istek.GizliEtkinlikEkleyebilir;
        kullanici.DosyaGonderebilir = istek.DosyaGonderebilir;
        kullanici.SahaPersoneli = istek.SahaPersoneli;
        // FcmToken ve WebFcmToken'a DOKUNULMAZ: cihaz bağlantısı yönetim
        // formunun konusu değil, cihazın kendisi kaydeder.

        var sonuc = await _kullaniciYoneticisi.UpdateAsync(kullanici);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(HatalariBirlestir(sonuc));
        }

        var cikacaklar = mevcutRoller.Except(istek.Roller).ToList();
        var girecekler = istek.Roller.Except(mevcutRoller).ToList();

        // v1 önce TÜM rolleri silip yeniden ekliyordu; aradaki anlık boşlukta
        // gelen bir istek yetkisiz kalabiliyordu. Yalnızca farkı uygula.
        if (cikacaklar.Count > 0)
        {
            await _kullaniciYoneticisi.RemoveFromRolesAsync(kullanici, cikacaklar);
        }
        if (girecekler.Count > 0)
        {
            await _kullaniciYoneticisi.AddToRolesAsync(kullanici, girecekler);
        }

        return await KullaniciAsync(kullanici.Id);
    }

    public async Task ParolaSifirlaAsync(long id, ParolaSifirlaIstegi istek)
    {
        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(id.ToString())
            ?? throw new EntityNotFoundException($"{id} kimlikli kullanıcı bulunamadı.");

        var jeton = await _kullaniciYoneticisi.GeneratePasswordResetTokenAsync(kullanici);
        var sonuc = await _kullaniciYoneticisi.ResetPasswordAsync(kullanici, jeton, istek.YeniParola);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(HatalariBirlestir(sonuc));
        }

        if (istek.SmsGonder && !string.IsNullOrWhiteSpace(kullanici.PhoneNumber))
        {
            await _mesajServisi.CreateAsync(
                kullanici.Id,
                kullanici.PhoneNumber,
                "Şifre Değişikliği",
                $"Randevu sistemi şifreniz değiştirildi. Kullanıcı adı: {kullanici.UserName} Şifre: {istek.YeniParola}",
                SendMessageType.SMS,
                NotifikasyonTip.Always,
                null,
                // HASSAS: gövde yeni parolayı taşıyor. Gönderildikten (ya da
                // denemeler tükendikten) sonra `messages` satırının içeriği
                // boşaltılıyor; aksi hâlde her sıfırlanan parola
                // veritabanında düz metin olarak süresiz kalırdı.
                hassas: true);
        }

        _logger.LogInformation("Kullanıcı parolası yöneticice sıfırlandı: {Id}", id);
    }

    public async Task KullaniciSilAsync(long id, long isteyenId)
    {
        if (id == isteyenId)
        {
            throw new BusinessRuleException("Kendi hesabınızı silemezsiniz.");
        }

        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(id.ToString())
            ?? throw new EntityNotFoundException($"{id} kimlikli kullanıcı bulunamadı.");

        var sonuc = await _kullaniciYoneticisi.DeleteAsync(kullanici);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(HatalariBirlestir(sonuc));
        }
    }

    // --------------------------------------------------------------- birim

    public async Task<List<BirimDugumDto>> BirimAgaciAsync()
    {
        var birimler = await _context.Birimler.AsNoTracking().OrderBy(b => b.Ad).ToListAsync();

        var sayimlar = await _context.Users
            .Where(k => k.BirimId != null)
            .GroupBy(k => k.BirimId!.Value)
            .Select(g => new { BirimId = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.BirimId, x => x.Adet);

        var dugumler = birimler.ToDictionary(b => b.Id, b => new BirimDugumDto
        {
            Id = b.Id,
            Ad = b.Ad,
            Yetkili = b.Yetkili,
            Unvan = b.Unvan,
            Telefon = b.Telefon,
            Eposta = b.Email,
            Adres = b.Adres,
            Aciklama = b.Aciklama,
            UstBirimId = b.UstBirimId,
            KullaniciSayisi = sayimlar.GetValueOrDefault(b.Id, 0),
        });

        var kokler = new List<BirimDugumDto>();
        foreach (var dugum in dugumler.Values)
        {
            if (dugum.UstBirimId is { } ustId && dugumler.TryGetValue(ustId, out var ust))
            {
                ust.AltBirimler.Add(dugum);
            }
            else
            {
                // Üst birimi silinmiş olan kayıtlar da kökte görünür — aksi
                // hâlde ağaçtan sessizce düşer ve yönetilemez hâle gelirdi.
                kokler.Add(dugum);
            }
        }

        return kokler;
    }

    public async Task<BirimDugumDto> BirimOlusturAsync(BirimIstegi istek)
    {
        if (istek.UstBirimId is { } ustId && !await _context.Birimler.AnyAsync(b => b.Id == ustId))
        {
            throw new BusinessRuleException("Seçilen üst birim bulunamadı.");
        }

        var birim = new Birim
        {
            Ad = istek.Ad,
            Yetkili = istek.Yetkili,
            Unvan = istek.Unvan,
            Telefon = istek.Telefon,
            Email = istek.Eposta,
            Adres = istek.Adres,
            Aciklama = istek.Aciklama,
            UstBirimId = istek.UstBirimId,
        };

        _context.Birimler.Add(birim);
        await _context.SaveChangesAsync();

        return BirimDugumu(birim, 0);
    }

    public async Task<BirimDugumDto> BirimGuncelleAsync(long id, BirimIstegi istek)
    {
        var birim = await _context.Birimler.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli birim bulunamadı.");

        if (istek.UstBirimId == id)
        {
            throw new BusinessRuleException("Bir birim kendi üst birimi olamaz.");
        }

        if (istek.UstBirimId is { } yeniUst && await AltindaMiAsync(id, yeniUst))
        {
            // Döngü oluşursa birim ağacını gezen her kod sonsuz döngüye girer.
            throw new BusinessRuleException("Bir birim, kendi alt birimlerinden birinin altına taşınamaz.");
        }

        birim.Ad = istek.Ad;
        birim.Yetkili = istek.Yetkili;
        birim.Unvan = istek.Unvan;
        birim.Telefon = istek.Telefon;
        birim.Email = istek.Eposta;
        birim.Adres = istek.Adres;
        birim.Aciklama = istek.Aciklama;
        birim.UstBirimId = istek.UstBirimId;

        await _context.SaveChangesAsync();

        var sayi = await _context.Users.CountAsync(k => k.BirimId == id);
        return BirimDugumu(birim, sayi);
    }

    public async Task BirimSilAsync(long id)
    {
        var birim = await _context.Birimler.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli birim bulunamadı.");

        if (await _context.Birimler.AnyAsync(b => b.UstBirimId == id))
        {
            throw new BusinessRuleException("Alt birimi olan bir birim silinemez. Önce alt birimleri taşıyın.");
        }

        if (await _context.Users.AnyAsync(k => k.BirimId == id))
        {
            throw new BusinessRuleException("Bu birime bağlı kullanıcılar var. Önce kullanıcıları başka birime alın.");
        }

        if (await _context.Ajandalar.IgnoreQueryFilters().AnyAsync(a => a.BirimId == id))
        {
            throw new BusinessRuleException("Bu birime ait etkinlik kayıtları var; birim silinemez.");
        }

        _context.Birimler.Remove(birim);
        await _context.SaveChangesAsync();
    }

    // ----------------------------------------------------------------- rol

    public async Task<List<RolDto>> RollerAsync()
    {
        var roller = await _rolYoneticisi.Roles.AsNoTracking().ToListAsync();

        var sayimlar = await _context.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Adet);

        var izinSayimlari = await _context.RolIzinleri
            .GroupBy(x => x.RolId)
            .Select(g => new { RolId = g.Key, Adet = g.Count() })
            .ToDictionaryAsync(x => x.RolId, x => x.Adet);

        return roller
            .Select(r => new RolDto
            {
                Id = r.Id,
                Ad = r.Name ?? string.Empty,
                Aciklama = r.Description,
                KullaniciSayisi = sayimlar.GetValueOrDefault(r.Id, 0),
                IzinSayisi = izinSayimlari.GetValueOrDefault(r.Id, 0),
                Korumali = KorumaliRoller.Contains(r.Name),
            })
            .OrderBy(r => r.Ad)
            .ToList();
    }

    // ═══════════════════════════════════════════════ rol ve izin yönetimi

    private static string? Bosalt(string? m) => string.IsNullOrWhiteSpace(m) ? null : m.Trim();

    public async Task<RolDto> RolOlusturAsync(RolIstegi istek)
    {
        var ad = istek.Ad.Trim();

        if (await _rolYoneticisi.RoleExistsAsync(ad))
        {
            throw new BusinessRuleException($"\"{ad}\" adında bir rol zaten var.");
        }

        var rol = new AppRole { Name = ad, Description = Bosalt(istek.Aciklama) };
        var sonuc = await _rolYoneticisi.CreateAsync(rol);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(
                string.Join(" ", sonuc.Errors.Select(e => e.Description)));
        }

        return new RolDto { Id = rol.Id, Ad = ad, Aciklama = rol.Description };
    }

    /// <remarks>
    /// <b>Rol ADI değiştirilemez.</b> Ad, kodda <c>UserRoles</c> sabitleriyle
    /// ve eski MVC sayfalarındaki <c>[Authorize(Roles=...)]</c> ile eşleşiyor;
    /// yeniden adlandırmak o denetimleri sessizce devre dışı bırakırdı.
    /// Değiştirilebilen tek şey açıklama.
    /// </remarks>
    public async Task<RolDto> RolGuncelleAsync(long id, RolIstegi istek)
    {
        var rol = await _rolYoneticisi.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("Rol bulunamadı");

        rol.Description = Bosalt(istek.Aciklama);
        await _rolYoneticisi.UpdateAsync(rol);

        return new RolDto { Id = rol.Id, Ad = rol.Name ?? string.Empty, Aciklama = rol.Description };
    }

    /// <remarks>
    /// Kodda tanımlı roller (<c>UserRoles</c>) silinemez: onlara eski MVC
    /// sayfaları ve v1 uçları doğrudan ad ile bağlı. Kullanıcısı olan rol de
    /// silinemez — o kullanıcılar bir anda yetkisiz kalırdı.
    /// </remarks>
    public async Task RolSilAsync(long id)
    {
        var rol = await _rolYoneticisi.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new EntityNotFoundException("Rol bulunamadı");

        if (rol.Name is not null && UserRoles.GetRoles().Contains(rol.Name))
        {
            throw new BusinessRuleException(
                $"\"{rol.Name}\" sistemin kendi rolü; silinemez. İzinlerini kısabilirsiniz.");
        }

        var kullaniciVar = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
        if (kullaniciVar)
        {
            throw new BusinessRuleException(
                "Bu role atanmış kullanıcılar var. Önce onları başka bir role taşıyın.");
        }

        await _rolYoneticisi.DeleteAsync(rol);
    }

    /// <summary>İzin kataloğu — yönetim ekranındaki seçim listesi.</summary>
    public async Task<List<IzinDto>> IzinKatalogAsync()
        => await _context.Izinler
            .AsNoTracking()
            .OrderBy(i => i.SiraNo)
            .Select(i => new IzinDto
            {
                Ad = i.Ad,
                Grup = i.Grup,
                Baslik = i.Baslik,
                Aciklama = i.Aciklama,
                Kullanimda = i.Kullanimda,
            })
            .ToListAsync();

    public async Task<List<string>> RolIzinleriAsync(long rolId)
        => await _context.RolIzinleri
            .AsNoTracking()
            .Where(x => x.RolId == rolId)
            .Select(x => x.IzinAd)
            .ToListAsync();

    /// <summary>Rolün izinlerini TOPLUCA yazar.</summary>
    /// <remarks>
    /// <para>
    /// Gelen liste rolün sahip olacağı izinlerin tamamıdır; listede olmayan
    /// kaldırılır. Tek tek ekle/çıkar uçları, iki sekmede açık iki yöneticinin
    /// birbirinin değişikliğini sessizce geri alması demekti.
    /// </para>
    /// <para>
    /// <b>Katalogda olmayan izin reddedilir.</b> Elle kurulmuş bir istek
    /// uydurma bir izin adı yazabilseydi, o ad hiçbir uçta denetlenmediği için
    /// sessizce yok sayılır ve yönetici yetkiyi verdiğini sanırdı.
    /// </para>
    /// </remarks>
    public async Task RolIzinleriniYazAsync(long rolId, IReadOnlyList<string> izinler)
    {
        var rol = await _rolYoneticisi.Roles.FirstOrDefaultAsync(r => r.Id == rolId)
            ?? throw new EntityNotFoundException("Rol bulunamadı");

        var istenen = izinler.Distinct().ToList();
        var gecersiz = istenen.Where(i => !Izinler.Gecerli(i)).ToList();
        if (gecersiz.Count > 0)
        {
            throw new BusinessRuleException(
                $"Tanınmayan izin: {string.Join(", ", gecersiz)}");
        }

        var mevcutlar = await _context.RolIzinleri.Where(x => x.RolId == rolId).ToListAsync();

        _context.RolIzinleri.RemoveRange(
            mevcutlar.Where(m => !istenen.Contains(m.IzinAd)));

        var varOlan = mevcutlar.Select(m => m.IzinAd).ToHashSet();
        _context.RolIzinleri.AddRange(
            istenen.Where(i => !varOlan.Contains(i))
                   .Select(i => new RolIzin { RolId = rolId, IzinAd = i }));

        await _context.SaveChangesAsync();

        // Yetki DEĞİŞTİ: o roldeki herkesin önbelleği düşer, yoksa değişiklik
        // 5 dakika boyunca uygulanmazdı.
        if (_izinServisi is not null) await _izinServisi.RolDegistiAsync(rolId);
        _ = rol;
    }

    /// <summary>
    /// Birim detayı — istatistikler ve birimdeki kullanıcılar.
    /// </summary>
    /// <remarks>
    /// Sayılar tek tek sorgulanıyor ama hepsi <c>Count</c>; ağaçtaki her
    /// düğüm için ayrı ekran açılmadığından maliyeti önemsiz. Etkinlik ve
    /// talep sayıları <c>IgnoreQueryFilters</c> ile okunur: birimin geçmiş
    /// yükünü göstermek istiyoruz, silinmişler dahil.
    /// </remarks>
    public async Task<BirimDetayDto> BirimDetayAsync(long id)
    {
        var b = await _context.Birimler
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException($"{id} kimlikli birim bulunamadı.");

        var ustBirimAd = b.UstBirimId is { } ust
            ? await _context.Birimler.Where(x => x.Id == ust).Select(x => x.Ad).FirstOrDefaultAsync()
            : null;

        var kullanicilar = await _kullaniciYoneticisi.Users
            .AsNoTracking()
            .Where(u => u.BirimId == id)
            .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
            .ToListAsync();

        var ozetler = new List<KullaniciOzetDto>();
        foreach (var k in kullanicilar)
        {
            ozetler.Add(new KullaniciOzetDto
            {
                Id = k.Id,
                KullaniciAdi = k.UserName ?? string.Empty,
                Ad = k.Ad,
                Soyad = k.Soyad,
                Unvan = k.Unvan,
                Eposta = k.Email,
                Telefon = k.PhoneNumber,
                BirimId = k.BirimId,
                BirimAdi = b.Ad,
                Roller = await RolleriGetirAsync(k.Id),
                MobilBagli = !string.IsNullOrWhiteSpace(k.FcmToken),
                WebBagli = !string.IsNullOrWhiteSpace(k.WebFcmToken),
                GizliEtkinlikEkleyebilir = k.GizliEtkinlikEkleyebilir,
                DosyaGonderebilir = k.DosyaGonderebilir,
                SahaPersoneli = k.SahaPersoneli,
            });
        }

        return new BirimDetayDto
        {
            Id = b.Id,
            Ad = b.Ad,
            Yetkili = b.Yetkili,
            Unvan = b.Unvan,
            Telefon = b.Telefon,
            Eposta = b.Email,
            Adres = b.Adres,
            Aciklama = b.Aciklama,
            UstBirimId = b.UstBirimId,
            UstBirimAd = ustBirimAd,
            KullaniciSayisi = ozetler.Count,
            AltBirimSayisi = await _context.Birimler.CountAsync(x => x.UstBirimId == id),
            EtkinlikSayisi = await _context.Ajandalar.IgnoreQueryFilters().CountAsync(a => a.BirimId == id),
            TalepSayisi = await _context.Randevular.IgnoreQueryFilters().CountAsync(r => r.BirimId == id),
            Kullanicilar = ozetler,
        };
    }

    /// <summary>Bir roldeki kullanıcılar.</summary>
    public async Task<List<KullaniciOzetDto>> RolKullanicilariAsync(string rolAdi)
    {
        var rol = await _rolYoneticisi.FindByNameAsync(rolAdi)
            ?? throw new EntityNotFoundException($"\"{rolAdi}\" rolü bulunamadı.");

        var idler = await _context.UserRoles
            .Where(ur => ur.RoleId == rol.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        // Birim ADI da gerekiyor: rol listesinde "bu Sekreter hangi birimde"
        // sorusu ilk sorulan şey; kimlik numarası bunu cevaplamıyor.
        var kullanicilar = await _kullaniciYoneticisi.Users
            .AsNoTracking()
            .Include(u => u.Birim)
            .Where(u => idler.Contains(u.Id))
            .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
            .ToListAsync();

        var sonuc = new List<KullaniciOzetDto>();
        foreach (var k in kullanicilar)
        {
            sonuc.Add(new KullaniciOzetDto
            {
                Id = k.Id,
                KullaniciAdi = k.UserName ?? string.Empty,
                Ad = k.Ad,
                Soyad = k.Soyad,
                Unvan = k.Unvan,
                Eposta = k.Email,
                Telefon = k.PhoneNumber,
                BirimId = k.BirimId,
                BirimAdi = k.Birim?.Ad,
                Roller = await RolleriGetirAsync(k.Id),
            });
        }

        return sonuc;
    }

    public async Task RoleKullaniciEkleAsync(string rolAdi, long kullaniciId, long isteyenId)
    {
        await KorumaliRolKontrolAsync(rolAdi, isteyenId);

        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(kullaniciId.ToString())
            ?? throw new EntityNotFoundException($"{kullaniciId} kimlikli kullanıcı bulunamadı.");

        if (await _kullaniciYoneticisi.IsInRoleAsync(kullanici, rolAdi)) return;

        var sonuc = await _kullaniciYoneticisi.AddToRoleAsync(kullanici, rolAdi);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(HatalariBirlestir(sonuc));
        }

        _logger.LogInformation("Kullanıcı {Id} → {Rol} rolüne eklendi.", kullaniciId, rolAdi);
    }

    public async Task RoldenKullaniciCikarAsync(string rolAdi, long kullaniciId, long isteyenId)
    {
        await KorumaliRolKontrolAsync(rolAdi, isteyenId);

        var kullanici = await _kullaniciYoneticisi.FindByIdAsync(kullaniciId.ToString())
            ?? throw new EntityNotFoundException($"{kullaniciId} kimlikli kullanıcı bulunamadı.");

        // Kullanıcının SON rolü çıkarılamaz: rolsüz kullanıcı hiçbir politikadan
        // geçemez ve arayüzde boş bir kabukla karşılaşır.
        var roller = await _kullaniciYoneticisi.GetRolesAsync(kullanici);
        if (roller.Count <= 1)
        {
            throw new BusinessRuleException(
                "Kullanıcının tek rolü çıkarılamaz. Önce başka bir rol verin.");
        }

        var sonuc = await _kullaniciYoneticisi.RemoveFromRoleAsync(kullanici, rolAdi);
        if (!sonuc.Succeeded)
        {
            throw new BusinessRuleException(HatalariBirlestir(sonuc));
        }

        _logger.LogInformation("Kullanıcı {Id} → {Rol} rolünden çıkarıldı.", kullaniciId, rolAdi);
    }

    /// <summary>
    /// <c>Sistem</c> ve <c>BaskanOzel</c> rollerini yalnızca <c>Sistem</c> atar.
    /// </summary>
    private async Task KorumaliRolKontrolAsync(string rolAdi, long isteyenId)
    {
        if (!KorumaliRoller.Contains(rolAdi)) return;

        var isteyeninRolleri = await RolleriGetirAsync(isteyenId);
        if (!isteyeninRolleri.Contains(UserRoles.Sistem))
        {
            // Kullanıcı oluşturmadaki yükseltme kapısıyla AYNI tür: yetki
            // reddi 403 olmalı. BusinessRuleException 400 üretiyor ve istemci
            // bunu "girdini düzelt" diye okuyup formu yeniden gönderiyordu.
            _logger.LogWarning(
                "{Isteyen} kullanıcısı korumalı {Rol} rolüne atama denedi.", isteyenId, rolAdi);
            throw new UnauthorizedAccessException(
                $"\"{rolAdi}\" rolünü yalnızca Sistem yetkisi olanlar atayabilir.");
        }
    }

    // ------------------------------------------------------------ yardımcı

    private async Task<List<string>> RolleriGetirAsync(long kullaniciId) =>
        await (from ur in _context.UserRoles
               join r in _context.Roles on ur.RoleId equals r.Id
               where ur.UserId == kullaniciId
               select r.Name!).ToListAsync();

    private static void RolleriDogrula(IEnumerable<string> roller, bool sistemYetkisi)
    {
        if (sistemYetkisi) return;

        var ihlal = roller.FirstOrDefault(r => KorumaliRoller.Contains(r));
        if (ihlal is not null)
        {
            throw new UnauthorizedAccessException(
                $"'{ihlal}' rolünü yalnızca Sistem yetkisine sahip kullanıcılar atayabilir.");
        }
    }

    /// <summary><paramref name="aday"/>, <paramref name="kokId"/> biriminin altında mı?</summary>
    private async Task<bool> AltindaMiAsync(long kokId, long aday)
    {
        var ustler = await _context.Birimler
            .AsNoTracking()
            .Select(b => new { b.Id, b.UstBirimId })
            .ToDictionaryAsync(b => b.Id, b => b.UstBirimId);

        var gecerli = (long?)aday;
        var adim = 0;
        while (gecerli is { } id && adim++ < 100)
        {
            if (id == kokId) return true;
            gecerli = ustler.GetValueOrDefault(id);
        }
        return false;
    }

    private static BirimDugumDto BirimDugumu(Birim b, int kullaniciSayisi) => new()
    {
        Id = b.Id,
        Ad = b.Ad,
        Yetkili = b.Yetkili,
        Unvan = b.Unvan,
        Telefon = b.Telefon,
        Eposta = b.Email,
        Adres = b.Adres,
        Aciklama = b.Aciklama,
        UstBirimId = b.UstBirimId,
        KullaniciSayisi = kullaniciSayisi,
    };

    private static KullaniciOzetDto Ozet(AppUser k, List<string> roller) => new()
    {
        Id = k.Id,
        KullaniciAdi = k.UserName ?? string.Empty,
        Ad = k.Ad,
        Soyad = k.Soyad,
        Unvan = k.Unvan,
        Eposta = k.Email,
        Telefon = k.PhoneNumber,
        BirimId = k.BirimId,
        BirimAdi = k.Birim?.Ad,
        Roller = roller,
        MobilBagli = !string.IsNullOrWhiteSpace(k.FcmToken),
        WebBagli = !string.IsNullOrWhiteSpace(k.WebFcmToken),
        GizliEtkinlikEkleyebilir = k.GizliEtkinlikEkleyebilir,
        DosyaGonderebilir = k.DosyaGonderebilir,
        SahaPersoneli = k.SahaPersoneli,
    };

    private static string HatalariBirlestir(IdentityResult sonuc) =>
        string.Join(" ", sonuc.Errors.Select(e => e.Description));
}
