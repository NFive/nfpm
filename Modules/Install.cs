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
using System.Net;
using System.Threading.Tasks;
using Plugin = NFive.SDK.Plugins.Plugin;
using Version = NFive.SDK.Core.Plugins.Version;

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
			catch (DirectoryNotFoundException)
			{
				Console.WriteLine("NFive installation or plugin not found.".Red());
				Console.WriteLine("Use ", "nfpm setup".Yellow(), " to install NFive in this directory.");

				return 1;
			}

			DefinitionGraph graph;

			try
			{
				graph = DefinitionGraph.Load();
			}
			catch (FileNotFoundException)
			{
				graph = null;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):".Red());
				Console.WriteLine(ex.Message.Red());
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message.Red());

				return 1;
			}

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
	}
}
