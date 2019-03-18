using CommandLine;
using NFive.PluginManager.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// List installed NFive plugins.
	/// </summary>
	[Verb("list", HelpText = "List installed NFive plugins.")]
	internal class List : Module
	{
		public override async Task<int> Main()
		{
			var definition = LoadDefinition();
			var graph = LoadGraph();

			if (graph == null)
			{
				Console.WriteLine("You must run ".Red(), "nfpm install".Yellow(), " before using this command.".Red());
				return 1;
			}

			Console.WriteLine(definition.FullName.Yellow());

			foreach (var plugin in graph.Plugins)
			{
				RecurseDependencies(plugin, string.Empty, plugin == graph.Plugins.Last());
			}

			return await Task.FromResult(0);
		}

		private static void RecurseDependencies(Plugin plugin, string prefix, bool last)
		{
			Console.Write(prefix.DarkGray());

			if (last)
			{
				Console.Write("└─ ".DarkGray());
				prefix += "  ";
			}
			else
			{
				Console.Write("├─ ".DarkGray());
				prefix += "│  ";
			}

			if (prefix.Length <= 3)
			{
				Console.WriteLine(plugin.FullName.White());
			}
			else
			{
				Console.WriteLine(plugin.FullName.Gray());
			}

			if (plugin.DependencyNodes == null) return;

			foreach (var pluginDependencyNode in plugin.DependencyNodes)
			{
				RecurseDependencies(pluginDependencyNode, prefix, pluginDependencyNode == plugin.DependencyNodes.Last());
			}
		}
	}
}
