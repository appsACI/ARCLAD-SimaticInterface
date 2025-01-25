using FluentValidation;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Validators.RoadMap
{
  public class RoadMapRequestOperationValidator : AbstractValidator<RoadMapRequestOperations>
  {
    public RoadMapRequestOperationValidator()
    {
      RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id dentro de operations debe ser obligatorio");
      RuleFor(m => m.Level).NotEmpty().WithMessage("El campo Level dentro de operations debe ser obligatorio");
      RuleFor(m => m.Operation).NotEmpty().WithMessage("El campo Operation dentro de operations debe ser obligatorio");
      RuleFor(m => m.KgHs).NotEmpty().WithMessage("El campo KgHs dentro de operations debe ser obligatorio");
      RuleFor(m => m.KgHsMach).NotEmpty().WithMessage("El campo KgHsMach dentro de operations debe ser obligatorio");
      RuleFor(m => m.Theoretical).NotEmpty().WithMessage("El campo Theoretical dentro de operations debe ser obligatorio");
      RuleFor(m => m.Efficiency).NotEmpty().WithMessage("El campo Efficiency dentro de operations debe ser obligatorio");
      RuleFor(m => m.Crew).NotEmpty().WithMessage("El campo Crew dentro de operations debe ser obligatorio");
      RuleFor(m => m.MachineHs).NotEmpty().WithMessage("El campo MachineHs dentro de operations debe ser obligatorio");
      RuleFor(m => m.ManHs).NotEmpty().WithMessage("El campo ManHs dentro de operations debe ser obligatorio");
      RuleFor(m => m.From).NotEmpty().WithMessage("El campo From dentro de operations debe ser obligatorio");
      RuleFor(m => m.To).NotEmpty().WithMessage("El campo To dentro de operations debe ser obligatorio");
    }
  }
}
