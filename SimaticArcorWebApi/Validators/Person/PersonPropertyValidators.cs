using SimaticArcorWebApi.Model.Custom.Person;
using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace SimaticArcorWebApi.Validators.Person
{
    public class PersonPropertyValidator : AbstractValidator<CreatePersonRequest>
    {
        public PersonPropertyValidator()
        {
            RuleFor(m => m.plantID).NotEmpty().WithMessage("El campo PlantID es obligatorio");
            RuleFor(m => m.startTime).NotEmpty().WithMessage("El campo StartTime es obligatorio");
            RuleFor(m => m.personnel).NotEmpty().WithMessage("El campo PersonnelFile es obligatorio");
        }
    }
}
