using FluentValidation;
using SimaticArcorWebApi.Model.Custom;

namespace SimaticArcorWebApi.Validators.Order
{
    public class PQRLMSRequestValidator : AbstractValidator<PQRLMSRequest>
    {
        public PQRLMSRequestValidator()
        {
            RuleFor(m => m.Title).NotEmpty().WithMessage("El campo Title es obligatorio");
            RuleFor(m => m.EquipmentNId).NotEmpty().WithMessage("El campo EquipmentNId es obligatorio");
            RuleFor(m => m.IncommingMessage).NotEmpty().WithMessage("El campo IncommingMessage es obligatorio");
            RuleFor(m => m.Tipo).NotEmpty().WithMessage("El campo Tipo es obligatorio");
            
        }
    }

}
