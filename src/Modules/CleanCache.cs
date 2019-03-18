using CommandLine;
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using NFive.PluginManager.Extensions;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Remove locally cached nfpm packages.
	/// </summary>
	[Verb("clean-cache", HelpText = "Remove locally cached nfpm packages.")]
	public class CleanCache : Module
	{
		public CleanCache(IFileSystem fs) : base(fs) { }

		public override async Task<int> Main()
		{
			var path = this.Fs.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache"); // TODO: CachePath

			if (this.Fs.Directory.Exists(path))
			{
				if (this.Verbose) Console.WriteLine("Deleting cache: ".DarkGray(), path.Gray());

				this.Fs.Directory.Delete(path, true);
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
