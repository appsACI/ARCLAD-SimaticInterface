using FluentValidation;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Validators.Bom
{
  public class BillOfMaterialsRequestValidator : AbstractValidator<BillOfMaterialsRequest>
  {
    public BillOfMaterialsRequestValidator()
    {
      RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id es obligatorio");
      RuleFor(m => m.MaterialId).NotEmpty().WithMessage("El campo MaterialId es obligatorio");
      RuleFor(m => m.QuantityValue).NotEmpty().WithMessage("El campo QuantityValue es obligatorio");
      RuleFor(m => m.UoMNId).NotEmpty().WithMessage("El campo UoMNId es obligatorio");
      RuleFor(m => m.Items).NotEmpty().WithMessage("El campo Items es obligatorio");
      RuleFor(m => m.Items.Length).GreaterThan(0).WithMessage("La cantidad de items debe ser mayor a cero");
      RuleForEach(m => m.Items).SetValidator(new BillOfMaterialsRequestItemValidator());
      RuleForEach(m => m.Properties).SetValidator(new BillOfMaterialsRequestPropertyValidator()).When(m => m.Properties.Length > 0);
    }
  }
}
