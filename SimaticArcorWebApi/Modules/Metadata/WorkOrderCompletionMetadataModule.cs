using System.Net;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using SimaticArcorWebApi.Model.Custom.WorkOrderCompletion;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
    public class WorkOrderCompletionMetadataModule : MetadataModule<PathItem>
    {
        public WorkOrderCompletionMetadataModule(ISwaggerModelCatalog modelCatalog)
        {
            modelCatalog.AddModels(
                typeof(WorkOrderCompletionModel),
                typeof(DetailsForWorkOrderCompletion),
                typeof(MaterialConsumedActualForWorkOrderCompletion),
                typeof(InventoryForWorkOrderCompletion),
                typeof(ScrapForWorkOrderCompletion),
                typeof(WorkOrderCompletionVinilosModel),
                typeof(DetailsForWorkOrderCompletionVinilos),
                typeof(MaterialConsumedActualForWorkOrderCompletionVinilos),
                typeof(InventoryForWorkOrderCompletionVinilos),
                typeof(ScrapForWorkOrderCompletionVinilos),
                typeof(WorkOrderCompletionConsumoModel),
                typeof(MaterialConsumedActualForWorkOrderCompletionConsumo),
                typeof(InventoryForWorkOrderCompletionConsumo)

            );

            Describe["CreateWoCompletion"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateWoCompletion")
                        .Tag("WoCompletion")
                        .Summary("Crear declaracion de orden")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza los parametros de un lote").Schema<dynamic>())
                        .BodyParameter(p => p.Description("Work_Order_Completion").Name("body").Schema<WorkOrderCompletionModel>())
                )
            );

            Describe["CreateWoCompletionVinilos"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateWoCompletionVinilos")
                        .Tag("WoCompletion")
                        .Summary("Crear declaracion de orden de vinilos")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Eniva el completion de una orden de vinilos").Schema<dynamic>())
                        .BodyParameter(p => p.Description("Work_Order_Completion").Name("body").Schema<WorkOrderCompletionVinilosModel>())
                )
            );

            Describe["CreateWoCompletionConsumo"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateWoCompletionConsumo")
                        .Tag("WoCompletion")
                        .Summary("Crear declaracion de orden")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Actualiza los parametros de un lote"))
                        .BodyParameter(p => p.Description("Consumo de WO COmpletion").Name("body").Schema<WorkOrderCompletionConsumoModel>())
                )
            );

        }

    }
}
