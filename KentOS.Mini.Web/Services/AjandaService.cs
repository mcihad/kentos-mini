using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using KentOS.Mini.Application.Dto;
using KentOS.Mini.Application.Dto.Randevu;
using KentOS.Mini.Application.Dto.ViewModels;
using KentOS.Mini.Application.Enums;
using KentOS.Mini.Application.Identity;
using KentOS.Mini.Application.Models;
using KentOS.Mini.Application.Services;
using KentOS.Mini.Web.Data;
using KentOS.Mini.Web.Exceptions;
using System.Diagnostics;
using System.Linq;

namespace KentOS.Mini.Web.Services
{
    public class AjandaService(
        AppDbContext _context,
        ICurrentUserService _currentUserService,
        Options.ApplicationOptions _uygulamaAyari,
        IMessageService _messageService,
        IAjandaOlayService _olayService,
        Storage.IFileStorage _fileStorage,
        IAjandaSeriService _seriService,
        IMapper _mapper,
        // İSTEĞE BAĞLI: testler bu servisi kurmadan çalışıyor ve gizli
        // etkinlik yetkisi bayraktan da gelebiliyor. Zorunlu yapmak, izin
        // sistemiyle ilgisi olmayan onlarca testi kurulum değişikliğine
        // zorlardı.
        AuthPolicies.IIzinServisi? _izinler = null) : IAjandaService
    {
        // ---------------------------------------------------------------- gizlilik
        // Gizli etkinlikleri yalnızca EKLEYEN ve KATILIMCILAR görür. Filtre tek
        // yerde tanımlıdır (AjandaSorguUzantilari.GorunurOlanlar) ve buradaki iki
        // yardımcı üzerinden TÜM okuma yollarına uygulanır. Gizli olmayan
        // etkinliklerde davranış birebir aynı kalır.

        /// <summary>
        /// Çağıran kullanıcının görebileceği etkinlikler. <paramref name="filtreleriAtla"/>
        /// true ise soft-delete global filtresi atlanır (silinmiş kayıt listeleri) —
        /// gizlilik filtresi bundan etkilenmez, çünkü açık bir Where'dir.
        /// </summary>
        private async Task<IQueryable<Ajanda>> GorunurAjandalarAsync(bool filtreleriAtla = false)
        {
            var kullaniciId = await _currentUserService.GetUserIdAsync();
            var kullaniciAdi = _currentUserService.GetUsername();
            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            var kaynak = filtreleriAtla
                ? _context.Ajandalar.IgnoreQueryFilters()
                : _context.Ajandalar.AsQueryable();

            return kaynak.GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin);
        }

        /// <summary>
        /// Etkinliğin ALT KAYNAKLARI (not, fotoğraf, çiçek, zaman çizelgesi) için
        /// erişim kapısı. Gizli etkinlikte yetkisiz kişi için false döner; gizli
        /// olmayan etkinlikte her zaman true (mevcut davranış korunur).
        /// Silinmiş gizli etkinliğin verisi de sızmasın diye filtreler atlanır.
        /// </summary>
        public async Task<bool> GorebilirMiAsync(long ajandaId)
        {
            var kullaniciId = await _currentUserService.GetUserIdAsync();
            var kullaniciAdi = _currentUserService.GetUsername();

            // Basın kullanıcısı ajandanın yalnızca basına açık kısmını görür.
            // Kapı `ICurrentUserService`te: bu sorguların hepsinde zaten var.
            var yalnizcaBasin = await _currentUserService.YalnizcaBasinMiAsync();

            return await _context.Ajandalar
                .IgnoreQueryFilters()
                .Where(a => a.Id == ajandaId)
                .GorunurOlanlar(kullaniciId, kullaniciAdi, yalnizcaBasin)
                .AnyAsync();
        }

        // ------------------------------------------------------------ zenginleştirme
        /// <summary>
        /// DTO'ları eşlemenin ULAŞAMADIĞI bilgilerle tamamlar: gizli etkinliğin
        /// katılımcıları ve tekrar serisinin kuralı/özeti.
        ///
        /// Bu alanlar Mapster ile eşlenmez (katılımcıda kimlik karışması riski,
        /// seride ayrı tablo). Liste uçlarında TEK sorguyla toplu okunur — etkinlik
        /// başına sorgu açılmaz.
        /// </summary>
        private async Task<IEnumerable<AjandaDto>> ZenginlestirAsync(IEnumerable<AjandaDto> dtolar)
        {
            var liste = dtolar as IList<AjandaDto> ?? dtolar.ToList();
            if (liste.Count == 0)
            {
                return liste;
            }

            // KATILIMCILAR HER ETKİNLİKTE okunur.
            //
            // Burada bir dönem yalnızca GİZLİ etkinlikler için yükleniyordu;
            // katılımcı o zaman "bu gizli kaydı kim görebilir" demekti.
            // Katılımcı BİRİM eklendiğinde anlam genişledi — açık bir
            // toplantıya müdürlük davet etmek de katılımcıdır — ama bu satır
            // olduğu gibi kaldı: birimler kaydediliyor, hiçbir ekranda
            // görünmüyordu. Düzenlemeye girildiğinde liste boş geliyor,
            // kaydedince de seçim siliniyordu.
            var idler = liste.Select(d => d.Id).ToList();
            var harita = await _context.KatilimcilariGetirAsync(idler);
            foreach (var d in liste)
            {
                if (harita.TryGetValue(d.Id, out var katilimcilar))
                {
                    d.Katilimcilar = katilimcilar;
                }
            }

            // BİRİM ADI: "bu etkinlik kimin ajandasından geldi?"
            //
            // Katılımcı birim, davet edildiği etkinliği kendi kayıtlarıyla yan
            // yana görüyor ve ayırt edemiyordu. Tek sorguyla dolduruluyor;
            // Mapster'a bırakmak her sorguda `Include(a => a.Birim)` şartı
            // koymak ve unutulan yerde alanı sessizce boş bırakmak demekti.
            var birimIdler = liste.Where(d => d.BirimId != null)
                .Select(d => d.BirimId!.Value).Distinct().ToList();
            if (birimIdler.Count > 0)
            {
                var birimAdlari = await _context.Birimler
                    .Where(b => birimIdler.Contains(b.Id))
                    .Select(b => new { b.Id, b.Ad })
                    .ToDictionaryAsync(b => b.Id, b => b.Ad);

                foreach (var d in liste)
                {
                    if (d.BirimId != null && birimAdlari.TryGetValue(d.BirimId.Value, out var ad))
                    {
                        d.BirimAd = ad;
                    }
                }
            }

            var seriIdler = liste.Where(d => d.SeriId != null).Select(d => d.SeriId!.Value).Distinct().ToList();
            if (seriIdler.Count > 0)
            {
                var kurallar = await _context.AjandaSeriler
                    .Where(s => seriIdler.Contains(s.Id))
                    .Select(s => new { s.Id, s.Rrule, s.Dtstart })
                    .ToDictionaryAsync(s => s.Id, s => s);

                foreach (var d in liste)
                {
                    if (d.SeriId != null && kurallar.TryGetValue(d.SeriId.Value, out var seri))
                    {
                        d.TekrarKurali = seri.Rrule;
                        d.TekrarOzeti = RRuleGenisletici.Ozet(seri.Rrule);
                        d.TekrarBitisi = RRuleGenisletici.SonTekrar(seri.Rrule, seri.Dtstart);
                    }
                }
            }

            return liste;
        }

        private async Task<AjandaDto> ZenginlestirAsync(AjandaDto dto)
            => (await ZenginlestirAsync(new[] { dto })).First();

        /// <summary>
        /// Gizlilikle çelişen alan birleşimlerini engeller.
        ///
        /// "Basın katılsın" işareti etkinliği medya listesine (Medya modülü, PWA
        /// görünümü) taşır; gizli bir etkinlik için bu ikisi bir arada anlamsızdır.
        /// Arayüzlerde de karşılıklı olarak kapatılır, bu son kapıdır.
        /// </summary>
        private static void GizlilikTutarliligiKontrol(Ajanda ajanda)
        {
            if (ajanda.Gizli && ajanda.BasinKatilsin)
            {
                throw new BusinessRuleException(
                    "Gizli etkinlikte 'Basın Katılsın' seçilemez — basın listesi birim genelinde görünür.");
            }
        }

        /// <summary>
        /// Gizli etkinlik OLUŞTURMA yetkisini denetler.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Yetkinin kaynağı <b>izindir</b> (<c>ajanda.gizliEtkinlik</c>), rol
        /// değil. <c>AppUser.GizliEtkinlikEkleyebilir</c> sütunu veritabanında
        /// duruyor ama artık karar vermiyor: aynı yetkinin iki kaynağı olması,
        /// yönetim ekranından kısılan bir iznin kullanıcı kaydından açık
        /// kalması demekti ve hangisinin geçerli olduğu ekrandan
        /// anlaşılmıyordu.
        /// </para>
        /// <para>
        /// <b>Bu bayrak görünürlüğü ETKİLEMEZ.</b> Gizli bir etkinliği yine
        /// yalnızca oluşturan ve katılımcıları görür; bayrağa sahip biri
        /// başkasının gizli kaydını göremez.
        /// </para>
        /// <para>
        /// Kontrol servis katmanında: hem v1 (mobil) hem v2 (web) buradan
        /// geçiyor. Yalnızca controller'da olsaydı mobil kapıyı atlardı.
        /// </para>
        /// </remarks>
        private async Task GizliEkleyebilirMiKontrolAsync(Ajanda ajanda)
        {
            if (!ajanda.Gizli) return;

            var kullanici = await _currentUserService.GetCurrentAsync();
            if (kullanici is null)
            {
                throw new BusinessRuleException("Oturum çözülemedi.");
            }

            // TEK KAYNAK: izin.
            //
            // `_izinler` yalnızca birim testlerinde null olabilir (servis eski
            // kurucuyla kuruluyor); orada bayrak son sözü söyler. Uygulamada
            // her zaman doludur.
            if (_izinler is null)
            {
                if (kullanici.GizliEtkinlikEkleyebilir) return;
            }
            else if (await _izinler.VarMiAsync(kullanici.Id, Izinler.AjandaGizliEtkinlik))
            {
                return;
            }

            throw new BusinessRuleException(
                "Gizli etkinlik oluşturma yetkiniz yok. " +
                "Bu yetki rolünüzden ya da kullanıcı kaydınızdan gelir; birim yöneticinizle görüşün.");
        }

        // --------------------------------------------------------------- bildirim
        /// <summary>
        /// Etkinlik bildirimlerinin TEK dağıtım noktası.
        ///
        /// Gizli etkinlikte alıcılar: <b>katılımcılar ∪ ekleyen</b>.
        /// Gizli olmayan etkinlikte: bugüne kadar olduğu gibi <b>etkinliğin birimi</b>
        /// (<see cref="IMessageService.CreateForAllPersonAsync"/>) — davranış birebir korunur.
        ///
        /// <paramref name="birimId"/> verilmezse etkinliğin birimi kullanılır; havale
        /// gibi "hedef birime bildir" durumlarında açıkça geçilir.
        /// </summary>
        private async Task BildirAsync(
            Ajanda ajanda,
            string baslik,
            string icerik,
            SendMessageType tip,
            NotifikasyonTip notifikasyonTip,
            string? data,
            long? birimId = null)
        {
            await AjandaSorguUzantilari.EtkinlikBildirAsync(
                _context, _messageService, ajanda, baslik, icerik, tip, notifikasyonTip, data,
                birimId ?? ajanda.BirimId ?? _currentUserService.GetCurrentBirimId());
        }

        /// <summary>
        /// GÖRME kapsamındaki etkinliği getirir: kendi birimi <b>veya</b>
        /// katılımcı olarak çağrıldığı etkinlik. Gizlilik yine üstte.
        /// </summary>
        private async Task<Ajanda> KapsamdakiEtkinlikAsync(long? id)
        {
            if (id == null) throw new EntityNotFoundException("Ajanda bulunamadı");

            var birimId = _currentUserService.GetCurrentBirimId();
            var ajanda = await (await GorunurAjandalarAsync())
                .Include(a => a.AjandaNotlar)
                .BirimKapsami(birimId)
                .FirstOrDefaultAsync(a => a.Id == id);

            return ajanda ?? throw new EntityNotFoundException("Etkinlik bulunamadı");
        }

        public async Task<Ajanda> GetByIdAsync(long? id)
        {
            if (id == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");
            }

            var birimId = _currentUserService.GetCurrentBirimId();
            var ajanda = await (await GorunurAjandalarAsync())
                .Include(a => a.Birim)
                .Include(a => a.RandevuTip)
                .Include(a => a.Durum)
                .FirstOrDefaultAsync(m => m.Id == id && m.BirimId == birimId);

            return ajanda == null ? throw new EntityNotFoundException("Ajanda bulunamadı") : ajanda;
        }


        public async Task<List<Ajanda>> GetAllAsync(AjandaSearchParametersDto searchParameters)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var query = (await GorunurAjandalarAsync())
                .Include(a => a.Birim)
                .Include(a => a.RandevuTip)
                .Include(a => a.Durum)
                .Include(a => a.Photos)
                // Çağrılan birim de görür — yazma yolları sahibine bağlı kalır.
                .BirimKapsami(birimId)
                .AsQueryable();

            if (searchParameters.StartDate.HasValue)
            {
                query = query.Where(a => a.BaslangicTarihi >= searchParameters.StartDate.Value);
            }
            if (searchParameters.EndDate.HasValue)
            {
                query = query.Where(a => a.BaslangicTarihi <= searchParameters.EndDate.Value);
            }
            if (!string.IsNullOrEmpty(searchParameters.SearchString))
            {
                query = query.Where(a => a.Baslik.Contains(searchParameters.SearchString));
            }
            if (searchParameters.TipId.HasValue)
            {
                query = query.Where(a => a.RandevuTipId == searchParameters.TipId.Value);
            }
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<AjandaDto>> GetAllAsync()
        {
            //from one week before to all future events
            var birimId = _currentUserService.GetCurrentBirimId();
            var query = (await GorunurAjandalarAsync())
                .Include(a => a.Photos)
                .Include(a => a.Cicek)
                .BirimKapsami(birimId)
                .Where(a => a.BaslangicTarihi.Date >= DateTime.Now.Date)
                .OrderBy(a => a.BaslangicTarihi)
                .AsQueryable();

            return await ZenginlestirAsync(_mapper.Map<IEnumerable<AjandaDto>>(await query.ToListAsync()));
        }

        public async Task<IEnumerable<AjandaDto>> SearchAsync(AjandaSearchParametersDto searchParameters)
        {
            var birimId = _currentUserService.GetCurrentBirimId();

            // Silinmiş (soft-delete) filtresi. Global query filter varsayılan olarak
            // yalnızca !IsDeleted döndürür; silinmişleri görmek için filtreyi atlatırız.
            // Gizlilik filtresi açık bir Where olduğu için buradan ETKİLENMEZ.
            var query = (await GorunurAjandalarAsync(
                    searchParameters.SilinmisFiltre != SilinmisFiltre.Aktif))
                .Include(a => a.Photos)
                .AsQueryable();

            // Çağrılan birim de görür (yalnızca GÖRME; yazma sahibine bağlı).
            query = query.BirimKapsami(birimId);
            if (searchParameters.SilinmisFiltre == SilinmisFiltre.Silinmis)
            {
                query = query.Where(a => a.IsDeleted);
            }
            // Tumu: IsDeleted kısıtı yok (silinmiş + aktif birlikte)

            if (searchParameters.StartDate.HasValue)
            {
                query = query.Where(a => a.BaslangicTarihi >= searchParameters.StartDate.Value);
            }
            if (searchParameters.EndDate.HasValue)
            {
                query = query.Where(a => a.BaslangicTarihi <= searchParameters.EndDate.Value);
            }
            if (!string.IsNullOrEmpty(searchParameters.SearchString))
            {
                // Daha etkili arama: Başlık + Açıklama + Konum (case-insensitive).
                var s = searchParameters.SearchString;
                query = query.Where(b =>
                    EF.Functions.ILike(b.Baslik, $"%{s}%") ||
                    (b.Aciklama != null && EF.Functions.ILike(b.Aciklama, $"%{s}%")) ||
                    (b.Konum != null && EF.Functions.ILike(b.Konum, $"%{s}%")));
            }
            if (searchParameters.TipId.HasValue)
            {
                query = query.Where(a => a.RandevuTipId == searchParameters.TipId.Value);
            }

            // Gizli / tekrarlanan süzgeçleri. Varsayılan Tumu olduğu için filtre
            // göndermeyen istemcilerin sonucu değişmez.
            query = searchParameters.GizliFiltre switch
            {
                OzellikFiltre.Sadece => query.Where(a => a.Gizli),
                OzellikFiltre.Haric => query.Where(a => !a.Gizli),
                _ => query
            };
            query = searchParameters.TekrarFiltre switch
            {
                OzellikFiltre.Sadece => query.Where(a => a.SeriId != null || a.TekrarEden),
                OzellikFiltre.Haric => query.Where(a => a.SeriId == null && !a.TekrarEden),
                _ => query
            };

            // SİLİNMİŞ LİSTESİ SİLİNME TARİHİNE GÖRE SIRALANIR.
            //
            // Bu bir çöp kutusu: merak edilen "ne zaman yapılacaktı" değil "ne
            // zaman silindi". Etkinlik tarihine göre sıralamak, dün silinmiş
            // eski bir kaydı listenin dibine atıyordu.
            //
            // Aynı anahtar `GET etkinlik/silinmis` ucunda da kullanılıyor;
            // ikisi ayrışırsa web ile mobil AYNI kayıtları FARKLI sırada
            // gösterir ve kullanıcı hangisinin doğru olduğunu bilemez.
            // Silinme anı ayrı bir sütunda değil, soft-delete'in yazdığı
            // `GuncellemeTarihi`nde.
            query = searchParameters.SilinmisFiltre == SilinmisFiltre.Silinmis
                ? query.OrderByDescending(a => a.GuncellemeTarihi ?? a.OlusturmaTarihi)
                : query.OrderByDescending(a => a.BaslangicTarihi);

            return await ZenginlestirAsync(_mapper.Map<IEnumerable<AjandaDto>>(await query.ToListAsync()));
        }

        public async Task<AjandaDto> GetAsync(long id)
        {
            var birimId = _currentUserService.GetCurrentBirimId();

            // SİLİNMİŞ kayıtların DETAYI da okunabilir (salt okunur gösterim):
            // arşiv/geçmiş aramasından bir kayda dokunulduğunda detay açılmalı.
            // Gizlilik kapısı yerinde kalır — gizli etkinliği yine yalnızca
            // ekleyen ve katılımcılar görür. Yazma yolları (GetByIdAsync) silinmiş
            // kaydı GÖRMEZ, yani silinmiş etkinlik düzenlenemez.
            // KAPSAM listeyle AYNI: kendi birimi VEYA katılımcı birim.
            //
            // Burada yalnızca `m.BirimId == birimId` vardı: davet edilen birim
            // etkinliği kendi ajandasında GÖRÜYOR (liste `BirimKapsami`
            // kullanıyor) ama üstüne dokununca detay 404 veriyordu. Aynı kayıt
            // için iki farklı kapsam, "listede var ama açılmıyor" demek.
            // Gizlilik kapısı `GorunurAjandalarAsync` içinde ve yerinde
            // duruyor: davetli birim GİZLİ etkinliği zaten göremez.
            // ÇİÇEK DE YÜKLENİR. Liste ucu (`GetAllAsync`) `Cicek`i zaten
            // Include ediyordu, DETAY etmiyordu: yanıtta `cicekId` dolu ama
            // `cicek` null geliyordu. İstemcinin elinde yalnızca "talimat
            // verilmiş mi" bilgisi kalıyor, "çiçek gitti mi" bilgisi HİÇ
            // gelmiyordu — etkinlik detayındaki rozet, çiçek teslim edilmiş
            // olsa bile sonsuza kadar "bekliyor" gösteriyordu.
            var ajanda = await (await GorunurAjandalarAsync(filtreleriAtla: true))
                .Include(a => a.Photos)
                .Include(a => a.Cicek)
                .BirimKapsami(birimId)
                .FirstOrDefaultAsync(m => m.Id == id);

            return ajanda == null
                ? throw new EntityNotFoundException("Etkinlik bulunamadı")
                : await ZenginlestirAsync(_mapper.Map<AjandaDto>(ajanda));
        }
        public async Task<IEnumerable<AjandaPhotoDto>> GetAjandaPhotosAsync(long ajandaId)
        {
            // Gizli etkinliğin fotoğrafları yalnızca ekleyen + katılımcılara açıktır.
            if (!await GorebilirMiAsync(ajandaId))
            {
                return [];
            }

            var photos = await _context.AjandaPhotos.Where(x => x.AjandaId == ajandaId).ToListAsync();
            return _mapper.Map<IEnumerable<AjandaPhotoDto>>(photos);
        }

        public async Task<IEnumerable<AjandaDto>> GetByDateAsync(DateOnly date)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var query = (await GorunurAjandalarAsync())
                .Include(a => a.Photos)
                .BirimKapsami(birimId)
                .Where(a => DateOnly.FromDateTime(a.BaslangicTarihi) == date)
                .OrderBy(a => a.BaslangicTarihi)
                .AsQueryable();

            return await ZenginlestirAsync(_mapper.Map<IEnumerable<AjandaDto>>(await query.ToListAsync()));

        }

        public async Task<AjandaDto> CreateAsync(AjandaDto ajandaDto)
        {
            // Tekrar kuralı geldiyse etkinlik bir SERİ olarak kurulur; tekrarlar
            // gerçek etkinlik satırı olarak üretilir (bkz. AjandaSeriService).
            if (ajandaDto.Tekrar != null && !string.IsNullOrWhiteSpace(ajandaDto.Tekrar.Rrule))
            {
                return await _seriService.OlusturAsync(ajandaDto);
            }

            var ajanda = _mapper.Map<Ajanda>(ajandaDto);
            ajanda.RandevuTipId = ajandaDto.RandevuTipId;
            ajanda.BirimId = _currentUserService.GetCurrentBirimId();
            ajanda.KullaniciId = _currentUserService.GetUsername();
            //ajanda bitiş tarihi başlangıç tarihinden yarım saat sonra olacak

            ajanda.OlusturmaTarihi = DateTime.Now;
            ajanda.GuncellemeTarihi = DateTime.Now;

            GizlilikTutarliligiKontrol(ajanda);
            await GizliEkleyebilirMiKontrolAsync(ajanda);

            // Katılımcılar etkinlikle AYNI kayıtta oluşur ki bildirim
            // gönderilirken alıcı listesi hazır olsun.
            //
            // Katılımcı BİRİMLER gizlilikten bağımsız: "toplantıya çağrılan
            // birim" bilgisi açık etkinlikte de anlamlı.
            if (ajandaDto.KatilimciBirimIdler is { Count: > 0 })
            {
                foreach (var id in ajandaDto.KatilimciBirimIdler.Distinct())
                {
                    ajanda.Katilimcilar.Add(
                        new AjandaKatilimci { BirimId = id, OlusturmaTarihi = DateTime.Now });
                }
            }

            // ESKİ: kişi katılımcılar yalnızca gizli etkinlikte ve yalnızca
            // sahadaki eski uygulama sürümleri için.
            if (ajanda.Gizli && ajandaDto.KatilimciIdler is { Count: > 0 })
            {
                foreach (var id in ajandaDto.KatilimciIdler.Distinct())
                {
                    ajanda.Katilimcilar.Add(
                        new AjandaKatilimci { KullaniciId = id, OlusturmaTarihi = DateTime.Now });
                }
            }

            _context.Ajandalar.Add(ajanda);

            await _context.SaveChangesAsync();
            //tarih format 12 Ocak 2022 14:00 şeklinde olacak
            var tarih = ajanda.BaslangicTarihi.ToString("dd MMMM yyyy HH:mm");
            var name = await _currentUserService.GetFullNameAsync();

            // NOT: Ekleme kaydı artık NOTLARA yazılmaz; değişiklik geçmişinin tek
            // yeri `ajanda_olaylar` tablosudur (aşağıdaki KaydetAsync). Notlar
            // yalnızca kullanıcının kendi yazdığı notlar için kullanılır.
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.Olusturuldu,
                $"Etkinlik oluşturuldu: {ajanda.Baslik}");

            var message = $"{ajanda.Baslik} konulu etkinlik {name} tarafından {tarih} tarihine eklendi.";

            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Yeni Etkinlik Eklendi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnCreated,
                data.ToJson()
            );

            return await ZenginlestirAsync(_mapper.Map<AjandaDto>(ajanda));
        }

        public async Task<AjandaDto> PostponeAsync(AjandaErteleDto ajandaErteleDto)
        {
            var ajanda = await GetByIdAsync(ajandaErteleDto.Id);
            var oldDate = ajanda.BaslangicTarihi;
            ajanda.BaslangicTarihi = ajandaErteleDto.Tarih;
            ajanda.BitisTarihi = ajandaErteleDto.Tarih.AddHours(1);
            ajanda.GuncellemeTarihi = DateTime.Now;
            var author = await _currentUserService.GetFullNameAsync();
            // Erteleme kaydı `ajanda_olaylar` tablosuna yazılır (aşağıda).
            await _context.SaveChangesAsync();

            var tarih = ajanda.BaslangicTarihi.ToString("dd MMMM yyyy HH:mm");
            var message = $"{ajanda.Baslik} konulu etkinlik {author} tarafından {tarih} tarihine ertelendi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.Ertelendi,
                "Etkinlik ertelendi",
                new[] { new AjandaAlanDegisikligiDto("Başlangıç Tarihi",
                    oldDate.ToString("dd.MM.yyyy HH:mm"),
                    ajandaErteleDto.Tarih.ToString("dd.MM.yyyy HH:mm")) });
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Etkinlik Ertelendi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnPostponed,
                data.ToJson()
            );

            return _mapper.Map<AjandaDto>(ajanda);
        }

        public async Task<AjandaDto> HavaleAsync(AjandaHavaleDto ajandaHaveleDto)
        {
            var ajanda = await GetByIdAsync(ajandaHaveleDto.Id);

            // Gizli etkinlik başka bir birime havale EDİLEMEZ: havale, etkinliği
            // hedef birimin tamamına açar ve o birimin herkesine bildirim gider —
            // gizliliğin tanımıyla çelişir.
            if (ajanda.Gizli)
            {
                throw new BusinessRuleException(
                    "Gizli etkinlik havale edilemez. Önce gizliliği kaldırın veya kişileri katılımcı olarak ekleyin.");
            }

            var oldBirimId = ajanda.BirimId;
            var newBirim = await _context.Birimler.FindAsync(ajandaHaveleDto.BirimId);
            ajanda.BirimId = ajandaHaveleDto.BirimId;
            ajanda.GuncellemeTarihi = DateTime.Now;
            var author = await _currentUserService.GetFullNameAsync();
            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).
            await _context.SaveChangesAsync();

            var message = $"{ajanda.Baslik} konulu etkinlik {author} tarafından {newBirim?.Ad} birimine havale edildi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.HavaleEdildi,
                $"Etkinlik {newBirim?.Ad} birimine havale edildi",
                new[] { new AjandaAlanDegisikligiDto("Birim",
                    oldBirimId?.ToString() ?? "(boş)", newBirim?.Ad ?? "(boş)") });
            var targetMessage = $"{ajanda.Baslik} konulu etkinlik {author} tarafından size havale edildi.";
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Etkinlik Havale Edildi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnOrganized,
                data.ToJson()
            );

            //send notification to the new department
            await BildirAsync(
                ajanda,
                "Yeni Etkinlik Eklendi",
                targetMessage,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnCreated,
                data.ToJson(),
                birimId: ajandaHaveleDto.BirimId
            );

            return _mapper.Map<AjandaDto>(ajanda);
        }

        public async Task<bool> SendToParent(long ajandaId)
        {
            var ajanda = await GetByIdAsync(ajandaId);

            // `ajanda.Birim` yüklenmemiş olabilir; doğrudan alan üzerinden
            // gitmek NullReferenceException (HTTP 500) üretiyordu.
            var ustBirimId = await _context.Birimler
                .Where(b => b.Id == ajanda.BirimId)
                .Select(b => b.UstBirimId)
                .FirstOrDefaultAsync();

            var parentBirim = ustBirimId == null
                ? null
                : await _context.Birimler.FindAsync(ustBirimId);

            if (parentBirim == null)
            {
                throw new EntityNotFoundException(
                    "Üst birim bulunamadı — bu birimin bağlı olduğu bir üst birim yok.");
            }

            ajanda.BirimId = parentBirim.Id;
            ajanda.GuncellemeTarihi = DateTime.Now;
            var author = await _currentUserService.GetFullNameAsync();
            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).
            await _context.SaveChangesAsync();

            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.UstBirimeGonderildi,
                $"Etkinlik üst birime ({parentBirim.Ad}) gönderildi");
            return true;
        }

        public async Task<AjandaDto> CicekGonderAsync(AjandaCicekGonderDto ajandaCicekGonderDto)
        {
            var ajanda = await GetByIdAsync(ajandaCicekGonderDto.AjandaId);

            // Gizli etkinlik için çiçek talimatı verilemez: talimat SMS'i kurum
            // dışındaki çiçekçiye etkinlik başlığını ve adresini gönderir, ayrıca
            // çiçekçinin anonim doğrulama kartı gizli etkinlikte 404 döner —
            // yani akış hem sızıntı hem de yarım kalır.
            if (ajanda.Gizli)
            {
                throw new BusinessRuleException(
                    "Gizli etkinlik için çiçek talimatı verilemez — talimat kurum dışı çiçekçiye gönderilir.");
            }
            var author = await _currentUserService.GetFullNameAsync();
            /*
              KOD KRİPTOGRAFİK ÜRETİLİR.

              `new Random()` zaman tabanlı tohumlanır ve tahmin edilebilir:
              aynı saniyede verilen iki talimat aynı kodu alabiliyor, kodun
              üretim anını bilen biri aralığı daraltabiliyordu. Kod, çiçeğin
              gerçekten teslim edildiğini kanıtlayan tek şey.
            */
            var dogrulamaKodu = System.Security.Cryptography.RandomNumberGenerator.GetInt32(10000, 100000);
            var cicek = new Cicek
            {
                AjandaId = ajanda.Id,
                Gonderildi = false,
                Olusturan = author,
                Guid = Guid.NewGuid().ToString(),
                Aciklama = ajandaCicekGonderDto.Not,
                Adres = ajanda.Konum,
                OlusturulmaTarihi = DateTime.Now,
                Ad = ajanda.Baslik,
                CicekciId = ajandaCicekGonderDto.CicekciId,
                DogrulamaKodu = dogrulamaKodu

            };

            // First save Cicek to get its generated ID
            _context.Cicekler.Add(cicek);
            await _context.SaveChangesAsync();

            // Now update Ajanda with the generated Cicek ID
            ajanda.CicekId = cicek.Id;
            ajanda.Cicek = cicek;

            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).
            _context.Ajandalar.Update(ajanda);
            await _context.SaveChangesAsync();

            var message = $"{ajanda.Baslik} konulu etkinliğe {author} tarafından çiçek gönderme talimatı verildi. Dogrulama kodu: {dogrulamaKodu}";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.CicekGonderildi,
                "Çiçek gönderme talimatı verildi");
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Çiçek Gönderme Talimatı",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnFlowerSent,
                data.ToJson()
            );

            var cicekci = await _context.Cicekciler.FindAsync(ajandaCicekGonderDto.CicekciId);
            /*
              BAĞLANTI YENİ ARAYÜZE GİDER.

              Eski adres `/Cicekci/CicekKart/{guid}` idi ve karşılığı olan MVC
              controller'ı YOK: SMS'teki bağlantı çiçekçide 404 açıyordu.
              Yeni adres SPA'nın anonim teslim ekranı.

              Alan adı KURUMDAN gelir (`APP__BASEURL`); koda yazılmaz.
            */
            var url = $"{_uygulamaAyari.BaseUrl.TrimEnd('/')}/cicek-teslim/{cicek.Guid}";
            var cicekciMessage = $"{ajanda.Baslik} konulu etkinlik için çiçek gönderilecek. Dogrulama kodu: {dogrulamaKodu}. Çiçeği gönderdikten sonra verilen adrese gidip doğrulama kodunu girin: {url}";
            var token = cicekci.Telefon;
            if (token.Length >= 10 && !string.IsNullOrEmpty(token))
            {
                var cicekciData = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
                await _messageService.CreateAsync(
                    cicekci.Id,
                    token,
                    "Çiçek Gönderme Talimatı",
                    cicekciMessage,
                    SendMessageType.SMS,
                    NotifikasyonTip.Always,
                    cicekciData.ToJson()
                );
            }

            return _mapper.Map<AjandaDto>(ajanda);
        }

        /// <summary>
        /// Etkinliğe not ekler.
        /// </summary>
        /// <remarks>
        /// <b>Çağrılan birim de not ekleyebilir.</b> Kapı <see cref="GetByIdAsync"/>
        /// değil: o, etkinliğin SAHİBİ birimi arıyor ve toplantıya davet edilen
        /// müdürlüğün "geleceğiz / şu kişi katılacak" diye not düşmesini
        /// engelliyordu. Not eklemek kaydı DEĞİŞTİRMEZ; düzenleme ve silme
        /// yolları sahibine bağlı kalır.
        /// </remarks>
        public async Task<bool> CreateNoteAsync(AjandaNotDto ajandaNotDto)
        {
            var ajanda = await KapsamdakiEtkinlikAsync(ajandaNotDto.AjandaId);
            var ajandaNot = _mapper.Map<AjandaNot>(ajandaNotDto);
            ajandaNot.Olusturan = _currentUserService.GetUsername();
            ajandaNot.OlusturulmaTarihi = DateTime.Now;
            ajanda.AjandaNotlar.Add(ajandaNot);
            await _context.SaveChangesAsync();
            var author = await _currentUserService.GetFullNameAsync();
            var message = $"{ajanda.Baslik} konulu etkinliğe {author} tarafından not eklendi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.NotEklendi,
                "Etkinliğe not eklendi");
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenNotes);
            await BildirAsync(
                ajanda,
                "Yeni Not Eklendi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnNoteAdded,
                data.ToJson()
            );
            return true;
        }

        public async Task<IEnumerable<AjandaNotDto>> GetAllNoteAsync(long ajandaId)
        {
            // Gizli etkinliğin notları yalnızca ekleyen + katılımcılara açıktır.
            if (!await GorebilirMiAsync(ajandaId))
            {
                return [];
            }

            // En yeni not en üstte olacak şekilde sırala.
            var ajanda = await _context.AjandaNotlar
                .Where(x => x.AjandaId == ajandaId)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
            return _mapper.Map<IEnumerable<AjandaNotDto>>(ajanda);
        }

        public async Task<CicekDto> GetCicekAsync(long ajandaId)
        {
            // Gizli etkinliğin çiçek bilgisi yalnızca ekleyen + katılımcılara açıktır.
            // Yetkisizde, çiçeği olmayan etkinlikle aynı sonuç döner (null).
            var cicek = await GorebilirMiAsync(ajandaId)
                ? await _context.Cicekler.FirstOrDefaultAsync(x => x.AjandaId == ajandaId)
                : null;

            return _mapper.Map<CicekDto>(cicek);
        }

        public async Task<bool> DeleteCicekAsync(long ajandaId)
        {
            var cicek = await _context.Cicekler.FirstOrDefaultAsync(x => x.AjandaId == ajandaId);
            var ajanda = await GetByIdAsync(ajandaId);

            if (cicek == null)
            {
                return false;
            }

            _context.Cicekler.Remove(cicek);
            ajanda.CicekId = null;
            ajanda.Cicek = null;

            var author = await _currentUserService.GetFullNameAsync();
            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).

            await _context.SaveChangesAsync();

            var message = $"{ajanda.Baslik} konulu etkinliğe çiçek gönderme talimatı iptal edildi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.CicekIptalEdildi,
                "Çiçek gönderme talimatı iptal edildi");
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Çiçek Gönderme Talimatı İptal Edildi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnFlowerDeleted,
                data.ToJson()
            );
            return true;
        }

        public async Task<bool> ChangeStateAsync(AjandaChangeStateDto ajandaChangeStateDto)
        {
            var ajanda = await GetByIdAsync(ajandaChangeStateDto.Id);
            ajanda.Status = ajandaChangeStateDto.NewStatus;

            var author = _currentUserService.GetUsername();
            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).

            await _context.SaveChangesAsync();
            var statusString = ajandaChangeStateDto.NewStatus switch
            {
                AjandaStatus.Pending => "Beklemede",
                AjandaStatus.Completed => "Tamamlandı",
                AjandaStatus.Canceled => "İptal Edildi",
                _ => "Bilinmeyen"
            };

            var message = $"{ajanda.Baslik} konulu etkinliğin iptal durumu {author} tarafından {statusString} olarak değiştirildi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.StatuDegisti,
                $"Durum {statusString} olarak değiştirildi");
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Etkinlik İptal Durumu Değiştirildi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnStatusChange,
                data.ToJson()
            );

            return true;
        }

        public async Task<AjandaDto> ChangeTipId(long ajandaId, long tipId)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var ajanda = await (await GorunurAjandalarAsync()).FirstOrDefaultAsync(x => x.Id == ajandaId && x.BirimId == birimId);
            if (ajanda == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");
            }

            ajanda.RandevuTipId = tipId;
            ajanda.GuncellemeTarihi = DateTime.Now;

            var author = _currentUserService.GetUsername();
            var tipAd = await _context.RandevuTipleri.Where(x => x.Id == tipId).Select(x => x.Ad).FirstOrDefaultAsync();
            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).

            await _context.SaveChangesAsync();
            var name = await _currentUserService.GetFullNameAsync();
            var message = $"{ajanda.Baslik} konulu etkinlik {name} tarafından {tipAd} olarak değiştirildi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.TipDegisti,
                $"Etkinlik tipi {tipAd} olarak değiştirildi");
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Etkinlik Değiştirildi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnStatusChange,
                data.ToJson()
            );

            return _mapper.Map<AjandaDto>(ajanda);
        }

        public async Task<AjandaDto> ChangeDurumId(long ajandaId, long durumId)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var ajanda = await (await GorunurAjandalarAsync()).FirstOrDefaultAsync(x => x.Id == ajandaId && x.BirimId == birimId);
            if (ajanda == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");
            }

            ajanda.DurumId = durumId;
            ajanda.GuncellemeTarihi = DateTime.Now;

            var author = _currentUserService.GetUsername();
            var durumAd = await _context.AjandaDurumlar.Where(x => x.Id == durumId).Select(x => x.Ad).FirstOrDefaultAsync();
            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).

            await _context.SaveChangesAsync();

            var name = await _currentUserService.GetFullNameAsync();
            var message = $"{ajanda.Baslik} konulu etkinliğin durumu {name} tarafından {durumAd} olarak değiştirildi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.DurumDegisti,
                $"Etkinlik durumu {durumAd} olarak değiştirildi");
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Etkinlik Durumu Değiştirildi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnStatusChange,
                data.ToJson()
            );

            return _mapper.Map<AjandaDto>(ajanda);
        }

        public async Task<AjandaDto> UploadPhotoAsync(long ajandaId, MultipartFormDataContent photo)
        {
            var ajanda = await GetByIdAsync(ajandaId);

            // Extract the file content from 'photo'
            var fileContent = photo.FirstOrDefault();
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
                // değişmedi (`wwwroot/uploads/ajanda`), nesne deposu seçiliyse
                // aynı anahtar kovaya gider. v1'in dönüş şekli etkilenmez.
                await _fileStorage.SaveAsync(
                    Storage.StorageArea.Public, $"uploads/ajanda/{fileName}", fileBytes, contentType);

                // Save details in AjandaPhoto table
                var newPhoto = new AjandaPhoto
                {
                    Filename = fileName,
                    ContentType = contentType,
                    AjandaId = ajandaId
                };
                _context.AjandaPhotos.Add(newPhoto);
                var author = _currentUserService.GetUsername();
                // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
                // tablosunda tutulur (aşağıdaki KaydetAsync).

                await _context.SaveChangesAsync();
                var name = await _currentUserService.GetFullNameAsync();
                var message = $"{ajanda.Baslik} konulu etkinliğe {name} tarafından fotoğraf eklendi.";
                await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.FotografEklendi,
                    "Etkinliğe fotoğraf eklendi");
                var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenImages);
                await BildirAsync(
                    ajanda,
                    "Yeni Fotoğraf Eklendi",
                    message,
                    SendMessageType.PushNotification,
                    NotifikasyonTip.AgendaOnImageUpload,
                    data.ToJson()
                );
            }
            return _mapper.Map<AjandaDto>(ajanda);
        }

        /// <summary>
        /// Tekrarlanan etkinliği verilen KAPSAMDA siler. Kapsam "yalnızca bu" ise
        /// (varsayılan) bugünkü tek kayıt silme davranışı işler.
        /// </summary>
        public async Task<bool> DeleteAsync(long ajandaId, TekrarKapsam kapsam)
        {
            if (kapsam == TekrarKapsam.Yalnizca)
            {
                return await DeleteAsync(ajandaId);
            }

            var seriliMi = await (await GorunurAjandalarAsync())
                .AnyAsync(a => a.Id == ajandaId && a.SeriId != null);

            return seriliMi
                ? await _seriService.SilAsync(ajandaId, kapsam)
                : await DeleteAsync(ajandaId);
        }

        public async Task<bool> DeleteAsync(long ajandaId)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var ajanda = await (await GorunurAjandalarAsync()).FirstOrDefaultAsync(x => x.Id == ajandaId && x.BirimId == birimId);
            if (ajanda == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");
            }
            ajanda.IsDeleted = true;

            // Tekrar serisine ait tek bir tekrarın silinmesi, kuralın İSTİSNASIDIR
            // (RFC 5545 EXDATE karşılığı). "Ayrık" işaretlenmezse ufuk uzatma bu
            // tekrarı yeniden üretir ve silinen etkinlik geri gelir. Kapsam
            // göndermeyen eski istemcilerin silme işlemi de bu yoldan geçtiği için
            // işaret BURADA konur.
            if (ajanda.SeriId != null)
            {
                ajanda.SeriAyrik = true;
            }

            _context.Ajandalar.Update(ajanda);

            var name = await _currentUserService.GetFullNameAsync();
            // Değişiklik geçmişi: silme kaydı (kayıt fiziksel olarak durduğu için
            // not da etkinlikle birlikte kalır ve arşivde görünür).
            // Silme kaydı `ajanda_olaylar` tablosuna yazılır (aşağıda).
            await _context.SaveChangesAsync();

            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.Silindi,
                "Etkinlik silindi olarak işaretlendi");

            var message = $"{ajanda.Baslik} konulu etkinlik {name} tarafından silindi olarak işaretlendi.";
            // Silme bildirimi TEK istisna: tıklanınca detaya gitmez, listede kalır.
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.None);
            await BildirAsync(
                ajanda,
                "Etkinlik Silindi Olarak İşaretlendi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnDeleted,
                data.ToJson()
            );
            return true;
        }

        public async Task<AjandaDto> GetByIdWithoutUserRestrictionAsync(long? id)
        {
            // DİKKAT: Bu metot birim/oturum kısıtı UYGULAMAZ — çiçekçinin anonim
            // doğrulama sayfası (Cicekci/CicekKart) tarafından kullanılıyor.
            // GİZLİ etkinlik burada da paylaşılamaz: gizli kayıt "bulunamadı"
            // sayılır, aksi halde etkinlik başlığı/konumu/tarihi kimlik
            // doğrulaması olmadan dışarı sızardı.
            var ajanda = await _context.Ajandalar
                .Include(a => a.Birim)
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(m => m.Id == id && !m.Gizli);

            return ajanda == null ? throw new EntityNotFoundException("Ajanda bulunamadı") : _mapper.Map<AjandaDto>(ajanda);

        }

        public async Task<IEnumerable<AjandaDto>> GetAllMediaJoinsAsync()
        {
            //start from today and select all media joins
            var birimId = _currentUserService.GetCurrentBirimId();
            // Basın/medya listesi kurum içi geniş bir yüzey (Medya politikası,
            // Layout'suz PWA görünümü) — GİZLİ etkinlik buraya hiç girmez.
            var ajandalar = await _context.Ajandalar
                .Include(a => a.Photos)
                .Include(a => a.Birim)
                .Include(a => a.RandevuTip)
                .Include(a => a.Durum)
                .Where(a => !a.Gizli)
                .Where(a => a.BirimId == birimId && a.BaslangicTarihi.Date >= DateTime.Now.Date && a.BasinKatilsin)
                .OrderBy(a => a.BaslangicTarihi)
                .ToListAsync();

            return _mapper.Map<IEnumerable<AjandaDto>>(ajandalar);
        }

        public async Task<IEnumerable<AjandaCountDto>> GetCountByDayAsync(int month, int year)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            // Sayaç GÜN LİSTESİYLE aynı kümeyi saymalı; ayrışınca takvimde
            // nokta görünmeyen bir güne girip etkinlik bulunuyordu.
            var query = (await GorunurAjandalarAsync())
                .BirimKapsami(birimId)
                .Where(a => a.BaslangicTarihi.Month == month && a.BaslangicTarihi.Year == year)
                .GroupBy(a => a.BaslangicTarihi.Day)
                .Select(g => new AjandaCountDto
                {
                    Day = g.Key,
                    Count = g.Count()
                });

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<AjandaDto>> GetByDateAsync(AjandaDateSearchDto dateSearch)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            // KATILIMCI BİRİM DE GÖRÜR. Burada yalnızca `a.BirimId == birimId`
            // vardı ve bu metodun DateOnly ikizi çoktan `BirimKapsami`ye
            // geçmişti: davet edilen birim etkinliği aramada ve takvimde
            // görüyor, GÜN LİSTESİNDE göremiyordu. Kullanıcının gördüğü tablo
            // "ajandama katılımcı olduğum etkinlik düşmüyor" idi.
            var query = (await GorunurAjandalarAsync())
                .AsNoTracking()
                .BirimKapsami(birimId)
                .Where(a => a.BaslangicTarihi.Date == dateSearch.Date)
                .OrderBy(a => a.BaslangicTarihi)
                .AsQueryable();

            return await ZenginlestirAsync(_mapper.Map<IEnumerable<AjandaDto>>(await query.ToListAsync()));
        }


        public async Task<bool> SendSmsToBirimAsync(SendSmsToBirimDto sendSmsToBirim)
        {
            // v1 sözleşmesi: yalnızca başarı/başarısızlık. Mantık tek yerde.
            var sonuc = await SendSmsToBirimDetayliAsync(sendSmsToBirim);
            return sonuc.Gonderilen > 0;
        }

        public async Task<SmsGonderimSonucuDto> SendSmsToBirimDetayliAsync(
            SendSmsToBirimDto sendSmsToBirim)
        {
            //get all users in the departmentIds departments and send them sms message and add note to the event all department names
            var ajanda = await GetByIdAsync(sendSmsToBirim.AjandaId);

            // Gizli etkinliğin bilgisi seçilen birimlerin TÜM kullanıcılarına SMS
            // olarak gidemez — gizlilik yalnızca katılımcıları kapsar.
            if (ajanda.Gizli)
            {
                throw new BusinessRuleException(
                    "Gizli etkinlik için birimlere SMS gönderilemez. Bilgi verilecek kişileri katılımcı olarak ekleyin.");
            }

            var author = await _currentUserService.GetFullNameAsync();
            var birimler = await _context.Birimler.Where(x => sendSmsToBirim.BirimIds.Contains(x.Id)).ToListAsync();
            var birimAdListe = string.Join(", ", birimler.Select(x => x.Ad));
            var birim = await _context.Birimler.FindAsync(_currentUserService.GetCurrentBirimId());
            var users = await _context.Users.Where(x => sendSmsToBirim.BirimIds.Contains(x.BirimId ?? 0)).ToListAsync();
            var gonderici = birim?.Yetkili + "-" + birim?.Unvan;

            var sonuc = new SmsGonderimSonucuDto
            {
                Birimler = [.. birimler.Select(x => x.Ad)],
                // Hiç kullanıcısı olmayan birim, "gönderdim ama gitmedi"nin
                // ikinci sebebi: birim seçiliyor ama içi boş.
                BosBirimler = [.. birimler
                    .Where(x => users.All(u => u.BirimId != x.Id))
                    .Select(x => x.Ad)],
            };

            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            foreach (var user in users)
            {
                // Telefonu olmayan kullanıcı atlanır; `MessageService` de aynı
                // korumayı taşıyor ama niyetin çağrı yerinde de görünmesi
                // gerekiyor: bu döngü bir birimin TÜM kullanıcılarını geziyor
                // ve numarası olmayan tek bir kişi tüm gönderimi düşürüyordu.
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    sonuc.TelefonsuzKisiler.Add($"{user.Ad} {user.Soyad}".Trim());
                    continue;
                }

                // Yer tutucular TEK yerden: `SmsYerTutucu`. Burada elle iki
                // `Replace` vardı ({gonderici}, {alici}) ve arayüzde bunu
                // söyleyen hiçbir şey yoktu — özellik vardı ama kimse
                // bilmiyordu. Katalog artık istemcilere de sunuluyor.
                var degerler = SmsYerTutucu.EtkinlikDegerleri(
                    ajanda.Baslik, ajanda.BaslangicTarihi, ajanda.Konum, birim?.Ad);
                degerler["gonderici"] = gonderici;
                degerler["alici"] = $"{user.Ad} {user.Soyad}".Trim();

                var messageStr = SmsYerTutucu.Uygula(sendSmsToBirim.Message, degerler);
                await _messageService.CreateAsync(
                    user.Id,
                    user.PhoneNumber,
                    user.UserName,
                    messageStr,
                    SendMessageType.SMS,
                    NotifikasyonTip.Always,
                    data.ToJson()
                    );

                sonuc.Gonderilen++;
            }

            // İşlem kaydı NOTLARA yazılmaz; değişiklik geçmişi `ajanda_olaylar`
            // tablosunda tutulur (aşağıdaki KaydetAsync).
            await _context.SaveChangesAsync();

            var message = $"{ajanda.Baslik} konulu etkinliğe {author} tarafından {birimAdListe} birim(lerine) SMS gönderildi.";
            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.SmsGonderildi,
                $"{birimAdListe} birim(ler)ine SMS gönderildi");
            await BildirAsync(
                ajanda,
                "SMS Gönderildi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnOrganized,
                data.ToJson()
            );

            return sonuc;
        }

        public async Task<AjandaDto> UpdateAsync(AjandaDto ajandaDto)
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var ajanda = await (await GorunurAjandalarAsync())
                .Include(a => a.Katilimcilar)
                .FirstOrDefaultAsync(x => x.Id == ajandaDto.Id && x.BirimId == birimId);
            if (ajanda == null)
            {
                throw new EntityNotFoundException("Ajanda bulunamadı");
            }

            // ---------------------------------------------------------- tekrar
            // Tekrar kararları TEK yerde toplanır; web ve mobil aynı davranışı alır.
            var yeniKural = ajandaDto.Tekrar?.Rrule?.Trim();

            // 1) Tekrarı KALDIR (açık bayrak). Alanın boş gelmesi kaldırma sayılmaz:
            //    tekrar alanını hiç göndermeyen eski istemciler serileri bozardı.
            if (ajandaDto.TekrarKaldir && ajanda.SeriId != null)
            {
                return await _seriService.TekrariKaldirAsync(ajandaDto, ajandaDto.Kapsam);
            }

            // 2) TEK SEFERLİK → TEKRARLANAN: kayıt seriye çevrilir, kimliği korunur.
            if (!string.IsNullOrEmpty(yeniKural) && ajanda.SeriId == null)
            {
                return await _seriService.SeriyeCevirAsync(ajandaDto);
            }

            // 3) Mevcut seride KURAL DEĞİŞİKLİĞİ — YALNIZCA kullanıcı "bundan
            //    sonrakiler"/"tümü" kapsamını SEÇTİYSE dikkate alınır.
            //
            //    DİKKAT (gerçek hata): İstemciler tekrar kuralını formdaki
            //    BAŞLANGIÇ TARİHİNDEN türetir. Kullanıcı tek bir tekrarı
            //    ("yalnızca bu") Çarşamba'dan Perşembe'ye aldığında istemci
            //    kuralı BYDAY=TH olarak yeniden üretip gönderiyordu; sunucu
            //    bunu "kural değişti" sayıp seriyi bölünce kullanıcının
            //    düzenlediği tekrar bambaşka bir yere taşınıyor, diğer
            //    tekrarlar da yeniden üretildiği için etkinlik "kayboluyor"
            //    gibi görünüyordu. Tek tekrar düzenlemesi kurala ASLA dokunmaz.
            if (!string.IsNullOrEmpty(yeniKural)
                && ajanda.SeriId != null
                && ajandaDto.Kapsam != TekrarKapsam.Yalnizca)
            {
                var mevcutKural = await _context.AjandaSeriler
                    .Where(x => x.Id == ajanda.SeriId.Value)
                    .Select(x => x.Rrule)
                    .FirstOrDefaultAsync();

                if (!string.Equals(mevcutKural, yeniKural, StringComparison.OrdinalIgnoreCase))
                {
                    return await _seriService.GuncelleAsync(ajandaDto, TekrarKapsam.BundanSonrakiler);
                }
            }

            // 4) Kapsam "yalnızca bu"dan farklıysa seri servisi devralır.
            //    Kapsam göndermeyen istemcilerde varsayılan Yalnizca olduğu için
            //    mevcut davranış birebir korunur.
            if (ajanda.SeriId != null && ajandaDto.Kapsam != TekrarKapsam.Yalnizca)
            {
                return await _seriService.GuncelleAsync(ajandaDto, ajandaDto.Kapsam);
            }

            // ETKİNLİĞİN SAHİBİ korunur.
            //
            // ÖLÜMCÜL HATA: `_mapper.Map(dto, entity)` gövdede olmayan alanları
            // da yazıyor ve istemciler `kullaniciId` göndermediği için her
            // güncelleme sahibi NULL'a çekiyordu. Etkinlik gizliye
            // çevrildiğinde görünürlük kuralı "oluşturan" eşleşmesine bakar;
            // sahip silinmiş olduğu için kayıt OLUŞTURANIN DA gözünden
            // kayboluyor, detay 404 veriyordu. Kullanıcı "kaydettim, etkinlik
            // kayboldu" diyordu ve sistem hatası da düşmüyordu — çünkü ortada
            // istisna yok, yalnızca görünmez olmuş bir satır var.
            var sahip = ajanda.KullaniciId;

            // Map updated properties from DTO to entity
            _mapper.Map(ajandaDto, ajanda);
            ajanda.GuncellemeTarihi = DateTime.Now;
            ajanda.BirimId = birimId;

            // Sahip boşsa (eski kayıtlar ve bu hatanın dokunduğu satırlar)
            // düzenleyen kişi sahiplenir: kaydı görebilen biri, kimsenin
            // göremediği bir kayıttan iyidir.
            ajanda.KullaniciId = string.IsNullOrWhiteSpace(sahip)
                ? _currentUserService.GetUsername()
                : sahip;

            GizlilikTutarliligiKontrol(ajanda);
            await GizliEkleyebilirMiKontrolAsync(ajanda);
            await KatilimcilariEsitleAsync(ajanda, ajandaDto);

            // Seriye ait tek bir tekrar düzenlendiyse bu tekrar artık bir İSTİSNADIR:
            // "tümü" kapsamlı seri güncellemeleri bu satırı atlar, ufuk uzatma da
            // onu yeniden üretmez.
            var ayrikOldu = false;
            if (ajanda.SeriId != null && !ajanda.SeriAyrik)
            {
                ajanda.SeriAyrik = true;
                ayrikOldu = true;
            }

            var name = await _currentUserService.GetFullNameAsync();

            // Değişiklik geçmişi (audit): değişen alanları eski→yeni olarak NOT olarak
            // kaydet. Bu not için AYRICA bildirim GÖNDERİLMEZ (sessiz kayıt).
            var entry = _context.Entry(ajanda);
            var degisenler = AuditHelper.DegisenAlanlar(entry);
            var yapisal = AuditHelper.DegisenAlanlarYapisal(entry);
            // Değişen alanlar NOTLARA yazılmaz; yapısal olarak olay kaydına gider.
            _context.Ajandalar.Update(ajanda);
            await _context.SaveChangesAsync();

            await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.Guncellendi,
                yapisal.Count > 0
                    ? $"{yapisal.Count} alan güncellendi"
                    : "Etkinlik güncellendi",
                yapisal);

            if (ayrikOldu)
            {
                await _olayService.KaydetAsync(ajanda.Id, AjandaOlayTip.TekrarAyrildi,
                    "Bu tekrar seriden ayrıldı (yalnızca bu etkinlik düzenlendi)");
            }

            // Bildirim SADECE "güncellendi" der. Değişen alan adları ve eski/yeni
            // değerler bildirime KONMAZ — push bildirimleri kilit ekranında ve
            // bildirim merkezinde herkese görünür; alan içerikleri (irtibat
            // telefonu, konum, bilgi notu vb.) oraya sızmamalı. Ayrıntı, yetkisi
            // olan kişinin görebildiği etkinlik notlarında tutulur.
            var message = $"{ajanda.Baslik} konulu etkinlik {name} tarafından güncellendi.";
            var data = new TokenDataDto(NotificationEntity.Ajanda, (int)ajanda.Id, NotificationAction.OpenDetails);
            await BildirAsync(
                ajanda,
                "Etkinlik Güncellendi",
                message,
                SendMessageType.PushNotification,
                NotifikasyonTip.AgendaOnUpdated,
                data.ToJson()
            );

            return await ZenginlestirAsync(_mapper.Map<AjandaDto>(ajanda));
        }

        /// <summary>
        /// Katılımcı listesini DTO'ya göre eşitler.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Birim katılımcılar her etkinlikte tutulur</b> — gizli olsun ya da
        /// olmasın. Katılımcı listesi artık "toplantıya kim çağrıldı"
        /// sorusunun cevabı; gizliliğe bağlamak, açık bir toplantının
        /// davetlilerini kaydedememek demekti.
        /// </para>
        /// <para>
        /// <b>Eski kişi katılımcılar</b> gizliliğe bağlı kalır: tek işlevleri
        /// gizli etkinliği görünür kılmaktı, gizlilik kapanınca anlamları yok.
        /// </para>
        /// <para>
        /// Liste alanı <c>null</c> gönderilirse o liste DOKUNULMAZ — alanı hiç
        /// göndermeyen istemciler mevcut katılımcıları silmesin.
        /// </para>
        /// </remarks>
        private async Task KatilimcilariEsitleAsync(Ajanda ajanda, AjandaDto dto)
        {
            await KatilimciEkleyebilirMiKontrolAsync(dto);

            await _context.KatilimcilariEsitleAsync(
                ajanda.Id, dto.Gizli, dto.KatilimciBirimIdler, dto.KatilimciIdler,
                _currentUserService.GetCurrentBirimId());
        }

        /// <summary>
        /// Katılımcı birim EKLEME yetkisi (<c>ajanda.katilimciEkle</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Katılımcının kendi ucu YOK; etkinlik gövdesinin bir alanı olarak
        /// geliyor. Bu yüzden denetim <c>[Izin]</c> özniteliğiyle yapılamıyor:
        /// öznitelik uca konsaydı, katılımcı izni olmayan kimse etkinlik bile
        /// oluşturamazdı (öznitelikler VE ile birleşiyor).
        /// </para>
        /// <para>
        /// Alan <b>hiç gönderilmemişse</b> denetim yapılmaz: listeye
        /// dokunmayan bir güncelleme yetki istememeli, yoksa katılımcı izni
        /// olmayan biri etkinliğin başlığını bile değiştiremezdi.
        /// </para>
        /// </remarks>
        private async Task KatilimciEkleyebilirMiKontrolAsync(AjandaDto dto)
        {
            if (dto.KatilimciBirimIdler is null || _izinler is null) return;

            var kullanici = await _currentUserService.GetCurrentAsync();
            if (kullanici is null) return;

            if (await _izinler.VarMiAsync(kullanici.Id, Izinler.AjandaKatilimciEkle)) return;

            throw new BusinessRuleException(
                "Etkinliğe katılımcı birim ekleme yetkiniz yok.");
        }

        public async Task<IEnumerable<AjandaNotDto>> GetNotesAsync(long ajandaId)
        {
            // Gizli etkinliğin notları yalnızca ekleyen + katılımcılara açıktır.
            if (!await GorebilirMiAsync(ajandaId))
            {
                return [];
            }

            // En yeni not en üstte olacak şekilde sırala.
            var notes = await _context.AjandaNotlar
                .Where(x => x.AjandaId == ajandaId)
                .OrderByDescending(x => x.OlusturulmaTarihi)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
            return _mapper.Map<IEnumerable<AjandaNotDto>>(notes);
        }

        public async Task<List<Ajanda>> GetAllFromTodayAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            var query = await (await GorunurAjandalarAsync())
                .Include(a => a.Birim)
                .Include(a => a.RandevuTip)
                .Include(a => a.Durum)
                .Include(a => a.Photos)
                .BirimKapsami(birimId)
                .Where(a => a.BaslangicTarihi.Date >= DateTime.Now.Date)
                .ToListAsync();

            return query;
        }

        public async Task<List<Ajanda>> GetDeletedAsync()
        {
            var birimId = _currentUserService.GetCurrentBirimId();
            return await (await GorunurAjandalarAsync(filtreleriAtla: true))
                .Include(a => a.Birim)
                .Include(a => a.RandevuTip)
                .Include(a => a.Durum)
                .Include(a => a.Photos)
                .Where(a => a.IsDeleted && a.BirimId==birimId)
                .ToListAsync();
        }
    }
}
