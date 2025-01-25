using System;

namespace SimaticArcorWebApi.Model.Custom.RoadMap
{
  public class RoadMapRequestOperations
  {
    public string Id { get; set; }
    public string Level { get; set; }
    public string Operation { get; set; }
    public double? Theoretical { get; set; }
    public double? Crew { get; set; }
    public double? MachineHs { get; set; }
    public double? ManHs { get; set; }
    public double? KgHs { get; set; }
    public double? KgHsMach { get; set; }
    public double? Efficiency { get; set; }
    public double? Costs { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public double? Vapor { get; set; }
    public double? Gas { get; set; }
    public double? Energia { get; set; }
    public double? GPM { get; set; }
    public double? PPU { get; set; }
    public double? UPG { get; set; }
    public double? CDB { get; set; }
    public double? GPMD { get; set; }
    public string ERPID{ get; set; }
    }
}