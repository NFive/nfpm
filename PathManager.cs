using System;
using System.IO;
using System.Linq;

namespace NFive.PluginManager
{
	public static class PathManager
	{
		public static readonly string ServerFile = "FXServer.exe";
		public static readonly string ConfigFile = "server.cfg";

		public static string FindServer()
		{
			for (var i = 0; i < 10; i++)
			{
				var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, string.Concat(Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", i))));

				if (File.Exists(Path.Combine(path, ServerFile))) return path;
			}

			throw new FileNotFoundException("Unable to locate server in the directory tree", ServerFile);
		}

		public static string FindResource()
		{
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, "nfive.lock"))) return Environment.CurrentDirectory;
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, "nfive.yml"))) return Environment.CurrentDirectory;

			var server = FindServer();

			var path = Path.Combine(server, "resources", "nfive");

			if (Directory.Exists(path)) return path;

			throw new DirectoryNotFoundException("Unable to locate resource in the directory tree");
		}

		public static bool IsResource()
		{
			if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "nfive.lock"))) return false;
			if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "nfive.yml"))) return false;

			return File.Exists(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"..{Path.DirectorySeparatorChar}", $"..{Path.DirectorySeparatorChar}", ServerFile)));
		}
	}
}
