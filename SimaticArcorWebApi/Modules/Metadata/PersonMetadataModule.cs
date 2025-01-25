using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
  public class PersonMetadataModule : MetadataModule<PathItem>
  {
    public PersonMetadataModule(ISwaggerModelCatalog modelCatalog)
    {
      //modelCatalog.AddModels(
      //  typeof(CreatePersonRequest)
      //  );
      //Describe["CreatePersonBulkInsert"] = description => description.AsSwagger(
      //   with => with.Operation(
      //       op => op.OperationId("CreatePersonBulkInsert")
      //            .Tag("Personnel")
      //            .Summary("Cargar array de personal")
      //            .Response((int)HttpStatusCode.Created, r => r.Description("Cargar array de personal"))
      //            .BodyParameter(p => p.Description("Array de personal").Name("body").Schema<CreatePersonRequest>())
      //    )
      //);
    }
  }
}
