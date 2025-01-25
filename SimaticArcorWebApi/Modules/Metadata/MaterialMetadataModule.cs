using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Simatic.Material;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
	public class MaterialMetadataModule : MetadataModule<PathItem>
	{
		public MaterialMetadataModule(ISwaggerModelCatalog modelCatalog)
		{
			modelCatalog.AddModels(
				typeof(MaterialRequest), typeof(MaterialRequestProperty)
			);
			
			Describe["GetMaterial"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetMaterial")
						.Tag("Material")
						.Summary("Devuelve todos los materials")
						.Description("Devuelve todos los materials.")
						.Response(200, r => r.Description("OK"))
				)
			);

			Describe["GetMaterialById"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetMaterialById")
						.Tag("Material")
						.Parameter(p => p.Name("Id").Description("Identificador del material").IsRequired().In(ParameterIn.Path))
						.Summary("Devuelve un material según el Id")
						.Description("Devuelve un material según el Id")
						.Response(200, r => r.Description("OK"))
				)
			);

			Describe["GetClass"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetClass")
						.Tag("Material")
						.Summary("Devuelve todos los material class")
						.Description("Devuelve todos los material class en simatic.")
						.Response(200, r => r.Description("OK"))
					)
			);

			Describe["GetClassById"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetClassById")
						.Tag("Material")
						.Parameter(p => p.Name("Id").Description("Identificador del material class").IsRequired().In(ParameterIn.Path))
						.Summary("Devuelve un material class según el Id")
						.Description("Devuelve un material class según el Id")
						.Response(200, r => r.Description("OK"))
				)
			);

			Describe["GetDefinition"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetDefinition")
						.Tag("Material")
						.Summary("Devuelve todos los material definition")
						.Description("Devuelve todos los material definition en simatic.")
						.Response(200, r => r.Description("OK"))
				)
			);

			Describe["GetDefinitionById"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetDefinitionById")
						.Tag("Material")
						.Parameter(p => p.Name("Id").Description("Identificador del material definition").IsRequired().In(ParameterIn.Path))
						.Summary("Devuelve un material definition según el Id")
						.Description("Devuelve un material definition según el Id")
						.Response(200, r => r.Description("OK"))
				)
			);

			Describe["PostMaterial"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("PostMaterial")
						.Tag("Material")
						.Summary("Crea un material con las propiedades correspondiente")
						.Response((int)HttpStatusCode.Created, r => r.Description("Material creado"))
						.BodyParameter(p => p.Description("Material").Name("body").Schema<MaterialRequest>())
				)
			);

			Describe["PostMaterialBulkInsert"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("PostMaterialBulkInsert")
						.Tag("Material")
						.Summary("Crea materiales a partir de un array")
						.Response((int)HttpStatusCode.Created, r => r.Description("Los Material fueron creados"))
						.BodyParameter(p => p.Description("Array de materiales").Name("body").Schema<MaterialRequest>())
				)
			);
			
		}

	}
}
