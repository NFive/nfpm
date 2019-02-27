using CommandLine;
using JetBrains.Annotations;
using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// List installed NFive plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("list", HelpText = "List installed NFive plugins.")]
	internal class List
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

			DefinitionGraph graph;

			try
			{
				graph = DefinitionGraph.Load();
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm install` to install some dependencies first");

				return 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):");
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);

				return 1;
			}

			Console.WriteLine($"{definition.FullName}");

			foreach (var plugin in graph.Plugins)
			{
				RecurseDependencies(plugin, string.Empty, plugin == graph.Plugins.Last());
			}

			return await Task.FromResult(0);
		}

		private static void RecurseDependencies(Plugin plugin, string prefix, bool last)
		{
			Console.Write(prefix);

			if (last)
			{
				Console.Write("└─ ");
				prefix += "  ";
			}
			else
			{
				Console.Write("├─ ");
				prefix += "│  ";
			}

			Console.WriteLine(plugin.FullName);

			if (plugin.DependencyNodes == null) return;

			foreach (var pluginDependencyNode in plugin.DependencyNodes)
			{
				RecurseDependencies(pluginDependencyNode, prefix, pluginDependencyNode == plugin.DependencyNodes.Last());
			}
		}
	}
}
