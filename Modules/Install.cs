using CommandLine;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Models;
using NFive.PluginManager.Utilities;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Plugin = NFive.SDK.Plugins.Plugin;
using Version = NFive.SDK.Core.Plugins.Version;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Installs new plugins or processes the existing lock file.
	/// </summary>
	[Verb("install", HelpText = "Install NFive plugins.")]
	internal class Install : Module
	{
		[Value(0, Required = false, HelpText = "plugin name and optional version")]
		public IEnumerable<string> Plugins { get; set; } = new List<string>();

		internal override async Task<int> Main()
		{
			var definition = LoadDefinition();
			var graph = LoadGraph();

			// New plugins
			if (this.Plugins.Any())
			{
				foreach (var plugin in this.Plugins)
				{
					var input = plugin;

					// Local install
					if (Directory.Exists(plugin) && File.Exists(Path.Combine(plugin, ConfigurationManager.DefinitionFile)))
					{
						var path = Path.GetFullPath(plugin);

						var pluginDefinition = Plugin.Load(Path.Combine(path, ConfigurationManager.DefinitionFile));

						if (definition.Repositories == null) definition.Repositories = new List<Repository>();
						definition.Repositories.Add(new Repository
						{
							Name = pluginDefinition.Name,
							Type = "local",
							Path = path
						});

						input = pluginDefinition.Name;
					}

					var parts = input.Split(new[] { '@' }, 2);
					var name = new Name(parts[0]);
					var version = parts.Length == 2 ? new Models.VersionRange(parts[1]) : new Models.VersionRange("*");

					List<Version> versions;
					try
					{
						var adapter = new AdapterBuilder(name, definition).Adapter();
						versions = (await adapter.GetVersions()).ToList();
					}
					catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
					{
						Console.WriteLine("Error ".DarkRed(), $"{name}".Red(), " not found.".DarkRed());

						return 1;
					}

					var versionMatch = version.Latest(versions);
					if (versionMatch == null)
					{
						Console.WriteLine("Error ".DarkRed(), $"{name}@{version}".Red(), " not found, available versions: ".DarkRed(), string.Join(" ", versions.Select(v => v.ToString())).Red());

						return 1;
					}

					if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, SDK.Core.Plugins.VersionRange>();

					Console.WriteLine("+ ", $"{name}@{versionMatch}".White());

					if (definition.Dependencies.ContainsKey(name))
					{
						definition.Dependencies[name] = new Models.VersionRange($"^{versionMatch}");
					}
					else
					{
						definition.Dependencies.Add(name, new Models.VersionRange($"^{versionMatch}"));
					}
				}

				graph = new DefinitionGraph();
				await graph.Apply(definition);

				definition.Save(ConfigurationManager.DefinitionFile);
				graph.Save();
			}
			else
			{
				if (graph != null)
				{
					await graph.Apply();
				}
				else
				{
					graph = new DefinitionGraph();

					await graph.Apply(definition);

					graph.Save();
				}
			}

			if (PathManager.IsResource()) ResourceGenerator.Serialize(graph);

			return 0;
		}
	}
}
