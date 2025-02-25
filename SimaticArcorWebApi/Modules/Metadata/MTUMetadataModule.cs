using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Services;

using SimaticArcorWebApi.Model.Custom.MaterialTrackingUnit;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticWebApi.Model.Custom.PrintLabel;
using SimaticWebApi.Model.Custom.RDL;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
    public class MTUMetadataModule : MetadataModule<PathItem>
    {
        public MTUMetadataModule(ISwaggerModelCatalog modelCatalog)
        {
            modelCatalog.AddModels(
              typeof(MTURequest),
              typeof(MTURequestMaterialLotProperty),
              typeof(MTURequestLocation),
              typeof(MTURequestPropertyValue),
              typeof(MTURequestQuantity),
              typeof(MTUAsigned),
              typeof(MTUAssignedUnassign),
              typeof(MTUDescount),
              typeof(MtuInfo),
              typeof(PrintModel),
              typeof(ReimpresionFilters),
              typeof(MezclasPrintModel),
              typeof(RecubrimientoModel),
              typeof(RolloPrintModel),
              typeof(ARSEGPrintModel),
              typeof(OtherTagsModel),
              typeof(MaterialTrackingUnitProperty),
              typeof(MTUId),
              typeof(Requirement),
              typeof(CreateRequirementModel),
              typeof(CreateRequirementModelCorte),
              typeof(UpdateLengthModel)
              );

            Describe["UpdateLot"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("UpdateLot")
                        .Tag("Material Tracking Unit")
                        .Summary("Actualiza un lote")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza los parametros de un lote"))
                        .BodyParameter(p => p.Description("Actualiza lote y mtu").Name("body").Schema<MTURequest>())
                )
            );

            Describe["UpdateLotVinilos"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("UpdateLotVinilos")
                        .Tag("Material Tracking Unit")
                        .Summary("Actualizar array de vinilos y su estiba")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza un array de vinilos"))
                        .BodyParameter(p => p.Description("Actualiza Un array de vinilos").Name("body").Schema<MTURequest>())
                )
            );


            Describe["CreateSample"] = description => description.AsSwagger(
             with => with.Operation(
                 op => op.OperationId("CreateSample")
                     .Tag("Material Tracking Unit")
                     .Summary("Crear Mtu de muestra")
                     .Response((int)HttpStatusCode.Created, r => r.Description("crea y actualiza las muestras"))
                     .BodyParameter(p => p.Description("crea y actualiza las muestras").Name("body").Schema<MTURequest>())
             )
         );

            Describe["CreateRequirement"] = description => description.AsSwagger(
             with => with.Operation(
                 op => op.OperationId("CreateRequirement")
                     .Tag("Material Tracking Unit")
                     .Summary("Crear requerimiento de muestra")
                     .Response((int)HttpStatusCode.Created, r => r.Description("crea los requerimientos de muestras"))
                     .BodyParameter(p => p.Description("crea y actualiza los requerimientos de muestra").Name("body").Schema<CreateRequirementModel>())
             )
         );

            Describe["UpdateRequirement"] = description => description.AsSwagger(
          with => with.Operation(
              op => op.OperationId("UpdateRequirement")
                  .Tag("Material Tracking Unit")
                  .Summary("Crear requerimiento de muestra")
                  .Response((int)HttpStatusCode.Created, r => r.Description(" actualiza los requerimientos de muestras"))
                  .BodyParameter(p => p.Description("crea y actualiza los requerimientos de muestra").Name("body").Schema<CreateRequirementModel>())
          )
      );

            Describe["CreateRequirementCorte"] = description => description.AsSwagger(
            with => with.Operation(
                op => op.OperationId("CreateRequirementCorte")
                    .Tag("Material Tracking Unit")
                    .Summary("Crear requerimiento corte de muestra")
                    .Response((int)HttpStatusCode.Created, r => r.Description("crea y actualiza los requerimientos de muestras de corte"))
                    .BodyParameter(p => p.Description("crea y actualiza los requerimientos de muestra de Corte").Name("body").Schema<CreateRequirementModelCorte>())
            )
        );

            Describe["UpdateRequirementLength"] = description => description.AsSwagger(
            with => with.Operation(
                op => op.OperationId("UpdateRequirementLength")
                    .Tag("Material Tracking Unit")
                    .Summary("actualizar longitud requirement")
                    .Response((int)HttpStatusCode.Created, r => r.Description("actualiza la longitud"))
                    .BodyParameter(p => p.Description("actualiza la longitud").Name("body").Schema<UpdateLengthModel>())
            )
        );

            Describe["ConsultaRequirement"] = description => description.AsSwagger(
            with => with.Operation(
                op => op.OperationId("ConsultaRequirement")
                    .Tag("Material Tracking Unit")
                    .Parameter(p => p.Name("Lote").Description("Identificador del Lote a consultar").IsRequired().In(ParameterIn.Path))
                    .Parameter(p => p.Name("Nid").Description("Identificador del Material a consultar").IsRequired().In(ParameterIn.Path))
                    .Summary("Consulta info requerimientos")
                    .Response((int)HttpStatusCode.Created, r => r.Description("consulta los requerimientos de muestras de corte"))
            )
        );

            Describe["Descount"] = description => description.AsSwagger(
                 with => with.Operation(
                    op => op.OperationId("Descount")
                  .Tag("Material Tracking Unit")
                  .Summary("descuenta el material a un lote")
                  .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza la cantidad de un lote y mtu"))
                  .BodyParameter(p => p.Description("actualiza la cantidad del mtu y el lote").Name("body").Schema<MTUDescount>())
          )
      );

            Describe["AssignedUnassign"] = description => description.AsSwagger(
                 with => with.Operation(
                    op => op.OperationId("AssignedUnassign")
                  .Tag("Material Tracking Unit")
                  .Summary("Desasigna los materiales de la orden")
                  .Response((int)HttpStatusCode.Created, r => r.Description("Devolucion de materiales"))
                  .BodyParameter(p => p.Description("Desasigna los materiales de la orden").Name("body").Schema<MTUAssignedUnassign>())
          )
      );
            Describe["PrintLabel"] = description => description.AsSwagger(
                 with => with.Operation(
                    op => op.OperationId("PrintLabel")
                  .Tag("Material Tracking Unit")
                  .Summary("Imprime la etiqueta del nuevo lote")
                  .Response((int)HttpStatusCode.Created, r => r.Description("Etiqueta impresa exitosamente"))
                  .BodyParameter(p => p.Description("Imprime la etiqueta del nuevo lote").Name("body").Schema<PrintModel>())
          )
      );
            Describe["PrintLabelBulk"] = description => description.AsSwagger(
                with => with.Operation(
                   op => op.OperationId("PrintLabelBulk")
                 .Tag("Material Tracking Unit")
                 .Summary("Imprime la etiqueta de los nuevos lotes")
                 .Response((int)HttpStatusCode.Created, r => r.Description("Etiquetas impresas exitosamente"))
                 .BodyParameter(p => p.Description("trae los datos de las etiquetas ya impresas").Name("body").Schema<PrintModel>())
         )
     );

            Describe["LabelHistory"] = description => description.AsSwagger(
                with => with.Operation(
                   op => op.OperationId("LabelHistory")
                 .Tag("Material Tracking Unit")
                 .Summary("trae los datos de las etiquetas por filtro")
                 .Response((int)HttpStatusCode.Created, r => r.Description("Etiquetas impresas exitosamente"))
                 .BodyParameter(p => p.Description("Imprime la etiqueta de los nuevos lotes").Name("body").Schema<ReimpresionFilters>())
         )
     );



            Describe["UpdateLotBulk"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("UpdateLotBulk")
                        .Tag("Material Tracking Unit")
                        .Summary("Procesa lista de lotes")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza lista de lotes y sus los parametros"))
                        .BodyParameter(p => p.Description("Actualiza Lote y MTU desde lista").Name("body").Schema<MTURequest>())
                )
            );

            Describe["MoveMTU"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("MoveLot")
                        .Tag("Material Tracking Unit")
                        .Summary("Mueve MTU o parte de el (SplitMTU)")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Efectua split de un mtu"))
                        .BodyParameter(p => p.Description("Realiza Split del MTU").Name("body").Schema<MTURequest>())
                )
            );

            Describe["GetMaterialTrackingUnitProperty"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("GetMaterialTrackingUnitProperty")
                        .Tag("Material Tracking Unit")
                        .Parameter(p => p.Name("NId").Description("Identificador de la orden a consultar").IsRequired().In(ParameterIn.Path))
                        .Summary("Consultar los parametros de la MTU")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Listado de parametros de la tabla.").Schema<MaterialTrackingUnitProperty>())
                )
            );

            Describe["GetPropertiesDefectos"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("GetPropertiesDefectos")
                        .Tag("Material Tracking Unit")
                        .Summary("Consultar las propiedades de defectos padre de las MTU")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Propiedades del MTU consultadas los defectos"))
                        .BodyParameter(p => p.Description("Objeto de entrada para consultar las MTU propiedades de defectos").Name("body").Schema<MTUId>())
                )
            );

            // Describe["G8Response"] = description => description.AsSwagger(
            //     with => with.Operation(
            //       op => op.OperationId("G8Response")
            //         .Tag("Process G8 response")
            //         .Summary("Procesa Respuesta del G8")
            //         .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza la ubicacion de un lote"))
            //         .BodyParameter(p => p.Description("G8ResponseRequest").Name("body").Schema<G8ResponseRequest>())
            //     )
            //   );

            // Describe["DCMovement"] = description => description.AsSwagger(
            //   with => with.Operation(
            //     op => op.OperationId("DCMovement")
            //       .Tag("Send information from JDE to System")
            //       .Summary("Send information from JDE to System")
            //       .Response((int)HttpStatusCode.Created, r => r.Description("Send information from JDE to System"))
            //       .BodyParameter(p => p.Description("DCMovementRequest").Name("body").Schema<DCMovementRequest>())
            //   )
            // );
            // Describe["LotMovementJDE"] = description => description.AsSwagger(
            //  with => with.Operation(
            //    op => op.OperationId("LotMovementJDE")
            //      .Tag("Delivery or Return of lot to JDE")
            //      .Summary("Entrega o retorno de lotes a JDE")
            //      .Response((int)HttpStatusCode.Created, r => r.Description("Entrega o retorno de lotes a JDE"))
            //      .BodyParameter(p => p.Description("LotMovementJDE").Name("body").Schema<MTURequest>())
            //  )
            //);
        }

    }
}
