using FluentValidation;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;

namespace SimaticArcorWebApi.Validators.MTU
{
    public class MTUValidator : AbstractValidator<MTURequest>
    {
        public MTUValidator()
        {
            RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id es obligatorio");
        }
    }
}
