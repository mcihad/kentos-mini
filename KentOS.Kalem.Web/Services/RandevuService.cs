using MapsterMapper;
using Mapster;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Dto.Analiz;
using KentOS.Kalem.Application.Dto.Randevu;
using KentOS.Kalem.Application.Dto.ViewModels;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;
using KentOS.Kalem.Web.Extensions;
using System.Diagnostics;
using System.Text.Json;

namespace KentOS.Kalem.Web.Services
{
    public class RandevuService(
        AppDbContext context,
        ICurrentUserService _currentUserService,
        IMessageService _messageService,
        ILogger<RandevuService> _logger,
        Storage.IFileStorage _fileStorage,
        Services.V2.IOzgecmisServisi _ozgecmisHavuzu,
        IMapper mapper) : IRandevuService
    {
        public async Task<RandevuDto> CreateAsync(RandevuDto randevuDto)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var randevu = mapper.Map<Randevu>(randevuDto);
            var author = await _currentUserService.GetFullNameAsync();
            randevu.OlusturmaTarih = DateTime.Now;
            randevu.Olusturan = _currentUserService.GetUsername();
            randevu.BirimId = birimId;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = author;

            await context.Randevular.AddAsync(randevu);
            await context.SaveChangesAsync();

            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0, "Yeni Talep", $"{randevu.Konu} konulu yeni bir talep oluşturuldu. {author} tarafından.", SendMessageType.PushNotification,NotifikasyonTip.RequestOnCreated, data.ToJson());

            return mapper.Map<RandevuDto>(randevu);
        }

        public async Task<RandevuDto> CreateHavaleAsync(RandevuHavaleDto randevuHavaleDto)
        {
            var currentBirim = _currentUserService.GetCurrentBirimId();
            var randevu = await context.Randevular.FirstOrDefaultAsync(r => r.Id == randevuHavaleDto.Id && r.BirimId == currentBirim);

            if (randevu == null)
            {
                throw new EntityNotFoundException($"Randevu bulunamadı. Id:{randevuHavaleDto.Id}");
            }

            // Update department
            randevu.BirimId = randevuHavaleDto.BirimId;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = _currentUserService.GetUsername();

            // Create havale note
            var not = new RandevuNot
            {
                RandevuId = randevu.Id,
                Tip = "Havale",
                Not = randevuHavaleDto.Not,
                Tarih = DateTime.Now,
                Olusturan = _currentUserService.GetUsername()
            };


            context.Randevular.Update(randevu);
            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();
            var author = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            await _messageService.CreateForAllPersonAsync(randevu.BirimId ?? 0, "Talep Havale", $"{randevu.Konu} konulu talep başka bir birime havale edildi. {author} tarafından.", SendMessageType.PushNotification,NotifikasyonTip.RequestOnRemittance, data.ToJson());
            //create for new department
            await _messageService.CreateForAllPersonAsync(randevuHavaleDto.BirimId, "Talep Havale", $"{randevu.Konu} konulu talep size havale edildi. {author} tarafından.", SendMessageType.PushNotification,NotifikasyonTip.RequestOnRemittance, data.ToJson());
            return mapper.Map<RandevuDto>(randevu);
        }

        public async Task<bool> SendToParentAsync(long randevuId)
        {
            var randevu = await context.Randevular.FindAsync(randevuId);
            if (randevu == null)
            {
                return false;
            }

            var ustBirimId = await context.Birimler
                .Where(b => b.Id == randevu.BirimId)
                .Select(b => b.UstBirimId)
                .FirstOrDefaultAsync();

            if (ustBirimId == null || ustBirimId == 0)
            {
                // Kök birimin üstü yok. Eskiden birim_id NULL'a çekilip
                // `randevu_birim_change` tetikleyicisi NOT NULL kolona NULL
                // yazmaya çalışıyor ve istek 500 ile düşüyordu.
                _logger.LogWarning(
                    "Talep {RandevuId} üst birime gönderilemedi: {BirimId} biriminin üst birimi yok.",
                    randevuId, randevu.BirimId);
                return false;
            }

            randevu.BirimId = ustBirimId;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = _currentUserService.GetUsername();

            context.Randevular.Update(randevu);
            await context.SaveChangesAsync();

            var author = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0,
                "Talep Havale", $"{randevu.Konu} konulu talep üst birime havale edildi. {author} tarafından.",
                SendMessageType.PushNotification,
                NotifikasyonTip.RequestOnRemittance,
                data.ToJson());

            var not = new RandevuNot
            {
                RandevuId = randevuId,
                Tip = "Havale",
                Not = $"{author} tarafından üst birime havale edildi.",
                Tarih = DateTime.Now,
                Olusturan = _currentUserService.GetUsername()
            };

            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();


            return true;

        }

        public async Task<RandevuNotDto> CreateNotAsync(long id, RandevuNotDto randevuNotDto)
        {
            var not = mapper.Map<RandevuNot>(randevuNotDto);
            var randevu = await context.
                Randevular.
                FirstOrDefaultAsync(r => r.Id == id);
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Randevu bulunamadı. Id:{id}");
            }

            not.RandevuId = id;
            not.Tarih = DateTime.Now;
            not.Olusturan = _currentUserService.GetUsername();

            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();

            var author = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenNotes);
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0,
                "Yeni Not", $"{randevu.Konu} konulu talebe yeni bir not eklendi. {author} tarafından.",
                SendMessageType.PushNotification, NotifikasyonTip.RequestOnUpdated, data.ToJson());

            return mapper.Map<RandevuNotDto>(not);
        }

        public async Task DeleteAsync(long id)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var randevu = await context.Randevular.FirstOrDefaultAsync(r => r.Id == id && r.BirimId == birimId);
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Randevu bulunamadı. Id:{id}");
            }

            context.Randevular.Remove(randevu);
            await context.SaveChangesAsync();

            var author = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0, "Talep Silme", $"{randevu.Konu} konulu talep silindi. {author} tarafından.",
                SendMessageType.PushNotification,
                NotifikasyonTip.RequestOnDeleted,
                data.ToJson());
        }
        /// <summary>v1 sözleşmesi — imzası değişmez, gövdesi yenisine devreder.</summary>
        public async Task<bool> RandevuToAjandaAsync(RandevuToAjandaDto randevuToAjandaDto)
        {
            try
            {
                await TalebiEtkinligeCevirAsync(randevuToAjandaDto);
                return true;
            }
            catch
            {
                // v1 istemcileri `false` bekliyor; şekil korunuyor.
                return false;
            }
        }

        public async Task<long> TalebiEtkinligeCevirAsync(RandevuToAjandaDto istek)
        {
            var randevu = await context.Randevular.FirstOrDefaultAsync(r => r.Id == istek.RandevuId)
                ?? throw new EntityNotFoundException($"Randevu bulunamadı. Id:{istek.RandevuId}");

            var kullaniciAdi = _currentUserService.GetUsername();

            var ajanda = new Ajanda
            {
                DurumId = istek.AjandaDurumId,
                BaslangicTarihi = istek.BaslangicTarih,
                BitisTarihi = istek.BaslangicTarih.AddMinutes(30),
                OlusturmaTarihi = DateTime.Now,
                GuncellemeTarihi = DateTime.Now,
                RandevuTipId = randevu.RandevuTipId,
                RandevuId = istek.RandevuId,
                BasinKatilsin = istek.BasinKatilsin,
                BilgiNotuDurum = istek.BilgiNotuEklensin,
                KonusmaMetniDurum = istek.KonusmaMetniEklensin,
                ResimVar = istek.ResimEklensin,
                Baslik = $"{randevu.Ad} {randevu.Soyad} {randevu.Konu}".Trim(),
                IrtibatKisi = $"{randevu.Ad} {randevu.Soyad}".Trim(),
                IrtibatTelefon = randevu.Telefon,
                Aciklama = randevu.Aciklama + "  Adres:" + randevu.Adres,
                BirimId = randevu.BirimId,
                Konum = randevu.Yer,
                Status = Application.Enums.AjandaStatus.Pending,
                // OLUŞTURAN YAZILMIYORDU: `Ajanda.KullaniciId` kullanıcı ADI
                // tutuyor ve boş kalınca etkinliğin sahibi olmuyordu — kayıt
                // bilgilerinde "ekleyen" boş çıkıyor, gizlilik kuralında da
                // oluşturan eşleşmesi hiç tutmuyordu.
                KullaniciId = kullaniciAdi,
            };

            await context.Ajandalar.AddAsync(ajanda);

            // BAYRAK YAZILMIYORDU. Talep ajandaya eklendiği hâlde listede
            // "Ajandada: Hayır" görünüyor, "ajandaya eklenmemiş" süzgeci de
            // her şeyi döndürüyordu. Etkinlik oluşuyor ama talep bunu bilmiyor.
            randevu.AjandaDurum = true;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = kullaniciAdi;

            var author = await _currentUserService.GetFullNameAsync();
            await context.Notlar.AddAsync(new RandevuNot
            {
                RandevuId = randevu.Id,
                Tip = "Ajandaya Ekleme",
                Not = $"Talep {istek.BaslangicTarih:dd.MM.yyyy HH:mm} tarihiyle ajandaya eklendi. {author} tarafından.",
                Tarih = DateTime.Now,
                Olusturan = kullaniciAdi,
            });

            await context.SaveChangesAsync();

            // BİLDİRİM ETKİNLİĞİ AÇAR, talebi değil: bu bildirimin haber
            // verdiği şey artık takvimdeki kayıt. Talebe götürmek, kullanıcıyı
            // "eklendi mi?" diye bakacağı yere değil, geldiği yere geri
            // gönderiyordu.
            var data = new TokenDataDto(
                NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);

            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0,
                "Ajandaya Eklendi",
                $"{randevu.Konu} konulu talep {istek.BaslangicTarih:dd.MM.yyyy HH:mm} için ajandaya eklendi. {author} tarafından.",
                SendMessageType.PushNotification,
                NotifikasyonTip.RequestOnAddedToAgenda,
                data.ToJson());

            return ajanda.Id;
        }
        public async Task<IEnumerable<RandevuDto>> GetAllAsync(bool includeDescendants = false)
        {

            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList() : new List<long> { birimId };
            return await context.Randevular
                .Where(r => descendantIds.Contains(r.BirimId ?? 0))
                //tarihe göre en yeni randevular en üstte olacak şekilde sıralama
                .OrderByDescending(r => r.BaslangicTarih)
                .ProjectToType<RandevuDto>()
                .ToListAsync();
        }

        public async Task<IEnumerable<RandevuListDto>> GetAllListAsync(bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList() : new List<long> { birimId };
            return await context.Randevular
                .Where(r => descendantIds.Contains(r.BirimId ?? 0))
                .OrderByDescending(r => r.BaslangicTarih)
                .ProjectToType<RandevuListDto>()
                .ToListAsync();
        }

        public async Task<IEnumerable<RandevuDosyaDto>> GetAllDosyaAsync(long randevuId)
        {
            //var birimId = _currentUserService.GetCurrentBirimId();
            //if (!RandevuExists(randevuId, birimId))
            //{
            //    throw new EntityNotFoundException($"Talep bulunamadı. Id:{randevuId}");
            //}

            return await context.Dosyalar
                .Where(d => d.RandevuId == randevuId)
                .ProjectToType<RandevuDosyaDto>()
                .ToListAsync();
        }

        public async Task<IEnumerable<RandevuHareketDto>> GetAllHareketAsync(long randevuId)
        {
            //var birimId = _currentUserService.GetCurrentBirimId();
            //if (!RandevuExists(randevuId, birimId))
            //{
            //    throw new EntityNotFoundException($"Talep bulunamadı. Id:{randevuId}");
            //}

            return await context.RandevuHareketler
                .Where(h => h.RandevuId == randevuId)
                .ProjectToType<RandevuHareketDto>()
                .ToListAsync();
        }

        public async Task<IEnumerable<RandevuNotDto>> GetAllNotAsync(long randevuId)
        {
            //var birimId = _currentUserService.GetCurrentBirimId();
            //if (!RandevuExists(randevuId, birimId))
            //{
            //    throw new EntityNotFoundException($"Talep bulunamadı. Id:{randevuId}");
            //}

            return await context.Notlar
                .Where(n => n.RandevuId == randevuId)
                .ProjectToType<RandevuNotDto>()
                .ToListAsync();

        }

        public async Task<RandevuDto> GetByIdAsync(long id)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            var randevu = await context.Randevular
                .IgnoreQueryFilters()
                .Where(r => descendantIds.Contains(r.BirimId ?? 0))
                .ProjectToType<RandevuDto>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (randevu == null)
            {
                throw new EntityNotFoundException($"Talep bulunamadı. Id:{id}");
            }

            return randevu;
        }



        public async Task<IEnumerable<RandevuDto>> SearchAsync(RandevuSearchParametersDto searchParameters)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();

            var query = context.Randevular
                .Where(r => descendantIds.Contains(r.BirimId ?? 0));

            // Apply filters based on search parameters
            if (!string.IsNullOrWhiteSpace(searchParameters.Konu))
                query = query.Where(r => r.Konu.Contains(searchParameters.Konu));

            if (!string.IsNullOrWhiteSpace(searchParameters.Ad))
                query = query.Where(r => r.Ad.Contains(searchParameters.Ad));

            if (!string.IsNullOrWhiteSpace(searchParameters.Soyad))
                query = query.Where(r => r.Soyad.Contains(searchParameters.Soyad));

            if (!string.IsNullOrWhiteSpace(searchParameters.Meslek))
                query = query.Where(r => r.Meslek.Contains(searchParameters.Meslek));

            if (!string.IsNullOrWhiteSpace(searchParameters.Telefon))
                query = query.Where(r => r.Telefon.Contains(searchParameters.Telefon));

            if (searchParameters.BaslangicTarih.HasValue)
                query = query.Where(r => r.BaslangicTarih >= searchParameters.BaslangicTarih.Value);

            if (searchParameters.BitisTarih.HasValue)
                query = query.Where(r => r.BaslangicTarih <= searchParameters.BitisTarih.Value);

            if (searchParameters.BirimId.HasValue)
                query = query.Where(r => r.BirimId == searchParameters.BirimId);

            if (searchParameters.MahalleId.HasValue)
                query = query.Where(r => r.MahalleId == searchParameters.MahalleId);

            if (searchParameters.RandevuDurumId.HasValue)
                query = query.Where(r => r.RandevuDurumId == searchParameters.RandevuDurumId);

            if (searchParameters.RandevuTipId.HasValue)
                query = query.Where(r => r.RandevuTipId == searchParameters.RandevuTipId);

            if (searchParameters.OzgecmisDurum.HasValue)
                query = query.Where(r => r.OzgecmisDurum == searchParameters.OzgecmisDurum);

            return await query
                .ProjectToType<RandevuDto>()
                .ToListAsync();
        }

        public async Task<RandevuDto> UpdateAsync(RandevuDto randevuDto)
        {
            //_log randevuDto to json
            var randevu = await context.Randevular.FindAsync(randevuDto.Id);
            
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Randevu bulunamadı. Id:{randevuDto.Id}");
            }
            var birimId = randevu.BirimId;
            randevu = mapper.Map(randevuDto, randevu);
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = _currentUserService.GetUsername();
            randevu.BirimId = birimId;

            var author = await _currentUserService.GetFullNameAsync();

            // Değişiklik geçmişi (audit): değişen alanları eski→yeni NOT olarak kaydet.
            // Bu not için AYRICA bildirim GÖNDERİLMEZ (sessiz kayıt).
            var degisenler = AuditHelper.DegisenAlanlar(context.Entry(randevu));
            if (degisenler.Count > 0)
            {
                context.Notlar.Add(new RandevuNot
                {
                    RandevuId = randevu.Id,
                    Tip = "Güncelleme",
                    Not = $"Güncelleme: {string.Join("; ", degisenler)}. {author} tarafından.",
                    Tarih = DateTime.Now,
                    Olusturan = _currentUserService.GetUsername()
                });
            }

            context.Randevular.Update(randevu);
            await context.SaveChangesAsync();

            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            var alanOzeti = degisenler.Count > 0
                ? $" Değişen alanlar: {string.Join(", ", degisenler.Select(d => d.Split(':')[0]))}."
                : string.Empty;
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0,
                "Talep Güncelleme",
                $"{randevu.Konu} konulu talep güncellendi.{alanOzeti} {author} tarafından.",
                SendMessageType.PushNotification,
                NotifikasyonTip.RequestOnUpdated,
                data.ToJson());

            return mapper.Map<RandevuDto>(randevu);
        }

        private bool RandevuExists(long id, long birimId)
        {
            var descendantIds = context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            return context.Randevular.Any(r => descendantIds.Contains(r.BirimId ?? 0) && r.Id == id);
        }

        public async Task<RandevuDto> ChangeDurumAsync(long randevuId, long durumId)
        {
            var randevu = await context.Randevular.FindAsync(randevuId);
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Randevu bulunamadı. Id:{randevuId}");
            }

            randevu.RandevuDurumId = durumId;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = _currentUserService.GetUsername();

            var durumAd = await context.RandevuDurumlar.Where(x => x.Id == durumId).Select(x => x.DurumAd).FirstOrDefaultAsync();
            // Create durum change note
            var not = new RandevuNot
            {
                RandevuId = randevuId,
                Tip = "Durum Değişikliği",
                Not = $"Randevu durumu {durumAd} olarak değiştirildi.",
                Tarih = DateTime.Now,
                Olusturan = _currentUserService.GetUsername()
            };

            context.Randevular.Update(randevu);
            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();

            var author = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0,
                "Talep Durum Değişikliği",
                $"{randevu.Konu} konulu talebin durumu {durumAd} olarak değiştirildi. {author} tarafından.",
                SendMessageType.PushNotification,
                NotifikasyonTip.RequestOnStatusChange,
                data.ToJson());

            return mapper.Map<RandevuDto>(randevu);
        }

        public async Task<RandevuDto> ChangeTipAsync(long randevuId, long tipId)
        {
            var randevu = await context.Randevular.FindAsync(randevuId);
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Randevu bulunamadı. Id:{randevuId}");
            }

            randevu.RandevuTipId = tipId;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = _currentUserService.GetUsername();
            var tipAd = await context.RandevuTipleri
                .Where(x => x.Id == tipId)
                .Select(x => x.Ad)
                .FirstOrDefaultAsync();
            // Create tip change note
            var not = new RandevuNot
            {
                RandevuId = randevuId,
                Tip = "Tip Değişikliği",
                Not = $"Randevu tipi {tipAd} olarak değiştirildi.",
                Tarih = DateTime.Now,
                Olusturan = _currentUserService.GetUsername()
            };

            context.Randevular.Update(randevu);
            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();

            var author = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenDetails);
            await _messageService.CreateForAllPersonAsync(
                randevu.BirimId ?? 0,
                "Talep Tip Değişikliği", $"{randevu.Konu} konulu talebin tipi {tipAd} olarak değiştirildi. {author} tarafından.",
                SendMessageType.PushNotification,
                NotifikasyonTip.RequestOnStatusChange,
                data.ToJson());


            return mapper.Map<RandevuDto>(randevu);
        }

        public async Task<IEnumerable<RandevuCountByDurumDto>> GetCountByDurum(bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants
                ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList()
                : new List<long> { birimId };

            return await context.Randevular
                .IgnoreQueryFilters()
                .Where(r => descendantIds.Contains(r.BirimId ?? 0) && r.Arsivlendi)
                .GroupBy(r => new { r.RandevuDurumId, r.RandevuDurum.DurumAd })
                .Select(g => new RandevuCountByDurumDto
                {
                    DurumId = g.Key.RandevuDurumId ?? 0,
                    DurumAd = g.Key.DurumAd,
                    Count = g.Count()
                })
                .ToListAsync();

        }
        public async Task<IEnumerable<RandevuCountByTipDto>> GetCountByTip(bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants
                ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList()
                : new List<long> { birimId };
            return await context.Randevular
                .IgnoreQueryFilters()
                .Where(r => descendantIds.Contains(r.BirimId ?? 0) && r.Arsivlendi)
                .GroupBy(r => new { r.RandevuTipId, r.RandevuTip.Ad })
                .Select(g => new RandevuCountByTipDto
                {
                    TipId = g.Key.RandevuTipId ?? 0,
                    TipAd = g.Key.Ad,
                    Count = g.Count()
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<RandevuListDto>> GetByDurumIdAsync(long durumId, bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList() : new List<long> { birimId };
            return await context.Randevular
                .IgnoreQueryFilters()
                .Where(r => descendantIds.Contains(r.BirimId ?? 0) && r.RandevuDurumId == durumId)
                .ProjectToType<RandevuListDto>()
                .ToListAsync();
        }

        public async Task<IEnumerable<RandevuListDto>> GetByTipIdAsync(long tipId, bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList() : new List<long> { birimId };
            return await context.Randevular
                .IgnoreQueryFilters()
                .Where(r => descendantIds.Contains(r.BirimId ?? 0) && r.RandevuTipId == tipId && r.Arsivlendi)
                .ProjectToType<RandevuListDto>()
                .ToListAsync();
        }



        public async Task<long> CountAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList();
            return await context.Randevular
                .Where(r => descendantIds.Contains(r.BirimId ?? 0))
                .LongCountAsync();
        }

        public async Task<long> CountByDurumAsync(long durumId, bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList() : new List<long> { birimId };
            return await context.Randevular
                .Where(r => descendantIds.Contains(r.BirimId ?? 0) && r.RandevuDurumId == durumId)
                .LongCountAsync();
        }



        
        public async Task<RandevuDto> UploadOzgecmisAsync(long randevuId, MultipartFormDataContent ozgecmisFile)

        {
            var randevu = await context.Randevular.FindAsync(randevuId);
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Talep bulunamadı. Id:{randevuId}");
            }

            // Extract the file content from 'ozgecmisFile'
            var fileContent = ozgecmisFile.FirstOrDefault();
            if (fileContent is StreamContent streamContent)
            {
                var fileBytes = await streamContent.ReadAsByteArrayAsync();
                var uzanti = Path.GetExtension(fileContent.Headers.ContentDisposition?.FileName?.Trim('"'));

                /*
                  ÇALIŞABİLİR UZANTI REDDEDİLİR.

                  Güvenlik sınırı `Middleware/YuklemeGuvenligi` (servis anında
                  etkisizleştirme); burası kullanıcıya ANLAŞILIR bir hata
                  vermek için. İkisi ayrı iş: ara katman diskte zaten duran
                  dosyaları da kapsıyor, bu satır ise dosyanın hiç
                  yüklenmemesini sağlıyor.
                */
                if (Middleware.YuklemeGuvenligi.Calisabilir(uzanti))
                {
                    throw new BusinessRuleException(
                        $"'{uzanti}' uzantılı dosyalar yüklenemez. Belgeyi PDF ya da resim olarak gönderin.");
                }

                var fileName = $"{Guid.NewGuid()}{uzanti}";
                var contentType = fileContent.Headers.ContentType?.MediaType ?? "application/octet-stream";

                // Depo soyutlaması üzerinden yazılır: yerel diskte yol
                // değişmedi (`wwwroot/uploads/ozgecmis`), nesne deposu seçiliyse
                // aynı anahtar kovaya gider. v1'in dönüş şekli etkilenmez.
                await _fileStorage.SaveAsync(
                    Storage.StorageArea.Public, $"uploads/ozgecmis/{fileName}", fileBytes, contentType);

                // Update Randevu with the new file details
                randevu.OzgecmisDosya = fileName;
                randevu.GuncellemeTarih = DateTime.Now;
                randevu.Guncelleyen = _currentUserService.GetUsername();

                // Create a note for the upload
                var author = _currentUserService.GetUsername();
                var not = new RandevuNot
                {
                    RandevuId = randevuId,
                    Tip = "Özgeçmiş Yükleme",
                    Not = $"Özgeçmiş {author} tarafından yüklendi. Dosya adı: {fileName}.",
                    Tarih = DateTime.Now,
                    Olusturan = author
                };

                context.Randevular.Update(randevu);
                await context.Notlar.AddAsync(not);
                await context.SaveChangesAsync();

                // ÖZGEÇMİŞ HAVUZUNA yansıt: talebe yüklenen CV, "elimizde
                // kaynakçı var mı?" sorusunun cevabına da girmeli. Dosya
                // KOPYALANMAZ; havuzdaki kayıt aynı dosyayı gösterir ve
                // hangi talepten geldiği kaydın üstünde yazar.
                await _ozgecmisHavuzu.TalepOzgecmisiniYansitAsync(
                    randevu,
                    fileName,
                    fileContent.Headers.ContentDisposition?.FileName?.Trim('"') ?? fileName,
                    fileBytes.LongLength,
                    contentType);

                var name = await _currentUserService.GetFullNameAsync();
                var message = $"{randevu.Konu} konulu talebe {name} tarafından özgeçmiş yüklendi.";
                var data = new TokenDataDto(NotificationEntity.Talep, (int)randevu.Id, NotificationAction.OpenImages);
                await _messageService.CreateForAllPersonAsync(
                    randevu.BirimId ?? 0,
                    "Yeni Özgeçmiş Yüklendi",
                    message,
                    SendMessageType.PushNotification,
                    NotifikasyonTip.RequestOnFileAttached,
                    data.ToJson()
                );
            }
            return mapper.Map<RandevuDto>(randevu);
        }

        public async Task<bool> AddToArchiveAsync(long randevuId)
        {
            var randevu = await context.Randevular.FindAsync(randevuId);
            if (randevu == null)
            {
                throw new EntityNotFoundException($"Talep bulunamadı. Id:{randevuId}");
            }

            var author = _currentUserService.GetUsername();
            randevu.Arsivlendi = true;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = author;

            var not = new RandevuNot
            {
                RandevuId = randevuId,
                Tip = "Arşivleme",
                Not = $"{author} tarafından arşivlendi.",
                Tarih = DateTime.Now,
                Olusturan = author
            };

            context.Randevular.Update(randevu);

            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromArchiveAsync(long randevuId)
        {
            var randevu = await context
                .Randevular
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == randevuId);

            if (randevu == null)
            {
                throw new EntityNotFoundException($"Talep bulunamadı. Id:{randevuId}");
            }
            var author = _currentUserService.GetUsername();
            randevu.Arsivlendi = false;
            randevu.GuncellemeTarih = DateTime.Now;
            randevu.Guncelleyen = author;
            var not = new RandevuNot
            {
                RandevuId = randevuId,
                Tip = "Arşivden Çıkarma",
                Not = $"{author} tarafından arşivden çıkarıldı ve Taleplere eklendi.",
                Tarih = DateTime.Now,
                Olusturan = author
            };
            context.Randevular.Update(randevu);
            await context.Notlar.AddAsync(not);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RandevuListDto>> GetArchiveListAsync(bool includeDescendants = false)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var descendantIds = includeDescendants ? context.Birimler.GetDescendants(birimId, true).Select(b => b.Id).ToList() : new List<long> { birimId };
            return await context.Randevular
                .IgnoreQueryFilters()
                .Where(r => descendantIds.Contains(r.BirimId ?? 0) && r.Arsivlendi)
                .ProjectToType<RandevuListDto>()
                .ToListAsync();
        }


    }
}
