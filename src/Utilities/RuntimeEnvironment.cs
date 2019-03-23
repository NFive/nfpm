using System;

namespace NFive.PluginManager.Utilities
{
	internal static class RuntimeEnvironment
	{
		private static readonly Lazy<bool> Mono = new Lazy<bool>(() => Type.GetType("Mono.Runtime") != null);

		public static bool IsWindows
		{
			get
			{
				var platform = (int)Environment.OSVersion.Platform;
				return platform != 4 && platform != 6 && platform != 128;
			}
		}

		public static bool IsMono => Mono.Value;

		public static bool IsLinux => (int)Environment.OSVersion.Platform == 4;
	}
}
