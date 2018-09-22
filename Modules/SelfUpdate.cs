using System.IO;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update nfpm.
	/// </summary>
	[UsedImplicitly]
	[Verb("self-update", HelpText = "Update nfpm.")]
	internal class SelfUpdate
	{
		internal async Task<int> Main()
		{
			using (var client = new WebClient())
			{
				Console.WriteLine("Downloading latest nfpm...");

				var data = await client.DownloadDataTaskAsync("https://ci.appveyor.com/api/projects/NFive/nfpm/artifacts/bin/Release/nfpm.exe?branch=master");

				File.Delete("nfpm.exe.old");
				File.Move("nfpm.exe", "nfpm.exe.old");

				File.WriteAllBytes("nfpm.exe", data);
			}

			return await Task.FromResult(0);
		}
	}
}
