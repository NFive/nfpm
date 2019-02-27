using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using NFive.PluginManager.Utilities.Console;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Check for updates for installed NFive plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("outdated", HelpText = "Check for updates for installed NFive plugins.")]
	internal class Outdated
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

			ColorConsole.WriteLine("NAME".PadRight(30).White(), " | ", "CURRENT".PadRight(8).White(), " | ", "WANTED".PadRight(8).White(), " | ", "LATEST".PadRight(8).White());

			foreach (var dependency in definition.Dependencies)
			{
				var repo = definition.Repositories?.FirstOrDefault(r => r.Name.ToString() == dependency.Key.ToString());
				var adapter = new AdapterBuilder(dependency.Key, repo).Adapter();

				var versions = (await adapter.GetVersions()).ToList();
				var versionMatch = versions.LastOrDefault(version => dependency.Value.IsSatisfied(version.ToString()));
				if (versionMatch == null) throw new Exception("No matching version found");

				var current = "MISSING".PadRight(8).Red();
				var wanted = versionMatch.ToString().PadRight(8).Color(null);
				var latest = versions.Last().ToString().PadRight(8).Color(null);

				var pluginDefinition = new FileInfo(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, dependency.Key.Vendor, dependency.Key.Project, ConfigurationManager.DefinitionFile));

				if (pluginDefinition.Exists)
				{
					var plugin = Plugin.Load(pluginDefinition.FullName);

					current = plugin.Version.ToString().PadRight(8);

					current = current.Text != wanted.Text ? current.Red() : current.Green();
					wanted = wanted.Text != latest.Text ? wanted.Red() : wanted.Green();
				}

				ColorConsole.WriteLine(dependency.Key.ToString().PadRight(30), " | ", current, " | ", wanted, " | ", latest);
			}

			return await Task.FromResult(0);
		}
	}
}
