using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy.Metadata.Modules;
using Nancy.Swagger;
using Nancy.Swagger.Services;
using SimaticArcorWebApi.Model.Custom;
using SimaticArcorWebApi.Model.Custom.Bom;
using SimaticArcorWebApi.Model.Simatic.Material;
using Swagger.ObjectModel;

namespace SimaticArcorWebApi.Modules.Metadata
{
	public class BillOfMaterialsMetadataModule : MetadataModule<PathItem>
	{
		public BillOfMaterialsMetadataModule(ISwaggerModelCatalog modelCatalog)
		{
			modelCatalog.AddModels(
				typeof(BillOfMaterialsRequest), typeof(BillOfMaterialsRequestItem), typeof(BillOfMaterialsRequestProperty), typeof(UomConversionRequest)
			);

			Describe["GetBillOfMaterials"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("GetBillOfMaterials")
						.Tag("Bill Of Materials")
						.Summary("Devuelve todos los BOMs")
						.Description("Devuelve todos BOMs.")
						.Response(200, r => r.Description("OK"))
					//.Response(500, r => r.Description("Error inesperado"))
					//.BodyParameter(p => p.Description("NotifierOperationRequest").Name("body").Schema<NotifierOperationRequest>())
				)
			);

			Describe["PostBillOfMaterials"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("PostBillOfMaterials")
						.Tag("Bill Of Materials")
						.Summary("Crea una BOM")
						.Response((int)HttpStatusCode.Created, r => r.Description("BOM Creada"))
						//.Response(500, r => r.Description("Error inesperado"))
						.BodyParameter(p => p.Description("Material").Name("body").Schema<BillOfMaterialsRequest>())
				)
			);

			Describe["PostBillOfMaterialsBulkInsert"] = description => description.AsSwagger(
				with => with.Operation(
					op => op.OperationId("PostBillOfMaterialsBulkInsert")
						.Tag("Bill Of Materials")
						.Summary("Crea BOM's a partir de un array")
						.Response((int)HttpStatusCode.Created, r => r.Description("BOM Creadas"))
						.BodyParameter(p => p.Description("Array de bill of materials").Name("body").Schema<BillOfMaterialsRequest>())
				)
			);

			//Describe["PostBOMConversionBulkInsert"] = description => description.AsSwagger(
			//	with => with.Operation(
			//		op => op.OperationId("PostBOMConversionBulkInsert")
			//			.Tag("UOM")
			//			.Summary("Crea Uom Conversions a partir de un array")
			//			.Response((int)HttpStatusCode.Created, r => r.Description("Uom conversions Creadas"))
			//			.BodyParameter(p => p.Description("Array de uom conversion").Name("body").Schema<UomConversionRequest>())
			//	)
			//);

			//Describe["PostBOMConversion"] = description => description.AsSwagger(
			//	with => with.Operation(
			//		op => op.OperationId("PostBOMConversion")
			//			.Tag("UOM")
			//			.Summary("Crea una uom conversion")
			//			.Response((int)HttpStatusCode.Created, r => r.Description("Uom conversion Creada"))
			//			.BodyParameter(p => p.Description("Uom conversion").Name("body").Schema<UomConversionRequest>())
			//	)
			//);

		}

	}
}
