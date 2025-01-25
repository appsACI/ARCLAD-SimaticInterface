using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.PalletReception;
using SimaticArcorWebApi.Model.Simatic.Material;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
  public class PalletReceiveMetadataModule : MetadataModule<PathItem>
  {
    public PalletReceiveMetadataModule(ISwaggerModelCatalog modelCatalog)
    {
      //modelCatalog.AddModels(
      //	typeof(CreatePalletReceptionRequest), typeof(PalletRequestQuantity), typeof(PalletRequestProperty));

      //Describe["CreatePalletReception"] = description => description.AsSwagger(
      //	with => with.Operation(
      //		op => op.OperationId("Create")
      //			.Tag("Pallets")
      //			.Summary("Generar una devolución de pallets")
      //			.Response((int)HttpStatusCode.Created, r => r.Description("Generar una devolución de pallets"))
      //			.BodyParameter(p => p.Description("CreatePalletReceptionRequest").Name("body").Schema<CreatePalletReceptionRequest>())
      //	)
      //);

    }

  }
}
