using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using KentOS.Mini.Web.Extensions;
using KentOS.Mini.Web.Models;

namespace KentOS.Mini.Web.Services
{
    public class SettingsService(
        AppDbContext _context,
        IMapper _mapper, 
        IMemoryCache _memoryCache,
        ICicekciService _cicekciService,
        ICurrentUserService _currentUserService) : ISettingsService
    {
        public async Task<IEnumerable<AjandaDurumDto>> GetAjandaDurumlarAsync()
        {
            if (!_memoryCache.TryGetValue(CacheKeys.AjandaDurum, out IEnumerable<AjandaDurumDto> ajandaDurumlar))
            {
                ajandaDurumlar = _mapper.Map<IEnumerable<AjandaDurumDto>>(await _context.AjandaDurumlar.OrderBy(a => a.Id).ToListAsync());
                _memoryCache.Set(CacheKeys.AjandaDurum, ajandaDurumlar);
            }

            return ajandaDurumlar ?? Enumerable.Empty<AjandaDurumDto>();

            //return _mapper.Map<IEnumerable<AjandaDurumDto>>(await _context.AjandaDurumlar.ToListAsync());
        }

        public async Task<IEnumerable<MahalleDto>> GetMahallelerAsync()
        {
            if (!_memoryCache.TryGetValue(CacheKeys.Mahalle, out IEnumerable<MahalleDto> mahalleler))
            {
                mahalleler = _mapper.Map<IEnumerable<MahalleDto>>(await _context.Mahalleler.OrderBy(m => m.Id).ToListAsync());
                _memoryCache.Set(CacheKeys.Mahalle, mahalleler);
            }

            return mahalleler ?? Enumerable.Empty<MahalleDto>();
            //return _mapper.Map<IEnumerable<MahalleDto>>(await _context.Mahalleler.ToListAsync());
        }

        public async Task<IEnumerable<MeslekDto>> GetMesleklerAsync()
        {
            if (!_memoryCache.TryGetValue(CacheKeys.Meslek, out IEnumerable<MeslekDto> meslekler))
            {
                meslekler = _mapper.Map<IEnumerable<MeslekDto>>(await _context.Meslekler.OrderBy(m => m.Id).ToListAsync());
                _memoryCache.Set(CacheKeys.Meslek, meslekler);
            }

            return meslekler ?? Enumerable.Empty<MeslekDto>();
            //return _mapper.Map<IEnumerable<MeslekDto>>(await _context.Meslekler.ToListAsync());
        }

        public async Task<IEnumerable<RandevuDurumDto>> GetRandevuDurumlarAsync()
        {
            if (!_memoryCache.TryGetValue(CacheKeys.RandevuDurum, out IEnumerable<RandevuDurumDto> randevuDurumlar))
            {
                randevuDurumlar = _mapper.Map<IEnumerable<RandevuDurumDto>>(await _context.RandevuDurumlar.OrderBy(r =>r.Id).ToListAsync());
                _memoryCache.Set(CacheKeys.RandevuDurum, randevuDurumlar);
            }

            return randevuDurumlar ?? Enumerable.Empty<RandevuDurumDto>();
            //return _mapper.Map<IEnumerable<RandevuDurumDto>>(await _context.RandevuDurumlar.ToListAsync());
        }

        public async Task<IEnumerable<RandevuTipDto>> GetRandevuTiplerAsync()
        {
            if (!_memoryCache.TryGetValue(CacheKeys.RandevuTip, out IEnumerable<RandevuTipDto> randevuTipler))
            {
                randevuTipler = _mapper.Map<IEnumerable<RandevuTipDto>>(await _context.RandevuTipleri.OrderBy(r=>r.Id).ToListAsync());
                _memoryCache.Set(CacheKeys.RandevuTip, randevuTipler);
            }

            return randevuTipler ?? Enumerable.Empty<RandevuTipDto>();
            //return _mapper.Map<IEnumerable<RandevuTipDto>>(await _context.RandevuTipleri.ToListAsync());
        }

        //update or create fcm token
        public async Task UpdateFcmTokenAsync(string fcmToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == _currentUserService.GetUsername());
            if (user == null)
            {
                throw new EntityNotFoundException("Kullanıcı bulunamadı");
            }
            user.FcmToken = fcmToken;
            await _context.SaveChangesAsync();
        }

        public Task LoadAllAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<BirimDto>> GetBirimlerAsync()
        {
            if (!_memoryCache.TryGetValue(CacheKeys.Birim, out IEnumerable<BirimDto> birimler))
            {
                birimler = _mapper.Map<IEnumerable<BirimDto>>(await _context.Birimler.OrderBy(b => b.Id).ToListAsync());
                _memoryCache.Set(CacheKeys.Birim, birimler);
            }

            return birimler ?? Enumerable.Empty<BirimDto>();
        }

        public async Task<IEnumerable<BirimDto>> GetAltBirimlerAsync()
        {
            var birimler = await _context.Birimler.Where(b => b.UstBirimId == _currentUserService.GetCurrentBirimId()).ToListAsync();
            return _mapper.Map<IEnumerable<BirimDto>>(birimler);
        }

        /// <summary>
        /// Etkinliğe katılımcı olarak eklenebilecek birimler.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Kullanıcının kendi seviyesindekiler ve altındakiler
        /// (<c>Level &gt;= kendi seviyesi</c>). Üsttekiler listelenmez: bir
        /// müdürlük başkan yardımcısını kendi toplantısına çağıramaz, o davet
        /// yukarıdan gelir.
        /// </para>
        /// <para>
        /// Kullanıcının KENDİ birimi de listede yok: etkinlik zaten o birime
        /// ait ve kendini davet etmek anlamsız.
        /// </para>
        /// </remarks>
        public async Task<IEnumerable<BirimDto>> GetKatilimciBirimlerAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();

            var seviye = await _context.Birimler
                .Where(b => b.Id == birimId)
                .Select(b => (int?)b.Level)
                .FirstOrDefaultAsync();

            // Birimi çözülemeyen kullanıcıya boş liste: yanlışlıkla TÜM
            // birimleri açmak, gizli etkinliği herkese davet edilebilir kılardı.
            if (seviye is null)
            {
                return [];
            }

            var birimler = await _context.Birimler
                .AsNoTracking()
                .Where(b => b.Level >= seviye.Value && b.Id != birimId)
                .OrderBy(b => b.Level).ThenBy(b => b.Ad)
                .ToListAsync();

            return _mapper.Map<IEnumerable<BirimDto>>(birimler);
        }

        /// <summary>
        /// Oturum açan kullanıcının BİRİMİNDEKİ kullanıcılar — gizli etkinlik
        /// katılımcı seçicisini besler.
        ///
        /// <see cref="UserDto"/> yerine <see cref="KatilimciDto"/> döner: o DTO
        /// FcmToken/e-posta/telefon taşıyor ve bu liste birimdeki herkese açık
        /// olduğu için oraya cihaz/iletişim bilgisi konmamalı.
        /// Kullanıcının kendisi listede YER ALMAZ; ekleyen kişi zaten her zaman
        /// etkinliği görür ve bildirim alır.
        /// </summary>
        public async Task<IEnumerable<KatilimciDto>> GetBirimKullanicilariAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            if (birimId == 0)
            {
                return Enumerable.Empty<KatilimciDto>();
            }

            var kullaniciAdi = _currentUserService.GetUsername();

            return await _context.Users
                .Where(u => u.BirimId == birimId && u.UserName != kullaniciAdi)
                .OrderBy(u => u.Ad).ThenBy(u => u.Soyad)
                .Select(u => new KatilimciDto
                {
                    Id = u.Id,
                    Ad = u.Ad,
                    Soyad = u.Soyad,
                    Unvan = u.Unvan,
                    BirimAd = u.Birim != null ? u.Birim.Ad : null
                })
                .ToListAsync();
        }

        public async Task<BirimDto> GetUstBirimAsync()
        {
            var birim = await _context.Birimler.FirstOrDefaultAsync(b => b.Id == _currentUserService.GetCurrentBirimId());
            return birim == null ? throw new EntityNotFoundException("Birim bulunamadı") : _mapper.Map<BirimDto>(birim);
        }

        public async Task<IEnumerable<BirimDto>> GetAltBirimlerTreeAsync()
        {
            //add fake delay
            await Task.Delay(0);
            var birimler =  _context.Birimler.GetDescendants(_currentUserService.GetCurrentBirimId(), true).ToList();
            return _mapper.Map<IEnumerable<BirimDto>>(birimler);
        }

        public async Task<IEnumerable<CicekciDto>> GetCicekcilerAsync()
        {
            return await _cicekciService.GetAllAsync();
        }
    }
}
