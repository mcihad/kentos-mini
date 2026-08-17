using Mapster;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Models;

namespace KentOS.Mini.Web.Mapping
{
    /// <summary>
    /// Mapster global eşleme kuralları. Program.cs ile testler AYNI kuralları
    /// kullansın diye tek yerde toplanmıştır.
    /// </summary>
    /// <remarks>
    /// NEDEN: Bazı DTO'lar (AjandaDto.AjandaNotlar / AjandaDto.Cicek,
    /// CicekDto.Ajanda, AjandaHareketDto.Ajanda) entity tiplerini doğrudan
    /// taşıyor. EF, izlenen (tracked) bir Ajanda'ya not eklendiğinde ilişki
    /// düzeltmesi (fixup) yapıp <c>AjandaNot.Ajanda</c> geri referansını
    /// dolduruyor; böylece Ajanda ⇄ AjandaNot döngüsü oluşuyor. Aynı durum
    /// Ajanda ⇄ Cicek, Ajanda ⇄ AjandaPhoto ve Birim ⇄ Birim için de geçerli.
    ///
    /// Mapster döngüsel referansları varsayılan olarak KLONLAR; bu da sonsuz
    /// özyineleme ve yakalanamayan <see cref="StackOverflowException"/> demektir.
    /// Sonuç: kayıt veritabanına yazıldıktan sonra API süreci komple çöküyor,
    /// istemciye hiç yanıt dönmüyor (mobilde "DioExceptionType.unknown",
    /// response null) ve o anda uçuşta olan diğer istekler de düşüyor.
    /// AutoMapper döngüleri kendiliğinden çözdüğü için sorun Mapster geçişiyle
    /// ortaya çıktı.
    /// </remarks>
    public static class MapsterConfig
    {
        public static void Register(TypeAdapterConfig config)
        {
            // 1) Genel güvenlik ağı: aynı nesneyi ikinci kez eşlemek yerine ilk
            //    klonu yeniden kullan. AutoMapper'ın PreserveReferences
            //    davranışının Mapster karşılığı; ileride eklenecek yeni geri
            //    referanslarda da süreç çökmesini engeller.
            config.Default.PreserveReference(true);

            // 2) Bilinen geri referansları eşlemenin tamamen dışında bırak:
            //    yanıt gövdesi küçülür ve döngü kaynağında kesilir.
            //    NOT: `CicekDto.Ajanda` ve `AjandaDto.AjandaNotlar` alanları
            //    tamamen kaldırıldı; artık kural gerekmiyor.
            config.ForType<AjandaNot, AjandaNot>().Ignore(x => x.Ajanda!);
            config.ForType<AjandaPhoto, AjandaPhoto>().Ignore(x => x.Ajanda!);
            config.ForType<Cicek, Cicek>().Ignore(x => x.Ajanda!);

            // 3) Gizli etkinlik katılımcıları ve tekrar serisi: yeni geri
            //    referanslar (AjandaKatilimci.Ajanda, AjandaSeri.Tekrarlar) aynı
            //    döngü tuzağını kurar. Ayrıca isim benzerliği tehlikeli:
            //    - Entity `Ajanda.Katilimcilar` (AjandaKatilimci) ile DTO
            //      `AjandaDto.Katilimcilar` (KatilimciDto) AYNI ada sahip ama farklı
            //      anlamda: birinde `Id` bağlantı satırının kimliği, diğerinde
            //      KULLANICI kimliği. Mapster bunları eşlerse katılımcı listesi
            //      yanlış kimliklerle döner ve DTO→entity yönünde var olan satırlara
            //      sahte PK atanır. Bu yüzden HER İKİ YÖNDE de kapatılır; DTO'daki
            //      liste servis içinde elle doldurulur (AjandaService.KatilimcilariYukle).
            config.ForType<AjandaKatilimci, AjandaKatilimci>().Ignore(x => x.Ajanda!);
            config.ForType<Ajanda, AjandaDto>().Ignore(x => x.Katilimcilar!);
            config.ForType<AjandaDto, Ajanda>()
                .Ignore(x => x.Katilimcilar!)
                .Ignore(x => x.Seri!)
                // Seri alanları yalnızca sunucu tarafında (AjandaSeriService)
                // yönetilir; istemciden gelen değerler yoksayılır.
                .Ignore(x => x.SeriId!)
                .Ignore(x => x.SeriOrijinalBaslangic!)
                .Ignore(x => x.SeriAyrik)
                // KÜNYE alanları da istemciden GELMEZ.
                //
                // `_mapper.Map(dto, entity)` güncellemede var olan satırın
                // üstüne yazıyor: gövdede olmayan her alan varsayılana düşüyor.
                // `KullaniciId` böyle silinmişti ve gizliye çevrilen etkinlik
                // OLUŞTURANIN gözünden kayboluyordu. Aynı tuzak oluşturma
                // tarihinde de var: gövdede yoksa 0001-01-01 yazılırdı.
                // Servis bu alanları zaten kendisi yönetiyor.
                .Ignore(x => x.KullaniciId!)
                .Ignore(x => x.OlusturmaTarihi)
                .Ignore(x => x.GuncellemeTarihi);
            config.ForType<AjandaSeri, AjandaSeri>().Ignore(x => x.Tekrarlar!);
            config.ForType<Ajanda, Ajanda>()
                .Ignore(x => x.Katilimcilar!)
                .Ignore(x => x.Seri!);
        }
    }
}
