
using Microsoft.Extensions.Logging;

namespace Endor.Core.Logging
{
	public class ApplicationLogging
	{
		private static ILoggerFactory _Factory = null;

		public static ILoggerFactory LoggerFactory
		{
			get => _Factory;
			set { _Factory = value; }
		}

		public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
	}
}