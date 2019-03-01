using NFive.PluginManager.Exceptions;
using NFive.PluginManager.Utilities;
using NFive.SDK.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using DefinitionGraph = NFive.PluginManager.Models.DefinitionGraph;

namespace NFive.PluginManager.Modules
{
	internal abstract class Module
	{
		protected Plugin LoadDefinition()
		{
			try
			{
				Environment.CurrentDirectory = PathManager.FindResource();

				return Plugin.Load(ConfigurationManager.DefinitionFile);
			}
			catch (DirectoryNotFoundException)
			{
				throw new DefinitionLoadException();
			}
		}

		protected DefinitionGraph LoadGraph()
		{
			try
			{
				return DefinitionGraph.Load();
			}
			catch (FileNotFoundException)
			{
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
