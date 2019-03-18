using CommandLine;
using System;
using System.IO;
using System.Threading.Tasks;
using NFive.PluginManager.Extensions;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Remove locally cached nfpm packages.
	/// </summary>
	[Verb("clean-cache", HelpText = "Remove locally cached nfpm packages.")]
	internal class CleanCache : Module
	{
		internal override async Task<int> Main()
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache"); // TODO: CachePath

			if (Directory.Exists(path))
			{
				if (this.Verbose) Console.WriteLine("Deleting cache: ".DarkGray(), path.Gray());

				Directory.Delete(path, true);
			}
			else
			{
				if (this.Verbose) Console.WriteLine("Cache directory does not exist: ".DarkGray(), path.Gray());
			}

			if (!this.Quiet) Console.WriteLine("Cache directory emptied.");

			return await Task.FromResult(0);
		}
	}
}
