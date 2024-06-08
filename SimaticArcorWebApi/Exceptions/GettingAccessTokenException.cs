using System;

namespace SimaticArcorWebApi.Exceptions
{
	public class GettingAccessTokenException : BaseException
	{
		private string message;

		public GettingAccessTokenException(string message)
		{
			this.message = message;
		}

		public override string Message => this.ToString();

		public override string ToString()
		{
			return string.Format("Error getting access token. Message: {0}", message);
		}
	}
}