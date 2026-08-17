using FluentValidation;
using KentOS.Mini.Application.Dto.V2.Etkinlik;

namespace KentOS.Mini.Web.Validation.V2;

public class AralikIstegiDogrulayici : AbstractValidator<AralikIstegi>
{
    public AralikIstegiDogrulayici()
    {
        RuleFor(x => x.Baslangic).NotEmpty().WithMessage("Başlangıç tarihi zorunludur.");
        RuleFor(x => x.Bitis)
            .NotEmpty().WithMessage("Bitiş tarihi zorunludur.")
            .GreaterThan(x => x.Baslangic).WithMessage("Bitiş, başlangıçtan sonra olmalı.");

        // Aşırı geniş pencere sunucuyu yorar; en fazla ~14 ay.
        RuleFor(x => x)
            .Must(x => (x.Bitis - x.Baslangic).TotalDays <= 430)
            .WithMessage("Tarih aralığı en fazla 14 ay olabilir.");
    }
}

public class ZamanIstegiDogrulayici : AbstractValidator<ZamanIstegi>
{
    public ZamanIstegiDogrulayici()
    {
        RuleFor(x => x.Baslangic).NotEmpty().WithMessage("Başlangıç zorunludur.");

        RuleFor(x => x.Bitis)
            .GreaterThan(x => x.Baslangic)
            .When(x => x.Bitis.HasValue)
            .WithMessage("Bitiş, başlangıçtan sonra olmalı.");

        RuleFor(x => x)
            .Must(x => !x.Bitis.HasValue || (x.Bitis.Value - x.Baslangic).TotalMinutes >= 15)
            .WithMessage("Etkinlik en az 15 dakika olmalı.");

        RuleFor(x => x.Kapsam)
            .InclusiveBetween(0, 2)
            .WithMessage("Geçersiz tekrar kapsamı.");
    }
}
