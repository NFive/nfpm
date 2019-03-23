using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Remove locally cached nfpm packages.
	/// </summary>
	[Verb("clean-cache", HelpText = "Remove locally cached nfpm packages.")]
	public class CleanCache : Module
	{
		[UsedImplicitly]
		public CleanCache() { }

		public CleanCache(IFileSystem fs) : base(fs) { }

		public override async Task<int> Main()
		{
			if (this.Fs.Directory.Exists(PathManager.CachePath))
			{
				Output.Debug("Deleting cache: ".DarkGray(), PathManager.CachePath.Gray());

				this.Fs.Directory.Delete(PathManager.CachePath, true);
			}
			else
			{
				Output.Debug("Cache directory does not exist: ".DarkGray(), PathManager.CachePath.Gray());
			}

			Output.Info("Cache directory emptied.");

			return await Task.FromResult(0);
		}
	}
}
