using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Remove locally cached nfpm packages.
	/// </summary>
	[UsedImplicitly]
	[Verb("clean-cache", HelpText = "Remove locally cached nfpm packages.")]
	internal class CleanCache
	{
		internal async Task<int> Main()
		{
			Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache"), true);

			Console.WriteLine("Cache directory emptied.");

			return await Task.FromResult(0);
		}
	}
}
