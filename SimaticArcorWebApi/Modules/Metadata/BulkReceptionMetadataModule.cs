using System;
using System.Collections.Generic;
using System.Text;
using Nancy;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
  public class BulkReceptionMetadataModule : MetadataModule<PathItem>
  {
    public BulkReceptionMetadataModule(ISwaggerModelCatalog modelCatalog)
    {
      //    modelCatalog.AddModels(
      //        typeof(TruckReceiveRequest),
      //        typeof(MTURequest),
      //        typeof(MTURequestMaterialLotProperty),
      //        typeof(MTURequestLocation),
      //        typeof(MTURequestPropertyValue),
      //        typeof(MTURequestQuantity)
      //    );

      //    Describe["TruckReceive"] = description => description.AsSwagger(
      //    with => with.Operation(
      //      op => op.OperationId("TruckReceive")
      //        .Tag("Bulk Reception")
      //        .Summary("Crea un Lote y MTU a partir de una llegada de camion")
      //        .Response((int)HttpStatusCode.Created, r => r.Description("Crea MTU y Lote"))
      //        .BodyParameter(p => p.Description("TruckReceive").Name("body").Schema<TruckReceiveRequest>())
      //    )
      //  );
      //    Describe["Confirm"] = description => description.AsSwagger(
      //        with => with.Operation(
      //            op => op.OperationId("Confirm")
      //                .Tag("Bulk Reception")
      //                .Summary("Confirma la recepción de un lote")
      //                .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza un lote de recepción de camión"))
      //                .BodyParameter(p => p.Description("Confirm").Name("body").Schema<TruckReceiveRequest>())
      //        )
      //    );
    }

  }
}
