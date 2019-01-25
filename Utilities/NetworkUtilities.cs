using System;
using System.Net;

namespace NFive.PluginManager.Utilities
{
	public static class NetworkUtilities
	{
		public static void ConfigureSupportedSslProtocols()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		}

		public static void SetConnectionLimit()
		{
			// Increase the maximum number of connections per server.
			if (!RuntimeEnvironment.IsMono)
			{
				ServicePointManager.DefaultConnectionLimit = 64;
			}
			else
			{
				// Keep mono limited to a single download to avoid issues.
				ServicePointManager.DefaultConnectionLimit = 1;
			}
		}
	}
}
