using System.Diagnostics;
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
			var file = Path.GetFullPath(Process.GetCurrentProcess().MainModule.FileName);

			Console.WriteLine("Downloading latest nfpm...");

			using (var client = new WebClient())
			{
				var data = await client.DownloadDataTaskAsync("https://ci.appveyor.com/api/projects/NFive/nfpm/artifacts/bin/Release/nfpm.exe?branch=master");

				File.Delete($"{file}.old");
				File.Move(file, $"{file}.old");

				File.WriteAllBytes(file, data);
			}

			return await Task.FromResult(0);
		}

		internal static void Cleanup()
		{
			File.Delete($"{Path.GetFullPath(Process.GetCurrentProcess().MainModule.FileName)}.old");
		}
	}
}
