using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.SDK.Plugins.Configuration;
using NFive.SDK.Plugins.Models;
using Version = SemVer.Version;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Initialized a new NFive installation by creating "plugin.yml".
	/// </summary>
	[UsedImplicitly]
	[Verb("init", HelpText = "Initialize a new NFive installation or plugin.")]
	internal class Init
	{
		internal async Task<int> Main()
		{
			Console.WriteLine($"This utility will walk you through creating a {ConfigurationManager.DefinitionFile} file.");
			Console.WriteLine();
			Console.WriteLine($"Use `nfpm install <plugin>` afterwards to install a plugin and{Environment.NewLine}save it as a dependency in the {ConfigurationManager.DefinitionFile} file.");
			Console.WriteLine();
			Console.WriteLine("Press ^C at any time to quit.");
			Console.WriteLine();

			var name = ParseName();
			var version = ParseVersion();
			var description = Input.String("description");
			var author = Input.String("author");
			var license = Input.String("license");
			var website = Input.String("website");

			var definition = new Definition
			{
				Name = name,
				Version = new SDK.Plugins.Models.Version(version.ToString()),
				//Type = PluginTypes.Plugin,
				Description = !string.IsNullOrEmpty(description) ? description : null,
				Author = author,
				License = license,
				Website = website
			};

			var path = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.DefinitionFile);

			var yml = Yaml.Serialize(definition);

			Console.WriteLine();
			Console.WriteLine($"About to write to {path}:");
			Console.WriteLine();
			Console.WriteLine(yml.Trim());
			Console.WriteLine();

			var confirm = Input.Bool("Is this OK?", true);

			if (confirm) File.WriteAllText(path, yml);

			return await Task.FromResult(0);
		}

		private static string ParseName()
		{
			var defaultName = Path.GetFileName(Environment.CurrentDirectory).ToLowerInvariant(); // TODO: Trim chars
			Console.Write($"plugin name: ({defaultName}) ");

			var input = Console.ReadLine()?.Trim(); // TODO: Validate

			return !string.IsNullOrEmpty(input) ? input : defaultName;
		}

		private static Version ParseVersion()
		{
			Console.Write("version: (1.0.0) ");

			var input = Console.ReadLine();

			return string.IsNullOrEmpty(input) ? new Version(1, 0, 0) : new Version(input.Trim());
		}
	}
}
