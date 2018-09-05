using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Ionic.Zip;
using JetBrains.Annotations;
using NFive.SDK.Plugins.Configuration;
using NFive.SDK.Plugins.Models;
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
			Console.WriteLine("This utility will walk you through setting up a brand new FiveM server with NFive installed.");
			Console.WriteLine();
			Console.WriteLine("If you already have FiveM server installed you should cancel and use `nfpm init`.");
			Console.WriteLine();
			Console.WriteLine("Press ^C at any time to quit.");
			Console.WriteLine();

			var config = new ConfigGenerator();

			config.Hostname = ParseSimple("server name", "NFive");
			var serverMaxPlayers = ParseSimple("server max players", "32");
			config.Tags = ParseSimple("server tags (separate with space)", "nfive").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			config.LicenseKey = ParseSimple("server license key (https://keymaster.fivem.net/)", "<skip>");

			Console.WriteLine();

			Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "resources", "nfive"));

			var definition = new Definition
			{
				Name = "local/nfive-install",
				Version = "1.0.0"
			};

			using (var client = new WebClient())
			{
				Console.WriteLine("Finding latest FiveM Windows server version...");

				var yml = await client.DownloadStringTaskAsync("https://nfive.io/fivem/server-versions-windows.yml");
				var versions = Yaml.Deserialize<List<string>>(yml);
				var latest = versions.First();

				Console.WriteLine($"Downloading FiveM server v{latest.Split(new[] { '-' }, 2)[0]}...");

				var data = await client.DownloadDataTaskAsync($"https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/{latest}/server.zip");

				Console.WriteLine("Installing FiveM server...");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(Environment.CurrentDirectory, ExtractExistingFileAction.OverwriteSilently);
				}

				Console.WriteLine();
				Console.WriteLine("Downloading NFive...");

				data = await client.DownloadDataTaskAsync("https://ci.appveyor.com/api/projects/NFive/nfive/artifacts/nfive.zip");

				Console.WriteLine("Installing NFive...");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(Path.Combine(Environment.CurrentDirectory, "resources", "nfive"), ExtractExistingFileAction.OverwriteSilently);
				}
			}

			config.Serialize().Save(Path.Combine(Environment.CurrentDirectory, "server.cfg"));
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "resources", "nfive", "nfive.yml"), Yaml.Serialize(definition));

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
