using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Starts a local FiveM server.
	/// </summary>
	[UsedImplicitly]
	[Verb("start", HelpText = "Starts up an installed FiveM server.")]
	internal class Start
	{
		internal async Task<int> Main()
		{
			var cd = Environment.CurrentDirectory;
			Environment.CurrentDirectory = PathManager.FindServer();

			Process.Start(PathManager.ServerFile, $"+set citizen_dir citizen +exec {PathManager.ConfigFile}");

			Environment.CurrentDirectory = cd;

			return await Task.FromResult(0);
		}
	}
}
