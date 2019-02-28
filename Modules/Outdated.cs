using CommandLine;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using NFive.PluginManager.Utilities.Console;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Check for updates for installed NFive plugins.
	/// </summary>
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

			var results = new List<ColorToken[]>
			{
				new []
				{
					"NAME".White(),
					"CURRENT".White(),
					"WANTED".White(),
					"LATEST".White()
				}
			};

			foreach (var dependency in definition.Dependencies)
			{
				var repo = definition.Repositories?.FirstOrDefault(r => r.Name.ToString() == dependency.Key.ToString());
				var adapter = new AdapterBuilder(dependency.Key, repo).Adapter();

				var versions = (await adapter.GetVersions()).ToList();
				var versionMatch = versions.LastOrDefault(version => dependency.Value.IsSatisfied(version.ToString()));
				if (versionMatch == null) throw new Exception("No matching version found");

				var current = "MISSING".Red();
				ColorToken wanted = versionMatch.ToString();
				ColorToken latest = versions.Last().ToString();

				var pluginDefinition = new FileInfo(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, dependency.Key.Vendor, dependency.Key.Project, ConfigurationManager.DefinitionFile));

				if (pluginDefinition.Exists)
				{
					var plugin = Plugin.Load(pluginDefinition.FullName);

					current = plugin.Version.ToString();

					current = current.Text != wanted.Text ? current.Red() : current.Green();
					wanted = wanted.Text != latest.Text ? wanted.Red() : wanted.Green();
				}

				results.Add(new[]
				{
					dependency.Key.ToString(),
					current,
					wanted,
					latest
				});
			}

			var nameLength = Math.Max(Math.Min(50, results.Max(d => d[0].Text.Length)), 10);
			var currentLength = Math.Max(Math.Min(20, results.Max(d => d[1].Text.ToString().Length)), 7);
			var wantedLength = Math.Max(Math.Min(20, results.Max(d => d[2].Text.ToString().Length)), 7);
			var latestLength = Math.Max(Math.Min(20, results.Max(d => d[3].Text.ToString().Length)), 7);

			foreach (var result in results)
			{
				result[0].Text = result[0].Text.Truncate(nameLength).PadRight(nameLength);
				result[1].Text = result[1].Text.Truncate(currentLength).PadLeft(currentLength);
				result[2].Text = result[2].Text.Truncate(wantedLength).PadLeft(wantedLength);
				result[3].Text = result[3].Text.Truncate(latestLength).PadLeft(latestLength);

				Console.WriteLine(result[0], " | ", result[1], " | ", result[2], " | ", result[3]);
			}

			return await Task.FromResult(0);
		}
	}
}
