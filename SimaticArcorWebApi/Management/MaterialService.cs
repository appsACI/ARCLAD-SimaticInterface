using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Constants;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;

namespace SimaticArcorWebApi.Management
{
    public class MaterialService : IMaterialService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<MaterialService> logger;

        private MaterialConfig config;

        public ISimaticMaterialService SimaticMaterialService { get; set; }

        public ISimaticBOMService SimaticBOMService { get; set; }

        public IUOMService UomService { get; set; }

        public MaterialService(IConfiguration configuration, ISimaticMaterialService simatic, ISimaticBOMService simaticBomService, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<MaterialService>();

            config = new MaterialConfig();
            configuration.GetSection("MaterialConfig").Bind(config);

            SimaticMaterialService = simatic;

            SimaticBOMService = simaticBomService;

            UomService = uomService;
        }

        public async Task ProcessNewMaterialAsync(MaterialRequest mat, CancellationToken token)
        {
            logger.LogInformation($"Process new material. Id [{mat.Id}] Name [{mat.Name}]");

            //ConvertStatusFieldIntoProperty(mat);

            TransformUOM(mat.Properties);
            mat.TemplateNId = "Default"; // TEMP
            bool disableMaterial = false;
            bool disableRelatedBoMs = false;

            await GetOrCreateMaterialTemplate(mat.TemplateNId, mat.Uom, token);

            CheckAndFixPropertyTypes(mat.Properties);

            string evalPropertyName = "AR ESTADO DE INGENIERIA";
            var propState = mat.Properties.Where(p => p.Name.ToUpper().Equals(evalPropertyName)).FirstOrDefault();
            if (propState != null)
            {
                if (propState.Value.Equals("5"))
                {
                    logger.LogInformation($"Material [{mat.Id}] will be disabled, as it's status is: {propState.Value}");
                    disableMaterial = true;
                    disableRelatedBoMs = true;
                }
            }
            else
            {
                logger.LogDebug($"Material [{mat.Id}] engineering status is not specifyed.");
            }

            Material revMat = await SimaticMaterialService.GetMaterialByNIdAsync(mat.Id, true, false, token);
            Material noRevMat = await SimaticMaterialService.GetMaterialByNIdAsync(mat.Id, false, false, token);

            var currentMat = revMat ?? noRevMat;
            if (currentMat != null)
            {
                string updateMatId = currentMat.Id;

                logger.LogInformation($"Material Found. Id [{currentMat.Id}] Name [{currentMat.Name}] Revision [{currentMat.Revision}]");

                if (currentMat.Name != mat.Name || currentMat.Description != mat.Description || currentMat.UoMNId != UomService.GetSimaticUOM(mat.Uom) ||
                    currentMat.TemplateNId.ToLower() != mat.TemplateNId.ToLower())
                {
                    if (!disableMaterial && currentMat.IsCurrent && config.EnableCreateNewRevision)
                    {
                        logger.LogInformation($"New Revision for material");
                        var newRev = await GetNewRevision(revMat, noRevMat);

                        var newMatId = await SimaticMaterialService.CreateNewMaterialRevision(mat.Id, currentMat.Revision, newRev, token);
                        await SimaticMaterialService.UnSetMaterialRevisionCurrentAsync(currentMat.Id, token);

                        await AlignBomMaterialLink(currentMat, newRev, token);

                        updateMatId = newMatId;
                    }

                    await SimaticMaterialService.UpdateMaterial(updateMatId, mat.Name, mat.Description, mat.Uom, token);
                }

                // Handle IsCurrent status
                if (disableMaterial)
                {
                    await SimaticMaterialService.UnSetMaterialRevisionCurrentAsync(updateMatId, token);
                    logger.LogInformation($"Unsetting as current material [{currentMat.NId}] - [{currentMat.Name}] succeeded.");


                }
                else if (!currentMat.IsCurrent)
                {
                    await SimaticMaterialService.SetMaterialRevisionCurrentAsync(updateMatId, token);
                    logger.LogInformation($"Setting as current material [{currentMat.NId}] - [{currentMat.Name}] succeeded.");



                }

                if (disableRelatedBoMs)
                {
                    logger.LogInformation($"Unsetting ALL related BoMs for the material [{currentMat.NId}] - [{currentMat.Name}] as current.");

                    var relatedBoMs = await SimaticBOMService.GetBillOfMaterialsByMaterialNIdAsync(currentMat.NId, token);

                    if (relatedBoMs != null)
                    {
                        foreach (var b in relatedBoMs)
                        {
                            await SimaticBOMService.UnsetBillOfMaterialsRevisionCurrentAsync(b.Id, token);
                            logger.LogDebug($"Unsetting as current BoM '{b.NId}' for the material [{currentMat.NId}] - [{currentMat.Name}] succeeded.");
                        }
                    }
                }

                logger.LogInformation($"Material has been updated.");
                logger.LogInformation($"envio a rdl del nuevo material [{mat.Id}]");
                await CreateOrUpdateMaterialProperties(updateMatId, mat.Properties, token);

                Material materialRdl1 = await SimaticMaterialService.GetMaterialByNIdAsync(mat.Id, false, false, token);
                var rdl1 = CreateRDLSpecification(mat.Id, materialRdl1.IsCurrent, token);






            }
            else
            {
                logger.LogInformation($"Creating new material");
                await CreateMaterialAsync(mat, disableMaterial, token);

                logger.LogInformation($"envio a rdl del actualizar material [{mat.Id}]");
                Material materialRdl4 = await SimaticMaterialService.GetMaterialByNIdAsync(mat.Id, false, false, token);

                var rdl2 = CreateRDLSpecification(mat.Id, materialRdl4.IsCurrent, token);


                logger.LogInformation($"Material has been created.");


            }
        }

        private void ConvertStatusFieldIntoProperty(MaterialRequest mat)
        {
            mat.Properties = mat.Properties.Append(new MaterialRequestProperty()
            {
                Name = nameof(mat.Status),
                Value = mat.Status,
                Uom = "n/a",
                Type = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            }).ToArray();

            mat.Properties = mat.Properties.Append(new MaterialRequestProperty()
            {
                Name = nameof(mat.InitialLotStatus),
                Value = mat.InitialLotStatus,
                Uom = "n/a",
                Type = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            }).ToArray();
        }

        private async Task GetOrCreateMaterialTemplate(string nid, string uom, CancellationToken ct)
        {
            logger.LogInformation($"Check Material template. Nid [{nid}]");
            MaterialTemplate template = await SimaticMaterialService.GetMaterialTemplateByNIdAsync(nid, false, ct);

            if (template != null)
            {
                logger.LogInformation("Material template found");
                return;
            }

            logger.LogInformation("Material template not found, creating one...");
            //await SimaticMaterialService.CreateMaterialTemplateAsync(nid, nid, nid, uom, GetTemplateDefaultProperties(), ct);
            await SimaticMaterialService.CreateMaterialTemplateAsync(nid, nid, nid, uom, null, ct);

            logger.LogInformation($"Material template [{nid}] has been created");
        }

        private IList<PropertyParameterTypeCreate> GetTemplateDefaultProperties()
        {
            var properties = new List<PropertyParameterTypeCreate>();

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = "BultosPorPalet",
            //  PropertyType = "Int" //typeof(int).Name.First().ToString().ToUpper() + typeof(int).Name.Substring(1)
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.DUN14,
            //  PropertyType = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.VidaUtil,
            //  PropertyType = "Int"
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.Familia,
            //  PropertyType = "Int"
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.TipoProducto,
            //  PropertyType = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.Mercado,
            //  PropertyType = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.Status,
            //  PropertyType = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            //});

            //properties.Add(new PropertyParameterTypeCreate()
            //{
            //  PropertyNId = MaterialConstants.MaterialConstantsProperties.InitialLotStatus,
            //  PropertyType = typeof(string).Name.First().ToString().ToUpper() + typeof(string).Name.Substring(1)
            //});

            return properties;
        }

        private async Task AlignBomMaterialLink(Material mat, string newRev, CancellationToken token)
        {
            var boms = (IList<BillOfMaterialsExtended>)await SimaticBOMService.GetBillOfMaterialsExtended(mat.NId,
              mat.Revision, token);
            foreach (var bom in boms)
            {
                await SimaticBOMService.RemoveBillOfMaterialsExtended(bom.BillOfMaterials_Id, token);

                await SimaticBOMService.CreateBillOfMaterialsExtended(bom.BillOfMaterials_Id, mat.NId, newRev, token);
            }
        }

        /// <summary>
        /// Get new revision number
        /// Format number correspond to 3 levels Version.Patch.Minor
        /// 
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<string> GetNewRevision(Material rev, Material noRev)
        {
            var newRev = "1.0.99";

            var revVer = rev.Revision.Split('.').Select(t => Convert.ToInt32(t)).ToList();

            var nrevVer = noRev.Revision.Split('.').Select(t => Convert.ToInt32(t)).ToList();

            if (revVer.Count != 3 || nrevVer.Count != 3)
                return newRev;

            if (revVer[0] >= nrevVer[0])
            {
                if (revVer[1] >= nrevVer[1])
                {
                    revVer[1] += 1;
                    revVer[2] = 0;
                }
                else
                {
                    revVer[2] += 1;
                }
            }
            else
            {
                revVer[0] = nrevVer[0];
                revVer[1] = nrevVer[1] + 1;
                revVer[2] = 0;
            }
            return string.Join('.', revVer);
        }

        private void TransformUOM(MaterialRequestProperty[] properties)
        {
            foreach (var prop in properties)
            {
                if (!prop.Uom.IsNA())
                {
                    prop.Type = "Quantity";
                }
            }
        }

        private void CheckAndFixPropertyTypes(MaterialRequestProperty[] properties)
        {
            foreach (var prop in properties)
            {
                prop.Type = prop.Type.First().ToString().ToUpper() + prop.Type.Substring(1);
            }
        }

        private int GetNumbersFromString(string input)
        {
            // Match only digits 
            string pattern = @"\d";

            StringBuilder sb = new StringBuilder();

            foreach (Match m in Regex.Matches(input, pattern))
            {
                sb.Append(m);
            }

            return Convert.ToInt32(sb.ToString());
        }

        public async Task CreateMaterialAsync(MaterialRequest mat, bool disable, CancellationToken token)
        {
            logger.LogInformation($"Create material with properties. Id [{mat.Id}] Name [{mat.Name}]");

            var mat_Rev = "1";
            //mat.Revision 

            var matId = await SimaticMaterialService.CreateMaterialAsync(mat.Id, mat.Name, mat.Description, mat.Uom, mat.TemplateNId, mat_Rev, token);

            logger.LogInformation($"Material Created. ID [{matId}]");
            if (!disable)
            {
                logger.LogInformation($"Setting material as current.");
                await SimaticMaterialService.SetMaterialRevisionCurrentAsync(matId, token);


            }

            await CreateOrUpdateMaterialProperties(matId, mat.Properties, token);

            logger.LogInformation($"propiedades creadas");




        }

        private async Task CreateOrUpdateMaterialProperties(string matId, MaterialRequestProperty[] requestProperties, CancellationToken token)
        {
            //Get properties
            var properties = await SimaticMaterialService.GetMaterialPropertyForMaterialAsync(matId, token);

            var updateProp = requestProperties.Where(x => properties.Any(p => p.NId == x.Name)).ToArray();
            if (updateProp.Length > 0)
            {
                await SimaticMaterialService.UpdateMaterialProperties(matId, updateProp, token);
                logger.LogInformation($"Material Properties has been updated: Count [{updateProp.Length}].");
            }

            //Create Properties
            var createProp = requestProperties.Where(x => properties.All(p => p.NId != x.Name)).ToArray();
            if (createProp.Length > 0)
            {
                await SimaticMaterialService.CreateMaterialProperties(matId, createProp, token);
                logger.LogInformation($"Material Properties has been created: Count [{createProp.Length}].");

            }
        }

        private async Task CreateRDLSpecification(string MatNId, bool isCurrent, CancellationToken token)
        {
            var rdl = await SimaticMaterialService.RDLSpecification(MatNId, isCurrent, token);

            logger.LogInformation($"RDL Response: [{rdl}].");

        }
    }
}
