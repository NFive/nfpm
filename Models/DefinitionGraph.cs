using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NFive.PluginManager.Models.Plugin;
using JetBrains.Annotations;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using Version = NFive.PluginManager.Models.Plugin.Version;

namespace NFive.PluginManager.Models
{
	public class DefinitionGraph
	{
		[UsedImplicitly]
		public List<Definition> Definitions { get; set; }

		public async Task Parse(Definition definition)
		{
			await StageDefinition(definition);

			if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, VersionRange>();

			var definitions = definition.Dependencies.Select(d => Definition.Load(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", d.Key.Vendor, d.Key.Project, Program.DefinitionFile))).ToList();

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

		public async Task Apply(Definition masterDefinition)
		{
			if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"))) Directory.Delete(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"), true);

			foreach (var definition in this.Definitions)
			{
				var path = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, definition.Name.Vendor, definition.Name.Project, Program.DefinitionFile);

				if (File.Exists(path))
				{
					var loadedDefinition = Definition.Load(path);

					if (loadedDefinition.Name == definition.Name && loadedDefinition.Version == definition.Version) continue;
				}

				// Missing or outdated

				var dir = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, definition.Name.Vendor, definition.Name.Project);
				if (Directory.Exists(dir)) Directory.Delete(dir, true);


				var repo = masterDefinition.Repositories?.FirstOrDefault(r => r.Name == definition.Name); // TODO
				var adapter = new AdapterBuilder(definition.Name, repo).Adapter();
				await adapter.Download(definition.Version);

				var dependencyDefinition = Definition.Load(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", definition.Name.Vendor, definition.Name.Project, Program.DefinitionFile));

				if (dependencyDefinition.Name != definition.Name) throw new Exception("Downloaded package does not match requested.");
				if (dependencyDefinition.Version != definition.Version) throw new Exception("Downloaded package does not match requested.");

				var src = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", definition.Name.Vendor, definition.Name.Project);
				var dst = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, definition.Name.Vendor, definition.Name.Project);

				new DirectoryInfo(src).Copy(dst);
			}
		}

		private async Task StageDefinition(Definition definition)
		{
			foreach (KeyValuePair<Name, VersionRange> dependency in definition.Dependencies ?? new Dictionary<Name, VersionRange>())
			{
				var repo = definition.Repositories?.FirstOrDefault(r => r.Name == dependency.Key);

				var adapter = new AdapterBuilder(dependency.Key, repo).Adapter();

				var versions = await adapter.GetVersions();
				Version versionMatch = null;

				foreach (var version in versions)
				{
					if (!dependency.Value.IsSatisfied(version)) continue;

					versionMatch = version;
					break;
				}

				if (versionMatch == null) throw new Exception("No matching version found");

				Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", dependency.Key.Vendor, dependency.Key.Project));

				await adapter.Download(versionMatch);

				var dependencyDefinition = Definition.Load(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", dependency.Key.Vendor, dependency.Key.Project, Program.DefinitionFile));

				if (dependencyDefinition.Name != dependency.Key) throw new Exception("Downloaded package does not match requested.");
				if (dependencyDefinition.Version != versionMatch) throw new Exception("Downloaded package does not match requested.");

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
	}
}
