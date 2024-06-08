using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;

namespace SimaticArcorWebApi.Management
{
  public interface IEquipmentService
  {
    Task ProcessNewPQRAsync(EquipmentPQRNotification equipmentPQR, CancellationToken token);

    Task ProcessPreventiveMaitenanceDataAsync(EquipmentPreventiveMaintenaceNotification preventiveData, CancellationToken token);
  }
}
