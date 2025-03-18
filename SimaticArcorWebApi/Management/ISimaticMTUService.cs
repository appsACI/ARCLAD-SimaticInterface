using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Custom.PalletReception;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Modules.DCMovement;
using SimaticWebApi.Model.Custom.PrintLabel;
using SimaticWebApi.Model.Custom.RDL;

namespace SimaticArcorWebApi.Management
{
    public interface ISimaticMTUService
    {

        #region MTU
        Task<dynamic> GetMTUAsync(string nid, CancellationToken token);

        Task<dynamic> GetMTUSampleAsync(string nid, CancellationToken token);

        Task<dynamic> GetMTUAsigned(string nid, string opNid, CancellationToken token);

        Task<dynamic> GetMTUByMaterialLotIdAndEquipmentNIdAsync(string materialLotId, string equipmentNId, CancellationToken token);

        Task<dynamic> GetMTUPropertiesAsync(string id, CancellationToken token);

        Task<string> GetLastCreatedMTUByMaterialLotIdAndEquipmentNIdAsync(string id, string nId, double quantityValue, CancellationToken ct);

        Task<string> CreateMaterialTrackingUnitAsync(string id, string materialId, string materialDesc, string equipmentId, string uom, double quantity,
          MTURequestMaterialLotProperty[] propertiesRequest, CancellationToken token);

        Task CreateMaterialTrackingUnitProperties(string id, MTURequestMaterialLotProperty[] properties, CancellationToken token);

        Task<string> CreateMaterialTrackingUnitVinilosAsync(string id, string materialId, string materialDesc, string equipmentId, string uom, double quantity, string materialLotNId, CancellationToken token);

        Task SetMaterialTrackingUnitQuantity(string id, string uoMnId, double quantity, CancellationToken token);

        Task SetMaterialTrackingUnitStatus(string id, string verb, CancellationToken token);

        Task MoveMaterialTrackingUnitAsync(string id, double quantity, string equipment, CancellationToken token);

        Task MoveMaterialTrackingUnitToEquipmentAsync(string id, string equipment, CancellationToken token);

        Task UpdateMTUAsigned(string id, decimal quantity, CancellationToken token);

        Task UpdateMaterialTrackingUnitAsync(string id, string name, string description, string status, CancellationToken token);

        Task UpdateMaterialTrackingUnitProperties(string id, MTURequestMaterialLotProperty[] properties, CancellationToken token);

        Task UnassignMTURequired(string id, CancellationToken token);

        Task<dynamic> TrazabilidadMP(string LotId, string material, decimal quantity, string UoM, string user, DateTime time, string equipment, string WorkOrderNId, string WoOperationNId, string MtuNId, decimal MtuQuantity, string MtuMaterialNId, string MtuUoM, CancellationToken token);

        Task<dynamic> GetPropertiesDefectos(string Id, CancellationToken ct);

        Task<dynamic> GetPrinter(string equipmentNId, CancellationToken token);

        Task<dynamic> GetLotePadreProp(string NId, CancellationToken token);
        #endregion

        #region MaterialLot

        Task<dynamic> GetMaterialLot(string nid, CancellationToken token);

        Task<string> CreateMaterialLot(string id, string materialId, string materialDesc, string uom, double quantity, CancellationToken token);

        Task SetMaterialLotQuantity(string id, string uoMnId, double quantity, CancellationToken token);

        #endregion

        #region Print Labels
        Task<dynamic> PrintLabel(PrintModel req, TagModel[] tags, CancellationToken token);

        Task<dynamic> LabelHistory(string dateStart, string dateEnd, CancellationToken token);
        Task<dynamic> LabelHistoryTags(string historyId, CancellationToken token);
        #endregion

        Task<dynamic> GetPropertyWorkOrder(string Id, CancellationToken token);
        Task<dynamic> GetMaquinaDeCreacion(string Id, CancellationToken token);

        #region RDL

        Task<string> CreateRequirement(CreateRequirementModel req, CancellationToken token);
        Task<string> UpdateRequirement(CreateRequirementModel req, CancellationToken token);
        Task<string> CreateRequirementCorte(CreateRequirementModelCorte req, CancellationToken token);
        Task<string> UpdateRequirementLength(UpdateLengthModel req, CancellationToken token);
        Task<string> ConsultaRequirement(string Lote, string Nid, CancellationToken token);

        #endregion

        #region Other
        Task CreatePalletReception(CreatePalletReceptionRequest req, CancellationToken ct);

        Task DCMovement(DCMovementRequest req, CancellationToken ct);

        Task TriggerNewMaterialTrackingUnitEventAsync(string id, CancellationToken token);

        Task HandleLotFromJDE(MTURequest req, CancellationToken token);


        #endregion
    }
}
