namespace SimaticArcorWebApi.Model.Custom.Bom
{
	public class UomConversionRequest
	{
		public string Item { get; set; }
		public string DestinationUom { get; set; }
		public double? Factor { get; set; }
	}
}