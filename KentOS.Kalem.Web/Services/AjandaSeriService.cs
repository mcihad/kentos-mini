using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using KentOS.Kalem.Application.Dto;
using KentOS.Kalem.Application.Enums;
using KentOS.Kalem.Application.Models;
using KentOS.Kalem.Application.Services;
using KentOS.Kalem.Web.Data;
using KentOS.Kalem.Web.Exceptions;

namespace KentOS.Kalem.Web.Services
{
    /// <summary>
    /// Tekrarlanan etkinlik serisi işlemleri — bkz. <see cref="IAjandaSeriService"/>.
    /// </summary>
    public class AjandaSeriService(
        AppDbContext _context,
        ICurrentUserService _currentUserService,
        IMessageService _messageService,
        IAjandaOlayService _olayService,
        IMapper _mapper,
        ILogger<AjandaSeriService> _logger) : IAjandaSeriService
    {
        /// <summary>Bir seride üretilebilecek en fazla tekrar (listeleme sınırı).</summary>
        public const int EnFazlaTekrar = RRuleGenisletici.VarsayilanEnFazlaAdet;

        /// <summary>Sonsuz kurallarda ileriye üretim ufku (ay).</summary>
        public const int UfukAy = RRuleGenisletici.VarsayilanUfukAy;

        // ===================================================================
        //  OLUŞTURMA
        // ===================================================================
        public async Task<AjandaDto> OlusturAsync(AjandaDto sablon)
        {
            if (sablon.Tekrar == null || string.IsNullOrWhiteSpace(sablon.Tekrar.Rrule))
            {
                throw new BusinessRuleException("Tekrar kuralı boş olamaz.");
            }

            var kural = KuralAyristir(sablon.Tekrar.Rrule);
            var dtstart = sablon.BaslangicTarihi;
            var sureDakika = SureHesapla(sablon, sablon.Tekrar.SureDakika);
            var ufuk = UfukHesapla(dtstart);

            var tarihler = RRuleGenisletici.Genislet(kural, dtstart, ufuk, EnFazlaTekrar);
            if (tarihler.Count == 0)
            {
                throw new BusinessRuleException(
                    "Bu tekrar kuralı hiç etkinlik üretmiyor. Kuralı ve başlangıç tarihini kontrol edin.");
            }

            var birimId = _currentUserService.GetCurrentBirimId();
            var kullaniciAdi = _currentUserService.GetUsername();

            var seri = new AjandaSeri
            {
                Rrule = kural.ToString(),
                Dtstart = dtstart,
                SureDakika = sureDakika,
                BitisTarihi = kural.BitisTarihi,
                TekrarSayisi = kural.Adet,
                BirimId = birimId,
                KullaniciId = kullaniciAdi,
                UretilenSonTarih = UretilenSonTarihHesapla(tarihler, ufuk),
                OlusturmaTarihi = DateTime.Now
            };
            _context.AjandaSeriler.Add(seri);
            await _context.SaveChangesAsync();

            // Şablon → entity. Katılımcı ve seri alanları Mapster tarafından
            // bilinçli olarak eşlenmez (bkz. MapsterConfig); burada elle kurulur.
            var sablonEntity = _mapper.Map<Ajanda>(sablon);

            // Katılımcı BİRİMLER gizlilikten bağımsız taşınır; eski kişi
            // katılımcılar yalnızca gizli etkinlikte anlamlı.
            var katilimciIdler = sablon.KatilimciBirimIdler ?? [];
            var kisiKatilimciIdler = sablon.Gizli ? (sablon.KatilimciIdler ?? []) : [];

            var tekrarlar = new List<Ajanda>();
            for (var i = 0; i < tarihler.Count; i++)
            {
                var ajanda = TekrarUret(sablonEntity, tarihler[i], sureDakika, seri, birimId, kullaniciAdi, katilimciIdler, kisiKatilimciIdler);

                // RandevuId YALNIZCA ilk tekrarda kalır: `trigger_update_randevu_on_ajanda_insert`
                // eklenen her etkinlikte randevu.ajanda_id'yi günceller; 200 tekrarın
                // hepsi randevuya bağlansa talep en son tekrarı gösterirdi.
                ajanda.RandevuId = i == 0 ? sablon.RandevuId : 0;

                tekrarlar.Add(ajanda);
            }

            _context.Ajandalar.AddRange(tekrarlar);
            await _context.SaveChangesAsync();

            var ilk = tekrarlar[0];
            var ozet = RRuleGenisletici.Ozet(kural);

            await _olayService.KaydetAsync(ilk.Id, AjandaOlayTip.SeriOlusturuldu,
                $"Tekrarlanan etkinlik oluşturuldu ({ozet}) — {tekrarlar.Count} tekrar");

            // Seri için TEK bildirim gönderilir; 200 tekrar için 200 bildirim
            // göndermek kullanıcıyı bildirim çöplüğüne boğardı.
            var ad = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ilk.Id, NotificationAction.OpenDetails);
            await AjandaSorguUzantilari.EtkinlikBildirAsync(
                _context, _messageService, ilk,
                "Yeni Tekrarlanan Etkinlik",
                $"{ilk.Baslik} konulu tekrarlanan etkinlik {ad} tarafından eklendi ({ozet}, {tekrarlar.Count} tekrar).",
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnCreated,
                data.ToJson());

            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            return await DtoyaCevirAsync(ilk, seri);
        }

        // ===================================================================
        //  OKUMA
        // ===================================================================
        public async Task<AjandaSeriDto?> GetirAsync(long ajandaId)
        {
            var ajanda = await _context.Ajandalar
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => a.Id == ajandaId)
                .Select(a => new { a.SeriId, a.Gizli, a.KullaniciId })
                .FirstOrDefaultAsync();

            if (ajanda?.SeriId == null)
            {
                return null;
            }

            // Gizlilik kapısı: gizli etkinliğin kuralı da yalnızca yetkililere açık.
            var kullaniciId = await _currentUserService.GetUserIdAsync();
            var kullaniciAdi = _currentUserService.GetUsername();
            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();
            var gorunur = await _context.Ajandalar
                .IgnoreQueryFilters()
                .Where(a => a.Id == ajandaId)
                .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
                .AnyAsync();

            if (!gorunur)
            {
                return null;
            }

            var seri = await _context.AjandaSeriler.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == ajanda.SeriId.Value);

            if (seri == null)
            {
                return null;
            }

            var tekrarlar = await _context.Ajandalar.AsNoTracking()
                .Where(a => a.SeriId == seri.Id)
                .OrderBy(a => a.SeriOrijinalBaslangic ?? a.BaslangicTarihi)
                .Select(a => new { a.Id })
                .ToListAsync();

            return new AjandaSeriDto
            {
                Id = seri.Id,
                Rrule = seri.Rrule,
                Ozet = RRuleGenisletici.Ozet(seri.Rrule),
                Dtstart = seri.Dtstart,
                SureDakika = seri.SureDakika,
                BitisTarihi = seri.BitisTarihi,
                TekrarSayisi = seri.TekrarSayisi,
                UretilenSonTarih = seri.UretilenSonTarih,
                Iptal = seri.Iptal,
                UretilenAdet = tekrarlar.Count,
                IlkAjandaId = tekrarlar.FirstOrDefault()?.Id
            };
        }

        // ===================================================================
        //  GÜNCELLEME (kapsamlı)
        // ===================================================================
        public async Task<AjandaDto> GuncelleAsync(AjandaDto dto, TekrarKapsam kapsam)
        {
            var hedef = await GorunurTekAsync(dto.Id)
                ?? throw new EntityNotFoundException("Etkinlik bulunamadı");

            if (hedef.SeriId == null)
            {
                throw new BusinessRuleException("Bu etkinlik tekrarlanan bir etkinlik değil.");
            }

            var seri = await _context.AjandaSeriler.FirstOrDefaultAsync(s => s.Id == hedef.SeriId.Value)
                ?? throw new EntityNotFoundException("Tekrar serisi bulunamadı");

            var eskiBaslangic = hedef.BaslangicTarihi;
            var yeniBaslangic = dto.BaslangicTarihi;
            var yeniSure = SureHesapla(dto, dto.Tekrar?.SureDakika ?? seri.SureDakika);
            var ad = await _currentUserService.GetFullNameAsync();

            if (kapsam == TekrarKapsam.Tumu)
            {
                await TumunuGuncelleAsync(dto, hedef, seri, eskiBaslangic, yeniBaslangic, yeniSure);
                await _olayService.KaydetAsync(hedef.Id, AjandaOlayTip.SeriGuncellendi,
                    "Tekrarlanan etkinliğin tümü güncellendi");
            }
            else
            {
                await BundanSonrakileriGuncelleAsync(dto, hedef, seri, yeniBaslangic, yeniSure);
                await _olayService.KaydetAsync(hedef.Id, AjandaOlayTip.SeriGuncellendi,
                    "Bu tekrar ve sonrakiler güncellendi");
            }

            var kapsamMetni = kapsam == TekrarKapsam.Tumu ? "tüm tekrarlar" : "bu ve sonraki tekrarlar";
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)hedef.Id, NotificationAction.OpenDetails);
            await AjandaSorguUzantilari.EtkinlikBildirAsync(
                _context, _messageService, hedef,
                "Tekrarlanan Etkinlik Güncellendi",
                $"{hedef.Baslik} konulu tekrarlanan etkinlik {ad} tarafından güncellendi ({kapsamMetni}).",
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnUpdated,
                data.ToJson());

            // "Bundan sonrakiler" kapsamında kayıt YENİ bir seriye taşınmış olur;
            // istemciye eski serinin kuralı dönmemeli.
            var guncelSeri = hedef.SeriId == seri.Id
                ? seri
                : await _context.AjandaSeriler.FirstAsync(s => s.Id == hedef.SeriId!.Value);

            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            return await DtoyaCevirAsync(hedef, guncelSeri);
        }

        /// <summary>
        /// TÜMÜ kapsamı: içerik alanları ve saat/süre, ayrık olmayan bütün
        /// tekrarlara uygulanır. Takvim GÜNÜ değiştiyse tüm tekrarlar aynı gün
        /// farkıyla kaydırılır ve kural yeni tarihe çapalanır (standart takvim
        /// davranışı). Bireysel düzenlenmiş (ayrık) tekrarlar korunur.
        /// </summary>
        private async Task TumunuGuncelleAsync(
            AjandaDto dto, Ajanda hedef, AjandaSeri seri,
            DateTime eskiBaslangic, DateTime yeniBaslangic, int yeniSure)
        {
            var gunFarki = (yeniBaslangic.Date - eskiBaslangic.Date).Days;
            var yeniSaat = yeniBaslangic.TimeOfDay;

            var tekrarlar = await _context.Ajandalar
                .Include(a => a.Katilimcilar)
                .Where(a => a.SeriId == seri.Id && !a.SeriAyrik)
                .ToListAsync();

            foreach (var t in tekrarlar)
            {
                IcerikAlanlariniUygula(dto, t);

                var yeniTarih = t.BaslangicTarihi.Date.AddDays(gunFarki) + yeniSaat;
                t.BaslangicTarihi = yeniTarih;
                t.BitisTarihi = yeniTarih.AddMinutes(yeniSure);
                t.SeriOrijinalBaslangic = (t.SeriOrijinalBaslangic ?? t.BaslangicTarihi).Date.AddDays(gunFarki) + yeniSaat;
                t.GuncellemeTarihi = DateTime.Now;

                await KatilimcilariEsitleAsync(t, dto);
            }

            var kural = KuralAyristir(seri.Rrule).YenidenCapala(eskiBaslangic, yeniBaslangic);
            seri.Rrule = kural.ToString();
            seri.Dtstart = seri.Dtstart.Date.AddDays(gunFarki) + yeniSaat;
            seri.SureDakika = yeniSure;
            seri.BitisTarihi = kural.BitisTarihi;
            seri.TekrarSayisi = kural.Adet;
            if (seri.UretilenSonTarih != null && gunFarki != 0)
            {
                seri.UretilenSonTarih = seri.UretilenSonTarih.Value.AddDays(gunFarki);
            }
            seri.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// BUNDAN SONRAKİLER kapsamı: eski seri bu tekrardan hemen önce kapatılır,
        /// bu tekrar ve sonrası YENİ bir seri olarak yeniden üretilir.
        ///
        /// Veri kaybını önlemek için: içinde kullanıcı verisi (not/fotoğraf/çiçek)
        /// olan ileriki tekrarlar silinmez, "silinmiş" işaretlenir; boş olanlar
        /// tablodan kaldırılır. Bireysel düzenlenmiş (ayrık) tekrarlara DOKUNULMAZ —
        /// kullanıcının bilinçli istisnaları korunur.
        /// </summary>
        private async Task BundanSonrakileriGuncelleAsync(
            AjandaDto dto, Ajanda hedef, AjandaSeri seri, DateTime yeniBaslangic, int yeniSure)
        {
            var kesmeNoktasi = hedef.SeriOrijinalBaslangic ?? hedef.BaslangicTarihi;
            var orijinalKural = KuralAyristir(seri.Rrule);

            // Kesme noktasından ÖNCE kaç tekrar üretilmiş? COUNT'lu bir kural
            // bölündüğünde yeni serinin tekrar sayısı "kalan" kadar olmalı; aksi
            // halde toplam tekrar sayısı kendiliğinden artardı.
            var oncekiAdet = RRuleGenisletici
                .Genislet(orijinalKural, seri.Dtstart, kesmeNoktasi.AddSeconds(-1), EnFazlaTekrar)
                .Count;

            // 1) Eski seriyi kesme noktasından önce bitir.
            var eskiKural = orijinalKural.BitisleKapat(kesmeNoktasi.AddSeconds(-1));
            seri.Rrule = eskiKural.ToString();
            seri.BitisTarihi = eskiKural.BitisTarihi;
            seri.TekrarSayisi = null;
            seri.GuncellemeTarihi = DateTime.Now;

            // 2) Kesme noktasından itibaren ayrık OLMAYAN tekrarları temizle.
            //    DÜZENLENEN KAYIT HARİÇ: o kayıt yeni serinin ilk tekrarı olarak
            //    yerinde kalır (notları/fotoğrafları korunsun diye kimliği değişmez).
            var sonrakiler = await _context.Ajandalar
                .Include(a => a.Katilimcilar)
                .Where(a => a.SeriId == seri.Id
                            && a.Id != hedef.Id
                            && !a.SeriAyrik
                            && (a.SeriOrijinalBaslangic ?? a.BaslangicTarihi) >= kesmeNoktasi)
                .ToListAsync();

            var idler = sonrakiler.Select(a => a.Id).ToList();
            var veriliIdler = await VeriTasiyanlarAsync(idler);

            foreach (var t in sonrakiler)
            {
                if (veriliIdler.Contains(t.Id))
                {
                    t.IsDeleted = true;
                    t.GuncellemeTarihi = DateTime.Now;
                }
                else
                {
                    _context.Ajandalar.Remove(t);
                }
            }
            await _context.SaveChangesAsync();

            // 3) Yeni seri: kullanıcı yeni bir kural verdiyse o, vermediyse ÖZGÜN
            //    kural (kesilmiş hâli değil) yeni tarihe çapalanır. COUNT'lu
            //    kuralda tekrar sayısı "kalan" kadar olur.
            var temelKural = dto.Tekrar?.Rrule != null
                ? KuralAyristir(dto.Tekrar.Rrule)
                : orijinalKural.KalanAdetle(oncekiAdet);

            var yeniKural = temelKural.YenidenCapala(kesmeNoktasi, yeniBaslangic);

            // Eski serinin sınırları yeni seriye taşınır; UNTIL zaten kuralda.
            var ufuk = UfukHesapla(yeniBaslangic);
            var tarihler = RRuleGenisletici.Genislet(yeniKural, yeniBaslangic, ufuk, EnFazlaTekrar);
            if (tarihler.Count == 0)
            {
                throw new BusinessRuleException("Yeni tekrar kuralı hiç etkinlik üretmiyor.");
            }

            var birimId = _currentUserService.GetCurrentBirimId();
            var kullaniciAdi = _currentUserService.GetUsername();

            var yeniSeri = new AjandaSeri
            {
                Rrule = yeniKural.ToString(),
                Dtstart = yeniBaslangic,
                SureDakika = yeniSure,
                BitisTarihi = yeniKural.BitisTarihi,
                TekrarSayisi = yeniKural.Adet,
                BirimId = birimId,
                KullaniciId = kullaniciAdi,
                UretilenSonTarih = UretilenSonTarihHesapla(tarihler, ufuk),
                OlusturmaTarihi = DateTime.Now
            };
            _context.AjandaSeriler.Add(yeniSeri);
            await _context.SaveChangesAsync();

            // 4) İlk tekrar: kullanıcının düzenlediği kaydın KENDİSİ olur (id korunur,
            //    notları/fotoğrafları yerinde kalır); kalanlar yeni satır olarak üretilir.
            IcerikAlanlariniUygula(dto, hedef);
            hedef.BaslangicTarihi = tarihler[0];
            hedef.BitisTarihi = tarihler[0].AddMinutes(yeniSure);
            hedef.SeriId = yeniSeri.Id;
            hedef.SeriOrijinalBaslangic = tarihler[0];
            hedef.SeriAyrik = false;
            hedef.TekrarEden = true;
            hedef.IsDeleted = false;
            hedef.GuncellemeTarihi = DateTime.Now;
            await KatilimcilariEsitleAsync(hedef, dto);

            var sablonEntity = _mapper.Map<Ajanda>(dto);
            // Tekrarlara KATILIMCI BİRİMLER kopyalanır; gizlilikten bağımsız,
            // çünkü katılımcı artık "toplantıya çağrılan birim" demek.
            var katilimciIdler = dto.KatilimciBirimIdler
                ?? hedef.Katilimcilar
                    .Where(k => k.BirimId != null)
                    .Select(k => k.BirimId!.Value)
                    .ToList();

            var kisiKatilimciIdler = dto.Gizli
                ? (dto.KatilimciIdler
                    ?? hedef.Katilimcilar
                        .Where(k => k.KullaniciId != null)
                        .Select(k => k.KullaniciId!.Value)
                        .ToList())
                : [];

            var yeniler = new List<Ajanda>();
            for (var i = 1; i < tarihler.Count; i++)
            {
                var yeni = TekrarUret(sablonEntity, tarihler[i], yeniSure, yeniSeri, birimId, kullaniciAdi, katilimciIdler, kisiKatilimciIdler);
                yeni.RandevuId = 0;
                yeniler.Add(yeni);
            }

            _context.Ajandalar.AddRange(yeniler);
            await _context.SaveChangesAsync();
        }

        // ===================================================================
        //  TEK SEFERLİK → TEKRARLANAN
        // ===================================================================
        public async Task<AjandaDto> SeriyeCevirAsync(AjandaDto dto)
        {
            var hedef = await GorunurTekAsync(dto.Id)
                ?? throw new EntityNotFoundException("Etkinlik bulunamadı");

            if (hedef.SeriId != null)
            {
                throw new BusinessRuleException("Bu etkinlik zaten bir tekrar serisine ait.");
            }
            if (dto.Tekrar == null || string.IsNullOrWhiteSpace(dto.Tekrar.Rrule))
            {
                throw new BusinessRuleException("Tekrar kuralı boş olamaz.");
            }

            var kural = KuralAyristir(dto.Tekrar.Rrule);
            var dtstart = dto.BaslangicTarihi;
            var sureDakika = SureHesapla(dto, dto.Tekrar.SureDakika);
            var ufuk = UfukHesapla(dtstart);

            var tarihler = RRuleGenisletici.Genislet(kural, dtstart, ufuk, EnFazlaTekrar);
            if (tarihler.Count == 0)
            {
                throw new BusinessRuleException(
                    "Bu tekrar kuralı hiç etkinlik üretmiyor. Kuralı ve başlangıç tarihini kontrol edin.");
            }

            var birimId = _currentUserService.GetCurrentBirimId();
            var kullaniciAdi = _currentUserService.GetUsername();

            var seri = new AjandaSeri
            {
                Rrule = kural.ToString(),
                Dtstart = dtstart,
                SureDakika = sureDakika,
                BitisTarihi = kural.BitisTarihi,
                TekrarSayisi = kural.Adet,
                BirimId = birimId,
                KullaniciId = kullaniciAdi,
                UretilenSonTarih = UretilenSonTarihHesapla(tarihler, ufuk),
                OlusturmaTarihi = DateTime.Now
            };
            _context.AjandaSeriler.Add(seri);
            await _context.SaveChangesAsync();

            // Mevcut kayıt serinin İLK tekrarı olur: kimliği korunur, böylece
            // notları/fotoğrafları/çiçeği yerinde kalır.
            IcerikAlanlariniUygula(dto, hedef);
            hedef.BaslangicTarihi = tarihler[0];
            hedef.BitisTarihi = tarihler[0].AddMinutes(sureDakika);
            hedef.SeriId = seri.Id;
            hedef.SeriOrijinalBaslangic = tarihler[0];
            hedef.SeriAyrik = false;
            hedef.TekrarEden = true;
            hedef.GuncellemeTarihi = DateTime.Now;
            await KatilimcilariEsitleAsync(hedef, dto);

            var sablonEntity = _mapper.Map<Ajanda>(dto);
            // Tekrarlara KATILIMCI BİRİMLER kopyalanır; gizlilikten bağımsız,
            // çünkü katılımcı artık "toplantıya çağrılan birim" demek.
            var katilimciIdler = dto.KatilimciBirimIdler
                ?? hedef.Katilimcilar
                    .Where(k => k.BirimId != null)
                    .Select(k => k.BirimId!.Value)
                    .ToList();

            var kisiKatilimciIdler = dto.Gizli
                ? (dto.KatilimciIdler
                    ?? hedef.Katilimcilar
                        .Where(k => k.KullaniciId != null)
                        .Select(k => k.KullaniciId!.Value)
                        .ToList())
                : [];

            var yeniler = new List<Ajanda>();
            for (var i = 1; i < tarihler.Count; i++)
            {
                var yeni = TekrarUret(sablonEntity, tarihler[i], sureDakika, seri, birimId, kullaniciAdi, katilimciIdler, kisiKatilimciIdler);
                yeni.RandevuId = 0;   // talep bağı yalnızca ilk kayıtta kalır
                yeniler.Add(yeni);
            }

            _context.Ajandalar.AddRange(yeniler);
            await _context.SaveChangesAsync();

            var ozet = RRuleGenisletici.Ozet(kural);
            await _olayService.KaydetAsync(hedef.Id, AjandaOlayTip.SeriOlusturuldu,
                $"Etkinlik tekrarlanan hâle getirildi ({ozet}) — {tarihler.Count} tekrar");

            var ad = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)hedef.Id, NotificationAction.OpenDetails);
            await AjandaSorguUzantilari.EtkinlikBildirAsync(
                _context, _messageService, hedef,
                "Etkinlik Tekrarlanan Hâle Getirildi",
                $"{hedef.Baslik} konulu etkinlik {ad} tarafından tekrarlanan hâle getirildi ({ozet}, {tarihler.Count} tekrar).",
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnUpdated,
                data.ToJson());

            return await DtoyaCevirAsync(hedef, seri);
        }

        // ===================================================================
        //  TEKRARI KALDIRMA
        // ===================================================================
        public async Task<AjandaDto> TekrariKaldirAsync(AjandaDto dto, TekrarKapsam kapsam)
        {
            var hedef = await GorunurTekAsync(dto.Id)
                ?? throw new EntityNotFoundException("Etkinlik bulunamadı");

            if (hedef.SeriId == null)
            {
                // Zaten tek seferlik; yalnızca içerik güncellenir.
                IcerikAlanlariniUygula(dto, hedef);
                hedef.TekrarEden = false;
                hedef.GuncellemeTarihi = DateTime.Now;
                await KatilimcilariEsitleAsync(hedef, dto);
                await _context.SaveChangesAsync();
                return _mapper.Map<AjandaDto>(hedef);
            }

            var seriId = hedef.SeriId.Value;
            var seri = await _context.AjandaSeriler.FirstOrDefaultAsync(s => s.Id == seriId);
            var kesmeNoktasi = hedef.SeriOrijinalBaslangic ?? hedef.BaslangicTarihi;

            if (kapsam != TekrarKapsam.Yalnizca)
            {
                // Kaldırılacak tekrarlar: "bundan sonrakiler"de kesme noktasından
                // itibaren, "tümü"nde serinin tamamı. Hedef kaydın KENDİSİ hariç —
                // o tek seferlik etkinlik olarak kalır.
                var sorgu = _context.Ajandalar.Where(a => a.SeriId == seriId && a.Id != hedef.Id);
                if (kapsam == TekrarKapsam.BundanSonrakiler)
                {
                    sorgu = sorgu.Where(a => (a.SeriOrijinalBaslangic ?? a.BaslangicTarihi) >= kesmeNoktasi);
                }

                var kaldirilacaklar = await sorgu.ToListAsync();
                var idler = kaldirilacaklar.Select(a => a.Id).ToList();
                var veriliIdler = await VeriTasiyanlarAsync(idler);

                foreach (var t in kaldirilacaklar)
                {
                    if (veriliIdler.Contains(t.Id))
                    {
                        // Kullanıcı verisi taşıyan tekrar silinmez, arşivlenir.
                        t.IsDeleted = true;
                        t.SeriAyrik = true;
                        t.GuncellemeTarihi = DateTime.Now;
                    }
                    else
                    {
                        _context.Ajandalar.Remove(t);
                    }
                }

                if (seri != null)
                {
                    if (kapsam == TekrarKapsam.Tumu)
                    {
                        seri.Iptal = true;
                    }
                    else
                    {
                        var kesilmis = KuralAyristir(seri.Rrule).BitisleKapat(kesmeNoktasi.AddSeconds(-1));
                        seri.Rrule = kesilmis.ToString();
                        seri.BitisTarihi = kesilmis.BitisTarihi;
                        seri.TekrarSayisi = null;
                    }
                    seri.GuncellemeTarihi = DateTime.Now;
                }
            }

            // Hedef kayıt seriden koparılır ve tek seferlik etkinliğe döner.
            IcerikAlanlariniUygula(dto, hedef);
            hedef.SeriId = null;
            hedef.SeriOrijinalBaslangic = null;
            hedef.SeriAyrik = false;
            hedef.TekrarEden = false;
            hedef.BaslangicTarihi = dto.BaslangicTarihi;
            hedef.BitisTarihi = dto.BitisTarihi ?? dto.BaslangicTarihi.AddMinutes(30);
            hedef.GuncellemeTarihi = DateTime.Now;
            await KatilimcilariEsitleAsync(hedef, dto);

            await _context.SaveChangesAsync();

            await _olayService.KaydetAsync(hedef.Id, AjandaOlayTip.SeriSilindi,
                kapsam switch
                {
                    TekrarKapsam.Tumu => "Tekrar kaldırıldı (serinin tamamı)",
                    TekrarKapsam.BundanSonrakiler => "Tekrar kaldırıldı (bu ve sonrakiler)",
                    _ => "Bu etkinlik seriden çıkarıldı (tek seferlik yapıldı)"
                });

            var ad = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)hedef.Id, NotificationAction.OpenDetails);
            await AjandaSorguUzantilari.EtkinlikBildirAsync(
                _context, _messageService, hedef,
                "Etkinlik Tekrarı Kaldırıldı",
                $"{hedef.Baslik} konulu etkinliğin tekrarı {ad} tarafından kaldırıldı ({KapsamMetni(kapsam)}).",
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnUpdated,
                data.ToJson());

            return _mapper.Map<AjandaDto>(hedef);
        }

        // ===================================================================
        //  SİLME (kapsamlı)
        // ===================================================================
        public async Task<bool> SilAsync(long ajandaId, TekrarKapsam kapsam)
        {
            var hedef = await GorunurTekAsync(ajandaId)
                ?? throw new EntityNotFoundException("Etkinlik bulunamadı");

            if (hedef.SeriId == null || kapsam == TekrarKapsam.Yalnizca)
            {
                // Tek tekrarın silinmesi = kuralın istisnası (EXDATE). Kayıt
                // "silinmiş" kalır; ayrık işaretlendiği için seri güncellemeleri
                // ona dokunmaz ve ufuk uzatma onu yeniden üretmez.
                hedef.IsDeleted = true;
                hedef.SeriAyrik = hedef.SeriId != null;
                hedef.GuncellemeTarihi = DateTime.Now;
                await _context.SaveChangesAsync();

                await _olayService.KaydetAsync(hedef.Id, AjandaOlayTip.Silindi,
                    hedef.SeriId != null ? "Bu tekrar silindi" : "Etkinlik silindi olarak işaretlendi");
            }
            else
            {
                var seri = await _context.AjandaSeriler.FirstOrDefaultAsync(s => s.Id == hedef.SeriId.Value);
                var kesmeNoktasi = hedef.SeriOrijinalBaslangic ?? hedef.BaslangicTarihi;

                var sorgu = _context.Ajandalar.Where(a => a.SeriId == hedef.SeriId.Value);
                if (kapsam == TekrarKapsam.BundanSonrakiler)
                {
                    sorgu = sorgu.Where(a => (a.SeriOrijinalBaslangic ?? a.BaslangicTarihi) >= kesmeNoktasi);
                }

                var silinecekler = await sorgu.ToListAsync();
                foreach (var t in silinecekler)
                {
                    t.IsDeleted = true;
                    t.GuncellemeTarihi = DateTime.Now;
                }

                if (seri != null)
                {
                    if (kapsam == TekrarKapsam.Tumu)
                    {
                        seri.Iptal = true;
                    }
                    else
                    {
                        var kural = KuralAyristir(seri.Rrule).BitisleKapat(kesmeNoktasi.AddSeconds(-1));
                        seri.Rrule = kural.ToString();
                        seri.BitisTarihi = kural.BitisTarihi;
                        seri.TekrarSayisi = null;
                    }
                    seri.GuncellemeTarihi = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                await _olayService.KaydetAsync(hedef.Id, AjandaOlayTip.SeriSilindi,
                    kapsam == TekrarKapsam.Tumu
                        ? $"Tekrarlanan etkinliğin tümü silindi ({silinecekler.Count} tekrar)"
                        : $"Bu tekrar ve sonrakiler silindi ({silinecekler.Count} tekrar)");
            }

            var ad = await _currentUserService.GetFullNameAsync();
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)hedef.Id, NotificationAction.None);
            await AjandaSorguUzantilari.EtkinlikBildirAsync(
                _context, _messageService, hedef,
                "Etkinlik Silindi Olarak İşaretlendi",
                $"{hedef.Baslik} konulu tekrarlanan etkinlik {ad} tarafından silindi ({KapsamMetni(kapsam)}).",
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnDeleted,
                data.ToJson());

            return true;
        }

        // ===================================================================
        //  UFUK UZATMA
        // ===================================================================
        public async Task<int> UfkuGenisletAsync(CancellationToken iptalJetonu = default)
        {
            var hedefUfuk = DateTime.Now.AddMonths(UfukAy);

            var seriler = await _context.AjandaSeriler
                .Where(s => !s.Iptal
                            && (s.UretilenSonTarih == null || s.UretilenSonTarih < hedefUfuk)
                            && (s.BitisTarihi == null || s.BitisTarihi > s.UretilenSonTarih))
                .ToListAsync(iptalJetonu);

            var toplamUretilen = 0;

            foreach (var seri in seriler)
            {
                iptalJetonu.ThrowIfCancellationRequested();

                try
                {
                    toplamUretilen += await SeriyiGenisletAsync(seri, hedefUfuk, iptalJetonu);
                }
                catch (Exception ex)
                {
                    // Bir seri hata verirse diğerleri etkilenmesin.
                    _logger.LogError(ex, "Tekrar serisi genişletilemedi (SeriId={SeriId})", seri.Id);
                }
            }

            if (toplamUretilen > 0)
            {
                _logger.LogInformation("Tekrar ufku uzatıldı: {Adet} yeni tekrar üretildi.", toplamUretilen);
            }

            return toplamUretilen;
        }

        private async Task<int> SeriyiGenisletAsync(AjandaSeri seri, DateTime hedefUfuk, CancellationToken iptalJetonu)
        {
            // Toplam tekrar tavanı: silinmişler DAHİL sayılır, böylece kullanıcı
            // tekrar sildikçe seri sonsuza kadar büyümez.
            var mevcutAdet = await _context.Ajandalar
                .IgnoreQueryFilters()
                .CountAsync(a => a.SeriId == seri.Id, iptalJetonu);

            var kapasite = EnFazlaTekrar - mevcutAdet;
            if (kapasite <= 0)
            {
                return 0;
            }

            var baslangic = seri.UretilenSonTarih?.AddTicks(1) ?? seri.Dtstart;
            var tarihler = RRuleGenisletici.Genislet(
                seri.Rrule, seri.Dtstart, hedefUfuk, kapasite, baslangicDahil: baslangic);

            if (tarihler.Count == 0)
            {
                // Üretilecek yeni tekrar yok; imi ileri taşı ki tekrar taranmasın.
                seri.UretilenSonTarih = hedefUfuk;
                await _context.SaveChangesAsync(iptalJetonu);
                return 0;
            }

            // Şablon: en son (ayrık olmayan) tekrar — içerik güncellemeleri yansısın.
            var sablon = await _context.Ajandalar
                .Include(a => a.Katilimcilar)
                .IgnoreQueryFilters()
                .Where(a => a.SeriId == seri.Id && !a.SeriAyrik)
                .OrderByDescending(a => a.SeriOrijinalBaslangic ?? a.BaslangicTarihi)
                .FirstOrDefaultAsync(iptalJetonu);

            sablon ??= await _context.Ajandalar
                .Include(a => a.Katilimcilar)
                .IgnoreQueryFilters()
                .Where(a => a.SeriId == seri.Id)
                .OrderBy(a => a.SeriOrijinalBaslangic ?? a.BaslangicTarihi)
                .FirstOrDefaultAsync(iptalJetonu);

            if (sablon == null)
            {
                return 0;   // seride hiç tekrar kalmamış; üretmek için şablon yok
            }

            var katilimciIdler = sablon.Katilimcilar
                .Where(k => k.BirimId != null)
                .Select(k => k.BirimId!.Value)
                .ToList();

            var kisiKatilimciIdler = sablon.Katilimcilar
                .Where(k => k.KullaniciId != null)
                .Select(k => k.KullaniciId!.Value)
                .ToList();

            var yeniler = tarihler
                .Select(t => TekrarUret(sablon, t, seri.SureDakika, seri, sablon.BirimId, sablon.KullaniciId, katilimciIdler, kisiKatilimciIdler))
                .ToList();

            foreach (var y in yeniler)
            {
                y.RandevuId = 0;
            }

            _context.Ajandalar.AddRange(yeniler);
            seri.UretilenSonTarih = UretilenSonTarihHesapla(tarihler, hedefUfuk);
            await _context.SaveChangesAsync(iptalJetonu);

            return yeniler.Count;
        }

        // ===================================================================
        //  YARDIMCILAR
        // ===================================================================
        private static string KapsamMetni(TekrarKapsam kapsam) => kapsam switch
        {
            TekrarKapsam.Tumu => "tüm tekrarlar",
            TekrarKapsam.BundanSonrakiler => "bu ve sonraki tekrarlar",
            _ => "yalnızca bu tekrar"
        };

        /// <summary>RRULE ayrıştırma hatasını istemciye 400 olarak ileten sarmalayıcı.</summary>
        private static RRuleKural KuralAyristir(string rrule)
        {
            try
            {
                return RRuleKural.Ayristir(rrule);
            }
            catch (FormatException ex)
            {
                throw new BusinessRuleException("Tekrar kuralı geçersiz: " + ex.Message);
            }
        }

        /// <summary>
        /// Tekrar süresi. Öncelik sırası:
        /// 1) Formdaki başlangıç/bitiş farkı — kullanıcının EKRANDA gördüğü değer,
        /// 2) açıkça gönderilen <c>SureDakika</c>,
        /// 3) 30 dakika (uygulamanın geleneksel varsayılanı).
        ///
        /// Sıra önemli: "tümü" kapsamıyla bitiş saati uzatıldığında, serinin eski
        /// süresi yedek olarak geldiği için ilk kural olmasa yeni süre yoksayılırdı.
        /// </summary>
        private static int SureHesapla(AjandaDto dto, int? verilen)
        {
            if (dto.BitisTarihi != null && dto.BitisTarihi > dto.BaslangicTarihi)
            {
                return (int)Math.Round((dto.BitisTarihi.Value - dto.BaslangicTarihi).TotalMinutes);
            }

            if (verilen is > 0)
            {
                return verilen.Value;
            }

            return 30;
        }

        /// <summary>
        /// Üretim ufku: bugünden ya da (ileri tarihli seride) başlangıçtan itibaren
        /// <see cref="UfukAy"/> ay.
        /// </summary>
        private static DateTime UfukHesapla(DateTime dtstart)
            => (dtstart > DateTime.Now ? dtstart : DateTime.Now).AddMonths(UfukAy);

        /// <summary>
        /// Ufuk imi. Tavana takılarak kesildiyse son üretilen tekrar; aksi hâlde
        /// ufkun kendisi (o ana kadar her şey üretilmiş demektir).
        /// </summary>
        private static DateTime UretilenSonTarihHesapla(List<DateTime> tarihler, DateTime ufuk)
            => tarihler.Count >= EnFazlaTekrar ? tarihler[^1] : ufuk;

        /// <summary>Şablon etkinlikten tek bir tekrar satırı üretir.</summary>
        private static Ajanda TekrarUret(
            Ajanda sablon, DateTime baslangic, int sureDakika, AjandaSeri seri,
            long? birimId, string? kullaniciAdi, IEnumerable<long> katilimciIdler,
            IEnumerable<long>? kisiKatilimciIdler = null)
        {
            var ajanda = new Ajanda
            {
                Baslik = sablon.Baslik,
                Aciklama = sablon.Aciklama,
                Konum = sablon.Konum,
                Koordinat = sablon.Koordinat,
                BaslangicTarihi = baslangic,
                BitisTarihi = baslangic.AddMinutes(sureDakika),
                TumGun = sablon.TumGun,
                IrtibatKisi = sablon.IrtibatKisi,
                IrtibatTelefon = sablon.IrtibatTelefon,
                BasinKatilsin = sablon.BasinKatilsin,
                KonusmaMetniDurum = sablon.KonusmaMetniDurum,
                BilgiNotuDurum = sablon.BilgiNotuDurum,
                ResimVar = sablon.ResimVar,
                BilgiNotu = sablon.BilgiNotu,
                KonusmaMetni = sablon.KonusmaMetni,
                RandevuTipId = sablon.RandevuTipId,
                DurumId = sablon.DurumId,
                Status = sablon.Status,
                Gizli = sablon.Gizli,
                BirimId = birimId,
                KullaniciId = kullaniciAdi,
                TekrarEden = true,
                SeriId = seri.Id,
                SeriOrijinalBaslangic = baslangic,
                SeriAyrik = false,
                OlusturmaTarihi = DateTime.Now,
                GuncellemeTarihi = DateTime.Now
            };

            // Katılımcılar BİRİM: seri her tekrara aynı davetli birimleri taşır.
            foreach (var id in katilimciIdler.Distinct())
            {
                ajanda.Katilimcilar.Add(
                    new AjandaKatilimci { BirimId = id, OlusturmaTarihi = DateTime.Now });
            }

            // ESKİ kişi katılımcılar da kopyalanır. Kopyalanmazsa sahadaki eski
            // uygulama sürümlerinin oluşturduğu tekrarlar KATILIMCISIZ kalır ve
            // o kişiler gizli tekrarları göremez — sessiz bir erişim kaybı.
            foreach (var id in (kisiKatilimciIdler ?? []).Distinct())
            {
                ajanda.Katilimcilar.Add(
                    new AjandaKatilimci { KullaniciId = id, OlusturmaTarihi = DateTime.Now });
            }

            return ajanda;
        }

        /// <summary>
        /// Seri genelinde uygulanabilir İÇERİK alanları (tarih/saat hariç).
        /// Tarih alanları çağıran tarafından ayrıca hesaplanır.
        /// </summary>
        private static void IcerikAlanlariniUygula(AjandaDto dto, Ajanda ajanda)
        {
            ajanda.Baslik = dto.Baslik;
            ajanda.Aciklama = dto.Aciklama;
            ajanda.Konum = dto.Konum;
            ajanda.Koordinat = dto.Koordinat;
            ajanda.TumGun = dto.TumGun;
            ajanda.IrtibatKisi = dto.IrtibatKisi;
            ajanda.IrtibatTelefon = dto.IrtibatTelefon;
            ajanda.BasinKatilsin = dto.BasinKatilsin;
            ajanda.KonusmaMetniDurum = dto.KonusmaMetniDurum;
            ajanda.BilgiNotuDurum = dto.BilgiNotuDurum;
            ajanda.ResimVar = dto.ResimVar;
            ajanda.BilgiNotu = dto.BilgiNotu;
            ajanda.KonusmaMetni = dto.KonusmaMetni;
            ajanda.RandevuTipId = dto.RandevuTipId;
            ajanda.DurumId = dto.DurumId;
            ajanda.Gizli = dto.Gizli;
        }

        /// <summary>
        /// Katılımcı listesini DTO'ya göre eşitler. <c>KatilimciIdler</c> null ise
        /// dokunulmaz (kısmi güncelleme); gizlilik kapatıldıysa liste temizlenir.
        /// </summary>
        private Task KatilimcilariEsitleAsync(Ajanda ajanda, AjandaDto dto)
            => _context.KatilimcilariEsitleAsync(
                ajanda.Id, dto.Gizli, dto.KatilimciBirimIdler, dto.KatilimciIdler,
                _currentUserService.GetCurrentBirimId());

        /// <summary>Not/fotoğraf/çiçek taşıyan tekrarlar — silinmek yerine arşivlenirler.</summary>
        private async Task<HashSet<long>> VeriTasiyanlarAsync(List<long> idler)
        {
            if (idler.Count == 0)
            {
                return [];
            }

            // AjandaNot.AjandaId ve Cicek.AjandaId nullable; 0'a düşürerek karşılaştır.
            var notlu = await _context.AjandaNotlar.Where(n => idler.Contains(n.AjandaId ?? 0))
                .Select(n => n.AjandaId ?? 0).Distinct().ToListAsync();
            var fotografli = await _context.AjandaPhotos.Where(p => idler.Contains(p.AjandaId))
                .Select(p => p.AjandaId).Distinct().ToListAsync();
            var cicekli = await _context.Cicekler.Where(c => idler.Contains(c.AjandaId ?? 0))
                .Select(c => c.AjandaId ?? 0).Distinct().ToListAsync();

            return [.. notlu, .. fotografli, .. cicekli];
        }

        private async Task<Ajanda?> GorunurTekAsync(long id)
        {
            var kullaniciId = await _currentUserService.GetUserIdAsync();
            var kullaniciAdi = _currentUserService.GetUsername();
            var birimId = _currentUserService.GetCurrentBirimId();
            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            return await _context.Ajandalar
                .Include(a => a.Katilimcilar)
                .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
                .FirstOrDefaultAsync(a => a.Id == id && a.BirimId == birimId);
        }

        private async Task<AjandaDto> DtoyaCevirAsync(Ajanda ajanda, AjandaSeri seri)
        {
            var dto = _mapper.Map<AjandaDto>(ajanda);
            dto.TekrarKurali = seri.Rrule;
            dto.TekrarOzeti = RRuleGenisletici.Ozet(seri.Rrule);
            dto.TekrarBitisi = RRuleGenisletici.SonTekrar(seri.Rrule, seri.Dtstart);
            dto.SeriId = seri.Id;
            dto.SeriOrijinalBaslangic = ajanda.SeriOrijinalBaslangic;
            dto.SeriAyrik = ajanda.SeriAyrik;
            dto.Katilimcilar = await _context.KatilimcilariGetirAsync(ajanda.Id);
            return dto;
        }
    }
}
