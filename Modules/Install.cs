using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Models;
using NFive.SDK.Plugins.Configuration;
using NFive.SDK.Plugins.Models;
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
		public IEnumerable<string> Plugins { get; set; } = new List<string>();

		internal async Task<int> Main()
		{
			Definition definition;

			try
			{
				Environment.CurrentDirectory = PathManager.FindResource();

				definition = Definition.Load(ConfigurationManager.DefinitionFile);
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm init` to setup NFive in this directory");

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
				Console.WriteLine("Unable to build definition graph (PANIC):", Color.Red);
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

					await graph.Apply();

					if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();
				}
				else
				{
					Console.WriteLine("Building new tree...");

					graph = new DefinitionGraph();
					await graph.Build(definition);

					Console.WriteLine("Applying dependencies...");

					await graph.Apply();

					graph.Save();

					if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();
				}

				Console.WriteLine("Finished");

				return 0;
			}

			foreach (var plugin in this.Plugins)
			{
				var parts = plugin.Split(new[] { '@' }, 2);
				Name name;
				var version = new VersionRange("*");

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
						version = new VersionRange(parts[1]);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						return 1;
					}
				}
				
				if (definition.Dependencies == null) definition.Dependencies = new Dictionary<Name, VersionRange>();

				if (definition.Dependencies.ContainsKey(name))
				{
					if (definition.Dependencies[name] == version)
					{
						Console.WriteLine($"Plugin \"{name}@{version}\" is already installed", Color.Yellow);
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
			await graph.Apply();

			definition.Save(ConfigurationManager.DefinitionFile);
			graph.Save();

			if (PathManager.IsResource()) ResourceGenerator.Serialize(graph).Save();

			Console.WriteLine("Finished");

			return 0;
		}
	}
}
