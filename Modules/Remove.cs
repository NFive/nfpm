using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using Console = Colorful.Console;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Uninstall a NFive plugin.
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

			Plugin definition;

			try
			{
				Environment.CurrentDirectory = PathManager.FindResource();

				definition = NFive.SDK.Plugins.Plugin.Load(ConfigurationManager.DefinitionFile);
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm setup` to setup NFive in this directory");

				return 1;
			}

			definition.Dependencies?.Remove(plugin);

			var venderPath = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, plugin.Vendor);

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
			await graph.Apply(definition);

			definition.Save(ConfigurationManager.DefinitionFile);
			graph.Save();
			ResourceGenerator.Serialize(graph).Save();

			return 0;
		}
	}
}
