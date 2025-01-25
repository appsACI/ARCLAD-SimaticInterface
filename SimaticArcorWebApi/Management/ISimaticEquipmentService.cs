using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.Equipment;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimaticArcorWebApi.Management
{
  public interface ISimaticEquipmentService
  {
    Task<dynamic> SetEquipmentPQRAsync(string equId, string pqrNumber, bool throwException, CancellationToken token);

    Task<Equipment> GetEquipmentDataAsync(string equipmentNId, bool throwException, CancellationToken token);

    Task<IList<EquipmentConfigurationProperty>> GetEquipmentConfigurationPropertyDataAsync(string equipmentNId, bool throwException, CancellationToken token);

    [Obsolete]
    Task<dynamic> GetEquipmentByPropertyValueAsync(string propertyValue, string propertyName, bool throwException, CancellationToken token);

    Task<dynamic> AddEquipmentConfigurationPropertiesAsync(Guid equConfigId, List<EquipmentConfigurationPropertyType> equProps, CancellationToken token);

    Task<dynamic> SetEquipmentPropertyValuesAsync(Guid equPropId, string qeuPropValue, CancellationToken token);
  }
}
