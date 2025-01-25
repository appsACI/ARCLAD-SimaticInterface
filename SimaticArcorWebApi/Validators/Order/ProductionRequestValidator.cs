using FluentValidation;
using SimaticArcorWebApi.Model.Custom;

namespace SimaticArcorWebApi.Validators.Order
{
    public class ProductionRequestValidator : AbstractValidator<ProductionRequest>
    {
        public ProductionRequestValidator()
        {
            RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id es obligatorio");
            //RuleFor(m => m.Order).NotEmpty().WithMessage("El campo Order es obligatorio");
            RuleFor(m => m.WorkOrder).NotEmpty().WithMessage("El campo WorkOrder es obligatorio");
            RuleFor(m => m.OrderType).NotEmpty().WithMessage("El campo OrderType es obligatorio");
            RuleFor(m => m.Location).NotEmpty().WithMessage("El campo Location es obligatorio");
            RuleFor(m => m.Quantity).GreaterThanOrEqualTo(0).WithMessage("El campo Quantity debe ser mayor o igual a zero");
            RuleFor(m => m.AssemblyItem).NotEmpty().WithMessage("El campo AssemblyItem es obligatorio");
            RuleFor(m => m.BomId).NotEmpty().WithMessage("El campo BomId es obligatorio");
            //RuleFor(m => m.Department).NotEmpty().WithMessage("El campo Department es obligatorio");
            RuleFor(m => m.ManufacturingRouting).NotEmpty().WithMessage("El campo ManufacturingRouting es obligatorio");
            RuleFor(m => m.StartTime).NotEmpty().WithMessage("El campo StartTime es obligatorio");
            RuleFor(m => m.EndTime).NotEmpty().WithMessage("El campo EndTime es obligatorio");
            RuleFor(m => m.Status).NotEmpty().WithMessage("El campo Status es obligatorio");
            //RuleFor(m => m.ARObservaciones).NotEmpty().WithMessage("El campo ARObservaciones es obligatorio");
            //RuleFor(m => m.Customer).NotEmpty().WithMessage("El campo Customer es obligatorio");
            //RuleFor(m => m.OrdenVenta).NotEmpty().WithMessage("El campo OrdenVenta es obligatorio");
            RuleFor(m => m.Operations.Length).GreaterThan(0).WithMessage("Debe haber una o mas Operations para la WorkOrder");
            RuleForEach(m => m.Operations).SetValidator(new ProductionRequestOperationsValidator());
            //RuleForEach(m => m.Parameters).SetValidator(new ProductionRequestParametersValidator()).When(a => a.Parameters.Length > 0);
        }
    }

    /// <summary>
    /// Validator for WorkOrder Parameters, if any.
    /// </summary>
    public class ProductionRequestParametersValidator : AbstractValidator<ProductionRequestParameters>
    {
        public ProductionRequestParametersValidator()
        {
            RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name para cada Parameter de la WorkOrder es obligatorio");
        }
    }

    /// <summary>
    /// Validator for WorkOrder Operation.
    /// </summary>
    public class ProductionRequestOperationsValidator : AbstractValidator<ProductionRequestOperations>
    {
        public ProductionRequestOperationsValidator()
        {
            RuleFor(m => m.Id).NotEmpty().WithMessage("El campo Id de la Operation es obligatorio");
            RuleFor(m => m.Sequence).NotEmpty().WithMessage("El campo Sequence de la Operation es obligatorio");
            RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name de la Operation es obligatorio");
            RuleFor(m => m.Asset).NotEmpty().WithMessage("El campo Asset de la Operation es obligatorio");
            RuleFor(m => m.StartTime).NotEmpty().WithMessage("El campo StartTime es obligatorio");
            RuleFor(m => m.EndTime).NotEmpty().WithMessage("El campo EndTime es obligatorio");
            RuleForEach(m => m.AlternativeAssets).SetValidator(new ProductionRequestOperationAlternativeAssetsValidator()).When(a => a.AlternativeAssets != null && a.AlternativeAssets.Length > 0);
            RuleForEach(m => m.Parameters).SetValidator(new ProductionRequestOperationParametersValidator()).When(a => a.Parameters.Length > 0);
            //RuleForEach(m => m.Trims_SO_Customer).SetValidator(new ProductionRequestOperationTrimValidator()).When(a => a.Trims_SO_Customer != null && a.Trims_SO_Customer.Length > 0);
        }
    }

    /// <summary>
    /// Validator for WorkOrder Operation Alternative Asset (Equipments) if any.
    /// </summary>
    public class ProductionRequestOperationAlternativeAssetsValidator : AbstractValidator<ProductionRequestOperationAlternativeAssets>
    {
        public ProductionRequestOperationAlternativeAssetsValidator()
        {
            RuleFor(m => m.Code).NotEmpty().WithMessage("El campo Code para cada AlternativeAssets es obligatorio");
        }
    }

    /// <summary>
    /// Validator for WorkOrder Operation Parameters, if any.
    /// </summary>
    public class ProductionRequestOperationParametersValidator : AbstractValidator<ProductionRequestOperationParameters>
    {
        public ProductionRequestOperationParametersValidator()
        {
            RuleFor(m => m.Name).NotEmpty().WithMessage("El campo Name para cada Parameter de la WorkOrder Operation es obligatorio");
        }
    }

    /// <summary>
    /// Validator for WorkOrder Operation Trims, if any.
    /// </summary>
    //public class ProductionRequestOperationTrimValidator : AbstractValidator<ProductionRequestOperationTrims>
    //{
    //  public ProductionRequestOperationTrimValidator()
    //  {
    //    RuleFor(m => m.Customer).NotEmpty().WithMessage("El campo Customer del Customer Trim no puede estar vacio");
    //    RuleFor(m => m.Trim).NotEmpty().WithMessage("El campo Trim del Customer Trim no puede estar vacio");
    //  }
    //}

}
