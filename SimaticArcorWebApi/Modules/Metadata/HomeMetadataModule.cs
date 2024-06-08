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
	public class HomeMetadataModule : MetadataModule<PathItem>
	{
		public HomeMetadataModule(ISwaggerModelCatalog modelCatalog)
		{
			//modelCatalog.AddModels(
			//	typeof(MaterialRequest), typeof(MaterialRequestProperty)
			//);
			
			//Describe["PostCustomOperation"] = description => description.AsSwagger(
			//	with => with.Operation(
			//		op => op.OperationId("PostCustomOperation")
			//			.Tag("Home")
			//			.IsDeprecated()
			//			.Summary("Custom operation call")
			//			.Response((int)HttpStatusCode.OK, r => r.Description("OK"))
			//			.BodyParameter(p => p.Description("Data").Name("body").Schema<string>())
			//	)
			//);
			
		}

	}
}
