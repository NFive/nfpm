using CommandLine;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Models;
using NFive.PluginManager.Utilities;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Installs a plugin or processes the lock file.
	/// </summary>
	[Verb("install", HelpText = "Install NFive plugins.")]
	internal class Install
	{
		[Value(0, Required = false, HelpText = "plugin name")]
		public IEnumerable<string> Plugins { get; set; } = new List<string>();

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
				Console.WriteLine("Building dependency tree...");

				graph = DefinitionGraph.Load();
			}
			catch (FileNotFoundException)
			{
				graph = null;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):");
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);

				return 1;
			}
			
			// New plugins
			if (this.Plugins.Any())
			{
				definition = await LocalInstall(definition, this.Plugins);

				graph = new DefinitionGraph();
				await graph.Apply(definition);

				definition.Save(ConfigurationManager.DefinitionFile);
				graph.Save();

				if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();
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

				if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();
			}
			
			return 0;
		}

		private async Task<Plugin> LocalInstall(Plugin definition, IEnumerable<string> plugins)
		{
			foreach (var plugin in plugins)
			{
				var input = plugin;

				// Local install
				if (Directory.Exists(plugin) && File.Exists(Path.Combine(plugin, ConfigurationManager.DefinitionFile)))
				{
					var path = Path.GetFullPath(plugin);

					var pluginDef = Plugin.Load(Path.Combine(path, ConfigurationManager.DefinitionFile));

					if (definition.Repositories == null) definition.Repositories = new List<Repository>();
					definition.Repositories.Add(new Repository
					{
						Name = pluginDef.Name,
						Type = "local",
						Path = path
					});

					input = pluginDef.Name;
				}

				var parts = input.Split(new[] { '@' }, 2);
				var name = new Name(parts[0]);
				var version = new Models.VersionRange("*");

				if (parts.Length == 2) version = new Models.VersionRange(parts[1]);

				var adapter = new AdapterBuilder(name, definition).Adapter();
				var versions = await adapter.GetVersions();

				var versionMatch = versions.LastOrDefault(v => version.IsSatisfied(v));
				if (versionMatch == null) throw new Exception("Version not found");

				if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, SDK.Core.Plugins.VersionRange>();

				Console.WriteLine($"+ {name}@{versionMatch}");

				if (definition.Dependencies.ContainsKey(name))
				{
					definition.Dependencies[name] = new Models.VersionRange($"^{versionMatch}");
				}
				else
				{
					definition.Dependencies.Add(name, new Models.VersionRange($"^{versionMatch}"));
				}
			}

			return definition;
		}
	}
}
