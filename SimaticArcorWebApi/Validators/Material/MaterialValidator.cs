using FluentValidation;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.Material;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimaticArcorWebApi.Model.Constants;

namespace SimaticArcorWebApi.Validators.Material
{
    public class MaterialValidator : AbstractValidator<MaterialRequest>
    {
        public MaterialValidator()
        {
            RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id es obligatorio");
            RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name es obligatorio");
            //RuleFor(m => m.TemplateNId).NotEmpty().WithMessage("El campo TemplateNId es obligatorio");
            RuleFor(m => m.Uom).NotEmpty().WithMessage("El campo Uom es obligatorio");

            //Rules Only to materials type with "producto terminado"
            RuleFor(m => m.Properties).Must(i => i.Any(p => p.Type != MaterialConstants.MaterialConstantsTypes.ProductoTerminado || p.Name == MaterialConstants.MaterialConstantsProperties.DUN14)).WithMessage("No existe la propiedad DUN14");
            RuleFor(m => m.Properties).Must(i => i.Any(p => p.Type != MaterialConstants.MaterialConstantsTypes.ProductoTerminado || p.Name == MaterialConstants.MaterialConstantsProperties.DUN14 && !string.IsNullOrWhiteSpace(p.Value) && p.Value.Length == 14)).WithMessage("La propiedad DUN14 debe tener un tamaño de 14 caracteres.");
            RuleForEach(m => m.Properties).SetValidator(new MaterialPropertyValidator());
        }

    }
}
