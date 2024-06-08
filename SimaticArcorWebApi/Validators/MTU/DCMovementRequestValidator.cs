using FluentValidation;
using SimaticArcorWebApi.Model.Constants;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Modules.DCMovement;
using System.Linq;

namespace SimaticArcorWebApi.Validators.MTU
{
  public class DCMovementRequestValidator : AbstractValidator<DCMovementRequest>
  {
    public DCMovementRequestValidator()
    {
      RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id es obligatorio");
      RuleFor(m => m.MaterialDefinitionID).NotEmpty().WithMessage("El campo MaterialDefinitionId es obligatorio");
      RuleFor(m => m.MaterialLotProperty).Must(i => i.Any(p => p.Id != MTUConstants.MTUConstantsProperties.FechaIngresoCD)).WithMessage("No existe la propiedad Fecha de IngresoCD en el request.");
      RuleFor(m => m.MaterialLotProperty).Must(i => i.Any(p => p.Id != MTUConstants.MTUConstantsProperties.PosicionCD)).WithMessage("No existe la propiedad Fecha de IngresoCD en el request."); 
    }
  }
}
