using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NFive.SDK.Plugins.Models;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using NFive.SDK.Plugins.Configuration;

namespace NFive.PluginManager.Models
{
	public class DefinitionGraph
	{
		public List<Definition> Definitions { get; set; }

		public async Task Build(Definition definition)
		{
			await StageDefinition(definition);

			if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, VersionRange>();

			var definitions = definition.Dependencies.Select(d => Definition.Load(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", d.Key.Vendor, d.Key.Project, ConfigurationManager.DefinitionFile))).ToList();

			foreach (var plugin in definitions.Where(d => d.Dependencies != null))
			{
				foreach (var dependency in plugin.Dependencies)
				{
					var dependencyPlugin = definitions.FirstOrDefault(p => p.Name.ToString() == dependency.Key.ToString());
					if (dependencyPlugin == null) throw new Exception($"Unable to find dependency {dependency.Key}@{dependency.Value} required by {plugin.Name}@{plugin.Version}"); // TODO: DependencyException
					if (!dependency.Value.IsSatisfied(dependencyPlugin.Version)) throw new Exception($"{plugin.Name}@{plugin.Version} requires {dependencyPlugin.Name}@{dependency.Value} but {dependencyPlugin.Name}@{dependencyPlugin.Version} was found");

					if (plugin.Server == null) plugin.Server = new Server();
					if (plugin.DependencyNodes == null) plugin.DependencyNodes = new List<Definition>();
					plugin.DependencyNodes.Add(dependencyPlugin);
				}
			}

			this.Definitions = Sort(definitions);
		}

		public async Task Apply()
		{
			if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging"))) Directory.Delete(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging"), true);

			foreach (var definition in this.Definitions)
			{
				var path = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, definition.Name.Vendor, definition.Name.Project, ConfigurationManager.DefinitionFile);

				if (File.Exists(path))
				{
					var loadedDefinition = Definition.Load(path);

					if (loadedDefinition.Name == definition.Name && loadedDefinition.Version == definition.Version) continue;
				}

				// Missing or outdated

				var dir = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, definition.Name.Vendor, definition.Name.Project);
				if (Directory.Exists(dir)) Directory.Delete(dir, true);

				Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", definition.Name.Vendor, definition.Name.Project));

				Repository repo = null; // masterDefinition.Repositories?.FirstOrDefault(r => r.Name == definition.Name); // TODO
				var adapter = new AdapterBuilder(definition.Name, repo).Adapter();
				await adapter.Download(definition.Version);

				var dependencyDefinition = Definition.Load(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", definition.Name.Vendor, definition.Name.Project, ConfigurationManager.DefinitionFile));

				if (dependencyDefinition.Name != definition.Name) throw new Exception("Downloaded package does not match requested.");
				if (dependencyDefinition.Version != definition.Version) throw new Exception("Downloaded package does not match requested.");

				var src = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", definition.Name.Vendor, definition.Name.Project);
				var dst = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, definition.Name.Vendor, definition.Name.Project);
				var configSrc = Path.Combine(src, ConfigurationManager.ConfigurationPath);
				var configDst = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.ConfigurationPath, definition.Name.Vendor, definition.Name.Project);

				new DirectoryInfo(src).Copy(dst);

				if (Directory.Exists(configSrc))
				{
					Directory.CreateDirectory(configDst);

					foreach (var yml in Directory.EnumerateFiles(configSrc, "*.yml", SearchOption.TopDirectoryOnly))
					{
						File.Copy(yml, configDst, false);
					}
				}
			}

			if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging"))) Directory.Delete(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging"), true);
		}

		private async Task StageDefinition(Definition definition)
		{
			foreach (var dependency in definition.Dependencies ?? new Dictionary<Name, VersionRange>())
			{
				var repo = definition.Repositories?.FirstOrDefault(r => r.Name == dependency.Key);

				var adapter = new AdapterBuilder(dependency.Key, repo).Adapter();

				var versions = await adapter.GetVersions();

				var versionMatch = versions.LastOrDefault(version => dependency.Value.IsSatisfied(version));
				if (versionMatch == null) throw new Exception("No matching version found");

				Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", dependency.Key.Vendor, dependency.Key.Project));

				await adapter.Download(versionMatch);

				var dependencyDefinition = Definition.Load(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", dependency.Key.Vendor, dependency.Key.Project, ConfigurationManager.DefinitionFile));

				// TODO: What should be validated?
				//if (dependencyDefinition.Name != dependency.Key) throw new Exception("Downloaded package does not match requested.");
				//if (dependencyDefinition.Version != versionMatch) throw new Exception("Downloaded package does not match requested.");

				await StageDefinition(dependencyDefinition);
			}
		}

		private static List<Definition> Sort(List<Definition> definitions)
		{
			var results = new List<Definition>();

			Visit(definitions, results, new List<Definition>(), new List<Definition>());

			return results;
		}

		private static void Visit(IEnumerable<Definition> graph, ICollection<Definition> results, ICollection<Definition> dead, ICollection<Definition> pending)
		{
			foreach (var node in graph)
			{
				if (dead.Contains(node)) continue;

				if (pending.Contains(node)) throw new Exception($"Cycle detected (node {node.Name})");
				pending.Add(node);

				Visit(node.DependencyNodes ?? new List<Definition>(), results, dead, pending);

				if (pending.Contains(node)) pending.Remove(node);

				dead.Add(node);

				results.Add(node);
			}
		}

		public static DefinitionGraph Load(string path = ConfigurationManager.LockFile)
		{
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
			if (!File.Exists(path)) throw new FileNotFoundException("Unable to find the plugin lock file", path);

			return Yaml.Deserialize<DefinitionGraph>(File.ReadAllText(path));
		}

		public void Save(string path = ConfigurationManager.LockFile)
		{
			File.WriteAllText(path, Yaml.Serialize(this));
		}
	}
}
