using FluentValidation;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Validators.RoadMap
{
  public class RoadMapRequestValidator : AbstractValidator<RoadMapRequest>
  {
    public RoadMapRequestValidator()
    {
      RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id es obligatorio");
      RuleFor(m => m.Plant).NotEmpty().WithMessage("El campo Plant es obligatorio");
      RuleFor(m => m.Type).NotEmpty().WithMessage("El campo Type es obligatorio");
      RuleFor(m => m.QuantityValue).NotEmpty().WithMessage("El campo QuantityValue es obligatorio");
      RuleFor(m => m.UoMNId).NotEmpty().WithMessage("El campo UoMNId es obligatorio");
      RuleForEach(m => m.Operations).SetValidator(new RoadMapRequestOperationValidator());
    }
  }
}
