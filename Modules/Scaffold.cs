using CommandLine;
using Ionic.Zip;
using JetBrains.Annotations;
using NFive.PluginManager.Extensions;
using Scriban;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Generate the boilerplate code for a new NFive plugin.
	/// </summary>
	[UsedImplicitly]
	[Verb("scaffold", HelpText = "Generate the boilerplate code for a new plugin.")]
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

		private const string Repo = "skeleton-plugin-server";
		private const string Branch = "master";

		internal async Task<int> Main()
		{
			Console.WriteLine("This utility will walk you through generating the boilerplate code for a new plugin.");
			Console.WriteLine();
			Console.WriteLine("Press ^C at any time to quit.");
			Console.WriteLine();

			var org = string.IsNullOrWhiteSpace(this.Owner) ? Input.String("Owner", "Acme").Dehumanize() : this.Owner.Trim();
			var project = string.IsNullOrWhiteSpace(this.Project) ? Input.String("Project", "Foo").Dehumanize() : this.Project.Trim();
			var desc = string.IsNullOrWhiteSpace(this.Description) ? Input.String("Description", "Test plugin") : this.Description.Trim();
			var client = this.Client ?? Input.Bool("Generate client plugin", true);
			var server = this.Server ?? Input.Bool("Generate server plugin", true);
			var shared = false;

			if (server && client)
			{
				shared = this.Shared ?? Input.Bool("Generate shared library", true);
			}

			var config = new
			{
				org,
				project,
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
			Console.WriteLine("Downloading plugin skeleton...");

			using (var webClient = new WebClient())
			{
				var data = await webClient.DownloadDataTaskAsync($"https://github.com/NFive/{Repo}/archive/{Branch}.zip");

				Console.WriteLine("Extracting plugin skeleton...");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(Path.Combine(Environment.CurrentDirectory), ExtractExistingFileAction.OverwriteSilently);
				}
			}

			var directory = $"plugin-{config.project.ToLowerInvariant()}";

			Directory.Move(Path.Combine(Environment.CurrentDirectory, $"{Repo}-{Branch}"), Path.Combine(Environment.CurrentDirectory, directory));

			Console.WriteLine("Applying templates...");

			foreach (var dir in Directory.EnumerateDirectories(Path.Combine(Environment.CurrentDirectory, directory), "*{{*}}*", SearchOption.AllDirectories))
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

			foreach (var file in Directory.EnumerateFiles(Path.Combine(Environment.CurrentDirectory, directory), "*.tpl", SearchOption.AllDirectories))
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
				Console.WriteLine("Running \"nuget restore\" to download packages...");

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
				Console.WriteLine("nuget.exe not found in %PATH%, skipping package restore", Color.Yellow);
				Console.WriteLine("You will need to manually restore packages from Visual Studio", Color.Yellow);
			}

			Console.WriteLine();
			Console.WriteLine("Scaffolding is complete, you can now write your plugin!");

			return 0;
		}
	}
}
