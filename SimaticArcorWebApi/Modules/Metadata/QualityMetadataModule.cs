using Nancy;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using SimaticArcorWebApi.Model.Custom.Quality;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
  public class QualityMetadataModule : MetadataModule<PathItem>
    {
        public QualityMetadataModule(ISwaggerModelCatalog modelCatalog)
        {
            modelCatalog.AddModels(
                typeof(SampleRequestProperty),
                typeof(ProductSpecificationRequestProperty),
              typeof(CreateSampleRequest),
              typeof(CreateProductSpecificationRequest)
              );

            Describe["CreateSample"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateSample")
                        .Tag("Quality")
                        .Summary("Crea una muestra de calidad")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Crea una muestra de calidad para un determinado lote, con ciertas propiedades"))
                        .BodyParameter(p => p.Description("CreateSampleRequest").Name("body").Schema<CreateSampleRequest>())
                )
            );

            Describe["CreateProductSpecification"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateProductSpecification")
                        .Tag("Quality")
                        .Summary("Crea una especificación de producto")
                        .Response((int)HttpStatusCode.Created, r => r.Description("Crea una especificación de producto con ciertas propiedades"))
                        .BodyParameter(p => p.Description("CreateProductSpecificationRequest").Name("body").Schema<CreateProductSpecificationRequest>())
                )
            );

            Describe["CreateProductSpecificationBulkInsert"] = description => description.AsSwagger(
                with => with.Operation(
                    op => op.OperationId("CreateProductSpecificationBulkInsert")
                        .Tag("Quality")
                        .Summary("Crea product specification a partir de un array")
                        .Response((int)HttpStatusCode.Created, r => r.Description("product specification Creadas"))
                        .BodyParameter(p => p.Description("Array de bill of product specification").Name("body").Schema<CreateProductSpecificationRequest>())
                )
            );
        }
    }
}
