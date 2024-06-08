using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.RoadMap;
using SimaticArcorWebApi.Model.Simatic.Material;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
  public class RoadMapMetadataModule : MetadataModule<PathItem>
  {
    public RoadMapMetadataModule(ISwaggerModelCatalog modelCatalog)
    {
      //modelCatalog.AddModels(
      //	typeof(RoadMapRequest),
      //             typeof(RoadMapRequestOperations)
      //         );

      //Describe["GetRoadMap"] = description => description.AsSwagger(
      //	with => with.Operation(
      //		op => op.OperationId("GetRoadMap")
      //			.Tag("Hoja De Ruta")
      //			.Summary("Devuelve todas las hojas de rutas")
      //			.Description("Devuelve todas las hojas de rutas.")
      //			.Response(200, r => r.Description("OK"))
      //	)
      //);

      //Describe["GetRoadMapById"] = description => description.AsSwagger(
      //	with => with.Operation(
      //		op => op.OperationId("GetRoadMapById")
      //			.Tag("Hoja De Ruta")
      //			.Parameter(p => p.Name("Id").Description("Identificador de la hoja de ruta").IsRequired().In(ParameterIn.Path))
      //			.Summary("Devuelve una hoja de ruta según el Id")
      //			.Description("Devuelve una hoja de ruta según el Id")
      //			.Response(200, r => r.Description("OK"))
      //	)
      //);

      //   Describe["PostRoadMap"] = description => description.AsSwagger(
      //	with => with.Operation(
      //		op => op.OperationId("PostRoadMap")
      //			.Tag("Hoja De Ruta")
      //			.Summary("Crea una hoja de ruta")
      //			.Response((int)HttpStatusCode.Created, r => r.Description("Hoja de ruta creada"))
      //			.BodyParameter(p => p.Description("RoadMap").Name("body").Schema<RoadMapRequest>())
      //	)
      //);

      //Describe["PostRoadMapBulkInsert"] = description => description.AsSwagger(
      //	with => with.Operation(
      //		op => op.OperationId("PostRoadMapBulkInsert")
      //			.Tag("Hoja De Ruta")
      //			.Summary("Crea hojas de ruta a partir de un array")
      //			.Response((int)HttpStatusCode.Created, r => r.Description("Las hojas de rutas fueron creadas"))
      //			.BodyParameter(p => p.Description("Array de hojas de ruta").Name("body").Schema<RoadMapRequest>())
      //	)
      //);

    }

  }
}
