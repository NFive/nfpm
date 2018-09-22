using CommandLine;
using Ionic.Zip;
using JetBrains.Annotations;
using NFive.SDK.Plugins.Configuration;
using NFive.SDK.Plugins.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Install and configure a new FiveM server with NFive installed.
	/// </summary>
	[UsedImplicitly]
	[Verb("setup", HelpText = "Install and configure a new FiveM server with NFive installed.")]
	internal class Setup
	{
		[Option("servername", Required = false, HelpText = "Set server name.")]
		public string ServerName { get; set; } = null;

		[Option("maxplayers", Required = false, HelpText = "Set server max players.")]
		public ushort? MaxPlayers { get; set; } = null;

		[Option("tags", Required = false, HelpText = "Set server tags.")]
		public string Tags { get; set; } = null;

		[Option("licensekey", Required = false, HelpText = "Set server license key.")]
		public string LicenseKey { get; set; } = null;

		[Option("db-host", Required = false, HelpText = "Set database host.")]
		public string DatabaseHost { get; set; } = null;

		[Option("db-port", Required = false, HelpText = "Set database port.")]
		public int? DatabasePort { get; set; } = null;

		[Option("db-user", Required = false, HelpText = "Set database user.")]
		public string DatabaseUser { get; set; } = null;

		[Option("db-password", Required = false, HelpText = "Set database password.")]
		public string DatabasePassword { get; set; } = null;

		[Option("db-name", Required = false, HelpText = "Set database name.")]
		public string DatabaseName { get; set; } = null;

		internal async Task<int> Main()
		{
			Console.WriteLine("This utility will walk you through setting up a brand new FiveM server with NFive installed.");
			Console.WriteLine();
			Console.WriteLine("If you already have FiveM server installed you should cancel and use `nfpm init`.");
			Console.WriteLine();
			Console.WriteLine("Press ^C at any time to quit.");
			Console.WriteLine();

			Console.WriteLine("Server Configuration...");

			var config = new ConfigGenerator
			{
				Hostname = string.IsNullOrWhiteSpace(this.ServerName) ? Input.String("server name", "NFive") : this.ServerName,
				MaxPlayers = this.MaxPlayers ?? Convert.ToUInt16(Input.Int("server max players", 1, 32, 32)),
				Tags = (string.IsNullOrWhiteSpace(this.Tags) ? Input.String("server tags (separate with space)", "nfive") : this.Tags).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
				LicenseKey = string.IsNullOrWhiteSpace(this.LicenseKey) ? Input.String("server license key (https://keymaster.fivem.net/)", s =>
				{
					if (Regex.IsMatch(s, @"[\d\w]{32}")) return true;

					Console.Write("Please enter a valid license key: ");

					return false;
				}).ToLowerInvariant() : this.LicenseKey
			};

			Console.WriteLine();
			Console.WriteLine("Database Configuration...");

			var dbHost = string.IsNullOrWhiteSpace(this.DatabaseHost) ? Input.String("database host", "localhost") : this.DatabaseHost;
			var dbPort = this.DatabasePort ?? Input.Int("database port", 1, ushort.MaxValue, 3306);
			var dbUser = string.IsNullOrWhiteSpace(this.DatabaseUser) ? Input.String("database user", "root") : this.DatabaseUser;
			var dbPass = string.IsNullOrWhiteSpace(this.DatabasePassword) ? Regex.Replace(Input.String("database password", "<blank>"), "^<blank>$", string.Empty) : this.DatabasePassword;
			var dbName = string.IsNullOrWhiteSpace(this.DatabaseHost) ? Input.String("database name", "fivem", s =>
			{
				if (Regex.IsMatch(s, "^[^\\/?%*:|\"<>.]{1,64}$")) return true;

				Console.Write("Please enter a valid database name: ");

				return false;
			}) : this.DatabaseHost;

			// TODO: Include stock plugins

			Console.WriteLine();

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

				data = await client.DownloadDataTaskAsync("https://ci.appveyor.com/api/projects/NFive/nfive/artifacts/nfive.zip?branch=master");

				Console.WriteLine("Installing NFive...");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "resources", "nfive"));
					zip.ExtractAll(Path.Combine(Environment.CurrentDirectory, "resources", "nfive"), ExtractExistingFileAction.OverwriteSilently);
				}
			}

			config.Serialize(Path.Combine(Environment.CurrentDirectory, PathManager.ConfigFile));
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "resources", "nfive", ConfigurationManager.DefinitionFile), Yaml.Serialize(new Definition
			{
				Name = "local/nfive-install",
				Version = "1.0.0"
			}));

			var dbYml = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "resources", "nfive", "config", "database.yml"));
			dbYml = Regex.Replace(dbYml, "(\\s*host\\: ).+", $"${{1}}{dbHost}");
			dbYml = Regex.Replace(dbYml, "(\\s*port\\: ).+", $"${{1}}{dbPort}");
			dbYml = Regex.Replace(dbYml, "(\\s*database\\: ).+", $"${{1}}{dbName}");
			dbYml = Regex.Replace(dbYml, "(\\s*user\\: ).+", $"${{1}}{dbUser}");
			dbYml = Regex.Replace(dbYml, "(\\s*password\\: ).+", $"${{1}}{dbPass}");
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "resources", "nfive", "config", "database.yml"), dbYml);

			Console.WriteLine();
			Console.WriteLine("Installation is complete, you can now start the server with `nfpm start`!");

			return await Task.FromResult(0);
		}
	}
}
