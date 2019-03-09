using CommandLine;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Uninstall a NFive plugin.
	/// </summary>
	[Verb("remove", HelpText = "Uninstall a NFive plugin.")]
	internal class Remove : Module
	{
		[Value(0, Required = true, HelpText = "plugin name")]
		public IEnumerable<string> Plugins { get; set; } = new List<string>();

		internal override async Task<int> Main()
		{
			var definition = LoadDefinition(this.Verbose);

			foreach (var plugin in this.Plugins)
			{
				var name = new Name(plugin); // TODO: Handle

				if (definition.Dependencies == null || !definition.Dependencies.ContainsKey(name)) continue;

				if (!this.Quiet) Console.WriteLine("- ", name.ToString().White());

				definition.Dependencies.Remove(name);
			}

			var graph = new DefinitionGraph();
			await graph.Apply(definition);

			definition.Save(ConfigurationManager.DefinitionFile);
			graph.Save();

			if (PathManager.IsResource()) ResourceGenerator.Serialize(graph);

			return 0;
		}
	}
}
