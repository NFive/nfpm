using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Models;
using NFive.PluginManager.Models.Plugin;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Uninstalls a plugin.
	/// </summary>
	[UsedImplicitly]
	[Verb("remove", HelpText = "Uninstall a NFive plugin.")]
	internal class Remove
	{
		[PublicAPI]
		[Value(0, Required = true, HelpText = "plugin name")]
		public string Plugin { get; set; }

		internal async Task<int> Main()
		{
			var plugin = new Name(this.Plugin);

			var definitionPath = Path.Combine(Environment.CurrentDirectory, Program.DefinitionFile);

			if (!File.Exists(definitionPath))
			{
				Console.WriteLine($"Unable to find {definitionPath}", Color.Red);
				Console.WriteLine("Use `nfpm init` to setup NFive in this directory");

				return 1;
			}

			Definition definition;

			try
			{
				definition = Definition.Load(definitionPath);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to deserialize {definitionPath}:", Color.Red);
				Console.WriteLine(ex.Message);

				return 1;
			}

			definition.Dependencies?.Remove(plugin);

			var venderPath = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, plugin.Vendor);

			if (Directory.Exists(venderPath))
			{
				var projectPath = Path.Combine(venderPath, plugin.Project);

				if (Directory.Exists(projectPath))
				{
					Directory.Delete(projectPath, true);
				}

				if (!Directory.EnumerateFileSystemEntries(venderPath).Any()) Directory.Delete(venderPath);
			}

			// TODO: Remove orphaned child dependencies

			DefinitionGraph graph = new DefinitionGraph();

			try
			{
				await graph.Parse(definition); // Build and sort dependency tree
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):", Color.Red);
				Console.WriteLine(ex.Message);

				return 1;
			}

			// Save locks
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Program.DefinitionFile), Yaml.Serialize(definition));
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Program.LockFile), Yaml.Serialize(graph));
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Program.ResourceFile), new ResourceGenerator().Serialize(graph));

			return await Task.FromResult(0);
		}
	}
}
