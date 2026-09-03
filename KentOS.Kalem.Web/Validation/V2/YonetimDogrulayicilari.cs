using FluentValidation;
using KentOS.Kalem.Application.Dto.V2.Yonetim;
using KentOS.Kalem.Application.Identity;

namespace KentOS.Kalem.Web.Validation.V2;

public class KullaniciOlusturIstegiDogrulayici : AbstractValidator<KullaniciOlusturIstegi>
{
    public KullaniciOlusturIstegiDogrulayici()
    {
        RuleFor(x => x.KullaniciAdi)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(64).WithMessage("Kullanıcı adı en fazla 64 karakter olabilir.")
            .Matches("^[a-zA-Z0-9._@-]+$")
            .WithMessage("Kullanıcı adı yalnızca harf, rakam ve . _ @ - karakterlerini içerebilir.");

        // Identity'nin varsayılan politikasıyla aynı taban; buradaki mesaj
        // Türkçe olduğu için kullanıcı Identity'nin İngilizce hatasını görmez.
        RuleFor(x => x.Parola)
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(6).WithMessage("Parola en az 6 karakter olmalı.");

        RuleFor(x => x.Telefon)
            .Matches(@"^0\d{10}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefon))
            .WithMessage("Telefon 0 ile başlayan 11 haneli olmalı (örn. 05551112233).");

        RuleFor(x => x.Eposta)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Eposta))
            .WithMessage("Geçerli bir e-posta girin.");

        RuleFor(x => x.Roller)
            .NotEmpty().WithMessage("En az bir rol seçilmelidir.")
            .Must(BilinenRoller).WithMessage("Tanımsız rol gönderildi.");

        // SMS gönderilecekse telefon şart; aksi hâlde kullanıcı giriş bilgisini
        // hiç öğrenemez ve bu sessizce olur.
        RuleFor(x => x.Telefon)
            .NotEmpty()
            .When(x => x.SmsGonder)
            .WithMessage("SMS gönderilecekse telefon numarası zorunludur.");
    }

    /// <summary>
    /// Rol adları BOŞ olmasın — varlık denetimi servis katmanında.
    /// </summary>
    /// <remarks>
    /// Önce <c>UserRoles.GetRoles()</c> ile koddaki sabit listeye bakılıyordu.
    /// Rol artık yönetim ekranından oluşturulabildiği için bu, <b>yeni
    /// oluşturulan hiçbir rolün kullanıcıya atanamaması</b> demekti: rol
    /// yaratılıyor, izinleri veriliyor, sonra kimseye verilemiyordu.
    /// Gerçek denetim <c>YonetimServisi</c> içinde, veritabanındaki rollere
    /// karşı yapılır.
    /// </remarks>
    internal static bool BilinenRoller(List<string> roller) =>
        roller.All(r => !string.IsNullOrWhiteSpace(r));
}

public class KullaniciGuncelleIstegiDogrulayici : AbstractValidator<KullaniciGuncelleIstegi>
{
    public KullaniciGuncelleIstegiDogrulayici()
    {
        RuleFor(x => x.KullaniciAdi)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(64).WithMessage("Kullanıcı adı en fazla 64 karakter olabilir.");

        RuleFor(x => x.Telefon)
            .Matches(@"^0\d{10}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefon))
            .WithMessage("Telefon 0 ile başlayan 11 haneli olmalı (örn. 05551112233).");

        RuleFor(x => x.Eposta)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Eposta))
            .WithMessage("Geçerli bir e-posta girin.");

        RuleFor(x => x.Roller)
            .NotEmpty().WithMessage("En az bir rol seçilmelidir.")
            .Must(KullaniciOlusturIstegiDogrulayici.BilinenRoller)
            .WithMessage("Tanımsız rol gönderildi.");
    }
}

public class ParolaSifirlaIstegiDogrulayici : AbstractValidator<ParolaSifirlaIstegi>
{
    public ParolaSifirlaIstegiDogrulayici()
    {
        RuleFor(x => x.YeniParola)
            .NotEmpty().WithMessage("Yeni parola zorunludur.")
            .MinimumLength(6).WithMessage("Parola en az 6 karakter olmalı.");
    }
}

public class BirimIstegiDogrulayici : AbstractValidator<BirimIstegi>
{
    public BirimIstegiDogrulayici()
    {
        RuleFor(x => x.Ad)
            .NotEmpty().WithMessage("Birim adı zorunludur.")
            .MaximumLength(100).WithMessage("Birim adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.Yetkili)
            .NotEmpty().WithMessage("Yetkili zorunludur.")
            .MaximumLength(100).WithMessage("Yetkili en fazla 100 karakter olabilir.");

        RuleFor(x => x.Eposta)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Eposta))
            .WithMessage("Geçerli bir e-posta girin.");
    }
}
