using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.Equipment;
using System.Linq;

namespace SimaticArcorWebApi.Management
{
  public class EquipmentService : IEquipmentService
  {
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<EquipmentService> logger;

    public ISimaticEquipmentService SimaticEquipmentService { get; set; }

    //public ISimaticBOMService SimaticBOMService { get; set; }

    //public IUOMService UomService { get; set; }

    public EquipmentService(ISimaticEquipmentService simatic)
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<EquipmentService>();

      SimaticEquipmentService = simatic;
    }

    /// <summary>
    /// Process a Callback request data, saving PQR into equipment Automation Node variable.
    /// </summary>
    /// <param name="pqrNotif"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task ProcessNewPQRAsync(EquipmentPQRNotification pqrNotif, CancellationToken token)
    {
      logger.LogInformation($"Process new Equipment PQR Notification. EquipmentId [{pqrNotif.EquipmentId}] PQR [{pqrNotif.SupportCaseId}]");

      await SimaticEquipmentService.SetEquipmentPQRAsync(pqrNotif.EquipmentId, pqrNotif.SupportCaseId, false, token);

      logger.LogInformation($"Equipment '[{pqrNotif.EquipmentId}]' PQR has been set to '[{pqrNotif.SupportCaseId}]' succesfully.");
    }

    /// <summary>
    /// Handle Preventive Maintenance data sent from Netsuite (initializier).
    /// </summary>
    /// <param name="preventiveData"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task ProcessPreventiveMaitenanceDataAsync(EquipmentPreventiveMaintenaceNotification preventiveData, CancellationToken token)
    {
      logger.LogInformation($"Handling Equipment [{preventiveData.EquipmentNId}] Preventive Maintenace Notification.");

      // Check Equipment properties
      List<EquipmentConfigurationPropertyType> equConfigurationProps = new List<EquipmentConfigurationPropertyType>() { };

      equConfigurationProps.Add(new EquipmentConfigurationPropertyType { NId = "Mtto OrderID", PropertyType = "String", PropertyValue = "", Operational = true });
      equConfigurationProps.Add(new EquipmentConfigurationPropertyType { NId = "Mtto Fecha Inicio", PropertyType = "Datetime", PropertyValue = "", Operational = true });
      equConfigurationProps.Add(new EquipmentConfigurationPropertyType { NId = "Mtto Fecha Fin", PropertyType = "Datetime", PropertyValue = "", Operational = true });
      equConfigurationProps.Add(new EquipmentConfigurationPropertyType { NId = "Mtto Comentarios", PropertyType = "String", PropertyValue = "", Operational = true });
      equConfigurationProps.Add(new EquipmentConfigurationPropertyType { NId = "Mtto Status", PropertyType = "String", PropertyValue = "", Operational = true });
      equConfigurationProps.Add(new EquipmentConfigurationPropertyType { NId = "Mtto Tipo", PropertyType = "String", PropertyValue = "", Operational = true });

      List<EquipmentConfigurationPropertyType> equPropsToAdd = new List<EquipmentConfigurationPropertyType>() { };
      List<EquipmentPropertyType> equPropsToUpdate = new List<EquipmentPropertyType>() { };
      EquipmentPropertyType equProp = new EquipmentPropertyType { };
      EquipmentConfigurationProperty equConfigurationProperty = new EquipmentConfigurationProperty();
      EquipmentProperty equProperty = new EquipmentProperty();

      #region -- Check Equipment actual ConfigurationProperties, add if necessary --

      IList<EquipmentConfigurationProperty> equConfigData = await SimaticEquipmentService.GetEquipmentConfigurationPropertyDataAsync(preventiveData.EquipmentNId, true, token);

      foreach (EquipmentConfigurationPropertyType p in equConfigurationProps)
      {
        equConfigurationProperty = equConfigData.Where(ep => ep.NId.Equals(p.NId)).FirstOrDefault();

        if (equConfigurationProperty == null)
        {
          logger.LogDebug($"Equipment [{preventiveData.EquipmentNId}] :: will Add Configuration property [{p.NId}].");
          equPropsToAdd.Add(p);
        }
      }

      if (equPropsToAdd.Count() > 0)
      {
        logger.LogDebug($"Equipment [{preventiveData.EquipmentNId}] :: will Add {equPropsToAdd.Count()} properties.");

        var ret = await SimaticEquipmentService.AddEquipmentConfigurationPropertiesAsync(equConfigData.FirstOrDefault().EquipmentConfiguration_Id, equPropsToAdd, token);

        logger.LogInformation($"Equipment [{preventiveData.EquipmentNId}] :: Properties added successfully.");
      }

      #endregion

      #region -- Update Equipment Properties --

      Equipment equData = await SimaticEquipmentService.GetEquipmentDataAsync(preventiveData.EquipmentNId, true, token);

      foreach (EquipmentConfigurationPropertyType p in equConfigurationProps)
      {
        equProperty = equData.EquipmentProperties.Where(ep => ep.NId.Equals(p.NId)).FirstOrDefault();

        if (equProperty != null)
        {
          logger.LogDebug($"Equipment [{preventiveData.EquipmentNId}] Preventive handling :: will Update property [{p.NId}].");

          switch (p.NId.Trim())
          {
            case "Mtto OrderID":
              equProp = new EquipmentPropertyType { EquipmentPropertyId = equProperty.Id, EquipmentPropertyValue = preventiveData.OrderId.ToString() };
              break;
            case "Mtto Fecha Inicio":
              equProp = new EquipmentPropertyType { EquipmentPropertyId = equProperty.Id, EquipmentPropertyValue = preventiveData.StartTime.ToString("yyyy-MM-dd HH:mm:ss") };
              break;
            case "Mtto Fecha Fin":
              equProp = new EquipmentPropertyType { EquipmentPropertyId = equProperty.Id, EquipmentPropertyValue = preventiveData.EndTime.ToString("yyyy-MM-dd HH:mm:ss") };
              break;
            case "Mtto Comentarios":
              equProp = new EquipmentPropertyType { EquipmentPropertyId = equProperty.Id, EquipmentPropertyValue = preventiveData.Comments.ToString() };
              break;
            case "Mtto Status":
              equProp = new EquipmentPropertyType { EquipmentPropertyId = equProperty.Id, EquipmentPropertyValue = preventiveData.Status.ToString() };
              break;
            case "Mtto Tipo":
              equProp = new EquipmentPropertyType { EquipmentPropertyId = equProperty.Id, EquipmentPropertyValue = preventiveData.Tipo.ToString() };
              break;
            default:
              logger.LogWarning($"Equipment [{preventiveData.EquipmentNId}] property [{p.NId}] is not handled!");
              continue;
          }

          equPropsToUpdate.Add(equProp);
        }
        else
        {
          logger.LogError($"Equipment [{preventiveData.EquipmentNId}] :: property [{p.NId}] not found @ Equipment, and will not be updated.");
        }
      }

      if (equPropsToUpdate.Count() > 0)
      {
        logger.LogDebug($"Equipment [{preventiveData.EquipmentNId}] :: will Update {equPropsToUpdate.Count()} properties.");

        foreach (EquipmentPropertyType p in equPropsToUpdate)
        {
          var ret = await SimaticEquipmentService.SetEquipmentPropertyValuesAsync(p.EquipmentPropertyId, p.EquipmentPropertyValue, token);
        }

        logger.LogDebug($"Equipment [{preventiveData.EquipmentNId}] :: Properties updated successfully.");
      }

      #endregion

    }
  }
}
