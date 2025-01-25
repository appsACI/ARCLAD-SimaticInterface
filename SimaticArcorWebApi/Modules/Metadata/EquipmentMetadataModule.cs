using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using SimaticArcorWebApi.Model.Custom;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
  public class EquipmentMetadataModule : MetadataModule<PathItem>
  {
    public EquipmentMetadataModule(ISwaggerModelCatalog modelCatalog)
    {
      modelCatalog.AddModels(
          typeof(EquipmentPQRNotification),
          typeof(EquipmentPreventiveMaintenaceNotification)
      );

      Describe["PostPQRNotification"] = description => description.AsSwagger(
          with => with.Operation(
              op => op.OperationId("PostPQRNotification")
                  .Tag("Equipment")
                  .Summary("Asigna el PQR generado al equipo.")
                  .Response((int)HttpStatusCode.Created, r => r.Description("PQR asignado"))
                  .BodyParameter(p => p.Description("Equipment PQR").Name("body").Schema<EquipmentPQRNotification>())
          )
      );

      Describe["EquipmentPreventiveMaintenaceNotification"] = description => description.AsSwagger(
          with => with.Operation(
              op => op.OperationId("EquipmentPreventiveMaintenaceNotification")
                  .Tag("Equipment")
                  .Summary("Almacena los datos del Mantenimiento Preventivo programado en las propiedades del equipo.")
                  .Response((int)HttpStatusCode.Accepted, r => r.Description("Preventivo registrado satisfactoriamente"))
                  .BodyParameter(p => p.Description("Preventive Maintenance").Name("body").Schema<EquipmentPreventiveMaintenaceNotification>())
          )
      );
    }
  }
}
