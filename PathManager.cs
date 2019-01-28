using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.Linq;
using NFive.PluginManager.Utilities;

namespace NFive.PluginManager
{
	public static class PathManager
	{
		public static readonly string ServerFileWindows = "FXServer.exe";
		public static readonly string ServerFileLinux = "FXServer";
		public static readonly string ConfigFile = "server.cfg";

		public static string FindServer()
		{
			for (var i = 0; i < 10; i++)
			{
				var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, string.Concat(Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", i))));
				if (!RuntimeEnvironment.IsWindows) path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "alpine", "opt", "cfx-server", string.Concat(Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", i))));

				if (File.Exists(Path.Combine(path, RuntimeEnvironment.IsWindows ? ServerFileWindows : ServerFileLinux))) return path;
			}

			throw new FileNotFoundException("Unable to locate server in the directory tree", RuntimeEnvironment.IsWindows ? ServerFileWindows : ServerFileLinux);
		}

		public static string FindResource()
		{
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.LockFile))) return Environment.CurrentDirectory;
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.DefinitionFile))) return Environment.CurrentDirectory;

			var server = FindServer();

			var path = Path.Combine(server, "resources", "nfive");

			if (Directory.Exists(path)) return path;

			throw new DirectoryNotFoundException("Unable to locate resource in the directory tree");
		}

		public static bool IsResource()
		{
			if (!File.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.LockFile))) return false;

			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (!File.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.DefinitionFile))) return false;

			return File.Exists(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"..{Path.DirectorySeparatorChar}", $"..{Path.DirectorySeparatorChar}", RuntimeEnvironment.IsWindows ? ServerFileWindows : ServerFileLinux)));
		}
	}
}
