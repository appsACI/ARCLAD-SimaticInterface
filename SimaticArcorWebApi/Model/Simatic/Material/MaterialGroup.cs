﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Simatic.Material
{
	public class MaterialGroup
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
		public string Name { get; set; }
		public object Description { get; set; }
	}
}
