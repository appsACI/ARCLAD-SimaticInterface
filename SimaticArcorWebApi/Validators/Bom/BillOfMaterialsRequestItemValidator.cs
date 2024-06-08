using FluentValidation;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Validators.Bom
{
  public class BillOfMaterialsRequestItemValidator : AbstractValidator<BillOfMaterialsRequestItem>
  {
    public BillOfMaterialsRequestItemValidator()
    {
      RuleFor(m => m.MaterialId).NotEmpty().WithMessage("El campo MaterialId dentro de items debe ser obligatorio");
      RuleFor(m => m.Sequence).NotEmpty().WithMessage("El campo Sequence dentro de items debe ser obligatorio");
      RuleFor(m => m.QuantityValue).NotEmpty().WithMessage("El campo QuantityValue dentro de items debe ser obligatorio");
      RuleFor(m => m.UoMNId).NotEmpty().WithMessage("El campo UoMNId dentro de items debe ser obligatorio");
      //RuleFor(m => m.From).NotEmpty().WithMessage("El campo From dentro de items no puede estar vacio");
      //RuleFor(m => m.To).NotEmpty().WithMessage("El campo To dentro de items no puede estar vacio");
    }
  }
}
