using CommandLine;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Utilities;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update installed NFive plugins.
	/// </summary>
	[Verb("update", HelpText = "Update installed NFive plugins.")]
	internal class Update
	{
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

			var graph = new DefinitionGraph();
			await graph.Build(definition);
			await graph.Apply(definition);
			graph.Save();

			if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();

			return await Task.FromResult(0);
		}
	}
}
