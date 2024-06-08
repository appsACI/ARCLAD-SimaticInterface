using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Simatic
{
	public class AccessToken
	{
		public DateTime ExpirationDate { get; set; }

		public string Value { get; set; }
	}
}
