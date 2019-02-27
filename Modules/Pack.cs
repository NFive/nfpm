using CommandLine;
using NFive.SDK.Plugins.Configuration;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// List installed NFive plugins.
	/// </summary>
	[Verb("pack", HelpText = "Packs a NFive plugin from source.")]
	internal class Pack
	{
		[Value(0, Default = "{project}.zip", Required = false, HelpText = "Zip file to pack plugin into.")]
		public string Output { get; set; }

		internal async Task<int> Main()
		{
			Plugin definition;

			try
			{
				Environment.CurrentDirectory = PathManager.FindResource();

				definition = Plugin.Load(ConfigurationManager.DefinitionFile);
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm setup` to setup NFive in this directory");

				return 1;
			}

			var outputPath = string.Equals(this.Output, "{project}.zip") ? $"{definition.Name.Project}.zip" : this.Output;

			Console.WriteLine($"Packing {definition.FullName} in {outputPath}");

			var standardFiles = new[]
			{
				"nfive.yml",
				"nfive.lock",
				"readme*",
				"license*"
			};

			File.Delete(outputPath);

			using (var zip = ZipArchive.Create())
			{
				foreach (var file in standardFiles)
				{
					var matches = Directory.EnumerateFiles(Environment.CurrentDirectory, file).ToList();

					if (!matches.Any()) continue;

					foreach (var match in matches) 
					{
						var fileName = Path.GetFileName(match);

						Console.WriteLine($"Adding {fileName}...");

						zip.AddEntry(fileName, File.OpenRead(match));
					}
				}

				var files = new List<string>();

				if (definition.Server?.Main != null) files.AddRange(definition.Server.Main.Select(f => $"{f}.net.dll"));
				if (definition.Server?.Include != null) files.AddRange(definition.Server.Include.Select(f => $"{f}.net.dll"));

				if (definition.Client?.Main != null) files.AddRange(definition.Client.Main.Select(f => $"{f}.net.dll"));
				if (definition.Client?.Include != null) files.AddRange(definition.Client.Include.Select(f => $"{f}.net.dll"));
				if (definition.Client?.Files != null) files.AddRange(definition.Client.Files);

				foreach (var file in files.Distinct().Select(f => f.Replace(Path.DirectorySeparatorChar, '/')))
				{
					Console.WriteLine($"Adding {file}...");

					zip.AddEntry(file, File.OpenRead(file));
				}

				zip.SaveTo(File.OpenWrite(outputPath));
			}

			Console.WriteLine("Packing complete");

			return await Task.FromResult(0);
		}
	}
}
