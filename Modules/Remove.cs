using CommandLine;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Models;
using NFive.PluginManager.Utilities;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Uninstall NFive plugins.
	/// </summary>
	[Verb("remove", HelpText = "Uninstall NFive plugins.")]
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

				Directory.Delete(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, name.Vendor, name.Project), true);
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
