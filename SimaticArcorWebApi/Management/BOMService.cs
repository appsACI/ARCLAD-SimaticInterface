using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.BOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nancy.Extensions;
using SimaticArcorWebApi.Model.Config;
using SimaticArcorWebApi.Model.Custom.Bom;
using SimaticArcorWebApi.Model.Simatic.Material;
using SimaticArcorWebApi.Model.Constants;

namespace SimaticArcorWebApi.Management
{
    public class BOMService : IBOMService
    {
        /// <summary>
        /// The custom logger
        /// </summary>
        private ILogger<BOMService> logger;

        private BOMConfig config;

        public ISimaticBOMService SimaticService { get; set; }

        public ISimaticMaterialService SimaticMaterialService { get; set; }

        public IUOMService UomService { get; set; }

        public BOMService(IConfiguration configuration, ISimaticBOMService simatic, ISimaticMaterialService materialSimatic, IUOMService uomService)
        {
            if (logger == null) logger = ApplicationLogging.CreateLogger<BOMService>();

            config = new BOMConfig();
            configuration.GetSection("BOMConfig").Bind(config);

            SimaticService = simatic;

            SimaticMaterialService = materialSimatic;

            UomService = uomService;
        }

        public async Task ProcessNewBomAsync(BillOfMaterialsRequest bom, CancellationToken token)
        {
            logger.LogInformation($"Processing BOM Id [{bom.Id}] ...");

            bool disableBom = false;
            bool needToRecreateBoM = false;
            string bomTemplateNId = "DEFAULT";
            //var bomNId = $"{bom.List}{bom.Id}";
            var bomNId = $"{bom.Id}";
            BillOfMaterials currentBom = null;
            BillOfMaterials revBom = null;
            BillOfMaterials nrevBom = null;
            BillOfMaterialsRequestProperty[] updateTemplateProperties = null;
            BillOfMaterialsRequestProperty[] createTemplateProperties = null;
            BillOfMaterialsRequestProperty[] createBoMProperties = null;

            string bomId = string.Empty;

            Material material = await SimaticMaterialService.GetMaterialByNIdAsync(bom.MaterialId, true, true, token);
            logger.LogInformation($"Found BoM Material [{bom.MaterialId}]");

            #region BoM actual engineering status & check Template properties.

            if (bom.Properties != null && bom.Properties.Count() > 0)
            {
                // Validate BoM Status by Engineering State based on a property value (Disabled == 5).
                string evalPropertyName = "AR ESTADO DE INGENIERIA";
                var propState = bom.Properties.Where(p => p.Name.ToUpper().Equals(evalPropertyName)).FirstOrDefault();
                if (propState != null)
                {
                    if (propState.Value.Equals("5"))
                    {
                        logger.LogInformation($"BOM [{bom.Id}] will be disabled, as it's status is: {propState.Value}");
                        disableBom = true;
                    }
                }
                else
                {
                    logger.LogDebug($"BOM [{bom.Id}] engineering status is not specifyed.");
                }

                // Validate BoM DEFAULT template properties (aka UserFields).
                #region -- BoM Template & Properties --

                if (!disableBom)
                {
                    logger.LogDebug($"Check BOM [{bom.Id}] Template '{bomTemplateNId}' Properties");

                    Guid bomTemplateId = Guid.Empty;
                    IList<MasterDataTemplate> bomTemplate = await SimaticService.GetBillOfMaterialsDefaultTemplateAsync(bomTemplateNId, token);
                    if (bomTemplate.FirstOrDefault() == null)
                    {
                        await SimaticService.CreateBillOfMaterialsDefaultTemplateAsync(bomTemplateNId, token);

                        bomTemplate = await SimaticService.GetBillOfMaterialsDefaultTemplateAsync(bomTemplateNId, token);
                        if (bomTemplate.FirstOrDefault() == null)
                        {
                            logger.LogDebug($"BOM [{bom.Id}] creating a default '{bomTemplateNId}' template failed, something was terrible wrong!");
                            return;
                        }
                        await SimaticService.SetBillOfMaterialsTemplateAsDefaultAsync(bomTemplate.FirstOrDefault().Id, token);
                    }
                    else if (bomTemplate.FirstOrDefault() != null && bomTemplate.FirstOrDefault().IsDefaut != true)
                    {
                        await SimaticService.SetBillOfMaterialsTemplateAsDefaultAsync(bomTemplate.FirstOrDefault().Id, token);
                    }

                    bomTemplateId = bomTemplate.FirstOrDefault().Id;

                    IList<BillOfMaterialsTemplateUserField> bomTemplatePoperties = await SimaticService.GetBillOfMaterialsDefaultTemplatePropertiesAsync(bomTemplateNId, token);
                    if (bomTemplatePoperties != null)
                    {
                        // Match Template properties
                        updateTemplateProperties = bom.Properties.Where(x => bomTemplatePoperties.Any(p => p.NId.ToLower() == x.Name.ToLower())).ToArray();
                        createTemplateProperties = bom.Properties.Where(x => bomTemplatePoperties.All(p => p.NId.ToLower() != x.Name.ToLower())).ToArray();
                        // Note: There is no intention to DELETE Template properties.
                        logger.LogInformation($"BOM Template '{bomTemplateNId}' already has {updateTemplateProperties.Length} Properties, need to create: {createTemplateProperties.Length} Properties.");

                        List<UserFieldParamterType> newUserFields = new List<UserFieldParamterType>();

                        if (createTemplateProperties.Length > 0)
                        {
                            // Add a new properties to the template
                            string ufType = "String";

                            foreach (var p in createTemplateProperties)
                            {
                                switch (p.Type.ToLower())
                                {
                                    case "int":
                                        ufType = "Int";
                                        break;
                                    case "decimal":
                                        ufType = "Decimal";
                                        break;
                                }

                                UserFieldParamterType uf = new UserFieldParamterType() { NId = p.Name, UserFieldType = ufType };
                                newUserFields.Add(uf);
                            }
                            // BoM will be recreated, as new Properties were added to Template.
                            needToRecreateBoM = true;
                        }

                        if (newUserFields.Count > 0)
                        {
                            await SimaticService.UpdateBillOfMaterialsTemplateAsync(bomTemplateId, newUserFields.ToArray(), token);
                        }
                    }
                    else
                    {
                        logger.LogWarning($"Probably no BOM Template '{bomTemplateNId}' found.");
                    }
                }

                #endregion
            }
            else
            {
                logger.LogWarning($"BOM [{bom.Id}] comes with no properties!");
            }

            #endregion

            #region Check Material existence (previous to creation)

            // TODO: ...

            #endregion

            revBom = await SimaticService.GetBillOfMaterialsByNIdAsync(bomNId, true, false, token); // returns CURRENT
            nrevBom = await SimaticService.GetBillOfMaterialsByNIdAsync(bomNId, false, false, token); // returns ANY revision
            currentBom = revBom ?? nrevBom;

            if (currentBom != null)
            {
                bomId = currentBom.Id;
            }

            /*
             * NOTE: As Opcenter does not permit add new Properties (UserFields) to existent BoM, we need to recreate it with updated Template!
             */
            #region Evaluate if will need to Add BoM Properties

            if (!string.IsNullOrEmpty(bomId) && bom.Properties.Count() > 0)
            {
                BillOfMaterialsUserField[] userField = await SimaticService.GetBillOfMaterialsUserFields(bomId, token);
                createBoMProperties = bom.Properties.Where(x => userField.All(p => p.NId != x.Name)).ToArray();
                if (createBoMProperties.Count() > 0)
                {
                    needToRecreateBoM = true;
                    logger.LogInformation($"BOM [{bomNId}] will need to create {createBoMProperties.Count()} UserFields, so it need to be recreated!");
                }
            }

            #endregion

            if (needToRecreateBoM && currentBom != null)
            {
                logger.LogInformation($"Need to recreate BOM [{bom.Id}], so it will be deleted first!");
                await SimaticService.DeleteBillOfMaterialsAsync(currentBom.Id, token);
                // Reset actual BoM object
                currentBom = null;
            }

            #region Create new BoM or Revision

            if (currentBom != null)
            {
                logger.LogInformation($"BOM [{bomNId}] Found. Id [{currentBom.Id}] Name [{currentBom.Name}] Revision [{currentBom.Revision}] IsCurrent [{currentBom.IsCurrent}].");

                if (currentBom.IsCurrent && config.EnableCreateNewRevision)
                {
                    logger.LogInformation($"New Revision for BOM [{bomNId}] will be created (config flag is '{config.EnableCreateNewRevision}').");
                    string newRev = GetNewRevision(revBom, nrevBom);

                    bomId = await SimaticService.CreateNewBillOfMaterialsRevision(bomNId, currentBom.Revision, newRev, token);
                    logger.LogInformation($"New Revision [{newRev}] for BOM [{bomNId}] is created with ID [{bomId}] (old was [{currentBom.Revision}] [{currentBom.Id}]).");

                    await SimaticService.UnsetBillOfMaterialsRevisionCurrentAsync(currentBom.Id, token);
                    await AlignBomMaterialLink(currentBom, bomId, token);
                }
            }
            else
            {
                logger.LogInformation($"Creating new Bom ID [{bomNId}].");

                bomId = await SimaticService.CreateBillOfMaterialsAsync(bomNId, bom.Id, bom.Id, material, bom.UoMNId, bom.QuantityValue.GetValueOrDefault(), token);
                currentBom = await SimaticService.GetBillOfMaterialsByNIdAsync(bomNId, false, false, token); // returns ANY revision

                //await SimaticService.CreateBillOfMaterialsExtended(updateBomId, material.NId, material.Revision, token);
                logger.LogInformation($"New BOM [{bomNId}] has been created successfully.");
            }

            #endregion

            if (!string.IsNullOrEmpty(bomId))
            {
                #region Evaluate & Update BoM Properties

                if (bom.Properties.Count() > 0)
                {
                    BillOfMaterialsUserField[] userField = await SimaticService.GetBillOfMaterialsUserFields(bomId, token);
                    foreach (BillOfMaterialsRequestProperty p in bom.Properties)
                    {
                        BillOfMaterialsUserField uf = userField.FirstOrDefault(f => f.NId.ToLower().Equals(p.Name.ToLower()));
                        if (uf != null)
                        {
                            await SimaticService.UpdateBillOfMaterialsUserFieldAsync(uf.Id, p.Value, token);
                        }
                        else
                        {
                            logger.LogWarning($"BOM [{bomNId}] UserField '{p.Name}' was not found among existent BoM Properties and will not be updated!");
                        }
                    }
                }

                #endregion

                await CheckBomMaterialLink(currentBom, bomId, material, token);
                logger.LogInformation($"Link BOM [{bomNId}] to material [{material.NId}] succeed.");

                #region BoM Items

                logger.LogInformation($"Updating BOM [{bomNId}] items...");
                IList<BillOfMaterialsItem> items = await SimaticService.GetBillOfMaterialsItemsByIdAsync(bomId, token);
                var updateItems = bom.Items.Where(x => items.Any(p => p.MaterialNId == x.MaterialId)).ToArray();
                if (updateItems.Length > 0)
                {
                    foreach (var bomItem in updateItems)
                    {
                        var uomValue = await GetUomAndValue(bomItem.MaterialId, bomItem.UoMNId, bomItem.QuantityValue.GetValueOrDefault(), token);

                        var item = items.FirstOrDefault(p => p.MaterialNId == bomItem.MaterialId);
                        Material matIem = await SimaticMaterialService.GetMaterialByNIdAsync(item.MaterialNId, true, true, token);
                        await SimaticService.UpdateBillOfMaterialsItem(item.Id, matIem.Description, bomItem.MaterialId, bomItem.Sequence, bomItem.MaterialId, uomValue.Key, uomValue.Value, token);
                    }

                    logger.LogInformation($"BOM [{bomNId}] {updateItems.Length} items has been updated.");
                }

                var createItems = bom.Items.Where(x => items.All(p => p.MaterialNId != x.MaterialId)).ToArray();
                foreach (var bomItem in createItems)
                {
                    var uomValue = await GetUomAndValue(bomItem.MaterialId, bomItem.UoMNId, bomItem.QuantityValue.GetValueOrDefault(), token);
                    bomItem.UoMNId = uomValue.Key;
                    bomItem.QuantityValue = uomValue.Value;

                    Material matIem = await SimaticMaterialService.GetMaterialByNIdAsync(bomItem.MaterialId, true, true, token);
                    bomItem.Description = matIem.Description;
                }
                logger.LogInformation($"BOM [{bomNId}] {createItems.Length} items has been created.");

                // Updates BOM (new or existent) items.
                await SimaticService.UpdateOrCreateBillOfMaterials(bomId, material, bom.UoMNId, bom.QuantityValue.GetValueOrDefault(), createItems, null, token);
                logger.LogInformation($"BOM [{bomNId}] has been updated successfully.");

                var deleteItems = items.Where(x => bom.Items.All(p => p.MaterialId != x.MaterialNId)).ToArray();
                foreach (var item in deleteItems)
                {
                    logger.LogInformation($"Deleting BOM [{bomNId}] old items. NId: {item.NId}, Material: {item.MaterialNId}");
                    await SimaticService.DeleteBillOfMaterialsItem(item.Id, token);
                }

                if (deleteItems.Count() > 0)
                    logger.LogInformation($"BOM [{bomNId}] {deleteItems.Length} items has been deleted.");

                #endregion

                #region Set / Unset BoM as CURRENT

                if (!currentBom.IsCurrent && !disableBom)
                {
                    await SimaticService.SetBillOfMaterialsRevisionCurrentAsync(bomId, token);
                    logger.LogInformation($"Check / Set BOM [{bomNId}] as CURRENT succeed.");
                }
                else
                {
                    if (disableBom && currentBom.IsCurrent)
                    {
                        await SimaticService.UnsetBillOfMaterialsRevisionCurrentAsync(bomId, token);
                        logger.LogInformation($"Unset BOM [{bomNId}] as CURRENT succeed.");
                    }
                }

                #endregion
            }
            else
            {
                logger.LogCritical($"BOM [{bomNId}] NOT FOUND, can not continue!");
            }

            //logger.LogInformation($"Setting BOM [{bomNId}] validation date.");
            //foreach (var item in bom.Items)
            //{
            //  await SimaticService.SetValidationOnBillOfMaterialsItem(updateBomId, item.MaterialId, item.UoMNId, item.QuantityValue.GetValueOrDefault(), item.Sequence, item.Scrap, item.From, item.To, token);
            //}

            logger.LogInformation($"BOM [{bomNId}] has been processed successfully.");
        }

        private async Task CheckBomMaterialLink(BillOfMaterials currentBom, string bomId, Material material, CancellationToken token)
        {
            logger.LogInformation($"Checking BOM link");

            if (currentBom == null)
            {
                logger.LogInformation($"BoM extended not found, creating one");
                await SimaticService.CreateBillOfMaterialsExtended(bomId, material.NId, material.Revision, token);
                return;
            }

            var bomsExt = (IList<BillOfMaterialsExtended>)await SimaticService.GetBillOfMaterialsExtended(currentBom.Id, token);

            if (bomsExt == null || bomsExt.Count == 0)
            {
                logger.LogInformation($"BoM extended not found, creating one");
                await SimaticService.CreateBillOfMaterialsExtended(bomId, material.NId, material.Revision, token);
            }
            else
            {
                logger.LogInformation($"Link found, get link for this material {material.NId}");
                var current = bomsExt.FirstOrDefault(x => x.MaterialNId == material.NId && x.MaterialRevision == material.Revision);
                if (current == null)
                {
                    logger.LogInformation($"Link for material not found, will create one");
                    //{ "command":{ "BoMId":"36baf3ef-6185-ec11-b828-020017035491","MaterialNId":"3RTRE.003","MaterialRevision":"1.0.0","MaterialGroupNId":null} }
                    await SimaticService.CreateBillOfMaterialsExtended(bomId, material.NId, material.Revision, token);
                }

                foreach (var bomExt in bomsExt.Where(x => x.MaterialNId != material.NId || x.MaterialRevision != material.Revision))
                {
                    logger.LogInformation($"Links with other materials for this BoM has been detected, clearing that links.");
                    //await SimaticService.RemoveBillOfMaterialsExtended(bomExt.BillOfMaterials_Id, token);
                }
            }
        }

        public async Task AddUomConversionEntity(UomConversionRequest uom, CancellationToken token)
        {
            logger.LogInformation($"Add new conversion. Material NId [{uom.Item}] Destination [{uom.DestinationUom} Factor [{uom.Factor}]]");

            logger.LogInformation($"Getting material...");
            Material material = await SimaticMaterialService.GetMaterialByNIdAsync(uom.Item, true, true, token);

            UomConversion conv = await SimaticService.GetUomConversion(material.Id, uom.DestinationUom, false, token);

            if (conv == null)
            {
                logger.LogInformation($"Add new conversion");
                await SimaticService.AddUomConversion(material.Id, uom.DestinationUom, uom.Factor, token);
            }
            else
            {
                logger.LogInformation($"Edit conversion");
                await SimaticService.UpdateUomConversion(conv.Id, uom.DestinationUom, uom.Factor, token);
            }

            logger.LogInformation($"UOM has been added.");
        }

        private async Task<KeyValuePair<string, double>> GetUomAndValue(string materialNId, string uomSource, double sourceValue, CancellationToken token)
        {
            uomSource = UomService.GetSimaticUOM(uomSource);

            return new KeyValuePair<string, double>(uomSource, sourceValue);

            // Need to be refactoried
            //Material material = await SimaticMaterialService.GetMaterialByNIdAsync(materialNId, true, true, token);

            //if (!string.IsNullOrEmpty(material.UoMNId) && !string.IsNullOrEmpty(uomSource))
            //{

            //  if (material.UoMNId == uomSource)
            //  {
            //    return new KeyValuePair<string, double>(uomSource, sourceValue);
            //  }
            //  else
            //  {
            //    try
            //    {
            //      var targetValue = await SimaticService.ConvertQuantityByUoM(material.Id, sourceValue, uomSource, token);
            //      return new KeyValuePair<string, double>(material.UoMNId, targetValue);
            //    }
            //    catch (Exception ex)
            //    {
            //      logger.LogWarning($"Error getting uom and value conversion. {ex.Message}");
            //      return new KeyValuePair<string, double>(uomSource, sourceValue);
            //    }
            //  }
            //}
            //else
            //{
            //  return new KeyValuePair<string, double>(uomSource, sourceValue);
            //}
        }

        private async Task AlignBomMaterialLink(BillOfMaterials currentBom, string newBomId, CancellationToken token)
        {
            var bomsExt = (IList<BillOfMaterialsExtended>)await SimaticService.GetBillOfMaterialsExtended(currentBom.Id, token);
            foreach (var bomExt in bomsExt)
            {
                await SimaticService.RemoveBillOfMaterialsExtended(currentBom.Id, token);

                await SimaticService.CreateBillOfMaterialsExtended(newBomId, bomExt.MaterialNId, bomExt.MaterialRevision, token);
            }
        }

        private static string GetNewRevision(BillOfMaterials rev, BillOfMaterials noRev)
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

        //[Obsolete]
        //private async Task UpdateUserFieldDueDateAsync(string bomId, DateTime newDueDate, CancellationToken ct)
        //{
        //  BillOfMaterialsUserField[] userField = await SimaticService.GetBillOfMaterialsUserFields(bomId, ct);

        //  var dueDateUF = userField.FirstOrDefault(t => t.NId == "DueDate");
        //  if (dueDateUF != null && (dueDateUF.UserFieldValue == null || Convert.ToDateTime(dueDateUF.UserFieldValue) != newDueDate))
        //  {
        //    await SimaticService.UpdateBillOfMaterialsUserField(dueDateUF.Id, newDueDate, ct);
        //  }
        //}

    }
}
