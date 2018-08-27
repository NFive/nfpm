using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
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
		public IEnumerable<string> Plugins { get; set; } = new List<string>();

		internal async Task<int> Main()
		{
			Definition definition;

			try
			{
				definition = Definition.Load();
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
				if (graph != null)
				{
					await graph.Apply();

					ResourceGenerator.Serialize(graph).Save();
				}
				else
				{
					graph = new DefinitionGraph();
					await graph.Build(definition);
					await graph.Apply();

					graph.Save();
					ResourceGenerator.Serialize(graph).Save();
				}

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
					definition.Dependencies[name] = version;
				}
				else
				{
					definition.Dependencies.Add(name, version);
				}
			}

			graph = new DefinitionGraph();
			await graph.Build(definition);
			await graph.Apply();

			definition.Save();
			graph.Save();
			ResourceGenerator.Serialize(graph).Save();

			return 0;
		}
	}
}
