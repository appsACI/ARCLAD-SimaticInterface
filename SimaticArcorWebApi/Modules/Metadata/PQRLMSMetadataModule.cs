using System.Net;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.Order;
using SimaticArcorWebApi.Model.Simatic.WorkOrder;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
    public class PQRLMSMetadataModule : MetadataModule<PathItem>
    {
        public PQRLMSMetadataModule(ISwaggerModelCatalog modelCatalog)
        {
            modelCatalog.AddModels(
                typeof(PQRLMSRequest),
                typeof(PQRLMSResponse),
                typeof(IncommingMessage)
            );

            Describe["CreatePQRLMSInsert"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreatePQRLMSInsert")
                        .Tag("PQR_LMS")
                        .Summary("Crear un caso en el Netsuit")
                        .Response((int)HttpStatusCode.Accepted, r => r.Description("Crear un caso en el Netsuit").Schema<PQRLMSResponse>())
                        .BodyParameter(p => p.Description("PQRLMSRequest").Name("body").Schema<PQRLMSRequest>())
                )
            );

            
        }

    }
}
