using CommandLine;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Utilities;
using System.Threading.Tasks;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update installed NFive plugins.
	/// </summary>
	[Verb("update", HelpText = "Update installed NFive plugins.")]
	internal class Update : Module
	{
		internal override async Task<int> Main()
		{
			var graph = new DefinitionGraph();
			await graph.Apply(LoadDefinition());
			graph.Save();

			if (PathManager.IsResource()) ResourceGenerator.Serialize(graph);

			return await Task.FromResult(0);
		}
	}
}
