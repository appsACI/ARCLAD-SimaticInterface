using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using SimaticArcorWebApi.Model.Custom.Quality;

namespace SimaticArcorWebApi.Validators.Quality
{
    public class SamplePropertyValidator : AbstractValidator<SampleRequestProperty>
    {
        public SamplePropertyValidator()
        {
            RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name en las properties debe ser obligatorio");
            RuleFor(m => m.Name).Length(1, 20).WithMessage("El campo Name no puede ser mayor a 20 caracteres");
            //RuleFor(m => m.Type).NotEmpty().WithMessage("El campo Type en las properties debe ser obligatorio");
            //RuleFor(m => m.UoM).NotEmpty().WithMessage("El campo UoM en las properties debe ser obligatorio");
            //RuleFor(m => m.Value).NotEmpty().WithMessage("El campo Value en las properties debe ser obligatorio");
        }
    }
}
