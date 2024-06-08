using System;

namespace SimaticArcorWebApi.Model.Simatic.Material
{
	public class MaterialProperties
	{
		public string Id { get; set; }
		public string AId { get; set; }
		public bool IsFrozen { get; set; }
		public int ConcurrencyVersion { get; set; }
		public int IsDeleted { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime LastUpdatedOn { get; set; }
		public string EntityType { get; set; }
		public string OptimisticVersion { get; set; }
		public object ConcurrencyToken { get; set; }
		public bool IsLocked { get; set; }
		public bool ToBeCleaned { get; set; }
		public string NId { get; set; }
		public object PropertyValue { get; set; }
		public object PropertyUoMNId { get; set; }
		public string PropertyType { get; set; }
		public string Material_Id { get; set; }
	}
}