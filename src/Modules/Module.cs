using CommandLine;
using NFive.PluginManager.Exceptions;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;

namespace NFive.PluginManager.Modules
{
	public abstract class Module
	{
		protected readonly IFileSystem Fs;

		[Option('q', "quiet", Default = false, Required = false, HelpText = "Quiet output.")]
		public bool Quiet
		{
			get => Output.Quiet;
			set => Output.Quiet = value;
		}

		[Option('v', "verbose", Default = false, Required = false, HelpText = "Verbose output.")]
		public bool Verbose
		{
			get => Output.Verbose;
			set => Output.Verbose = value;
		}

		[Option('S', "fivem-source", Required = false, HelpText = "Location of FiveM server core files.")]
		public string FiveMSource { get; set; } = "core";

		[Option('D', "fivem-data", Required = false, HelpText = "Location of FiveM server data files.")]
		public string FiveMData { get; set; } = "data";

		[UsedImplicitly]
		protected Module() : this(new FileSystem()) { }

		protected Module(IFileSystem fileSystem) => this.Fs = fileSystem;

		public async Task<int> Run()
		{
			try
			{
				return await this.Main();
			}
			catch (DefinitionLoadException)
			{
				Output.Error("NFive installation or plugin not found.".DarkRed());
				Output.Error("Use ", "nfpm setup".Yellow(), " to install NFive in this directory.");

				return 1;
			}
			catch (GraphLoadException ex)
			{
				Output.Error("Unable to build definition graph (PANIC):".DarkRed());
				Output.Error(ex.Message.Red());
				if (ex.InnerException != null) Output.Error(ex.InnerException.Message.Red());

				return 1;
			}
		}

		public abstract Task<int> Main();

		protected Plugin LoadDefinition(bool verbose = false)
		{
			try
			{
				if (verbose) Console.WriteLine("Searching directory tree for NFive definition...".DarkGray());

				Environment.CurrentDirectory = PathManager.FindResource(this.FiveMData);

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
	}
}
