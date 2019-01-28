using CommandLine;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Threading.Tasks;

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
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache");

			if (Directory.Exists(path)) Directory.Delete(path, true);

			Console.WriteLine("Cache directory emptied.");

			return await Task.FromResult(0);
		}
	}
}
