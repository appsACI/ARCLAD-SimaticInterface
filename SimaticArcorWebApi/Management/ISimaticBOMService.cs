using System;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom.Bom;
using SimaticArcorWebApi.Model.Simatic.BOM;

namespace SimaticArcorWebApi.Management
{
  public interface ISimaticBOMService
  {
    Task<dynamic> GetAllBillOfMaterialsAsync(CancellationToken ct);

    Task<dynamic> GetBillOfMaterialsByMaterialNIdAsync(string nid, CancellationToken ct);

    Task<dynamic> GetBillOfMaterialsByNIdAsync(string id, bool checkCurrent, bool throwException, CancellationToken token);

    Task<string> CreateNewBillOfMaterialsRevision(string matId, string sourceRevision, string revision, CancellationToken token);

    Task<dynamic> CreateBillOfMaterialsAsync(string id, string name, string description, Model.Simatic.Material.Material material, string uoMNId, double quantityValue, CancellationToken token);
    
    Task DeleteBillOfMaterialsAsync(string id, CancellationToken token);

    Task SetBillOfMaterialsRevisionCurrentAsync(string id, CancellationToken token);

    Task SetBillOfMaterialsTemplateAsDefaultAsync(Guid id, CancellationToken token);

    Task UnsetBillOfMaterialsRevisionCurrentAsync(string id, CancellationToken token);

    Task<dynamic> GetBillOfMaterialsUserFields(string id, CancellationToken token);

    Task UpdateBillOfMaterialsUserFieldAsync(string id, string value, CancellationToken token);
    
    //Task UpdateBillOfMaterials(string id, Model.Simatic.Material.Material material, string uom, double quantity, CancellationToken token);

    Task SetValidationOnBillOfMaterialsItem(string bomId, string nId, string uom, double quantity, int sequence, double? scrap, DateTime from, DateTime to, CancellationToken token);

    Task UpdateOrCreateBillOfMaterials(string id, Model.Simatic.Material.Material material, string uom, double quantity, BillOfMaterialsRequestItem[] newItems, BillOfMaterialsRequestProperty[] newProps, CancellationToken token);

    Task UpdateBillOfMaterialsItem(string id, string description, string name, int sequence, string materialNId, string uom, double quantity, CancellationToken token);

    Task CreateBillOfMaterialsDefaultTemplateAsync(string templateNId, CancellationToken token);

    Task<dynamic> GetBillOfMaterialsDefaultTemplateAsync(string templateNId, CancellationToken token);

    Task<dynamic> GetBillOfMaterialsDefaultTemplatePropertiesAsync(string templateNId, CancellationToken token);

    Task<dynamic> GetBillOfMaterialsExtended(string nId, string revision, CancellationToken token);

    Task<dynamic> GetBillOfMaterialsExtended(string bomId, CancellationToken token);

    Task CreateBillOfMaterialsExtended(string bomId, string matId, string revision, CancellationToken token);

    Task RemoveBillOfMaterialsExtended(string bomId, CancellationToken token);

    Task<dynamic> GetBillOfMaterialsItemsByIdAsync(string bomId, CancellationToken token);

    Task DeleteBillOfMaterialsItem(string id, CancellationToken token);

    Task<double> ConvertQuantityByUoM(string materialId, double sourceQuantity, string sourceUom, CancellationToken token);

    Task AddUomConversion(string materialNId, string destinationUom, double? factor, CancellationToken token);

    Task<dynamic> GetUomConversion(string materialId, string destinationUom, bool throwException, CancellationToken token);

    Task UpdateUomConversion(string id, string destinationUom, double? factor, CancellationToken token);

    Task UpdateBillOfMaterialsTemplateAsync(Guid templateId, UserFieldParamterType[] properties, CancellationToken token);

  }
}
