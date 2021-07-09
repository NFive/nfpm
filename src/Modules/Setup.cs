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
	using SharpCompress.Archives.SevenZip;

	/// <summary>
	/// Install and configure a new FiveM server with NFive installed.
	/// </summary>
	[Verb("setup", HelpText = "Install and configure a new FiveM server with NFive installed.")]
	internal class Setup : Module
	{
		[Option("fivem", Required = false, HelpText = "Install FiveM server.")]
		public bool? FiveM { get; set; } = null;

		[Option("fivem-version", Required = false, HelpText = "FiveM server version.")]
		public string FiveMVersion { get; set; } = null;

		[Option("fivem-source", Required = false, HelpText = "Location of FiveM server install files.")]
		public string FiveMSource { get; set; } = null;

		[Option("nfive", Required = false, HelpText = "Install NFive.")]
		public bool? NFive { get; set; } = null;

		[Option("nfive-source", Required = false, HelpText = "Location of NFive server install files.")]
		public string NFiveSource { get; set; } = null;

		[Option("servername", Required = false, HelpText = "Set server name.")]
		public string ServerName { get; set; } = null;

		[Option("maxplayers", Required = false, HelpText = "Set server max players.")]
		public ushort? MaxPlayers { get; set; } = null;

		[Option("locale", Required = false, HelpText = "Set server locale.")]
		public string Locale { get; set; } = null;

		[Option("onesync", Required = false, HelpText = "Enable OneSync.")]
		public bool? OneSync { get; set; } = null;

		[Option("tags", Required = false, HelpText = "Set server tags.")]
		public string Tags { get; set; } = null;

		[Option("licensekey", Required = false, HelpText = "Set server license key.")]
		public string LicenseKey { get; set; } = null;

		[Option("steamkey", Required = false, HelpText = "Set Steam API license key.")]
		public string SteamKey { get; set; } = null;

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

		public override async Task<int> Main()
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
					MaxPlayers = this.MaxPlayers ?? Convert.ToUInt16(Input.Int("server max players", 1, 128, 32)),
					Locale = string.IsNullOrWhiteSpace(this.Locale) ? Input.String("server locale", "en-US", s =>
					{
						if (Regex.IsMatch(s, @"[a-z]{2}-[A-Z]{2}")) return true;

						Console.Write("Please enter a valid locale (xx-XX format): ");
						return false;
					}) : this.Locale,
					OneSync = this.OneSync ?? Input.Bool("enable OneSync", true),
					Tags = (string.IsNullOrWhiteSpace(this.Tags) ? Input.String("server tags (separate with space)", "NFive") : this.Tags).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList(),
					LicenseKey = string.IsNullOrWhiteSpace(this.LicenseKey) ? Input.String("server license key (https://keymaster.fivem.net/)", s =>
					{
						if (Regex.IsMatch(s, @"[\d\w]{32}")) return true;

						Console.Write("Please enter a valid license key: ");
						return false;
					}).ToLowerInvariant() : this.LicenseKey,
					SteamKey = string.IsNullOrWhiteSpace(this.SteamKey) ? Regex.Replace(Input.String("Steam API license key (https://steamcommunity.com/dev/apikey)", "<disabled>", s =>
					{
						if (s == "<disabled>") return true;
						if (s == "none") return true;
						if (Regex.IsMatch(s, @"[0-9a-fA-F]{32}")) return true;

						Console.Write("Please enter a valid Steam API license key: ");
						return false;
					}), "^<disabled>$", "none") : this.SteamKey,
					RconPassword = string.IsNullOrWhiteSpace(this.RconPassword) ? Regex.Replace(Input.Password("RCON password", "<disabled>"), "^<disabled>$", string.Empty) : this.RconPassword
				};

				Directory.CreateDirectory(RuntimeEnvironment.IsWindows ? this.Location : Path.Combine(this.Location, "alpine", "opt", "cfx-server"));

				config.Serialize(Path.Combine(this.Location, RuntimeEnvironment.IsWindows ? PathManager.ConfigFile : Path.Combine("alpine", "opt", "cfx-server", PathManager.ConfigFile)));

				if (!this.FiveM.HasValue) Console.WriteLine();

				await InstallFiveM(this.Location, this.FiveMSource);

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

				// TODO: Ask to include stock plugins?
			}

			Console.WriteLine("Installation is complete, you can now start the server with `nfpm start`!");

			return 0;
		}

		private async Task InstallFiveM(string path, string source)
		{
			var platformName = RuntimeEnvironment.IsWindows ? "Windows" : "Linux";
			var platformUrl = RuntimeEnvironment.IsWindows ? "build_server_windows" : "build_proot_linux";
			var platformFileGroupPattern = RuntimeEnvironment.IsWindows ? $"({Regex.Escape("server.zip")}|{Regex.Escape("server.7z")})" : "(fx.tar.xz)";
			var platformPath = RuntimeEnvironment.IsWindows ? Path.Combine(path) : Path.Combine(path, "alpine", "opt", "cfx-server");
			var versionFilenames = new Dictionary<uint, string>();
			string platformFile = null; // wil be provided latest when targetVersion is determined.

			if (!string.IsNullOrWhiteSpace(source) && File.Exists(source))
			{
				Install(path, $"FiveM {platformName} server", File.ReadAllBytes(source));

				return;
			}

			Console.WriteLine($"Finding available FiveM {platformName} server versions...");

			try
			{
				var versions = new List<Tuple<uint, string>>();
				var recommendedVersion = 0u;
				var optionalVersion = 0u;

				using (var client = new WebClient())
				{
					var page = await client.DownloadStringTaskAsync(
						$"https://runtime.fivem.net/artifacts/fivem/{platformUrl}/master/");
					for (var match =
							new Regex(
								$"href= *\"\\./(\\d+)-([a-f0-9]{{40}})/{platformFileGroupPattern}\"( class=\"button is-link is-primary)?( class=\"button is-link is-danger)?",
								RegexOptions.IgnoreCase).Match(page);
						match.Success;
						match = match.NextMatch())
					{
						var version = uint.Parse(match.Groups[1].Value);
						versions.Add(new Tuple<uint, string>(version, match.Groups[2].Value));
						
						// Storing filenames by version number we can pull out the correct filename later
						versionFilenames[version] = match.Groups[3].Value;

						if (match.Groups[4].Success) recommendedVersion = version;
						if (match.Groups[5].Success) optionalVersion = version;
					}


				}

				var latestVersion = versions.Max();

				Console.WriteLine($"{versions.Count:N0} versions available, latest v{latestVersion.Item1}, recommended v{recommendedVersion}, optional v{optionalVersion}");

				if (string.IsNullOrWhiteSpace(this.FiveMVersion) ||
				    ValidateVersion(this.FiveMVersion, versions, recommendedVersion, optionalVersion) == null)
					this.FiveMVersion = Input.String("FiveM server version", "latest", s =>
					{
						if (ValidateVersion(s, versions, recommendedVersion, optionalVersion) != null) return true;

						Console.Write("Please enter an available version: ");
						return false;
					});

				var targetVersion = ValidateVersion(this.FiveMVersion, versions, recommendedVersion, optionalVersion);
				platformFile = versionFilenames[targetVersion.Item1];

				var fileExt = Path.GetExtension(platformFile);
				var platformCache = RuntimeEnvironment.IsWindows
					? $"fivem_server_{targetVersion.Item1}{fileExt}"
					: $"fivem_server_{targetVersion.Item1}.tar.xz";

				var data = await DownloadCached(
					$"https://runtime.fivem.net/artifacts/fivem/{platformUrl}/master/{targetVersion.Item1}-{targetVersion.Item2}/{platformFile}",
					$"FiveM {platformName} server", targetVersion.Item1.ToString(), platformCache);

				Install(path, $"FiveM {platformName} server",
					data.Item1,
					fileExt == ".7z" ? data.Item2 : null
					);

				File.WriteAllText(Path.Combine(platformPath, "version"), targetVersion.Item1.ToString());
			}
			catch (WebException ex)
			{
				Console.WriteLine($"Error downloading FiveM server archive: {ex.Message}");
				Console.WriteLine();
				Console.WriteLine("Reverting to local install");

				if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
					source = Input.String("Local FiveM server archive path", platformFile, s =>
					{
						if (File.Exists(s)) return true;

						Console.Write("Please enter a local path: ");
						return false;
					});

				Install(path, $"FiveM {platformName} server", File.ReadAllBytes(source));
			}
		}

		private static Tuple<uint, string> ValidateVersion(string version, IEnumerable<Tuple<uint, string>> versions, uint recommendedVersion, uint optionalVersion)
		{
			version = version.Trim().TrimStart('v').ToLowerInvariant();
			if (version == "latest") return versions.Max();
			if (version == "recommended") return versions.FirstOrDefault(v => v.Item1 == recommendedVersion);
			if (version == "optional") return versions.FirstOrDefault(v => v.Item1 == optionalVersion);

			var value = uint.Parse(version);
			return versions.FirstOrDefault(v => v.Item1 == value);
		}

		private static async Task InstallNFive(string path)
		{
			Console.WriteLine("Finding latest NFive version...");

			var version = (await Adapters.Bintray.Version.Get("nfive/NFive/NFive")).Name;

			var data = await DownloadCached($"https://dl.bintray.com/nfive/NFive/{version}/nfive.zip", "NFive", version, $"nfive_{version}.zip");

			Install(path, "NFive", data.Item1);
		}

		private static async Task<byte[]> Download(string url)
		{
			using (var client = new WebClient())
			using (var progress = new ProgressBar())
			{
				return await client.DownloadDataTaskAsync(url, new Progress<Tuple<long, int, long>>(tuple => progress.Report(tuple.Item2 * 0.01)));
			}
		}

		private static async Task<Tuple<byte[], string>> DownloadCached(string url, string name, string version, string cacheName)
		{
			var cacheFile = Path.Combine(PathManager.CachePath, cacheName);

			if (File.Exists(cacheFile))
			{
				Console.WriteLine($"Reading {name} v{version} from cache...");

				return new Tuple<byte[], string>(File.ReadAllBytes(cacheFile), cacheFile);
			}

			Console.WriteLine($"Downloading {name} v{version}...");

			var data = await Download(url);

			Directory.CreateDirectory(PathManager.CachePath);
			File.WriteAllBytes(cacheFile, data);

			return new Tuple<byte[], string>(data, cacheFile);
		}

		private static void Install(string path, string name, byte[] data, string filename = null)
		{
			Console.WriteLine($"Installing {name}...");

			Directory.CreateDirectory(path);

			var skip = new[]
			{
				"server.cfg",
				"fxmanifest.lua",
				"nfive.yml",
				"nfive.lock",
				"config/nfive.yml",
				"config/database.yml"
			};

			if (string.IsNullOrWhiteSpace(filename) == false && SevenZipArchive.IsSevenZipFile(filename))
				InstallFromSevenZipArchive(path, skip, filename, data); 
			else
				InstallFromStream(path, skip, data);

			Console.WriteLine();
		}

		private static void InstallFromSevenZipArchive(string path, string[] skip, string filename, byte[] data)
		{
			using (var stream = new MemoryStream(data))
			{
				var archive = SevenZipArchive.Open(stream);
				var reader = archive.ExtractAllEntries();
				Extract(reader, path, skip);
			}
		}

		private static void InstallFromStream(string path, string[] skip, byte[] data)
		{
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
					Extract(reader, path, skip);
				}
			}
		}

		private static void Extract(IReader reader, string path, string[] skip)
		{
			while (reader.MoveToNextEntry())
			{
				if (reader.Entry.IsDirectory) continue;

				var opts = new ExtractionOptions
					{ ExtractFullPath = true, Overwrite = true, PreserveFileTime = true };

				if (skip.Contains(reader.Entry.Key) && File.Exists(Path.Combine(path, reader.Entry.Key)))
				{
					//opts.Overwrite = false; // TODO: Prompt to overwrite existing config?
				}

				reader.WriteEntryToDirectory(path, opts);
			}
		}

	}
}
