using Mapster;
using MapsterMapper;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Web.Mapping;
using Xunit;

namespace KentOS.Mini.Tests;

/// <summary>
/// Döngüsel navigasyon içeren entity grafiklerinin DTO'ya eşlenmesini doğrular.
///
/// REGRESYON: Mapster geçişinden sonra Ajanda kaydı oluşturulurken EF ilişki
/// düzeltmesi <c>AjandaNot.Ajanda</c> geri referansını dolduruyor, Mapster bu
/// döngüyü klonlarken StackOverflowException ile TÜM API sürecini çökertiyordu.
/// Kayıt veritabanına yazıldığı hâlde istemciye yanıt dönmüyordu.
/// <see cref="MapsterConfig"/> kuralları bu döngüyü keser.
///
/// NOT: Bu test başarısız olursa süreç StackOverflow ile ölür; xunit "Test Run
/// Aborted" der. Assert'lerin çalışması bile düzeltmenin kanıtıdır.
/// </summary>
public class MapsterCycleTests
{
    private readonly IMapper _mapper;

    public MapsterCycleTests()
    {
        var config = new TypeAdapterConfig();
        MapsterConfig.Register(config);
        _mapper = new Mapper(config);
    }

    private static Ajanda AjandaWithCycle()
    {
        var ajanda = new Ajanda
        {
            Id = 5,
            Baslik = "Deneme",
            BaslangicTarihi = new DateTime(2026, 8, 11, 18, 50, 0),
            BitisTarihi = new DateTime(2026, 8, 11, 19, 20, 0),
        };

        // EF fixup'ın izlenen grafikte yaptığı şey: geri referanslar dolu.
        ajanda.AjandaNotlar.Add(new AjandaNot
        {
            Id = 1,
            Not = "➕ EKLENDİ",
            AjandaId = ajanda.Id,
            Ajanda = ajanda,
        });
        ajanda.Photos.Add(new AjandaPhoto
        {
            Id = 2,
            AjandaId = ajanda.Id,
            Ajanda = ajanda,
        });
        ajanda.Cicek = new Cicek { Id = 3, AjandaId = ajanda.Id, Ajanda = ajanda };
        ajanda.CicekId = 3;

        return ajanda;
    }

    [Fact]
    public void Ajanda_DonguselGrafik_AjandaDtoya_Eslenebilir()
    {
        var ajanda = AjandaWithCycle();

        var dto = _mapper.Map<AjandaDto>(ajanda);

        Assert.Equal(5, dto.Id);
        Assert.Equal("Deneme", dto.Baslik);
        Assert.Single(dto.Photos);
        Assert.NotNull(dto.Cicek);
        // AjandaDto artık NOT TAŞIMAZ (notlar ayrı uçtan alınır) ve `cicek`
        // alanı entity değil CicekDto'dur — döngü kaynağında yok.
        Assert.Equal(3, dto.Cicek!.Id);
        Assert.Equal(5, dto.Cicek!.AjandaId);
    }

    [Fact]
    public void AjandaHareket_DonguselGrafik_DtoyaEslenebilir()
    {
        var hareket = new AjandaHareket
        {
            Id = 9,
            AjandaId = 5,
            Ajanda = AjandaWithCycle(),
        };

        var dto = _mapper.Map<Application.Dto.Randevu.AjandaHareketDto>(hareket);

        Assert.Equal(9, dto.Id);
        Assert.NotNull(dto.Ajanda);
    }

    [Fact]
    public void Cicek_DonguselGrafik_CicekDtoya_Eslenebilir()
    {
        var ajanda = AjandaWithCycle();

        var dto = _mapper.Map<CicekDto>(ajanda.Cicek!);

        Assert.Equal(3, dto.Id);
        Assert.Equal(5, dto.AjandaId);
    }

    /// <summary>
    /// PreserveReference, IQueryable projeksiyonlarını (RandevuService yoğun
    /// biçimde ProjectToType kullanıyor) bozmamalı.
    /// </summary>
    [Fact]
    public void ProjectToType_PreserveReferenceIle_Calisir()
    {
        var config = new TypeAdapterConfig();
        MapsterConfig.Register(config);

        var randevular = new List<Randevu>
        {
            new() { Id = 1, Konu = "Test", Ad = "Ali", Soyad = "Veli", BirimId = 8 },
        }.AsQueryable();

        var projected = randevular.ProjectToType<RandevuDto>(config).ToList();

        Assert.Single(projected);
        Assert.Equal("Test", projected[0].Konu);
        Assert.Equal("Ali", projected[0].Ad);
    }

    /// <summary>
    /// Entity grafiğindeki DİĞER döngü aileleri de aynı sınıf risktir:
    /// Randevu ⇄ Birim/RandevuTip/Mahalle/RandevuDurum/Notlar/Dosyalar/Hareketler.
    /// PreserveReference bunların hepsini genel olarak keser.
    /// </summary>
    [Fact]
    public void Randevu_TumGeriReferanslarDolu_RandevuDtoya_Eslenebilir()
    {
        // Koleksiyonlar entity'lerde varsayılan olarak null olabildiği için
        // burada açıkça oluşturulur (EF çalışma zamanında kendisi doldurur).
        var birim = new Birim { Id = 8, Ad = "Özel Kalem", Yetkili = "Yetkili", Randevular = new List<Randevu>() };
        var tip = new RandevuTip { Id = 3, Ad = "Görüşme Talebi", Randevular = new List<Randevu>() };
        var durum = new RandevuDurum { Id = 1, DurumAd = "Beklemede", Randevular = new List<Randevu>() };
        var mahalle = new Mahalle { Id = 1, Ad = "Merkez", Randevular = new List<Randevu>() };

        var randevu = new Randevu
        {
            Id = 152,
            Konu = "Görüşme",
            Ad = "Test",
            Soyad = "Kullanıcı",
            BirimId = birim.Id,
            Birim = birim,
            RandevuTipId = tip.Id,
            RandevuTip = tip,
            RandevuDurumId = durum.Id,
            RandevuDurum = durum,
            MahalleId = mahalle.Id,
            Mahalle = mahalle,
        };

        // EF fixup'ın kurduğu çift yönlü bağlar.
        birim.Randevular!.Add(randevu);
        tip.Randevular!.Add(randevu);
        durum.Randevular!.Add(randevu);
        mahalle.Randevular!.Add(randevu);
        randevu.Notlar!.Add(new RandevuNot { Id = 1, Not = "not", RandevuId = randevu.Id, Randevu = randevu });
        randevu.Dosyalar!.Add(new RandevuDosya { Id = 2, Ad = "dosya.pdf", RandevuId = randevu.Id, Randevu = randevu });
        randevu.Hareketler!.Add(new RandevuHareket { Id = 3, RandevuId = randevu.Id, Randevu = randevu });

        var dto = _mapper.Map<RandevuDto>(randevu);

        Assert.Equal(152, dto.Id);
        Assert.Equal("Görüşme", dto.Konu);
        Assert.Equal(8, dto.BirimId);
    }

    /// <summary>Birim kendi kendine döngüseldir (UstBirim ⇄ AltBirimler).</summary>
    [Fact]
    public void Birim_UstVeAltBirimDolu_BirimDtoya_Eslenebilir()
    {
        var ust = new Birim { Id = 1, Ad = "Başkanlık", Yetkili = "Başkan" };
        var alt = new Birim { Id = 8, Ad = "Özel Kalem", Yetkili = "Yetkili", UstBirimId = 1, UstBirim = ust };
        ust.AltBirimler!.Add(alt);

        var dto = _mapper.Map<BirimDto>(alt);

        Assert.Equal(8, dto.Id);
        Assert.Equal(1, dto.UstBirimId);
    }

    /// <summary>
    /// TERS YÖN: DTO → entity. Gövdede `cicek` gelirse aynı döngü riski oluşur.
    /// </summary>
    [Fact]
    public void AjandaDto_CicekIle_Ajanda_Entityye_Eslenebilir()
    {
        var ajanda = AjandaWithCycle();
        var dto = new AjandaDto
        {
            Id = 5,
            Baslik = "Deneme",
            Cicek = _mapper.Map<CicekDto>(ajanda.Cicek!),
        };

        var entity = _mapper.Map<Ajanda>(dto);

        Assert.Equal(5, entity.Id);
        Assert.NotNull(entity.Cicek);
    }
}
