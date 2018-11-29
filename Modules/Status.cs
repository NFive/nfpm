using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NFive.SDK.Plugins.Configuration;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Show the status of the current directory.
	/// </summary>
	[UsedImplicitly]
	[Verb("status", HelpText = "Show the status of the current directory.")]
	internal class Status
	{
		internal async Task<int> Main()
		{
			var cd = Path.GetFullPath(Environment.CurrentDirectory);
			Console.WriteLine($"Current directory: {cd}");

			Console.WriteLine();
			
			// NFPM

			var nfpm = Process.GetCurrentProcess().MainModule;

			Console.WriteLine("NFPM:");
			Console.WriteLine($"-- Path: {Relative(Path.GetDirectoryName(nfpm.FileName), cd)}");
			Console.WriteLine($"-- Binary: {Relative(nfpm.FileName, cd)}");
			Console.WriteLine($"-- Version: {nfpm.FileVersionInfo.FileVersion}");

			Console.WriteLine();

			// FIVEM

			var server = FindServer();

			Console.Write("FIVEM:");

			if (string.IsNullOrWhiteSpace(server))
			{
				Console.WriteLine(" NOT FOUND");
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine($"-- Path: {Relative(server, cd)}");

				var binary = Path.Combine(server, "FXServer.exe");
				Console.Write("-- Binary: ");
				Console.WriteLine(File.Exists(binary) ? Relative(binary, cd) : "MISSING");

				var version = GetServerVersion(server);
				Console.Write("-- Version: ");
				Console.WriteLine(!string.IsNullOrWhiteSpace(version) ? version : "UNKNOWN");

				var config = Path.Combine(server, "server.cfg");
				Console.Write("-- Config: ");
				Console.WriteLine(File.Exists(config) ? Relative(config, cd) : "MISSING");
			}

			Console.WriteLine();

			// NFIVE

			var nfive = FindResource();

			Console.Write("NFIVE:");

			if (string.IsNullOrWhiteSpace(nfive))
			{
				Console.WriteLine(" NOT FOUND");
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine($"-- Path: {Relative(nfive, cd)}");

				var version = AssemblyName.GetAssemblyName(Path.Combine(nfive, "NFive.Server.net.dll")).Version;
				Console.WriteLine($"-- Version: {version}");

				var definition = Path.Combine(nfive, ConfigurationManager.DefinitionFile);
				Console.Write("-- Definition: ");
				Console.WriteLine(File.Exists(definition) ? Relative(definition, cd) : "MISSING");

				var @lock = Path.Combine(nfive, ConfigurationManager.LockFile);
				Console.Write("-- Lock: ");
				Console.WriteLine(File.Exists(@lock) ? Relative(@lock, cd) : "MISSING");
			}

			return await Task.FromResult(0);
		}

		private string FindServer()
		{
			try
			{
				return PathManager.FindServer();
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		private string FindResource()
		{
			try
			{
				return PathManager.FindResource();
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		private string GetServerVersion(string serverPath)
		{
			try
			{
				return File.ReadAllText(Path.Combine(serverPath, "version"));
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		private string Relative(string path, string cd)
		{
			var res = path.TrimStart(cd);

			if (string.IsNullOrEmpty(res))
			{
				return @".\";
			}
			else
			{
				if (res == path)
				{
					return res;
				}

				return "." + res;
			}
		}
	}
}
