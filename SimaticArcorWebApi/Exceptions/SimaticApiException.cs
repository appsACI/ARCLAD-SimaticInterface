using System;
using System.Net.Http;
using Nancy;

namespace SimaticArcorWebApi.Exceptions
{
	public class SimaticApiException : BaseException
	{
		public HttpStatusCode StatusCode { get; set; }

		public dynamic Result { get; set; }

		public override string Message => this.ToString();

    public SimaticApiException(dynamic result)
    {
      Result = result;
      StatusCode = HttpStatusCode.BadRequest;
    }

    public SimaticApiException(HttpStatusCode statusCode, dynamic result)
		{
			Result = result;
			StatusCode = statusCode;
		}
		
		public override string ToString()
		{
            if (Result is string)
            return Result;

            if (Result?.Error is string)
				return Result.Error;

            if (Result?.Error?.ErrorMessage != null)
				return Result?.Error?.ErrorMessage;
    
            if (Result?.error?.message != null)
				return Result?.error?.message.ToString();

			return Result;
		}
	}
}