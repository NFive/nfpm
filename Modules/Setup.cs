using CommandLine;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using NFive.SDK.Plugins.Configuration;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Version = NFive.SDK.Core.Plugins.Version;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Install and configure a new FiveM server with NFive installed.
	/// </summary>
	[Verb("setup", HelpText = "Install and configure a new FiveM server with NFive installed.")]
	internal class Setup : Module
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

		internal override async Task<int> Main()
		{
			this.Location = Path.GetFullPath(this.Location);

			Console.WriteLine("This utility will walk you through setting up a new FiveM server with NFive installed.");
			Console.WriteLine();
			Console.WriteLine($"The server will be installed at {this.Location}");
			Console.WriteLine();
			Console.WriteLine("Press ", "Ctrl+C".Yellow(), " at any time to quit.");
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

				Directory.CreateDirectory(RuntimeEnvironment.IsWindows ? this.Location : Path.Combine(this.Location, "alpine", "opt", "cfx-server"));

				config.Serialize(Path.Combine(this.Location, RuntimeEnvironment.IsWindows ? PathManager.ConfigFile : Path.Combine("alpine", "opt", "cfx-server", PathManager.ConfigFile)));

				if (!this.FiveM.HasValue) Console.WriteLine();

				await InstallFiveM(this.Location);

				if (!RuntimeEnvironment.IsWindows) this.Location = Path.Combine(this.Location, "alpine", "opt", "cfx-server");
				this.Location = Path.Combine(this.Location, "resources", "nfive");
			}

			if (this.NFive.HasValue && this.NFive.Value || !this.NFive.HasValue && Input.Bool("Install NFive?", true))
			{
				if (!this.FiveM.HasValue) Console.WriteLine();
				Console.WriteLine("NFive database configuration...");

				var dbHost = string.IsNullOrWhiteSpace(this.DatabaseHost) ? Input.String("database host", "localhost") : this.DatabaseHost;
				var dbPort = this.DatabasePort ?? Input.Int("database port", 1, ushort.MaxValue, 3306);
				var dbUser = string.IsNullOrWhiteSpace(this.DatabaseUser) ? Input.String("database user", "root") : this.DatabaseUser;
				var dbPass = string.IsNullOrWhiteSpace(this.DatabasePassword) ? Regex.Replace(Input.Password("database password", "<blank>"), "^<blank>$", string.Empty) : this.DatabasePassword;
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

				var dbYml = File.ReadAllText(Path.Combine(this.Location, "config", "database.yml")); // TODO: Handle as YAML?
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
			var platformName = RuntimeEnvironment.IsWindows ? "Windows" : "Linux";
			var platformUrl = RuntimeEnvironment.IsWindows ? "build_server_windows" : "build_proot_linux";
			var platformFile = RuntimeEnvironment.IsWindows ? "server.zip" : "fx.tar.xz";
			var platformPath = RuntimeEnvironment.IsWindows ? Path.Combine(path) : Path.Combine(path, "alpine", "opt", "cfx-server");

			Console.WriteLine($"Finding latest FiveM {platformName} server version...");

			using (var client = new WebClient())
			{
				var page = await client.DownloadStringTaskAsync($"https://runtime.fivem.net/artifacts/fivem/{platformUrl}/master/");
				var regex = new Regex("href=\"(\\d+)-([a-f0-9]{40})/\"", RegexOptions.IgnoreCase);
				var versions = new List<Tuple<uint, string>>();
				for (var match = regex.Match(page); match.Success; match = match.NextMatch())
				{
					versions.Add(new Tuple<uint, string>(uint.Parse(match.Groups[1].Value), match.Groups[2].Value));
				}

				var latest = versions.Max();

				var platformCache = RuntimeEnvironment.IsWindows ? $"fivem_server_{latest.Item1}.zip" : $"fivem_server_{latest.Item1}.tar.xz";

				await Install(path, $"FiveM {platformName} server", latest.Item1.ToString(), platformCache, $"https://runtime.fivem.net/artifacts/fivem/{platformUrl}/master/{latest.Item1}-{latest.Item2}/{platformFile}");

				File.WriteAllText(Path.Combine(platformPath, "version"), latest.Item1.ToString());
			}
		}

		private static async Task InstallNFive(string path)
		{
			Console.WriteLine("Finding latest NFive version...");

			var version = (await Adapters.Bintray.Version.Get("nfive/NFive/NFive")).Name;

			await Install(path, "NFive", version, $"nfive_{version}.zip", $"https://dl.bintray.com/nfive/NFive/{version}/nfive.zip");
		}

		private static async Task Install(string path, string name, string version, string cacheName, string url)
		{
			using (var client = new WebClient())
			{
				var cacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache", cacheName);

				byte[] data;

				if (File.Exists(cacheFile))
				{
					Console.WriteLine($"Reading {name} v{version} from cache...");

					data = File.ReadAllBytes(cacheFile);
				}
				else
				{
					Console.WriteLine($"Downloading {name} v{version}...");

					data = await client.DownloadDataTaskAsync(url);

					Directory.CreateDirectory(Path.GetDirectoryName(cacheFile));
					File.WriteAllBytes(cacheFile, data);
				}

				Console.WriteLine($"Installing {name}...");

				Directory.CreateDirectory(path);

				var skip = new[]
				{
					"server.cfg",
					"__resource.lua",
					"nfive.yml",
					"nfive.lock",
					"config/nfive.yml",
					"config/database.yml"
				};

				using (var stream = new MemoryStream(data))
				using (var reader = ReaderFactory.Open(stream))
				{
					if (reader.ArchiveType == ArchiveType.Tar && RuntimeEnvironment.IsLinux)
					{
						stream.Position = 0;

						var tempFile = Path.GetTempFileName();

						using (var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
						{
							stream.CopyTo(file);
						}

						using (var process = new Process
						{
							StartInfo =
							{
								FileName = "tar",
								Arguments = $"xJ -C {path} -f {tempFile}",
								UseShellExecute = false,
								RedirectStandardInput = false,
								RedirectStandardOutput = false
							}
						})
						{
							process.Start();
							process.WaitForExit();
						}

						File.Delete(tempFile);
					}
					else
					{
						while (reader.MoveToNextEntry())
						{
							if (reader.Entry.IsDirectory) continue;

							var opts = new ExtractionOptions { ExtractFullPath = true, Overwrite = true, PreserveFileTime = true };

							if (skip.Contains(reader.Entry.Key) && File.Exists(Path.Combine(path, reader.Entry.Key)))
							{
								opts.Overwrite = false;
							}

							reader.WriteEntryToDirectory(path, opts);
						}
					}
				}
			}

			Console.WriteLine();
		}
	}
}
