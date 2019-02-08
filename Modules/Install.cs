using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Models;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Utilities;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Installs a plugin or processes the lock file.
	/// </summary>
	[UsedImplicitly]
	[Verb("install", HelpText = "Install NFive plugins.")]
	internal class Install
	{
		[PublicAPI]
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

			if (!this.Plugins.Any())
			{
				Console.WriteLine("No new plugins to add");

				if (graph != null)
				{
					Console.WriteLine("Applying dependencies...");

					await graph.Apply(definition);

					if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();
				}
				else
				{
					Console.WriteLine("Building new tree...");

					graph = new DefinitionGraph();
					await graph.Build(definition);

					Console.WriteLine("Applying dependencies...");

					await graph.Apply(definition);

					graph.Save(ConfigurationManager.LockFile);

					if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();
				}

				Console.WriteLine("Finished");

				return 0;
			}

			foreach (var plugin in this.Plugins)
			{
				var input = plugin;

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
				Name name;
				var version = new Models.VersionRange("*");

				try
				{
					name = new Name(parts[0]);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					return 1;
				}

				if (parts.Length == 2)
				{
					try
					{
						version = new Models.VersionRange(parts[1]);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						return 1;
					}
				}

				if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, SDK.Core.Plugins.VersionRange>();

				if (definition.Dependencies.ContainsKey(name))
				{
					if (definition.Dependencies[name] == version)
					{
						Console.WriteLine($"Plugin \"{name}@{version}\" is already installed");
						continue;
					}

					Console.WriteLine($"Updating plugin \"{name}\" from \"{definition.Dependencies[name]}\" to \"{version}\"");
					definition.Dependencies[name] = version;
				}
				else
				{
					Console.WriteLine($"Adding new plugin \"{name}@{version}\"");
					definition.Dependencies.Add(name, version);
				}
			}

			Console.WriteLine("Applying dependencies...");

			graph = new DefinitionGraph();
			await graph.Build(definition);
			await graph.Apply(definition);

			definition.Save(ConfigurationManager.DefinitionFile);
			graph.Save(ConfigurationManager.LockFile);

			if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();

			Console.WriteLine("Finished");

			return 0;
		}
	}
}
