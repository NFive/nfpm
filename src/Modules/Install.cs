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
using Version = NFive.PluginManager.Models.Version;

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

		public override async Task<int> Main()
		{
			var path = Path.GetFullPath(Environment.CurrentDirectory);

			var definition = LoadDefinition();
			var graph = LoadGraph();

			// New plugins
			if (this.Plugins.Any())
			{
				foreach (var plugin in this.Plugins)
				{
					var input = plugin;

					// Local install
					if (Directory.Exists(Path.Combine(path, plugin)) && File.Exists(Path.Combine(path, plugin, ConfigurationManager.DefinitionFile)) || Directory.Exists(plugin) && File.Exists(Path.Combine(plugin, ConfigurationManager.DefinitionFile)))
					{
						if (Directory.Exists(Path.Combine(path, plugin)) && File.Exists(Path.Combine(path, plugin, ConfigurationManager.DefinitionFile)))
						{
							var current = new Uri(Environment.CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
							var target = new Uri(Path.GetFullPath(Path.Combine(path, plugin)).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
							path = Uri.UnescapeDataString(current.MakeRelativeUri(target).OriginalString.Replace('/', Path.DirectorySeparatorChar));
						}
						else
						{
							path = Path.GetFullPath(plugin);
						}

						var pluginDefinition = Plugin.Load(Path.Combine(Path.GetFullPath(path), ConfigurationManager.DefinitionFile));

						if (definition.Repositories == null) definition.Repositories = new List<Repository>();

						definition.Repositories.RemoveAll(r => r.Name == pluginDefinition.Name);
						definition.Repositories.Add(new Repository
						{
							Name = pluginDefinition.Name,
							Type = "local",
							Path = path
						});

						input = pluginDefinition.Name;
					}

					var parts = input.Split(new[] { '@' }, 2);
					var name = new Name(parts[0].Trim());

					var versionInput = parts.Length == 2 ? parts[1].Trim() : "*";

					Models.VersionRange range = null;
					Version version = null;
					PartialVersion partial = null;

					try
					{
						partial = new PartialVersion(versionInput);
					}
					catch (Exception)
					{
						// ignored
					}

					var isSpecific = partial?.Major != null && partial.Minor.HasValue && partial.Patch.HasValue;

					try
					{
						range = new Models.VersionRange(versionInput);
					}
					catch (Exception)
					{
						// ignored
					}

					try
					{
						version = new Version(versionInput);
					}
					catch (Exception)
					{
						// ignored
					}

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

					var versionMatch = range.MaxSatisfying(versions);
					if (versionMatch == null)
					{
						Console.WriteLine("Error ".DarkRed(), $"{name}@{range}".Red(), " not found, available versions: ".DarkRed(), string.Join(" ", versions.Select(v => v.ToString())).Red());

						return 1;
					}

					if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, SDK.Core.Plugins.VersionRange>();
					definition.Dependencies[name] = new Models.VersionRange("^" + (isSpecific ? partial.ToZeroVersion() : version ?? versionMatch));

					Console.WriteLine("+ ", $"{name}@{definition.Dependencies[name]}".White());
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
