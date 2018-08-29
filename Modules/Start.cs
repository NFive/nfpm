using System;
using System.Diagnostics;
using System.IO;
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
			Environment.CurrentDirectory = Path.Combine(Environment.CurrentDirectory, "server");
			Process.Start(Path.Combine(Environment.CurrentDirectory, "run.cmd"), "+exec server.cfg");

			return await Task.FromResult(0);
		}
	}
}
