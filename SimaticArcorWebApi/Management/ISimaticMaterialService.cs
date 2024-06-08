using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Simatic.MTU;

namespace SimaticArcorWebApi.Management
{
    public interface ISimaticMaterialService
    {
        Task<dynamic> GetAllMaterialTemplateAsync(CancellationToken token);

        Task<dynamic> GetAllMaterialGroupAsync(CancellationToken token);

        Task<dynamic> GetAllMaterialAsync(CancellationToken token);

        Task<string> CreateMaterialAsync(string id, string name, string description, string uom, string templateNId, string revision, CancellationToken token);

        Task<MaterialProperties[]> GetMaterialPropertyForMaterialAsync(string id, CancellationToken token);

        Task CreateMaterialProperties(string id, MaterialRequestProperty[] properties, CancellationToken token);

        Task UpdateMaterialProperties(string id, MaterialRequestProperty[] properties, CancellationToken token);

        Task DeleteMaterialAsync(string id, CancellationToken token);

        Task SetMaterialRevisionCurrentAsync(string id, CancellationToken token);

        Task UnSetMaterialRevisionCurrentAsync(string id, CancellationToken token);

        Task<dynamic> GetMaterialByNIdAsync(string id, bool checkCurrent, bool throwException, CancellationToken token);

        Task<dynamic> GetAllMaterialGroupByIdAsync(string id, CancellationToken token);

        Task UpdateMaterial(string newMatId, string name, string description, string uom, CancellationToken token);

        Task<dynamic> GetAllMaterialTemplateByIdAsync(string id, CancellationToken token);

        Task<string> CreateNewMaterialRevision(string matId, string sourceRevision, string revision, CancellationToken token);

        Task<dynamic> GetMaterialTemplateByNIdAsync(string id, bool throwException, CancellationToken token);

        Task<string> CreateMaterialTemplateAsync(string id, string name, string description, string uom, IList<PropertyParameterTypeCreate> properties, CancellationToken token);
    }
}
