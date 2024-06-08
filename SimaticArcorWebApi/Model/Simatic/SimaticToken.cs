using Newtonsoft.Json;

namespace SimaticArcorWebApi.Model.Simatic
{
	public class SimaticToken
	{
		[JsonProperty(PropertyName = "access_token")]
		public string AccessToken { get; set; }

		[JsonProperty(PropertyName = "expires_in")]
		public int ExpiresIn { get; set; }

		[JsonProperty(PropertyName = "token_type")]
		public string TokenType { get; set; }

		[JsonProperty(PropertyName = "refresh_token")]
		public object RefreshToken { get; set; }
	}
}