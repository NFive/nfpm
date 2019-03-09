using NFive.PluginManager.Exceptions;
using NFive.PluginManager.Utilities;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using NFive.PluginManager.Extensions;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;

namespace NFive.PluginManager.Modules
{
	internal abstract class Module
	{
		[Option('q', "quiet", Default = false, Required = false, HelpText = "Quiet output.")]
		public bool Quiet { get; set; }

		[Option('v', "verbose", Default = false, Required = false, HelpText = "Verbose output.")]
		public bool Verbose { get; set; }

		protected Plugin LoadDefinition(bool verbose = false)
		{
			try
			{
				if (verbose) Console.WriteLine("Searching directory tree for NFive definition...".DarkGray());

				Environment.CurrentDirectory = PathManager.FindResource();

				if (verbose) Console.WriteLine("Setting working directory: ".DarkGray(), Environment.CurrentDirectory.Gray());

				if (verbose) Console.WriteLine("Loading definition: ".DarkGray(), ConfigurationManager.DefinitionFile.Gray());

				return Plugin.Load(ConfigurationManager.DefinitionFile);
			}
			catch (DirectoryNotFoundException)
			{
				throw new DefinitionLoadException();
			}
		}

		protected DefinitionGraph LoadGraph(bool verbose = false)
		{
			try
			{
				if (verbose) Console.WriteLine("Loading graph: ".DarkGray(), ConfigurationManager.LockFile.Gray());

				return DefinitionGraph.Load();
			}
			catch (FileNotFoundException)
			{
				if (verbose) Console.WriteLine("Unable to find graph file".DarkGray());

				return null;
			}
			catch (Exception ex)
			{
				throw new GraphLoadException(ex);
			}
		}

		internal abstract Task<int> Main();
	}
}
