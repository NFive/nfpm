using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
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

			Definition definition;

			try
			{
				definition = Definition.Load();
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm init` to setup NFive in this directory");

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

			var graph = new DefinitionGraph();
			await graph.Build(definition);
			await graph.Apply();

			definition.Save();
			graph.Save();
			ResourceGenerator.Serialize(graph).Save();

			return 0;
		}
	}
}
