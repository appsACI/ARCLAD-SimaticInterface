using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Endor.Core.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;
using SimaticArcorWebApi.Helpers;
using SimaticArcorWebApi.HttpClient;
using SimaticArcorWebApi.Model.Custom.Bom;
using SimaticArcorWebApi.Model.Simatic.BOM;
using SimaticArcorWebApi.Model.Simatic.Material;

namespace SimaticArcorWebApi.Management
{
  public class SimaticBOMService : ISimaticBOMService
  {
    #region Fields
    /// <summary>
    /// The custom logger
    /// </summary>
    private ILogger<SimaticBOMService> logger;

    #endregion

    public ISimaticService SimaticService { get; set; }

    public IUOMService UomService { get; set; }

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="SimaticService"/> class.
    /// </summary>
    public SimaticBOMService(ISimaticService simatic, IUOMService uomService)
    {
      if (logger == null) logger = ApplicationLogging.CreateLogger<SimaticBOMService>();

      SimaticService = simatic;
      UomService = uomService;
    }
    #endregion

    #region Interface implementation

    public async Task<dynamic> GetBillOfMaterialsByNIdAsync(string id, bool checkCurrent, bool throwException, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var url = $"sit-svc/application/UDM/odata/BillOfMaterials?$filter=NId eq '{id}'";

        if (checkCurrent)
          url += " and IsCurrent eq true";

        url += "&$orderby=Revision desc";

        HttpResponseMessage response = await client.GetAsync(url, token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

            if (result.value.Count >= 1)
              return ((IList<BillOfMaterials>)result.value.ToObject<BillOfMaterials[]>()).First();

            if (!throwException)
              return null;

            dynamic errorMessage = new { Error = "Bill of materials not found." };
            throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
          }, token);
      }
    }

    public async Task<dynamic> GetBillOfMaterialsItemsByIdAsync(string bomId, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var url = $"sit-svc/application/UDM/odata/BillOfMaterialsItem?$filter=BillOfMaterials_Id eq {bomId}";

        HttpResponseMessage response = await client.GetAsync(url, token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<BillOfMaterialsItem[]>();
          }, token);
      }
    }

    public async Task<dynamic> GetAllBillOfMaterialsAsync(CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        HttpResponseMessage response = await client.GetAsync($"sit-svc/application/UDM/odata/BillOfMaterials", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<BillOfMaterials[]>();
          }, token);
      }
    }

    /// <summary>
    /// Gets all BoMs to which Material belongs.
    /// </summary>
    /// <param name="nid">Material NId</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<dynamic> GetBillOfMaterialsByMaterialNIdAsync(string nid, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        //sit-svc/Application/PICore/odata/BillOfMaterials?$count=true&$filter=IsLocked eq false and IsCurrent eq true and (Facets/any(f:f/Siemens.SimaticIT.UAPI.PICore.PICore.PIPOMModel.DataModel.ReadingModel.BillOfMaterialsExtended/MaterialNId eq '3RPBB.029'))&$select=Id

        if (string.IsNullOrWhiteSpace(nid))
        {
          logger.LogError($"Error! Material NId was not provided for BoMs by Material NId list.");
          throw new SimaticApiException((Nancy.HttpStatusCode.BadRequest), null);
        }

        HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/PICore/odata/BillOfMaterials?$count=true&$filter=IsLocked eq false and IsCurrent eq true and (Facets/any(f:f/Siemens.SimaticIT.UAPI.PICore.PICore.PIPOMModel.DataModel.ReadingModel.BillOfMaterialsExtended/MaterialNId eq '{nid}'))", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<BillOfMaterials[]>();
          }, token);
      }
    }

    public async Task<dynamic> CreateBillOfMaterialsAsync(string id, string name, string description, Material material, string uoMNId, double quantityValue,
      CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        //{"command":{"ReferenceQuantity":{"UoMNId":"u","QuantityValue":12},"NId":"bomtest","Revision":"bomtest","Name":"bomtest","Description":"bomtest","TemplateNId":null}}
        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            NId = id,
            material.Description,
            Name = material.Description,
            MaterialNId = material.NId,
            MaterialRevision = "",
            ReferenceQuantity = new
            {
              UoMNId = UomService.GetSimaticUOM(uoMNId),
              QuantityValue = (decimal)quantityValue
            },
            Revision = "1"
          }
        });
        // CreateBillOfMaterialsWithMaterialAssociation
        // OLD: CreateBillOfMaterials
        var response = await client.PostAsync("sit-svc/application/UDM/odata/CreateBillOfMaterialsWithMaterialAssociation",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.BillOfMaterialsId.ToString();
          }, token);
      }
    }

    public async Task<string> CreateNewBillOfMaterialsRevision(string bomId, string sourceRevision, string revision, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            SourceNId = bomId,
            SourceRevision = sourceRevision,
            TargetRevision = revision
          }
        });

        var response = await client.PostAsync("sit-svc/Application/UDM/odata/CreateNewBillOfMaterialsRevision",
            new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
              return result.BillOfMaterialsId.ToString();
            }, token);
      }
    }

    public async Task<dynamic> GetBillOfMaterialsUserFields(string id, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));


        //BillOfMaterialsUserField?$filter=BillOfMaterials/Id%20eq%20b516e2e8-c02c-4e04-aa4e-48f27b7d2898&$expand=BillOfMaterials
        HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/UDM/odata/BillOfMaterialsUserField?$expand=BillOfMaterials&$filter=BillOfMaterials/Id eq {id}", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<BillOfMaterialsUserField[]>();
          }, token);
      }
    }

    public async Task DeleteBillOfMaterialsAsync(string id, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new { command = new { Id = id } });

        var response = await client.PostAsync("sit-svc/application/UDM/odata/DeleteBillOfMaterials",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task UpdateBillOfMaterialsUserFieldAsync(string id, string value, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id,
            UserFieldValue = value.ToString()
          }
        });

        var response = await client.PostAsync("sit-svc/Application/UDM/odata/UpdateBillOfMaterialsUserField",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task SetBillOfMaterialsRevisionCurrentAsync(string id, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id
          }
        });

        var response = await client.PostAsync("sit-svc/application/UDM/odata/SetBillOfMaterialsRevisionCurrent",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task SetBillOfMaterialsTemplateAsDefaultAsync(Guid id, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id
          }
        });

        var response = await client.PostAsync("sit-svc/Application/UDM/odata/SetBillOfMaterialsTemplateAsDefault",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task UnsetBillOfMaterialsRevisionCurrentAsync(string id, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id
          }
        });

        var response = await client.PostAsync("sit-svc/application/UDM/odata/UnsetBillOfMaterialsRevisionCurrent",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task UpdateBillOfMaterials(string id, string description, string name, string uom, double quantity, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id,
            Description = description,
            Name = name,
            ReferenceQuantity = new
            {
              UoMNId = UomService.GetSimaticUOM(uom),
              QuantityValue = (decimal)quantity
            }
          }
        });

        var response = await client.PostAsync("sit-svc/application/UDM/odata/UpdateBillOfMaterials",
        new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task UpdateBillOfMaterialsItem(string id, string description, string name, int sequence, string materialNId, string uom, double quantity, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id,
            Name = name,
            Description = description,
            Sequence = sequence,
            MaterialNId = materialNId,
            Quantity = new
            {
              UoMNId = UomService.GetSimaticUOM(uom),
              QuantityValue = ((decimal)quantity)
            },
            Alternative = new
            {
              AlternativeElement = 0,
              AlternativeGroup = 0,
              IsDefault = false
            },
            UseBillOfMaterials = "Never"
          }
        });

        var response = await client.PostAsync("sit-svc/application/UDM/odata/UpdateBillOfMaterialsItem",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task<dynamic> GetBillOfMaterialsExtended(string nId, string revision, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));
        HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/PICore/odata/BillOfMaterialsExtended?$filter=MaterialNId eq '{nId}' and MaterialRevision eq '{revision}'", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<BillOfMaterialsExtended[]>();
          }, token);
      }
    }

    public async Task<dynamic> GetBillOfMaterialsExtended(string bomId, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));
        HttpResponseMessage response = await client.GetAsync($"sit-svc/Application/PICore/odata/BillOfMaterialsExtended?$filter=BillOfMaterials_Id eq {bomId}", token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<BillOfMaterialsExtended[]>();
          }, token);
      }
    }

    public async Task CreateBillOfMaterialsExtended(string bomId, string matId, string revision, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        //{ "command":{ "BoMId":"36baf3ef-6185-ec11-b828-020017035491","MaterialNId":"3RTRE.003","MaterialRevision":"1.0.0","MaterialGroupNId":null} }
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            BoMId = bomId,
            MaterialNId = matId,
            MaterialRevision = revision
          }
        });

        var response = await client.PostAsync("sit-svc/Application/PICore/odata/CreateBillOfMaterialsExtended",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task RemoveBillOfMaterialsExtended(string bomId, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            BoMId = bomId
          }
        });

        var response = await client.PostAsync("sit-svc/Application/PICore/odata/DeleteBillOfMaterialsExtended",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task UpdateOrCreateBillOfMaterials(string id, Material material, string uom, double quantity, BillOfMaterialsRequestItem[] newItems, BillOfMaterialsRequestProperty[] newProperties, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id,
            material.Description,
            ReferenceQuantity = new
            {
              UoMNId = UomService.GetSimaticUOM(uom),
              QuantityValue = (decimal)quantity
            },
            BillOfMaterialsItems = newItems.Select(i => new
            {
              AlternativeElement = 0,
              AlternativeGroup = 0,
              AlternativeIsSelected = false,
              Description = i.Description,
              MaterialNId = i.MaterialId,
              NId = i.MaterialId,
              Name = i.MaterialId,
              QuantityValue = (decimal?)i.QuantityValue,
              Sequence = i.Sequence,
              UoMNId = UomService.GetSimaticUOM(i.UoMNId),
              UseBillOfMaterials = "Never"
            })
          }
        });

        var response = await client.PostAsync("sit-svc/application/UDM/odata/UpdateBillOfMaterials",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    [Obsolete]
    public async Task SetValidationOnBillOfMaterialsItem(string bomId, string nId, string uom, double quantity, int sequence, double? scrap, DateTime from, DateTime to, CancellationToken token)
    {
      return;

      //using (var client = new AuditableHttpClient(logger))
      //{
      //  client.BaseAddress = new Uri(SimaticService.GetUrl());

      //  // We want the response to be JSON.
      //  client.DefaultRequestHeaders.Accept.Clear();
      //  client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

      //  // Add the Authorization header with the AccessToken.
      //  client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

      //  var json = JsonConvert.SerializeObject(new
      //  {
      //    command = new
      //    {
      //      BOMId = bomId,
      //      MaterialNId = nId,
      //      ValidationFrom = from,
      //      ValidationTo = to
      //    }
      //  });

      //  var response = await client.PostAsync("sit-svc/application/WebCoreApplication/odata/CreateBOMItemWithLifetimeValidation",
      //  new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

      //  SimaticServerHelper.CheckFaultResponse(token, response, logger);

      //  await response.Content.ReadAsStringAsync();
      //}
    }

    public async Task DeleteBillOfMaterialsItem(string id, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = id
          }
        });

        var response = await client.PostAsync("sit-svc/Application/UDM/odata/DeleteBillOfMaterialsItem",
            new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    [Obsolete]
    public async Task<double> ConvertQuantityByUoM(string materialId, double sourceQuantity, string sourceUom,
        CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            MaterialId = materialId,
            SourceUoM = sourceUom,
            Quantity = (decimal)sourceQuantity
          }
        });

        var response = await client.PostAsync("sit-svc/Application/MaterialOperationApp/odata/ConvertQuantityToMaterialUoM",
        new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
              return Convert.ToDouble(result.Quantity.ToString());
            }, token);
      }
    }

    public async Task<dynamic> GetUomConversion(string materialId, string destinationUom, bool throwException, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var url = $"sit-svc/Application/MaterialOperationApp/odata/UoMConversion?$filter=Material_Id eq {materialId} and DestinationUoM eq '{destinationUom}'";

        HttpResponseMessage response = await client.GetAsync(url, token);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
            .ContinueWith(task =>
            {
              var result = JsonConvert.DeserializeObject<dynamic>(task.Result);

              if (result.value.Count >= 1)
                return ((IList<UomConversion>)result.value.ToObject<UomConversion[]>()).First();

              if (!throwException)
                return null;

              dynamic errorMessage = new { Error = "Uom conversion not found." };
              throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
            }, token);
      }
    }

    public async Task AddUomConversion(string materialId, string destinationUom, double? factor, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Material_Id = materialId,
            NId = "Planta",
            DestinationUoM = UomService.GetSimaticUOM(destinationUom),
            Factor = (decimal?)factor
          }
        });

        var response = await client.PostAsync("sit-svc/Application/MaterialOperationApp/odata/CreateUoMConversion",
            new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task UpdateUomConversion(string id, string destinationUom, double? factor, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            UoMConversionId = id,
            NId = "Planta",
            DestinationUoM = destinationUom,
            Factor = (decimal?)factor
          }
        });

        var response = await client.PostAsync("sit-svc/Application/MaterialOperationApp/odata/UpdateUoMConversion",
            new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task CreateBillOfMaterialsDefaultTemplateAsync(string templateNId, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // http://localhost/sit-svc/Application/UDM/odata/CreateBillOfMaterialsTemplate
        // {"command":{"NId":"DEFAULT"}}
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            NId = templateNId
          }
        });

        var response = await client.PostAsync("sit-svc/Application/UDM/odata/CreateBillOfMaterialsTemplate",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    public async Task<dynamic> GetBillOfMaterialsDefaultTemplateAsync(string templateNId, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var url = $"sit-svc/application/UDM/odata/BillOfMaterialsTemplate?$filter=NId eq '{templateNId}'";
        var response = await client.GetAsync(url, token);
        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            return result.value.ToObject<MasterDataTemplate[]>();
          }, token);
      }
    }

    public async Task<dynamic> GetBillOfMaterialsDefaultTemplatePropertiesAsync(string templateNId, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        var url = $"sit-svc/application/UDM/odata/MasterDataTemplateUserField?$filter=MasterDataTemplate/NId eq '{templateNId}' and MasterDataTemplate/IsDefault eq true&$expand=MasterDataTemplate";
        HttpResponseMessage response = await client.GetAsync(url, token);
        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        return await response.Content.ReadAsStringAsync()
          .ContinueWith(task =>
          {
            var result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            if (result.value.Count > 0)
            {
              logger.LogWarning($"Bill of Materials Template '{templateNId}' has {result.value.Count} Properties.");
            }
            else
            {
              logger.LogWarning($"Bill of Materials Template '{templateNId}' not found or it has no Properties.");
              //dynamic errorMessage = new { Error = $"Bill of Materials Template '{templateNId}' not found." };
              //throw new SimaticApiException(Nancy.HttpStatusCode.NotFound, errorMessage);
            }

            return result.value.ToObject<BillOfMaterialsTemplateUserField[]>();
          }, token);
      }
    }

    /// <summary>
    /// Updates BoM Template UserFields (Properties)
    /// </summary>
    /// <param name="templateId"></param>
    /// <param name="properties"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task UpdateBillOfMaterialsTemplateAsync(Guid templateId, UserFieldParamterType[] properties, CancellationToken token)
    {
      using (var client = new AuditableHttpClient(logger))
      {
        client.BaseAddress = new Uri(SimaticService.GetUrl());

        // We want the response to be JSON.
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add the Authorization header with the AccessToken.
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + await SimaticService.GetAccessToken(token));

        // Build up the data to POST.
        var json = JsonConvert.SerializeObject(new
        {
          command = new
          {
            Id = templateId,
            UserFields = properties
          }
        });

        var response = await client.PostAsync("sit-svc/Application/UDM/odata/UpdateBillOfMaterialsTemplate",
          new StringContent(json, Encoding.UTF8, "application/json"), token).ConfigureAwait(true);

        SimaticServerHelper.CheckFaultResponse(token, response, logger);

        await response.Content.ReadAsStringAsync();
      }
    }

    #endregion

  }
}
