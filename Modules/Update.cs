﻿using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update installed NFive plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("update", HelpText = "Update installed NFive plugins.")]
	internal class Update
	{
		internal async Task<int> Main()
		{
			Plugin plugin;

			try
			{
				Environment.CurrentDirectory = PathManager.FindResource();

				plugin = Plugin.Load(ConfigurationManager.DefinitionFile);
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
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm install` to install some dependencies first");

				return 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):", Color.Red);
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);

				return 1;
			}



			return await Task.FromResult(0);
		}
	}
}
