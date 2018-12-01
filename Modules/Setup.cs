using CommandLine;
using Ionic.Zip;
using JetBrains.Annotations;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Console = Colorful.Console;
using Version = NFive.SDK.Core.Plugins.Version;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Install and configure a new FiveM server with NFive installed.
	/// </summary>
	[UsedImplicitly]
	[Verb("setup", HelpText = "Install and configure a new FiveM server with NFive installed.")]
	internal class Setup
	{
		[Option("fivem", Required = false, HelpText = "Install FiveM server.")]
		public bool? FiveM { get; set; } = null;

		[Option("nfive", Required = false, HelpText = "Install NFive.")]
		public bool? NFive { get; set; } = null;

		[Option("servername", Required = false, HelpText = "Set server name.")]
		public string ServerName { get; set; } = null;

		[Option("maxplayers", Required = false, HelpText = "Set server max players.")]
		public ushort? MaxPlayers { get; set; } = null;

		[Option("tags", Required = false, HelpText = "Set server tags.")]
		public string Tags { get; set; } = null;

		[Option("licensekey", Required = false, HelpText = "Set server license key.")]
		public string LicenseKey { get; set; } = null;

		[Option("rcon-password", Required = false, HelpText = "Set RCON password.")]
		public string RconPassword { get; set; } = null;

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

		[Value(0, Default = "server", Required = false, HelpText = "Path to install server at.")]
		public string Location { get; set; }

		internal async Task<int> Main()
		{
			this.Location = Path.GetFullPath(this.Location);

			Console.WriteLine("This utility will walk you through setting up a new FiveM server with NFive installed.");
			Console.WriteLine();
			Console.WriteLine($"The server will be installed at {this.Location}");
			Console.WriteLine();
			Console.WriteLine("Press Ctrl+C at any time to quit.");
			Console.WriteLine();

			if (this.FiveM.HasValue && this.FiveM.Value || !this.FiveM.HasValue && Input.Bool("Install FiveM server?", true))
			{
				if (!this.FiveM.HasValue) Console.WriteLine();
				Console.WriteLine("FiveM server configuration...");

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
					}).ToLowerInvariant() : this.LicenseKey,
					RconPassword = string.IsNullOrWhiteSpace(this.RconPassword) ? Regex.Replace(Input.String("RCON password", "<disabled>"), "^<disabled>$", string.Empty) : this.RconPassword
				};

				Directory.CreateDirectory(this.Location);

				config.Serialize(Path.Combine(this.Location, PathManager.ConfigFile));

				if (!this.FiveM.HasValue) Console.WriteLine();

				await InstallFiveM(this.Location);

				this.Location = Path.Combine(this.Location, "resources", "nfive");
			}

			if (this.NFive.HasValue && this.NFive.Value || !this.NFive.HasValue && Input.Bool("Install NFive?", true))
			{
				if (!this.FiveM.HasValue) Console.WriteLine();
				Console.WriteLine("NFive database configuration...");

				var dbHost = string.IsNullOrWhiteSpace(this.DatabaseHost) ? Input.String("database host", "localhost") : this.DatabaseHost;
				var dbPort = this.DatabasePort ?? Input.Int("database port", 1, ushort.MaxValue, 3306);
				var dbUser = string.IsNullOrWhiteSpace(this.DatabaseUser) ? Input.String("database user", "root") : this.DatabaseUser;
				var dbPass = string.IsNullOrWhiteSpace(this.DatabasePassword) ? Regex.Replace(Input.String("database password", "<blank>"), "^<blank>$", string.Empty) : this.DatabasePassword;
				var dbName = string.IsNullOrWhiteSpace(this.DatabaseName) ? Input.String("database name", "fivem", s =>
				{
					if (Regex.IsMatch(s, "^[^\\/?%*:|\"<>.]{1,64}$")) return true;

					Console.Write("Please enter a valid database name: ");

					return false;
				}) : this.DatabaseName;

				if (!this.FiveM.HasValue) Console.WriteLine();

				await InstallNFive(this.Location);

				File.WriteAllText(Path.Combine(this.Location, ConfigurationManager.DefinitionFile), Yaml.Serialize(new
				{
					Name = "local/nfive-install",
					Version = new Version
					{
						Major = 1,
						Minor = 0,
						Patch = 0
					}
				}));

				var dbYml = File.ReadAllText(Path.Combine(this.Location, "config", "database.yml"));
				dbYml = Regex.Replace(dbYml, "(\\s*host\\: ).+", $"${{1}}{dbHost}");
				dbYml = Regex.Replace(dbYml, "(\\s*port\\: ).+", $"${{1}}{dbPort}");
				dbYml = Regex.Replace(dbYml, "(\\s*database\\: ).+", $"${{1}}{dbName}");
				dbYml = Regex.Replace(dbYml, "(\\s*user\\: ).+", $"${{1}}{dbUser}");
				dbYml = Regex.Replace(dbYml, "(\\s*password\\: ).+", $"${{1}}{dbPass}");
				File.WriteAllText(Path.Combine(this.Location, "config", "database.yml"), dbYml);

				// TODO: Ask to include stock plugins
			}

			Console.WriteLine("Installation is complete, you can now start the server with `nfpm start`!");

			return 0;
		}

		private static async Task InstallFiveM(string path)
		{
			Console.WriteLine("Finding latest FiveM Windows server version...");

			using (var client = new WebClient())
			{
				var page = await client.DownloadStringTaskAsync("https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/");
				var regex = new Regex("href=\"(\\d{3})-([^\"]*)/\"", RegexOptions.IgnoreCase);
				var versions = new List<Tuple<string, string>>();
				for (var match = regex.Match(page); match.Success; match = match.NextMatch())
				{
					versions.Add(new Tuple<string, string>(match.Groups[1].Value, match.Groups[2].Value));
				}

				var latest = versions.Max();

				Console.WriteLine($"Downloading FiveM server v{latest.Item1}...");

				var data = await client.DownloadDataTaskAsync($"https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/{latest.Item1}-{latest.Item2}/server.zip");

				Console.WriteLine("Installing FiveM server...");

				Directory.CreateDirectory(path);

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
				}

				File.WriteAllText(Path.Combine(path, "version"), latest.Item1);
			}

			Console.WriteLine();
		}

		private static async Task InstallNFive(string path)
		{
			Console.WriteLine("Downloading NFive...");

			using (var client = new WebClient())
			{
				var data = await client.DownloadDataTaskAsync("https://ci.appveyor.com/api/projects/NFive/nfive/artifacts/nfive.zip?branch=master");

				Console.WriteLine("Installing NFive...");

				Directory.CreateDirectory(path);

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
				}
			}

			Console.WriteLine();
		}
	}
}
