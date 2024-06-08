using FluentValidation;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;

namespace SimaticArcorWebApi.Validators.WorkOrderCompletion
{
    public class WorkOrderCompletionValidator : AbstractValidator<WorkOrderCompletionModel>
    {
        public WorkOrderCompletionValidator()
        {
            //RuleFor(m => m.WoId).NotEmpty().WithMessage("El campo wOId es obligatorio");

        }
    }

}
