using CommandLine;
using NFive.PluginManager.Extensions;
using Scriban;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Generate the boilerplate code for a new NFive plugin.
	/// </summary>
	[Verb("scaffold", HelpText = "Generate the boilerplate code for a new NFive plugin.")]
	internal class Scaffold
	{
		[Option("owner", Required = false, HelpText = "Set plugin owner.")]
		public string Owner { get; set; } = null;

		[Option("project", Required = false, HelpText = "Set plugin project.")]
		public string Project { get; set; } = null;

		[Option("description", Required = false, HelpText = "Set plugin description.")]
		public string Description { get; set; } = null;

		[Option("client", Required = false, HelpText = "Generate client plugin component.")]
		public bool? Client { get; set; } = null;

		[Option("server", Required = false, HelpText = "Generate server plugin component.")]
		public bool? Server { get; set; } = null;

		[Option("shared", Required = false, HelpText = "Generate shared plugin library.")]
		public bool? Shared { get; set; } = null;

		[Value(0, Default = "NFive/skeleton-plugin-default", Required = false, HelpText = "Location of skeleton files, can be a local or remote zip file, local directory or Github short link (user/repo#branch).")]
		public string Source { get; set; }

		internal async Task<int> Main()
		{
			var uri = ValidateSource(this.Source);

			Console.WriteLine("This utility will walk you through generating the boilerplate code for a new plugin.");
			Console.WriteLine();
			Console.WriteLine("Press ", "Ctrl+C".Yellow(), " at any time to quit.");
			Console.WriteLine();

			var org = string.IsNullOrWhiteSpace(this.Owner) ? Input.String("Owner", s =>
			{
				if (!string.IsNullOrWhiteSpace(s)) return true;

				Console.Write("Owner: ");
				return false;
			}) : this.Owner.Trim();
			var project = string.IsNullOrWhiteSpace(this.Project) ? Input.String("Project", s =>
			{
				if (!string.IsNullOrWhiteSpace(s)) return true;

				Console.Write("Project: ");
				return false;
			}) : this.Project.Trim();
			var desc = string.IsNullOrWhiteSpace(this.Description) ? Input.String("Description", "NFive Plugin") : this.Description.Trim();
			var client = this.Client ?? Input.Bool("Generate client plugin", true);
			var server = this.Server ?? Input.Bool("Generate server plugin", true);
			var shared = this.Shared ?? Input.Bool("Generate shared library", true);

			var config = new
			{
				org = org.Dehumanize(),
				project = project.Dehumanize(),
				orgorig = org,
				projectorig = project,
				desc,
				client,
				server,
				shared,
				solutionguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				projectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				clientprojectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				serverprojectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				sharedprojectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}"
			};

			Console.WriteLine();

			var directory = Path.Combine(Environment.CurrentDirectory, project);

			await FetchSource(uri, directory);

			Console.WriteLine("Applying templates...");

			foreach (var dir in Directory.EnumerateDirectories(directory, "*{{*}}*", SearchOption.AllDirectories))
			{
				if (!config.server && dir.EndsWith(".Server"))
				{
					Directory.Delete(dir, true);
					continue;
				}

				if (!config.client && dir.EndsWith(".Client"))
				{
					Directory.Delete(dir, true);
					continue;
				}

				if (!config.shared && dir.EndsWith(".Shared"))
				{
					Directory.Delete(dir, true);
					continue;
				}

				var tpl = Template.Parse(dir);
				var dirname = tpl.Render(config);

				Directory.Move(dir, dirname);
			}

			foreach (var file in Directory.EnumerateFiles(directory, "*.tpl", SearchOption.AllDirectories))
			{
				var tpl = Template.Parse(file);
				var filename = tpl.Render(config);

				tpl = Template.Parse(File.ReadAllText(file));
				var content = tpl.Render(config);

				File.Delete(file);

				File.WriteAllText(filename.Substring(0, filename.Length - 4), content);
			}

			var path = Environment
				.GetEnvironmentVariable("PATH")
				?.Split(';')
				.FirstOrDefault(p => File.Exists(Path.Combine(p, "nuget.exe")));

			if (!string.IsNullOrWhiteSpace(path))
			{
				Console.WriteLine("Running ", "nuget restore".Yellow(), " to download packages...");

				Process.Start(new ProcessStartInfo(Path.Combine(path, "nuget.exe"), $"restore {directory}")
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					ErrorDialog = false,
					WindowStyle = ProcessWindowStyle.Hidden
				});
			}
			else
			{
				Console.WriteLine("nuget.exe not found in %PATH%, skipping package restore");
				Console.WriteLine("You will need to manually restore packages from Visual Studio");
			}

			Console.WriteLine();
			Console.WriteLine("Scaffolding is complete, you can now develop your plugin!");

			return 0;
		}

		private static Uri ValidateSource(string source)
		{
			if (!File.Exists(source) && !Directory.Exists(source))
			{
				var gh = new Regex(@"^(?<org>[\w_-]+)/(?<repo>[\w_-]+)(#(?<branch>.+))?$", RegexOptions.IgnoreCase).Match(source);

				if (gh.Success)
				{
					source = $"https://github.com/{gh.Groups["org"].Value}/{gh.Groups["repo"].Value}/archive/{(gh.Groups["branch"].Success ? gh.Groups["branch"].Value : "master")}.zip";
				}
			}

			if (File.Exists(source) || Directory.Exists(source))
			{
				source = Path.GetFullPath(source);
			}

			return new Uri(source);
		}

		private static async Task FetchSource(Uri uri, string directory)
		{
			if (!uri.IsFile)
			{
				var schemes = new[]
				{
					"http",
					"https"
				};

				if (!schemes.Contains(uri.Scheme)) throw new ArgumentException($"Source URL \"{uri.AbsoluteUri}\" uses unsupported scheme \"{uri.Scheme}\". Supported schemes: {string.Join(", ", schemes)}.");

				using (var webClient = new WebClient())
				{
					Console.WriteLine("Downloading plugin skeleton...");

					var data = await webClient.DownloadDataTaskAsync(uri.AbsoluteUri);

					Extract(data, directory);
				}
			}
			else
			{
				if (File.GetAttributes(uri.AbsolutePath).HasFlag(FileAttributes.Directory))
				{
					Console.WriteLine("Copying plugin skeleton...");

					new DirectoryInfo(uri.AbsolutePath).Copy(directory);
				}
				else
				{
					Extract(File.ReadAllBytes(uri.AbsolutePath), directory);
				}
			}
		}

		private static void Extract(byte[] data, string directory)
		{
			Console.WriteLine("Extracting plugin skeleton...");

			using (var stream = new MemoryStream(data))
			using (var zip = ZipArchive.Open(stream))
			{
				var topLevelFiles = zip.Entries.Count(e => !e.IsDirectory && e.Key.Count(c => c == '/') == 0);
				var topLevelDirs = zip.Entries.Count(e => e.IsDirectory && e.Key.Count(c => c == '/') == 1);

				Directory.CreateDirectory(directory);

				if (topLevelFiles == 0 && topLevelDirs == 1)
				{
					var topLevel = zip.Entries.First(e => e.IsDirectory && e.Key.Count(c => c == '/') == 1);

					foreach (var e in zip.Entries.Where(e => e.Key != topLevel.Key))
					{
						var newPath = Path.Combine(directory, e.Key.Replace(topLevel.Key, ""));

						if (e.IsDirectory)
						{
							Directory.CreateDirectory(newPath);
						}
						else
						{
							using (var file = new FileStream(newPath, FileMode.Create))
							{
								e.WriteTo(file);
							}
						}
					}
				}
				else
				{
					zip.WriteToDirectory(directory, new ExtractionOptions
					{
						Overwrite = true,
						ExtractFullPath = true
					});
				}
			}
		}
	}
}
