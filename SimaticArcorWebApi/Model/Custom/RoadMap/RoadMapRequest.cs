namespace SimaticArcorWebApi.Model.Custom.RoadMap
{
    public class RoadMapRequest
    {
        public string Id { get; set; }
        public string Plant { get; set; }
        public string Type { get; set; }
        public int QuantityValue { get; set; }
        public string UoMNId { get; set; }

        public RoadMapRequestOperations[] Operations { get; set; }
    }
}