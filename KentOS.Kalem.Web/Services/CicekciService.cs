using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Services
{
    public class CicekciService(
        AppDbContext _context,
        IAjandaService _ajandaService,
        ICurrentUserService _currentUserService,
        IMapper _mapper,
        Storage.IFileStorage _fileStorage
        ) : ICicekciService
    {
        public async Task<CicekciDto> CreateAsync(CicekciDto cicekciDto)
        {
            var cicekci = _mapper.Map<Cicekci>(cicekciDto);
            _context.Cicekciler.Add(cicekci);
            await _context.SaveChangesAsync();
            return _mapper.Map<CicekciDto>(cicekci);
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var cicekci = await _context.Cicekciler.FindAsync(id);
            if (cicekci == null)
            {
                return false;
            }

            _context.Cicekciler.Remove(cicekci);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CicekciDto>> GetAllAsync()
        {
            var cicekciler = await _context.Cicekciler.ToListAsync();
            return _mapper.Map<IEnumerable<CicekciDto>>(cicekciler);
        }

        public async Task<CicekciDto> GetByIdAsync(long id)
        {
            var cicekci = await _context.Cicekciler.FindAsync(id);
            if (cicekci == null)
            {
                throw new EntityNotFoundException("Çiçekçi bulunamadı.");
            }

            return _mapper.Map<CicekciDto>(cicekci);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Cicekciler.CountAsync();
        }

        public async Task<IEnumerable<CicekDto>> GetCiceklerAsync(long cicekciId)
        {
            var cicekler = await _context.Cicekler.Where(c => c.CicekciId == cicekciId).ToListAsync();
            return _mapper.Map<IEnumerable<CicekDto>>(cicekler);
        }

        public async Task<CicekciDto> UpdateAsync(CicekciDto cicekciDto)
        {
            var cicekci = await _context.Cicekciler.FindAsync(cicekciDto.Id);
            if (cicekci == null)
            {
                throw new EntityNotFoundException("Çiçekçi bulunamadı.");
            }

            _mapper.Map(cicekciDto, cicekci);

            await _context.SaveChangesAsync();
            return _mapper.Map<CicekciDto>(cicekci);
        }

        public async Task<CicekKartDto> GetCicekKartAsync(string guid)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid);
            if (cicek == null)
            {
                throw new EntityNotFoundException("Çiçek kartı bulunamadı");
            }

            var ajanda = await _ajandaService.GetByIdWithoutUserRestrictionAsync(cicek.AjandaId);

            if (ajanda == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");

            }

            var cicekKart = new CicekKartDto
            {
                Cicek = _mapper.Map<CicekDto>(cicek),
                Ajanda = _mapper.Map<AjandaDto>(ajanda)
            };

            return cicekKart;
        }

        /// <summary>
        /// Çiçekçinin gördüğü teslim kartı — <b>doğrulama kodu taşımaz</b>.
        /// </summary>
        public async Task<Application.Dto.V2.Cicek.CicekTeslimKartiDto> TeslimKartiAsync(string guid)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid);
            if (cicek == null)
            {
                throw new EntityNotFoundException("Çiçek kartı bulunamadı");
            }

            var ajanda = await _ajandaService.GetByIdWithoutUserRestrictionAsync(cicek.AjandaId);
            if (ajanda == null)
            {
                throw new EntityNotFoundException("Etkinlik bulunamadı");
            }

            var kurum = await _context.KurumBilgileri.AsNoTracking().FirstOrDefaultAsync();

            return new Application.Dto.V2.Cicek.CicekTeslimKartiDto
            {
                EtkinlikBasligi = ajanda.Baslik ?? string.Empty,
                EtkinlikTarihi = ajanda.BaslangicTarihi,
                EtkinlikKonumu = ajanda.Konum,
                Alici = cicek.Ad,
                Adres = cicek.Adres,
                Not = cicek.Aciklama,
                KurumAdi = kurum?.DisplayName ?? kurum?.Name,
                TeslimEdildi = cicek.Gonderildi,
                TeslimTarihi = cicek.Gonderildi ? cicek.GonderilmeTarihi : null,
                Fotograf = cicek.Resim,
            };
        }

        /// <summary>
        /// Kartı teslim edildi olarak işaretler.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>DENEME SINIRI VAR.</b> Uç anonim (çiçekçinin hesabı yok) ve kod
        /// beş haneli; sınırsız deneme, kodu kaba kuvvetle bulmayı birkaç
        /// dakikalık işe çevirirdi. Beş yanlış denemeden sonra kart kilitlenir
        /// ve talimatı veren personelin yeniden göndermesi gerekir.
        /// </para>
        /// <para>
        /// Sayaç veritabanında: uygulama birden çok sunucuda çalışabiliyor ve
        /// bellekte tutulan bir sayaç, isteği başka örneğe göndererek
        /// aşılabilirdi.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Çiçekçinin gördüğü uçtan teslim — isteğe bağlı fotoğrafla.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Fotoğraf da doğrulama kodu ister.</b> Teslim işaretlemesi
        /// devretmeli (aynı kartı ikinci kez işaretlemek hata değil) ama
        /// fotoğraf öyle değil: kod aranmasaydı, bağlantıyı bilen herkes
        /// teslim edilmiş bir kartın fotoğrafının üzerine yazabilirdi.
        /// </para>
        /// <para>
        /// <b>Yalnızca resim.</b> Uzantı beyaz listeyle sınırlı; sunucu
        /// tarafındaki ikinci savunma <c>Middleware/YuklemeGuvenligi</c> ama
        /// burada anlaşılır bir hata vermek daha iyi. Boyut sınırı da var:
        /// uç anonim, yani sınırsız yükleme diskin tamamını doldurabilirdi.
        /// </para>
        /// </remarks>
        public async Task<Application.Dto.V2.Cicek.CicekTeslimKartiDto> TeslimEtAsync(
            string guid, int dogrulamaKodu, Stream? fotograf, string? dosyaAdi, string? icerikTipi)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid)
                ?? throw new EntityNotFoundException("Çiçek kartı bulunamadı");

            // Kod DAİMA denetlenir; teslim edilmiş kart da fotoğraf alabiliyor.
            KoduDogrula(cicek);

            if (fotograf is not null)
            {
                cicek.Resim = await FotografiKaydetAsync(guid, fotograf, dosyaAdi, icerikTipi);
            }

            if (!cicek.Gonderildi)
            {
                cicek.Gonderildi = true;
                cicek.GonderilmeTarihi = DateTime.Now;
            }

            cicek.GuncellemeTarihi = DateTime.Now;
            _context.Cicekler.Update(cicek);
            await _context.SaveChangesAsync();

            return await TeslimKartiAsync(guid);

            void KoduDogrula(Cicek k)
            {
                const int denemeSiniri = 5;

                if (k.DogrulamaDenemesi >= denemeSiniri)
                {
                    throw new BusinessRuleException(
                        "Çok fazla hatalı deneme yapıldı. Talimatı veren personelle görüşün.");
                }

                if (k.DogrulamaKodu == dogrulamaKodu) return;

                k.DogrulamaDenemesi++;
                _context.Cicekler.Update(k);
                _context.SaveChanges();

                var kalan = denemeSiniri - k.DogrulamaDenemesi;
                throw new BusinessRuleException(
                    kalan > 0
                        ? $"Doğrulama kodu hatalı. {kalan} deneme hakkınız kaldı."
                        : "Doğrulama kodu hatalı. Deneme hakkınız bitti.");
            }
        }

        /// <summary>İzin verilen teslim fotoğrafı uzantıları.</summary>
        private static readonly string[] FotografUzantilari = [".jpg", ".jpeg", ".png", ".webp", ".heic"];

        /// <summary>Teslim fotoğrafı üst sınırı — uç ANONİM.</summary>
        private const long FotografSiniri = 12 * 1024 * 1024;

        private async Task<string> FotografiKaydetAsync(
            string guid, Stream akis, string? dosyaAdi, string? icerikTipi)
        {
            var uzanti = Path.GetExtension(dosyaAdi ?? string.Empty).ToLowerInvariant();

            if (!FotografUzantilari.Contains(uzanti))
            {
                throw new BusinessRuleException(
                    "Yalnızca fotoğraf yükleyebilirsiniz (JPG, PNG, WEBP, HEIC).");
            }

            using var bellek = new MemoryStream();
            await akis.CopyToAsync(bellek);

            if (bellek.Length == 0)
            {
                throw new BusinessRuleException("Fotoğraf okunamadı.");
            }

            if (bellek.Length > FotografSiniri)
            {
                throw new BusinessRuleException("Fotoğraf çok büyük (en fazla 12 MB).");
            }

            /*
              AD KARTIN GUID'İNDEN TÜRETİLİR, kullanıcının dosya adından
              değil: gelen ad yol ayracı taşıyabiliyor ve aynı kart yeniden
              yüklendiğinde eskisinin üzerine yazılması isteniyor — her
              denemede yeni bir dosya bırakmak diskte çöp biriktirirdi.
            */
            var anahtar = $"uploads/cicek/{guid}{uzanti}";

            await _fileStorage.SaveAsync(
                Storage.StorageArea.Public, anahtar, bellek.ToArray(), icerikTipi);

            return "/" + anahtar;
        }

        public async Task<bool> CicekKartGonderildiAsync(string guid, int dogrulamaKodu)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.Guid == guid);
            if (cicek == null)
            {
                throw new EntityNotFoundException("Çiçek kartı bulunamadı");
            }

            // Zaten teslim edilmişse ikinci kez işaretlemek bir şey değiştirmez;
            // hata yerine başarı dönmek çiçekçinin sayfayı yenilemesini
            // "kod yanlış" gibi göstermiyor.
            if (cicek.Gonderildi)
            {
                return true;
            }

            const int denemeSiniri = 5;
            if (cicek.DogrulamaDenemesi >= denemeSiniri)
            {
                throw new BusinessRuleException(
                    "Çok fazla hatalı deneme yapıldı. Talimatı veren personelle görüşün.");
            }

            if (cicek.DogrulamaKodu != dogrulamaKodu)
            {
                cicek.DogrulamaDenemesi++;
                _context.Cicekler.Update(cicek);
                await _context.SaveChangesAsync();

                var kalan = denemeSiniri - cicek.DogrulamaDenemesi;
                throw new BusinessRuleException(
                    kalan > 0
                        ? $"Doğrulama kodu hatalı. {kalan} deneme hakkınız kaldı."
                        : "Doğrulama kodu hatalı. Deneme hakkınız bitti.");
            }
            cicek.Gonderildi = true;
            cicek.GonderilmeTarihi = DateTime.Now;
            _context.Cicekler.Update(cicek);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddCicekAsync(long cicekciId, CicekDto cicekDto)
        {

            if (cicekDto.AjandaId == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı.");
            }

            var cicekGonderDto = new AjandaCicekGonderDto
            {
                CicekciId = cicekciId,
                AjandaId = cicekDto.AjandaId ?? 0,
                Not = cicekDto.Aciklama,
            };

            await _ajandaService.CicekGonderAsync(cicekGonderDto);

            return true;
        }

    }
}
