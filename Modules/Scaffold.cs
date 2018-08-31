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
	/// Generate the boilerplate code for a new server plugin.
	/// </summary>
	[UsedImplicitly]
	[Verb("scaffold", HelpText = "Generate the boilerplate code for a new server plugin.")]
	internal class Scaffold
	{
		internal async Task<int> Main()
		{
			var config = new
			{
				org = ParseSimple("Organization", "Acme"),
				project = ParseSimple("Project", "Foo"),
				desc = ParseSimple("Description", "Test plugin"),
				solutionguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}",
				projectguid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}"
			};

			Console.WriteLine("Downloading plugin skeleton...");

			using (var client = new WebClient())
			{
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				var data = await client.DownloadDataTaskAsync("https://github.com/NFive/skeleton-plugin-server/archive/master.zip");

				Console.WriteLine("Extracting plugin skeleton...");

				using (var stream = new MemoryStream(data))
				using (var zip = ZipFile.Read(stream))
				{
					zip.ExtractAll(Path.Combine(Environment.CurrentDirectory), ExtractExistingFileAction.OverwriteSilently);
				}
			}

			Directory.Move(Path.Combine(Environment.CurrentDirectory, "skeleton-plugin-server-master"), Path.Combine(Environment.CurrentDirectory, "plugin"));

			Console.WriteLine("Applying templates...");

			foreach (var file in Directory.EnumerateFiles(Path.Combine(Environment.CurrentDirectory, "plugin"), "*.tpl", SearchOption.AllDirectories))
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
