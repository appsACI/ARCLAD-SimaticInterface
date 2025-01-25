using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using SimaticArcorWebApi.Model.Custom.Quality;

namespace SimaticArcorWebApi.Validators.Quality
{
    public class ProductSpecificationRequestValidator : AbstractValidator<CreateProductSpecificationRequest>
    {
        public ProductSpecificationRequestValidator()
        {
            //RuleFor(m => m.Frequency).NotEmpty().WithMessage("El campo Frequency es obligatorio");
            RuleFor(m => m.Definition).NotEmpty().WithMessage("El campo Definition es obligatorio");
            RuleFor(m => m.IdProtocol).NotEmpty().WithMessage("El campo IdProtocol es obligatorio");
            RuleFor(m => m.Revision).NotEmpty().WithMessage("El campo Revision es obligatorio");
            RuleFor(m => m.IdProtocol).Length(0, 20).WithMessage("El campo IdProtocol no puede ser mayor a 20 caracteres");
            RuleFor(m => m.Properties).NotEmpty().WithMessage("El campo Properties es obligatorio");
            //RuleFor(m => m.UoMF).Must(str => new List<string>() { "M", "S", "H" }.Contains(str)).WithMessage("El campo UoMF debe tener los valores M, S ó H");
            RuleForEach(m => m.Properties).SetValidator(new ProductSpecificationPropertyValidator());
            //RuleFor(m => m.IdPlant).NotEmpty().WithMessage("El campo IdPlant es obligatorio");
            //RuleFor(m => m.Sample).NotEmpty().WithMessage("El campo Sample es obligatorio");
            //RuleFor(m => m.UoMF).NotEmpty().WithMessage("El campo UoMF es obligatorio");
            //RuleFor(m => m.WC).NotEmpty().WithMessage("El campo WC es obligatorio");
        }
    }
}
