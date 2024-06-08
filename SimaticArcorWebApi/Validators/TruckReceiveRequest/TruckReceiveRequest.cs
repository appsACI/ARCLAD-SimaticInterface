using System;
using System.Collections.Generic;
using System.Text;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using FluentValidation; 

namespace SimaticArcorWebApi.Validators.TruckReceiveRequestValidator
{
  public class TruckReceiveValidator: AbstractValidator <TruckReceiveRequest>
  {
    public TruckReceiveValidator()
    {
      //RuleFor(m => m.Material).Length(10).WithMessage("El codigo de material es incorrecto");
      RuleFor(m => m.Material).NotEmpty().WithMessage("El codigo del material es obligatorio");
      RuleFor(m => m.Lot).NotEmpty().WithMessage("El Lote del material es obligatorio");

    }
    }
  }

