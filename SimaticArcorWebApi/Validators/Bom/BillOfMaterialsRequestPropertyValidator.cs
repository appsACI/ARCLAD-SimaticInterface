using FluentValidation;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Validators.Bom
{
  public class BillOfMaterialsRequestPropertyValidator : AbstractValidator<BillOfMaterialsRequestProperty>
  {
    public BillOfMaterialsRequestPropertyValidator()
    {
      RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name dentro de properties es obligatorio");
      RuleFor(m => m.Type).NotEmpty().WithMessage("El campo Type dentro de properties es obligatorio");
      //RuleFor(m => m.value).NotEmpty().WithMessage("El campo Value dentro de properties es obligatorio");
    }
  }
}
