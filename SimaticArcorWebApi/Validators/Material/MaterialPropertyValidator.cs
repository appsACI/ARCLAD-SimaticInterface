using FluentValidation;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.Material;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Validators.Material
{
    public class MaterialPropertyValidator : AbstractValidator<MaterialRequestProperty>
    {
        public MaterialPropertyValidator()
        {
            RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name en las properties debe ser obligatorio");
            RuleFor(m => m.Type).NotEmpty().WithMessage("El campo Type en las properties debe ser obligatorio");
            RuleFor(m => m.Uom).NotEmpty().WithMessage("El campo Uom en las properties debe ser obligatorio");
            //RuleFor(m => m.Value).NotEmpty().WithMessage("El campo Value en las properties debe ser obligatorio");
        }

    }
}
