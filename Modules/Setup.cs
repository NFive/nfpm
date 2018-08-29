using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Ionic.Zip;
using JetBrains.Annotations;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Models.Plugin;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update installed NFive plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("setup", HelpText = "Install and configure a brand new FiveM server with NFive installed.")]
	internal class Setup
	{
		internal async Task<int> Main()
		{
			Console.WriteLine($"This utility will walk you through setting up a brand new FiveM server with NFive installed.");
			Console.WriteLine();
			Console.WriteLine($"If you already have FiveM server installed you should cancel and use `nfpm init`.");
			Console.WriteLine();
			Console.WriteLine("Press ^C at any time to quit.");
			Console.WriteLine();

			var config = new ConfigGenerator();

			config.Hostname = ParseSimple("server name", "NFive");
			var serverMaxPlayers = ParseSimple("server max players", "32");
			config.Tags = ParseSimple("server tags (separate with space)", "nfive").Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList();
			config.LicenseKey = ParseSimple("server license key (https://keymaster.fivem.net/)", "<skip>");

			Console.WriteLine();

			Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "server", "resources", "nfive"));

			config.Serialize().Save(Path.Combine(Environment.CurrentDirectory, "server", "server.cfg"));

			var definition = new Definition
			{
				Name = "local/nfive-install",
				Version = "1.0.0"
			};

			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "server", "resources", "nfive", "nfive.yml"), Yaml.Serialize(definition));

			using (var client = new WebClient())
			{
				Console.WriteLine("Downloading FiveM server v744...");

				var data = await client.DownloadDataTaskAsync("https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/744-36dd6e2d1a0521e195d4c2c612a3a58e6208068b/server.zip");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(Path.Combine(Environment.CurrentDirectory, "server"), ExtractExistingFileAction.OverwriteSilently);
				}

				Console.WriteLine("Installing FiveM server...");
				Console.WriteLine();
				Console.WriteLine("Downloading NFive...");

				data = await client.DownloadDataTaskAsync("https://ci.appveyor.com/api/projects/NFive/nfive/artifacts/nfive.zip");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(Path.Combine(Environment.CurrentDirectory, "server", "resources", "nfive"), ExtractExistingFileAction.OverwriteSilently);
				}

				Console.WriteLine("Installing NFive...");
			}

			Console.WriteLine();
			Console.WriteLine("Installation is complete, you can now start the server with `nfpm start`!");

			return await Task.FromResult(0);
		}

		private static string ParseSimple(string description, string defaultValue)
		{
			Console.Write($"{description}: ({defaultValue}) ");

			var input = Console.ReadLine()?.Trim();

			return !string.IsNullOrEmpty(input) ? input : defaultValue;
		}
	}
}
