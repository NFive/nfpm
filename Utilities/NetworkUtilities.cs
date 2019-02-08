using System.Net;

namespace NFive.PluginManager.Utilities
{
	internal static class NetworkUtilities
	{
		public static void ConfigureSupportedSecurityProtocols()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		}

		public static void SetConnectionLimit()
		{
			// Increase the maximum number of connections per server, except on Mono
			ServicePointManager.DefaultConnectionLimit = !RuntimeEnvironment.IsMono ? 64 : 1;
		}
	}
}
