using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Configuration;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Models;
using NFive.PluginManager.Models.Plugin;
using Console = Colorful.Console;

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
		public IEnumerable<string> Plugin { get; set; } = new List<string>();
		internal async Task<int> Main()
		{
			var definitionPath = Path.Combine(Environment.CurrentDirectory, Program.DefinitionFile);

			if (!File.Exists(definitionPath))
			{
				Console.WriteLine($"Unable to find {definitionPath}", Color.Red);
				Console.WriteLine("Use `nfpm init` to setup NFive in this directory");

				return 1;
			}

			var lockPath = Path.Combine(Environment.CurrentDirectory, Program.LockFile);

			if (File.Exists(lockPath))
			{
				return await ProcessLock(definitionPath, lockPath);
			}
			else
			{
				return await ProcessDefinition(definitionPath);
			}
		}

		private async Task<int> ProcessLock(string definitionPath, string lockPath)
		{
			var definition = Definition.Load(definitionPath);

			DefinitionGraph graph;

			try
			{
				graph = Yaml.Deserialize<DefinitionGraph>(File.ReadAllText(lockPath));
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):", Color.Red);
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);

				return 1;
			}

			await graph.Apply(definition);

			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Program.ResourceFile), new ResourceGenerator().Serialize(graph));

			if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"))) Directory.Delete(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"), true);

			return 0;
		}

		private async Task<int> ProcessDefinition(string path)
		{
			Definition definition;

			try
			{
				definition = Definition.Load(path);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to deserialize {path}:", Color.Red);
				Console.WriteLine(ex.Message);

				return 1;
			}

			// TODO: Validate definition

			Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, Program.PluginPath)); // Create plugin dir if needed
			if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"))) Directory.Delete(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"), true);

			if (definition.Dependencies?.Count < 1) return 0; // No packages to install

			DefinitionGraph graph = new DefinitionGraph();

			try
			{
				await graph.Parse(definition); // Build and sort dependency tree
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):", Color.Red);
				Console.WriteLine(ex.Message);

				return 1;
			}

			foreach (var graphDefinition in graph.Definitions)
			{
				var src = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", graphDefinition.Name.Vendor, graphDefinition.Name.Project);
				var dst = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, graphDefinition.Name.Vendor, graphDefinition.Name.Project);

				new DirectoryInfo(src).Copy(dst);

				// TODO: Copy cfg
			}

			if (Directory.Exists(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"))) Directory.Delete(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging"), true);

			// Save locks
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Program.LockFile), Yaml.Serialize(graph));
			File.WriteAllText(Path.Combine(Environment.CurrentDirectory, Program.ResourceFile), new ResourceGenerator().Serialize(graph));

			return 0;
		}
	}
}
