using FluentValidation;
using SimaticArcorWebApi.Model.Custom;

namespace SimaticArcorWebApi.Validators.Equipment
{
  public class EquipmentPQRValidator : AbstractValidator<EquipmentPQRNotification>
  {
    public EquipmentPQRValidator()
    {
      RuleFor(m => m.EquipmentId).NotEmpty().WithMessage("El campo EquipmentId es obligatorio");
      RuleFor(m => m.SupportCaseId).NotEmpty().WithMessage("El campo SupportCaseId es obligatorio");
    }
  }

  public class EquipmentPreventiveNotificationValidator : AbstractValidator<EquipmentPreventiveMaintenaceNotification>
  {
    public EquipmentPreventiveNotificationValidator()
    {
      RuleFor(m => m.EquipmentNId).NotEmpty().WithMessage("El campo EquipmentNId es obligatorio");
      RuleFor(m => m.OrderId).NotEmpty().WithMessage("El campo OrderId es obligatorio");
      RuleFor(m => m.Tipo).NotEmpty().WithMessage("El campo Tipo es obligatorio");
      RuleFor(m => m.StartTime).NotEmpty().WithMessage("El campo StartTime es obligatorio");
      RuleFor(m => m.EndTime).NotEmpty().WithMessage("El campo EndTime es obligatorio");
      RuleFor(m => m.Status).NotEmpty().WithMessage("El campo Status es obligatorio");
      RuleFor(m => m.Comments).NotEmpty().WithMessage("El campo Comments es obligatorio");
    }
  }
}
