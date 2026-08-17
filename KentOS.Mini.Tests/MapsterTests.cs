using Mapster;
using MapsterMapper;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Models;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// AutoMapper → Mapster geçişinin çalıştığını runtime'da doğrular. Uygulamayla
/// AYNI global TypeAdapterConfig üzerinden convention-based eşleme test edilir.
/// </summary>
public class MapsterTests
{
    // Program.cs'teki ServiceMapper ile aynı global config.
    private readonly IMapper _mapper = new Mapper(TypeAdapterConfig.GlobalSettings);

    [Fact]
    public void Oneri_To_OneriDto_TumAlanlariEsler()
    {
        var oneri = new Oneri
        {
            Id = 42,
            Baslik = "Test Öneri",
            Aciklama = "Açıklama",
            Tip = OneriTip.Istek,
            Tarih = new DateTime(2026, 8, 10, 9, 30, 0),
            KullaniciId = 7,
            KullaniciAdi = "Ali Veli",
            Cevap = "Cevap metni",
            CevapTarih = new DateTime(2026, 8, 11)
        };

        var dto = _mapper.Map<OneriDto>(oneri);

        Assert.Equal(42, dto.Id);
        Assert.Equal("Test Öneri", dto.Baslik);
        Assert.Equal("Açıklama", dto.Aciklama);
        Assert.Equal(OneriTip.Istek, dto.Tip);
        Assert.Equal(oneri.Tarih, dto.Tarih);
        Assert.Equal(7, dto.KullaniciId);
        Assert.Equal("Ali Veli", dto.KullaniciAdi);
        Assert.Equal("Cevap metni", dto.Cevap);
        Assert.Equal(oneri.CevapTarih, dto.CevapTarih);
    }

    [Fact]
    public void OneriDto_To_Oneri_TersYon()
    {
        var dto = new OneriDto
        {
            Id = 1,
            Baslik = "Başlık",
            Tip = OneriTip.Hata,
            KullaniciAdi = "X",
        };

        var entity = _mapper.Map<Oneri>(dto);

        Assert.Equal(1, entity.Id);
        Assert.Equal("Başlık", entity.Baslik);
        Assert.Equal(OneriTip.Hata, entity.Tip);
        Assert.Equal("X", entity.KullaniciAdi);
    }

    [Fact]
    public void Ajanda_To_AjandaDto_TemelAlanlar()
    {
        var bas = new DateTime(2026, 8, 10, 14, 0, 0);
        var ajanda = new Ajanda
        {
            Id = 5,
            Baslik = "Toplantı",
            Aciklama = "desc",
            Konum = "Salon",
            BaslangicTarihi = bas,
            BitisTarihi = bas.AddHours(1),
            TumGun = false,
            IsDeleted = true,
            RandevuTipId = 3,
            DurumId = 2,
            Status = AjandaStatus.Pending
        };

        var dto = _mapper.Map<AjandaDto>(ajanda);

        Assert.Equal(5, dto.Id);
        Assert.Equal("Toplantı", dto.Baslik);
        Assert.Equal("desc", dto.Aciklama);
        Assert.Equal(bas, dto.BaslangicTarihi);
        Assert.Equal(bas.AddHours(1), dto.BitisTarihi);
        Assert.True(dto.IsDeleted); // yeni eklenen alan da eşleşiyor
        Assert.Equal(3, dto.RandevuTipId);
    }

    [Fact]
    public void Koleksiyon_Mapleme()
    {
        var list = new List<Oneri>
        {
            new() { Id = 1, Baslik = "A", Tip = OneriTip.Istek },
            new() { Id = 2, Baslik = "B", Tip = OneriTip.Bilgi },
        };

        var dtos = _mapper.Map<IEnumerable<OneriDto>>(list).ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Equal("A", dtos[0].Baslik);
        Assert.Equal(2, dtos[1].Id);
        Assert.Equal(OneriTip.Bilgi, dtos[1].Tip);
    }

    [Fact]
    public void YerindeGuncelleme_Map_DtoToEntity()
    {
        // Servislerdeki UpdateAsync deseni: _mapper.Map(dto, entity)
        var entity = new Oneri { Id = 9, Baslik = "Eski", Aciklama = "eski" };
        var dto = new OneriDto
        {
            Id = 9,
            Baslik = "Yeni",
            Aciklama = "yeni",
            Tip = OneriTip.Diger
        };

        var sonuc = _mapper.Map(dto, entity);

        Assert.Same(entity, sonuc); // aynı takip edilen örneği döndürür
        Assert.Equal("Yeni", entity.Baslik); // yerinde güncellenir
        Assert.Equal("yeni", entity.Aciklama);
        Assert.Equal(OneriTip.Diger, entity.Tip);
    }

    [Fact]
    public void Adapt_Extension_GlobalConfigIleCalisir()
    {
        // Uygulama ProjectToType/Adapt için global TypeAdapterConfig kullanır.
        var oneri = new Oneri { Id = 100, Baslik = "Adapt" };
        var dto = oneri.Adapt<OneriDto>();

        Assert.Equal(100, dto.Id);
        Assert.Equal("Adapt", dto.Baslik);
    }
}
