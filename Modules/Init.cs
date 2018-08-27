using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Models.Plugin;
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
			Console.WriteLine($"This utility will walk you through creating a {Program.DefinitionFile} file.");
			Console.WriteLine();
			Console.WriteLine($"Use `nfpm install <plugin>` afterwards to install a plugin and{Environment.NewLine}save it as a dependency in the {Program.DefinitionFile} file.");
			Console.WriteLine();
			Console.WriteLine("Press ^C at any time to quit.");
			Console.WriteLine();

			var name = ParseName();
			var version = ParseVersion();
			var description = ParseSimple("description");
			var author = ParseSimple("author");
			var license = ParseSimple("license");
			var website = ParseSimple("website");

			var definition = new Definition
			{
				Name = name,
				Version = new Models.Plugin.Version(version.ToString()),
				//Type = PluginTypes.Plugin,
				Description = !string.IsNullOrEmpty(description) ? description : null,
				Author = author,
				License = license,
				Website = website
			};

			var path = Path.Combine(Environment.CurrentDirectory, Program.DefinitionFile);

			var yml = Yaml.Serialize(definition);

			Console.WriteLine();
			Console.WriteLine($"About to write to {path}:");
			Console.WriteLine();
			Console.WriteLine(yml.Trim());
			Console.WriteLine();

			var confirm = ParseYesNo("Is this OK?");

			if (confirm) File.WriteAllText(path, yml);

			return await Task.FromResult(0);
		}

		private static string ParseSimple(string description)
		{
			Console.Write($"{description}: ");

			var input = Console.ReadLine()?.Trim();

			return !string.IsNullOrEmpty(input) ? input : null;
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

		private static bool ParseYesNo(string description, bool defaultValue = true)
		{
			Console.Write($"{description} ({(defaultValue ? "yes" : "no")}) ");

			var input = Console.ReadLine()?.Trim().ToLowerInvariant();

			if (string.IsNullOrEmpty(input)) return defaultValue;

			return input == "yes" || input == "y";
		}
	}
}
