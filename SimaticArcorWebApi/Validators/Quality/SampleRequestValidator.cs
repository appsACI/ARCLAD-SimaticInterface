using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using SimaticArcorWebApi.Model.Custom.Quality;

namespace SimaticArcorWebApi.Validators.Quality
{
    public class SampleRequestValidator : AbstractValidator<CreateSampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(m => m.Datetime).NotEmpty().WithMessage("El campo Datetime es obligatorio");
            RuleFor(m => m.Definition).NotEmpty().WithMessage("El campo Definition es obligatorio");
            //RuleFor(m => m.IdCarga).NotEmpty().WithMessage("El campo IdCarga es obligatorio");
            RuleFor(m => m.IdProtocol).NotEmpty().WithMessage("El campo IdProtocol es obligatorio");
            RuleFor(m => m.IdProtocol).Length(0,20).WithMessage("El campo IdProtocol no puede ser mayor a 20 caracteres.");
            RuleFor(m => m.Revision).NotEmpty().WithMessage("El campo Revision es obligatorio");
            //RuleFor(m => m.Lot).NotEmpty().WithMessage("El campo Lot es obligatorio");
            //RuleFor(m => m.IdPlant).NotEmpty().WithMessage("El campo IdPlant es obligatorio");
            RuleFor(m => m.Properties).NotEmpty().WithMessage("El campo Properties es obligatorio");
            RuleFor(m => m.SampleId).NotEmpty().WithMessage("El campo SampleId es obligatorio");
            RuleForEach(m => m.Properties).SetValidator(new SamplePropertyValidator());
        }
    }
}
