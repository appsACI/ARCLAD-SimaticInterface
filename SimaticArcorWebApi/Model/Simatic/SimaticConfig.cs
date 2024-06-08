using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Model.Simatic
{
	public class SimaticConfig
	{
		public string Url { get; set; }
		public string GrantType { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Scope { get; set; }
		public int? Timeout  { get; set; }
	}
}
