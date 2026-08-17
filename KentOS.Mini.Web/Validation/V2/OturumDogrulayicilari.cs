using FluentValidation;
using KentOS.Mini.Application.Dto.V2.Oturum;

namespace KentOS.Mini.Web.Validation.V2;

public class GirisIstegiDogrulayici : AbstractValidator<GirisIstegi>
{
    public GirisIstegiDogrulayici()
    {
        RuleFor(x => x.KullaniciAdi)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(256).WithMessage("Kullanıcı adı çok uzun.");

        RuleFor(x => x.Parola)
            .NotEmpty().WithMessage("Parola zorunludur.");
    }
}
