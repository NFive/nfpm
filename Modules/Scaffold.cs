using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Ionic.Zip;
using JetBrains.Annotations;
using Scriban;
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
		private const string Repo = "skeleton-plugin-server";
		private const string Branch = "master";

		internal async Task<int> Main()
		{
			var config = new
			{
				org = ParseSimple("Organization", "Acme"),
				project = ParseSimple("Project", "Foo"),
				desc = ParseSimple("Description", "Test plugin"),
				client = ParseSimple("Client plugin", "Yes").ToLowerInvariant() == "yes",
				server = ParseSimple("Server plugin", "Yes").ToLowerInvariant() == "yes",
				shared = ParseSimple("Shared library", "Yes").ToLowerInvariant() == "yes",
				solutionguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				projectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				clientprojectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				serverprojectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				sharedprojectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}"
			};

			Console.WriteLine("Downloading plugin skeleton...");

			using (var client = new WebClient())
			{
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				var data = await client.DownloadDataTaskAsync($"https://github.com/NFive/{Repo}/archive/{Branch}.zip");

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
