using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plugin = NFive.SDK.Plugins.Plugin;

namespace NFive.PluginManager.Models
{
	public class DefinitionGraph : SDK.Plugins.DefinitionGraph
	{
		public async Task Apply(Plugin baseDefinition = null)
		{
			if (baseDefinition != null)
			{
				var results = await StageDefinition(baseDefinition);
				var top = results.Item1;
				var all = top.Concat(results.Item2).Distinct().ToList();

				foreach (var plugin in top.Where(d => d.Dependencies != null))
				{
					foreach (var dependency in plugin.Dependencies)
					{
						var dependencyPlugin = all.FirstOrDefault(p => p.Name == dependency.Key);
						if (dependencyPlugin == null) throw new Exception($"Unable to find dependency {dependency.Key}@{dependency.Value} required by {plugin.Name}@{plugin.Version}"); // TODO: DependencyException
						if (!dependency.Value.IsSatisfied(dependencyPlugin.Version)) throw new Exception($"{plugin.Name}@{plugin.Version} requires {dependencyPlugin.Name}@{dependency.Value} but {dependencyPlugin.Name}@{dependencyPlugin.Version} was found");

						if (plugin.DependencyNodes == null) plugin.DependencyNodes = new List<Plugin>();
						plugin.DependencyNodes.Add(dependencyPlugin);
					}
				}

				this.Plugins = top;
			}

			this.Plugins = Sort(this.Plugins); // TODO: Don't store nested dependencies but still load them

			foreach (var definition in this.Plugins)
			{
				var path = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, definition.Name.Vendor, definition.Name.Project, ConfigurationManager.DefinitionFile);

				if (File.Exists(path))
				{
					var loadedDefinition = Plugin.Load(path);

					// TODO: IEquality
					if (loadedDefinition.Name.ToString() == definition.Name.ToString() && loadedDefinition.Version.ToString() == definition.Version.ToString()) continue;
				}

				// TODO: Remove extra plugin folders/files

				// Missing or outdated

				var dir = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, definition.Name.Vendor, definition.Name.Project);
				if (Directory.Exists(dir)) DeleteDirectory(dir);
				Directory.CreateDirectory(dir);

				var repo = baseDefinition?.Repositories?.FirstOrDefault(r => r.Name == definition.Name);
				var adapter = new AdapterBuilder(definition.Name, repo).Adapter();
				await adapter.Download(new Version(definition.Version.ToString()));

				var dependencyDefinition = Plugin.Load(Path.Combine(dir, ConfigurationManager.DefinitionFile));

				if (dependencyDefinition.Name != definition.Name) throw new Exception("Downloaded package does not match requested.");
				if (dependencyDefinition.Version.ToString() != definition.Version.ToString()) throw new Exception("Downloaded package does not match requested.");

				var configSrc = Path.Combine(dir, ConfigurationManager.ConfigurationPath);
				var configDst = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.ConfigurationPath, definition.Name.Vendor, definition.Name.Project);

				if (!Directory.Exists(configSrc)) continue;
				if (!Directory.Exists(configDst)) Directory.CreateDirectory(configDst);

				// TODO: More files?
				// TODO: Ask user to replace
				foreach (var yml in Directory.EnumerateFiles(configSrc, "*.yml", SearchOption.TopDirectoryOnly))
				{
					try
					{
						File.Copy(yml, Path.Combine(configDst, Path.GetFileName(yml)), false);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				}
			}
		}

		private static async Task<Tuple<List<Plugin>, List<Plugin>>> StageDefinition(SDK.Core.Plugins.Plugin definition, IDictionary<Name, Tuple<SDK.Core.Plugins.VersionRange, Plugin>> loaded = null)
		{
			var top = new List<Plugin>();
			var nested = new List<Plugin>();

			foreach (var dependency in definition.Dependencies ?? new Dictionary<Name, SDK.Core.Plugins.VersionRange>())
			{
				var repo = definition.Repositories?.FirstOrDefault(r => r.Name.ToString() == dependency.Key.ToString());
				var adapter = new AdapterBuilder(dependency.Key, repo).Adapter();

				var versions = await adapter.GetVersions();
				var versionMatch = versions.LastOrDefault(version => dependency.Value.IsSatisfied(version));
				if (versionMatch == null) throw new Exception("No matching version found");

				if (loaded == null) loaded = new Dictionary<Name, Tuple<SDK.Core.Plugins.VersionRange, Plugin>>();

				if (loaded.ContainsKey(dependency.Key))
				{
					if (dependency.Value.Value != "*" && loaded[dependency.Key].Item1.Value != "*" && !dependency.Value.IsSatisfied(loaded[dependency.Key].Item2.Version)) throw new Exception($"{dependency.Key} was found");
				}

				var localPath = await adapter.Cache(versionMatch);

				var plugin = Plugin.Load(Path.Combine(localPath, ConfigurationManager.DefinitionFile));

				if (loaded.ContainsKey(dependency.Key))
				{
					loaded.Add(dependency.Key, new Tuple<SDK.Core.Plugins.VersionRange, Plugin>(dependency.Value, plugin));
				}

				top.Add(plugin);

				// TODO: What should be validated?
				//if (plugin.Name != dependency.Key) throw new Exception("Downloaded package does not match requested.");
				//if (plugin.Version != versionMatch) throw new Exception("Downloaded package does not match requested.");

				nested.AddRange((await StageDefinition(plugin, loaded)).Item1);
			}

			return new Tuple<List<Plugin>, List<Plugin>>(top, nested);
		}

		private static List<Plugin> Sort(IEnumerable<Plugin> plugins)
		{
			var results = new List<Plugin>();

			Visit(plugins, results, new List<Plugin>(), new List<Plugin>());

			return results;
		}

		private static void Visit(IEnumerable<Plugin> graph, ICollection<Plugin> results, ICollection<Plugin> dead, ICollection<Plugin> pending)
		{
			foreach (var node in graph)
			{
				if (dead.Contains(node)) continue;

				if (pending.Contains(node)) throw new Exception($"Cycle detected (node {node.Name})");
				pending.Add(node);

				Visit(node.DependencyNodes ?? new List<Plugin>(), results, dead, pending);

				if (pending.Contains(node)) pending.Remove(node);

				dead.Add(node);

				results.Add(node);
			}
		}

		public new static DefinitionGraph Load(string path = ConfigurationManager.LockFile)
		{
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
			if (!File.Exists(path)) throw new FileNotFoundException("Unable to find the plugin lock file", path);

			return Yaml.Deserialize<DefinitionGraph>(File.ReadAllText(path));
		}

		public new void Save(string path = ConfigurationManager.LockFile)
		{
			File.WriteAllText(path, Yaml.Serialize(this));
		}

		private static void NormalizeAttributes(string directoryPath)
		{
			foreach (var file in Directory.GetFiles(directoryPath)) File.SetAttributes(file, FileAttributes.Normal);

			foreach (var directory in Directory.GetDirectories(directoryPath)) NormalizeAttributes(directory);

			File.SetAttributes(directoryPath, FileAttributes.Normal);
		}

		private static void DeleteDirectory(string directoryPath)
		{
			try
			{
				NormalizeAttributes(directoryPath);

				Directory.Delete(directoryPath, true);
			}
			catch (Exception ex)
			{
				if (!new[] { typeof(IOException), typeof(UnauthorizedAccessException) }.Any(e => e.IsInstanceOfType(ex))) throw;
			}
		}
	}
}
