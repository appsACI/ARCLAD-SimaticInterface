using System.Net;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.MTU;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using SimaticWebApi.Model.Custom.Counters;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
    public class OrderMetadataModule : MetadataModule<PathItem>
    {
        public OrderMetadataModule(ISwaggerModelCatalog modelCatalog)
        {
            modelCatalog.AddModels(
                typeof(ProductionRequest),
                typeof(ProductionRequestParameters),
                typeof(ProductionRequestOperations),
                typeof(ProductionRequestOperationAlternativeAssets),
                typeof(ProductionRequestOperationParameters),
                typeof(ProductionRequestOperationInventoryDetail),
                typeof(ProductionResponse),
                typeof(OrderResponseObject),
                typeof(ParametersRollosClientes),
                typeof(WorkOrderOperationRevisionRequest),
                typeof(WorkOrderOperation),
                typeof(WorkOrderOperationStatus),
                typeof(ProcessParameter),
                typeof(object),
                typeof(StatusAndTimeOrder),
                typeof(Counters),
                typeof(Params),
                typeof(WorkOrderOperationParameterSpecification),
                typeof(MaterialTrackingUnitProperty),
                typeof(MaterialOperationEmpaqueCorteHojas),
                typeof(MaterialsQuantity),
                typeof(RTDSRequest)
            );

            Describe["CreateOrders"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateOrders")
                        .Tag("Order")
                        .Summary("Crear una orden de producción")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Orden aceptada").Schema<ProductionResponse>())
                        .BodyParameter(p => p.Description("ProductionRequest").Name("body").Schema<ProductionRequest>())
                )
            );

            Describe["CreateOrdersBulkInsert"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateOrdersBulkInsert")
                        .Tag("Order")
                        .Summary("Crear varias ordenes de producción")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Crear varias ordenes de producción"))
                        .BodyParameter(p => p.Description("Array de production request").Name("body").Schema<ProductionRequest>())
                )
            );

            Describe["CreateOrdersOperations"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateOrdersOperations")
                        .Tag("Order")
                        .Summary("Crear varias operaciones asociadas a una orden")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Crear varias operaciones asociadas a una orden"))
                        .BodyParameter(p => p.Description("Objeto de entrada para crear una Operación en la orden").Name("body").Schema<WorkOrderOperationRevisionRequest>())
                )
            );

            Describe["GetParametersRevisionTrim"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("GetParametersRevisionTrim")
                        .Tag("Order")
                        .Parameter(p => p.Name("NId").Description("Identificador de la orden a consultar").IsRequired().In(ParameterIn.Path))
                        .Summary("Consultar los parametros de revision traer solo el trim")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Listado de parametros de la tabla.").Schema<WorkOrderOperationParameterSpecification>())
                )
            );

            Describe["GetParametersRevisionMTU"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("GetParametersRevisionMTU")
                        .Tag("Order")
                        .Parameter(p => p.Name("NId").Description("Identificador de la orden a consultar").IsRequired().In(ParameterIn.Path))
                        .Summary("Consultar los parametros de revision traer solo los parametros de la MTU")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Listado de parametros de la tabla.").Schema<WorkOrderOperationParameterSpecification>())
                )
            );

            Describe["ChangeStatusAndTime"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("ChangeStatusAndTime")
                        .Tag("Order")

                        .Summary("Actualizar el estado y el tiempo de la orden")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Orden actualizada correctamente"))
                        .BodyParameter(p => p.Description("Body para cambiar el estado y el tiempo de la orden").Name("body").Schema<StatusAndTimeOrder>())

                )
            );

            Describe["UpdateCounters"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("UpdateCounters")
                        .Tag("Order")

                        .Summary("Actualizar Los contadores de la order")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Orden actualizada correctamente"))
                        .BodyParameter(p => p.Description("Body para cambiar el estado y el tiempo de la orden").Name("body").Schema<Counters>())

                )
            );

            Describe["UpdateCounterTrimCorte"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("UpdateCounterTrimCorte")
                        .Tag("Order")
                        .Parameter(p => p.Name("NId").Description("Identificador del parametro a consultar del Trim").IsRequired().In(ParameterIn.Path))
                        .Parameter(p => p.Name("cantCortes").Description("Cantidad de cortes del trim (Solo vinilos)").IsRequired().In(ParameterIn.Path))
                        .Summary("Sumar uno mas en el contador de revision de cada trim")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Parametro del Trim contador actualizado.").Schema<WorkOrderOperationParameterSpecification>())
                )
            );

            Describe["UpdateChecksRevisionOperations"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("UpdateChecksRevisionOperations")
                        .Tag("Order")
                        .Summary("Actualizar Checks de Declarar y Desperdicios de la Revision")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Parametros de revision actualizado"))
                        .BodyParameter(p => p.Description("Objeto de entrada para actualizar los parametros de revision").Name("body").Schema<WorkOrderOperationRevisionRequest>())
                )
            );

            Describe["AddMaterialOperationEmpaque"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("AddMaterialOperationEmpaque")
                        .Tag("Order")
                        .Summary("Crear material en la operacion de empaque para corte de hojas")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Material creado en la operacion de corte"))
                        .BodyParameter(p => p.Description("Objeto de entrada para crear un Material en la operacion de empaque de corte de hojas").Name("body").Schema<MaterialOperationEmpaqueCorteHojas>())
                )
            );

            Describe["PostToRTDSWrite"] = description => description.AsSwagger(
                 with => with.Operation(
                     op => op.OperationId("PostToRTDSWrite")
                         .Tag("RTDS")
                         .Summary("Enviar datos a RTDSWrite")
                         .Response((int)HttpStatusCode.OK, r => r.Description("Solicitud exitosa").Schema<string>())
                         .Response((int)HttpStatusCode.InternalServerError, r => r.Description("Error interno del servidor"))
                         .BodyParameter(p => p.Description("Datos para enviar a RTDSWrite").Name("body").Schema<RTDSRequest>())
                 )
             );

            //Describe["GetOrderStatus"] = description => description.AsSwagger(
            //    with => with.Operation(
            //        op => op.OperationId("GetOrderStatus")
            //            .Tag("Order")
            //            .Parameter(p => p.Name("NId").Description("Identificador de la orden a consultar").IsRequired().In(ParameterIn.Path))
            //            .Summary("Devuelve el estado de una orden creada asincrónicamente")
            //            .Description("Devuelve el estado de una orden creada asincrónicamente")
            //            .Response((int)HttpStatusCode.NotFound, r => r.Description("No existe la Orden o Work Order asociada"))
            //            .Response((int)HttpStatusCode.Created,r => r.Description("Orden y Work Order asociada creadas correctamente"))
            //            .Response((int)HttpStatusCode.Processing,r => r.Description("Orden y Work Order en proceso."))
            //    )
            //);
        }

    }
}
