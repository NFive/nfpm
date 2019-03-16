using System;
using System.IO;
using System.Linq;
using NFive.SDK.Plugins.Configuration;

namespace NFive.PluginManager.Utilities
{
	public static class PathManager
	{
		/// <summary>
		/// The name of the Linux FiveM server binary file.
		/// </summary>
		public const string ServerFileLinux = "FXServer";

		/// <summary>
		/// The name of the Windows FiveM server binary file.
		/// </summary>
		public const string ServerFileWindows = ServerFileLinux + ".exe";

		/// <summary>
		/// The name of the FiveM server configuration file.
		/// </summary>
		public const string ConfigFile = "server.cfg";

		// TODO: Move to ConfigurationManager
		public static string CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache", ConfigurationManager.PluginPath);


		/// <summary>
		/// Finds the FiveM server binary in the current directory tree.
		/// </summary>
		/// <remarks>Searches 10 directories up from the current directory.</remarks>
		/// <returns>Full path to the FiveM server directory.</returns>
		/// <exception cref="FileNotFoundException">Unable to locate FiveM server in the directory tree.</exception>
		public static string FindServer()
		{
			var osPath = RuntimeEnvironment.IsWindows ? "." : Path.Combine("alpine", "opt", "cfx-server");

			for (var i = 0; i < 10; i++)
			{
				var path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, osPath, string.Concat(Enumerable.Repeat($"..{Path.DirectorySeparatorChar}", i))));

				if (File.Exists(Path.Combine(path, RuntimeEnvironment.IsWindows ? ServerFileWindows : ServerFileLinux))) return path;
			}

			throw new FileNotFoundException("Unable to locate FiveM server in the directory tree.", RuntimeEnvironment.IsWindows ? ServerFileWindows : ServerFileLinux);
		}

		public static string FindResource()
		{
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.LockFile))) return Environment.CurrentDirectory;
			if (File.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.DefinitionFile))) return Environment.CurrentDirectory;

			try
			{
				var path = Path.Combine(FindServer(), "resources", "nfive");

				if (Directory.Exists(path)) return path;
			}
			catch (FileNotFoundException ex)
			{
				throw new DirectoryNotFoundException("Unable to locate resource in the directory tree.", ex);
			}
			
			throw new DirectoryNotFoundException("Unable to locate resource in the directory tree.");
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
